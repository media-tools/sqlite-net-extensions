namespace SQLiteNetExtensions.Attributes
{
    public enum OnDeleteAction
    {
        Cascade,    // Delete also the destination object
        Deny,       // Don't allow deleting if relationship is set
        Nullify,    // Nullify inverse relationships
        None
    }
}