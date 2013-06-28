using System;

namespace SQLiteNetExtensions.Attributes
{
    public class ManyToManyAttribute : RelationshipAttribute
    {
        public ManyToManyAttribute(Type intermediateTable, string thisIdProperty = null, string otherIdProperty = null, OnDeleteAction onDeleteAction = OnDeleteAction.None)
            : base(onDeleteAction)
        {
            OtherIdProperty = otherIdProperty;
            ThisIdProperty = thisIdProperty;
            IntermediateTable = intermediateTable;
        }

        public Type IntermediateTable { get; private set; }
        public string ThisIdProperty { get; private set; }
        public string OtherIdProperty { get; private set; }
    }
}