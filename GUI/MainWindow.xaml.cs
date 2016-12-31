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
        bool uploaded = false;
        Searcher searcher;

        /// <summary>
        /// Ctor for the MainWindow
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Keyboard.Focus(srcPath);
            stemming = true;
            hasIndex = false;
            stemCheck.IsChecked = false;
            dest = "";
            src = "";
            destPath.Text = @"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles";
            srcPath.Text = @"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\corpus";
            ResizeMode = ResizeMode.NoResize;
            loadSuccess = false;
            uploaded = true;
        }

        /// <summary>
        /// Interaction logic for the Start button
        /// </summary>
        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            ///start stopwatch for statistics purposes
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
            //run backgroundworker for the actual process
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += _Start_Click;
            worker.RunWorkerAsync();
            ProgressWindow pWin = new ProgressWindow(ref idx);
            pWin.ShowDialog();
            //protection from permature killing of the progress window
            if (pWin.DialogResult != true)
            {
                return;
            }
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
            int documentCount = idx.DocumentsData.Count();
            // Get number of unique terms
            int terms = idx.MainDictionary.Count();
            #endregion

            // Show statistics window
            StatisticsWindow sWin = new StatisticsWindow(documentCount, terms, result);
            sWin.ShowDialog();
            Lang.ItemsSource = idx.DocLanguages;
        }

        /// <summary>
        /// The actual process behind the Start button
        /// </summary>
        private void _Start_Click(object sender, DoWorkEventArgs e)
        {
            //use dispatcher to interact with the UI thread
            this.Dispatcher.Invoke(() =>
            {
                src = srcPath.Text;
                dest = destPath.Text;
            });
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
            loadSuccess = true;
            searcher = idx.GetSearcher();
        }

        /// <summary>
        /// validation of the destination path given by the user
        /// </summary>
        /// <returns></returns>
        private bool IsValid_src()
        {
            //true if exists
                src = srcPath.Text;
            return System.IO.Directory.Exists(src);
        }
        
        /// <summary>
        /// validation of the destination path given by the user
        /// </summary>
        /// <param name="type">identifies the type of path given</param>
        /// <returns></returns>
        private bool IsVaild_dest(string type)
        {
            //user wants to load an existing dictionary
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
                //make sure that the correct dictionary exists in directory
                if ((stemming && !File.Exists(target + "MainDictionaryStemming.zip")) || (!stemming && !File.Exists(target + "MainDictionaryWithoutStemming.zip")))
                    return false;
                return System.IO.Directory.Exists(dest);
            }
            //user wants to create a new dictionary
            else if (type == "start")
            {
                //true if not empty
                    dest = destPath.Text;
                return dest != "";
            }
            else return false;
        }

        /// <summary>
        /// Interaction logic for the RESET button; Empties main memory and calls file deletion function
        /// </summary>
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            //reset args, empty path textboxes
            stemming = true;
            stemCheck.IsChecked = true;
            //TODO: Delete posting files and dictionary here
            Lang.ItemsSource = null;
            idx = null;
            hasIndex = false;
            searcher = null;
            loadSuccess = false;
            Delete_Files();
            System.Windows.MessageBox.Show("The IR Engine has been reset.", "IREngine Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Function to delete dictionary and posting files
        /// </summary>
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

        /// <summary>
        /// Interaction logic for the Show Dictionary button; Calls a DictionaryWindow
        /// </summary>
        private void Show_Click(object sender, RoutedEventArgs e)
        {
            //Check that there's a dictionary in the main memory
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

        /// <summary>
        /// Interaction logic for the LOAD button
        /// </summary>
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            //check stemming checkbox
            if ((bool)stemCheck.IsChecked)
                stemming = true;
            else
                stemming = false;
            if (!IsVaild_dest("load"))
            {
                System.Windows.MessageBox.Show("Please input valid path.", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            dest = destPath.Text;
            idx = new Indexer(dest, dest, Mode.Load);
            hasIndex = true;
            //call backgroundworker for the actual loading process
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += _Load_Click;
            worker.RunWorkerAsync();
            IndeterminateProgressWindow ipWin = new IndeterminateProgressWindow(ref idx);
            ipWin.ShowDialog();
            //protection for premature killing of the progress window
            if(!loadSuccess)
            {
                System.Windows.MessageBox.Show("Dictionary loading failed.", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Lang.ItemsSource = idx.DocLanguages;

        }

        /// <summary>
        /// Actual loading process
        /// </summary>
        private void _Load_Click(object sender, DoWorkEventArgs e)
        {
            loadSuccess = idx.LoadMainDictionaryFromMemory(stemming);
            searcher = idx.GetSearcher();

            //saveXML();
        }

        /// <summary>
        /// XML export function for statistical purposes
        /// </summary>
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

        /// <summary>
        /// Getter/Setter for src_path
        /// </summary>
        public string src_path
        {
            get { return srcPath.Text; }
            set { srcPath.Text = value; }
        }

        /// <summary>
        /// Getter/Setter for dst_path
        /// </summary>
        public string dst_path
        {
            get { return destPath.Text; }   
            set { destPath.Text = value; }
        }

        /// <summary>
        /// Directory selection dialog for the source path box
        /// </summary>
        private void src_browse_button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            src_path = fbd.SelectedPath;
        }

        /// <summary>
        /// Directory selection dialog for the destination path box
        /// </summary>
        private void dst_browse_button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            dst_path = fbd.SelectedPath;
        }


        private void SubmittingQuery_Click(object sender, SelectionChangedEventArgs e)
        {

            if (uploaded==true && !loadSuccess && mainTabControl.SelectedIndex==1)
            {
                System.Windows.MessageBox.Show("Please start the indexing or load a dictionary first.", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
               // mainTabControl.SelectedIndex = 0;
            }
        }

        private void UserQueryChanged(object sender, TextChangedEventArgs e)
        {
            string userQuery = txtbxUserQuery.Text;
            ObservableCollection<string> SuggestionsList = new ObservableCollection<string>();
            if (userQuery.Length>0 && userQuery[userQuery.Length - 1] == ' ')
            {
                List<string> autoCompletionSuggestion = searcher.GetCompletionSuggestions(userQuery.Substring(0, userQuery.Length - 1).ToLower());
                foreach (string suggestion in autoCompletionSuggestion)
                    SuggestionsList.Add(userQuery + suggestion);
                if (autoCompletionSuggestion.Count>0)
                    lstbxAutoComplet.Visibility = Visibility.Visible;
            }
            else
            {
                lstbxAutoComplet.Visibility = Visibility.Hidden;
            }
            lstbxAutoComplet.ItemsSource = SuggestionsList;

        }
    }
}
