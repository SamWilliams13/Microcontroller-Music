using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Microcontroller_Music
{
    /// <summary>
    /// Interaction logic for TitleChange.xaml
    /// </summary>
    public partial class TitleChange : Window
    {
        public TitleChange()
        {
            InitializeComponent();
        }

        //handles pressing the OK button so the main program can continue on
        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //returns the new title written in the text box.
        public string GetNewTItle()
        {
            return textBox.Text;
        }
    }
}
