using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;
using java.util.zip;

using OpenRSS.Extensions;
using RSUtilities.Cryptography;
using RSUtilities.Tools;

namespace OpenRSS.Cache
{
    /// <summary>
    ///     The <seealso cref="Cache" /> class provides a unified, high-level API for modifying
    ///     the cache of a Jagex game.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public sealed class Cache : AbstractCloseable
    {
        /// <summary>
        ///     The file store that backs this cache.
        /// </summary>
        private readonly FileStore store;

        /// <summary>
        ///     Creates a new <seealso cref="Cache" /> backed by the specified <seealso cref="FileStore" />.
        /// </summary>
        /// <param name="store"> The <seealso cref="FileStore" /> that backs this <seealso cref="Cache" />. </param>
        public Cache(FileStore store)
        {
            this.store = store;
        }

        /// <summary>
        ///     Gets the number of index files, not including the meta index file.
        /// </summary>
        /// <returns> The number of index files. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public int GetTypeCount()
        {
            return store.GetTypeCount();
        }

        /// <summary>
        ///     Gets the number of files of the specified type.
        /// </summary>
        /// <param name="type"> The type. </param>
        /// <returns> The number of files. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public int GetFileCount(int type)
        {
            return store.GetFileCount(type);
        }

        /// <summary>
        ///     Gets the <seealso cref="FileStore" /> that backs this <seealso cref="Cache" />.
        /// </summary>
        /// <returns> The underlying file store. </returns>
        public FileStore GetStore()
        {
            return store;
        }

        /// <summary>
        ///     Computes the <seealso cref="ChecksumTable" /> for this cache. The checksum table
        ///     forms part of the so-called "update keys".
        /// </summary>
        /// <returns> The <seealso cref="ChecksumTable" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public ChecksumTable CreateChecksumTable()
        {
            /* create the checksum table */
            var size = store.GetTypeCount();
            var table = new ChecksumTable(size);

            /* loop through all the reference tables and get their CRC and versions */
            for (var i = 0; i < size; i++)
            {
                var buf = store.Read(255, i);

                var crc = 0;
                var version = 0;
                var whirlpool = new byte[64];

                /* 
                 * if there is actually a reference table, calculate the CRC,
                 * version and whirlpool hash
                 */
                if (buf.limit() > 0) // some indices are not used, is this appropriate?
                {
                    var @ref = ReferenceTable.Decode(Container.Decode(buf)
                                                              .GetData());
                    crc = ByteBufferExtensions.GetCrcChecksum(buf);
                    version = @ref.GetVersion();
                    buf.position(0);
                    whirlpool = ByteBufferExtensions.GetWhirlpoolDigest(buf);
                }

                table.SetEntry(i, new ChecksumTable.Entry(crc, version, whirlpool));
            }

            /* return the table */
            return table;
        }

        /// <summary>
        ///     Reads a file from the cache.
        /// </summary>
        /// <param name="type"> The type of file. </param>
        /// <param name="file"> The file id. </param>
        /// <returns> The file. </returns>
        /// <exception cref="IOException"> if an I/O error occurred. </exception>
        public Container Read(int type, int file)
        {
            /* we don't want people reading/manipulating these manually */
            if (type == 255)
            {
                throw new IOException("Reference tables can only be read with the low level FileStore API!");
            }

            /* delegate the call to the file store then decode the container */
            return Container.Decode(store.Read(type, file));
        }

        /// <summary>
        ///     Writes a file to the cache and updates the <seealso cref="ReferenceTable" /> that
        ///     it is associated with.
        /// </summary>
        /// <param name="type"> The type of file. </param>
        /// <param name="file"> The file id. </param>
        /// <param name="container"> The <seealso cref="Container" /> to write. </param>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public void Write(int type, int file, Container container)
        {
            /* we don't want people reading/manipulating these manually */
            if (type == 255)
            {
                throw new IOException("Reference tables can only be modified with the low level FileStore API!");
            }

            /* increment the container's version */
            container.SetVersion(container.GetVersion() + 1);

            /* decode the reference table for this index */
            var tableContainer = Container.Decode(store.Read(255, type));
            var table = ReferenceTable.Decode(tableContainer.GetData());

            /* grab the bytes we need for the checksum */
            var buffer = container.Encode();
            var bytes = new byte[buffer.limit() - 2]; // last two bytes are the version and shouldn't be included
            buffer.mark();
            try
            {
                buffer.position(0);
                buffer.get(bytes, 0, bytes.Length);
            }
            finally
            {
                buffer.reset();
            }

            /* calculate the new CRC checksum */
            var crc = new CRC32();
            crc.update(bytes, 0, bytes.Length);

            /* update the version and checksum for this file */
            var entry = table.GetEntry(file);
            if (entry == null)
            {
                /* create a new entry for the file */
                entry = new ReferenceTable.Entry();
                table.PutEntry(file, entry);
            }
            entry.SetVersion(container.GetVersion());
            entry.SetCrc((int) crc.getValue());

            /* calculate and update the whirlpool digest if we need to */
            if ((table.GetFlags() & ReferenceTable.FLAG_WHIRLPOOL) != 0)
            {
                var whirlpool = Whirlpool.Crypt(bytes, 0, bytes.Length);
                entry.SetWhirlpool(whirlpool);
            }

            /* update the reference table version */
            table.SetVersion(table.GetVersion() + 1);

            /* save the reference table */
            tableContainer = new Container(tableContainer.GetType(), table.Encode());
            store.Write(255, type, tableContainer.Encode());

            /* save the file itself */
            store.Write(type, file, buffer);
        }

        /// <summary>
        ///     Reads a file contained in an archive in the cache.
        /// </summary>
        /// <param name="type"> The type of the file. </param>
        /// <param name="file"> The archive id. </param>
        /// <param name="file"> The file within the archive. </param>
        /// <returns> The file. </returns>
        /// <exception cref="IOException"> if an I/O error occurred. </exception>
        public ByteBuffer Read(int type, int file, int member)
        {
            /* grab the container and the reference table */
            var container = Read(type, file);
            var tableContainer = Container.Decode(store.Read(255, type));
            var table = ReferenceTable.Decode(tableContainer.GetData());

            /* check if the file/member are valid */
            var entry = table.GetEntry(file);
            if (entry == null || member < 0 || member >= entry.Capacity())
            {
                throw new FileNotFoundException();
            }

            /* extract the entry from the archive */
            var archive = Archive.Decode(container.GetData(), entry.Capacity());
            return archive.GetEntry(member);
        }

        /// <summary>
        ///     Writes a file contained in an archive to the cache.
        /// </summary>
        /// <param name="type"> The type of file. </param>
        /// <param name="file"> The id of the archive. </param>
        /// <param name="member"> The file within the archive. </param>
        /// <param name="data"> The data to write. </param>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public void Write(int type, int file, int member, ByteBuffer data)
        {
            /* grab the reference table */
            var tableContainer = Container.Decode(store.Read(255, type));
            var table = ReferenceTable.Decode(tableContainer.GetData());

            /* create a new entry if necessary */
            var entry = table.GetEntry(file);
            var oldArchiveSize = -1;
            if (entry == null)
            {
                entry = new ReferenceTable.Entry();
                table.PutEntry(file, entry);
            }
            else
            {
                oldArchiveSize = entry.Capacity();
            }

            /* add a child entry if one does not exist */
            var child = entry.GetEntry(member);
            if (child == null)
            {
                child = new ReferenceTable.ChildEntry();
                entry.PutEntry(member, child);
            }

            /* extract the current archive into memory so we can modify it */
            Archive archive;
            int containerType,
                containerVersion;
            Container container;
            if (file < store.GetFileCount(type) && oldArchiveSize != -1)
            {
                container = Read(type, file);
                containerType = container.GetType();
                containerVersion = container.GetVersion();
                archive = Archive.Decode(container.GetData(), oldArchiveSize);
            }
            else
            {
                containerType = Container.COMPRESSION_GZIP;
                containerVersion = 1;
                archive = new Archive(member + 1);
            }

            /* expand the archive if it is not large enough */
            if (member >= archive.Size())
            {
                var newArchive = new Archive(member + 1);
                for (var id = 0; id < archive.Size(); id++)
                {
                    newArchive.PutEntry(id, archive.GetEntry(id));
                }
                archive = newArchive;
            }

            /* put the member into the archive */
            archive.PutEntry(member, data);

            /* create 'dummy' entries */
            for (var id = 0; id < archive.Size(); id++)
            {
                if (archive.GetEntry(id) == null)
                {
                    entry.PutEntry(id, new ReferenceTable.ChildEntry());
                    archive.PutEntry(id, ByteBuffer.allocate(1));
                }
            }

            /* write the reference table out again */
            tableContainer = new Container(tableContainer.GetType(), table.Encode());
            store.Write(255, type, tableContainer.Encode());

            /* and write the archive back to memory */
            container = new Container(containerType, archive.Encode(), containerVersion);
            Write(type, file, container);
        }
        public override void Close()
        {
            store.Close();
        }
    }
}
