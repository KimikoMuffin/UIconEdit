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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace UIconEdit
{
    /// <summary>
    /// Represents a single frame in an icon.
    /// </summary>
    public class IconFrame
    {
        /// <summary>
        /// Creates a new instance with the specified image.
        /// </summary>
        /// <param name="baseImage">The image associated with the current instance.</param>
        /// <param name="bitDepth">Indicates the bit depth of the resulting image.</param>
        /// <param name="alphaThreshold">If the alpha value of a given pixel is below this value, that pixel will be fully transparent.
        /// If the alpha value is greater than or equal to this value, the pixel will be fully opaque.</param>
        /// <param name="width">The width of the new image.</param>
        /// <param name="height">The height of the new image.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="baseImage"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="bitDepth"/> is not a valid <see cref="UIconEdit.BitDepth"/> value.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="width"/> or <paramref name="height"/> is less than <see cref="MinDimension"/> or is greater than <see cref="MaxDimension"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// <paramref name="baseImage"/> is disposed.
        /// </exception>
        public IconFrame(Image baseImage, BitDepth bitDepth, short width, short height, byte alphaThreshold)
        {
            _setImage(baseImage, "image");
            _setDepth(bitDepth, "bitDepth");
            _setSize(ref _width, width, "width");
            _setSize(ref _height, height, "height");
            _alphaThreshold = alphaThreshold;
        }

        /// <summary>
        /// The minimum dimensions of an icon. 16 as of Windows 10.
        /// </summary>
        public const int MinDimension = 16;
        /// <summary>
        /// The maximum dimensions of an icon. 768 as of Windows 10.
        /// </summary>
        public const int MaxDimension = 768;

        private void _setImage(Image image, string paramName)
        {
            if (image == null) throw new ArgumentNullException(paramName);
            int width, height;
            try
            {
                width = image.Width;
                height = image.Height;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch
            {
                throw new ObjectDisposedException(paramName);
            }
            _image = image;
        }

        private Image _image;
        /// <summary>
        /// Gets and sets the image associated with the current instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// In a set operation, the specified value is <c>null</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// In a set operation, the specified value is disposed.
        /// </exception>
        public Image BaseImage
        {
            get { return _image; }
            set { _setImage(value, null); }
        }

        private void _setSize(ref short dim, short value, string paramName)
        {
            if (value < MinDimension || value > MaxDimension)
                throw new ArgumentOutOfRangeException(paramName, value, "The specified value is out of bounds.");
            dim = value;
        }

        private short _width;
        /// <summary>
        /// Gets and sets the resampled width of the icon.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// In a set operation, the specified value is less than <see cref="MinDimension"/> or is greater than <see cref="MaxDimension"/>.
        /// </exception>
        public short Width
        {
            get { return _width; }
            set { _setSize(ref _width, value, null); }
        }

        private short _height;
        /// <summary>
        /// Gets and sets the resampled height of the icon.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// In a set operation, the specified value is less than <see cref="MinDimension"/> or is greater than <see cref="MaxDimension"/>.
        /// </exception>
        public short Height
        {
            get { return _height; }
            set { _setSize(ref _height, value, null); }
        }

        private void _setDepth(BitDepth depth, string paramName)
        {
            switch (depth)
            {
                case BitDepth.Bit24:
                case BitDepth.Bit32:
                case BitDepth.Color2:
                case BitDepth.Color16:
                case BitDepth.Color256:
                    break;
                default:
                    throw new InvalidEnumArgumentException(null, (int)depth, typeof(BitDepth));
            }
            _depth = depth;
        }

        private BitDepth _depth;
        /// <summary>
        /// Gets and sets the bit depth of the current instance. This property is ignored if the width or height of the image is greater than 255.
        /// </summary>
        /// <exception cref="InvalidEnumArgumentException">
        /// In a set operation, the specified value is not a valid <see cref="UIconEdit.BitDepth"/> value.
        /// </exception>
        public BitDepth BitDepth
        {
            get { return _depth; }
            set { _setDepth(value, null); }
        }

        /// <summary>
        /// Gets the pixel format of the resulting image file.
        /// </summary>
        public PixelFormat PixelFormat
        {
            get
            {
                switch (_depth)
                {
                    case BitDepth.Color2:
                        return PixelFormat.Format1bppIndexed;
                    case BitDepth.Color16:
                        return PixelFormat.Format4bppIndexed;
                    case BitDepth.Color256:
                        return PixelFormat.Format8bppIndexed;
                    //case BitDepth.Bit24:
                    //    return PixelFormat.Format24bppRgb;
                    default:
                        return PixelFormat.Format32bppArgb;
                }
            }
        }

        internal short BitsPerPixel
        {
            get
            {
                switch (_depth)
                {
                    default:
                        return 32;
                    case BitDepth.Bit24:
                        return 24;
                    case BitDepth.Color2:
                        return 1;
                    case BitDepth.Color16:
                        return 4;
                    case BitDepth.Color256:
                        return 8;
                }
            }
        }

        private byte _alphaThreshold;
        /// <summary>
        /// Gets and sets a value indicating the threshold of alpha values when <see cref="BitDepth"/> is any value other than
        /// <see cref="UIconEdit.BitDepth.Bit32"/>. Alpha values less than this value will be fully transparent; alpha values greater than or equal to this
        /// value will be fully opaque.
        /// </summary>
        public byte AlphaThreshold
        {
            get { return _alphaThreshold; }
            set { _alphaThreshold = value; }
        }

        internal Bitmap GetQuantized(out Bitmap alphaMask, out int paletteCount)
        {
            const PixelFormat fullFormat = PixelFormat.Format32bppArgb, alphaFormat = PixelFormat.Format1bppIndexed;

            if (_image is Bitmap && _image.PixelFormat == PixelFormat.Format32bppArgb && _depth == BitDepth.Bit32 && _image.Width == _width && _image.Height == _height)
            {
                alphaMask = null;
                paletteCount = 0;
                return (Bitmap)_image;
            }

            Bitmap fullColor = new Bitmap(_width, _height, fullFormat);

            using (Graphics g = Graphics.FromImage(fullColor))
                g.DrawImage(_image, 0, 0, _width, _height);

            if (_depth == BitDepth.Bit32)
            {
                alphaMask = null;
                paletteCount = 0;
                return fullColor;
            }

            Rectangle fullRect = new Rectangle(0, 0, _width, _height);

            alphaMask = new Bitmap(_width, _height, alphaFormat);

            unsafe
            {
                BitmapData fullData = fullColor.LockBits(fullRect, ImageLockMode.ReadOnly, fullFormat);
                BitmapData alphaData = alphaMask.LockBits(fullRect, ImageLockMode.WriteOnly, alphaFormat);
                int offWidth = fullRect.Width / 8;

                for (int y = 0; y < _height; y++)
                {
                    List<byte> byteList = new List<byte>(Pack2(BelowThreshold(fullData.Stride, _alphaThreshold, fullData.Scan0 + (y * fullData.Stride))));

                    byte[] buffer = byteList.ToArray();

                    Marshal.Copy(buffer, 0, alphaData.Scan0 + (y * alphaData.Stride), buffer.Length);
                }
            }

            unsafe
            {
                BitmapData fullData = fullColor.LockBits(fullRect, ImageLockMode.ReadWrite, fullFormat);

                for (int y = 0; y < _height; y++)
                {
                    int* pRow = (int*)(fullData.Scan0 + (y * fullData.Stride));

                    const int upperMask = -16777216; //0xFF000000 - opaque alpha

                    for (int x = 0; x < _width; x++)
                        pRow[x] |= upperMask;
                }
                fullColor.UnlockBits(fullData);
            }

            if (_depth == BitDepth.Bit24)
            {
                paletteCount = 0;
                return fullColor;
            }

            Bitmap quantized = new Bitmap(_width, _height, PixelFormat);
            switch (_depth)
            {
                case BitDepth.Color2:
                    paletteCount = 2;
                    break;
                case BitDepth.Color16:
                    paletteCount = 16;
                    break;
                default:
                    paletteCount = 256;
                    break;
            }
            //TODO: Quantize
            fullColor.Dispose();
            return quantized;
        }

        internal static byte[] Pack16(byte[] indices)
        {
            int returnLength = indices.Length / 2 + (indices.Length % 2);

            byte[] returner = new byte[returnLength];

            for (int i = 0; i < returner.Length; i++)
            {
                int dex2 = i * 2;
                byte result = (byte)(indices[dex2] << 4);

                dex2++;
                if (dex2 < indices.Length)
                    result |= indices[dex2];

                returner[i] = result;
            }
            return returner;
        }

        internal static IEnumerable<byte> Pack2(IEnumerable<bool> indices)
        {
            int offset = 7;
            byte curVal = 0;
            foreach (bool value in indices)
            {
                if (value) curVal |= (byte)(1 << offset);
                offset = (offset - 1) & 7;
                if (offset == 7)
                {
                    yield return curVal;
                    curVal = 0;
                }
            }
            if (offset != 7)
                yield return curVal;
        }

        internal static byte[] Pack2(byte[] indices)
        {
            int returnLength = indices.Length / 8;
            if (indices.Length % 8 > 0) returnLength++;
            byte[] returner = new byte[returnLength];

            int curCount = 0;
            foreach (byte b in Pack2(indices.Select(i => (i & 1) == 1)))
                returner[curCount++] = b;

            return returner;
        }

        internal static unsafe bool[] BelowThreshold(int stride, byte alphaThreshold, IntPtr scan)
        {
            bool[] returner = new bool[stride / 4];
            uint* pColor = (uint*)scan;

            for (int i = 0; i < returner.Length; i++)
            {
                uint value = pColor[i * 4] >> 24;
                returner[i] = value >= alphaThreshold;
            }
            return returner;
        }
    }

    internal struct IconFrameComparer : IEqualityComparer<IconFrame>, IComparer<IconFrame>
    {
        public int Compare(IconFrame x, IconFrame y)
        {
            int compare = x.BitDepth.CompareTo(y.BitDepth);
            if (compare != 0) return compare;

            compare = x.Width.CompareTo(y.Width);
            if (compare != 0) return compare;

            return x.Height.CompareTo(y.Height);
        }

        public bool Equals(IconFrame x, IconFrame y)
        {
            if (ReferenceEquals(x, y)) return true;
            return x.BitDepth == y.BitDepth && x.Width == y.Width && x.Height == y.Height;
        }

        public int GetHashCode(IconFrame obj)
        {
            if (obj == null) return 0;
            return (int)obj.Width | (obj.Height << 16) | ((int)obj.BitDepth << 12);
        }
    }

    /// <summary>
    /// Indicates the bit depth of an icon frame.
    /// </summary>
    public enum BitDepth
    {
        /// <summary>
        /// Indicates that the frame is full color with alpha (32 bits per pixel).
        /// </summary>
        Bit32,
        /// <summary>
        /// Indicates that the frame is full color without alpha (24 bits per pixel plus 1 bit per pixel alpha mask).
        /// </summary>
        Bit24,
        /// <summary>
        /// Indicates that the frame is 256-color (8 bits per pixel plus 1 bit per pixel alpha mask)
        /// </summary>
        Color256,
        /// <summary>
        /// Indicates that the frame is 16-color (4 bits per pixel plus 1 bit per pixel alpha mask).
        /// </summary>
        Color16,
        /// <summary>
        /// Indicates that the frame is 2-color (1 bit per pixel plus 1 bit per pixel alpha mask).
        /// </summary>
        Color2,
    }
}
