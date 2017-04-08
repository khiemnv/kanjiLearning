using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.Diagnostics;
using Windows.UI.ViewManagement;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace test_universalApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static contentProvider s_content = contentProvider.getInstance();

        public MainPage()
        {
            this.InitializeComponent();

            //test();
            //testWriteData();
            ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            s_content.LoadChapterCompleted += C_LoadCompleted;
            s_content.LoadMultiChapterCompleted += S_content_LoadMultiChapterCompleted;
        }

        private void S_content_LoadMultiChapterCompleted(object sender, contentProvider.LoadChapterCompletedEventArgs e)
        {
            browserPath.Text = e.path;
        }

        private async void testWriteData()
        {
            content c = new content();
            await c.loadData();
            c.m_words.Add(new word("心配(TÂM PHỐI)"));
            await c.saveData();
        }

        private void test()
        {
            //parse obj from string
            string txt = "言葉 ことば  (NGÔN DIỆP) Câu nói";
            //txt = "積極 せいこう (THÀNH CÔNG)";
            //txt = "心配 (TÂM PHỐI)";
            word w = new word(txt);
            txt = w.ToString();
        }

        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            //var itemId = ((MainPage)e.ClickedItem).UniqueId;
            this.Frame.Navigate(typeof(chapters));
        }

        private void onLoadDataComplete()
        {
            
        }

        private async void reloadBtn_Click(object sender, RoutedEventArgs e)
        {
            int ret = await s_content.loadChapter();
            Debug.WriteLine(string.Format("{0} {1} {2}", this.ToString(), "loadChapter", ret));
        }

        private void C_LoadCompleted(object sender, EventArgs e)
        {
            string txt = "";
            foreach (var word in s_content.m_content.m_words)
            {
                txt = txt + word.ToString() + "\r\n";
            }
            Debug.WriteLine(txt);
            TextBox.Text = txt;
        }

        private async void addBtn_Click(object sender, RoutedEventArgs e)
        {
            string txt = TextBox.Text;
            int ret = await s_content.saveChapter(txt);
        }

        private async void browserBtn_Click(object sender, RoutedEventArgs e)
        {
            int ret = await s_content.loadMultipleChapter();
        }
    }
}
