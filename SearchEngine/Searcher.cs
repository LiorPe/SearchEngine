using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class Searcher
    {
        // Main dictionarry of terms - saves amountt of total frequencies in all docs, name of file (posting file) in which term is saved, and
        // ptr to file (row number in which term is stored)
        Dictionary<string, TermData>[] _splittedMainDictionary;
        private Dictionary<string, DocumentData> _documnentsData;
        //How many different posting files exist.
        public static int NumOfPostingFiles { get; set; }
        public int ParserFactor { get; set; }
        //Path for directory in which postinf files will be saved.
        string _destPostingFiles;
        int _charValuesRange = 'z' - 'a' + 1;
        int _charIntervalForPostingFile;
        const int _minCharValue = 'a';

        public Searcher(Dictionary<string, TermData>[] splittedMainDictionary, Dictionary<string, DocumentData> documnentsData, int numOfPostingFiles, int parserFactor, string destPostingFiles)
        {
            _splittedMainDictionary = splittedMainDictionary;
            _documnentsData = documnentsData;
            NumOfPostingFiles = numOfPostingFiles;
            ParserFactor = parserFactor;
            _destPostingFiles = destPostingFiles;
            _charIntervalForPostingFile = (int)Math.Ceiling((double)_charValuesRange / (double)NumOfPostingFiles);
        }

        private int MatchPostingFileToTerm(string term)
        {
            int ans = (int)Math.Ceiling((double)(term[0] - _minCharValue) / (double)(_charIntervalForPostingFile));
            return Math.Max(Math.Min(ans, NumOfPostingFiles - 1), 0);

        }

        public List<string> GetCompletionSuggestions(string userQuery)
        {
            Dictionary<string, PostingFileRecord> postingFileRecord = ExtractPostingFileRecords(new string[] { userQuery },true);
            List<string> completionSuggestions = new List<string>();
            if (postingFileRecord.ContainsKey(userQuery))
                completionSuggestions = postingFileRecord[userQuery].NextTermFrequencies.Keys.ToList();
            return completionSuggestions;

        }

        private Dictionary<string, PostingFileRecord> ExtractPostingFileRecords(string[] terms, bool generateAutoCompletion)
        {
            Dictionary<string, PostingFileRecord> extractedRecordsFromPostingFiles = new Dictionary<string, PostingFileRecord>();
            List<TermData> termsRecordsFromMainDictionary = new List<TermData>();
            foreach (string term in terms)
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
