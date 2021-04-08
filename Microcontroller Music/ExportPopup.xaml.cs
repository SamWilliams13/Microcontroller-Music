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
    /// Interaction logic for ExportPopup.xaml
    /// </summary>
    public partial class ExportPopup : Window
    {
        private readonly Song songToTalkAbout;
        private readonly int output;
        private readonly ComboBox buttonReadBox = new ComboBox();
        private readonly Label buttonReadLabel = new Label();

        public ExportPopup(Song s, int chosenDevice)
        {
            //set variables and set up comboboxes
            songToTalkAbout = s;
            output = chosenDevice;
            InitializeComponent();
            GenerateTrackCombo();
            GeneratePossibleSpeakerButtonPins();
        }

        //loops through all tracks to add them to combobox as selectable options
        private void GenerateTrackCombo()
        {
            for(int i = 0; i < songToTalkAbout.GetTrackCount(); i++)
            {
                TrackSelector.Items.Add(new ComboBoxItem()
                {
                    Content = songToTalkAbout.GetTrackTitle(i)
                });
            }
            TrackSelector.SelectedIndex = 0;
        }

        //set up the other 2 combo boxes
        private void GeneratePossibleSpeakerButtonPins()
        {
            switch (output)
            {
                //microbit pins
                case 0:
                    for(int i = 0; i < 3; i++)
                    {
                        SpeakerPin.Items.Add(new ComboBoxItem()
                        {
                            Content = i
                        });
                    }
                    //microbit uses the onboard buttons along with the option to forego buttons completely
                    ButtonPin.Items.Add(new ComboBoxItem()
                    {
                        Content = "a"
                    });
                    ButtonPin.Items.Add(new ComboBoxItem()
                    {
                        Content = "b"
                    });
                    ButtonPin.Items.Add(new ComboBoxItem()
                    {
                        Content = "No Button"
                    });
                    SpeakerPin.SelectedIndex = 0;
                    break;
                case 1:
                    ButtonPin.Items.Add(new ComboBoxItem()
                    {
                        Content = "No Button"
                    });
                    for (int i = 2; i < 14; i++)
                    {
                        ButtonPin.Items.Add(new ComboBoxItem()
                        {
                            Content = i
                        });
                        SpeakerPin.Items.Add(new ComboBoxItem()
                        {
                            Content = i
                        });
                    }
                    buttonReadBox.Items.Add("LOW");
                    buttonReadBox.Items.Add("HIGH");
                    buttonReadBox.SelectedIndex = 0;
                    buttonReadBox.Height = 20;
                    buttonReadBox.Width = 422;
                    buttonReadBox.Margin = new Thickness(142, 107, 0, 0);
                    buttonReadLabel.Content = "Button Pin Read: ";
                    buttonReadLabel.Margin = new Thickness(10, 103, 0, 0);
                    buttonReadLabel.Height = 34;
                    buttonReadLabel.Width = 100;
                    buttonReadLabel.HorizontalAlignment = HorizontalAlignment.Left;
                    buttonReadLabel.VerticalAlignment = VerticalAlignment.Top;
                    buttonReadBox.HorizontalAlignment = HorizontalAlignment.Left;
                    buttonReadBox.VerticalAlignment = VerticalAlignment.Top;
                    MainGrid.Children.Add(buttonReadBox);
                    MainGrid.Children.Add(buttonReadLabel);
                    SpeakerPin.SelectedIndex = 9;
                    break;
            }
            ButtonPin.SelectedIndex = 0;
        }

        //logic to close the dialog and continue export
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //returns the button selected by the user
        public string GetButtonPin()
        {
            return ButtonPin.Text;
        }

        //returns the selected pin for the speaker
        public int GetSpeakerPin()
        {
            return Convert.ToInt32(SpeakerPin.Text);
        }

        //returns the index of the track selected by the user
        public int GetTrackIndex()
        {
            return TrackSelector.SelectedIndex;
        }

        public bool GetButtonRead()
        {
            if (output == 1)
            {
                return (buttonReadBox.SelectedIndex == 1);
            }
            else return false;
        }
    }
}
