using System.Collections.Generic;

namespace Microsoft.CST.OAT.Captures
{
    /// <summary>
    /// The capture object that holds a rule and the clause captures
    /// </summary>
    public class RuleCapture
    {
        /// <summary>
        /// The constructor for a Rule Capture
        /// </summary>
        /// <param name="r">The Rule</param>
        /// <param name="captures">The ClauseCaptures</param>
        public RuleCapture(Rule r, List<ClauseCapture> captures)
        {
            Rule = r;
            Captures = captures;
        }

        /// <summary>
        /// The Rule this capture was triggered by
        /// </summary>
        public Rule Rule { get; }
        /// <summary>
        /// The ClauseCaptures associated with the Rule
        /// </summary>
        public List<ClauseCapture> Captures { get; }
    }
}
