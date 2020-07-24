using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DevSkim
{
    class OatRuleHolder : CST.OAT.Rule
    {
        public OatRuleHolder(string name, Rule rule): base (name)
        {
            Rule = rule;
        }

        public Rule Rule { get; }
    }
}
