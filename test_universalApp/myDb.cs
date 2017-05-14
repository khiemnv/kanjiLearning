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
        public chapterInfo()
        {
            markedIndexs = new List<int>();
        }
    }
    //offset->  |hdr
    //          |rKey
    //+rKey     |key as string
    //          |....
    //          |rMarked
    //+rMarked  |marked as string
    //          |....
    public class chapterRec
    {
        public UInt32 offset;
        public UInt32 size;
        public bool isDeleted;
        public UInt32 keyLen;
        public string key;
        public UInt32 markedLen;
        public string marked;
    }
    public class myFileDb
    {
        IRandomAccessStream stream;
        public Stream readStream;
        public Stream writeStream;
        string c_dataFile = "mydb.dat";
        public ulong Size;
        public myFileDb()
        {
        }
        public void load()
        {
            var t = Task.Run(async () => await loadAsync());
            t.Wait();
        }
        public void drop()
        {
            writeStream.SetLength(0);
            writeCursor = 0;
            readStream.SetLength(0);
            readCursor = 0;
            Size = 0;
        }
        public void unload()
        {
            writeStream.Flush();
            writeStream.Dispose();
            readStream.Dispose();
            stream.Dispose();
        }

        async Task loadAsync()
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
    }
    public class myDb
    {
        Dictionary<string, chapterRec> m_dict;  //path - marker data offset
        Dictionary<string, chapterRec> m_cache; //path - marker data offset
        List<chapterRec> m_deltedItem;    //header & offset
        myFileDb m_file;
        UInt32 c_pageSize = 32;
        UInt32 c_nPageMark = 0x0FFFffff;
        UInt32 c_fDeleted = 0x80000000;
        byte[] t_buff;
        byte[] t_page;
        bool m_bLoaded;

        public myDb()
        {
            m_bLoaded = false;

            m_dict = new Dictionary<string, chapterRec>();
            m_cache = new Dictionary<string, chapterRec>();
            m_deltedItem = new List<chapterRec>();
            t_buff = new byte[1024];
            t_page = new byte[c_pageSize];
            m_file = new myFileDb();
        }

        readBdError firstChapter(out chapterRec rec)
        {
            rec = null;
            m_file.seekReadCursor(0);
            return nextChapter(out rec);
        }
        enum readBdError
        {
            success,
            eof,
            deletedRec,
            dbMalform
        };
        private readBdError nextChapter(out chapterRec rec)
        {
            rec = new chapterRec();
            rec.offset = (UInt32)m_file.readCursor;

            UInt32 hdr;
            bool ret = m_file.readData(t_page, (int)c_pageSize);
            if (!ret) return readBdError.eof;

            hdr = BitConverter.ToUInt32(t_page, 0);
            rec.size = (hdr & c_nPageMark) * c_pageSize;
            if ((rec.size + rec.offset) > m_file.Size)
            {
                return readBdError.dbMalform;
            }

            if ((hdr & c_fDeleted) != 0)
            {
                rec.isDeleted = true;
                return readBdError.deletedRec;
            }

            if (rec.size > t_buff.Length)
            {
                Array.Resize<byte>(ref t_buff, (int)rec.size);
            }

            t_page.CopyTo(t_buff, 0);
            UInt32 nRead = c_pageSize;
            for (; nRead < rec.size; nRead += c_pageSize)
            {
                ret = m_file.readData(t_page, (int)c_pageSize);
                Debug.Assert(ret);
                t_page.CopyTo(t_buff, (int)nRead);
            }

            UInt32 iCursor = 4;
            rec.keyLen = BitConverter.ToUInt32(t_buff, (int)iCursor);
            iCursor += 4;
            rec.key = Encoding.UTF8.GetString(t_buff, (int)iCursor, (int)rec.keyLen);
            iCursor += rec.keyLen;
            rec.markedLen = BitConverter.ToUInt32(t_buff, (int)iCursor);
            iCursor += 4;
            rec.marked = Encoding.UTF8.GetString(t_buff, (int)iCursor, (int)rec.markedLen);

            return readBdError.success;
        }

        public void loadMarkeds(List<string> keys)
        {
            m_cache.Clear();
            foreach (var key in keys)
            {
                if (m_dict.ContainsKey(key))
                {
                    m_cache.Add(key, m_dict[key]);
                }
            }
        }

        public void unload()
        {
            if (!m_bLoaded) return;
            m_bLoaded = false;

            m_file.unload();
            m_dict.Clear();
            m_cache.Clear();
            m_deltedItem.Clear();
        }
        public void load()
        {
            if (m_bLoaded) return;
            m_bLoaded = true;

            //open file db
            m_file.load();

            //check file db malform

            //load marked info
            chapterRec ch;
            var error = firstChapter(out ch);
            while (error != readBdError.eof)
            {
                if (error == readBdError.dbMalform)
                {
                    m_file.drop();
                    m_dict.Clear();
                    m_deltedItem.Clear();
                    Debug.Assert(m_cache.Count == 0);
                    break;
                }
                if (ch.isDeleted)
                {
                    m_deltedItem.Add(ch);
                }
                else
                {
                    m_dict.Add(ch.key, ch);
                }
                error = nextChapter(out ch);
            }
        }
        chapterRec findRec(string key)
        {
            chapterRec rec = null;

            if (m_cache.ContainsKey(key))
            {
                rec = m_cache[key];
            }
            else if (m_dict.ContainsKey(key))
            {
                rec = m_dict[key];
                m_cache.Add(key, rec);
            }
            return rec;
        }
        public void getMarked(chapterInfo c)
        {
            c.markedIndexs.Clear();
            string key = c.path;
            chapterRec rec = findRec(key);
            if (rec == null) return;

            var arr = rec.marked.Split(new char[] { ';' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var i in arr)
            {
                int idx;
                if (int.TryParse(i, out idx)) { c.markedIndexs.Add(idx); }
            }
        }
        public void saveMarked(chapterInfo c)
        {
            string key = c.path;
            chapterRec rec = findRec(key);

            if (rec != null)
            {
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

                m_cache.Add(key, newrec);
                m_dict.Add(key, newrec);
            }
        }

        void resizeTmpBuff(int size)
        {
            if (t_buff.Length < size) { Array.Resize<byte>(ref t_buff, size); }
        }

        chapterRec delListFind(UInt32 size)
        {
            foreach (chapterRec i in m_deltedItem)
            {
                if (size <= i.size)
                {
                    return i;
                }
            }

            return null;
        }

        private void addMarked(chapterRec rec)
        {
            //estimate req size
            UInt32 size = 4 + 8 + (UInt32)(rec.key.Length + rec.marked.Length) * 2;
            size = (size + c_pageSize - 1) & ~(c_pageSize - 1);

            resizeTmpBuff((int)size);

            UInt32 offset = 4;
            rec.keyLen = (UInt32)Encoding.UTF8.GetBytes(rec.key, 0, rec.key.Length, t_buff, (int)offset + 4);
            BitConverter.GetBytes(rec.keyLen).CopyTo(t_buff, (int)offset);

            offset += rec.keyLen + 4;
            rec.markedLen = (UInt32)Encoding.UTF8.GetBytes(rec.marked, 0, rec.marked.Length, t_buff, (int)offset + 4);
            BitConverter.GetBytes(rec.keyLen).CopyTo(t_buff, (int)offset);

            size = (rec.markedLen + offset + 4 + c_pageSize - 1) & ~(c_pageSize - 1);
            BitConverter.GetBytes(size / c_pageSize).CopyTo(t_buff, 0);

            //find in delete list
            var reuse = delListFind(size);
            if (reuse != null)
            {
                m_deltedItem.Remove(reuse);
                rec.offset = reuse.offset;
                m_file.seekWriteCursor((int)rec.offset);
                rec.isDeleted = false;
                m_file.writeData(t_buff, (int)size);
                rec.size = reuse.size;
                Debug.Assert(size <= reuse.size);
            }
            else
            {
                //seek to end of file
                m_file.seekWriteCursor(-1);
                rec.offset = (UInt32)m_file.writeCursor;
                rec.isDeleted = false;
                m_file.writeData(t_buff, (int)size);
                rec.size = size;
            }
        }

        private void updateMarked(chapterRec rec)
        {
            //calc size
            int estimateSize = rec.marked.Length * 2;
            resizeTmpBuff(estimateSize);
            rec.markedLen = (UInt32)Encoding.UTF8.GetBytes(rec.marked, 0, rec.marked.Length, t_buff, 0);
            UInt32 newSize = 4 + 4 + rec.keyLen + 4 + rec.markedLen;
            if (rec.size < newSize)
            {
                //mark rec as delete
                m_file.seekWriteCursor((int)rec.offset);
                UInt32 hdr = (UInt32)(rec.size / c_pageSize) | c_fDeleted;
                m_file.writeInt32((int)hdr);

                m_deltedItem.Add(new chapterRec { size = rec.size, offset = rec.offset });
                addMarked(rec);
            }
            else
            {
                m_file.seekWriteCursor((int)rec.offset + 4 + 4 + (int)rec.keyLen);    //hdr + keysize + keylen
                m_file.writeInt32((int)rec.markedLen);
                m_file.writeData(t_buff, (int)rec.markedLen);
            }
        }
    }
}
