using System;
using System.Windows;
using System.Windows.Controls;

namespace Microcontroller_Music
{
    /// <summary>
    /// Interaction logic for ExportPopup.xaml
    /// The popup to get all information for any microcontroller output
    /// </summary>
    public partial class ExportPopup : Window
    {
        //song is needed to get track titles
        private readonly Song songToTalkAbout;
        //output is the integer that tells the program which type of microcontroller output is being used
        private readonly int output;
        //an extra combobox and label for when the program needs to know whether to read high or low
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
                    //arduino
                case 1:
                    //give the option to play immediately when powered
                    ButtonPin.Items.Add(new ComboBoxItem()
                    {
                        Content = "No Button"
                    });
                    //buttons 2-13 are usable for both speaker and button
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
                    //sets up the label and combobox for reading high or low
                    MakeRead();
                    break;
                    //pico
                case 2:
                    //option for no button - start automatically
                    ButtonPin.Items.Add(new ComboBoxItem()
                    {
                        Content = "No Button"
                    });
                    //pins 0-22 are usable - these are the GPIO pins not the numbered pins - avoids selecting ground
                    for (int i = 0; i < 23; i++)
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
                    //sets up UP/DOWN option
                    MakeRead();
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

        //returns the boolean of whether to read high/low or up/down
        public bool GetButtonRead()
        {
            if (output == 1)
            {
                return (buttonReadBox.SelectedIndex == 1);
            }
            else return false;
        }

        //sets up high/low/up/down combobox and label
        public void MakeRead()
        {
            //add selectable values to the combobox
            //values needed for arduino
            if (output == 1)
            {
                buttonReadBox.Items.Add("LOW");
                buttonReadBox.Items.Add("HIGH");
            }
            //values needed for pico
            else if (output == 2)
            {
                buttonReadBox.Items.Add("PULL_DOWN");
                buttonReadBox.Items.Add("PULL_UP");
            }
            //button defaults to LOW (arduino) or DOWN (pico)
            buttonReadBox.SelectedIndex = 0;
            //sizing and positioning of the label and box in grid
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
            //speaker pin defaults to 11 (arduino) or 9 (pico)
            SpeakerPin.SelectedIndex = 9;
        }
    }
}
