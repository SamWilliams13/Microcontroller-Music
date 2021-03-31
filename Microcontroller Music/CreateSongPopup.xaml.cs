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
using System.Text.RegularExpressions;

namespace Microcontroller_Music
{
    /// <summary>
    /// Interaction logic for CreateSongPopup.xaml
    /// </summary>
    public partial class CreateSongPopup : Window
    {
        //integer to ensure that the top number remains constant when it can
        int previousTop = 0;
        public CreateSongPopup()
        {
            InitializeComponent();
        }

        //when ok is pressed allow the main window to continue
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //when the bottom number is changed then the available top numbers need to change
        private void TimeSigBottom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //update previous top
            previousTop = TimeSigTop.SelectedIndex;
            //remove all old ones
            TimeSigTop.Items.Clear();
            int max = 0;
            //set a maximum value based on the bottom number
            switch (TimeSigBottom.SelectedIndex)
            {
                case 0:
                    max = 6;
                    break;
                case 1:
                    max = 12;
                    break;
                case 2:
                    max = 16;
                    break;
            }
            //update previous top to be a number that exists
            if (previousTop > max - 2)
            {
                previousTop = max - 2;
            }
            //loop from 2 to the max number to populate the combobox
            for (int i = 2; i <= max; i++)
            {
                TimeSigTop.Items.Add(new ComboBoxItem() { Content = i });
            }
            //update the selected index to keep it constant.
            TimeSigTop.SelectedIndex = previousTop;
        }
    }
}
