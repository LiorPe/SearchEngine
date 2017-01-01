using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SearchEngine
{
    public enum Mode { Create ,Load };
    public class Indexer: INotifyPropertyChanged
    {
        // Main dictionarry of terms - saves amountt of total frequencies in all docs, name of file (posting file) in which term is saved, and
        // ptr to file (row number in which term is stored)
        Dictionary<string, TermData>[] splittedMainDictionary;
        public int ParserFactor { get; set; }
        //Path for directory in which postinf files will be saved.
        string _mainDictionaryFilePath;

        public ObservableCollection<TermData> MainDictionary;
        public ObservableCollection<DocumentData> DocumentsData;

        public const string MainDictionaryFileNameStemming = "MainDictionaryStemming.zip";
        public const string MainDictionaryFileNameWithoutStemming = "MainDictionaryWithoutStemming.zip";


        public ObservableCollection<string> DocLanguages;
        private Dictionary<string, DocumentData> _documnentsData = new Dictionary<string, DocumentData>();

        PostingFilesAPI _postingFilesAPI;

        // for showing progress:
        public event PropertyChangedEventHandler PropertyChanged;
        
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        
        //progress statues between 0-1 
        double _progress = 0;
        public double Progress
        {
            get { return _progress; }
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    NotifyPropertyChanged("Progress");
                }
            }
        }
        //Message about status
        string _status;
        public string Status
        {
            get { return _status; }
            set { if(_status != value)
                    {
                        _status = value;
                        NotifyPropertyChanged("Status");
                    }
            }
        }

        #region Inits
        /// <summary>
        /// C`tor
        /// </summary>
        /// <param name="destPostingFiles">Path to dircetory where posting files will be saved</param>
        /// <param name="mainDictionaryFilePath">Path to dircetory where main dicationary file will be saved</param>
        /// <param name="mode">Creat index/ load one</param>
        /// <param name="parserFactor">How many files to read to memory simultaneously</param>
        /// <param name="numOfPostiongFiles"> How many posting files are kept</param>
        public Indexer(string mainDictionaryFilePath, Mode mode, PostingFilesAPI postingFilesAPI, int parserFactor = 10)
        {
            ParserFactor = parserFactor;
            _mainDictionaryFilePath = mainDictionaryFilePath;
            _postingFilesAPI = postingFilesAPI;
            // Init data structures for creating index
            if (mode == Mode.Create)
            {
                InitMainDictionary();
                _postingFilesAPI.InitPostingFiles(MainDictionaryFileNameStemming, MainDictionaryFileNameWithoutStemming);
            }


        }
        /// <summary>
        /// Index corpus
        /// </summary>
        /// <param name="corpusDirectoryPath">Path to dircetory where corpus is saved</param>
        /// <param name="stopWordsFilePath">Path to file of stopwords</param>
        /// <param name="useStemming">need to use stemming</param>
        public void IndexCorpus(string corpusDirectoryPath, string stopWordsFilePath, bool useStemming)
        {
            // Extract all files in source directory, excluding stopwords file.
            HashSet<string> allFileEntries = new HashSet<string>( Directory.GetFiles(corpusDirectoryPath));
            if (allFileEntries.Contains(stopWordsFilePath))
            {
                allFileEntries.Remove(stopWordsFilePath);

            }
            // Divide the files in source directory to groups of smaller size.
            int amountOfFiles = allFileEntries.Count;
            string[][] docFilesNames;
            if (amountOfFiles < ParserFactor)
                ParserFactor = amountOfFiles;
            if (amountOfFiles % ParserFactor == 0)
                docFilesNames = new string[amountOfFiles / ParserFactor][];
            else
                docFilesNames = new string[amountOfFiles / ParserFactor + 1][];
            string[] set_of_files = new string[ParserFactor];
            int lastOccupied = 0;
            for (int i = 0; i < amountOfFiles; i++)
            {
                set_of_files[lastOccupied] = allFileEntries.ElementAt(i);
                lastOccupied++;
                if (i % ParserFactor == ParserFactor - 1 || i == amountOfFiles - 1)
                {
                    docFilesNames[i / ParserFactor] = set_of_files;
                    set_of_files = new string[Math.Min(ParserFactor, amountOfFiles - i - 1)];
                    lastOccupied = 0;
                }

            }
            TermFrequency[] termsFrequencies;
            int size = docFilesNames.Length;
            Parser.InitStopWords(stopWordsFilePath);
            // for each group of files:
            for (int i = 0; i < size; i++)
            {
                // Update status message for GUI
                string filesBeingProccessed =String.Empty;
                foreach (string fileName in docFilesNames[i])
                {
                    filesBeingProccessed += String.Format("{0};", Path.GetFileName(fileName));

                }
                Status = String.Format("Parsing files: {0}", filesBeingProccessed);
                // Parse group of files and information of terms and documents in files.
                Parser.Parse(docFilesNames[i], useStemming, out termsFrequencies, _documnentsData);
                Status = String.Format("Indexing files: {0}", filesBeingProccessed);
                // Index returned terms
                _postingFilesAPI.UpdateTermsInPostingFiles(termsFrequencies,splittedMainDictionary);
                Progress = (double)(i+1) / (double)(size+1);
                //Console.WriteLine("{0} , {1}", status, progress);
            }
            Status = "Merging main dictionary"; 
            MergeSplittedDictionaries();
            InitDocumentsData();
            Status = "Finding all languages exist in courpus";
            ExtractLanguages();
            Status = "Saving dictionary to file";

            SaveMainDictionaryToMemory(useStemming);
            Progress = 1;
            
        }

        //Find all languages exist in documents datas
        private void ExtractLanguages()
        {
            HashSet<string> languages = new HashSet<string>();
            foreach (DocumentData docData in _documnentsData.Values)
            {
                string language = docData.Language;
                if (language!=String.Empty && !languages.Contains(language))
                    languages.Add(language);
            }
            DocLanguages = new ObservableCollection<string>(languages);
        }


        // Create main dictionary which maps for every term its total frequencies, file name of posting file and ptr to row in file in which it`s stored.
        private void InitMainDictionary()
        {
            int numOfPostingFiles = PostingFilesAPI.NumOfPostingFiles;
            splittedMainDictionary = new Dictionary<string, TermData>[numOfPostingFiles];
            for (int i = 0; i < numOfPostingFiles; i++)
            {
                splittedMainDictionary[i] = new Dictionary<string, TermData>();
            }
        }
        #endregion

       

        /// <summary>
        /// Merge sub-dictionaries to one sorted dictionary
        /// </summary>
        public void MergeSplittedDictionaries()
        {
            int totoalEntriesInDictionary = 0;
            foreach (Dictionary<string, TermData> dict in splittedMainDictionary)
                totoalEntriesInDictionary += dict.Count();
            TermData[] allTerms = new TermData[totoalEntriesInDictionary];
            int index = 0;

            foreach (Dictionary<string, TermData> dict in splittedMainDictionary)
            {
                var sortedDictionary = dict.Values.OrderBy(term => term.Term);
                foreach (TermData term in sortedDictionary)
                {
                    allTerms[index] = term;
                    index++;
                }
            }
            MainDictionary = new ObservableCollection<TermData>(allTerms);

        }

        /// <summary>
        /// Saving information of main dictionary to file
        /// </summary>
        /// <param name="useStemming"></param>
        public void SaveMainDictionaryToMemory(bool useStemming)
        {
            var formatter = new BinaryFormatter();
            string fullPath;
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameWithoutStemming;
            try
            {
                System.IO.File.Delete(fullPath);
                DictionaryData dictionaryData = new DictionaryData(this);
                using (var outputFile = new FileStream(fullPath, FileMode.CreateNew))
                //using (var compressionStream = new GZipStream(outputFile, CompressionMode.Compress))
                {
                    formatter.Serialize(outputFile, dictionaryData);
                    outputFile.Flush();
                }
            }
            catch
            {
                return;
            }

        }


        /// <summary>
        /// Load main dictionary from memory
        /// </summary>
        /// <param name="useStemming"></param>
        /// <returns></returns>
        public bool LoadMainDictionaryFromMemory(bool useStemming)
        {
            string fullPath;
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameWithoutStemming;
            Progress = 0;
            Status = "Reading main dictionary data file";
            var formatter = new BinaryFormatter();
            DictionaryData dictionaryData;
            try
            {
                using (var outputFile = new FileStream(fullPath, FileMode.Open))
                //using (var compressionStream = new GZipStream(
                //                         outputFile, CompressionMode.Decompress))
                {
                    dictionaryData = (DictionaryData)formatter.Deserialize(outputFile);
                    outputFile.Flush();
                }
            }
            catch
            {
                return false;
            }

            splittedMainDictionary = dictionaryData._splittedMainDictionary;
            //MainDictionary = dictionaryData._mainDictionary;
            Status = "Merging sub-dictionaries to main dictionary";
            DocLanguages = dictionaryData._docLanguages;
            _documnentsData = dictionaryData._docData;
            MergeSplittedDictionaries();
            InitDocumentsData();
            Status = "Done";
            Progress = 1;
            return true;
        }

        /// <summary>
        /// Init Observable collection of documnets.
        /// </summary>
        private void InitDocumentsData()
        {
            DocumentsData = new ObservableCollection<DocumentData>(_documnentsData.Values.ToArray());
        }

        public Searcher GetSearcher()
        {
            return new Searcher(splittedMainDictionary, _documnentsData,_postingFilesAPI);
        }

        [Serializable]
        internal class DictionaryData
        {
            // Main dictionarry of terms - saves amountt of total frequencies in all docs, name of file (posting file) in which term is saved, and
            // ptr to file (row number in which term is stored)
            internal Dictionary<string, TermData>[] _splittedMainDictionary;
            internal ObservableCollection<string> _docLanguages;
            internal  Dictionary<string, DocumentData> _docData;


            public DictionaryData(Indexer indexer)
            {
                _splittedMainDictionary = indexer.splittedMainDictionary;
                _docLanguages = indexer.DocLanguages;
                _docData = indexer._documnentsData;
            }

        }
    }
}
