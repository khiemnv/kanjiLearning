#define dict_dist
#define sepate_kanji
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
//using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace test_guide
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class studyPg : Page
    {
        public studyPg()
        {
            this.InitializeComponent();
            Loaded += pgLoaded;
            srchBtn.Click += searchBtn_Click;
            myNode.OnHyberlinkClick += Hb_Click;
        }

        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            string txt = srchTxt.Text;
            search(txt);
        }

        const int m_limitContentLen = 3;
        const int m_limitContentCnt = 7;
        void search(string txt)
        {
            var ret = dict.Search(txt);
            rtb.Blocks.Clear();
            List<myWord> words = new List<myWord>();
            Span s = new Span();
            foreach (var kanji in ret)
            {
                Hyperlink hb = crtBlck(kanji.val);
                s.Inlines.Add(hb);
                var foundKanji = kanji.relatedWords.Find((w) => { return w.term == kanji.val.ToString(); });
                if (foundKanji != null) {
                    s.Inlines.Add(crtDefBlck(foundKanji.definitions[0]));
                }
                else {
                s.Inlines.Add(new Run { Text = string.Format("({0}) stroke {1}, radical ", kanji.hn, kanji.totalStrokes) });
                hb = crtBlck(kanji.radical.zRadical);
                s.Inlines.Add(hb);
                //s.Inlines.Add(new Run { Text = string.Format("({0}) ", kanji.radical.iRadical) });
                var rdInfo = dict.Search(kanji.radical.zRadical.ToString());
                if (rdInfo.Count > 0)
                {
                    var k = rdInfo[0];
                    if (k.hn != "") s.Inlines.Add(new Run { Text = string.Format("({0})", k.hn) });
                    if (k.simple != '\0') s.Inlines.Add(new Run { Text = string.Format(" simple {0}", k.simple) });
                }
                s.Inlines.Add(new Run { Text = ", meaning: " });
                foreach (var def in kanji.definitions)
                {
                    s.Inlines.Add(crtDefBlck(def));
                    break;
                }
                s.Inlines.Add(new LineBreak());
                }
                words.AddRange(kanji.relatedWords);
            }
            s.Inlines.Add(new LineBreak());
            //found word
            myWord found = null;
            if (ret.Count > 1) { found = words.Find((w) => { return w.term == txt; }); }
            var sFound = new Span();
            if (found != null)
            {
                s.Inlines.Add(crtWdBlck(found));
                s.Inlines.Add(new LineBreak());
                //remove from list
                words.Remove(found);
            }
            //related word
            if (found!=null) s.Inlines.Add(sFound);
            //s.Inlines.Add(new Run { Text = "related word:" });
            //s.Inlines.Add(new LineBreak());
            int count = 0;
            foreach (var rWd in words)
            {
                if (++count > m_limitContentCnt)
                {
                    s.Inlines.Add(crtWdBlck(rWd, true));
                }
                else
                s.Inlines.Add(crtWdBlck(rWd));
                if (txt.Contains(rWd.term)) continue;
                //s.Inlines.Add(new LineBreak());
                s.Inlines.Add(new LineBreak());
            }

            //create paragraph
            var p = new Paragraph();
            //if (found != null) p.Inlines.Add(sFound);
            p.Inlines.Add(s);
            rtb.Blocks.Add(p);
            //TextPointer pstart = rtb.ContentStart;
            //rtb.Select(pstart, pstart);
            //rtbScroll.VerticalScrollMode = ScrollMode.Enabled;
            //rtbScroll.BringIntoViewOnFocusChange = true;
            rtbScroll.ChangeView(0, 0, null);
            //rtbScroll.ScrollToVerticalOffset(0);
        }

        Span crtDefBlck(myDefinition def)
        {
            return crtDefBlck(def, m_limitContentLen);
        }
        Span crtDefBlck(myDefinition def, int limit)
        {
            if (def.bFormated)
            {
                var des = myNode.convert2(def.text, limit);
                return des;
            }
            else
            {
                var s = new Span();
                s.Inlines.Add(new Run() { Text = def.text });
                s.Inlines.Add(new LineBreak());
                return s;
            }
        }
        Span crtBlck(myRadical rd)
        {
            //言 Radical 149, speaking, speech
            var s = new Span();
            var r = new Run { Text = string.Format("{0} {1}", rd.zRadical, rd.iRadical) };
            s.Inlines.Add(r);
            var descr = crtDefBlck(rd.descr);
            s.Inlines.Add(descr);
            return s;
        }
        Span crtWdBlck(myWord wd, bool showBrift)
        {
            var s = new Span();
            if (showBrift) {
#if dict_dist
                if (!wd.definitions[0].bFormated)
#endif
                {
#if sepate_kanji
                    foreach (var kj in wd.term) s.Inlines.Add(crtBlck(kj));
#else
                s.Inlines.Add(crtHb(wd.term));
#endif
                    //s.Inlines.Add(new Run { Text = string.Format(" {0}", wd.hn) });
                    s.Inlines.Add(new LineBreak());
                }
                s.Inlines.Add(crtDefBlck(wd.definitions[0], 1));
            }
            return s;
        }
        Span crtWdBlck(myWord wd)
        {
            var s = new Span();
#if dict_dist
            if (!wd.definitions[0].bFormated)
#endif
            {
#if sepate_kanji
                foreach (var kj in wd.term) s.Inlines.Add(crtBlck(kj));
#else
                s.Inlines.Add(crtHb(wd.term));
#endif
                s.Inlines.Add(new Run { Text = string.Format(" {0}", wd.hn) });
                s.Inlines.Add(new LineBreak());
            }
            s.Inlines.Add(crtDefBlck(wd.definitions[0]));
            return s;
        }
        Block crtBlck(myKanji kanji)
        {
            var p = new Paragraph();
            var r = new Run { Text = string.Format("{2} {0} {1}",
                kanji.extraStrokes, kanji.totalStrokes, kanji.val) };
            var s = new Span();
            s.Inlines.Add(r);
            p.Inlines.Add(s);
            foreach(var df in kanji.definitions)
            {
                var tmp = crtDefBlck(df);
                p.Inlines.Add(new LineBreak());
                p.Inlines.Add(tmp);
            }
            foreach(var wd in kanji.relatedWords)
            {
                var tmp = crtWdBlck(wd);
            }
            return p;
        }

        myDict dict;
        private void pgLoaded(object sender, RoutedEventArgs e)
        {
            string txt = "<div id='dataarea'><font size='6' color='darkblue'><a href='#'>阿</a><a href='#'>保</a> a bảo</font><hr><ol><li>Bảo hộ nuôi nấng. ◇Hán Thư <a href='#'>漢</a><a href='#'>書</a>: <i>Hữu a bảo chi công, giai thụ quan lộc điền trạch tài vật</i> <a href='#'>有</a><a href='#'>阿</a><a href='#'>保</a><a href='#'>之</a><a href='#'>功</a>, <a href='#'>皆</a><a href='#'>受</a><a href='#'>官</a><a href='#'>祿</a><a href='#'>田</a><a href='#'>宅</a><a href='#'>財</a><a href='#'>物</a> (Tuyên đế kỉ <a href='#'>宣</a><a href='#'>帝</a><a href='#'>紀</a>) (Những người) có công bảo hộ phủ dưỡng, đều được nhận quan lộc ruộng đất nhà cửa tiền của.<li>Bảo mẫu (nữ sư dạy dỗ con cháu vương thất hay quý tộc).<li>Bề tôi thân cận, cận thần.  ◇Sử Kí <a href='#'>史</a><a href='#'>記</a>: <i>Cư thâm cung chi trung, bất li a bảo chi thủ</i> <a href='#'>居</a><a href='#'>深</a><a href='#'>宮</a><a href='#'>之</a><a href='#'>中</a>, <a href='#'>不</a><a href='#'>離</a><a href='#'>阿</a><a href='#'>保</a><a href='#'>之</a><a href='#'>手</a> (Phạm Thư Thái Trạch truyện <a href='#'>范</a><a href='#'>雎</a><a href='#'>蔡</a><a href='#'>澤</a><a href='#'>傳</a>) Ở trong thâm cung, không rời tay đám bề tôi thân cận.</li></li></li></ol><hr></div>";
            //var tmp = myNode.convert2(txt);
            //rtb.Blocks.Add(tmp);
            dict = myDict.Load();
            var ret = dict.Search("阿保");

#if false
            Paragraph p = new Paragraph();
            XmlDocument doc = new XmlDocument();
            //Windows.Data.Xml.Dom.XmlDocument doc = new Windows.Data.Xml.Dom.XmlDocument();
            doc.LoadXml(txt);
            List<Block> blocks = new List<Block>();
            {
                Block b = GenerateParagraph(doc.DocumentElement);
                blocks.Add(b);
            }
#else
            rtb.Blocks.Clear();
            Span s = new Span();
            foreach (var kanji in ret) {
                Hyperlink hb = crtBlck(kanji.val);
                s.Inlines.Add(hb);
                s.Inlines.Add(new Run { Text = string.Format(" {0} ({1}) ", kanji.totalStrokes, kanji.hn)});
                hb = crtBlck(kanji.radical.zRadical);
                s.Inlines.Add(hb);
                s.Inlines.Add(new Run { Text = string.Format(" {0} ", kanji.radical.iRadical) });
                var rdInfo = dict.Search(kanji.radical.zRadical.ToString());
                if (rdInfo.Count > 0) {
                    var k = rdInfo[0];
                    s.Inlines.Add(new Run { Text = string.Format("{2} {0} ({1})", k.totalStrokes, k.hn, k.simple) });
                }
                s.Inlines.Add(new LineBreak());
            }
            var p = new Paragraph();
            p.Inlines.Add(s);
            rtb.Blocks.Add(p);

            return;
            myNode node = myNode.convert(txt);
            List<Block> blocks = Properties.GenerateBlocksForHtml(txt);
#endif

            //Add the blocks to the RichTextBlock
            rtb.Blocks.Clear();
            foreach (Block b in blocks)
            {
                rtb.Blocks.Add(b);
            }
        }

        private Hyperlink crtBlck(char val)
        {
            var hb = new Hyperlink();
            hb.Click += Hb_Click;
            hb.AccessKey = val.ToString();
            hb.Inlines.Add(new Run() { Text = val.ToString() });
            return hb;
        }
        private Hyperlink crtHb(string txt)
        {
            var hb = new Hyperlink();
            hb.Click += Hb_Click;
            hb.AccessKey = txt;
            hb.Inlines.Add(new Run() { Text = txt });
            return hb;
        }

        private static string CleanText(string input)
        {
            string clean = Windows.Data.Html.HtmlUtilities.ConvertToText(input);
            //clean = System.Net.WebUtility.HtmlEncode(clean); 
            if (clean == "\0")
                clean = "\n";
            return clean;
        }
        private static Block GenerateParagraph(XmlNode node)
        {
            Paragraph p = new Paragraph();
            AddChildren(p, node);
            return p;
        }
        private static void AddChildren(Span s, XmlNode node)
        {
            bool added = false;

            foreach (XmlNode child in node.ChildNodes)
            {
                Inline i = GenerateBlockForNode(child);
                if (i != null)
                {
                    s.Inlines.Add(i);
                    added = true;
                }
            }
            if (!added)
            {
                s.Inlines.Add(new Run() { Text = CleanText(node.InnerText) });
            }
        }
        private static void AddChildren(Paragraph p, XmlNode node)
        {
            bool added = false;
            foreach (XmlNode child in node.ChildNodes)
            {
                Inline i = GenerateBlockForNode(child);
                if (i != null)
                {
                    p.Inlines.Add(i);
                    added = true;
                }
            }
            if (!added)
            {
                p.Inlines.Add(new Run() { Text = CleanText(node.InnerText) });
            }
        }
        private static Inline GenerateSpan(XmlNode node)
        {
            Span s = new Span();
            AddChildren(s, node);
            return s;
        }
        private static Inline GenerateInnerParagraph(XmlNode node)
        {
            Span s = new Span();
            s.Inlines.Add(new LineBreak());
            AddChildren(s, node);
            s.Inlines.Add(new LineBreak());
            return s;
        }
        private static Inline GenerateImage(XmlNode node)
        {
            Span s = new Span();
            //try
            {
                InlineUIContainer iui = new InlineUIContainer();
                var sourceUri = System.Net.WebUtility.HtmlDecode(node.Attributes["src"].Value);
                Image img = new Image() { Source = new BitmapImage(new Uri(sourceUri, UriKind.Absolute)) };
                img.Stretch = Stretch.Uniform;
                img.VerticalAlignment = VerticalAlignment.Top;
                img.HorizontalAlignment = HorizontalAlignment.Left;
                //img.ImageOpened += img_ImageOpened;
                //img.ImageFailed += img_ImageFailed;
                //img.Tapped += ScrollingBlogPostDetailPage.img_Tapped;
                iui.Child = img;
                s.Inlines.Add(iui);
                s.Inlines.Add(new LineBreak());
            }
            //catch (Exception ex)
            {
            }
            return s;
        }
        private static Inline GenerateHyperLink(XmlNode node)
        {
#if !use_link
#if use_link_btn
            Span s = new Span();
            InlineUIContainer iui = new InlineUIContainer();
            HyperlinkButton hb = new HyperlinkButton() {
                //NavigateUri = new Uri(node.Attributes["href"].Value, UriKind.Absolute),
                Content = CleanText(node.InnerText)
            };
            hb.Click += Hb_Click;

            //if (node.ParentNode != null && (node.ParentNode.Name == "li" || node.ParentNode.Name == "LI"))
            //    hb.Style = (Style)Application.Current.Resources["RTLinkLI"];
            //else if ((node.NextSibling == null || string.IsNullOrWhiteSpace(node.NextSibling.InnerText)) && (node.PreviousSibling == null || string.IsNullOrWhiteSpace(node.PreviousSibling.InnerText)))
            //    hb.Style = (Style)Application.Current.Resources["RTLinkOnly"];
            //else
            //    hb.Style = (Style)Application.Current.Resources["RTLink"];

            iui.Child = hb;
            s.Inlines.Add(iui);
            return s;
#else
            Hyperlink hb = new Hyperlink();
            //hb.Click += Hb_Click;
            hb.Inlines.Add(new Run() { Text = node.InnerText });
            //if (node.ParentNode != null && (node.ParentNode.Name == "li" || node.ParentNode.Name == "LI"))
            //    hb.Style = (Style)Application.Current.Resources["RTLinkLI"];
            //else if ((node.NextSibling == null || string.IsNullOrWhiteSpace(node.NextSibling.InnerText)) && (node.PreviousSibling == null || string.IsNullOrWhiteSpace(node.PreviousSibling.InnerText)))
            //    hb.Style = (Style)Application.Current.Resources["RTLinkOnly"];
            //else
            //    hb.Style = (Style)Application.Current.Resources["RTLink"];
            return hb;
#endif
#else
#if not_use_button
            return new Run() { Text = node.InnerText };
#else
            Span s = new Span();
            Button btn = new Button() { Content = node.InnerText };
            InlineUIContainer iui = new InlineUIContainer() { Child = btn};
            s.Inlines.Add(iui);
            return s;
#endif
#endif
        }

        private void Hb_Click(object sender, RoutedEventArgs e)
        {
            //
            var hb = (Hyperlink)sender;
            string text = ((Run)hb.Inlines[0]).Text;
            //text = hb.AccessKey;
            search(text);
        }

        private static Inline GenerateLI(XmlNode node)
        {
            Span s = new Span();
            InlineUIContainer iui = new InlineUIContainer();
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = new SolidColorBrush(Colors.Black);
            ellipse.Width = 6;
            ellipse.Height = 6;
            ellipse.Margin = new Thickness(-30, 0, 0, 1);
            iui.Child = ellipse;
            s.Inlines.Add(iui);
            AddChildren(s, node);
            s.Inlines.Add(new LineBreak());
            return s;
        }
        private static Inline GenerateBold(XmlNode node)
        {
            Bold b = new Bold();
            AddChildren(b, node);
            return b;
        }
        private static Inline GenerateUnderline(XmlNode node)
        {
            Underline u = new Underline();
            AddChildren(u, node);
            return u;
        }

        private static Inline GenerateItalic(XmlNode node)
        {
            Italic i = new Italic();
            AddChildren(i, node);
            return i;
        }
        private static Span GenerateH3(XmlNode node)
        {
            Span s = new Span();
            s.Inlines.Add(new LineBreak());
            Bold bold = new Bold();
            Run r = new Run() { Text = CleanText(node.InnerText) };
            bold.Inlines.Add(r);
            s.Inlines.Add(bold);
            s.Inlines.Add(new LineBreak());
            return s;
        }

        private static Inline GenerateH2(XmlNode node)
        {
            Span s = new Span() { FontSize = 24 };
            s.Inlines.Add(new LineBreak());
            Run r = new Run() { Text = CleanText(node.InnerText) };
            s.Inlines.Add(r);
            s.Inlines.Add(new LineBreak());
            return s;
        }

        private static Inline GenerateH1(XmlNode node)
        {
            Span s = new Span() { FontSize = 30 };
            s.Inlines.Add(new LineBreak());
            Run r = new Run() { Text = CleanText(node.InnerText) };
            s.Inlines.Add(r);
            s.Inlines.Add(new LineBreak());
            return s;
        }
        private static Inline GenerateSpanWNewLine(XmlNode node)
        {
            Span s = new Span();
            AddChildren(s, node);
            if (s.Inlines.Count > 0)
                s.Inlines.Add(new LineBreak());
            return s;
        }
        private static Inline GenerateIFrame(XmlNode node)
        {
            //try
            {
                Span s = new Span();
                s.Inlines.Add(new LineBreak());
                InlineUIContainer iui = new InlineUIContainer();
                WebView ww = new WebView() { Source = new Uri(node.Attributes["src"].Value, UriKind.Absolute), Width = Int32.Parse(node.Attributes["width"].Value), Height = Int32.Parse(node.Attributes["height"].Value) };
                iui.Child = ww;
                s.Inlines.Add(iui);
                s.Inlines.Add(new LineBreak());
                return s;
            }
            //catch (Exception ex)
            {
                return null;
            }
        }
        private static Inline GenerateUL(XmlNode node)
        {
            Span s = new Span();
            s.Inlines.Add(new LineBreak());
            AddChildren(s, node);
            return s;
        }
        private static Inline GenerateBlockForNode(XmlNode node)
        {
            switch (node.Name)
            {
                case "div":
                    return GenerateSpan(node);
                case "p":
                case "P":
                    return GenerateInnerParagraph(node);
                case "img":
                case "IMG":
                    return GenerateImage(node);
                case "a":
                case "A":
                    if (node.ChildNodes.Count >= 1 && (node.FirstChild.Name == "img" || node.FirstChild.Name == "IMG"))
                        return GenerateImage(node.FirstChild);
                    else
                        return GenerateHyperLink(node);
                case "li":
                case "LI":
                    return GenerateLI(node);
                case "b":
                case "B":
                    return GenerateBold(node);
                case "i":
                case "I":
                    return GenerateItalic(node);
                case "u":
                case "U":
                    return GenerateUnderline(node);
                case "br":
                case "BR":
                    return new LineBreak();
                case "span":
                case "Span":
                    return GenerateSpan(node);
                case "iframe":
                case "Iframe":
                    return GenerateIFrame(node);
                case "#text":
                    if (!string.IsNullOrWhiteSpace(node.InnerText))
                        return new Run() { Text = CleanText(node.InnerText) };
                    break;
                case "h1":
                case "H1":
                    return GenerateH1(node);
                case "h2":
                case "H2":
                    return GenerateH2(node);
                case "h3":
                case "H3":
                    return GenerateH3(node);
                case "ul":
                case "UL":
                    return GenerateUL(node);
                default:
                    return GenerateSpanWNewLine(node);
                    //if (!string.IsNullOrWhiteSpace(node.InnerText)) 
                    //    return new Run() { Text = CleanText(node.InnerText) }; 
                    //break; 
            }
            return null;
        }
    }
}
