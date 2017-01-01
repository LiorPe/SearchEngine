using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class PostingFilesAPI
    {
        public static int NumOfPostingFiles { get; set; }
        //Path for directory in which postinf files will be saved.
        string _destPostingFiles;
        int _charValuesRange = 'z' - 'a' + 1;
        int _charIntervalForPostingFile;
        const int _minCharValue = 'a';
        // Saves what is the last row that was written in each posting file (so you can know what is the next availabe row infile)
        Dictionary<int, int> _lastRowWrittenInFile;

        public PostingFilesAPI(int numOfPostingFiles, string destPostingFiles)
        {
            NumOfPostingFiles = numOfPostingFiles;
            _destPostingFiles = destPostingFiles;
            _charIntervalForPostingFile = (int)Math.Ceiling((double)_charValuesRange / (double)NumOfPostingFiles);
            InitLastRowWrittenInFile();
        }

        //init dictionary whichmaps the posting file and the last availabe row
        private void InitLastRowWrittenInFile()
        {
            _lastRowWrittenInFile = new Dictionary<int, int>();
            for (int i = 0; i < NumOfPostingFiles; i++)
            {
                _lastRowWrittenInFile[i] = 0;
            }

        }

        //Create all files for posting files.
        public void InitPostingFiles(string mainDictionaryFileNameStemming,string mainDictionaryFileNameWithoutStemming)
        {
            if (!Directory.Exists(_destPostingFiles))  // if it doesn't exist, create
                Directory.CreateDirectory(_destPostingFiles);
            System.IO.DirectoryInfo di = new DirectoryInfo(_destPostingFiles);

            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name != mainDictionaryFileNameStemming && file.Name != mainDictionaryFileNameWithoutStemming)
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

        private int MatchPostingFileToTerm(string term)
        {
            int ans = (int)Math.Ceiling((double)(term[0] - _minCharValue) / (double)(_charIntervalForPostingFile));
            return Math.Max(Math.Min(ans, NumOfPostingFiles - 1), 0);

        }



        /// <summary>
        /// Update main dictionary and posting files for terms found in group of files.
        /// </summary>
        /// <param name="termsToIndex"></param>
        public void UpdateTermsInPostingFiles(TermFrequency[] termsToIndex, Dictionary<string, TermData>[] splittedMainDictionary)
        {
            int size = termsToIndex.Length;
            TermFrequency termFreq;
            //For each term:
            for (int termIndex = 0; termIndex < size; termIndex++)
            {
                termFreq = termsToIndex[termIndex];
                // Find the posting file where term should be saved.
                int postingFileName = MatchPostingFileToTerm(termFreq.Term);
                // Find the sub-dictionary of main dictionary where term should be saved.
                Dictionary<string, TermData> correlatedDictionary = splittedMainDictionary[postingFileName];
                // If main dictionary doesn`t contain term - create new entry
                if (!correlatedDictionary.ContainsKey(termFreq.Term))
                {
                    correlatedDictionary[termFreq.Term] = new TermData(termFreq.Term, termFreq.DocumentFrequency, termFreq.CollectionFrequency, postingFileName, _lastRowWrittenInFile[postingFileName]);
                    _lastRowWrittenInFile[postingFileName]++;
                }
                // Otherwise - update existant entry
                else
                {
                    correlatedDictionary[termFreq.Term].DocumentFrequency += termFreq.DocumentFrequency;
                    correlatedDictionary[termFreq.Term].CollectionFrequency += termFreq.CollectionFrequency;
                }
                // Assign the term its posting file name and its row in file.
                termFreq.PostingFileName = postingFileName;
                termFreq.RowInPostFile = correlatedDictionary[termFreq.Term].PtrToFile;
                termsToIndex[termIndex] = termFreq;

            }
            // Begin update posting files: sort terms by there posting files name, and then by their row in posting files.
            termsToIndex = termsToIndex.OrderBy(term => term.PostingFileName).ThenBy(term => term.RowInPostFile).ToArray<TermFrequency>();
            int i = 0;
            TermFrequency termToIndex;
            // For each term:
            while (i < size)
            {

                termToIndex = termsToIndex[i];

                int fileName = termToIndex.PostingFileName;
                // Rename the exiting posting file, and create a new file which will replace current file.
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
                // Read from old posting file, and write to new one:
                using (BinaryReader sourcePostingFile = new BinaryReader(File.Open(duplicatePostingFileDestPath, FileMode.Open)))
                {
                    using (BinaryWriter targetPostingFile = new BinaryWriter(File.Create(postfileDestPath)))
                    {
                        // As long terms belong to same file:
                        while (i < size && (termToIndex = termsToIndex[i]).PostingFileName == fileName)
                        {
                            int rowToUpdate = termToIndex.RowInPostFile;
                            // write all rows from posting file which doesn`t need to be updated to new file.
                            while (sourcePostingFile.BaseStream.Position != sourcePostingFile.BaseStream.Length & fileCursor < rowToUpdate)
                            {
                                sourcePostingFileEntry = sourcePostingFile.ReadString();
                                targetPostingFile.Write(sourcePostingFileEntry);
                                fileCursor++;
                            }
                            // If old file isn`t over -  read the entry of term in old posting file, update it, and write it to new posting file.
                            if (sourcePostingFile.BaseStream.Position != sourcePostingFile.BaseStream.Length)
                            {
                                sourcePostingFileEntry = sourcePostingFile.ReadString();
                                sourcePostingFileEntry = TermFrequency.AddFrequenciesToString(sourcePostingFileEntry, termToIndex.FrequenciesInDocuments);
                                targetPostingFile.Write(sourcePostingFileEntry);
                                fileCursor++;
                            }
                            // If file is over - it means that no previous entry in file of this term  exists - write the frequencies of current group of files.
                            else
                            {
                                targetPostingFile.Write(termToIndex.FrequenciesInDocuments);
                            }
                            i++;


                        }
                    }
                }
                // Remove old version of file.
                File.Delete(duplicatePostingFileDestPath);
            }

        }

        public Dictionary<string, PostingFileRecord> ExtractPostingFileRecords(string[] termsToExtractFromPostingFiles, Dictionary<string, TermData>[] _splittedMainDictionary, bool generateAutoCompletion)
        {
            Dictionary<string, PostingFileRecord> extractedRecordsFromPostingFiles = new Dictionary<string, PostingFileRecord>();
            List<TermData> termsRecordsFromMainDictionary = new List<TermData>();
            foreach (string term in termsToExtractFromPostingFiles)
            {
                int dictionaryNumber = MatchPostingFileToTerm(term);
                if (_splittedMainDictionary[dictionaryNumber].ContainsKey(term))
                {
                    termsRecordsFromMainDictionary.Add(_splittedMainDictionary[dictionaryNumber][term]);
                }
            }
            TermData[] sortedTermsToExtract = termsRecordsFromMainDictionary.OrderBy(term => term.PostingFileName).ThenBy(term => term.PtrToFile).ToArray<TermData>();
            int i = 0;
            int size = sortedTermsToExtract.Length;
            TermData termToExtract;
            while (i < size)
            {

                termToExtract = sortedTermsToExtract[i];

                int fileName = termToExtract.PostingFileName;
                int fileCursor = 0;
                string postingFileEntry = null;
                string postfileDestPath = _destPostingFiles + "\\" + fileName + ".txt";

                // Read from old posting file, and write to new one:
                using (BinaryReader sourcePostingFile = new BinaryReader(File.Open(postfileDestPath, FileMode.Open)))
                {
                    // As long terms belong to same file:
                    while (i < size && (termToExtract = sortedTermsToExtract[i]).PostingFileName == fileName)
                    {
                        int rowToUpdate = termToExtract.PtrToFile;
                        // write all rows from posting file which doesn`t need to be updated to new file.
                        while (sourcePostingFile.BaseStream.Position != sourcePostingFile.BaseStream.Length & fileCursor < rowToUpdate)
                        {
                            sourcePostingFile.ReadString();
                            fileCursor++;
                        }
                        // If old file isn`t over -  read the entry of term in old posting file, update it, and write it to new posting file.
                        if (sourcePostingFile.BaseStream.Position != sourcePostingFile.BaseStream.Length)
                        {
                            postingFileEntry = sourcePostingFile.ReadString();
                            extractedRecordsFromPostingFiles[termToExtract.Term] = TermFrequency.DeseralizePostingFileRecord(postingFileEntry, generateAutoCompletion);
                            fileCursor++;
                        }
                        i++;


                    }
                }
            }
            return extractedRecordsFromPostingFiles;
        }
    }
}
