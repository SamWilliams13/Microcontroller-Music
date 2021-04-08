using System;
using System.Windows;

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

        //returns whether the bpm entered is a number, and passes the new bpm out by reference
        public bool GetNewBPM(ref int beepeem)
        {
            return Int32.TryParse(textBox.Text, out beepeem);
        }
    }
}
