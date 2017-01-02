using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.Ranking
{
    public class Ranker
    {
        Dictionary<string, DocumentData> _documents;
        Dictionary<string, TermData>[] _splittedMainDictionary;

        public Ranker(Dictionary<string, DocumentData> documents, Dictionary<string, TermData>[] splittedMainDictionary)
        {
            _documents = documents;
            _splittedMainDictionary = splittedMainDictionary;
        }

        public List<DocumentRank> RankDocuments(string query, string queryID, List<PostingFileRecord> releventDocuments, List<DocumentRank> previousRankedDocuments)
        {
            throw new NotImplementedException();
        }
    }
}
