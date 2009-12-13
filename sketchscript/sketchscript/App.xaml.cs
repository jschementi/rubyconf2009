using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace SketchScript
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public App() {
            this.DispatcherUnhandledException += (s, a) => {
                a.Handled = true;
                MainWindow w = (MainWindow) this.MainWindow;

                // TODO: format an exception properly
                string formattedEx = a.Exception.ToString();
                
                System.Windows.MessageBox.Show(formattedEx);
            };
        }
    }
}
