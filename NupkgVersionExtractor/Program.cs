using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NupkgVersionExtractor
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args[0] == null || File.Exists(args[0]) == false)
            {
                throw new ArgumentException("1st argument should be a valid path to a file.");
            }

            const string pattern = @"^(.*?)\.(?=(?:[0-9]+\.){2,}[0-9]+(?:-[a-z]+)?\.nupkg)(.*?)\.nupkg$";
            RegexOptions options = RegexOptions.Singleline;

            foreach (Match m in Regex.Matches(args[0], pattern, options))
            {
                var packageVersion = m.Groups?.Values?.Last()?.Value;

                Console.WriteLine(packageVersion ?? string.Empty);
            }
        }
    }
}
