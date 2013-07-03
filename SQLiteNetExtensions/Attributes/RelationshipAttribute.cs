using System;
#if USING_MVVMCROSS
using IgnoreAttribute = Cirrious.MvvmCross.Plugins.Sqlite.IgnoreAttribute;
#else
using SQLite;
#endif

namespace SQLiteNetExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class RelationshipAttribute : IgnoreAttribute
    {
        protected RelationshipAttribute(string foreignKey, string inverseProperty, OnDeleteAction onDeleteAction)
        {
            InverseProperty = inverseProperty;
            ForeignKey = foreignKey;
            OnDeleteAction = onDeleteAction;
        }

        public string ForeignKey { get; private set; }
        public string InverseProperty { get; private set; }
        public OnDeleteAction OnDeleteAction { get; private set; }
    }
}
