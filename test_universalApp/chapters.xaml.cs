﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
    public sealed partial class chapters : Page
    {
        static contentProvider s_cp = contentProvider.getInstance();
        myChapterPgCfg m_config;
        ObservableCollection<chapterItem> m_data;

        class chapterItem
        {
            public string Name { get; set; }
            public string Path { get { return c.path; } }
            public int Count { get; set; }
            public double Percent { get { return (Count - nMarked) / (double)Count; } }
            private int nMarked { get { return c.markedIndexs.Count; } }
            public chapter c;

            public override string ToString() { return string.Format("{0} ({1})", Name, Count); }

            public string status { get { return string.Format("Total/Marked: {0}/{1}", Count, nMarked); } }
            public Brush color
            {
                get
                {
                    Color[] arr = {Colors.Red, Colors.Orange, Colors.Yellow,
                    Colors.Green, Colors.Cyan, Colors.DarkBlue, Colors.Violet };
                    int i = (Count - nMarked) * (arr.Length - 1) / Count;
                    //Debug.WriteLine("i color {0}", i);
                    SolidColorBrush br = new SolidColorBrush(arr[i]);
                    return br;
                }
            }
        }

        public chapters()
        {
            this.InitializeComponent();

            Loaded += Chapters_Loaded;
            Unloaded += Chapters_Unloaded;

            //fill chapter
            fillterTxt.TextChanged += FillterTxt_TextChanged; ;
            fillterTxt.QuerySubmitted += FillterTxt_QuerySubmitted;
            fillterTxt.SuggestionChosen += FillterTxt_SuggestionChosen;
            //fillterTxt.KeyDown += FillterTxt_KeyDown;

            checkAll.Tapped += CheckAll_Tapped;

            chapterList.SelectionChanged += ChapterList_SelectionChanged;

             m_data = new ObservableCollection<chapterItem>();
        }

        //private void FillterTxt_KeyDown(object sender, KeyRoutedEventArgs e)
        //{
        //    if (e.Key == VirtualKey.Enter)
        //    {
        //        Debug.WriteLine(string.Format("FillterTxt_KeyDown enter"));
        //    }
        //}

        void updateStatus(string txt)
        {
            statusBar.Text = txt;
            Debug.WriteLine(string.Format("[status] {0}", txt));
        }

        private void ChapterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count >0) {
                chapterList.ScrollIntoView(e.AddedItems[0]);
            }
            updateStatus(string.Format("Selected {0} chapters!", chapterList.SelectedItems.Count));
        }

        private void CheckAll_Tapped(object sender, TappedRoutedEventArgs e)
        {
            chapterList.SelectedItems.Clear();
        }

        private void FillterTxt_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var ch = (chapterItem) args.SelectedItem;
            sender.Text = ch.Name;
        }

        private void FillterTxt_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                // User selected an item, take an action on it here
                chapterList.SelectedItems.Add(args.ChosenSuggestion);
            }
            else
            {
                // Do a fuzzy search on the query text
                var chs = searchCh(args.QueryText);
                chapterList.SelectedItems.Clear();
                foreach (var ch in chs) { chapterList.SelectedItems.Add(ch); }
                // Choose the first match, or clear the selection if there are no matches.
                //SelectContact(matchingContacts.FirstOrDefault());
                //this.Focus(FocusState.Programmatic);
            }
        }

        //Regex preReg = new Regex("*");
#if use_reg
        List<chapterItem> preMatching = new List<chapterItem>();
        List<chapterItem> searchCh(string txt)
        {
            try {
                Regex reg = new Regex(txt, RegexOptions.IgnoreCase);
                preMatching.Clear();
                foreach (chapterItem ch in chapterList.Items)
                {
                    if (reg.IsMatch(ch.name))
                    {
                        preMatching.Add(ch);
                    }
                }
                return preMatching;
            } catch {
                return preMatching;
            }
        }
#else
        List<chapterItem> searchCh(string txt)
        {
            var m = new List<chapterItem>();
            foreach (chapterItem ch in chapterList.Items)
            {
                if (ch.Name.Contains(txt))
                {
                    m.Add(ch);
                }
            }
            return m;
        }
#endif

        private void FillterTxt_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                sender.ItemsSource = searchCh(sender.Text);
            }
        }

        private void Chapters_Loaded(object sender, RoutedEventArgs e)
        {
            loadData();

            if (chapterList.SelectedItems.Count > 0) { 
                //chapterList.ScrollIntoView(chapterList.SelectedItems[0]);
            }
        }

        private void Chapters_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("{0} selectd {1}", this, chapterList.SelectedItems.Count);
        }

        private void loadData()
        {
            m_config = myChapterPgCfg.getInstance();

            //load chapter data
            chapterList.Items.Clear();
            int iCur = 0;
            chapterList.ItemsSource = m_data;
            foreach (var i in s_cp.m_chapters)
            {
                var chapter = i.Value;
                var item = new chapterItem()
                {
                    Name = chapter.name,
                    Count = chapter.words.Count,
                    c = chapter
                };
                s_cp.m_db.getMarked(chapter);
                if (chapter.words.Count < chapter.markedIndexs.Count)
                {
                    //db malform
                    Debug.WriteLine("[error] {0} loadData chapter {1} marked data malform", this, chapter.path);
                    chapter.markedIndexs.Clear();
                }

                m_data.Add(item);

                //check selected chapter
                if (m_config.selectedChapters.Contains(chapter.path))
                {
                    m_config.selectedChapters.Remove(chapter.path);
                    chapterList.SelectedItems.Add(item);
                    chapter.selected = false;
                }
                iCur++;
            }

            //load mydb file
            //s_cp.loadDb();
        }

        async void showError()
        {
            MessageDialog msgbox = new MessageDialog("No selected chapter");
            msgbox.Title = "Study selected chapter error!";
            await msgbox.ShowAsync();
        }
        private void StudyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (chapterList.SelectedItems.Count > 0)
            {
                foreach (chapterItem i in chapterList.SelectedItems)
                {
                    Debug.WriteLine("{0} {1}", i.Name, i.Count);
                    i.c.selected = true;
                    m_config.selectedChapters.Add(i.c.path);
                }

                //save selected chaptes
                m_config.save();

                //load chater marked info to cache
                s_cp.m_db.loadMarkeds(m_config.selectedChapters);

                this.Frame.Navigate(typeof(study));
            }
            else
            {
                showError();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            foreach (chapterItem i in chapterList.SelectedItems)
            {
                Debug.WriteLine("{0} {1}", i.Name, i.Count);
                i.c.selected = true;
                m_config.selectedChapters.Add(i.c.path);
            }
            m_config.save();
            this.Frame.Navigate(typeof(MainPage));
        }

    }
}
