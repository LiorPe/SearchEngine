﻿using System;

namespace SearchEngine
{
    public class TermFrequency
    {
        public string Term { get; set; }
        private static char Delimiter = '~';
        public int AmountOfTotalFrequencies;
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

        public TermFrequency(string term, string documentNumner, int amountOfFtrequncies)
        {
            Term = term;
            frequenciesInDocuments = string.Format("{0} {1}", documentNumner, amountOfFtrequncies);
            AmountOfTotalFrequencies += amountOfFtrequncies;

        }

        public TermFrequency(string dataFromPostinfFiles)
        {
            string[] attributes = dataFromPostinfFiles.Split(Delimiter);
            Term = attributes[0];
            AmountOfTotalFrequencies = Int32.Parse(attributes[1]);
            frequenciesInDocuments = attributes[2];
        }

        public void AddFrequencyInDocument(string documentNumner, int amountOfFtrequncies)
        {
            frequenciesInDocuments = frequenciesInDocuments + string.Format("\t{0} {1}", documentNumner, amountOfFtrequncies);
            
            AmountOfTotalFrequencies += amountOfFtrequncies;


        }

        public override string ToString() {
            string res = string.Format("{0}{1}{2}{1}{3}", Term, Delimiter, AmountOfTotalFrequencies, frequenciesInDocuments);
            return res;


        }

        public static string AddFrequenciesToString(string sourceFrequencies, string freqToAdd)
        {
            return sourceFrequencies += '\t' + freqToAdd;

        }

    }



}