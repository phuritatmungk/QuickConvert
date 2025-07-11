using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using QuickConvert.Services;
using System.Diagnostics;

namespace QuickConvert
{
    public partial class AppShell : Shell
    {
        public static int LastTabIndex { get; private set; } = 0;
        
        public AppShell()
        {
            try
            {
                InitializeComponent();
                
                Debug.WriteLine("AppShell initialized successfully");
                
                Navigated += AppShell_Navigated;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error initializing AppShell: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void AppShell_Navigated(object? sender, ShellNavigatedEventArgs e)
        {
            try
            {
                if (CurrentItem != null && Items.Count > 0 && Items[0] is TabBar tabBar)
                {
                    for (int i = 0; i < tabBar.Items.Count; i++)
                    {
                        if (tabBar.Items[i].Route == CurrentItem.Route)
                        {
                            LastTabIndex = i;
                            Debug.WriteLine($"Stored LastTabIndex = {LastTabIndex}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error tracking tab index: {ex.Message}");
            }
        }
    }
}
