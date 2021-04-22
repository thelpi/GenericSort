namespace GenericSort
{
    /// <summary>
    /// Error management types.
    /// </summary>
    public enum ErrorManagementType
    {
        /// <summary>
        /// If an error occurs during the retrieve of the sort of the field, the field is ignored.
        /// </summary>
        Ignore,
        /// <summary>
        /// If an error occurs during the retrieve of the sort of the field, an exception is thrown.
        /// </summary>
        Throw
    }
}
