using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Microcontroller_Music
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void AppStart(object sender, StartupEventArgs e)
        {
            MainWindow window = new MainWindow();
            if(e.Args.Length == 1 && e.Args[0].EndsWith(".mmf"))
            {
                window.OpenFile(e.Args[0]);//return here to do auto-open once open is written :)
            }
            else
            {
                window.NewFile();
            }
            window.Show();
        }
    }
}
