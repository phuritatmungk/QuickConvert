using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using QuickConvert.Models;
using QuickConvert.Helpers;
using System.Diagnostics;

namespace QuickConvert.Services
{
    public class DatabaseService
    {
        private static readonly string _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ConverterDatabase.db3");

        private SQLiteAsyncConnection _db = null!;

        public DatabaseService()
        {
            try
            {
                var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
                _db = new SQLiteAsyncConnection(_dbPath, flags);
                
                Debug.WriteLine($"Database connection created at: {_dbPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating database connection: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                try
                {
                    _db = new SQLiteAsyncConnection(":memory:");
                    Debug.WriteLine("Created in-memory fallback database");
                }
                catch
                {
                    Debug.WriteLine("Failed to create even an in-memory database");
                }
            }
        }
        
        public async Task InitializeAsync()
        {
            Debug.WriteLine("DatabaseService.InitializeAsync called");
            
            if (_db == null)
            {
                Debug.WriteLine("Cannot initialize database: connection is null");
                return;
            }

            try
            {
                SQLiteHelper.EnsureInitialized();
                Debug.WriteLine("SQLite initialized successfully");
                
                var result1 = await _db.CreateTableAsync<WalletDbItem>();
                Debug.WriteLine($"WalletDbItem table creation result: {result1}");
                
                var result2 = await _db.CreateTableAsync<BookmarkDbItem>();
                Debug.WriteLine($"BookmarkDbItem table creation result: {result2}");
                
                try
                {
                    var bookmarkCount = await _db.Table<BookmarkDbItem>().CountAsync();
                    Debug.WriteLine($"BookmarkDbItem table has {bookmarkCount} records");
                    
                    var walletCount = await _db.Table<WalletDbItem>().CountAsync();
                    Debug.WriteLine($"WalletDbItem table has {walletCount} records");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error verifying tables: {ex.Message}");
                }
                
                Debug.WriteLine("Database initialization completed successfully");
            }
            catch (SQLiteException sqlEx)
            {
                Debug.WriteLine($"SQLite error creating database tables: {sqlEx.Message}");
                Debug.WriteLine($"SQLite error code: {sqlEx.Result}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating database tables: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        public async Task<List<WalletDbItem>> GetWalletItemsAsync()
        {
            try
            {
                return await _db.Table<WalletDbItem>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting wallet items: {ex.Message}");
                return new List<WalletDbItem>();
            }
        }

        public async Task<int> SaveWalletItemAsync(WalletDbItem item)
        {
            try
            {
                if (item.Id != 0)
                {
                    return await _db.UpdateAsync(item);
                }
                return await _db.InsertAsync(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving wallet item: {ex.Message}");
                return -1;
            }
        }

        public async Task<int> DeleteWalletItemAsync(WalletDbItem item)
        {
            try
            {
                return await _db.DeleteAsync(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting wallet item: {ex.Message}");
                return -1;
            }
        }

        public async Task<int> DeleteWalletItemAsync(int id)
        {
            try
            {
                return await _db.DeleteAsync<WalletDbItem>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting wallet item by ID: {ex.Message}");
                return -1;
            }
        }

        public async Task<List<BookmarkDbItem>> GetBookmarksAsync()
        {
            try
            {
                return await _db.Table<BookmarkDbItem>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting bookmarks: {ex.Message}");
                return new List<BookmarkDbItem>();
            }
        }

        public async Task<int> SaveBookmarkAsync(BookmarkDbItem item)
        {
            try
            {
                Debug.WriteLine($"Attempting to save bookmark for: {item.CurrencyCode}, ID: {item.Id}");
                
                if (item.Id != 0)
                {
                    Debug.WriteLine($"Updating existing bookmark (ID: {item.Id})");
                    return await _db.UpdateAsync(item);
                }
                else
                {
                    var existing = await GetBookmarkByCurrencyCodeAsync(item.CurrencyCode);
                    if (existing != null)
                    {
                        Debug.WriteLine($"Found existing bookmark for {item.CurrencyCode}, using ID: {existing.Id}");
                        item.Id = existing.Id;
                        return existing.Id;
                    }
                    
                    Debug.WriteLine($"Inserting new bookmark for {item.CurrencyCode}");
                    int newId = await _db.InsertAsync(item);
                    Debug.WriteLine($"New bookmark inserted with ID: {newId}");
                    return newId;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving bookmark: {ex.Message}");
                Debug.WriteLine($"Exception stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return -1;
            }
        }

        public async Task<int> DeleteBookmarkAsync(BookmarkDbItem item)
        {
            try
            {
                return await _db.DeleteAsync(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting bookmark: {ex.Message}");
                return -1;
            }
        }

        public async Task<int> DeleteBookmarkAsync(int id)
        {
            try
            {
                return await _db.DeleteAsync<BookmarkDbItem>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting bookmark by ID: {ex.Message}");
                return -1;
            }
        }

        public async Task<BookmarkDbItem> GetBookmarkByCurrencyCodeAsync(string currencyCode)
        {
            try
            {
                return await _db.Table<BookmarkDbItem>()
                    .Where(b => b.CurrencyCode == currencyCode)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting bookmark by currency code: {ex.Message}");
                return null;
            }
        }

        public async Task<WalletDbItem> GetWalletItemByCurrencyAsync(string currency)
        {
            try
            {
                return await _db.Table<WalletDbItem>()
                    .Where(w => w.Currency == currency)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting wallet item by currency: {ex.Message}");
                return null;
            }
        }
    }
} 
