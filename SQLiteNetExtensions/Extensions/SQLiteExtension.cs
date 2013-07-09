using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SQLiteNetExtensions.Attributes;

#if USING_MVVMCROSS
using SQLiteConnection = Cirrious.MvvmCross.Plugins.Sqlite.ISQLiteConnection;
#else
using SQLite;
#endif

namespace SQLiteNetExtensions.Extensions
{
    public static class SQLiteExtension
    {
        public static T GetWithChildren<T>(this SQLiteConnection conn, object pk) where T : new()
        {
            var element = conn.Get<T>(pk);
            conn.GetChildren(ref element);
            return element;
        }

        public static void GetChildren<T>(this SQLiteConnection conn, ref T element) where T : new()
        {
            foreach (var relationshipProperty in typeof(T).GetRelationshipProperties())
            {
                conn.GetChild(ref element, relationshipProperty);
            }
        }

        public static void GetChild<T>(this SQLiteConnection conn, ref T element, string relationshipProperty)
        {
            conn.GetChild(ref element, typeof(T).GetProperty(relationshipProperty));
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
            Debug.Assert(currentEntityPrimaryKeyProperty != null || otherEntityPrimaryKeyProperty != null, "At least one entity in a OneToOne relationship must have Primary Key");

            var currentEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
            var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
            Debug.Assert(currentEntityForeignKeyProperty != null || otherEntityForeignKeyProperty != null, "At least one entity in a OneToOne relationship must have Foreign Key");

            var hasForeignKey = otherEntityPrimaryKeyProperty != null && currentEntityForeignKeyProperty != null;
            var hasInverseForeignKey = currentEntityPrimaryKeyProperty != null && otherEntityForeignKeyProperty != null;
            Debug.Assert(hasForeignKey || hasInverseForeignKey, "Missing either ForeignKey or PrimaryKey for a complete OneToOne relationship");

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
                    var query = string.Format("select * from {0} where {1} = ? limit 1", entityType.Name, otherEntityForeignKeyProperty.Name);
                    value = conn.Query(tableMapping, query, primaryKeyValue).FirstOrDefault(); // Its a OneToOne, take only the first
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
            var type = typeof(T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Debug.Assert(enclosedType == EnclosedType.None, "ManyToOne relationship cannot be of type List or Array");

            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            Debug.Assert(otherEntityPrimaryKeyProperty != null, "ManyToOne relationship destination must have Primary Key");

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
            var type = typeof(T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Debug.Assert(enclosedType != EnclosedType.None, "OneToMany relationship must be a List or Array");

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            Debug.Assert(currentEntityPrimaryKeyProperty != null, "OneToMany relationship origin must have Primary Key");

            var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
            Debug.Assert(otherEntityForeignKeyProperty != null, "OneToMany relationship destination must have Foreign Key to the origin class");

            var tableMapping = conn.GetMapping(entityType);
            Debug.Assert(tableMapping != null, "There's no mapping table for OneToMany relationship destination");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);

            IEnumerable values = null;
            var primaryKeyValue = currentEntityPrimaryKeyProperty.GetValue(element, null);
            if (primaryKeyValue != null)
            {
                var query = string.Format("select * from {0} where {1} = ?", entityType.Name, otherEntityForeignKeyProperty.Name);
                var queryResults = conn.Query(tableMapping, query, primaryKeyValue);
                if (enclosedType == EnclosedType.List)
                {
                    // Create a generic list of the expected type
                    var list = (IList)Activator.CreateInstance(typeof (List<>).MakeGenericType(entityType));
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
            var type = typeof(T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Debug.Assert(enclosedType != EnclosedType.None, "ManyToMany relationship must be a List or Array");

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            Debug.Assert(currentEntityPrimaryKeyProperty != null, "ManyToMany relationship origin must have Primary Key");
            Debug.Assert(otherEntityPrimaryKeyProperty != null, "ManyToMany relationship destination must have Primary Key");

            var manyToManyMetaInfo = type.GetManyToManyMetaInfo(relationshipProperty);
            var currentEntityForeignKeyProperty = manyToManyMetaInfo.OriginProperty;
            var otherEntityForeignKeyProperty = manyToManyMetaInfo.DestinationProperty;
            var intermediateType = manyToManyMetaInfo.IntermediateType;
            Debug.Assert(intermediateType != null, "ManyToMany relationship intermediate type cannot be null");
            Debug.Assert(currentEntityForeignKeyProperty != null, "ManyToMany relationship origin must have a foreign key defined in the intermediate type");
            Debug.Assert(otherEntityForeignKeyProperty != null, "ManyToMany relationship destination must have a foreign key defined in the intermediate type");

            var tableMapping = conn.GetMapping(entityType);
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
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entityType));
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

        public static void UpdateWithChildren<T>(this SQLiteConnection conn, T element)
        {
            // Update the current element
            RefreshForeignKeys(ref element);
            conn.Update(element);

            // Update inverse foreign keys
            conn.UpdateInverseForeignKeys(element);
        }

        private static void RefreshForeignKeys<T>(ref T element)
        {
            var type = typeof (T);
            foreach (var relationshipProperty in type.GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();
                if (relationshipAttribute is OneToOneAttribute || relationshipAttribute is ManyToOneAttribute)
                {
                    var foreignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
                    if (foreignKeyProperty != null)
                    {
                        EnclosedType enclosedType;
                        var entityType = relationshipProperty.GetEntityType(out enclosedType);
                        var destinationPrimaryKeyProperty = entityType.GetPrimaryKey();
                        Debug.Assert(enclosedType == EnclosedType.None, "ToOne relationships cannot be lists or arrays");
                        Debug.Assert(destinationPrimaryKeyProperty != null, "Found foreign key but destination Type doesn't have primary key");

                        var relationshipValue = relationshipProperty.GetValue(element, null);
                        object foreignKeyValue = null;
                        if (relationshipValue != null)
                        {
                            foreignKeyValue = destinationPrimaryKeyProperty.GetValue(relationshipValue, null);
                        }
                        foreignKeyProperty.SetValue(element, foreignKeyValue, null);
                    }
                }
            }
        }


        private static void UpdateInverseForeignKeys<T>(this SQLiteConnection conn, T element)
        {
            var type = typeof (T);
            foreach (var relationshipProperty in type.GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();
                if (relationshipAttribute is OneToManyAttribute)
                {
                    conn.UpdateOneToManyInverseForeignKey(element, relationshipProperty);
                }
                else if (relationshipAttribute is OneToOneAttribute)
                {
                    conn.UpdateOneToOneInverseForeignKey(element, relationshipProperty);
                }
                else if (relationshipAttribute is ManyToManyAttribute)
                {
                    conn.UpdateManyToManyForeignKeys(element, relationshipProperty);
                }
            }
        }
            
        private static void UpdateOneToManyInverseForeignKey<T>(this SQLiteConnection conn, T element, PropertyInfo relationshipProperty)
        {
            var type = typeof(T);

            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var originPrimaryKeyProperty = type.GetPrimaryKey();
            var inversePrimaryKeyProperty = entityType.GetPrimaryKey();
            var inverseForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);

            Debug.Assert(enclosedType != EnclosedType.None, "OneToMany relationships must be List or Array of entities");
            Debug.Assert(originPrimaryKeyProperty != null, "OneToMany relationships require Primary Key in the origin entity");
            Debug.Assert(inversePrimaryKeyProperty != null, "OneToMany relationships require Primary Key in the destination entity");
            Debug.Assert(inverseForeignKeyProperty != null, "Unable to find foreign key for OneToMany relationship");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);
            if (inverseProperty != null)
            {
                EnclosedType inverseEnclosedType;
                var inverseEntityType = inverseProperty.GetEntityType(out inverseEnclosedType);
                Debug.Assert(inverseEnclosedType == EnclosedType.None, "OneToMany inverse relationship shouldn't be List or Array");
                Debug.Assert(inverseEntityType == type, "OneToMany inverse relationship is not the expected type");
            }

            var keyValue = originPrimaryKeyProperty.GetValue(element, null);
            var children = (IEnumerable)relationshipProperty.GetValue(element, null);
            var childrenKeyList = new List<object>();
            if (children != null)
            {
                foreach (var child in children)
                {
                    var childKey = inversePrimaryKeyProperty.GetValue(child, null);
                    childrenKeyList.Add(childKey);

                    inverseForeignKeyProperty.SetValue(child, keyValue, null);
                    if (inverseProperty != null)
                    {
                        inverseProperty.SetValue(child, element, null);
                    }
                }
            }

            // Objects already updated, now change the database
            var childrenKeys = string.Join(",", childrenKeyList);
            var query = string.Format("update {0} set {1} = ? where {2} in ({3})",
                entityType.Name, inverseForeignKeyProperty.Name, inversePrimaryKeyProperty.Name, childrenKeys);
            conn.Execute(query, keyValue);

            // Delete previous relationships
            var deleteQuery = string.Format("update {0} set {1} = NULL where {1} == ? and {2} not in ({3})",
                entityType.Name, inverseForeignKeyProperty.Name, inversePrimaryKeyProperty.Name, childrenKeys);
            conn.Execute(deleteQuery, keyValue);
        }

        private static void UpdateOneToOneInverseForeignKey<T>(this SQLiteConnection conn, T element, PropertyInfo relationshipProperty)
        {
            var type = typeof(T);

            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var originPrimaryKeyProperty = type.GetPrimaryKey();
            var inversePrimaryKeyProperty = entityType.GetPrimaryKey();
            var inverseForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);

            Debug.Assert(enclosedType == EnclosedType.None, "OneToOne relationships cannot be List or Array of entities");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);
            if (inverseProperty != null)
            {
                EnclosedType inverseEnclosedType;
                var inverseEntityType = inverseProperty.GetEntityType(out inverseEnclosedType);
                Debug.Assert(inverseEnclosedType == EnclosedType.None, "OneToOne inverse relationship shouldn't be List or Array");
                Debug.Assert(inverseEntityType == type, "OneToOne inverse relationship is not the expected type");
            }

            object keyValue = null;
            if (originPrimaryKeyProperty != null && inverseForeignKeyProperty != null)
            {
                keyValue = originPrimaryKeyProperty.GetValue(element, null);
            }

            object childKey = null;
            var child = relationshipProperty.GetValue(element, null);
            if (child != null)
            {
                if (inverseForeignKeyProperty != null && keyValue != null)
                {
                    inverseForeignKeyProperty.SetValue(child, keyValue, null);
                }
                if (inverseProperty != null)
                {
                    inverseProperty.SetValue(child, element, null);
                }
                if (inversePrimaryKeyProperty != null)
                {
                    childKey = inversePrimaryKeyProperty.GetValue(child, null);
                }
            }


            // Objects already updated, now change the database
            if (inverseForeignKeyProperty != null && inversePrimaryKeyProperty != null)
            {
                var query = string.Format("update {0} set {1} = ? where {2} == ?",
                                          entityType.Name, inverseForeignKeyProperty.Name, inversePrimaryKeyProperty.Name);
                conn.Execute(query, keyValue, childKey);

                // Delete previous relationships
                var deleteQuery = string.Format("update {0} set {1} = NULL where {1} == ? and {2} not in ({3})",
                    entityType.Name, inverseForeignKeyProperty.Name, inversePrimaryKeyProperty.Name, childKey ?? "");
                conn.Execute(deleteQuery, keyValue);
            }
        }

        private static void UpdateManyToManyForeignKeys<T>(this SQLiteConnection conn, T element, PropertyInfo relationshipProperty)
        {
            
        }
    } 


}
