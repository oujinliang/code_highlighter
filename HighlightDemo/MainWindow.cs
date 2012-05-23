//---------------------------------------------------------------------
// <copyright file="MainWindow.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Use of this source code is subject to the terms of the Microsoft 
// end-user license agreement (EULA) under which you licensed this
// SOFTWARE PRODUCT. If you did not accept the terms of the EULA, you 
// are not authorized to use this source code. For a copy of the EULA, 
// please see the LICENSE.RTF on your install media.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Internals.Tools.Ding.HighlightDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.Win32;
    using System.IO;
    using Microsoft.Internals.Tools.Ding.HighlightEngine;
    using System.Diagnostics;

    internal class MainWindow : Window
    {
        #region Private Fields

        private Button btnOpenFile;
        private Label label;

        private CodeBox codeViewer;

        #endregion

        public MainWindow()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Title = "Code Highlight Demo";
            this.MinHeight = 600;
            this.MinWidth = 800;
            this.Height = 700;
            this.Width = 1000;

            DockPanel dock = new DockPanel();
            dock.HorizontalAlignment = HorizontalAlignment.Stretch;
            dock.VerticalAlignment = VerticalAlignment.Stretch;
            this.Content = dock;

            btnOpenFile = new Button
            {
                Content = "Open File",
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 100,
                Margin = new Thickness(10),
            };
            btnOpenFile.Click += new RoutedEventHandler(BtnOpenFile_Click);

            label = new Label
            {
                Content = "",
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            StackPanel top = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                FlowDirection = FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            top.Children.Add(btnOpenFile);
            top.Children.Add(label);
            dock.Children.Add(top);
            DockPanel.SetDock(top, Dock.Top);

            this.codeViewer = new CodeBox
            {
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            dock.Children.Add(codeViewer);
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "All document|*.*", Multiselect = false };
            if (dlg.ShowDialog() == true)
            {
                string info;
                var lines = LoadFile(dlg.FileName, out info);
                label.Content = dlg.FileName + info;

                codeViewer.UpdateLines(lines);

                this.InvalidateVisual();
            }
        }

        public TextLineInfo[] LoadFile(string filePath, out string info)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            TimeSpan loadProfile;
            HighlightProfile profile =
                GetTimeSpan(() => HighlightProfileFactory.GetProfileByExtension(fileInfo.Extension), out loadProfile);

            TimeSpan readLines;
            string[] lines = GetTimeSpan(() => File.ReadAllLines(filePath), out readLines);

            TimeSpan parseLines;
            TextLineInfo[] infos = GetTimeSpan(() => new HighlightParser(profile).Parse(lines, 0), out parseLines);

            string[] fileLoadnfos = 
                {
                    " File Length: " + fileInfo.Length / 1024 + " KB", 
                    " File Lines: " + lines.Length, "\n",
                    " Load Profile Time: " + loadProfile,
                    " Read All Lines Time: " + readLines,
                    " Parse Highlight Time: " + parseLines,
                };
            info = string.Concat(fileLoadnfos);

            return infos;
        }

        private static T GetTimeSpan<T>(Func<T> func, out TimeSpan timespan)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            T t = func();

            sw.Stop();
            timespan = sw.Elapsed;

            return t;
        }
    }
}
