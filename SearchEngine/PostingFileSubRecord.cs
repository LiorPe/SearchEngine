using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class PostingFileSubRecord
    {
        public int DF { get; set; }
        public string NextTokn { get; set; }

        public PostingFileSubRecord(int df, string nextToken)
        {
            DF = df;
            NextTokn = nextToken;
        }
    }
}
