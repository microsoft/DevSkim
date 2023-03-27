namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.LanguageServer.Client;
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;

    public class CSharp
    {
        [Export]
        [Name("csharp")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition BarContentTypeDefinition;

        [Export]
        [FileExtension(".cs")]
        [ContentType("csharp")]
        internal static FileExtensionToContentTypeDefinition BarFileExtensionDefinition;
    }
}
