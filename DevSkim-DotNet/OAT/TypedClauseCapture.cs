namespace Microsoft.CST.OAT
{
    /// <summary>
    /// Holds a clause and object states, can be extended to hold a Result with specific data
    /// </summary>
    public class TypedClauseCapture<T> : ClauseCapture
    {
        /// <summary>
        /// A basic Clause Capture constructor
        /// </summary>
        /// <param name="clause"></param>
        /// <param name="result">The object to hold</param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        public TypedClauseCapture(Clause clause, T result, object? state1 = null, object? state2 = null) : base(clause, state1, state2)
        {
            Result = result;
        }

        /// <summary>
        /// The result data
        /// </summary>
        public T Result { get; set; }
    }
}
