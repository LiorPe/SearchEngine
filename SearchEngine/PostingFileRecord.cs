using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class PostingFileRecord
    {
        //maps document to df of term 
        public Dictionary<string,int> DF { get; set; }
        //maps terms after this term to its frequency after this terms
        public Dictionary<string, int> NextTermFrequencies { get; set; }

        public PostingFileRecord()
        {
            DF = new Dictionary<string, int>() ;
            NextTermFrequencies =new Dictionary<string, int>();
        }
    }
}
