using System;
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

namespace GUI
{
    /// <summary>
    /// Interaction logic for StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsWindow : Window
    {
        /// <summary>
        /// Ctor for StatisticsWindow
        /// </summary>
        /// <param name="docs">int representing the number of documents processed</param>
        /// <param name="terms">int representing the number of unique terms in dictionary</param>
        /// <param name="time">string representing the total runtime of the indexing process</param>
        public StatisticsWindow(int docs, int terms, string time)
        {
            InitializeComponent();
            docsTB.Text = ""+docs;
            termsTB.Text = ""+terms;
            timeTB.Text = time;
        }

        /// <summary>
        /// Function to close the window on clicking the OK button
        /// </summary>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
