using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace Microcontroller_Music
{
    [KnownType(typeof(Bar))]
    [DataContract]
    //biggest class outside of MainWindow. Handles everything that includes the song as a whole, like key signatures,
    //and access of anything below it from mainwindow
    public class Track
    {
        [DataMember]
        private readonly List<Bar> Bars;
        [DataMember]
        //name of the track
        private readonly string Name;
        [DataMember]
        private static readonly string[] CheckFitErrors = { "This process works", "The note is too long to fit in the bar", "A note already exists that means the new note cannot be added", "The note would cause a gap to exist in the bar", "", "The object is a rest and therefore cannot be changed in this way", "Multiple melody lines are not allowed." };
        //used as a return value when checking the fit in a bar
        private int tempCheck;
        [DataMember]
        private readonly bool Treble;

        public Track(string name, int keysig, int timesig, bool treble)
        {
            Name = name;
            Bars = new List<Bar>
            {
                new Bar(timesig, keysig)
            };
            Treble = treble;
        }

        #region getters
        public List<Bar> GetBars()
        {
            return Bars;
        }

        public Bar GetBars(int barIndex)
        {
            return Bars[barIndex];
        }

        public string GetName()
        {
            return Name;
        }

        public bool GetTreble()
        {
            return Treble;
        }
        #endregion

        #region accessing things in bar
        //calls findnote on the bar, passes the result up
        public int FindNote(int barIndex, int pitch, int startPoint)
        {
            return Bars[barIndex].FindNote(pitch, startPoint);
        }

        //deletes the chosen note, selected by findnote.
        public void DeleteNote(int barIndex, int noteIndex, int newLength = -1)
        {
            if (Bars[barIndex].GetTie(noteIndex) != null)
            {
                RemoveConnection(barIndex, noteIndex, true);
            }
            if (Bars[barIndex].GetTiedTo(noteIndex) != null)
            {
                RemoveConnection(barIndex, noteIndex, false);
            }
            Bars[barIndex].DeleteNote(noteIndex, (barIndex != Bars.Count - 1), newLength);
        }

        //changes the time signature of the bar.
        public void ChangeTimeSig(int barIndex, int MaxLength)
        {
            Bars[barIndex].ChangeTimeSig(MaxLength);
        }

        //really just calls togglestaccato
        public void ToggleStaccato(int noteIndex, int barIndex)
        {
            Bars[barIndex].ToggleStaccato(noteIndex);
            if (Bars[barIndex].GetTie(noteIndex) != null)
            {
                RemoveConnection(barIndex, noteIndex, true);
            }
            if (Bars[barIndex].GetTiedTo(noteIndex) != null)
            {
                //makes a new connection - required as the note copy needs updating
                CreateConnection(barIndex, noteIndex, Bars[barIndex].GetTiedTo(noteIndex), false);
            }
        }

        public void CreateConnection(int barIndex, int noteIndex, Symbol TieTo, bool Mode)
        {
            //the given symbol is the second note in the connection
            if (Mode)
            {
                Bars[barIndex].ToggleTie(noteIndex, TieTo);
                if (Bars[barIndex].GetNoteEnd(noteIndex) >= Bars[barIndex].GetMaxLength() && barIndex < Bars.Count - 1)
                {
                    Bars[barIndex + 1].ToggleTiedTo(Bars[barIndex + 1].FindNote(TieTo), Bars[barIndex].GetNotes(noteIndex));
                }
                else if (Bars[barIndex].GetNoteEnd(noteIndex) < Bars[barIndex].GetMaxLength())
                {
                    Bars[barIndex].ToggleTiedTo(Bars[barIndex].FindNote(TieTo), Bars[barIndex].GetNotes(noteIndex));
                }
            }
            //the given symbol is the first note in the connection
            else
            {
                Bars[barIndex].ToggleTiedTo(noteIndex, TieTo);
                if (Bars[barIndex].GetNotes(noteIndex).GetStart() == 0 && barIndex > 0)
                {
                    Bars[barIndex - 1].ToggleTie(Bars[barIndex - 1].FindNote(TieTo), Bars[barIndex].GetNotes(noteIndex));
                }
                else if (Bars[barIndex].GetNotes(noteIndex).GetStart() != 0)
                {
                    Bars[barIndex].ToggleTie(Bars[barIndex].FindNote(TieTo), Bars[barIndex].GetNotes(noteIndex));
                }
            }
        }

        public void RemoveConnection(int barIndex, int noteIndex, bool mode)
        {
            //if the first note in the tie
            if (mode)
            {
                if (Bars[barIndex].GetNoteEnd(noteIndex) >= Bars[barIndex].GetMaxLength() && barIndex < Bars.Count - 1)
                {
                    Bars[barIndex + 1].ToggleTiedTo(Bars[barIndex + 1].FindNote(Bars[barIndex].GetTie(noteIndex)), null);
                }
                else
                {
                    Bars[barIndex].ToggleTiedTo(Bars[barIndex].FindNote(Bars[barIndex].GetTie(noteIndex)), null);
                }
                Bars[barIndex].ToggleTie(noteIndex, null);
            }
            //if the second note in the tie
            else if (!mode)
            {
                if (Bars[barIndex].GetNotes(noteIndex).GetStart() == 0 && barIndex > 0)
                {
                    Bars[barIndex - 1].ToggleTie(Bars[barIndex - 1].FindNote(Bars[barIndex].GetTiedTo(noteIndex)), null);
                }
                else
                {
                    Bars[barIndex].ToggleTie(Bars[barIndex].FindNote(Bars[barIndex].GetTiedTo(noteIndex)), null);
                }
                Bars[barIndex].ToggleTiedTo(noteIndex, null);
            }
        }

        public List<Symbol> GetNotesToTie(int barIndex, int noteIndex)
        {
            int tempLength = Bars[barIndex].GetNotes(noteIndex).GetLength() + Bars[barIndex].GetNotes(noteIndex).GetStart();
            List<Symbol> availableTies = new List<Symbol>();
            if (tempLength < Bars[barIndex].GetMaxLength())
            {
                foreach (Symbol n in Bars[barIndex].GetNotes())
                {
                    if (!(n is Rest) && n.GetStart() == tempLength)
                    {
                        availableTies.Add(n);
                    }
                }
            }
            else if (barIndex < Bars.Count - 1)
            {
                foreach (Symbol n in Bars[barIndex + 1].GetNotes())
                {
                    if (!(n is Rest) && n.GetStart() == 0)
                    {
                        availableTies.Add(n);
                    }
                }
            }
            return availableTies;
        }

        public void AddNote(int barIndex, Symbol n)
        {
            tempCheck = Bars[barIndex].AddNote(n);
            if (tempCheck == 1)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "This note does not fit in the bar");
            }
            else if (tempCheck != 0 && tempCheck != 7)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", CheckFitErrors[tempCheck]);
            }
            if (Bars[Bars.Count - 1].GetLength() == Bars[Bars.Count - 1].GetMaxLength())
            {
                NewBar();
            }
            Bars[barIndex].SortNotes();
            Bars[barIndex].FixSpacing(barIndex != Bars.Count - 1);

        }

        public void ChangeAccidental(int barIndex, int noteIndex, int newAccidental)
        {
            tempCheck = Bars[barIndex].ChangeAccidental(noteIndex, newAccidental);
            if (tempCheck != 0)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", CheckFitErrors[tempCheck]);
            }
            if (Bars[barIndex].GetTie(noteIndex) != null)
            {
                CreateConnection(barIndex, noteIndex, Bars[barIndex].GetTie(noteIndex), true);
            }
            if (Bars[barIndex].GetTiedTo(noteIndex) != null)
            {
                //makes a new connection - required as the note copy needs updating
                CreateConnection(barIndex, noteIndex, Bars[barIndex].GetTiedTo(noteIndex), false);
            }
            Bars[barIndex].SortNotes();
        }

        public void ChangeKeySig(int newSig, int bar)
        {
            Bars[bar].ChangeKeySig(newSig);
        }

        public void ChangeBarLength(int barIndex, int newLength)
        {
            if (barIndex < Bars.Count)
            {
                if (Bars[barIndex].GetMaxLength() > newLength)
                {
                    if (MainWindow.GenerateYesNoDialog("Confirm action", "By shortening the bar, you will delete any notes inside it which no longer fit. Are you sure you want to do this?"))
                    {
                        bool noteDeleted;
                        do
                        {
                            noteDeleted = false;
                            for (int i = 0; i < Bars[barIndex].GetNoteCount(); i++)
                            {
                                if (Bars[barIndex].GetNoteEnd(i) > newLength)
                                {
                                    DeleteNote(barIndex, i, newLength);
                                    noteDeleted = true;
                                }
                            }
                        } while (noteDeleted == true);
                        Bars[barIndex].ChangeTimeSig(newLength);
                    }
                }
                else Bars[barIndex].ChangeTimeSig(newLength);
            }
        }
        #endregion


        #region modifiers
        //adds a new bar with the same metadata as the previous one to the end of the bar.
        public void NewBar()
        {
            Bars.Add(new Bar(Bars[Bars.Count - 1].GetMaxLength(), Bars[Bars.Count - 1].GetKeySig()));
            Bars[Bars.Count - 2].FixSpacing(true);
        }

        public void InsertBarAt(int bar)
        {
            Bars.Insert(bar, new Bar(Bars[Bars.Count - 1].GetMaxLength(), Bars[Bars.Count - 1].GetKeySig()));
            Bars[bar - 1].FixSpacing(true);
        }

        public void DeleteBar(int barIndex)
        {
            for (int i = 0; i < Bars[barIndex].GetNotes().Count; i++)
            {
                RemoveConnection(barIndex, i, true);
                RemoveConnection(barIndex, i, false);
            }
            Bars.RemoveAt(barIndex);
        }
        #endregion
    }
}
