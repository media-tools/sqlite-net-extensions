using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SQLiteNetExtensions.Exceptions;


#if USING_MVVMCROSS
using SQLiteConnection = Cirrious.MvvmCross.Community.Plugins.Sqlite.ISQLiteConnection;
using Cirrious.MvvmCross.Community.Plugins.Sqlite;
#elif PCL
using SQLite.Net;
using SQLite.Net.Attributes;
#else
using SQLite;
#endif
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions.TextBlob;

namespace SQLiteNetExtensions.Extensions
{
    public static class WriteOperations
    {
        /// <summary>
        /// Enable to allow descriptive error descriptions on incorrect relationships. Enabled by default.
        /// Disable for production environments to remove the checks and reduce performance penalty
        /// </summary>
        public static bool EnableRuntimeAssertions = true;

        /// <summary>
        /// Updates the with foreign keys of the current object and save changes to the database and
        /// updates the inverse foreign keys of the defined relationships so the relationships are
        /// stored correctly in the database. This operation will create or delete the required intermediate
        /// objects for ManyToMany relationships. All related objects must have a primary key assigned in order
        /// to work correctly. This also implies that any object with 'AutoIncrement' primary key must've been
        /// inserted in the database previous to this call.
        /// This method will also update inverse relationships of objects that currently exist in the object tree,
        /// but it won't update inverse relationships of objects that are not reachable through this object
        /// properties. For example, objects removed from a 'ToMany' relationship won't be updated in memory.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="element">Object to be updated. Must already have been inserted in the database</param>
        /// <typeparam name="T">The Entity type, it should match de database entity type</typeparam>
        public static void UpdateWithChildren<T>(this SQLiteConnection conn, T element)
        {
            // Update the current element
            RefreshForeignKeys(element);
            conn.Update(element);

            // Update inverse foreign keys
            conn.UpdateInverseForeignKeys(element);
        }

        public static void InsertOrReplaceWithChildren<T>(this SQLiteConnection conn, T element, bool recursive = false) {
            conn.InsertWithChildrenRecursive(element, true, recursive);
        }

        public static void InsertWithChildren<T>(this SQLiteConnection conn, T element, bool recursive = false) {
            conn.InsertWithChildrenRecursive(element, false, recursive);
        }

        /// <summary>
        /// Deletes all the objects passed as parameters from the database.
        /// Relationships are not taken into account in this method
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="objects">Objects to be deleted from the database</param>
        /// <typeparam name="T">The Entity type, it should match de database entity type</typeparam>
        public static void DeleteAll<T>(this SQLiteConnection conn, IEnumerable<T> objects) {
            if (objects == null)
                return;

            var type = typeof(T);
            var primaryKeyProperty = type.GetPrimaryKey();

            var primaryKeyValues = (from element in objects
                select primaryKeyProperty.GetValue(element, null)).ToArray();
                
            conn.DeleteAllIds(primaryKeyValues, type.GetTableName(), primaryKeyProperty.GetColumnName());
        }

        /// <summary>
        /// Deletes all the objects passed with IDs equal to the passed parameters from the database.
        /// Relationships are not taken into account in this method
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="primaryKeyValues">Primary keys of the objects to be deleted from the database</param>
        /// <typeparam name="T">The Entity type, it should match de database entity type</typeparam>
        public static void DeleteAllIds<T>(this SQLiteConnection conn, IEnumerable<object> primaryKeyValues) {
            var type = typeof(T);
            var primaryKeyProperty = type.GetPrimaryKey();

            conn.DeleteAllIds(primaryKeyValues.ToArray(), type.GetTableName(), primaryKeyProperty.GetColumnName());
        }


        #region Private methods
        static void InsertAllWithChildrenRecursive(this SQLiteConnection conn, IEnumerable elements, bool replace, bool recursive, ISet<object> objectCache = null) {
            if (elements == null)
                return;

            objectCache = objectCache ?? new HashSet<object>();
            var elementsToInsert = elements.Cast<object>().Except(objectCache).ToList();
            if (elementsToInsert.Count == 0)
                return;
                
            var primaryKeyProperty = elementsToInsert[0].GetType().GetPrimaryKey();
            var isAutoIncrementPrimaryKey = primaryKeyProperty != null && primaryKeyProperty.GetAttribute<AutoIncrementAttribute>() != null;

            foreach (var element in elementsToInsert)
            {
                bool shouldReplace = false;
                bool isPrimaryKeySet = false;
                if (replace && isAutoIncrementPrimaryKey)
                {
                    var primaryKeyValue = primaryKeyProperty.GetValue(element, null);
                    var defaultPrimaryKeyValue = primaryKeyProperty.PropertyType.GetDefault();
                    isPrimaryKeySet = primaryKeyValue != null && !primaryKeyValue.Equals(defaultPrimaryKeyValue);
                }

                shouldReplace = replace && (!isAutoIncrementPrimaryKey || isPrimaryKeySet);

                if (shouldReplace)
                    conn.InsertOrReplace(element);
                else
                    conn.Insert(element);
            }

            if (recursive) {
                foreach (var element in elementsToInsert)
                    objectCache.Add(element);
                    
                foreach (var element in elementsToInsert)
                    conn.InsertChildrenRecursive(element, replace, recursive, objectCache);
            }

            foreach (var element in elementsToInsert)
                conn.UpdateWithChildren(element);
        }

        static void InsertWithChildrenRecursive(this SQLiteConnection conn, object element, bool replace, bool recursive, ISet<object> objectCache = null) {
            objectCache = objectCache ?? new HashSet<object>();
            if (objectCache.Contains(element))
                return;

            var primaryKeyProperty = element.GetType().GetPrimaryKey();
            var isAutoIncrementPrimaryKey = primaryKeyProperty != null && primaryKeyProperty.GetAttribute<AutoIncrementAttribute>() != null;

            bool shouldReplace = false;
            bool isPrimaryKeySet = false;
            if (replace && isAutoIncrementPrimaryKey)
            {
                var primaryKeyValue = primaryKeyProperty.GetValue(element, null);
                var defaultPrimaryKeyValue = primaryKeyProperty.PropertyType.GetDefault();
                isPrimaryKeySet = primaryKeyValue != null && !primaryKeyValue.Equals(defaultPrimaryKeyValue);
            }

            shouldReplace = replace && (!isAutoIncrementPrimaryKey || isPrimaryKeySet);

            // Only replace elements that have an assigned primary key
            if (shouldReplace)
                conn.InsertOrReplace(element);
            else
                conn.Insert(element);

            if (recursive) {
                objectCache.Add(element);
                conn.InsertChildrenRecursive(element, replace, recursive, objectCache);
            }

            conn.UpdateWithChildren(element);
        }

        static void InsertChildrenRecursive(this SQLiteConnection conn, object element, bool replace, bool recursive, ISet<object> objectCache = null) {
            if (element == null)
                return;

            objectCache = objectCache ?? new HashSet<object>();
            foreach (var relationshipProperty in element.GetType().GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

                // Ignore read-only attributes and process only 'CascadeInsert' attributes
                if (relationshipAttribute.ReadOnly || !relationshipAttribute.IsCascadeInsert)
                    continue;

                var value = relationshipProperty.GetValue(element, null);
                var enumerable = value as IEnumerable;
                if (enumerable != null)
                    conn.InsertAllWithChildrenRecursive(enumerable, replace, recursive, objectCache);
                else if (value != null)
                    conn.InsertWithChildrenRecursive(value, replace, recursive, objectCache);
            }
        }

        private static void RefreshForeignKeys<T>(T element)
        {
            var type = element.GetType();
            foreach (var relationshipProperty in type.GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

                // Ignore read-only attributes
                if (relationshipAttribute.ReadOnly)
                    continue;

                if (relationshipAttribute is OneToOneAttribute || relationshipAttribute is ManyToOneAttribute)
                {
                    var foreignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
                    if (foreignKeyProperty != null)
                    {
                        EnclosedType enclosedType;
                        var entityType = relationshipProperty.GetEntityType(out enclosedType);
                        var destinationPrimaryKeyProperty = entityType.GetPrimaryKey();
                        Assert(enclosedType == EnclosedType.None, type, relationshipProperty,  "ToOne relationships cannot be lists or arrays");
                        Assert(destinationPrimaryKeyProperty != null, type, relationshipProperty,  "Found foreign key but destination Type doesn't have primary key");

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
                    TextBlobOperations.UpdateTextBlobProperty(element, relationshipProperty);
                }
            }
        }


        private static void UpdateInverseForeignKeys<T>(this SQLiteConnection conn, T element)
        {
            foreach (var relationshipProperty in element.GetType().GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

                // Ignore read-only attributes
                if (relationshipAttribute.ReadOnly)
                    continue;

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
            var type = element.GetType();

            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var originPrimaryKeyProperty = type.GetPrimaryKey();
            var inversePrimaryKeyProperty = entityType.GetPrimaryKey();
            var inverseForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);

            Assert(enclosedType != EnclosedType.None, type, relationshipProperty,  "OneToMany relationships must be List or Array of entities");
            Assert(originPrimaryKeyProperty != null, type, relationshipProperty,  "OneToMany relationships require Primary Key in the origin entity");
            Assert(inversePrimaryKeyProperty != null, type, relationshipProperty,  "OneToMany relationships require Primary Key in the destination entity");
            Assert(inverseForeignKeyProperty != null, type, relationshipProperty,  "Unable to find foreign key for OneToMany relationship");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);
            if (inverseProperty != null)
            {
                EnclosedType inverseEnclosedType;
                var inverseEntityType = inverseProperty.GetEntityType(out inverseEnclosedType);
                Assert(inverseEnclosedType == EnclosedType.None, type, relationshipProperty,  "OneToMany inverse relationship shouldn't be List or Array");
                Assert(inverseEntityType == type, type, relationshipProperty,  "OneToMany inverse relationship is not the expected type");
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
            var childrenPlaceHolders = string.Join(",", Enumerable.Repeat("?", childrenKeyList.Count));
            var query = string.Format("update {0} set {1} = ? where {2} in ({3})",
                entityType.GetTableName(), inverseForeignKeyProperty.GetColumnName(), inversePrimaryKeyProperty.GetColumnName(), childrenPlaceHolders);
            var parameters = new List<object> { keyValue };
            parameters.AddRange(childrenKeyList);
            conn.Execute(query, parameters.ToArray());

            // Delete previous relationships
            var deleteQuery = string.Format("update {0} set {1} = NULL where {1} == ? and {2} not in ({3})",
                entityType.GetTableName(), inverseForeignKeyProperty.GetColumnName(), inversePrimaryKeyProperty.GetColumnName(), childrenPlaceHolders);
            conn.Execute(deleteQuery, parameters.ToArray());
        }

        private static void UpdateOneToOneInverseForeignKey<T>(this SQLiteConnection conn, T element, PropertyInfo relationshipProperty)
        {
            var type = element.GetType();

            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var originPrimaryKeyProperty = type.GetPrimaryKey();
            var inversePrimaryKeyProperty = entityType.GetPrimaryKey();
            var inverseForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);

            Assert(enclosedType == EnclosedType.None, type, relationshipProperty,  "OneToOne relationships cannot be List or Array of entities");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);
            if (inverseProperty != null)
            {
                EnclosedType inverseEnclosedType;
                var inverseEntityType = inverseProperty.GetEntityType(out inverseEnclosedType);
                Assert(inverseEnclosedType == EnclosedType.None, type, relationshipProperty,  "OneToOne inverse relationship shouldn't be List or Array");
                Assert(inverseEntityType == type, type, relationshipProperty,  "OneToOne inverse relationship is not the expected type");
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
                    entityType.GetTableName(), inverseForeignKeyProperty.GetColumnName(), inversePrimaryKeyProperty.GetColumnName());
                conn.Execute(query, keyValue, childKey);

                // Delete previous relationships
                var deleteQuery = string.Format("update {0} set {1} = NULL where {1} == ? and {2} not in (?)",
                    entityType.GetTableName(), inverseForeignKeyProperty.GetColumnName(), inversePrimaryKeyProperty.GetColumnName());
                conn.Execute(deleteQuery, keyValue, childKey ?? "");
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

            Assert(enclosedType != EnclosedType.None, type, relationshipProperty,  "ManyToMany relationship must be a List or Array");
            Assert(currentEntityPrimaryKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship origin must have Primary Key");
            Assert(otherEntityPrimaryKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship destination must have Primary Key");
            Assert(intermediateType != null, type, relationshipProperty,  "ManyToMany relationship intermediate type cannot be null");
            Assert(currentEntityForeignKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship origin must have a foreign key defined in the intermediate type");
            Assert(otherEntityForeignKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship destination must have a foreign key defined in the intermediate type");

            var primaryKey = currentEntityPrimaryKeyProperty.GetValue(element, null);

            // Obtain the list of children keys
            var childList = (IEnumerable)relationshipProperty.GetValue(element, null);
            var childKeyList = (from object child in childList ?? new List<object>()
                               select otherEntityPrimaryKeyProperty.GetValue(child, null)).ToList();

            // Check for already existing relationships
            var childrenPlaceHolders = string.Join(",", Enumerable.Repeat("?", childKeyList.Count));
            var currentChildrenQuery = string.Format("select {0} from {1} where {2} == ? and {0} in ({3})",
                otherEntityForeignKeyProperty.GetColumnName(), intermediateType.GetTableName(), currentEntityForeignKeyProperty.GetColumnName(), childrenPlaceHolders);
            var parameters = new List<object>{ primaryKey };
            parameters.AddRange(childKeyList);
            var currentChildKeyList =
                from object child in conn.Query(conn.GetMapping(intermediateType), currentChildrenQuery, parameters.ToArray())
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
                intermediateType.GetTableName(), currentEntityForeignKeyProperty.GetColumnName(),
                otherEntityForeignKeyProperty.GetColumnName(), childrenPlaceHolders);
            conn.Execute(deleteQuery, parameters.ToArray());
        }

        private static void DeleteAllIds(this SQLiteConnection conn, object[] primaryKeyValues, string entityName, string primaryKeyName) {
            if (primaryKeyValues == null || primaryKeyValues.Length == 0)
                return;

            var placeholdersString = string.Join(",", Enumerable.Repeat("?", primaryKeyValues.Length));
            var deleteQuery = string.Format("delete from {0} where {1} in ({2})", entityName, primaryKeyName, placeholdersString);

            conn.Execute(deleteQuery, primaryKeyValues);
        }
            
        static void Assert(bool assertion, Type type, PropertyInfo property, string message) {
            if (EnableRuntimeAssertions && !assertion)
                throw new IncorrectRelationshipException(type.Name, property.Name, message);
        }
        #endregion
    }
}