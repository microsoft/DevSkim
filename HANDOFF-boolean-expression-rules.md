# Handoff: boolean-expression rules for the SDL ruleset

**Delete this file as part of the work it describes.** It is a working note, not documentation.

## What this is

A follow-up to PR #780 ("Close SDL coverage gaps and fix rules that could never match"). That PR
took the shipped ruleset from 123 to 155 rules and moved DevSkim to ApplicationInspector 1.10.1.
One piece was designed but deliberately not included, because it could not pass CI at the time.
That blocker is now fixed upstream. This note contains the task, the artifact that was already
built, and the things that will otherwise cost you a day to rediscover.

This branch is stacked on `gfs-sdl-ruleset-gap-audit` (PR #780), so it already has the 1.10.1 bump
and all the new rules. If #780 has merged, rebase onto `main` instead.

## Why it was blocked, and why it no longer is

ApplicationInspector 1.10.1 shipped boolean expression support, but a rule that supplied both
`expression` and `conditions` behaved correctly at scan time while the rule verifier reported its
`must-not-match` samples as failures. DevSkim's CI gate (`DefaultRulesTests.ValidateDefaultRules`)
runs that verifier, so a correct rule could not merge.

The cause was in `RulesVerifier.SelfTestMatches`, which asked OAT whether the rule matched, whereas
`RuleProcessor` applies the authored expression as a per-finding predicate afterwards. The two
answered different questions.

**Fixed in ApplicationInspector 1.10.2** by
[#656](https://github.com/microsoft/ApplicationInspector/pull/656), which moves the capture
reduction into a shared `CaptureFilter` used by both paths. 1.10.2 was published 2026-09-10.

## The task

1. **Bump to 1.10.2.** Two `PackageReference` entries:
   - `DevSkim-DotNet/Microsoft.DevSkim/Microsoft.DevSkim.csproj` -> `Microsoft.CST.ApplicationInspector.RulesEngine`
   - `DevSkim-DotNet/Microsoft.DevSkim.CLI/Microsoft.DevSkim.CLI.csproj` -> `Microsoft.CST.ApplicationInspector.Logging`

2. **Confirm the fix is real before building on it.** Take the rule below, run `verify`, and
   confirm it passes. If it still fails on `curl --tlsv1.3 https://example.com`, stop: the fix did
   not land as expected and everything downstream is unsafe.

3. **Apply the merged `DS440016`** (given in full below). It replaces *both* existing `DS440016`
   entries in `rules/default/security/TLS/tls_generic.json` with one rule, retiring a duplicate ID.

4. **Write the soundness-gap rules**, which are the reason this feature matters. See below.

5. **Revisit `DS132784`** (Java XXE). It is currently `ManualReview` with six negated same-file
   conditions, which means "fires only when no hardening appears anywhere in the file". With
   condition disjunction it can be scoped to the factory rather than the file, which would justify
   raising it above `ManualReview`. Judgement call; the current form is deliberately unsound in the
   safe direction and that is defensible if the tighter form proves noisy.

6. **Changelog and cleanup.** Add an entry, and delete this file.

## Artifact 1: the merged DS440016

This was built and reviewed but never committed, because it could not pass the verifier under
1.10.1. The two existing `DS440016` entries in `tls_generic.json` are the same logical rule split
only because one pattern needs a guard: curl's `--tlsv1.x` flag is excused by an explicit
`--tlsv1.3` on the same line, the other seven patterns are not. That is exactly the case the fixed
shape could not express, and it is upstream's own worked example.

Replace both entries with this single rule:

```json
{
    "name": "Generic: Hard-coded SSL/TLS Protocol",
    "id": "DS440016",
    "description": "Generic: Hard-coded SSL/TLS Protocol",
    "recommendation": "Review to ensure that a TLS protocol agility is maintained.",
    "overrides": [ "DS440000" ],
    "does_not_apply_to": [ "json", "yaml" ],
    "tags": [ "Cryptography.Protocol.TLS.Hard-Coded" ],
    "confidence": "high",
    "severity": "ManualReview",
    "rule_info": "DS440001.md",
    "patterns": [
        { "pattern": "--(sslv2|sslv3|tlsv1\\.1|tlsv1\\.2|tlsv11|tlsv1)", "type": "regex", "scopes": [ "code" ], "_comment": "curl", "label": "curlFlag" },
        { "pattern": "--secure-protocol=\\S*", "type": "regex", "scopes": [ "code" ], "_comment": "wget", "label": "p0" },
        { "pattern": "CURL_SSLVERSION_(MAX_)?(SSL|TLS)v[0123_]+", "type": "regex", "scopes": [ "code" ], "_comment": "curl (library)", "label": "p1" },
        { "pattern": "ssl_protocols\\s+[^;]+;", "type": "regex", "scopes": [ "code" ], "_comment": "Nginx", "label": "p2" },
        { "pattern": "ssl_version", "type": "string", "scopes": [ "code" ], "label": "p3" },
        { "pattern": "DISABLE_SSL_([^\\b]+)", "type": "regex", "scopes": [ "code" ], "label": "p4" },
        { "pattern": "SSLProtocol\\s.+", "type": "regex", "scopes": [ "code" ], "_comment": "Apache", "label": "p5" },
        { "pattern": "sslEnabledProtocols\\s*=", "type": "regex", "scopes": [ "code" ], "_comment": "Generic", "label": "p6" }
    ],
    "conditions": [
        {
            "pattern": { "pattern": "--tlsv1.3", "type": "substring", "scopes": [ "code" ] },
            "negate_finding": false,
            "search_in": "same-line",
            "label": "tls13"
        }
    ],
    "expression": "(curlFlag AND NOT tls13) OR p0 OR p1 OR p2 OR p3 OR p4 OR p5 OR p6",
    "must-match": [
        "SSLProtocol -all +TLSv1",
        "conn = HTTPSConnection(host, ssl_version=3)",
        "curl --sslv3 https://example.com",
        "curl --tlsv1.1 https://example.com",
        "curl --tlsv1.2 https://example.com",
        "curl_easy_setopt(h, CURLOPT_SSLVERSION, CURL_SSLVERSION_TLSv1_1);",
        "export DISABLE_SSL_VERIFY=1",
        "sslEnabledProtocols = TLSv1.1",
        "ssl_protocols TLSv1 TLSv1.1;",
        "wget --secure-protocol=SSLv3 https://example.com"
    ],
    "must-not-match": [
        "curl --tlsv1.3 https://example.com",
        "wget https://example.com"
    ]
}
```

Note `"negate_finding": false`. The engine **rejects** a rule that sets `negate_finding: true` while
also supplying an `expression`; negation must live in the expression instead.

After applying, `DS440016` should appear once. The remaining deliberate duplicate is `DS148264`
(three entries split by language), which needs per-pattern `applies_to` and is **not** in scope
here.

## Artifact 2: the soundness-gap rules to write

These are the reason the feature matters, and none of them can be written correctly without it.

The expressible-today shape gives `P AND NOT C0 AND NOT C1`, which is
`P AND NOT (C0 OR C1)` -- "no mitigation present at all". Several requirements need
`P AND NOT (C0 AND C1)` -- "any required mitigation missing". The difference is the partially
hardened case, which the old shape silently misses.

Candidates, in rough value order:

- **Cookie flags.** `Set-Cookie` present, and not (`Secure` and `HttpOnly` and `SameSite`).
  A cookie with `Secure` but no `HttpOnly` is a real finding that DevSkim cannot report today.
- **HSTS.** `Strict-Transport-Security` present, and not (`max-age` sufficient and
  `includeSubDomains`).
- **`DocumentBuilderFactory` hardening.** A tightened companion to `DS132784`, scoped per factory
  rather than per file.

Check for free IDs before assigning; `DS610001+` and `DS200008+` were unused at the time of writing.

## Validation

The repo's `nuget.config` points at a private Azure DevOps feed. **Do not commit a modified
`nuget.config` or `.npmrc.pipeline`** -- CI depends on them and the repo instructions call this out
explicitly.

```bash
cd DevSkim-DotNet
dotnet test Microsoft.DevSkim.Tests/Microsoft.DevSkim.Tests.csproj
```

Baseline as of this branch: **364 tests, all passing**, on net8.0, net9.0 and net10.0.

Rule self-tests specifically, which is the gate that matters here:

```bash
dotnet DevSkim-DotNet/Microsoft.DevSkim.CLI/bin/Debug/net9.0/devskim.dll verify \
  -r rules/default \
  --languages DevSkim-DotNet/Microsoft.DevSkim/resources/languages.json \
  --comments  DevSkim-DotNet/Microsoft.DevSkim/resources/comments.json
```

Silence means success. Do not trust self-tests alone for a new rule: scan a realistic fixture and a
negative fixture of the recommended safe alternative. Every rule in PR #780 was checked that way,
and that is what caught the dead ones.

## Things that will cost you time if you rediscover them

**The `<EmbeddedResource>` allowlist is hand-maintained.** `Microsoft.DevSkim.csproj` lists rule
files explicitly rather than globbing. Adding a rule file without adding it there ships nothing,
and nothing fails. This is how four rules sat in the repo unshipped. Not an issue if you only edit
existing files, but check it if you add one:

```bash
python3 - <<'EOF'
import re, glob, os, json
c = open('DevSkim-DotNet/Microsoft.DevSkim/Microsoft.DevSkim.csproj').read()
emb = {m.group(1).replace('\\','/') for m in
       re.finditer(r'EmbeddedResource Include="\.\.\\\.\.\\(rules\\[^"]+)"', c)}
disk = {os.path.relpath(p).replace(os.sep,'/') for p in glob.glob('rules/default/**/*.json', recursive=True)}
print("not embedded:", sorted(disk - emb) or "none")
print("shipped rules:", sum(len(json.load(open(f))) for f in emb))
EOF
```

**Expression syntax constraints**, all confirmed empirically:

- No operator precedence. Folding is strictly left to right, so `a OR b AND c` means
  `(a OR b) AND c`. Parenthesise everything you mean.
- Mixing operators at one level without parentheses is rejected by validation, which is a good
  safety net but means you will see errors for expressions that look fine.
- Labels may not contain spaces or parentheses, and must be unique within the rule. Duplicate
  labels evaluate false.
- Nesting depth cap is 64. Not a practical limit.

**Boolean operators over pattern labels alone are nearly useless.** A finding comes from exactly one
pattern, so at any finding every *other* pattern label is false. Consequences, measured:

| expression | behaves as |
|---|---|
| `a AND b` | rejected outright by validation |
| `a AND NOT b` | identical to plain `a` |
| `a XOR b` | identical to `a OR b` |
| `a NAND b` | true whenever anything matches |

Only `OR` has distinct meaning over patterns, and the implicit pattern-OR already provided that.
**All the real expressiveness needs patterns and conditions in the same expression**, because a
condition is evaluated relative to the finding and can be independently true or false where a
sibling pattern cannot. Do not spend time on clever pattern-only algebra.

**When testing with `ApplicationInspector`'s own CLI, filter results by rule ID.** Its SARIF output
includes findings from ApplicationInspector's built-in rules, and ordinary test content like
`curl http://x` trips several of them. "The SARIF has results" is not evidence that your rule
matched. This produced a false conclusion during the original investigation and cost real time.

**Changelog is a required gate**, and the version heading must come from Nerdbank.GitVersioning
rather than being incremented by hand, because the patch number is the git height and drifts as
other PRs merge:

```bash
dotnet tool install --global nbgv    # once
nbgv get-version -v SimpleVersion    # run after committing, rebased on main
```

## Background reading

- PR #780 for the full audit and what shipped.
- ApplicationInspector #654 (the feature) and #656 (the verifier fix).
- `AppInspector.Tests/RuleProcessor/RuleExpressionBehaviourTests.cs` upstream is the best
  specification of the intended semantics; `PerPatternCondition_UnguardedPatternStillReports` is
  the `DS440016` case almost verbatim.
