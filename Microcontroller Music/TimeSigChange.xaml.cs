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
    /// Interaction logic for TimeSigChange.xaml
    /// </summary>
    public partial class TimeSigChange : Window
    {
        //stores the previously selected index for continuity
        int previousTop;
        public TimeSigChange()
        {
            InitializeComponent();
        }

        //when the bottom number is changed the available top numbers change
        private void TimeSigBottom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //update selected index before it breaks
            previousTop = TopNumber.SelectedIndex;
            //remove the previously available top numbers
            TopNumber.Items.Clear();
            //determine the highest top number from the bottom number's index
            int max = 0;
            switch (BottomNumber.SelectedIndex)
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
            //if the previously selected index will no longer exist then update it to the max possible
            if (previousTop > max - 2)
            {
                previousTop = max - 2;
            }
            //populate the combobox with numbers from 2 to max
            for (int i = 2; i <= max; i++)
            {
                TopNumber.Items.Add(new ComboBoxItem() { Content = i });
            }
            //select the item closest to the one user had selected
            TopNumber.SelectedIndex = previousTop;
        }

        //when ok button is pressed, allow the main window to continue
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //returns the top number by adding 2 to index
        public int GetTopNumber()
        {
            return TopNumber.SelectedIndex + 2;
        }

        //returns the bottom number using exponentials to calculate from index
        public int GetBottomNumber()
        {
            return (int)Math.Pow(2, BottomNumber.SelectedIndex + 1);
        }
    }
}
