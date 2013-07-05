namespace SQLiteNetExtensions.Attributes
{
    public class ManyToOneAttribute : RelationshipAttribute
    {
        public ManyToOneAttribute(string foreignKey = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(foreignKey, null, inverseProperty, onDeleteAction)
        {
        }

    }
}