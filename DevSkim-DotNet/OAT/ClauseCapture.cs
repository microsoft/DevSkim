namespace Microsoft.CST.OAT
{
    /// <summary>
    /// Holds a clause and object states, can be extended to hold a Result with specific data
    /// </summary>
    public class ClauseCapture
    {
        /// <summary>
        /// A basic Clause Capture constructor
        /// </summary>
        /// <param name="clause"></param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        public ClauseCapture(Clause clause, object? state1, object? state2)
        {
            Clause = clause;
            State1 = state1;
            State2 = state2;
        }

        /// <summary>
        /// The Clause that caused the capture
        /// </summary>
        public Clause Clause { get; }
        /// <summary>
        /// Object state 1
        /// </summary>
        public object? State1 { get; }
        /// <summary>
        /// Object state 2
        /// </summary>
        public object? State2 { get; }
    }
}
