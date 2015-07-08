﻿#region BSD License
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

using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace UIconEdit.Maker
{
    /// <summary>
    /// Interaction logic for AddWindow.xaml
    /// </summary>
    partial class AddWindow
    {
        public AddWindow(MainWindow mainWindow, bool duplicated, bool newFile, BitmapSource image, BitDepth bitDepth)
        {
            Owner = _mainWindow = mainWindow;
            LoadedImage = image;

            const string prefix = "Owner.SettingsFile.LanguageFile.";
            Binding binding = new Binding(prefix + (duplicated ? "DuplicateTitle" : "AddTitle"));
            binding.ElementName = "window";
            binding.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(this, TitleProperty, binding);

            SetValue(NewFilePropertyKey, newFile);

            short width = 32, height = 32;
            if (image.PixelWidth >= IconEntry.MinDimension && image.PixelWidth <= IconEntry.MaxDimension)
                width = (short)image.PixelWidth;
            if (image.PixelHeight >= IconEntry.MinDimension && image.PixelHeight <= IconEntry.MaxDimension)
                height = (short)image.PixelHeight;

            EntryWidth = width;
            EntryHeight = height;

            InitializeComponent();

            bool initSize = false;

            if (width == height)
            {
                switch (width)
                {
                    case 16:
                        initSize = true;
                        sz16.IsChecked = true;
                        break;
                    case 24:
                        initSize = true;
                        sz24.IsChecked = true;
                        break;
                    case 32:
                        initSize = true;
                        sz32.IsChecked = true;
                        break;
                    case 48:
                        initSize = true;
                        sz48.IsChecked = true;
                        break;
                    case 256:
                        initSize = true;
                        sz256.IsChecked = true;
                        break;
                    case 768:
                        initSize = true;
                        sz768.IsChecked = true;
                        break;
                    case 20:
                        initSize = true;
                        sz20.IsChecked = chkExtended.IsChecked = true;
                        break;
                    case 40:
                        initSize = true;
                        sz40.IsChecked = chkExtended.IsChecked = true;
                        break;
                    case 64:
                        initSize = true;
                        sz64.IsChecked = chkExtended.IsChecked = true;
                        break;
                    case 80:
                        initSize = true;
                        sz80.IsChecked = chkExtended.IsChecked = true;
                        break;
                    case 96:
                        initSize = true;
                        sz96.IsChecked = chkExtended.IsChecked = true;
                        break;
                    case 128:
                        initSize = true;
                        sz128.IsChecked = chkExtended.IsChecked = true;
                        break;
                    case 512:
                        initSize = true;
                        sz512.IsChecked = chkExtended.IsChecked = true;
                        break;
                }
            }
            if (!initSize)
            {
                CustomWidth = width;
                CustomHeight = height;
                szCust.IsChecked = true;
            }

            switch (bitDepth)
            {
                default: //Depth32BitsPerPixel
                    rad32bit.IsChecked = true;
                    break;
                case BitDepth.Depth24BitsPerPixel:
                    rad24bit.IsChecked = true;
                    break;
                case BitDepth.Depth8BitsPerPixel:
                    rad8bit.IsChecked = true;
                    break;
                case BitDepth.Depth4BitsPerPixel:
                    rad4bit.IsChecked = true;
                    break;
                case BitDepth.Depth1BitPerPixel:
                    rad1bit.IsChecked = true;
                    break;
            }
        }

        private MainWindow _mainWindow;

        public IconEntry GetIconEntry()
        {
            var entry = new IconEntry(LoadedImage, EntryWidth, EntryHeight, BitDepth, AlphaThreshold);
            entry.SetQuantized();
            return entry;
        }

        #region NewFile
        private static readonly DependencyPropertyKey NewFilePropertyKey = DependencyProperty.RegisterReadOnly("NewFile", typeof(bool), typeof(AddWindow),
            new PropertyMetadata());
        public static readonly DependencyProperty NewFileProperty = NewFilePropertyKey.DependencyProperty;

        public bool NewFile { get { return (bool)GetValue(NewFileProperty); } }
        #endregion

        #region AlphaThreshold
        public static readonly DependencyProperty AlphaThresholdProperty = DependencyProperty.Register("AlphaThreshold", typeof(byte), typeof(AddWindow),
            new PropertyMetadata(IconEntry.DefaultAlphaThreshold));

        public byte AlphaThreshold
        {
            get { return (byte)GetValue(AlphaThresholdProperty); }
            set { SetValue(AlphaThresholdProperty, value); }
        }
        #endregion

        #region LoadedImage
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("LoadedImage", typeof(BitmapSource), typeof(AddWindow));

        public BitmapSource LoadedImage
        {
            get { return (BitmapSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }
        #endregion

        #region BitDepth
        private static readonly DependencyPropertyKey BitDepthPropertyKey = DependencyProperty.RegisterReadOnly("BitDepth", typeof(BitDepth), typeof(AddWindow),
            new PropertyMetadata(BitDepth.Depth32BitsPerPixel, BitDepthChanged));

        private static void BitDepthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AddWindow a = (AddWindow)d;

            switch ((BitDepth)e.NewValue)
            {
                default: //Depth32BitsPerPixel
                    a.rad32bit.IsChecked = true;
                    break;
                case BitDepth.Depth24BitsPerPixel:
                    a.rad24bit.IsChecked = true;
                    break;
                case BitDepth.Depth8BitsPerPixel:
                    a.rad8bit.IsChecked = true;
                    break;
                case BitDepth.Depth4BitsPerPixel:
                    a.rad4bit.IsChecked = true;
                    break;
                case BitDepth.Depth1BitsPerPixel:
                    a.rad1bit.IsChecked = true;
                    break;
            }
        }

        public static readonly DependencyProperty BitDepthProperty = BitDepthPropertyKey.DependencyProperty;

        public BitDepth BitDepth
        {
            get { return (BitDepth)GetValue(BitDepthProperty); }
            private set { SetValue(BitDepthPropertyKey, value); }
        }
        #endregion

        #region ExtendedSizes
        public static readonly DependencyProperty ExtendedSizesProperty = DependencyProperty.Register("ExtendedSizes", typeof(bool), typeof(AddWindow),
            new PropertyMetadata(false));

        public bool ExtendedSizes
        {
            get { return (bool)GetValue(ExtendedSizesProperty); }
            set { SetValue(ExtendedSizesProperty, value); }
        }
        #endregion

        private static bool SizeValidate(object value)
        {
            short val = (short)value;
            return val >= IconEntry.MinDimension && val <= IconEntry.MaxDimension;
        }

        #region EntryWidth
        public static readonly DependencyProperty EntryWidthProperty = DependencyProperty.Register("EntryWidth", typeof(short), typeof(AddWindow),
            new PropertyMetadata((short)32), SizeValidate);

        public short EntryWidth
        {
            get { return (short)GetValue(EntryWidthProperty); }
            set { SetValue(EntryWidthProperty, value); }
        }
        #endregion

        #region EntryHeight
        public static readonly DependencyProperty EntryHeightProperty = DependencyProperty.Register("EntryHeight", typeof(short), typeof(AddWindow),
            new PropertyMetadata((short)32), SizeValidate);

        public short EntryHeight
        {
            get { return (short)GetValue(EntryHeightProperty); }
            set { SetValue(EntryHeightProperty, value); }
        }
        #endregion

        private static void CustomSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AddWindow a = (AddWindow)d;
            if (a.chkExtended.IsChecked.HasValue && a.chkExtended.IsChecked.Value)
            {
                a.EntryWidth = a.CustomWidth;
                a.EntryHeight = a.CustomHeight;
            }
        }

        #region CustomWidth
        public static readonly DependencyProperty CustomWidthProperty = DependencyProperty.Register("CustomWidth", typeof(short), typeof(AddWindow),
            new PropertyMetadata((short)32), SizeValidate);

        public short CustomWidth
        {
            get { return (short)GetValue(CustomWidthProperty); }
            set { SetValue(CustomWidthProperty, value); }
        }
        #endregion

        #region CustomHeight
        public static readonly DependencyProperty CustomHeightProperty = DependencyProperty.Register("CustomHeight", typeof(short), typeof(AddWindow),
            new PropertyMetadata((short)32, CustomSizeChanged), SizeValidate);

        public short CustomHeight
        {
            get { return (short)GetValue(CustomHeightProperty); }
            set { SetValue(CustomHeightProperty, value); }
        }
        #endregion

        private void bit_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == rad24bit)
                BitDepth = BitDepth.Depth24BitsPerPixel;
            else if (sender == rad8bit)
                BitDepth = BitDepth.Depth8BitsPerPixel;
            else if (sender == rad4bit)
                BitDepth = BitDepth.Depth4BitsPerPixel;
            else if (sender == rad1bit)
                BitDepth = BitDepth.Depth1BitPerPixel;
            else
                BitDepth = BitDepth.Depth32BitsPerPixel;
        }

        private void size_Checked(object sender, RoutedEventArgs e)
        {
            SizeRadioButton rad = sender as SizeRadioButton;
            if (rad == null) return;
            EntryWidth = rad.EntryWidth;
            EntryHeight = rad.EntryHeight;
        }

        private void szCust_Checked(object sender, RoutedEventArgs e)
        {
            if (!szCust.IsChecked.HasValue || !szCust.IsChecked.Value)
                return;

            EntryWidth = CustomWidth;
            EntryHeight = CustomHeight;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            var file = _mainWindow.LoadedFile;
            if (!NewFile && file != null && file.Entries.ContainsSimilar(EntryWidth, EntryHeight, BitDepth))
            {
                ErrorWindow.Show(_mainWindow, this, string.Format(_mainWindow.SettingsFile.LanguageFile.ImageAddError,
                    IconEntry.GetBitsPerPixel(BitDepth), EntryWidth, EntryHeight));
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}