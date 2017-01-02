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
using System.Linq;
using GUI.DataGridRecords;
using SearchEngine.Ranking;

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
        bool stemLoadSuccess;
        bool noStemLoadSuccess;
        bool uploaded = false;
        Searcher searcher;
        Ranker ranker;
        PostingFilesAPI _postingFilesAPI;
        Dictionary<string, HashSet<string>> languages;
        ObservableCollection<LanguageSelection> languageSelected;
        List<DocumentRank> rankedDocument;

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
            stemLoadSuccess = false;
            noStemLoadSuccess = false;
            uploaded = true;
            InitLanguages();
            InitLanguageSelected();
        }


        #region Defining language
        private void InitLanguages()
        {
            languages = new Dictionary<string, HashSet<string>>();
            languages["Afrikaans"] = new HashSet<string>{ "Afrikaans" };
            languages["Albanian"] = new HashSet<string>{ "Albanian" };
            languages["Amharic"] = new HashSet<string>{ "Amharic" };
            languages["Arabic"] = new HashSet<string>{ "Arabic", "Arabi" };
            languages["Armenian"] = new HashSet<string>{ "Armenian" };
            languages["Azeri"] = new HashSet<string>{ "Azeri" };
            languages["Belarusian"] = new HashSet<string>{ "Belarusian" };
            languages["Bengali"] = new HashSet<string>{ "Bengali" };
            languages["Bulgarian"] = new HashSet<string>{ "Bulgarian" };
            languages["Burmese"] = new HashSet<string>{ "Burmese" };
            languages["Cambodian"] = new HashSet<string>{ "Cambodian" };
            languages["Cantonese"] = new HashSet<string>{ "Cantonese" };
            languages["Chinese"] = new HashSet<string>{ "Chinese" };
            languages["Creole"] = new HashSet<string>{ "Creole" };
            languages["Czech"] = new HashSet<string>{ "Czech" };
            languages["Danish"] = new HashSet<string>{ "Danish" };
            languages["Dari"] = new HashSet<string>{ "Dari" };
            languages["Dutch"] = new HashSet<string>{ "Dutch" };
            languages["English"] = new HashSet<string>{ "Eng", "English", "Enlgish" };
            languages["Estonian"] = new HashSet<string>{ "Estonian" };
            languages["Finnish"] = new HashSet<string>{ "Finnish" };
            languages["French"] = new HashSet<string>{ "French" };
            languages["Georgian"] = new HashSet<string>{ "Georgian" };
            languages["German"] = new HashSet<string>{ "German" };
            languages["Greek"] = new HashSet<string>{ "Greek" };
            languages["Hebrew"] = new HashSet<string>{ "Hebrew" };
            languages["Hindi"] = new HashSet<string>{ "Hindi" };
            languages["Hungarian"] = new HashSet<string>{ "Hungarian" };
            languages["Indonesian"] = new HashSet<string>{ "Indonesian" };
            languages["International"] = new HashSet<string>{ "International" };
            languages["Italian"] = new HashSet<string>{ "Italian" };
            languages["Japanese"] = new HashSet<string>{ "Japanese" };
            languages["Kazakh"] = new HashSet<string>{ "Kazakh" };
            languages["Kinyarwanda"] = new HashSet<string>{ "Kinyarwanda" };
            languages["Kirundi"] = new HashSet<string>{ "Kirundi" };
            languages["Korean"] = new HashSet<string>{ "Korean" };
            languages["Kyrgyz"] = new HashSet<string>{ "Kyrgyz" };
            languages["Lao"] = new HashSet<string>{ "Lao" };
            languages["Latvian"] = new HashSet<string>{ "Latvian" };
            languages["Lithuanian"] = new HashSet<string>{ "Lithuanian" };
            languages["Macedonian"] = new HashSet<string>{ "Macedonian" };
            languages["Malay"] = new HashSet<string>{ "Malay" };
            languages["Mandarin"] = new HashSet<string>{ "Mandarin" };
            languages["Norwegian"] = new HashSet<string>{ "Norwegian" };
            languages["Pashto"] = new HashSet<string>{ "Pashto" };
            languages["Persian"] = new HashSet<string>{ "Persian" };
            languages["Polish"] = new HashSet<string>{ "Polish" };
            languages["Portuguese"] = new HashSet<string>{ "Portuguese" };
            languages["Romanian"] = new HashSet<string>{ "Romanian" };
            languages["Russian"] = new HashSet<string>{ "Russian", "RUssian" };
            languages["Slovak"] = new HashSet<string>{ "Slovak" };
            languages["Slovene"] = new HashSet<string>{ "Slovene", "Slovenian" };
            languages["Somali"] = new HashSet<string>{ "Somali" };
            languages["spanish"] = new HashSet<string>{ "Span", "Spanish" };
            languages["Swahili"] = new HashSet<string>{ "Swahili" };
            languages["Swedish"] = new HashSet<string>{ "Swedish" };
            languages["Tagalog"] = new HashSet<string>{ "Tagalog" };
            languages["Tajik"] = new HashSet<string>{ "Tajik" };
            languages["Tamil"] = new HashSet<string>{ "Tamil" };
            languages["Thai"] = new HashSet<string>{ "Thai" };
            languages["Tigrignya"] = new HashSet<string>{ "Tigrigna", "Tigrignya", "Tigrinya" };
            languages["Turkish"] = new HashSet<string>{ "Turkish" };
            languages["Ukrainian"] = new HashSet<string>{ "Ukrainian" };
            languages["Urdu"] = new HashSet<string>{ "Urdu" };
            languages["Vietnamese"] = new HashSet<string>{ "Vietnamese" };
            languages["Xhosa"] = new HashSet<string>{ "Xhosa" };
            languages["Zulu"] = new HashSet<string> { "Zulu" };
        }
        private void InitLanguageSelected()
        {
            languageSelected = new ObservableCollection<LanguageSelection>();
            foreach (string language in languages.Keys)
            {
                languageSelected.Add(new LanguageSelection(language));
            }
            datagridLanguageSelection.ItemsSource = languageSelected;

        }
        #endregion

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
            _postingFilesAPI = new PostingFilesAPI(10, dest);
            idx = new Indexer(dest, Mode.Create, _postingFilesAPI);
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
            if (src[src.Length - 1] == '\\')
                stopwords = src + "stop_words.txt";
            else stopwords = src + "\\stop_words.txt";
            idx.IndexCorpus(src, stopwords, stemming);
            if (stemming)
            {
                stemLoadSuccess = true;
                noStemLoadSuccess = false;
            }
            else
            {
                stemLoadSuccess = false;
                noStemLoadSuccess = true;
            }

            searcher = idx.GetSearcher();
            ranker = idx.GetRanker();
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
                if ((stemming && !Indexer.StemmingFiles.All(fileName => (File.Exists(target + fileName)))) || (!stemming && !Indexer.NoStemmingFiles.All(fileName => (File.Exists(target + fileName)))))
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
            ranker = null;
            stemLoadSuccess = false;
            noStemLoadSuccess = false;
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

            if (!hasIndex || !stemLoadSuccess || !noStemLoadSuccess)
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
            _postingFilesAPI = new PostingFilesAPI(10, dest);
            idx = new Indexer(dest, Mode.Load, _postingFilesAPI);
            hasIndex = true;
            //call backgroundworker for the actual loading process
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += _Load_Click;
            worker.RunWorkerCompleted += CheckIfLoadingSucceeded;
            worker.RunWorkerAsync();
            IndeterminateProgressWindow ipWin = new IndeterminateProgressWindow(ref idx);
            ipWin.ShowDialog();
            //protection for premature killing of the progress window


        }

        private void CheckIfLoadingSucceeded(object sender, RunWorkerCompletedEventArgs e)
        {
            
            if (!noStemLoadSuccess && !stemLoadSuccess)
            {
                System.Windows.MessageBox.Show("Dictionary loading failed.", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            searcher = idx.GetSearcher();
            ranker = idx.GetRanker();
            Lang.ItemsSource = idx.DocLanguages;
            string[] languages = idx.DocLanguages.ToArray();
            Array.Sort(languages);
            File.WriteAllLines("languages.txt", languages);
        }

        /// <summary>
        /// Actual loading process
        /// </summary>
        private void _Load_Click(object sender, DoWorkEventArgs e)
        {
            if (stemming)
            {
                stemLoadSuccess =idx.LoadMainDictionaryFromMemory(stemming);
                noStemLoadSuccess = false;
            }
            else
            {
                stemLoadSuccess = false;
                noStemLoadSuccess = idx.LoadMainDictionaryFromMemory(stemming);
            }
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
            if ((bool)stemCheck.IsChecked)
                stemming = true;
            else
                stemming = false;

            if (mainTabControl.SelectedIndex == 1 && ( (stemming&&!stemLoadSuccess) || (!stemming && !noStemLoadSuccess)) )
            {
                System.Windows.MessageBox.Show("Please start the indexing or load a dictionary first.", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // mainTabControl.SelectedIndex = 0;
            }
        }

        private void UserQueryChanged(object sender, TextChangedEventArgs e)
        {

            string userQuery = txtbxUserQuery.Text;
            ObservableCollection<string> SuggestionsList = new ObservableCollection<string>();
            if (userQuery.Length > 0 && userQuery[userQuery.Length - 1] == ' ' && userQuery.Substring(0, userQuery.Length - 1).All(c => c != ' '))
            {
                List<string> autoCompletionSuggestion = searcher.GetCompletionSuggestions(userQuery.Substring(0, userQuery.Length - 1).ToLower());
                foreach (string suggestion in autoCompletionSuggestion)
                    SuggestionsList.Add(userQuery + suggestion);
                if (autoCompletionSuggestion.Count > 0)
                    lstbxAutoComplet.Visibility = Visibility.Visible;
            }
            else
            {
                lstbxAutoComplet.Visibility = Visibility.Hidden;
            }
            lstbxAutoComplet.ItemsSource = SuggestionsList;

        }

        private void Suggestion_Click(object sender, SelectionChangedEventArgs e)
        {
            if (lstbxAutoComplet.SelectedIndex != -1)
            {
                string suggestion = (string)lstbxAutoComplet.SelectedItem;
                txtbxUserQuery.Text = suggestion;
            }

        }

        private void BrowseQueryFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog win = new OpenFileDialog();
            DialogResult result = win.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtbxFileQuery.Text = win.FileName;
            }
        }

        private void SearchQuery_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtbxUserQuery.Text) && String.IsNullOrWhiteSpace(txtbxFileQuery.Text))
            {
                System.Windows.MessageBox.Show("Please submit or upload a query");
            }
            if (!String.IsNullOrWhiteSpace(txtbxUserQuery.Text) && !String.IsNullOrWhiteSpace(txtbxFileQuery.Text))
            {
                System.Windows.MessageBox.Show("Can`t search submitted query and fule query on the same time");
            }
            rankedDocument = new List<DocumentRank>();
            string stopwords;
            if (src[src.Length - 1] == '\\')
                stopwords = src + "stop_words.txt";
            else stopwords = src + "\\stop_words.txt";
            Parser.InitStopWords(src);
            if (!String.IsNullOrWhiteSpace(txtbxUserQuery.Text))
            {
                SearchQuery(txtbxUserQuery.Text,"295");
            }
            if (!String.IsNullOrWhiteSpace(txtbxFileQuery.Text))
            {
                SearchFileQuery();
            }
        }

        private void SearchFileQuery()
        {
            throw new NotImplementedException();
        }

        private void SearchQuery(string query, string queryID)
        {
            Dictionary<string, int> parsedQuery = idx.ParseQuery(query, stemming);
            List<PostingFileRecord> releventDocuments = searcher.FindReleventDocuments(parsedQuery);
            rankedDocument = ranker.RankDocuments(query, queryID, releventDocuments, rankedDocument);
        }
    }
}
