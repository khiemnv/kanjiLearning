using System;
using System.Collections.Generic;
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
using Microsoft.Data.Sqlite;
using Microsoft.Data.Sqlite.Internal;
//using test_wrc;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.Data.Json;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using System.Text.RegularExpressions;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace test_data
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            //init();
            Loaded += MainPage_Loaded;

        }

        async Task longwork()
        {
            for (int i = 0; i < 10; i++)
            {
                //await Task.Delay(1000);

                Debug.WriteLine("{0}", i);
            }
            MessageDialog msg = new MessageDialog("test");
            await msg.ShowAsync();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {

            string pat = "(?:^|,)(?=[^\"]|(\")?)\"?((?(1)[^\"]*|[^,\"]*))\"?(?=,|$)";
            Regex reg = new Regex(pat);
            string txt = "22,123,\"123\",\"\"\"12\"";
            //txt = "1,一口氣,nhất khẩu khí,\" / Liên tục không gián đoạn, một mạch./ Nỗi tức giận, oán khí trong lòng./ Một hơi thở, chỉ mạng sống.\"";
            var m = reg.Matches(txt);
            //test3();
        }


        private async void test3()
        {
            //test load db .js
            StorageFile file = null;
            string path = @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js";
            {
                var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                foreach (AccessListEntry entry in mru.Entries)
                {
                    if (entry.Metadata == path)
                    {
                        string mruToken = entry.Token;
                        string mruMetadata = entry.Metadata;
                        IStorageItem item = await mru.GetItemAsync(mruToken);
                        file = (StorageFile)item;
                        break;
                    }
                }
            }

            if (file == null) {
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation =
                    PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".txt");
                picker.FileTypeFilter.Add(".dat");
                picker.FileTypeFilter.Add("*");

                file = await picker.PickSingleFileAsync();

                if (file != null)
                {
                    var mru = StorageApplicationPermissions.MostRecentlyUsedList;
                    string mruToken = mru.Add(file, file.Path);
                }
            }

            if (file == null) return;

            parseCSV(file);
        }

        async void parseCSV(StorageFile file)
        {
            //var lines = await FileIO.ReadLinesAsync(file);
            Stream stream = await file.OpenStreamForReadAsync();
            StreamReader sRd = new StreamReader(stream);

            string pat = "(?:^|,)(?=[^\"]|(\")?)\"?((?(1)[^\"]*|[^,\"]*))\"?(?=,|$)";
            //pat = "(?:^|,)(?:\")([^\"]*)\"(?=,|$)";
            Regex reg = new Regex(pat, RegexOptions.Compiled);
            var line = await sRd.ReadLineAsync();
            while (line != null)
            {
                var arr = reg.Matches(line);
                if (arr.Count > 0)
                {
                    var ret = arr[0].Groups[1].Value;
                }
                line = await sRd.ReadLineAsync();
            }
            sRd.Dispose();
            stream.Dispose();
        }
        //test load & save config file
        void test_cfg()
        {
                pathCfg cfg = new pathCfg();
                cfg.load();
                cfg.lastPath = "last path";
                cfg.selectedChapters.Add("item1");
                cfg.save();
                return;
            }
            async void test1()
        {
                Debug.WriteLine("start {0}", Environment.TickCount / 1000);
                //var t = Task.Run(()=> longwork());
                //t.Wait();
                await longwork();
                Debug.WriteLine("end {0}", Environment.TickCount / 1000);
                return;
            }
            void test2()
        {
                myDb db = new myDb();
                List<string> keys = new List<string> { @"c:\tmp\abc.txt", @"c:\tmp\123.txt" };
                db.load();

                chapterInfo c1 = new chapterInfo() { path = @"c:\tmp\123.txt", };
                chapterInfo c2 = new chapterInfo() { path = @"c:\tmp\abc.txt", };
                chapterInfo c3 = new chapterInfo() { path = @"c:\tmp\efd.txt", };
                db.getMarked(c3);
                db.saveMarked(c3);
                db.getMarked(c1);
                db.getMarked(c2);
                c2.markedIndexs = new List<int> { 1, 2, 4 };
                c1.markedIndexs = new List<int> { 7, 8, 9, 10, 11 };
                db.saveMarked(c1);
                db.saveMarked(c2);
                db.unload();

                db.load();
                db.getMarked(c2);
                c2.markedIndexs = new List<int> { 1, 2, 4, 5, 6, 7, 8, 9, 10, 11 };
                db.saveMarked(c2);
                c2.markedIndexs = new List<int> { 1, 2, 4, 5, 6, 7, 8, 9, 10 };
                db.saveMarked(c2);
                c1.markedIndexs = new List<int> { 1, 2, 4, 5, 6, 7, 8, 9, 10 };
                db.saveMarked(c1);
                db.loadMarkeds(new List<string> { c1.path });
                db.saveMarked(c1);
            }

            //configMng m_config;
            //async void init()
            //{
            //    Class1 cl1 = new Class1();
            //    StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
            //            "test.data", CreationCollisionOption.OpenIfExists);
            //    BasicProperties bs = await dataFile.GetBasicPropertiesAsync();
            //    if (bs.Size > 0)
            //    {
            //        IRandomAccessStream stream = await dataFile.OpenAsync(FileAccessMode.ReadWrite);
            //        Stream readStream = stream.AsStreamForRead();
            //        Stream writeStream = stream.AsStreamForWrite();
            //        DataReader dr = null;
            //        var buf = dr.ReadBuffer(512);
            //        //BinaryReader br = null;

            //        m_config = new configMng();

            //        byte[] data = new byte[512];
            //        int ret;
            //        ret = readStream.Read(data, 0, 512);

            //        IntPtr des = Marshal.AllocHGlobal(512);
            //        Marshal.Copy(data, 0, des, 512);
            //        ret = m_config.parseData(des.ToInt64(), ret);
            //        Marshal.FreeHGlobal(des);
            //        //UnmanagedMemoryStream t = null;

            //    }
            //}

        }



    }
