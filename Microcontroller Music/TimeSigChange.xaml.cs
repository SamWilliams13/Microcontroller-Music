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
        int previousTop;
        int previousSelection;
        public TimeSigChange()
        {
            InitializeComponent();
        }

        private void TimeSigBottom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            previousTop = TopNumber.SelectedIndex;
            TopNumber.Items.Clear();
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
            if (previousTop > max - 2)
            {
                previousTop = max - 2;
            }
            for (int i = 2; i <= max; i++)
            {
                TopNumber.Items.Add(new ComboBoxItem() { Content = i });
            }
            previousSelection = BottomNumber.SelectedIndex;
            TopNumber.SelectedIndex = previousTop;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public int GetTopNumber()
        {
            return TopNumber.SelectedIndex + 2;
        }


        public int GetBottomNumber()
        {
            return (int)Math.Pow(2, BottomNumber.SelectedIndex + 1);
        }
    }
}
