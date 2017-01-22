using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SearchEngine
{

    // Represents term frequencies in group of files read to memory (before indexing)
    public class TermFrequency
    {
        public string Term { get; set; }
        private static char InterRecerodDelimiter = '\t';
        private static char IntraRecerodDelimiter = '|';

        // How many times the term appeared in documents
        public int CollectionFrequency;
        // In how many documents the term appeared.
        public int DocumentFrequency;
        private string frequenciesInDocuments;
        public string FrequenciesInDocuments { get { return frequenciesInDocuments; } }
        private int postingFileName;

        public int PostingFileName
        {
            get
            {
                return postingFileName;
            }
            set
            {
                postingFileName = value;
            }
        }
        int rowInPostFile;
        public int RowInPostFile
        {
            get
            {
                return rowInPostFile;
            }
            set
            {
                rowInPostFile = value;
            }
        }

        public TermFrequency(string term, string documentNumner, int df, Dictionary<string, int> nextTerms)
        {
            Term = term;
            DocumentFrequency = 1;
            frequenciesInDocuments = SeralizeDocumentInformationToString(documentNumner, df, nextTerms);
            CollectionFrequency += df;
        }

        public void AddFrequencyInDocument(string documentNumner, int df, Dictionary<string, int> nextTerms)
        {

            frequenciesInDocuments += ( InterRecerodDelimiter+ SeralizeDocumentInformationToString(documentNumner, df, nextTerms) );
            CollectionFrequency += df;
            DocumentFrequency++;


        }

        private static string SeralizeDocumentInformationToString(string documentNumner, int df, Dictionary<string, int> nextTerms)
        {
            string completionTerms = String.Empty;
            foreach (string nextTerm in nextTerms.Keys)
            {
                if (!String.IsNullOrEmpty(nextTerm))
                    completionTerms += String.Format("{0}{1}{2}{1}", nextTerm, IntraRecerodDelimiter, nextTerms[nextTerm]);
                //to remove
                if (nextTerm.IndexOf("FBIS") >= 0)
                {
                }
            }
           return documentNumner + IntraRecerodDelimiter + df + IntraRecerodDelimiter + completionTerms;

        }


        public static string AddFrequenciesToString(string sourceFrequencies, string freqToAdd)
        {

            return sourceFrequencies += InterRecerodDelimiter + freqToAdd;

        }

        internal static PostingFileRecord DeseralizePostingFileRecord(string postingFileEntry, bool searchAutoComplete)
        {
            PostingFileRecord postingRecord = new PostingFileRecord();
            postingRecord.NextTermFrequencies = new Dictionary<string, Dictionary<string, int>>();
            postingRecord.NextTermInAllDocuments = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> nextTermFrequencies = postingRecord.NextTermFrequencies;
            string[] termDataSplittedByDocuments = postingFileEntry.Split(InterRecerodDelimiter);
            foreach (string termDataInOneDocument in termDataSplittedByDocuments)
            {
                string[] attributes = termDataInOneDocument.Split(IntraRecerodDelimiter);
                string docName = attributes[0];
                int DF = Int32.Parse(attributes[1]);
                postingRecord.DF[docName] = DF;
                nextTermFrequencies[docName] = new Dictionary<string, int>();
                Dictionary<string, int> currentDoucmentNextTokens = nextTermFrequencies[docName];

                string nextTerm;
                int frequency;
                int i = 2;
                while (i + 1 < attributes.Length)
                {
                    nextTerm = attributes[i];
                    if (!String.IsNullOrEmpty(nextTerm))
                    {
                        frequency = Int32.Parse(attributes[i + 1]);
                        if (nextTermFrequencies.ContainsKey(nextTerm))
                            currentDoucmentNextTokens[nextTerm] += frequency;
                        else
                            currentDoucmentNextTokens[nextTerm] = frequency;
                        if (searchAutoComplete)
                        {
                            if (postingRecord.NextTermInAllDocuments.ContainsKey(nextTerm))
                                postingRecord.NextTermInAllDocuments[nextTerm] += frequency;
                            else
                                postingRecord.NextTermInAllDocuments[nextTerm] = frequency;
                        }
                        i += 2;
                    }
                    else
                        i += 1;
                }


            }
        
            if (searchAutoComplete)
           {
                var sortedNextTermsByFrequency = postingRecord.NextTermInAllDocuments.ToList();

                sortedNextTermsByFrequency.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
                postingRecord.NextTermInAllDocuments = new Dictionary<string, int>();
                int numOfSuggestion = Math.Min(5, sortedNextTermsByFrequency.Count);
                for (int i = 0; i < numOfSuggestion; i++)
                {
                    KeyValuePair<string, int> nextTermFrequency = sortedNextTermsByFrequency[i];
                    postingRecord.NextTermInAllDocuments[nextTermFrequency.Key] = nextTermFrequency.Value;
                }
            }
            return postingRecord;
        }
    }



}