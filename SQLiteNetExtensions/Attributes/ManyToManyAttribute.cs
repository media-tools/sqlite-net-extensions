using System;

namespace SQLiteNetExtensions.Attributes
{
    public class ManyToManyAttribute : RelationshipAttribute
    {
        public ManyToManyAttribute(Type intermediateType, string inverseForeignKey = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(null, inverseForeignKey, inverseProperty, onDeleteAction)
        {
            IntermediateType = intermediateType;
        }

        public Type IntermediateType { get; private set; }
    }
}