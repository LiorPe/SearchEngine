using System;
using System.Collections;
using System.Collections.Generic;

namespace SearchEngine
{

    // Represents term frequencies in group of files read to memory (before indexing)
    public class TermFrequency
    {
        public string Term { get; set; }
        private static char InterRecerodDelimiter = '|';
        private static char IntraRecerodDelimiter = '*';

        // How many times the term appeared in documents
        public int CollectionFrequency;
        // In how many documents the term appeared.
        public int DocumentFrequency;
        private string frequenciesInDocuments;
        public string FrequenciesInDocuments { get { return frequenciesInDocuments; }  }
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
            frequenciesInDocuments = SeralizeDocumentInformationToString(documentNumner, df, nextTerms, true);
            CollectionFrequency += df;
        }

        public void AddFrequencyInDocument(string documentNumner, int df, Dictionary<string,int> nextTerms)
        {

            frequenciesInDocuments += frequenciesInDocuments = SeralizeDocumentInformationToString(documentNumner, df, nextTerms, true);
            CollectionFrequency += df;
            DocumentFrequency++;


        }

        private static string SeralizeDocumentInformationToString(string documentNumner, int df, Dictionary<string, int> nextTerms, bool firstDocument)
        {
            string completionTerms = String.Empty;
            foreach (string nextTerm in nextTerms.Keys)
            {
                completionTerms += String.Format("{0}{1}{2}{1}", nextTerm, IntraRecerodDelimiter, nextTerms[nextTerm]);
            }
            if (!firstDocument)
                return InterRecerodDelimiter + documentNumner + IntraRecerodDelimiter + df + IntraRecerodDelimiter + completionTerms;
            else
                return documentNumner + IntraRecerodDelimiter + df + IntraRecerodDelimiter + completionTerms; 
        }


        public static string AddFrequenciesToString(string sourceFrequencies, string freqToAdd)
        {

            return sourceFrequencies += IntraRecerodDelimiter + freqToAdd;

        }

    }



}