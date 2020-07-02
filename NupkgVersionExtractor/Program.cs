using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NupkgVersionExtractor
{
    public class Program
    {
        static void Main(string[] args)
        {
            var extractionConfig = ExtractionConfiguration.FromArguments(args);
            var path = extractionConfig.Path.FailIfNotValid();

            Console.WriteLine(
                extractionConfig.ScanNupkg
                ? GetValuesFromNupkg(path, GetValuesFromPath(path, true).Split(" ")[0])
                : GetValuesFromPath(path, extractionConfig.WithName));
        }

        private static string GetValuesFromPath(string path, bool withName = false)
        {
            var result = string.Empty;
            const string pattern = @"^(.*?)\.(?=(?:[0-9]+\.){2,}[0-9]+(?:-[a-z]+)?\.nupkg)(.*?)\.nupkg$";
            var options = RegexOptions.Singleline;

            var m = Regex.Matches(path, pattern, options)[0];
            var packageVersion = m.Groups?.Values?.Last()?.Value;
            var packageName = m.Groups?.Values?.ElementAt(1)?.Value?.Split('\\').Last();

            if (withName)
            {
                result = packageName != null ? $"{packageName} " : string.Empty;
            }

            result += packageVersion ?? string.Empty;
            return result;
        }

        private static string GetValuesFromNupkg(string path, string packageName)
        {
            var nuspecContent = NugetExtensions.GetNuspecContent(path, packageName);

            return string.Join(Environment.NewLine, nuspecContent.Select(x => $"{x.Key} {x.Value}"));
        }
    }

    internal sealed class ExtractionConfiguration
    {
        public bool WithName { get; set; }
        public bool ScanNupkg { get; set; }
        public string Path { get; set; }

        public static ExtractionConfiguration FromArguments(string[] arguments)
        {
            // Parse the arguments.
            var mapped = arguments
                .Select(Parse)
                .ToDictionary(pair => pair.Item1, pair => pair.Item2, StringComparer.OrdinalIgnoreCase);

            // Read parameters' values.
            var withName = mapped.ContainsKey("withName");
            var scanNupkg = mapped.ContainsKey("scanNupkg");

            if (mapped.TryGetValue("path", out var path) == false)
            {
                throw new Exception("Parameter 'path' must be supplied.");
            }

            return new ExtractionConfiguration
            {
                Path = path,
                WithName = withName,
                ScanNupkg = scanNupkg
            };
        }

        private static Tuple<string, string> Parse(string argument)
        {
            argument = argument.Trim();

            if (argument[0] != '-')
            {
                throw new Exception($"Expected a parameter starting with '-' but '{argument}' found.");
            }

            // Get rid if the dash prefix.
            if (argument.Length > 1)
            {
                argument = argument[1..];
            }

            // Find the value separator.
            var valueSeparatorIndex = argument.IndexOf('=');

            if (valueSeparatorIndex > -1)
            {
                // Dissociate the key from the value.
                var key = argument.Substring(0, valueSeparatorIndex);
                var value = argument
                    .Substring(valueSeparatorIndex + 1, argument.Length - valueSeparatorIndex - 1)
                    .Trim('"');

                return new Tuple<string, string>(key, value);
            }

            return new Tuple<string, string>(argument, null);
        }
    }

    /// <summary>
    /// Extensions methods for <see cref="String"/> based paths.
    /// </summary>
    internal static class PathExtensions
    {
        /// <summary>
        /// Tests the validity of a file and fails if it doesn't exist.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The <paramref name="path"/> or throws if path is not valid..</returns>
        internal static string FailIfNotValid(this string path)
        {
            var bashPathInWin = Path.GetFullPath(path.Substring(1).Insert(1, ":"));
            var isBashPathInWin = File.Exists(bashPathInWin);
            var fileExists = File.Exists(path) || isBashPathInWin;

            if (fileExists == false)
            {
                throw new Exception($"{path} not valid or in invalid format.");
            }

            return isBashPathInWin ? bashPathInWin : path;
        }
    }

    /// <summary>
    /// Contains extension operations for nuget.
    /// </summary>
    internal static class NugetExtensions
    {
        /// <summary>
        /// Returns a dictionary containing a .nuspec's file contents.
        /// </summary>
        /// <param name="path">The path to the .nupkg file.</param>
        /// <param name="packageName">The name of the package to be parsed.</param>
        /// <returns></returns>
        /// <remarks>Advanced elements, like 'dependencies', are removed from the resulted dictionary.</remarks>
        internal static Dictionary<string, string> GetNuspecContent(string path, string packageName)
        {
            // Extract nuspec's content from .nupkg
            var extractionPath = Path.Combine(Path.GetTempPath(), AppDomain.CurrentDomain.FriendlyName);
            ZipFile.ExtractToDirectory(path, extractionPath, true);
            var nuspecContent = File.ReadAllText(Path.Combine(extractionPath, $"{packageName}.nuspec"));

            // Read nuspec key-values
            var nuspecDictionary = XDocument
                .Parse(nuspecContent)
                .Root
                ?.Elements()
                .ElementAt(0)
                .Nodes()
                .Select(node => node as XElement)
                .ToDictionary(x => x?.Name.LocalName, x => x?.Value);

            // Remove complex nuspec elements
            nuspecDictionary?.Remove("dependencies");
            nuspecDictionary?.Remove("packageTypes");
            nuspecDictionary?.Remove("contentFiles");

            return nuspecDictionary;
        }
    }
}
