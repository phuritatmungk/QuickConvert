using System;
using System.Diagnostics;

namespace QuickConvert.Helpers
{
    public static class SQLiteHelper
    {
        private static readonly object _lockObject = new object();
        private static bool _initialized = false;
        
        public static void EnsureInitialized()
        {
            if (_initialized)
            {
                Debug.WriteLine("SQLite already initialized, skipping");
                return;
            }
            
            lock(_lockObject)
            {
                if (_initialized)
                {
                    Debug.WriteLine("SQLite already initialized inside lock, skipping");
                    return;
                }
                    
                try
                {
                    Debug.WriteLine("Attempting to initialize SQLite...");
                    
                    try
                    {
                        SQLitePCL.Batteries_V2.Init();
                        Debug.WriteLine("SQLite initialized using Batteries_V2.Init()");
                    }
                    catch (Exception exV2)
                    {
                        Debug.WriteLine($"Error with Batteries_V2.Init(): {exV2.Message}, trying fallback method");
                        
                        SQLitePCL.Batteries.Init();
                        Debug.WriteLine("SQLite initialized using fallback Batteries.Init()");
                    }
                    
                    try
                    {
                        var conn = new SQLite.SQLiteConnection(":memory:");
                        var versionInfo = conn.ExecuteScalar<string>("SELECT sqlite_version()");
                        Debug.WriteLine($"SQLite version: {versionInfo}");
                        conn.Close();
                    }
                    catch (Exception vEx)
                    {
                        Debug.WriteLine($"Could not get SQLite version: {vEx.Message}");
                    }
                    
                    _initialized = true;
                    Debug.WriteLine("SQLite initialization completed and marked as initialized");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR initializing SQLite: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    
                }
            }
        }
        
        public static bool IsInitialized() 
        {
            return _initialized;
        }
    }
} 
