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
using System.Windows.Threading;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Xml;

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
        Boolean loadSuccess;

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
            loadSuccess = false;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
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
            idx = new Indexer(dest, dest, Mode.Create);
            hasIndex = true;
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += _Start_Click;
            worker.RunWorkerAsync();
            ProgressWindow pWin = new ProgressWindow(ref idx);
            pWin.ShowDialog();
            if (pWin.DialogResult != true)
            {
                return;
            }
            loadSuccess = true;
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
            int fileCount = Directory.GetFiles(src).Length - 1;
            // Get number of unique terms
            int terms = idx.MainDictionary.Count();
            #endregion

            // Show statistics window
            StatisticsWindow sWin = new StatisticsWindow(fileCount, terms, result);
            sWin.ShowDialog();
            Lang.ItemsSource = idx.DocLanguages;
        }

        private void _Start_Click(object sender, DoWorkEventArgs e)
        {

            this.Dispatcher.Invoke(() =>
            {
                src = srcPath.Text;
                dest = destPath.Text;
            });
            //ShowProgress();
            //check stemming checkbox
            this.Dispatcher.Invoke(() =>
            {
                if ((bool)stemCheck.IsChecked)
                    stemming = true;
                else
                    stemming = false;
            });

            string stopwords;
            if (src[src.Length-1] == '\\')
                stopwords = src + "stop_words.txt";
            else stopwords = src + "\\stop_words.txt";
            idx.IndexCorpus(src, stopwords, stemming);
        }

        private void ShowProgress()
        {

            Thread t = new Thread(() =>
            {
                ProgressWindow pWin = new ProgressWindow(ref idx);
                pWin.Show();
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();

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
                if (dest == String.Empty || dest == null)
                    return false;
                if (dest[dest.Length - 1] == '\\')
                    target = dest;
                else target = dest + '\\';
                if (!File.Exists(target + "MainDictionaryStemming.zip") && !File.Exists(target + "MainDictionaryWithoutStemming.zip"))
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
            Lang.ItemsSource = null;
            idx = null;
            hasIndex = false;
            loadSuccess = false;
            Delete_Files();
            System.Windows.MessageBox.Show("The IR Engine has been reset.", "IREngine Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Delete_Files()
        {
            string target;
            if (dest[dest.Length - 1] == '\\')
                target = dest;
            else target = dest + '\\';
            DirectoryInfo d = new DirectoryInfo(target);
            FileInfo[] Files = d.GetFiles();
            foreach (FileInfo file in Files)
            {
                try
                {
                    File.Delete(target + file.Name);
                }
                catch
                {

                }
            }
            
        }

        private void Show_Click(object sender, RoutedEventArgs e)
        {
            if (!hasIndex || !loadSuccess)
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
            idx = new Indexer(dest, dest, Mode.Load);
            hasIndex = true;
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += _Load_Click;
            worker.RunWorkerAsync();
            IndeterminateProgressWindow ipWin = new IndeterminateProgressWindow(ref idx);
            ipWin.ShowDialog();
            if(!loadSuccess)
            {
                System.Windows.MessageBox.Show("Dictionary loading failed.", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Lang.ItemsSource = idx.DocLanguages;

        }

        private void _Load_Click(object sender, DoWorkEventArgs e)
        {
            loadSuccess = idx.LoadMainDictionaryFromMemory(stemming);
            //saveXML();
        }

        private void saveXML()
        {
            string target;
            if (dest[dest.Length - 1] == '\\')
                target = dest + "data.xml";
            else target = dest + "\\data.xml";
            using (XmlWriter writer = XmlWriter.Create(target))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Terms");

                foreach (TermData t in idx.MainDictionary)
                {
                    writer.WriteStartElement("Term");

                    writer.WriteElementString("Term", t.Term);
                    writer.WriteElementString("CollectionFrequency", t.CollectionFrequency.ToString());

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
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
