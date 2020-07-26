using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    class WithinOperation : OatOperation
    {
        private RegexOperation regexEngine;
        public WithinOperation(Analyzer analyzer) : base(Operation.Custom, analyzer)
        {
            regexEngine = new RegexOperation(analyzer);
            CustomOperation = "Within";
            OperationDelegate = (Clause c, object? state1, object? _, IEnumerable<ClauseCapture>? captures) =>
            {
                if (c is WithinClause wc && state1 is TextContainer tc)
                {
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
                    else
                    {
                        var start = tc.LineEnds[Math.Max(0, tc.LineNumber - (wc.Before + 1))];
                        var end = tc.LineEnds[Math.Min(tc.LineEnds.Count - 1, tc.LineNumber + wc.After)];
                        var process = ProcessLambda(tc.FullContent[start..end]);
                    }
                    // Subtracting before would give us the end of the line N before but we want the start so go back 1 more

                    OperationResult ProcessLambda(string target)
                    {
                        foreach (var pattern in c.Data.Select(x => regexEngine.StringToRegex(x)))
                        {
                            if (pattern?.IsMatch(target) is true)
                            {
                                return new OperationResult(!wc.Invert, null);
                            }
                        }
                        return new OperationResult(false, null);
                    }

                }
                return new OperationResult(false, null);
            };
            
        }
    }
}
