#define console_mode
#define bg_parse

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
#if console_mode
    public class Debug
    {
        public static void WriteLine(object txt)
        {
            Console.WriteLine(txt);
        }
        public static void Write(object txt)
        {
            Console.Write(txt);
        }

        internal static void Assert(bool v)
        {
            if (!v) throw new NotImplementedException();
        }
    }
#endif
    public class myTextReaderJs : myTextReader
    {
        public override int lineCount
        {
            get { return lines != null ? (lines.Count - 1) : 0; }
        }
        public override string ReadLine()
        {
            string ret = (iCur < lines.Count - 1) ? lines[iCur++] : null;
            return ret;
        }
    }
    public class myTextReader : IDisposable
    {
        protected int iCur = 0;
        public virtual int lineCount { get { return lines != null ? lines.Count : 0; } }
        protected IList<string> lines;
        public event EventHandler<int> Reading;
        protected virtual void OnReading(int e)
        {
            Reading?.Invoke(this, e);
        }
        public event EventHandler LoadCompleted;
        protected virtual void OnLoadCompleted(EventArgs e)
        {
            LoadCompleted?.Invoke(this, e);
        }
        public void Open(string path)
        {
            var t = Task.Run(() => OpenAsync(path));
            int i = 0;
            for (bool done = t.Wait(100); !done; done = t.Wait(100), i++)
            {
                OnReading(i);
            }
        }
        public void Open(Uri path)
        {
            const int timeOut = 1000;
            var t = Task.Run(() => OpenAsync(path));
            int i = 0;
            for (bool done = t.Wait(timeOut); !done; done = t.Wait(timeOut), i++)
            {
                OnReading(i);
                Debug.WriteLine(string.Format("{0} Open i {1} count {2}", this, i, lines != null ? lines.Count : -1));
            }
        }
        public myTextReader()
        {
        }
        public void Close() { }
#if console_mode
        public async Task OpenAsync(Uri uri)
        {
            var name = Path.GetFileName(uri.ToString());
            string[] arr = {
                @"C:\Users\Khiem\Desktop\hv_word.csv",
                @"C:\Users\Khiem\Desktop\kangxi.csv",
                @"C:\Users\Khiem\Desktop\hv_org.csv",
                @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js",
                @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hvchubothu.js",
                @"C:\Users\Khiem\Desktop\hannom_index.csv",
                @"C:\Users\Khiem\Desktop\character.csv",
                @"C:\Users\Khiem\Desktop\character_jdict.csv",
                @"C:\Users\Khiem\Desktop\bothu214.csv",
                @"C:\Users\Khiem\Desktop\component.txt",
                @"C:\Users\Khiem\Desktop\search.csv",
                @"C:\Users\Khiem\Desktop\conjugation.csv",
            };
            string path = arr.First((s) => { return s.Contains(name); });

            TextReader rd = File.OpenText(path);
            var line = await rd.ReadLineAsync();
            lines = new List<string>();
            for (; line != null;)
            {
                lines.Add(line);
                line = await rd.ReadLineAsync();
            }
            rd.Close();
            rd.Dispose();
        }
        public async Task OpenAsync(string path)
        {
            throw new NotImplementedException();
        }
#else
        public async Task OpenAsync(Uri uri)
        {
            StorageFile sf = await StorageFile.GetFileFromApplicationUriAsync(uri);
            lines = await FileIO.ReadLinesAsync(sf);
            OnLoadCompleted(new EventArgs());
        }
        public async Task OpenAsync(string path)
        {
            StorageFile sf = await StorageFile.GetFileFromPathAsync(path);
            lines = await FileIO.ReadLinesAsync(sf);
        }
#endif
        public virtual string ReadLine()
        {
            string ret = (iCur < lines.Count) ? lines[iCur++] : null;
            return ret;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~myTextReader() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

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
    public class myDict
    {
        myDictHvWord hv_word;   //hv_word.csv
        myDictHVORG hv_org;     //hv_org.csv
        myDictHV hvdict;        //hanvietdict.js
        myDictBT hvbt;          //hvchubothu.js
        myDictKangxi kxDict;    //kangxi.csv
        myDictHannom hnDict;    //hannom_index.csv
        myDictCharacter chDict; //character.csv
        myDictJDC jdcDict;      //character_jdict.csv
        myDict214 bt214;        //
        myCompo kjCompo;        //kanji component
        myDictSearch dictSearch;//verd info
        myDictConj dictConj;
        void load_hv_word()
        {
            string path = @"Assets/hv_word.csv";
            myTextReader rd = new myTextReader();
            rd.Reading += Rd_OnReading;
            rd.Open(getUri(path));
            int startTC = Environment.TickCount;
            var dict1 = new myDictHvWord(0);
            var line = rd.ReadLine();   //read header
            for (line = rd.ReadLine(); line != null; line = rd.ReadLine())
            {
                dict1.add(line);
            }
            //Console.WriteLine("hv_org.csv {0} {1}", dict1.count, Environment.TickCount - startTC);
            rd.Close();
            rd.Dispose();
            hv_word = dict1;
        }

        private void Rd_OnReading(object sender, int e)
        {
            Debug.WriteLine(string.Format("{0} Rd_OnReading {1}", this, e));
        }

        void load_hv_org()
        {
            string path = @"Assets/hv_org.csv";
            myTextReader rd = new myTextReader();
            rd.Reading += Rd_OnReading;
            rd.Open(getUri(path));
            myDictHVORG dict1 = new myDictHVORG(0);
            var line = rd.ReadLine();   //read header
            for (line = rd.ReadLine(); line != null; line = rd.ReadLine())
            {
                dict1.add(line);
            }
            //Debug.WriteLine("hv_org.csv {0} {1}", dict1.count, Environment.TickCount - startTC);
            rd.Close();
            hv_org = dict1;
            rd.Dispose();
        }
        void load_hv_org_2()
        {
            var dict = new myDictHVORG(0);
            string path = @"Assets/hv_org.csv";
            load_dict_2(dict, path);
            hv_org = dict;
        }
#if console_mode
#else
        string getAbsolutePath(string path)
        {
            //StorageFolder folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
            path = string.Format("{0}{1}", "ms-appx:///", path);
            return path;
        }
#endif
        Uri getUri(string path)
        {
            //ms-appx-web:///
            return new Uri(string.Format("{0}{1}", "ms-appx:///", path), UriKind.Absolute);
        }
        #region load_dict
        //working state
        enum wrkState
        {
            init = -1,
            begin = 0,
            wait4read = 1,
            parsing = 2,
            end = 3,
        }
        //state trans rules
        //init->(line count > 0) begin
        //init->(read cmpl) end
        //begin->(has new line) parsing
        //begin->(wait 4 get line) wait4read
        //wait4read->(has new line) parsing
        //wait4read->(read cmple) end
        //wait4read->(wait 4 get line) wait4read
        //parsing->(has new line) parsing
        //parsing->(wait 4 get line) wait4read

        void load_hanvietdict()
        {
            var dict = new myDictHV(0);
            string path = @"Assets/hanvietdict.js";
            myTextReader rd = new myTextReaderJs();
            load_dict(dict, rd, path);
            hvdict = dict;
        }
        void load_dict_2(myDictBase dict, string path)
        {
            //var fs = await StorageFile.GetFileFromPathAsync(path);
            //var lines = await FileIO.ReadLinesAsync(fs);
            csvParser csv = new csvParser();
            csv.uri = getUri(path);
            Task t = Task.Run(() => csv.start());

            int startTC = Environment.TickCount;
            wrkState s = wrkState.init;
            string[] arr;
#if bg_parse
            int nBlock;
            int iBlock = 0;
            int nRead;
            byte[] block;
            int count;
#endif
            int nRec;
            int iRec = 0;
            bool bFetch;
            for (; s != wrkState.end;)
            {
                Debug.WriteLine(string.Format("{0} load_dict i {1} s {2}", this, iRec, s));
                switch (s)
                {
                    case wrkState.init:
#if bg_parse
                        nBlock = csv.blockCount;
                        if (iBlock < nBlock) {
                            block = csv.getBlock(out nRead);
                            iBlock++;
                            csv.parseBlock(nRead, block);
                        }
#endif
                        nRec = csv.recCount;
                        if (nRec > 0) s = wrkState.begin;
                        else if (t.Status == TaskStatus.RanToCompletion) s = wrkState.end;
                        break;
                    case wrkState.begin:
                        arr = csv.getRec();   //ignore first line
                        iRec++;
#if bg_parse
                        bFetch = (iBlock < csv.blockCount);
#else
                        bFetch = (iRec < csv.recCount);
#endif
                        if (bFetch) s = wrkState.parsing;
                        else s = wrkState.wait4read;
                        break;
                    case wrkState.wait4read:
#if bg_parse
                        bFetch = (iBlock < csv.blockCount);
#else
                        bFetch = (iRec < csv.recCount);
#endif
                        if (bFetch) s = wrkState.parsing;
                        else if (t.Status == TaskStatus.RanToCompletion) s = wrkState.end;
                        Task.Delay(1);
                        break;
                    case wrkState.parsing:
#if bg_parse
                        nBlock = csv.blockCount;
                        for (; iBlock < nBlock;)
#endif
                        {
#if bg_parse
                            block = csv.getBlock(out nRead);
                            iBlock++;
                            csv.parseBlock(nRead, block);
#endif
                            nRec = csv.recCount;
                            for (; iRec < nRec;)
                            {
                                arr = csv.getRec();
                                dict.add(arr);
                                iRec++;
                            }
                        }
#if bg_parse
                        bFetch = (iBlock < csv.blockCount);
#else
                        bFetch = (iRec < csv.recCount);
#endif
                        if (!bFetch) s = wrkState.wait4read;
                        break;
                }
            }
            csv.Dispose();
        }
        void load_dict(myDictBase dict, myTextReader rd, string path)
        {
            //var fs = await StorageFile.GetFileFromPathAsync(path);
            //var lines = await FileIO.ReadLinesAsync(fs);
            Task t = Task.Run(() => rd.Open(getUri(path)));

            int startTC = Environment.TickCount;
            wrkState s = wrkState.init;
            string line;
            for (int i = 0; s != wrkState.end;)
            {
                Debug.WriteLine(string.Format("{0} load_dict i {1} s {2}", this, i, s));
                switch (s)
                {
                    case wrkState.init:
                        if (rd.lineCount > 0) s = wrkState.begin;
                        else if (t.Status == TaskStatus.RanToCompletion) s = wrkState.end;
                        break;
                    case wrkState.begin:
                        line = rd.ReadLine();   //ignore first line
                        i++;
                        if (i < rd.lineCount) s = wrkState.parsing;
                        else s = wrkState.wait4read;
                        break;
                    case wrkState.wait4read:
                        if (i < rd.lineCount) s = wrkState.parsing;
                        else if (t.Status == TaskStatus.RanToCompletion) s = wrkState.end;
                        Task.Delay(1);
                        break;
                    case wrkState.parsing:
                        var c = rd.lineCount;
                        for (; i < c;)
                        {
                            line = rd.ReadLine();
                            dict.add(line);
                            i++;
                        }
                        if (i == rd.lineCount) s = wrkState.wait4read;
                        break;
                }
            }
            rd.Close();
            rd.Dispose();
        }
#endregion
        void load_hvchubothu()
        {
            string path = @"Assets/hvchubothu.js";
            myTextReader rd = new myTextReader();
            rd.Reading += Rd_OnReading;
            rd.Open(getUri(path));
            int startTC = Environment.TickCount;
            var dict = new myDictBT(0);
            var line = rd.ReadLine();   //read header
            line = rd.ReadLine();
            string[] buff = { line, "" };
            line = rd.ReadLine();
            buff[1] = line;
            uint iCur = 1;
            int count = 2;
            for (; line != null;)
            {
                dict.add(buff[iCur ^ 1]);
                iCur = iCur ^ 1;
                line = rd.ReadLine();
                buff[iCur] = line;
                count++;
            }
            //Console.WriteLine("hvchubothu.js {0} {1}", dict.count, Environment.TickCount - startTC);
            rd.Close();
            rd.Dispose();
            hvbt = dict;
        }
        void load_kangxi()
        {
            var dict = new myDictKangxi(0);
            string path = @"Assets/kangxi.csv";
            myTextReader rd = new myTextReader();
            load_dict(dict, rd, path);
            kxDict = dict;
        }
        void load_hannom_index()
        {
            var dict = new myDictHannom(0);
            string path = @"Assets/hannom_index.csv";
            myTextReader rd = new myTextReader();
            load_dict(dict, rd, path);
            hnDict = dict;
        }
        void load_character()
        {
            var dict = new myDictCharacter(0);
            string path = @"Assets/character.csv";
            myTextReader rd = new myTextReader();
            load_dict(dict, rd, path);
            chDict = dict;
        }
        //character_jdict.csv
        void load_character_jdict()
        {
            var dict = new myDictJDC(0);
            string path = @"Assets/character_jdict.csv";
            myTextReader rd = new myTextReader();
            load_dict(dict, rd, path);
            jdcDict = dict;
        }
        void load_bt214()
        {
            var dict = new myDict214(0);
            string path = @"Assets/bothu214.csv";
            myTextReader rd = new myTextReader();
            load_dict(dict, rd, path);
            bt214 = dict;
        }
        //load kanji component
        void load_kjCompo()
        {
            var dict = new myCompo(0);
            string path = @"Assets/component.txt";
            myTextReader rd = new myTextReader();
            load_dict(dict, rd, path);
            kjCompo = dict;
        }
        void load_conj()
        {
            var dict = new myDictConj(0);
            string path = @"Assets/conjugation.csv";
            myTextReader rd = new myTextReader();
            load_dict(dict, rd, path);
            dictConj = dict;
        }
        void load_search()
        {
            var dict = new myDictSearch(0);
            string path = @"Assets/search.csv";
            myTextReader rd = new myTextReader();
            load_dict(dict, rd, path);
            dictSearch = dict;
        }
        myDict() { }
        public Dictionary<char, List<IRecord>> m_kanjis { get { return myDictBase.m_kanjis; } }
        static myDict m_instance;
        public static int loadProgress = 0;
        public static myDict Load()
        {
            if (m_instance == null)
            {
                m_instance = new myDict();
                //m_instance.load_character();
                loadProgress = 10;

                m_instance.load_hv_org_2();
                loadProgress = 20;
#if false
                m_instance.load_hanvietdict();
                loadProgress = 30;
                //m_instance.load_hvchubothu();
                m_instance.load_hv_word();
                loadProgress = 40;
                m_instance.load_kangxi();
                loadProgress = 50;
                //m_instance.load_hannom_index();

                m_instance.load_character_jdict();
                loadProgress = 60;

                m_instance.load_bt214();
                loadProgress = 70;
                m_instance.load_kjCompo();

                //verd info
                m_instance.load_conj();
                m_instance.load_search();
#endif
                loadProgress = 100;

            }
            return m_instance;
        }

        public List<myKanji> Search(string w)
        {
            List<myKanji> kanjis = new List<myKanji>();
            foreach (char key in w)
            {
                if (myDictBase.m_kanjis.ContainsKey(key))
                {
                    var arr = myDictBase.m_kanjis[key].Distinct();
                    myKanji kanji = new myKanji() { val = key };
                    foreach (var rec in arr)
                    {
                        rec.format(kanji);
                    }
                    if (kanji.radical.zRadical == '\0')
                    {
                        IRecord rd;
                        //rd = hvbt.Search(kanji.radical.iRadical);
                        //rd.format(kanji);
                        rd = kxDict[kanji.radical.iRadical];
                        rd.format(kanji);
                        kjCompo.Update(kanji);
                    }
                    kanjis.Add(kanji);
                }
            }
            return kanjis;
        }
    }
    public class myRadicalMng : myDictBase
    {
        static protected List<RadRec> m_sRadicals = new List<RadRec>();
        static protected Dictionary<char, RadRec> m_sRadDict = new Dictionary<char, RadRec>();

        protected class RadRec : IRecord
        {
            //kangxi
            public string alt, name, reading;
            public char literal;
            public int stroke_count;
            //hn
            public string hn, mean;

            public void initKangxi(string[] arr)
            {
                literal = arr[1][0];
                alt = arr[2];
                name = arr[5];
                reading = arr[6];
                int.TryParse(arr[7], out stroke_count);
                add(literal);
                foreach (char c in alt) { add(c); }
            }
            void add(char c)
            {
                if (!isKanji(c)) return;
                if (m_sRadDict.ContainsKey(c)) return;

                m_sRadDict.Add(c, this);
            }
            public void initBt214(string[] arr)
            {
                var term = arr[1];
                hn = arr[2];
                mean = arr[3];
                foreach (char c in term) { add(c); }
            }

            public void format(myKanji word)
            {
                //kangxi
                word.radical.zRadical = literal;
                word.radical.alt = alt;
                word.radical.definitions.Add(
                    new myDefinition { text = string.Format("{0} {1}", name, reading) }
                    );
                word.radical.nStrokes = stroke_count;
                //bt214
                word.radical.hn = hn;
            }

            public string getKey()
            {
                return literal.ToString();
            }
        }

        public myRadicalMng(int maxWordCount) : base(maxWordCount)
        {
        }
        protected RadRec CrtRec(int id)
        {
            if (m_sRadicals.Count < id)
            {
                var rec = new RadRec();
                m_sRadicals.Add(rec);
                return rec;
            }
            else
            {
                return m_sRadicals[id - 1];
            }
        }
        //public IRecord this[char rad]{get {return m_data[rad];}}
        public IRecord this[int id] { get { return m_sRadicals[id - 1]; } }
        protected virtual IRecord Search(char rad)
        {
            throw new NotImplementedException();
        }
    }
    public class myDictBase
    {
        public static Dictionary<char, List<IRecord>> m_kanjis = new Dictionary<char, List<IRecord>>();
        public int count { get { return m_data.Count; } }
        protected Dictionary<string, IRecord> m_data;
        protected IRecord search(string key)
        {
            return m_data.ContainsKey(key) ? m_data[key] : null;
        }
        public myDictBase(int maxWordCount)
        {
            m_data = new Dictionary<string, IRecord>();
        }
        protected virtual IRecord crtRec(string[] arr)
        {
            throw new NotImplementedException();
        }
        protected virtual string[] parseLine(string line)
        {
            throw new NotImplementedException();
        }
        protected string[] parseLine(string line, bool isCSV)
        {
            myPaserResult m = myparser2(line);
            if (isCSV)
            {
                m.arr.Add(m.gerCurObj());
            }
            return m.arr.ToArray();
        }


        public static bool isKanji(char c)
        {
            if (c < 0x2e85) return false;
            //3040-30ff
            if ((c & 0xFF00) == 0x3000) return false;
            return true;
        }

        public void add(string line)
        {
            //add(line, false);
            //throw new NotImplementedException();
            string[] arr = parseLine(line);
            add(arr);
        }
        public void add(string[] arr)
        {
            IRecord rec = crtRec(arr);
            if (rec == null) return;

            string zKey = rec.getKey();
            if (!m_data.ContainsKey(zKey))
            {
                //add to local dict
                m_data.Add(zKey, rec);
                //add to share kanji data
                foreach (var c in zKey)
                {
                    if (!isKanji(c))
                    {
                        Debug.Write(c);
                        continue;
                    }
                    if (m_kanjis.ContainsKey(c)) { m_kanjis[c].Add(rec); }
                    else m_kanjis.Add(c, new List<IRecord> { rec });
                }
            }
            else
            {
                ResolveConfilct(rec);
            }
        }
        protected virtual void ResolveConfilct(IRecord rec)
        {

        }
        //protected virtual void add(string line, bool isCSV)
        //{
        //    myPaserResult m = myparser2(line);
        //    if (isCSV)
        //    {
        //        m.arr.Add(m.gerCurObj());
        //    }
        //    IRecord rec = crtRec(m.arr.ToArray());
        //    //if (!m_data.ContainsKey(rec.getKey()))
        //        m_data.Add(rec.getKey(), rec);
        //}

#region csv parser
        enum myTkType
        {
            t_comma = 0,
            t_dblq,
            t_other,
        }
        class myToken
        {
            public myTkType type;
            public char val;
            public myToken(char c)
            {
                switch (c)
                {
                    case '"':
                        type = myTkType.t_dblq;
                        break;
                    case ',':
                        type = myTkType.t_comma;
                        break;
                    default:
                        type = myTkType.t_other;
                        break;
                }
                val = c;
            }

            public static implicit operator string(myToken v)
            {
                return v.val.ToString();
            }

            public static implicit operator char(myToken v)
            {
                return v.val;
            }

            public static explicit operator int(myToken v)
            {
                return (int)v.type;
            }
        };
        enum myState
        {
            invalid = -1,
            s = 0,  //begin
            a,
            b,
            e
        };

        class myField
        {
            public int iStart;
            public int iCur;
            public bool dblQt;
        }
        class myPaserResult
        {
            public myPaserResult(string txt)
            {
                line = txt;
            }
            public string line;
            public List<string> arr = new List<string>();
            //public List<myField> fields = new List<myField>();
            public int count = 0;
            public myField curObj = new myField();
            public string gerCurObj()
            {
                return line.Substring(curObj.iStart, curObj.iCur - curObj.iStart);
            }
        }
        delegate void myRule(myPaserResult res, myToken tk);
        static void f1(myPaserResult res, myToken tk)
        {

        }
        static void f_ss(myPaserResult res, myToken tk)
        {
            res.count++;
            res.arr.Add("");
            res.curObj.iStart = ++res.curObj.iCur;
            //res.fields.Add(res.curObj); //to debug
        }
        static void f_sa(myPaserResult res, myToken tk)
        { res.curObj.iStart = ++res.curObj.iCur; }
        static void f_aa(myPaserResult res, myToken tk)
        {
            res.curObj.iCur++;
        }
        static void f_bs(myPaserResult res, myToken tk)
        {
            res.count++;
            res.arr.Add(res.gerCurObj());
            res.curObj.iCur += 2;
            //res.fields.Add(res.curObj);
        }
        static void f_es(myPaserResult res, myToken tk)
        {
            res.count++;
            res.arr.Add(res.gerCurObj());
            res.curObj.iCur++;
            //res.fields.Add(res.curObj);
        }
        static void f_ba(myPaserResult res, myToken tk)
        {
            res.curObj.iCur += 2;
            res.curObj.dblQt = true;
        }
        static myState[,] tbl = new myState[4, 3] {
            {myState.s, myState.a, myState.e, },
            {myState.a, myState.b, myState.a, },
            { myState.s, myState.a, myState.invalid,},
            {myState.s, myState.invalid, myState.e, },
        };
        static myRule[,] clbTbl = new myRule[4, 3] {
            {f_ss, f_sa,(res, tk) => {res.curObj.iStart = res.curObj.iCur++; }},
            {f_aa, f1, f_aa },
            {f_bs, f_ba, f1 },
            {f_es, f1, f_aa },
        };
        static myPaserResult myparser2(string line)
        {
            //state table
            //state |token
            //      |,      |"      |other
            //------+-------+-------+------
            //s     |s      |a      |e
            //a     |a      |b      |a
            //b     |s      |a      |invalid
            //e     |s      |invalid|e
            myPaserResult res = new myPaserResult(line);
            myState cur = myState.s;
            myState nState = myState.s;
            foreach (char c in line)
            {
                myTkType type;
#if false
                myToken tk = new myToken(c);
                type = tk.type;
#else
                switch (c)
                {
                    case '"':
                        type = myTkType.t_dblq;
                        break;
                    case ',':
                        type = myTkType.t_comma;
                        break;
                    default:
                        type = myTkType.t_other;
                        break;
                }
#endif
                cur = nState;
                nState = tbl[(int)cur, (int)type];
                if (nState == myState.invalid) break;
                myRule cb = clbTbl[(int)cur, (int)type];
                cb(res, null);
            }

            //case eol
            return res;
        }
#endregion
    }
    public class myDefinition
    {
        public string text;
        public bool bFormated;
    }
    public class myWord
    {
        public string term; //kanjis
        public string hn;   //hn
        public List<myDefinition> definitions = new List<myDefinition>();
    }
    public class myRadical : myWord
    {
        public int iRadical;
        public char zRadical;
        public string alt;
        public int nStrokes;
    }
    public class myKanji : myWord
    {
        public char val;
        public char simple;
        public int extraStrokes, totalStrokes;
        public string decomposite = "";
        public myRadical radical = new myRadical();
        public List<myWord> relatedWords = new List<myWord>();
        public myWord relateWord(string key)
        {
            myWord word = relatedWords.Find((w) => { return w.term == key; });
            if (word == null)
            {
                word = new myWord { term = key };
                relatedWords.Add(word);
            }
            return word;
        }
    }
    public interface IRecord
    {
        string getKey();
        void format(myKanji kanji);
    }
    public class myDictHVORG : myDictBase
    {
        class recordHVORG : IRecord
        {
            string kanji;
            string hn;
            int radical, extraStrokes, totalStrokes;
            string simple;
            string define;
            public recordHVORG(string[] arr)
            {
                //[0]uni_id,unicode,hHan,hPinyin,
                //[4]hRadical,hExtraStrokes,hTotalStrokes,hTraditionalVariant,
                //[8]hSimplifiedVariant,hDefinition
                kanji = arr[1];
                hn = arr[2];
                int.TryParse(arr[4], out radical);
                int.TryParse(arr[5], out extraStrokes);
                int.TryParse(arr[6], out totalStrokes);
                simple = arr[8];
                define = arr[9];
            }

            public string getKey()
            {
                return kanji;
            }

            public void format(myKanji word)
            {
                word.hn = hn;
                word.radical.iRadical = radical;
                word.extraStrokes = extraStrokes;
                word.totalStrokes = totalStrokes;
                word.simple = simple.Length > 0 ? simple[0] : '\0';
                var arr = define.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var def in arr)
                {
                    word.definitions.Add(new myDefinition { text = def });
                }
            }
        }
        protected override IRecord crtRec(string[] arr)
        {
            return new recordHVORG(arr);
        }
        protected override string[] parseLine(string line)
        {
            return parseLine(line, true);
        }
        public myDictHVORG(int maxWordCount) : base(maxWordCount)
        {
        }
    }
    public class myDictBT : myDictBase
    {
        class recordBT : IRecord
        {
            string radical, meaning;

            public recordBT(string[] arr)
            {
                radical = arr[0];
                meaning = arr[1];
            }

            public void format(myKanji word)
            {
                word.radical.zRadical = radical[0];
                word.radical.definitions.Add(
                    new myDefinition
                    {
                        text = meaning,
                        bFormated = true
                    });
            }

            public string getKey()
            {
                return radical;
            }
        }
        public myDictBT(int maxWordCount) : base(maxWordCount)
        {
        }
        protected override IRecord crtRec(string[] arr)
        {
            var rec = new recordBT(arr);
            m_records.Add(rec);
            return rec;
        }
        protected override string[] parseLine(string line)
        {
            //base.add(line);
            var arr = line.Split(new string[] { "\", \"", "\",", "\"" }, StringSplitOptions.RemoveEmptyEntries);
            return arr;
        }
        List<recordBT> m_records = new List<recordBT>();
        public IRecord Search(int radical)
        {
            return m_records[radical - 1];
        }
    }
    //myDictKangxi
    public class myDictKangxi : myRadicalMng
    {
        public myDictKangxi(int maxWordCount) : base(maxWordCount)
        {
        }
        protected override IRecord crtRec(string[] arr)
        {
            int id = int.Parse(arr[0]);
            var rec = (RadRec)CrtRec(id);
            rec.initKangxi(arr);
            return rec;
        }
        protected override string[] parseLine(string line)
        {
            return parseLine(line, true);
        }
    }
    //myDictHannom
    public class myDictHannom : myDictBase
    {
        class recordHannom : IRecord
        {
            public string hHan;
            public char unicode;
            public recordHannom() { }
            public recordHannom(string[] arr)
            {
                unicode = arr[1][0];
                hHan = arr[2];
            }

            public void format(myKanji word)
            {
                word.radical.definitions.Add(new myDefinition { text = hHan });
            }

            public string getKey()
            {
                return unicode.ToString();
            }
        }
        public myDictHannom(int maxWordCount) : base(maxWordCount)
        {
        }
        protected override IRecord crtRec(string[] arr)
        {
            var rec = new recordHannom(arr);
            return rec;
        }

        protected override void ResolveConfilct(IRecord irec)
        {
            recordHannom rec;
            string key = irec.getKey();
            rec = (recordHannom)m_data[key];
            rec.hHan += ", " + ((recordHannom)irec).hHan;
        }

        protected override string[] parseLine(string line)
        {
            return parseLine(line, true);
        }

        public IRecord Search(char zRadical)
        {
            string key = zRadical.ToString();
            if (m_data.ContainsKey(key))
            {
                return m_data[key];
            }
            return null;
        }
    }
    public class myDictHV : myDictBase
    {
        class recordHV : IRecord
        {
            string hn, kanji, meaning;

            public recordHV(string[] arr)
            {
                hn = arr[0];
                kanji = arr[1];
                meaning = arr[2];
            }

            public void format(myKanji kanji)
            {
                myWord word = kanji.relatedWords.Find((w) => { return w.term == this.kanji; });
                if (word == null)
                {
                    word = new myWord { term = this.kanji, hn = hn, };
                    kanji.relatedWords.Add(word);
                }
                word.definitions.Add(new myDefinition { text = meaning, bFormated = true });
            }

            public string getKey()
            {
                return kanji;
            }
        }
        public myDictHV(int maxWordCount) : base(maxWordCount)
        {
        }
        protected override IRecord crtRec(string[] arr)
        {
            return new recordHV(arr);
        }
        protected override string[] parseLine(string line)
        {
#if false
            //base.add(line);
            var arr = line.Split(new string[] {"\",\"", "\",", "\"" }, StringSplitOptions.RemoveEmptyEntries);
#else
            return parseLine(line, false);
#endif
        }
    }
    public class myDictHvWord : myDictBase
    {
        public myDictHvWord(int maxWordCount) : base(maxWordCount)
        {
        }
        class recordHvWord : IRecord
        {
            string word, hn, def;

            public recordHvWord(string[] arr)
            {
                word = arr[1];
                hn = arr[2];
                def = arr[3];
            }

            public void format(myKanji kanji)
            {
                myWord word = kanji.relatedWords.Find((w) => { return w.term == this.word; });
                if (word == null)
                {
                    word = new myWord { term = this.word, hn = this.hn, };
                    kanji.relatedWords.Add(word);
                }
                foreach (var d in def.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    word.definitions.Add(new myDefinition { text = d });
                }
            }

            public string getKey()
            {
                return word;
            }
        }
        protected override IRecord crtRec(string[] arr)
        {
            return new recordHvWord(arr);
        }
        protected override string[] parseLine(string line)
        {
            return parseLine(line, true);
        }
    }
    //myDictCharacter
    public class myDictCharacter : myDictBase
    {
        class recordCharacter : IRecord
        {
            public char literal;
            public string reading, meaning;
            int stroke_count, kangxi;
            public recordCharacter() { }
            public recordCharacter(string[] arr)
            {
                literal = arr[1][0];
                reading = arr[5];
                meaning = arr[6];
                int.TryParse(arr[7], out stroke_count);
                int.TryParse(arr[8], out kangxi);
            }
            public void format(myKanji word)
            {
                word.radical.iRadical = kangxi;
                word.totalStrokes = stroke_count;
                word.definitions.Add(
                    new myDefinition { text = reading + " " + meaning }
                    );
            }
            public string getKey()
            {
                return literal.ToString();
            }
        }
        public myDictCharacter(int maxWordCount) : base(maxWordCount)
        {
        }
        protected override IRecord crtRec(string[] arr)
        {
            var rec = new recordCharacter(arr);
            return rec;
        }
        protected override string[] parseLine(string line)
        {
            return parseLine(line, true);
        }
    }

    //character jdict
    public class myDictJDC : myDictBase
    {
        public myDictJDC(int maxWordCount) : base(maxWordCount)
        {
        }
        class recordJC : IRecord
        {
            public char literal;
            public string decomposition;
            public recordJC() { }
            public recordJC(string[] arr)
            {
                literal = arr[1][0];
                decomposition = arr[2];
            }
            public void format(myKanji word)
            {
                word.decomposite = decomposition;
            }
            public string getKey()
            {
                return literal.ToString();
            }
        }
        protected override IRecord crtRec(string[] arr)
        {
            var rec = new recordJC(arr);
            return rec;
        }
        protected override string[] parseLine(string line)
        {
            var arr = line.Split(',');
            return arr;
        }
    }

    //conj
    public class myDictConj : myDictBase
    {
        public static List<recordConj> m_list = new List<recordConj>();
        public myDictConj(int maxWordCount) : base(maxWordCount)
        {
        }
        public class recordConj : IRecord
        {
            public string pos, sample, perfective, negative, i, te, potential, passive, causative, eba, imperative, volitional;

            public recordConj() { }
            public recordConj(string[] arr)
            {
                pos = arr[1];
                sample = arr[2];
                perfective = arr[3];
                negative = arr[4];
                i = arr[5];
                te = arr[6];
                potential = arr[7];
                passive = arr[8];
                causative = arr[9];
                eba = arr[10];
                imperative = arr[11];
                volitional = arr[12];
            }
            public void format(myKanji word)
            {
            }
            public string getKey()
            {
                return pos;
            }
            public override string ToString()
            {
                return string.Format(" sample {0}\n perfective {1}\n negative{2}\n " +
                    "i {3}\n te {4}\n potential {5}\n " +
                    "passive {6}\n causative {7}\n eba {8}\n " +
                    "imperative {9}\n volitional {10}\n",
                    sample, perfective, negative,
                    i, te, potential,
                    passive, causative, eba,
                    imperative, volitional);
            }
        }
        protected override IRecord crtRec(string[] arr)
        {
            var rec = new recordConj(arr);
            m_list.Add(rec);
            return null;
        }
        protected override string[] parseLine(string line)
        {
            return parseLine(line, true);
        }
    }
    public class myDictSearch : myDictBase
    {
        public myDictSearch(int maxWordCount) : base(maxWordCount)
        {
        }
        class recordSrch : IRecord
        {
            public string hira;
            public string term;
            public string meaning;
            public string pos;
            public int iConj;
            public recordSrch() { }
            public recordSrch(string[] arr)
            {
                hira = arr[3];
                term = arr[4];
                meaning = arr[5];
                pos = arr[6];
            }
            string getDef()
            {
                string ret = "";
                var arr = pos.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string i in arr)
                {
                    var l = myDictConj.m_list.FindAll((rec) => { return rec.pos == i; });
                    foreach (myDictConj.recordConj conj in l)
                    {
                        ret = hira + "\n" + conj.ToString();
                        break;
                    }
                }
                return ret;
            }
            public void format(myKanji kanji)
            {
                bool found = false;
                foreach(var ch in term)
                {
                    if (isKanji(ch))
                    {
                        if (ch == kanji.val) { found = true; }
                        else { found = false; break; }
                    }
                }
                if (found)
                {
                    string def = getDef();
                    if (def != "") {
                        var word = kanji.relateWord(term);
                        word.definitions.Add(new myDefinition { text = def });
                    }
                }
            }
            public string getKey()
            {
                return term.ToString();
            }
        }
        protected override IRecord crtRec(string[] arr)
        {
            var rec = new recordSrch(arr);
            return rec;
        }
        protected override string[] parseLine(string line)
        {
            return parseLine(line, true);
        }
    }

    public class myDict214 : myRadicalMng
    {
        public myDict214(int maxWordCount) : base(maxWordCount)
        {
        }
        protected override IRecord crtRec(string[] arr)
        {
            int id = int.Parse(arr[0]);
            var rec = CrtRec(id);
            rec.initBt214(arr);
            return rec;
        }
        protected override string[] parseLine(string line)
        {
            return base.parseLine(line, true);
        }
    }

    //req: radical data loaded
    public class myCompo : myRadicalMng
    {
        Dictionary<char, compoRec> m_dict = new Dictionary<char, compoRec>();
        public myCompo(int maxWordCount) : base(maxWordCount)
        {
        }
        class compoRec
        {
            public char ch;
            public string hn;
        }

        char[] splitor = new char[] { ' ', '/' };
        protected override IRecord crtRec(string[] arr)
        {
            var rec = new compoRec { ch = arr[0][0] };
            int radId = int.Parse(arr[1]);
            string hn;
            if (radId != 0)
            {
                RadRec tmp = m_sRadicals[radId - 1];
                hn = tmp.hn;
            }
            else
            {
                hn = arr[2];
            }
            rec.hn = hn.Split(splitor)[0];
            m_dict.Add(rec.ch, rec);
            return null;
        }
        public void Update(myKanji kj)
        {
            //update radical.hn & decomposite
            //var rec = (RadRec)this[kj.radical.iRadical - 1];
            //kj.radical.hn = rec.hn;
            Debug.WriteLine(string.Format("{0} Update decomposite {1}", this, kj.val));
            List<string> arr = new List<string>();
            foreach (char key in kj.decomposite)
            {
                if (!isKanji(key)) continue;
                if (m_dict.ContainsKey(key))
                {
                    //arr.Add(c);
                    var tmp = m_dict[key];
                    arr.Add(tmp.hn);
                }
                else
                {
                    Debug.Write(key);
                }
            }
            if (arr.Count > 0) { kj.decomposite = string.Format("{0}({1})", kj.decomposite, string.Join(";", arr)); }
        }
        protected override string[] parseLine(string line)
        {
            return line.Split('\t');
        }
    }
}
