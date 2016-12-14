﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SearchEngine;

namespace GUI
{
    /// <summary>
    /// Interaction logic for DictionaryWindow.xaml
    /// </summary>
    public partial class DictionaryWindow : Window
    {
        public DictionaryWindow(ref Indexer idx)
        {
            InitializeComponent();
            DataContext = "idx";
            dgIndex.ItemsSource = idx.MainDictionary;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
