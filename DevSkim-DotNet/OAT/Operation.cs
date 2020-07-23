namespace Microsoft.CST.OAT
{
    /// <summary>
    ///     Operations available for Analysis rules.
    /// </summary>
    public enum Operation
    {
        /// <summary>
        ///     Generates regular expressions from the Data list provided and tests them against the specified
        ///     field. If any match it is a success.
        /// </summary>
        Regex,

        /// <summary>
        ///     Checks that any value in the Data list or DictData dictionary have a match in the specified
        ///     field's object as appropriate.
        /// </summary>
        Equals,

        /// <summary>
        ///     Checks whether any of the specified fields values when parsed as an int is less than first value in
        ///     the Data list as Parsed as an int
        /// </summary>
        LessThan,

        /// <summary>
        ///     Checks whether any of the specified fields values when parsed as an int is greater than first value in
        ///     the Data list as Parsed as an int
        /// </summary>
        GreaterThan,

        /// <summary>
        ///     Checks if the specified fields values contain all of the data in the Data list or DictData
        ///     dictionary as appropriate for the field.
        /// </summary>
        Contains,

        /// <summary>
        ///     Checks if the specified field was modified between the two runs.
        /// </summary>
        WasModified,

        /// <summary>
        ///     Checks if the specified field ends with any of the strings in the Data list.
        /// </summary>
        EndsWith,

        /// <summary>
        ///     Checks if the specified field starts with any of the strings in the Data list.
        /// </summary>
        StartsWith,

        /// <summary>
        ///     Checks if the specified fields values contain any of the data in the Data list or DictData
        ///     dictionary as appropriate for the field.
        /// </summary>
        ContainsAny,

        /// <summary>
        ///     Checks if the specified field is null in both states.
        /// </summary>
        IsNull,

        /// <summary>
        ///     Checks if the specified field is true in either state.
        /// </summary>
        IsTrue,

        /// <summary>
        ///     Checks if the specified field, as parsed as time, is before the time specified in the first
        ///     entry of the Data list
        /// </summary>
        IsBefore,

        /// <summary>
        ///     Checks if the specified field, as parsed as time, is after the time specified in the first
        ///     entry of the Data list
        /// </summary>
        IsAfter,

        /// <summary>
        ///     Checks if the specified field, as parsed as time, is before DateTime.Now.
        /// </summary>
        IsExpired,

        /// <summary>
        ///     Checks if the field, if a dictionary, contains the specified key
        /// </summary>
        ContainsKey,

        /// <summary>
        ///     Specifies that a custom operation has been specified
        /// </summary>
        Custom
    }
}
