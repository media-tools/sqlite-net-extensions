using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions.TextBlob;
using System.Linq.Expressions;
using SQLiteNetExtensions.Exceptions;

#if USING_MVVMCROSS
using SQLiteConnection = Cirrious.MvvmCross.Community.Plugins.Sqlite.ISQLiteConnection;
#elif PCL
using SQLite.Net;
using SQLite.Net.Attributes;
#else
using SQLite;
#endif

namespace SQLiteNetExtensions.Extensions
{
    public static class ReadOperations
    {
        // Enable to allow descriptive error descriptions on incorrect relationships
        public static bool EnableRuntimeAssertions = true;

        public static T GetWithChildren<T>(this SQLiteConnection conn, object pk) where T : new()
        {
            var element = conn.Get<T>(pk);
            conn.GetChildren(element);
            return element;
        }

        public static T FindWithChildren<T>(this SQLiteConnection conn, object pk) where T : new()
        {
            var element = conn.Find<T>(pk);
            if (!EqualityComparer<T>.Default.Equals(element, default(T)))
                conn.GetChildren(element);
            return element;
        }

        public static void GetChildren<T>(this SQLiteConnection conn, T element) where T : new()
        {
            foreach (var relationshipProperty in typeof (T).GetRelationshipProperties())
            {
                conn.GetChild(element, relationshipProperty);
            }
        }

        public static void GetChild<T>(this SQLiteConnection conn, T element, string relationshipProperty)
        {
            conn.GetChild(element, typeof (T).GetProperty(relationshipProperty));
        }

        public static void GetChild<T>(this SQLiteConnection conn, T element, Expression<Func<T, object>> expression)
        {
            conn.GetChild(element, ReflectionExtensions.GetProperty(expression));
        }

        public static void GetChild<T>(this SQLiteConnection conn, T element, PropertyInfo relationshipProperty)
        {
            var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

            if (relationshipAttribute is OneToOneAttribute)
            {
                conn.GetOneToOneChild(element, relationshipProperty);
            }
            else if (relationshipAttribute is OneToManyAttribute)
            {
                conn.GetOneToManyChildren(element, relationshipProperty);
            }
            else if (relationshipAttribute is ManyToOneAttribute)
            {
                conn.GetManyToOneChild(element, relationshipProperty);
            }
            else if (relationshipAttribute is ManyToManyAttribute)
            {
                conn.GetManyToManyChildren(element, relationshipProperty);
            }
            else if (relationshipAttribute is TextBlobAttribute)
            {
                TextBlobOperations.GetTextBlobChild(element, relationshipProperty);
            }
        }

        #region Private methods
        private static void GetOneToOneChild<T>(this SQLiteConnection conn, T element,
                                                PropertyInfo relationshipProperty)
        {
            var type = typeof (T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Assert(enclosedType == EnclosedType.None, type, relationshipProperty, "OneToOne relationship cannot be of type List or Array");

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            Assert(currentEntityPrimaryKeyProperty != null || otherEntityPrimaryKeyProperty != null, type, relationshipProperty, 
                         "At least one entity in a OneToOne relationship must have Primary Key");

            var currentEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
            var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
            Assert(currentEntityForeignKeyProperty != null || otherEntityForeignKeyProperty != null, type, relationshipProperty, 
                         "At least one entity in a OneToOne relationship must have Foreign Key");

            var hasForeignKey = otherEntityPrimaryKeyProperty != null && currentEntityForeignKeyProperty != null;
            var hasInverseForeignKey = currentEntityPrimaryKeyProperty != null && otherEntityForeignKeyProperty != null;
            Assert(hasForeignKey || hasInverseForeignKey, type, relationshipProperty, 
                         "Missing either ForeignKey or PrimaryKey for a complete OneToOne relationship");

            var tableMapping = conn.GetMapping(entityType);
            Assert(tableMapping != null, type, relationshipProperty,  "There's no mapping table for OneToOne relationship");

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
                    var query = string.Format("select * from {0} where {1} = ? limit 1", entityType.GetTableName(),
                        otherEntityForeignKeyProperty.GetColumnName());
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


        private static void GetManyToOneChild<T>(this SQLiteConnection conn, T element,
                                                 PropertyInfo relationshipProperty)
        {
            var type = typeof (T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Assert(enclosedType == EnclosedType.None, type, relationshipProperty,  "ManyToOne relationship cannot be of type List or Array");

            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            Assert(otherEntityPrimaryKeyProperty != null, type, relationshipProperty, 
                         "ManyToOne relationship destination must have Primary Key");

            var currentEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
            Assert(currentEntityForeignKeyProperty != null, type, relationshipProperty,  "ManyToOne relationship origin must have Foreign Key");

            var tableMapping = conn.GetMapping(entityType);
            Assert(tableMapping != null, type, relationshipProperty,  "There's no mapping table for OneToMany relationship destination");

            object value = null;
            var foreignKeyValue = currentEntityForeignKeyProperty.GetValue(element, null);
            if (foreignKeyValue != null)
            {
                value = conn.Find(foreignKeyValue, tableMapping);
            }

            relationshipProperty.SetValue(element, value, null);

        }

        private static void GetOneToManyChildren<T>(this SQLiteConnection conn, T element,
                                                    PropertyInfo relationshipProperty)
        {
            var type = typeof (T);
            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            Assert(enclosedType != EnclosedType.None, type, relationshipProperty,  "OneToMany relationship must be a List or Array");

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            Assert(currentEntityPrimaryKeyProperty != null, type, relationshipProperty,  "OneToMany relationship origin must have Primary Key");

            var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
            Assert(otherEntityForeignKeyProperty != null, type, relationshipProperty, 
                         "OneToMany relationship destination must have Foreign Key to the origin class");

            var tableMapping = conn.GetMapping(entityType);
            Assert(tableMapping != null, type, relationshipProperty,  "There's no mapping table for OneToMany relationship destination");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);

            IEnumerable values = null;
            var primaryKeyValue = currentEntityPrimaryKeyProperty.GetValue(element, null);
            if (primaryKeyValue != null)
            {
                var query = string.Format("select * from {0} where {1} = ?", entityType.GetTableName(),
                    otherEntityForeignKeyProperty.GetColumnName());
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

        private static void GetManyToManyChildren<T>(this SQLiteConnection conn, T element,
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

            Assert(enclosedType != EnclosedType.None, type, relationshipProperty,  "ManyToMany relationship must be a List or Array");
            Assert(currentEntityPrimaryKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship origin must have Primary Key");
            Assert(otherEntityPrimaryKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship destination must have Primary Key");
            Assert(intermediateType != null, type, relationshipProperty,  "ManyToMany relationship intermediate type cannot be null");
            Assert(currentEntityForeignKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship origin must have a foreign key defined in the intermediate type");
            Assert(otherEntityForeignKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship destination must have a foreign key defined in the intermediate type");
            Assert(tableMapping != null, type, relationshipProperty,  "There's no mapping table defined for ManyToMany relationship origin");

            IEnumerable values = null;
            var primaryKeyValue = currentEntityPrimaryKeyProperty.GetValue(element, null);
            if (primaryKeyValue != null)
            {
                // Obtain the relationship keys
                var keysQuery = string.Format("select {0} from {1} where {2} = ?", otherEntityForeignKeyProperty.GetColumnName(),
                    intermediateType.GetTableName(), currentEntityForeignKeyProperty.GetColumnName());

                var query = string.Format("select * from {0} where {1} in ({2})", entityType.GetTableName(),
                    otherEntityPrimaryKeyProperty.GetColumnName(), keysQuery);

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
            
        static void Assert(bool assertion, Type type, PropertyInfo property, string message) {
            if (EnableRuntimeAssertions && !assertion)
                throw new IncorrectRelationshipException(type.Name, property.Name, message);

        }
        #endregion
    }
}
