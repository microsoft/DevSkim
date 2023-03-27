namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.LanguageServer.Client;
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;

    public class JsonContentType
    {
        [Export]
        [Name("json")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition JsonContentTypeDefinition;

        [Export]
        [FileExtension(".json")]
        [ContentType("json")]
        internal static FileExtensionToContentTypeDefinition JsonFileExtensionDefinition;
    }
}
