using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace test_universalApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class dict : Page
    {
        public dict()
        {
            this.InitializeComponent();

            mainBtn.Click += MainBtn_Click;
            srchBtn.Click += SrchBtn_Click;
            nextBtn.Tapped += NextBtn_Click;
            prevBtn.Tapped += PrevBtn_Click;
            pasteBtn.Click += PasteBtn_Click;
            studyBtn.Click += StudyBtn_Click;

            myNode.regOnHyberlinkClick("dictModule", Hb_Click);

            Loaded += Dict_Loaded;
        }

        private void Dict_Loaded(object sender, RoutedEventArgs e)
        {
            string txt = mHistory.cur();
            if (txt != "")
            {
                historyTxt.Text = mHistory.print();
                search(txt, true);
            }
        }

        public class dictNaviParam
        {
            public string searchTxt;
            public int prePos;
        }

        private study.studyNaviParam studyNP;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            dictNaviParam parameter = (dictNaviParam)e.Parameter;
            if (parameter != null)
            {
                studyNP = new study.studyNaviParam { prePos = parameter.prePos };
                mainBtn.Visibility = Visibility.Collapsed;

                search(parameter.searchTxt);
            }
            else
            {
                studyBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void StudyBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(study), studyNP);
        }

        private void PasteBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = Clipboard.GetContent();
            string txt = "";
            var t = Task.Run(async () => { txt = await data.GetTextAsync(); });
            t.Wait();
            search(txt);
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            var txt = mHistory.prev();
            if (txt != "")
            {
                historyTxt.Text = mHistory.print();
                search(txt, true);
            }
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            var txt = mHistory.next();
            if (txt != "")
            {
                historyTxt.Text = mHistory.print();
                search(txt, true);
            }
        }

        private void SrchBtn_Click(object sender, RoutedEventArgs e)
        {
            string txt = srchTxt.Text;
            search(txt);
        }

        private void MainBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        myDict mDict = myDict.Load();

        static rHistory mHistory = new rHistory();

        string m_preTxt;
        Span curLine = new Span();
        int totalLine = 0;
        void search(string txt, bool isHistory = false)
        {
            //redue search
            if (m_preTxt != txt)
            {
                m_preTxt = txt;
                if (!isHistory)
                {
                    var tmp = "";
                    foreach (char c in txt)
                    {
                        if (myDictBase.isKanji(c)) {
                            tmp = tmp + c;
                        }
                    }
                    mHistory.add(tmp);
                    historyTxt.Text = mHistory.print();
                }
            }
            else
            {
                return;
            }

            srchRtb.Blocks.Clear();
            //rtbScroll.ChangeView(0, 0, null);
            totalLine = 0;

            //qryBgTask(new myBgTask { type = myBgTask.taskType.search, data = txt });
            fg_search(txt);
            //while (m_srchWorker.IsBusy)
            //{
            //    m_srchWorker.CancelAsync();
            //}
            //m_srchWorker.RunWorkerAsync();
        }
        private void Hb_Click(object sender, RoutedEventArgs e)
        {
            //
            var hb = (Hyperlink)sender;
            string text = ((Run)hb.Inlines[0]).Text;
            //text = hb.AccessKey;
            search(text);
        }
        Span crtDefBlck(myDefinition def)
        {
            return crtDefBlck(def, m_limitContentLen);
        }
        Span crtDefBlck(myDefinition def, int limit)
        {
            if (def.bFormated)
            {
                var des = myNode.convert2(def.text, limit);
                return des;
            }
            else
            {
#if false
                var s = new Span();
                s.Inlines.Add(new Run() { Text = def.text });
                s.Inlines.Add(new LineBreak());
                return s;
#else
                return addLink(def.text);
#endif
            }
        }
        Span addLink(string txt)
        {
            Span spn = null;
            //var sentences = txt.Split(new char[] { });
            var sentences = new string[] { txt };
            for (int i = 0; i < sentences.Length; i++)
            {
                var line = sentences[i];
                spn = new Span();
                var buff = new char[line.Length];
                int len = 0;
                foreach (char ch in line)
                {
                    if (mDict.IsKanji(ch))
                    {
                        Hyperlink hb = crtHb(ch.ToString());
                        if (len > 0)
                        {
                            var tmp = new string(buff, 0, len);
                            spn.Inlines.Add(new Run
                            {
                                Text = tmp
                            });
                            len = 0;
                        }
                        spn.Inlines.Add(hb);
                    }
                    else
                    {
                        buff[len] = ch;
                        len++;
                    }
                }
                if (len > 0)
                {
                    var tmp = new string(buff, 0, len);
                    spn.Inlines.Add(new Run { Text = tmp });
                    len = 0;
                }
            }
            return spn;
        }
        Span crtBlck(myRadical rd)
        {
            //言 Radical 149, speaking, speech
            var s = new Span();
            var r = new Run { Text = string.Format("{0} {1}", rd.zRadical, rd.iRadical) };
            s.Inlines.Add(r);
            var descr = crtDefBlck(rd.definitions[0]);
            s.Inlines.Add(descr);
            return s;
        }
        Span crtWdBlck(myWord wd, bool showBrift)
        {
            var s = new Span();
            if (showBrift)
            {
#if dict_dist
                if (!wd.definitions[0].bFormated)
#endif
                {
#if sepate_kanji
                    foreach (var kj in wd.term) s.Inlines.Add(crtBlck(kj));
#else
                    s.Inlines.Add(crtHb(wd.term));
#endif
                    //s.Inlines.Add(new Run { Text = string.Format(" {0}", wd.hn) });
                    s.Inlines.Add(new LineBreak());
                }
                s.Inlines.Add(crtDefBlck(wd.definitions[0], 1));
            }
            return s;
        }
        Span crtWdBlck(myWord wd)
        {
            var s = new Span();
#if dict_dist
            if (!wd.definitions[0].bFormated)
#endif
            if (true)
            {
#if sepate_kanji
                foreach (var kj in wd.term) s.Inlines.Add(crtBlck(kj));
#else
                s.Inlines.Add(crtHb(wd.term));
#endif
                s.Inlines.Add(new Run { Text = string.Format(" {0}", wd.hn) });
                s.Inlines.Add(new LineBreak());
            }
            if (wd.definitions.Count > 0)
            {
                s.Inlines.Add(crtDefBlck(wd.definitions[0]));
            }
            return s;
        }
        Block crtBlck(myKanji kanji)
        {
            var p = new Paragraph();
            var r = new Run
            {
                Text = string.Format("{2} {0} {1}",
                kanji.extraStrokes, kanji.totalStrokes, kanji.val)
            };
            var s = new Span();
            s.Inlines.Add(r);
            p.Inlines.Add(s);
            foreach (var df in kanji.definitions)
            {
                var tmp = crtDefBlck(df);
                p.Inlines.Add(new LineBreak());
                p.Inlines.Add(tmp);
            }
            foreach (var wd in kanji.relatedWords)
            {
                var tmp = crtWdBlck(wd);
            }
            return p;
        }
        private Hyperlink crtBlck(char val)
        {
            var hb = new Hyperlink();
            hb.Click += Hb_Click;
            hb.Inlines.Add(new Run() { Text = val.ToString(), FontSize = 30 });
            return hb;
        }
        private Hyperlink crtHb(string txt)
        {
            var hb = new Hyperlink();
            hb.Click += Hb_Click;
            hb.Inlines.Add(new Run() { Text = txt });
            return hb;
        }
        class myFgTask
        {
            public enum qryType
            {
                hyperlink,
                linebreak,
                run,
                define,
                word,
                scroll,
                speech,
                loadProgress,
                updateGUI
            }
            public qryType type;
            public object data;
            public myFgTask(qryType type, object data)
            {
                this.type = type;
                this.data = data;
            }
        }
        //call from background
        BackgroundWorker m_srchWorker;
        bool m_srchWorkerIsRunning { get { return m_srchWorker.IsBusy; } }
        void bg_qryFgTask(myFgTask.qryType type, object data)
        {
            m_srchWorker.ReportProgress(totalLine++, new myFgTask(type, data));
        }

        void fg_processQry(myFgTask.qryType type, object data)
        {
            switch (type)
            {
                case myFgTask.qryType.updateGUI:
                    break;
                case myFgTask.qryType.loadProgress:
                    {
                        Debug.WriteLine("load progress ");
                    }
                    break;
                case myFgTask.qryType.speech:
                    break;
                case myFgTask.qryType.linebreak:
                    {
                        Paragraph p = new Paragraph();
                        //curLine.Inlines.Add(new LineBreak());
                        p.Inlines.Add(curLine);
#if false
                        m_srchWorker.ReportProgress(totalLine++, p);
#else
                        srchRtb.Blocks.Add(p);
#endif
                        curLine = new Span();
                    }
                    break;
                case myFgTask.qryType.hyperlink:
                    {
                        var ch = (char)data;
                        Hyperlink hb = crtBlck(ch);
                        curLine.Inlines.Add(hb);
                    }
                    break;
                case myFgTask.qryType.run:
                    {
                        var txt = (string)data;
                        curLine.Inlines.Add(new Run { Text = txt });
                    }
                    break;
                case myFgTask.qryType.define:
                    {
                        var def = (myDefinition)data;
                        curLine.Inlines.Add(crtDefBlck(def));
                    }
                    break;
                case myFgTask.qryType.word:
                    {
                        var wd = (myWord)data;
                        curLine.Inlines.Add(crtWdBlck(wd));
                    }
                    break;
                case myFgTask.qryType.scroll:
                    //rtbScroll.ScrollToVerticalOffset(0);
                    rtbScroll.ChangeView(0, 0, 1);
                    break;
            }
        }
        //run in background
        //fg_search
        delegate void srchCallback(myFgTask.qryType type, object data);
        void fg_search(string txt)
        {
            srchCallback callback = fg_processQry;
            search(callback, txt);
        }
        void bg_search(string txt)
        {
            srchCallback callback = bg_qryFgTask;
            search(callback, txt);
        }
        
        List<myKanji> mixSearch(string txt)
        {
            List<myKanji> ret = new List<myKanji>();
            Regex reg = new Regex(@"[^\w\s]+");
            txt = reg.Replace(txt, " ");
            char[] buff = new char[txt.Length];
            string zKanji = "";
            int i;
            for (i = 0; i < txt.Length; i++)
            {
                char ch = txt[i];
                if (mDict.m_kanjis.ContainsKey(ch))
                {
                    zKanji += ch;
                    ch = ' ';
                }
                else if ((0x3040 < ch && ch < 0x3097)
                   || (0x30A0 < ch && ch < 0x31FF))
                {
                    ch = ' ';
                }
                buff[i] = char.ToLower(ch);
            }
            if (zKanji.Length > 0) ret.AddRange(mDict.Search(zKanji));
            ret.AddRange(mDict.SearchHn(new string(buff)));
            return ret;
        }

        //int m_limitContentLen { get { return m_option.fullDef ? -1 : 3; } }
        //int m_limitContentCnt { get { return m_option.fullDef ? -1 : 7; } }
        int m_limitContentLen = -1;
        int m_limitContentCnt = -1;
        myWord removeDuplicate(List<myKanji> kanjis, string w, List<myWord> words)
        {
            var tHash = new HashSet<string>();
            myWord found = null;
            words.RemoveAll(word =>
            {
                if (tHash.Contains(word.term))
                {
                    return true;
                }
                tHash.Add(word.term);
                if (kanjis.Exists(kanji => (word.term.Length == 1) && (kanji.val == word.term[0])))
                {
                    return true;
                }
                if (w == word.hn || w == word.term)
                {
                    found = word;
                    return true;
                }
                return false;
            });
            return found;
        }
        void search(srchCallback callback, string txt)
        {
            //= m_preTxt;
            var kanjis = mixSearch(txt);
            List<myWord> words = new List<myWord>();
            List<myWord> verbs = new List<myWord>();
            //Span s = new Span();
            foreach (var kanji in kanjis)
            {
                //display kanji with link
                callback(myFgTask.qryType.hyperlink, kanji.val);
                if (kanji.decomposite != "")
                {
                    callback(myFgTask.qryType.run, kanji.decomposite);
                    callback(myFgTask.qryType.linebreak, null);
                }

                //display japanese define
                callback(myFgTask.qryType.define, kanji.definitions[0]);
                kanji.definitions.RemoveAt(0);

                //find kanji define - formated txt
                var foundKanji = kanji.relatedWords.Find((w) => { return w.term == kanji.val.ToString(); });
                if (foundKanji != null)
                {
                    //display kanji define (in formated txt)
                    callback(myFgTask.qryType.define, (foundKanji.definitions[0]));
                }
                else
                {
                    //display radical info
                    var rdInfo = mDict.Search(kanji.radical.zRadical.ToString());
                    string zRad = string.Format("Bộ {0} {1} {2} [{3}, {4}] {5}",
                        kanji.radical.iRadical, kanji.radical.zRadical, kanji.radical.hn, kanji.radical.nStrokes,
                        kanji.totalStrokes, kanji.val);
                    {
                        if (kanji.hn != null) zRad += string.Format(" ({0})", kanji.hn);
                        if (kanji.simple != '\0') zRad += string.Format(" simple {0}", kanji.simple);
                    }
                    callback(myFgTask.qryType.run, zRad);

                    //display other kanji define
                    foreach (var def in kanji.definitions)
                    {
                        callback(myFgTask.qryType.define, (def));
                        break;
                    }
                    callback(myFgTask.qryType.linebreak, null);
                }
                words.AddRange(kanji.relatedWords);
                verbs.AddRange(kanji.relateVerbs);
                callback(myFgTask.qryType.linebreak, null);
            }
            callback(myFgTask.qryType.linebreak, null);

            //remove duplicated
            var found = removeDuplicate(kanjis, txt, words);

            //var sFound = new Span();
            if (found != null)
            {
                callback(myFgTask.qryType.word, found);
                callback(myFgTask.qryType.linebreak, null);
            }
            //related word
            //if (found != null) bg_qryDisplay(sFound);
            //bg_qryDisplay(new Run { Text = "related word:" });
            //bg_qryDisplay(new LineBreak());
            int count = 0;
            foreach (var rWd in words)
            {
#if !show_brift
                //m_limitContentCnt
                //  (-1) no limit
                if ((count++) == m_limitContentCnt)
                    break;
#else
                {
                    bg_qryDisplay(crtWdBlck(rWd, true));
                }
                else
#endif
                {
                    callback(myFgTask.qryType.word, (rWd));
                }
                //bg_qryDisplay(new LineBreak());
                callback(myFgTask.qryType.linebreak, null);
            }

            //verb
#if show_verb
            if (m_option.showVerb)
            {
                foreach (var v in verbs)
                {
#if !show_brift
                    //m_limitContentCnt
                    //  (-1) no limit
                    if ((count++) == m_limitContentCnt)
                        break;
#else
                {
                    bg_qryDisplay(crtWdBlck(rWd, true));
                }
                else
#endif  //show_brift
                    {
                        callback(myFgTask.qryType.word, (v));
                    }
                    //bg_qryDisplay(new LineBreak());
                    callback(myFgTask.qryType.linebreak, null);
                }
            }
#endif //show_verb
            callback(myFgTask.qryType.scroll, null);
        }
    }
}
