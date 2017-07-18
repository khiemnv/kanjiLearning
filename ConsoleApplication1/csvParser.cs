using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class csvParser : IDisposable
    {
        int m_recCount = 0;
        int m_recCur = 0;
        public int recCount { get { return m_recCount; } }
        public string[] getRec() {
            Debug.Assert(m_recCur < m_recCount);
            var res = m_res[m_recCur];
            m_recCur++;
            return res.ToArray();
        }
        public Uri uri;
        public Int64 total_size = 1;
        public Int64 processed = 0;
        enum binParserState
        {
            s,
            a2,
            b3, b2,
            c4
        }
        enum myTkType
        {
            t_comma = 0,
            t_dblq,
            t_eol,
            t_other,
        }
        string[] arr = {
                @"C:\Users\Khiem\Desktop\hv_word.csv",
                @"C:\Users\Khiem\Desktop\kangxi.csv",
                @"C:\Users\Khiem\Desktop\hv_org.csv",
                @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hanvietdict.js",
                @"C:\Users\Khiem\Downloads\Từ điển Hán Việt_v1.4_apkpure.com\assets\buildhvdict\hvchubothu.js",
                @"C:\Users\Khiem\Desktop\hannom_index.csv",
                @"C:\Users\Khiem\Desktop\character.csv",
                @"C:\Users\Khiem\Desktop\character_jdict.csv",
                @"C:\Users\Khiem\Desktop\bothu214.csv",
                @"C:\Users\Khiem\Desktop\component.txt",
                @"C:\Users\Khiem\Desktop\search.csv",
                @"C:\Users\Khiem\Desktop\conjugation.csv",
            };
        class myToken
        {
            public myTkType type;
            public char val;
            public myToken(char c)
            {
                switch (c)
                {
                    case '"':
                        type = myTkType.t_dblq;
                        break;
                    case ',':
                        type = myTkType.t_comma;
                        break;
                    default:
                        type = myTkType.t_other;
                        break;
                }
                val = c;
            }

            public static implicit operator string(myToken v)
            {
                return v.val.ToString();
            }

            public static implicit operator char(myToken v)
            {
                return v.val;
            }

            public static explicit operator int(myToken v)
            {
                return (int)v.type;
            }
        };
        enum myState
        {
            invalid = -1,
            s = 0,  //begin
            a,
            b,
            e,
            z
        };
        myState cur = myState.s;
        myState nState = myState.s;

        binParserState bp_state = binParserState.s;

        myTkType type;
        int wchr = 0;
        class myField
        {
            public int iStart;
            public int iCur;
            public bool dblQt;
        }
        class myPaserResult
        {
            public myPaserResult(string txt)
            {
                //line = txt;
            }
            //public string line;
            public List<string> arr = new List<string>();
            //public List<myField> fields = new List<myField>();
            public int count = 0;
            public myField curObj = new myField();
            public string gerCurObj()
            {
                //return line.Substring(curObj.iStart, curObj.iCur - curObj.iStart);
                return new string(buff, curObj.iStart, curObj.iCur - curObj.iStart);
            }
            public void add(char c)
            {
                if (iCur == buff.Length)
                {
                    int newSize = buff.Length + 512;
                    Array.Resize(ref buff, newSize);
                }
                buff[iCur] = c;
                iCur++;
            }
            int iCur = 0;
            char[] buff = new char[512];
            public void reset()
            {
                arr = new List<string>();
                count = 0;
                curObj.iCur = 0;
                curObj.iStart = 0;
                curObj.dblQt = false;
                iCur = 0;
            }
        }
        delegate void myRule(myPaserResult res, myToken tk);
        static void f_01(myPaserResult res, myToken tk)
        {

        }
        static void f_ss(myPaserResult res, myToken tk)
        {
            res.count++;
            res.arr.Add("");
            res.curObj.iStart = ++res.curObj.iCur;
            //res.fields.Add(res.curObj); //to debug
        }
        static void f_sa(myPaserResult res, myToken tk)
        { res.curObj.iStart = ++res.curObj.iCur; }
        static void f_aa(myPaserResult res, myToken tk)
        {
            res.curObj.iCur++;
        }
        static void f_bs(myPaserResult res, myToken tk)
        {
            res.count++;
            res.arr.Add(res.gerCurObj());
            res.curObj.iCur += 2;
            //res.fields.Add(res.curObj);
        }
        static void f_es(myPaserResult res, myToken tk)
        {
            res.count++;
            res.arr.Add(res.gerCurObj());
            res.curObj.iCur++;
            //res.fields.Add(res.curObj);
        }
        static void f_ba(myPaserResult res, myToken tk)
        {
            res.curObj.iCur += 2;
            res.curObj.dblQt = true;
        }
        static void f_en(myPaserResult res, myToken tk)
        {
            res.arr.Add(res.gerCurObj());
        }
        static void f_se(myPaserResult res, myToken tk)
        {
            res.curObj.iStart = res.curObj.iCur++;
        }
        static void f_zz(myPaserResult res, myToken tk)
        {
            res.curObj.iCur++;
        }
        //state table
        //state |token
        //      |,      |"      |eol|other
        //------+-------+-------+---+------
        //s     |s      |a      |end|e
        //a     |a      |b      |a  |a
        //b     |s      |a      |end|invalid
        //e     |s      |invalid|end|e
        //end   |s      |a      |end|e
        static myState[,] tbl = new myState[5, 4] {
            {myState.s, myState.a, myState.z, myState.e, },
            {myState.a, myState.b, myState.a, myState.a, },
            {myState.s, myState.a, myState.z, myState.invalid,},
            {myState.s, myState.invalid, myState.z, myState.e, },
            {myState.s, myState.a, myState.z, myState.e, },
        };
        static myRule[,] clbTbl = new myRule[5, 4] {
            {f_ss, f_sa, f_en, f_se},
            {f_aa, f_01, f_aa, f_aa },
            {f_bs, f_ba, f_en, f_01 },
            {f_es, f_01, f_en, f_aa },
            {f_ss, f_sa, f_zz, f_se },
        };
        myPaserResult res = new myPaserResult("");
        List<List<string>> m_res = new List<List<string>>();

        void tokenParse()
        {
            res.add((char)wchr);
            switch (wchr)
            {
                case '"':
                    type = myTkType.t_dblq;
                    break;
                case ',':
                    type = myTkType.t_comma;
                    break;
                case '\n':
                case '\r':
                    type = myTkType.t_eol;
                    break;
                default:
                    type = myTkType.t_other;
                    break;
            }
            cur = nState;
            nState = tbl[(int)cur, (int)type];
            cb = clbTbl[(int)cur, (int)type];
            cb(res, null);
        }
        void tokenParse2()
        {
            type = myTkType.t_other;
            if (wchr >= 0x10000)
            {
                wchr -= 0x10000;
                res.add( (char)(0xD800 | (wchr >> 10)));
                res.add( (char)(0xDC00 | (wchr & 0x3FF)));

                //char 1
                cur = nState;
                nState = tbl[(int)cur, (int)type];
                cb = clbTbl[(int)cur, (int)type];
                cb(res, null);
                //char 2
                cur = nState;
                nState = tbl[(int)cur, (int)type];
                cb = clbTbl[(int)cur, (int)type];
                cb(res, null);
            }
            else
            {
                res.add((char)(wchr));
                //char 1
                cur = nState;
                nState = tbl[(int)cur, (int)type];
                cb = clbTbl[(int)cur, (int)type];
                cb(res, null);
            }
        }
        myRule cb = null;
        class myCode
        {
            public enum nByte
            {
                n1,
                n2,
                n3,
                n4,
                nx,     //10xxxxx
                nz,     //invalid
            }
            byte[] table = new byte[256];
            public nByte decode(byte b)
            {
                return (nByte)table[b];
            }
            public myCode()
            {
                //init table
                for (int i = 0; i < 255; i++)
                {
                    if ((i & 0x80) == 0)
                    {
                        table[i] = (byte)nByte.n1;
                    }
                    else if ((i & 0x40) == 0)
                    {
                        table[i] = (byte)nByte.nx;
                    }
                    else if ((i & 0x20) == 0)
                    {
                        table[i] = (byte)nByte.n2;
                    }
                    else if ((i & 0x10) == 0)
                    {
                        table[i] = (byte)nByte.n3;
                    }
                    else if ((i & 0x08) == 0)
                    {
                        table[i] = (byte)nByte.n4;
                    }
                    else
                    {
                        table[i] = (byte)nByte.nz;
                    }
                }
            }
        }
        List<myBlock> m_block = new List<myBlock>();
        myCode m_code = new myCode();
        public void start()
        {
            var name = Path.GetFileName(uri.ToString());
            string path = arr.First((s) => { return s.Contains(name); });
            var fs = File.OpenRead(path);
            fs.Seek(0, SeekOrigin.Begin);
            const int block_size = 512;
            byte[] block = new byte[block_size];
            int nRead;
            nRead = fs.Read(block, 0, block_size);
            for (; nRead > 0; nRead = fs.Read(block, 0, block_size))
            {
                m_block.Add(new myBlock(nRead, block));
                m_nBlock++;
                block = new byte[block_size];
            }
        }
        int m_nBlock = 0;
        int m_curBlock = 0;
        public int blockCount { get { return m_nBlock; } }
        class myBlock {
            public byte[] m_data;
            public int m_count;
            public myBlock(int nRead, byte[] block)
            {
                m_count = nRead;
                m_data = block;
            }
        }
        public byte[] getBlock(out int nRead)
        {
            Debug.Assert(m_curBlock < m_nBlock);
            var b = m_block[m_curBlock];
            m_curBlock++;
            nRead = b.m_count;
            return b.m_data;
        }
        public void parseBlock(int nRead, byte[] block)
        {
            //var name = Path.GetFileName(uri.ToString());
            //string path = arr.First((s) => { return s.Contains(name); });
            //var fs = File.OpenRead(path);
            //fs.Seek(0, SeekOrigin.Begin);
            //const int block_size = 512;
            //byte[] block = new byte[block_size];
            //int nRead
            //res = new myPaserResult("");
            //nRead = fs.Read(block, 0, block_size);
            //for (; nRead > 0; nRead = fs.Read(block, 0, block_size))
            {
                processed += nRead;

                for (int i = 0; i < nRead; i++)
                {
                    //bin parser
                    switch (bp_state)
                    {
                        case binParserState.s:
                            {
                                myCode.nByte nByte = m_code.decode(block[i]);
                                switch (nByte)
                                {
                                    case myCode.nByte.n1:
                                        bp_state = binParserState.s;
                                        wchr = block[i];

                                        //token type: q, comma, eol, other
                                        tokenParse();
                                        break;
                                    case myCode.nByte.n2:
                                        bp_state = binParserState.a2;
                                        wchr = (block[i] & 0x1F) << 6;
                                        break;
                                    case myCode.nByte.n3:
                                        bp_state = binParserState.b3;
                                        wchr = (block[i] & 0x0F) << 12;
                                        break;
                                    case myCode.nByte.n4:
                                        bp_state = binParserState.c4;
                                        wchr = (block[i] & 0x07) << 18;
                                        break;
                                    case myCode.nByte.nx:
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            break;
                        case binParserState.a2:
                            Debug.Assert((block[i] & 0x80) > 0);
                            wchr |= (block[i] & 0x3F);
                            bp_state = binParserState.s;

                            //token type: other
                            tokenParse2();
                            break;
                        case binParserState.b3:
                            Debug.Assert((block[i] & 0x80) > 0);
                            wchr |= (block[i] & 0x3F) << 6;
                            bp_state = binParserState.a2;
                            break;
                        case binParserState.c4:
                            Debug.Assert((block[i] & 0x80) > 0);
                            wchr |= (block[i] & 0x3F) << 12;
                            bp_state = binParserState.b3;
                            break;
                    }
                    //token parser

                    //crt record
                    if (cb == f_en)
                    {
                        m_res.Add(res.arr);
                        m_recCount++;
                        res.reset();
                    }
                }
            }
            //fs.Close();
            //fs.Dispose();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~csvParser() {
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
}
