﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Xml;
using Windows.Storage.Streams;
using Windows.Storage.AccessCache;
#if use_sqlite
using Microsoft.Data.Sqlite;
using Microsoft.Data.Sqlite.Internal;
#endif
using Windows.Storage.FileProperties;

namespace test_universalApp
{
    [DataContract]
    public class contentProvider:IDisposable
    {
        public readonly content m_content;
        public myDb m_db;
        public myMainPgCfg m_mainPgCfg;
        public myChapterPgCfg m_chapterPgCfg;
        public myLessonPgCfg m_lessonPgCfg;

        [DataMember]
        public Dictionary<string, chapter> m_chapters { get; private set; }

        [DataMember]
        public Dictionary<string, lessons> m_lessons { get; private set; }

        public void loadDb()
        {
            if (m_chapterPgCfg == null) throw new Exception("not init pg cfg");
            m_db.load(m_chapterPgCfg);
        }
        public void unloadDb()
        {
            if (m_chapterPgCfg == null) throw new Exception("not init pg cfg");
            m_db.unload(m_chapterPgCfg);
        }
#if use_sqlite
        public SqliteConnection m_cnn;
        public async void loadData()
        {
            if (m_cnn == null)
            {
                string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "db.sqlite");
                SqliteEngine.UseWinSqlite3();
                m_cnn = new SqliteConnection("Filename=study.db");
                await m_cnn.OpenAsync();
                string sql = " CREATE TABLE IF NOT EXISTS chapterInfo ("
                + " id INTEGER PRIMARY KEY AUTOINCREMENT,"
                + " path text,"
                + " marked text)";
                var cmd = new SqliteCommand(sql, m_cnn);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        public void unloadDb()
        {
            Debug.WriteLine("{0} unloadDb {1}", this, m_cnn);
            if (m_cnn != null)
            {
                m_cnn.Close();
                m_cnn.Dispose();
                m_cnn = null;
            }
        }
#endif
        contentProvider()
        {
            m_content = new content();
            m_chapters = new Dictionary<string, chapter>();
            m_lessons = new Dictionary<string, lessons>();
            m_db = new myDb();
            m_mainPgCfg = myMainPgCfg.getInstance();
            m_chapterPgCfg = myChapterPgCfg.getInstance();
            m_lessonPgCfg = myLessonPgCfg.getInstance();

            m_content.SaveCompleted += M_content_SaveCompleted;
            m_content.LoadCompleted += M_content_LoadCompleted;
        }

        string c_dataFileName = "data.txt";
        public async Task saveData()
        {
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(c_dataFileName, CreationCollisionOption.ReplaceExisting);
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
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(c_dataFileName, CreationCollisionOption.OpenIfExists);
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
        public async Task<int> loadMultipleChapter(BackgroundWorker m_worker, StorageFolder folder)
        {
            if (folder != null)
            {
                //var fileTypeFilter = new string[] { ".txt", ".dat" };
                //QueryOptions queryOptions = new QueryOptions(CommonFileQuery.OrderBySearchRank, fileTypeFilter);
                //queryOptions.UserSearchFilter = "chapter";
                //StorageFileQueryResult queryResult = folder.CreateFileQueryWithOptions(queryOptions);
                //IReadOnlyList<StorageFile> files = queryResult.GetFilesAsync().GetResults();
                IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
                int count = 0;
                foreach (var file in fileList)
                {
                    Debug.WriteLine(file.Name);
                    string txt = await FileIO.ReadTextAsync(file);
                    var words = parse(txt);
                    if (words.Count > 0)
                    {
                        updateDict(file, words);
                    }
                    count++;
                    m_worker.ReportProgress(count * 100 / fileList.Count);
                }
                //OnLoadMultiChaperCompleted(new LoadChapterCompletedEventArgs() { path = folder.Path });
            }
            return -1;
        }

        public async Task<int> loadSingleChapter(StorageFile file)
        {
            if (file !=null)
            {
                {
                    Debug.WriteLine(file.Name);
                    string txt = await FileIO.ReadTextAsync(file);
                    var words = parse(txt);
                    if (words.Count > 0)
                    {
                        updateDict(file, words);
                    }
                }
                return 0;
            }
            return -1;
        }
        public async Task<int> loadSingleLesson(StorageFile file)
        {
            if (file != null)
            {
                {
                    Debug.WriteLine(file.Name);
                    string txt = await FileIO.ReadTextAsync(file);
                    var sentences = parseLesson(txt);
                    if (sentences.Count > 0)
                    {
                        updateLessonsDict(file, sentences);
                    }
                }
                return 0;
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
        public List<string> parseLesson(string text)
        {
            var senctences = new List<string>();
            var lines = text.Split(new char[] { '\r', '\n', '.' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                senctences.Add(line);
            }
            return senctences;
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

#if use_sqlite
        public async void getMarked(chapter c)
        {
            string pathField = "path";
            string markedField = "marked";
            string table = "chapterInfo";
            SqliteParameter param = new SqliteParameter()
            {
                ParameterName = string.Format("@{0}", pathField),
                Value = c.path
            };
            string sql = string.Format("select {2} from {0} where {1}=@{1}", table, pathField, markedField);
            SqliteCommand cmd = new SqliteCommand(sql, m_cnn);
            cmd.Parameters.Add(param);
            var ret = await cmd.ExecuteScalarAsync();
            var indexs = new List<int>();
            if (ret!=null)
            {
                string txt = ret.ToString();
                var arr = txt.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var i in arr)
                {
                    indexs.Add(int.Parse(i));
                }
            }
            c.markedIndexs = indexs;
        }
        public async void updateMarked(chapter c)
        {
            string pathField = "path";
            string idField = "id";
            string markedField = "marked";
            string table = "chapterInfo";

            string sql = string.Format("select {2} from {0} where {1}=@{1}", table, pathField, idField);
            SqliteParameter param = new SqliteParameter
            {
                ParameterName = string.Format("@{0}", pathField),
                Value = c.path,
                DbType = System.Data.DbType.String
            };
            SqliteCommand cmd = new SqliteCommand(sql, m_cnn);
            cmd.Parameters.Add(param);
            var ret = await cmd.ExecuteScalarAsync();
            Debug.WriteLine("{0} updateMarked find {2} {1}", this, ret, c.path);

            string markedVal = string.Join(";", c.markedIndexs);
            List<SqliteParameter> arrParams = new List<SqliteParameter> {
                new SqliteParameter {ParameterName = "@"+markedField, Value = markedVal,
                    DbType =System.Data.DbType.String }
            };

            if (ret != null)
            {
                string idVal = ret.ToString();
                sql = string.Format("update {0} set {1}=@{1} where {2}=@{2}", table, markedField, idField);
                arrParams.Add(new SqliteParameter { ParameterName="@"+idField, Value=idVal,
                    DbType =System.Data.DbType.UInt64 });
            }
            else
            {
                sql = string.Format("insert into {0} ({1}, {2}) values (@{1}, @{2}) ",
                    table, pathField, markedField);
                arrParams.Add(new SqliteParameter { ParameterName = "@" + pathField, Value = c.path, DbType = System.Data.DbType.String });
            }
            cmd.CommandText = sql;
            cmd.Parameters.Clear();
            cmd.Parameters.AddRange(arrParams);
            int x = await cmd.ExecuteNonQueryAsync();
            Debug.WriteLine("{0} updateMarked complete {1} {2}", this, x, Environment.TickCount);
        }
#endif
#if not_use_sqlite
        public async void getMarked(chapter c)
        {
            List<int> indexs = new List<int>();
            XmlDocument xd = new XmlDocument();
            StorageFile file;

            XmlElement found = null;
            do
            {
                try
                {
                    file = await ApplicationData.Current.LocalFolder.GetFileAsync(path);
                }
                catch
                {
                    //if file not exist, create new
                    break;
                }

                IRandomAccessStream textStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                Stream stream = textStream.AsStreamForRead();

                xd.Load(stream);
                XmlElement root = xd.DocumentElement;

                foreach (XmlElement node in root.ChildNodes)
                {
                    if (node.GetAttribute("path") == c.path)
                    {
                        found = node;
                        break;
                    }
                }
                stream.Dispose();
            } while (false);
            if (found!=null)
            {
                string txt = found.GetAttribute("markedIndex");
                var arr = txt.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var i in arr)
                {
                    indexs.Add(int.Parse(i));
                }
            }
            c.markedIndexs = indexs;
        }
        string path = "study.data";
        public async void updateMarked(chapter c)
        {
            XmlDocument xd = new XmlDocument();
            StorageFile file;
            bool isNewFile = false;

            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(path);
            } catch { 
                //if file not exist, create new
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);
                isNewFile = true;
            }

            IRandomAccessStream textStream = await file.OpenAsync(FileAccessMode.ReadWrite);
            Stream stream = textStream.AsStreamForRead();

            XmlElement root = null;
            if (isNewFile)
            {
                root = xd.CreateElement("chapter");
                xd.AppendChild(root);
            }
            else
            {
                xd.Load(stream);
                root = xd.DocumentElement;
            }

            XmlElement found = null;
            foreach (XmlElement node in root.ChildNodes)
            {
                if (node.GetAttribute("path") == c.path)
                {
                    found = node;
                    break;
                }
            }

            string txt = string.Join(";", c.markedIndexs);
            if (found == null)
            {
                XmlElement e = xd.CreateElement(c.name);
                e.SetAttribute("path", c.path);
                e.SetAttribute("markedIndex", txt);
                root.AppendChild(e);
            } else
            {
                found.SetAttribute("markedIndex", txt);
            }

            stream.SetLength(0);
            xd.Save(stream);
            stream.Dispose();
            textStream.Dispose();
        }
#endif //not_use_sqlite

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
            updateChapterDict();
            OnLoadChaperCompleted(new EventArgs());
        }
        private void M_content_SaveCompleted(object sender, EventArgs e)
        {
            updateChapterDict();
        }
        private void updateDict(StorageFile file, List<word> words)
        {
            string path = file.Path;
            string name = Path.GetFileNameWithoutExtension(path);
            string key = path;
            if (m_chapters.ContainsKey(key))
            {
                //var oldWords = m_chapters[key].words;
                m_chapters[key].words = words;
                //oldWords.Clear();
            }
            else
            {
                m_chapters.Add(key, new chapter() { words = words, name = name, path = path, file = file });
            }
        }
        private void updateLessonsDict(StorageFile file, List<string> sentences)
        {
            string path = file.Path;
            string name = Path.GetFileNameWithoutExtension(path);
            string key = path;
            if (m_lessons.ContainsKey(key))
            {
                //var oldWords = m_chapters[key].words;
                m_lessons[key].sentences = sentences;
                //oldWords.Clear();
            }
            else
            {
                m_lessons.Add(key, new lessons() { sentences = sentences, name = name, path = path, file = file });
            }
        }
        private void updateChapterDict()
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

        #region markerd
        public void saveMarkeds()
        {
            if (m_chapters.Count > 0)
            {
                //data was loaded
                //save marked
                foreach (var key in m_chapterPgCfg.selectedChapters)
                {
                    var ch = m_chapters[key];
                    m_db.saveMarked(ch);
                }
            }
#if test_save_marked
            m_db.save();
#endif
        }
        public async Task saveMarkedsAsyn()
        {
            if (m_chapters.Count > 0)
            {
                //data was loaded
                //save marked
                foreach (var key in m_chapterPgCfg.selectedChapters)
                {
                    var ch = m_chapters[key];
                    m_db.saveMarked(ch);
                }
            }
            await m_db.saveAsyn();
        }
#endregion

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    unloadDb();
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~contentProvider()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
#endregion
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

    public class chapter: chapterInfo
    {
        public bool selected;
//        public string path;
        public string name;
        //public List<int> markedIndexs;

        public List<word> words;

        public StorageFile file { get {
                throw new Exception();
            }
            set
            {
                Debug.WriteLine(value.Path);
            }
        }
    }

    public class lessons
    {
        public bool selected;
        //        public string path;
        public string name;
        //public List<int> markedIndexs;

        public string path;
        public List<string> sentences;

        public int readed;

        public StorageFile file
        {
            get
            {
                throw new Exception();
            }
            set
            {
                Debug.WriteLine(value.Path);
            }
        }
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

        public content()
        {
            m_words = new List<word>();
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

    public class rHistory
    {
        const int m_MAXCOUNT = 16;
        int nCount;
        int ifirst;
        int iCur;
        string[] data;
        public rHistory()
        {
            nCount = 0;
            ifirst = 0;
            iCur = -1; //zero base
            data = new string[m_MAXCOUNT];
        }
        public string print()
        {
            var txt = "";
            for (int i = 0; i < nCount; i++)
            {
                var j = (i + ifirst) % m_MAXCOUNT;
                if (i == iCur)
                    txt = txt + " [" + data[j] + "]";
                else
                    txt = txt + " " + data[j];
            }
            return txt;
        }
        //...[newData]
        //   cursor
        public void add(string txt)
        {
            //[first]...[curosr]...[last]
            //<-----n------------------->
            Debug.Assert(iCur < nCount);
            //[first]...[curosr][+1]
            //<-----n-------------->
            if (iCur < (m_MAXCOUNT - 1))
            {
                //|0|1| ......|i]|i+1|
                //^first ...      ^curosr
                //<-----n = -------->
                iCur++;
                nCount = iCur + 1;
            }
            else
            {
                //data|0|1
                //>>    |0|1| ......|max-1]
                //       ^first ...   ^curosr
                //      <-----n = max----->
                ifirst = (ifirst + 1) % m_MAXCOUNT;
            }

            var i = (ifirst + iCur) % m_MAXCOUNT;
            data[i] = txt;
        }
        //[cursor][next]
        public string next()
        {
            //|0|...|i+1|      |n-1|
            //       ^cur       ^last
            var ret = "";
            if (iCur < (nCount - 1))
            {
                iCur++;
                var i = (iCur + ifirst) % m_MAXCOUNT;
                ret = data[i];
            }
            return ret;
        }
        //[prev][cursor]
        public string prev()
        {
            //|0|.....[i-1]|i|
            // ^first  ^cur
            var ret = "";
            if (iCur > 0)
            {
                iCur--;
                var i = (iCur + ifirst) % m_MAXCOUNT;
                ret = data[i];
            }
            return ret;
        }
        public string cur()
        {
            //|0|.....[i-1]|i|
            // ^first  ^cur
            var ret = "";
            if (iCur >= 0)
            {
                var i = (iCur + ifirst) % m_MAXCOUNT;
                ret = data[i];
            }
            return ret;
        }
    }

    [DataContract]
    public class myConfig
    {
        public myConfig() { }
        protected async Task saveAsyn<T>()
        {
            //object configData = this;
            //string configFile = m_configFile;
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                m_configFile, CreationCollisionOption.OpenIfExists);
            Stream writeStream = await dataFile.OpenStreamForWriteAsync();
            writeStream.SetLength(0);
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            serializer.WriteObject(writeStream, this);
            await writeStream.FlushAsync();
            writeStream.Dispose();
        }

        protected async Task<object> load<T>()
        {
            string cfgFile = m_configFile;
            object ret = null;
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                cfgFile, CreationCollisionOption.OpenIfExists);
            BasicProperties bs = await dataFile.GetBasicPropertiesAsync();
            if (bs.Size > 0)
            {
                Stream stream = await dataFile.OpenStreamForReadAsync();
                stream.Seek(0, SeekOrigin.Begin);
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                var obj = serializer.ReadObject(stream);
                if (obj != null) { ret = obj; }
                stream.Dispose();
            }
            //await Task.Delay(1000);
            Debug.WriteLine("{0} load complete {1}", cfgFile, Environment.TickCount);
            return ret;
        }
        protected string m_configFile;
        protected object m_configData;
    }

#region main_pg_config
    [DataContract]
    public class myMainPgCfg:myConfig
    {
        [DataMember]
        public EStudyMode studyMode;

        public enum EStudyMode
        {
            learningWords,
            readingNews,
        }

        public myMainPgCfg() {
            m_configFile = "mainPg.cfg";
        }
        static myMainPgCfg m_config;
        public static myMainPgCfg getInstance()
        {
            if (m_config == null)
            {
                var cfgObj = new myMainPgCfg();
                var t = Task.Run(async () => m_config = (myMainPgCfg) await cfgObj.load<myMainPgCfg>());
                t.Wait();
                if (m_config == null)
                {
                    m_config = cfgObj;
                }
                m_config.m_configData = m_config;
            }
            return m_config;
        }
        public void save()
        {
            var t = Task.Run(() => saveAsyn<myMainPgCfg>());
            t.Wait();
        }
    }
#endregion
#region lesson_pg_config
    [DataContract]
    public class myLessonPgCfg:myConfig
    {
        [DataMember]
        public string mruToken;
        [DataMember]
        public string lastPath;
        [DataMember]
        public List<string> selectedLessons;

        public myLessonPgCfg()
        {
            mruToken = "";
            lastPath = "";
            selectedLessons = new List<string>();
            m_configFile = "lessonPg.cfg";
        }
        static myLessonPgCfg m_config;

        public static myLessonPgCfg getInstance()
        {
            if (m_config == null)
            {
                var newObj = new myLessonPgCfg();
                var t = Task.Run(async () => m_config = (myLessonPgCfg) await newObj.load<myLessonPgCfg>());
                t.Wait();
                if (m_config == null)
                {
                    m_config = new myLessonPgCfg();
                }
            }
            return m_config;
        }
        public void save()
        {
            var t = Task.Run(() => saveAsyn<myLessonPgCfg>());
            t.Wait();
        }
    }
#endregion
#region chapter_pg_config
    [DataContract]
    public class myChapterPgCfg:myConfig
    {
        [DataMember]
        public string mruToken;
        [DataMember]
        public string lastPath;
        [DataMember]
        public List<string> selectedChapters;
        [DataMember]
        public List<string> starLst;
        [DataMember]
        public List<string> markedInfo; //chapter|marked

        public myChapterPgCfg()
        {
            mruToken = "";
            lastPath = "";
            selectedChapters = new List<string>();
            starLst = new List<string>();
            markedInfo = new List<string>();
            m_configFile = "chapterPg.cfg";
        }
        static myChapterPgCfg m_config;

        public static myChapterPgCfg getInstance()
        {
            if (m_config == null)
            {
                var newCfg = new myChapterPgCfg();
                var t = Task.Run(async () => m_config = (myChapterPgCfg) await newCfg.load<myChapterPgCfg>());
                t.Wait();
                if (m_config == null)
                {
                    m_config = newCfg;
                }
            }
            return m_config;
        }
        public void save()
        {
            Debug.Assert(m_configFile != null);
            if (m_configFile == null)
            {
                m_configFile = "chapterPg.cfg";
            }
            var t = Task.Run(() => saveAsyn<myChapterPgCfg>());
            t.Wait();
        }
        public void clean()
        {
            selectedChapters.Clear();
            starLst.Clear();
        }
    }
#endregion
#region db_file_config
    [DataContract]
    public class myDbFileCfg : myConfig
    {
        [DataMember]
        public List<chapterRec> chapters;

        public myDbFileCfg()
        {
            chapters = new List<chapterRec>();
            m_configFile = "dbfile.cfg";
        }
        static myDbFileCfg m_config;

        public static myDbFileCfg getInstance()
        {
            if (m_config == null)
            {
                var newCfg = new myDbFileCfg();
                var t = Task.Run(async () => m_config = (myDbFileCfg)await newCfg.load<myDbFileCfg>());
                t.Wait();
                if (m_config == null)
                {
                    m_config = newCfg;
                }
            }
            return m_config;
        }
        public void save()
        {
            var t = Task.Run(() => saveAsyn<myDbFileCfg>());
            t.Wait();
        }
        public async Task saveAsyn()
        {
            await saveAsyn<myDbFileCfg>();
        }
    }
#endregion
}
