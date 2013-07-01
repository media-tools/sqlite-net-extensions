using System;

namespace SQLiteNetExtensions.Attributes
{
    public class ManyToManyAttribute : RelationshipAttribute
    {
        public ManyToManyAttribute(Type intermediateTable, string foreignKey = null, string inverseProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(foreignKey, inverseProperty, onDeleteAction)
        {
            IntermediateTable = intermediateTable;
        }

        public Type IntermediateTable { get; private set; }
    }
}