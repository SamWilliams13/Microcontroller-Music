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
        int previousSelection = -1;
        int previousTop = 0;
        public CreateSongPopup()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void TimeSigBottom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            previousTop = TimeSigTop.SelectedIndex;
            TimeSigTop.Items.Clear();
            int max = 0;
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
            if (previousTop > max - 2)
            {
                previousTop = max - 2;
            }
            for (int i = 2; i <= max; i++)
            {
                TimeSigTop.Items.Add(new ComboBoxItem() { Content = i });
            }
            previousSelection = TimeSigBottom.SelectedIndex;
            TimeSigTop.SelectedIndex = previousTop;
        }
    }
}
