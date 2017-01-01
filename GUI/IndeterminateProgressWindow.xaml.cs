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

        /// <summary>
        /// Ctor for IndeterminateProgressWindow
        /// </summary>
        /// <param name="idx">Reference to the used Indexer in the main window</param>
        public IndeterminateProgressWindow(ref Indexer idx)
        {
            InitializeComponent();
            index = idx;
            DataContext = "index";
            this.Dispatcher.Invoke(() =>
            {
                statusTB.Text = index.Status;
            });
            index.PropertyChanged += delegate (Object sender, PropertyChangedEventArgs e)
            {
                //update status on property change notification
                if (e.PropertyName == "Status")
                    this.Dispatcher.Invoke(() =>
                    {
                        statusTB.Text = ((Indexer)sender).Status;
                        //kill window if given the proper notification
                        if (index.Status == "Done")
                            Close();
                    });
            };
        }
    }
}
