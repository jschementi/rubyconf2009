#define HOSTING

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Microsoft.Scripting.Hosting;

namespace GameEngine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public App() {
#if HOSTING
            this.DispatcherUnhandledException += (s, a) => {
                a.Handled = true;
                MainWindow w = (MainWindow) this.MainWindow;
                string formattedEx = w.Runtime.GetEngine("ruby").GetService<ExceptionOperations>().FormatException(a.Exception);
                System.Windows.MessageBox.Show(formattedEx);
            };
#endif
        }
    }
}
