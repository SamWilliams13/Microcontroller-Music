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
        private string Title;
        [DataMember]
        private int BPM;
        [DataMember]
        private List<int> KeySigs;
        [DataMember]
        private List<Track> Tracks;
        [DataMember]
        //0 = start, 1=end, 2=number of repeats
        private List<int[]> Repeats = new List<int[]>();
        [DataMember]
        private int totalBars = 0;
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
        private List<TimeSig> TimeSigs;
        public Song(string title, int bpm, int keySig, int topp = 4, int bot = 4)
        {
            Title = title;
            BPM = bpm;
            Tracks = new List<Track>();
            TimeSigs = new List<TimeSig>();
            TimeSigs.Add(new TimeSig() { bottom = bot, top = topp });
            KeySigs = new List<int>();
            KeySigs.Add(keySig);
        }

        public int GetTotalLength()
        {
            int total = 0;
            foreach (TimeSig t in TimeSigs)
            {
                total += t.top * (16 / t.bottom);
            }
            return total;
        }

        public int GetTotalBars()
        {
            return totalBars;
        }

        public int GetKeySigs(int index)
        {
            return KeySigs[index];
        }

        public List<TimeSig> GetTimeSigs()
        {
            return TimeSigs;
        }

        public TimeSig GetTimeSigs(int index)
        {
            return TimeSigs[index];
        }

        public bool TimeSigIsDifferentToPrevious(int barIndex)
        {
            if (barIndex > 0 && barIndex < TimeSigs.Count)
            {
                return (TimeSigs[barIndex].top != TimeSigs[barIndex - 1].top || TimeSigs[barIndex].bottom != TimeSigs[barIndex - 1].bottom);
            }
            else
            {
                return false;
            }
        }

        public int GetSigLength(int barIndex)
        {
            return TimeSigs[barIndex].top * (16 / TimeSigs[barIndex].bottom);
        }


        #region repeats
        public void AddRepeat(int bar1, int bar2, int noRepeats = 1)
        {
            if (noRepeats > 0 && bar2 != bar1)
            {
                if (bar1 > bar2)
                {
                    int temp = bar1;
                    bar1 = bar2;
                    bar2 = temp;
                }

            }
            UpdateTotalBars();
            int[] newRepeat = { bar1, bar2, noRepeats };
            Repeats.Add(newRepeat);
            Repeats.Sort();
        }

        public void UpdateTotalBars() //makes sure that all tracks have the same number of bars. important for repeats and adding notes
        {
            bool somethingChanged = false;
            do
            {
                somethingChanged = false;
                foreach (Track t in Tracks)
                {
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
                    else if (t.GetBars().Count < totalBars)
                    {
                        somethingChanged = true;
                        for (int j = 0; j < totalBars - t.GetBars().Count; j++)
                        {
                            t.NewBar();
                        }
                    }
                    if (!somethingChanged)
                    {
                        for (int i = 0; i < totalBars; i++)
                        {
                            if (KeySigs[i] != t.GetBars(i).GetKeySig())
                            {
                                t.ChangeKeySig(KeySigs[i], i);
                                somethingChanged = true;
                            }
                            if (GetSigLength(i) != t.GetBars(i).GetMaxLength())
                            {
                                t.ChangeTimeSig(i, GetSigLength(i));
                                somethingChanged = true;
                            }
                        }
                    }
                }
            } while (somethingChanged);
        }

        public void RemoveRepeat(int bar1)
        {
            foreach (int[] repeat in Repeats)
            {
                if (repeat[0] == bar1)
                {
                    Repeats.Remove(repeat);
                }
            }
        }

        public List<int[]> GetRepeats()
        {
            return Repeats;
        }

        public int GetBPM()
        {
            return BPM;
        }

        public string GetTitle()
        {
            return Title;
        }

        public void SetTitle(string title)
        {
            Title = title;
        }
        #endregion
        #region accessing tracks
        public Track GetTracks(int track)
        {
            if(CheckTrack(track))
            {
                return Tracks[track];
            }
            else
            {
                MainWindow.GenerateErrorDialog("invalid operation", "the requested track does not exist");
                return null;
            }
        }
        
        public List<Track> GetTracks()
        {
            return Tracks;
        }

        public void AddNote(int track, int bar, Symbol n)
        {
            if (CheckTrack(track))
            {
                Tracks[track].AddNote(bar, n);
            }
            UpdateTotalBars();
        }

        public void NewTrack(string name, int keysig, int top, int bottom, bool treble)
        {
            if (Tracks.Count < 12)
            {
                Tracks.Add(new Track(name, keysig, top * (16 / bottom), treble));
            }
            else
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "The program has a limit of 12 tracks.");
            }

                UpdateTotalBars();
            
        }

        public bool DeleteTrack(int track)
        {
            if (Tracks.Count == 1)
            {
                return false;
            }
            else if (CheckTrack(track))
            {
                Tracks.RemoveAt(track);
                return true;
            }
            else
            {
                return true;
            }
        }
        #endregion
        #region carry over from subs
        public int FindNote(int track, int bar, int pitch, int startpoint)
        {
            if (CheckTrack(track))
            {
                return Tracks[track].FindNote(bar, pitch, startpoint);
            }
            else
            {
                return -1;
            }
        }

        public int FindNote(int track, int bar, Symbol n)
        {
            if (CheckTrack(track))
            {
                return Tracks[track].FindNote(bar, n);
            }
            else
            {
                return -1;
            }
        }

        public void DeleteNote(int track, int bar, Symbol n)
        {
            if (CheckTrack(track))
            {
                Tracks[track].DeleteNote(bar, n);
            }
        }

        public void DeleteNote(int track, int bar, int noteIndex)
        {
            if (CheckTrack(track))
            {
                Tracks[track].DeleteNote(bar, noteIndex);
            }
        }

        public void ChangeTimeSig(int track, int bar, int timeSig)
        {
            if (CheckTrack(track))
            {
                Tracks[track].ChangeTimeSig(bar, timeSig);
            }
        }
        public void ToggleStaccato(int track, int bar, int noteIndex)
        {
            if (CheckTrack(track))
            {
                Tracks[track].ToggleStaccato(noteIndex, bar);
            }
        }

        public void ToggleRest(int track, int bar, int noteIndex)
        {
            if (CheckTrack(track))
            {
                Tracks[track].ToggleRest(bar, noteIndex);
            }
        }

        public void CreateConnection(int track, int bar, int noteIndex, Symbol TieTo, bool mode)
        {
            if (CheckTrack(track))
            {
                Tracks[track].CreateConnection(bar, noteIndex, TieTo, mode);
            }
        }

        public void RemoveConnection(int track, int barIndex, int noteIndex, bool mode)
        {
            if (CheckTrack(track))
            {
                Tracks[track].RemoveConnection(barIndex, noteIndex, mode);
            }
        }

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

        public void ChangeAccidental(int track, int barIndex, int noteIndex, int newAccidental)
        {
            if(CheckTrack(track))
            {
                Tracks[track].ChangeAccidental(barIndex, noteIndex, newAccidental);
            }
        }

        public void ChangePitch(int track, int barIndex, int noteIndex, int newPitch)
        {
            if(CheckTrack(track))
            {
                Tracks[track].ChangePitch(barIndex, noteIndex, newPitch);
            }
        }

        public void ChangeKeySig(int newSig, int bar)
        {
            KeySigs[bar] = newSig;
            foreach(Track t in Tracks)
            {
                t.ChangeKeySig(newSig, bar);
            }
        }

        public void ChangeStartPoint(int track, int barIndex, int noteIndex, int newStart)
        {
            if(CheckTrack(track))
            {
                Tracks[track].ChangeStartPoint(barIndex, noteIndex, newStart);
            }
        }

        public void ChangeNoteLength(int track, int barIndex, int noteIndex, int newLength)
        {
            if(CheckTrack(track))
            {
                Tracks[track].ChangeNoteLength(barIndex, noteIndex, newLength);
            }
        }

        public void ChangeBarLength(int barIndex, int top, int bottom)
        {
            TimeSigs[barIndex] = new TimeSig(top, bottom);
            foreach(Track t in Tracks)
            {
                t.ChangeBarLength(barIndex, GetSigLength(barIndex));
            }
        }

        public bool CheckTrack(int track)
        {
            if (track < Tracks.Count)
            {
                return true;
            }
            else
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "The requested track does not exist.");
                return false;
            }
        }

        public void NewBar(int track)
        {
            if(CheckTrack(track))
            {
                Tracks[track].NewBar();
            }
            KeySigs.Add(KeySigs[KeySigs.Count - 1]);
            UpdateTotalBars();
        }

        public bool DeleteBar(int barIndex)
        {
            if (totalBars == 1)
            {
                return false;
            }
            else
            {
                foreach (Track t in Tracks)
                {
                    t.DeleteBar(barIndex);
                }
                TimeSigs.RemoveAt(barIndex);
                KeySigs.RemoveAt(barIndex);
                totalBars--;
                UpdateTotalBars();
                return true;
            }
        }

        #endregion
    }
}

