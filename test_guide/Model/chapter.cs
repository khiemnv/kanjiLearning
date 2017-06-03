using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

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
        public string status { get { return string.Format("Total/Marked: {0}/{1}", count, nmarked); } }
        public Brush color { get {
                Color[] arr = {Colors.Red, Colors.Orange, Colors.Yellow,
                    Colors.Green, Colors.Cyan, Colors.DarkBlue, Colors.Violet };
                int i = nmarked * arr.Length / count;
                Debug.WriteLine("i color {0}", i);
                SolidColorBrush br = new SolidColorBrush(arr[i]);
                return br;
            } }
    }
}
