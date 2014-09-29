using System.Collections.Generic;
using System.Linq;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.nio.channels;
using java.text;
using java.util;

using OpenRSS.JavaAPI;
using OpenRSS.Utility;

namespace OpenRSS.Cache
{
    /// <summary>
    ///     A file store holds multiple files inside a "virtual" file system made up of
    ///     several index files and a single data file.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public sealed class FileStore : AbstractCloseable
    {
        /// <summary>
        ///     The data file.
        /// </summary>
        private readonly FileChannel dataChannel;

        /// <summary>
        ///     The index files.
        /// </summary>
        private readonly FileChannel[] indexChannels;

        /// <summary>
        ///     The 'meta' index files.
        /// </summary>
        private readonly FileChannel metaChannel;

        /// <summary>
        ///     Creates a new file store.
        /// </summary>
        /// <param name="data"> The data file. </param>
        /// <param name="indexes"> The index files. </param>
        /// <param name="meta"> The 'meta' index file. </param>
        public FileStore(FileChannel data, FileChannel[] indexes, FileChannel meta)
        {
            dataChannel = data;
            indexChannels = indexes;
            metaChannel = meta;
        }

        /// <summary>
        ///     Opens the file store stored in the specified directory.
        /// </summary>
        /// <param name="root"> The directory containing the index and data files. </param>
        /// <returns> The file store. </returns>
        /// <exception cref="FileNotFoundException">
        ///     if any of the {@code main_file_cache.*}
        ///     files could not be found.
        /// </exception>
        public static FileStore Open(string root)
        {
            return Open(new File(root));
        }

        /// <summary>
        ///     Opens the file store stored in the specified directory.
        /// </summary>
        /// <param name="root"> The directory containing the index and data files. </param>
        /// <returns> The file store. </returns>
        /// <exception cref="FileNotFoundException">
        ///     if any of the {@code main_file_cache.*}
        ///     files could not be found.
        /// </exception>
        public static FileStore Open(File root)
        {
            var data = new File(root, "main_file_cache.dat2");
            if (!data.exists())
            {
                throw new FileNotFoundException();
            }

            var raf = new RandomAccessFile(data, "rw");
            var dataChannel = raf.getChannel();

            var indexChannels = new List<FileChannel>();
            for (var i = 0; i < 254; i++)
            {
                var index = new File(root, "main_file_cache.idx" + i);
                if (!index.exists())
                {
                    break;
                }

                raf = new RandomAccessFile(index, "rw");
                var indexChannel = raf.getChannel();
                indexChannels.Add(indexChannel);
            }

            if (indexChannels.Count == 0)
            {
                throw new FileNotFoundException();
            }

            var meta = new File(root, "main_file_cache.idx255");
            if (!meta.exists())
            {
                throw new FileNotFoundException();
            }

            raf = new RandomAccessFile(meta, "rw");
            var metaChannel = raf.getChannel();

            return new FileStore(dataChannel, indexChannels.ToArray(), metaChannel);
        }

        /// <summary>
        ///     Gets the number of index files, not including the meta index file.
        /// </summary>
        /// <returns> The number of index files. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public int GetTypeCount()
        {
            return indexChannels.Length;
        }

        /// <summary>
        ///     Gets the number of files of the specified type.
        /// </summary>
        /// <param name="type"> The type. </param>
        /// <returns> The number of files. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public int GetFileCount(int type)
        {
            if ((type < 0 || type >= indexChannels.Length) && type != 255)
            {
                throw new FileNotFoundException();
            }

            if (type == 255)
            {
                return (int) (metaChannel.size() / Index.SIZE);
            }
            return (int) (indexChannels[type].size() / Index.SIZE);
        }

        /// <summary>
        ///     Writes a file.
        /// </summary>
        /// <param name="type"> The type of the file. </param>
        /// <param name="id"> The id of the file. </param>
        /// <param name="data"> A <seealso cref="ByteBuffer" /> containing the contents of the file. </param>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public void Write(int type, int id, ByteBuffer data)
        {
            data.mark();
            if (!Write(type, id, data, true))
            {
                data.reset();
                Write(type, id, data, false);
            }
        }

        /// <summary>
        ///     Writes a file.
        /// </summary>
        /// <param name="type"> The type of the file. </param>
        /// <param name="id"> The id of the file. </param>
        /// <param name="data"> A <seealso cref="ByteBuffer" /> containing the contents of the file. </param>
        /// <param name="overwrite">
        ///     A flag indicating if the existing file should be
        ///     overwritten.
        /// </param>
        /// <returns> A flag indicating if the file was written successfully. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        private bool Write(int type, int id, ByteBuffer data, bool overwrite)
        {
            if ((type < 0 || type >= indexChannels.Length) && type != 255)
            {
                throw new FileNotFoundException();
            }

            var indexChannel = type == 255 ? metaChannel : indexChannels[type];

            var nextSector = 0;
            long ptr = id * Index.SIZE;
            ByteBuffer buf;
            Index index;
            if (overwrite)
            {
                if (ptr < 0)
                {
                    throw new IOException();
                }
                if (ptr >= indexChannel.size())
                {
                    return false;
                }

                buf = ByteBuffer.allocate(Index.SIZE);
                FileChannelUtils.ReadFully(indexChannel, buf, ptr);

                index = Index.Decode((ByteBuffer) buf.flip());
                nextSector = index.GetSector();
                if (nextSector <= 0 || nextSector > dataChannel.size() * Sector.SIZE)
                {
                    return false;
                }
            }
            else
            {
                nextSector = (int) ((dataChannel.size() + Sector.SIZE - 1) / Sector.SIZE);
                if (nextSector == 0)
                {
                    nextSector = 1;
                }
            }

            index = new Index(data.remaining(), nextSector);
            indexChannel.write(index.Encode(), ptr);

            buf = ByteBuffer.allocate(Sector.SIZE);

            int chunk = 0,
                remaining = index.GetSize();
            do
            {
                var curSector = nextSector;
                ptr = curSector * Sector.SIZE;
                nextSector = 0;

                Sector sector;
                if (overwrite)
                {
                    buf.clear();
                    FileChannelUtils.ReadFully(dataChannel, buf, ptr);

                    sector = Sector.Decode((ByteBuffer) buf.flip());

                    if (sector.GetType() != type)
                    {
                        return false;
                    }

                    if (sector.GetId() != id)
                    {
                        return false;
                    }

                    if (sector.GetChunk() != chunk)
                    {
                        return false;
                    }

                    nextSector = sector.GetNextSector();
                    if (nextSector < 0 || nextSector > dataChannel.size() / Sector.SIZE)
                    {
                        return false;
                    }
                }

                if (nextSector == 0)
                {
                    overwrite = false;
                    nextSector = (int) ((dataChannel.size() + Sector.SIZE - 1) / Sector.SIZE);
                    if (nextSector == 0)
                    {
                        nextSector++;
                    }
                    if (nextSector == curSector)
                    {
                        nextSector++;
                    }
                }

                var bytes = new byte[Sector.DATA_SIZE];
                if (remaining < Sector.DATA_SIZE)
                {
                    data.get(bytes, 0, remaining);
                    nextSector = 0; // mark as EOF
                    remaining = 0;
                }
                else
                {
                    remaining -= Sector.DATA_SIZE;
                    data.get(bytes, 0, Sector.DATA_SIZE);
                }

                sector = new Sector(type, id, chunk++, nextSector, bytes);
                dataChannel.write(sector.Encode(), ptr);
            }
            while (remaining > 0);

            return true;
        }

        /// <summary>
        ///     Reads a file.
        /// </summary>
        /// <param name="type"> The type of the file. </param>
        /// <param name="id"> The id of the file. </param>
        /// <returns> A <seealso cref="ByteBuffer" /> containing the contents of the file. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public ByteBuffer Read(int type, int id)
        {
            if ((type < 0 || type >= indexChannels.Length) && type != 255)
            {
                throw new FileNotFoundException();
            }

            var indexChannel = type == 255 ? metaChannel : indexChannels[type];

            long ptr = id * Index.SIZE;
            if (ptr < 0 || ptr >= indexChannel.size())
            {
                throw new FileNotFoundException();
            }

            var buf = ByteBuffer.allocate(Index.SIZE);
            FileChannelUtils.ReadFully(indexChannel, buf, ptr);

            var index = Index.Decode((ByteBuffer) buf.flip());

            var data = ByteBuffer.allocate(index.GetSize());
            buf = ByteBuffer.allocate(Sector.SIZE);

            int chunk = 0,
                remaining = index.GetSize();
            ptr = index.GetSector() * Sector.SIZE;
            do
            {
                buf.clear();
                FileChannelUtils.ReadFully(dataChannel, buf, ptr);
                var sector = Sector.Decode((ByteBuffer) buf.flip());

                if (remaining > Sector.DATA_SIZE)
                {
                    data.put(sector.GetData(), 0, Sector.DATA_SIZE);
                    remaining -= Sector.DATA_SIZE;

                    if (sector.GetType() != type)
                    {
                        throw new IOException("File type mismatch.");
                    }

                    if (sector.GetId() != id)
                    {
                        throw new IOException("File id mismatch.");
                    }

                    if (sector.GetChunk() != chunk++)
                    {
                        throw new IOException("Chunk mismatch.");
                    }

                    ptr = sector.GetNextSector() * Sector.SIZE;
                }
                else
                {
                    data.put(sector.GetData(), 0, remaining);
                    remaining = 0;
                }
            }
            while (remaining > 0);

            return (ByteBuffer) data.flip();
        }
        public override void Close()
        {
            dataChannel.close();

            foreach (var channel in indexChannels)
            {
                channel.close();
            }

            metaChannel.close();
        }
    }
}
