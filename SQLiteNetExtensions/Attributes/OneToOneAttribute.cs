namespace SQLiteNetExtensions.Attributes
{
    public class OneToOneAttribute : ReversibleRelationshipAttribute
    {
        public OneToOneAttribute(string idProperty = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None) 
            : base(idProperty, inverseProperty, onDeleteAction)
        {
        }
    }
}