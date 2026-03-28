using System.Configuration;
using System.Data;
using System.Windows;
using NbnStock.Core.Data;

namespace NbnStock.Windows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DatabaseInitialiser.Initialise();
        }
    }

}
