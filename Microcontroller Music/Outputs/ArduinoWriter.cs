using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;

namespace Microcontroller_Music
{
    class ArduinoWriter : Writer
    {
        string filePath;
        string textOut;
        int pin = 11;
        int track = 0;
        string buttonAdjustment = "\n}";
        int buttonNumber = 13;
        bool buttonHigh = false;
        public ArduinoWriter(Song s) : base(s)
        {

        }

        public override bool GetDetails()
        {
            ExportPopup exportPopup = new ExportPopup(songToConvert, 1);
            if (exportPopup.ShowDialog() == true)
            {
                track = exportPopup.GetTrackIndex();
                pin = exportPopup.GetSpeakerPin();
                switch (exportPopup.GetButtonPin())
                {
                    case ("No Button"):
                        buttonNumber = -1;
                        buttonAdjustment = "";
                        break;
                    default:
                        buttonNumber = Convert.ToInt32(exportPopup.GetButtonPin());
                        buttonHigh = exportPopup.GetButtonRead();
                        break;
                }
                SaveFileDialog saveFile = new SaveFileDialog()
                {
                    FileName = songToConvert.GetTitle().Replace(" ", "") + songToConvert.GetTrackTitle(track).Replace(" ", "") + "ForArduinoUno.ino",
                    Filter = "Arduino Files (*.ino)|*.ino"
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
                List<int> repeatStarts = new List<int>();
                List<int> repeatEnds = new List<int>();
                textOut = "const int bars[] = {";
                int currentIndex = 0;
                repeatStarts.Add(0);
                for (int i = 0; i < tableOfValues.Count; i++)
                {
                    int countAtStart = currentIndex;
                    if (songToConvert.DoesARepeatStartorEndOn(i, 0))
                    {
                        int numberOfRepeats = songToConvert.GetNumberOfRepeatsAtStartIndex(i);
                        for (int j = 0; j < numberOfRepeats; j++)
                        {
                            repeatStarts.Add(currentIndex);
                        }
                    }
                    for (int j = 0; j < tableOfValues[i].Length; j++)
                    {
                        textOut += tableOfValues[i][j];
                        currentIndex++;
                        if (j < tableOfValues[i].Length - 1)
                        {
                            textOut += ", ";
                        }
                    }
                    if (songToConvert.DoesARepeatStartorEndOn(i, 1))
                    {
                        int numberOfRepeats = songToConvert.GetNumberOfRepeatsAtEndIndex(i);
                        for (int j = 0; j < numberOfRepeats; j++)
                        {
                            repeatEnds.Add(currentIndex);
                        }
                    }
                    if (i < tableOfValues.Count - 1 && countAtStart != currentIndex)
                    {
                        textOut += ", \n";
                    }
                }
                repeatEnds.Add(currentIndex);
                textOut += "};\nint p = " + pin + ";";
                if (buttonNumber != -1) textOut += "\nint b = " + buttonNumber + ";";
                textOut += "\nvoid setup()\n{\npinMode(p, OUTPUT);\npinMode(b, INPUT);\n}\nvoid PlayTune(int establishment, int conclusion)\n{\nfor (int i = establishment; i < conclusion; i += 3)\n{" +
                "\nif (bars[i] == 0)\n{\nnoTone(p);\n}\nelse\n{\ntone(p, bars[i]);\n}\ndelay(bars[i + 1]);\nnoTone(p);\ndelay(bars[i + 2]);\n}\n}\n\nvoid loop()\n{";
                if (buttonNumber != -1)
                {
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
                for (int i = 0; i < repeatStarts.Count; i++)
                {
                    textOut += "\nPlayTune(" + repeatStarts[i] + ", " + repeatEnds[i] + ");";
                }
                textOut += "\n}" + buttonAdjustment;
                File.WriteAllText(filePath, textOut);
                MainWindow.GenerateErrorDialog("Song Exported", songToConvert.GetTitle());
            }
        }
    }
}
