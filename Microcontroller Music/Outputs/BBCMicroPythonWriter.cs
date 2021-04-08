using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;

namespace Microcontroller_Music
{
    class BBCMicroPythonWriter : Writer
    {
        string filePath;
        string textOut;
        int pin = 0;
        int track = 0;
        string buttonAdjustment = "\t";
        string buttonText = "";
        public BBCMicroPythonWriter(Song s) : base(s)
        {

        }

        public override bool GetDetails()
        {
            ExportPopup exportPopup = new ExportPopup(songToConvert, 0);
            if (exportPopup.ShowDialog() == true)
            {
                track = exportPopup.GetTrackIndex();
                pin = exportPopup.GetSpeakerPin();
                switch (exportPopup.GetButtonPin())
                {
                    case ("a"):
                    case ("b"):
                        buttonText = "\n\tif button_" + exportPopup.GetButtonPin() + ".is_pressed():";
                        break;
                    case ("No Button"):
                        buttonAdjustment = "";
                        break;
                }
                SaveFileDialog saveFile = new SaveFileDialog()
                {
                    FileName = songToConvert.GetTitle() + " - " + songToConvert.GetTrackTitle(track) + " for BBC MicroBit.py",
                    Filter = "Python Files (*.py)|*.py"
                };
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
            if (GetDetails())
            {
                List<int[]> tableOfValues = Generate2dFrequencyTable(track);
                List<int[]> repeats = songToConvert.GetRepeats();
                textOut = "from microbit import *\nimport music\nimport time\nbars = [";
                for (int i = 0; i < tableOfValues.Count; i++)
                {
                    textOut += "[";
                    for (int j = 0; j < tableOfValues[i].Length; j++)
                    {
                        textOut += tableOfValues[i][j];
                        if (j < tableOfValues[i].Length - 1)
                        {
                            textOut += ", ";
                        }
                    }
                    textOut += "]";
                    if (i < tableOfValues.Count - 1)
                    {
                        textOut += ", \n";
                    }
                }
                if (repeats.Count > 0)
                {
                    textOut += "]\ndef PlayLoop(start, end):\n\tfor i in range(start, end + 1):\n\t\tfor j in range(0, len(bars[i]), 3):\n\t\t\tif(bars[i][j] == 0):\n\t\t\t\ttime.sleep_ms(bars[i][j+1])" +
                        "\n\t\t\telse:\n\t\t\t\tmusic.pitch(bars[i][j], bars[i][j+1], pin=pin" + pin + ")\n\t\t\ttime.sleep_ms(bars[i][j+2])\nwhile True:" + buttonText;
                    int currentBar = 0;
                    for (int i = 0; i < repeats.Count; i++)
                    {
                        textOut += "\n\t" + buttonAdjustment + "PlayLoop(" + currentBar + ", " + repeats[i][1] + ")";
                        currentBar = repeats[i][0];
                        if (repeats[i][2] > 1)
                        {
                            for (int j = 1; j < repeats[i][2]; j++)
                            {
                                textOut += "\n\t" + buttonAdjustment + "PlayLoop(" + currentBar + ", " + repeats[i][1] + ")";
                            }
                        }
                    }
                    textOut += ("\n\t" + buttonAdjustment + "PlayLoop(" + currentBar + ", len(bars) - 1)");
                }
                else textOut += ("]\nwhile True:" + buttonText + "\n\t" + buttonAdjustment + "for i in range(len(bars)):\n\t\t" + buttonAdjustment + "for j in range(0, len(bars[i]), 3):\n\t\t\t" + buttonAdjustment +
                        "if(bars[i][j] == 0):\n\t\t\t\t" + buttonAdjustment + "time.sleep_ms(bars[i][j+1])" +
                        "\n\t\t\t" + buttonAdjustment + "else:\n\t\t\t\t" + buttonAdjustment + "music.pitch(bars[i][j], bars[i][j+1], pin=pin" + pin + ")\n\t\t\t\t" + buttonAdjustment + "time.sleep_ms(bars[i][j+2])");
                File.WriteAllText(filePath, textOut);
                MainWindow.GenerateErrorDialog("Song Exported", songToConvert.GetTitle());
            }
        }
    }
}
