using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DevSkim
{
    class ConvertedOatRule : CST.OAT.Rule
    {
        public ConvertedOatRule(string name, Rule rule): base (name)
        {
            Rule = rule;
        }

        public Rule Rule { get; }
    }
}
