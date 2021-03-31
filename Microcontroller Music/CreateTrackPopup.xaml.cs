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
    /// Interaction logic for CreateTrackPopup.xaml
    /// </summary>
    public partial class CreateTrackPopup : Window
    {
        public CreateTrackPopup()
        {
            InitializeComponent();
        }

        //returns the text in the textbox (title)
        public string GetTitle()
        {
            return TrackTitle.Text.ToString();
        }

        //returns the boolean of whether the combobox has treble selected
        public bool GetTreble()
        {
            return KeyBox.SelectedIndex == 0;
        }

        //when ok button is pressed, allow main window to continue.
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
