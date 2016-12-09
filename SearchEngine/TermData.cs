using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace SearchEngine
{
    [Serializable]
    public class TermData
    {
        public string Term { get; set; }
        public int RawFrequency { get; set; }
        int PostingFileName { get; set; }
        public int PtrToFile { get; set; }
        public string CourpusFileName { get; set; }

        public TermData(string term, int rawFrequency, int fileName, int ptrToPostingFile,string courpusFileName)
        {
            Term = term;
            RawFrequency = rawFrequency;
            PtrToFile = ptrToPostingFile;
            PostingFileName = fileName;
            CourpusFileName = courpusFileName;
        }
        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}\t{4}", Term, RawFrequency, PostingFileName, PtrToFile, CourpusFileName);
        }
    }
}