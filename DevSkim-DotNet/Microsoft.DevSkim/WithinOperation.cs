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
        public WithinOperation(Analyzer analyzer) : base(Operation.Custom, analyzer)
        {
            _analyzer = analyzer;
            regexEngine = new RegexOperation(analyzer);
            CustomOperation = "Within";
            OperationDelegate = WithinOperationDelegate;
            ValidationDelegate = WithinValidationDelegate;
        }

        public OperationResult WithinOperationDelegate(Clause c, object? state1, object? _,
            IEnumerable<ClauseCapture>? captures)
        {
            if (c is WithinClause wc && state1 is TextContainer tc)
            {
                var passed =
                    new List<Boundary>();
                var failed =
                    new List<Boundary>();

                foreach (var capture in captures ?? Array.Empty<ClauseCapture>())
                {
                    if (capture is TypedClauseCapture<List<Boundary>> tcc)
                    {
                        foreach (var boundary in tcc.Result)
                        {
                            var boundaryToCheck = GetBoundaryToCheck();
                            if (boundaryToCheck is { })
                            {
                                var operationResult = ProcessLambda(boundaryToCheck);
                                if (operationResult.Result)
                                {
                                    passed.Add(boundary);
                                }
                                else
                                {
                                    failed.Add(boundary);
                                }
                            }

                            Boundary? GetBoundaryToCheck()
                            {
                                if (wc.FindingOnly)
                                {
                                    return boundary;
                                }

                                if (wc.SameLineOnly)
                                {
                                    var startInner = tc.LineStarts[tc.GetLocation(boundary.Index).Line];
                                    var endInner = tc.LineEnds[tc.GetLocation(startInner + (boundary.Length - 1)).Line];
                                    return new Boundary
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner + 1
                                    };
                                }

                                if (wc.SameFile)
                                {
                                    var startInner = tc.LineStarts[0];
                                    var endInner = tc.LineEnds[^1];
                                    return new Boundary
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner + 1
                                    };
                                }
                                if (wc.OnlyBefore)
                                {
                                    var startInner = tc.LineStarts[0];
                                    var endInner = boundary.Index;
                                    return new Boundary
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner + 1
                                    };
                                }
                                if (wc.OnlyAfter)
                                {
                                    var startInner = boundary.Index + boundary.Length;
                                    var endInner = tc.LineEnds[^1];
                                    return new Boundary
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner + 1
                                    };
                                }
                                else{
                                    var startLine = tc.GetLocation(boundary.Index).Line;
                                    // Before is already a negative number
                                    var startInner = tc.LineStarts[Math.Max(1, startLine + wc.Before)];
                                    var endInner = tc.LineEnds[Math.Min(tc.LineEnds.Count - 1, startLine + wc.After)];
                                    return new Boundary
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner + 1
                                    };
                                }
                                return null;
                            }
                        }
                    }

                    var passedOrFailed = wc.Invert ? failed : passed;
                    return new OperationResult(passedOrFailed.Any(),
                        passedOrFailed.Any()
                            ? new TypedClauseCapture<List<Boundary>>(wc, passedOrFailed.ToList())
                            : null);
                }

                OperationResult ProcessLambda(Boundary target)
                {
                    return _analyzer.GetClauseCapture(wc.SubClause, tc, target, captures);
                }
            }

            return new OperationResult(false);
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
                yield return new Violation($"Rule {rule.Name} has a null clause", rule);
                yield break;
            }
            if (clause is WithinClause wc)
            {
                if (!wc.OnlyAfter && !wc.OnlyBefore && !wc.FindingOnly && !wc.SameLineOnly && (wc.Before == 0 && wc.After == 0))
                {
                    yield return new Violation($"Either FindingOnly, SameLineOnly or some Combination of Before and After is required", rule, clause);
                }
                if (!wc.Data?.Any() ?? true)
                {
                    yield return new Violation($"Must provide some regexes as data.", rule, clause);
                    yield break;
                }
                foreach (var datum in wc.Data ?? new List<string>())
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

        private RegexOperation regexEngine;
        private readonly Analyzer _analyzer;
    }
}