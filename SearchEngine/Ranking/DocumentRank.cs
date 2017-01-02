using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.Ranking
{
    public class DocumentRank
    {
        public string QueryId { get; set; }
        public int Rank { get; set; }
        public string DocumentNo { get; set; }
        public double Score { get; set; }

        public DocumentRank(string queryId, int rank, string documentNo, double score)
        {
            QueryId = queryId;
            Rank = rank;
            DocumentNo = documentNo;
            Score = score;
        }
        public override string ToString()
        {
            //query_id, iter, docno, rank, sim, run_id
            return String.Format("{0} {1} {2} {3} {4} {5}", QueryId, 0, DocumentNo, Rank, Score, "user"); 
        }

    }
}
