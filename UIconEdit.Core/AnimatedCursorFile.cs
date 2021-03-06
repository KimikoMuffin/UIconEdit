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

#if DRAWING
using FileFormatException = System.IO.InvalidDataException;

namespace UIconDrawing
#else
using System.Windows;

namespace UIconEdit
#endif
{
    /// <summary>
    /// Represents an animated cursor file.
    /// </summary>
    public class AnimatedCursorFile :
#if DRAWING
        IDisposable, INotifyPropertyChanged
#else
        DependencyObject
#endif
    {
        /// <summary>
        /// Initializes a new <see cref="AnimatedCursorFile"/> instance.
        /// </summary>
        public AnimatedCursorFile()
        {
            _entries = new EntryList(this);
        }

        #region Load
        private const int _idBaseRiff = 0x46464952; //little-endian "RIFF"
        private const int _idBaseAcon = 0x4e4f4341; //little-endian "ACON"
        private const int _idChnkAnih = 0x68696e61; //little-endian "anih"
        private const int _idChnkList = 0x5453494c; //little-endian "LIST"
        private const int _idListInfo = 0x4f464e49; //little-endian "INFO"
        private const int _idListFram = 0x6d617266; //little-endian "fram"
        private const int _idItemIcon = 0x6e6f6369; //little-endian "icon"
        private const int _idItemInam = 0x4d414e49; //little-endian "INAM"
        private const int _idItemIart = 0x54524149; //little-endian "IART"
        private const int _idChnkRate = 0x65746172; //little-endian "rate"
        private const int _idChnkSeq = 0x20716573; //little-endian "seq "

        private const int sizeAnih = 36;

        /// <summary>
        /// Loads an animated cursor file from the specified stream.
        /// </summary>
        /// <param name="input">The stream containing the animated cursor file to load.</param>
        /// <param name="handler">An error handler for loading the individual cursor files, or <see langword="null"/> to always throw an exception.</param>
        /// <returns>A loaded <see cref="AnimatedCursorFile"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="input"/> is closed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="input"/> is closed.
        /// </exception>
        /// <exception cref="IconExtractException">
        /// An error occurred when loading the animated cursor file.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// An error occurred when loading the animated cursor file.
        /// </exception>
        public static AnimatedCursorFile Load(Stream input, IconExtractExceptionHandler handler)
        {
            AnimatedCursorFile file = new AnimatedCursorFile();

            Dictionary<int, MemoryStream> chunks = new Dictionary<int, MemoryStream>(), lists = new Dictionary<int, MemoryStream>();

            using (MemoryStream ms = new MemoryStream())
            using (BinaryReader reader = new BinaryReader(ms))
            {
                using (BinaryReader br = new BinaryReader(input, new UTF8Encoding(), true))
                {
                    if (br.ReadUInt32() != _idBaseRiff) throw new FileFormatException();
                    _copyBuffer(br, null, ms);
                }

                if (reader.ReadUInt32() != _idBaseAcon)
                    throw new FileFormatException();

                while (ms.Position + 8 < ms.Length)
                {
                    int id = reader.ReadInt32();

                    MemoryStream curStream = null;
                    int length = reader.ReadInt32();

                    if (id == _idChnkList)
                    {
                        length -= 4;

                        id = reader.ReadInt32();

                        if ((id == _idListInfo || id == _idListFram) && !lists.ContainsKey(id))
                        {
                            curStream = new MemoryStream();

                            lists.Add(id, curStream);
                        }

                    }
                    else if ((id == _idChnkAnih || id == _idChnkRate || id == _idChnkSeq) && !chunks.ContainsKey(id))
                    {
                        curStream = new MemoryStream();

                        chunks.Add(id, curStream);
                    }

                    _copyBuffer(reader, length, curStream);
                }
            }

            try
            {
                int frameCount, steps, displayRate;

                bool hasSequences, rawData;

                MemoryStream ms;

                if (lists.TryGetValue(_idListInfo, out ms))
                {
                    string name = null, author = null;

                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        while (ms.Position + 8 < ms.Length)
                        {
                            int id = reader.ReadInt32();
                            int length = reader.ReadInt32();
                            if (length < 0) throw new FileFormatException();

                            byte[] data = reader.ReadBytes(length);
                            if (data.Length < length) throw new EndOfStreamException();

                            if ((length & 1) == 1)
                                reader.ReadByte();

                            try
                            {
                                if (id == _idItemInam)
                                    name = _getString(data);
                                else if (id == _idItemIart)
                                    author = _getString(data);
                            }
                            catch
                            {
                                string eName;
                                if (id == _idItemInam)
                                    eName = "INAM";
                                else
                                    eName = "IART";

                                throw new FileFormatException(string.Format("Invalid UTF-8 data under {0} info.", eName));
                            }
                        }

#if DRAWING
                        file._name = name;
                        file._author = author;
#else
                        file.CursorName = name;
                        file.CursorAuthor = author;
#endif
                    }
                }

                #region "anih" chunk
                if (!chunks.TryGetValue(_idChnkAnih, out ms))
                    throw new FileFormatException();

                using (BinaryReader reader = new BinaryReader(ms))
                {
                    if (ms.Length != sizeAnih || sizeAnih != reader.ReadUInt32())
                        throw new FileFormatException();

                    frameCount = reader.ReadInt32();
                    if (frameCount < 0) throw new FileFormatException();
                    steps = reader.ReadInt32();
                    if (steps < 0) throw new FileFormatException();

                    int width = reader.ReadInt32();
                    if (width < 0) throw new FileFormatException();
                    int height = reader.ReadInt32();
                    if (height < 0) throw new FileFormatException();

                    int bitsPerPixel = reader.ReadInt32();
                    switch (bitsPerPixel)
                    {
                        case 0:
                        case 1:
                        case 4:
                        case 8:
                        case 24:
                        case 32:
                            break;
                        default:
                            throw new FileFormatException("Invalid bits per pixel: " + bitsPerPixel);
                    }
                    int numPanes = reader.ReadInt32();
                    displayRate = reader.ReadInt32();

                    if (displayRate <= 0) throw new FileFormatException();

#if DRAWING
                    file._rate = displayRate;
#else
                    file.DisplayRateJiffies = displayRate;
#endif
                    uint flags = reader.ReadUInt32();

                    hasSequences = (flags & 2) != 0;
                    rawData = (flags & 1) == 0;

                    if (rawData) throw new NotSupportedException("Raw data is not supported."); //TODO: figure out how raw data works?
                }
                #endregion

                #region "fram" list (with "icon" chunks)
                AnimatedCursorFrame[] frames = new AnimatedCursorFrame[frameCount];

                if (!lists.TryGetValue(_idListFram, out ms))
                    throw new FileFormatException();

                using (BinaryReader reader = new BinaryReader(ms))
                {
                    for (int i = 0; i < frameCount; i++)
                    {
                        if (reader.ReadInt32() != _idItemIcon)
                            throw new FileFormatException();

                        using (MemoryStream iconStream = new MemoryStream())
                        {
                            _copyBuffer(reader, null, iconStream);

                            if (rawData)
                            {
                                //TODO: Figure out how raw data works?
                                throw new NotSupportedException("Raw data is not supported.");
                            }
                            else
                            {
                                try
                                {
                                    IconLoadExceptionHandler sHandler = null;
                                    if (handler != null)
                                        sHandler = e => handler(new IconExtractException(e, i));

                                    IconFileBase iconFile = IconFileBase.Load(iconStream, sHandler);

                                    CursorFile cursorFile = iconFile as CursorFile;

                                    if (cursorFile == null)
                                    {
                                        cursorFile = iconFile.CloneAsCursorFile();
#if DRAWING
                                        iconFile.Dispose();
#endif
                                    }
                                    frames[i] = new AnimatedCursorFrame(cursorFile);
                                }
                                catch (IconLoadException e)
                                {
                                    throw new IconExtractException(e, i);
                                }
                                catch (Exception e)
                                {
                                    throw new IconExtractException(e, IconTypeCode.Unknown, i);
                                }
                            }
                        }
                    }
                }
                #endregion

                #region "seq " chunk
                int[] indices = null;
                if (hasSequences && chunks.TryGetValue(_idChnkSeq, out ms))
                {
                    if (ms.Length != steps * 4)
                        throw new FileFormatException("Invalid seq chunk size.");

                    indices = new int[steps];

                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        for (int i = 0; i < steps; i++)
                        {
                            int curVal = reader.ReadInt32();
                            if (curVal < 0 || curVal >= frameCount)
                                throw new FileFormatException(string.Format("Invalid sequence index at entry {0}: {1}", i, curVal));
                            indices[i] = curVal;
                        }
                    }
                }
                #endregion

                #region "rate" chunk
                if (chunks.TryGetValue(_idChnkRate, out ms))
                {
                    if (ms.Length != frameCount * 4)
                        throw new FileFormatException("Invalid rate chunk size.");

                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        for (int i = 0; i < frameCount; i++)
                        {
                            int curRate = reader.ReadInt32();
                            if (curRate <= 0)
                                throw new FileFormatException(string.Format("Invalid rate at entry {0}: {1}", i, curRate));
                            if (curRate != displayRate)
                                frames[i].LengthJiffies = curRate;
                        }
                    }
                }
                #endregion

                switch (AllEntriesSame(frames))
                {
                    case EntryKeyResult.NoEntries:
                        throw new InvalidDataException("No entries were loaded.");
                    case EntryKeyResult.Different:
                        throw new InvalidDataException("Mismatch between entries.");
                    case EntryKeyResult.EmptyList:
                        throw new InvalidDataException("At least one entry contains zero elements.");
                }

                foreach (AnimatedCursorFrame cFrame in frames)
                    file._entries.Add(cFrame);

                if (hasSequences)
                {
                    foreach (int i in indices)
                        file._indices.Add(i);
                }

                return file;
            }
            finally
            {
                foreach (MemoryStream ms in chunks.Values.Concat(lists.Values))
                    ms.Close();
            }
        }

        /// <summary>
        /// Loads an animated cursor file from the specified stream.
        /// </summary>
        /// <param name="input">The stream containing the animated cursor file to load.</param>
        /// <returns>A loaded <see cref="AnimatedCursorFile"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="input"/> is closed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="input"/> is closed.
        /// </exception>
        /// <exception cref="IconExtractException">
        /// An error occurred when loading the animated cursor file.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// An error occurred when loading the animated cursor file.
        /// </exception>
        public static AnimatedCursorFile Load(Stream input)
        {
            return Load(input, null);
        }

        /// <summary>
        /// Loads an <see cref="AnimatedCursorFile"/> from the specified path.
        /// </summary>
        /// <param name="path">The path to a cursor file.</param>
        /// <param name="handler">An error handler for loading the individual cursor files, or <see langword="null"/> to always throw an exception.</param>
        /// <returns>An <see cref="IconFileBase"/> implementation loaded from <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is empty, contains only whitespace, or contains one or more invalid path characters as defined in
        ///  <see cref="Path.GetInvalidPathChars()"/>.
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
        /// <exception cref="IconExtractException">
        /// An error occurred when loading the animated cursor file.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// An error occurred when loading the animated cursor file.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public static AnimatedCursorFile Load(string path, IconExtractExceptionHandler handler)
        {
            using (FileStream fs = File.OpenRead(path))
                return Load(fs, handler);
        }

        /// <summary>
        /// Loads an <see cref="AnimatedCursorFile"/> from the specified path.
        /// </summary>
        /// <param name="path">The path to a cursor file.</param>
        /// <returns>An <see cref="IconFileBase"/> implementation loaded from <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is empty, contains only whitespace, or contains one or more invalid path characters as defined in
        ///  <see cref="Path.GetInvalidPathChars()"/>.
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
        /// An error occurred when processing the cursor file's format.
        /// </exception>
        /// <exception cref="FileFormatException">
        /// An error occurred when processing the cursor file's format.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public static AnimatedCursorFile Load(string path)
        {
            return Load(path, null);
        }

        private static void _copyBuffer(BinaryReader reader, int? getLength, MemoryStream output)
        {
            int length;

            if (getLength.HasValue)
                length = getLength.Value;
            else
                length = reader.ReadInt32();
            if (length < 0) throw new FileFormatException();

            const int bufferSize = 8192;

            byte[] buffer = new byte[bufferSize];

            while (length > 0)
            {
                int read = reader.Read(buffer, 0, Math.Min(length, bufferSize));

                if (read == 0) throw new EndOfStreamException();

                if (output != null)
                    output.Write(buffer, 0, read);

                length -= read;
            }

            if (output != null)
                output.Seek(0, SeekOrigin.Begin);
        }

        private static string _getString(byte[] buffer)
        {
            string s = Encoding.UTF8.GetString(buffer);

            int nullDex = s.IndexOf('\0');

            if (nullDex >= 0)
                return s.Substring(0, nullDex);

            return s;
        }
        #endregion

        #region Save
#if DRAWING
        /// <summary>
        /// Saves the current instance to the specified stream.
        /// </summary>
        /// <param name="output">The stream to which the current instance is written.</param>
        /// <exception cref="ObjectDisposedException">
        /// <para>The current instance is disposed.</para>
        /// <para>-OR-</para>
        /// <para><paramref name="output"/> is closed.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <para><see cref="Entries"/> is empty.</para>
        /// <para>-OR-</para>
        /// <para>The elements in <see cref="Entries"/> do not all have the same number of <see cref="IconEntry"/> objects with the same
        /// combination of <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>.</para>
        /// <para>-OR-</para>
        /// <para><see cref="FrameIndices"/> contains elements which are less than 0, or are greater than or equal to the number of elements
        /// in <see cref="Entries"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="output"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="output"/> does not support writing.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public void Save(Stream output)
#else
        /// <summary>
        /// Saves the current instance to the specified stream.
        /// </summary>
        /// <param name="output">The stream to which the current instance is written.</param>
        /// <exception cref="InvalidOperationException">
        /// <para><see cref="Entries"/> is empty.</para>
        /// <para>-OR-</para>
        /// <para>The elements in <see cref="Entries"/> do not all have the same number of <see cref="IconEntry"/> objects with the same
        /// combination of <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>.</para>
        /// <para>-OR-</para>
        /// <para><see cref="FrameIndices"/> contains elements which are less than 0, or are greater than or equal to the number of elements
        /// in <see cref="Entries"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="output"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="output"/> is closed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="output"/> does not support writing.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public void Save(Stream output)
#endif
        {
            using (BinaryWriter outputWriter = new BinaryWriter(output))
            using (MemoryStream ms = Save())
                Save(ms, output, outputWriter);
        }

#if DRAWING
        /// <summary>
        /// Saves the current instance to the specified path.
        /// </summary>
        /// <param name="path">The path to the file to which the current instance will be saved.</param>
        /// <exception cref="ObjectDisposedException">
        /// The current instance is disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <para><see cref="Entries"/> is empty.</para>
        /// <para>-OR-</para>
        /// <para>The elements in <see cref="Entries"/> do not all have the same number of <see cref="IconEntry"/> objects with the same
        /// combination of <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>.</para>
        /// <para>-OR-</para>
        /// <para><see cref="FrameIndices"/> contains elements which are less than 0, or are greater than or equal to the number of elements
        /// in <see cref="Entries"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is empty, contains only whitespace, or contains one or more invalid path characters as defined in
        /// <see cref="Path.GetInvalidPathChars()"/>.
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
#else
        /// <summary>
        /// Saves the current instance to the specified path.
        /// </summary>
        /// <param name="path">The path to the file to which the current instance will be saved.</param>
        /// <exception cref="InvalidOperationException">
        /// <para><see cref="Entries"/> is empty.</para>
        /// <para>-OR-</para>
        /// <para>The elements in <see cref="Entries"/> do not all have the same number of <see cref="IconEntry"/> objects with the same
        /// combination of <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>.</para>
        /// <para>-OR-</para>
        /// <para><see cref="FrameIndices"/> contains elements which are less than 0, or are greater than or equal to the number of elements
        /// in <see cref="Entries"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is empty, contains only whitespace, or contains one or more invalid path characters as defined in
        /// <see cref="Path.GetInvalidPathChars()"/>.
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
#endif
        {
            using (MemoryStream ms = Save())
            {
                using (FileStream fs = File.Create(path))
                using (BinaryWriter fsWriter = new BinaryWriter(fs))
                    Save(ms, fs, fsWriter);
            }
        }

        private MemoryStream Save()
        {
#if DRAWING
            if (_isDisposed) throw new ObjectDisposedException(null);
#endif
            CheckSizes();

            foreach (int i in _indices)
            {
                if (i < 0)
                    throw new InvalidOperationException("The FrameIndices property contains an element which is less than 0.");
                if (i >= _entries.Count)
                    throw new InvalidOperationException("The FrameIndices property contains an element which is greater than or equal to " +
                        "the number of elements in Entries.");
            }

            MemoryStream ms = new MemoryStream();
            using (BinaryWriter msWriter = new BinaryWriter(ms, new UTF8Encoding(), true))
            {
                msWriter.Write(_idBaseAcon);

#if !DRAWING
                string _author = CursorAuthor, _name = CursorName;
                int _rate = DisplayRateJiffies;
#endif
                if (!IsNullOrEmptyExceptForNull(_name) || !IsNullOrEmptyExceptForNull(_author))
                {
                    using (MemoryStream listInfoStream = new MemoryStream())
                    using (BinaryWriter listInfoWriter = new BinaryWriter(listInfoStream))
                    {
                        listInfoWriter.Write(_idListInfo);

                        byte[] nameBytes = GetStringBytes(_name);
                        byte[] authorBytes = GetStringBytes(_author);

                        listInfoWriter.Write(_idItemInam);
                        listInfoWriter.Write(nameBytes.Length);
                        listInfoWriter.Write(nameBytes);
                        if ((nameBytes.Length & 1) == 1) listInfoWriter.Write(byte.MinValue);

                        listInfoWriter.Write(_idItemIart);
                        listInfoWriter.Write(authorBytes.Length);
                        listInfoWriter.Write(authorBytes);
                        if ((authorBytes.Length & 1) == 1) listInfoWriter.Write(byte.MinValue);

                        listInfoStream.Seek(0, SeekOrigin.Begin);

                        msWriter.Write(_idChnkList);
                        msWriter.Write((int)listInfoStream.Length);
                        listInfoStream.CopyTo(ms);
                    }
                }

                msWriter.Write(_idChnkAnih);
                msWriter.Write(sizeAnih);
                msWriter.Write(sizeAnih);

                msWriter.Write(_entries.Count);
                if (_indices.Count == 0) msWriter.Write(_entries.Count);
                else msWriter.Write(_indices.Count);
                msWriter.Write(new byte[16]);
#if DRAWING
                msWriter.Write(_rate);
#else
                msWriter.Write(DisplayRateJiffies);
#endif
                uint flags = 1; //No raw-data here.
                if (_indices.Count != 0)
                    flags |= 2;
                msWriter.Write(flags);

                if (_indices.Count != 0)
                {
                    msWriter.Write(_idChnkSeq);
                    msWriter.Write(_indices.Count * 4);

                    for (int i = 0; i < _indices.Count; i++)
                        msWriter.Write(_indices[i]);
                }

                int[] rates = null;

                using (MemoryStream iconListStream = new MemoryStream())
                using (BinaryWriter iconListWriter = new BinaryWriter(iconListStream))
                {
                    iconListWriter.Write(_idListFram);
                    for (int i = 0; i < _entries.Count; i++)
                    {
                        AnimatedCursorFrame cFrame = _entries[i];

                        using (MemoryStream cStream = new MemoryStream())
                        {
                            cFrame.File.Save(cStream);

                            int? lengthJiffies = cFrame.LengthJiffies;
                            if (lengthJiffies.HasValue && lengthJiffies.Value != _rate)
                            {
                                if (rates == null)
                                {
                                    rates = new int[_entries.Count];
                                    for (int rI = 0; rI < rates.Length; rI++)
                                        rates[rI] = _rate;
                                }
                                rates[i] = lengthJiffies.Value;
                            }

                            iconListWriter.Write(_idItemIcon);
                            iconListWriter.Write((int)cStream.Length);

                            cStream.Seek(0, SeekOrigin.Begin);
                            cStream.CopyTo(iconListStream);
                            if ((cStream.Length & 1) == 1)
                                iconListWriter.Write(byte.MinValue);
                        }
                    }

                    msWriter.Write(_idChnkList);
                    msWriter.Write((int)iconListStream.Length);
                    iconListStream.Seek(0, SeekOrigin.Begin);
                    iconListStream.CopyTo(ms);
                }

                if (rates != null)
                {
                    msWriter.Write(_idChnkRate);
                    msWriter.Write(rates.Length * 4);

                    for (int i = 0; i < rates.Length; i++)
                        msWriter.Write(rates[i]);
                }

                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
        }

        private void CheckSizes()
        {
            if (_entries.Count == 0)
                throw new InvalidOperationException("No entries present.");
            var entry0 = _entries[0];

            for (int i = 1; i < _entries.Count; i++)
            {
                if (!entry0.SimilarListEquals(_entries[i]))
                    throw new InvalidOperationException("All AnimatedCursorFrame objects must have a File with the same number of IconEntry objects, " +
                        "with the same combination of Width, Height, and BitDepth.");
            }
        }

        private void Save(MemoryStream ms, Stream output, BinaryWriter outputWriter)
        {
            outputWriter.Write(_idBaseRiff);
            outputWriter.Write((int)ms.Length);
            ms.CopyTo(output);
        }

        private static bool IsNullOrEmptyExceptForNull(string s)
        {
            return string.IsNullOrEmpty(s) || s[0] == '\0';
        }

        private static byte[] GetStringBytes(string s)
        {
            if (string.IsNullOrEmpty(s)) return new byte[1];

            int dex = s.IndexOf('\0');
            if (dex < 0) s += "\0";
            else s = s.Substring(0, dex + 1);

            return Encoding.UTF8.GetBytes(s);
        }
        #endregion

        private enum EntryKeyResult
        {
            AllSame,
            Different,
            EmptyList,
            NoEntries,
        }

        private static EntryKeyResult AllEntriesSame(ICollection<AnimatedCursorFrame> entries)
        {
            IconEntryKey[] keys = null;

            foreach (AnimatedCursorFrame frame in entries)
            {
                var iconEntries = frame.File.Entries;
                IconEntryKey[] curKeys = new IconEntryKey[iconEntries.Count];

                if (curKeys.Length == 0) return EntryKeyResult.EmptyList;

                for (int i = 0; i < curKeys.Length; i++)
                    curKeys[i] = iconEntries[i].EntryKey;
                Array.Sort(curKeys);

                if (keys == null)
                    keys = curKeys;
                else
                {
                    if (keys.Length != curKeys.Length) return EntryKeyResult.Different;

                    for (int i = 0; i < keys.Length; i++)
                        if (keys[i] != curKeys[i]) return EntryKeyResult.Different;
                }
            }

            if (keys == null) return EntryKeyResult.NoEntries;

            return EntryKeyResult.AllSame;
        }

        /// <summary>
        /// Gets a collection containing all <see cref="IconEntry"/> objects in <see cref="Entries"/>, organized by size and bit depth.
        /// </summary>
        /// <returns>A read-only dictionary</returns>
        /// <exception cref="InvalidOperationException">
        /// <para><see cref="Entries"/> is empty.</para>
        /// <para>-OR-</para>
        /// <para>The elements in <see cref="Entries"/> do not all have the same number of <see cref="IconEntry"/> objects with the same
        /// combination of <see cref="IconEntry.Width"/>, <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/>.</para>
        /// </exception>
        public ReadOnlyDictionary<IconEntryKey, ReadOnlyCollection<AnimatedCursorSingleSizeFrame>> GetFramesBySize()
        {
            CheckSizes();

            Dictionary<IconEntryKey, List<AnimatedCursorSingleSizeFrame>> baseDict =
                _entries[0].File.Entries.OrderBy(i => i.EntryKey).ToDictionary(i => i.EntryKey, i => new List<AnimatedCursorSingleSizeFrame>());

            for (int i = 0; i < _entries.Count; i++)
            {
                var curFrame = _entries[i];
                var curFile = curFrame.File;

                int jiffies;
                if (curFrame.LengthJiffies.HasValue)
                    jiffies = curFrame.LengthJiffies.Value;
                else
                    jiffies = DisplayRateJiffies;

                for (int j = 0; j < curFile.Entries.Count; j++)
                {
                    var curEntry = curFile.Entries[j];

                    baseDict[curEntry.EntryKey].Add(new AnimatedCursorSingleSizeFrame(curEntry, jiffies));
                }
            }

            return new ReadOnlyDictionary<IconEntryKey, ReadOnlyCollection<AnimatedCursorSingleSizeFrame>>(baseDict.
                ToDictionary(i => i.Key, i => i.Value.AsReadOnly()));
        }

        #region Entries
        private EntryList _entries;
        /// <summary>
        /// Gets a list of <see cref="AnimatedCursorFrame"/> objects containing all entries in the animated cursor file.
        /// </summary>
#if !DRAWING
        [Bindable(true)]
#endif
        public EntryList Entries { get { return _entries; } }
        #endregion

        #region FrameIndices
        private ObservableCollection<int> _indices = new ObservableCollection<int>();
        /// <summary>
        /// Gets the ordering of the frames, as indices within <see cref="Entries"/>.
        /// </summary>
#if !DRAWING
        [Bindable(true)]
#endif
        public ObservableCollection<int> FrameIndices { get { return _indices; } }
        #endregion

        internal static TimeSpan JiffiesToTime(int jiffies, string paramName)
        {
            if (jiffies < 1) throw new ArgumentOutOfRangeException(paramName);
            return new TimeSpan(TimeSpan.TicksPerSecond * jiffies / 60);
        }

        /// <summary>
        /// Converts the specified number of "jiffies" (1/60 of a second) to its corresponding <see cref="TimeSpan"/> value.
        /// </summary>
        /// <param name="jiffies">The number of jiffies to convert.</param>
        /// <returns>A <see cref="TimeSpan"/> with a <see cref="TimeSpan.TotalSeconds"/> value equal to <paramref name="jiffies"/> divided by 60.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="jiffies"/> is less than or equal to 0.
        /// </exception>
        public static TimeSpan JiffiesToTime(int jiffies)
        {
            return JiffiesToTime(jiffies, nameof(jiffies));
        }

        internal static int TimeToJiffies(TimeSpan value, string paramName)
        {
            if (value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(paramName);
            long jiffies = 60 * value.Ticks / TimeSpan.TicksPerSecond;

            if (jiffies <= 0 || jiffies > int.MaxValue)
                throw new ArgumentOutOfRangeException(paramName);

            return (int)jiffies;
        }

        /// <summary>
        /// Converts the specified <see cref="TimeSpan"/> to its equivalent number of "jiffies" (1/60 of a second).
        /// </summary>
        /// <param name="value">The <see cref="TimeSpan"/> to convert.</param>
        /// <returns>A number of jiffies equal to <paramref name="value"/>'s <see cref="TimeSpan.TotalSeconds"/> multiplied by 60.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="value"/> translates to a number of jiffies less than or equal to 0, or greater than <see cref="int.MaxValue"/>.
        /// </exception>
        public static int TimeToJiffies(TimeSpan value)
        {
            return TimeToJiffies(value, nameof(value));
        }

        #region DisplayRateJiffies
#if DRAWING
        private int _rate = 10;
#else
        /// <summary>
        /// The dependency property for the <see cref="DisplayRateJiffies"/> property.
        /// </summary>
        public static readonly DependencyProperty DisplayRateJiffiesProperty = DependencyProperty.Register(nameof(DisplayRateJiffies), typeof(int), typeof(AnimatedCursorFile),
            new PropertyMetadata(10, DisplayRateJiffiesChanged, JiffiesCoerce));

        private static void DisplayRateJiffiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(DisplayRateTimeProperty, JiffiesToTime((int)e.NewValue));
        }

        internal static object JiffiesCoerce(DependencyObject d, object baseValue)
        {
            int value = (int)baseValue;
            if (value <= 0) return 1;
            return baseValue;
        }
#endif
        /// <summary>
        /// Gets and sets the default delay before displaying the next frame, in "jiffies" (1/60 of a second).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// In a set operation, the specified value is less than or equal to 0.
        /// </exception>
        public int DisplayRateJiffies
        {
#if DRAWING
            get { return _rate; }
#else
            get { return (int)GetValue(DisplayRateJiffiesProperty); }
#endif
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
#if DRAWING
                _rate = value;

                OnPropertyChanged(nameof(DisplayRateJiffies));
                OnPropertyChanged(nameof(DisplayRateTime));
#else
                SetValue(DisplayRateJiffiesProperty, value);
#endif
            }
        }
        #endregion

        #region DisplayRateTime
#if !DRAWING
        /// <summary>
        /// The dependency property for the <see cref="DisplayRateTime"/> property.
        /// </summary>
        public static readonly DependencyProperty DisplayRateTimeProperty = DependencyProperty.Register(nameof(DisplayRateTime), typeof(TimeSpan), typeof(AnimatedCursorFile),
            new PropertyMetadata(new TimeSpan((TimeSpan.TicksPerSecond * 10) / 60), DisplayRateTimeChanged, TimeCoerce));

        private static void DisplayRateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(DisplayRateJiffiesProperty, TimeToJiffies((TimeSpan)e.NewValue));
        }

        internal static object TimeCoerce(DependencyObject d, object baseValue)
        {
            TimeSpan value = (TimeSpan)baseValue;

            var jiffies = 60 * value.Ticks / TimeSpan.TicksPerSecond;

            if (jiffies < 1)
                return new TimeSpan(TimeSpan.TicksPerSecond / 60);

            if (jiffies > int.MaxValue)
                return new TimeSpan(TimeSpan.TicksPerSecond * int.MaxValue / 60);

            return new TimeSpan(TimeSpan.TicksPerSecond * jiffies / 60);
        }
#endif
        /// <summary>
        /// Gets and sets the default delay before displaying the next frame. Fitted to the nearest "jiffy" (1/60 of a second).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// In a set operation, the specified value translates to a number of jiffies less than or equal to 0 or greater than <see cref="int.MaxValue"/>
        /// </exception>
        public TimeSpan DisplayRateTime
        {
#if DRAWING
            get { return JiffiesToTime(_rate); }
            set { DisplayRateJiffies = TimeToJiffies(value, nameof(value)); }
#else
            get { return (TimeSpan)GetValue(DisplayRateTimeProperty); }
            set
            {
                TimeToJiffies(value, nameof(value));
                SetValue(DisplayRateTimeProperty, value);
            }
#endif
        }
        #endregion

        #region CursorName
#if DRAWING
        private string _name;
#else
        /// <summary>
        /// The dependency property for the <see cref="CursorName"/> property.
        /// </summary>
        public static readonly DependencyProperty CursorNameProperty = DependencyProperty.Register(nameof(CursorName), typeof(string), typeof(AnimatedCursorFile));
#endif
        /// <summary>
        /// Gets and sets the name of the animated cursor file.
        /// This value is null-terminated when written to the file.
        /// </summary>
        public string CursorName
        {
#if DRAWING
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(CursorName));
            }
#else
            get { return (string)GetValue(CursorNameProperty); }
            set { SetValue(CursorNameProperty, value); }
#endif
        }
        #endregion

        #region CursorAuthor
#if DRAWING
        private string _author;
#else
        /// <summary>
        /// The dependency property for the <see cref="CursorAuthor"/> property.
        /// </summary>
        public static readonly DependencyProperty CursorAuthorProperty = DependencyProperty.Register(nameof(CursorAuthor), typeof(string), typeof(AnimatedCursorFile));
#endif
        /// <summary>
        /// Gets and sets the author of the animated cursor file.
        /// This value is null-terminated when written to the file.
        /// </summary>
        public string CursorAuthor
        {
#if DRAWING
            get { return _author; }
            set
            {
                _author = value;
                OnPropertyChanged(nameof(CursorAuthor));
            }
#else
            get { return (string)GetValue(CursorAuthorProperty); }
            set { SetValue(CursorAuthorProperty, value); }
#endif
        }
        #endregion

#if DRAWING
        /// <summary>
        /// Raised when a property on the current instance changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property which was changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Disposal
        /// <summary>
        /// Raised when the current instance is disposed.
        /// </summary>
        public event EventHandler Disposed;

        private bool _isDisposed;
        /// <summary>
        /// Gets a value indicating whether the current instance has been disposed.
        /// </summary>
        public bool IsDisposed { get { return _isDisposed; } }

        /// <summary>
        /// Immediately releases all resources used by the current instance.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            Dispose(true);
            _isDisposed = true;
            if (Disposed != null)
                Disposed(this, EventArgs.Empty);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _entries.Dispose();
            _entries.Clear();
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~AnimatedCursorFile()
        {
            Dispose(false);
        }
        #endregion
#endif

        /// <summary>
        /// Represents a list of <see cref="AnimatedCursorFrame"/> objects for use in an animated cursor file.
        /// </summary>
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(DebugView))]
        public sealed class EntryList : IList<AnimatedCursorFrame>, IReadOnlyList<AnimatedCursorFrame>, IList, INotifyPropertyChanged, INotifyCollectionChanged
        {
            private AnimatedCursorFile _file;
            private ObservableCollection<AnimatedCursorFrame> _items;

            internal EntryList(AnimatedCursorFile file)
            {
                _file = file;
                _items = new ObservableCollection<AnimatedCursorFrame>();
                _items.CollectionChanged += _list_CollectionChanged;
                ((INotifyPropertyChanged)_items).PropertyChanged += _list_PropertyChanged;
            }

            /// <summary>
            /// Raised when the collection changes.
            /// </summary>
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            private void _list_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (CollectionChanged != null)
                    CollectionChanged(this, e);
            }

            private void _list_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, e);
            }

            private event PropertyChangedEventHandler PropertyChanged;
            event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
            {
                add { PropertyChanged += value; }
                remove { PropertyChanged -= value; }
            }

            /// <summary>
            /// Gets and sets the element at the specified index.
            /// </summary>
            /// <param name="index">The index of the element to get or set.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than 0 or is greater than or equal to <see cref="Count"/>.
            /// </exception>
            /// <exception cref="ArgumentNullException">
            /// In a set operation, the specified value is <see langword="null"/>.
            /// </exception>
            public AnimatedCursorFrame this[int index]
            {
                get { return _items[index]; }
                set
                {
                    if (value == null) throw new ArgumentNullException(nameof(value));
                    _items[index] = value;
                }
            }

            object IList.this[int index]
            {
                get { return _items[index]; }
                set
                {
                    if (value == null) throw new ArgumentNullException(nameof(value));
                    AnimatedCursorFrame frame = value as AnimatedCursorFrame;
                    if (frame == null) throw new ArgumentException("Invalid object type.", nameof(value));
                    _items[index] = frame;
                }
            }

            /// <summary>
            /// Gets the number of elements contained in the list.
            /// </summary>
            public int Count { get { return _items.Count; } }

            /// <summary>
            /// Adds the specified <see cref="AnimatedCursorFrame"/> to the list at the specified index.
            /// </summary>
            /// <param name="index">The index at which the frame will be inserted.</param>
            /// <param name="frame">The <see cref="AnimatedCursorFrame"/> to add.</param>
            /// <returns><see langword="true"/> if <paramref name="frame"/> was successfully added; <see langword="false"/> if <paramref name="frame"/> is <see langword="null"/>,
            /// already exists in the list, or is already associated with a different <see cref="AnimatedCursorFile"/>.</returns>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than 0 or is greater than <see cref="Count"/>.
            /// </exception>
            public bool Insert(int index, AnimatedCursorFrame frame)
            {
                if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
                if (frame == null || frame.CFile != null)
                    return false;
                frame.CFile = _file;
                _items.Insert(index, frame);
                return true;
            }

            void IList<AnimatedCursorFrame>.Insert(int index, AnimatedCursorFrame item)
            {
                Insert(index, item);
            }

            void IList.Insert(int index, object value)
            {
                if (value is AnimatedCursorFrame)
                {
                    if (!Insert(index, (AnimatedCursorFrame)value))
                        throw new NotSupportedException("Invalid value.");
                }
                throw new ArgumentException("Invalid value type.", nameof(value));
            }

            /// <summary>
            /// Adds the specified <see cref="AnimatedCursorFrame"/> to the list.
            /// </summary>
            /// <param name="frame">The <see cref="AnimatedCursorFrame"/> to add.</param>
            /// <returns><see langword="true"/> if <paramref name="frame"/> was successfully added; <see langword="false"/> if <paramref name="frame"/> is <see langword="null"/>,
            /// already exists in the list, or is already associated with a different <see cref="AnimatedCursorFile"/>.</returns>
            public bool Add(AnimatedCursorFrame frame)
            {
                return Insert(_items.Count, frame);
            }

            void ICollection<AnimatedCursorFrame>.Add(AnimatedCursorFrame item)
            {
                Add(item);
            }

            int IList.Add(object value)
            {
                int count = _items.Count;
                ((IList)this).Insert(count, value);
                return count;
            }

            /// <summary>
            /// Removes the element at the specified index.
            /// </summary>
            /// <param name="index">The index of the element to remove.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than 0 or is greater than or equal to <see cref="Count"/>.
            /// </exception>
            public void RemoveAt(int index)
            {
                var frame = _items[index];
                frame.CFile = null;
                _items.RemoveAt(index);
            }

            /// <summary>
            /// Removes the specified frame from the list.
            /// </summary>
            /// <param name="frame">The frame to remove.</param>
            /// <returns><see langword="true"/> if <paramref name="frame"/> was found and successfully removed; <see langword="false"/> otherwise.</returns>
            public bool Remove(AnimatedCursorFrame frame)
            {
                int dex = IndexOf(frame);

                if (dex < 0) return false;

                RemoveAt(dex);

                return true;
            }

            void IList.Remove(object value)
            {
                Remove(value as AnimatedCursorFrame);
            }

            /// <summary>
            /// Returns the index of the specified frame.
            /// </summary>
            /// <param name="frame">The frame to search for in the list.</param>
            /// <returns>The index of <paramref name="frame"/>, if found; otherwise, -1.</returns>
            public int IndexOf(AnimatedCursorFrame frame)
            {
                if (frame == null || frame.CFile != _file) return -1;
                return _items.IndexOf(frame);
            }

            int IList.IndexOf(object value)
            {
                return IndexOf(value as AnimatedCursorFrame);
            }

            /// <summary>
            /// Determines whether the specified frame exists in the list.
            /// </summary>
            /// <param name="frame">The frame to search for in the list.</param>
            /// <returns><see langword="true"/> if <paramref name="frame"/> was found; <see langword="false"/> otherwise.</returns>
            public bool Contains(AnimatedCursorFrame frame)
            {
                return frame != null && frame.CFile == _file;
            }

            bool IList.Contains(object value)
            {
                return Contains(value as AnimatedCursorFrame);
            }

            /// <summary>
            /// Removes all elements from the list.
            /// </summary>
            public void Clear()
            {
                foreach (AnimatedCursorFrame frame in _items)
                    frame.CFile = null;
                _items.Clear();
            }

            /// <summary>
            /// Copies all elements in the list to the specified array, starting at the specified index.
            /// </summary>
            /// <param name="array">The array to which all elements in the list will be copied.</param>
            /// <param name="index">The index in <paramref name="array"/> at which copying begins.</param>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="array"/> is <see langword="null"/>.
            /// </exception>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than 0.
            /// </exception>
            /// <exception cref="ArgumentException">
            /// The length of <paramref name="array"/> minus <paramref name="index"/> is less than <see cref="Count"/>.
            /// </exception>
            public void CopyTo(AnimatedCursorFrame[] array, int index)
            {
                _items.CopyTo(array, index);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                ((ICollection)_items).CopyTo(array, index);
            }

            /// <summary>
            /// Returns an array containing all elements in the current list.
            /// </summary>
            /// <returns>An array containing elements copied from the current list.</returns>
            public AnimatedCursorFrame[] ToArray()
            {
                AnimatedCursorFrame[] items = new AnimatedCursorFrame[_items.Count];
                _items.CopyTo(items, 0);
                return items;
            }

            /// <summary>
            /// Moves an element from one index to another.
            /// </summary>
            /// <param name="oldIndex">The index of the element to move.</param>
            /// <param name="newIndex">The destination index.</param>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="oldIndex"/> or <paramref name="newIndex"/> are less than 0 or are greater than or equal to <see cref="Count"/>.
            /// </exception>
            public void Move(int oldIndex, int newIndex)
            {
                _items.Move(oldIndex, newIndex);
            }

#if DRAWING
            internal void Dispose()
            {
                var items = ToArray();
                Clear();

                foreach (AnimatedCursorFrame frame in items)
                    frame.File.Dispose();

                CollectionChanged = null;
                PropertyChanged = null;
            }
#endif

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

            IEnumerator<AnimatedCursorFrame> IEnumerable<AnimatedCursorFrame>.GetEnumerator()
            {
                return GetEnumerator();
            }

            bool ICollection<AnimatedCursorFrame>.IsReadOnly
            {
                get { return false; }
            }

            bool IList.IsReadOnly
            {
                get { return false; }
            }

            bool IList.IsFixedSize
            {
                get { return false; }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)_items).SyncRoot; }
            }

            /// <summary>
            /// An enumerator which iterates through the list.
            /// </summary>
            public struct Enumerator : IEnumerator<AnimatedCursorFrame>
            {
                private AnimatedCursorFrame _current;
                private IEnumerator<AnimatedCursorFrame> _enum;

                internal Enumerator(EntryList set)
                {
                    _current = null;
                    _enum = set._items.GetEnumerator();
                }

                /// <summary>
                /// Gets the element at the current position in the enumerator.
                /// </summary>
                public AnimatedCursorFrame Current
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
                /// <returns><see langword="true"/> if the enumerator successfully advanced; <see langword="false"/> if the enumerator has passed the end of the list.</returns>
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
                public AnimatedCursorFrame[] Items
                {
                    get { return _list.ToArray(); }
                }
            }
        }
    }

    /// <summary>
    /// Represents rate information for a single frame of an animated cursor.
    /// </summary>
    public sealed class AnimatedCursorFrame :
#if DRAWING
        INotifyPropertyChanged
#else
        DependencyObject
#endif
    {
        /// <summary>
        /// Creates a new instance with the specified cursor file.
        /// </summary>
        /// <param name="file">The cursor file associated with the current instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="file"/> is <see langword="null"/>.
        /// </exception>
        public AnimatedCursorFrame(CursorFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            _file = file;
#if DRAWING
            _file.Disposed += _file_Disposed;
#endif
        }

        internal AnimatedCursorFile CFile;

        /// <summary>
        /// Creates a new instance with the specified cursor file and delay.
        /// </summary>
        /// <param name="file">The cursor file associated with the current instance.</param>
        /// <param name="jiffies">The delay before displaying the next frame in the animated cursor, in "jiffies" (1/60 of a second).</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="file"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="jiffies"/> is less than 0.
        /// </exception>
        public AnimatedCursorFrame(CursorFile file, int jiffies)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (jiffies < 0) throw new ArgumentOutOfRangeException(nameof(jiffies));
            _file = file;
#if DRAWING
            _file.Disposed += _file_Disposed;
            _jiffies = jiffies;
#else
            SetValue(LengthJiffiesProperty, jiffies);
#endif
        }

        /// <summary>
        /// Creates a new instance with the specified cursor file and delay.
        /// </summary>
        /// <param name="file">The cursor file associated with the current instance.</param>
        /// <param name="length">The delay before displaying the next frame in the animated cursor.
        /// Fitted to the nearest "jiffy" (1/60 of a second).</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="file"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="length"/> is less than <see cref="TimeSpan.Zero"/>, or represents a number of "jiffies" (1/60 of a second)
        /// greater than <see cref="int.MaxValue"/>.
        /// </exception>
        public AnimatedCursorFrame(CursorFile file, TimeSpan length)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            int jiffies = AnimatedCursorFile.TimeToJiffies(length, nameof(length));
            _file = file;
#if DRAWING
            _jiffies = jiffies;
            _file.Disposed += _file_Disposed;
#else
            SetValue(LengthJiffiesProperty, jiffies);
#endif
        }

#if DRAWING
        private void _file_Disposed(object sender, EventArgs e)
        {
            if (CFile != null)
                CFile.Entries.Remove(this);
        }

        /// <summary>
        /// Raised when a property on the current instance changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
#endif

        /// <summary>
        /// Determines if the elements in <see cref="File"/> in the current instance are all similar to that of specified other
        /// <see cref="AnimatedCursorFrame"/>.
        /// </summary>
        /// <param name="other">The other <see cref="AnimatedCursorFrame"/> to compare.</param>
        /// <returns><see langword="true"/> if the current instance contains the same number of elements with the same <see cref="IconEntry.Width"/>,
        /// <see cref="IconEntry.Height"/>, and <see cref="IconEntry.BitDepth"/> values as the specified other <see cref="AnimatedCursorFrame"/>;
        /// <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        public bool SimilarListEquals(AnimatedCursorFrame other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            if (_file.Entries.Count != other._file.Entries.Count)
                return false;

            var entries1 = _file.Entries.OrderBy(i => i.EntryKey);
            var entries2 = other._file.Entries.OrderBy(i => i.EntryKey);

            using (IEnumerator<IconEntry> enum1 = entries1.GetEnumerator(), enum2 = entries2.GetEnumerator())
            {
                while (enum1.MoveNext() && enum2.MoveNext())
                {
                    if (enum1.Current.EntryKey != enum2.Current.EntryKey)
                        return false;
                }
            }

            return true;
        }

        #region File
        private readonly CursorFile _file;
        /// <summary>
        /// Gets the cursor file associated with the current instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// In a set operation, the specified value is <see langword="null"/>.
        /// </exception>
#if !DRAWING
        [Bindable(true)]
#endif
        public CursorFile File
        {
            get { return _file; }
        }
        #endregion

        #region LengthJiffies
#if DRAWING
        private int? _jiffies;
#else
        /// <summary>
        /// Dependency property for the <see cref="LengthJiffies"/> property.
        /// </summary>
        public static readonly DependencyProperty LengthJiffiesProperty = DependencyProperty.Register(nameof(LengthJiffies), typeof(int?), typeof(AnimatedCursorFrame),
            new PropertyMetadata(default(int?), LengthJiffiesChanged, LengthJiffiesCoerce));

        private static void LengthJiffiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int? newVal = (int)e.NewValue;
            d.SetValue(LengthProperty, newVal.HasValue ? AnimatedCursorFile.JiffiesToTime(newVal.Value) : default(TimeSpan?));
        }

        private static object LengthJiffiesCoerce(DependencyObject d, object baseValue)
        {
            int? value = (int?)baseValue;

            if (!value.HasValue) return value;

            value = (int)AnimatedCursorFile.JiffiesCoerce(d, value.Value);

            return value;
        }
#endif
        /// <summary>
        /// Gets and sets the delay before displaying the next frame in the animated cursor, in "jiffies" (1/60 of a second),
        /// or <see langword="null"/> to use the animated cursor file's <see cref="AnimatedCursorFile.DisplayRateJiffies"/> value.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// In a set operation, the specified value is not <see langword="null"/> and is less than 0.
        /// </exception>
        public int? LengthJiffies
        {
#if DRAWING
            get { return _jiffies; }
#else
            get { return (int?)GetValue(LengthJiffiesProperty); }
#endif
            set
            {
                if (value.HasValue && value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value));
#if DRAWING
                _jiffies = value;
                OnPropertyChanged(nameof(LengthJiffies));
                OnPropertyChanged(nameof(LengthTime));
#else
                SetValue(LengthJiffiesProperty, value);
#endif
            }
        }
        #endregion

        #region LengthTime
#if !DRAWING
        /// <summary>
        /// Dependency property for the <see cref="LengthTime"/> property.
        /// </summary>
        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(nameof(LengthTime), typeof(TimeSpan?), typeof(AnimatedCursorFrame),
            new PropertyMetadata(default(TimeSpan?), LengthTimeChanged, LengthTimeCoerce));

        private static void LengthTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeSpan? newVal = (TimeSpan?)e.NewValue;

            d.SetValue(LengthJiffiesProperty, newVal.HasValue ? AnimatedCursorFile.TimeToJiffies(newVal.Value) : default(int?));
        }

        private static object LengthTimeCoerce(DependencyObject d, object baseValue)
        {
            TimeSpan? value = (TimeSpan?)baseValue;

            if (!value.HasValue) return value;

            value = (TimeSpan)AnimatedCursorFile.TimeCoerce(d, value.Value);

            return value;
        }
#endif
        /// <summary>
        /// Gets and sets the delay before displaying the next frame in the animated cursor, or <see langword="null"/> to use the
        /// animated cursor file's <see cref="AnimatedCursorFile.DisplayRateTime"/> property.
        /// Fitted to the nearest "jiffy" (1/60 of a second).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// In a set operation, the specified value is not <see langword="null"/>, and is less than <see cref="TimeSpan.Zero"/>
        /// or represents a number of <see cref="LengthJiffies"/> greater than <see cref="int.MaxValue"/>.
        /// </exception>
        public TimeSpan? LengthTime
        {
#if DRAWING
            get
            {
                if (_jiffies.HasValue)
                    return new TimeSpan(TimeSpan.TicksPerSecond * _jiffies.Value / 60);
                return null;
            }
            set { LengthJiffies = value.HasValue ? AnimatedCursorFile.TimeToJiffies(value.Value, nameof(value)) : default(int?); }
#else
            get { return (TimeSpan?)GetValue(LengthJiffiesProperty); }
            set
            {
                if (value.HasValue)
                    AnimatedCursorFile.TimeToJiffies(value.Value, nameof(value));
                SetValue(LengthJiffiesProperty, value);
            }
#endif
        }
        #endregion

        /// <summary>
        /// Returns a string representation of the current instance.
        /// </summary>
        /// <returns>A string representation of the current instance.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Length: ");
#if !DRAWING
            int? _jiffies = LengthJiffies;
#endif
            if (_jiffies.HasValue)
                sb.Append(string.Format("{0} jiffies ({1})", _jiffies.Value, LengthTime.Value));
            else
            {
                sb.Append("default");
                if (CFile != null)
                    sb.Append(string.Format(" {0} jiffies ({1})", CFile.DisplayRateJiffies, CFile.DisplayRateTime));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets an equivalent of an animated cursor frame with a single entry.
    /// </summary>
    public class AnimatedCursorSingleSizeFrame
    {
        internal AnimatedCursorSingleSizeFrame(IconEntry entry, int jiffies)
        {
            _entry = entry;
            _jiffies = jiffies;
        }

        private IconEntry _entry;
        /// <summary>
        /// Gets the <see cref="IconEntry"/> for the current value.
        /// </summary>
        public IconEntry Entry { get { return _entry; } }

        private int _jiffies;
        /// <summary>
        /// Gets the delay before displaying the next frame, in "jiffies" (1/60 of a second).
        /// </summary>
        public int LengthJiffies { get { return _jiffies; } }

        /// <summary>
        /// Gets the delay before displaying the next frame.
        /// </summary>
        public TimeSpan LengthTime { get { return new TimeSpan(TimeSpan.TicksPerSecond * _jiffies / 60); } }

        /// <summary>
        /// Returns a string representation of the current instance.
        /// </summary>
        /// <returns>A string representation of the current instance.</returns>
        public override string ToString()
        {
            return string.Concat("Length: ", _jiffies, " jiffies (", LengthTime, ")");
        }
    }
}