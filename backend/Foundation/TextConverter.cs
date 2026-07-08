using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace BizSrt.Api.Foundation
{
    public static partial class Extensions
    {
        public static void WriteEndElement(this XmlWriter xmlWriter, string tag)
        {
            switch (tag)
            {
                case "img":
                case "br":
                case "hr":
                case "object":
                case "embed":
                    xmlWriter.WriteEndElement();
                    break;
                default:
                    xmlWriter.WriteFullEndElement();
                    break;
            }
        }

        public static void EnsureSpace(this StringBuilder textBuilder)
        {
            if (textBuilder.Length > 0 && textBuilder[textBuilder.Length - 1] != ' ')
                textBuilder.Append(' ');
        }

        public static void EnsureSpace(this string text)
        {
            if (text.Length > 0 && text[text.Length - 1] != ' ')
                text += ' ';
        }
    }

    public class TextConverter
    {
        //http://en.wikipedia.org/wiki/HTML_sanitization
        //http://en.wikipedia.org/wiki/HTML_sanitization
        public static void CheckHtml(BizSrt.Api.Model.IRichText richText, bool allowRelative)
        {
            var xHtml = richText.RichText.Trim()
                        .Replace("&nbsp;", "&#160;")
                        .Replace("<br>", "<br />")
                        .Replace("<p></p>", ""); //Empty paragraphs that may have been left behind by contenteditable are not rendered in Html (inline?). Explicit line breaks created by RTE would have been <p>&#160;</p>
            if (!string.IsNullOrEmpty(xHtml))
            {
                if (xHtml.Length < 7 || xHtml[0] != '<' || xHtml[xHtml.Length - 1] != '>')
                    xHtml = toHtml(xHtml, true, true);
                else if(!allowRelative)
                {
                    var regex = new System.Text.RegularExpressions.Regex("href=\"(?!http)");
                    if(regex.IsMatch(xHtml) || xHtml.Contains("href=\"/"))
                        throw new ArgumentException("Content may only contain absolute urls");
                }

                var textBuilder = new StringBuilder();
                //recursiveParse(xHtml, textBuilder);

                var xHtmlBuilder = new StringBuilder();
                using (var xmlWriter = XmlWriter.Create(xHtmlBuilder, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
                {
                    using (var xmlReader = XmlReader.Create(new StringReader(xHtml), new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment }))
                    {
                        //xmlReader.WhitespaceHandling = System.Xml.WhitespaceHandling.None;
                        xmlReader.MoveToContent();
                        string name;
                        bool elementRead;
                        var tags = new Stack<string>();
                        while (!(xmlReader.EOF || xmlReader.ReadState == ReadState.Error))
                        {
                            elementRead = false;
                            name = xmlReader.Name.ToLower();
                            switch (xmlReader.NodeType)
                            {
                                case XmlNodeType.Element:
                                    switch (name)
                                    {
                                        //http://en.wikipedia.org/wiki/HTML_sanitization
                                        case "script":
                                        //case "iframe":
                                        case "object":
                                        case "embed":
                                        case "link":
                                            xmlReader.ReadInnerXml();
                                            elementRead = true;
                                            break;
                                        default:
                                            if (xmlReader.IsStartElement())
                                            {
                                                xmlWriter.WriteStartElement(name);
                                                if (xmlReader.HasAttributes && xmlReader.AttributeCount > 0)
                                                {
                                                    while (xmlReader.MoveToNextAttribute())
                                                    {
                                                        switch (xmlReader.Name.ToLower())
                                                        {
                                                            case "id":
                                                            case "class":
                                                            case "name":
                                                                break;
                                                            default:
                                                                xmlWriter.WriteAttributeString(xmlReader.Name, xmlReader.Value);
                                                                break;
                                                        }
                                                    }
                                                    xmlReader.MoveToElement();
                                                }
                                                //http://stackoverflow.com/questions/241336/xmlreader-self-closing-element-does-not-fire-a-endelement-event
                                                if (!xmlReader.IsEmptyElement)
                                                    tags.Push(name);
                                                else
                                                    xmlWriter.WriteEndElement(name);
                                            }
                                            break;
                                    }
                                    break;
                                case XmlNodeType.EndElement:
                                    xmlWriter.WriteEndElement(name);
                                    name = tags.Pop();
                                    break;
                                case XmlNodeType.Text:
                                    textBuilder.EnsureSpace();
                                    textBuilder.Append(fromHtml(xmlReader.Value).Trim());
                                    xmlWriter.WriteString(xmlReader.Value); //toHtml(xmlReader.Value) - Assume that this is done on the client, otherwise would need to escape write Raw as in Foundation.RichText.writeText
                                    break;
                                case XmlNodeType.Whitespace:
                                    //textBuilder.EnsureSpace();
                                    break;
                            }

                            if (!elementRead)
                                xmlReader.Read();
                        }
                        if (tags.Count > 0)
                            throw new ArgumentOutOfRangeException("tags");
                    }
                    richText.Text = textBuilder.ToString();
                    xmlWriter.Flush();
                    richText.RichText = xHtmlBuilder.ToString();
                }
            }
        }

        /*public static void CheckHtml2(Model.IRichText richText)
        {
            var xHtml = richText.RichText.Trim().Replace("<br>", "<br />");
            if (!System.String.IsNullOrEmpty(xHtml))
            {
                if (!xHtml.StartsWith("<p", StringComparison.OrdinalIgnoreCase) || !xHtml.EndsWith("p>", StringComparison.OrdinalIgnoreCase))
                    xHtml = "<p>" + xHtml + "</p>";

                var textBuilder = new StringBuilder();
                //recursiveParse(xHtml, textBuilder);

                var xHtmlBuilder = new StringBuilder();
                using (var xmlWriter = XmlWriter.Create(xHtmlBuilder, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
                {
                    using (var xmlReader = XmlReader.Create(new StringReader(xHtml), new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
                    {
                        //xmlReader.WhitespaceHandling = System.Xml.WhitespaceHandling.None;
                        xmlReader.MoveToContent();
                        var tags = new Stack<string>();
                        CopyXml(xmlReader, xmlWriter, textBuilder, tags);
                        if (tags.Count > 0)
                            throw new ArgumentOutOfRangeException("tags");
                    }
                    richText.Text = textBuilder.ToString();
                    xmlWriter.Flush();
                    richText.RichText = xHtmlBuilder.ToString();
                }
            }
        }

        private class xAttribute
        {
            public xAttribute(string l, string n, string v, string p)
            {
                LocalName = l;
                Namespace = n;
                Value = v;
                Prefix = p;
            }

            public string LocalName = string.Empty;
            public string Namespace = string.Empty;
            public string Value = string.Empty;
            public string Prefix = string.Empty;
        }

        //http://www.hanselman.com/blog/StrippingOutEmptyXmlElementsInAPerformantWayAndTheBusFactor.aspx
        //Not quite working
        internal static void CopyXml(XmlReader xmlReader, XmlWriter xmlWriter, StringBuilder textBuilder, Stack<string> tags)
        {
            int depth = xmlReader.Depth;

            while (true && !xmlReader.EOF)
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Text:
                        textBuilder.EnsureSpace();
                        textBuilder.Append(fromHtml(xmlReader.Value).Trim());
                        xmlWriter.WriteString(xmlReader.Value); //toHtml(xmlReader.Value) - Assume that this is done on the client, otherwise would need to escape write Raw as in Foundation.RichText.writeText
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        //textBuilder.EnsureSpace();
                        //xmlWriter.WriteWhitespace(xmlReader.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        xmlWriter.WriteEntityRef(xmlReader.Name);
                        break;
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        xmlWriter.WriteProcessingInstruction(xmlReader.Name, xmlReader.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        xmlWriter.WriteDocType(xmlReader.Name,
                            xmlReader.GetAttribute("PUBLIC"), xmlReader.GetAttribute("SYSTEM"),
                            xmlReader.Value);
                        break;
                    case XmlNodeType.Comment:
                        xmlWriter.WriteComment(xmlReader.Value);
                        break;
                    case XmlNodeType.EndElement:
                        if (depth > xmlReader.Depth)
                            return;
                        break;
                }

                if (xmlReader.EOF) 
                    return;
                else if (xmlReader.IsStartElement())
                {
                    string name = xmlReader.Name.ToLower();
                    string localName = xmlReader.LocalName;
                    string prefix = xmlReader.Prefix;
                    string uri = xmlReader.NamespaceURI;
                    List<xAttribute> attributes = null;

                    if (xmlReader.IsEmptyElement)
                    { 
                        switch(name)
                        {
                            case "br":
                                xmlWriter.WriteStartElement(name);
                                xmlWriter.WriteEndElement(name);
                                break;
                        }
                        return;
                    }

                    switch (name)
                    {
                        //http://en.wikipedia.org/wiki/HTML_sanitization
                        case "script":
                        case "object":
                        case "embed":
                        case "link":
                            xmlReader.ReadInnerXml();
                            return;
                    }

                    if (xmlReader.HasAttributes)
                    {
                        attributes = new List<xAttribute>();
                        while (xmlReader.MoveToNextAttribute())
                        {
                            switch (xmlReader.Name.ToLower())
                            {
                                case "id":
                                case "class":
                                case "name":
                                    break;
                                default:
                                    attributes.Add(new xAttribute(xmlReader.LocalName, xmlReader.NamespaceURI, xmlReader.Value, xmlReader.Prefix));
                                    //xmlWriter.WriteAttributeString(xmlReader.Name, xmlReader.Value);
                                    break;
                            }
                        }
                    }

                    bool CData = false;
                    tags.Push(name);
                    xmlReader.Read();

                    if (xmlReader.NodeType == XmlNodeType.CDATA)
                    {
                        CData = true;
                        if(xmlReader.Value.Length == 0)
                            xmlReader.Read();
                    }
                    else if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name.Equals(name))
                    {
                        xmlReader.Read();

                        if (xmlReader.Depth < depth)
                            return;
                        else
                            continue;
                    }

                    xmlWriter.WriteStartElement(prefix, localName, uri);

                    if (attributes != null && attributes.Count > 0)
                    {
                        foreach (var a in attributes)
                            xmlWriter.WriteAttributeString(a.Prefix, a.LocalName, a.Namespace, a.Value);
                    }

                    if (xmlReader.IsStartElement())
                    {
                        if (xmlReader.Depth > depth)
                            CopyXml(xmlReader, xmlWriter, textBuilder, tags);
                        else
                            continue;
                    }
                    else
                    {
                        if (CData)
                            xmlWriter.WriteCData(xmlReader.Value);
                        else
                            xmlWriter.WriteString(xmlReader.Value);

                        xmlReader.Read();
                    }

                    xmlWriter.WriteFullEndElement();
                    xmlReader.Read();
                    tags.Pop();
                }
            }
        }*/

        /*Recursive
        public static void recursiveParse(string xHtml, StringBuilder textBuilder)
        {
            if (!System.String.IsNullOrWhiteSpace(xHtml))
            {
                var parser = new System.Xml.XmlTextReader(xHtml, System.Xml.XmlNodeType.Element, null);
                parser.WhitespaceHandling = System.Xml.WhitespaceHandling.None;
                parser.MoveToContent();
                while (!parser.EOF)
                {
                    if (parser.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        if (!parser.IsEmptyElement)
                        {
                            parse(parser.ReadInnerXml(), textBuilder);
                        }
                        else
                            parser.Read();
                    }
                    else if (parser.NodeType == System.Xml.XmlNodeType.Text)
                    {
                        textBuilder.Append(parser.Value);
                        parser.Read();
                    }
                    else
                        parser.Read();
                }
            }
        }*/

        public static string? VarcharMax(string? value)
        {
            return Varchar(value, 8000);
        }

        public static string? Varchar(string? value, int size)
        {
            if (value != null && value.Length > 0)
            {
                if (value.Length <= size)
                    return value;
                else
                    return value.Substring(0, size);
            }
            return null;
        }

        public static string? Normalize(string text)
        {
            return Normalize(text, WordBreaker.ParseOptions.Sentence);
        }

        public static string RichText(string richText, string text)
        {
            return string.IsNullOrWhiteSpace(richText) ? string.Format("<p>{0}</p>", text): richText;
        }

        public static string? Normalize(string text, WordBreaker.ParseOptions parseOptions)
        {
            var words = (List<string>)WordBreaker.Parse(text, parseOptions);
            if (words.Count > 1)
            {
                var sb = new StringBuilder(words[0]);
                for (int i = 1; i < words.Count; i++)
                {
                    sb.Append(' ');
                    sb.Append(words[i]);
                }
                return sb.ToString();
            }
            else if (words.Count == 1)
                return words[0];
            else
                return null;
        }

        //Foundation.RichText.textFromHtml
        static string? fromHtml(string? value)
        {
            //remove line breaks and collapse spaces
            return value != null ? System.Text.RegularExpressions.Regex.Replace(value, @"[\s]+", " ") : value;
        }

        //replace line breaks with <br />, etc
        static string? toHtml(string? value, bool escape, bool wrap)
        {
            if (value != null)
            {
                value = value.Trim();

                if (value.Length > 0)
                {
                    if (value[0] != '<' || value[value.Length - 1] != '>')
                        wrap = escape = false;

                    if (escape)
                        value = escapeXml(value.Trim());

                    //remove line breaks and collapse spaces
                    //return value != null ? value.Replace(Environment.NewLine, "<br />") : value; //when pasting multiline text from notepad it seems to be separated with "\r" only
                    //http://stackoverflow.com/questions/238002/replace-line-breaks-in-a-string-c-sharp/8196219#8196219
                    value = System.Text.RegularExpressions.Regex.Replace(value, @"\r\n?|\n", "<br />");

                    if (wrap)
                        value = "<p>" + value + "</p>";
                }
            }
            return value;
        }

        static string escapeXml(string value)
        {
            //http://msdn.microsoft.com/en-us/library/system.xml.xmlwriter.writestring.aspx
            //http://weblogs.sqlteam.com/mladenp/archive/2008/10/21/Different-ways-how-to-escape-an-XML-string-in-C.aspx
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;")/*.Replace("'", "&apos;")*/;
        }
    }
}
