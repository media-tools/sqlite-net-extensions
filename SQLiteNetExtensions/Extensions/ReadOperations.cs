using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions.TextBlob;

#if USING_MVVMCROSS
using SQLiteConnection = Cirrious.MvvmCross.Plugins.Sqlite.ISQLiteConnection;
#else
using SQLite;
#endif

namespace SQLiteNetExtensions.Extensions
{
    public static class ReadOperations
    {
        public static T GetWithChildren<T>(this SQLiteConnection conn, object pk) where T : new()
        {
            var element = conn.Get<T>(pk);
            conn.GetChildren(ref element);
            return element;
        }

        public static void GetChildren<T>(this SQLiteConnection conn, ref T element) where T : new()
        {
            foreach (var relationshipProperty in typeof (T).GetRelationshipProperties())
            {
                conn.GetChild(ref element, relationshipProperty);
            }
        }

        public static void GetChild<T>(this SQLiteConnection conn, ref T element, string relationshipProperty)
        {
            conn.GetChild(ref element, typeof (T).GetProperty(relationshipProperty));
        }

        public static void GetChild<T>(this SQLiteConnection conn, ref T element, PropertyInfo relationshipProperty)
        {
            var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

            if (relationshipAttribute is OneToOneAttribute)
            {
                conn.GetOneToOneChild(ref element, relationshipProperty);
            }
            else if (relationshipAttribute is OneToManyAttribute)
            {
                conn.GetOneToManyChildren(ref element, relationshipProperty);
            }
            else if (relationshipAttribute is ManyToOneAttribute)
            {
                conn.GetManyToOneChild(ref element, relationshipProperty);
            }
            else if (relationshipAttribute is ManyToManyAttribute)
            {
                conn.GetManyToManyChildren(ref element, relationshipProperty);
            }
            else if (relationshipAttribute is TextBlobAttribute)
            {
                TextBlobOperations.GetTextBlobChild(ref element, relationshipProperty);
            }
        }

        private static void GetOneToOneChild<T>(this SQLiteConnection conn, ref T element,
                                                PropertyInfo relationshipProperty)
        {
            var type = typeof (T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Debug.Assert(enclosedType == EnclosedType.None, "OneToOne relationship cannot be of type List or Array");

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            Debug.Assert(currentEntityPrimaryKeyProperty != null || otherEntityPrimaryKeyProperty != null,
                         "At least one entity in a OneToOne relationship must have Primary Key");

            var currentEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
            var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
            Debug.Assert(currentEntityForeignKeyProperty != null || otherEntityForeignKeyProperty != null,
                         "At least one entity in a OneToOne relationship must have Foreign Key");

            var hasForeignKey = otherEntityPrimaryKeyProperty != null && currentEntityForeignKeyProperty != null;
            var hasInverseForeignKey = currentEntityPrimaryKeyProperty != null && otherEntityForeignKeyProperty != null;
            Debug.Assert(hasForeignKey || hasInverseForeignKey,
                         "Missing either ForeignKey or PrimaryKey for a complete OneToOne relationship");

            var tableMapping = conn.GetMapping(entityType);
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
                    var query = string.Format("select * from {0} where {1} = ? limit 1", entityType.Name,
                                              otherEntityForeignKeyProperty.Name);
                    value = conn.Query(tableMapping, query, primaryKeyValue).FirstOrDefault();
                        // Its a OneToOne, take only the first
                }
            }

            relationshipProperty.SetValue(element, value, null);

            if (value != null && inverseProperty != null)
            {
                inverseProperty.SetValue(value, element, null);
            }
        }


        private static void GetManyToOneChild<T>(this SQLiteConnection conn, ref T element,
                                                 PropertyInfo relationshipProperty)
        {
            var type = typeof (T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Debug.Assert(enclosedType == EnclosedType.None, "ManyToOne relationship cannot be of type List or Array");

            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            Debug.Assert(otherEntityPrimaryKeyProperty != null,
                         "ManyToOne relationship destination must have Primary Key");

            var currentEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
            Debug.Assert(currentEntityForeignKeyProperty != null, "ManyToOne relationship origin must have Foreign Key");

            var tableMapping = conn.GetMapping(entityType);
            Debug.Assert(tableMapping != null, "There's no mapping table for OneToMany relationship destination");

            object value = null;
            var foreignKeyValue = currentEntityForeignKeyProperty.GetValue(element, null);
            if (foreignKeyValue != null)
            {
                value = conn.Find(foreignKeyValue, tableMapping);
            }

            relationshipProperty.SetValue(element, value, null);

        }

        private static void GetOneToManyChildren<T>(this SQLiteConnection conn, ref T element,
                                                    PropertyInfo relationshipProperty)
        {
            var type = typeof (T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Debug.Assert(enclosedType != EnclosedType.None, "OneToMany relationship must be a List or Array");

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            Debug.Assert(currentEntityPrimaryKeyProperty != null, "OneToMany relationship origin must have Primary Key");

            var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
            Debug.Assert(otherEntityForeignKeyProperty != null,
                         "OneToMany relationship destination must have Foreign Key to the origin class");

            var tableMapping = conn.GetMapping(entityType);
            Debug.Assert(tableMapping != null, "There's no mapping table for OneToMany relationship destination");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);

            IEnumerable values = null;
            var primaryKeyValue = currentEntityPrimaryKeyProperty.GetValue(element, null);
            if (primaryKeyValue != null)
            {
                var query = string.Format("select * from {0} where {1} = ?", entityType.Name,
                                          otherEntityForeignKeyProperty.Name);
                var queryResults = conn.Query(tableMapping, query, primaryKeyValue);
                if (enclosedType == EnclosedType.List)
                {
                    // Create a generic list of the expected type
                    var list = (IList) Activator.CreateInstance(typeof (List<>).MakeGenericType(entityType));
                    foreach (var result in queryResults)
                    {
                        list.Add(result);
                    }
                    values = list;
                }
                else
                {
                    // Create a generic list of the expected type
                    var array = Array.CreateInstance(entityType, queryResults.Count);
                    for (var i = 0; i < queryResults.Count; i++)
                    {
                        array.SetValue(queryResults[i], i);
                    }
                    values = array;
                }
            }

            relationshipProperty.SetValue(element, values, null);

            if (inverseProperty != null && values != null)
            {
                // Stablish inverse relationships (we already have that object anyway)
                foreach (var value in values)
                {
                    inverseProperty.SetValue(value, element, null);
                }
            }
        }

        private static void GetManyToManyChildren<T>(this SQLiteConnection conn, ref T element,
                                                     PropertyInfo relationshipProperty)
        {
            var type = typeof (T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            var manyToManyMetaInfo = type.GetManyToManyMetaInfo(relationshipProperty);
            var currentEntityForeignKeyProperty = manyToManyMetaInfo.OriginProperty;
            var otherEntityForeignKeyProperty = manyToManyMetaInfo.DestinationProperty;
            var intermediateType = manyToManyMetaInfo.IntermediateType;
            var tableMapping = conn.GetMapping(entityType);

            Debug.Assert(enclosedType != EnclosedType.None, "ManyToMany relationship must be a List or Array");
            Debug.Assert(currentEntityPrimaryKeyProperty != null, "ManyToMany relationship origin must have Primary Key");
            Debug.Assert(otherEntityPrimaryKeyProperty != null, "ManyToMany relationship destination must have Primary Key");
            Debug.Assert(intermediateType != null, "ManyToMany relationship intermediate type cannot be null");
            Debug.Assert(currentEntityForeignKeyProperty != null, "ManyToMany relationship origin must have a foreign key defined in the intermediate type");
            Debug.Assert(otherEntityForeignKeyProperty != null, "ManyToMany relationship destination must have a foreign key defined in the intermediate type");
            Debug.Assert(tableMapping != null, "There's no mapping table defined for ManyToMany relationship origin");

            IEnumerable values = null;
            var primaryKeyValue = currentEntityPrimaryKeyProperty.GetValue(element, null);
            if (primaryKeyValue != null)
            {
                // Obtain the relationship keys
                var keysQuery = string.Format("select {0} from {1} where {2} = ?", otherEntityForeignKeyProperty.Name,
                                              intermediateType.Name, currentEntityForeignKeyProperty.Name);

                var query = string.Format("select * from {0} where {1} in ({2})", entityType.Name,
                                          otherEntityPrimaryKeyProperty.Name, keysQuery);

                var queryResults = conn.Query(tableMapping, query, primaryKeyValue);

                if (enclosedType == EnclosedType.List)
                {
                    // Create a generic list of the expected type
                    var list = (IList) Activator.CreateInstance(typeof (List<>).MakeGenericType(entityType));
                    foreach (var result in queryResults)
                    {
                        list.Add(result);
                    }
                    values = list;
                }
                else
                {
                    // Create a generic list of the expected type
                    var array = Array.CreateInstance(entityType, queryResults.Count);
                    for (var i = 0; i < queryResults.Count; i++)
                    {
                        array.SetValue(queryResults[i], i);
                    }
                    values = array;
                }
            }

            relationshipProperty.SetValue(element, values, null);

        }
    }
}
