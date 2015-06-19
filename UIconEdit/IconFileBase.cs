﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace UIconEdit
{
    /// <summary>
    /// Base class for icon and cursor files.
    /// </summary>
    public abstract class IconFileBase
    {
        /// <summary>
        /// When overridden in a derived class, gets the 16-bit identifier for the file type.
        /// </summary>
        public abstract IconTypeCode ID { get; }

        /// <summary>
        /// When overridden in a derived class, computes the 16-bit X component.
        /// </summary>
        /// <param name="frame">The image frame to calculate.</param>
        /// <returns>In icon files, the color panes. In cursor files, the horizontal offset of the hotspot from the left in pixels.</returns>
        protected abstract short GetImgX(IconFrame frame);

        /// <summary>
        /// When overridden in a derived class,
        /// gets a set containing all frames in the icon file.
        /// </summary>
        protected abstract ISet<IconFrame> FrameSet { get; }

        /// <summary>
        /// When overridden in a derived class, computes the 16-bit Y component.
        /// </summary>
        /// <param name="frame">The image frame to calculate.</param>
        /// <returns>In icon files, the number of bits per pixel. In cursor files, the vertical offset of the hotspot from the top, in pixels.</returns>
        protected abstract short GetImgY(IconFrame frame);

        /// <summary>
        /// Saves the icon file to the specified stream.
        /// </summary>
        /// <param name="output">The stream to which icon file will be written.</param>
        /// <exception cref="InvalidOperationException">
        /// The image contains zero frames.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="output"/> is <c>null</c>.
        /// </exception>
        public void Save(Stream output)
        {
            if (FrameSet.Count == 0) throw new InvalidOperationException("No images set.");
            using (BinaryWriter writer = new BinaryWriter(output, new UTF8Encoding(), true))
            {
                SortedSet<IconFrame> frames = new SortedSet<IconFrame>(FrameSet, new IconFrameComparer());

                writer.Write(ushort.MinValue);
                writer.Write((short)ID);
                writer.Write(frames.Count);

                int offset = 6;

                List<MemoryStream> streamList = new List<MemoryStream>();

                foreach (IconFrame curFrame in frames)
                {
                    MemoryStream writeStream;
                    WriteImage(writer, curFrame, ref offset, out writeStream);
                    streamList.Add(writeStream);
                }

                foreach (MemoryStream ms in streamList)
                {
                    ms.CopyTo(output);
                    ms.Dispose();
                }
            }
        }

        const int dibSize = 40;

        private void WriteImage(BinaryWriter writer, IconFrame frame, ref int offset, out MemoryStream writeStream)
        {
            var image = frame.BaseImage;
            if (image.Width > byte.MaxValue) writer.Write(byte.MinValue);
            else writer.Write((byte)image.Width); //1
            if (image.Height > byte.MaxValue) writer.Write(byte.MinValue);
            else writer.Write((byte)image.Height); //2

            if (image.Palette == null)
                writer.Write(byte.MinValue);
            else
                writer.Write((byte)(image.Palette.Entries.Length - 1)); //3

            writer.Write(byte.MinValue); //4

            writer.Write(GetImgX(frame)); //6
            writer.Write(GetImgY(frame)); //8

            int length;
            writeStream = new MemoryStream();
            if (frame.BitDepth == BitDepth.Bit32)
            {
                image.Save(writeStream, ImageFormat.Png);
            }
            else
            {
                Bitmap alphaMask;
                Bitmap quantized = frame.GetQuantized(out alphaMask);
                if (alphaMask == null)
                {
                    quantized.Save(writeStream, ImageFormat.Png);
                }
                else
                {
                    using (BinaryWriter msWriter = new BinaryWriter(writeStream, new UTF8Encoding(), true))
                    {
                        msWriter.Write(dibSize);
                        msWriter.Write(quantized.Width);
                        msWriter.Write(quantized.Height + alphaMask.Height);
                        msWriter.Write((short)1);
                        msWriter.Write(frame.BitsPerPixel); //1, 4, 8, or 24
                        msWriter.Write(0); //Compression method = 0

                        Rectangle fullRect = new Rectangle(0, 0, quantized.Width, quantized.Height);
                        BitmapData alphaData = alphaMask.LockBits(fullRect, ImageLockMode.ReadOnly, alphaMask.PixelFormat);
                        BitmapData imageData = quantized.LockBits(fullRect, ImageLockMode.ReadOnly, quantized.PixelFormat);

                        msWriter.Write((alphaData.Stride * fullRect.Height) + (imageData.Stride * fullRect.Height));

                        msWriter.Write(0L); //Skip resolution
                        if (quantized.Palette == null)
                            msWriter.Write(0);
                        else
                            msWriter.Write(quantized.Palette.Entries.Length);
                        msWriter.Write(0); //Skip "important colors" which nobody uses anyway

                        if (quantized.Palette != null)
                        {
                            foreach (Color c in quantized.Palette.Entries)
                            {
                                msWriter.Write(c.R);
                                msWriter.Write(c.G);
                                msWriter.Write(c.B);
                                msWriter.Write(byte.MaxValue);
                            }
                        }

                        for (int y = fullRect.Height - 1; y >= 0; y--)
                        {
                            byte[] buffer = new byte[alphaData.Stride];

                            Marshal.Copy(alphaData.Scan0 + (y * alphaData.Stride), buffer, 0, buffer.Length);
                            msWriter.Write(buffer);
                        }

                        alphaMask.UnlockBits(alphaData);
                        alphaMask.Dispose();

                        for (int y = fullRect.Height - 1; y >= 0; y--)
                        {
                            byte[] buffer = new byte[imageData.Stride];

                            Marshal.Copy(imageData.Scan0 + (y * imageData.Stride), buffer, 0, buffer.Length);
                            msWriter.Write(buffer);
                        }
                        quantized.UnlockBits(imageData);
                        if (!ReferenceEquals(quantized, image)) //Hey, it could happen.
                            quantized.Dispose();
                    }
                }
            }

            length = (int)writeStream.Length;
            writer.Write(length); //12
            writer.Write(offset); //16
            offset += length;
        }
    }

    /// <summary>
    /// The type code for an icon file.
    /// </summary>
    public enum IconTypeCode : short
    {
        /// <summary>
        /// Indicates an icon (.ICO) file.
        /// </summary>
        Icon = 1,
        /// <summary>
        /// Indicates a cursor (.CUR) file.
        /// </summary>
        Cursor = 2,
    }
}
