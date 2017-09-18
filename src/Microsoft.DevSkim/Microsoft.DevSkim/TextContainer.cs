// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    public class TextContainer
    {
        public TextContainer(string content)
        {
            _content = content;
            _lineEnds = new List<int>();
            _lineEnds.Add(0);

            // Find line end in the text
            int pos = 0;
            while(pos > -1)
            {
                pos =_content.IndexOf('\n', pos+1);
                _lineEnds.Add(pos);
            }

            // Text can end with \n or not
            if (_lineEnds[_lineEnds.Count - 1] == -1)
                _lineEnds[_lineEnds.Count - 1] = (_content.Length > 0) ? content.Length - 1 : 0;
        }

        public List<Boundary> MatchPattern(SearchPattern pattern)
        {
            return MatchPattern(pattern, _content);
        }

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
                    matchList.Add(new Boundary() { Start = m.Index, End = m.Index + m.Length });
                }
            }

            return matchList;
        }

        public bool MatchPattern(SearchPattern pattern, Boundary boundary, string searchIn)
        {
            bool result = false;

            Boundary scope = ParseSearchBoundary(boundary, searchIn);

            string text = _content.Substring(scope.Start, scope.End - scope.Start);
            List<Boundary> macthes = MatchPattern(pattern, text);
            if (macthes.Count > 0)
                result = true;

            return result;
        }

        private Boundary ParseSearchBoundary(Boundary boundary, string searchIn)
        {
            // Default baundary is the fidning line
            Boundary result = GetLineBoundary(boundary.Start);
            string srch = (string.IsNullOrEmpty(searchIn)) ? string.Empty : searchIn.ToLower();

            if (srch == "finding-only")
            {
                result.Start = boundary.Start;
                result.End = boundary.End;
            }
            else if (srch.StartsWith("finding-region"))
            {
                int[] args;
                if (ParseSearchIn(srch, out args))
                {
                    Location loc = GetLocation(boundary.Start);
                    result.Start = BoundaryByLine(loc.Line, args[0]);
                    result.End = BoundaryByLine(loc.Line, args[1]);
                }
            }

            return result;
        }

        private bool ParseSearchIn(string searchIn, out int[] args)
        {
            bool result = false;
            List<int> arglist = new List<int>();

            Regex reg = new Regex(".*\\((.*),(.*)\\)");
            Match m = reg.Match(searchIn);
            if (m.Success)
            {
                result = true;
                foreach (Group group in m.Groups)
                {
                    int value;
                    if (int.TryParse(group.Value, out value))
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

        public Boundary GetLineBoundary(int index)
        {
            Boundary result = new Boundary();
            
            for(int i=0; i < _lineEnds.Count; i++)
            {
                if (_lineEnds[i] >= index)
                {
                    result.Start = (i > 0) ? _lineEnds[i - 1] : 0;
                    result.End = _lineEnds[i];
                    break;
                }
            }

            return result;
        }        

        public string GetLineContent(int line)
        {
            Boundary bound = GetLineBoundary(line);

            return _content.Substring(bound.Start, bound.End - bound.Start + 1);
        }

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

        private string _content;
        private List<int> _lineEnds;
    }
}
