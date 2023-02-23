namespace DevSkim.LanguageServer
{
    using OmniSharp.Extensions.LanguageServer.Protocol.Models;

    public class CodeFixMapping
    {
        private Diagnostic diagnostic;
        private string replacement;
        private string filename;
        public CodeFixMapping(Diagnostic diagnostic, string replacement, string filename) {
            this.diagnostic = diagnostic;
            this.replacement = replacement;
            this.filename = filename;
        }
    }
}
