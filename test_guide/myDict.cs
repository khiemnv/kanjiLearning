using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace test_guide
{
    public class myTextReader:IDisposable
    {
        int iCur = 0;
        private IList<string> lines;
        public event EventHandler<int> OnReading;
        public void Open(string path)
        {
            var t = Task.Run(() => OpenAsync(path));
            int i = 0;
            for (bool done = t.Wait(100); !done; done = t.Wait(100), i++)
            {
                OnReading(this, i);
            }
        }
        public void Open(Uri path)
        {
            var t = Task.Run(() => OpenAsync(path));
            int i = 0;
            for(bool done = t.Wait(100); !done; done = t.Wait(100), i++)
            {
                OnReading(this, i);
            }
        }
        public myTextReader()
        {
        }
        public void Close() { }
        public async Task OpenAsync(Uri uri)
        {
            StorageFile sf = await StorageFile.GetFileFromApplicationUriAsync(uri);
            lines = await FileIO.ReadLinesAsync(sf);
        }
        public async Task OpenAsync(string path)
        {
            StorageFile sf = await StorageFile.GetFileFromPathAsync(path);
            lines = await FileIO.ReadLinesAsync(sf);
        }
        public string ReadLine()
        {
            string ret = (iCur < lines.Count)?lines[iCur++]:null;
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
        void load_dict4()
        {
            string path = @"Assets/hv_word.csv";
            myTextReader rd = new myTextReader();
            rd.OnReading += Rd_OnReading;
            rd.Open (getUri(path));
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
            Debug.WriteLine("on reading {0}", e);
        }

        void load_dict1()
        {
            string path = @"Assets/hv_org.csv";
            myTextReader rd = new myTextReader();
            rd.OnReading += Rd_OnReading;
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
        
        string getAbsolutePath(string path)
        {
            //StorageFolder folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
            path = string.Format("{0}{1}", "ms-appx:///", path);
            return path;
        }
        Uri getUri(string path)
        {
            //ms-appx-web:///
            return new Uri(string.Format("{0}{1}", "ms-appx:///", path), UriKind.Absolute);
        }
        void load_dict2()
        {
            string path = @"Assets/hanvietdict.js";
            //var fs = await StorageFile.GetFileFromPathAsync(path);
            //var lines = await FileIO.ReadLinesAsync(fs);
            myTextReader rd = new myTextReader();
            rd.OnReading += Rd_OnReading;
            rd.Open(getUri(path));
            int startTC = Environment.TickCount;
            var dict = new myDictHV(0);
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
            rd.Close();
            rd.Dispose();
            hvdict = dict;
        }
        void load_dict3()
        {
            string path = @"Assets/hvchubothu.js";
            myTextReader rd = new myTextReader();
            rd.OnReading += Rd_OnReading;
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

        myDict() { }
        public Dictionary<char, List<IRecord>> m_kanjis { get { return myDictBase.m_kanjis; } }
        static myDict m_instance;
        public static myDict Load()
        {
            if (m_instance == null)
            {
                m_instance = new myDict();
                //m_instance.load_dict1();
                m_instance.load_dict2();
                //m_instance.load_dict3();
                //m_instance.load_dict4();
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
                    myKanji kanji = new myKanji();
                    foreach (var rec in arr)
                    {
                        rec.format(kanji);
                    }
                    if (kanji.radical.zRadical == '\0')
                    {
                        IRecord rd = hvbt.Search(kanji.radical.iRadical);
                        rd.format(kanji);
                    }
                    kanjis.Add(kanji);
                }
            }
            return kanjis;
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
        public void add(string line)
        {
            //add(line, false);
            //throw new NotImplementedException();
            string[] arr = parseLine(line);
            IRecord rec = crtRec(arr);
            string zKey = rec.getKey();
            //add to local dict
            m_data.Add(zKey, rec);
            //add to share kanji data
            foreach (var c in zKey)
            {
                if (m_kanjis.ContainsKey(c)) { m_kanjis[c].Add(rec); }
                else m_kanjis.Add(c, new List<IRecord> { rec });
            }
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
        public string term;
        public string hn;
        public List<myDefinition> definitions = new List<myDefinition>();
    }
    public class myRadical
    {
        public int iRadical;
        public char zRadical;
        public myDefinition descr = new myDefinition();
    }
    public class myKanji
    {
        public List<myWord> relatedWords = new List<myWord>();
        public List<myDefinition> definitions = new List<myDefinition>();
        public char simple;
        public int extraStrokes, totalStrokes;
        public myRadical radical = new myRadical();
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
            int radical, extraStrokes, totalStrokes;
            string simple;
            string define;
            public recordHVORG(string[] arr)
            {
                //[0]uni_id,unicode,hHan,hPinyin,
                //[4]hRadical,hExtraStrokes,hTotalStrokes,hTraditionalVariant,
                //[8]hSimplifiedVariant,hDefinition
                kanji = arr[1];
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
                word.radical.iRadical = radical;
                word.extraStrokes = extraStrokes;
                word.totalStrokes = totalStrokes;
                //word.simple = simple[0];
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
                word.radical.descr = new myDefinition { text = meaning, bFormated = true };
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
}
