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
        // Saves what is the last row that was written in each posting file (so you can know what is the next availabe row infile)
        Dictionary<int, int> lastRowWrittenInFile;
        //How many different posting files exist.
        public static int NumOfPostingFiles { get; set; }
        public int ParserFactor { get; set; }
        //Path for directory in which postinf files will be saved.
        string _destPostingFiles;
        string _mainDictionaryFilePath;

        int charValuesRange = 'z' -'a' + 1;
        int charIntervalForPostingFile;
        const int minCharValue = 'a';
        public ObservableCollection<TermData> MainDictionary;
        public const string MainDictionaryFileNameStemming = "MainDictionaryStemming.zip";
        public const string MainDictionaryFileNameWithoutStemming = "MainDictionaryWithoutStemming.zip";


        // for showing progress:
        public event PropertyChangedEventHandler PropertyChanged;
        
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        
        //progress statues between 0-1 
        double _progress = 0;
        public double progress
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
        public string status
        {
            get { return _status; }
            set { if(_status != value)
                    {
                        _status = value;
                        NotifyPropertyChanged("Status");
                    }
            }
        }

        public ObservableCollection<string> DocLanguages;
        private Dictionary<string, DocumentData> _documnentsData = new Dictionary<string, DocumentData>();
        #region Inits

        public Indexer(string destPostingFiles, string mainDictionaryFilePath, Mode mode, int parserFactor=10, int numOfPostiongFiles = 10)
        {
            NumOfPostingFiles = numOfPostiongFiles;
            ParserFactor = parserFactor;
            _destPostingFiles = destPostingFiles;
            charIntervalForPostingFile = (int)Math.Ceiling((double)charValuesRange / (double)NumOfPostingFiles);
            _mainDictionaryFilePath = mainDictionaryFilePath;

            if (mode == Mode.Create)
            {
            InitLastRowWrittenInFile();
            InitMainDictionary();
            InitLastRowWrittenInFile();
            InitPostingFiles();
            }


        }

        public void IndexCorpus(string corpusDirectoryPath, string stopWordsFilePath, bool useStemming)
        {
            HashSet<string> allFileEntries = new HashSet<string>( Directory.GetFiles(corpusDirectoryPath));
            if (allFileEntries.Contains(stopWordsFilePath))
            {
                allFileEntries.Remove(stopWordsFilePath);

            }
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
            for (int i = 0; i < size; i++)
            {
                string filesBeingProccessed=String.Empty;
                foreach (string fileName in docFilesNames[i])
                {
                    filesBeingProccessed += String.Format("{0};", Path.GetFileName(fileName));

                }
                status = String.Format("Parsing files: {0}", filesBeingProccessed);
                Parser.Parse(docFilesNames[i], useStemming, out termsFrequencies, _documnentsData);
                status = String.Format("Indexing files: {0}", filesBeingProccessed);
                IndexParsedTerms(termsFrequencies);
                progress = (double)(i+1) / (double)(size+1);
                //Console.WriteLine("{0} , {1}", status, progress);
            }
            status = "Merging main dictionary"; 
            MergeSplittedDictionaries();
            status = "Finding all languages exist in courpus";
            ExtractLanguages();
            status = "Saving dictionary to file";

            SaveMainDictionaryToMemory(useStemming);
            progress = 1;
            
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

        //init dictionary whichmaps the posting file and the last availabe row
        private void InitLastRowWrittenInFile()
        {
            lastRowWrittenInFile = new Dictionary<int, int>();
            for (int i = 0; i < NumOfPostingFiles; i++)
            {
                lastRowWrittenInFile[i] = 0;
            }

        }

        //Create all files for posting files.
        private void InitPostingFiles()
        {
            if (!Directory.Exists(_destPostingFiles))  // if it doesn't exist, create
                Directory.CreateDirectory(_destPostingFiles);
            System.IO.DirectoryInfo di = new DirectoryInfo(_destPostingFiles);

            foreach (FileInfo file in di.GetFiles())
            {
                if(file.Name != MainDictionaryFileNameStemming && file.Name != MainDictionaryFileNameWithoutStemming)
                file.Delete();
            }
            string fullPostingFilesPath;
            for (int i = 0; i < NumOfPostingFiles; i++)
            {
                fullPostingFilesPath = _destPostingFiles + "\\" + i + ".txt";
                if (!File.Exists(fullPostingFilesPath))
                    using (StreamWriter sw = File.CreateText(fullPostingFilesPath)) { }
            }
        }

        // Create main dictionary which maps for every term its total frequencies, file name of posting file and ptr to row in file in which it`s stored.
        private void InitMainDictionary()
        {
            splittedMainDictionary = new Dictionary<string, TermData>[NumOfPostingFiles];
            for (int i = 0; i < NumOfPostingFiles; i++)
            {
                splittedMainDictionary[i] = new Dictionary<string, TermData>();
            }
        }
        #endregion
        private void IndexParsedTerms(TermFrequency[] termsToIndex)
        {
            int size = termsToIndex.Length;
            TermFrequency termFreq;
            for (int termIndex = 0; termIndex < size; termIndex++)
            {
                termFreq = termsToIndex[termIndex];
                int postingFileName = MatchPostingFileToTerm(termFreq.Term);
                Dictionary<string, TermData> correlatedDictionary = splittedMainDictionary[postingFileName];
                if (!correlatedDictionary.ContainsKey(termFreq.Term))
                {
                    correlatedDictionary[termFreq.Term] = new TermData(termFreq.Term,termFreq.DocumentFrequency, termFreq.CollectionFrequency, postingFileName, lastRowWrittenInFile[postingFileName]);
                    lastRowWrittenInFile[postingFileName]++;
                }
                else
                {
                    correlatedDictionary[termFreq.Term].DocumentFrequency += termFreq.DocumentFrequency;
                    correlatedDictionary[termFreq.Term].CollectionFrequency += termFreq.CollectionFrequency;
                }
                termFreq.PostingFileName = postingFileName;
                termFreq.RowInPostFile = correlatedDictionary[termFreq.Term].PtrToFile;
                termsToIndex[termIndex] = termFreq;

            }
            termsToIndex = termsToIndex.OrderBy(term => term.PostingFileName).ThenBy(term => term.RowInPostFile).ToArray<TermFrequency>();
            int i = 0;
            TermFrequency termToIndex;
            while (i < size)
            {

                termToIndex = termsToIndex[i];

                int fileName = termToIndex.PostingFileName;
                string postfileDestPath = _destPostingFiles + "\\" + fileName + ".txt";
                string duplicatePostingFileDestPath = _destPostingFiles + "\\" + fileName + "_duplicate" + ".txt";
                try
                {
                    File.Move(postfileDestPath, duplicatePostingFileDestPath);

                }
                catch
                {
                    File.Delete(duplicatePostingFileDestPath);
                    return;
                }
                int fileCursor = 0;
                string sourcePostingFileEntry = null;
                using (BinaryReader sourcePostingFile = new BinaryReader(File.Open(duplicatePostingFileDestPath, FileMode.Open)))
                {
                    using (BinaryWriter targetPostingFile = new BinaryWriter(File.Create(postfileDestPath)))
                    {
                        while (i < size && (termToIndex = termsToIndex[i]).PostingFileName == fileName)
                        {
                            int rowToUpdate = termToIndex.RowInPostFile;
                            while (sourcePostingFile.BaseStream.Position != sourcePostingFile.BaseStream.Length & fileCursor < rowToUpdate)
                            {
  
                                    sourcePostingFileEntry = sourcePostingFile.ReadString();
                                    targetPostingFile.Write(sourcePostingFileEntry);
                                    fileCursor++;
                                

                            }
                            if (sourcePostingFile.BaseStream.Position != sourcePostingFile.BaseStream.Length)
                            {
                                sourcePostingFileEntry = sourcePostingFile.ReadString();
                                sourcePostingFileEntry = TermFrequency.AddFrequenciesToString(sourcePostingFileEntry, termToIndex.FrequenciesInDocuments);
                                targetPostingFile.Write(sourcePostingFileEntry);
                                fileCursor++;
                            }
                            else
                            {
                                targetPostingFile.Write(termToIndex.FrequenciesInDocuments);
                            }
                            i++;


                        }
                    }
                }
                File.Delete(duplicatePostingFileDestPath);
            }

        }

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
                using (var compressionStream = new GZipStream(outputFile, CompressionMode.Compress))
                {
                    formatter.Serialize(compressionStream, dictionaryData);
                    compressionStream.Flush();
                }
            }
            catch
            {
                return;
            }

        }


        private int MatchPostingFileToTerm(string term)
        {
            int ans = (int)Math.Ceiling((double)(term[0] - minCharValue ) / (double)(charIntervalForPostingFile));
            return Math.Max(Math.Min(ans, NumOfPostingFiles - 1), 0);
        }

        public bool LoadMainDictionaryFromMemory(bool useStemming)
        {
            string fullPath;
            if (useStemming)
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameStemming;
            else
                fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileNameWithoutStemming;
            progress = 0;
            status = "Reading main dictionary data file";
            var formatter = new BinaryFormatter();
            DictionaryData dictionaryData;
            try
            {
                using (var outputFile = new FileStream(fullPath, FileMode.Open))
                using (var compressionStream = new GZipStream(
                                         outputFile, CompressionMode.Decompress))
                {
                    dictionaryData = (DictionaryData)formatter.Deserialize(compressionStream);
                    compressionStream.Flush();
                }
            }
            catch
            {
                return false;
            }

            splittedMainDictionary = dictionaryData._splittedMainDictionary;
            lastRowWrittenInFile = dictionaryData._lastRowWrittenInFile;
            //MainDictionary = dictionaryData._mainDictionary;
            status = "Merging sub-dictionaries to main dictionary";
            MergeSplittedDictionaries();
            DocLanguages = dictionaryData._docLanguages;
            _documnentsData = dictionaryData._docData;
            status = "Done";
            progress = 1;
            return true;
        }

        [Serializable]
        internal class DictionaryData
        {
            // Main dictionarry of terms - saves amountt of total frequencies in all docs, name of file (posting file) in which term is saved, and
            // ptr to file (row number in which term is stored)
            internal Dictionary<string, TermData>[] _splittedMainDictionary;
            // Saves what is the last row that was written in each posting file (so you can know what is the next availabe row infile)
            internal Dictionary<int, int> _lastRowWrittenInFile;
            //Path for directory in which postinf files will be saved.
            //internal ObservableCollection<TermData> _mainDictionary;
            internal ObservableCollection<string> _docLanguages;
            internal  Dictionary<string, DocumentData> _docData;


            public DictionaryData(Indexer indexer)
            {
                _splittedMainDictionary = indexer.splittedMainDictionary;
                _lastRowWrittenInFile = indexer.lastRowWrittenInFile;
                //_mainDictionary = indexer.MainDictionary;
                _docLanguages = indexer.DocLanguages;
                _docData = indexer._documnentsData;
            }

        }
    }
}
