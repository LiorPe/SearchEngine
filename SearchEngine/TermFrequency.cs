using System;
using System.Collections;
using System.Collections.Generic;

namespace SearchEngine
{

    // Represents term frequencies in group of files read to memory (before indexing)
    public class TermFrequency
    {
        public string Term { get; set; }
        private static char Delimiter = '~';
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

        public TermFrequency(string term, string documentNumner, int collectionFrequency)
        {
            Term = term;
            DocumentFrequency = 1;
            frequenciesInDocuments = string.Format("{0} {1}", documentNumner, collectionFrequency);
            CollectionFrequency += collectionFrequency;
        }

        public void AddFrequencyInDocument(string documentNumner, int amountOfFtrequncies)
        {
        
            frequenciesInDocuments = frequenciesInDocuments + string.Format("\t{0} {1}", documentNumner, amountOfFtrequncies);
            
            CollectionFrequency += amountOfFtrequncies;
            DocumentFrequency++;


        }

        public override string ToString() {
            string res = string.Format("{0}{1}{2}{1}{3}", Term, Delimiter, CollectionFrequency, frequenciesInDocuments);
            return res;


        }

        public static string AddFrequenciesToString(string sourceFrequencies, string freqToAdd)
        {

            return sourceFrequencies += '\t' + freqToAdd;

        }

    }



}