using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ApiTemplate.SharedKernel.PrimitivesExtensions
{
    public static class StringExtensions
    {
        public static string ToUpperFirst(this string text)
        {
            return text[0].ToString().ToUpper() + text[1..];
        }

        public static string ReplaceFirst(this string text, string search, string replace, int startIndex = 0, int endIndex = -1)
        {
            if (startIndex == -1)
            {
                return text;
            }

            int searchCount;
            if (endIndex > -1)
            {
                searchCount = endIndex - startIndex + 1;
            }
            else
            {
                searchCount = text.Length - startIndex + 1;
            }

            if (searchCount > text.Length)
                searchCount = text.Length;
            int pos = text.IndexOf(search, startIndex, searchCount);
            if (pos < 0)
            {
                return text;
            }
            var str = new StringBuilder();
            str.Append(text.Substring(0, pos));
            str.Append(replace);
            str.Append(text.Substring(pos + search.Length));
            return str.ToString();
        }

        public static string PhoneRemoveChars(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            Regex regex = new("\\d+");
            MatchCollection matches = regex.Matches(text);
            return string.Join("", matches.Select(x => x.Value).ToArray());
        }

        public static string PhoneAddPlus(this string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            return '+' + phoneNumber;
        }

        public static string GetCredsFromDbConnection(this string connection, string matchOn)
        {
            var indexOfStart = connection.LastIndexOf(matchOn);
            var db = connection[indexOfStart..];
            var indexOfEnd = db.Remove(db.IndexOf(";"));
            return indexOfEnd[(indexOfEnd.IndexOf("=") + 1)..];
        }

        public static List<string> GetSrcOfImages(this string html)
        {
            Regex regex = new("src=\"([^\"]+)\"");
            MatchCollection matches = regex.Matches(html);

            return matches.Select(x => x.Groups[1].Value).ToList();
        }

        public static string HideLastCharacters(this string original, int cntNotHide)
        {
            if (string.IsNullOrWhiteSpace(original)) return null;

            if (original.Length > cntNotHide) return original.Remove(cntNotHide).Insert(cntNotHide, "*****");
            else return original.Insert(original.Length, "*****");
        }

        public static string EngArticle(this string txt)
        {
            txt = txt.TrimStart();
            var _1 = txt[0].ToString().ToLower()[0];
            if (_1 == 'a' || _1 == 'e' || _1 == 'i' || _1 == 'o' || _1 == 'u' || _1 == 'y')
                return "an " + txt;
            else
                return "a " + txt;
        }
    }
}
