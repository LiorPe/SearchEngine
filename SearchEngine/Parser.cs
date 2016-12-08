using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SearchEngine
{
    public static class Parser
    {
        #region Attributes
        public static int counter = 0;
        static readonly string DocNumberOpeningTag = "<DOCNO>";
        static readonly string DocLanguageOpeningTag = "P=105>";
        static readonly string BeginningOfTextTag = "<TEXT>";
        static readonly string EndOfTextTag = "</TEXT>";
        public static HashSet<string> StopWords= null;
        private readonly static char[] SuffixToRemove = { '.', ',', '!', '?', ')', '\'', '"', ';' };
        private readonly static char[] prefixToRemove = { '\'', '"', '(' };
        private static readonly Dictionary<string, string> monthes = new Dictionary<string, string>
        {
            {"january","01" },
            {"february","02" },
            {"march","03" },
            {"april","04" },
            {"may","05" },
            {"june","06" },
            {"july","07" },
            {"august","08" },
            {"september","09" },
            {"october","10" },
            {"novomber","11" },
            {"december","12" },
        };

        private static readonly Dictionary<string, double> largeNumbers = new Dictionary<string, double>
        {
            {"milion",1 },
            {"billion",(int)1E3},
            {"trilion",(int)1E6},
        };

        #endregion
        public static void Parse(string[] filePathes, bool useStemming,out TermFrequency[] termsToIndex,out DocumentData[] DocsDats)
        {


            Dictionary<string, TermFrequency> postingFile = new Dictionary<string, TermFrequency>();
            Dictionary<string, DocumentData> documentsData = new Dictionary<string, DocumentData>();
            int numOfFiles = filePathes.Length;
            for (int i = 0; i < numOfFiles; i++)
            {
                string[] file = FileReader.ReadTextFile(filePathes[i]);
                int fileLength = file.Length;
                int fileIndexer = 0;
                while (fileIndexer < fileLength)
                {
                    string docNo = GetDocNummer(file, ref fileIndexer);
                    if (fileIndexer == fileLength)
                        break;
                    string docLanguage = GetLanguage(file, ref fileIndexer);
                    string mostFrequentTerm = "";
                    int documentLength = 0;
                    int frquenciesOfMostFrequentTerm = 0;
                    Dictionary<string, int> termFrequencies = new Dictionary<string, int>();


                    FindBegginingOfText(file, ref fileIndexer);

                    while (!ReachedTODocumentEnd(file, fileIndexer))
                    {
                        fileIndexer++;
                        MoveIndexToNextToken(ref fileIndexer, file);
                        string token = file[fileIndexer];

                        token = NormalizeToken(token);

                        if (!IsAStopWord(token) && !EliminatedByCustomedRules(token))
                        {

                            bool termRepresentANumber = ActivateDerivationLaws(ref token, file, ref fileIndexer);
                            if (useStemming & !termRepresentANumber)
                                token = ActivateStemming(token);
                            documentLength++;
                            UpdateFrequencies(token, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
                        }
                    }
                    documentsData[docNo] = new DocumentData(docNo, mostFrequentTerm, frquenciesOfMostFrequentTerm, termFrequencies.Keys.Count, docLanguage, documentLength);
                    UpdatePostingFile(termFrequencies, postingFile, docNo);



                }
            }
            termsToIndex = postingFile.Values.ToArray<TermFrequency>();
            DocsDats = documentsData.Values.ToArray<DocumentData>();

        }

        private static void UpdatePostingFile(Dictionary<string, int> termFrequencies, Dictionary<string, TermFrequency> postingFile,string docNumber)
        {
            foreach(string term in termFrequencies.Keys)
            {
                if (postingFile.ContainsKey(term)){
                    postingFile[term].AddFrequencyInDocument(docNumber, termFrequencies[term]);
                }
                else
                {
                    postingFile[term] = new TermFrequency(term, docNumber, termFrequencies[term]);
                }

            }
        }


        private static void PrintOutputt(DocumentData documentData, Dictionary<string, int> termFrequencies)
        {
            Console.WriteLine("\"{0}\" is the most frequent term, and it appears {1} times. There are {2} unique terms, and its language is:{3}. Document`s length: {4}.", documentData.MostFrequentTerm, documentData.FrquenciesOfMostFrequentTerm, documentData.AmmountOfUniqueTerms, documentData.Language, documentData.DocumentLength);
            Console.WriteLine("Press Enter to continue.");
            Console.ReadLine();
            foreach (string term in termFrequencies.Keys)
                Console.WriteLine(String.Format("{0} , {1}", term, termFrequencies[term]));

        }

        private static void MoveIndexToNextToken(ref int fileIndexer, string[] file)
        {
            while (file[fileIndexer] == "")
                fileIndexer++;
        }

        public static void InitStopWords(string stopWordsFilePath)
        {
            string[] stopWords = File.ReadAllText(@"C:\Users\ליאור\Documents\לימודים\סמסטר ה'\אחזור מידע\מנוע\stop_words.txt").Split(new char[] { '\n', '\r' });
            StopWords = new HashSet<string>(stopWords);
        }

        private static void UpdateFrequencies(string term, Dictionary<string, int> termFrequencies, ref int frquenciesOfMostFrequentTerm, ref string mostFrequentTerm)
        {
            if (!termFrequencies.ContainsKey(term))
                termFrequencies[term] = 1;
            else
                termFrequencies[term]++;

            if (termFrequencies[term] > frquenciesOfMostFrequentTerm)
            {
                frquenciesOfMostFrequentTerm = termFrequencies[term];
                mostFrequentTerm = term;
            }
        }

        private static string ActivateStemming(string term)
        {
            throw new NotImplementedException();
        }




        /// <summary>
        /// Get document number 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileIndexer"></param>
        /// <returns> string of file number</returns>
        private static string GetDocNummer(string[] file, ref int fileIndexer)
        {
            //Reach to opening tag of doc number
            for (; fileIndexer < file.Length && file[fileIndexer] != DocNumberOpeningTag; fileIndexer++) ;
            // if got to the end of file - break
            if (fileIndexer >= file.Length)
                return "";
            //Move to doc number 
            fileIndexer++;
            string docNo = file[fileIndexer];
            // Skip on closing tag of doc numner
            fileIndexer = fileIndexer + 2;
            return docNo;
        }

        /// <summary>
        /// Get document language
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileIndexer"></param>
        /// <returns></returns>
        private static string GetLanguage(string[] file, ref int fileIndexer)
        {
            //Reach to opening tag of doc number
            for (; file[fileIndexer] != DocLanguageOpeningTag && file[fileIndexer] != BeginningOfTextTag; fileIndexer++) ;
            //If reached to begginning of text (which means this doc doesn`t have language tag)
            if (file[fileIndexer] == BeginningOfTextTag)
            {
                return "";
            }
            else
            {
                //Move to doc language tag 
                fileIndexer++;
                string language = file[fileIndexer];
                // Skip on closing tag of doc language tag
                fileIndexer = fileIndexer + 2;
                return language;
            }

        }

        /// <summary>
        ///  Move indexer to bginning of text 
        /// </summary>
        private static void FindBegginingOfText(string[] file, ref int fileIndexer)
        {
            for (; file[fileIndexer] != BeginningOfTextTag; fileIndexer++) ;
        }

        /// <summary>
        /// check if index points to end of text tage
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileIndexer"></param>
        /// <returns></returns>
        private static bool ReachedTODocumentEnd(string[] file, int fileIndexer)
        {
            return (file[fileIndexer] == EndOfTextTag);
        }


        #region Methods for Parsing
        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        private static string NormalizeToken(string token)
        {
            token = token.TrimEnd(SuffixToRemove).TrimStart(prefixToRemove).ToLower();
            return token;
        }



        /// <summary>
        /// Check if a word is a stop word
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsAStopWord(string token)
        {
            return StopWords.Contains(token);
        }

        /// <summary>
        /// check if token can be eliminated 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool EliminatedByCustomedRules(string token)
        {
            bool ans = false;
            if (token[0] == '<' || token[token.Length - 1] == '>')
                ans = true;

            return ans;
        }


        private static bool ActivateDerivationLaws(ref string token, string[] file, ref int fileIndexer)
        {
            if (Char.IsDigit(token[0]))
            {
                ActivateDerivationLawsForNumbers(token, file, ref fileIndexer);
                return true;
            }
            else
            {
                ActivateDerivationLawsForWords(token, file, ref fileIndexer);
                return false;
            }
        }


        #endregion
        #region Derivation Laws For Words


        private static void ActivateDerivationLawsForWords(string token, string[] file, ref int fileIndexer)
        {
            // if an expression - leave it like this
            if (token.Contains("-"))
            {
                return;
            }
            //if begins with $ - move the $ to the end of word, and activate rules for numbers
            if (token.Substring(0, 1) == "$")
            {
                token = token.Substring(1, token.Length - 1) + "$";
                ActivateDerivationLawsForNumbers(token, file, ref fileIndexer);
            }
            token = NormalizeToken(token);
            // if it`s a date
            if (monthes.ContainsKey(token))
            {
                int value;
                file[fileIndexer + 1] = NormalizeToken(file[fileIndexer + 1]);
                bool valueIsANumber = Int32.TryParse(file[fileIndexer + 1], out value);
                //if number after month is a day/year
                if (valueIsANumber)
                {
                    //if it represents day
                    if (value <= 31)
                    {
                        token = token + "-" + file[fileIndexer + 1];
                        fileIndexer++;
                    }
                    //if it represents year
                    else
                    {
                        token = value + "-" + token;
                        fileIndexer++;
                    }


                }
            }

        }
        #endregion
        #region Derivation Laws For Numbers
        private static void ActivateDerivationLawsForNumbers(string token, string[] file, ref int fileIndexer)
        {

            // if token is expression with hyphen
            if (token.Contains("-"))
            {
                return;
            }
            double numericalValue;
            // if this is not an expression
            if (!token.Contains("/"))
            {
                //Check for suffix
                string suffix = ExtractSuffix(ref token);
                numericalValue = ExtractNumber(ref token, suffix, file, ref fileIndexer);


                //if represents a date
                if (numericalValue <= 31 && monthes.ContainsKey(file[fileIndexer + 1]))
                {
                    token = monthes[file[fileIndexer + 1]] + "-" + numericalValue;
                    fileIndexer++;
                    // Check if next word is a year
                    int year = 0;
                    if (Int32.TryParse(NormalizeToken(file[fileIndexer + 1]), out year))
                    {
                        if (year < 100)
                            year += 1900;
                        token = year + "-" + token;
                        fileIndexer++;
                    }
                    return;
                }
                if (suffix == "%" || NormalizeToken(file[fileIndexer + 1]) == "percent" || NormalizeToken(file[fileIndexer + 1]) == "percentage")
                {
                    token = token + "%";
                    fileIndexer++;
                }
                if (suffix == "$" || NormalizeToken(file[fileIndexer + 1]) == "dollars")
                {
                    token = token + "Dollars";
                    fileIndexer++;
                }
                if (NormalizeToken(file[fileIndexer + 1]) == "us" && NormalizeToken(file[fileIndexer + 2]) == "dollars")
                {
                    token = token + "Dollars";
                    fileIndexer += 2;
                }

            }

        }

        private static double ExtractNumber(ref string token, string suffix, string[] file, ref int fileIndexer)
        {

            double numericalValue;
            bool parseSucceeded = Double.TryParse(token, out numericalValue);
            if (!parseSucceeded)
            {
                int firstDigitIndex = 0;
                for (int i = 0; i < token.Length; i++)
                {
                    if (Char.IsLetter(token[i]))
                    {
                        firstDigitIndex = i;
                        break;
                    }

                }
                token = token.Substring(0, firstDigitIndex);
            }
            // if number is Bigger than milion
            if (numericalValue > 1E6)
            {
                token = numericalValue / 1E6 + " M";

            }
            if (suffix == "m")
                token = token + " M";
            if (suffix == "bn")
            {
                token = numericalValue * (int)1E3 + " M";
            }
            // if a fraction follow number
            if (file[fileIndexer + 1].Contains("/"))
            {
                string[] fraction = file[fileIndexer + 1].Split(new char[] { '\\' });
                int value = 0;
                if (fraction.Length == 2 && Int32.TryParse(fraction[0], out value) && Int32.TryParse(fraction[1], out value))
                {
                    token = token + file[fileIndexer + 1];
                    fileIndexer++;
                }

            }
            // if number follows a large number in word
            if (largeNumbers.ContainsKey(NormalizeToken(file[fileIndexer + 1])))
            {
                fileIndexer++;
                token = "" + (int)(numericalValue * largeNumbers[NormalizeToken(file[fileIndexer])]) + " M";
            }

            return numericalValue;
        }

        private static string ExtractSuffix(ref string token)
        {
            //remove th from the end of number if exists 
            if (token.Length >= 2)
            {
                if (token.Substring(token.Length - 2).ToLower() == "th")
                {
                    token = token.Substring(0, token.Length - 2);
                    return "th";
                }
                if (token.Substring(token.Length - 2).ToLower() == "st")
                {
                    token = token.Substring(0, token.Length - 2);
                    return "st";
                }
                if (token.Substring(token.Length - 2).ToLower() == "bm")
                {
                    token = token.Substring(0, token.Length - 2);
                    return "bn";
                }
            }
            if (token.Length > 1)
            {
                if (token.Substring(token.Length - 1) == "%")
                {
                    token = token.Substring(0, token.Length - 1);
                    return "%";
                }
                if (token.Substring(token.Length - 1) == "$")
                {
                    token = token.Substring(0, token.Length - 1);
                    return "$";
                }
                if (token.Substring(token.Length - 1) == "m")
                {
                    token = token.Substring(0, token.Length - 1);
                    return "m";
                }
            }


            return "";
        }
        #endregion
    }


}

