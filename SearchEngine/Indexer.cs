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
using SearchEngine.Ranking;

namespace SearchEngine
{
    public enum Mode { Create ,Load };
    public class Indexer : INotifyPropertyChanged
    {
        // Main dictionarry of terms - saves amountt of total frequencies in all docs, name of file (posting file) in which term is saved, and
        // ptr to file (row number in which term is stored)
        Dictionary<string, TermData>[] splittedMainDictionary;
        public int ParserFactor { get; set; }
        //Path for directory in which postinf files will be saved.
        string _mainDictionaryFilePath;

        public ObservableCollection<TermData> MainDictionary;

        public const string MainDictionaryFileNameStemming = "MainDictionaryStemming";
        public const string MainDictionaryFileNameWithoutStemming = "MainDictionaryWithoutStemming";
        public const string DocumentsDataFileNameStemming = "DocumentsDataStemming";
        public const string DocumentsDataFileNameWithoutStemming = "DocumentsDataWithoutStemming";
        public const string LanguagesFileNameStemming = "LanguagesStemming";
        public const string LanguagesFileNameWithoutStemming = "LanguagesWithoutStemming";
        public static HashSet<string> StemmingFiles = new HashSet<string>{ MainDictionaryFileNameStemming, DocumentsDataFileNameStemming, LanguagesFileNameStemming };
        public static HashSet<string> NoStemmingFiles=new HashSet<string> { MainDictionaryFileNameWithoutStemming, DocumentsDataFileNameWithoutStemming, LanguagesFileNameWithoutStemming };
        public double AvgDocumentLength { get; set; }
    

        public ObservableCollection<string> DocLanguages;
        public Dictionary<string, DocumentData> DocumentsData = new Dictionary<string, DocumentData>();

        PostingFilesManager _postingFilesAPI;

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
        bool _useStemming;
        #region Inits
        /// <summary>
        /// C`tor
        /// </summary>
        /// <param name="destPostingFiles">Path to dircetory where posting files will be saved</param>
        /// <param name="mainDictionaryFilePath">Path to dircetory where main dicationary file will be saved</param>
        /// <param name="mode">Creat index/ load one</param>
        /// <param name="parserFactor">How many files to read to memory simultaneously</param>
        /// <param name="numOfPostiongFiles"> How many posting files are kept</param>
        public Indexer(string mainDictionaryFilePath, Mode mode, PostingFilesManager postingFilesAPI, int parserFactor = 10)
        {
            ParserFactor = parserFactor;
            _mainDictionaryFilePath = mainDictionaryFilePath;
            _postingFilesAPI = postingFilesAPI;
            // Init data structures for creating index
            if (mode == Mode.Create)
            {
                InitMainDictionary();
                _postingFilesAPI.InitPostingFiles(StemmingFiles,NoStemmingFiles);
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
                Parser.Parse(docFilesNames[i], useStemming, out termsFrequencies, DocumentsData);
                Status = String.Format("Indexing files: {0}", filesBeingProccessed);
                // Index returned terms
                _postingFilesAPI.UpdateTermsInPostingFiles(termsFrequencies,splittedMainDictionary);
                Progress = (double)(i+1) / (double)(size+1);
                //Console.WriteLine("{0} , {1}", status, progress);
            }
            Status = "Merging main dictionary"; 
            MergeSplittedDictionaries();
            Status = "Finding all languages exist in courpus";
            ExtractLanguages();
            Status = "Saving dictionary to file";

            SaveMainDictionaryToMemory(useStemming);
            CalculateAverageDocumenbtLength();
            Progress = 1;
            
        }

        private void CalculateAverageDocumenbtLength()
        {
            double averageDocumentLength = 0;
            foreach (DocumentData docData in DocumentsData.Values)
            {
                averageDocumentLength += docData.DocumentLength;
            }
            AvgDocumentLength = averageDocumentLength / (double)DocumentsData.Count;
        }

        //Find all languages exist in documents datas
        private void ExtractLanguages()
        {
            HashSet<string> languages = new HashSet<string>();
            foreach (DocumentData docData in DocumentsData.Values)
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
            int numOfPostingFiles = PostingFilesManager.NumOfPostingFiles;
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
            _useStemming = useStemming;
            string fullPath;
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameWithoutStemming;
            Status = "Saving main dictionary";
            SavePropertyToFile(splittedMainDictionary, fullPath);
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + DocumentsDataFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + DocumentsDataFileNameWithoutStemming;
            Status = "Saving documents data";
            SavePropertyToFile(DocumentsData, fullPath);
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + LanguagesFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + LanguagesFileNameWithoutStemming;
            Status = "Saving languages";
            SavePropertyToFile(DocLanguages, fullPath);


        }

        public void SavePropertyToFile(object obj,string fullPath)
        {
            var formatter = new BinaryFormatter();

            try
            {
                System.IO.File.Delete(fullPath);
                //DictionaryData dictionaryData = new DictionaryData(this);

                using (var outputFile = new FileStream(fullPath, FileMode.CreateNew))
                //using (var compressionStream = new GZipStream(outputFile, CompressionMode.Compress))
                {
                    formatter.Serialize(outputFile, obj);
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
            _useStemming = useStemming;
            string fullPath;
            bool succeed;
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameWithoutStemming;
            Status = "Loading main dictionary";

            splittedMainDictionary =  (Dictionary<string,TermData>[]) LoadProeprtyFromFile(fullPath, out succeed);
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + DocumentsDataFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + DocumentsDataFileNameWithoutStemming;
            Status = "Loading document data";
            DocumentsData = (Dictionary<string,DocumentData>)LoadProeprtyFromFile(fullPath, out succeed);
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + LanguagesFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + LanguagesFileNameWithoutStemming;
            Status = "Loading languages";
            DocLanguages = (ObservableCollection<string>)LoadProeprtyFromFile(fullPath, out succeed);
            Status = "Merging main dictionary";
            MergeSplittedDictionaries();
            CalculateAverageDocumenbtLength();
            Status = "Done";
            Progress = 1;
            return succeed;
        }

        object LoadProeprtyFromFile(string fullPath,out bool succeed)
        {
            var formatter = new BinaryFormatter();
            object property;
            try
            {
                using (var outputFile = new FileStream(fullPath, FileMode.Open))
                //using (var compressionStream = new GZipStream(
                //                         outputFile, CompressionMode.Decompress))
                {
                    property = formatter.Deserialize(outputFile);
                    outputFile.Flush();
                    succeed = true;
                    return property;
                }
            }
            catch (Exception e)
            {
                succeed = false;
                return null;
            }

        }


        public Searcher GetSearcher()
        {
            return new Searcher(splittedMainDictionary, DocumentsData,_postingFilesAPI);
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
                _docData = indexer.DocumentsData;
            }

        }

        public Ranker GetRanker()
        {
            return new Ranker(DocumentsData,splittedMainDictionary, GetSearcher(),_useStemming);
        }

        public Dictionary<string, int> ParseQuery(string[] query,bool useStemming)
        {
            int queryIndexer = 0;
            Dictionary<string, int> termsFrequencyInQuery = new Dictionary<string, int>() ;
            Parser.UseStemming = useStemming;
            Parser.IterateTokens(ref queryIndexer, query, termsFrequencyInQuery,false);
            return termsFrequencyInQuery;
        }
    }
}
