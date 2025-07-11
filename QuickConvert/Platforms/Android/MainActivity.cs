using Android.App;
using Android.Content.PM;
using Android.OS;

namespace QuickConvert
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Enable assembly preloading
            JavaSystem.LoadLibrary("System.Security.Cryptography.Native.Android");
            
            base.OnCreate(savedInstanceState);
            
            // Initialize AndroidX Core through reflection to avoid direct dependency
            InitializeAndroidX();
        }
        
        private void InitializeAndroidX()
        {
            try
            {
                // Use reflection to avoid direct type references that might cause compatibility issues
                var contextCompatType = Java.Lang.Class.ForName("androidx.core.content.ContextCompat");
                var getMainExecutorMethod = contextCompatType.GetMethod("getMainExecutor", Java.Lang.Class.FromType(typeof(Android.Content.Context)));
                getMainExecutorMethod.Invoke(null, this);
                
                System.Diagnostics.Debug.WriteLine("Successfully initialized AndroidX Core");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing AndroidX Core: {ex.Message}");
            }
        }
    }
}

// Helper class to access Java System class
internal static class JavaSystem
{
    public static void LoadLibrary(string name)
    {
        Java.Lang.JavaSystem.LoadLibrary(name);
    }
}

