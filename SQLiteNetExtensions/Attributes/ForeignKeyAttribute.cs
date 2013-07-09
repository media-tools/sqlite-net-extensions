using System;

#if USING_MVVMCROSS
using Cirrious.MvvmCross.Plugins.Sqlite;
#else
using SQLite;
#endif

namespace SQLiteNetExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : IndexedAttribute
    {
        public ForeignKeyAttribute(Type foreignType)
        {
            ForeignType = foreignType;
        }

        public Type ForeignType { get; private set; }
    }
}