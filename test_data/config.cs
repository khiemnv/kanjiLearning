using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace test_data
{
    [DataContract]
    public class pathCfgData
    {
        [DataMember]
        public string mruToken;
        [DataMember]
        public string lastPath;
        [DataMember]
        public List<string> selectedChapters;

        public StorageFolder m_lastFolder;

        public pathCfgData()
        {
            selectedChapters = new List<string>();
        }
    }

    public class pathCfg: baseCfg<pathCfgData>
    {

        public string lastPath
        {
            get { return m_data.lastPath; }
            set { m_data.lastPath = value; }
        }
        public List<string> selectedChapters
        {
            get { return m_data.selectedChapters; }
            set { m_data.selectedChapters = value; }
        }

        public pathCfg()
        {
            m_configFile = "mycfg.file";
        }
        public new void load()
        {
            base.load();
            if (m_data == null) { m_data = new pathCfgData(); }
        }
    }

    public class baseCfg<T>
    {
        protected string m_configFile;
        protected T m_data;

        protected void load()
        {
            var t = Task.Run(() => loadAsync());
            t.Wait();
        }
        public void save()
        {
            var t = Task.Run(() => saveAsync());
            t.Wait();
        }

        protected async Task loadAsync()
        {
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                m_configFile, CreationCollisionOption.OpenIfExists);
            BasicProperties bs = await dataFile.GetBasicPropertiesAsync();
            if (bs.Size > 0)
            {
                Stream stream = await dataFile.OpenStreamForReadAsync();
                stream.Seek(0, SeekOrigin.Begin);
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                m_data = (T)serializer.ReadObject(stream);
                stream.Dispose();
            }
        }

        protected async Task saveAsync()
        {
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                m_configFile, CreationCollisionOption.OpenIfExists);
            Stream writeStream = await dataFile.OpenStreamForWriteAsync();
            writeStream.SetLength(0);
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            serializer.WriteObject(writeStream, m_data);
            await writeStream.FlushAsync();
            writeStream.Dispose();
        }
    }
}
