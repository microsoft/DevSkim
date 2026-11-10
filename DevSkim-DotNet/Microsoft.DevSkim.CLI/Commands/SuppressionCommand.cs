// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DevSkim.CLI.Options;
using Microsoft.Extensions.Logging;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class SuppressionCommand
    {
        private readonly SuppressionCommandOptions _opts;
        private readonly Languages devSkimLanguages;
        private readonly ILoggerFactory _logFactory;
        private readonly ILogger<SuppressionCommand> _logger;

        public SuppressionCommand(SuppressionCommandOptions options)
        {
            _opts = options;
            _logFactory = _opts.GetLoggerFactory();
            _logger = _logFactory.CreateLogger<SuppressionCommand>();
            devSkimLanguages = !string.IsNullOrEmpty(options.LanguagesPath) && !string.IsNullOrEmpty(options.CommentsPath) ? DevSkimLanguages.FromFiles(options.CommentsPath, options.LanguagesPath) : DevSkimLanguages.LoadEmbedded();
        }

        public int Run()
        {
            SarifLog sarifLog = SarifLog.Load(_opts.SarifInput);
            if (sarifLog.Runs.Count > 0)
            {
                Run run = sarifLog.Runs[0];
                System.Collections.Generic.IEnumerable<IGrouping<Uri, Result>> groupedResults = run.Results.GroupBy(x => x.Locations[0].PhysicalLocation.ArtifactLocation.Uri);
                if (!_opts.ApplyAllSuppression && !_opts.FilesToApplyTo.Any() && !_opts.RulesToApplyFrom.Any())
                {
                    _logger.LogError("Must specify either apply all suppression comments or a combination of file and rules to apply");
                    return (int)ExitCode.CriticalError;
                }

                if (_opts.FilesToApplyTo.Any())
                {
                    groupedResults =
                        groupedResults.Where(grouping => _opts.FilesToApplyTo.Any(fileName => grouping.Key.ToString().Contains(fileName)));
                }

                groupedResults = groupedResults.ToList();
                foreach (IGrouping<Uri, Result> resultGroup in groupedResults)
                {
                    Uri fileName = resultGroup.Key;
                    string potentialPath = Path.Combine(_opts.Path, fileName.OriginalString);
                    var issueRecords = resultGroup
                      .Where(result => result.Locations is { })
                      .SelectMany(result => result.Locations, (x, y) => new { x.RuleId, y.PhysicalLocation })
                      .ToList();

                    // Exclude the issues that do not have any suppression matching the rules
                    if (_opts.RulesToApplyFrom.Any())
                    {
                        issueRecords = issueRecords.Where(x => _opts.RulesToApplyFrom.Any(y => y == x.RuleId)).ToList();
                    }

                    // No issues remain for file after filtering to specified rule ids, skip file
                    if (!issueRecords.Any())
                    {
                        continue;
                    }
                    
                    issueRecords
                    .Sort((a, b) => a.PhysicalLocation.Region.StartLine - b.PhysicalLocation.Region.StartLine);

                    var distinctIssueRecords = issueRecords
                    .GroupBy(x => x.PhysicalLocation.Region.StartLine)
                    .Select(x => new
                    {
                        PhysicalLocation = x.FirstOrDefault()?.PhysicalLocation,
                        RulesId = string.Join(",", x.Select(y => y.RuleId).Distinct())
                    });

                    if (!File.Exists(potentialPath))
                    {
                        _logger.LogError($"{potentialPath} specified in sarif does not appear to exist on disk.");
                        continue;
                    }
                    bool isXml = devSkimLanguages.FromFileNameOut(potentialPath, out LanguageInfo language) && language.Name == "xml";
                    string content;
                    Encoding encoding;
                    using (var reader = new StreamReader(potentialPath, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true))
                    {
                        content = reader.ReadToEnd();
                        encoding = reader.CurrentEncoding;
                    }
                    string[] theContent = SplitStringByLinesWithNewLines(content);
                    int currLine = 0;
                    StringBuilder sb = new StringBuilder();
                    var xmlComments = new HashSet<(int Line, string Comment)>();

                    foreach (var issueRecord in distinctIssueRecords)
                    {
                        if (issueRecord.PhysicalLocation is { })
                        {
                            Region region = issueRecord.PhysicalLocation.Region;
                            int zeroBasedStartLine = region.StartLine - 1;
                            string originalLine = theContent[zeroBasedStartLine];
                            int lineEndPosition = FindNewLine(originalLine);
                            // If line ends with `\` it may have continuation on the next line,
                            //  so put the comment at the line start and use multiline format
                            bool forceMultiLine = lineEndPosition >= 0 && originalLine[..lineEndPosition].EndsWith(@"\");
                            string ignoreComment = DevSkimRuleProcessor.GenerateSuppressionByLanguage(region.SourceLanguage, issueRecord.RulesId, _opts.PreferMultiline || forceMultiLine, _opts.Duration, _opts.Reviewer, devSkimLanguages);
                            if (!string.IsNullOrEmpty(ignoreComment))
                            {
                                if (isXml)
                                {
                                    xmlComments.Add((region.StartLine, ignoreComment));
                                }
                                foreach (string line in theContent[currLine..zeroBasedStartLine])
                                {
                                    sb.Append($"{line}");
                                }
                                
                                if (forceMultiLine)
                                {
                                    sb.Append($"{ignoreComment} {originalLine}");
                                }
                                else
                                {
                                    // Use the content then the ignore comment then the original newline characters from the extra array
                                    if (lineEndPosition != -1)
                                    {
                                        sb.Append($"{originalLine[0..lineEndPosition]} {ignoreComment}{originalLine[lineEndPosition..]}");
                                    }
                                    // No new line so we can just use the line as is
                                    else
                                    {
                                        sb.Append($"{originalLine} {ignoreComment}");
                                    }
                                }
                            }

                            currLine = zeroBasedStartLine + 1;
                        }
                    }

                    if (currLine < theContent.Length)
                    {
                        foreach (string line in theContent[currLine..^1])
                        {
                            sb.Append($"{line}");
                        }
                        sb.Append($"{theContent.Last()}");
                    }

                    string updatedContent = sb.ToString();
                    if (isXml && !ValidateXmlSuppressions(updatedContent, encoding, xmlComments, potentialPath))
                    {
                        return (int)ExitCode.CriticalError;
                    }

                    if (!_opts.DryRun)
                    {
                        if (isXml)
                        {
                            File.WriteAllText(potentialPath, updatedContent, encoding);
                        }
                        else
                        {
                            File.WriteAllText(potentialPath, updatedContent);
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"{potentialPath} will be changed from: {string.Join(Environment.NewLine, theContent)} to {sb.ToString()}");
                    }
                }
            }

            return (int)ExitCode.NoIssues;
        }

        private bool ValidateXmlSuppressions(string content, Encoding encoding,
            HashSet<(int Line, string Comment)> expectedComments, string path)
        {
            try
            {
                using var stream = new MemoryStream(encoding.GetBytes(content));
                using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                });
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.XmlDeclaration &&
                        reader.GetAttribute("encoding") is string declaredEncoding &&
                        !string.Equals(declaredEncoding, encoding.WebName, StringComparison.OrdinalIgnoreCase) &&
                        !(encoding.CodePage == Encoding.BigEndianUnicode.CodePage &&
                          string.Equals(declaredEncoding, "utf-16", StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogError("Cannot safely add XML suppressions to {Path}; XML declaration does not match the detected encoding. File unchanged.", path);
                        return false;
                    }
                    if (reader.NodeType == XmlNodeType.Comment && reader is IXmlLineInfo location)
                    {
                        expectedComments.Remove((location.LineNumber, $"<!--{reader.Value}-->"));
                    }
                }

                // Well-formed XML alone is insufficient: text inside CDATA is not a comment.
                if (expectedComments.Count == 0)
                {
                    return true;
                }
            }
            catch (XmlException ex)
            {
                _logger.LogError("Cannot safely add XML suppressions to {Path}; file unchanged: {Reason}", path, ex.Message);
                return false;
            }

            _logger.LogError("Cannot safely add XML suppressions to {Path}; generated text is not an XML comment on the finding's line. File unchanged.", path);
            return false;
        }

        /// <summary>
        /// Find the first location of a newline (\n or \r\n) in a string
        /// </summary>
        /// <param name="originalLine"></param>
        /// <returns>Character index of the first newline sequence or -1 if none found</returns>
        private int FindNewLine(string originalLine)
        {
            int indexOfNewLine = originalLine.IndexOf('\n');
            if (indexOfNewLine >= 1)
            {
                if (originalLine[indexOfNewLine - 1] == '\r')
                {
                    indexOfNewLine = indexOfNewLine - 1;
                }
            }

            return indexOfNewLine;
        }

        /// <summary>
        /// Split string into lines including the newline characters
        /// </summary>
        /// <param name="content"></param>
        /// <returns>Array of strings, each containing the content of one line from the input string including any newline characters from that line</returns>
        private string[] SplitStringByLinesWithNewLines(string content)
        {
            List<string> lines = new();
            int curPos = 0;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    lines.Add(content[curPos..(i+1)]);
                    curPos = i + 1;
                }

                if (i == content.Length - 1)
                {
                    lines.Add(content[curPos..(i+1)]);
                }
            }
            return lines.ToArray();
        }
    }
}
