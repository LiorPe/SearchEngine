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

        PostingFilesManager _postingFilesAPI;

        public Searcher(Dictionary<string, TermData>[] splittedMainDictionary, Dictionary<string, DocumentData> documnentsData, PostingFilesManager postingFilesAPI)
        {
            _splittedMainDictionary = splittedMainDictionary;
            _documnentsData = documnentsData;
            _postingFilesAPI = postingFilesAPI;
        }


        /// <summary>
        /// Gets completion for user querry
        /// </summary>
        /// <param name="userQuery"> querry </param>
        /// <returns>List of suggenstion to complete querry</returns>
        public List<string> GetCompletionSuggestions(string userQuery)
        {
            // Extract posting files records of the words] user submitted
            Dictionary<string, PostingFileRecord> postingFileRecord = _postingFilesAPI.ExtractPostingFileRecords(new string[] { userQuery }, _splittedMainDictionary, true);
            List<string> completionSuggestions = new List<string>();
            // If term exists in posting files - get all its completion suggestions
            if (postingFileRecord.ContainsKey(userQuery))
                completionSuggestions = postingFileRecord[userQuery].NextTermInAllDocuments.Keys.ToList();
            return completionSuggestions;

        }

        /// <summary>
        /// Get all posting records of terms in querry
        /// </summary>
        /// <param name="parsedQuery">Querry after parsing</param>
        /// <returns>Posting file records of term in querry</returns>
        public Dictionary<string,PostingFileRecord> FindReleventDocuments(Dictionary<string, int> parsedQuery)
        {
            List<PostingFileRecord> releventPostingFilesRecord = new List<PostingFileRecord>();
            return _postingFilesAPI.ExtractPostingFileRecords(parsedQuery.Keys.ToArray(), _splittedMainDictionary, false);
        }
    }
}
