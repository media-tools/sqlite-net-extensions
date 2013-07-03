using System.Collections.Generic;
using System.Diagnostics;
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
            var type = typeof (T);
            foreach (var relationshipProperty in type.GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();
                
                EnclosedType enclosedType;
                var entityType = relationshipProperty.GetEntityType(out enclosedType);
                
                if (relationshipAttribute is OneToOneAttribute)
                {
                    Debug.Assert(enclosedType == EnclosedType.None, "OneToOne relationship cannot be of type List or Array");

                    var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
                    var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
                    Debug.Assert(currentEntityPrimaryKeyProperty != null || otherEntityPrimaryKeyProperty != null, "At least one entity in a OneToOne relationship must have Primary Key");

                    var currentEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
                    var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
                    Debug.Assert(currentEntityForeignKeyProperty != null || otherEntityForeignKeyProperty != null, "At least one entity in a OneToOne relationship must have Foreign Key");

                    var hasForeignKey = otherEntityPrimaryKeyProperty != null && currentEntityForeignKeyProperty != null;
                    var hasInverseForeignKey = currentEntityPrimaryKeyProperty != null && otherEntityForeignKeyProperty != null;
                    Debug.Assert(hasForeignKey || hasInverseForeignKey, "Missing either ForeignKey or PrimaryKey for a complete OneToOne relationship");

                    ITableMapping tableMapping = conn.GetMapping(entityType);
                    Debug.Assert(tableMapping != null, "There's no mapping table for OneToOne relationship");

                    var inverseProperty = type.GetInverseProperty(relationshipProperty);

                    object value = null;
                    if (hasForeignKey)
                    {
                        var foreignKeyValue = currentEntityForeignKeyProperty.GetValue(element, null);
                        if (foreignKeyValue != null)
                        {
                            value = conn.Find(foreignKeyValue, tableMapping);
                        }
                    }
                    else
                    {
                        var primaryKeyValue = currentEntityPrimaryKeyProperty.GetValue(element, null);
                        if (primaryKeyValue != null)
                        {
                            var query = string.Format("select * from {0} where {1} = ?", entityType.Name, otherEntityForeignKeyProperty.Name);
                            value = conn.Query(tableMapping, query, primaryKeyValue);
                        }
                    }

                    if (value != null)
                    {
                        relationshipProperty.SetValue(element, value, null);

                        if (inverseProperty != null)
                        {
                            inverseProperty.SetValue(value, element, null);
                        }
                    }
                }
            }
        }


    } 

}
