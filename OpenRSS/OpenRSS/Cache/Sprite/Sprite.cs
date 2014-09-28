using System;
using System.Collections.Generic;

using java.awt.image;
using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using net.openrs.util;

namespace net.openrs.cache.sprite
{
    /// <summary>
    ///     Represents a <seealso cref="Sprite" /> which may contain one or more frames.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public sealed class Sprite
    {
        /// <summary>
        ///     This flag indicates that the pixels should be read vertically instead of
        ///     horizontally.
        /// </summary>
        public const int FLAG_VERTICAL = 0x01;

        /// <summary>
        ///     This flag indicates that every pixel has an alpha, as well as red, green
        ///     and blue, component.
        /// </summary>
        public const int FLAG_ALPHA = 0x02;

        /// <summary>
        ///     The array of animation frames in this sprite.
        /// </summary>
        private readonly BufferedImage[] frames;

        /// <summary>
        ///     The height of this sprite.
        /// </summary>
        private readonly int height;

        /// <summary>
        ///     The width of this sprite.
        /// </summary>
        private readonly int width;

        /// <summary>
        ///     Creates a new sprite with one frame.
        /// </summary>
        /// <param name="width"> The width of the sprite in pixels. </param>
        /// <param name="height"> The height of the sprite in pixels. </param>
        public Sprite(int width, int height)
            : this(width, height, 1)
        { }

        /// <summary>
        ///     Creates a new sprite with the specified number of frames.
        /// </summary>
        /// <param name="width"> The width of the sprite in pixels. </param>
        /// <param name="height"> The height of the sprite in pixels. </param>
        /// <param name="size"> The number of animation frames. </param>
        public Sprite(int width, int height, int size)
        {
            if (size < 1)
            {
                throw new ArgumentException();
            }

            this.width = width;
            this.height = height;
            frames = new BufferedImage[size];
        }

        /// <summary>
        ///     Decodes the <seealso cref="Sprite" /> from the specified <seealso cref="ByteBuffer" />.
        /// </summary>
        /// <param name="buffer"> The buffer. </param>
        /// <returns> The sprite. </returns>
        public static Sprite Decode(ByteBuffer buffer)
        {
            /* find the size of this sprite set */
            buffer.position(buffer.limit() - 2);
            var size = buffer.getShort() & 0xFFFF;

            /* allocate arrays to store info */
            var offsetsX = new int[size];
            var offsetsY = new int[size];
            var subWidths = new int[size];
            var subHeights = new int[size];

            /* read the width, height and palette size */
            buffer.position(buffer.limit() - size * 8 - 7);
            var width = buffer.getShort() & 0xFFFF;
            var height = buffer.getShort() & 0xFFFF;
            var palette = new int[(buffer.get() & 0xFF) + 1];

            /* and allocate an object for this sprite set */
            var set = new Sprite(width, height, size);

            /* read the offsets and dimensions of the individual sprites */
            for (var i = 0; i < size; i++)
            {
                offsetsX[i] = buffer.getShort() & 0xFFFF;
            }
            for (var i = 0; i < size; i++)
            {
                offsetsY[i] = buffer.getShort() & 0xFFFF;
            }
            for (var i = 0; i < size; i++)
            {
                subWidths[i] = buffer.getShort() & 0xFFFF;
            }
            for (var i = 0; i < size; i++)
            {
                subHeights[i] = buffer.getShort() & 0xFFFF;
            }

            /* read the palette */
            buffer.position(buffer.limit() - size * 8 - 7 - (palette.Length - 1) * 3);
            palette[0] = 0; // transparent colour (black)
            for (var index = 1; index < palette.Length; index++)
            {
                palette[index] = ByteBufferUtils.GetTriByte(buffer);
                if (palette[index] == 0)
                {
                    palette[index] = 1;
                }
            }

            /* read the pixels themselves */
            buffer.position(0);
            for (var id = 0; id < size; id++)
            {
                /* grab some frequently used values */
                int subWidth = subWidths[id],
                    subHeight = subHeights[id];
                int offsetX = offsetsX[id],
                    offsetY = offsetsY[id];

                /* create a BufferedImage to store the resulting image */
                var image = set.frames[id] = new BufferedImage(width, height, BufferedImage.TYPE_INT_ARGB);

                /* allocate an array for the palette indices */
                //JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
                //ORIGINAL LINE: int[][] indices = new int[subWidth][subHeight];
                var indices = RectangularArrays.ReturnRectangularIntArray(subWidth, subHeight);

                /* read the flags so we know whether to read horizontally or vertically */
                var flags = buffer.get() & 0xFF;

                /* read the palette indices */
                if ((flags & FLAG_VERTICAL) != 0)
                {
                    for (var x = 0; x < subWidth; x++)
                    {
                        for (var y = 0; y < subHeight; y++)
                        {
                            indices[x][y] = buffer.get() & 0xFF;
                        }
                    }
                }
                else
                {
                    for (var y = 0; y < subHeight; y++)
                    {
                        for (var x = 0; x < subWidth; x++)
                        {
                            indices[x][y] = buffer.get() & 0xFF;
                        }
                    }
                }

                /* read the alpha (if there is alpha) and convert values to ARGB */
                if ((flags & FLAG_ALPHA) != 0)
                {
                    if ((flags & FLAG_VERTICAL) != 0)
                    {
                        for (var x = 0; x < subWidth; x++)
                        {
                            for (var y = 0; y < subHeight; y++)
                            {
                                var alpha = buffer.get() & 0xFF;
                                image.setRGB(x + offsetX, y + offsetY, alpha << 24 | palette[indices[x][y]]);
                            }
                        }
                    }
                    else
                    {
                        for (var y = 0; y < subHeight; y++)
                        {
                            for (var x = 0; x < subWidth; x++)
                            {
                                var alpha = buffer.get() & 0xFF;
                                image.setRGB(x + offsetX, y + offsetY, alpha << 24 | palette[indices[x][y]]);
                            }
                        }
                    }
                }
                else
                {
                    for (var x = 0; x < subWidth; x++)
                    {
                        for (var y = 0; y < subHeight; y++)
                        {
                            var index = indices[x][y];
                            if (index == 0)
                            {
                                image.setRGB(x + offsetX, y + offsetY, 0);
                            }
                            else
                            {
                                image.setRGB(x + offsetX, y + offsetY, (int) (0xFF000000 | palette[index]));
                            }
                        }
                    }
                }
            }
            return set;
        }

        /// <summary>
        ///     Gets the width of this sprite.
        /// </summary>
        /// <returns> The width of this sprite. </returns>
        public int GetWidth()
        {
            return width;
        }

        /// <summary>
        ///     Gets the height of this sprite.
        /// </summary>
        /// <returns> The height of this sprite. </returns>
        public int GetHeight()
        {
            return height;
        }

        /// <summary>
        ///     Gets the number of frames in this set.
        /// </summary>
        /// <returns> The number of frames. </returns>
        public int Size()
        {
            return frames.Length;
        }

        /// <summary>
        ///     Gets the frame with the specified id.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <returns> The frame. </returns>
        public BufferedImage GetFrame(int id)
        {
            return frames[id];
        }

        /// <summary>
        ///     Sets the frame with the specified id.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <param name="frame"> The frame. </param>
        public void SetFrame(int id, BufferedImage frame)
        {
            if (frame.getWidth() != width || frame.getHeight() != height)
            {
                throw new ArgumentException("The frame's dimensions do not match with the sprite's dimensions.");
            }

            frames[id] = frame;
        }

        /// <summary>
        ///     Encodes this <seealso cref="Sprite" /> into a <seealso cref="ByteBuffer" />.
        ///     <p />
        ///     Please note that this is a fairly simple implementation which only
        ///     supports vertical encoding. It does not attempt to use the offsets
        ///     to save space.
        /// </summary>
        /// <returns> The buffer. </returns>
        /// <exception cref="IOException"> if an I/O exception occurs. </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public java.nio.ByteBuffer encode() throws java.io.IOException
        public ByteBuffer Encode()
        {
            var bout = new ByteArrayOutputStream();
            var os = new DataOutputStream(bout);
            try
            {
                /* set up some variables */
                IList<int?> palette = new List<int?>();
                palette.Add(0); // transparent colour

                /* write the sprites */
                foreach (var image in frames)
                {
                    /* check if we can encode this */
                    if (image.getWidth() != width || image.getHeight() != height)
                    {
                        throw new IOException("All frames must be the same size.");
                    }

                    /* loop through all the pixels constructing a palette */
                    var flags = FLAG_VERTICAL; // TODO: do we need to support horizontal encoding?
                    for (var x = 0; x < width; x++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            /* grab the colour of this pixel */
                            int argb = image.getRGB(x, y);
                            var alpha = (argb >> 24) & 0xFF;
                            var rgb = argb & 0xFFFFFF;
                            if (rgb == 0)
                            {
                                rgb = 1;
                            }

                            /* we need an alpha channel to encode this image */
                            if (alpha != 0 && alpha != 255)
                            {
                                flags |= FLAG_ALPHA;
                            }

                            /* add the colour to the palette if it isn't already in the palette */
                            if (!palette.Contains(rgb))
                            {
                                if (palette.Count >= 256)
                                {
                                    throw new IOException("Too many colours in this sprite!");
                                }
                                palette.Add(rgb);
                            }
                        }
                    }

                    /* write this sprite */
                    os.write(flags);
                    for (var x = 0; x < width; x++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            int argb = image.getRGB(x, y);
                            var alpha = (argb >> 24) & 0xFF;
                            var rgb = argb & 0xFFFFFF;
                            if (rgb == 0)
                            {
                                rgb = 1;
                            }

                            if ((flags & FLAG_ALPHA) == 0 && alpha == 0)
                            {
                                os.write(0);
                            }
                            else
                            {
                                os.write(palette.IndexOf(rgb));
                            }
                        }
                    }

                    /* write the alpha channel if this sprite has one */
                    if ((flags & FLAG_ALPHA) != 0)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            for (var y = 0; y < height; y++)
                            {
                                int argb = image.getRGB(x, y);
                                var alpha = (argb >> 24) & 0xFF;
                                os.write(alpha);
                            }
                        }
                    }
                }

                /* write the palette */
                for (var i = 1; i < palette.Count; i++)
                {
                    int rgb = (int) palette[i];
                    os.write((byte) (rgb >> 16));
                    os.write((byte) (rgb >> 8));
                    os.write((byte) rgb);
                }

                /* write the max width, height and palette size */
                os.writeShort(width);
                os.writeShort(height);
                os.write(palette.Count - 1);

                /* write the individual offsets and dimensions */
                for (var i = 0; i < frames.Length; i++)
                {
                    os.writeShort(0); // offset X
                    os.writeShort(0); // offset Y
                    os.writeShort(width);
                    os.writeShort(height);
                }

                /* write the number of frames */
                os.writeShort(frames.Length);

                /* convert the stream to a byte array and then wrap a buffer */
                byte[] bytes = bout.toByteArray();
                return ByteBuffer.wrap(bytes);
            }
            finally
            {
                os.close();
            }
        }
    }
}
