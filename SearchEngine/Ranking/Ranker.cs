using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordNet;
using System.Reflection;
using System.Windows.Forms;
using LAIR.Collections.Generic;

namespace SearchEngine.Ranking
{
    public class Ranker
    {
        public double k1 = 1.6;
        public double k2 = 100;
        public double b = 0.25;


        public double w1 = 1.8;
        public double w2 = 0.5;
        public double w3 = 0.2;
        public double w4 = 0.65;
        public double wSemantics = 1.1;


        // Dcouments data
        Dictionary<string, DocumentData> _documents;

        Dictionary<string, TermData>[] _splittedMainDictionary;
        int resultsToRetrieve = 50;
        WordNetEngine _similarityEngine;
        Searcher _searcher;
        Stemmer _stemmer;
        bool _useStemming;
        // Memory of retreived posting files records from memory
        static Dictionary<string, PostingFileRecord> _postingFileRecordsInMemory;

        /// <summary>
        /// C`tor of ranker
        /// </summary>
        /// <param name="documents"> Data about documents in corpus</param>
        /// <param name="splittedMainDictionary"> Main dictionary from indexer</param>
        /// <param name="searcher">Searcher </param>
        /// <param name="useStemming"> Is use stemming </param>
        public Ranker(Dictionary<string, DocumentData> documents, Dictionary<string, TermData>[] splittedMainDictionary, Searcher searcher, bool useStemming)
        {

            _documents = documents;
            _splittedMainDictionary = splittedMainDictionary;
            string root = Path.GetDirectoryName(Application.ExecutablePath);
            _similarityEngine = new WordNetEngine(@"WNdb-3.0", true);
            _searcher = searcher;
            _stemmer = new Stemmer();
            _useStemming = useStemming;
            if (_postingFileRecordsInMemory == null)
                _postingFileRecordsInMemory = new Dictionary<string, PostingFileRecord>();
        }

        /// <summary>
        /// Rank documents
        /// </summary>
        /// <param name="termsInQuery">Term of query and their frquency</param>
        /// <param name="queryID"> Query ID</param>
        /// <param name="releventPostingFilesRecords"> Posting files from searcher of terms from query</param>
        /// <param name="previousRankedDocuments"> Documents ranked from previous querries </param>
        /// <param name="avgDocumentLength"> Average document length of all documents in corpus </param>
        /// <param name="chosenLanguages"> Languages user selected from query</param>
        /// <returns> Rankinng of doucments</returns>
        public DocumentRank[] RankDocuments(Dictionary<string, double> termsInQuery, string queryID, Dictionary<string, PostingFileRecord> releventPostingFilesRecords,
            DocumentRank[] previousRankedDocuments, double avgDocumentLength, HashSet<string> chosenLanguages)
        {
            bool considerLanguages = chosenLanguages.Count > 0;
            //Extract all relevent documents
            HashSet<string> releventDocuments = GetReleventDocument(releventPostingFilesRecords, considerLanguages, chosenLanguages);


            Dictionary<string, double> documentRanking = new Dictionary<string, double>();

            double N = _documents.Count();
            // Check if can find relevent documents fo query
            double R = 0;

            // Rank all documents
            foreach (string docNum in releventDocuments)
            {
                documentRanking[docNum] = BM25(termsInQuery, docNum, releventPostingFilesRecords, R, avgDocumentLength, N);
                documentRanking[docNum] += w1 * TitleRank(termsInQuery, docNum);
                documentRanking[docNum] += w3 * AddAdjacentTermsScore(termsInQuery, docNum, releventPostingFilesRecords);
                documentRanking[docNum] += w4 * SpecificityOfDocument(termsInQuery, docNum, releventPostingFilesRecords);
             }
            // Add score for semantic care
            UpdateRankWithSemantics(termsInQuery, ref documentRanking, chosenLanguages, R, avgDocumentLength, N);
            int rankIndexer = 0;

            // Sort ranking by descending order
            var sortedRank = documentRanking.OrderByDescending(i => i.Value);
            int numOfResults = Math.Min(resultsToRetrieve, sortedRank.Count());
            List<DocumentRank> sortedRankedDocuments = new List<DocumentRank>();
            // copy results from previous querries
            for (int i = 0; i < previousRankedDocuments.Length; i++, rankIndexer++)
            {
                sortedRankedDocuments.Add(previousRankedDocuments[i]);
            }
            // Add results from current querry
            for (int i = 1; i <= numOfResults; i++, rankIndexer++)
            {
                var rankedResut = sortedRank.ElementAt(i - 1);
                if (rankedResut.Value > 0)
                    sortedRankedDocuments.Add(new DocumentRank(queryID, i, rankedResut.Key, rankedResut.Value));
                else
                    break;
            }
            return sortedRankedDocuments.ToArray();

        }

        /// <summary>
        /// Ranking based on how many terms from the querry exist in document 
        /// </summary>
        /// <param name="termsInQuery">Querry </param>
        /// <param name="docNum">Document number </param>
        /// <param name="releventPostingFilesRecords">Posting files record of terms in querry.</param>
        /// <returns>Score </returns>
        private double SpecificityOfDocument(Dictionary<string, double> termsInQuery, string docNum, Dictionary<string, PostingFileRecord> releventPostingFilesRecords)
        {
            double termsAppearInDoc = 0;
            foreach (string term in termsInQuery.Keys)
            {
                if (!releventPostingFilesRecords.ContainsKey(term))
                    continue;
                PostingFileRecord postingFileOfTerm = releventPostingFilesRecords[term];
                if (!postingFileOfTerm.DF.ContainsKey(docNum))
                    continue;
                else
                    termsAppearInDoc++;
            }
            return (double)_documents[docNum].AmmountOfUniqueTerms / _documents[docNum].DocumentLength * termsAppearInDoc / termsInQuery.Count;

        }

        /// <summary>
        /// Score based on how many terms from query are following each other in document
        /// </summary>
        /// <param name="termsInQuery">Querry </param>
        /// <param name="docNum">Document number </param>
        /// <param name="releventPostingFilesRecords">Posting files record of terms in querry.</param>
        /// <returns> Score</returns>
        private double AddAdjacentTermsScore(Dictionary<string, double> termsInQuery, string docNum, Dictionary<string, PostingFileRecord> releventPostingFilesRecords)
        {
            double score = 0;
            // for each term in querry
            foreach (string term in termsInQuery.Keys)
            {
                double termScore = 0;
                if (!releventPostingFilesRecords.ContainsKey(term))
                    continue;
                PostingFileRecord postingFileOfTerm = releventPostingFilesRecords[term];
                if (!postingFileOfTerm.NextTermFrequencies.ContainsKey(docNum))
                    continue;
                Dictionary<string, int> nextTermsInDocument = postingFileOfTerm.NextTermFrequencies[docNum];
                // and other terms in querry
                foreach (string nextTerm in termsInQuery.Keys)
                {
                    // if two terms are following each other in document - add to score number of times it happens
                    if (nextTerm != term && nextTermsInDocument.ContainsKey(nextTerm))
                        termScore += nextTermsInDocument[nextTerm];

                }
                // Normalize by frequency of term
                score += termScore / postingFileOfTerm.DF[docNum];

            }

            return score;
        }
        /// <summary>
        /// Add score to querry based on their semantic
        /// </summary>
        /// <param name="termsInQuery">Query without syns</param>
        /// <param name="documentRanking">document ranking withous semantic score</param>
        /// <param name="chosenLanguages">Languages to retreive documents of</param>
        /// <param name="R"> R (BM25) </param>
        /// <param name="avgDocumentLength"> Average document length of all corpus </param>
        /// <param name="N"> N (BM 25) </param>
        private void UpdateRankWithSemantics(Dictionary<string, double> termsInQuery, ref Dictionary<string, double> documentRanking, HashSet<string> chosenLanguages, double R, double avgDocumentLength, double N)
        {
            Dictionary<string, double> queryWithSemantics = GetQueryCombindedWithSemantics(termsInQuery);
            Dictionary<string, double> documentRankingForExpandedQuery = new Dictionary<string, double>();
            Dictionary<string, PostingFileRecord> releventPostingFilesRecordsExpandedQuery = new Dictionary<string, PostingFileRecord>();
            Dictionary<string, int> termsToRetreiveFromMemory = new Dictionary<string, int>();
            // Check for every term if its posting file record is stored in memory
            foreach (string term in queryWithSemantics.Keys)
            {
                if (_postingFileRecordsInMemory.ContainsKey(term))
                    releventPostingFilesRecordsExpandedQuery[term] = _postingFileRecordsInMemory[term];
                else
                    termsToRetreiveFromMemory[term] = 0;
            }
            // Retreive posting file reacords for each term which are not stored in memory 
            Dictionary<string, PostingFileRecord> retreivedPostinfFilesRecord = _searcher.FindReleventDocuments(termsToRetreiveFromMemory);
            // Add to memory new posting files record
            foreach (string retreivedTerm in retreivedPostinfFilesRecord.Keys)
            {
                _postingFileRecordsInMemory[retreivedTerm] = retreivedPostinfFilesRecord[retreivedTerm];
                releventPostingFilesRecordsExpandedQuery[retreivedTerm] = retreivedPostinfFilesRecord[retreivedTerm];
            }
            List<string> termsToRemove = new List<string>();
            // If syn does not have a posting file record - delete it from query
            foreach (string term in queryWithSemantics.Keys)
            {
                if (!releventPostingFilesRecordsExpandedQuery.ContainsKey(term))
                    termsToRemove.Add(term);
            }
            foreach (string term in termsToRemove)
                queryWithSemantics.Remove(term);

            // Get relevent documents consist terms of expanded query
            HashSet<string> releventDocumentsForExpandedQuery = GetReleventDocument(releventPostingFilesRecordsExpandedQuery, false, chosenLanguages);

            // Calculate rank for expanded query (aside from previous score)
            foreach (string docNum in releventDocumentsForExpandedQuery)
            {
                if (!documentRankingForExpandedQuery.ContainsKey(docNum))
                    documentRankingForExpandedQuery[docNum] = 0;
                documentRankingForExpandedQuery[docNum] += BM25(queryWithSemantics, docNum, releventPostingFilesRecordsExpandedQuery, R, avgDocumentLength, N);
                documentRankingForExpandedQuery[docNum] += w1 * TitleRank(queryWithSemantics, docNum);
                documentRankingForExpandedQuery[docNum] += w3 * AddAdjacentTermsScore(queryWithSemantics, docNum, releventPostingFilesRecordsExpandedQuery);
                documentRankingForExpandedQuery[docNum] += w4 * SpecificityOfDocument(queryWithSemantics, docNum, releventPostingFilesRecordsExpandedQuery);
                //documentRankingForExpandedQuery[docNum] += w5 * RealtiveFrequency(queryWithSemantics, docNum, releventPostingFilesRecordsExpandedQuery);
            }
            // Add semantic score to total score
            foreach (string docNum in documentRankingForExpandedQuery.Keys)
            {
                if (!documentRanking.ContainsKey(docNum))
                    documentRanking[docNum] = w2 * documentRankingForExpandedQuery[docNum];
                documentRanking[docNum] += w2 * documentRankingForExpandedQuery[docNum];
            }

        }



        /// <summary>
        /// Returns expanded querry with syns
        /// </summary>
        /// <param name="termsInQuery">Querey to expand</param>
        /// <returns> Querry with origina terms and all their syns</returns>
        private Dictionary<string, double> GetQueryCombindedWithSemantics(Dictionary<string, double> termsInQuery)
        {
            string termToAdd;
            Dictionary<string, double> queryCombindedWithSemantics = new Dictionary<string, double>();
            foreach (string term in termsInQuery.Keys)
            {
                queryCombindedWithSemantics[term] = termsInQuery[term];
                Set<SynSet> termSynSets = _similarityEngine.GetSynSets(term, null);
                foreach (SynSet synSet in termSynSets)
                {
                    foreach (string syn in synSet.Words)
                    {
                        // if syns consists of two wors
                        if (syn.Contains("_"))
                        {
                            string[] splittedTerm = syn.Split('_');
                            foreach (string subTerm in splittedTerm)
                            {
                                if (_useStemming)
                                    termToAdd = _stemmer.stemTerm(subTerm);
                                else
                                    termToAdd = subTerm;
                                if (!Parser.StopWords.Contains(termToAdd) && !termsInQuery.ContainsKey(termToAdd))
                                {
                                    queryCombindedWithSemantics[termToAdd] = termsInQuery[term] / wSemantics;
                                }
                            }
                            queryCombindedWithSemantics[syn.Replace('_', '-')] = termsInQuery[term] / wSemantics;
                        }
                        // if syn consists of one word
                        else
                        {
                            if (_useStemming)
                                termToAdd = _stemmer.stemTerm(syn);
                            else
                                termToAdd = syn;
                            if (!termsInQuery.ContainsKey(termToAdd))
                            {

                                queryCombindedWithSemantics[termToAdd] = termsInQuery[term] / wSemantics;
                            }
                        }

                    }
                }

            }
            return queryCombindedWithSemantics;
        }

        /// <summary>
        /// Copy synset object to list of strings
        /// </summary>
        /// <param name="synSet">Synset </param>
        /// <returns> list of string of all syns</returns>
        private List<string> CopySynsFromSynset(SynSet synSet)
        {
            List<string> allSynsInSynset = new List<string>();
            foreach (string syn in synSet.Words)
                allSynsInSynset.Add(syn);
            return allSynsInSynset;
        }

        /// <summary>
        /// Extract all documents contains terms from query and written in one of the selected languages
        /// </summary>
        /// <param name="releventPostingFilesRecords">Posting file records of term in querry</param>
        /// <param name="considerLanguages">Did user select languages of documents</param>
        /// <param name="chosenLanguages"> The languages user selecetd</param>
        /// <returns></returns>
        private HashSet<string> GetReleventDocument(Dictionary<string, PostingFileRecord> releventPostingFilesRecords, bool considerLanguages, HashSet<string> chosenLanguages)
        {
            HashSet<string> releventDocuments = new HashSet<string>();
            if (releventPostingFilesRecords == null || releventPostingFilesRecords.Count == 0)
                return releventDocuments;
            foreach (PostingFileRecord record in releventPostingFilesRecords.Values)
            {
                foreach (string document in record.DF.Keys)
                {
                    if (!releventDocuments.Contains(document) && (!considerLanguages || chosenLanguages.Contains(_documents[document].Language)))
                        releventDocuments.Add(document);
                }
            }
            return releventDocuments;
        }

        /// <summary>
        /// BM25
        /// </summary>
        /// <param name="termsInQuery">Queery </param>
        /// <param name="docNum">Document numbers</param>
        /// <param name="releventPostingFilesRecords">Posting files record</param>
        /// <param name="R">R (BM25)</param>
        /// <param name="avgDocumentLength"> Avergage document length </param>
        /// <param name="N"> number of documents in corpus</param>
        /// <returns></returns>
        private double BM25(Dictionary<string, double> termsInQuery, string docNum, Dictionary<string, PostingFileRecord> releventPostingFilesRecords, double R, double avgDocumentLength, double N)
        {
            double documentLength = _documents[docNum].DocumentLength;
            double docRank = 0;
            foreach (string termInQuery in termsInQuery.Keys)
            {
                // Check if can find relevent documents fo term

                double n;
                if (releventPostingFilesRecords.ContainsKey(termInQuery))
                    n = releventPostingFilesRecords[termInQuery].DF.Count();
                else
                    n = 0;
                double f;
                if (releventPostingFilesRecords.ContainsKey(termInQuery) && releventPostingFilesRecords[termInQuery].DF.ContainsKey(docNum))
                    f = releventPostingFilesRecords[termInQuery].DF[docNum];
                else
                    f = 0;
                double r = 0;
                double qf = termsInQuery[termInQuery];


                double K = k1 * ((1 - b) + b * documentLength / avgDocumentLength);

                double termContributionToRank;
                termContributionToRank = Math.Log(((r + 0.5) / (R - r + 0.5)) / ((n - r + 0.5) / (N - n - R + r + 0.5)));
                termContributionToRank = termContributionToRank * (k1 + 1) * f / (K + f);
                termContributionToRank = termContributionToRank * (k2 + 1) * qf / (k2 + qf);
                docRank += Math.Max(termContributionToRank, 0);
            }
            return docRank / termsInQuery.Count;
        }
        /// <summary>
        /// Score based on same terms in query and document`s title
        /// </summary>
        /// <param name="termsInQuery">Querry </param>
        /// <param name="docNum"> Document number s</param>
        /// <returns> Score </returns>
        private double TitleRank(Dictionary<string, double> termsInQuery, string docNum)
        {
            double queryLength = 0;
            foreach (int freq in termsInQuery.Values)
                queryLength += freq;
            Dictionary<string, int> titleTerms = _documents[docNum].TermsInTitle;
            double titleLength = 0;
            foreach (int freq in titleTerms.Values)
                titleLength += freq;
            double rank = 0;
            foreach (string term in termsInQuery.Keys)
            {
                double queryFreq = termsInQuery[term];
                double titleFreq;
                if (titleTerms.ContainsKey(term))
                    titleFreq = titleTerms[term];
                else
                    titleFreq = 0;
                rank += titleFreq / titleLength * queryFreq / queryLength;
            }

            return rank;
        }
    }
}
