//#define test_study_page
//#define init_status
#define item_editable
//#define start_use_checkbox
#define once_synth

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechSynthesis;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace test_universalApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class study : Page
    {
        static contentProvider s_cp = contentProvider.getInstance();

        public study()
        {
            this.InitializeComponent();

            //option panel
            #region option_ctrls
            optionBtn.Click += OptionBtn_Click;

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

            optWordTermDefineCmb.SelectionChanged += DefineCmb_SelectionChanged;
            optWordTermCmb.SelectionChanged += TermCmb_SelectionChanged;

            optWordDetailChk.Tapped += DetailChk_Tapped;
            optWordStarChk.Click += OptStarChk_Click;

            optSpkTermChk.Click += OptSpkTermChk_Click;
            optSpkDefineChk.Click += OptSpkDefineChk_Click;
            #endregion
            split.PaneClosed += Split_PaneClosed;

            //canvas buttons
#if start_use_checkbox
            starChk.Click += starChk_Checked;
#else
            canvasStar.Tapped += starChk_Checked;
#endif

#if item_editable
            canvasEdit.Tapped += CanvasEdit_Tapped;
            canvasAccept.Tapped += CanvasAccept_Tapped;
            canvasCancel.Tapped += CanvasCancel_Tapped;

            editTxt.AcceptsReturn = true;
            editTxt.TextWrapping = TextWrapping.Wrap;
            //editTxt.Header = "Editing word (please use \";\" as separator)";
            editTxt.PlaceholderText = "Using \";\" as separator";
            ScrollViewer.SetVerticalScrollBarVisibility(editTxt, ScrollBarVisibility.Auto);
#endif

            canvasSpeak.Tapped += CanvasSpeak_Tapped;

            //register play end event
            media.MediaOpened += Media_MediaOpened;
            media.MediaEnded += Media_MediaEnded;
            media.MediaFailed += Media_MediaFailed;

            //decorate
            initCtrls();
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

        Dictionary<string, VoiceInformation> voiceDict = new Dictionary<string, VoiceInformation>();
        bool getVoice(out VoiceInformation voice, string lang)
        {
            voice = null;
            if (voiceDict.ContainsKey(lang)) {
                voice = voiceDict[lang];
            }
            else {
                foreach(var v in SpeechSynthesizer.AllVoices)
                {
                    if (v.Language.Contains(lang))
                    {
                        voiceDict.Add(lang, v);
                        voice = v;
                        break;
                    }
                }
            }
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
        SpeechSynthesizer lastTTSsynth = new SpeechSynthesizer();
        SpeechSynthesisStream lastTTSstream;
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
                lastTTSsynth.Voice = voice;
                lastTTSstream = await lastTTSsynth.SynthesizeTextToStreamAsync(txt);
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

        List<UIElement> grp1;
        List<UIElement> grp2;
        private void initCtrls()
        {
            editEllipse.Stroke = new SolidColorBrush(Colors.White);
            cancelEllipse.Stroke = new SolidColorBrush(Colors.White);
            acceptEllipse.Stroke = new SolidColorBrush(Colors.White);
            starEllipse.Stroke = new SolidColorBrush(Colors.White);

            detailTxt.Text = "";
            termTxt.Text = "";

            grp1 = new List<UIElement>() { canvasEdit, termTxt, detailTxt,
                nextBtn, backBtn,
                bntStack,
                canvasSpeak, canvasStar};
            grp2 = new List<UIElement>() { canvasAccept, canvasCancel, editTxt };
        }

        private void CanvasCancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            finishEdit(false);
        }

        wordItem m_editingItem;
        void showHideCtrls(bool editMode)
        {
            var grp1V = editMode ? Visibility.Collapsed : Visibility.Visible;
            var grp2V = editMode ? Visibility.Visible : Visibility.Collapsed;
            foreach (var e in grp1) { e.Visibility = grp1V; }
            foreach (var e in grp2) { e.Visibility = grp2V; }
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

        class studyOption
        {
            public mode termMode;
            public mode defineMode;
            public bool showDetail;
            public bool showMarked;
            public bool spkTerm;
            public bool spkDefine;
        }

        static studyOption m_option = new studyOption() {
            termMode = mode.kanji, defineMode = mode.hiragana,
            showDetail = false,
        };

        class wordItem
        {
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
        List<wordItem> m_items = new List<wordItem>();
        List<wordItem> m_markedItems = new List<wordItem>();

        private void OptionBtn_Click(object sender, RoutedEventArgs e)
        {
            split.IsPaneOpen = !split.IsPaneOpen;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            loadData();
        }

        private void loadData()
        {
            m_markedItems.Clear();
            m_items.Clear();
#if !test_study_page
            foreach ( var chapter in s_cp.m_chapters.Values)
            {
                if (chapter.selected) { 
                    var words = chapter.words;
                    foreach(var w in words)
                    {
                        var item = new wordItem() {c = chapter, word = w, status = itemStatus.term };
                        m_items.Add(item);
                        if (item.marked) { m_markedItems.Add(item); }
                    }
                }
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

            //update option panel
            //+ show marked
            if (m_markedItems.Count == 0) {
                m_option.showMarked = false;
            }
            optWordStarChk.IsChecked = m_option.showMarked;
            //+ show detail
            optWordDetailChk.IsChecked = m_option.showDetail;

            m_iCursor = 0;
            updateTerm();
            updateNum();
        }

        private void updateTerm()
        {
            updateTerm(true);
        }
        private void updateTerm(bool reqInit)
        {
            //not change term while speak
            Debug.Assert(m_speakStat == speakStatus.end);

            var items = getCurItems();
            Debug.Assert(m_iCursor >= 0 && m_iCursor < items.Count);
            wordItem curItem = items[m_iCursor];
            curItem.status = reqInit? itemStatus.term: curItem.status;

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

        int m_iCursor;

        private void detail_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
        private void term_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var items = getCurItems();
            //rotate
            Debug.Assert(m_iCursor >= 0 && m_iCursor < items.Count);
            wordItem curItem = items[m_iCursor];
            curItem.rotate();
            updateTerm(false);
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

            i.marked = (bool)starChk.IsChecked;

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
            s_cp.updateMarked(i.c);

            if (m_option.showMarked) {
                if (m_markedItems.Count == 0)
                {
                    m_option.showMarked = false;
                    optWordStarChk.IsChecked = false;
                    m_iCursor = 0;
                    //updateTerm();
                    //updateNum();
                }
                else if (m_iCursor == m_markedItems.Count) {
                    m_iCursor--;
                }
                Debug.Assert(m_iCursor >= 0);
#if init_status
                items[m_iCursor].status = itemStatus.term;
#endif
                updateTerm();
                updateNum();
            }
        }

        private void term_swiped(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            //not in editing state
            if (m_editingItem == null)
            {
                if (e.Cumulative.Translation.X < 0)
                {
                    //move right
                    nextBtn_Click(sender, e);
                }
                else
                {
                    //move left
                    prevBtn_Click(sender, e);
                }
            }
        }
    }
}
