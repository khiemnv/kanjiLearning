using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using Windows.Storage;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.Storage.Search;
using System.IO;
using System.Runtime.Serialization;

namespace test_universalApp
{
    [DataContract]
    public class contentProvider
    {
        public readonly content m_content;

        [DataMember]
        public Dictionary<string, chapter> m_chapters { get; private set; }

        contentProvider()
        {
            m_content = new content();
            m_chapters = new Dictionary<string, chapter>();

            m_content.SaveCompleted += M_content_SaveCompleted;
            m_content.LoadCompleted += M_content_LoadCompleted;
        }

        string dataFileName = "data.txt";
        public async Task saveData()
        {
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(dataFileName, CreationCollisionOption.ReplaceExisting);
            Stream writeStream = await dataFile.OpenStreamForWriteAsync();
            //writeStream.Seek(0, SeekOrigin.Begin);
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, chapter>));
            serializer.WriteObject(writeStream, m_chapters);
            await writeStream.FlushAsync();
            //writeStream.Flush();
            writeStream.Dispose();
        }
        public async void restoreData()
        {
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(dataFileName, CreationCollisionOption.OpenIfExists);
            if (dataFile != null) {
                Stream readStream = await dataFile.OpenStreamForReadAsync();
                Debug.Assert(readStream != null);
                try {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, chapter>));
                    var m_chapters = (Dictionary<string, chapter>)serializer.ReadObject(readStream);
                    Debug.Assert(m_chapters != null);
                } catch
                {
                    Debug.WriteLine("read data error");
                }
                readStream.Dispose();
            }
        }

        //load multiple chapter
        public async Task<int> loadMultipleChapter()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null) { 
                //var fileTypeFilter = new string[] { ".txt", ".dat" };
                //QueryOptions queryOptions = new QueryOptions(CommonFileQuery.OrderBySearchRank, fileTypeFilter);
                //queryOptions.UserSearchFilter = "chapter";
                //StorageFileQueryResult queryResult = folder.CreateFileQueryWithOptions(queryOptions);
                //IReadOnlyList<StorageFile> files = queryResult.GetFilesAsync().GetResults();
                IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
                foreach(var file in fileList)
                {
                    Debug.WriteLine(file.Name);
                    string txt = await FileIO.ReadTextAsync(file);
                    var words = parse(txt);
                    if (words.Count > 0)
                    {
                        updateDict(file, words);
                    }
                }
                OnLoadMultiChaperCompleted(new LoadChapterCompletedEventArgs() { path = folder.Path});
            }
            return -1;
        }

        //for test
        public List<word> parse(string text)
        {
            var words = new List<word>();
            var lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var word = new word(line);
                if (!word.isEmpty) words.Add(word);
            }
            return words;
        }

        public static contentProvider m_instance;
        public static contentProvider getInstance()
        {
            if (m_instance == null)
                m_instance = new contentProvider();
            return m_instance;
        }

        public async Task<int> saveWords(StorageFile file, List<word> words)
        {
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            var outputStream = stream.GetOutputStreamAt(0);
            var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream);

            uint len = 0;
            foreach (var w in words)
            {
                len += dataWriter.WriteString(w.ToString() + "\r\n");
            }
            await dataWriter.StoreAsync();
            await outputStream.FlushAsync();
            dataWriter.Dispose();
            outputStream.Dispose();
            stream.Size = len;
            stream.Dispose();

            return 0;
        }

        public async void saveChapter(chapter c)
        {
            //open file & save
            StorageFile file = c.file;
            if (file!=null)
            {
                await saveWords(file, c.words);
            }
        }

        //txt: words list
        //
        public async Task<int> saveChapter(string text)
        {
            //clear cur content data
            var words = new List<word>();
            var lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var word = new word(line);
                if (!word.isEmpty) words.Add(word);
            }
            m_content.m_words = words;
            Debug.WriteLine("{0} {1} {2}", this, "saveChapter", words.Count);
            int ret = await m_content.saveData();
            return ret;
        }

        private void M_content_LoadCompleted(object sender, EventArgs e)
        {
            updateDict();
            OnLoadChaperCompleted(new EventArgs());
        }
        private void M_content_SaveCompleted(object sender, EventArgs e)
        {
            updateDict();
        }
        private void updateDict(StorageFile file, List<word> words)
        {
            string path = file.Path;
            string key = Path.GetFileName(path);
            if (m_chapters.ContainsKey(key))
            {
                var oldWords = m_chapters[key].words;
                m_chapters[key].words = words;
                oldWords.Clear();
            }
            else
            {
                m_chapters.Add(key, new chapter() { words = words, name = key, path = path, file = file });
            }
        }
        private void updateDict()
        {
            //update chapters dict
            var file = m_content.m_file;
            var words = m_content.m_words;
            updateDict(file, words);
        }

        public async Task<int> loadChapter()
        {
            Debug.WriteLine(this + "loadChapter start");
            int ret = await m_content.loadData();
            return ret;
        }

        public class LoadChapterCompletedEventArgs:EventArgs
        {
            public string path;
        }
        public event EventHandler LoadChapterCompleted;
        protected virtual void OnLoadChaperCompleted(EventArgs e)
        {
            LoadChapterCompleted?.Invoke(this, e);
        }
        public event EventHandler<LoadChapterCompletedEventArgs> LoadMultiChapterCompleted;
        protected virtual void OnLoadMultiChaperCompleted(LoadChapterCompletedEventArgs e)
        {
            LoadMultiChapterCompleted?.Invoke(this, e);
        }
    }

    [DataContract]
    public class word
    {
        [DataMember]
        public string kanji;
        [DataMember]
        public string hiragana;
        [DataMember]
        public string hn;
        [DataMember]
        public string vn;
        [DataMember]
        public bool isEmpty;
        [DataMember]
        public bool isMarked;

        public override string ToString()
        {
            return string.Format("{0}; {1}; {2}; {3}", kanji, hiragana, hn, vn);
        }

        delegate void voidDelegate();
        struct myMap
        {
            public string pattern;
            public voidDelegate function;
        };

        public word(string txt)
        {
            //parse obj from string
            //string txt = "言葉 ことば  (NGÔN DIỆP) Câu nói";
            //txt = "積極 せいこう (THÀNH CÔNG)";
            //txt = "心配 (TÂM PHỐI)";
            //（～に）関する;  \t（～に）かんする; (QUAN); về～, liên quan～

            Regex reg = new Regex(@"\s+");
            var refine = reg.Replace(txt, " ");

            {//with separator ";"
                string part = @";\s*";
                Regex regSplit = new Regex(part);
                var tmp = regSplit.Split(refine);
                if (tmp.Length == 4)
                {
                    kanji = tmp[0];
                    hiragana = tmp[1];
                    hn = tmp[2];
                    vn = tmp[3];
                    return;
                }
            }

            string pattern = @"(\w+) (\w+) (\(.*\)) (.*)";
            string pattern2 = @"(\w+) (\w+) (\(.*\))";
            string pattern3 = @"(\w+) (\(.*\))";
            //string pattern4 = @"(.*); (.*); (.*); (.*)";
            Match m = null;
            var func1 = new voidDelegate(() =>
            {
                kanji = m.Groups[1].Value;
                hiragana = m.Groups[2].Value;
                hn = m.Groups[3].Value;
                vn = m.Groups[4].Value;
            });
            var func2 = new voidDelegate(() =>
            {
                kanji = m.Groups[1].Value;
                hiragana = m.Groups[2].Value;
                hn = m.Groups[3].Value;
                vn = "";
            });
            var func3 = new voidDelegate(() =>
            {
                kanji = m.Groups[1].Value;
                hiragana = "";
                hn = m.Groups[2].Value;
                vn = "";
            });
            //var func4 = new voidDelegate(() =>
            //{
            //    kanji = m.Groups[1].Value;
            //    hiragana = m.Groups[2].Value;
            //    hn = m.Groups[3].Value;
            //    vn = m.Groups[4].Value;
            //});

            List<myMap> arr = new List<myMap> {
                    //new myMap() {pattern = pattern4, function = func4},
                    new myMap() {pattern = pattern, function = func1},
                    new myMap() {pattern = pattern2, function = func2},
                    new myMap() {pattern = pattern3, function = func3},
                };

            isEmpty = true;
            foreach (var i in arr)
            {
                reg = new Regex(i.pattern);
                m = reg.Match(refine);
                if (m.Success)
                {
                    i.function();
                    isEmpty = false;
                    break;
                }
            }
        }
    }

    [DataContract]
    public class chapter
    {
        [DataMember]
        public bool selected;
        [DataMember]
        public string path;
        [DataMember]
        public string name;
        [DataMember]
        public List<word> words;

        public StorageFile file;
    }

    public class content
    {
        public event EventHandler LoadCompleted;
        protected virtual void OnLoadCompleted(EventArgs e)
        {
            LoadCompleted?.Invoke(this, e);
        }
        public event EventHandler SaveCompleted;
        protected virtual void OnSaveCompleted(EventArgs e)
        {
            SaveCompleted?.Invoke(this, e);
        }

        public List<word> m_words;
        public StorageFile m_file;
        public string m_fileName;

        public async Task<int> loadData()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".dat");
            picker.FileTypeFilter.Add("*");

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                m_file = file;
                m_fileName = file.Name;
                if (m_words == null) m_words = new List<word>();
                loadData(file);
                return 0;
            }
            return -1;
        }

        //assume:
        //  path exist
        //  m_words not null
        private async void loadData(StorageFile file)
        {
            //Debug.Assert(path != null);
            //Debug.Assert(System.IO.File.Exists(path));
            Debug.Assert(m_words != null);

            //StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            string text = await FileIO.ReadTextAsync(file);
            var lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                m_words.Add(new word(line));
            }

            OnLoadCompleted(new EventArgs());
        }

        public async Task<int> saveData()
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            picker.FileTypeChoices.Add("Plain text", new List<string>() { ".txt" });
            StorageFile file = await picker.PickSaveFileAsync();

            if (file != null && m_words != null) {
                m_fileName = file.Name;
                saveData(file);
                return 0;
            }
            return -1;
        }

        //req:
        //  path file exist
        //  m_words has words
        private async void saveData(StorageFile file)
        {
            //Debug.Assert(path != null);
            //Debug.Assert(System.IO.File.Exists(path));
            Debug.Assert(m_words != null);

            //StorageFile file = await StorageFile.GetFileFromPathAsync(path);

            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            var outputStream = stream.GetOutputStreamAt(0);
            var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream);
            foreach (var w in m_words)
            {
                dataWriter.WriteString(w.ToString() + "\r\n");
            }
            await dataWriter.StoreAsync();
            await outputStream.FlushAsync();
            dataWriter.Dispose();
            outputStream.Dispose();
            stream.Dispose();
        }
    }
}
