// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Class to handle text as a searchable container
    /// </summary>
    public class TextContainer
    {
        /// <summary>
        ///     Creates new instance
        /// </summary>
        /// <param name="content"> Text to work with </param>
        public TextContainer(string content, string language, int lineNumber = -1)
        {
            Language = language;
            LineNumber = lineNumber;
            FullContent = content;
            Line = LineNumber == -1 ? FullContent : GetLineContent(lineNumber);
            LineEnds = new List<int>() { 0 };

            // Find line end in the text
            int pos = 0;
            while (pos > -1 && pos < FullContent.Length)
            {
                if (++pos < FullContent.Length)
                {
                    pos = FullContent.IndexOf("\n", pos, StringComparison.InvariantCultureIgnoreCase);
                    LineEnds.Add(pos);
                }
            }

            // Text can end with \n or not
            if (LineEnds[LineEnds.Count - 1] == -1)
                LineEnds[LineEnds.Count - 1] = (FullContent.Length > 0) ? FullContent.Length - 1 : 0;

            prefix = DevSkim.Language.GetCommentPrefix(Language);
            suffix = DevSkim.Language.GetCommentSuffix(Language);
            inline = DevSkim.Language.GetCommentInline(Language);
        }

        public string Language { get; set; }
        public int LineNumber { get; }
        public string FullContent { get; }
        public string Line { get; }
        string prefix;
        string suffix;
        string inline;
        /// <summary>
        ///     Returns the Boundary of a specified line number
        /// </summary>
        /// <param name="lineNumber"> The line number to return the boundary for </param>
        /// <returns> </returns>
        public Boundary GetBoundaryFromLine(int lineNumber)
        {
            Boundary result = new Boundary();

            if (lineNumber >= LineEnds.Count)
            {
                return result;
            }

            // Fine when the line number is 0
            var start = 0;
            if (lineNumber > 0)
            {
                start = LineEnds[lineNumber - 1] + 1;
            }
            result.Index = start;
            result.Length = LineEnds[lineNumber] - result.Index + 1;

            return result;
        }

        /// <summary>
        ///     Returns boundary for a given index in text
        /// </summary>
        /// <param name="index"> Position in text </param>
        /// <returns> Boundary </returns>
        public Boundary GetLineBoundary(int index)
        {
            Boundary result = new Boundary();

            for (int i = 0; i < LineEnds.Count; i++)
            {
                if (LineEnds[i] >= index)
                {
                    result.Index = (i > 0 && LineEnds[i - 1] > 0) ? LineEnds[i - 1] + 1 : 0;
                    result.Length = LineEnds[i] - result.Index + 1;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        ///     Return content of the line
        /// </summary>
        /// <param name="line"> Line number </param>
        /// <returns> Text </returns>
        public string GetLineContent(int line)
        {
            int index = LineEnds[line];
            Boundary bound = GetLineBoundary(index);
            return FullContent.Substring(bound.Index, bound.Length);
        }

        /// <summary>
        ///     Returns location (Line, Column) for given index in text
        /// </summary>
        /// <param name="index"> Position in text </param>
        /// <returns> Location </returns>
        public Location GetLocation(int index)
        {
            Location result = new Location();

            if (index == 0)
            {
                result.Line = 1;
                result.Column = 1;
            }
            else
            {
                for (int i = 0; i < LineEnds.Count; i++)
                {
                    if (LineEnds[i] >= index)
                    {
                        result.Line = i;
                        result.Column = index - LineEnds[i - 1];

                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///     Check whether the boundary in a text matches the scope of a search pattern (code, comment etc.)
        /// </summary>
        /// <param name="pattern"> Pattern with scope </param>
        /// <param name="boundary"> Boundary in a text </param>
        /// <param name="text"> Text </param>
        /// <returns> True if boundary is matching the pattern scope </returns>
        public bool ScopeMatch(IEnumerable<PatternScope> patterns, Boundary boundary)
        {
            if (patterns.Contains(PatternScope.All) || string.IsNullOrEmpty(prefix))
                return true;

            bool isInComment = IsBetween(FullContent, boundary.Index, prefix, suffix, inline);

            return !(isInComment && !patterns.Contains(PatternScope.Comment));
        }

        public List<int> LineEnds;

        public List<int> LineStarts { get; }

        /// <summary>
        ///     Return boundary defined by line and its offset
        /// </summary>
        /// <param name="line"> Line number </param>
        /// <param name="offset"> Offset from line number </param>
        /// <returns> Boundary </returns>
        private int BoundaryByLine(int line, int offset)
        {
            int index = line + offset;

            // We need the begining of the line when going up
            if (offset < 0)
                index--;

            if (index < 0)
                index = 0;
            if (index >= LineEnds.Count)
                index = LineEnds.Count - 1;

            return LineEnds[index];
        }

        /// <summary>
        ///     Checks if the index in the string lies between preffix and suffix
        /// </summary>
        /// <param name="text"> Text </param>
        /// <param name="index"> Index to check </param>
        /// <param name="prefix"> Prefix </param>
        /// <param name="suffix"> Suffix </param>
        /// <returns> True if the index is between prefix and suffix </returns>
        private bool IsBetween(string text, int index, string prefix, string suffix, string inline = "")
        {
            bool result = false;
            string preText = string.Concat(text.Substring(0, index));
            int lastPrefix = preText.LastIndexOf(prefix, StringComparison.InvariantCulture);
            if (lastPrefix >= 0)
            {
                int lastInline = preText.Substring(0, lastPrefix).LastIndexOf(inline, StringComparison.InvariantCulture);
                // For example in C#, If this /* is actually commented out by a //
                if (!(lastInline >= 0 && lastInline < lastPrefix && !preText.Substring(lastInline, lastPrefix - lastInline).Contains(Environment.NewLine)))
                {
                    var commentedText = text.Substring(lastPrefix);
                    int nextSuffix = commentedText.IndexOf(suffix, StringComparison.Ordinal);

                    // If the index is in between the last prefix before the index and the next suffix after that
                    // prefix Then it is commented out
                    if (lastPrefix + nextSuffix > index)
                        return true;
                }
            }
            if (!string.IsNullOrEmpty(inline))
            {
                int lastInline = preText.LastIndexOf(inline, StringComparison.InvariantCulture);
                if (lastInline >= 0)
                {
                    var commentedText = text.Substring(lastInline);
                    int endOfLine = commentedText.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                    if (endOfLine < 0)
                    {
                        endOfLine = commentedText.Length - 1;
                    }
                    if (index > lastInline && index < lastInline + endOfLine)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}