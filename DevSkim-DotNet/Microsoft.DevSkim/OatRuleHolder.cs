namespace Microsoft.DevSkim
{
    public class ConvertedOatRule : CST.OAT.Rule
    {
        public ConvertedOatRule(string name, Rule rule): base (name)
        {
            Rule = rule;
        }

        public Rule Rule { get; }
    }
}
