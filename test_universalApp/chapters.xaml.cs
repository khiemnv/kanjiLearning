using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
        myConfig m_config;

        class chapterItem
        {
            public string name { get; set; }
            public int count { get; set; }
            public chapter c;
        }

        public chapters()
        {
            this.InitializeComponent();

            Loaded += Chapters_Loaded;
            Unloaded += Chapters_Unloaded;
        }

        private void Chapters_Loaded(object sender, RoutedEventArgs e)
        {
            loadData();

            if (chapterList.SelectedItems.Count > 0) { 
            chapterList.ScrollIntoView(chapterList.SelectedItems[0]);
            }
        }

        private void Chapters_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("{0} selectd {1}", this, chapterList.SelectedItems.Count);
        }

        private void loadData()
        {
            m_config = myConfig.getInstance();

            //load chapter data
            chapterList.Items.Clear();
            int iCur = 0;
            foreach (var i in s_cp.m_chapters)
            {
                var chapter = i.Value;
                var item = new chapterItem() { name = chapter.name,
                    count = chapter.words.Count, c = chapter };
                chapterList.Items.Add(item);

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
            s_cp.loadDb();
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
                    Debug.WriteLine("{0} {1}", i.name, i.count);
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
                Debug.WriteLine("{0} {1}", i.name, i.count);
                i.c.selected = true;
                m_config.selectedChapters.Add(i.c.path);
            }
            m_config.save();
            this.Frame.Navigate(typeof(MainPage));
        }

    }
}
