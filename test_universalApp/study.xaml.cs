//#define test_study_page
//#define init_status
#define item_editable
//#define start_use_checkbox
#define once_synth
#define reduce_disk_opp

#define dict_dist
#define sepate_kanji

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using Windows.Data.Xml.Dom;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
using Windows.UI.Popups;
using System.Reflection;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace test_universalApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class study : Page, IDisposable
    {
        static contentProvider s_cp = contentProvider.getInstance();
        List<UIElement> m_grp1;
        List<UIElement> m_grp2;
        wordItem m_editingItem;
        static studyOption m_option;
        static public event EventHandler<string> GetKanji;
        List<wordItem> m_items = new List<wordItem>();
        List<wordItem> m_markedItems = new List<wordItem>();
        BackgroundWorker m_worker = new BackgroundWorker();
        int m_iCursor;

        //singleton dict
        myDict dict = myDict.Load();

        public study()
        {
            this.InitializeComponent();

            //register play end event
            media.MediaOpened += Media_MediaOpened;
            media.MediaEnded += Media_MediaEnded;
            media.MediaFailed += Media_MediaFailed;

            //decorate
            initCtrls();

            Loaded += Study_Loaded;
            Unloaded += Study_Unloaded;
        }

        private void Study_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach(var s in synthDic.Values)
            {
                s.Dispose();
            }
            synthDic.Clear();
            if (lastTTSstream!= null) lastTTSstream.Dispose();

            m_grp1.Clear();
            m_grp2.Clear();
        }

        private void Study_Loaded(object sender, RoutedEventArgs e)
        {
            synthDic = new Dictionary<string, SpeechSynthesizer>();

            m_grp1 = new List<UIElement>() { canvasEdit, termTxt, detailTxt,
                nextBtn, backBtn,
                bntStack,
                canvasSpeak, canvasStar};
            m_grp2 = new List<UIElement>() { canvasAccept, canvasCancel, editTxt };

            loadData();
        }

        private void OptSpkDefineChk_Click(object sender, RoutedEventArgs e)
        {
            m_option.spkDefine = (bool)optSpkDefineChk.IsChecked;
        }

        private void OptSpkTermChk_Click(object sender, RoutedEventArgs e)
        {
            m_option.spkTerm = (bool)optSpkTermChk.IsChecked;
        }

        BitmapImage speakBM = new BitmapImage(new Uri("ms-appx:///Assets/speak.png"));
        BitmapImage speakBM2 = new BitmapImage(new Uri("ms-appx:///Assets/speak2.png"));
        private void Media_MediaOpened(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("{0} Media_MediaOpened play start", this);
            m_speakStat = speakStatus.engine_played;
            speakImage.Source = speakBM2;
        }

        void speakTxtEnd(bool isSucess)
        {
            speakImage.Source = speakBM;
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
            if (m_speakStat != speakStatus.end)
            {
                Debug.WriteLine("{0} speakTxt m_speakStat {1}", this, m_speakStat.ToString());
                return;
            }
            m_speakStat = speakStatus.begin;

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
            speakTxt(txt, lang);
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

            VoiceInformation voice;
            ret = getVoice(out voice, lang);
            if (!ret) return null;

            var s = new SpeechSynthesizer {Voice = voice };
            synthDic.Add(lang, s);
            return s;
        }
#endif

        async void speakTxt(string txt, string lang)
        {
            Debug.WriteLine("{0} speakTxt call play", this);
            //txt = "hello world";
            //IEnumerable<VoiceInformation> frenchVoices = from voice in SpeechSynthesizer.AllVoices
            //                                             where voice.Language == "fr-FR"
            //                                             select voice;
            VoiceInformation voice;
            bool ret = getVoice(out voice, lang);
            if (ret)
            {
                m_speakStat = speakStatus.called;
#if !once_synth
                lastTTSsynth = new SpeechSynthesizer();
#endif
                var synth = getSpeechSynth(lang);
                lastTTSstream = await synth.SynthesizeTextToStreamAsync(txt);
                // The media object for controlling and playing audio.
                MediaElement mediaElement = media;
                //MediaElement mediaElement = new MediaElement();
                mediaElement.SetSource(lastTTSstream, lastTTSstream.ContentType);
                Debug.WriteLine("  + call play()");
                mediaElement.Play();
                Debug.WriteLine("  + play() return");
            }
            else
            {
                MessageDialog msgbox = new MessageDialog(
                    string.Format("Not found {0} voice infomation. " +
                    "Maybe {0} voice recognition was not installed!", lang));
                msgbox.Title = "Speak word error!";
                await msgbox.ShowAsync();
                m_speakStat = speakStatus.end;
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
#region option_ctrls
            optWordTermCmb.Items.Add("kanji");
            optWordTermCmb.Items.Add("hiragana");
            optWordTermCmb.Items.Add("hán nôm");
            optWordTermCmb.Items.Add("vietnamese");
            optWordTermCmb.SelectedIndex = 0;

            optWordTermDefineCmb.Items.Add("kanji");
            optWordTermDefineCmb.Items.Add("hiragana");
            optWordTermDefineCmb.Items.Add("hán nôm");
            optWordTermDefineCmb.Items.Add("vietnamese");
            optWordTermDefineCmb.SelectedIndex = 1;
#endregion

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

            detailTxt.Text = "";
            termTxt.Text = "";
            numberTxt.Text = "";

            termTxt.IsTextSelectionEnabled = true;

            rtb.Blocks.Clear();
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
        const int m_limitContentLen = 3;
        const int m_limitContentCnt = 7;

        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            string txt = srchTxt.Text;
            search(txt);
        }

        private void Hb_Click(object sender, RoutedEventArgs e)
        {
            //
            var hb = (Hyperlink)sender;
            string text = ((Run)hb.Inlines[0]).Text;
            //text = hb.AccessKey;
            search(text);
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

            var ret = dict.Search(txt);
            rtb.Blocks.Clear();
            List<myWord> words = new List<myWord>();
            Span s = new Span();
            foreach (var kanji in ret)
            {
                Hyperlink hb = crtBlck(kanji.val);
                s.Inlines.Add(hb);
                var foundKanji = kanji.relatedWords.Find((w) => { return w.term == kanji.val.ToString(); });
                if (foundKanji != null)
                {
                    s.Inlines.Add(crtDefBlck(foundKanji.definitions[0]));
                }
                else
                {
                    s.Inlines.Add(new Run { Text = string.Format("({0}) stroke {1}, radical ", kanji.hn, kanji.totalStrokes) });
                    hb = crtBlck(kanji.radical.zRadical);
                    s.Inlines.Add(hb);
                    //s.Inlines.Add(new Run { Text = string.Format("({0}) ", kanji.radical.iRadical) });
                    var rdInfo = dict.Search(kanji.radical.zRadical.ToString());
                    if (rdInfo.Count > 0)
                    {
                        var k = rdInfo[0];
                        if (k.hn != "") s.Inlines.Add(new Run { Text = string.Format("({0})", k.hn) });
                        if (k.simple != '\0') s.Inlines.Add(new Run { Text = string.Format(" simple {0}", k.simple) });
                    }
                    s.Inlines.Add(new Run { Text = ", meaning: " });
                    foreach (var def in kanji.definitions)
                    {
                        s.Inlines.Add(crtDefBlck(def));
                        break;
                    }
                    s.Inlines.Add(new LineBreak());
                }
                words.AddRange(kanji.relatedWords);
                s.Inlines.Add(new LineBreak());
            }
            s.Inlines.Add(new LineBreak());
            //found word
            myWord found = null;
            if (ret.Count > 1) { found = words.Find((w) => { return w.term == txt; }); }
            var sFound = new Span();
            if (found != null)
            {
                s.Inlines.Add(crtWdBlck(found));
                s.Inlines.Add(new LineBreak());
                //remove from list
                words.Remove(found);
            }
            //related word
            if (found != null) s.Inlines.Add(sFound);
            //s.Inlines.Add(new Run { Text = "related word:" });
            //s.Inlines.Add(new LineBreak());
            int count = 0;
            foreach (var rWd in words)
            {
                if (txt.Contains(rWd.term)) continue;
#if !show_brift
                if ((count++) == m_limitContentCnt)
                    break;
#else
                {
                    s.Inlines.Add(crtWdBlck(rWd, true));
                }
                else
#endif
                {
                    s.Inlines.Add(crtWdBlck(rWd));
                }
                //s.Inlines.Add(new LineBreak());
                s.Inlines.Add(new LineBreak());
            }

            //create paragraph
            var p = new Paragraph();
            //if (found != null) p.Inlines.Add(sFound);
            p.Inlines.Add(s);
            rtb.Blocks.Add(p);
            //TextPointer pstart = rtb.ContentStart;
            //rtb.Select(pstart, pstart);
            //rtbScroll.VerticalScrollMode = ScrollMode.Enabled;
            //rtbScroll.BringIntoViewOnFocusChange = true;
            rtbScroll.ChangeView(0, 0, null);
            //rtbScroll.ScrollToVerticalOffset(0);
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
            var descr = crtDefBlck(rd.descr);
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

        private void CanvasCancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            finishEdit(false);
        }

        void showHideCtrls(bool editMode)
        {
            var grp1V = editMode ? Visibility.Collapsed : Visibility.Visible;
            var grp2V = editMode ? Visibility.Visible : Visibility.Collapsed;
            foreach (var e in m_grp1) { e.Visibility = grp1V; }
            foreach (var e in m_grp2) { e.Visibility = grp2V; }
        }
        void startEdit()
        {
            showHideCtrls(true);

            var items = getCurItems();
            m_editingItem = items[m_iCursor];
            editTxt.Text = m_editingItem.word.ToString();
        }
        void finishEdit(bool isAccept)
        {
            showHideCtrls(false);
            if (isAccept)
            {
                string txt = editTxt.Text;
                var w = new word(txt);
                if (!w.isEmpty)
                {
                    var item = m_editingItem;
                    int count = 0;

                    if (item.word.kanji.CompareTo(w.kanji) != 0)
                    {
                        item.word.kanji = w.kanji;
                        count++;
                    }
                    if (item.word.hiragana.CompareTo(w.hiragana)!=0)
                    {
                        item.word.hiragana = w.hiragana;
                        count++;
                    }
                    if (item.word.hn.CompareTo(w.hn) != 0)
                    {
                        item.word.hn = w.hn;
                        count++;
                    }
                    if (item.word.vn.CompareTo(w.vn) != 0)
                    {
                        item.word.vn = w.vn;
                        count++;
                    }

                    if (count > 0)
                    {
                        updateTerm();
                        s_cp.saveChapter(item.c);
                    }
                }
            }
            m_editingItem = null;
        }
        private void CanvasAccept_Tapped(object sender, TappedRoutedEventArgs e)
        {
            finishEdit(true);
        }
        private void CanvasEdit_Tapped(object sender, TappedRoutedEventArgs e)
        {
            startEdit();
        }

        private async void OptStarChk_Click(object sender, RoutedEventArgs e)
        {
            if (m_markedItems.Count > 0)
            {
                m_option.showMarked = (bool)optWordStarChk.IsChecked;
                m_iCursor = 0;
                updateTerm();
                updateNum();
            }
            else
            {
                MessageDialog msgbox = new MessageDialog("No marked word");
                msgbox.Title = "Show marked word error!";
                await msgbox.ShowAsync();
                optWordStarChk.IsChecked = false;
            }
        }

        private void Split_PaneClosed(SplitView sender, object args)
        {
            updateTerm();
            m_option.save();
        }

        private void DetailChk_Tapped(object sender, TappedRoutedEventArgs e)
        {
            m_option.showDetail = (bool)optWordDetailChk.IsChecked;
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
            public bool spkDefine;

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
            split.IsPaneOpen = !split.IsPaneOpen;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

#region loadData
        private void loadData()
        {
            loadProgress.Visibility = Visibility.Visible;
            loadProgress.Maximum = 100;

            m_worker.DoWork += M_worker_DoWork;
            m_worker.ProgressChanged += M_worker_ProgressChanged;
            m_worker.WorkerReportsProgress = true;
            m_worker.RunWorkerCompleted += M_worker_RunWorkerCompleted;
            m_worker.RunWorkerAsync();
        }

        private void M_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //update option panel
            //+ show marked
            if (m_markedItems.Count == 0)
            {
                m_option.showMarked = false;
            }
            optWordStarChk.IsChecked = m_option.showMarked;
            //+ show detail
            optWordDetailChk.IsChecked = m_option.showDetail;
            //+ cmb
            optWordTermCmb.SelectedIndex = (int)m_option.termMode;
            optWordTermDefineCmb.SelectedIndex = (int)m_option.defineMode;
            //+ speak
            optSpkDefineChk.IsChecked = m_option.spkDefine;
            optSpkTermChk.IsChecked = m_option.spkTerm;

            //setup EventHandler
            //+ register GetKanji event before updateTerm()?
            //  =>necessary to involked event when updateTerm
            initEvents();

            //update term
            m_iCursor = 0;
            updateTerm();
            updateNum();

            loadProgress.Visibility = Visibility.Collapsed;

            //update status
            updateStatus(string.Format("Marked/Total: {0}/{1}", m_markedItems.Count, m_items.Count));
        }

        private void initEvents()
        {
            termGrid.Tapped += term_Tapped;
            termGrid.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            termGrid.ManipulationCompleted += term_swiped;

            sulfBnt.Click += sulfBnt_Click;
            prevBtn.Click += prevBtn_Click;
            nextBtn.Click += nextBtn_Click;
            backBtn.Click += back_Click;

            //canvas buttons
#if start_use_checkbox
            starChk.Click += starChk_Checked;
#else
            canvasStar.Tapped += starChk_Checked;
#endif

            //option ctrls
            optionBtn.Click += OptionBtn_Click;
            optWordTermDefineCmb.SelectionChanged += DefineCmb_SelectionChanged;
            optWordTermCmb.SelectionChanged += TermCmb_SelectionChanged;

            optWordDetailChk.Tapped += DetailChk_Tapped;
            optWordStarChk.Click += OptStarChk_Click;

            optSpkTermChk.Click += OptSpkTermChk_Click;
            optSpkDefineChk.Click += OptSpkDefineChk_Click;

            split.PaneClosed += Split_PaneClosed;

#if item_editable
            canvasEdit.Tapped += CanvasEdit_Tapped;
            canvasAccept.Tapped += CanvasAccept_Tapped;
            canvasCancel.Tapped += CanvasCancel_Tapped;
#endif
            //speak
            canvasSpeak.Tapped += CanvasSpeak_Tapped;

            //key
            //mainGrid.KeyDown += Study_KeyDown;
            //split.KeyDown += Study_KeyDown;
            //KeyDown += Study_KeyDown;
            flipBtn.Click += term_Tapped;

            //search & dict
            srchBtn.Click += searchBtn_Click;
            myNode.OnHyberlinkClick += Hb_Click;

            srchTxt.KeyDown += SrchTxt_KeyDown;

            //set search event
            if (GetKanji != null) {
                foreach (Delegate d in GetKanji.GetInvocationList()) {
                    GetKanji -= (EventHandler<string>)d;
                }
            }
            GetKanji += Study_GetKanji;
        }

        private void SrchTxt_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == VirtualKey.Enter) { searchBtn_Click(this, e); }
        }

        void onKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch( e.Key)
            {
                case VirtualKey.Right:
                    nextBtn_Click(null, null);
                    break;
                case VirtualKey.Left:
                    prevBtn_Click(null, null);
                    break;
                case VirtualKey.S:
                    starChk_Checked(null, null);
                    break;
                case VirtualKey.Up:
                case VirtualKey.Down:
                    term_Tapped(null, null);
                    break;
            }
        }

        private void M_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            loadProgress.Value = e.ProgressPercentage;
            Debug.WriteLine("{0} M_worker_ProgressChanged loadedChapter {1} ", this, e.ProgressPercentage);
        }

        private void M_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //load option
            m_worker.ReportProgress(20);
            Debug.WriteLine("{0} call load option {1}", this, Environment.TickCount);
            m_option = studyOption.getInstance();
            Debug.WriteLine("{0} load option return {1}", this, Environment.TickCount);
            m_worker.ReportProgress(50);

            //load marked words
            m_markedItems.Clear();
            m_items.Clear();
#if !test_study_page
            int loadedChapter = 0;
            int totalChaptes = s_cp.m_config.selectedChapters.Count;
            foreach (var key in s_cp.m_config.selectedChapters)
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
                m_worker.ReportProgress(50 + (loadedChapter * 50 / totalChaptes));
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
        }

        private void updateTerm()
        {
            updateTerm(false);
        }
#endregion

        private void updateTerm(bool isTab)
        {
            //not change term while speak
            //Debug.Assert(m_speakStat == speakStatus.end);
            Debug.WriteLine("{0} updateTerm m_speakStat {1}", this, m_speakStat);

            var items = getCurItems();
            Debug.Assert(m_iCursor >= 0 && m_iCursor < items.Count);
            wordItem curItem = items[m_iCursor];
            curItem.status = isTab? curItem.status : itemStatus.term;

            switch (curItem.status)
            {
                case itemStatus.term:
                    termTxt.Text = curItem.term;
                    termTxt.Foreground = new SolidColorBrush() { Color = Colors.Blue };
                    detailTxt.Visibility = Visibility.Collapsed;
                    if (m_option.spkTerm) { speakTxt(); }
                    break;
                case itemStatus.define:
                    termTxt.Text = curItem.define;
                    termTxt.Foreground = new SolidColorBrush() { Color = Colors.Green };
                    if (m_option.showDetail)
                    {
                        detailTxt.Text = curItem.detail;
                        detailTxt.Visibility = Visibility.Visible;
                    }
                    if (m_option.spkDefine) { speakTxt(); }
                    break;
            }
            starChk.IsChecked = curItem.marked;
#if !start_use_checkbox
            updateStarCanvas();
#endif
        }

        private List<wordItem> getCurItems()
        {
            return m_option.showMarked ? m_markedItems : m_items;
        }

        private void detail_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
        private void term_Tapped(object sender, RoutedEventArgs e)
        {
            var items = getCurItems();
            //rotate
            Debug.Assert(m_iCursor >= 0 && m_iCursor < items.Count);
            wordItem curItem = items[m_iCursor];
            curItem.rotate();
            updateTerm(true);
        }

        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            var items = getCurItems();
            if (m_iCursor < (items.Count - 1))
            {
                m_iCursor++;
#if init_status
                items[m_iCursor].status = itemStatus.term;
#endif
                updateTerm();
                updateNum();
            }
        }

        private void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (m_iCursor > 0)
            {
                m_iCursor--;
#if init_status
                var items = getCurItems();
                items[m_iCursor].status = itemStatus.term;
#endif
                updateTerm();
                updateNum();
            }
        }

        private void updateNum()
        {
            var items = getCurItems();
            int count = items.Count;
            numberTxt.Text = string.Format("{0}/{1}", m_iCursor + 1, count);
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
            updateTerm();
            updateNum();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
#if reduce_disk_opp
            s_cp.saveMarkeds();
#endif
            this.Frame.Navigate(typeof(chapters));
        }

        class myChkBox
        {
            public bool IsChecked;
        };
        static myChkBox starChk = new myChkBox();

        public void updateStarCanvas()
        {
            if (starChk.IsChecked)
            {
                starEllipse.Fill = new SolidColorBrush() { Color = Colors.Yellow };
                //starEllipse.Stroke = new SolidColorBrush() { Color = Colors.White };
                starPolyline.Stroke = new SolidColorBrush() { Color = Colors.Black };
            }
            else
            {
                //SolidColorBrush mySolidColorBrush = new SolidColorBrush();
                //mySolidColorBrush.Color = Color.FromArgb(255, 0, 255, 0);
                // Describes the brush's color using RGB values. 
                // Each value has a range of 0-255.

                starEllipse.Fill = new SolidColorBrush() { Color = Colors.Silver };
                //starEllipse.Stroke = new SolidColorBrush() { Color = Colors.White };
                starPolyline.Stroke = new SolidColorBrush() { Color = Colors.White };
            }
        }

        private void starChk_Checked(object sender, RoutedEventArgs e)
        {
#if !start_use_checkbox
            starChk.IsChecked = !starChk.IsChecked;
            updateStarCanvas();
#endif
            var items = getCurItems();
            var i = items[m_iCursor];

            i.marked = starChk.IsChecked;

            if (i.marked)
            {
                m_markedItems.Add(i);
                i.c.markedIndexs.Add(i.index);
            }
            else
            {
                m_markedItems.Remove(i);
                i.c.markedIndexs.Remove(i.index);
            }

#if !reduce_disk_opp
            Debug.WriteLine("{0} call updateMarked start {1}", this, Environment.TickCount);
            s_cp.m_db.saveMarked(i.c);
            Debug.WriteLine("{0} call updateMarked end {1}", this, Environment.TickCount);
#endif

            if (m_option.showMarked)
            {
                if (m_markedItems.Count == 0)
                {
                    m_option.showMarked = false;
                    optWordStarChk.IsChecked = false;
                    m_iCursor = 0;
                    //updateTerm();
                    //updateNum();
                }
                else if (m_iCursor == m_markedItems.Count)
                {
                    m_iCursor--;
                }
                Debug.Assert(m_iCursor >= 0);
#if init_status
                items[m_iCursor].status = itemStatus.term;
#endif
                updateTerm();
                updateNum();
            }

            //update status bar
            updateStatus(string.Format("Marked/Total: {0}/{1}", m_markedItems.Count, m_items.Count));
        }

        private void term_swiped(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            const int delta = 15;
            //not in editing state
            if (m_editingItem == null)
            {
                if (e.Cumulative.Translation.X < -delta)
                {
                    //move right
                    nextBtn_Click(sender, e);
                }
                else if (e.Cumulative.Translation.X > delta)
                {
                    //move left
                    prevBtn_Click(sender, e);
                }
                else
                {
                    term_Tapped(sender, null);
                }
            }
        }

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    m_worker.Dispose();
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
    }
}
