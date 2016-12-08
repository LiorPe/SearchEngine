using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace SearchEngine
{
    [Serializable]
    public class TermData
    {
        public string Term { get; set; }
        public int RawFrequency { get; set; }
        int FileName { get; set; }
        public int PtrToFile { get; set; }

        public TermData(string term, int rawFrequency, int fileName, int ptrToPostingFile)
        {
            Term = term;
            RawFrequency = rawFrequency;
            PtrToFile = ptrToPostingFile;
            FileName = fileName;
        }
        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}", Term, RawFrequency, FileName, PtrToFile);
        }
    }
}