﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class Indexer
    {
        // Main dictionarry of terms - saves amountt of total frequencies in all docs, name of file (posting file) in which term is saved, and
        // ptr to file (row number in which term is stored)
        Dictionary<string, TermData>[] splittedMainDictionary;
        // Saves what is the last row that was written in each posting file (so you can know what is the next availabe row infile)
        Dictionary<int, int> lastRowWrittenInFile;
        //How many different posting files exist.
        public int NumOfPostingFiles{get;set; }
        public int ParserFactor { get; set; }
        //Path for directory in which postinf files will be saved.
        string _destPostingFiles;
        string _mainDictionaryFilePath;

        int charValuesRange = 'z' - '-' + 1;
        int charIntervalForPostingFile;
        const int minCharValue = '-';
        public ObservableCollection<TermData> MainDictionary;
        public const string MainDictionaryFileName = "MainDictionary.zip";

        #region Inits

        public Indexer(string destPostingFiles, string mainDictionaryFilePath)
        {
            NumOfPostingFiles = 2;
            ParserFactor = 2;
            _destPostingFiles = destPostingFiles;
            InitLastRowWrittenInFile();
            InitMainDictionary();
            InitLastRowWrittenInFile();
            InitPostingFiles();
            charIntervalForPostingFile = (int)Math.Ceiling((double)charValuesRange / (double)NumOfPostingFiles);
            _mainDictionaryFilePath = mainDictionaryFilePath;
        }

        public void IndexCorpus(string corpusDirectoryPath, string stopWordsFilePath)
        {
            string[] allFileEntries = Directory.GetFiles(corpusDirectoryPath);
            int amountOfFiles = allFileEntries.Length;
            string[][] docFilesNames;
            if (amountOfFiles % ParserFactor == 0)
                docFilesNames = new string[amountOfFiles / ParserFactor][];
            else
                docFilesNames = new string[amountOfFiles / ParserFactor + 1][];
            string[] set_of_files = new string[ParserFactor];
            int lastOccupied = 0;
            for (int i = 0; i < amountOfFiles; i++)
            {
                set_of_files[lastOccupied] = allFileEntries[i];
                lastOccupied++;
                if (i % ParserFactor == ParserFactor - 1 || i == amountOfFiles - 1)
                {
                    docFilesNames[i / ParserFactor] = set_of_files;
                    set_of_files = new string[Math.Min(ParserFactor, amountOfFiles - i - 1)];
                    lastOccupied = 0;
                }

            }
            DocumentData[] docsData;
            TermFrequency[] termsFrequencies;
            int size = docFilesNames.Length;
            Parser.InitStopWords(stopWordsFilePath);
            for (int i = 0; i < size; i++)
            {
                Parser.Parse(docFilesNames[i], false, out termsFrequencies, out docsData);
                IndexParsedTerms(termsFrequencies, docsData);
                Console.WriteLine("Indexed {0}\\{1} ", i, size);
            }
            MergeSplittedDictionaries();
            SaveMainDictionaryToMemory();
            Console.WriteLine("Dictionaries wew mereged!");
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
        private void IndexParsedTerms(TermFrequency[] termsToIndex, DocumentData[] docsData)
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
                    correlatedDictionary[termFreq.Term] = new TermData(termFreq.Term, termFreq.AmountOfTotalFrequencies, postingFileName, lastRowWrittenInFile[postingFileName]);
                    lastRowWrittenInFile[postingFileName]++;
                }
                else
                {
                    correlatedDictionary[termFreq.Term].RawFrequency += termFreq.AmountOfTotalFrequencies;
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
                string[] postingFile = FileReader.ReadUtfFile(postfileDestPath);
                int sizeOfPostingFile = postingFile.Length;

                int lastTermInSameFileIndex = i;
                for (; lastTermInSameFileIndex < size - 1 && termsToIndex[lastTermInSameFileIndex + 1].PostingFileName == fileName; lastTermInSameFileIndex++) ;

                if (termsToIndex[lastTermInSameFileIndex].RowInPostFile >= postingFile.Length)
                    Array.Resize<string>(ref postingFile, termsToIndex[lastTermInSameFileIndex].RowInPostFile + 1);
                for (int j = i; j <= lastTermInSameFileIndex; j++)
                {
                    termToIndex = termsToIndex[j];
                    if (postingFile[termToIndex.RowInPostFile] == null)
                        postingFile[termToIndex.RowInPostFile] = termToIndex.FrequenciesInDocuments;
                    else
                    {
                        TermFrequency.AddFrequenciesToString(postingFile[termToIndex.RowInPostFile], termToIndex.FrequenciesInDocuments);
                    }
                }
                File.WriteAllLines(postfileDestPath, postingFile);
                i = lastTermInSameFileIndex + 1;


            }
        }

        public void MergeSplittedDictionaries()
        {
            int totoalEntriesInDictionary = 0;
            foreach (Dictionary<string, TermData> dict in splittedMainDictionary)
                totoalEntriesInDictionary += dict.Count();
            TermData[] allTerms = new TermData[totoalEntriesInDictionary];
            string[] mainDictionaryFile = new string[totoalEntriesInDictionary];
            int index = 0;

            foreach (Dictionary<string, TermData> dict in splittedMainDictionary)
            {
                var sortedDictionary = dict.Values.OrderBy(term => term.Term);
                foreach (TermData term in sortedDictionary)
                {
                    allTerms[index] = term;
                    mainDictionaryFile[index] = term.ToString();
                    index++;
                }
            }
            MainDictionary = new ObservableCollection<TermData>(allTerms);
            File.WriteAllLines(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\SortedDictionary.txt", mainDictionaryFile);
        }

        public void SaveMainDictionaryToMemory()
        {
            var formatter = new BinaryFormatter();
            string fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileName;
            System.IO.File.Delete(fullPath);
            using (var outputFile = new FileStream(fullPath, FileMode.CreateNew))
            using (var compressionStream = new GZipStream(outputFile, CompressionMode.Compress))
            {
                formatter.Serialize(compressionStream, splittedMainDictionary);
                compressionStream.Flush();
            }
        }


        private int MatchPostingFileToTerm(string term)
        {
            char firstLetter = term[0];
            int ans = (firstLetter - minCharValue) / charIntervalForPostingFile;
            return Math.Max(Math.Min(ans, NumOfPostingFiles - 1), 0);
        }

        public void LoadMainDictionaryFromMemory()
        {
            var formatter = new BinaryFormatter();
            string fullPath = _mainDictionaryFilePath + "\\" + MainDictionaryFileName;

            using (var outputFile = new FileStream(fullPath, FileMode.Open))
            using (var compressionStream = new GZipStream(
                                     outputFile, CompressionMode.Decompress))
            {
                splittedMainDictionary = (Dictionary<string, TermData>[])formatter.Deserialize(compressionStream);
                compressionStream.Flush();
            }
            MergeSplittedDictionaries();

        }
    }
}
