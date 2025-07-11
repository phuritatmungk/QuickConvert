using Android.App;
using Android.Runtime;
using System.Reflection;

namespace QuickConvert
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            // Force-load dependent assemblies to prevent AOT issues
            PreloadAssemblies();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        private void PreloadAssemblies()
        {
            // Force load Xamarin.AndroidX.Core assembly using reflection
            System.Diagnostics.Debug.WriteLine("Preloading Xamarin.AndroidX.Core...");
            try
            {
                var androidXCoreAssembly = Assembly.Load("Xamarin.AndroidX.Core");
                var activityCompatType = androidXCoreAssembly.GetType("AndroidX.Core.App.ActivityCompat");
                
                if (activityCompatType != null)
                {
                    System.Diagnostics.Debug.WriteLine("Successfully loaded Xamarin.AndroidX.Core");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to find ActivityCompat type");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Xamarin.AndroidX.Core: {ex.Message}");
            }

            // Load Microsoft.Maui.Essentials explicitly
            try
            {
                System.Diagnostics.Debug.WriteLine("Preloading Microsoft.Maui.Essentials...");
                var assembly = Assembly.Load("Microsoft.Maui.Essentials");
                System.Diagnostics.Debug.WriteLine("Successfully loaded Microsoft.Maui.Essentials");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Microsoft.Maui.Essentials: {ex.Message}");
            }
        }
    }
}

