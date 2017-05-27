using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication1
{

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
            foreach(var c in zKey)
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
    public interface IRecord
    {
        string getKey();
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
        }
        protected override IRecord crtRec(string[] arr)
        {
            return new recordHVORG(arr);
        }
        protected override string[] parseLine(string line) {
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
            return new recordBT(arr);
        }
        protected override string[] parseLine(string line)
        {
            //base.add(line);
            var arr = line.Split(new string[] {"\", \"", "\",", "\"" }, StringSplitOptions.RemoveEmptyEntries);
            return arr;
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
    class Program
    {
        static void Main(string[] args)
        {
            test_dict1();   //hv_org.csv
            test_dict2();   //hanvietdict.js
            test_dict3();   //bo thu
            test_dict4();   //hv_word.csv
        }
        static void test_dict4()
        {
            string path = @"C:\Users\Khiem\Desktop\hv_word.csv";
            //path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js";
            //FileStream f = File.Open(path, FileMode.Open);
            TextReader rd = File.OpenText(path);
            int startTC = Environment.TickCount;
            var dict1 = new myDictHvWord(0);
            var line = rd.ReadLine();   //read header
            for (line = rd.ReadLine(); line != null; line = rd.ReadLine())
            {
                dict1.add(line);
            }
            Console.WriteLine("hv_org.csv {0} {1}", dict1.count, Environment.TickCount - startTC);
            rd.Close();
            rd.Dispose();
        }
        static void test_dict1()
        {
            string path = @"C:\Users\Khiem\Desktop\hv_org.csv";
            //path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js";
            //FileStream f = File.Open(path, FileMode.Open);
            TextReader rd = File.OpenText(path);
            int startTC = Environment.TickCount;
            myDictHVORG dict1 = new myDictHVORG(0);
            var line = rd.ReadLine();   //read header
            for (line = rd.ReadLine(); line != null; line =rd.ReadLine()) {
                dict1.add(line);
            }
            Console.WriteLine("hv_org.csv {0} {1}", dict1.count, Environment.TickCount - startTC);
            rd.Close();
            rd.Dispose();
        }
        static void test_dict2()
        {
            string path = @"C:\Users\Khiem\Desktop\hv_org.csv";
            path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js";
            //path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hvchubothu.js";
            //FileStream f = File.Open(path, FileMode.Open);
            TextReader rd = File.OpenText(path);
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
            Console.WriteLine("hanvietdict {0} {1}", dict.count, Environment.TickCount - startTC);
            rd.Close();
            rd.Dispose();
        }
        static void test_dict2_2()
        {
            string path = @"C:\Users\Khiem\Desktop\hv_org.csv";
            path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js";
            //path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hvchubothu.js";
            //FileStream f = File.Open(path, FileMode.Open);
            TextReader rd = File.OpenText(path);
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
            Console.WriteLine("hanvietdict (false) {0} {1}", dict.count, Environment.TickCount - startTC);
            rd.Close();
            rd.Dispose();
        }
        static void test_dict3()
        {
            string path = @"C:\Users\Khiem\Desktop\hv_org.csv";
            //path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js";
            path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hvchubothu.js";
            //FileStream f = File.Open(path, FileMode.Open);
            TextReader rd = File.OpenText(path);
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
            Console.WriteLine("hvchubothu.js {0} {1}", dict.count, Environment.TickCount - startTC);
            rd.Close();
            rd.Dispose();
        }
        void test_parseline()
        {
            string testTxt = ",,\"\",123,\"abc\",\"\"\"\",\"\"\"mot hai\"\r\n";
            var res = myparser2(testTxt);

            string path = @"C:\Users\Khiem\Desktop\hv_org.csv";
            //path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js";
            //FileStream f = File.Open(path, FileMode.Open);
            TextReader rd = File.OpenText(path);
#if false
            var line = rd.ReadLine();
            string pat = "(?:^|,)(?=[^\"]|(\")?)\"?((?(1)[^\"]*|[^,\"]*))\"?(?=,|$)";   //worng with ""
                                                                                        //pat = ",?\".+?\"|[^\"]+?(?=,)|[^\"]+";  //wrong with ""
                                                                                        //pat = "(?<=\r|\n|^)(?!\r|\n|$)(?:(?:\"(?<Value>(?:[^\"]|\"\")*)\"|(?<Value1>(?!\")[^,\r\n]+)|\"(?<OpenValue>(?:[^\"]|\"\")*)(?=\r|\n|$)|(?<Value2>))(?:,|(?=\r|\n|$)))+?(?:(?<=,)(?<Value3>))?(?:\r\n|\r|\n|$)";

            //Regex reg = new Regex(pat);
            //var m = reg.Matches(testTxt);
            line = rd.ReadLine();

            //m = reg.Matches(line);
            var m = myparser2(line);

            foreach(var i in m.arr)
            {
                Debug.WriteLine(i);
            }
#endif
        }
        enum myTkType
        {
            t_comma = 0,
            t_dblq,
            t_other,
            //t_spec = ' ',
            //t_lsqb = '[',
            //t_rsqb = ']',
        }
        class myToken
        {
            public myTkType type;
            public char val;
            public myToken(char c)
            {
                switch (c)
                {
                    //case ' ':
                    //case '\t':
                    //    type = myTkType.t_spec;
                    //    break;
                    case '"':
                        type = myTkType.t_dblq;
                        break;
                    case ',':
                        type = myTkType.t_comma;
                        break;
                    //case '[':
                    //    type = myTkType.t_lsqb;
                    //    break;
                    //case ']':
                    //    type = myTkType.t_rsqb;
                    //    break;
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

        class myPaserResult
        {
            public List<string> arr = new List<string>();
            public int count = 0;
            public string curObj = "";
        }
        delegate void myRule(myPaserResult res, myToken tk);
        static void f1(myPaserResult res, myToken tk)
        {

        }
        static void f_ss(myPaserResult res, myToken tk)
        {
            res.count++;
            res.arr.Add("");
        }
        static void f_aa(myPaserResult res, myToken tk)
        {
            res.curObj += tk;
        }
        static void f_bs(myPaserResult res, myToken tk)
        {
            res.count++;
            res.arr.Add(res.curObj);
        }
        static void f_ba(myPaserResult res, myToken tk)
        {
            res.curObj += "\"";
        }
        static myState[,] tbl = new myState[4,3] {
            {myState.s, myState.a, myState.e, },
            {myState.a, myState.b, myState.a, },
            { myState.s, myState.a, myState.invalid,},
            {myState.s, myState.invalid, myState.e, },
        };
        static myRule[,] clbTbl = new myRule[4,3] {
            {f_ss, (res, tk) => {res.curObj = ""; },(res, tk) => {res.curObj = tk; }},
            {f_aa, f1, f_aa },
            {f_bs, f_ba, f1 },
            {f_bs, f1, f_aa },
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
            myPaserResult res = new myPaserResult();
            myState cur = myState.s;
            myState nState = myState.s;
            foreach (char c in line)
            {
                myToken tk = new myToken(c);
                cur = nState;
                nState = tbl[(int)cur, (int)tk.type];
                if (nState == myState.invalid) break;
                myRule cb = clbTbl[(int)cur, (int)tk.type];
                cb(res, tk);
            }

            //case eol
            return res;
        }
        static void myparser(string line)
        {
            Stack<myState> sStack = new Stack<myState>();
            Stack<myToken> tStack = new Stack<myToken>();
            //rule
            //-----------------------------
            //s->space s    //trim - not use
            //s->,s         //empty
            //s->"a
            //a->other a
            //a->space a
            //a->,a
            //a->"b
            //b->,s
            //b->"a
            //s->other e
            //e->other e
            //e->,s
            //------------------------------
            myState cur;
            myState nState = myState.s;
            //sStack.Push(myState.s);
            foreach (char c in line)
            {
                myToken tk = new myToken(c);
                cur = nState;
                nState = myState.invalid;
                switch(cur)
                {
                    case myState.s:
                        if (tk.type == myTkType.t_comma) nState = myState.s;
                        //else if (tk.type == myTkType.t_spec) nState = myState.s;
                        else if (tk.type == myTkType.t_dblq) nState = myState.a;
                        else if (tk.type == myTkType.t_other) nState = myState.e;
                        break;
                    case myState.a:
                        if (tk.type == myTkType.t_dblq) nState = myState.b;
                        else nState = myState.a;
                        break;
                    case myState.b:
                        if (tk.type == myTkType.t_comma) nState = myState.s;
                        else if (tk.type == myTkType.t_dblq) nState = myState.a;
                        break;
                    case myState.e:
                        if (tk.type == myTkType.t_other) nState = myState.e;
                        else if (tk.type == myTkType.t_comma) nState = myState.s;
                        break;
                }
                if (nState == myState.invalid) break;
            }
        }
    }
}
