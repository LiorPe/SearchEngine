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
        PostingFilesManager _postingFilesAPI;
        Dictionary<string, HashSet<string>> languages;
        ObservableCollection<LanguageSelection> languageSelected;
        DocumentRank[] rankedDocument;
        static char[] QuerrySplitters = new char[] { ' ', '\t', '\r' };
        bool searchDone = false;


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
            languages["Afrikaans"] = new HashSet<string> { "Afrikaans" };
            languages["Albanian"] = new HashSet<string> { "Albanian" };
            languages["Amharic"] = new HashSet<string> { "Amharic" };
            languages["Arabic"] = new HashSet<string> { "Arabic", "Arabi" };
            languages["Armenian"] = new HashSet<string> { "Armenian" };
            languages["Azeri"] = new HashSet<string> { "Azeri" };
            languages["Belarusian"] = new HashSet<string> { "Belarusian" };
            languages["Bengali"] = new HashSet<string> { "Bengali" };
            languages["Bulgarian"] = new HashSet<string> { "Bulgarian" };
            languages["Burmese"] = new HashSet<string> { "Burmese" };
            languages["Cambodian"] = new HashSet<string> { "Cambodian" };
            languages["Cantonese"] = new HashSet<string> { "Cantonese" };
            languages["Chinese"] = new HashSet<string> { "Chinese" };
            languages["Creole"] = new HashSet<string> { "Creole" };
            languages["Czech"] = new HashSet<string> { "Czech" };
            languages["Danish"] = new HashSet<string> { "Danish" };
            languages["Dari"] = new HashSet<string> { "Dari" };
            languages["Dutch"] = new HashSet<string> { "Dutch" };
            languages["English"] = new HashSet<string> { "Eng", "English", "Enlgish" };
            languages["Estonian"] = new HashSet<string> { "Estonian" };
            languages["Finnish"] = new HashSet<string> { "Finnish" };
            languages["French"] = new HashSet<string> { "French" };
            languages["Georgian"] = new HashSet<string> { "Georgian" };
            languages["German"] = new HashSet<string> { "German" };
            languages["Greek"] = new HashSet<string> { "Greek" };
            languages["Hebrew"] = new HashSet<string> { "Hebrew" };
            languages["Hindi"] = new HashSet<string> { "Hindi" };
            languages["Hungarian"] = new HashSet<string> { "Hungarian" };
            languages["Indonesian"] = new HashSet<string> { "Indonesian" };
            languages["International"] = new HashSet<string> { "International" };
            languages["Italian"] = new HashSet<string> { "Italian" };
            languages["Japanese"] = new HashSet<string> { "Japanese" };
            languages["Kazakh"] = new HashSet<string> { "Kazakh" };
            languages["Kinyarwanda"] = new HashSet<string> { "Kinyarwanda" };
            languages["Kirundi"] = new HashSet<string> { "Kirundi" };
            languages["Korean"] = new HashSet<string> { "Korean" };
            languages["Kyrgyz"] = new HashSet<string> { "Kyrgyz" };
            languages["Lao"] = new HashSet<string> { "Lao" };
            languages["Latvian"] = new HashSet<string> { "Latvian" };
            languages["Lithuanian"] = new HashSet<string> { "Lithuanian" };
            languages["Macedonian"] = new HashSet<string> { "Macedonian" };
            languages["Malay"] = new HashSet<string> { "Malay" };
            languages["Mandarin"] = new HashSet<string> { "Mandarin" };
            languages["Norwegian"] = new HashSet<string> { "Norwegian" };
            languages["Pashto"] = new HashSet<string> { "Pashto" };
            languages["Persian"] = new HashSet<string> { "Persian" };
            languages["Polish"] = new HashSet<string> { "Polish" };
            languages["Portuguese"] = new HashSet<string> { "Portuguese" };
            languages["Romanian"] = new HashSet<string> { "Romanian" };
            languages["Russian"] = new HashSet<string> { "Russian", "RUssian" };
            languages["Slovak"] = new HashSet<string> { "Slovak" };
            languages["Slovene"] = new HashSet<string> { "Slovene", "Slovenian" };
            languages["Somali"] = new HashSet<string> { "Somali" };
            languages["spanish"] = new HashSet<string> { "Span", "Spanish" };
            languages["Swahili"] = new HashSet<string> { "Swahili" };
            languages["Swedish"] = new HashSet<string> { "Swedish" };
            languages["Tagalog"] = new HashSet<string> { "Tagalog" };
            languages["Tajik"] = new HashSet<string> { "Tajik" };
            languages["Tamil"] = new HashSet<string> { "Tamil" };
            languages["Thai"] = new HashSet<string> { "Thai" };
            languages["Tigrignya"] = new HashSet<string> { "Tigrigna", "Tigrignya", "Tigrinya" };
            languages["Turkish"] = new HashSet<string> { "Turkish" };
            languages["Ukrainian"] = new HashSet<string> { "Ukrainian" };
            languages["Urdu"] = new HashSet<string> { "Urdu" };
            languages["Vietnamese"] = new HashSet<string> { "Vietnamese" };
            languages["Xhosa"] = new HashSet<string> { "Xhosa" };
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

            if ((bool)stemCheck.IsChecked)
                stemming = true;
            else
                stemming = false;
            _postingFilesAPI = new PostingFilesManager(10, dest, stemming);
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
            if (dest != "")
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
            if ((bool)stemCheck.IsChecked)
                stemming = true;
            else
                stemming = false;
            _postingFilesAPI = new PostingFilesManager(10, dest, stemming);
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
                stemLoadSuccess = idx.LoadMainDictionaryFromMemory(stemming);
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

            if (mainTabControl.SelectedIndex == 1 && ((stemming && !stemLoadSuccess) || (!stemming && !noStemLoadSuccess)))
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
            rankedDocument = new DocumentRank[0];
            string stopwords;
            src = srcPath.Text;
            if (src[src.Length - 1] == '\\')
                stopwords = src + "stop_words.txt";
            else stopwords = src + "\\stop_words.txt";
            Parser.InitStopWords(stopwords);
            if (!String.IsNullOrWhiteSpace(txtbxUserQuery.Text))
            {
                SearchQuery(txtbxUserQuery.Text.Split(QuerrySplitters), "295");
            }
            if (!String.IsNullOrWhiteSpace(txtbxFileQuery.Text))
            {
                SearchFileQuery();
            }
            dataGridResults.ItemsSource = new ObservableCollection<DocumentRank>(rankedDocument);
            searchDone = true;

        }

        private void SearchFileQuery()
        {
            string fileQueryPath = txtbxFileQuery.Text;
            if (!File.Exists(fileQueryPath))
            {
                System.Windows.MessageBox.Show("File chosen does not exist");
                return;
            }
            string[] queries;
            try
            {
                queries = FileReader.ReadQueryFile(fileQueryPath);

            }
            catch
            {
                System.Windows.MessageBox.Show("File could not be read, make sure it`s not used by another app");
                return;
            }
            foreach (string query in queries)
            {
                string[] splittedQuery = query.Split(QuerrySplitters);
                string queryId = splittedQuery[0];
                splittedQuery[0] = String.Empty;
                SearchQuery(splittedQuery, queryId);

            }
        }

        private void SearchQuery(string[] query, string queryID)
        {
            Dictionary<string, int> termsInQuery = idx.ParseQuery(query, stemming);
            Dictionary<string, double> termsInQueryToDouble = new Dictionary<string, double>();
            foreach (string term in termsInQuery.Keys)
                termsInQueryToDouble[term] = termsInQuery[term];
            Dictionary<string, PostingFileRecord> releventDocuments = searcher.FindReleventDocuments(termsInQuery);
            HashSet<string> chosenLanguages = ExtractChosenLanguages();
            rankedDocument = ranker.RankDocuments(termsInQueryToDouble, queryID, releventDocuments, rankedDocument, idx.AvgDocumentLength, chosenLanguages);
        }

        private HashSet<string> ExtractChosenLanguages()
        {
            HashSet<string> chosenLanguages = new HashSet<string>();
            foreach (LanguageSelection langSelection in languageSelected)
            {
                if (langSelection.Selected == true)
                {
                    HashSet<string> equivalentLanguages = languages[langSelection.Language];
                    foreach (string equivalentLanguage in equivalentLanguages)
                    {
                        chosenLanguages.Add(equivalentLanguage);
                    }
                }
            }
            return chosenLanguages;
        }

        private void SaveResultsToFile_Click(object sender, RoutedEventArgs e)
        {
            if (!searchDone)
            {
                System.Windows.MessageBox.Show("Please searh a query first.");
                return;
            }
            SaveFileDialog win = new SaveFileDialog();
            win.Filter = "txt files (*.txt)|*.txt";

            DialogResult result = win.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                int resultsCount = rankedDocument.Length;
                string[] queriesRecords = new string[resultsCount];
                for (int i = 0; i < resultsCount; i++)
                {
                    queriesRecords[i] = rankedDocument[i].ToString();
                }
                File.WriteAllLines(win.FileName, queriesRecords);
            }

        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtbxUserQuery.Text) && String.IsNullOrWhiteSpace(txtbxFileQuery.Text))
            {
                System.Windows.MessageBox.Show("Please submit or upload a query");
                return;
            }
            string target = @"C:\IR\";
            DirectoryInfo d = new DirectoryInfo(target);
            FileInfo[] Files = d.GetFiles();
            foreach (FileInfo file in Files)
            {
                try
                {
                    if (file.Name != "treceval.exe" && file.Name != "qrels.txt")
                        File.Delete(target + file.Name);
                }
                catch
                {

                }
            }
            string stopwords;
            src = srcPath.Text;
            if (src[src.Length - 1] == '\\')
                stopwords = src + "stop_words.txt";
            else stopwords = src + "\\stop_words.txt";
            Parser.InitStopWords(stopwords);




            Test(1.8, 0.5, 0.2, 0.65, 1.1);// best








            System.Windows.MessageBox.Show("Test is done");
            return;
        }


        public void Test(params double[] parameters)
        {
            ranker.w1 = parameters[0];
            ranker.w2 = parameters[1];
            ranker.w3 = parameters[2];
            ranker.w4 = parameters[3];

            ranker.wSemantics = parameters[4];

            //for (double w2 = 0; w2 <= 0; w2 += 0)
            //{


            //ranker.w2 = w2;
            rankedDocument = new DocumentRank[0];
            SearchFileQuery();
            int resultsCount = rankedDocument.Length;
            string[] queriesRecords = new string[resultsCount];
            for (int i = 0; i < resultsCount; i++)
            {
                queriesRecords[i] = rankedDocument[i].ToString();
            }
            string fileName = String.Format("results_w1_{0}_w2_{1}_w3_{2}_w4_{3}_wSemantics_{4}.txt", ranker.w1, ranker.w2, ranker.w3 , ranker.w4, ranker.wSemantics);
            string filePath = String.Format(@"C:\IR\{0}", fileName);
            File.WriteAllLines(filePath, queriesRecords);


            Process p = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;

            p.StartInfo = info;
            p.Start();

            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(@"cd C:\IR");
                    sw.WriteLine(String.Format("treceval qrels.txt {0} > output_{0}", fileName));
                }
            }
        }
    }

}
