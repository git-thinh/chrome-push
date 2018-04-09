using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace chrome_push
{
    public class Html2Text
    {
        private bool hasH1 = false, hasContentEnd = false;
        public string Convert(string html) {
            string result = string.Empty;

            HtmlDocument doc = null;
            doc = new HtmlDocument();
            doc.LoadHtml(html);

            StringWriter sw = new StringWriter();
            hasH1 = false;
            hasContentEnd = false;
            ConvertToText(doc.DocumentNode, sw);
            sw.Flush();
            result = sw.ToString();
            ////////int p = result.IndexOf("{H1}");
            ////////if (p > 0) { p += 4; result = result.Substring(p, result.Length - p).Trim(); }
            ////////if (!hasContentEnd)
            ////////{
            ////////    int pos_end = -1;
            ////////    for (int k = 0; k < TEXT_END.Length; k++)
            ////////    {
            ////////        pos_end = result.IndexOf(TEXT_END[k]);
            ////////        if (pos_end != -1)
            ////////        {
            ////////            result = result.Substring(0, pos_end);
            ////////            hasContentEnd = true;
            ////////            break;
            ////////        }
            ////////    }
            ////////}
            result = result.Replace(@"""", "”");

            result = string.Join(Environment.NewLine, result.Split(new char[] { '\r', '\n' })
                //.Select(x => x.Trim())
                .Where(x => x != string.Empty && !x.Contains("©"))
                .ToArray());

            return result;
        }


        private void ConvertToText(HtmlNode node, TextWriter outText)
        {
            if (hasContentEnd) return;

            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html));
                    }
                    break;

                case HtmlNodeType.Element:
                    bool isHeading = false, isList = false, isCode = false;
                    switch (node.Name)
                    {
                        case "pre":
                            isCode = true;
                            outText.Write("\r\n^\r\n");
                            break;
                        case "ol":
                        case "ul":
                            isList = true;
                            outText.Write("\r\n⌐\r\n");
                            break;
                        case "li":
                            outText.Write("\r\n● ");
                            break;
                        case "div":
                            outText.Write("\r\n");
                            //////if (hasH1 && !hasContentEnd)
                            //////{
                            //////    var css = node.getAttribute("class");
                            //////    if (css != null && css.Length > 0)
                            //////    {
                            //////        bool is_end_content = DIV_CLASS_END.Where(x => css.IndexOf(x) != -1).Count() > 0;
                            //////        if (is_end_content)
                            //////            hasContentEnd = true;
                            //////    }
                            //////}
                            break;
                        case "p":
                            outText.Write("\r\n");
                            break;
                        case "h2":
                        case "h3":
                        case "h4":
                        case "h5":
                        case "h6":
                            isHeading = true;
                            outText.Write("\r\n■ ");
                            break;
                        case "h1":
                            hasH1 = true;
                            //outText.Write("\r\n{H1}\r\n");
                            //outText.Write("§ ");
                            break;
                        case "img":
                            var src = node.getAttribute("src");
                            if (!string.IsNullOrEmpty(src))
                                outText.Write("\r\n¦" + src + "\r\n");

                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }

                    if (isHeading) outText.Write("\r\n");
                    if (isList) outText.Write("\r\n┘\r\n");
                    if (isCode) outText.Write("\r\nⱽ\r\n");

                    break;
            }
        }

        private string CleanHTMLFromScript(string str)
        {
            Regex re = new Regex("<script.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            str = re.Replace(str, string.Empty);
            re = new Regex("<script[^>]*>", RegexOptions.IgnoreCase);
            str = re.Replace(str, string.Empty);
            re = new Regex("<[a-z][^>]*on[a-z]+=\"?[^\"]*\"?[^>]*>", RegexOptions.IgnoreCase);
            str = re.Replace(str, string.Empty);
            re = new Regex("<a\\s+href\\s*=\\s*\"?\\s*javascript:[^\"]*\"[^>]*>", RegexOptions.IgnoreCase);
            str = re.Replace(str, string.Empty);
            return (str);
        }

        private void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                if (hasContentEnd) break;
                ConvertToText(subnode, outText);
            }
        }
    }
}
