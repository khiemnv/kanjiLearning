#define use_worker
#define load_dict_use_thread

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
    public sealed partial class MainPage : Page
    {
        static contentProvider s_content = contentProvider.getInstance();
        myConfig m_config { get { return s_content.m_config; } }
        myWorker m_bgwork;

        public MainPage()
        {
            this.InitializeComponent();

            //test();
            //testWriteData();
            //ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            //s_content.LoadChapterCompleted += C_LoadCompleted;
            //s_content.LoadMultiChapterCompleted += S_content_LoadMultiChapterCompleted;

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

            //background work
            m_bgwork = new myWorker();
            m_bgwork.BgProcess += bg_process;
            m_bgwork.FgProcess += fg_process;
        }

        enum bgTaskType
        {
            saveFolder,
            loadData,
            loadDict
        }
        enum fgTaskType
        {
            prepareProgress,
            updateProgress,
            hideProgress,
            updateStatus
        }

        //pick folder
        async void getFolder()
        {
            FolderPicker picker;
            StorageFolder folder;

            picker = new FolderPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                Debug.WriteLine(string.Format("getFolder success {0}", folder.Path));
                m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.saveFolder, data = folder});
                m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.loadData, data = folder });
                browserPath.Text = folder.Path;
            }
        }

        private void fg_process(object sender, FgTask e)
        {
            switch((fgTaskType)e.type)
            {
                case fgTaskType.prepareProgress:
                    browserProg.Visibility = Visibility.Visible;
                    browserProg.Value = 0;
                    break;
                case fgTaskType.updateProgress:
                    browserProg.Value = (double)e.data;
                    break;
                case fgTaskType.hideProgress:
                    browserProg.Visibility = Visibility.Collapsed;
                    break;
                case fgTaskType.updateStatus:
                    statusBar.Text = (string)e.data;
                    break;
            }
        }

        int getTickCount() { return Environment.TickCount / 1000; }

        private void bg_process(object sender, BgTask e)
        {
            Debug.WriteLine(string.Format("bg_process {0}", (bgTaskType)e.type));
            switch((bgTaskType)e.type)
            {
                case bgTaskType.saveFolder:
                    {
                        StorageFolder folder = (StorageFolder)e.data;
                        //save last selected folder
                        if (folder.Path != m_config.lastPath)
                        {
                            m_config.lastPath = folder.Path;
                            //clear prev data
                            m_config.selectedChapters.Clear();
                            s_content.m_chapters.Clear();
                        }
                        var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                        mru.Clear();
                        string mruToken = mru.Add(folder, folder.Path);
                    }
                    break;
                case bgTaskType.loadData:
                    {
                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.prepareProgress});

                        StorageFolder folder = (StorageFolder)e.data;
                        IReadOnlyList<StorageFile> fileList = null;
                        var t = Task.Run(async () => {
                            fileList = await folder.GetFilesAsync();
                        });
                        t.Wait();
                        int n = fileList.Count;
                        int ret = 0;
                        for (int i = 0; i < n; i++)
                        {
                            t = Task.Run(async () => { 
                                ret = await s_content.loadSingleChapter(fileList[i]);
                            });
                            t.Wait();

                            m_bgwork.qryFgTask(new FgTask {type = (int)fgTaskType.updateProgress, data = (double)i*100/n });
                        }

                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.hideProgress });
                    }
                    break;
                case bgTaskType.loadDict:
                    {
                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.updateStatus, data = "Loading dict ..." });
                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.prepareProgress });
                        Task t = Task.Run(() => { myDict.Load(); });
                        while(t.Status != TaskStatus.RanToCompletion)
                        //while (myDict.loadProgress < 100)
                        {
                            m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.updateProgress, data = (double)myDict.loadProgress });
                            //myWorker.sleep(100);
                            t.Wait(100);
                        }
                        Debug.Assert(myDict.loadProgress == 100);
                        //Debug.Assert(t.Status == TaskStatus.RanToCompletion);
                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.hideProgress });
                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.updateStatus, data = "Loading completed!" });
                    }
                    break;
            }
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

        static bool s_lastFolderLoaded = false;
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
#if !enable_loaddata
            Debug.WriteLine("loaded");
            if (s_lastFolderLoaded) {
                browserPath.Text = m_config.lastPath;
                updateStatus(string.Format("Total chapters: {0}", s_content.m_chapters.Count));
            }
            else
            {
                s_lastFolderLoaded = true;
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

#if load_dict_use_thread
            m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.loadDict });
#else
            myDict.Load();
#endif  //load_dict_use_thread
#endif  //not enable_loaddata
        }

        private void loadLastPath()
        {
            //load last selected folder
            IStorageItem item = null;
            var mru = StorageApplicationPermissions.MostRecentlyUsedList;
            foreach (AccessListEntry entry in mru.Entries)
            {
                if (entry.Metadata == m_config.lastPath)
                {
                    string mruToken = entry.Token;
                    string mruMetadata = entry.Metadata;
                    var t = Task.Run(async () =>
                    {
                        try
                        {
                            item = await mru.GetFolderAsync(mruToken);
                        }
                        catch
                        {
                            //case last folder is removed
                            Debug.WriteLine("loadLastPath last path not exists");
                        }
                    });
                    t.Wait();
                    break;
                }
            }
            var lastFolder = (StorageFolder)item;
            if (lastFolder != null)
            {
                browserPath.Text = m_config.lastPath;
                m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.loadData, data = lastFolder});
            }
        }

        //private void S_content_LoadMultiChapterCompleted(object sender, contentProvider.LoadChapterCompletedEventArgs e)
        //{
        //    browserPath.Text = e.path;
        //}

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
            if (myDict.loadProgress < 100)
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

        private void browserBtn_Click(object sender, RoutedEventArgs e)
        {
            //m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.selectFolder });
            getFolder();
            Debug.WriteLine("browserBtn_Click end");
        }
    }
}
