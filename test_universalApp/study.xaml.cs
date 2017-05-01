//#define test_study_page
#define init_status
#define item_editable
//#define start_use_checkbox

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

            optionBtn.Click += OptionBtn_Click;

            termCmb.Items.Add("kanji");
            termCmb.Items.Add("hiragana");
            termCmb.Items.Add("hán nôm");
            termCmb.Items.Add("vietnamese");
            termCmb.SelectedIndex = 0;

            defineCmb.Items.Add("kanji");
            defineCmb.Items.Add("hiragana");
            defineCmb.Items.Add("hán nôm");
            defineCmb.Items.Add("vietnamese");
            defineCmb.SelectedIndex = 1;

            defineCmb.SelectionChanged += DefineCmb_SelectionChanged;
            termCmb.SelectionChanged += TermCmb_SelectionChanged;

            detailChk.Tapped += DetailChk_Tapped;
            optStarChk.Click += OptStarChk_Click;
#if start_use_checkbox
            starChk.Click += starChk_Checked;
#else
            starCanvas.Tapped += starChk_Checked;
#endif

            split.PaneClosed += Split_PaneClosed;

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

            initCtrls();
        }

        bool getJapaness(out VoiceInformation jp)
        {
            jp = SpeechSynthesizer.DefaultVoice;
            foreach (var v in SpeechSynthesizer.AllVoices)
            {
                if (v.Language.Contains("ja-JP"))
                {
                    jp = v;
                    return true;
                }
            }
            return false;
        }

        async void speakTxt(string txt)
        {
            //txt = "hello world";

            //IEnumerable<VoiceInformation> frenchVoices = from voice in SpeechSynthesizer.AllVoices
            //                                             where voice.Language == "fr-FR"
            //                                             select voice;
            VoiceInformation jp;
            bool ret = getJapaness(out jp);
            if (true)
            {
                SpeechSynthesizer synth = new SpeechSynthesizer();
                synth.Voice = (jp);
                SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(txt);
                // The media object for controlling and playing audio.
                MediaElement mediaElement = media;
                mediaElement.SetSource(stream, stream.ContentType);
                mediaElement.Play();
            }
            else
            {
                MessageDialog msgbox = new MessageDialog("Not found japanese void info");
                msgbox.Title = "Speak word error!";
                await msgbox.ShowAsync();
            }
        }

        private void CanvasSpeak_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //speak text
            string txt = termTxt.Text;
            speakTxt(txt);
        }

        private void initCtrls()
        {
            editEllipse.Stroke = new SolidColorBrush(Colors.White);
            cancelEllipse.Stroke = new SolidColorBrush(Colors.White);
            acceptEllipse.Stroke = new SolidColorBrush(Colors.White);
            starEllipse.Stroke = new SolidColorBrush(Colors.White);

            detailTxt.Text = "";
            termTxt.Text = "";
        }

        private void CanvasCancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            finishEdit(false);
        }

        wordItem m_editingItem;
        void startEdit()
        {
            canvasEdit.Visibility = Visibility.Collapsed;
            canvasAccept.Visibility = Visibility.Visible;
            canvasCancel.Visibility = Visibility.Visible;

            editTxt.Visibility = Visibility.Visible;
            termTxt.Visibility = Visibility.Collapsed;
            detailTxt.Visibility = Visibility.Collapsed;

            var items = getCurItems();
            m_editingItem = items[m_iCursor];
            editTxt.Text = m_editingItem.word.ToString();
        }
        void finishEdit(bool isAccept)
        {
            canvasEdit.Visibility = Visibility.Visible;
            canvasAccept.Visibility = Visibility.Collapsed;
            canvasCancel.Visibility = Visibility.Collapsed;

            editTxt.Visibility = Visibility.Collapsed;
            termTxt.Visibility = Visibility.Visible;
            detailTxt.Visibility = Visibility.Visible;
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
                m_option.showMarked = (bool)optStarChk.IsChecked;
                m_iCursor = 0;
                updateTerm();
                updateNum();
            }
            else
            {
                MessageDialog msgbox = new MessageDialog("No marked word");
                msgbox.Title = "Show marked word error!";
                await msgbox.ShowAsync();
                optStarChk.IsChecked = false;
            }
        }

        private void Split_PaneClosed(SplitView sender, object args)
        {
            updateTerm();
        }

        private void DetailChk_Tapped(object sender, TappedRoutedEventArgs e)
        {
            m_option.showDetail = (bool)detailChk.IsChecked;
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
        }

        static studyOption m_option = new studyOption() {
            termMode = mode.kanji, defineMode = mode.hiragana,
            showDetail = false,
        };

        class wordItem
        {
            public chapter c = null;
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
            string txt = "言葉 ことば (NGÔN DIỆP) Câu nói \n"
                + "積極 せいこう (THÀNH CÔNG) \n"
                + "心配 (TÂM PHỐI) \n";
            var words = s_cp.parse(txt);
            foreach(var w in words) {
                m_items.Add(new wordItem() { word = w, status = itemStatus.term });
            }
#endif

            //update option panel
            //+ show marked
            if (m_markedItems.Count == 0) {
                m_option.showMarked = false;
            }
            optStarChk.IsChecked = m_option.showMarked;
            //+ show detail
            detailChk.IsChecked = m_option.showDetail;

            m_iCursor = 0;
            updateTerm();
            updateNum();
        }

        private void updateTerm()
        {
            var items = getCurItems();
            Debug.Assert(m_iCursor >= 0 && m_iCursor < items.Count);
            wordItem curItem = items[m_iCursor];
            switch (curItem.status)
            {
                case itemStatus.term:
                    termTxt.Text = curItem.term;
                    termTxt.Foreground = new SolidColorBrush() { Color = Colors.Blue };
                    detailTxt.Visibility = Visibility.Collapsed;
                    break;
                case itemStatus.define:
                    termTxt.Text = curItem.define;
                    termTxt.Foreground = new SolidColorBrush() { Color = Colors.Green };
                    if (m_option.showDetail)
                    {
                        detailTxt.Text = curItem.detail;
                        detailTxt.Visibility = Visibility.Visible;
                    }
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
            updateTerm();
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
            }
            else
            {
                m_markedItems.Remove(i);
            }

            if (m_option.showMarked) {
                if (m_markedItems.Count == 0)
                {
                    m_option.showMarked = false;
                    optStarChk.IsChecked = false;
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
