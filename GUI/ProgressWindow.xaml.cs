﻿using SearchEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GUI
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        Indexer index;

        public ProgressWindow(ref Indexer idx)
        {
            InitializeComponent();
            index = idx;
            DataContext = "index";
            index.PropertyChanged += delegate (Object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Status")
                    this.Dispatcher.Invoke(() =>
                    {
                        statusTB.Text = ((Indexer)sender).status;
                    });
                if (e.PropertyName == "Progress")
                this.Dispatcher.Invoke(() =>
                {
                    pBar.Value = ((Indexer)sender).progress;
                    pBarPercent.Text = Convert.ToInt32(((Indexer)sender).progress*100).ToString() +"%";
                    if (Convert.ToInt32(((Indexer)sender).progress * 100) == 100)
                    {
                        DialogResult = true;
                        Close();
                    }
                });
            };
        }
    }
}
