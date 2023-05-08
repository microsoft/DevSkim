namespace Microsoft.DevSkim.LanguageProtoInterop
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public enum ValueResultEnum
    {
        //
        // Summary:
        //     The value was retrieved and converted to the specified type successfully.
        Success,
        //
        // Summary:
        //     The value is not present in the store.
        Missing,
        //
        // Summary:
        //     The stored value could not be deserialized.
        Corrupt,
        //
        // Summary:
        //     The deserialized value could not be converted to the specified type.
        IncompatibleType,
        //
        // Summary:
        //     The stored value is in a old serialization format that is no longer supported.
        ObsoleteFormat,
        //
        // Summary:
        //     An unexpected error occurred.
        UnknownError
    }
}
