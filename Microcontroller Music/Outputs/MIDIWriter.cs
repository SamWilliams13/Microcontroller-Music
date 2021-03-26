using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Midi;

namespace Microcontroller_Music
{
    class MIDIWriter : Writer
    {
        //array of instruments so that each track can be played on a different instrument
        Instrument[] instruments;
        //device used to play the MIDI - Microsoft Wavetable GS Synth tends to be the default
        OutputDevice output;
        //used to schedule all notes before they are played. no need to have threading and sleep.
        Clock clock;
        //constructor - calls base to set memory address of song to songToConvert.
        public MIDIWriter(Song s) : base(s)
        {
            //initialises the clock with the correct BPM for the song
            clock = new Clock(songToConvert.GetBPM());
            //sets up the clock
        }

        //opens a dialog box and gets some info from the user, then starts the writing process
        public override bool GetDetails()
        {
            //Step 1 is to get all the information for the user in a presentable manner. 
            //First line makes an array of the right length to store the names of all tracks for display reasons.
            string[] namesArray = new string[songToConvert.GetTracks().Count];
            //loop through all the tracks in the song
            for (int i = 0; i < songToConvert.GetTracks().Count; i++)
            {
                //add the name of the track to the array
                namesArray[i] = songToConvert.GetTracks(i).GetName();
            }
            //make a dialog box with the needed information to let the user select output and instrument
            MIDIDetails detailsBox = new MIDIDetails(songToConvert.GetTracks().Count, namesArray);
            //check the user wants to go ahead
            if (detailsBox.ShowDialog() == true)
            {
                //use the method in dialog box to get an array of instruments to use
                instruments = detailsBox.GetTrackInstruments();
                //use a different method in dialog box to fetch the output device chosen
                output = detailsBox.GetOutputDevice();
                //start the process of scheduling the notes in the song
                Write();
                return true;
            }
            else return false;
        }

        //uses the info generated from GetDetails and the song to run up a clock and then start the clock.
        public override void Write() 
        {
            clock.Reset();
            //used to store the repeats in the song --needed so that decreasing the count in repeat isn't permanent
            List<int[]> repeats = new List<int[]>();
            //opens the chosen output device to allow for 
            if(!output.IsOpen) output.Open();
            //the following lines are used so that the program can use a clone of the repeat list instead of a copy
            //loop through all the repeats in the song
            foreach (int[] r in songToConvert.GetRepeats())
            {
                //clone the repeat array and add it to a new list. no need to sort again - already sorted
                repeats.Add((int[])r.Clone());
            }
            //loop through each track. do one track completely before starting on the next one
            for (int i = 0; i < songToConvert.GetTracks().Count; i++)
            {
                //sets the channel that the track is going to be played on to the desired instrument.
                output.SendProgramChange((Channel)i, instruments[i]);
                //totalLength is to store the length of the song so far. This is added to after every bar so that the clock can have an idea of where it is
                float totalLength = 0;
                //loop through all bars in the track
                for (int j = 0; j < songToConvert.GetTracks(i).GetBars().Count; j++)
                {
                    //get the notes in the bar
                    List<Symbol> barNotes = songToConvert.GetTracks(i).GetBars(j).GetNotes();
                    //loop through notes
                    foreach (Symbol s in barNotes)
                    {
                        //only play the note if it is not the second part of a tie or slur - or notes will be duplicated.
                        if ((s is Note && (s as Note).GetTiedTo() == null) || (s is Tuplet && (s as Tuplet).GetTiedTo() == null))
                        {
                            //call notehandler. give it the symbol, tell it there are no previous note lengths tied to it, start point in bar, total length
                            //of previous bars, the channel of the track
                            NoteHandler(s, 0F, s.GetStart(), totalLength, (Channel)i);
                        }
                    }
                    //once the end of the bar has been reached, increase the totalLength to reflect that.
                    totalLength += (songToConvert.GetTracks(i).GetBars(j).GetMaxLength()) / 4;
                    //handle repeats, go back to start of repeat when needed
                    //checks if the bar that just ended was the end of a repeat section
                    for(int l = 0; l < repeats.Count; l++)
                    {
                        //variable to store the current repeat to be looked at
                        int[] r = repeats[l];
                        //if the program has reached the end of an active repeat section then it goes back to the start of it.
                        if (j == r[1] && r[2] > 0)
                        {
                            //sets the bar to look at to be one less than the start of the repeat. This is so when the for loop increments it looks at the correct bar
                            j = r[0] - 1;
                            //decreases the number of times the current repeat section has remaining
                            r[2]--;
                            //stops looping through as the value has been found
                            break;
                        }
                        //once the program is looking at bars after the repeated section, the number of repeats it needs can be reset
                        else if (j > r[1] && r[2] < 1)
                        {
                            //reset the number of repeats the section should have so the next track can use them
                            r[2] = songToConvert.GetRepeats()[l][2];
                        }
                    }
                }

            }
            //starts playing the song
            clock.Start();
        }

        //recursive function that looks at a symbol, checks for ties and calls MakeNote to schedule the note
        //requires a symbol, the length of previous tied notes, the start point of the first note, the total length of previous bars,
        //the channel of the track to output to.
        public void NoteHandler(Symbol s, float noteLength, float start, float total, Channel channel)
        {
            //check what type of symbol s is - tuplets require some more processing
            if (s is Note)
            {
                //if the note does not tie or slur into anything else
                if ((s as Note).GetTie() == null)
                {
                    //schedule the note
                    MakeNote(s as Note, channel, total, start, noteLength, s.GetLength(), false);
                }
                //otherwise if the note is tied to something of the same pitch as it - this line is long as it has to handle things as different types
                else if (((s as Note).GetTie() is Note && (s as Note).GetPitch() == ((s as Note).GetTie() as Note).GetPitch()) ||
                    ((s as Note).GetTie() is Tuplet && ((s as Note).GetTie() as Tuplet).GetComponent(0) is Note &&
                    (s as Note).GetPitch() == (((s as Note).GetTie() as Tuplet).GetComponent(0) as Note).GetPitch()))
                {
                    //call notehandler again but increase the length of previous notes to reflect this one
                    NoteHandler((s as Note).GetTie(), s.GetLength() + noteLength, start, total, channel);
                }
                //otherwise the note is slurred
                else
                {
                    //play the note, but with isSlur as true so the full length of the note is played
                    MakeNote(s as Note, channel, total, start, noteLength, s.GetLength(), true);
                    //but then also call notehandler so the note it slurs to can be scheduled too
                    //this requires the start point of the note to be handled here, as the start point stored in the note will not be accurate if the notes slur
                    //over the end of a bar.
                    NoteHandler((s as Note).GetTie(), 0F, start + s.GetLength() + noteLength, total, channel);
                }
            }
            //this could be an else because rests are removed by an earlier check, but better safe than sorry
            else if (s is Tuplet)
            {
                //make a variable for the tuplet so that (s as Tuplet) doesn't appear everywhere - uses minimal extra memory
                Tuplet t = s as Tuplet;
                //calculates the length of one note in a tuplet, as this doesn't exist in the actual structure as it could be decimal
                float tupLen = (float)t.GetLength() / (float)t.GetNumberOfNotes();
                //loop through all notes in the tuplet except for the last one. this doesn't care about TiedTo as it won't duplicate notes like that
                for (int i = 0; i < (t).GetNumberOfNotes() - 1; i++)
                {
                    //tuplet symbols can only be note or rest, so this just removes rests.
                    if (t.GetComponent(i) is Note)
                    {
                        //if the note isn't tied to the next component of the tuplet
                        if ((t.GetComponent(i) as Note).GetTie() == null)
                        {
                            //play the note in the tuplet
                            MakeNote(t.GetComponent(i) as Note, channel, total, start, noteLength, tupLen, false);
                            //calculate the start point of the next note in the tuplet
                            start += noteLength + tupLen;
                            //reset notelength
                            noteLength = 0;
                        }
                        //if the note is tied to the next one and they are the same pitch
                        else if ((t.GetComponent(i) as Note).GetTie() is Note && 
                            (t.GetComponent(i) as Note).GetPitch() == ((t.GetComponent(i) as Note).GetTie() as Note).GetPitch())
                        {
                            //just increase the length of all previous notes and move on
                            noteLength += tupLen;
                        }
                        //if the note slurs into the next one
                        else
                        {
                            //play the note for its full duration
                            MakeNote(t.GetComponent(i) as Note, channel, total, start, noteLength, tupLen, true);
                            //adjust the start point for the next note
                            start += noteLength + tupLen;
                            //reset notelength
                            noteLength = 0;
                        }
                    }
                    //make sure that the start of the next note is adjusted if there is a rest.
                    else
                    {
                        start += tupLen;
                    }
                }
                //the last component of the tuplet has to be handled differently as it could be tied to another note- therefore it is handled more like
                //a regular note than a tuplet note.
                //if the last component is even a note
                if (t.GetComponent((t).GetNumberOfNotes() - 1) is Note)
                {
                    //if the last note is not tied to anything
                    if (t.GetTie() == null)
                    {
                        //play the last note as if it were any other in the tuplet
                        MakeNote(t.GetComponent((t).GetNumberOfNotes() - 1) as Note, channel, total, start, noteLength, tupLen, false);
                    }
                    //if the last note is tied to another symbol which has the same pitch as it
                    else if ((t.GetTie() is Note && (t.GetComponent(t.GetNumberOfNotes() - 1) as Note).GetPitch() == (t.GetTie() as Note).GetPitch()) ||
                        (t.GetTie() is Tuplet && (t.GetTie() as Tuplet).GetComponent(0) is Note && 
                        (t.GetComponent(t.GetNumberOfNotes() - 1) as Note).GetPitch() == ((t.GetTie() as Tuplet).GetComponent(0) as Note).GetPitch()))
                    {
                        //call notehandler and continue the recursion
                        NoteHandler(t.GetTie(), tupLen + noteLength, start, total, channel);
                    }
                    //if it slurs into the next symbol
                    else
                    {
                        //play the note for its full duration
                        MakeNote(t.GetComponent((t).GetNumberOfNotes() - 1) as Note, channel, total, start, noteLength, tupLen, true);
                        //call notehandler for the symbol it slurs into.
                        NoteHandler(t.GetTie(), 0F, start + tupLen + noteLength, total, channel);
                    }
                }
            }
        }

        //this is the part that schedules a note on the clock. it needs the note to play, channel to output onto, the total length of previous bars
        //the start point of the note in the bar, the length of the note (excluding the last part), the length of the last note in the ties, and whether the note is slurred or played normally
        public void MakeNote(Note n, Channel channel, float total, float start, float t, float endNoteLength, bool isSlur)
        {
            //gets start time in crotchets
            start /= 4F;
            //length of note in crotchets
            t = (float)(t / 4.0);
            //following lines make the notes the right value for midi which takes things in crotchets not semiquavers
            //makes the note half as long as usual
            if (n.GetStaccato())
            {
                t += (float)(endNoteLength / 8.0);
            }
            //makes the note full legato length
            else if (isSlur)
            {
                t += (float)(endNoteLength / 4.0);
            }
            //makes note play for 7/8 of its length to differentiate between that and slurs
            else
            {
                t += (float)(7 * endNoteLength / 32.0);
            }
            //sets the clock to play the note at the needed startpoint
            clock.Schedule(new NoteOnMessage(output, channel, (Pitch)(n.GetPitch() + 20), 80, total + start));
            //sets the clock to stop the note once it is over
            clock.Schedule(new NoteOffMessage(output, channel, (Pitch)(n.GetPitch() + 20), 80, total + start + t)); 
        }

        //starts the process of playing the song
        public bool Play()
        {
            if (GetDetails())
            {
                return true;
            }
            else return false;
        }

        //stops the process of playing the song
        public void Stop()
        {
            if (clock.IsRunning)
            {
                clock.Stop();
            }
            if(output != null && output.IsOpen)
            {
                output.Close();
            }
        }

        //makes sure the song it is converting is up to date, though it should be redundant
        public void Update(Song s)
        {
            songToConvert = s;
        }
    }
}
