using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test_guide.Model
{
    public class chapter
    {
        private string name;
        private string path;
        private int count;
        private int nmarked;
        public string Name { get { return name; } set { name = value; } }
        public string Path { get { return path; } set { path = value; } }
        public int Count { get { return count; } set { count = value; } }
        public int Nmarked { get { return nmarked; } set { nmarked = value; } }
        public double Percent { get { return (double)nmarked/count; } }
        public string zPercent { get { return string.Format("{0}/{1}", nmarked, count); } }
    }
}
