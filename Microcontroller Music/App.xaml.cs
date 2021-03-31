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
            //makes it so that a file can be opened directly from explorer
            if(e.Args.Length == 1 && e.Args[0].EndsWith(".mmf"))
            {
                window.OpenFile(e.Args[0]);
            }
            //if no file is being opened then provide the basic file.
            else
            {
                window.NewFile();
            }
            //open the window
            window.Show();
        }
    }
}
