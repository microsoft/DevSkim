﻿namespace DevSkim.LanguageServer
{
    using OmniSharp.Extensions.LanguageServer.Protocol.Models;

    public class CodeFixMapping
    {
        /// <summary>
        /// The diagnostic to attach the fix to
        /// </summary>
        public Diagnostic diagnostic { get; }
        /// <summary>
        /// The replacement to make
        /// </summary>
        public string replacement { get; }
        /// <summary>
        /// The Filename this fix should apply to
        /// </summary>
        public string fileName { get; }
        /// <summary>
        /// The description for the CodeFix in the IDE
        /// </summary>
        public string friendlyString { get; }
        /// <summary>
        /// Create a codefixmapping to send to the IDE
        /// </summary>
        /// <param name="diagnostic"></param>
        /// <param name="replacement"></param>
        /// <param name="fileName"></param>
        /// <param name="friendlyString"></param>
        public CodeFixMapping(Diagnostic diagnostic, string replacement, string fileName, string friendlyString)
        {
            this.diagnostic = diagnostic;
            this.replacement = replacement;
            this.fileName = fileName;
            this.friendlyString = friendlyString;
        }
    }
}