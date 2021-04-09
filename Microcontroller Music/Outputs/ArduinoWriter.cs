using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace Microcontroller_Music
{
    class ArduinoWriter : Writer
    {
        //where file is stored
        string filePath;
        //text to store in file
        string textOut;
        //pin for buzzer
        int pin = 11;
        //track in song to play
        int track = 0;
        //extra close bracked and new line for when a button is used. placed at end of file.
        string buttonAdjustment = "\n}";
        //pin for button
        int buttonNumber = 13;
        //whether reading HIGH or LOW means the button has been pressed. false is low, true is high.
        bool buttonHigh = false;

        //constructor just calls base
        public ArduinoWriter(Song s) : base(s)
        {

        }

        //gets all info needed from the user to write the program
        public override bool GetDetails()
        {
            //uses default microcontroller popup and tells it to specify for Arduino - needed for pins and high/low option
            ExportPopup exportPopup = new ExportPopup(songToConvert, 1);
            //if the user presses export, carry on with the export process
            if (exportPopup.ShowDialog() == true)
            {
                //set track and speaker pin to relevant variables
                track = exportPopup.GetTrackIndex();
                pin = exportPopup.GetSpeakerPin();
                //button handling
                switch (exportPopup.GetButtonPin())
                {
                    //if no button, set pin to -1 so that those lines of code aren't added
                    case ("No Button"):
                        buttonNumber = -1;
                        //remove the adjustment for correct syntax
                        buttonAdjustment = "";
                        break;
                        //set the button pin number to the selected pin, and get the value of HIGH/LOW
                    default:
                        buttonNumber = Convert.ToInt32(exportPopup.GetButtonPin());
                        buttonHigh = exportPopup.GetButtonRead();
                        break;
                }
                SaveFileDialog saveFile = new SaveFileDialog()
                {
                    //default filename includes the title of song and track and the supported microcontroller
                    FileName = songToConvert.GetTitle().Replace(" ", "") + songToConvert.GetTrackTitle(track).Replace(" ", "") + "ForArduinoUno.ino",
                    //.ino file needed
                    Filter = "Arduino Files (*.ino)|*.ino"
                };
                //if they select a save file then carry on with export
                if (saveFile.ShowDialog() == true)
                {
                    filePath = saveFile.FileName;
                    return true;
                }
            }
            return false;
        }

        //writes the program in c++
        public override void Write()
        {
            //get the relevant details
            if (GetDetails())
            {
                //use parent class to get 2d array of frequency, length in ms, rest time in ms
                List<int[]> tableOfValues = Generate2dFrequencyTable(track);
                //lists for the 1d index of all starts and ends of repeats
                List<int> repeatStarts = new List<int>();
                List<int> repeatEnds = new List<int>();
                //start of file, declare array
                textOut = "const int bars[] = {";
                //index to track how many items have been added to array
                int currentIndex = 0;
                //first repeat start is the start of the song
                repeatStarts.Add(0);
                //loop through all bars
                for (int i = 0; i < tableOfValues.Count; i++)
                {
                    //integer to store index of notes in array at start of bar. compared to index at end of bar to avoid extra commas being added.
                    int countAtStart = currentIndex;
                    //if a repeat starts on this bar, store the index of the start of the bar in the list as many times as it is repeated
                    if (songToConvert.DoesARepeatStartorEndOn(i, 0))
                    {
                        int numberOfRepeats = songToConvert.GetNumberOfRepeatsAtStartIndex(i);
                        for (int j = 0; j < numberOfRepeats; j++)
                        {
                            repeatStarts.Add(currentIndex);
                        }
                    }
                    //loop through all values stored in the bar to write them to the string in the correct format.
                    for (int j = 0; j < tableOfValues[i].Length; j++)
                    {
                        textOut += tableOfValues[i][j];
                        currentIndex++;
                        if (j < tableOfValues[i].Length - 1)
                        {
                            textOut += ", ";
                        }
                    }
                    //if a repeat ends on this bar, add the index of the end of the bar to the list as many times as needed.
                    if (songToConvert.DoesARepeatStartorEndOn(i, 1))
                    {
                        int numberOfRepeats = songToConvert.GetNumberOfRepeatsAtEndIndex(i);
                        for (int j = 0; j < numberOfRepeats; j++)
                        {
                            repeatEnds.Add(currentIndex);
                        }
                    }
                    //add a comma and new line if the bar was not empty or the last bar.
                    if (i < tableOfValues.Count - 1 && countAtStart != currentIndex)
                    {
                        textOut += ", \n";
                    }
                }
                //last value in repeatEnds is the end of the song
                repeatEnds.Add(currentIndex);
                //end the array and set declare the value of the speaker pin
                textOut += "};\nint p = " + pin + ";";
                if (buttonNumber != -1) textOut += "\nint b = " + buttonNumber + ";";
                //the main part of the program:
                //set up the pins
                textOut += "\nvoid setup()" +
                    "\n{" +
                    "\npinMode(p, OUTPUT);" +
                    "\npinMode(b, INPUT);" +
                    "\n}" +
                    //function to play all the notes between a start and end index (exclusive)
                    "\nvoid PlayTune(int establishment, int conclusion)" +
                    "\n{" +
                    //loop from start to end, incrementing by 3
                    "\nfor (int i = establishment; i < conclusion; i += 3)" +
                    "\n{" +
                    //if the frequency of the note is 0 then play nothing (it is a rest)
                    "\nif (bars[i] == 0)" +
                    "\n{\nnoTone(p);" +
                    "\n}" +
                    //otherwise play the frequency for the length
                    "\nelse" +
                    "\n{" +
                    "\ntone(p, bars[i]);" +
                    "\n}" +
                    //wait for the length of note
                    "\ndelay(bars[i + 1]);" +
                    //play nothing for the length of silence after note
                    "\nnoTone(p);" +
                    "\ndelay(bars[i + 2]);" +
                    "\n}" +
                    "\n}" +
                    //main loop
                    "\n\nvoid loop()" +
                    "\n{";
                //button handling
                //if a button is wanted
                if (buttonNumber != -1)
                {
                    //add an if statement for when the button has been pressed (be that high or low)
                    textOut += "if(digitalRead(b) == ";
                    if(buttonHigh)
                    {
                        textOut += "HIGH)\n{";
                    }
                    else
                    {
                        textOut += "LOW)\n{";
                    }
                }
                //write out play tone as many times as there are repeats
                for (int i = 0; i < repeatStarts.Count; i++)
                {
                    textOut += "\nPlayTune(" + repeatStarts[i] + ", " + repeatEnds[i] + ");";
                }
                //end the file with some close brackets
                textOut += "\n}" + buttonAdjustment;
                //write to file
                File.WriteAllText(filePath, textOut);
                //tell the user
                MainWindow.GenerateErrorDialog("Song Exported", songToConvert.GetTitle());
            }
        }
    }
}
