using System;
using Microsoft.Data.Sqlite;
using System.IO;

namespace NbnStock.Core.Data
{
    public static class DatabaseInitaliser
    {
        public static void Initialise()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = System.IO.Path.Combine(appDataPath, "NbnStock");
            string databaseFilePath = System.IO.Path.Combine(appFolderPath, "NbnStock.db");
           
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }
        }
    }
}
