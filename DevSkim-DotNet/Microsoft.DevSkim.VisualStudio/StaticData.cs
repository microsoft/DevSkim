namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class StaticData
    {
        // Maps file name to a dictionary of file versions to a deduplicated set of CodeFixMappings
        internal static ConcurrentDictionary<Uri, ConcurrentDictionary<int, HashSet<CodeFixMapping>>> FileToCodeFixMap { get; } = new ConcurrentDictionary<Uri, ConcurrentDictionary<int, HashSet<CodeFixMapping>>>();
    }
}
