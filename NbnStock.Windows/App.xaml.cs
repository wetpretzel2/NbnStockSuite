using Microsoft.Win32;
using NbnStock.Core.Data;
using System;
using System.Windows;

namespace NbnStock.Windows
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DatabaseInitialiser.Initialise();

            string startupTheme = GetWindowsTheme();
            ApplyTheme(startupTheme);
        }

        public void ApplyTheme(string themeName)
        {
            string themePath = themeName switch
            {
                "Dark" => "Themes/DarkTheme.xaml",
                _ => "Themes/LightTheme.xaml"
            };

            var newTheme = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(newTheme);
        }

        private string GetWindowsTheme()
        {
            const string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "AppsUseLightTheme";

            object? value = Registry.CurrentUser.OpenSubKey(registryKey)?.GetValue(valueName);

            if (value is int intValue)
            {
                return intValue == 0 ? "Dark" : "Light";
            }

            return "Light";
        }
    }
}