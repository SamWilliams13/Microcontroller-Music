using System;
using System.Collections.Generic;

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

        //makes a list of arrays of all the note frequencies, time in ms, rest time in ms in each bar
        protected List<int[]> Generate2dFrequencyTable(int track)
        {
            List<int[]> frequency2dList = new List<int[]>();
            //how many bars in song
            int totalBars = songToConvert.GetTotalBars();
            //point at which the bar must start to be looked at, protects against tying across bars
            int startingSemiPos = 0;
            //protects against tying for more than a bar
            int barsIntoFuture = 0;
            //loop through all bars in track
            for (int i = 0; i < totalBars; i++)
            {
                //prevents tied semibreve duplication
                if(barsIntoFuture >= 1)
                {
                    frequency2dList.Add(new int[0]);
                    barsIntoFuture--;
                }
                else
                {
                    //convert the bar into array of freq, length, rest time
                    frequency2dList.Add(GenerateFrequencyTable(track, i, ref barsIntoFuture, ref startingSemiPos));
                }
            }
            //return the list
            return frequency2dList;
        }

        //generates an array of note frequencies, note lengths in milliseconds and the amount of silence after in milliseconds.
        protected int[] GenerateFrequencyTable(int track, int bar, ref int barsIntoFuture, ref int semiPos)
        {
            int[] frequencyTable;
            //list will be converted into int array later
            List<int> frequencyList = new List<int>();
            //gets the symbols needing conversion, shortens some lines
            List<Symbol> notes = songToConvert.GetTracks(track).GetBars(bar).GetNotes();
            //tracks so that the process can stop when the bar has ended (also skips bars in certain cases)
            barsIntoFuture = 0;
            //loop through all symbols in the bar
            for (int i = 0; i < notes.Count; i++)
            {
                //if they start at the end of the previous symbol then add them to the list (this means only the highest concurrent pitch is added)
                if (notes[i].GetStart() == semiPos)
                {
                    GenerateNoteFrequency(notes[i], i, track, bar, ref barsIntoFuture, ref semiPos, ref frequencyList);
                }
                //if the bar has ended already then subtract 1 from barsIntoFuture so the higher up method sees it has to skip that many more bars
                if (barsIntoFuture > 0)
                {
                    barsIntoFuture--;
                    break;
                }
            }
            //convert the list into an int array by looping through and assigning all the values.
            frequencyTable = new int[frequencyList.Count];
            for(int i = 0; i < frequencyList.Count; i++)
            {
                frequencyTable[i] = frequencyList[i];
            }
            //give the int array back
            return frequencyTable;
        }

        //adds the specific symbol's frequency, duration and rest duration to the 1d list
        protected void GenerateNoteFrequency(Symbol n, int symbol, int track, int bar, ref int barsIntoFuture, ref int semiPos, ref List<int> frequencyList, bool continuation = false, int length = 0)
        {
            //calculate the length of a semiquaver, it is 1 minute divided by the semiquavers per minute (bpm * 4)
            int semiTime = 60000 / (songToConvert.GetBPM() * 4);
            //if the symbol is a note
            if (n is Note)
            {
                //saves length
                Note note = n as Note;
                //when a note is a continuation, it is the second or third (etc) note in a series of ties, and therefore the frequency is already in the list
                if (!continuation)
                {
                    //calculate the frequency from the pitch number of the note. calculated relative to A4 440Hz using equation
                    frequencyList.Add((int)(440 * Math.Pow(2, (note.GetPitch() - 49) / 12d)));
                }
                //update the position to look at in bar loop to be the end of the note
                semiPos = note.GetStart() + note.GetLength();
                //add the length of the note to the running length
                length += note.GetLength();
                //if the note reaches the end of the bar
                if (semiPos >= songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetMaxLength())
                {
                    //move the semiPos into the next bar
                    semiPos -= songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetMaxLength();
                    //increase barsIntoFuture so future recursions and higher methods can see it is in next bar
                    barsIntoFuture++;
                }
                //if the note is tied
                if (note.GetTie() != null && (note.GetTie() as Note).GetPitch() == note.GetPitch())
                {
                    //call the method again, saying it is a continuation and giving it the length that has already played
                    GenerateNoteFrequency(note.GetTie(), symbol, track, bar, ref barsIntoFuture, ref semiPos, ref frequencyList, true, length);
                }
                //if it is a slur
                else if(note.GetTie() != null && (note.GetTie() as Note).GetPitch() != note.GetPitch())
                {
                    //make it so the note plays its full length
                    frequencyList.Add(length * semiTime);
                    //and therefore there is no silence after
                    frequencyList.Add(0);
                }
                //if the note is staccato
                else if(note.GetStaccato())
                {
                    //the note plays the full length of any previous tied notes but only plays half of the current note length
                    frequencyList.Add((length - note.GetLength()) * semiTime + (note.GetLength() * semiTime / 2));
                    //and is therefore silent for half of the current note length
                    frequencyList.Add(note.GetLength() * semiTime / 2);
                }
                //if it is just a normal note
                else
                {
                    //play the full length of previous tied notes and 7/8 of the current note
                    frequencyList.Add((length - note.GetLength()) * semiTime + (note.GetLength() * semiTime * 7 / 8));
                    //therefore silent for 1/8 of the length of the current note. this is what makes the notes sound separate
                    frequencyList.Add(note.GetLength() * semiTime / 8);
                }
            }
            //otherwise the note is a rest, in which case it need not care about ties and staccato and frequency
            else
            {
                Rest rest = n as Rest;
                //update the semiPos to look at the end of the rest
                semiPos = rest.GetStart() + rest.GetLength();
                length += rest.GetLength();
                //if that was the end of the bar, move the semiPos into the next bar and update barsIntoFuture to reflect that
                if (semiPos >= songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetMaxLength())
                {
                    semiPos -= songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetMaxLength();
                    barsIntoFuture++;
                    symbol = -1;
                }
                //makes sure that rests that are grouped together will be stored as one
                Symbol nextNote = null;
                //if statement prevents the program from trying to find bars that don't exist
                if (symbol != -1 || bar + barsIntoFuture < songToConvert.GetTotalBars())
                {
                    nextNote = songToConvert.GetTracks(track).GetBars(bar + barsIntoFuture).GetNotes(symbol + 1);
                }
                //if the next note in the track is also a rest then they can be grouped
                if (nextNote is Rest)
                {
                    //calls itself to extend the length of the time. saves some space, especially if there is a lot of silence at the start of a track.
                    GenerateNoteFrequency(nextNote, symbol + 1, track, bar, ref barsIntoFuture, ref semiPos, ref frequencyList, true, length);
                }
                //otherwise add the length of what has already been grouped.
                else
                {
                    //add 0 as the frequency as it can be handled by program to play nothing
                    frequencyList.Add(0);
                    //be silent for full length of rest
                    frequencyList.Add(length * semiTime);
                    //another 0 to make sure that 3 spaces are taken up by each symbol
                    frequencyList.Add(0);
                }
            }
        }
    }
}
