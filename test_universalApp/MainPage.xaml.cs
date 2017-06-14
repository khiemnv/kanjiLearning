#define use_worker

using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using System.ComponentModel;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Input;
using System.Collections.Generic;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace test_universalApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IDisposable
    {
        static contentProvider s_content = contentProvider.getInstance();
        BackgroundWorker m_worker;
        myConfig m_config { get { return s_content.m_config; } }
        bool isLoadingData = false;

        public MainPage()
        {
            this.InitializeComponent();

            //test();
            //testWriteData();
            //ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            s_content.LoadChapterCompleted += C_LoadCompleted;
            s_content.LoadMultiChapterCompleted += S_content_LoadMultiChapterCompleted;

            //this page events
            Loaded += MainPage_Loaded;
            //LayoutUpdated += MainPage_LayoutUpdated;
            Unloaded += MainPage_Unloaded;

            initCtrls();

            browserBtn.Tapped += browserBtn_Click;
            reloadBtn.Click += reloadBtn_Click;
            addBtn.Click += btnAdd_Click;
            nextBtn.Click += btnNext_Click;
            clean.Click += btnClean_Click;
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            s_content.m_content.m_words.Clear();
            txtBox.Text = "";
        }

        private void initCtrls()
        {
            txtBox.Text = "";
            txtBox.PlaceholderText = "言葉; ことば; (NGÔN DIỆP); từ\r\nPlease use \";\" as seprator";
            //txtBox.AcceptsReturn = true;
            //txtBox.TextWrapping = TextWrapping.Wrap;
            //txtBox.Header = "Word list";
            //ScrollViewer.SetVerticalScrollBarVisibility(txtBox, ScrollBarVisibility.Auto);

#if use_worker
            m_worker = new BackgroundWorker();
            m_worker.DoWork += Worker_DoWork;
#if true
            m_worker.ProgressChanged += Worker_ProgressChanged;
#else
            worker.ProgressChanged += (s, e) => { browserProg.Value = e.ProgressPercentage; };
#endif
            m_worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            m_worker.WorkerReportsProgress = true;
#endif
            //swipe next
            termGrid.ManipulationMode = ManipulationModes.TranslateX;
            termGrid.ManipulationCompleted += swipedLeft;

            Debug.WriteLine("{0} initCtrls done", this);
        }

        private void swipedLeft(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            const int delta = 15;
            if (e.Cumulative.Translation.X < -delta)
            {
                //move right
                btnNext_Click(sender, e);
            }
       }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("unloaded");
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("loaded");
            if (m_config.m_lastFolder != null) {
                browserPath.Text = m_config.m_lastFolder.Path;
                updateStatus(string.Format("Total chapters: {0}", s_content.m_chapters.Count));
            }
            else
            {
                loadLastPath();
            }

            //load last open file
            {
                foreach(var w in s_content.m_content.m_words)
                {
                    txtBox.Text += w.ToString() + "\r\n";
                }
            }

            //load db
            s_content.loadDb();

            //load dict
            myDict.Load();
        }

        private async void loadLastPath()
        {
            //load last selected folder
            var mru = StorageApplicationPermissions.MostRecentlyUsedList;
            foreach (AccessListEntry entry in mru.Entries)
            {
                if (entry.Metadata == m_config.lastPath) {
                    string mruToken = entry.Token;
                    string mruMetadata = entry.Metadata;
                    IStorageItem item = await mru.GetItemAsync(mruToken);
                    m_config.m_lastFolder = (StorageFolder)item;
                    break;
                }
            }
            if (m_config.m_lastFolder != null)
            {
                //show progress bar
                browserProg.Visibility = Visibility.Visible;
                browserProg.Value = 0;
                browserProg.Maximum = 100;
                //start work
                m_worker.RunWorkerAsync();
            }
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

        async void showErrMsg()
        {
            MessageDialog msgbox = new MessageDialog("Data is loading...");
            msgbox.Title = "Data is loaing...!";
            await msgbox.ShowAsync();
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (isLoadingData)
            {
                showErrMsg();
            }
            else
            {
                this.Frame.Navigate(typeof(chapters));
            }
        }

        void updateStatus(string txt)
        {
            statusBar.Text = txt;
            Debug.WriteLine(string.Format("[status] {0}", txt));
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

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string txt = txtBox.Text;
            int ret = await s_content.saveChapter(txt);
        }

        private async void browserBtn_Click(object sender, RoutedEventArgs e)
        {
#if use_worker
            //pick folder
            var picker = new FolderPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            //save last selected folder
            if (folder.Path != m_config.lastPath)
            {
                m_config.m_lastFolder = folder;
                m_config.lastPath = folder.Path;
                //clear prev data
                m_config.selectedChapters.Clear();
                s_content.m_chapters.Clear();
            }
            var mru = StorageApplicationPermissions.MostRecentlyUsedList;
            string mruToken = mru.Add(folder, folder.Path);

            //show progress bar
            browserProg.Visibility = Visibility.Visible;
            browserProg.Value = 0;
            browserProg.Maximum = 100;
            //start work
            m_worker.RunWorkerAsync();
#else
            int ret = await s_content.loadMultipleChapter();
#endif
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine("Worker_RunWorkerCompleted {0}", e.Result);
            isLoadingData = false;
            browserPath.Text = m_config.m_lastFolder.Path;
            browserProg.Visibility = Visibility.Collapsed;

            //update status bar
            updateStatus(string.Format("Load {0} chapters completed!", s_content.m_chapters.Count));
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
            isLoadingData = true;
#if !track_progress
            Task t = Task.Run(() => s_content.loadMultipleChapter(m_worker, m_config.m_lastFolder));
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

                m_worker.Dispose();

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
