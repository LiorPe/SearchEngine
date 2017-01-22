*****************************************************
*                                                   *
* Information Retrival Search Engine Project        *
*  Date:  August 21, 2017                           *
*  Authors:  Lior Perry                             *
*            Ido Solomon                  	    *
*                                                   *
*****************************************************

                          
I. File list
-------------------
GUI
	App.config
	App.xaml
	App.xaml.cs
	DictionaryWindow.xaml
	DictionaryWindow.xaml.cs
	GUI.csproj
	IndeterminateProgressWindow.xaml
	IndeterminateProgressWindow.xaml.cs
	MainWindow.xaml
	MainWindow.xaml.cs
	ProgressWindow.xaml
	ProgressWindow.xaml.cs
	StatisticsWindow.xaml
	StatisticsWindow.xaml.cs

SearchEngine
	App.config
	DocumentData.cs
	FileReader.cs
	Indexer.cs
	Parser.cs
	PostingFileRecord.cs
	PostingFilesManager.cs
	SearchEngine.csproj
	Searcher.cs
	Stemmer.cs
	TermData.cs
	TermFrequency.cs



II. Instructions
-----------------
Program has 2 tabs: 
	i. "Part A - Indexing corpus"
	ii. "Part B - Submitting querries".
in order to make part B accessible and search queries, you must first index a corpus or load 
dictionary from file. we`ll explain about how use there two parts:

i. "Part A - Indexing corpus"
-------------------------------
A. Creating a dictionary from a new corpus

1. Click on the Browse button next to the source path box and select the folder where your corpus
and stop-words files are located.
Alternatively, manually enter the full directory path into the box.

2. Click on the Browse button next to the destination path box and select the folder where you wish
to save the new dictionary to.
Alternatively, manually enter the full directory path into the box.

3. Select/Deselect the Stemming checkbox to enable/disable term stemming during the indexing process.

4. Click on the Index Corpus button. A new window will appear, showing the progress of the indexing
process. Once the progress reaches 100%, the window will close, and the dictionary file will be found
in the destination directory you have provided.

5. You can now click on the Show Dictionary button to show a window listing the various unique terms
in the dictionary, along with their collection frequency.


B. Loading an existing dictionary from file

1. Click on the Browse button next to the destination path box and select the folder containing the
pre-indexed dictionary.
Alternatively, manually enter the full directory path into the box.

3. Select/Deselect the Stemming checkbox to declare which type of dictionary is to be loaded.

4. Click on the LOAD button. A new window will appear, showing the progress of the loading
process. Once the loading ends, the window will close.

5. You can now click on the Show Dictionary button to show a window listing the various unique terms
in the dictionary, along with their collection frequency.


C. Reseting the process memory

1. Click on the RESET button. This will reset the process' main memory, as well as delete the posting
files and dictionary in the destination directory.


D. Selecting a document language

1. Once a dictionary has been created or loaded, the document language dropdown box will populate
with the various languages in the dictionary.

2. Selecting a language currently does nothing.


i. "Part B - Submitting Querrties"
-------------------------------
There are 2 possible ways for submitting querries:
1. Load a query from file.
2. Submit a query in free language. - when you begin to write a query, our engine will auto complete the first
word you wrote. 
After you submit a query in one of the way specified (you can also both submit a query and upload a file
to search them simultaneously) , you can click on "Search your query" in order to start finding results. 
After a few seconds, the results of all queries submitted will appear on screen, presented in decsending 
order by their rank (From the most relevent to the least).
You can save results to external file in order to use them later.

We hope you`ll find our engine useful,
Lior Perry and Ido Solomon.

