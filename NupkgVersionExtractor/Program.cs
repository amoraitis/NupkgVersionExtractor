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

        static string GetValuesFromPath(string path, bool withName = false)
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

        static string GetValuesFromNupkg(string path, string packageName)
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
                .Select(ParseArgument)
                .ToDictionary(pair => pair.key, pair => pair.value);

            // Wrap the result in a case-insensitive dictionary.
            mapped = new Dictionary<string, string>(mapped, StringComparer.OrdinalIgnoreCase);

            // Read the normalized parameters (and their values).
            var withName = mapped.ContainsKey("withName");
            var scanNupkg = mapped.ContainsKey("scanNupkg");

            if (mapped.TryGetValue("path", out var path) == false)
            {
                throw new Exception("Parameter 'ServiceUser' must be supplied.");
            }

            return new ExtractionConfiguration
            {
                Path = path,
                WithName = withName,
                ScanNupkg = scanNupkg
            };
        }

        private static (string key, string value) ParseArgument(string argument)
        {
            // Remove white spaces around.
            argument = argument.Trim();

            if (argument[0] != '-')
            {
                throw new Exception($"Expected a parameter starting with '-' but found '{argument}'.");
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

                return (key, value);
            }

            return (argument, null);
        }
    }

    internal static class PathValidationExtensions
    {
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

    internal static class NugetExtensions
    {
        internal static Dictionary<string, string> GetNuspecContent(string path, string packageName)
        {
            var extractionPath = Path.Combine(Path.GetTempPath(), AppDomain.CurrentDomain.FriendlyName);
            ZipFile.ExtractToDirectory(path, extractionPath, true);
            var nuspecContent = File.ReadAllText(Path.Combine(extractionPath, $"{packageName}.nuspec"));

            var nuspecDictionary = XDocument
                .Parse(nuspecContent)
                .Root
                ?.Elements()
                .ElementAt(0)
                .Nodes()
                .Select(node => node as XElement)
                .ToDictionary(x=>x?.Name.LocalName, x=> x?.Value);

            nuspecDictionary?.Remove("dependencies");
            nuspecDictionary?.Remove("packageTypes");
            nuspecDictionary?.Remove("contentFiles");

            return nuspecDictionary;
        }
    }
}
