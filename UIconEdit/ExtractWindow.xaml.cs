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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace UIconEdit.Maker
{
    /// <summary>
    /// Interaction logic for ExtractWindow.xaml
    /// </summary>
    partial class ExtractWindow : IDisposable
    {
        public ExtractWindow(MainWindow owner, string path, int iconCount)
        {
            Owner = owner;

            _path = path;
            _iconCount = iconCount;

            SetValue(HasIconsPropertyKey, (iconCount != 0));

            InitializeComponent();
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            var settingsFile = SettingsFile;

            if (HasIcons)
            {
                for (int i = 0; i < _iconCount; i++)
                {
                    try
                    {
                        _icons.Add(new FileToken(_path, settingsFile, i));
                    }
                    catch { }
                }
                SetValue(HasIconsPropertyKey, _icons.Count != 0);
            }

            Mouse.OverrideCursor = null;

            if (HasIcons)
                listIcons.SelectedIndex = 0;
            else
            {
                ErrorWindow.Show((MainWindow)Owner, this, string.Format(settingsFile.LanguageFile.IconExtractNone, _path));
                DialogResult = false;
                return;
            }
        }

        private string _path;

        private int _iconCount;
        [Bindable(true)]
        public SettingsFile SettingsFile { get { return ((MainWindow)Owner).SettingsFile; } }

        private static void _handler(IconExtractException e) { }

        #region IsFullyLoaded
        private static readonly DependencyPropertyKey IsFullyLoadedPropertyKey = DependencyProperty.RegisterReadOnly("IsFullyLoaded", typeof(bool), typeof(ExtractWindow),
            new PropertyMetadata(false));
        public static DependencyProperty IsFullyLoadedProperty = IsFullyLoadedPropertyKey.DependencyProperty;

        public bool IsFullyLoaded { get { return (bool)GetValue(IsFullyLoadedProperty); } }
        #endregion

        #region HasIcons
        private static readonly DependencyPropertyKey HasIconsPropertyKey = DependencyProperty.RegisterReadOnly("HasIcons", typeof(bool), typeof(ExtractWindow),
            new PropertyMetadata(false));
        public static DependencyProperty HasIconsProperty = HasIconsPropertyKey.DependencyProperty;

        public bool HasIcons { get { return (bool)GetValue(HasIconsProperty); } }
        #endregion

        private static ObservableCollection<FileToken> _icons = new ObservableCollection<FileToken>();
        [Bindable(true)]
        public ObservableCollection<FileToken> IconFiles { get { return _icons; } }

        private static ObservableCollection<FileToken> _cursors = new ObservableCollection<FileToken>();
        [Bindable(true)]
        public ObservableCollection<FileToken> CursorFiles { get { return _cursors; } }

        public int IconIndex { get { return listIcons.SelectedIndex; } }

        public struct FileToken : IDisposable
        {
            public FileToken(string path, SettingsFile settings, int index)
            {
                IntPtr hIcon = IntPtr.Zero;
                try
                {
                    hIcon = ExtractIcon(System.Diagnostics.Process.GetCurrentProcess().Handle, path, index);
                    _count = 0; //TODO: Fix this!
                    if (hIcon == IntPtr.Zero) throw new Win32Exception();
                    _image = new WriteableBitmap(Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()));
                }
                finally
                {
                    if (hIcon != IntPtr.Zero)
                        DestroyIcon(hIcon);
                }
                _settings = settings;
                _index = index;
            }

            [DllImport("shell32.dll")]
            private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

            [DllImport("user32.dll")]
            private static extern bool DestroyIcon(IntPtr hIcon);

            public BitmapSource _image;
            [Bindable(true)]
            public BitmapSource Image { get { return _image; } }

            public SettingsFile _settings;
            [Bindable(true)]
            public SettingsFile Settings { get { return _settings; } }

            public int _index;
            [Bindable(true)]
            public int Index { get { return _index; } }

            public int _count;
            [Bindable(true)]
            public int Count { get { return _count; } }

            public override string ToString()
            {
                string format;
                if (_settings == null) format = "#{0} ({1})";
                else format = _settings.LanguageFile.ExtractFrameCount;
                return string.Format(format, _index, _count);
            }

            public void Dispose()
            {
                _image = null;
                _settings = null;
            }
        }

        private static void _handler(IconLoadException e) { }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void listIcons_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public void Dispose()
        {
            foreach (FileToken curToken in _cursors.Concat(_icons))
                curToken.Dispose();
            _icons.Clear();
            _cursors.Clear();
        }
    }
}
