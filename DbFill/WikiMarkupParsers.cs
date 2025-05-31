using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DbFill
{
    internal static class WikiMarkupParsers
    {
        internal static string ReadWikipediaLinkArticleName(string link)
        {
            string inner = link.Replace("[[", string.Empty).Replace("]]", string.Empty);

            if (inner.Contains('|'))
            {
                inner = inner.Split("|")[0];
            }

            return inner;
        }

        internal static string ReadWikipediaLinkDisplayText(string link)
        {
            string inner = link.Replace("[[", string.Empty).Replace("]]", string.Empty);

            if (inner.Contains("|"))
            {
                inner = inner.Split("|")[1];
            }

            return inner;
        }

        internal static bool IsLink(string text)
        {
            return text.Contains("[[") || text.Contains("]]");
        }

        internal static string FlattenLinks(this string text, out List<string> linkedPages)
        {
            string pattern = @"\[\[(.*?)\]\]";
            string newText = text;
            MatchCollection matches = Regex.Matches(text, pattern);

            linkedPages = [];

            Match match = Regex.Match(newText, pattern);
            while (match.Success)
            {
                string parsedValue = WikiMarkupParsers.ReadWikipediaLinkDisplayText(match.Value);
                linkedPages.Add(WikiMarkupParsers.ReadWikipediaLinkArticleName(match.Value));
                newText = newText.Substring(0, match.Index) + parsedValue + newText.Substring(match.Index + match.Length);

                match = Regex.Match(newText, pattern);
            }

            return newText;
        }

        public static string RemoveRefs(this string text)
        {
            string pattern = @"<ref(.*)<\/ref>";
            string newText = text;
            MatchCollection matches = Regex.Matches(text, pattern);

            Match match = Regex.Match(newText, pattern);
            while (match.Success)
            {
                newText = newText.Substring(0, match.Index) + newText.Substring(match.Index + match.Length);

                match = Regex.Match(newText, pattern);
            }

            return newText;
        }
    }
}
