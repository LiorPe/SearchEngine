using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchEngine;
using System.IO;

namespace ConsoleTest
{
    class Program
    {
            static void Main(string[] args)
        {
                int[] PostingFilesAmount = new int[] { 2,4,6,8,10,12,14 };
                int[] ParserFactor = new int[] { 2, 4, 6, 8, 10, 12, 14 };

            List<string> results = new List<string>();
            foreach (int parser in ParserFactor)
            {
                foreach (int post in PostingFilesAmount)
                {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();
                    Indexer indexer;
                    CreeateIndexForTheFirstTime(out indexer,parser,post);
                    stopWatch.Stop();
                    // Get the elapsed time as a TimeSpan value.
                    TimeSpan ts = stopWatch.Elapsed;
                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    string result = string.Format("Duration:{0}\tNumber of corpus files to read to memory:{1}\tNumber of posting files:{2}", elapsedTime,parser,post);
                    results.Add(result);
                    results.Sort();
                    File.WriteAllLines("results.txt", results.ToArray());
                    Console.WriteLine(result);

                }
            }
             //LoadIndexFromMemory(out indexer);
            //ParseFile("case--different	");
            //ParseFile(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\corpus\FB396161");





        }
        public static void CreeateIndexForTheFirstTime(out Indexer indexer,int parser=10,int post=10)
        {
            indexer = new Indexer(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles", @"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles",Mode.Create,parser,post);
            indexer.IndexCorpus(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\corpus", @"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\corpus\stop_words.txt", false);
        }

        public static void LoadIndexFromMemory(out Indexer indexer)
        {
            indexer = new Indexer(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles", @"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles",Mode.Load);
            indexer.LoadMainDictionaryFromMemory(false);
        }
        public static void DerivationLaws(string s)
        {
            int fileIndexer = 0;
            string[] file = new string[]{ s };
            int documentLength = 0;
            Dictionary<string, int> termFrequencies = new Dictionary<string, int>();
            int frquenciesOfMostFrequentTerm = 0;
            string mostFrequentTerm = String.Empty;
            bool tokenRecursivelyParsed = false;
            bool countFrequenciesSeperately = false;

            Parser.InitStopWords(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\stop_words.txt");
            s = Parser.NormalizeToken(s);
            Parser.ActivateDerivationLaws(ref s, file, ref fileIndexer, ref tokenRecursivelyParsed, ref countFrequenciesSeperately, false, ref documentLength, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
        }

        public static void ParseFile(string word)
        {
            int fileIndexer = 0;
            string[] file = new string[] { word };
            int documentLength = 0;
            Dictionary<string, int> termFrequencies = new Dictionary<string, int>();
            int frquenciesOfMostFrequentTerm = 0;
            string mostFrequentTerm = String.Empty;
            bool tokenRecursivelyParsed = false;
            bool countFrequenciesSeperately = false;

            Parser.InitStopWords(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\stop_words.txt");
            Parser.IterateTokens(ref fileIndexer, file, false, ref documentLength, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
        }

    }
}
