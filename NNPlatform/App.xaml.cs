using System;
using System.Windows;

namespace NNPlatform
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Environment.CurrentDirectory += "..\\..\\..\\..";
            base.OnStartup(e);
        }
    }
}
