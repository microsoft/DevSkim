namespace DevSkim.LanguageServer
{
    using OmniSharp.Extensions.LanguageServer.Protocol.Models;

    public class CodeFixMapping
    {
        public Diagnostic diagnostic { get; }
        public string replacement { get; }
        public string fileName { get; }
        public CodeFixMapping(Diagnostic diagnostic, string replacement, string fileName) {
            this.diagnostic = diagnostic;
            this.replacement = replacement;
            this.fileName = fileName;
        }
    }
}
