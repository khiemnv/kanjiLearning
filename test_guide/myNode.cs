#define dict_dist
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Data.Xml.Xsl;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation;

namespace test_guide
{

    public class myNode
    {
        public string txt;
        public enum Type
        {
            none = -1,
            div = 0,
            p,
            img,
            a,
            li,
            b,
            i,
            u,
            br,
            span,
            iframe,
            text, //run
            h1,
            h2,
            h3,
            ul,
            hr,
            font,
        }
        public Type type
        {
            get
            {
                Type type;
                return Enum.TryParse<Type>(zType, out type) ? type : Type.none;
            }
        }

        public static TypedEventHandler<Hyperlink, HyperlinkClickEventArgs> OnHyberlinkClick { get; set; }

        public string zType;

        public myNode child;
        public myNode sib;
        public myNode() {; }
        public myNode(HtmlNode xNode)
        {
            txt = xNode.InnerText;
            zType = xNode.Name;
        }
        public static myNode convert(string htmltxt)
        {
            Stack<myNode> nStack = new Stack<myNode>();
            Stack<HtmlNode> xStack = new Stack<HtmlNode>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmltxt);
            var xRoot = doc.DocumentNode;
            var nRoot = new myNode(xRoot);
            //dfs
            nStack.Push(nRoot);
            xStack.Push(xRoot);
            int curLevel = 0;
            while (xStack.Count != 0)
            {
                var xCur = xStack.Peek();
                var nCur = nStack.Peek();
                //dfs next x node
                if (xCur.HasChildNodes && nCur.child == null)
                {
                    //go depth
                    curLevel++;
                    xStack.Push(xCur.FirstChild);
                    nCur.child = new myNode(xCur.FirstChild);
                    nStack.Push(nCur.child);
                }
                else if (xCur.NextSibling != null)
                {
                    nCur.sib = new myNode(xCur.NextSibling);
                    xCur = xCur.NextSibling;
                    nCur = nCur.sib;
                    xStack.Pop();
                    nStack.Pop();
                    xStack.Push(xCur);
                    nStack.Push(nCur);
                }
                else
                {
                    //go up
                    curLevel--;
                    xStack.Pop();
                    nStack.Pop();
                }
            }

            return nRoot;
        }

        private static Inline crtInline(HtmlNode xNode)
        {
            Inline converted = null;
            switch (xNode.Name)
            {
                case "hr":
#if dict_dist
                    converted = new LineBreak();
#endif
                    break;
                case "a":
                    var hb = new Hyperlink();
                    hb.Click += OnHyberlinkClick;
                    hb.Inlines.Add(new Run() { Text = xNode.InnerText });
                    return hb;
                case "i":
                case "#text":
                    converted = new Run() { Text = xNode.InnerText };
                    break;
                default:
                    break;
            }
            return converted;
        }

        public static Span convert2(string htmltxt)
        {
            List<HtmlNode> trace = new List<HtmlNode>();
            Stack<HtmlNode> xStack = new Stack<HtmlNode>();
            Stack<Span> lStack = new Stack<Span>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmltxt);
            var xRoot = doc.DocumentNode;
            var lRoot = new Span();
            //dfs
            xStack.Push(xRoot);
            lStack.Push(lRoot);

            int curLevel = 0;
            int lineNumber = 0;
            while (xStack.Count != 0)
            {
                var xCur = xStack.Peek();
                var lCur = lStack.Peek();

                if (!trace.Contains(xCur))
                {
                    //trace cur node
                    trace.Add(xCur);

                    //crt new line
                    if (needCrtNewLine(xCur))
                    {
                        var newLine = new Span();
                        //lCur.Inlines.Add(new LineBreak());
                        newLine.Inlines.Add(new Run() { Text = string.Format("{0}. ", ++lineNumber) });
                        lCur.Inlines.Add(newLine);
                        lCur = newLine;
                    }

                    if (!isLeaf(xCur))
                    {
                        //go depth
                        curLevel++;
                        xStack.Push(xCur.FirstChild);
                        lStack.Push(lCur);
                    }
                    else
                    {
                        //is leaf
                        Inline converted = crtInline(xCur);
                        if (converted != null) lCur.Inlines.Add(converted);
                    }
                }
                else
                {
                    trace.Remove(xCur);
                    //if node traced ->goto sib or go up
                    if (xCur.NextSibling != null)
                    {
                        xCur = xCur.NextSibling;
                        //
                        if (needCrtNewLine(xCur))
                        {
                            var newLine = new Span();
                            lCur.Inlines.Add(new LineBreak());
                            lCur.Inlines.Add(newLine);
                            lCur = newLine;
                        }
                        //
                        xStack.Pop();
                        lStack.Pop();
                        xStack.Push(xCur);
                        lStack.Push(lCur);
                    }
                    else
                    {
                        //go up
                        curLevel--;
                        xStack.Pop();
                        lStack.Pop();
                    }
                }
            }

            //Paragraph p = new Paragraph();
            //p.Inlines.Add(lRoot);
            return lRoot;
        }

        private static bool isLeaf(HtmlNode xCur)
        {
            if (!xCur.HasChildNodes) return true;
            switch (xCur.Name)
            {
#if !dict_dist
                case "font":
#endif
                case "a":
                case "i":
                case "#text":
                    return true;
            }
            return false;
        }

        private static bool needCrtNewLine(HtmlNode xCur)
        {
            switch (xCur.Name)
            {
                case "li":
                    return true;
                default:
                    return false;
            }
        }
    }
}
