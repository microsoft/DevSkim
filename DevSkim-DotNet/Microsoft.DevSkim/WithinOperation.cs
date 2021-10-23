﻿using Microsoft.CST.OAT;
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
                var passed = new List<Boundary>();
                foreach (var captureHolder in captures ?? Array.Empty<ClauseCapture>())
                {
                    if (captureHolder is TypedClauseCapture<List<Boundary>> tcc)
                    {
                        foreach (var capture in tcc.Result)
                        {
                            if (wc.FindingOnly)
                            {
                                passed.AddRange(ProcessLambda(tc.GetBoundaryText(capture), capture));
                            }
                            else if (wc.SameLineOnly)
                            {
                                var start = tc.LineStarts[tc.GetLocation(capture.Index).Line];
                                var end = tc.LineEnds[tc.GetLocation(start + capture.Length).Line];
                                passed.AddRange(ProcessLambda(tc.FullContent[start..end], capture));
                            }
                            else if (wc.SameFile)
                            {
                                var start = tc.LineStarts[0];
                                var end = tc.LineEnds[^1];
                                passed.AddRange(ProcessLambda(tc.FullContent[start..end], capture));
                            }
                            else if (wc.OnlyBefore)
                            {
                                var start = tc.LineStarts[0];
                                var end = capture.Index;
                                passed.AddRange(ProcessLambda(tc.FullContent[start..end], capture));
                            }
                            else if (wc.OnlyAfter)
                            {
                                var start = capture.Index + capture.Length;
                                var end = tc.LineEnds[^1];
                                passed.AddRange(ProcessLambda(tc.FullContent[start..end], capture));
                            }
                            else
                            {
                                var startLine = tc.GetLocation(capture.Index).Line;
                                // Before is already a negative number
                                var start = tc.LineEnds[Math.Max(0, startLine + wc.Before)];
                                var end = tc.LineEnds[Math.Min(tc.LineEnds.Count - 1, startLine + wc.After)];
                                passed.AddRange(ProcessLambda(tc.FullContent[start..end], capture));
                            }
                        }
                    }
                }
                return new OperationResult(passed.Any() ^ wc.Invert, passed.Any() ? new TypedClauseCapture<List<Boundary>>(wc, passed) : null);

                IEnumerable<Boundary> ProcessLambda(string target, Boundary targetBoundary)
                {
                    foreach (var pattern in wc.Data.Select(x => regexEngine.StringToRegex(x, regexOpts)))
                    {
                        if (pattern is Regex r)
                        {
                            var matches = r.Matches(target);
                            foreach (var match in matches)
                            {
                                if (match is Match m)
                                {
                                    Boundary translatedBoundary = new Boundary()
                                    {
                                        Length = m.Length,
                                        Index = targetBoundary.Index + m.Index
                                    };
                                    // Should return only scoped matches
                                    if (tc.ScopeMatch(wc.Scopes, translatedBoundary))
                                    {
                                        yield return translatedBoundary;
                                    }
                                }
                            }
                        }
                    }
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
    }
}