using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace Microcontroller_Music
{
    class BBCMicroPythonWriter : Writer
    {
        //stores where the file will be exported
        string filePath;
        //stores text to export
        string textOut;
        //stores the pin of the speaker
        int pin = 0;
        //stores the index of the track being exported
        int track = 0;
        //an extra tab for when the song starts playing after a button press
        string buttonAdjustment = "\t";
        //stores the text used for the button pressed condition
        string buttonText = "";

        //nothing new in constructor, just call base
        public BBCMicroPythonWriter(Song s) : base(s)
        {

        }

        //open the export popup and then the savefiledialog to get all information needed from user before exporting
        //boolean is whether the user finished this process and the export should go ahead
        public override bool GetDetails()
        {
            //specifit popup for microbit (0)
            ExportPopup exportPopup = new ExportPopup(songToConvert, 0);
            //if they pressed the export button (not X)
            if (exportPopup.ShowDialog() == true)
            {
                //set track and pin to what was in each combobox
                track = exportPopup.GetTrackIndex();
                pin = exportPopup.GetSpeakerPin();
                //if the user picked a button for that part then add the text for the if statement when that button is pressed
                switch (exportPopup.GetButtonPin())
                {
                    case ("a"):
                    case ("b"):
                        buttonText = "\n\tif button_" + exportPopup.GetButtonPin() + ".is_pressed():";
                        break;
                        //otherwise there should be no extra tab for the rest of the program and no if statement
                    case ("No Button"):
                        buttonAdjustment = "";
                        break;
                }
                //now for the save file dialog
                SaveFileDialog saveFile = new SaveFileDialog()
                {
                    //default filename shows the name of the song, track and the device it works on
                    FileName = songToConvert.GetTitle() + " - " + songToConvert.GetTrackTitle(track) + " for BBC MicroBit.py",
                    //exports as .py to be uploaded to the online editor to make a hex file.
                    Filter = "Python Files (*.py)|*.py"
                };
                //if the user picked a save location then carry on with the process.
                if (saveFile.ShowDialog() == true)
                {
                    filePath = saveFile.FileName;
                    return true;
                }
            }
            //if the user failed to enter something then back out of process
            return false;
        }

        //writes the output for BBC Micro:Bit MicroPython
        public override void Write()
        {
            //get the information needed from the user
            if (GetDetails())
            {
                //get the array of frequencies and durations from the parent class
                List<int[]> tableOfValues = Generate2dFrequencyTable(track);
                //get the repeats from the song
                List<int[]> repeats = songToConvert.GetRepeats();
                //opening of the file:
                //microbit library
                textOut = "from microbit import *" +
                    //allows for simpler PWM functions
                    "\nimport music" +
                    //allows for sleep
                    "\nimport time" +
                    //start of the 2d array
                    "\nbars = [";
                //loop through all the bars in the 2d array
                for (int i = 0; i < tableOfValues.Count; i++)
                {
                    //open the array
                    textOut += "[";
                    //add all the values stored in the array with the correct formatting for python
                    for (int j = 0; j < tableOfValues[i].Length; j++)
                    {
                        textOut += tableOfValues[i][j];
                        //commas after all values but the last one
                        if (j < tableOfValues[i].Length - 1)
                        {
                            textOut += ", ";
                        }
                    }
                    //close the array
                    textOut += "]";
                    //add a comma and new line to the 2d array to make it readable
                    if (i < tableOfValues.Count - 1)
                    {
                        textOut += ", \n";
                    }
                }
                //if the song has repeats in it then there is a slightly more complex main function than if not
                if (repeats.Count > 0)
                {
                    //end the 2d array
                    textOut += "]" +
                        //function to loop through all the notes between a start and end bar
                        "\ndef PlayLoop(start, end):" +
                        //loop through the bars given
                        "\n\tfor i in range(start, end + 1):" +
                        //loop through the notes in the bar, incrementing by 3 each time (3 values stored per note)
                        "\n\t\tfor j in range(0, len(bars[i]), 3):" +
                        //if the frequency of the note is 0 treat it as a rest
                        "\n\t\t\tif(bars[i][j] == 0):" +
                        "\n\t\t\t\ttime.sleep_ms(bars[i][j+1])" +
                        //otherwise play the frequency on the given pin for the given time
                        "\n\t\t\telse:" +
                        "\n\t\t\t\tmusic.pitch(bars[i][j], bars[i][j+1], pin=pin" + pin + ")" +
                        //then wait the given time before playing the next note
                        "\n\t\t\ttime.sleep_ms(bars[i][j+2])" +
                        //the loop that runs
                        "\nwhile True:" + 
                        //if statement for button or nothing
                        buttonText;
                    //start of first loop is the first bar
                    int currentBar = 0;
                    //loop through all the repeats in the song
                    for (int i = 0; i < repeats.Count; i++)
                    {
                        //from here on out there is buttonAdjustment so that the indentation is correct
                        //set u pa loop from the start of the previous repeat to the end of the current one
                        textOut += "\n\t" + buttonAdjustment + "PlayLoop(" + currentBar + ", " + repeats[i][1] + ")";
                        //set current bar to be the start of the current repeat
                        currentBar = repeats[i][0];
                        //if the repeat loops more than once then add some PlayLoops that run from the start of the current repeat to the end of the current repeat
                        if (repeats[i][2] > 1)
                        {
                            for (int j = 1; j < repeats[i][2]; j++)
                            {
                                textOut += "\n\t" + buttonAdjustment + "PlayLoop(" + currentBar + ", " + repeats[i][1] + ")";
                            }
                        }
                    }
                    //add a final PlayLoop that runs from the start of the last repeat to the end of the song
                    textOut += ("\n\t" + buttonAdjustment + "PlayLoop(" + currentBar + ", len(bars) - 1)");
                }
                //if there are no repeats then the program is simpler - it just has to play all the notes in order
                //start of main loop
                else textOut += ("]\nwhile True:" + 
                        //if statement for button pressing or nothing
                        buttonText + 
                        //loop through all the bars in the array
                        "\n\t" + buttonAdjustment + "for i in range(len(bars)):" +
                        //loop through all symbols in the bar - increment by 3 because 3 values per symbol
                        "\n\t\t" + buttonAdjustment + "for j in range(0, len(bars[i]), 3):" +
                        //if the frequency is 0 then treat it as a rest. Must be added because Micro:Bit 2 doesn't like playing 0 frequencies
                        "\n\t\t\t" + buttonAdjustment + "if(bars[i][j] == 0):" +
                        "\n\t\t\t\t" + buttonAdjustment + "time.sleep_ms(bars[i][j+1])" +
                        //otherwise play the given frequency on the given pin for the given time in ms
                        "\n\t\t\t" + buttonAdjustment + "else:" +
                        "\n\t\t\t\t" + buttonAdjustment + "music.pitch(bars[i][j], bars[i][j+1], pin=pin" + pin + ")" +
                        //then wait for the given time in ms to play the next symbol
                        "\n\t\t\t\t" + buttonAdjustment + "time.sleep_ms(bars[i][j+2])");
                //write the text to the file
                File.WriteAllText(filePath, textOut);
                //tell the user that the process is done.
                MainWindow.GenerateErrorDialog("Song Exported", songToConvert.GetTitle());
            }
        }
    }
}
