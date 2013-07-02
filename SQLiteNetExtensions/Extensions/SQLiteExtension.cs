using System.Collections.Generic;
using Cirrious.MvvmCross.Plugins.Sqlite;
using SQLiteNetExtensions.Attributes;

namespace SQLiteNetExtensions.Extensions
{
    public static class SQLiteExtension
    {
        public static T GetWithChildren<T>(this ISQLiteConnection conn, object pk) where T : new()
        {
            var element = conn.Get<T>(pk);
            conn.GetChildren(ref element);
            return element;
        }

        public static void GetChildren<T>(this ISQLiteConnection conn, ref T element) where T : new()
        {
            foreach (var relationshipProperty in typeof (T).GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();
                
                if (relationshipAttribute is OneToOneAttribute)
                {
                    var currentEntityPrimaryKeyProperty = 
                }
            }
        }


    } 

}
