using System.Collections.Generic;
using Midi;

namespace Microcontroller_Music
{
    class MIDIWriter : Writer
    {
        //array of instruments so that each track can be played on a different instrument
        private Instrument[] instruments;
        //device used to play the MIDI - Microsoft Wavetable GS Synth tends to be the default
        private OutputDevice output;

        //used to schedule all notes before they are played. no need to have threading and sleep.
        private Clock clock;
        //constructor - calls base to set memory address of song to songToConvert.
        public MIDIWriter(Song s) : base(s)
        {
            //initialises the clock with the correct BPM for the song
            clock = new Clock(songToConvert.GetBPM());
            //sets up the clock
        }

        //called when the tempo changes so it can play at the right speed
        public void UpdateBPM()
        {
            //must stop music or this will fail
            Stop();
            //clock has to be remade with the right tempo
            clock = new Clock(songToConvert.GetBPM());
        }

        //opens a dialog box and gets some info from the user, then starts the writing process
        public override bool GetDetails()
        {
            //Step 1 is to get all the information for the user in a presentable manner. 
            //First line makes an array of the right length to store the names of all tracks for display reasons.
            string[] namesArray = new string[songToConvert.GetTrackCount()];
            //loop through all the tracks in the song
            for (int i = 0; i < songToConvert.GetTrackCount(); i++)
            {
                //add the name of the track to the array
                namesArray[i] = songToConvert.GetTracks(i).GetName();
            }
            //make a dialog box with the needed information to let the user select output and instrument
            MIDIDetails detailsBox = new MIDIDetails(songToConvert.GetTrackCount(), namesArray);
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
            //loop through each track. do one track completely before starting on the next one
            for (int i = 0; i < songToConvert.GetTrackCount(); i++)
            {
                repeats.Clear();
                //the following lines are used so that the program can use a clone of the repeat list instead of a copy
                //loop through all the repeats in the song
                foreach (int[] r in songToConvert.GetRepeats())
                {
                    //clone the repeat array and add it to a new list. no need to sort again - already sorted
                    repeats.Add((int[])r.Clone());
                }
                //sets the channel that the track is going to be played on to the desired instrument.
                output.SendProgramChange((Channel)i, instruments[i]);
                //totalLength is to store the length of the song so far. This is added to after every bar so that the clock can have an idea of where it is
                float totalLength = 0;
                //loop through all bars in the track
                for (int j = 0; j < songToConvert.GetTotalBars(); j++)
                {
                    //get the notes in the bar
                    List<Symbol> barNotes = songToConvert.GetTracks(i).GetBars(j).GetNotes();
                    //loop through notes
                    foreach (Symbol s in barNotes)
                    {
                        //only play the note if it is not the second part of a tie or slur - or notes will be duplicated.
                        if (s is Note && (s as Note).GetTiedTo() == null)
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
            //check what type of symbol s is
            if (s is Note)
            {
                //if the note does not tie or slur into anything else
                if ((s as Note).GetTie() == null)
                {
                    //schedule the note
                    MakeNote(s as Note, channel, total, start, noteLength, s.GetLength(), false);
                }
                //otherwise if the note is tied to something of the same pitch as it - this line is long as it has to handle things as different types
                else if ((s as Note).GetTie() is Note && (s as Note).GetPitch() == ((s as Note).GetTie() as Note).GetPitch())
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
            //that is, of course, if the song is playing.
            if (clock.IsRunning)
            {
                clock.Stop();
            }
            //if there is an open output, close it
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
