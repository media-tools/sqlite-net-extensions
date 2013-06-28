namespace SQLiteNetExtensions.Attributes
{
    public class OneToMany : ReversibleRelationshipAttribute
    {
        public OneToMany(string idProperty = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(idProperty, inverseProperty, onDeleteAction)
        {
        }
    }
}