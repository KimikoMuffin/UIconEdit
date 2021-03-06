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

using System;
using System.Windows;
using System.Windows.Media;

namespace UIconEdit.Maker
{
    /// <summary>
    /// Interaction logic for ListedIconPanel.xaml
    /// </summary>
    partial class ListedIconPanel
    {
        public ListedIconPanel()
        {
            InitializeComponent();
        }

        #region MainWindow
        public static DependencyProperty MainWindowProperty = DependencyProperty.Register(nameof(MainWindow), typeof(MainWindow), typeof(ListedIconPanel));

        public MainWindow MainWindow
        {
            get { return (MainWindow)GetValue(MainWindowProperty); }
            set { SetValue(MainWindowProperty, value); }
        }
        #endregion

        #region Entry
        public static DependencyProperty EntryProperty = DependencyProperty.Register(nameof(Entry), typeof(IconEntry), typeof(ListedIconPanel),
            new PropertyMetadata(null, EntryChanged));

        private static void EntryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IconEntry entry = (IconEntry)e.NewValue;
            if (entry == null) return;

            PresentationSource source = PresentationSource.FromVisual((ListedIconPanel)d);
            double width = source.CompositionTarget.TransformToDevice.M11 * 64;

            d.SetValue(ScalingModePropertyKey, entry.Width > width ? BitmapScalingMode.HighQuality : BitmapScalingMode.NearestNeighbor);
        }

        public IconEntry Entry
        {
            get { return (IconEntry)GetValue(EntryProperty); }
            set { SetValue(EntryProperty, value); }
        }
        #endregion

        #region ScalingMode
        private static readonly DependencyPropertyKey ScalingModePropertyKey = DependencyProperty.RegisterReadOnly(nameof(ScalingMode), typeof(BitmapScalingMode),
            typeof(ListedIconPanel), new PropertyMetadata(BitmapScalingMode.NearestNeighbor));
        public static readonly DependencyProperty ScalingModeProperty = ScalingModePropertyKey.DependencyProperty;

        public BitmapScalingMode ScalingMode { get { return (BitmapScalingMode)GetValue(ScalingModeProperty); } }
        #endregion

        #region CanCheckPng
        private static readonly DependencyPropertyKey CanCheckPngPropertyKey = DependencyProperty.RegisterReadOnly(nameof(CanCheckPng), typeof(bool),
            typeof(ListedIconPanel), new PropertyMetadata(true, null, CanCheckPngCoerce));
        public static readonly DependencyProperty CanCheckPngProperty = CanCheckPngPropertyKey.DependencyProperty;

        private static object CanCheckPngCoerce(DependencyObject d, object baseValue)
        {
            ListedIconPanel p = (ListedIconPanel)d;
            bool value = (bool)baseValue;

            var entry = p.Entry;
            if (entry.Width > byte.MaxValue || entry.Height > byte.MaxValue)
                return !entry.IsPng;

            if (entry.BitDepth != IconBitDepth.Depth32BitsPerPixel)
                return entry.IsPng;

            return true;
        }

        public bool CanCheckPng { get { return (bool)GetValue(CanCheckPngProperty); } }
        #endregion

        private void chkPng_Checked(object sender, RoutedEventArgs e)
        {
            MainWindow.IsModified = true;
            CoerceValue(CanCheckPngProperty);
        }

        private void control_Loaded(object sender, RoutedEventArgs e)
        {
            CoerceValue(CanCheckPngProperty);
            chkPng.Checked += chkPng_Checked;
            chkPng.Unchecked += chkPng_Checked;
        }
    }
}
