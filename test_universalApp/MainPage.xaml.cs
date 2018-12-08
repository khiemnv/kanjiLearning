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
        readonly contentProvider m_content;
        readonly myMainPgCfg m_config;
        readonly myChapterPgCfg m_chapterPgCfg;
        readonly myLessonPgCfg m_lessonPgCfg;
        readonly myWorker m_bgwork;

        public MainPage()
        {
            this.InitializeComponent();

            m_content = contentProvider.getInstance();
            m_config = m_content.m_mainPgCfg;
            m_lessonPgCfg = m_content.m_lessonPgCfg;
            m_chapterPgCfg = m_content.m_chapterPgCfg;

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

            browserBtn.Tapped += browserBtn_ClickAsync;
            openBtn.Click += browserBtn_ClickAsync;
            reloadBtn.Click += reloadBtn_Click;
            addBtn.Click += btnAdd_Click;
            nextBtn.Click += btnNext_Click;
            prevBtn.Click += PrevBtn_Click;
            clean.Click += btnClean_Click;
            clearBtn.Click += btnClean_Click;

            //background work
            m_bgwork = new myWorker();
            m_bgwork.BgProcess += bg_process;
            m_bgwork.FgProcess += fg_process;
        }

        private void OptWords_Checked(object sender, RoutedEventArgs e)
        {
            m_config.studyMode = myMainPgCfg.EStudyMode.learningWords;
            m_config.save();
        }

        private void OptNews_Click(object sender, RoutedEventArgs e)
        {
            m_config.studyMode = myMainPgCfg.EStudyMode.readingNews;
            m_config.save();
        }

        enum bgTaskType
        {
            saveFolder,
            loadData,
            loadDict,
            loadLessons,
            saveLessonFolder,
            delayNext
        }
        enum fgTaskType
        {
            prepareProgress,
            updateProgress,
            hideProgress,
            updateStatus,
            delayNext
        }

        //pick folder
        async Task<StorageFolder> getFolderAsync()
        {
            FolderPicker picker = null;
            StorageFolder folder = null;

            picker = new FolderPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            try
            { 
            folder = await picker.PickSingleFolderAsync();
            }
            catch(Exception e)
            {
                txtBox.Text += e.Message.ToString();
            }
            return folder;
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
                case fgTaskType.delayNext:
                    if (s_loadLastPathSuccess)
                    {
                        this.Frame.Navigate(typeof(chapters));
                    }
                    else
                    {
                        showErrMsg("Load folder data error!");
                    }
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
                        //not used
                        throw new Exception("not use");

                        StorageFolder folder = (StorageFolder)e.data;
                        //save last selected folder
                        if (folder.Path != m_chapterPgCfg.lastPath)
                        {
                            m_chapterPgCfg.lastPath = folder.Path;
                            //clear prev data
                            m_chapterPgCfg.clean();
                            m_chapterPgCfg.save();

                            m_content.m_chapters.Clear();
                        }
                        var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                        mru.Clear();
                        string mruToken = mru.Add(folder, folder.Path);
                    }
                    break;
                case bgTaskType.saveLessonFolder:
                    {
                        StorageFolder folder = (StorageFolder)e.data;
                        //save last selected folder
                        if (folder.Path != m_lessonPgCfg.lastPath)
                        {
                            m_lessonPgCfg.lastPath = folder.Path;
                            //clear prev data
                            m_lessonPgCfg.selectedLessons.Clear();
                            m_lessonPgCfg.save();

                            m_content.m_lessons.Clear();
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
                                ret = await m_content.loadSingleChapter(fileList[i]);
                            });
                            t.Wait();

                            m_bgwork.qryFgTask(new FgTask {type = (int)fgTaskType.updateProgress, data = (double)i*100/n });
                        }

                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.hideProgress });
                    }
                    break;
                case bgTaskType.loadLessons:
                    {
                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.prepareProgress });

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
                                ret = await m_content.loadSingleLesson(fileList[i]);
                            });
                            t.Wait();

                            m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.updateProgress, data = (double)i * 100 / n });
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
                case bgTaskType.delayNext:
                    {
                        m_bgwork.qryFgTask(new FgTask { type = (int)fgTaskType.delayNext });
                    }
                    break;
            }
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            //m_content.m_content.m_words.Clear();
            //txtBox.Text = "";
            recentLst.Items.Clear();
            s_lastFolderLoaded = "";

            m_chapterPgCfg.lastPath = "";
            m_chapterPgCfg.markedInfo.Clear();
            m_chapterPgCfg.clean();
            m_chapterPgCfg.save();

            m_content.m_chapters.Clear();

            var mru = StorageApplicationPermissions.MostRecentlyUsedList;
            mru.Clear();
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

            //recent list
            recentLst.SelectionChanged += RecentLst_SelectionChanged;

            //read btn
            readBtn.Click += ReadBtn_Click;

            Debug.WriteLine("{0} initCtrls done", this);
        }

        private void ReadBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(readPg2));
        }

        private void RecentLst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //save selected path
            if (recentLst.SelectedItems.Count > 0)
            {
                var selectedItem = recentLst.SelectedItem.ToString();
                //m_chapterPgCfg.lastPath = selectedItem;
            }
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

        static string s_lastFolderLoaded = "";
        static bool s_loadLastPathSuccess = false;
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
#if !enable_loaddata
            Debug.WriteLine("loaded");

            //recent list
            {
                recentLst.Items.Clear();
                IStorageItem item = null;
                List<string> q = new List<string>();
                string slected = "";
                var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                foreach (AccessListEntry entry in mru.Entries)
                {
                    try
                    {
                        item = await mru.GetFolderAsync(entry.Token);
                        q.Add(entry.Metadata);
                        if (entry.Metadata == m_chapterPgCfg.lastPath)
                        {
                            slected = m_chapterPgCfg.lastPath;
                        }
                    }
                    catch
                    {
                        //case last folder is removed
                        Debug.WriteLine("loadLastPath last path not exists");
                    }
                }

                q.Sort();
                foreach (string i in q)
                {
                    recentLst.Items.Add(i);
                }

                recentLst.SelectedItem = slected;
            }
            

            //load last open file
            {
                foreach (var w in m_content.m_content.m_words)
                {
                    txtBox.Text += w.ToString() + "\r\n";
                }
            }

            //load db
            m_content.loadDb();

#if load_dict_use_thread
            m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.loadDict });
#else
            myDict.Load();
#endif  //load_dict_use_thread
#endif  //not enable_loaddata

            if (m_config.studyMode == myMainPgCfg.EStudyMode.learningWords)
            {
                optWords.IsChecked = true;
            }
            else
            {
                optNews.IsChecked = true;
            }
            optNews.Checked += OptNews_Click;
            optWords.Checked += OptWords_Checked;

            optBtn.Click += OptBtn_Click;
        }

        private void OptBtn_Click(object sender, RoutedEventArgs e)
        {
            split.IsPaneOpen = true;
        }

        private async void loadLastPath(string path)
        {
            //load data?
            if (s_lastFolderLoaded != path)
            {
                s_lastFolderLoaded = path;
                //clear prev data
                if ( path != m_chapterPgCfg.lastPath)
                {
                    m_chapterPgCfg.lastPath = path;
                    m_chapterPgCfg.clean();
                    m_chapterPgCfg.save();
                }
                m_content.m_chapters.Clear();
            }
            else
            {
                m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.delayNext });
                return;
            }

            //open last path
            var lastPath = m_chapterPgCfg.lastPath;
            if (m_config.studyMode == myMainPgCfg.EStudyMode.readingNews)
            {
                lastPath = m_lessonPgCfg.lastPath;
            }

            s_loadLastPathSuccess = false;

            IStorageItem item = null;
            var mru = StorageApplicationPermissions.MostRecentlyUsedList;
            AccessListEntry entry;
            string token="";
            for (int i =0;i< mru.Entries.Count;i++)
            {
                entry = mru.Entries[i];
                token = entry.Token;
                if (lastPath == entry.Metadata)
                {
                    try
                    {
                        item = await mru.GetFolderAsync(entry.Token);
                    }
                    catch
                    {
                        //case last folder is removed
                        Debug.WriteLine("loadLastPath last path not exists");
                    }                        
                    break;
                }
            }

            //check open last path error
            do
            {
                if (item == null){break;}
                if (item.Path != lastPath){break;}

                s_loadLastPathSuccess = true;
            }                
            while (false);

            if (!s_loadLastPathSuccess)
            {
                mru.Remove(token);
                recentLst.Items.Remove(lastPath);
            }            
            else
            {
                var lastFolder = (StorageFolder)item;
                if (lastFolder != null)
                {
                    //browserPath.Text = lastPath;
                    if (m_config.studyMode == myMainPgCfg.EStudyMode.readingNews)
                    {
                        m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.loadLessons, data = lastFolder });
                    }
                    else
                    {
                        m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.loadData, data = lastFolder });
                    }
                }
            }

            //delay next
            m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.delayNext });
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

        async void showErrMsg(string msg = "Data is loading")
        {
            MessageDialog msgbox = new MessageDialog(msg);
            msgbox.Title = "ERROR";
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
                if (m_config.studyMode == myMainPgCfg.EStudyMode.readingNews)
                {
                    this.Frame.Navigate(typeof(lessonPg));
                }
                else
                {
                    //load selected data
                    if (recentLst.SelectedItems.Count > 0)
                    {
                        loadLastPath(recentLst.SelectedItem.ToString());
                        //this.Frame.Navigate(typeof(chapters));
                    }
                    else
                    {
                        showErrMsg("Not select folder!");
                    }
                }
            }
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (myDict.loadProgress < 100)
            {
                showErrMsg();
            }
            else
            {
                this.Frame.Navigate(typeof(dict));
            }
        }

        void updateStatus(string txt)
        {
            statusBar.Text = txt;
            Debug.WriteLine(string.Format("[status] {0}", txt));
        }

        private async void reloadBtn_Click(object sender, RoutedEventArgs e)
        {
            int ret = await m_content.loadChapter();
            Debug.WriteLine(string.Format("{0} {1} {2}", this.ToString(), "loadChapter", ret));
        }

        private void C_LoadCompleted(object sender, EventArgs e)
        {
            string txt = "";
            foreach (var word in m_content.m_content.m_words)
            {
                txt = txt + word.ToString() + "\r\n";
            }
            Debug.WriteLine(txt);
            txtBox.Text = txt;
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string txt = txtBox.Text;
            int ret = await m_content.saveChapter(txt);
        }

        private async void browserBtn_ClickAsync(object sender, RoutedEventArgs e)
        {
            //m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.selectFolder });
            StorageFolder folder = null;
            try
            {
                folder = await getFolderAsync();
            }
            catch
            {
                txtBox.Text += "get folder error!";
            }
            if (folder != null)
            {
                Debug.WriteLine(string.Format("getFolder success {0}", folder.Path));

                //save recent folder
                if (!recentLst.Items.Contains(folder.Path))
                {
                    var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                    string mruToken = mru.Add(folder, folder.Path);
                    recentLst.Items.Insert(0, folder.Path);
                    recentLst.SelectedItem = recentLst.Items[0];
                }

                if (m_config.studyMode == myMainPgCfg.EStudyMode.readingNews)
                {
                    //m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.saveLessonFolder, data = folder });
                    //m_lessonPgCfg.lastPath = folder.Path;
                    //m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.loadLessons, data = folder });
                }
                else
                {
                    //m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.saveFolder, data = folder });
                    //if (folder.Path != m_chapterPgCfg.lastPath)
                    //{
                    //    m_chapterPgCfg.lastPath = folder.Path;
                    //    //clear prev data
                    //    m_chapterPgCfg.selectedChapters.Clear();
                    //    m_chapterPgCfg.save();

                    //    m_content.m_chapters.Clear();
                    //    m_bgwork.qryBgTask(new BgTask { type = (int)bgTaskType.loadData, data = folder });
                    //}
                }
                //browserPath.Text = folder.Path;
            }
            Debug.WriteLine("browserBtn_Click end");
        }
    }
}
