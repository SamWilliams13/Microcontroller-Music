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
using Midi;
using System.Threading;

namespace Microcontroller_Music
{
    /// <summary>
    /// Interaction logic for MIDIDetails.xaml
    /// </summary>
    public partial class MIDIDetails : Window
    {
        //magic numbers defined below:
        private const int comboBoxHeight = 30;
        private const int avoidTaskbarHeight = 40;
        private const int numberOfMidiInstruments = 128;

        //governs how many rows are made
        private readonly int noTracks;
        //used to store the names of tracks taken from song so the entire thing doesn't need to be accessed
        private readonly string[] trackNames;
        //all the labels to tell the user which track they are selecting the instrument for
        private readonly Label[] trackInstructions;
        //all the comboboxes that hold lists of all the instruments for selection in each channel
        private readonly ComboBox[] instrumentSelections;
        //a button, calls the events to stop the box
        private readonly Button okButton;
        //used to differentiate between the dialog closing because it was cancelled or because it was ok'd
        private bool isOK = false;

        //constructor
        public MIDIDetails(int tracks, string[] tracknames) 
        {
            //uses argument, stores it internally
            noTracks = tracks;
            //initialises arrays for the given number of tracks
            trackInstructions = new Label[noTracks]; 
            instrumentSelections = new ComboBox[noTracks];
            //initialises ok button
            okButton = new Button();
            //grabs the track names for use in labels
            trackNames = tracknames;
            //starts up the dialog
            InitializeComponent();
            //sets the size of the array to be big enough to show all boxes
            //+2 is to account for the output device and okButton rows
            this.Height = (noTracks + 2) * comboBoxHeight + avoidTaskbarHeight;
            //fills up the combobox in the top row
            GenerateOutputDevices();
            //gives the combobox a default answer to avoid null error
            OutputDeviceOutput.SelectedIndex = 0;
            //initialises all the labels and comboboxes and fills them with the correct info
            GenerateRows(); 
        }

        //used to return the selected outputdevice so the MIDIWriter doesn't have to look at the components of this box
        public OutputDevice GetOutputDevice() 
        {
            return OutputDevice.InstalledDevices[OutputDeviceOutput.SelectedIndex];
        }

        //similar to GetOutputDevice, returns an array of instruments used in outputDevice setup
        public Instrument[] GetTrackInstruments()
        {
            //new array of instruments that is the same length of the combobox array
            Instrument[] returnedInstruments = new Instrument[instrumentSelections.Length];
            //for each combobox
            for(int i = 0; i < instrumentSelections.Length; i++)
            {
                //take the index of the selected instrument and convert it to a midi instrument
                returnedInstruments[i] = (Instrument)instrumentSelections[i].SelectedIndex;
            }
            //return to the MIDIWriter.
            return returnedInstruments;
        }

        //fills the top combobox with the list out available output devices
        public void GenerateOutputDevices()
        {
            //loop through all the installed output devices
            foreach (OutputDevice output in OutputDevice.InstalledDevices)
            {
                //add the installed outputs to the first combobox
                OutputDeviceOutput.Items.Add(output.Name);
            }
        }

        //creates and populates rows in the grid for track selections and the OK button
        public void GenerateRows()
        {
            //loops noTracks times so there is one combobox per track
            for(int i = 0; i < noTracks; i++)
            {
                // make a new row
                RowDefinition newRow = new RowDefinition
                {
                    //set the row's height to be the same as others
                    Height = new GridLength(comboBoxHeight)
                };
                //add the row to the grid
                MainGrid.RowDefinitions.Add(newRow);
                //initialise this row's label in the array
                trackInstructions[i] = new Label
                {
                    //set the content of the row's label to be the title of the track
                    Content = trackNames[i]
                };
                //fill the combobox with the names of all available instruments for this track
                PopulateInstrumentSelector(i);
                //place the label in the track's row
                    Grid.SetRow(trackInstructions[i], i + 1);
                //place the label in the left column
                    Grid.SetColumn(trackInstructions[i], 0);
                //place the combobox in the track's row
                    Grid.SetRow(instrumentSelections[i], i + 1);
                //place the combobox in the right column
                    Grid.SetColumn(instrumentSelections[i], 1);
                //add the label to the grid
                    MainGrid.Children.Add(trackInstructions[i]);
                //add the combobox to the grid
                    MainGrid.Children.Add(instrumentSelections[i]);
            }
            //this line is repeated here to save a condition being met every time in a loop
            //create the new row
            RowDefinition okRow = new RowDefinition
            {
                //set the row's height to 30 or whatever it ends up being
                Height = new GridLength(comboBoxHeight)
            };
            //add the row to the grid
            MainGrid.RowDefinitions.Add(okRow);
            //make the button say ok
            okButton.Content = "OK";
            //make the button the default value, so that pressing enter should press ok
            okButton.IsDefault = true;
            //set the button to call OKPressed when clicked
            okButton.Click += OKPressed;
            //add the button to the bottom right of the dialog box
            Grid.SetRow(okButton, noTracks + 1);
            Grid.SetColumn(okButton, 1);
            MainGrid.Children.Add(okButton);

        }

        //fills a combobox with the names of all instruments
        public void PopulateInstrumentSelector(int chosenBox)
        {
            //initialise the combobox in the array
            instrumentSelections[chosenBox] = new ComboBox();
            //loop through all instruments
            for (int i = 0; i < numberOfMidiInstruments; i++)
            {
                //add the string version of the instrument to the options in the combobox
                instrumentSelections[chosenBox].Items.Add(((Instrument)i).ToString());
            }
            //set a default value of the combobox to avoid null error (Acoustic Grand Piano)
            instrumentSelections[chosenBox].SelectedIndex = 0;
        }

        //called when the ok button is pressed - makes the dialog box true
        public void OKPressed(object sender, RoutedEventArgs e)
        {
            //isOK stops the "are you sure you want to cancel" from showing up when the dialog closes
            isOK = true;
            //return true - carry on with midi production
            this.DialogResult = true;
        }

        //called when the window closes - however it closes
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //of it closed in some other way than the user wanting to carry on
            if(!isOK)
            {
                //fail out of process
                this.DialogResult = false;
            }
        }
    }
}
