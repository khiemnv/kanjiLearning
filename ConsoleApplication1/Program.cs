

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleApplication1
{

    class Program
    {

        static void Main(string[] args)
        {

            //byte[] sample = new byte[4] { 0xF0, 0xA9, 0xA0, 0x8C };
            //string str = UTF8Encoding.UTF8.GetString(sample);
            //return;
            //test 
            //test parser
            //csvParser csv = new csvParser();
            //csv.uri = new Uri(@"ms-appx://Assets/hv_org.csv", UriKind.Absolute);
            //csv.start();
            //return;
            //test dict
            Console.WriteLine("load dict start");
            int tickcount = Environment.TickCount;
            myDict dict = myDict.Load();
            tickcount = (Environment.TickCount - tickcount);
            Console.WriteLine("load dict completed {0}", tickcount);
            //return;

            string w = "言葉";
            string p = @"D:\tmp\kanjiLearning\kanji";
            var di = new DirectoryInfo(p);
            var i = 0;
            var files = di.GetFiles("*.json");
            var chapters = files.ToList().ConvertAll(file =>
            {
                var txt = System.IO.File.ReadAllText(file.FullName);
                var c = txt.FromJson<List<Note>>();
                var tag = Path.GetFileNameWithoutExtension(file.Name).Replace("_", "\\");
                return new Chapter { notes = c, tag = tag };
            });

            // all kanjis
            var total = 0;
            var d = new Dictionary<string, Container>();
            var singleWord = new Dictionary<string,Container>();
            foreach (var c in chapters)
            {
                foreach (var n in c.notes)
                {
                    // search kanji
                    var kanjis = dict.GetKanjis(n.kanji);
                    foreach (var kanji in kanjis)
                    {
                        if (!d.ContainsKey(kanji))
                        {
                            var ret = dict.Search(kanji);
                            d.Add(kanji, new Container
                            {
                                kanji = ret[0],
                                noteWraps = new List<NoteWrap>()
                            });
                        }
                    }
                        
                    // word contains one kanji char
                    if (kanjis.Count == 1)
                    {
                        var k1 = kanjis[0];
                        n.kanjiObj = d[k1].kanji;
                        var c1 = d[k1];
                        c1.distributedCount++;
                        if (!singleWord.ContainsKey(k1)) { singleWord.Add(k1,c1); }
                    }
                    else if (kanjis.Count > 1)
                    {
                        for (int idx = 0;idx<kanjis.Count;idx++) { 
                            var kanji = kanjis[idx];
                            var container = d[kanji];
                            container.noteWraps.Add(new NoteWrap { idx = idx, note = n, kanjisCount=kanjis.Count });
                        }

                        // total words that has more than one kanji char
                        total++;
                    }
                }
            }

            // sort and init container cursor
            foreach (var container in d.Values)
            {
                container.noteWraps.Sort((a, b) => a.idx - b.idx);
                container.popOutCur = container.noteWraps.Count - 1;
            }

            // add kanji to notes
            var remain = total;
            var list = d.Keys.ToList();
            var unprocessed = new List<string>();
            while (remain > 0)
            {
                var processedLst = new List<string>();
                var popOutLst = new List<string>();
                foreach (var kanji in list)
                {
                    if (kanji == "族")
                    {
                        Debug.WriteLine($"{remain}");
                    }

                    var container = d[kanji];
                    var notewraps = container.noteWraps;

                    var emptyIdx =notewraps.FindIndex(tw=>tw.note.kanjiObj == null);
                    if (emptyIdx != -1)
                    {
                        // add
                        notewraps[emptyIdx].note.kanjiObj = container.kanji;
                        processedLst.Add(kanji);
                        container.distributedCount++;
                        remain--;
                    }

                    else if (container.distributedCount > 0)
                    {
                        // skip
                        Debug.WriteLine($"distributedCount: {container.distributedCount} of {kanji}");
                    }

                    // pop out
                    // replace
                    else if (container.popOutCur >= 0)
                    {
                        var noteWrap = container.noteWraps[container.popOutCur];
                        var old = noteWrap.note.kanjiObj;
                        var c2 = d[old.val.ToString()];
                        if (c2.distributedCount > 1)
                        {
                            c2.distributedCount--;
                            popOutLst.Add(old.val.ToString());
                            processedLst.Remove(old.val.ToString());
                            Debug.WriteLine($"replace: {old.val} by {kanji}");

                            noteWrap.note.kanjiObj = container.kanji;
                            container.distributedCount++;
                            processedLst.Add(kanji);
                            container.popOutCur--; // zero base
                        } else
                        {
                            Debug.WriteLine($"unprocessed: {kanji}");
                            unprocessed.Add(kanji);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"unprocessed: {kanji}");
                        unprocessed.Add(kanji);
                    }
                }

                // continue
                //if (popOutLst.Count > 0)
                //{
                //    list = popOutLst;
                //}
                //else
                {
                    list = d.Keys.ToList();
                }

                // debug
                Debug.WriteLine($"processedLst: {string.Join(",", processedLst)}\npopOutLst:{string.Join(",", popOutLst)} ");
            }

            // export
            System.IO.File.WriteAllText("chapters.json",chapters.ToJson());
            return;
            for (; ; )
            {
                w = "突っ込む";
                var ret = dict.Search(w);
                foreach (var v in ret)
                {
                    var obj = new
                    {
                        v.decomposite,
                        definitions = JoinDefs(v.definitions),
                        v.extraStrokes,
                        v.hn,
                        radical = v.radical.zRadical,
                        radicalDef = JoinDefs(v.radical.definitions),
                        radicalStrokes = v.radical.nStrokes,
                        radicalIdx = v.radical.iRadical,
                        radicalHn = v.radical.hn,
                        relatedWords = string.Join("", v.relatedWords.Select(tw => $"{tw.term} {tw.hn} {JoinDefs(tw.definitions)}").ToList())
                    };
                    var txt = obj.ToJson();
                    Debug.Write(obj.ToJson());
                }
            }
        }

        class Container
        {
            public int distributedCount;
            public string c;
            public List<NoteWrap> noteWraps;
            public myKanji kanji; // kanji objedt
            public int popOutCur; // cursor
        }
        class NoteWrap
        {
            public int idx; // index of kanji in note
            public int kanjisCount; // number of kanjis in word
            public Note note;
        }

        class Note
        {
            public string kanji; // 危険（な）
            public string radical; // 厄 ÁCH, NGỎA
            public string hira; // きけん（な
            public string vn; // nguy hiểm
            public string hn; // NGUY HIỂM
            public string explain; // Cao, ở nơi cao mà ghê sợ gọi là nguy.
            public string tag; // kanji\2\1\1
            public string meaning; // decomposite
            public myKanji kanjiObj; // single kanji char obj
        }
        class Chapter
        {
            public string tag;
            public List<Note> notes;
        }

        static HtmlDocument htmlSnippet = new HtmlDocument();
        public static string JoinDefs(List<myDefinition> defs)
        {
            List<string> list = new List<string>();
            foreach (var d in defs)
            {
                if (!d.bFormated)
                {
                    list.Add(d.text);
                }
                else
                {
                    htmlSnippet.LoadHtml(d.text);
                    var f = string.Join("", htmlSnippet.DocumentNode.Descendants("font").Select(y => y.InnerText).ToList());
                    var li = htmlSnippet.DocumentNode.SelectNodes("//div/ol/li").Select(y => y.InnerText).ToList();
                    list.AddRange(li);
                }
            }
            return $"<ul>{string.Join("", list.ConvertAll(li => $"<li>{li}</li>"))}</ul>";
        }

        void test_queue()
        {

            //test queue
            myQueue<int> q = new myQueue<int>();
            q.push(1);
            int tmp = q.pop();
            q.push(2);
            q.push(3);
            tmp = q.pop();
            tmp = q.pop();
            return;
        }
#if false
        void test_dict()
        {
            test_dict1();   //hv_org.csv
            test_dict2();   //hanvietdict.js
            test_dict3();   //bo thu
            test_dict4();   //hv_word.csv

            string w = "言葉";
            List<myKanji> kanjis = new List<myKanji>();
            foreach (char key in w) {
                if (myDictBase.m_kanjis.ContainsKey(key))
                {
                    var arr = myDictBase.m_kanjis[key].Distinct();
                    myKanji kanji = new myKanji();
                    foreach(var rec in arr)
                    {
                        rec.format(kanji);
                    }
                    kanjis.Add(kanji);
                }
            }
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
#endif
    }
}
