// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DevSkim
{
    public class TextContainer
    {
        public TextContainer(string content)
        {
            _content = content;
            _lineEnds = new List<int>();
            _lineEnds.Add(0);

            int pos = 0;
            while(pos > -1)
            {
                pos =_content.IndexOf('\n', pos+1);
                _lineEnds.Add(pos);
            }

            if (_lineEnds[_lineEnds.Count - 1] == -1)
                _lineEnds[_lineEnds.Count - 1] = (_content.Length > 0) ? content.Length - 1 : 0;

        }

        public Location GetLocation(int index)
        {
            Location result = new Location();

            for (int i = 0; i < _lineEnds.Count; i++)
            {
                if (_lineEnds[i] >= index)
                {
                    result.Line = i;                    
                    result.Column = index - _lineEnds[i - 1];
                    break;
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


        private string _content;
        private List<int> _lineEnds;
    }
}
