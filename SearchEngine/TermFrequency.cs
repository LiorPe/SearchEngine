using System;

namespace SearchEngine
{
    public class TermFrequency
    {
        public string Term { get; set; }
        private static char Delimiter = '~';
        public int AmountOfTotalFrequencies;
        private string frequenciesInDocuments;
        public string FrequenciesInDocuments { get { return frequenciesInDocuments; }  }
        public string CorpusFileName { get; set; }
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

        public TermFrequency(string term, string documentNumner, int amountOfFtrequncies,string corpusFileName)
        {
            Term = term;
            frequenciesInDocuments = string.Format("{0} {1}", documentNumner, amountOfFtrequncies);
            AmountOfTotalFrequencies += amountOfFtrequncies;
            CorpusFileName = corpusFileName;
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