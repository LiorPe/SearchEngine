using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class Searcher
    {
        // Main dictionarry of terms - saves amountt of total frequencies in all docs, name of file (posting file) in which term is saved, and
        // ptr to file (row number in which term is stored)
        Dictionary<string, TermData>[] _splittedMainDictionary;
        private Dictionary<string, DocumentData> _documnentsData;

        PostingFilesAPI _postingFilesAPI;

        public Searcher(Dictionary<string, TermData>[] splittedMainDictionary, Dictionary<string, DocumentData> documnentsData, PostingFilesAPI postingFilesAPI)
        {
            _splittedMainDictionary = splittedMainDictionary;
            _documnentsData = documnentsData;
            _postingFilesAPI = postingFilesAPI;
        }



        public List<string> GetCompletionSuggestions(string userQuery)
        {
            Dictionary<string, PostingFileRecord> postingFileRecord = _postingFilesAPI.ExtractPostingFileRecords(new string[] { userQuery }, _splittedMainDictionary, true);
            List<string> completionSuggestions = new List<string>();
            if (postingFileRecord.ContainsKey(userQuery))
                completionSuggestions = postingFileRecord[userQuery].NextTermFrequencies.Keys.ToList();
            return completionSuggestions;

        }

        public Dictionary<string,PostingFileRecord> FindReleventDocuments(Dictionary<string, int> parsedQuery)
        {
            List<PostingFileRecord> releventPostingFilesRecord = new List<PostingFileRecord>();
            return _postingFilesAPI.ExtractPostingFileRecords(parsedQuery.Keys.ToArray(), _splittedMainDictionary, false);
        }
    }
}
