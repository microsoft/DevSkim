// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Class to handle text as a searchable container
    /// </summary>
    public class TextContainer
    {
        /// <summary>
        /// Creates new instance
        /// </summary>
        /// <param name="content">Text to work with</param>
        public TextContainer(string content, string language)
        {
            Language = language;
            _content = content;
            _lineEnds = new List<int>(){ 0 };            

            // Find line end in the text
            int pos = 0;
            while(pos > -1 && pos < _content.Length)
            {
                if (++pos < _content.Length)
                {
                    pos = _content.IndexOf('\n', pos);
                    _lineEnds.Add(pos);
                }
            }

            // Text can end with \n or not
            if (_lineEnds[_lineEnds.Count - 1] == -1)
                _lineEnds[_lineEnds.Count - 1] = (_content.Length > 0) ? content.Length - 1 : 0;
        }

        /// <summary>
        /// Returns all boundaries matching the pattern
        /// </summary>
        /// <param name="pattern">Search pattern</param>
        /// <returns>List of boundaries</returns>
        public List<Boundary> MatchPattern(SearchPattern pattern)
        {
            return MatchPattern(pattern, _content);
        }

        /// <summary>
        /// Returns all boundaries matching the pattern
        /// </summary>
        /// <param name="pattern">Search pattern</param>
        /// <param name="boundary">Content boundary</param>
        /// <param name="searchIn">Search in command</param>
        /// <returns></returns>
        public bool MatchPattern(SearchPattern pattern, Boundary boundary, SearchCondition condition)
        {
            bool result = false;

            Boundary scope = ParseSearchBoundary(boundary, condition.SearchIn);

            string text = _content.Substring(scope.Index, scope.Length);
            List<Boundary> matches = MatchPattern(pattern, text);
            if (matches.Count > 0)
                result = true;

            return result;
        }

        /// <summary>
        /// Returns location (Line, Column) for given index in text
        /// </summary>
        /// <param name="index">Position in text</param>
        /// <returns>Location</returns>
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
                for (int i = 0; i < _lineEnds.Count; i++)
                {
                    if (_lineEnds[i] >= index)
                    {
                        result.Line = i;
                        result.Column = index - _lineEnds[i - 1];

                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns boundary for a given index in text
        /// </summary>
        /// <param name="index">Position in text</param>
        /// <returns>Boundary</returns>
        public Boundary GetLineBoundary(int index)
        {
            Boundary result = new Boundary();
            
            for(int i=0; i < _lineEnds.Count; i++)
            {
                if (_lineEnds[i] >= index)
                {
                    result.Index = (i > 0 && _lineEnds[i - 1] > 0) ? _lineEnds[i - 1] + 1 : 0;
                    result.Length = _lineEnds[i] - result.Index + 1;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the Boundary of a specified line number
        /// </summary>
        /// <param name="lineNumber">The line number to return the boundary for</param>
        /// <returns></returns>
        public Boundary GetBoundaryFromLine(int lineNumber)
        {
            Boundary result = new Boundary();

            if (lineNumber >= _lineEnds.Count)
            {
                return result;
            }

            // Fine when the line number is 0
            var start = 0;
            if (lineNumber > 0)
            {
                start = _lineEnds[lineNumber - 1] + 1;
            }
            result.Index = start;
            result.Length = _lineEnds[lineNumber] - result.Index + 1;

            return result;
        }

        /// <summary>
        /// Return content of the line
        /// </summary>
        /// <param name="line">Line number</param>
        /// <returns>Text</returns>
        public string GetLineContent(int line)
        {
            int index = _lineEnds[line];
            Boundary bound = GetLineBoundary(index);
            return _content.Substring(bound.Index, bound.Length);
        }

        /// <summary>
        /// Returns all boundaries matching the pattern in a text
        /// </summary>
        /// <param name="pattern">Search pattern</param>
        /// <param name="text">Text</param>
        /// <returns>List of boundaries</returns>
        private List<Boundary> MatchPattern(SearchPattern pattern, string text)
        {
            List<Boundary> matchList = new List<Boundary>();

            RegexOptions reopt = RegexOptions.None;
            if (pattern.Modifiers != null && pattern.Modifiers.Length > 0)
            {
                reopt |= (pattern.Modifiers.Contains("i")) ? RegexOptions.IgnoreCase : RegexOptions.None;
                reopt |= (pattern.Modifiers.Contains("m")) ? RegexOptions.Multiline : RegexOptions.None;
            }

            Regex patRegx = new Regex(pattern.Pattern, reopt);
            MatchCollection matches = patRegx.Matches(text);
            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    Boundary bound = new Boundary() { Index = m.Index, Length = m.Length };
                        matchList.Add(bound);
                }
            }

            return matchList;
        }

        /// <summary>
        /// Check whether the boundary in a text matches the scope of a search pattern (code, comment etc.)
        /// </summary>
        /// <param name="pattern">Pattern with scope</param>
        /// <param name="boundary">Boundary in a text</param>
        /// <param name="text">Text</param>
        /// <returns>True if boundary is matching the pattern scope</returns>
        public bool ScopeMatch(SearchPattern pattern, Boundary boundary)
        {
            string prefix = DevSkim.Language.GetCommentPrefix(Language);
            string suffix = DevSkim.Language.GetCommentSuffix(Language);
            string inline = DevSkim.Language.GetCommentInline(Language);

            if (pattern.Scopes.Contains(PatternScope.All) || string.IsNullOrEmpty(prefix))
                return true;

            bool isInComment = (  IsBetween(Content, boundary.Index, prefix, suffix, inline)
                               || IsBetween(Content, boundary.Index, inline, "\n"));

            return !(isInComment && !pattern.Scopes.Contains(PatternScope.Comment));
        }

        /// <summary>
        /// Checks if the index in the string lies between preffix and suffix
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="index">Index to check</param>
        /// <param name="prefix">Prefix</param>
        /// <param name="suffix">Suffix</param>
        /// <returns>True if the index is between prefix and suffix</returns>
        private bool IsBetween(string text, int index, string prefix, string suffix, string inline = "")
        {
            bool result = false;
            string preText = string.Concat(text.Substring(0, index));
            int lastPrefix = preText.LastIndexOf(prefix, StringComparison.InvariantCulture);
            if (!string.IsNullOrEmpty(inline) && lastPrefix >= 0)
            {
                int lastInline = preText.Substring(0, lastPrefix).LastIndexOf(inline, StringComparison.InvariantCulture);

                // For example in C#, If this /* is actually commented out by a //
                if (lastInline < lastPrefix && !preText.Substring(lastInline,lastPrefix - lastInline).Contains('\n'))
                {
                    lastPrefix = 0;
                }
            }
            if (lastPrefix >= 0)
            {
                preText = text.Substring(lastPrefix);
                int lastSuffix = preText.IndexOf(suffix, StringComparison.Ordinal);
                if (lastSuffix + lastPrefix > index)
                    result = true;
            }

            return result;
        }

        /// <summary>
        /// Return boundary defined by line and its offset
        /// </summary>
        /// <param name="line">Line number</param>
        /// <param name="offset">Offset from line number</param>
        /// <returns>Boundary</returns>
        private int BoundaryByLine(int line, int offset)
        {
            int index = line + offset;

            // We need the begining of the line when going up
            if (offset < 0)
                index--;

            if (index < 0)
                index = 0;
            if (index >= _lineEnds.Count)
                index = _lineEnds.Count - 1;

            return _lineEnds[index];
        }

        /// <summary>
        /// Return boundary based on searchIn command and given boundary
        /// </summary>
        /// <param name="boundary">Relative boundary</param>
        /// <param name="searchIn">Search in command</param>
        /// <returns>Boundary</returns>
        private Boundary ParseSearchBoundary(Boundary boundary, string searchIn)
        {
            // Default boundary is the finding line
            Boundary result = GetLineBoundary(boundary.Index);
            string srch = (string.IsNullOrEmpty(searchIn)) ? string.Empty : searchIn.ToLower();

            if (srch == "finding-only")
            {
                result.Index = boundary.Index;
                result.Length = boundary.Length;
            }
            else if (srch.StartsWith("finding-region"))
            {
                if (ParseSearchIn(srch, out int[] args))
                {
                    Location loc = GetLocation(boundary.Index);
                    result.Index = BoundaryByLine(loc.Line, args[0]);
                    result.Length = BoundaryByLine(loc.Line, args[1]) - result.Index;
                }
            }

            return result;
        }

        /// <summary>
        /// Parse search in command and return arguments
        /// </summary>
        /// <param name="searchIn">text to ba parsed</param>
        /// <param name="args">arguments</param>
        /// <returns>True if parsing was succsessful</returns>
        private bool ParseSearchIn(string searchIn, out int[] args)
        {
            bool result = false;
            List<int> arglist = new List<int>();

            Regex reg = new Regex(".*\\((.*),(.*)\\)");
            Match m = reg.Match(searchIn);
            if (m.Success)
            {
                result = true;
                for (int i=1; i < m.Groups.Count; i++)
                {
                    if (int.TryParse(m.Groups[i].Value, out int value))
                    {
                        arglist.Add(value);
                    }
                    else
                    {
                        result = false;
                        break;
                    }
                }
            }

            args = arglist.ToArray();
            return result;
        }

        private List<int> _lineEnds;
        private string _content;
        public string Content {
            get
            {
                return _content;
            }
            private set{
                _content = value;
            }
        }
        public string Language { get; set; }
    }
}
