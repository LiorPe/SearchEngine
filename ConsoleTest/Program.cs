using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchEngine;

namespace ConsoleTest
{
    class Program
    {
            static void Main(string[] args)
        {
                int[] PostingFilesAmount = new int[] { 2 };
                int[] ParserFactor = new int[] { 2 };


                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

            //CreeateIndexForTheFirstTime();
            //LoadIndexFromMemory();
            Parse("$104bn");

            stopWatch.Stop();

                // Get the elapsed time as a TimeSpan value.
                TimeSpan ts = stopWatch.Elapsed;
                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                string result = string.Format("Duration: {0}", elapsedTime);
                Console.WriteLine(result);

                Console.ReadKey();



            }
            public static void CreeateIndexForTheFirstTime()
        {
            Indexer indexer = new Indexer(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles", @"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles");
            indexer.IndexCorpus(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\corpus", @"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\stop_words.txt",false);
        }

        public static void LoadIndexFromMemory()
        {
            Indexer indexer = new Indexer(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles", @"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\postingFiles");
            indexer.LoadMainDictionaryFromMemory();
        }
        public static void Parse(string s)
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
            Parser.ActivateDerivationLaws(ref s, file, ref fileIndexer, ref tokenRecursivelyParsed, ref countFrequenciesSeperately, false, ref documentLength, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
        }
    
    }
}
