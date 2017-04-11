using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        class chapterItem
        {
            public string name { get; set; }
            public int count { get; set; }
            public chapter c;
        }

        public chapters()
        {
            this.InitializeComponent();

            loadData();
        }

        private void loadData()
        {
            //load chapter data
            chapterList.Items.Clear();
            int iCur = 0;
            foreach (var i in s_cp.m_chapters)
            {
                var chapter = i.Value;
                Debug.Assert(i.Key == chapter.name);
                var item = new chapterItem() { name = chapter.name, count = chapter.words.Count, c = chapter };
                chapterList.Items.Add(item);
                //check selected chapter
                //var ret = s_cp.m_selectedChapters.Find(input => i.Key.Equals(input));
                //var ret = s_cp.m_selectedChapters.Find(delegate(string input) { return i.Key.Equals(input); });
                if (chapter.selected)
                {
                    chapterList.SelectedItems.Add(item);
                    chapter.selected = false;
                }
                iCur++;
            }
        }

        private async void StudyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (chapterList.SelectedItems.Count > 0)
            {
                foreach (chapterItem i in chapterList.SelectedItems)
                {
                    Debug.WriteLine("{0} {1}", i.name, i.count);
                    i.c.selected = true;
                }
                this.Frame.Navigate(typeof(study));
            }
            else
            {
                MessageDialog msgbox = new MessageDialog("No selected chapter");
                msgbox.Title = "Study selected chapter error!";
                await msgbox.ShowAsync();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
