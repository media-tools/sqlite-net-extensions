namespace SQLiteNetExtensions.Attributes
{
    public class ManyToOneAttribute : ReversibleRelationshipAttribute
    {
        public ManyToOneAttribute(string idProperty = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(idProperty, inverseProperty, onDeleteAction)
        {
        }

    }
}