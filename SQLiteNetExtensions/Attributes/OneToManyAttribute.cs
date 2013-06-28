namespace SQLiteNetExtensions.Attributes
{
    public class OneToManyAttribute : ReversibleRelationshipAttribute
    {
        public OneToManyAttribute(string idProperty = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(idProperty, inverseProperty, onDeleteAction)
        {
        }
    }
}