using SearchEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace GUI
{
    /// <summary>
    /// Interaction logic for IndeterminateProgressWindow.xaml
    /// </summary>
    public partial class IndeterminateProgressWindow : Window
    {
        Indexer index;
        public IndeterminateProgressWindow(ref Indexer idx)
        {
            InitializeComponent();
            index = idx;
            DataContext = "index";
            this.Dispatcher.Invoke(() =>
            {
                statusTB.Text = index.status;
            });
            index.PropertyChanged += delegate (Object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Status")
                    this.Dispatcher.Invoke(() =>
                    {
                        statusTB.Text = ((Indexer)sender).status;
                        if (index.status == "Done")
                            Close();
                    });
            };
        }
    }
}
