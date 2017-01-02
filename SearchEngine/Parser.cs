using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SearchEngine
{
    public static class Parser
    {
        #region Attributes

        static int _fileIndexer;
        static string[] _file;
        public static bool UseStemming;
        static int _documentLength;
        static Dictionary<string, int> _termFrequencies;
        static int _frquenciesOfMostFrequentTerm;
        static string _mostFrequentTerm;
        static Dictionary<string, Dictionary<string, int>> _nextTermsFrequency;

        static readonly string DocNumberOpeningTag = "<DOCNO>";
        static readonly string PreecedantDocLanguage = "Language:";
        static readonly string DocLanguageOpeningTag = "P=105>";
        static readonly string BeginningOfTextTag = "<TEXT>";
        static readonly string EndOfTextTag = "</TEXT>";
        static readonly string TitleOpeningTag = "<TI>";
        static readonly string TitleClosingTag = "</TI></H3>";

        private static Stemmer sStemmer = new Stemmer();
        public static HashSet<string> StopWords = null;
        private readonly static char[] SuffixToRemove = { '-', '~', '`', ';', '!', '@', '#', '^', '&', '*', '(', ')', '=', '+', '[', ']', '{', '}', '\'', '"', '?', '/', '>', ',', '.', ':','\t','\b' };
        private readonly static char[] prefixToRemove = { '|', '~', '`', ';', '!', '@', '#', '^', '&', '*', '(', ')', '=', '+', '[', ']', '{', '}', '\'', '"', '?', '/', '<', ',', '.', '%', '-', ':', '\t', '\b' };
        private readonly static char[] delimiters = { '~', '`', ';', '!', '@', '#', '^', '&', '*', '(', ')', '=', '+', '[', ']', '{', '}', '\'', '"', '?', '/', '<', '>', ',', '-', '.', ':','|', '\t', '\b' };
        private static readonly Dictionary<string, string> months = new Dictionary<string, string>
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
            {"jan","01" },
            {"feb","02" },
            {"mar","03" },
            {"apr","04" },
            {"jul","07" },
            {"aug","08" },
            {"sep","09" },
            {"oct","10" },
            {"nov","11" },
            {"dec","12" }
        };

        private static readonly Dictionary<string, double> largeNumbers = new Dictionary<string, double>
        {
            {"million",1 },
            {"billion",(int)1E3},
            {"trillion",(int)1E6},
        };
        private static HashSet<string> PrefixesOfNumbers = new HashSet<string>() { "$", "%",String.Empty };
        private static HashSet<string> SufffixesOfNumbers = new HashSet<string>() { "%", "$", "m", "th", "st", "rd", "bn", String.Empty };


        
        
        #endregion
        /// <summary>
        /// Main funnction for parsing files.
        /// </summary>
        /// <param name="filePathes">Array of corpus files pathes.</param>
        /// <param name="useStemming"> if tou use stemming</param>
        /// <param name="termsToIndex"> The terms found in given documents</param>
        /// <param name="documentsData"> The documents found in files</param>
        public static void Parse(string[] filePathes, bool useStemming, out TermFrequency[] termsToIndex,  Dictionary<string, DocumentData> documentsData)
        {
            Dictionary<string, TermFrequency> termsFoundInFiles = new Dictionary<string, TermFrequency>();
            int numOfFiles = filePathes.Length;
            // for each file given to parser:
            for (int i = 0; i < numOfFiles; i++)
            {
                _file = FileReader.ReadTextFile(filePathes[i]);
                int fileLength = _file.Length;
                _fileIndexer = 0;
                while (_fileIndexer < fileLength)
                {
                    // Find the document number, and move file cursor to it.
                    string docNo = GetDocNummer(_file, ref _fileIndexer);
                    if (_fileIndexer == fileLength)
                        break;
                    _nextTermsFrequency = new Dictionary<string, Dictionary<string, int>>();
                    // Parse Title
                    Dictionary<string, int> termsInTitle = ParseTitle();
                    // Find beginning of text in this document.
                    FindBegginingOfText(_file, ref _fileIndexer);
                    if (_fileIndexer >= fileLength)
                        break;
                    // Look for language of document if exists
                    string docLanguage = GetLanguage(_file, ref _fileIndexer);
                    // Init most frequent term and its frequencies, document length for this document.
                    _mostFrequentTerm = "";
                    _documentLength = 0;
                    _frquenciesOfMostFrequentTerm = 0;
                    _termFrequencies = new Dictionary<string, int>();

                    // Derive all tokens of this document`s text.
                    IterateTokens(ref _fileIndexer,_file, _termFrequencies);
                    // Save data of current document
                    documentsData[docNo] = new DocumentData(docNo, _mostFrequentTerm, _frquenciesOfMostFrequentTerm, _termFrequencies.Keys.Count, docLanguage, _documentLength, termsInTitle);
                    // Merge terms frequencies of this document with term frequencies of previous documents.
                    UpdateTermsExistInDocument( termsFoundInFiles, docNo);



                }
            }
            // Return all term frequencies of all files.
            termsToIndex = termsFoundInFiles.Values.ToArray<TermFrequency>();

        }

        private static Dictionary<string, int> ParseTitle()
        {
            Dictionary<string, int> termsInTitle = new Dictionary<string, int>();
            //Reach to opening tag of doc number
            for (; _fileIndexer < _file.Length && _file[_fileIndexer] != TitleOpeningTag && _file[_fileIndexer] != BeginningOfTextTag; _fileIndexer++) ;
            // if got to the end of file - break
            if (_fileIndexer >= _file.Length || _file[_fileIndexer]== BeginningOfTextTag)
                return termsInTitle;
            //Move to title 
            _fileIndexer++;
            List <string> tokensInTitle = new List<string>();
            string token = _file[_fileIndexer];
            while (token != TitleClosingTag)
            {
                tokensInTitle.Add(token);
                token = _file[_fileIndexer];
                _fileIndexer++;
            }
            int titleIndexer = 0;
            if (tokensInTitle.Count>0)
                IterateTokens(ref titleIndexer, tokensInTitle.ToArray(), termsInTitle);
            return termsInTitle;
        }

        /// <summary>
        /// Read text of specific document, acitvate derviation laws, and save all terms frequencies found.
        /// </summary>
        /// <param name="_fileIndexer"> Cursor to file</param>
        /// <param name="_file"> All words in file.</param>
        /// <param name="_useStemming">Is using stemming </param>
        /// <param name="_documentLength"> Number of terms found in document</param>
        /// <param name="_termFrequencies">Maps terms to their frequencies in current document</param>
        /// <param name="_frquenciesOfMostFrequentTerm">How many times the most frequent term appeared.</param>
        /// <param name="_mostFrequentTerm"> Most frequent term</param>
        public static void IterateTokens(ref int fileIndexer, string[] file,Dictionary<string,int> termFrequencies,bool GenerateSuggestion=true)
        {

            string[] splittedToken;
            char[] tokenDelimiters = new char[] { ' ', '-' };
            string previousTerm = null;
            string currentTerm = null;

            // for all token in document`s text
            do
            {
                string token = file[fileIndexer];
                // Remove unneccessary signs from beginning and ending of token, and change all letters to lower.
                token = NormalizeToken(token);
                // If the token is not eliminated by customed rules:
                if (!EliminatedByCustomedRules(token))
                {
                    bool tokenRecursivelyParsed = false;
                    bool countFrequenciesSeperately = false;
                    bool avoidStopWords = false;
                    // Activate derivation laws on token, and return true if the token can be stemmed.
                    bool tokenCanBeStemmed = ActivateDerivationLaws(ref token, file, ref fileIndexer, ref tokenRecursivelyParsed, ref countFrequenciesSeperately, UseStemming, ref _documentLength, _termFrequencies, ref _frquenciesOfMostFrequentTerm, ref _mostFrequentTerm, ref avoidStopWords);
                    // If need to use stemming,and token can be stemmed, stem the token
                    if (UseStemming && tokenCanBeStemmed && !tokenRecursivelyParsed)
                        token = ActivateStemming(token);
                    // If token weren`t already parsed seperately (=tokenRecursivelyParsed) and token is not a stopword:
                    if (!tokenRecursivelyParsed && (!IsAStopWord(token) || avoidStopWords))
                    {
                        // Update frequencies of term in document
                        _documentLength++;
                         UpdateTermsFrequenciesInCurrentDocument(token, termFrequencies);
                        currentTerm= token;
                        // If the token consists of sub-tokens (for example: "Lior-Ido" can be splitted to "Lior" and "Ido"
                        if (countFrequenciesSeperately )
                        {
                            // Update frequencies of sub-tokens.
                            splittedToken = token.Split(tokenDelimiters);
                            string subtoken;
                            for (int i=0;i< splittedToken.Length;i++)
                            {
                                subtoken = splittedToken[i];
                                UpdateTermsFrequenciesInCurrentDocument(subtoken, termFrequencies);
                                if (i+1< splittedToken.Length && GenerateSuggestion)
                                {
                                    UpdateTermAutoCpmpletion(subtoken, splittedToken[i + 1]);
                                }
                            }

                        }
                                    
                    }
                }
                if (GenerateSuggestion)
                    UpdateTermAutoCpmpletion(previousTerm, currentTerm);
                previousTerm = currentTerm;
                currentTerm =null;
                fileIndexer++;
                // Find next token which is not empty string.
                MoveIndexToNextToken(ref fileIndexer, file);

            } while (!ReachedTODocumentEnd(file, fileIndexer));

        }

        /// <summary>
        /// Merge frequncies of terms from current documt, to frequncies of terms from previous documents
        /// </summary>
        /// <param name="termFrequencies">Dictionary of terms to their frequencies in current document</param>
        /// <param name="termsFromPreviousDocuments">Dictionary of terms to their frequencies in all previous documents </param>
        /// <param name="docNumber"></param>
        private static void UpdateTermsExistInDocument (Dictionary<string, TermFrequency> termsFromPreviousDocuments, string docNumber)
        {
            foreach (string term in _termFrequencies.Keys)
            {
                Dictionary<string, int> termsCompletion = new Dictionary<string, int>();
                if (_nextTermsFrequency.ContainsKey(term))
                    termsCompletion = _nextTermsFrequency[term];
                // If term already found in previous docoumnets - update its frequency
                if (termsFromPreviousDocuments.ContainsKey(term))
                {
                    termsFromPreviousDocuments[term].AddFrequencyInDocument(docNumber, _termFrequencies[term], termsCompletion);
                }

                // If term found for first time in this iteration, create a new entry in dicotionary for term.
                else
                {
                    termsFromPreviousDocuments[term] = new TermFrequency(term, docNumber, _termFrequencies[term], termsCompletion);
                }

            }
        }

        /// <summary>
        /// Moves file cursor to the next string which is not empty
        /// </summary>
        /// <param name="fileIndexer"></param>
        /// <param name="file"></param>
        private static void MoveIndexToNextToken(ref int fileIndexer, string[] file)
        {
            while (fileIndexer < file.Length && file[fileIndexer] == "")
                fileIndexer++;
        }

        /// <summary>
        /// Init stop words collection
        /// </summary>
        /// <param name="stopWordsFilePath"></param>
        public static void InitStopWords(string stopWordsFilePath)
        {
            string[] stopWords = File.ReadAllText(stopWordsFilePath).Split(new char[] { '\n', '\r' });
            StopWords = new HashSet<string>(stopWords);
        }

        /// <summary>
        /// Update frequncies of specific term, in regrads to the document it appeared in. 
        /// </summary>
        /// <param name="term"></param>
        /// <param name="termFrequencies"></param>
        /// <param name="frquenciesOfMostFrequentTerm"></param>
        /// <param name="mostFrequentTerm"></param>
        private static void UpdateTermsFrequenciesInCurrentDocument(string term, Dictionary<string,int> termFrequencies)
        {
            // If term wasn`t found previously in document - set its frequency to one
            if (!termFrequencies.ContainsKey(term))
                termFrequencies[term] = 1;
            // Otherwise increase it by 1.
            else
                termFrequencies[term]++;

            // Update mot frquent term in document if neccessary.
            if (termFrequencies[term] > _frquenciesOfMostFrequentTerm)
            {
                _frquenciesOfMostFrequentTerm = termFrequencies[term];
                _mostFrequentTerm = term;
            }


        }
        private static void UpdateTermAutoCpmpletion(string term, string nextTerm)
        {
            if (String.IsNullOrEmpty(term) || String.IsNullOrEmpty(nextTerm))
                return;
            // If term wasn`t found previously in document - set create new list of completion terms
            if (!_nextTermsFrequency.ContainsKey(term))
                _nextTermsFrequency[term] = new Dictionary<string, int>();
            if (!_nextTermsFrequency[term].ContainsKey(nextTerm))
                _nextTermsFrequency[term][nextTerm] = 1;
            else
                _nextTermsFrequency[term][nextTerm]++;
        }


        /// <summary>
        /// Activate stemming on term
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        private static string ActivateStemming(string term)
        {
            return sStemmer.stemTerm(term);
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
            //if (docNo.IndexOf("FBIS") >= 0)
            //{
            //    docNo = docNo.Substring(docNo.IndexOf("FBIS") + 4);
            //}
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
            string language=String.Empty;

            if (file[fileIndexer]==PreecedantDocLanguage  && fileIndexer+3< file.Length)
            {
                fileIndexer += 2;
                string openningTag = file[fileIndexer];
                // if the language attched to the tag (P=105>French)
                if (openningTag.Contains(DocLanguageOpeningTag) && openningTag.Substring(DocLanguageOpeningTag.Length).Length > 0)
                {
                    language = openningTag.Substring(DocLanguageOpeningTag.Length);
                    fileIndexer+=2;

                }
                // if the language sperated by space from tag (P=105> French)
                else if (openningTag.Contains(DocLanguageOpeningTag) && openningTag.Substring(DocLanguageOpeningTag.Length).Length == 0)
                {
                    fileIndexer++;
                    language = file[fileIndexer];
                    fileIndexer += 2;

                }

            }

            if (fileIndexer + 2 < file.Length && file[fileIndexer]== "Article")
            {
                fileIndexer += 2;
            }

            language = language.TrimEnd(',');
            if (language.Length==0 || !language.All(Char.IsLetter))
                language= String.Empty;
            return language;


        }

        /// <summary>
        ///  Move indexer to bginning of text 
        /// </summary>
        private static void FindBegginingOfText(string[] file, ref int fileIndexer)
        {
            for (; fileIndexer<file.Length && file[fileIndexer] != BeginningOfTextTag; fileIndexer++) ;
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
        /// Remove all unneccessary sing from beginning and ending of terms, set it to lower case.
        /// </summary>
        /// <param name="token"></param>
        public static string NormalizeToken(string token)
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


        /// <summary>
        /// Activate derivation laws on token
        /// </summary>
        /// <param name="token"> Token to derive</param>
        /// <param name="file">File</param>
        /// <param name="fileIndexer">Cursor to file</param>
        /// <param name="SkipUpdatingFrequnciesOfThisTerm"> its value set to true if this method parsed this token recursively (for example, lior-20-10 would be parsed as 3 sperated tokens, no need to add "lior-20-10" as another token). </param>
        /// <param name="countFrequenciesSeperately"> its value set to true by this method if this token can be splitted to subtokens (for example "lior-ido" can be also counted as "lior" and "ido"</param>
        /// <param name="useStemming">To use stemming</param>
        /// <param name="documentLength">doc length</param>
        /// <param name="termFrequencies">Term frequencies of current documents</param>
        /// <param name="frquenciesOfMostFrequentTerm">How many times the most frequent t</param>
        /// <param name="mostFrequentTerm"></param>
        /// <param name="avoidStopWords">Set to true if term need to be counted although it may be a stopword</param>
        /// <returns></returns>
        public static bool ActivateDerivationLaws(ref string token, string[] file, ref int fileIndexer, ref bool SkipUpdatingFrequnciesOfThisTerm, ref bool countFrequenciesSeperately, bool useStemming, ref int documentLength, Dictionary<string, int> termFrequencies, ref int frquenciesOfMostFrequentTerm, ref string mostFrequentTerm, ref bool avoidStopWords)
        {
            string[] splittedToken;
            double numericValue;
            string suffix;
            string prefix;

            //if token cnsists only from words
            if (token.All(Char.IsLetter))
            {
                ActivateDerivationLawsForWords(ref token, file, ref fileIndexer,ref countFrequenciesSeperately);
                return true;

            }

            // if a number
            if (ExtractNumericValueAndSuffix(ref token, out numericValue, out suffix, out prefix))
            {
                ActivateDerivationLawsForNumbers(ref token, file, ref fileIndexer, numericValue, suffix, prefix);
                return false;
            }



            // if two token connected by - (word-number,word-word,number-number,number word)
            else if ((splittedToken = token.Split('-')).Length == 2 && splittedToken.All(s => s != String.Empty) && splittedToken.All(s => s != String.Empty) && (splittedToken[0].All(Char.IsLetter) || Double.TryParse(splittedToken[0], out numericValue)) && (splittedToken[1].All(Char.IsLetter) || Double.TryParse(splittedToken[1], out numericValue)))
            {
                countFrequenciesSeperately = true;
                return false;
            }


            // if 3 words connected by - (word-word-word)
            else if ((splittedToken = token.Split('-')).Length == 3 && splittedToken.All(s => s != String.Empty) && splittedToken[0].All(Char.IsLetter) && splittedToken[1].All(Char.IsLetter) && splittedToken[2].All(Char.IsLetter))
            {
                countFrequenciesSeperately = true;
                return false;
            }
            else if (splittedToken.All(s => (s == String.Empty || s.All(Char.IsLetter) || ExtractNumericValueAndSuffix(ref s, out numericValue, out suffix, out prefix) )))
            {
                int recursiveFileIndexer = 0;
                MoveIndexToNextToken(ref recursiveFileIndexer, splittedToken);
                if (recursiveFileIndexer < splittedToken.Length)
                {
                    IterateTokens(ref recursiveFileIndexer, splittedToken,_termFrequencies);
                    SkipUpdatingFrequnciesOfThisTerm = true;
                    return false;
                }
            }


            // if  initials (u.s.a -> usa)
            else if ((splittedToken = token.Split('.')).Length > 1 && splittedToken.All(s => s.Length == 1 && Char.IsLetter(s[0])))
            {
                token = String.Empty;

                foreach (string initial in splittedToken)
                {
                    token += initial;
                }
                avoidStopWords = true;
                return false;

            }
            // if has possesive s in the end -> (lior`s apple -> lior-apple)
            else if ((token.IndexOf("'s") == token.Length - 2 && token.IndexOf("'s") >= 0) || (token.IndexOf("`s") == token.Length - 2 && token.IndexOf("`s") >= 0))
            {
                int endOfToken = Math.Max(token.IndexOf("'s"), token.IndexOf("`s"));
                token = token.Substring(0, endOfToken);
                if (token.All(Char.IsLetter))
                {
                    int nextTokenIndex = fileIndexer + 1;
                    string nextToken = String.Empty;
                    while (nextTokenIndex < file.Length && StopWords.Contains(nextToken = NormalizeToken(file[nextTokenIndex])))
                    {
                        nextTokenIndex++;
                    }
                    if (nextTokenIndex >= file.Length)
                        return true;
                    if (nextToken.All(Char.IsLetter) || nextToken.All(Char.IsDigit))
                    {
                        fileIndexer = nextTokenIndex;
                        token = String.Format("{0}-{1}", token, nextToken);
                        countFrequenciesSeperately = true;
                    }


                }
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
                    IterateTokens(ref recursiveFileIndexer, splittedToken, _termFrequencies);
                    SkipUpdatingFrequnciesOfThisTerm = true;
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
                    IterateTokens(ref recursiveFileIndexer, splittedToken, _termFrequencies);
                    SkipUpdatingFrequnciesOfThisTerm = true;
                    return false;
                }
            }
            SkipUpdatingFrequnciesOfThisTerm = true;
            return false;
        }


        #endregion
        #region Derivation Laws For Words

        /// <summary>
        ///  Derivation laws for words
        /// </summary>
        private static void ActivateDerivationLawsForWords(ref string token, string[] file, ref int fileIndexer,ref bool countFrequenciesSeperately)
        {
            // if it`s a date
            if (months.ContainsKey(token) && fileIndexer + 1 < file.Length)
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
                        token = months[token] + "-" + file[fileIndexer + 1];
                        fileIndexer++;
                    }
                    //if it represents year
                    else
                    {
                        token = value + "-" + months[token];
                        fileIndexer++;
                    }


                }
                countFrequenciesSeperately = true ;
            }
            double value1;
            double value2;

            if (fileIndexer+3< file.Length && token == "between" && Double.TryParse(NormalizeToken(file[fileIndexer+1]),out value1) && NormalizeToken(file[fileIndexer+2])=="and" && Double.TryParse(NormalizeToken(file[fileIndexer + 3]), out value2))
            {
                token = String.Format("between {0} and {1}", value1, value2);
                countFrequenciesSeperately = true;
            }

        }
        #endregion
        #region Derivation Laws For Numbers
        /// <summary>
        ///  Derivation laws for Numbers
        /// </summary>
        private static void ActivateDerivationLawsForNumbers(ref string token, string[] file, ref int fileIndexer, double numericValue, string suffix,string prefix)
        {

                numericValue = ParseLargeNumbers(ref token, numericValue, suffix, file, ref fileIndexer);
            string nextToken = string.Empty;
            if (fileIndexer + 1 < file.Length)
                nextToken = NormalizeToken(file[fileIndexer + 1] );

            //if represents a date
            if (numericValue <= 31 && months.ContainsKey(nextToken))
            {
                token = months[nextToken] + "-" + numericValue;
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
            if (prefix== "%" || suffix == "%" || nextToken == "percent" || nextToken == "percentage")
            {
                token = String.Format("{0}%", token);
                if (nextToken == "percent" || nextToken == "percentage")
                    fileIndexer++;
            }
            if (prefix == "$" || suffix == "$" || nextToken == "dollars")
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
        /// <summary>
        /// Parsing a large numner
        /// </summary>

        /// <returns></returns>
        private static double ParseLargeNumbers(ref string token, double numericValue, string suffix, string[] file, ref int fileIndexer)
        {

            if (numericValue > 1E6)
            {
                token = numericValue / 1E6 + "M";

            }
            if (suffix == "m")
                token = token + "M";
            if (suffix == "bn")
            {
                token = numericValue * (int)1E3 + "M";
            }

            // if number follows a large number in word
            if (fileIndexer + 1 < file.Length && largeNumbers.ContainsKey(NormalizeToken(file[fileIndexer + 1])))
            {
                fileIndexer++;
                token = "" + (int)(numericValue * largeNumbers[NormalizeToken(file[fileIndexer])]) + "M";
            }

            return numericValue;
        }


        /// <summary>
        /// Extract numeric value, importand suffix and prefix from token 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="numericValue"></param>
        /// <param name="suffix"></param>
        /// <param name="prefix"></param>
        /// <returns>Returns true if token is matching one of the drivation laws for numbers</returns>
        private static bool ExtractNumericValueAndSuffix(ref string token, out double numericValue, out string suffix, out string prefix)
        {
            string originalToken = token;
            string[] splittedNumber = token.Split('/');
            double mone;
            double mechane;

            if (!token.Any(Char.IsDigit))
            {
                suffix = String.Empty;
                prefix = String.Empty;
                numericValue = Double.NaN;
                return false;

            }

            //if fraction a/n
            if (splittedNumber.Length == 2 && Double.TryParse(splittedNumber[0], out mone) && Double.TryParse(splittedNumber[1], out mechane))
            {

                numericValue = mone / mechane;
                suffix = String.Empty;
                prefix = String.Empty;
                return true;
            }

            // if all token is numner (1201)
            if (Double.TryParse(token, out numericValue))
            {

                prefix = String.Empty;
                suffix = String.Empty;
                token = numericValue.ToString();
            }
            // if last char of token is a sign (100$)
            else if (token.Length>1 && Double.TryParse(token.Substring(0, token.Length - 1), out numericValue))
            {
                prefix = String.Empty;
                suffix = token.Substring(token.Length - 1);
                token = numericValue.ToString();

            }
            // if 2 last chars of token is a sign (100bn)
            else if (token.Length > 2 &&  Double.TryParse(token.Substring(0, token.Length - 2), out numericValue))
            {
                prefix = String.Empty;
                suffix = token.Substring(token.Length - 2);
                token = numericValue.ToString();

            }
            // if first char of token is a sign ($100)
            else if (token.Length > 1 &&  Double.TryParse(token.Substring(1, token.Length - 1), out numericValue))
            {
                prefix = token.Substring(0, 1);
                suffix = String.Empty;
                token = numericValue.ToString();

            }
            // if first and last char is a sign ($100m)
            else if (token.Length > 2 &&  Double.TryParse(token.Substring(1, token.Length - 2), out numericValue))
            {
                prefix = token.Substring(0, 1);
                suffix = token.Substring(token.Length - 1);
                token = numericValue.ToString();

            }
            // if first and last 2 chars are signs ($100bn)
            else if (token.Length > 3 &&  Double.TryParse(token.Substring(1, token.Length - 3), out numericValue))
            {
                prefix = token.Substring(0, 1);
                suffix = token.Substring(token.Length - 2);
                token = numericValue.ToString();

            }
            else if (largeNumbers.Keys.Any(s => originalToken.IndexOf(s) > -1))
            {
                prefix = String.Empty;
                suffix = String.Empty;
                foreach (string number in largeNumbers.Keys)
                {
                    int indexOfNumber = token.IndexOf(number);
                    if (indexOfNumber >= 0)
                    {
                        token = token.Substring(0, indexOfNumber);
                        if (Double.TryParse(token,out numericValue))
                        {
                            token = (numericValue * largeNumbers[number]).ToString();
                        
                            return true;
                        }
                        
                    }
                }
                token = originalToken;
                return false;

            }
            else
            {
                prefix = String.Empty;
                suffix = String.Empty;
                return false;
            }
            // "%","$","m","th","st","rd","bm"

            if (PrefixesOfNumbers.Contains(prefix) && SufffixesOfNumbers.Contains(suffix))
                return true;
            else
            {
                token = originalToken;
                return false;
            }

        }
        #endregion
    }


}

