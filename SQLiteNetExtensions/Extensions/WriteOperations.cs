using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
#if USING_MVVMCROSS
using SQLiteConnection = Cirrious.MvvmCross.Plugins.Sqlite.ISQLiteConnection;
#else
using SQLite;
#endif
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions.TextBlob;

namespace SQLiteNetExtensions.Extensions
{
    public static class WriteOperations
    {

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
                else if (relationshipAttribute is TextBlobAttribute)
                {
                    TextBlobOperations.UpdateTextBlobProperty(ref element, relationshipProperty);
                }
            }
        }


        private static void UpdateInverseForeignKeys<T>(this SQLiteConnection conn, T element)
        {
            foreach (var relationshipProperty in typeof(T).GetRelationshipProperties())
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
            var type = typeof (T);

            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            var manyToManyMetaInfo = type.GetManyToManyMetaInfo(relationshipProperty);
            var currentEntityForeignKeyProperty = manyToManyMetaInfo.OriginProperty;
            var otherEntityForeignKeyProperty = manyToManyMetaInfo.DestinationProperty;
            var intermediateType = manyToManyMetaInfo.IntermediateType;

            Debug.Assert(enclosedType != EnclosedType.None, "ManyToMany relationship must be a List or Array");
            Debug.Assert(currentEntityPrimaryKeyProperty != null, "ManyToMany relationship origin must have Primary Key");
            Debug.Assert(otherEntityPrimaryKeyProperty != null, "ManyToMany relationship destination must have Primary Key");
            Debug.Assert(intermediateType != null, "ManyToMany relationship intermediate type cannot be null");
            Debug.Assert(currentEntityForeignKeyProperty != null, "ManyToMany relationship origin must have a foreign key defined in the intermediate type");
            Debug.Assert(otherEntityForeignKeyProperty != null, "ManyToMany relationship destination must have a foreign key defined in the intermediate type");

            var primaryKey = currentEntityPrimaryKeyProperty.GetValue(element, null);

            // Obtain the list of children keys
            var childList = (IEnumerable)relationshipProperty.GetValue(element, null);
            var childKeyList = (from object child in childList ?? new List<object>()
                               select otherEntityPrimaryKeyProperty.GetValue(child, null)).ToList();
            var childKeysString = string.Join(",", childKeyList);

            // Check for already existing relationships
            var currentChildrenQuery = string.Format("select {0} from {1} where {2} == ? and {0} in ({3})",
                otherEntityForeignKeyProperty.Name, intermediateType.Name, currentEntityForeignKeyProperty.Name, childKeysString);
            var currentChildKeyList =
                from object child in conn.Query(conn.GetMapping(intermediateType), currentChildrenQuery, primaryKey)
                select otherEntityForeignKeyProperty.GetValue(child, null);
            

            // Insert missing relationships in the intermediate table
            var missingChildKeyList = childKeyList.Where(o => !currentChildKeyList.Contains(o)).ToList();
            var missingIntermediateObjects = new List<object>(missingChildKeyList.Count);
            foreach (var missingChildKey in missingChildKeyList)
            {
                var intermediateObject = Activator.CreateInstance(intermediateType);
                currentEntityForeignKeyProperty.SetValue(intermediateObject, primaryKey, null);
                otherEntityForeignKeyProperty.SetValue(intermediateObject, missingChildKey, null);

                missingIntermediateObjects.Add(intermediateObject);
            }

            conn.InsertAll(missingIntermediateObjects);

            // Delete any other pending relationship
            var deleteQuery = string.Format("delete from {0} where {1} == ? and {2} not in ({3})",
                intermediateType.Name, currentEntityForeignKeyProperty.Name,
                otherEntityForeignKeyProperty.Name, childKeysString);
            conn.Execute(deleteQuery, primaryKey);
        }
    }
}