namespace Microsoft.DevSkim
{
    public class ConvertedOatRule : CST.OAT.Rule
    {
        public ConvertedOatRule(string name, Rule rule): base (name)
        {
            DevSkimRule = rule;
        }

        public Rule DevSkimRule { get; }
    }
}
