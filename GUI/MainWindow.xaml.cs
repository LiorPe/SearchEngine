using SearchEngine;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Boolean stemming;
        Boolean hasIndex;
        string dest;
        string src;
        Indexer idx;

        public MainWindow()
        {
            InitializeComponent();
            Keyboard.Focus(srcPath);
            stemming = true;
            hasIndex = false;
            stemCheck.IsChecked = true;
            dest = "";
            src = "";
            ResizeMode = ResizeMode.NoResize;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            //check for valid paths
            if (!IsValid_src() || !IsVaild_dest("start"))
            {
                System.Windows.MessageBox.Show("Please input valid paths.", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            src = srcPath.Text;
            dest = destPath.Text;
            idx = new Indexer(dest, dest);
            hasIndex = true;
            ProgressWindow pWin = new ProgressWindow(ref idx);
            pWin.Show();
            //check stemming checkbox
            if ((bool)stemCheck.IsChecked)
                stemming = true;
            else
                stemming = false;
            string stopwords;
            if (src[src.Length-1] == '\\')
                stopwords = src + "stop_words.txt";
            else stopwords = src + "\\stop_words.txt";
            idx.IndexCorpus(src, stopwords, stemming);
            stopWatch.Stop();

            #region statistics

            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            string result = string.Format("{0}", elapsedTime);

            // Get number of indexed documents
            int fileCount = Directory.GetFiles(src).Length-1;
            // Get number of unique terms
            int terms = idx.MainDictionary.Count();
            #endregion

            // Show statistics window
            StatisticsWindow sWin = new StatisticsWindow(fileCount, terms, result);
            sWin.ShowDialog();
        }

        private void progressThread()
        {
            ProgressWindow pWin = new ProgressWindow(ref idx);
            pWin.Show();
        }

        private bool IsValid_src()
        {
            //true if exists
            src = srcPath.Text;
            return System.IO.Directory.Exists(src);
        }
        private bool IsVaild_dest(string type)
        {
            if (type == "load")
            {
                //true if exists
                dest = destPath.Text;
                string target;
                if (dest[dest.Length - 1] == '\\')
                    target = dest;
                else target = dest + '\\';
                if (!File.Exists(target + "MainDictionary.zip"))
                    return false;
                return System.IO.Directory.Exists(dest);
            }
            else if (type == "start")
            {
                //true if not empty
                dest = destPath.Text;
                return dest != "";
            }
            else return false;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            //reset args, empty path textboxes
            stemming = true;
            stemCheck.IsChecked = true;
            //TODO: Delete posting files and dictionary here
            idx = null;
            hasIndex = false;
            Delete_Files();
            System.Windows.MessageBox.Show("The IR Engine has been reset.", "IREngine Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Delete_Files()
        {
            string target;
            if (dest[dest.Length - 1] == '\\')
                target = dest;
            else target = dest + '\\';
            if (File.Exists(target + "MainDictionary.zip"))
                File.Delete(target + "MainDictionary.zip");
            if (File.Exists(target + "SortedDictionary.txt"))
                File.Delete(target + "SortedDictionary.txt");
            Boolean stop = false;
            int i = 0;
            while (!stop)
            {
                if (File.Exists(target + i + ".txt"))
                {
                    File.Delete(target + i + ".txt");
                    i++;
                }
                else stop = true;
            }
        }

        private void Show_Click(object sender, RoutedEventArgs e)
        {
            if (!hasIndex)
            {
                System.Windows.MessageBox.Show("Please start the indexing or load a dictionary first.", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                DictionaryWindow dWin = new DictionaryWindow(ref idx);
                dWin.ShowDialog();
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (!IsVaild_dest("load"))
            {
                System.Windows.MessageBox.Show("Please input valid path.", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //check stemming checkbox
            if ((bool)stemCheck.IsChecked)
                stemming = true;
            else
                stemming = false;
            dest = destPath.Text;
            idx = new Indexer(dest, dest);
            hasIndex = true;
            idx.LoadMainDictionaryFromMemory();
        }
        public string src_path
        {
            get { return srcPath.Text; }
            set { srcPath.Text = value; }
        }
        public string dst_path
        {
            get { return destPath.Text; }
            set { destPath.Text = value; }
        }

        private void src_browse_button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            //ofd.Filter = "Maze Files | *.maze";
            //ofd.DefaultExt = "maze";
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            src_path = fbd.SelectedPath;
        }

        private void dst_browse_button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            //ofd.Filter = "Maze Files | *.maze";
            //ofd.DefaultExt = "maze";
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            dst_path = fbd.SelectedPath;
        }
    }
}
