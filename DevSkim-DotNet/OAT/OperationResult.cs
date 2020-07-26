namespace Microsoft.CST.OAT
{
    /// <summary>
    /// The Type returned by Operations
    /// </summary>
    public class OperationResult
    {
        /// <summary>
        /// A result consists of a boolean outcome and captures
        /// </summary>
        /// <param name="result">The boolean outcome of the operation</param>
        /// <param name="clauseCaptures">Any captures</param>
        public OperationResult(bool result, ClauseCapture? clauseCaptures)
        {
            Result = result;
            Capture = clauseCaptures;
        }

        /// <summary>
        /// The boolean outcome of the operation
        /// </summary>
        public bool Result { get; }
        /// <summary>
        /// Captures from the operation
        /// </summary>
        public ClauseCapture? Capture { get; }
    }
}
