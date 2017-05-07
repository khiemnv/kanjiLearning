using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace test_universalApp
{
    public class chapterInfo
    {
        public string path;
        public List<int> markedIndexs;
    }
    //offset->  |size
    //          |isDeleted
    //          |rKey
    //          |rMarked
    //          |....
    //+rKey     |key as string
    //          |....
    //+rMarked  |marked as string
    //          |....
    public class chapterRec
    {
        public Int32 offset;
        public Int32 size;
        public Int32 isDeleted;
        public Int32 keyLen;
        public string key;
        public Int32 markedLen;
        public string marked;
    }
    public class myFileDb : IDisposable
    {
        IRandomAccessStream stream;
        public Stream readStream;
        public Stream writeStream;
        string c_dataFile = "mydb.dat";
        public ulong Size;
        public myFileDb()
        {
        }

        public async Task loadAsync()
        {
            StorageFile dataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                c_dataFile, CreationCollisionOption.OpenIfExists);
            BasicProperties bs = await dataFile.GetBasicPropertiesAsync();
            Size = bs.Size;
            stream = await dataFile.OpenAsync(FileAccessMode.ReadWrite);
            readStream = stream.AsStreamForRead();
            writeStream = stream.AsStreamForWrite();
            readCursor = 0;
            writeCursor = 0;
        }

        public Int32 readCursor;
        public Int32 writeCursor;
        public bool readInt32(out Int32 val)
        {
            val = -1;
            byte[] bInt32 = new byte[4];
            int ret = readStream.Read(bInt32, 0, 4);
            readCursor += ret;
            if (ret == 4)
            {
                val = Bit​Converter.ToInt32(bInt32, 0);
                return true;
            }
            return false;
        }
        public void writeInt32(Int32 val)
        {
            byte[] bInt32 = Bit​Converter.GetBytes(val);
            writeStream.Write(bInt32, 0, 4);
            writeCursor += 4;
        }
        public bool readData(byte[] data, Int32 size)
        {
            int ret = readStream.Read(data, 0, size);
            readCursor += ret;
            return (ret == size);
        }
        public void writeData(byte[] data, Int32 size)
        {
            writeStream.Write(data, 0, size);
            writeCursor += size;
            writeStream.Flush();
        }
        public void seekReadCursor(Int32 offset)
        {
            readCursor = (Int32)readStream.Seek(offset, SeekOrigin.Begin);
            Debug.Assert(readCursor == offset);
        }
        public void advanceWriteCursor(Int32 nByte)
        {
            writeCursor = (Int32)writeStream.Seek(nByte, SeekOrigin.Current);
        }
        public void seekWriteCursor(Int32 offset)
        {
            if (offset == -1)
            {
                writeCursor = (Int32)writeStream.Seek(0, SeekOrigin.End);
            }
            else
            {
                writeCursor = (Int32)writeStream.Seek(offset, SeekOrigin.Begin);
                Debug.Assert(writeCursor == offset);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writeStream.Flush();
                    readStream.Dispose();
                    writeStream.Dispose();
                    stream.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~myFileDb() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
    public class myDb
    {
        Dictionary<string, chapterRec> m_dict; //path - marker data offset

        public myDb()
        {
        }

        public async Task loadAsync()
        {
            if (m_file == null) { 
            m_dict = new Dictionary<string, chapterRec>();
            m_file = new myFileDb();
            await m_file.loadAsync();
            }
        }
        public void unload() {
            if (m_file != null) { 
            m_file.Dispose();
            m_file = null;
            }
        }

        myFileDb m_file;
        chapterRec firstChapter()
        {
            m_file.seekReadCursor(0);
            return nextChapter();
        }
        private chapterRec nextChapter()
        {
            chapterRec rec = new chapterRec();
            rec.offset = m_file.readCursor;

            Int32 size;
            bool ret = m_file.readInt32(out size);
            if (!ret) return null;
            rec.size = size;

            Int32 isDeleted;
            ret = m_file.readInt32(out isDeleted);
            if (!ret) return null;
            rec.isDeleted = isDeleted;

            byte[] data = new byte[size];
            ret = m_file.readData(data, size - 8);
            if (!ret) return null;

            rec.keyLen = BitConverter.ToInt32(data, 0);
            rec.markedLen = BitConverter.ToInt32(data, 4);
            rec.key = Encoding.UTF8.GetString(data, 8, rec.keyLen);
            rec.marked = Encoding.UTF8.GetString(data, 8 + rec.keyLen, rec.markedLen);

            return rec;
        }

        public void loadMarkeds(List<string> keys)
        {
            keys = new List<string>(keys);

            m_dict.Clear();
            var ch = firstChapter();
            while (ch != null)
            {
                if (ch.isDeleted == 0
                 && keys.Contains(ch.key))
                {
                    m_dict.Add(ch.key, ch);
                    keys.Remove(ch.key);
                }
                ch = nextChapter();
            }
        }

        public void getMarked(chapterInfo c)
        {
            c.markedIndexs = new List<int>();
            string key = c.path;
            if (m_dict.ContainsKey(key))
            {
                var rec = m_dict[key];
                var arr = rec.marked.Split(new char[] { ';' },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (var i in arr)
                {
                    c.markedIndexs.Add(int.Parse(i));
                }
            }
        }
        public void saveMarked(chapterInfo c)
        {
            string key = c.path;
            if (m_dict.ContainsKey(key))
            {
                var rec = m_dict[key];
                rec.marked = string.Join(";", c.markedIndexs);
                updateMarked(rec);
            }
            else
            {
                var newrec = new chapterRec()
                {
                    key = c.path,
                    marked = string.Join(";", c.markedIndexs)
                };
                addMarked(newrec);
                m_dict.Add(newrec.key, newrec);
            }
        }

        private void addMarked(chapterRec rec)
        {
            //seek to end of file
            m_file.seekWriteCursor(-1);
            rec.offset = m_file.writeCursor;
            rec.isDeleted = 0;

            //estimate req size
            int size = 8 + 8 + (rec.key.Length + rec.marked.Length) * 2;
            size = (size + 0xFF) & ~(0xFF);
            byte[] buff = new byte[size];

            Int32 offset = 16;
            rec.keyLen = Encoding.UTF8.GetBytes(rec.key, 0, rec.key.Length, buff, offset);

            offset += rec.keyLen;
            rec.markedLen = Encoding.UTF8.GetBytes(rec.marked, 0, rec.marked.Length, buff, offset);

            rec.size = (rec.markedLen + offset + 0xFF) & ~(0xFF);

            BitConverter.GetBytes(rec.size).CopyTo(buff, 0);
            BitConverter.GetBytes(rec.isDeleted).CopyTo(buff, 4);
            BitConverter.GetBytes(rec.keyLen).CopyTo(buff, 8);
            BitConverter.GetBytes(rec.markedLen).CopyTo(buff, 12);

            m_file.writeData(buff, rec.size);
        }

        private void updateMarked(chapterRec rec)
        {
            //calc size
            byte[] arr = new byte[rec.marked.Length * 2];
            rec.markedLen = Encoding.UTF8.GetBytes(rec.marked, 0, rec.marked.Length, arr, 0);
            int newSize = 16 + rec.keyLen + rec.markedLen;
            if (rec.size > newSize)
            {
                m_file.seekWriteCursor(rec.offset + 12);
                m_file.writeInt32(rec.markedLen);
                m_file.advanceWriteCursor(rec.keyLen);
                m_file.writeData(arr, rec.markedLen);
            }
            else
            {
                //mark rec as delete
                m_file.seekWriteCursor(rec.offset + 4);
                m_file.writeInt32(-1);
                //add new record
                addMarked(rec);
            }
        }
    }
}
