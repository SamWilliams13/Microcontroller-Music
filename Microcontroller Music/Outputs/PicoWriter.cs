using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace Microcontroller_Music
{
    class PicoWriter : Writer
    {
        //where file is stored
        private string filePath;
        //text to store in file
        private string textOut;
        //pin for buzzer
        private int pin = 11;
        //track in song to play
        private int track = 0;
        //extra tab so indentation difference when button is pressed
        private string buttonAdjustment = "\t";
        //pin for button
        private int buttonNumber = 13;
        //whether to pull up or down - false is down, true is up.
        private bool buttonHigh = false;

        //does nothing extra from base
        public PicoWriter(Song s) : base(s)
        {

        }

        //gets information from the user
        public override bool GetDetails()
        {
            //calls generic popup for exporting - tells it that it is pico
            ExportPopup exportPopup = new ExportPopup(songToConvert, 2);
            //if the user pressed the export button
            if (exportPopup.ShowDialog() == true)
            {
                //assign all the info to the appropriate variables
                track = exportPopup.GetTrackIndex();
                pin = exportPopup.GetSpeakerPin();
                switch (exportPopup.GetButtonPin())
                {
                    //if there is no button selected then certain lines of code are removed from the output
                    case ("No Button"):
                        buttonNumber = -1;
                        buttonAdjustment = "";
                        break;
                        //all other values of button selection refer directly to the button number that is placed in the code.
                    default:
                        buttonNumber = Convert.ToInt32(exportPopup.GetButtonPin());
                        //if there is a button selected, program needs to know whether is pulls up or down to get working code.
                        buttonHigh = exportPopup.GetButtonRead();
                        break;
                }
                //get a save location to output to.
                SaveFileDialog saveFile = new SaveFileDialog()
                {
                    //default filename includes the name of song, name of track and device it works on
                    FileName = songToConvert.GetTitle().Replace(" ", "_") +"_"+ songToConvert.GetTrackTitle(track).Replace(" ", "_") + "_for_Raspberry_Pi_Pico.py",
                    //outputs as a python file
                    Filter = "Python Files (*.py)|*.py"
                };
                //if they selected a filepath then export the song
                if (saveFile.ShowDialog() == true)
                {
                    filePath = saveFile.FileName;
                    return true;
                }
            }
            return false;
        }

        public override void Write()
        {
            //if they correctly entered all needed info
            if (GetDetails())
            {
                //uses parent class to get the frequencies and lengths of note and subsequent silence as 2d list
                List<int[]> tableOfValues = Generate2dFrequencyTable(track);
                //stores the start and end index in 1d array of all repeat starts and ends for later use
                List<int> repeatStarts = new List<int>();
                List<int> repeatEnds = new List<int>();
                //imports to access timing and pwm - start 1d array
                textOut = "from machine import Pin, PWM\nfrom utime import sleep\nbars = [";
                int currentIndex = 0;
                //first repeat start is just the start of the song
                repeatStarts.Add(0);
                //loop  through all bars in track
                for (int i = 0; i < tableOfValues.Count; i++)
                {
                    //used to compare to index at end of track so extra commas aren't added
                    int countAtStart = currentIndex;
                    //if there is a repeat starting at this bar, get the index and add it to repeatStarts as many times as necessary
                    if (songToConvert.DoesARepeatStartorEndOn(i, 0))
                    {
                        int numberOfRepeats = songToConvert.GetNumberOfRepeatsAtStartIndex(i);
                        for (int j = 0; j < numberOfRepeats; j++)
                        {
                            repeatStarts.Add(currentIndex);
                        }
                    }
                    //loop through contents of bar to write them in the format needed for an array in MicroPython
                    for (int j = 0; j < tableOfValues[i].Length; j++)
                    {
                        if(j % 3 == 0)
                        {
                            textOut += tableOfValues[i][j];
                        }
                        //timings need to be in seconds instead of milliseconds like other outputs, so divide by 1000 here.
                        else
                        {
                            textOut += (tableOfValues[i][j] / 1000d);
                        }
                        currentIndex++;
                        if (j < tableOfValues[i].Length - 1)
                        {
                            textOut += ", ";
                        }
                    }
                    //if a repeat ends on this bar, handle it just like repeatStarts earlier
                    if (songToConvert.DoesARepeatStartorEndOn(i, 1))
                    {
                        int numberOfRepeats = songToConvert.GetNumberOfRepeatsAtEndIndex(i);
                        for (int j = 0; j < numberOfRepeats; j++)
                        {
                            repeatEnds.Add(currentIndex);
                        }
                    }
                    //add a comma and new line if any values were added to the array and it's not the last bar
                    if (i < tableOfValues.Count - 1 && countAtStart != currentIndex)
                    {
                        textOut += ", \n";
                    }
                }
                //last repeat end is the end of the song
                repeatEnds.Add(currentIndex);
                //set up a PWM to play the music on the selected pin
                textOut += "]\np = PWM(Pin(" + pin + "))";
                //handle button if necessary
                if (buttonNumber != -1)
                {
                    //set up a button on the given pin with the given direction
                    textOut += "\nb = Pin(" + buttonNumber + ", Pin.IN, Pin.PULL_";
                    if(buttonHigh)
                    {
                        textOut += "UP)";
                    }
                    else
                    {
                        textOut += "DOWN)";
                    }
                }
                //the main function for playing a song - describes a function that
                textOut += "\ndef PlayLoop(start, end):" +
                    //loops from the start to end of a repeat, incrementing by 3 each time (3 values per loop)
                    "\n\tfor i in range(start, end, 3):" +
                    //replaces a note for more silence when needed
                    "\n\t\tif(bars[i] == 0):" +
                    "\n\t\t\tsleep(bars[i+1])" +
                    "\n\t\telse:" +
                    //sets the volume up
                    "\n\t\t\tp.duty_u16(4000)" +
                    //plays the frequency stored for the note
                    "\n\t\t\tp.freq(bars[i])" +
                    //waits for the length of the note
                    "\n\t\t\tsleep(bars[i+1])" +
                    //makes the buzzer silent
                    "\n\t\t\tp.duty_u16(0)" +
                    //waits the length of the break
                    "\n\t\t\tsleep(bars[i+2])" +
                    //start of main loop
                    "\nwhile True:";
                //more button handling - makes sure that song only plays after button pressed
                if (buttonNumber != -1)
                {
                    textOut += "\n\tif(b.value()):";
                }
                //writes out all the loops in order so it goes from start to finish with all repeats
                for (int i = 0; i < repeatStarts.Count; i++)
                {
                    textOut += "\n\t" + buttonAdjustment + "PlayLoop(" + repeatStarts[i] + ", " + repeatEnds[i] + ")";
                }
                //write to file
                File.WriteAllText(filePath, textOut);
                //tell user it is done.
                MainWindow.GenerateErrorDialog("Song Exported", songToConvert.GetTitle());
            }
        }
    }
}
