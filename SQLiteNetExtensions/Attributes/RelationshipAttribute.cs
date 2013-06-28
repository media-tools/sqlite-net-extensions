using System;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace SQLiteNetExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class RelationshipAttribute : IgnoreAttribute
    {
        protected RelationshipAttribute(OnDeleteAction onDeleteAction)
        {
            OnDeleteAction = onDeleteAction;
        }

        public OnDeleteAction OnDeleteAction { get; private set; }
    }
}
