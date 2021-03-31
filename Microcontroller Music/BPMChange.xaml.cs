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
    /// Interaction logic for BPMChange.xaml
    /// </summary>
    public partial class BPMChange : Window
    {
        public BPMChange()
        {
            InitializeComponent();
        }

        //allows the program to carry on with BPM change and closes dialog
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public bool GetNewBPM(ref int beepeem)
        {
            return Int32.TryParse(textBox.Text, out beepeem);
        }
    }
}
