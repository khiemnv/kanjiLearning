#define use_worker

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
using System.ComponentModel;
using Windows.Storage;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace test_universalApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IDisposable
    {
        static contentProvider s_content = contentProvider.getInstance();

        public MainPage()
        {
            this.InitializeComponent();

            //test();
            //testWriteData();
            //ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            s_content.LoadChapterCompleted += C_LoadCompleted;
            s_content.LoadMultiChapterCompleted += S_content_LoadMultiChapterCompleted;

            //this.Loaded += MainPage_Loaded;
            //this.LayoutUpdated += MainPage_LayoutUpdated;
            //this.Unloaded += MainPage_Unloaded;
            initCtrls();

            browserBtn.Click += browserBtn_Click;
            reloadBtn.Click += reloadBtn_Click;
            addBtn.Click += addBtn_Click;
            nextBtn.Click += nextBtn_Click;
        }

        void initEvents()
        {
            
        }

        BackgroundWorker worker;
        private void initCtrls()
        {
            txtBox.Text = "";
            txtBox.PlaceholderText = "Please use \";\" as seprator";
            //txtBox.AcceptsReturn = true;
            //txtBox.TextWrapping = TextWrapping.Wrap;
            //txtBox.Header = "Word list";
            //ScrollViewer.SetVerticalScrollBarVisibility(txtBox, ScrollBarVisibility.Auto);

#if use_worker
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
#if true
            worker.ProgressChanged += Worker_ProgressChanged;
#else
            worker.ProgressChanged += (s, e) => { browserProg.Value = e.ProgressPercentage; };
#endif
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.WorkerReportsProgress = true;
#endif
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("unloaded");
        }

        private void MainPage_LayoutUpdated(object sender, object e)
        {
            Debug.WriteLine("layoutUpdated");
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("loaded");
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
            txtBox.Text = txt;
        }

        private async void addBtn_Click(object sender, RoutedEventArgs e)
        {
            string txt = txtBox.Text;
            int ret = await s_content.saveChapter(txt);
        }

        StorageFolder folder;
        private async void browserBtn_Click(object sender, RoutedEventArgs e)
        {
#if use_worker
            //pick folder
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            folder = await picker.PickSingleFolderAsync();
            //show progress bar
            browserProg.Visibility = Visibility.Visible;
            browserProg.Value = 0;
            browserProg.Maximum = 100;
            //start work
            worker.RunWorkerAsync();
#else
            int ret = await s_content.loadMultipleChapter();
#endif
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine("Worker_RunWorkerCompleted {0}", e.Result);
            browserPath.Text = folder.Path;
            browserProg.Visibility = Visibility.Collapsed;

            //initEvents();
        }

#if track_progress
        int m_progress;
#endif
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
#if track_progress
            m_progress = e.ProgressPercentage;
#endif
            browserProg.Value = e.ProgressPercentage;
            Debug.WriteLine("Worker_ProgressChanged {0}", e.ProgressPercentage);
        }
#if use_worker
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Worker_DoWork start");
#if !track_progress
            Task t = Task.Run(() => s_content.loadMultipleChapter(worker, folder));
            t.Wait();
            Debug.WriteLine("Worker_DoWork end");
#else
            m_progress = 0;
            s_content.loadMultipleChapter(worker, folder);
            int i = 0;
            while (m_progress != 100)
            {
                Debug.WriteLine("Worker_DoWork wait {0} {1}", i++, m_progress);
                Task.Delay(1000);
            }
#endif
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                worker.Dispose();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~MainPage()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
#endif

    }
}
