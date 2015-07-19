﻿#region BSD license
/*
Copyright © 2015, KimikoMuffin.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.
3. The names of its contributors may not be used to endorse or promote 
   products derived from this software without specific prior written 
   permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UIconEdit
{
    /// <summary>
    /// Base class for icon and cursor files.
    /// </summary>
    public abstract class IconFileBase : DependencyObject, ICloneable, IDisposable
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public IconFileBase()
        {
            _entries = new EntryList(this);
        }

        #region Load
        /// <summary>
        /// Loads an <see cref="IconFileBase"/> implementation from the specified stream.
        /// </summary>
        /// <param name="input">A stream containing an icon or cursor file.</param>
        /// <returns>An <see cref="IconFileBase"/> implementation loaded from <paramref name="input"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="input"/> is closed or does not support reading.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="input"/> is closed.
        /// </exception>
        /// <exception cref="IconLoadException">
        /// An error occurred when processing the icon file's format.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public static IconFileBase Load(Stream input)
        {
            return Load(input, null, null);
        }

        /// <summary>
        /// Loads an <see cref="IconFileBase"/> implementation from the specified stream.
        /// </summary>
        /// <param name="input">A stream containing an icon or cursor file.</param>
        /// <param name="handler">A delegate used to process <see cref="IconLoadException"/>s thrown when processing individual icon entries,
        /// or <c>null</c> to throw an exception in those cases.</param>
        /// <returns>An <see cref="IconFileBase"/> implementation loaded from <paramref name="input"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="input"/> is closed or does not support reading.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="input"/> is closed.
        /// </exception>
        /// <exception cref="IconLoadException">
        /// An error occurred when processing the icon file's format.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public static IconFileBase Load(Stream input, IconLoadExceptionHandler handler)
        {
            return Load(input, null, handler);
        }

        /// <summary>
        /// Loads an <see cref="IconFileBase"/> implementation from the specified path.
        /// </summary>
        /// <param name="path">The path to a cursor or icon file.</param>
        /// <param name="handler">A delegate used to process <see cref="IconLoadException"/>s thrown when processing individual icon entries,
        /// or <c>null</c> to throw an exception in those cases.</param>
        /// <returns>An <see cref="IconFileBase"/> implementation loaded from <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is empty, contains only whitespace, or contains one or more invalid path characters as defined in <see cref="Path.GetInvalidPathChars()"/>.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// The specified path, filename, or both contain the system-defined maximum length.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The specified path was not found.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// The specified path was invalid.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <para><paramref name="path"/> specified a directory.</para>
        /// <para>-OR-</para>
        /// <para>The caller does not have the required permission.</para>
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="path"/> is in an invalid format.
        /// </exception>
        /// <exception cref="IconLoadException">
        /// An error occurred when processing the icon file's format.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public static IconFileBase Load(string path, IconLoadExceptionHandler handler)
        {
            using (FileStream fs = File.OpenRead(path))
                return Load(fs, handler);
        }

        /// <summary>
        /// Loads an <see cref="IconFileBase"/> implementation from the specified path.
        /// </summary>
        /// <param name="path">The path to a cursor or icon file.</param>
        /// <returns>An <see cref="IconFileBase"/> implementation loaded from <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is empty, contains only whitespace, or contains one or more invalid path characters as defined in <see cref="Path.GetInvalidPathChars()"/>.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// The specified path, filename, or both contain the system-defined maximum length.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// The specified path was not found.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// The specified path was invalid.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// <para><paramref name="path"/> specified a directory.</para>
        /// <para>-OR-</para>
        /// <para>The caller does not have the required permission.</para>
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="path"/> is in an invalid format.
        /// </exception>
        /// <exception cref="IconLoadException">
        /// An error occurred when processing the icon file's format.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public static IconFileBase Load(string path)
        {
            return Load(path, null);
        }

        internal static IconFileBase Load(Stream input, IconTypeCode? id, IconLoadExceptionHandler handler)
        {
#if DEBUG && MESSAGE
            Stopwatch sw = Stopwatch.StartNew();
#endif
#if LEAVEOPEN
            using (BinaryReader reader = new BinaryReader(input, new UTF8Encoding(), true))
#else
            BinaryReader reader = new BinaryReader(input, new UTF8Encoding());
#endif
            {
                if (reader.ReadInt16() != 0) throw new IconLoadException(IconErrorCode.InvalidFormat, 0);

                IconTypeCode loadedId = (IconTypeCode)reader.ReadInt16();
                if (id.HasValue && loadedId != id.Value) throw new IconLoadException(IconErrorCode.WrongType, loadedId, id.Value);

                IconFileBase returner;

                switch (loadedId)
                {
                    case IconTypeCode.Cursor:
                        returner = new CursorFile();
                        break;
                    case IconTypeCode.Icon:
                        returner = new IconFile();
                        break;
                    default:
                        throw new IconLoadException(IconErrorCode.InvalidFormat, IconTypeCode.Unknown, loadedId);
                }

                ushort entryCount = reader.ReadUInt16();

                if (entryCount == 0) throw new IconLoadException(IconErrorCode.ZeroEntries, loadedId);

                IconDirEntry[] entryList = new IconDirEntry[entryCount];
                long offset = (16 * entryCount) + 6;

                for (int i = 0; i < entryCount; i++)
                {
                    IconDirEntry entry = new IconDirEntry(loadedId);
                    entry.BWidth = reader.ReadByte();
                    entry.BHeight = reader.ReadByte();
                    entry.ColorCount = reader.ReadByte();
                    reader.ReadByte();
                    entry.XPlanes = reader.ReadUInt16();
                    entry.YBitsPerpixel = reader.ReadUInt16();
                    entry.ResourceLength = reader.ReadUInt32();
                    if (entry.ResourceLength < MinDibSize)
                        throw new IconLoadException(IconErrorCode.ResourceTooSmall, loadedId, entry.ResourceLength, i);
                    entry.ImageOffset = reader.ReadUInt32();
                    if (entry.ImageOffset < offset)
                        throw new IconLoadException(IconErrorCode.ResourceTooEarly, loadedId, entry.ImageOffset, i);
                    entryList[i] = entry;
                }

                Array.Sort(entryList);

                const int bufferSize = 8192;

                List<IconEntry> entries = new List<IconEntry>(entryList.Length);

                for (int i = 0; i < entryList.Length; i++)
                {
                    IconDirEntry entry = entryList[i];

                    try
                    {
                        long gapLength = entry.ImageOffset - offset;
                        byte[] curBuffer = new byte[bufferSize];
                        while (gapLength > 0)
                            gapLength -= input.Read(curBuffer, 0, (int)Math.Min(gapLength, bufferSize));

                        WriteableBitmap loadedImage, alphaMask;

                        int dibSize = reader.ReadInt32();

                        IconBitDepth bitDepth;
                        if (loadedId == IconTypeCode.Cursor)
                            bitDepth = 0;
                        else
                        {
                            switch (entry.YBitsPerpixel)
                            {
                                case 0:
                                case 1:
                                case 4:
                                case 8:
                                case 24:
                                case 32:
                                    bitDepth = IconEntry.GetBitDepth(entry.YBitsPerpixel);
                                    break;
                                default:
                                    throw new IconLoadException(IconErrorCode.InvalidBitDepth, loadedId, entry.YBitsPerpixel, i);
                            }
                        }

                        const int pngLittleEndian = 0x474e5089; //"\u0089PNG"  in little-endian order.
                        bool isPng = false;
                        if (dibSize == pngLittleEndian)
                        {
                            #region Load Png
                            alphaMask = null;

                            using (OffsetStream os = new OffsetStream(input, new byte[] { 0x89, 0x50, 0x4e, 0x47 }, entry.ResourceLength - 4, true))
                            using (MemoryStream ms = new MemoryStream())
                            {
                                os.CopyTo(ms);
                                ms.Seek(0, SeekOrigin.Begin);
                                try
                                {
                                    PngBitmapDecoder decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                                    BitmapSource frame = decoder.Frames[0];
                                    var pFormat = frame.Format;

                                    if (loadedId == IconTypeCode.Cursor)
                                    {
                                        if (pFormat == PixelFormats.Indexed8 || pFormat == PixelFormats.Gray8)
                                            bitDepth = IconBitDepth.Depth8BitsPerPixel;
                                        else if (pFormat == PixelFormats.Indexed4 || pFormat == PixelFormats.Gray4)
                                            bitDepth = IconBitDepth.Depth4BitsPerPixel;
                                        else if (pFormat == PixelFormats.Indexed2 || pFormat == PixelFormats.Gray2)
                                            bitDepth = IconBitDepth.Depth1BitPerPixel;
                                        else if (pFormat == PixelFormats.Bgr24 || pFormat == PixelFormats.Rgb24)
                                            bitDepth = IconBitDepth.Depth24BitsPerPixel;
                                        else if (pFormat == PixelFormats.Bgra32)
                                            bitDepth = IconBitDepth.Depth32BitsPerPixel;
                                        else
                                        {
                                            bitDepth = IconBitDepth.Depth32BitsPerPixel;
                                            frame = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
                                        }
                                    }

                                    loadedImage = new WriteableBitmap(frame);
                                }
                                catch (FileFormatException e)
                                {
                                    throw new IconLoadException(e.Message, IconErrorCode.InvalidPngFile, loadedId, i, e);
                                }
                            }
                            #endregion
                            isPng = true;
                        }
                        else if (dibSize == MinDibSize)
                        {
                            #region Load Bmp
                            using (OffsetStream ms = new OffsetStream(input, entry.ResourceLength - 4, true))
                            using (BinaryReader curReader = new BinaryReader(ms))
                            {
                                int width = curReader.ReadInt32(); //8
                                int height = curReader.ReadInt32(); //12
                                if (width > IconEntry.MaxDimension || width < IconEntry.MinDimension)
                                    throw new IconLoadException(IconErrorCode.InvalidBmpSize, loadedId, new Size(width, height), i);

                                ushort colorPanes = curReader.ReadUInt16(); //14
                                ushort bitsPerPixel = curReader.ReadUInt16(); //16
                                int bmpStride, alphaStride = (width + 7) / 8;
                                _catchStride(ref alphaStride);

                                PixelFormat pFormat;

                                switch (bitsPerPixel)
                                {
                                    case 1:
                                        bmpStride = alphaStride;
                                        pFormat = PixelFormats.Indexed1;
                                        break;
                                    case 4:
                                        bmpStride = (width + 1) >> 1;
                                        pFormat = PixelFormats.Indexed4;
                                        break;
                                    case 8:
                                        bmpStride = width;
                                        pFormat = PixelFormats.Indexed8;
                                        break;
                                    case 24:
                                        bmpStride = width * 3;
                                        pFormat = PixelFormats.Bgr24;
                                        break;
                                    case 32:
                                        bmpStride = width * 4;
                                        pFormat = PixelFormats.Bgra32;
                                        break;
                                    default:
                                        throw new IconLoadException(IconErrorCode.InvalidBmpBitDepth, loadedId, bitsPerPixel, i);
                                }
                                _catchStride(ref bmpStride);

                                if (loadedId == IconTypeCode.Cursor)
                                    bitDepth = IconEntry.GetBitDepth(bitsPerPixel);
                                else if (entry.YBitsPerpixel != 0 && bitsPerPixel != entry.YBitsPerpixel)
                                    throw new IconLoadException(IconErrorCode.InvalidBmpBitDepth, loadedId, new Tuple<int, int>(entry.YBitsPerpixel, bitsPerPixel), i);

                                int actualHeight;

                                if (bitDepth == IconBitDepth.Depth32BitsPerPixel && entry.BHeight != 0 && entry.BHeight == height)
                                {
                                    actualHeight = height;
                                }
                                else
                                {
                                    if ((height & 1) == 1)
                                        throw new IconLoadException(IconErrorCode.InvalidBmpHeightOdd, loadedId, height, i);
                                    actualHeight = height >> 1;
                                }

                                if (height > (IconEntry.MaxDimension << 1) || height < (IconEntry.MinDimension << 1))
                                    throw new IconLoadException(IconErrorCode.InvalidBmpSize, loadedId, new Size(width, height), i);

                                if ((entry.BWidth != 0 && entry.BWidth != width) ||
                                    (entry.BHeight != 0 && entry.BHeight != actualHeight && entry.BHeight != height))
                                {
                                    throw new IconLoadException(IconErrorCode.BmpSizeMismatch, loadedId,
                                        new Tuple<Size, Size>(new Size(entry.BWidth, entry.BHeight), new Size(width, actualHeight)), i);
                                }

                                if (reader.ReadInt32() != 0)
                                    throw new IconLoadException(IconErrorCode.InvalidBmpFile, loadedId, i);

                                int dataLength = reader.ReadInt32();
                                int bmpLength, alphaLength;

                                if (dataLength == 0)
                                {
                                    bmpLength = actualHeight * bmpStride;
                                    alphaLength = (actualHeight == height) ? 0 : actualHeight * alphaStride;
                                    dataLength = bmpLength + alphaLength;
                                }
                                else if (actualHeight == height)
                                {
                                    alphaLength = 0;
                                    bmpLength = dataLength;
                                }
                                else
                                {
                                    alphaLength = actualHeight * alphaStride;
                                    bmpLength = dataLength - alphaLength;
                                }

                                reader.ReadInt64(); //Skip next eight bytes.

                                int palCount = reader.ReadInt32();
                                if (palCount != 0)
                                {
                                    if (bitDepth == IconBitDepth.Depth32BitsPerPixel || bitDepth == IconBitDepth.Depth24BitsPerPixel || palCount != IconEntry.GetColorCount(bitDepth))
                                        throw new IconLoadException(IconErrorCode.InvalidBmpFile, loadedId, i);
                                }
                                else if (bitDepth != IconBitDepth.Depth32BitsPerPixel && bitDepth != IconBitDepth.Depth24BitsPerPixel)
                                    palCount = (int)IconEntry.GetColorCount(bitDepth);

                                reader.ReadInt32(); //Skip next 4 bytes

                                BitmapPalette palette;

                                if (palCount == 0)
                                    palette = null;
                                else
                                {
                                    List<Color> colors = new List<Color>(palCount);
                                    for (int p = 0; p < palCount; p++)
                                    {
                                        byte b = reader.ReadByte();
                                        byte g = reader.ReadByte();
                                        byte r = reader.ReadByte();
                                        reader.ReadByte();
                                        colors.Add(Color.FromRgb(r, g, b));
                                    }
                                    palette = new BitmapPalette(colors);
                                }

                                byte[] bmpData = _readBmpLines(reader, bmpStride, actualHeight);

                                loadedImage = new WriteableBitmap(BitmapSource.Create(width, actualHeight, 0, 0, pFormat, palette, bmpData, bmpStride));

                                if (actualHeight == height)
                                    alphaMask = null;
                                else
                                {
                                    byte[] alphaData = _readBmpLines(reader, alphaStride, actualHeight);

                                    alphaMask = new WriteableBitmap(BitmapSource.Create(width, actualHeight, 0, 0, PixelFormats.Indexed1,
                                        IconEntry.AlphaPalette, alphaData, alphaStride));
                                }
                            }
                            #endregion
                        }
                        else throw new IconLoadException(IconErrorCode.InvalidEntryType, loadedId, dibSize, i);

                        if (loadedImage.PixelWidth > IconEntry.MaxDimension || loadedImage.PixelHeight > IconEntry.MaxDimension ||
                            loadedImage.PixelWidth < IconEntry.MinDimension || loadedImage.PixelHeight < IconEntry.MinDimension)
                        {
                            throw new IconLoadException(isPng ? IconErrorCode.InvalidPngSize : IconErrorCode.InvalidBmpSize,
                                loadedId, new Size(loadedImage.PixelWidth, loadedImage.PixelHeight), i);
                        }

                        if ((entry.BWidth != 0 && entry.BWidth != loadedImage.PixelWidth) ||
                            (entry.BHeight != 0 && entry.BHeight != loadedImage.PixelHeight))
                        {
                            throw new IconLoadException(isPng ? IconErrorCode.PngSizeMismatch : IconErrorCode.BmpSizeMismatch, loadedId,
                                new Tuple<Size, Size>(new Size(entry.BWidth, entry.BHeight), new Size(loadedImage.PixelWidth, loadedImage.PixelHeight)), i);
                        }

#if DEBUG && MESSAGE
                        Debug.WriteLine("Reading type {0}, width:{1}, height:{2}, bit depth:{3}",
                            isPng ? "PNG" : "BMP", loadedImage.PixelWidth, loadedImage.PixelHeight, bitDepth);
#endif

                        IconEntry resultEntry;

                        if (loadedId == IconTypeCode.Cursor)
                            resultEntry = new IconEntry(loadedImage, alphaMask, bitDepth, entry.XPlanes, entry.YBitsPerpixel);
                        else
                            resultEntry = new IconEntry(loadedImage, alphaMask, bitDepth);

                        entries.Add(resultEntry);
                    }
                    catch (IconLoadException e)
                    {
                        if (handler == null)
#if DEBUG
                            throw new IconLoadException(e);
#else
                            throw;
#endif
                        handler(e);
                    }
                    finally
                    {
                        offset += entry.ResourceLength;
                    }
                }
#if DEBUG && MESSAGE
                sw.Stop();
                Debug.WriteLine("Finished processing all entries in {0}ms.", sw.Elapsed.TotalMilliseconds);
#endif
                if (entries.Count == 0)
                    throw new IconLoadException(IconErrorCode.ZeroValidEntries, loadedId);

                entries.Sort(new IconEntryComparer());

                returner.Entries.AddBulk(entries);

                return returner;
            }
        }

        private static byte[] _readBmpLines(BinaryReader reader, int stride, int height)
        {
            byte[] bmpData = new byte[stride * height];

            for (int y = height - 1; y >= 0; y--)
            {
                int yOff = stride * y;
                int count = stride;
                while (count > 0)
                {
                    int read = reader.Read(bmpData, yOff, count);
                    if (read == 0)
                        throw new EndOfStreamException();
                    yOff += read;
                    count -= read;
                }
            }

            return bmpData;
        }

        private static void _catchStride(ref int stride)
        {
            int offVal = stride & 3;
            if (offVal != 0)
                stride += 4 - offVal;
        }

        [DebuggerDisplay("ImageOffset = {ImageOffset}, ResourceLength = {ResourceLength}, End = {End}")]
        private class IconDirEntry : IComparable<IconDirEntry>
        {
            public IconDirEntry(IconTypeCode loadedId)
            {
                loadedid = loadedId;
            }
            private IconTypeCode loadedid;
            public byte BWidth;
            public byte BHeight;
            public byte ColorCount;
            public ushort XPlanes;
            public ushort YBitsPerpixel;
            public uint ResourceLength;
            public uint ImageOffset;
            public long End { get { return (long)ResourceLength + ImageOffset; } }

            public int CompareTo(IconDirEntry other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(other, null)) return 1;

                if (End <= other.ImageOffset) return -1; //If this end is comes before the other's offset
                if (other.End <= ImageOffset) return 1; //If the other's end comes before this offset

                throw new IconLoadException(IconErrorCode.ResourceOverlap, loadedid); //If there's any kind of overlap, someone's wrong.
            }
        }
        #endregion

        /// <summary>
        /// When overridden in a derived class,
        /// returns a duplicate of the current instance.
        /// </summary>
        /// <returns>A duplicate of the current instance, with copies of every icon entry and clones of each
        /// entry's <see cref="IconEntry.BaseImage"/> in <see cref="Entries"/>.</returns>
        public abstract IconFileBase Clone();

        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Returns a duplicate of the current instance as an <see cref="IconFile"/>
        /// </summary>
        /// <returns>An <see cref="IconFile"/> containing copies of every icon entry and clones of each
        /// entry's <see cref="IconEntry.BaseImage"/> in <see cref="Entries"/>, and with each entry's
        /// <see cref="IconEntry.HotspotX"/> and <see cref="IconEntry.HotspotY"/> values set to 0.</returns>
        public virtual IconFile CloneAsIconFile()
        {
            IconFile copy = new IconFile();
            foreach (IconEntry curEntry in _entries)
            {
                var newEntry = curEntry.Clone();
                newEntry.HotspotX = 0;
                newEntry.HotspotY = 0;
                copy._entries.Add(newEntry);
            }
            return copy;
        }

        /// <summary>
        /// Returns a duplicate of the current instance as an <see cref="IconFile"/>
        /// </summary>
        /// <returns>An <see cref="IconFile"/> containing copies of every icon entry and clones of each
        /// entry's <see cref="IconEntry.BaseImage"/> in <see cref="Entries"/>.</returns>
        public virtual CursorFile CloneAsCursorFile()
        {
            CursorFile copy = new CursorFile();
            foreach (IconEntry curEntry in _entries)
                copy._entries.Add(curEntry.Clone());
            return copy;
        }

        /// <summary>
        /// When overridden in a derived class, gets the 16-bit identifier for the file type.
        /// </summary>
        public abstract IconTypeCode ID { get; }

        private EntryList _entries;
        /// <summary>
        /// Gets a collection containing all entries in the icon file. 
        /// </summary>
        [Bindable(true)]
        public EntryList Entries { get { return _entries; } }

        internal virtual bool IsValid(IconEntry entry)
        {
            return entry != null && !entry.IsDisposed;
        }

        internal abstract ushort GetImgX(IconEntry entry);

        internal abstract ushort GetImgY(IconEntry entry);

        #region Save
        internal void Save(Stream output, IEnumerable<IconEntry> entryCollection)
        {
#if LEAVEOPEN
            using (BinaryWriter writer = new BinaryWriter(output, new UTF8Encoding(), true))
#else
            BinaryWriter writer = new BinaryWriter(output, new UTF8Encoding());
#endif
            {
                List<IconEntry> entries = new List<IconEntry>(entryCollection);
                entries.Sort(new IconEntryComparer());

                writer.Write(ushort.MinValue);
                writer.Write((short)ID);
                writer.Write((short)entries.Count);

                uint offset = (uint)(6 + (entries.Count * 16));

                List<MemoryStream> streamList = new List<MemoryStream>();
#if DEBUG && MESSAGE
                Stopwatch sw = Stopwatch.StartNew();
#endif
                foreach (IconEntry curEntry in entries)
                {
                    MemoryStream writeStream;
                    WriteImage(writer, curEntry, ref offset, out writeStream);
                    streamList.Add(writeStream);
                }

                foreach (MemoryStream ms in streamList)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.CopyTo(output);
                    ms.Dispose();
                }
#if DEBUG && MESSAGE
                sw.Stop();
                Debug.WriteLine("Finished processing all entries in {0}ms.", sw.Elapsed.TotalMilliseconds);
#endif
            }
#if !LEAVEOPEN
            writer.Flush();
#endif
        }

        /// <summary>
        /// Saves the file to the specified stream.
        /// </summary>
        /// <param name="output">The stream to which the file will be written.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Entries"/> contains zero elements.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="output"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="output"/> is closed or does not support writing.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="output"/> is closed.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public void Save(Stream output)
        {
            var entries = Entries;
            if (entries.Count == 0 || entries.Count > ushort.MaxValue) throw new InvalidOperationException();
            try
            {
                Save(output, entries);
            }
            catch (ObjectDisposedException) { throw; }
            catch (IOException) { throw; }
            catch (Exception e) { throw new IOException(e.Message, e); }
        }

        /// <summary>
        /// Saves the file to the specified file.
        /// </summary>
        /// <param name="path">The file to which the file will be written.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Entries"/> contains zero elements.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is empty, contains only whitespace, or contains one or more invalid path characters as defined in <see cref="Path.GetInvalidPathChars()"/>.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// The specified path, filename, or both contain the system-defined maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// The specified path is invalid.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public void Save(string path)
        {
            var entries = Entries;
            if (entries.Count == 0) throw new InvalidOperationException("At least one entry is needed.");
            using (MemoryStream ms = new MemoryStream())
                try
                {
                    Save(ms, entries);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (FileStream fs = File.Open(path, FileMode.Create))
                        ms.CopyTo(fs);
                }
                catch (ObjectDisposedException) { throw; }
                catch (IOException) { throw; }
                catch (Exception e) { throw new IOException(e.Message, e); }
        }

        const int MinDibSize = 40;

        private void WriteImage(BinaryWriter writer, IconEntry entry, ref uint offset, out MemoryStream writeStream)
        {
            var image = entry.BaseImage;

            bool isPng = entry.IsPng;

            if (image.Width > byte.MaxValue || image.Height > byte.MaxValue)
                writer.Write(ushort.MinValue); //2
            else
            {
                writer.Write((byte)entry.Width);
                writer.Write((byte)entry.Height);
            }

            BitmapSource alphaMask;
            WriteableBitmap quantized = entry.GetQuantized(isPng, out alphaMask);

            if (alphaMask == null || quantized.Palette == null || quantized.Palette.Colors.Count > byte.MaxValue)
                writer.Write(byte.MinValue);
            else
                writer.Write((byte)quantized.Palette.Colors.Count); //3

            writer.Write(byte.MinValue); //4

            writer.Write(GetImgX(entry)); //6
            writer.Write(GetImgY(entry)); //8

            uint length;
            writeStream = new MemoryStream();
            if (isPng)
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(quantized.Clone()));
                encoder.Save(writeStream);
            }
            else
            {
#if LEAVEOPEN
                using (BinaryWriter msWriter = new BinaryWriter(writeStream, new UTF8Encoding(), true))
#else
                BinaryWriter msWriter = new BinaryWriter(writeStream, new UTF8Encoding());
#endif
                {
                    ushort bitsPerPixel = entry.BitsPerPixel;
                    int height = quantized.PixelHeight;
                    if (alphaMask != null) height += alphaMask.PixelHeight; //Only if bit depth != 32
                    msWriter.Write(MinDibSize);
                    msWriter.Write(quantized.PixelWidth);
                    msWriter.Write(height);
                    msWriter.Write((short)1);
                    msWriter.Write(bitsPerPixel); //1, 4, 8, 24, or 32

                    //Skip format (4 bytes), size (4 bytes), resolution (8 bytes), palette count (4 bytes), and "important colors" (4 bytes)
                    msWriter.Write(new byte[24]);

                    if (quantized.Palette != null)
                    {
                        ushort paletteCount = (ushort)IconEntry.GetColorCount(entry.BitDepth);

                        for (int i = 0; i < paletteCount && i < quantized.Palette.Colors.Count; i++)
                        {
                            Color curColor = quantized.Palette.Colors[i];
                            msWriter.Write(curColor.B);
                            msWriter.Write(curColor.G);
                            msWriter.Write(curColor.R);
                            msWriter.Write(byte.MaxValue);
                        }

                        for (int i = quantized.Palette.Colors.Count; i < paletteCount; i++)
                            msWriter.Write(0xFF000000u);
                    }
                    int width = quantized.PixelWidth;
                    int alphaStride = (width + 7) >> 3;
                    _catchStride(ref alphaStride);
                    int bmpStride;
                    switch (entry.BitDepth)
                    {
                        default: //32-bit
                            bmpStride = width * 4;
                            break;
                        case IconBitDepth.Depth24BitsPerPixel:
                            bmpStride = width * 3;
                            break;
                        case IconBitDepth.Depth8BitsPerPixel:
                            bmpStride = width;
                            break;
                        case IconBitDepth.Depth4BitsPerPixel:
                            bmpStride = (width + 1) >> 1;
                            break;
                        case IconBitDepth.Depth1BitPerPixel:
                            bmpStride = alphaStride;
                            break;
                    }
                    _catchStride(ref bmpStride);

                    height = quantized.PixelHeight;

                    _writeBmpData(quantized, msWriter, height, bmpStride);

                    if (alphaMask != null)
                        _writeBmpData(alphaMask, msWriter, height, alphaStride);
                }
#if !LEAVEOPEN
                writer.Flush();
#endif
            }

#if DEBUG && MESSAGE
            Debug.WriteLine("Writing type {0} - width:{1}, height:{2}, bit depth:{3}, computed bits per pixel:{4}, length:{5}",
                isPng ? "PNG" : "BMP", entry.Width, entry.Height, entry.BitDepth, GetImgY(entry), writeStream.Length);
#endif

            length = (uint)writeStream.Length;
            writer.Write(length); //12
            writer.Write(offset); //16
            offset += length;
        }

        private static void _writeBmpData(BitmapSource bmp, BinaryWriter msWriter, int height, int stride)
        {
            byte[] bmpData = new byte[height * stride];
            bmp.CopyPixels(bmpData, stride, 0);

            for (int y = height - 1; y >= 0; y--)
            {
                int yOff = y * stride;
                msWriter.Write(bmpData, yOff, stride);
            }
        }
        #endregion

        #region Disposal
        private bool _isDisposed;
        /// <summary>
        /// Gets a value indicating whether the current instance has been disposed.
        /// Intended to be set in <see cref="Dispose(bool)"/>.
        /// </summary>
        public bool IsDisposed
        {
            get { return _isDisposed; }
            protected set { _isDisposed |= value; }
        }

        /// <summary>
        /// Releases all managed and unmanaged resources used by the current instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.Collect();
        }

        /// <summary>
        /// Releases all unmanaged resources used by the current instance, and optionally releases managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            foreach (IconEntry curEntry in _entries)
                curEntry.Dispose();

            _entries.Clear();
            _isDisposed = true;
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~IconFileBase()
        {
            Dispose(false);
        }
        #endregion

        /// <summary>
        /// Represents a list of icon entries. Entries with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and
        /// <see cref="IconEntry.BitDepth"/> cannot be added to the list; however, there may be duplicates if an icon loaded from an
        /// external icon file contained them.
        /// </summary>
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(DebugView))]
        public class EntryList : IList<IconEntry>, IList, INotifyCollectionChanged, INotifyPropertyChanged
#if IREADONLY
            , IReadOnlyList<IconEntry>
#endif
        {
            private HashSet<IconEntryKey> _set;
            private ObservableCollection<IconEntry> _items;
            private IconFileBase _file;
            private bool _noDups;

            private void _setItems(ObservableCollection<IconEntry> collection)
            {
                var oldItems = _items;
                _items = collection;
                _items.CollectionChanged += _items_CollectionChanged;
                ((INotifyPropertyChanged)_items).PropertyChanged += _items_PropertyChanged;
                if (oldItems != null)
                {
                    oldItems.CollectionChanged -= _items_CollectionChanged;
                    ((INotifyPropertyChanged)oldItems).PropertyChanged -= _items_PropertyChanged;
                }
            }

            internal EntryList(IconFileBase file)
            {
                _file = file;
                _set = new HashSet<IconEntryKey>();
                _setItems(new ObservableCollection<IconEntry>());
                _noDups = true;
            }

            private void _items_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, e);
            }

            private void _items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (CollectionChanged != null)
                    CollectionChanged(this, e);
            }

            /// <summary>
            /// Raised when elements are added to or removed from the list.
            /// </summary>
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            /// <summary>
            /// Raised when a property changes in the current instance.
            /// </summary>
            protected event PropertyChangedEventHandler PropertyChanged;
            event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
            {
                add { PropertyChanged += value; }
                remove { PropertyChanged -= value; }
            }

            private IconEntry _checkAdd(object value, string paramName)
            {
                if (value == null) throw new ArgumentNullException(paramName);
                IconEntry entry = value as IconEntry;
                if (entry == null) throw new ArgumentException("The specified value is the wrong type.", paramName);
                return entry;
            }

            /// <summary>
            /// Gets and sets the element at the specified index.
            /// </summary>
            /// <param name="index">The index of the element to get or set.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <para><paramref name="index"/> is less than 0 or is greater than or equal to <see cref="Count"/>.</para>
            /// <para>-OR-</para>
            /// <para>In a set operation, the specified value is <c>null</c>.</para>
            /// </exception>
            /// <exception cref="NotSupportedException">
            /// In a set operation, an element with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>
            /// already exists in the list at a different index, or the specified value is already associated with a different icon file.
            /// </exception>
            public IconEntry this[int index]
            {
                get { return _items[index]; }
                set
                {
                    if (value == null) throw new ArgumentOutOfRangeException(null, new ArgumentNullException().Message);
                    if (!_setValue(index, value, true))
                        throw new NotSupportedException("Could not set the specified value in the list.");
                }
            }

            object IList.this[int index]
            {
                get { return _items[index]; }
                set { this[index] = _checkAdd(value, null); }
            }

            /// <summary>
            /// Gets the number of elements in the list.
            /// </summary>
            public int Count { get { return _items.Count; } }

            internal void AddBulk(IList<IconEntry> entries)
            {
                _setItems(new ObservableCollection<IconEntry>(entries));
                foreach (var curItem in entries)
                    _noDups &= _set.Add(curItem.EntryKey);
            }

            /// <summary>
            /// Adds the specified icon entry to the list.
            /// </summary>
            /// <param name="item">The icon entry to add to the list.</param>
            /// <returns><c>true</c> if <paramref name="item"/> was successfully added; <c>false</c> if <paramref name="item"/> is <c>null</c> or disposed,
            /// is already associated with a different icon file, <see cref="Count"/> is equal to <see cref="ushort.MaxValue"/>, or if an element with the same
            /// <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/> already exists in the list.</returns>
            public bool Add(IconEntry item)
            {
                return Insert(_items.Count, item);
            }

            void ICollection<IconEntry>.Add(IconEntry item)
            {
                Add(item);
            }

            int IList.Add(object value)
            {
                if (Add(_checkAdd(value, "value"))) return _items.Count;
                throw new NotSupportedException("Could not add the specified value!");
            }

            /// <summary>
            /// Adds the specified icon entry to the list at the specified index.
            /// </summary>
            /// <param name="index">The index at which to insert the icon entry.</param>
            /// <param name="item">The icon entry to add to the list.</param>
            /// <returns><c>true</c> if <paramref name="item"/> was successfully added; <c>false</c> if <paramref name="item"/> is <c>null</c> or disposed,
            /// is already associated with a different icon file, <see cref="Count"/> is equal to <see cref="ushort.MaxValue"/>, or if an element with the same
            /// <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/> already exists in the list.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than 0 or is greater than <see cref="Count"/>.
            /// </exception>
            public bool Insert(int index, IconEntry item)
            {
                if (index < 0 || index > _items.Count) throw new ArgumentOutOfRangeException("index");
                if (_file._isDisposed || _items.Count == ushort.MaxValue || item == null || item.File != null || !_file.IsValid(item) || !_set.Add(item.EntryKey))
                    return false;
                item.File = _file;
                _items.Insert(index, item);
                return true;
            }

            void IList<IconEntry>.Insert(int index, IconEntry item)
            {
                Insert(index, item);
            }

            void IList.Insert(int index, object value)
            {
                Insert(index, _checkAdd(value, "value"));
            }

            private bool _setValue(int index, IconEntry value, bool setter)
            {
                if (_file._isDisposed) return false;
                if (setter && index == _items.Count)
                    return Add(value);
                var oldItem = _items[index];
                if (value == null || value.File != null || !_file.IsValid(value) || (_set.Contains(value.EntryKey) && oldItem.EntryKey != value.EntryKey))
                    return false;
                oldItem.File = null;
                value.File = _file;
                _items[index] = value;
                return true;
            }

            /// <summary>
            /// Sets the value at the specified index.
            /// </summary>
            /// <param name="index">The index of the value to set.</param>
            /// <param name="item">The item to set at the specified index.</param>
            /// <returns><c>true</c> if <paramref name="item"/> was successfully set; <c>false</c> if <paramref name="item"/> is <c>null</c> or disposed,
            /// is already associated with a different icon file, or if an element with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>,
            /// and <see cref="IconEntry.BitDepth"/> already exists at a different index.</returns>
            public bool SetValue(int index, IconEntry item)
            {
                return _setValue(index, item, false);
            }

            /// <summary>
            /// Removes the element at the specified index.
            /// </summary>
            /// <param name="index">The element at the specified index.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than 0 or is greater than or equal to <see cref="Count"/>.
            /// </exception>
            public void RemoveAt(int index)
            {
                IconEntry item = _items[index];

                bool remove = _noDups || _items.Where(i => i != item && i.EntryKey == item.EntryKey).FirstOrDefault() != null;
                if (remove)
                    _set.Remove(item.EntryKey);
                item.File = null;
                _items.RemoveAt(index);
                if (remove && !_noDups)
                    _noDups = _set.Count == _items.Count;
            }

            /// <summary>
            /// Removes the specified icon entry from the list.
            /// </summary>
            /// <param name="item">The icon entry to remove from the list.</param>
            /// <returns><c>true</c> if <paramref name="item"/> was found and successfully removed; <c>false</c> otherwise.</returns>
            public bool Remove(IconEntry item)
            {
                int index = _items.IndexOf(item);
                if (index < 0) return false;
                RemoveAt(index);
                return true;
            }

            void IList.Remove(object value)
            {
                Remove(value as IconEntry);
            }

            /// <summary>
            /// Removes an icon entry similar to the specified value from the list.
            /// </summary>
            /// <param name="item">The icon entry to compare.</param>
            /// <returns><c>true</c> if an icon entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>
            /// as <paramref name="item"/> was successfully found and removed; <c>false</c> if no such icon entry was found in the list.</returns>
            public bool RemoveSimilar(IconEntry item)
            {
                if (item == null) return false;
                return RemoveSimilar(item.EntryKey);
            }

            /// <summary>
            /// Removes an icon entry similar to the specified value from the list.
            /// </summary>
            /// <param name="key">The entry key to compare.</param>
            /// <returns><c>true</c> if an icon entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>
            /// as <paramref name="key"/> was successfully found and removed; <c>false</c> if no such icon entry was found in the list.</returns>
            public bool RemoveSimilar(IconEntryKey key)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    if (key == _items[i].EntryKey)
                    {
                        RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Removes an icon entry similar to the specified values from the list.
            /// </summary>
            /// <param name="width">The width of the icon entry to search for.</param>
            /// <param name="height">The height of the icon entry to search for.</param>
            /// <param name="bitDepth">The bit depth of the icon entry to search for.</param>
            /// <returns><c>true</c> if an icon entry with the same <see cref="IconEntry.Width"/> as <paramref name="width"/>, the same <see cref="IconEntry.Height"/>
            /// as <paramref name="height"/>, and the same <see cref="IconEntry.BitDepth"/> as <paramref name="bitDepth"/>  was successfully found and removed;
            /// <c>false</c> if no such icon entry was found in the list.</returns>
            public bool RemoveSimilar(short width, short height, IconBitDepth bitDepth)
            {
                if (!IconEntryKey.IsValid(width, height, bitDepth)) return false;
                return RemoveSimilar(new IconEntryKey(width, height, bitDepth));
            }

            /// <summary>
            /// Removes all elements from the list.
            /// </summary>
            public void Clear()
            {
                foreach (IconEntry item in _items)
                    item.File = null;
                _set.Clear();
                _items.Clear();
                _noDups = true;
            }

            /// <summary>
            /// Determines if the specified element exists in the list.
            /// </summary>
            /// <param name="item">The icon entry to search for in the list.</param>
            /// <returns><c>true</c> if <paramref name="item"/> was found; <c>false</c> otherwise.</returns>
            public bool Contains(IconEntry item)
            {
                return _file.IsValid(item) && _items.Contains(item);
            }

            bool IList.Contains(object value)
            {
                return Contains(value as IconEntry);
            }

            /// <summary>
            /// Determines if an element similar to the specified icon entry exists in the list.
            /// </summary>
            /// <param name="item">The icon entry to compare.</param>
            /// <returns><c>true</c> if an icon entry with the same with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>
            /// as <paramref name="item"/> exists in the list; <c>false</c> otherwise.</returns>
            public bool ContainsSimilar(IconEntry item)
            {
                return item != null && _set.Contains(item.EntryKey);
            }

            /// <summary>
            /// Determines if an element similar to the specified value exists in the list.
            /// </summary>
            /// <param name="key">The entry key to compare.</param>
            /// <returns><c>true</c> if an icon entry with the same with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>
            /// as <paramref name="key"/> exists in the list; <c>false</c> otherwise.</returns>
            public bool ContainsSimilar(IconEntryKey key)
            {
                return _set.Contains(key);
            }

            /// <summary>
            /// Determines if an element similar to the specified values exists in the list.
            /// </summary>
            /// <param name="width">The width of the icon entry to search for.</param>
            /// <param name="height">The height of the icon entry to search for.</param>
            /// <param name="bitDepth">The bit depth of the icon entry to search for.</param>
            /// <returns><c>true</c> if an icon entry with the same <see cref="IconEntry.Width"/> as <paramref name="width"/>, the same <see cref="IconEntry.Height"/>
            /// as <paramref name="height"/>, and the same <see cref="IconEntry.BitDepth"/> as <paramref name="bitDepth"/>  was found;
            /// <c>false</c> if no such icon entry was found in the list.</returns>
            public bool ContainsSimilar(short width, short height, IconBitDepth bitDepth)
            {
                if (!IconEntryKey.IsValid(width, height, bitDepth)) return false;
                return _set.Contains(new IconEntryKey(width, height, bitDepth));
            }

            /// <summary>
            /// Gets the index of the specified item.
            /// </summary>
            /// <param name="item">The icon entry to search for in the list.</param>
            /// <returns>The index of <paramref name="item"/>, if found; otherwise, -1.</returns>
            public int IndexOf(IconEntry item)
            {
                if (!_file.IsValid(item)) return -1;
                return _items.IndexOf(item);
            }

            int IList.IndexOf(object value)
            {
                return IndexOf(value as IconEntry);
            }

            /// <summary>
            /// Gets the index of an element similar to the specified item.
            /// </summary>
            /// <param name="item">The icon entry to compare.</param>
            /// <returns>The index of an icon entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>
            /// as <paramref name="item"/>, if found; otherwise, -1.</returns>
            public int IndexOfSimilar(IconEntry item)
            {
                if (item == null) return -1;
                return IndexOfSimilar(item.EntryKey);
            }

            /// <summary>
            /// Gets the index of an element similar to the specified value.
            /// </summary>
            /// <param name="key">The entry key to compare.</param>
            /// <returns>The index of an icon entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>
            /// as <paramref name="key"/>, if found; otherwise, -1.</returns>
            public int IndexOfSimilar(IconEntryKey key)
            {
                for (int i = 0; i < _items.Count; i++)
                    if (key == _items[i].EntryKey) return i;
                return -1;
            }

            /// <summary>
            /// Gets the index of an element similar to the specified values.
            /// </summary>
            /// <param name="width">The width of the icon entry to search for.</param>
            /// <param name="height">The height of the icon entry to search for.</param>
            /// <param name="bitDepth">The bit depth of the icon entry to search for.</param>
            /// <returns>The index of an icon entry with the same <see cref="IconEntry.Width"/> as <paramref name="width"/>, the same <see cref="IconEntry.Height"/>
            /// as <paramref name="height"/>, and the same <see cref="IconEntry.BitDepth"/> as <paramref name="bitDepth"/>, if found; otherwise, -1.</returns>
            public int IndexOfSimilar(short width, short height, IconBitDepth bitDepth)
            {
                if (!IconEntryKey.IsValid(width, height, bitDepth)) return -1;
                return IndexOfSimilar(new IconEntryKey(width, height, bitDepth));
            }

            /// <summary>
            /// Copies all elements in the list to the specified array, starting at the specified index.
            /// </summary>
            /// <param name="array">The array to which all elements in the list will be copied.</param>
            /// <param name="arrayIndex">The index in <paramref name="array"/> at which copying begins.</param>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="array"/> is <c>null</c>.
            /// </exception>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="arrayIndex"/> is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// The length of <paramref name="array"/> minus <paramref name="arrayIndex"/> is less than <see cref="Count"/>.
            /// </exception>
            public void CopyTo(IconEntry[] array, int arrayIndex)
            {
                _items.CopyTo(array, arrayIndex);
            }

            private void _binarySearchCheck(int index, int count)
            {
                if (index < 0) throw new ArgumentOutOfRangeException("index");
                if (count < 0) throw new ArgumentOutOfRangeException("count");
                if (index + count > _items.Count) throw new ArgumentException();
            }

            private int _binarySearch(int index, int count, IconEntryKey key)
            {
                int low = index, high = index + count - 1;

                while (low <= high)
                {
                    int i = low + (high - low) / 2;
                    int comp = key.CompareTo(_items[i].EntryKey);
                    if (comp == 0) return i;
                    if (comp < 0)
                        high = i - 1;
                    else
                        low = i + 1;
                }
                return ~low;
            }

            /// <summary>
            /// Performs a binary search for the specified entry within the entire list.
            /// This method presumes that the list is already sorted according to each <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <param name="entry">The icon entry to search for.</param>
            /// <returns>The index of <paramref name="entry"/>, if found; otherwise, the bitwise complement of the 
            /// index where <paramref name="entry"/> would be.</returns>
            public int BinarySearch(IconEntry entry)
            {
                return BinarySearch(0, _items.Count, entry);
            }

            /// <summary>
            /// Performs a binary search for the specified entry within the specified range of elements.
            /// This method presumes that the list is already sorted according to each <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <param name="index">The index in the list at which the search begins.</param>
            /// <param name="count">The number of elements to search.</param>
            /// <param name="entry">The icon entry to search for.</param>
            /// <returns>The index of <paramref name="entry"/>, if found; otherwise, the bitwise complement of the 
            /// index where <paramref name="entry"/> would be.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> or <paramref name="count"/> is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="index"/> and <paramref name="count"/> do not indicate a valid range of elements in the list.
            /// </exception>
            public int BinarySearch(int index, int count, IconEntry entry)
            {
                int dex = BinarySearchSimilar(index, count, entry);
                if (dex < 0 || _items[dex] == entry) return dex;
                return ~dex;
            }

            /// <summary>
            /// Performs a binary search for an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>,
            /// and <see cref="IconEntry.BitDepth"/> as the specified entry within the entire list.
            /// This method presumes that the list is already sorted according to each <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <param name="entry">The icon entry to search for.</param>
            /// <returns>The index of an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and
            /// <see cref="IconEntry.BitDepth"/> as <paramref name="entry"/>, if found; otherwise, the bitwise complement of the
            /// index where <paramref name="entry"/> would be.</returns>
            public int BinarySearchSimilar(IconEntry entry)
            {
                return BinarySearchSimilar(0, _items.Count, entry);
            }

            /// <summary>
            /// Performs a binary search for an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>,
            /// and <see cref="IconEntry.BitDepth"/> as the specified entry within the specified range of elements.
            /// This method presumes that the list is already sorted according to each <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <param name="index">The index in the list at which the search begins.</param>
            /// <param name="count">The number of elements to search.</param>
            /// <param name="entry">The icon entry to search for.</param>
            /// <returns>The index of an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and
            /// <see cref="IconEntry.BitDepth"/> as <paramref name="entry"/>, if found; otherwise, the bitwise complement of the
            /// index where <paramref name="entry"/> would be.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> or <paramref name="count"/> is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="index"/> and <paramref name="count"/> do not indicate a valid range of elements in the list.
            /// </exception>
            public int BinarySearchSimilar(int index, int count, IconEntry entry)
            {
                _binarySearchCheck(index, count);
                if (entry == null) return ~index;
                return _binarySearch(0, _items.Count, entry.EntryKey);
            }

            /// <summary>
            /// Performs a binary search for an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>,
            /// and <see cref="IconEntry.BitDepth"/> as the specified key within the entire list.
            /// This method presumes that the list is already sorted according to each <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <param name="key">The entry key to search for.</param>
            /// <returns>The index of an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and
            /// <see cref="IconEntry.BitDepth"/> as <paramref name="key"/>, if found; otherwise, the bitwise complement of the
            /// index where <paramref name="key"/> would be.</returns>
            public int BinarySearchSimilar(IconEntryKey key)
            {
                return _binarySearch(0, _items.Count, key);
            }

            /// <summary>
            /// Performs a binary search for an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>,
            /// and <see cref="IconEntry.BitDepth"/> as the specified key within the specified range of elements.
            /// This method presumes that the list is already sorted according to each <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <param name="index">The index in the list at which the search begins.</param>
            /// <param name="count">The number of elements to search.</param>
            /// <param name="key">The entry key to search for.</param>
            /// <returns>The index of <paramref name="key"/>, if found; otherwise, the bitwise complement of the 
            /// index where <paramref name="key"/> would be.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> or <paramref name="count"/> is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="index"/> and <paramref name="count"/> do not indicate a valid range of elements in the list.
            /// </exception>
            public int BinarySearchSimilar(int index, int count, IconEntryKey key)
            {
                _binarySearchCheck(index, count);
                return _binarySearch(index, count, key);
            }

            /// <summary>
            /// Performs a binary search for an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>,
            /// and <see cref="IconEntry.BitDepth"/> as the specified key within the entire list.
            /// This method presumes that the list is already sorted according to each <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <param name="width">The width of the icon entry to search for.</param>
            /// <param name="height">The height of the icon entry to search for.</param>
            /// <param name="bitDepth">The bit depth of the icon entry to search for.</param>
            /// <returns>The index of an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and
            /// <see cref="IconEntry.BitDepth"/> as <paramref name="width"/>, <paramref name="height"/>, and <paramref name="bitDepth"/>,
            /// if found; otherwise, the bitwise complement of the index of where <paramref name="width"/>, <paramref name="height"/>,
            /// and <paramref name="bitDepth"/> would be.</returns>
            public int BinarySearchSimilar(short width, short height, IconBitDepth bitDepth)
            {
                if (!IconEntryKey.IsValid(width, height, bitDepth)) return ~0;
                return BinarySearchSimilar(new IconEntryKey(width, height, bitDepth));
            }

            /// <summary>
            /// Performs a binary search for an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>,
            /// and <see cref="IconEntry.BitDepth"/> as the specified key within the entire list.
            /// This method presumes that the list is already sorted according to each <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <param name="index">The index in the list at which the search begins.</param>
            /// <param name="count">The number of elements to search.</param>
            /// <param name="width">The width of the icon entry to search for.</param>
            /// <param name="height">The height of the icon entry to search for.</param>
            /// <param name="bitDepth">The bit depth of the icon entry to search for.</param>
            /// <returns>The index of an entry with the same <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and
            /// <see cref="IconEntry.BitDepth"/> as <paramref name="width"/>, <paramref name="height"/>, and <paramref name="bitDepth"/>,
            /// if found; otherwise, the bitwise complement of the index of where <paramref name="width"/>, <paramref name="height"/>,
            /// and <paramref name="bitDepth"/> would be.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> or <paramref name="count"/> is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// <paramref name="index"/> and <paramref name="count"/> do not indicate a valid range of elements in the list.
            /// </exception>
            public int BinarySearchSimilar(int index, int count, short width, short height, IconBitDepth bitDepth)
            {
                if (!IconEntryKey.IsValid(width, height, bitDepth)) return -1;
                return BinarySearchSimilar(index, count, new IconEntryKey(width, height, bitDepth));
            }

            /// <summary>
            /// Sorts all elements in the list according to their <see cref="IconEntry.EntryKey"/> value.
            /// </summary>
            /// <remarks>
            /// This method raises the <see cref="CollectionChanged"/> event using <see cref="NotifyCollectionChangedAction.Reset"/>
            /// if the list contains more than 2 elements.
            /// </remarks>
            public void Sort()
            {
                if (_items.Count < 2) return;

                if (_items.Count == 2)
                {
                    if (_items[0].EntryKey < _items[1].EntryKey)
                        return;
                    _items.Move(1, 0);
                    return;
                }

                var allItems = _items.ToArray();
                Array.Sort(allItems, new IconEntryComparer());

                bool allSame = true;
                for (int i = 0; i < allItems.Length; i++)
                {
                    if (_items[i] != allItems[i])
                    {
                        allSame = false;
                        break;
                    }
                }
                if (allSame) return;

                _setItems(new ObservableCollection<IconEntry>(allItems));
                if (CollectionChanged != null)
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Item[]"));
            }

            /// <summary>
            /// Returns an enumerator which iterates through the list.
            /// </summary>
            /// <returns>An enumerator which iterates through the list.</returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<IconEntry> IEnumerable<IconEntry>.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// Moves an element from one index to another.
            /// </summary>
            /// <param name="oldIndex">The index of the element to move.</param>
            /// <param name="newIndex">The destination index.</param>
            public void Move(int oldIndex, int newIndex)
            {
                _items.Move(oldIndex, newIndex);
            }

            /// <summary>
            /// Returns an array containing all elements in the current list.
            /// </summary>
            /// <returns>An array containing elements copied from the current list.</returns>
            public IconEntry[] ToArray()
            {
                return _items.ToArray();
            }

            /// <summary>
            /// Searches for an element which matches the specified predicate, and returns the first matching icon entry in the list.
            /// </summary>
            /// <param name="match">A predicate used to define the element to search for.</param>
            /// <returns>An icon entry matching the specified predicate, or <c>null</c> if no such icon entry was found.</returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="match"/> is <c>null</c>.
            /// </exception>
            public IconEntry Find(Predicate<IconEntry> match)
            {
                if (match == null) throw new ArgumentNullException("match");
                foreach (IconEntry entry in _items)
                    if (match(entry)) return entry;
                return null;
            }

            /// <summary>
            /// Searches for an element which matches the specified predicate, and returns the index of the first matching icon entry in the list.
            /// </summary>
            /// <param name="match">A predicate used to define the element to search for.</param>
            /// <returns>The index of the icon entry matching the specified predicate, or -1 if no such icon entry was found.</returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="match"/> is <c>null</c>.
            /// </exception>
            public int FindIndex(Predicate<IconEntry> match)
            {
                if (match == null) throw new ArgumentNullException("match");
                for (int i = 0; i < _items.Count; i++)
                    if (match(_items[i])) return i;
                return -1;
            }

            /// <summary>
            /// Determines whether any element matching the specified predicate exists in the list.
            /// </summary>
            /// <param name="match">A predicate used to define the elements to search for.</param>
            /// <returns><c>true</c> if at least one element matches the specified predicate; <c>false</c> otherwise.</returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="match"/> is <c>null</c>.
            /// </exception>
            public bool Exists(Predicate<IconEntry> match)
            {
                return FindIndex(match) >= 0;
            }

            /// <summary>
            /// Determines whether every element in the list matches the specified predicate.
            /// </summary>
            /// <param name="match">A predicate used to define the elements to search for.</param>
            /// <returns><c>true</c> if every element in the list matches the specified predicate; <c>false</c> otherwise.</returns>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="match"/> is <c>null</c>.
            /// </exception>
            public bool TrueForAll(Predicate<IconEntry> match)
            {
                if (match == null) throw new ArgumentNullException("match");
                for (int i = 0; i < _items.Count; i++)
                    if (!match(_items[i])) return false;
                return true;
            }

            /// <summary>
            /// Returns a list containing all icon entries which match the specified predicate.
            /// </summary>
            /// <param name="match">A predicate used to define the elements to search for.</param>
            /// <returns>A list containing all elements matching <paramref name="match"/>.</returns>
            public List<IconEntry> FindAll(Predicate<IconEntry> match)
            {
                if (match == null) throw new ArgumentNullException("match");
                List<IconEntry> found = new List<IconEntry>();
                foreach (IconEntry curEntry in _items)
                    if (match(curEntry)) found.Add(curEntry);
                return found;
            }

            bool ICollection<IconEntry>.IsReadOnly
            {
                get { return true; }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            bool IList.IsFixedSize
            {
                get { return false; }
            }

            bool IList.IsReadOnly
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)_items).SyncRoot; }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                ((ICollection)_items).CopyTo(array, index);
            }

            /// <summary>
            /// An enumerator which iterates through the list.
            /// </summary>
            public struct Enumerator : IEnumerator<IconEntry>
            {
                private IconEntry _current;
                private IEnumerator<IconEntry> _enum;

                internal Enumerator(EntryList set)
                {
                    _current = null;
                    _enum = set._items.GetEnumerator();
                }

                /// <summary>
                /// Gets the element at the current position in the enumerator.
                /// </summary>
                public IconEntry Current
                {
                    get { return _current; }
                }

                object IEnumerator.Current
                {
                    get { return _current; }
                }

                /// <summary>
                /// Disposes of the current instance.
                /// </summary>
                public void Dispose()
                {
                    if (_enum == null) return;
                    _enum.Dispose();
                    _enum = null;
                    _current = null;
                }

                /// <summary>
                /// Advances the enumerator to the next position in the list.
                /// </summary>
                /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the enumerator has passed the end of the list.</returns>
                public bool MoveNext()
                {
                    if (_enum == null) return false;
                    if (!_enum.MoveNext())
                    {
                        Dispose();
                        return false;
                    }
                    _current = _enum.Current;
                    return true;
                }

                void IEnumerator.Reset()
                {
                    _enum.Reset();
                }
            }

            private class DebugView
            {
                private EntryList _list;

                public DebugView(EntryList list)
                {
                    _list = list;
                }

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public IconEntry[] Items
                {
                    get { return _list._items.ToArray(); }
                }
            }
        }
    }

    /// <summary>
    /// The type code for an icon file.
    /// </summary>
    public enum IconTypeCode : short
    {
        /// <summary>
        /// Indicates an unknown or invalid file.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Indicates an icon (.ICO) file.
        /// </summary>
        Icon = 1,
        /// <summary>
        /// Indicates a cursor (.CUR) file.
        /// </summary>
        Cursor = 2,
    }

    /// <summary>
    /// A delegate function for handling <see cref="IconLoadException"/> errors.
    /// </summary>
    /// <param name="e">An <see cref="IconLoadException"/> containing information about the error.</param>
    public delegate void IconLoadExceptionHandler(IconLoadException e);

    /// <summary>
    /// A delegate function for handling <see cref="IconExtractException"/> errors.
    /// </summary>
    /// <param name="e">An <see cref="IconExtractException"/> containing information about the error.</param>
    public delegate void IconExtractExceptionHandler(IconExtractException e);
}
