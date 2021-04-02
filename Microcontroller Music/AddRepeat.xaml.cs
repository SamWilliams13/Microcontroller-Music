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
    /// Interaction logic for AddRepeat.xaml
    /// </summary>
    public partial class AddRepeat : Window
    {
        public AddRepeat()
        {
            InitializeComponent();
        }

        //same as all other dialog boxes - allows program to continue
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //if the number entered is a valid number, give that back to program. otherwise give default value.
        public int GetRepeatCount()
        {
            if (Int32.TryParse(RepeatCount.Text, out int result) && result > 0)
            {
                return result;
            }
            else return 1;
        }
    }
}
