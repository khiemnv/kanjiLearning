#define test_study_page

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
            starChk.Click += starChk_Checked;

            split.PaneClosed += Split_PaneClosed;
        }

        private void OptStarChk_Click(object sender, RoutedEventArgs e)
        {
            m_option.showMarked = (bool)optStarChk.IsChecked;
            m_iCursor = 0;
            updateTerm();
            updateNum();
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
        class option
        {
            public mode termMode;
            public mode defineMode;
            public bool showDetail;
            public bool showMarked;
        }

        static option m_option = new option() {
            termMode = mode.kanji, defineMode = mode.hiragana,
            showDetail = false,
        };

        class wordItem
        {
            public word word;
            public itemStatus status;
            public bool marked;
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
            public string term {
                get {
                    return getDefine(m_option.termMode);
                }
            }
            public string define { get { return getDefine(m_option.defineMode); } }
            public string detail { get { return string.Join(" ", new List<string> { word.hn, word.vn }); } }
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
#if !test_study_page
            foreach( var i in s_cp.m_selectedChapters)
            {
                var chapterWords = s_cp.m_chapters[i];
                foreach(var w in chapterWords)
                {
                    m_items.Add(new wordItem() { word = w, status = itemStatus.term});
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
            numberTxt.Text = m_items.Count.ToString();

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
                    term.Text = curItem.term;
                    term.Foreground = new SolidColorBrush() { Color = Colors.Blue };
                    detail.Visibility = Visibility.Collapsed;
                    break;
                case itemStatus.define:
                    term.Text = curItem.define;
                    term.Foreground = new SolidColorBrush() { Color = Colors.Green };
                    if (m_option.showDetail)
                    {
                        detail.Text = curItem.detail;
                        detail.Visibility = Visibility.Visible;
                    }
                    break;
            }
            starChk.IsChecked = curItem.marked;
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
                updateTerm();
                updateNum();
            }
        }

        private void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (m_iCursor > 0)
            {
                m_iCursor--;
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

        private void starChk_Checked(object sender, RoutedEventArgs e)
        {
            wordItem i;
            if (m_option.showMarked)
            {
                i = m_markedItems[m_iCursor];
            }else {
                i = m_items[m_iCursor];
            }

            i.marked = (bool)starChk.IsChecked;
            if (i.marked)
            {
                m_markedItems.Add(i);
            }else
            {
                m_markedItems.Remove(i);
            }

            if (m_option.showMarked) {
                if (m_markedItems.Count == 0)
                {
                    m_option.showMarked = false;
                    optStarChk.IsChecked = false;
                    m_iCursor = 0;
                    updateTerm();
                    updateNum();
                }
                else if (m_iCursor == m_markedItems.Count) {
                    m_iCursor--;
                    Debug.Assert(m_iCursor >= 0);
                    updateTerm();
                    updateNum();
                }
            }
        }
    }
}
