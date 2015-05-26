using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        const string replyToPattern = @"\[id(\d+)[:\|]";
        static Regex replyToRegex = new Regex(replyToPattern, RegexOptions.IgnoreCase);

        static void Main(string[] args)
        {
            //string pattern = @"(\d{3})-(\d{3}-\d{4})";
            //string input = "212-555-6666 906-932-1111 415-222-3333 425-888-9999";
            string pattern = @"\[id(\d+)[:\|]";
            string input = @"[id73232:bp-2742_53133|Мария] or blah blah blah [id3338353|Ольга] fooo bar baz";
            


            MatchCollection matches = Regex.Matches(input, pattern);

            foreach (Match match in matches)
            {
                Console.WriteLine("Id:        {0}", match.Groups[1].Value);
//                Console.WriteLine("Group:     {0}", match.Groups[2].Value);
                Console.WriteLine();
            }
            Console.WriteLine();

            long to = parseCommentForReplyTo(input);
            Console.WriteLine("to:     {0}", to);

            Console.ReadLine();
        }

        static long parseCommentForReplyTo(String text)
        {
            if (String.IsNullOrEmpty(text))
                return 0;

            MatchCollection matches = replyToRegex.Matches(text);
            if (matches.Count > 0)
            {
                String to = matches[0].Groups[1].Value;
                if (!String.IsNullOrEmpty(to))
                {
                    return long.Parse(to);
                }
            }
            return 0;
        }

    }
}
