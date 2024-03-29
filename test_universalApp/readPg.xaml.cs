﻿//#define test_study_page
//#define init_status
//#define item_editable - case file/folder is renamed or deleted
//#define start_use_checkbox
#define once_synth
#define reduce_disk_opp
//#define transparent_canvas

#define dict_dist
#define sepate_kanji

using System;
using System.Collections.Generic;
using System.IO;
//using Windows.Data.Xml.Dom;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace test_universalApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class readPg : Page, IDisposable
    {
        static contentProvider s_cp = contentProvider.getInstance();
        List<UIElement> m_grp1;
        List<UIElement> m_grp2;
        wordItem m_editingItem;
        static studyOption m_option;
        static public event EventHandler<string> GetKanji;
        List<wordItem> m_items = new List<wordItem>();
        List<wordItem> m_markedItems = new List<wordItem>();
        //BackgroundWorker m_worker = new BackgroundWorker();
        BackgroundWorker m_srchWorker;
        bool m_srchWorkerIsRunning { get { return m_srchWorker.IsBusy; } }
        int m_iCursor;

        class myBgTask
        {
            public enum taskType
            {
                speek,
                search,
                loadData,
                //loadDataComplete,
            }
            public taskType type;
            public object data;
        }
        Queue<myBgTask> m_msgQueue = new Queue<myBgTask>();

        //singleton dict
        myDict dict = myDict.Load();

        public readPg()
        {
            this.InitializeComponent();

            //register play end event
            media.MediaOpened += Media_MediaOpened;
            media.MediaEnded += Media_MediaEnded;
            media.MediaFailed += Media_MediaFailed;

            //decorate
            initCtrls();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public class studyNaviParam
        {
            public int prePos;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            studyNaviParam parameter = e.Parameter as studyNaviParam;
            if (parameter != null)
            {
                m_iCursor = parameter.prePos;
            }
            else
            {
                m_iCursor = 0;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            m_srchWorker = new BackgroundWorker();

            //init search worker
            m_srchWorker.DoWork += M_srchWorker_DoWork;
            m_srchWorker.ProgressChanged += M_srchWorker_ProgressChanged;
            m_srchWorker.RunWorkerCompleted += M_srchWorker_RunWorkerCompleted;
            m_srchWorker.WorkerReportsProgress = true;
            m_srchWorker.WorkerSupportsCancellation = true;

            //synthDic = new Dictionary<string, SpeechSynthesizer>();
            rtb.Padding = new Thickness(5, 2, 10, 2);

            loadData();
        }
        
        //private void OptSrchTxtEnableChk_Click(object sender, RoutedEventArgs e)
        //{
        //    m_option.srchTxtEnable = (bool)optSrchTxtEnableChk.IsChecked;
        //    optSrchTxtEnableChk.IsEnabled = m_option.srchEnable;
        //}

        BitmapImage speakBM = new BitmapImage(new Uri("ms-appx:///Assets/speak.png"));
        BitmapImage speakBM2 = new BitmapImage(new Uri("ms-appx:///Assets/speak2.png"));
        private void Media_MediaOpened(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("{0} Media_MediaOpened play start", this);
            m_speakStat = speakStatus.engine_played;
            //speakImage.Source = speakBM2;
        }

        void speakTxtEnd(bool isSucess)
        {
            //speakImage.Source = speakBM;
#if !once_synth
            lastTTSstream.Dispose();
            lastTTSsynth.Dispose();
#else
            lastTTSstream.Dispose();
            lastTTSstream = null;
#endif
            m_speakStat = speakStatus.end;
        }

        private void Media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            m_speakStat = speakStatus.engine_finished;
            Debug.WriteLine("{0} Media_MediaFailed play failed {1}", this, e.ErrorMessage);
            speakTxtEnd(false);
        }

        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            m_speakStat = speakStatus.engine_finished;
            Debug.WriteLine("{0} Media_MediaEnded play success", this);
            speakTxtEnd(true);
        }

        //Dictionary<string, VoiceInformation> voiceDict = new Dictionary<string, VoiceInformation>();
        bool getVoice(out VoiceInformation voice, string lang)
        {
            voice = null;
            //if (voiceDict.ContainsKey(lang)) {
            //    voice = voiceDict[lang];
            //}
            //else {
                foreach(var v in SpeechSynthesizer.AllVoices)
                {
                    if (v.Language.Contains(lang))
                    {
                        //voiceDict.Add(lang, v);
                        voice = v;
                        break;
                    }
                }
            //}
            return voice != null;
        }

        enum speakStatus
        {
            begin,
            called,
            engine_played,
            engine_finished,
            end
        }
        speakStatus m_speakStat = speakStatus.end;

        void speakTxt()
        {
#if false
            if (m_speakStat != speakStatus.end)
            {
                Debug.WriteLine("{0} speakTxt m_speakStat {1}", this, m_speakStat.ToString());
                return;
            }
            m_speakStat = speakStatus.begin;
#endif
            //need disable all action
            var items = getCurItems();
            var item = items[m_iCursor];
            string txt;
            string lang;
            item.getSpeakInfo(item.status, out txt, out lang);
#if test_study_page
            txt = "hello world";
            lang = "en-US";
#endif
#if false
            speakTxt(txt, lang);
#else
            qryBgTask(new myBgTask
            {
                type = myBgTask.taskType.speek,
                data = new mySpeechQry { txt = txt, lang = lang }
            });
#endif
        }
        void qryBgTask(myBgTask task)
        {
            Debug.WriteLine("{0} qryBgTask {1}", this, task.type);
            m_msgQueue.Enqueue(task);
            if (m_srchWorkerIsRunning == false)
            {
                //m_srchWorkerIsRunning = true;
                m_srchWorker.RunWorkerAsync();
            }
        }
        class mySpeechQry
        {
            public string txt;
            public string lang;
        }

#if !once_synth
        SpeechSynthesizer lastTTSsynth;
        SpeechSynthesisStream lastTTSstream;
#else
        //SpeechSynthesizer lastTTSsynth = new SpeechSynthesizer();
        Dictionary<string, SpeechSynthesizer> synthDic;
        SpeechSynthesisStream lastTTSstream;
        SpeechSynthesizer getSpeechSynth(string lang)
        {
            bool ret = synthDic.ContainsKey(lang);
            if (ret)
            {
                return synthDic[lang];
            }

            //if not exist in cache
            //  + crt new instance
            VoiceInformation voice;
            ret = getVoice(out voice, lang);
            if (!ret) return null;

            var s = new SpeechSynthesizer {Voice = voice };
            synthDic.Add(lang, s);
            return s;
        }
#endif

        async Task speakTxt(string txt, string lang)
        {
            Debug.WriteLine("{0} speakTxt call play", this);
            //txt = "hello world";
            //IEnumerable<VoiceInformation> frenchVoices = from voice in SpeechSynthesizer.AllVoices
            //                                             where voice.Language == "fr-FR"
            //                                             select voice;
            //VoiceInformation voice;
            //bool ret = getVoice(out voice, lang);
            var synth = getSpeechSynth(lang);
            if (synth!=null)
            {
                m_speakStat = speakStatus.called;
#if !once_synth
                lastTTSsynth = new SpeechSynthesizer();
#endif
                lastTTSstream = await synth.SynthesizeTextToStreamAsync(txt);
            }
            else
            {
#if false   //not show error msgbox in background
                MessageDialog msgbox = new MessageDialog(
                    string.Format("Not found {0} voice infomation. " +
                    "Maybe {0} voice recognition was not installed!", lang));
                msgbox.Title = "Speak word error!";
                await msgbox.ShowAsync();
#endif
                //m_speakStat = speakStatus.end;
            }
        }

        private void CanvasSpeak_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //speak text
            speakTxt();
            Debug.WriteLine("{0} CanvasSpeak_Tapped speak return", this);
        }

        private void initCtrls()
        {
            //option panel

#if item_editable
            //editTxt.AcceptsReturn = true;
            //editTxt.TextWrapping = TextWrapping.Wrap;
            //editTxt.Header = "Editing word (please use \";\" as separator)";
            editTxt.PlaceholderText = "Using \";\" as separator";
            //ScrollViewer.SetVerticalScrollBarVisibility(editTxt, ScrollBarVisibility.Auto);
#endif

            //editEllipse.Stroke = new SolidColorBrush(Colors.White);
            //cancelEllipse.Stroke = new SolidColorBrush(Colors.White);
            //acceptEllipse.Stroke = new SolidColorBrush(Colors.White);
            //starEllipse.Stroke = new SolidColorBrush(Colors.White);


            //termTxt.IsTextSelectionEnabled = true;


            //disable search txt select
            //srchRtb.IsTextSelectionEnabled = m_option.srchTxtEnable;
        }

        private void M_srchWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine("M_srchWorker_RunWorkerCompleted");
            //m_srchWorkerIsRunning = false;
        }

        private void M_srchWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //throw new NotImplementedException();
            Debug.WriteLine(string.Format("{0} M_srchWorker_ProgressChanged {1}", this, e.ProgressPercentage));
            var qry = (myFgTask)e.UserState;
            fg_processQry(qry.type, qry.data);
        }

        void sleep(int timeout)
        {
            var t = Task.Run(() => Task.Delay(timeout));
            t.Wait();
        }
        private void M_srchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //throw new NotImplementedException();
            uint i = 0;
            for(;;i++) {
                if (m_msgQueue.Count >0)
                {
                    var msg = m_msgQueue.Dequeue();
                    switch (msg.type)
                    {
                        //case myBgTask.taskType.loadDataComplete:
                        //    bg_qryFgTask(myFgTask.qryType.updateGUI, null);
                        //    break;
                        case myBgTask.taskType.loadData:
                            bg_loadData(null, null);
                            break;
                        case myBgTask.taskType.search:
                            bg_search((string)msg.data);
                            break;
                        case myBgTask.taskType.speek:
                            {
                                var sq = (mySpeechQry)msg.data;
                                var tBegin = Environment.TickCount / 1000;
                                Debug.WriteLine("M_srchWorker_DoWork speak {0} start", sq.txt);
                                var t = Task.Run(()=>speakTxt(sq.txt, sq.lang));
                                t.Wait();
                                bg_qryFgTask(myFgTask.qryType.speech, null);
                                while(m_speakStat != speakStatus.end) { sleep(100); }
                                //t.Wait();
                                tBegin = Environment.TickCount / 1000 - tBegin;
                                Debug.WriteLine("M_srchWorker_DoWork speak end {0}", tBegin);
                            }
                            break;
                    }
                }
                else
                {
                    Debug.WriteLine("M_srchWorker_DoWork fall sleep, i = {0}", i);
                    //sleep(1000);
                    break;
                }
            }
        }

        private void updateStatus(string v)
        {
            headerTxt.Text = v;
            Debug.WriteLine(string.Format("[status] {0}", v));
        }

        private void Study_GetKanji(object sender, string e)
        {
            string txt = e;
            search(txt);
        }

#region search_rgn
        int m_limitContentLen { get { return m_option.fullDef ? -1 : 3; } }
        int m_limitContentCnt { get { return m_option.fullDef ? -1 : 7; } }

        bool singleTap;
        private void SrchBtn_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Debug.WriteLine("SrchBtn_DoubleTapped enter");
            this.singleTap = false;

            //show or hide search panel
            SearchBnt2_Click(sender, e);
        }

        private async void srchBtn_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("srchBtn_Click enter");

            this.singleTap = true;
            await Task.Delay(200);
            if (this.singleTap)
            {
                //show search panel
                if (!m_option.srchEnable)
                {
                    SearchBnt2_Click(sender, e);
                }
                else
                {
                    //search
                    //string txt = srchTxt.Text;
                    //search(txt);
                }
            }

            Debug.WriteLine("srchBtn_Click leave {0}", singleTap);
        }

        private void Hb_Click(object sender, RoutedEventArgs e)
        {
            //
            var hb = (Hyperlink)sender;
            string text = ((Run)hb.Inlines[0]).Text;
            //text = hb.AccessKey;
            dictCtrl.Search(text);
        }

        string m_preTxt = "init";
        void search(string txt)
        {
            //redue search
            if (m_preTxt != txt) {
                m_preTxt = txt;
            } else {
                return;
            }

            //srchRtb.Blocks.Clear();
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
        Span curLine = new Span();
        int totalLine = 0;

        class myFgTask {
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
        void bg_qryFgTask(myFgTask.qryType type, object data)
        {
            m_srchWorker.ReportProgress(totalLine++, new myFgTask(type,data));
        }
        void fg_processQry(myFgTask.qryType type, object data) {
            switch (type)
            {
                case myFgTask.qryType.updateGUI:
                    updateGUI(null, null);
                    break;
                case myFgTask.qryType.loadProgress:
                    {
                        Debug.WriteLine("load progress ");
                    }
                    break;
                case myFgTask.qryType.speech:
                    if (lastTTSstream != null)
                    {
                        // The media object for controlling and playing audio.
                        MediaElement mediaElement = media;
                        //MediaElement mediaElement = new MediaElement();
                        mediaElement.SetSource(lastTTSstream, lastTTSstream.ContentType);
                        Debug.WriteLine("  + call play()");
                        mediaElement.Play();
                        Debug.WriteLine("  + play() return");
                    }
                    break;
                case myFgTask.qryType.linebreak:
                    {
                        Paragraph p = new Paragraph();
                        //curLine.Inlines.Add(new LineBreak());
                        p.Inlines.Add(curLine);
#if false
                        m_srchWorker.ReportProgress(totalLine++, p);
#else
                        //srchRtb.Blocks.Add(p);
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
                    //rtbScroll.ChangeView(0, 0, 1);
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
        void search(srchCallback callback, string txt) {
             //= m_preTxt;
            var ret = dict.Search(txt);
            List<myWord> words = new List<myWord>();
            List<myWord> verbs = new List<myWord>();
            //Span s = new Span();
            foreach (var kanji in ret)
            {
                //display kanji with link
                callback(myFgTask.qryType.hyperlink, kanji.val);
                if (kanji.decomposite != "") {
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
                    var rdInfo = dict.Search(kanji.radical.zRadical.ToString());
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
            //found word
            myWord found = null;
            if (ret.Count > 1) { found = words.Find((w) => { return w.term == txt; }); }
            //var sFound = new Span();
            if (found != null)
            {
                callback(myFgTask.qryType.word, found);
                callback(myFgTask.qryType.linebreak, null);
                //remove from list
                words.Remove(found);
            }
            //related word
            //if (found != null) bg_qryDisplay(sFound);
            //bg_qryDisplay(new Run { Text = "related word:" });
            //bg_qryDisplay(new LineBreak());
            int count = 0;
            foreach (var rWd in words)
            {
                if (txt.Contains(rWd.term)) continue;
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
            if (m_option.showVerb)
            {
                foreach(var v in verbs)
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
                        callback(myFgTask.qryType.word, (v));
                    }
                    //bg_qryDisplay(new LineBreak());
                    callback(myFgTask.qryType.linebreak, null);
                }
            }
#if false
            //create paragraph
            var p = new Paragraph();
            //if (found != null) p.Inlines.Add(sFound);
            p.Inlines.Add(s);
            srchRtb.Blocks.Add(p);
            //TextPointer pstart = rtb.ContentStart;
            //rtb.Select(pstart, pstart);
            //rtbScroll.VerticalScrollMode = ScrollMode.Enabled;
            //rtbScroll.BringIntoViewOnFocusChange = true;
            rtbScroll.ChangeView(0, 0, null);
            //rtbScroll.ScrollToVerticalOffset(0);
#endif
            callback(myFgTask.qryType.scroll, null);
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
                var s = new Span();
                s.Inlines.Add(new Run() { Text = def.text });
                s.Inlines.Add(new LineBreak());
                return s;
            }
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
            {
#if sepate_kanji
                foreach (var kj in wd.term) s.Inlines.Add(crtBlck(kj));
#else
                s.Inlines.Add(crtHb(wd.term));
#endif
                s.Inlines.Add(new Run { Text = string.Format(" {0}", wd.hn) });
                s.Inlines.Add(new LineBreak());
            }
            s.Inlines.Add(crtDefBlck(wd.definitions[0]));
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
            hb.UnderlineStyle = UnderlineStyle.None;
            hb.Click += Hb_Click;
            hb.Inlines.Add(new Run() { Text = val.ToString() });
            return hb;
        }
        private Hyperlink crtHb(string txt)
        {
            var hb = new Hyperlink();
            hb.Click += Hb_Click;
            hb.Inlines.Add(new Run() { Text = txt });
            return hb;
        }
#endregion

        void showHideCtrls(bool editMode)
        {
            var grp1V = editMode ? Visibility.Collapsed : Visibility.Visible;
            var grp2V = editMode ? Visibility.Visible : Visibility.Collapsed;
            foreach (var e in m_grp1) { e.Visibility = grp1V; }
            foreach (var e in m_grp2) { e.Visibility = grp2V; }
        }

      

        private void Split_PaneClosed(SplitView sender, object args)
        {
            turnSearchOnOff(m_option.srchEnable);
            //updateTerm();
            m_option.save();
        }

        private void DetailChk_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //m_option.showDetail = (bool)optWordDetailChk.IsChecked;
        }

        private void TermCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            m_option.termMode = (mode)cmb.SelectedIndex;
        }

        private void DefineCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            m_option.defineMode = (mode)cmb.SelectedIndex;
        }

        enum itemStatus {
            term,
            define
        }

        enum mode
        {
            kanji,
            hiragana,
            hn,
            vn
        }

        [DataContract]
        class studyOption
        {
            [DataMember]
            public mode termMode;
            [DataMember]
            public mode defineMode;
            [DataMember]
            public bool showDetail;
            [DataMember]
            public bool showMarked;
            [DataMember]
            public bool spkTerm;
            [DataMember]
            public bool srchEnable;
            [DataMember]
            public bool srchTxtEnable;
            [DataMember]
            public bool spkDefine;
            [DataMember]
            public bool showVerb;
            [DataMember]
            public bool fullDef;
            [DataMember]
            public bool selectTxtOn;

            public studyOption() { }

            static studyOption m_option;
            public static studyOption getInstance()
            {
                if (m_option == null)
                {
                    var t = Task.Run(async () => m_option = await load());
                    t.Wait();
                    if (m_option == null) m_option = new studyOption
                    { termMode = mode.kanji, defineMode = mode.hiragana};
                }
                return m_option;
            }

            static string m_optionFile = "option.cfg";
            static async Task<studyOption> load()
            {
                studyOption ret = null;
                StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    m_optionFile, CreationCollisionOption.OpenIfExists);
                BasicProperties bs = await dataFile.GetBasicPropertiesAsync();
                if (bs.Size > 0)
                {
                    Stream stream = await dataFile.OpenStreamForReadAsync();
                    stream.Seek(0, SeekOrigin.Begin);
                    DataContractSerializer serializer = new DataContractSerializer(typeof(studyOption));
                    var obj = (studyOption)serializer.ReadObject(stream);
                    if (obj != null) { ret = obj; }
                    stream.Dispose();
                }
                //await Task.Delay(1000);
                Debug.WriteLine("{0} load complete {1}", "studyOpt", Environment.TickCount);
                return ret;
            }
            public async void save()
            {
                StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    m_optionFile, CreationCollisionOption.OpenIfExists);
                Stream writeStream = await dataFile.OpenStreamForWriteAsync();
                writeStream.SetLength(0);
                DataContractSerializer serializer = new DataContractSerializer(typeof(studyOption));
                serializer.WriteObject(writeStream, m_option);
                await writeStream.FlushAsync();
                writeStream.Dispose();
            }
        }

        class wordItem
        {
            //public wordItem(studyOption option) { m_option = option; }
            //studyOption m_option;
            public chapter c = null;
            public int index;
            public word word;
            public itemStatus status;
            public bool marked { get { return word.isMarked; } set { word.isMarked = value; } }
            private string getDefine(mode m)
            {
                switch (m)
                {
                    case mode.kanji:
                        OnGetKanji(word.kanji);
                        return word.kanji;
                    case mode.hiragana:
                        return word.hiragana;
                    case mode.hn:
                        return word.hn;
                    case mode.vn:
                        return word.vn;
                }
                return "";
            }

            protected virtual void OnGetKanji(string txt)
            {
                GetKanji?.Invoke(this, txt);
            }

            private string getExclude(mode m)
            {
                switch (m)
                {
                    case mode.kanji:
                        return string.Join(" ", new List<string> { word.hiragana, word.hn, word.vn });
                        //return word.kanji;
                    case mode.hiragana:
                        return string.Join(" ", new List<string> { word.kanji, word.hn, word.vn });
                        //return word.hiragana;
                    case mode.hn:
                        return string.Join(" ", new List<string> { word.kanji, word.hiragana, word.vn });
                    //return word.hn;
                    case mode.vn:
                        return string.Join(" ", new List<string> { word.kanji, word.hiragana, word.hn });
                        //return word.vn;
                }
                return "";
            }
            public string term {
                get {
                    return getDefine(m_option.termMode);
                }
            }
            public void getSpeakInfo(itemStatus status, out string txt, out string key)
            {
                mode m = status == itemStatus.term ? m_option.termMode : m_option.defineMode;
                switch (m)
                {
                    case mode.kanji:
                        txt = word.kanji;
                        key = "ja-JP";
                        break;
                    case mode.hiragana:
                        txt = word.hiragana;
                        key = "ja-JP";
                        break;
                    case mode.hn:
                        txt = word.hn;
                        key = "vi-VN";
                        break;
                    default:
                        txt = word.vn;
                        key = "vi-VN";
                        break;
                }
            }

            public string define { get { return getDefine(m_option.defineMode); } }
            public string detail { get { return getExclude(m_option.defineMode); } }
            public void rotate()
            {
                switch(status)
                {
                    case itemStatus.define:
                        status = itemStatus.term;
                        break;
                    case itemStatus.term:
                        status = itemStatus.define;
                        break;
                }
            }
        }

        private void OptionBtn_Click(object sender, RoutedEventArgs e)
        {
            //split.IsPaneOpen = !split.IsPaneOpen;
        }

#region loadData
        private void loadData()
        {
#if false
            loadProgress.Visibility = Visibility.Visible;
            loadProgress.Maximum = 100;

            m_worker.DoWork += bg_loadData;
            m_worker.ProgressChanged += M_worker_ProgressChanged;
            m_worker.WorkerReportsProgress = true;
            m_worker.RunWorkerCompleted += M_worker_RunWorkerCompleted;
            m_worker.RunWorkerAsync();
#endif
            //qryBgTask(new myBgTask {type = myBgTask.taskType.loadData });
            //qryBgTask(new myBgTask { type = myBgTask.taskType.loadDataComplete });
            Debug.Assert(s_cp.m_lessonPgCfg.selectedLessons.Count > 0);
            var lname = s_cp.m_lessonPgCfg.selectedLessons[0];
            var lesson = s_cp.m_lessons[lname];
            setRtb(lesson.sentences);

            initEvents();
        }

        private void setRtb(List<string> sentences)
        {
            Paragraph p = new Paragraph();
            for (int i = 0; i < sentences.Count; i++)
            {
                var line = sentences[i];
                var spn = new Span();
                var buff = new char[line.Length];
                int len = 0;
                foreach (char ch in line)
                {
                    if (dict.IsKanji(ch))
                    {
                        Hyperlink hb = crtBlck(ch);
                        if (len > 0)
                        {
                            var tmp = new string(buff, 0, len);
                            spn.Inlines.Add(new Run { Text = tmp });
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
                p.Inlines.Add(spn);
            }
            rtb.Blocks.Clear();
            rtb.Blocks.Add(p);
        }

        private void updateGUI(object sender, RunWorkerCompletedEventArgs e)
        {
            //update option panel
            //+ show marked
            if (m_markedItems.Count == 0)
            {
                m_option.showMarked = false;
            }

            //+ search
            //optSrchEnableChk.IsChecked = m_option.srchEnable;
            //optSrchTxtEnableChk.IsChecked = m_option.srchTxtEnable;

            //setup EventHandler
            //+ register GetKanji event before updateTerm()?
            //  =>necessary to involked event when updateTerm
            initEvents();

            //update term
            //m_iCursor = 0;


            loadProgress.Visibility = Visibility.Collapsed;

            //update status
            updateStatus(string.Format("Marked/Total: {0}/{1}", m_markedItems.Count, m_items.Count));

            //selection text
            //srchRtb.IsTextSelectionEnabled = m_option.selectTxtOn;
        }

        private void initEvents()
        {

            backBtn.Click += back_Click;

            //canvas buttons
#if start_use_checkbox
            starChk.Click += starChk_Checked;
#else
            //canvasStar.Tapped += starChk_Checked;
#endif

            //option ctrls
            //optionBtn.Click += OptionBtn_Click;
            //optWordTermDefineCmb.SelectionChanged += DefineCmb_SelectionChanged;
            //optWordTermCmb.SelectionChanged += TermCmb_SelectionChanged;

            //optWordDetailChk.Tapped += DetailChk_Tapped;
            //optWordStarChk.Click += OptStarChk_Click;

            //optSpkTermChk.Click += OptSpkTermChk_Click;
            //optSpkDefineChk.Click += OptSpkDefineChk_Click;
            //optFullDefChk.Click += OptFullDefChk_Click;
            //optVerbChk.Click += OptVerbChk_Click;
            //optSelectTxtOn.Click += OptSelectTxtOn_Click;
            //search option
            //optSrchEnableChk.Click += OptSrchEnableChk_Click;
            //optSrchTxtEnableChk.Click += OptSrchTxtEnableChk_Click;

            //split.PaneClosed += Split_PaneClosed;

#if item_editable
            canvasEdit.Tapped += CanvasEdit_Tapped;
            canvasAccept.Tapped += CanvasAccept_Tapped;
            canvasCancel.Tapped += CanvasCancel_Tapped;
#endif
            //speak
            //canvasSpeak.Tapped += CanvasSpeak_Tapped;

            //key
            //mainGrid.KeyDown += Study_KeyDown;
            //split.KeyDown += Study_KeyDown;
            //KeyDown += Study_KeyDown;
            //flipBtn.Click += term_Tapped;

            //search & dict
            //searchBnt2.Tapped += srchBtn_Click;                //search or show search panel
            //searchBnt2.DoubleTapped += SrchBtn_DoubleTapped;   //hide search panel
            myNode.regOnHyberlinkClick("readModule", Hb_Click);
            //searchBnt2.Click += SearchBnt2_Click;

            //srchTxt.KeyUp += SrchTxt_KeyUp;

            //set search event
            //turnSearchOnOff(m_option.srchEnable);

            //next to full dict
            dictBtn.Click += DictBtn_Click;

            //menu
            pasteBtn.Click += PasteBtn_Click;
        }

        private void PasteBtn_Click(object sender, RoutedEventArgs e)
        {
            var data = Clipboard.GetContent();
            string txt = "";
            var t = Task.Run(async () => { txt = await data.GetTextAsync(); });
            t.Wait();
            setRtb(new List<string> { txt });
        }

        private void DictBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchBnt2_Click(object sender, RoutedEventArgs e)
        {
            m_option.srchEnable = !m_option.srchEnable;
            //optSrchEnableChk.IsChecked = m_option.srchEnable;
            turnSearchOnOff(m_option.srchEnable);

        }

        private void turnSearchOnOff(bool enable)
        {
            if (GetKanji != null)
            {
                foreach (Delegate d in GetKanji.GetInvocationList())
                {
                    GetKanji -= (EventHandler<string>)d;
                }
            }
            if (enable)
            {
            }
            else
            {
            }
        }

        private void SrchTxt_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == VirtualKey.Enter) {
                this.Focus(FocusState.Programmatic);
                srchBtn_Click(this, e);
            }
        }

        //private void M_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    loadProgress.Value = e.ProgressPercentage;
        //    Debug.WriteLine("{0} M_worker_ProgressChanged loadedChapter {1} ", this, e.ProgressPercentage);
        //}

        private void bg_loadData(object sender, DoWorkEventArgs e)
        {
            //load option
            //m_worker.ReportProgress(20);
            bg_qryFgTask(myFgTask.qryType.loadProgress, 20);
            Debug.WriteLine("{0} call load option {1}", this, Environment.TickCount);
            //m_option = studyOption.getInstance();
            Debug.WriteLine("{0} load option return {1}", this, Environment.TickCount);
            //m_worker.ReportProgress(50);
            bg_qryFgTask(myFgTask.qryType.loadProgress, 50);

            //load marked words
            m_markedItems.Clear();
            m_items.Clear();
#if !test_study_page
            int loadedChapter = 0;
            int totalChaptes = s_cp.m_chapterPgCfg.selectedChapters.Count;
            foreach (var key in s_cp.m_chapterPgCfg.selectedChapters)
            {
                var chapter = s_cp.m_chapters[key];
                {
                    s_cp.m_db.getMarked(chapter);
                    var words = chapter.words;
                    int i = 0; bool marked;
                    foreach (var w in words)
                    {
                        //indexs maybe sliped if chapter file is modified
                        marked = chapter.markedIndexs.Contains(i);
                        var item = new wordItem()
                        {
                            word = w,
                            status = itemStatus.term,
                            c = chapter,
                            index = i++,
                            marked = marked
                        };
                        m_items.Add(item);
                        if (item.marked) { m_markedItems.Add(item); }
                    }
                }
                loadedChapter++;
                //m_worker.ReportProgress(50 + (loadedChapter * 50 / totalChaptes));
                bg_qryFgTask(myFgTask.qryType.loadProgress, 50 + (loadedChapter * 50 / totalChaptes));
                Debug.WriteLine("{0} M_worker_DoWork loadedChapter {1} ", this, loadedChapter);
            }
#else
            chapter ch = new chapter() { path = @"C:\Users\Khiem\Documents\kotoba\chapter_1_1.txt",
                name = "chapter_1_1"};

            string txt = "言葉 ことば (NGÔN DIỆP) Câu nói \n"
                + "積極 せいこう (THÀNH CÔNG) \n"
                + "心配 (TÂM PHỐI) \n";
            var words = s_cp.parse(txt);
            var t = Task.Run(()=> s_cp.getMarked(ch));
            t.Wait();
            int i = 0; bool marked;
            foreach (var w in words) {
                marked = ch.markedIndexs.Contains(i);
                m_items.Add(new wordItem() { word = w, status = itemStatus.term,
                    c = ch, index = i++, marked = marked });
            }
#endif
            bg_qryFgTask(myFgTask.qryType.updateGUI, null);
        }

#endregion

        private List<wordItem> getCurItems()
        {
            return m_option.showMarked ? m_markedItems : m_items;
        }

        private void detail_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void sulfBnt_Click(object sender, RoutedEventArgs e)
        {
            var items = getCurItems();

            Random rng = new Random();
            int count = items.Count;
            for (int i = 0; i<count;i++)
            {
                int irand = rng.Next(count - 1);
                var temp = items[i];
                items[i] = items[irand];
                items[irand] = temp;
            }
            m_iCursor = 0;
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(lessonPg));
        }

        class myChkBox
        {
            public bool IsChecked;
        };
        static myChkBox starChk = new myChkBox();

#if !transparent_canvas
        BitmapImage starBM = new BitmapImage(new Uri("ms-appx:///Assets/star_m.png"));
        BitmapImage starBM2 = new BitmapImage(new Uri("ms-appx:///Assets/star_u.png"));
#endif

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Debug.WriteLine(string.Format("Dispose true {0}", m_srchWorker.IsBusy));
                    m_srchWorker.Dispose();
                    foreach (var s in synthDic.Values)
                    {
                        s.Dispose();
                    }
                    synthDic.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~study() {
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

        myDictCtrl dictCtrl;
        private void dictCtrl_Loaded(object sender, RoutedEventArgs e)
        {
            dictCtrl = (myDictCtrl)sender;
        }
    }
}
