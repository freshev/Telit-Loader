using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TelitLoader {

    class XmlProcessor {

        #region consts
        private const string UNEXPECTED_SYMBOL = "Unexpected symbol '{0}' at position {1}, line {2}";
        #endregion

        #region XmlExpects
        private enum XmlExpects {
            Any,
            Char,
            Digit,
            CharOrDigit,
            AfterTag,
            AfterQuotation,
            Equals,
            Quotation,
            Open,
            Close,
            CloseOrChar,
        }
        #endregion

        #region PreProcess
        public static string PreProcess(string s, bool IgnoreDocType = true, bool UseLastElement = false) {

            bool singleStarted = false, doubleStarted = false, commentStarted = false, scriptStarted = false;
            bool tagStarted = false;
            bool tagEnded = true;
            bool paramStarted = true;
            bool delayedTag = false;
            bool iTag = false;
            bool elStarted = false;
            bool elEnded = false;
            bool dQuote = false;

            Stack<string> tags = new Stack<string>();
            Stack<StringBuilder> elements = new Stack<StringBuilder>();
            XmlExpects expects = XmlExpects.Any;

            string tag = "";
            StringBuilder element = new StringBuilder();
            int position = 0;
            int line = 1;
            int streampos = 0;
            char[] cha = new char[5];


            using (StringReader sr = new StringReader(s)) {
                while (sr.Peek() > 0) {
                    sr.Read(cha, 0, 1);
                    char c = cha[0];
                    int b = c;

                    if (b == 13) { line++; position = 1; }
                    position++;
                    streampos++;

                    #region expects
                    switch (expects) {
                        case XmlExpects.Char:
                            if (!(c == '!' || c == '/' || c == '>' || c == '?' || Char.IsLetter(c))) continue;
                            break;
                        case XmlExpects.Digit:
                            if (!Char.IsDigit(c)) continue;
                            break;
                        case XmlExpects.CharOrDigit:
                            if (!(Char.IsLetter(c) || Char.IsDigit(c))) continue;
                            break;
                        case XmlExpects.AfterTag:
                            if (!(c == '=' || c == '>' || c == '/' || Char.IsLetter(c) || Char.IsDigit(c) || Char.IsWhiteSpace(c))) continue;
                            break;
                        case XmlExpects.AfterQuotation:
                            expects = XmlExpects.Char;
                            paramStarted = false;
                            if (!(c == '>' || c == '/' || c == '"' || c == '\'' || c == '?' || Char.IsWhiteSpace(c))) {
                                if (!doubleStarted) {
                                    element.Remove(element.Length - 1, 1).Append("&quot;");
                                    doubleStarted = true;
                                    continue;
                                }
                                if (!singleStarted) {
                                    element.Remove(element.Length - 1, 1).Append("&apos;");
                                    singleStarted = true;
                                    continue;
                                }
                            }
                            break;
                        case XmlExpects.Close:
                            if (!(c == '>')) continue;
                            break;
                        case XmlExpects.CloseOrChar:
                            if (!(c == '>' || Char.IsWhiteSpace(c) || Char.IsLetter(c) || Char.IsDigit(c))) continue;
                            break;
                        case XmlExpects.Quotation:
                            if (!(c == '"' || c == '\'' || Char.IsWhiteSpace(c))) {
                                if (tagStarted) {
                                    if (c == '_' || Char.IsLetter(c) || Char.IsDigit(c)) {
                                        // quote next statement
                                        element.Append('"');
                                        dQuote = true;
                                    } else throw new XmlException(string.Format(UNEXPECTED_SYMBOL, c, position, line));
                                } else throw new XmlException(string.Format(UNEXPECTED_SYMBOL, c, position, line));
                            }
                            break;
                    }
                    if (dQuote && (c == '<' || c == '>' || c == ' ' || c == '\t' || c == '/' || c == '=')) { element.Append('"'); dQuote = false; }
                    #endregion

                    element.Append(c);
                    //if (line == 310 && position == 185) { }
                    //if (element.ToString().Contains("1709")) { }

                    switch (b) {
                        case 39: // '
                            if (tagStarted) {
                                if (!doubleStarted) {
                                    if (singleStarted) expects = XmlExpects.AfterQuotation; else expects = XmlExpects.Any;
                                    singleStarted = !singleStarted;
                                }
                            }
                            break;
                        case 34: // "
                            if (tagStarted) {
                                if (!singleStarted) {
                                    if (doubleStarted) expects = XmlExpects.AfterQuotation; else expects = XmlExpects.Any;
                                    doubleStarted = !doubleStarted;
                                }
                            }
                            break;

                        case 38: // '&'

                            #region '&'
                            if (tagStarted) {
                                if (singleStarted || doubleStarted) {
                                    if (element.Length > 0) {
                                        if (!CheckSpecials(s, streampos)) element.Remove(element.Length - 1, 1).Append("&amp;");
                                    }
                                }
                            }
                            if (scriptStarted || (!commentStarted && !singleStarted && !doubleStarted)) {
                                if (element.Length > 0) {
                                    if (!CheckSpecials(s, streampos)) element.Remove(element.Length - 1, 1).Append("&amp;");
                                }
                            }
                            if (CheckSpecials(s, streampos)) {
                                if (streampos + 5 < s.Length && s.Substring(streampos, 5).ToLower().Equals("nbsp;")) {
                                    sr.Read(cha, 0, 5);
                                    position += 5;
                                    streampos += 5;
                                    if (element.Length > 0) {
                                        element.Remove(element.Length - 1, 1).Append(" ");
                                    }
                                }
                            }
                            if (dQuote) { element.Append('"'); dQuote = false; }
                            #endregion

                            break;

                        case 60: // '<'

                            #region '<'
                            if (!singleStarted && !doubleStarted && !commentStarted) {
                                if (streampos + 7 < s.Length && s.Substring(streampos, 7).ToLower().Equals("/script")) scriptStarted = false;
                            }
                            if (tagEnded && !singleStarted && !doubleStarted && !commentStarted && !scriptStarted) {
                                expects = XmlExpects.Char;
                                if (tagStarted) throw new XmlException(string.Format(UNEXPECTED_SYMBOL, c, position, line));
                                else { tagStarted = true; tagEnded = false; tag = ""; }
                                if (elStarted) throw new XmlException(string.Format(UNEXPECTED_SYMBOL, c, position, line));
                                else {
                                    if (element.Length > 1) elements.Push(element.Remove(element.Length - 1, 1));
                                    elStarted = true; elEnded = false; element = new StringBuilder("<");
                                }
                                if (streampos + 3 < s.Length && s.Substring(streampos, 3).Equals("!--")) {
                                    commentStarted = true;
                                    expects = XmlExpects.Any;
                                    tagStarted = false;
                                }
                            }
                            if (scriptStarted) {
                                if (streampos + 3 < s.Length && s.Substring(streampos, 3).Equals("!--")) {
                                    commentStarted = true;
                                    expects = XmlExpects.Any;
                                    tagStarted = false;
                                } else {
                                    if (element.Length > 0) {
                                        element.Remove(element.Length - 1, 1).Append("&lt;");
                                    }
                                }
                            } else if (singleStarted || doubleStarted) {
                                // replace "<" with "&lt;"
                                if (element.Length > 0) {
                                    element.Remove(element.Length - 1, 1).Append("&lt;");
                                }
                            }
                            #endregion

                            break;

                        case 62: // '>'

                            #region '>' 
                            bool delayedScriptStarted = false;
                            if (tagStarted && !singleStarted && !doubleStarted && !commentStarted && !scriptStarted) {
                                expects = XmlExpects.Any;

                                //if (element.ToString().Contains("fb:like:")) { }

                                if (!"".Equals(tag)) {
                                    if (!delayedTag) {
                                        if (tag.ToLower().Equals("script")) {
                                            delayedScriptStarted = true;
                                            // remove "async" from "<script async src="...">
                                            RemoveFromTag(tag, "async", ref element);
                                        }
                                        if (tag.ToLower().Equals("iframe")) {
                                            RemoveFromTag(tag, "allowfullscreen", ref element);
                                            XmlTextReader textReader = new XmlTextReader(new StringReader(element.ToString()));
                                            try {
                                                textReader.Read();
                                            } catch (XmlException ex) {
                                                if (ex.LineNumber == 1) {
                                                    element.Remove(ex.LinePosition - 1, element.ToString().Length - ex.LinePosition);
                                                }
                                            }
                                        }
                                        /*
                                        string[] sa = element.ToString().Split(new char[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
                                        Dictionary<string, string> attributes = new Dictionary<string, string>();
                                        for (int i = 1; i < sa.Length; i++) {
                                            string attr = "";
                                            string param = "";
                                            if (!sa[i].Contains("\"") && !sa[i].Contains("'")) {
                                                attr = sa[i];
                                                if (i + 1 < sa.Length && (sa[i + 1].Contains("\"") || !sa[i + 1].Contains("'"))) {
                                                    param = sa[i + 1];
                                                } else {
                                                    if (!tag.StartsWith("!")) param = "\"\"";
                                                }
                                                i++;
                                            }
                                            if (!"".Equals(attr)) {
                                                if (!attributes.ContainsKey(attr)) attributes.Add(attr, param);
                                            }
                                        }
                                        StringBuilder sb = new StringBuilder("<" + tag);
                                        foreach (string attr in attributes.Keys) {
                                            sb.Append(" ");
                                            sb.Append(attr);
                                            if (!"".Equals(attributes[attr])) { sb.Append("="); sb.Append(attributes[attr]); }
                                        }
                                        if(attributes.Count == 0) sb.Append(">");
                                        element = sb;
                                        */
                                        tags.Push(tag);
                                    } else {
                                        if (tag.ToLower().Equals("script")) scriptStarted = false;
                                        if (tags.ToArray().Contains(tag)) {
                                            string stacktag = tags.Pop();
                                            while (!stacktag.Equals(tag)) {
                                                elements.Push(new StringBuilder("</" + stacktag + ">"));
                                                if (tags.Count > 0) {
                                                    stacktag = tags.Pop();
                                                } else break;
                                            }
                                        } else {
                                            // ignore unknown closing tag
                                            iTag = true;
                                        }
                                    }
                                    tag = "";
                                    tagStarted = false;
                                }
                                tagEnded = true;
                                delayedTag = false;
                                //iTag = false;
                            }
                            if (elStarted && !singleStarted && !doubleStarted && !commentStarted && !scriptStarted) {
                                if (!elEnded) {
                                    if (!iTag) if (element.Length > 1 || element[0] != '<') elements.Push(element);
                                    element = new StringBuilder();
                                }
                                elStarted = false;
                                elEnded = true;
                                iTag = false;
                            }

                            if (delayedScriptStarted) scriptStarted = true;

                            if (commentStarted || scriptStarted) {
                                if (streampos - 3 > 0 && s.Substring(streampos - 3, 3).Equals("-->")) {
                                    commentStarted = false;
                                    tagStarted = false;
                                    tagEnded = true;
                                    delayedTag = false;
                                    iTag = false;
                                    if (element.Length > 1 || element[0] != '<') elements.Push(element);
                                    element = new StringBuilder();
                                    elStarted = false;
                                    elEnded = true;
                                    expects = XmlExpects.Any;
                                }
                            } else if (singleStarted || doubleStarted) {
                                // replace ">" with "&gt;"
                                if (element.Length > 0) {
                                    element = element.Remove(element.Length - 1, 1).Append("&gt;");
                                }
                            }
                            #endregion

                            break;

                        case 47: // '/'                            
                            if (!singleStarted && !doubleStarted && !commentStarted && !scriptStarted) {
                                if (tagStarted) {
                                    expects = XmlExpects.CloseOrChar;
                                    delayedTag = true;
                                    if (!"".Equals(tag)) {
                                        tags.Push(tag);
                                        expects = XmlExpects.Close;
                                    }
                                } // else expects = XmlExpects.Any;
                            }
                            break;

                        case 9:
                        case 32:
                        case 10:
                        case 13:
                            if (!singleStarted && !doubleStarted && !commentStarted && !scriptStarted) {
                                if (tagStarted && !delayedTag && !"".Equals(tag)) {
                                    delayedTag = false;
                                    tagEnded = true;
                                }
                            }
                            break;

                        case 61: // '=' paramStarted                            
                            if (tagStarted && !singleStarted && !doubleStarted && !commentStarted && !scriptStarted) {
                                paramStarted = true;
                                expects = XmlExpects.Quotation;
                            }
                            break;
                        case 45:
                            // replace '--' with '-' in comments, but not in scripts
                            if (commentStarted && !scriptStarted) {
                                if (streampos - 3 > 0 && streampos + 1 < s.Length &&
                                    !s.Substring(streampos - 3, 3).Equals("!--") && !s.Substring(streampos - 2, 3).Equals("-->") &&
                                    s.Substring(streampos - 2, 2).Equals("--")) {
                                    element.Remove(element.Length - 1, 1);
                                }
                            }
                            expects = XmlExpects.Any;
                            if (tagStarted && !tagEnded) {
                                tag += c;
                                expects = XmlExpects.AfterTag;
                            }
                            break;
                        case 58:
                            expects = XmlExpects.Any;
                            if (tagStarted && !paramStarted) {
                                // not allow ":" in attribute name
                                element.Remove(element.Length - 1, 1);
                                element.Append('_');
                            }

                            if (tagStarted && !tagEnded) {
                                tag += c;
                                expects = XmlExpects.AfterTag;
                            }
                            break;

                        default:
                            expects = XmlExpects.Any;
                            if (tagStarted && !tagEnded) {
                                tag += c;
                                expects = XmlExpects.AfterTag;
                            }
                            break;
                    }
                }
            }
            while (tags.Count > 0) {
                string popTag = tags.Pop();
                if (!"?xml".Equals(popTag.ToLower()) && !"!doctype".Equals(popTag.ToLower())) elements.Push(new StringBuilder("</" + popTag + ">"));
            }
            if (UseLastElement) elements.Push(element);

            StringBuilder[] revsa = elements.ToArray();
            StringBuilder retsb = new StringBuilder();
            for (int i = revsa.Length - 1; i >= 0; i--) {
                if (!IgnoreDocType || !revsa[i].ToString().ToLower().StartsWith("<!doctype")) retsb.Append(revsa[i]);
            }
            return retsb.ToString().Trim();
        }
        #endregion

        #region RemoveFromTag
        public static void RemoveFromTag(string tag, string attribute, ref StringBuilder element) {
            string el = element.ToString().ToLower();
            if (el.StartsWith("<" + tag + " " + attribute)) { element.Remove(1 + tag.Length, 1 + attribute.Length); return; }
            if (el.EndsWith(" " + attribute + ">")) { element.Remove(el.Length - 2 - attribute.Length, 1 + attribute.Length); return; }
            if (el.Contains(" " + attribute + " ")) { element.Remove(el.IndexOf(" " + attribute + " "), 1 + attribute.Length); return; }
        }
        #endregion

        #region Check & Replace Specials
        public static bool CheckSpecials(string s, int streampos) {
            if (streampos + 4 < s.Length && s.Substring(streampos, 4).ToLower().Equals("amp;")) return true;
            if (streampos + 5 < s.Length && s.Substring(streampos, 5).ToLower().Equals("quot;")) return true;
            if (streampos + 5 < s.Length && s.Substring(streampos, 5).ToLower().Equals("apos;")) return true;
            if (streampos + 5 < s.Length && s.Substring(streampos, 5).ToLower().Equals("nbsp;")) return true;
            if (streampos + 3 < s.Length && s.Substring(streampos, 3).ToLower().Equals("lt;")) return true;
            if (streampos + 3 < s.Length && s.Substring(streampos, 3).ToLower().Equals("gt;")) return true;
            return false;
        }
        public static string ReplaceCodesWithSpecials(string s) {
            StringBuilder sb = new StringBuilder();
            for (int streampos = 0; streampos < s.Length; streampos++) {
                if (s[streampos] == '&') {
                    streampos++;
                    if (streampos + 4 <= s.Length && s.Substring(streampos, 4).ToLower().Equals("amp;")) { sb.Append('&'); streampos += 3; } else if (streampos + 5 <= s.Length && s.Substring(streampos, 5).ToLower().Equals("quot;")) { sb.Append('"'); streampos += 4; } else if (streampos + 5 <= s.Length && s.Substring(streampos, 5).ToLower().Equals("apos;")) { sb.Append('\''); streampos += 4; } else if (streampos + 5 <= s.Length && s.Substring(streampos, 5).ToLower().Equals("nbsp;")) { sb.Append(' '); streampos += 4; } else if (streampos + 3 <= s.Length && s.Substring(streampos, 3).ToLower().Equals("lt;")) { sb.Append('<'); streampos += 2; } else if (streampos + 3 <= s.Length && s.Substring(streampos, 3).ToLower().Equals("gt;")) { sb.Append('>'); streampos += 2; } else sb.Append(s[--streampos]);
                } else sb.Append(s[streampos]);
            }
            return sb.ToString();
        }
        public static string ReplaceSpecialsWithCodes(string s) {
            StringBuilder sb = new StringBuilder();
            for (int streampos = 0; streampos < s.Length; streampos++) {
                if (s[streampos] == '&') sb.Append("&amp;");
                else if (s[streampos] == '"') sb.Append("&quot;");
                else if (s[streampos] == '\'') sb.Append("&apos;");
                else if (s[streampos] == ' ') sb.Append("&nbsp;");
                else if (s[streampos] == '<') sb.Append("&lt;");
                else if (s[streampos] == '>') sb.Append("&gt;");
                else sb.Append(s[streampos]);
            }
            return sb.ToString();
        }
        #endregion

        #region GetHrefs
        public static string[] GetA(XmlNode parentNode, string url = "") {
            List<string> sa = new List<string>();
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && (node.Name.ToLower().Equals("a") || node.Name.ToLower().Equals("link")) && node.Attributes.Count > 0) {
                    foreach (XmlAttribute attr in node.Attributes) {
                        if (attr.Name.ToLower().Equals("href") && attr.Value.ToLower().StartsWith(url.ToLower())) {
                            sa.Add(ReplaceCodesWithSpecials(node.OuterXml));
                        }
                    }
                }
                if (node.HasChildNodes) sa.AddRange(GetA(node, url));
            }
            return sa.ToArray();
        }
        public static string[] GetHrefs(XmlNode parentNode, string url = "") {
            List<string> sa = new List<string>();
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && node.Name.ToLower().Equals("a") && node.Attributes.Count > 0) {
                    foreach (XmlAttribute attr in node.Attributes) {
                        if (attr.Name.ToLower().Equals("href") && attr.Value.ToLower().StartsWith(url.ToLower())) { sa.Add(ReplaceCodesWithSpecials(attr.Value)); }
                    }
                }
                if (node.HasChildNodes) sa.AddRange(GetHrefs(node, url));
            }
            return sa.ToArray();
        }
        #endregion

        #region GetElement
        public static XmlNode GetElementByAttributeValue(XmlNode parentNode, string attributeValue, string attributeName = "class") {
            XmlNode retNode = null;
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && node.Attributes.Count > 0) {
                    foreach (XmlAttribute attr in node.Attributes) {
                        if (attr.Name.ToLower().Equals(attributeName.ToLower()) && attr.Value.Equals(attributeValue)) return node;
                    }
                }
                if (node.HasChildNodes) retNode = GetElementByAttributeValue(node, attributeValue, attributeName);
                if (retNode != null) return retNode;
            }
            return retNode;
        }

        public static XmlNode GetNextElementByAttributeValue(XmlNode parentNode, string attributeValue, string attributeName = "class") {
            XmlNode retNode = null;
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && node.Attributes.Count > 0) {
                    foreach (XmlAttribute attr in node.Attributes) {
                        if (attr.Name.ToLower().Equals(attributeName.ToLower()) && attr.Value.Equals(attributeValue) && node.FirstChild != null) return node.FirstChild;
                    }
                }
                if (node.HasChildNodes) retNode = GetNextElementByAttributeValue(node, attributeValue, attributeName);
                if (retNode != null) return retNode;
            }
            return retNode;
        }

        public static XmlNode[] GetElementsByAttributeValue(XmlNode parentNode, string attributeValue, string attributeName = "class") {
            List<XmlNode> retNodes = new List<XmlNode>();
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && node.Attributes.Count > 0) {
                    foreach (XmlAttribute attr in node.Attributes) {
                        if (attr.Name.ToLower().Equals(attributeName.ToLower()) && attr.Value.Equals(attributeValue)) retNodes.Add(node);
                    }
                }
                if (node.HasChildNodes) retNodes.AddRange(GetElementsByAttributeValue(node, attributeValue, attributeName));
            }
            return retNodes.ToArray();
        }

        public static XmlNode GetElementByAttributeName(XmlNode parentNode, string attributeName) {
            XmlNode retNode = null;
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && node.Attributes.Count > 0) {
                    foreach (XmlAttribute attr in node.Attributes) {
                        if (attr.Name.ToLower().Equals(attributeName.ToLower())) return node;
                    }
                }
                if (node.HasChildNodes) retNode = GetElementByAttributeName(node, attributeName);
                if (retNode != null) return retNode;
            }
            return retNode;
        }

        public static XmlNode[] GetElementsByAttributeName(XmlNode parentNode, string attributeName) {
            List<XmlNode> retNodes = new List<XmlNode>();
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && node.Attributes.Count > 0) {
                    foreach (XmlAttribute attr in node.Attributes) {
                        if (attr.Name.ToLower().Equals(attributeName.ToLower())) retNodes.Add(node);
                    }
                }
                if (node.HasChildNodes) retNodes.AddRange(GetElementsByAttributeName(node, attributeName));
            }
            return retNodes.ToArray();
        }


        public static XmlNode[] GetElementsByTagName(XmlNode parentNode, string tagName) {
            List<XmlNode> retNodes = new List<XmlNode>();
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && node.Name.ToLower().Equals(tagName.ToLower())) retNodes.Add(node);
                if (node.HasChildNodes) retNodes.AddRange(GetElementsByTagName(node, tagName));
            }
            return retNodes.ToArray();
        }
        public static XmlNode[][] GetElementsByTagName(XmlNode parentNode, string tagName, string nextTagName) {
            List<XmlNode[]> retNodes = new List<XmlNode[]>();
            foreach (XmlNode node in parentNode.ChildNodes) {
                if (node.NodeType == XmlNodeType.Element && node.Name.ToLower().Equals(tagName.ToLower())) {
                    if (node.FirstChild != null && node.FirstChild.Name.ToLower().Equals(nextTagName.ToLower())) retNodes.Add(new XmlNode[] { node, node.FirstChild });
                }
                if (node.HasChildNodes) retNodes.AddRange(GetElementsByTagName(node, tagName, nextTagName));
            }
            return retNodes.ToArray<XmlNode[]>();
        }
        #endregion

        #region GetAttribute
        public static string GetAttributeByName(XmlNode node, string attributeName) {
            if (node.NodeType == XmlNodeType.Element && node.Attributes.Count > 0) {
                foreach (XmlAttribute attr in node.Attributes) {
                    if (attr.Name.ToLower().Equals(attributeName.ToLower())) return attr.Value;
                }
            }
            return null;
        }
        #endregion
    }
}
