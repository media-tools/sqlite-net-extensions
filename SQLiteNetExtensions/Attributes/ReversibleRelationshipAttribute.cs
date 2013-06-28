namespace SQLiteNetExtensions.Attributes
{
    public abstract class ReversibleRelationshipAttribute : RelationshipAttribute
    {
        protected ReversibleRelationshipAttribute(string idProperty, string inverseProperty, OnDeleteAction onDeleteAction)
            : base(onDeleteAction)
        {
            IdProperty = idProperty;
            InverseProperty = inverseProperty;
        }

        public string IdProperty { get; private set; }
        public string InverseProperty { get; private set; }
    }
}