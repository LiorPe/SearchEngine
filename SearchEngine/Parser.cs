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
        public static HashSet<string> StopWords = null;
        private readonly static char[] SuffixToRemove = { '-', '~', '`', ';', '!', '@', '#', '^', '&', '*', '(', ')', '=', '+', '[', ']', '{', '}', '\'', '"', '?', '/', '>', ',', '.', ':' };
        private readonly static char[] prefixToRemove = { '|', '~', '`', ';', '!', '@', '#', '^', '&', '*', '(', ')', '=', '+', '[', ']', '{', '}', '\'', '"', '?', '/', '<', ',', '.', '%', '-', ':' };
        private readonly static char[] delimiters = { '~', '`', ';', '!', '@', '#', '^', '&', '*', '(', ')', '=', '+', '[', ']', '{', '}', '\'', '"', '?', '/', '<', '>', ',', '-', '.', ':' };
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
            {"november","11" },
            {"december","12" },
        };

        private static readonly Dictionary<string, double> largeNumbers = new Dictionary<string, double>
        {
            {"milion",1 },
            {"billion",(int)1E3},
            {"trilion",(int)1E6},
        };
        //to remove:
        public static string lastToken;
        #endregion
        public static void Parse(string[] filePathes, bool useStemming, out TermFrequency[] termsToIndex, out DocumentData[] DocsDats)
        {

            string s = "";
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
                    IterateTokens(ref fileIndexer, file, useStemming, ref documentLength, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
                    documentsData[docNo] = new DocumentData(docNo, mostFrequentTerm, frquenciesOfMostFrequentTerm, termFrequencies.Keys.Count, docLanguage, documentLength);
                    UpdatePostingFile(termFrequencies, postingFile, docNo);



                }
            }
            termsToIndex = postingFile.Values.ToArray<TermFrequency>();
            DocsDats = documentsData.Values.ToArray<DocumentData>();

        }


        public static void IterateTokens(ref int fileIndexer, string[] file, bool useStemming, ref int documentLength, Dictionary<string, int> termFrequencies, ref int frquenciesOfMostFrequentTerm, ref string mostFrequentTerm)
        {


            do
            {
                string token = file[fileIndexer];
                token = NormalizeToken(token);
                if (!EliminatedByCustomedRules(token))
                {
                    bool tokenRecursivelyParsed = false;
                    bool countFrequenciesSeperately = false;
                    bool tokenCanBeStemmed = ActivateDerivationLaws(ref token, file, ref fileIndexer, ref tokenRecursivelyParsed, ref countFrequenciesSeperately, useStemming, ref documentLength, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
                    if (useStemming && !tokenCanBeStemmed && !tokenRecursivelyParsed)
                        token = ActivateStemming(token);
                    if (!tokenRecursivelyParsed && !IsAStopWord(token))
                    {
                        documentLength++;
                        UpdateFrequencies(token, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
                        lastToken = token;
                    }
                    //Console.WriteLine(token);
                }
                fileIndexer++;
                MoveIndexToNextToken(ref fileIndexer, file);

            } while (!ReachedTODocumentEnd(file, fileIndexer));
        }

        private static void UpdatePostingFile(Dictionary<string, int> termFrequencies, Dictionary<string, TermFrequency> postingFile, string docNumber)
        {
            foreach (string term in termFrequencies.Keys)
            {
                if (postingFile.ContainsKey(term))
                {
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
            while (fileIndexer < file.Length && file[fileIndexer] == "")
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
            fileIndexer++;
        }

        /// <summary>
        /// check if index points to end of text tage
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileIndexer"></param>
        /// <returns></returns>
        private static bool ReachedTODocumentEnd(string[] file, int fileIndexer)
        {
            return (fileIndexer >= file.Length || file[fileIndexer] == EndOfTextTag);
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
            if (token == String.Empty)
                return true;
            if (token.Length >= 2 && token[0] == '<' || token[token.Length - 1] == '>')
                return true;

            return false;
        }


        private static bool ActivateDerivationLaws(ref string token, string[] file, ref int fileIndexer, ref bool tokenRecursivelyParsed, ref bool countFrequenciesSeperately, bool useStemming, ref int documentLength, Dictionary<string, int> termFrequencies, ref int frquenciesOfMostFrequentTerm, ref string mostFrequentTerm)
        {
            string[] splittedToken;
            double numericValue;
            string suffix;

            //if token cnsists only from words
            if (token.All(Char.IsLetter))
            {
                ActivateDerivationLawsForWords(ref token, file, ref fileIndexer,ref countFrequenciesSeperately);
                return true;

            }

            // if a number
            if (ExtractNumericValueAndSuffix(ref token, out numericValue, out suffix))
            {
                ActivateDerivationLawsForNumbers(ref token, file, ref fileIndexer, numericValue, suffix);
                return false;
            }



            // if two token connected by - (word-number,word-word,number-number,number word)
            else if ((splittedToken = token.Split('-')).Length == 2 && (splittedToken[0].All(Char.IsLetter) || Double.TryParse(splittedToken[0], out numericValue)) && (splittedToken[1].All(Char.IsLetter) || Double.TryParse(splittedToken[1], out numericValue)))
            {
                countFrequenciesSeperately = true;
                return false;
            }
            // if 3 words connected by - (word-word-word)
            else if ((splittedToken = token.Split('-')).Length == 3 && splittedToken[0].All(Char.IsLetter) && splittedToken[1].All(Char.IsLetter) && splittedToken[2].All(Char.IsLetter))
            {
                countFrequenciesSeperately = true;
                return false;
            }
            // if  initials (u.s.a -> usa)
            else if ((splittedToken = token.Split('-')).Length > 1 && splittedToken.All(s => s.Length == 1 && Char.IsLetter(s[0])))
            {
                token = String.Empty;
                foreach (string initial in splittedToken)
                {
                    token += initial;
                }
                return false;

            }
            // if has possesive s in the end -> (lior`s apple -> lior-apple)
            else if (token.Contains("'s") || token.Contains("`s"))
            {
                int endOfToken = Math.Max(token.IndexOf("'s"), token.IndexOf("`s"));
                token = token.Substring(0, endOfToken);
                int nextTokenIndex = fileIndexer + 1;
                string nextToken=String.Empty;
                while (nextTokenIndex < file.Length && StopWords.Contains(nextToken = NormalizeToken(file[nextTokenIndex])))
                {
                    nextTokenIndex++;
                }
                if (nextTokenIndex >= file.Length)
                    return true;
                fileIndexer = nextTokenIndex;
                token = String.Format("{0}-{1}", token, nextToken);
                countFrequenciesSeperately = true;
                return false;

            }
            // if begins with number and ends with letters 
            else if (token.All(Char.IsLetterOrDigit))
            {
                List<string> tokens = new List<string>();
                bool lastCharWasLetter = Char.IsLetter(token[0]);

                string detachedToken = token[0].ToString();
                for (int tokenIndex = 1; tokenIndex < token.Length; tokenIndex++)
                {
                    if ((Char.IsLetter(token[tokenIndex]) && lastCharWasLetter) || (Char.IsDigit(token[tokenIndex]) && !lastCharWasLetter))
                    {
                        detachedToken += token[tokenIndex];
                    }
                    else
                    {
                        tokens.Add(detachedToken);
                        detachedToken = "" + token[tokenIndex];
                        lastCharWasLetter = !lastCharWasLetter;
                    }
                }
                tokens.Add(detachedToken);
                splittedToken = tokens.ToArray();
                int recursiveFileIndexer = 0;
                MoveIndexToNextToken(ref recursiveFileIndexer, splittedToken);
                if (recursiveFileIndexer < splittedToken.Length)
                {
                    IterateTokens(ref recursiveFileIndexer, splittedToken, useStemming, ref documentLength, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
                    tokenRecursivelyParsed = true;
                    return false;
                }
            }
            // if token can is splitted to sub-tokens by signs (lior%ido...something)
            if ((splittedToken = token.Split(delimiters)).Length > 1)
            {
                int recursiveFileIndexer = 0;
                MoveIndexToNextToken(ref recursiveFileIndexer, splittedToken);
                if (recursiveFileIndexer < splittedToken.Length)
                {
                    IterateTokens(ref recursiveFileIndexer, splittedToken, useStemming, ref documentLength, termFrequencies, ref frquenciesOfMostFrequentTerm, ref mostFrequentTerm);
                    tokenRecursivelyParsed = true;
                    return false;
                }
            }
            return false;
        }


        #endregion
        #region Derivation Laws For Words


        private static void ActivateDerivationLawsForWords(ref string token, string[] file, ref int fileIndexer,ref bool countFrequenciesSeperately)
        {
            // if it`s a date
            if (monthes.ContainsKey(token) && fileIndexer + 1 < file.Length)
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
                        token = value + "-" + monthes[token];
                        fileIndexer++;
                    }


                }
                countFrequenciesSeperately = true ;
            }
            double value1;
            double value2;

            if (fileIndexer+3< file.Length && token == "between" && Double.TryParse(NormalizeToken(file[fileIndexer+1]),out value1) && NormalizeToken(file[fileIndexer+2])=="and" && Double.TryParse(NormalizeToken(file[fileIndexer + 3]), out value2))
            {
                token = String.Format("Between {0} and {1}", value1, value2);
                countFrequenciesSeperately = true;
            }

        }
        #endregion
        #region Derivation Laws For Numbers
        private static void ActivateDerivationLawsForNumbers(ref string token, string[] file, ref int fileIndexer, double numericValue, string suffix)
        {
            numericValue = ParseLargeNumbers(ref token, numericValue, suffix, file, ref fileIndexer);
            string nextToken = string.Empty;
            if (fileIndexer + 1 < file.Length)
                nextToken = NormalizeToken(file[fileIndexer] + 1);

            //if represents a date
            if (numericValue <= 31 && monthes.ContainsKey(nextToken))
            {
                token = monthes[nextToken] + "-" + numericValue;
                fileIndexer++;
                // Check if next word is a year
                int year = 0;
                if (fileIndexer + 1 < file.Length && Int32.TryParse(NormalizeToken(file[fileIndexer + 1]), out year))
                {
                    if (year < 100)
                        year += 1900;
                    token = year + "-" + token;
                    fileIndexer++;
                }
                return;
            }
            if (suffix == "%" || nextToken == "percent" || nextToken == "percentage")
            {
                token = String.Format("{0}%", token);
                if (nextToken == "percent" || nextToken == "percentage")
                    fileIndexer++;
            }
            if (suffix == "$" || nextToken == "dollars")
            {
                token = String.Format("{0} Dollars",token);
                if (nextToken == "dollars")
                    fileIndexer++;
            }
            if (fileIndexer + 2 < file.Length && nextToken == "us" && NormalizeToken(file[fileIndexer + 2]) == "dollars")
            {
                token = String.Format("{0} Dollars", token);
                fileIndexer += 2;
            }



        }

        private static double ParseLargeNumbers(ref string token, double numericValue, string suffix, string[] file, ref int fileIndexer)
        {

            if (numericValue > 1E6)
            {
                token = numericValue / 1E6 + " M";

            }
            if (suffix == "m")
                token = token + " M";
            if (suffix == "bn")
            {
                token = numericValue * (int)1E3 + " M";
            }

            // if number follows a large number in word
            if (fileIndexer + 1 < file.Length && largeNumbers.ContainsKey(NormalizeToken(file[fileIndexer + 1])))
            {
                fileIndexer++;
                token = "" + (int)(numericValue * largeNumbers[NormalizeToken(file[fileIndexer])]) + " M";
            }

            return numericValue;
        }

        private static bool ExtractNumericValueAndSuffix(ref string token, out double numericValue, out string suffix)
        {

            //remove th from the end of number if exists 
            if (Double.TryParse(token, out numericValue))
            {
                suffix = String.Empty;
                return true;
            }
            string[] splittedNumber = token.Split('/');
            double mone;
            double mechane;
            string substeingedToken;
            if (splittedNumber.Length == 2 && Double.TryParse(splittedNumber[0], out mone) && Double.TryParse(splittedNumber[1], out mechane))
            {
                suffix = String.Empty;
                numericValue = mone / mechane;
                return true;
            }
            else if (token.Length > 1 & Double.TryParse((substeingedToken = token.Substring(0, token.Length - 1)).ToLower(), out numericValue))
            {
                suffix = token.Substring(token.Length - 1);
                if (suffix == "%")
                {
                    token = substeingedToken;
                    return true;
                }
                if (suffix == "$")
                {
                    token = substeingedToken;
                    return true;
                }
                if (suffix == "m")
                {
                    token = substeingedToken;
                    return true;
                }
            }
            else if (token.Length > 1 & Double.TryParse((substeingedToken = token.Substring(1, token.Length - 1)).ToLower(), out numericValue))
            {
                suffix = token.Substring(0, 1);
                if (suffix == "%")
                {
                    token = substeingedToken;
                    return true;
                }
                if (suffix == "$")
                {
                    token = substeingedToken;
                    return true;
                }
            }

            else if (token.Length >= 2 && Double.TryParse((substeingedToken = token.Substring(0, token.Length - 2)).ToLower(), out numericValue))
            {
                suffix = token.Substring(token.Length - 2);
                if (suffix == "th")
                {
                    token = substeingedToken;
                    return true;
                }
                if (suffix == "st")
                {
                    token = substeingedToken;
                    return true;
                }
                if (suffix == "bm")
                {
                    token = substeingedToken;
                    return true;
                }
            }

            suffix = string.Empty;
            return false;
        }
        #endregion
    }


}

