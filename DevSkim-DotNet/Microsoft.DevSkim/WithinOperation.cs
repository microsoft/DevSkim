using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    public class WithinOperation : OatOperation
    {
        private RegexOperation regexEngine;
        public WithinOperation(Analyzer analyzer) : base(Operation.Custom, analyzer)
        {
            regexEngine = new RegexOperation(analyzer);
            CustomOperation = "Within";
            OperationDelegate = WithinOperationDelegate;
            ValidationDelegate = WithinValidationDelegate;
        }

        public OperationResult WithinOperationDelegate(Clause c, object? state1, object? _, IEnumerable<ClauseCapture>? captures)
        {
            if (c is WithinClause wc && state1 is TextContainer tc)
            {
                var regexOpts = RegexOptions.Compiled;
                if (wc.Arguments.Contains("i"))
                {
                    regexOpts |= RegexOptions.IgnoreCase;
                }
                if (wc.Arguments.Contains("m"))
                {
                    regexOpts |= RegexOptions.Multiline;
                }
                if (wc.FindingOnly)
                {
                    foreach (var capture in captures ?? Array.Empty<ClauseCapture>())
                    {
                        if (capture is TypedClauseCapture<List<Match>> tcc)
                        {
                            return ProcessLambda(tcc.Result[0].Groups[0].Value);
                        }
                    }
                }
                else if (wc.SameLineOnly)
                {
                    var start = tc.LineEnds[Math.Max(tc.LineNumber - 1, 0)];
                    var end = tc.LineEnds[tc.LineNumber == -1 ? tc.LineEnds.Count - 1 : tc.LineNumber];
                    return ProcessLambda(tc.FullContent[start..end]);
                }
                else
                {
                    var start = tc.LineEnds[Math.Max(0, tc.LineNumber - (-wc.Before + 1))];
                    var end = tc.LineEnds[tc.LineNumber < 0 ? tc.LineEnds.Count - 1 : Math.Min(tc.LineEnds.Count - 1, tc.LineNumber + wc.After)];
                    return ProcessLambda(tc.FullContent[start..end]);
                }
                // Subtracting before would give us the end of the line N before but we want the start so go back 1 more

                OperationResult ProcessLambda(string target)
                {
                    foreach (var pattern in c.Data.Select(x => regexEngine.StringToRegex(x, regexOpts)))
                    {
                        if (pattern?.IsMatch(target) is true)
                        {
                            return new OperationResult(!wc.Invert, null);
                        }
                    }
                    return new OperationResult(wc.Invert, null);
                }
            }
            return new OperationResult(false, null);
        }

        public IEnumerable<Violation> WithinValidationDelegate(CST.OAT.Rule rule, Clause clause)
        {
            if (rule is null)
            {
                yield return new Violation($"Rule is null", new CST.OAT.Rule("RuleWasNull"));
                yield break;
            }
            if (clause is null)
            {
                yield return new Violation($"Rule {rule.Name} has a null clause",rule);
                yield break;
            }
            if (clause is WithinClause wc)
            {
                if (!wc.FindingOnly && !wc.SameLineOnly && (wc.Before == 0 && wc.After == 0))
                {
                    yield return new Violation($"Either FindingOnly, SameLineOnly or some Combination of Before and After is required", rule, clause);
                }
                if (!wc.Data?.Any() ?? true)
                {
                    yield return new Violation($"Must provide some regexes as data.", rule, clause);
                    yield break;
                }
                foreach(var datum in wc.Data ?? new List<string>())
                {
                    if (regexEngine.StringToRegex(datum, RegexOptions.None) is null)
                    {
                        yield return new Violation($"Regex {datum} in Rule {rule.Name} Clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} is not a valid regex.", rule, clause);
                    }
                }
            }
            else
            {
                yield return new Violation($"Rule {rule.Name ?? "Null Rule Name"} clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} is not a WithinClause", rule, clause);
            }
        }
    }
}
