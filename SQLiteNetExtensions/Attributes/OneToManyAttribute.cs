namespace SQLiteNetExtensions.Attributes
{
    public class OneToMany : RelationshipAttribute
    {
        public OneToMany(string foreignKey = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(foreignKey, inverseProperty, onDeleteAction)
        {
        }
    }
}