using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            foreach (var i in s_cp.m_chapters)
            {
                chapterList.Items.Add(new chapterItem() { name = i.Key, count = i.Value.Count });
            }
        }

        private void StudyBtn_Click(object sender, RoutedEventArgs e)
        {
            s_cp.m_selectedChapters.Clear();
            foreach (chapterItem i in chapterList.SelectedItems)
            {
                Debug.WriteLine("{0} {1}", i.name, i.count);
                s_cp.m_selectedChapters.Add(i.name);
            }
            this.Frame.Navigate(typeof(study));
        }
    }
}
