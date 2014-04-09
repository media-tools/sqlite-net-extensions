using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if PCL
using SQLite.Net;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.XamarinIOS;
#else
using Cirrious.MvvmCross.Community.Plugins.Sqlite;
using Community.SQLite;
#endif

namespace SQLiteNetExtensions.IntegrationTests
{
    /// <summary>
    /// Helper methods for the integration tests. 
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Returns the proper database file path to initialize the SQLite connection. 
        /// </summary>
        public static string DatabaseFilePath
        {
            get
            {
                #if SILVERLIGHT
                var path = "database.db3";
                #else
                    
                #if __ANDROID__
                string libraryPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); ;
                #else
                // we need to put in /Library/ on iOS5.1 to meet Apple's iCloud terms
                // (they don't want non-user-generated data in Documents)
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Documents folder
                string libraryPath = System.IO.Path.Combine(documentsPath, "../Library/");
                #endif
                var path = System.IO.Path.Combine(libraryPath, "database.db3");
                #endif
                return path;
            }
        }

        public static SQLiteConnection CreateConnection() {
            #if PCL
            return new SQLiteConnection(new SQLitePlatformIOS(), DatabaseFilePath);
            #else
            return new SQLiteConnection(DatabaseFilePath);
            #endif
        }
    }
}