using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microcontroller_Music
{
    [KnownType(typeof(Symbol))]
    [KnownType(typeof(Note))]
    [KnownType(typeof(Rest))]
    [DataContract]
    public class Bar
    {
        //retrievable attributes
        [DataMember]
        //stores all the notes in a bar
        private readonly List<Symbol> Notes;
        [DataMember]
        //stores the maximum length of the bar given its time signature
        private int MaxLength;
        [DataMember]
        //contains a number between + and - 7 relating to how many sharps/flats key signature has.
        private int KeySigIndex;
        [DataMember]
        //determines how far into the note there is currently noise so that when there is a gap it can be fixed.
        int Length = 0;
        [DataMember]
        readonly bool multipleMelodyLines;
        //internally used attributes
        //temporary value used for checking whether a change should be made.
        private Symbol changedSymbol;
        //temporary value to store the code returned by a checkfit test.
        private int tempCheck;

        //constructor
        public Bar(int maxLength, int keysig)
        {
            //creates list
            Notes = new List<Symbol>();
            //sets maxlength
            MaxLength = maxLength;
            KeySigIndex = keysig;
            multipleMelodyLines = false;
        }

        #region getters
        //returns the notes in a bar - used by song
        public List<Symbol> GetNotes()
        {
            return Notes;
        }

        //returns the number of symbols stored in the bar
        public int GetNoteCount()
        {
            return Notes.Count;
        }

        //returns the symbol at the given index.
        public Symbol GetNotes(int noteIndex)
        {
            return Notes[noteIndex];
        }

        //returns the current length of the symbols in the bar.
        public int GetLength()
        {
            return Length;
        }

        //returns the key signature for drawing/writing/creating adjacent bars
        public int GetKeySig()
        {
            return KeySigIndex;
        }

        //returns the length in semiquavers of the bar
        public int GetMaxLength()
        {
            return MaxLength;
        }

        //gets the endpoint of the symbol at the given index
        public int GetNoteEnd(int noteIndex)
        {
            return Notes[noteIndex].GetStart() + Notes[noteIndex].GetLength();
        }

        //returns the symbol connected to the symbol at the given index
        public Symbol GetTie(int noteIndex)
        {
            if (noteIndex < Notes.Count && Notes[noteIndex] is Note)
            {
                return (Notes[noteIndex] as Note).GetTie();
            }
            else return null;
        }

        //returns the symbol that a note at the given note index is connected to
        public Symbol GetTiedTo(int noteIndex)
        {
            //if the note exists
            if (noteIndex < Notes.Count)
            {
                //and is a note
                if (Notes[noteIndex] is Note)
                {
                    return (Notes[noteIndex] as Note).GetTiedTo();
                }
                else return null;
            }
            else return null;
        }

        //used to check if the previous bar needs to be redrawn too when a note is deleted
        public bool ZeroNoteIsTied()
        {
            bool tied = false;
            foreach (Symbol n in Notes)
            {
                if(n is Note && (n as Note).GetTiedTo() != null && n.GetStart() == 0)
                {
                    tied = true;
                    break;
                }
            }
            return tied;
        }
        #endregion

        #region checking contents

        //checks if the note fits in the bar. sees if length is ok, if it is at the same time as a rest, or the same as another preexisting note.
        public int CheckFit(Symbol newNote)
        {
            if ((newNote.GetStart() + newNote.GetLength()) > MaxLength)
            {
                //if the note's too long to fit in the bar, don't do it. 1 means too long
                return 1;
            }
            foreach (Symbol n in Notes)
            {
                //line is long due to lots of combinations
                //checks if 2 notes in the bar overlap
                if (((newNote.GetStart() <= n.GetStart()) && (newNote.GetStart() + newNote.GetLength()) > n.GetStart()) ||
                    ((n.GetStart() <= newNote.GetStart()) && ((n.GetStart() + n.GetLength()) > newNote.GetStart())))
                {
                    if (!multipleMelodyLines && !(n.GetStart() == newNote.GetStart() && n.GetLength() == newNote.GetLength()) && !(n is Rest))
                    {
                        //tells the user that multiple melody lines are turned off.
                        return 6;
                    }
                    //handles Rest/Note, Rest/Rest
                    if (newNote is Rest)
                    {
                        //rests cannot happen at the same time as notes or other rests, but notes can replace rests.
                        return 2;
                    }
                    //Handles Note/Note
                    //if both are notes and they would take up the same space in the stave
                    else if (newNote is Note && n is Note && ((newNote as Note).GetPitch() - (newNote as Note).GetAccidental()) == (n as Note).GetPitch() - (n as Note).GetAccidental())
                    {
                        return 2;
                    }
                }
            }
            //0 means the change is possible
            return 0;
        }

        //returns the index of the note that has the same startpoint and pitch as requested
        public int FindNote(int pitch, int startPoint)
        {
            List<int[]> Matches = new List<int[]>();
            //loop through all the symbols
            for (int i = 0; i < Notes.Count; i++)
            {
                //if it's a rest it can't check for pitch as it will fail.
                if (Notes[i] is Rest && Math.Abs(startPoint - Notes[i].GetStart()) <= 1)
                {
                    continue;
                }
                //if its a note then it could be a match
                else if (Notes[i] is Note)
                {
                    //if the note (ignoring accidental) is close enough to the pitch requested and they have the same startpoint
                    if (Math.Abs((Notes[i] as Note).GetPitch() - (Notes[i] as Note).GetAccidental() - pitch) <= 1 && startPoint == Notes[i].GetStart())
                    {
                        //add it to the list of possible candidates for matches
                        Matches.Add(new int[] { (Notes[i] as Note).GetPitch() - (Notes[i] as Note).GetAccidental(), i });
                    }
                }
            }
            //if there is at least one match
            if (Matches.Count != 0)
            {
                int comparison = 10000;
                int index = 0;
                //loop through each of the matches to find the one with the closest pitch.
                //this is less necessary as the mouse is reasonably accurate.
                for (int i = 0; i < Matches.Count; i++)
                {
                    if (Math.Abs(Matches[i][0] - pitch) < comparison)
                    {
                        comparison = Math.Abs(Matches[i][0] - pitch);
                        index = Matches[i][1];
                    }
                }
                return index;
            }
            //not found
            return -1;
        }

        //a different findnote that just looks at the bar to see if there is the exact note requested.
        public int FindNote(Symbol n)
        {
            return Notes.FindIndex(x => x == n);
        }
        #endregion

        #region modifiers
        //removes the note from the list at the index given.
        public void DeleteNote(int index, bool lastBar, int newLength = -1)
        {
            Notes.RemoveAt(index);
            //fixes the spacing in the bar.
            FixSpacing(lastBar, newLength);
        }

        //alternate version of delete where the exact note to be deleted is given as argument
        public void DeleteNote(Symbol note)
        {
            Notes.Remove(note);
        }

        //changes the length of a bar
        public void ChangeTimeSig(int newLength)
        {
            MaxLength = newLength;
            FixSpacing(true);
        }

        //deletes all the rests in the bar and calculates new ones.
        public void FixSpacing(bool notTheLastBar, int newLength = -1)
        {
            //noise until finds the start of a non rest or note part.
            int noiseUntil = 0;
            int fixUntil;
            bool restRemoved;
            //loop through all the symbols until all rests are removed
            do
            {
                restRemoved = false;
                for (int i = 0; i < Notes.Count; i++)
                {
                    if (Notes[i] is Rest)
                    {
                        Notes.Remove(Notes[i]);
                        restRemoved = true;
                    }
                }
            } while (restRemoved == true);
            //loop through all the remaining notes in the list
            for (int i = 0; i < Notes.Count; i++)
            {
                //if there is a gap between the end of the last symbol and the start of this one
                if (Notes[i].GetStart() > noiseUntil)
                {
                    //then fill that space with rests
                    FillGap(Notes[i].GetStart(), ref noiseUntil);
                    //increment noiseUntil to reflect change
                    noiseUntil += Notes[i].GetLength();
                }
                //otherwise just update noiseUntil to be until the end of the symbol.
                else if ((Notes[i].GetStart() + Notes[i].GetLength()) > noiseUntil)
                {
                    noiseUntil = Notes[i].GetStart() + Notes[i].GetLength();
                }
            }
            //if the time sig is being changed.
            if (newLength != -1)
            {
                fixUntil = newLength;
            }
            //if it isnt the last bar then there must be noise until the end of the bar
            else if (notTheLastBar)
            {
                fixUntil = MaxLength;
            }
            //if not it just has to be until the last note
            else fixUntil = Length;
            //if there is space between the end of the last note and the length it needs to be
            if (noiseUntil < fixUntil)
            {
                //then add rests
                FillGap(fixUntil, ref noiseUntil);
            }
            //sort the symbols so all the rests are in the right place
            SortNotes();
        }

        //places rests between a start and end point
        private void FillGap(int fixUntil, ref int noiseUntil)
        {
            //the length of time needing filling
            int gapLength = fixUntil - noiseUntil;
            int restLength = 0;
            for (int j = 16; j >= 2; j /= 2)
            {
                //if the note fits in the space
                if (noiseUntil % j == 0 && j <= gapLength)
                {
                    //the rest should be this length
                    restLength = j;
                    break;
                }
            }
            //makes sure that semiquaver rests can work - worried that 1/2 will round to 1
            if (restLength == 0) restLength = 1;
            //add the rest to the bar
            Notes.Add(new Rest(restLength, noiseUntil));
            //length to fill gets shorter, start point gets larger
            gapLength -= restLength;
            noiseUntil += restLength;
            //if the gap isn't full yet
            if (gapLength > 0)
            {
                //be recursive
                FillGap(fixUntil, ref noiseUntil);
            }

        }

        //checks a new note fits and adds it to the bar if it does.
        public int AddNote(Symbol n) 
        {
            //the first part changes the note based on the key signature of the bar.
            if (n is Note)
            {
                //essentially loops through the possible key signatures and checks if the note matches a symbol present in signature.
                for (int i = 1; i < 8; i++)
                {
                    if (KeySigIndex >= i && (n as Note).GetPitch() % 12 == (7 * i + 2) % 12)
                    {
                        (n as Note).SetAccidental(1);
                    }
                    if (KeySigIndex <= (-1 * i) && (n as Note).GetPitch() % 12 == Math.Abs((5 * i + 10) % 12))
                    {
                        (n as Note).SetAccidental(-1);
                    }
                }
            }
            //temporary value to check if the note fits
            tempCheck = CheckFit(n);
            //if the note fits
            if (tempCheck == 0) 
            {
                //add the note to the list
                Notes.Add(n); 
                if (n.GetStart() + n.GetLength() > Length)
                {
                    //updates the current length of the bar if necessary.
                    Length = n.GetStart() + n.GetLength();
                }
            }
            //makes sure the note has the same staccato value.
            StaccatoConsistency();
            return tempCheck;
        }

        //makes sure all notes at the same time are all staccato or all not.
        public void StaccatoConsistency()
        {
            //stores whether the target is staccato or not
            bool stac = false;
            int start = -1;
            //loop through the notes
            foreach (Symbol symbol in Notes)
            {
                if (symbol is Note)
                {
                    Note note = symbol as Note;
                    //if it has same startpoint as previous note and isn't the same staccato value
                    if (note.GetStart() == start && note.GetStaccato() != stac)
                    {
                        //then make it the same value
                        note.ToggleStaccato();
                    }
                    //if it isn't the same startpoint
                    else if (note.GetStart() != start)
                    {
                        //change startpoint and staccato to match new note.
                        start = note.GetStart();
                        stac = note.GetStaccato();
                    }
                }
            }
        }

        //sets note at the index's tie symbol to the argument
        public void ToggleTie(int noteIndex, Symbol tiedNote) 
        {
            //that is of course if the note exists
            if (noteIndex != -1)
            {
                if (Notes[noteIndex] is Note)
                {
                    (Notes[noteIndex] as Note).ToggleTie(tiedNote);
                }
            }
        }

        //if the note at index exists then set its tiedto symbol to the argument
        public void ToggleTiedTo(int noteIndex, Symbol TiedTo)
        {
            if (noteIndex != -1)
            {
                if (Notes[noteIndex] is Note)
                {
                    (Notes[noteIndex] as Note).SetTiedTo(TiedTo);
                }
            }
        }

        //if a note exists at the index then toggle staccato
        public void ToggleStaccato(int noteIndex)
        {
            if (noteIndex < Notes.Count)
            {
                if (Notes[noteIndex] is Note)
                {
                    //toggle staccato 
                    Note note = Notes[noteIndex] as Note;
                    note.ToggleStaccato();
                    //loop through all notes to make sure that concurrent notes have the same staccato value (using XOR to find out if they're different)
                    foreach (Symbol notecheck in Notes)
                    {
                        if (notecheck is Note && notecheck.GetStart() == note.GetStart() && ((notecheck as Note).GetStaccato() ^ note.GetStaccato()))
                        {
                            (notecheck as Note).ToggleStaccato();
                        }
                    }

                }
            }
        }

        //checks if the new pitch works, then uses note.setaccidental
        public int ChangeAccidental(int noteIndex, int newAccident) 
        {
            int original ;
            changedSymbol = Notes[noteIndex];
            //can't put accidental on rest
            if (Notes[noteIndex] is Note)
            {
                //make a spare note 
                original = (changedSymbol as Note).GetAccidental();
                (changedSymbol as Note).SetAccidental(newAccident);
                //remove the old note so it can't clash in checkfit
                DeleteNote(changedSymbol);
                //checks that the new note it makes fits
                tempCheck = CheckFit(changedSymbol);
                //if the new accidental fits add it to the bar
                if (tempCheck == 0)
                {
                    //uses the note method to change the accidental
                    Notes.Add(changedSymbol);
                }
                //if the checkfit failed add the original version back
                else
                {
                    (changedSymbol as Note).SetAccidental(original);
                    Notes.Add(changedSymbol);
                }
                return tempCheck;
            }
            //error: is rest.
            else return 5;
        }

        //handles changing the key signature
        public void ChangeKeySig(int newSig)
        {
            //makes sure new signature is valid. if not send error message
            if (newSig <= 7 && newSig >= -7)
            {
                KeySigIndex = newSig;
            }
            else MainWindow.GenerateErrorDialog("Invalid Operation", "This is not a real key signature");
        }
        #endregion

        #region sorting
        //compares one note to another for sorting in a list.
        public static int NoteComparison(Symbol one, Symbol two)
        {
            //if same start, compare pitch. should only happen if there are two notes.
            if (one.GetStart() == two.GetStart())
            {
                int pitchOne = 0;
                int pitchTwo = 0;
                if (one is Note)
                {
                    pitchOne = (one as Note).GetPitch();
                }
                if (two is Note)
                {
                    pitchTwo = (two as Note).GetPitch();
                }
                //higher pitch comes first
                if (pitchOne >= pitchTwo)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            //later start == larger, larger values come later in list
            else if (one.GetStart() > two.GetStart())
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        //sorts the notes. Essentially calls a sort function with noteComparison as the comparison
        public void SortNotes()
        {
            Notes.Sort(NoteComparison);
        }
        #endregion
    }
}
