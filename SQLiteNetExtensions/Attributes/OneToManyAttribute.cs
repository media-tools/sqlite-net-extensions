namespace SQLiteNetExtensions.Attributes
{
    public class OneToManyAttribute : RelationshipAttribute
    {
        public OneToManyAttribute(string inverseForeignKey = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(null, inverseForeignKey, inverseProperty, onDeleteAction)
        {
        }
    }
}