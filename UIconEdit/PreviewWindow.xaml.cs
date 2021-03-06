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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace UIconEdit.Maker
{
    /// <summary>
    /// Interaction logic for PreviewWindow.xaml
    /// </summary>
    partial class PreviewWindow
    {
        #region Constructors
        private PreviewWindow(AddWindow owner, bool quantize)
        {
            Owner = owner;
            AlphaThreshold = owner.AlphaThreshold;
            SetValue(SourceEntryPropertyKey, owner.GetIconEntry(quantize));
            InitializeComponent();
        }

        public PreviewWindow(AddWindow owner)
            : this(owner, true)
        {
        }

        public PreviewWindow(AddWindow owner, BitmapSource alphaImage)
            : this(owner, false)
        {
            SetValue(SettingAlphaPropertyKey, true);
            SourceEntry.AlphaImage = alphaImage;
        }
        #endregion

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            txtAlpha.GetBindingExpression(TextBox.TextProperty).UpdateSource();

            MainWindow.SelectZoom(this, scrollImage, SourceImage, ZoomProperty, ZoomedWidthPropertyKey, ZoomedHeightPropertyKey, ZoomScaleModePropertyKey);
            Mouse.OverrideCursor = null;
        }

        #region SourceImage
        public static readonly DependencyProperty SourceImageProperty = DependencyProperty.Register(nameof(SourceImage), typeof(BitmapSource), typeof(PreviewWindow));

        public BitmapSource SourceImage
        {
            get { return (BitmapSource)GetValue(SourceImageProperty); }
            set { SetValue(SourceImageProperty, value); }
        }
        #endregion

        private void _zoomSet()
        {
            MainWindow.ZoomSet(this, SourceImage, ZoomProperty, ZoomedWidthPropertyKey, ZoomedHeightPropertyKey, ZoomScaleModePropertyKey);
        }

        #region Zoom
        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(int), typeof(PreviewWindow),
            new PropertyMetadata(100, ZoomChanged), MainWindow.ZoomValidate);

        private static void ZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PreviewWindow m = (PreviewWindow)d;
            m._zoomSet();
        }

        public int Zoom
        {
            get { return (int)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }
        #endregion

        #region ZoomedWidth
        private static readonly DependencyPropertyKey ZoomedWidthPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ZoomedWidth), typeof(double),
            typeof(PreviewWindow), new PropertyMetadata());
        public static readonly DependencyProperty ZoomedWidthProperty = ZoomedWidthPropertyKey.DependencyProperty;

        public double ZoomedWidth { get { return (double)GetValue(ZoomedWidthProperty); } }
        #endregion

        #region ZoomedHeight
        private static readonly DependencyPropertyKey ZoomedHeightPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ZoomedHeight), typeof(double),
            typeof(PreviewWindow), new PropertyMetadata());
        public static readonly DependencyProperty ZoomedHeightProperty = ZoomedHeightPropertyKey.DependencyProperty;

        public double ZoomedHeight { get { return (double)GetValue(ZoomedHeightProperty); } }
        #endregion

        #region ZoomScaleMode
        private static readonly DependencyPropertyKey ZoomScaleModePropertyKey = DependencyProperty.RegisterReadOnly(nameof(ZoomScaleMode),
            typeof(BitmapScalingMode), typeof(PreviewWindow), new PropertyMetadata());
        public static readonly DependencyProperty ZoomScaleModeProperty = ZoomScaleModePropertyKey.DependencyProperty;

        public BitmapScalingMode ZoomScaleMode { get { return (BitmapScalingMode)GetValue(ZoomScaleModeProperty); } }
        #endregion

        #region SourceEntry
        private static readonly DependencyPropertyKey SourceEntryPropertyKey = DependencyProperty.RegisterReadOnly(nameof(SourceEntry), typeof(IconEntry),
            typeof(PreviewWindow), new PropertyMetadata(null, SourceEntryChanged));
        public static readonly DependencyProperty SourceEntryProperty = SourceEntryPropertyKey.DependencyProperty;

        private static void SourceEntryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var entry = (IconEntry)e.NewValue;
            d.SetValue(HasMultiImagePropertyKey, entry != null && entry.AlphaImage != null);
            ((PreviewWindow)d)._setChanged();
        }

        public IconEntry SourceEntry { get { return (IconEntry)GetValue(SourceEntryProperty); } }
        #endregion

        #region SourceIndex
        public static readonly DependencyProperty SourceIndexProperty = DependencyProperty.Register(nameof(SourceIndex), typeof(int), typeof(PreviewWindow),
            new PropertyMetadata(0));

        public int SourceIndex
        {
            get { return (int)GetValue(SourceIndexProperty); }
            set { SetValue(SourceIndexProperty, value); }
        }
        #endregion

        #region HasMultiImage
        private static readonly DependencyPropertyKey HasMultiImagePropertyKey = DependencyProperty.RegisterReadOnly(nameof(HasMultiImage), typeof(bool),
            typeof(PreviewWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty HasMultiImageProperty = HasMultiImagePropertyKey.DependencyProperty;

        public bool HasMultiImage { get { return (bool)GetValue(HasMultiImageProperty); } }
        #endregion

        #region AlphaThreshold
        public static readonly DependencyProperty AlphaThresholdProperty = DependencyProperty.Register(nameof(AlphaThreshold), typeof(byte), typeof(PreviewWindow),
            new PropertyMetadata(IconEntry.DefaultAlphaThreshold, AlphaThresholdChanged));

        private static void AlphaThresholdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PreviewWindow p = (PreviewWindow)d;
            var owner = (AddWindow)p.Owner;
            Mouse.OverrideCursor = Cursors.Wait;
            owner.AlphaThreshold = p.AlphaThreshold;
            owner.AlphaThresholdMode = p.AlphaThresholdMode;
            p.SetValue(SourceEntryPropertyKey, owner.GetIconEntry(!p.SettingAlpha));
            Mouse.OverrideCursor = null;
        }

        public byte AlphaThreshold
        {
            get { return (byte)GetValue(AlphaThresholdProperty); }
            set { SetValue(AlphaThresholdProperty, value); }
        }
        #endregion

        #region AlphaThresholdMode
        public static readonly DependencyProperty AlphaThresholdModePoperty = DependencyProperty.Register(nameof(AlphaThresholdMode), typeof(IconAlphaThresholdMode),
            typeof(PreviewWindow), new PropertyMetadata(IconAlphaThresholdMode.Darken, AlphaThresholdChanged));

        public IconAlphaThresholdMode AlphaThresholdMode
        {
            get { return (IconAlphaThresholdMode)GetValue(AlphaThresholdModePoperty); }
            set { SetValue(AlphaThresholdModePoperty, value); }
        }
        #endregion

        #region SettingAlpha
        private static readonly DependencyPropertyKey SettingAlphaPropertyKey = DependencyProperty.RegisterReadOnly(nameof(SettingAlpha), typeof(bool),
            typeof(PreviewWindow), new PropertyMetadata(false));
        public static readonly DependencyProperty SettingAlphaProperty = SettingAlphaPropertyKey.DependencyProperty;

        public bool SettingAlpha { get { return (bool)GetValue(SettingAlphaProperty); } }
        #endregion

        private void _setChanged()
        {
            var entry = SourceEntry;
            if (entry == null) return;
            switch (SourceIndex)
            {
                case 1:
                    SourceImage = entry.BaseImage;
                    break;
                case 2:
                    BitmapSource alphaImage;
                    entry.GetQuantized(out alphaImage);
                    SourceImage = alphaImage;
                    break;
                case 3:
                    SourceImage = entry.AlphaImage;
                    break;
                default:
                    SourceImage = entry.GetCombinedAlpha();
                    break;
            }
        }

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            SetValue(SourceEntryPropertyKey, ((AddWindow)Owner).GetIconEntry(!SettingAlpha));
            Mouse.OverrideCursor = null;
        }

        private void cmbWhich_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _setChanged();
            Mouse.OverrideCursor = null;
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            txtAlpha.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void txtAlpha_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            txtAlpha.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SettingAlpha)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
