using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Microcontroller_Music
{
    [DataContract]
    public class Song
    {
        [DataMember]
        //stores the name of the song
        private string Title;
        [DataMember]
        //stores the crotchets per minute of the song
        private int BPM;
        [DataMember]
        //a list of the key sig of each bar - consistent for all tracks
        private readonly List<int> KeySigs;
        [DataMember]
        //list of all tracks in song
        private readonly List<Track> Tracks;
        [DataMember]
        //0 = start, 1=end, 2=number of repeats
        private readonly List<int[]> Repeats = new List<int[]>();
        [DataMember]
        //number of bars that should be present 
        private int totalBars = 0;
        //struct to allow simpler representation of time sigs.
        public struct TimeSig
        {
            public int top;
            public int bottom;
            public TimeSig(int topn, int bottomn)
            {
                top = topn;
                bottom = bottomn;
            }
        }
        [DataMember]
        //list of the time signature of each bar, consistent across tracks.
        private readonly List<TimeSig> TimeSigs;

        //constructor. sets all starting values and properties of the first bar.
        public Song(string title, int bpm, int keySig, int topp = 4, int bot = 4)
        {
            Title = title;
            BPM = bpm;
            Tracks = new List<Track>();
            TimeSigs = new List<TimeSig>
            {
                new TimeSig() { bottom = bot, top = topp }
            };
            KeySigs = new List<int>
            {
                keySig
            };
        }

        //returns the number of bars in the song
        public int GetTotalBars()
        {
            return totalBars;
        }

        //returns the key signature of a given bar
        public int GetKeySigs(int index)
        {
            return KeySigs[index];
        }

        //returns the time signature of a given bar
        public TimeSig GetTimeSigs(int index)
        {
            return TimeSigs[index];
        }

        //checks the time sig of a given bar, and the bar before it.
        public bool TimeSigIsDifferentToPrevious(int barIndex)
        {
            if (barIndex > 0 && barIndex < TimeSigs.Count)
            {
                //return true if the time signatures are different
                return (TimeSigs[barIndex].top != TimeSigs[barIndex - 1].top || TimeSigs[barIndex].bottom != TimeSigs[barIndex - 1].bottom);
            }
            else
            {
                //return false if the time sig is the same between the bars.
                return false;
            }
        }

        //calculates and returns the required length of a given bar in semiquavers.
        public int GetSigLength(int barIndex)
        {
            return TimeSigs[barIndex].top * (16 / TimeSigs[barIndex].bottom);
        }

        //makes sure that all tracks have the same number of bars. important for repeats and adding notes.
        public void UpdateTotalBars()
        {
            //boolean that means that the method loops until there is a run where nothing changes.
            //this is because a change in the method requires the bars to be updated again.
            bool somethingChanged;
            do
            {
                somethingChanged = false;
                //loop through each track
                foreach (Track t in Tracks)
                {
                    //if the track has more bars than the song thinks there are
                    //update total bars
                    //update timesigs
                    //update keysigs
                    if (t.GetBars().Count > totalBars)
                    {
                        somethingChanged = true;
                        totalBars = t.GetBars().Count;
                        TimeSig newSig;
                        int newKeySig;
                        if (TimeSigs.Count > 0)
                        {
                            newSig = TimeSigs[TimeSigs.Count - 1];
                        }
                        else newSig = new TimeSig(4, 4);
                        if (KeySigs.Count > 0)
                        {
                            newKeySig = KeySigs[KeySigs.Count - 1];
                        }
                        else newKeySig = 0;
                        for (int j = TimeSigs.Count; j < t.GetBars().Count; j++)
                        {
                            TimeSigs.Add(newSig);
                            KeySigs.Add(newKeySig);
                        }
                    }
                    //if there are less bars in the track than the song thinks there should be
                    //add bars until it is the right length
                    else if (t.GetBars().Count < totalBars)
                    {
                        somethingChanged = true;
                        for (int j = 0; j < totalBars - t.GetBars().Count; j++)
                        {
                            t.NewBar();
                        }
                    }
                    //if nothing has changed yet - this is so all tracks are the right length when you get here
                    if (!somethingChanged)
                    {
                        //loop through all the bars in the track
                        for (int i = 0; i < totalBars; i++)
                        {
                            //if the key signature is not as expected, change the keysig of the bar to reflect the list in song
                            if (KeySigs[i] != t.GetBars(i).GetKeySig())
                            {
                                t.ChangeKeySig(KeySigs[i], i);
                                somethingChanged = true;
                            }
                            //if the length of the bar does not reflect the timesig, update the length of the timesig so it does.
                            if (GetSigLength(i) != t.GetBars(i).GetMaxLength())
                            {
                                t.ChangeTimeSig(i, GetSigLength(i));
                                somethingChanged = true;
                            }
                        }
                    }
                }
                //loop until nothing changes
            } while (somethingChanged);
        }

        #region repeats
        //Makes an array of the parameters, adds them to the list of repeats and sorts it to fit the way it is handled in MIDIWriter.
        public void AddRepeat(int bar1, int bar2, int noRepeats = 1)
        {
            //makes sure that the repeat makes sense
            if (noRepeats > 0)
            {
                //makes sure bar1 is the start of the repeat.
                if (bar1 > bar2)
                {
                    int temp = bar1;
                    bar1 = bar2;
                    bar2 = temp;
                }

            }
            //makes all the bars consistent before adding a repeat so repeats cannot be added to bars that are not complete.
            UpdateTotalBars();
            int[] newRepeat = { bar1, bar2, noRepeats };
            Repeats.Add(newRepeat);
            Repeats.Sort(repeatComparison);
        }

        //describes how to compare repeats for sorting - look at the start bar
        public static int repeatComparison(int[] one, int[] two)
        {
            if (one[0] >= one[1]) return 1;
            else return -1;
        }


        //removes the bar at index
        public void RemoveRepeat(int index)
        {
            if (index < Repeats.Count)
            {
                Repeats.RemoveAt(index);
            }
        }
        //returns the list of repeats
        public List<int[]> GetRepeats()
        {
            return Repeats;
        }

        //removes any repeats that start or end at bar index
        public void RemoveProblematicRepeats(int index)
        {
            //do loop structure to catch all occurences when indexes change on removal
            bool somethingChanged;
            do
            {
                somethingChanged = false;
                for (int i = 0; i < Repeats.Count; i++)
                {
                    //look at start and end of bar, compare them to index
                    for (int j = 0; j < 2; j++)
                    {
                        if (Repeats[i][j] == index)
                        {
                            somethingChanged = true;
                            RemoveRepeat(i);
                        }
                    }
                }
            } while (somethingChanged);
        }

        //if the final track is deleted, all repeats must go.
        public void ClearRepeats()
        {
            Repeats.Clear();
        }

        //returns true when a repeat starts or ends on the note (startEnd determines whether it checks for starts or ends).
        //used in drawing
        public bool DoesARepeatStartorEndOn(int index, int startEnd)
        {

            for(int i = 0; i < Repeats.Count; i++)
            {
                if (Repeats[i][startEnd] == index)
                {
                    return true;
                }
            }
            return false;
        }

        //used for the context menu - if it returns -1 a repeat can start there, otherwise the i value can be used in the delete tag
        public int CanARepeatGoHere(int index)
        {
            //loop through all repeats
            for (int i = 0; i < Repeats.Count; i++)
            {
                //if the bar index is in between repeat symbols, a new repeat cannot start here
                if (Repeats[i][0] <= index && Repeats[i][1] >= index)
                {
                    return i;
                }
            }
            return -1;
        }
            

        //used to draw the number above the end repeat symbol when necessary
        public int GetNumberOfRepeatsAtEndIndex(int endIndex)
        {
            //loop through repeats until the correct one is found
            foreach (int[] repeat in Repeats)
            {
                //once the correct one is found
                if (repeat[1] == endIndex)
                {
                    //give the number of repeats
                    return repeat[2];
                }
            }
            //shouldn't get here, but, if it does, just give the normal value.
            return 1;
        }

        //returns the bpm
        public int GetBPM()
        {
            return BPM;
        }

        //changes the bpm
        public void SetBPM(int newBPM)
        {
            BPM = newBPM;
        }

        //returns the title of the song
        public string GetTitle()
        {
            return Title;
        }

        //changes the title of the song
        public void SetTitle(string title)
        {
            Title = title;
        }
        #endregion
        #region accessing tracks
        //returns the track at the given index
        public Track GetTracks(int track)
        {
            //if the track exists
            if (CheckTrack(track))
            {
                return Tracks[track];
            }
            //give an error if the track doesn't exist
            else
            {
                MainWindow.GenerateErrorDialog("invalid operation", "the requested track does not exist");
                return null;
            }
        }

        //returns the name of the track if the track exists
        //otherwise returns an empty string
        public string GetTrackTitle(int track)
        {
            if (CheckTrack(track))
            {
                return Tracks[track].GetName();
            }
            else return "";
        }

        //returns the number of tracks in the song
        public int GetTrackCount()
        {
            return Tracks.Count;
        }

        //adds a note to a given bar in a given track if the track exists, and updates the bars as required
        public void AddNote(int track, int bar, Symbol n)
        {
            if (CheckTrack(track))
            {
                Tracks[track].AddNote(bar, n);
            }
            UpdateTotalBars();
        }

        //adds a new track to the song given all the necessary information
        public void NewTrack(string name, int keysig, int top, int bottom, bool treble)
        {
            //there is a limit of 12 tracks to prevent confusion and some issues with pages.
            if (Tracks.Count < 12)
            {
                Tracks.Add(new Track(name, keysig, top * (16 / bottom), treble));
            }
            else
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "The program has a limit of 12 tracks.");
            }
            //updates the bars as needed
            UpdateTotalBars();

        }

        //removes a track from the list at the given index
        //returns a boolean to say if the operation failed
        public bool DeleteTrack(int track)
        {
            //if there would be no tracks left when this one is deleted then
            //it must return false so that the new track window can be opened and the process can start again
            //if there are no tracks then drawer will throw an error.
            if (Tracks.Count == 1)
            {
                return false;
            }
            //if there is a track at the index
            else if (CheckTrack(track))
            {
                //then remove it - success.
                Tracks.RemoveAt(track);
                return true;
            }
            //otherwise say it worked anyway becuase there was already nothing at the index.
            else
            {
                return true;
            }
        }
        #endregion
        #region carry over from subs

        //calls findnote on the given track, passing all necessary arguments.
        public int FindNote(int track, int bar, int pitch, int startpoint)
        {
            if (CheckTrack(track))
            {
                return Tracks[track].FindNote(bar, pitch, startpoint);
            }
            //if that track doesn't exist, say the process failed.
            else
            {
                return -1;
            }
        }

        //calls delete note on a given track if it exists
        public void DeleteNote(int track, int bar, int noteIndex)
        {
            if (CheckTrack(track))
            {
                Tracks[track].DeleteNote(bar, noteIndex);
            }
        }

        //calls togglestaccato on a given track if it exists
        public void ToggleStaccato(int track, int bar, int noteIndex)
        {
            if (CheckTrack(track))
            {
                Tracks[track].ToggleStaccato(noteIndex, bar);
            }
        }

        //calls create connection on a given track if it exists
        public void CreateConnection(int track, int bar, int noteIndex, Symbol TieTo, bool mode)
        {
            if (CheckTrack(track))
            {
                Tracks[track].CreateConnection(bar, noteIndex, TieTo, mode);
            }
        }

        //calls remove connection on a given track if it exists
        public void RemoveConnection(int track, int barIndex, int noteIndex, bool mode)
        {
            if (CheckTrack(track))
            {
                Tracks[track].RemoveConnection(barIndex, noteIndex, mode);
            }
        }

        //calls getnotestotie on a given track if it exists
        //this is so the list of available connections to make can be displayed
        public List<Symbol> GetNotesToTie(int track, int barIndex, int noteIndex)
        {
            if (CheckTrack(track))
            {
                return Tracks[track].GetNotesToTie(barIndex, noteIndex);
            }
            else
            {
                return null;
            }
        }

        //calls changeaccidental on a given track if it exists
        public void ChangeAccidental(int track, int barIndex, int noteIndex, int newAccidental)
        {
            if (CheckTrack(track))
            {
                Tracks[track].ChangeAccidental(barIndex, noteIndex, newAccidental);
            }
        }

        //changes the value in the keysig list at the given index
        //and changes the keysig at the same bar for each track to reflect this change.
        public void ChangeKeySig(int newSig, int bar)
        {
            KeySigs[bar] = newSig;
            foreach (Track t in Tracks)
            {
                t.ChangeKeySig(newSig, bar);
            }
        }

        //changes the time signature in the list at the given barIndex
        //changes the time sig at the bar index in each track to reflect this change
        public void ChangeBarLength(int barIndex, int top, int bottom)
        {
            TimeSigs[barIndex] = new TimeSig(top, bottom);
            foreach (Track t in Tracks)
            {
                t.ChangeBarLength(barIndex, GetSigLength(barIndex));
            }
        }

        //checks if the index value of track is within the allowed range.
        //if not pass an error to the status bar.
        public bool CheckTrack(int track)
        {
            if (track < Tracks.Count && track >= 0)
            {
                return true;
            }
            else
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "The requested track does not exist.");
                return false;
            }
        }

        //add a bar to the given track if the track exists
        //
        public void NewBar(int track)
        {
            if (CheckTrack(track))
            {
                Tracks[track].NewBar();
            }
            //then update 
            KeySigs.Add(KeySigs[KeySigs.Count - 1]);
            UpdateTotalBars();
        }

        //inserts a bar after the index in params
        public void InsertBarAt(int bar)
        {
            //adds duplicates the time sig and key sig in the bar before
            KeySigs.Insert(bar + 1, KeySigs[bar]);
            TimeSigs.Insert(bar + 1, TimeSigs[bar]);
            //inserts a bar after the index in each track
            foreach (Track t in Tracks)
            {
                t.InsertBarAt(bar + 1);
            }
            //increase total bars
            totalBars++;
        }

        //deletes the bar at index
        public bool DeleteBar(int barIndex)
        {
            //if there's only one left then return false so a new bar can be added and the process started again
            if (totalBars == 1)
            {
                return false;
            }
            else
            {
                //delete the bar in all tracks
                foreach (Track t in Tracks)
                {
                    t.DeleteBar(barIndex);
                }
                //remove a timesig and keysig from the lists
                RemoveProblematicRepeats(barIndex);
                TimeSigs.RemoveAt(barIndex);
                KeySigs.RemoveAt(barIndex);
                //update total bars for consistency.
                totalBars--;
                UpdateTotalBars();
                return true;
            }
        }
        #endregion
    }
}

