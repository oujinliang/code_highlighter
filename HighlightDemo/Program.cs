/* Copyright (C) 2012  Jinliang Ou */

namespace Org.Jinou.HighlightDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows;

    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application app = new Application();
            app.Run(new MainWindow());
        }
    }
}
