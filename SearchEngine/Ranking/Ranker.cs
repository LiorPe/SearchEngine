using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.Ranking
{
    public class Ranker
    {
        public double k1 = 1.2;
        public double k2 = 500;
        public double b = 0.75;
        Dictionary<string, DocumentData> _documents;
        Dictionary<string, TermData>[] _splittedMainDictionary;

        public Ranker(Dictionary<string, DocumentData> documents, Dictionary<string, TermData>[] splittedMainDictionary)
        {
            _documents = documents;
            _splittedMainDictionary = splittedMainDictionary;
        }

        public DocumentRank[] RankDocuments(Dictionary<string, int> termsInQuery, string queryID, Dictionary<string, PostingFileRecord> releventPostingFilesRecords, DocumentRank[] previousRankedDocuments, double avgDocumentLength,HashSet<string> chosenLanguages)
        {
            bool considerLanguages = chosenLanguages.Count > 0;
            //Extract all relevent documents
            HashSet<string> releventDocuments = new HashSet<string>();
            foreach (PostingFileRecord record in releventPostingFilesRecords.Values)
            {
                foreach (string document in record.DF.Keys)
                {
                    if (!releventDocuments.Contains(document) && (!considerLanguages || chosenLanguages.Contains(_documents[document].Language) ) )
                        releventDocuments.Add(document);
                }
            }

            Dictionary<string, double> documentRanking = new Dictionary<string, double>();
            double N = _documents.Count();
            // Check if can find relevent documents fo query
            double R = 0;
            foreach (string docNum in releventDocuments)
                documentRanking[docNum] = CalCulateRankOfDocument(termsInQuery, docNum, releventPostingFilesRecords, R, avgDocumentLength, N);

            var sortedRank = documentRanking.OrderByDescending(i => i.Value);
            int numOfResults = Math.Min(50, sortedRank.Count());
            DocumentRank[] sortedRankedDocuments = new DocumentRank[previousRankedDocuments.Length + numOfResults];
            int rankIndexer = 0;
            for (int i=0;i< previousRankedDocuments.Length;i++, rankIndexer++)
            {
                sortedRankedDocuments[rankIndexer] = previousRankedDocuments[i];
            }
            for (int i=1; i<= numOfResults; i++, rankIndexer++)
            {
                var rankedResut = sortedRank.ElementAt(i - 1);
                sortedRankedDocuments[rankIndexer] = new DocumentRank(queryID, i, rankedResut.Key, rankedResut.Value);
            }

            return sortedRankedDocuments;
        }

        private double CalCulateRankOfDocument(Dictionary<string, int> termsInQuery, string docNum, Dictionary<string, PostingFileRecord> releventPostingFilesRecords,double R,double avgDocumentLength,double N)
        {
            double documentLength = _documents[docNum].DocumentLength;
            double docRank = 0;
            foreach (string termInQuery in termsInQuery.Keys)
            {
                // Check if can find relevent documents fo term
                double r = 0;
                double n = releventPostingFilesRecords[termInQuery].DF.Count;
                double f = releventPostingFilesRecords[termInQuery].DF[docNum];
                double qf = termsInQuery[termInQuery];

                double K = k1 * ((1 - b) + b * documentLength / avgDocumentLength);
                double termContributionToRank = (r + 0.5) / (R - r + 0.5) * (k1 + 1) * f * (k2 + 1) * qf;
                termContributionToRank = termContributionToRank / (n - r + 0.5) / (N - n - R + r + 0.5) / (K + f) / (k2 + qf);
                termContributionToRank = Math.Log(termContributionToRank);
                docRank += termContributionToRank;
            }
            return docRank;
        }
    }
}
