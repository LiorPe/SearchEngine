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
        public double k1 = 1.2;
        public double k2 = 100;
        public double b = 0.75;
        public double w1 = 2;
        public double w2 = 0.8;
        Dictionary<string, DocumentData> _documents;
        Dictionary<string, TermData>[] _splittedMainDictionary;
        int resultsToRetrieve = 50;
        WordNetEngine _similarityEngine;
        Searcher _searcher;
        Stemmer _stemmer;
        bool _useStemming;
        public Ranker(Dictionary<string, DocumentData> documents, Dictionary<string, TermData>[] splittedMainDictionary, Searcher searcher, bool useStemming)
        {
            _documents = documents;
            _splittedMainDictionary = splittedMainDictionary;
            string root = Path.GetDirectoryName(Application.ExecutablePath);
            _similarityEngine = new WordNetEngine(@"WNdb-3.0", true);
            _searcher = searcher;
            _stemmer = new Stemmer();
            _useStemming = useStemming;
        }

        public DocumentRank[] RankDocuments(Dictionary<string, int> termsInQuery, string queryID, Dictionary<string, PostingFileRecord> releventPostingFilesRecords,
            DocumentRank[] previousRankedDocuments, double avgDocumentLength, HashSet<string> chosenLanguages)
        {
            bool considerLanguages = chosenLanguages.Count > 0;
            //Extract all relevent documents
            HashSet<string> releventDocuments = GetReleventDocument(releventPostingFilesRecords, considerLanguages, chosenLanguages);


            Dictionary<string, double> documentRanking = new Dictionary<string, double>();

            double N = _documents.Count();
            // Check if can find relevent documents fo query
            double R = 0;

            foreach (string docNum in releventDocuments)
            {
                documentRanking[docNum] = BM25(termsInQuery, docNum, releventPostingFilesRecords, R, avgDocumentLength, N);
                documentRanking[docNum] += w1 * TitleRank(termsInQuery, docNum);

            }
            string synToAdd;
            Dictionary<string, PostingFileRecord> releventPostingFilesRecordsWithSemanitc;
            Dictionary<string, double> documentRankingWithSemantics = new Dictionary<string, double>();
            HashSet<string> syns = new HashSet<string>();
            Dictionary<string, int> queryCominedWithSynonyms = new Dictionary<string, int>();
            foreach (string term in termsInQuery.Keys)
            {
                queryCominedWithSynonyms[term] = termsInQuery[term];
                Set<SynSet> synsets = _similarityEngine.GetSynSets(term, null);
                foreach (SynSet syn in synsets)
                {
                    foreach (string synonym in syn.Words)
                    {
                        if (_useStemming)
                            synToAdd = _stemmer.stemTerm(synonym);
                        else
                            synToAdd = synonym;
                        syns.Add(synToAdd);
                        if (queryCominedWithSynonyms.ContainsKey(synToAdd))
                            queryCominedWithSynonyms[synToAdd] += termsInQuery[term];
                        else
                            queryCominedWithSynonyms[synToAdd] = termsInQuery[term];
                    }

                }

            }
            releventPostingFilesRecordsWithSemanitc = _searcher.FindReleventDocuments(queryCominedWithSynonyms);
            HashSet<string> releventDocumentsWithSemantic = GetReleventDocument(releventPostingFilesRecordsWithSemanitc, false, chosenLanguages);
            foreach (string docNum in releventDocumentsWithSemantic)
            {
                if (!documentRankingWithSemantics.ContainsKey(docNum))
                    documentRankingWithSemantics[docNum] = 0;
                documentRankingWithSemantics[docNum] += BM25(queryCominedWithSynonyms, docNum, releventPostingFilesRecordsWithSemanitc, R, avgDocumentLength, N,syns);
                documentRankingWithSemantics[docNum] += w1 * TitleRank(queryCominedWithSynonyms, docNum);

            }

            for (int i = 0; i < documentRankingWithSemantics.Count; i++)
            {
                string docNum = documentRankingWithSemantics.Keys.ElementAt(i);
                documentRankingWithSemantics[docNum] = documentRankingWithSemantics[docNum] / termsInQuery.Count;
                if (!documentRanking.ContainsKey(docNum))
                    documentRanking[docNum] = 0;
                documentRanking[docNum] += w2 * documentRankingWithSemantics[docNum];
            }


            var sortedRank = documentRanking.OrderByDescending(i => i.Value);
            int numOfResults = Math.Min(resultsToRetrieve, sortedRank.Count());
            List<DocumentRank> sortedRankedDocuments = new List<DocumentRank>();
            int rankIndexer = 0;
            for (int i = 0; i < previousRankedDocuments.Length; i++, rankIndexer++)
            {
                sortedRankedDocuments.Add(previousRankedDocuments[i]);
            }
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

        private HashSet<string> GetReleventDocument(Dictionary<string, PostingFileRecord> releventPostingFilesRecords, bool considerLanguages, HashSet<string> chosenLanguages)
        {
            HashSet<string> releventDocuments = new HashSet<string>();
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


        private double BM25(Dictionary<string, int> termsInQuery, string docNum, Dictionary<string, PostingFileRecord> releventPostingFilesRecords, double R, double avgDocumentLength, double N, HashSet<string> syns = null)
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
                if (syns!=null && syns.Contains(termInQuery))
                    qf = qf / 1.5;


                    double K = k1 * ((1 - b) + b * documentLength / avgDocumentLength);

                double termContributionToRank;
                termContributionToRank = Math.Log(((r + 0.5) / (R - r + 0.5)) / ((n - r + 0.5) / (N - n - R + r + 0.5)));
                termContributionToRank = termContributionToRank * (k1 + 1) * f / (K + f);
                termContributionToRank = termContributionToRank * (k2 + 1) * qf / (k2 + qf);
                docRank += Math.Max(termContributionToRank, 0);
            }
            return docRank;
        }
        private double TitleRank(Dictionary<string, int> termsInQuery, string docNum)
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
