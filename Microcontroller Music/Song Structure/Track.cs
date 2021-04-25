using System.Collections.Generic;
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
        //returns the number of bars in the track
        public int GetBarCount()
        {
            return Bars.Count;
        }

        //returns the bar at a given index
        public Bar GetBars(int barIndex)
        {
            return Bars[barIndex];
        }

        //returns the name of the track
        public string GetName()
        {
            return Name;
        }

        //returns whether the track is treble
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
            //if it is tied to something after...
            if (Bars[barIndex].GetTie(noteIndex) != null)
            {
                RemoveConnection(barIndex, noteIndex, true);
            }
            //...or before, remove that connection
            if (Bars[barIndex].GetTiedTo(noteIndex) != null)
            {
                RemoveConnection(barIndex, noteIndex, false);
            }
            //then delete the note
            Bars[barIndex].DeleteNote(noteIndex, (barIndex != Bars.Count - 1), newLength);
        }

        //really just calls togglestaccato
        public void ToggleStaccato(int noteIndex, int barIndex)
        {
            Bars[barIndex].ToggleStaccato(noteIndex);
            //it cannot be tied to another note after it so remove that connection
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

        //used to check if the previous bar needs to be redrawn too when a note is deleted
        public bool ZeroNoteIsTied(int bar)
        {
            return Bars[bar].ZeroNoteIsTied();
        }

        //creates a connection between 2 notes
        public void CreateConnection(int barIndex, int noteIndex, Symbol TieTo, bool Mode)
        {
            //the given symbol is the second note in the connection
            if (Mode)
            {
                Bars[barIndex].ToggleTie(noteIndex, TieTo);
                //if the second note is in the next bar
                if (Bars[barIndex].GetNoteEnd(noteIndex) >= Bars[barIndex].GetMaxLength() && barIndex < Bars.Count - 1)
                {
                    //find the index of the second note and grab the symbol for the indexed one, then toggle tied to.
                    Bars[barIndex + 1].ToggleTiedTo(Bars[barIndex + 1].FindNote(TieTo), Bars[barIndex].GetNotes(noteIndex));
                }
                else if (Bars[barIndex].GetNoteEnd(noteIndex) < Bars[barIndex].GetMaxLength())
                {
                    //same as above but for 2 notes in same bar
                    Bars[barIndex].ToggleTiedTo(Bars[barIndex].FindNote(TieTo), Bars[barIndex].GetNotes(noteIndex));
                }
            }
            //the given symbol is the first note in the connection
            else
            {
                Bars[barIndex].ToggleTiedTo(noteIndex, TieTo);
                //if they are in different bars
                if (Bars[barIndex].GetNotes(noteIndex).GetStart() == 0 && barIndex > 0)
                {
                    //find the index of first note and symbol of second, toggle tie
                    Bars[barIndex - 1].ToggleTie(Bars[barIndex - 1].FindNote(TieTo), Bars[barIndex].GetNotes(noteIndex));
                }
                else if (Bars[barIndex].GetNotes(noteIndex).GetStart() != 0)
                {
                    //same as above but for 2 notes in same bar
                    Bars[barIndex].ToggleTie(Bars[barIndex].FindNote(TieTo), Bars[barIndex].GetNotes(noteIndex));
                }
            }
        }

        public void RemoveConnection(int barIndex, int noteIndex, bool mode)
        {
            //if the first note in the tie
            if (mode)
            {
                //if the tied note is in the next bar, find the note that isn't given and set its backwards connection to null
                if (Bars[barIndex].GetNoteEnd(noteIndex) >= Bars[barIndex].GetMaxLength() && barIndex < Bars.Count - 1)
                {
                    Bars[barIndex + 1].ToggleTiedTo(Bars[barIndex + 1].FindNote(Bars[barIndex].GetTie(noteIndex)), null);
                }
                //otherwise its a little easier to find but you do the same thing
                else
                {
                    Bars[barIndex].ToggleTiedTo(Bars[barIndex].FindNote(Bars[barIndex].GetTie(noteIndex)), null);
                }
                //then remove the forwards connection from the given index
                Bars[barIndex].ToggleTie(noteIndex, null);
            }
            //if the second note in the tie
            else if (!mode)
            {
                //if the first note is in the previous bar
                if (Bars[barIndex].GetNotes(noteIndex).GetStart() == 0 && barIndex > 0)
                {
                    //find it and remove forwards connection
                    Bars[barIndex - 1].ToggleTie(Bars[barIndex - 1].FindNote(Bars[barIndex].GetTiedTo(noteIndex)), null);
                }
                else
                {
                    //otherwise do the same but look in the same bar instead of previous
                    Bars[barIndex].ToggleTie(Bars[barIndex].FindNote(Bars[barIndex].GetTiedTo(noteIndex)), null);
                }
                //remove backwards tie from given index
                Bars[barIndex].ToggleTiedTo(noteIndex, null);
            }
        }

        //returns a list of all possible notes to be tied to. Used for generation of the connection context menu.
        public List<Symbol> GetNotesToTie(int barIndex, int noteIndex)
        {

            int tempLength = Bars[barIndex].GetNoteEnd(noteIndex);
            //stores all notes that fit.
            List<Symbol> availableTies = new List<Symbol>();
            //if the note it connects to is in the same bar
            if (tempLength < Bars[barIndex].GetMaxLength())
            {
                //loop through all the symbols and add the notes that start where the indexed note ends.
                foreach (Symbol n in Bars[barIndex].GetNotes())
                {
                    if (!(n is Rest) && n.GetStart() == tempLength)
                    {
                        availableTies.Add(n);
                    }
                }
            }
            //if the note it connects to is in the next bar
            else if (barIndex < Bars.Count - 1)
            {
                //loop through all the symbols in the next bar and add any notes that start at the start of the bar
                foreach (Symbol n in Bars[barIndex + 1].GetNotes())
                {
                    if (!(n is Rest) && n.GetStart() == 0)
                    {
                        availableTies.Add(n);
                    }
                }
            }
            //then return the list
            return availableTies;
        }

        //adds a symbol to a given bar
        public void AddNote(int barIndex, Symbol n)
        {
            //tries to add the note to the bar, gets back an integer response
            tempCheck = Bars[barIndex].AddNote(n);
            //generates an error message dependent on what caused the failure
            if (tempCheck != 0 && tempCheck != 7)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", CheckFitErrors[tempCheck]);
            }
            //if the bar has reached its max length then add a new bar to the track
            if (Bars[Bars.Count - 1].GetLength() == Bars[Bars.Count - 1].GetMaxLength())
            {
                NewBar();
            }
            //sort the notes and rest spacing
            Bars[barIndex].SortNotes();
            Bars[barIndex].FixSpacing(barIndex != Bars.Count - 1);

        }

        public void ChangeAccidental(int barIndex, int noteIndex, int newAccidental)
        {
            //tries to change the note in the bar
            tempCheck = Bars[barIndex].ChangeAccidental(noteIndex, newAccidental);
            //if it fails show an error message
            if (tempCheck != 0)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", CheckFitErrors[tempCheck]);
            }
            //if the note has connections, update these connections to reflect the changes
            if (Bars[barIndex].GetTie(noteIndex) != null)
            {
                CreateConnection(barIndex, noteIndex, Bars[barIndex].GetTie(noteIndex), true);
            }
            if (Bars[barIndex].GetTiedTo(noteIndex) != null)
            {
                //makes a new connection - required as the note copy needs updating
                CreateConnection(barIndex, noteIndex, Bars[barIndex].GetTiedTo(noteIndex), false);
            }
            //sort the notes to make sure the other algorithms function
            Bars[barIndex].SortNotes();
        }

        //changes the key signature at a given index
        public void ChangeKeySig(int newSig, int bar)
        {
            Bars[bar].ChangeKeySig(newSig);
        }

        //changes the length of a bar based on a new time signature - length calculated before and given in semiquavers
        public void ChangeBarLength(int barIndex, int newLength)
        {
            //if the bar exists
            if (barIndex < Bars.Count && barIndex >= 0)
            {
                //if there are notes in the bar that do not fit in the new length
                if (Bars[barIndex].GetMaxLength() > newLength)
                {
                    //make sure the user wants to go ahead
                    if (MainWindow.GenerateYesNoDialog("Confirm action", "By shortening the bar, you will delete any notes inside it which no longer fit. Are you sure you want to do this?"))
                    {
                        bool noteDeleted;
                        //delete all notes that will no longer fit (while loop needed because of changing indexes in the list whenever a symbol is removed)
                        do
                        {
                            noteDeleted = false;
                            for (int i = 0; i < Bars[barIndex].GetNoteCount(); i++)
                            {
                                if (Bars[barIndex].GetNoteEnd(i) > newLength)
                                {
                                    //new parameter needed in several functions to make sure that new rests do not replace the old ones and get stuck in an infinite loop.
                                    DeleteNote(barIndex, i, newLength);
                                    noteDeleted = true;
                                }
                            }
                        } while (noteDeleted == true);
                        //then change the time signature of the bar after this is done
                        Bars[barIndex].ChangeTimeSig(newLength);
                    }
                }
                //if all the notes will fit you can just go straight ahead and change it.
                else Bars[barIndex].ChangeTimeSig(newLength);
            }
        }
        #endregion


        #region modifiers
        //adds a new bar with the same metadata as the previous one to the end of the bar.
        public void NewBar()
        {
            Bars.Add(new Bar(Bars[Bars.Count - 1].GetMaxLength(), Bars[Bars.Count - 1].GetKeySig()));
            //fix the rests on the bar before the new one so it reaches full length
            Bars[Bars.Count - 2].FixSpacing(true);
        }

        //adds a new bar at the given index
        public void InsertBarAt(int bar)
        {
            //makes a new bar with the same properties as the previous bar and inserts it into the list
            Bars.Insert(bar, new Bar(Bars[Bars.Count - 1].GetMaxLength(), Bars[Bars.Count - 1].GetKeySig()));
            //fix all the rests in the previous bar to make sure that it is full length
            Bars[bar - 1].FixSpacing(true);
            Bars[bar].FixSpacing(bar < Bars.Count - 1);
        }

        //removes a bar from the list at index
        public void DeleteBar(int barIndex)
        {
            //loops through all symbols in the bar and removes any connections
            for (int i = 0; i < Bars[barIndex].GetNotes().Count; i++)
            {
                RemoveConnection(barIndex, i, true);
                RemoveConnection(barIndex, i, false);
            }
            //then removes the bar from the list
            Bars.RemoveAt(barIndex);
        }
        #endregion
    }
}
