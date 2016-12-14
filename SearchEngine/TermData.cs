using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace SearchEngine
{
    [Serializable]
    public class TermData
    {
        public string Term { get; set; }
        // How many times the term appeared in all documents
        public int CollectionFrequency { get; set; }
        // In how many documents the term appeared.
        public int DocumentFrequency { get; set; }
        public int PostingFileName { get; set; }
        public int PtrToFile { get; set; }

        public TermData(string term,int documentFrequncy, int collectionFrequency, int fileName, int ptrToPostingFile)
        {
            Term = term;
            DocumentFrequency = documentFrequncy;
            CollectionFrequency = collectionFrequency;
            PtrToFile = ptrToPostingFile;
            PostingFileName = fileName;
        }
        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}\t(4)", Term,DocumentFrequency, CollectionFrequency, PostingFileName, PtrToFile);
        }
    }
}