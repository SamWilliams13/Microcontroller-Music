using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;

namespace Microcontroller_Music
{
    class PicoWriter : Writer
    {
        string filePath;
        string textOut;
        int pin = 11;
        int track = 0;
        string buttonAdjustment = "\t";
        int buttonNumber = 13;
        bool buttonHigh = false;
        public PicoWriter(Song s) : base(s)
        {

        }

        public override bool GetDetails()
        {
            ExportPopup exportPopup = new ExportPopup(songToConvert, 2);
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
                    FileName = songToConvert.GetTitle().Replace(" ", "_") +"_"+ songToConvert.GetTrackTitle(track).Replace(" ", "_") + "_for_Raspberry_Pi_Pico.py",
                    Filter = "Arduino Files (*.py)|*.py"
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
                textOut = "from machine import Pin, PWM\nfrom utime import sleep\nbars = [";
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
                        if(j % 3 == 0)
                        {
                            textOut += tableOfValues[i][j];
                        }
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
                textOut += "]\np = PWM(Pin(" + pin + "))";
                if (buttonNumber != -1)
                {
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
                textOut += "\ndef PlayLoop(start, end):\n\tfor i in range(start, end, 3):\n\t\tif(bars[i] == 0):\n\t\t\tsleep(bars[i+1])" +
                        "\n\t\telse:\n\t\t\tp.duty_u16(1000)\n\t\t\tp.freq(bars[i])\n\t\t\tsleep(bars[i+1])\n\t\t\tp.duty_u16(0)\n\t\t\tsleep(bars[i+2])\nwhile True:";
                if (buttonNumber != -1)
                {
                    textOut += "\n\tif(b.value()):";
                }
                for (int i = 0; i < repeatStarts.Count; i++)
                {
                    textOut += "\n\t" + buttonAdjustment + "PlayLoop(" + repeatStarts[i] + ", " + repeatEnds[i] + ")";
                }
                File.WriteAllText(filePath, textOut);
                MainWindow.GenerateErrorDialog("Song Exported", songToConvert.GetTitle());
            }
        }
    }
}
