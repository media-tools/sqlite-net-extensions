namespace SQLiteNetExtensions.Attributes
{
    public class OneToManyAttribute : RelationshipAttribute
    {
        public OneToManyAttribute(string foreignKey = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(foreignKey, inverseProperty, onDeleteAction)
        {
        }
    }
}