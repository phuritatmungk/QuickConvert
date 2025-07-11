using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using QuickConvert.Services;
using QuickConvert.Helpers;
using QuickConvert.Models;
using System;
using System.Diagnostics;

namespace QuickConvert
{
    public partial class App : Application
    {
        public static DatabaseService? DatabaseService { get; private set; }
        private bool _databaseInitialized = false;

        public App()
        {
            try
            {
                InitializeComponent();
                
                bool isDarkMode = Preferences.Get("IsDarkMode", false);
                Current.UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;

                Routing.RegisterRoute("ExchangeRateTrendsPage", typeof(ExchangeRateTrendsPage));
                
                try
                {
                    DatabaseService = IPlatformApplication.Current?.Services.GetService<DatabaseService>();
                    
                    if (DatabaseService != null)
                    {
                        Debug.WriteLine("Database service retrieved from DI container");
                    }
                    else
                    {
                        Debug.WriteLine("WARNING: DatabaseService not available from DI container");
                        
                        try
                        {
                            DatabaseService = new DatabaseService();
                            Debug.WriteLine("Created DatabaseService manually as fallback");
                        }
                        catch (Exception dbEx)
                        {
                            Debug.WriteLine($"Failed to create DatabaseService manually: {dbEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting DatabaseService from DI: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in App constructor: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
            }
        }
        
        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                var window = new Window(new AppShell());
                
                window.Created += (s, e) => {
                    Debug.WriteLine("Window created successfully");
                };
                
                return window;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating window: {ex.Message}");
                
                return new Window(new ContentPage
                {
                    Content = new Label
                    {
                        Text = "Error creating application window: " + ex.Message,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                });
            }
        }
        
        protected override async void OnStart()
        {
            try
            {
                base.OnStart();
                
                Debug.WriteLine("App.OnStart: Beginning initialization");
                
                try
                {
                    Debug.WriteLine("Explicitly initializing SQLite at app startup");
                    SQLiteHelper.EnsureInitialized();
                    
                    if (SQLiteHelper.IsInitialized())
                    {
                        Debug.WriteLine("SQLite initialization confirmed successful");
                    }
                    else
                    {
                        Debug.WriteLine("SQLite initialization failed - helper reports not initialized");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error initializing SQLite: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                
                if (!_databaseInitialized && DatabaseService != null)
                {
                    try
                    {
                        Debug.WriteLine("Calling InitializeAsync on database service");
                        await DatabaseService.InitializeAsync();
                        _databaseInitialized = true;
                        Debug.WriteLine("Database initialized successfully in OnStart");
                        
                        var bookmarks = await DatabaseService.GetBookmarksAsync();
                        Debug.WriteLine($"Database has {bookmarks.Count} bookmarks after initialization");
                        
                        try
                        {
                            Debug.WriteLine("Testing database operations...");
                            
                            var testBookmark = new QuickConvert.Models.BookmarkDbItem { CurrencyCode = "TEST" };
                            var insertResult = await DatabaseService.SaveBookmarkAsync(testBookmark);
                            Debug.WriteLine($"Test bookmark insertion result: {insertResult}");
                            
                            var fetchedBookmark = await DatabaseService.GetBookmarkByCurrencyCodeAsync("TEST");
                            if (fetchedBookmark != null)
                            {
                                Debug.WriteLine($"Successfully fetched test bookmark with ID: {fetchedBookmark.Id}");
                                var deleteResult = await DatabaseService.DeleteBookmarkAsync(fetchedBookmark);
                                Debug.WriteLine($"Test bookmark deletion result: {deleteResult}");
                            }
                            else
                            {
                                Debug.WriteLine("Failed to fetch test bookmark");
                            }
                        }
                        catch (Exception testEx)
                        {
                            Debug.WriteLine($"Error during database test operations: {testEx.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error initializing database in OnStart: {ex.Message}");
                        Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnStart: {ex.Message}");
            }
        }
    }
}