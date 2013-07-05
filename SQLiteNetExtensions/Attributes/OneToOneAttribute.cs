namespace SQLiteNetExtensions.Attributes
{
    public class OneToOneAttribute : RelationshipAttribute
    {
        public OneToOneAttribute(string foreignKey = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None) 
            : base(foreignKey, null, inverseProperty, onDeleteAction)
        {
        }
    }
}