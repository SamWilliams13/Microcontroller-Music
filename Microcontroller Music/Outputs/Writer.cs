using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microcontroller_Music
{
    //abstract parent class for all creators of outputs
    public abstract class Writer
    {
        //a song to convert
        protected Song songToConvert;

        //constructor
        protected Writer(Song s)
        {
            //sets the song to convert to the argument
            songToConvert = s;
        }

        //collects information required for the song to be made
        public abstract bool GetDetails();

        //carries out the process to make a form of output
        public abstract void Write();

        public List<int[]> Generate2dFrequencyTable(int track)
        {
            List<int[]> frequency2dList = new List<int[]>();
            int totalBars = songToConvert.GetTotalBars();
            int startingSemiPos = 0;
            int barsIntoFuture = 0;
            for (int i = 0; i < totalBars; i++)
            {
                //prevents tied semibreve duplication
                if(barsIntoFuture == 1)
                {
                    frequency2dList.Add(new int[0]);
                    barsIntoFuture--;
                }
                else
                {
                    frequency2dList.Add(GenerateFrequencyTable(track, i, ref barsIntoFuture, ref startingSemiPos));
                }
            }
            return frequency2dList;
        }

        public int[] GenerateFrequencyTable(int track, int bar, ref int barsIntoFuture, ref int semiPos)
        {
            int[] frequencyTable;
            List<int> frequencyList = new List<int>();
            List<Symbol> notes = songToConvert.GetTracks(track).GetBars(bar).GetNotes();
            barsIntoFuture = 0;
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].GetStart() == semiPos)
                {
                    GenerateNoteFrequency(notes[i], track, bar, ref barsIntoFuture, ref semiPos, ref frequencyList);
                }
                if (barsIntoFuture > 0)
                {
                    barsIntoFuture--;
                    break;
                }
            }
            frequencyTable = new int[frequencyList.Count];
            for(int i = 0; i < frequencyList.Count; i++)
            {
                frequencyTable[i] = frequencyList[i];
            }
            return frequencyTable;
        }

        public void GenerateNoteFrequency(Symbol n, int track, int bar, ref int barsIntoFuture, ref int semiPos, ref List<int> frequencyList, bool continuation = false, int length = 0)
        {
            int semiTime = 60000 / (songToConvert.GetBPM() * 4);
            if (n is Note)
            {
                Note note = n as Note;
                if (!continuation)
                {
                    frequencyList.Add((int)(440 * Math.Pow(2, (note.GetPitch() - 49) / 12d)));
                }
                semiPos = note.GetStart() + note.GetLength();
                length += note.GetLength();
                if (semiPos >= songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetMaxLength())
                {
                    semiPos -= songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetMaxLength();
                    barsIntoFuture++;
                }
                if (note.GetTie() != null && (note.GetTie() as Note).GetPitch() == note.GetPitch())
                {
                    GenerateNoteFrequency(note.GetTie(), track, bar, ref barsIntoFuture, ref semiPos, ref frequencyList, true, length);
                }
                else if(note.GetTie() != null && (note.GetTie() as Note).GetPitch() != note.GetPitch())
                {
                    frequencyList.Add(length * semiTime);
                    frequencyList.Add(0);
                }
                else if(note.GetStaccato())
                {
                    frequencyList.Add((length - note.GetLength()) * semiTime + (note.GetLength() * semiTime / 2));
                    frequencyList.Add(note.GetLength() * semiTime / 2);
                }
                else
                {
                    frequencyList.Add((length - note.GetLength()) * semiTime + (note.GetLength() * semiTime * 7 / 8));
                    frequencyList.Add(note.GetLength() * semiTime / 8);
                }
            }
            else
            {
                Rest rest = n as Rest;
                frequencyList.Add(0);
                frequencyList.Add(rest.GetLength() * semiTime);
                frequencyList.Add(0);
                semiPos = rest.GetStart() + rest.GetLength();
                if (semiPos >= songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetMaxLength())
                {
                    semiPos -= songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetMaxLength();
                    barsIntoFuture++;
                }
            }
        }
    }
}
