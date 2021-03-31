using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microcontroller_Music;
using System.IO;
using System.Runtime.Serialization;

namespace Microcontroller_Music
{
    [KnownType(typeof(Symbol))]
    [KnownType(typeof(Note))]
    [KnownType(typeof(Tuplet))]
    [KnownType(typeof(Rest))]
    [DataContract]
    public class Bar
    {
        //retrievable attributes
        [DataMember]
        private readonly List<Symbol> Notes; //stores all the notes in a bar
        [DataMember]
        private int MaxLength; //stores the maximum length of the bar given its time signature
        [DataMember]
        private int KeySigIndex; //contains a number between + and - 7 relating to how many sharps/flats key signature has.
        [DataMember]
        int Length = 0; //determines how far into the note there is currently noise so that when there is a gap it can be fixed.
        [DataMember]
        readonly bool multipleMelodyLines;
        //internally used attributes
        private Symbol changedSymbol; //temporary value used for checking whether a change should be made.
        private int tempCheck; //temporary value to store the code returned by a checkfit test.
        int TupletIndex = 0; //changed during findnote so that the user can click on a specific part of a tuplet.
        //the array above stores descriptions for what went wrong in every checkfit failstate

        public Bar(int maxLength, int keysig) //constructor
        {
            Notes = new List<Symbol>(); //creates list
            MaxLength = maxLength; //sets maxlength
            KeySigIndex = keysig;
            multipleMelodyLines = false;
        }

        #region getters
        public List<Symbol> GetNotes() //returns the notes in a bar - used by song
        {
            return Notes; //pretty much it
        }

        public int GetNoteCount()
        {
            return Notes.Count;
        }

        public Symbol GetNotes(int noteIndex)
        {
            return Notes[noteIndex];
        }

        public int GetLength() //returns the current length of the symbols in the bar.
        {
            return Length;
        }

        public int GetKeySig() //returns the key signature for drawing/writing/creating adjacent bars
        {
            return KeySigIndex;
        }

        public int GetMaxLength() //in case of an unforeseen future use
        {
            return MaxLength;
        }

        public int GetTupletIndex()
        {
            return TupletIndex;
        }

        public int GetPitch(int noteIndex, int tupletIndex = 0) //handles getting the pitch or an error from any note
        {
            if (Notes[noteIndex] is Note)
            {
                return ((Notes[noteIndex] as Note).GetPitch());
            }
            else if (Notes[noteIndex] is Tuplet)
            {
                if ((Notes[noteIndex] as Tuplet).GetComponent(tupletIndex) is Note)
                {
                    return ((Notes[noteIndex] as Tuplet).GetComponent(tupletIndex) as Note).GetPitch();
                }
            }
            return -1;
        }

        public int GetNoteEnd(int noteIndex)
        {
            return Notes[noteIndex].GetStart() + Notes[noteIndex].GetLength();
        }

        public Symbol GetTie(int noteIndex)
        {
            if (noteIndex < Notes.Count)
            {
                if (Notes[noteIndex] is Note)
                {
                    return (Notes[noteIndex] as Note).GetTie();
                }
                else if (Notes[noteIndex] is Tuplet)
                {
                    return (Notes[noteIndex] as Tuplet).GetTie();
                }
                else return null;
            }
            else return null;
        }

        public Symbol GetTiedTo(int noteIndex)
        {
            if (noteIndex < Notes.Count)
            {
                if (Notes[noteIndex] is Note)
                {
                    return (Notes[noteIndex] as Note).GetTiedTo();
                }
                else if (Notes[noteIndex] is Tuplet)
                {
                    return (Notes[noteIndex] as Tuplet).GetTiedTo();
                }
                else return null;
            }
            else return null;
        }
        #endregion

        #region checking contents
        public int CheckFit(Symbol newNote) //checks if the note fits in the bar. sees if length is ok, if it is at the same time as a rest, or the same as another preexisting note.
        {
            if ((newNote.GetStart() + newNote.GetLength()) > MaxLength)
            {
                return 1; //if the note's too long to fit in the bar, don't do it. 1 means too long
            }
            foreach (Symbol n in Notes)
            {
                //line is long due to lots of combinations
                //checks if 2 notes in the bar overlap
                if (((newNote.GetStart() <= n.GetStart()) && (newNote.GetStart() + newNote.GetLength()) > n.GetStart()) ||
                    ((n.GetStart() <= newNote.GetStart()) && ((n.GetStart() + n.GetLength()) > newNote.GetStart()))) //6 possible combinations
                {
                    if (!multipleMelodyLines && !(n.GetStart() == newNote.GetStart() && n.GetLength() == newNote.GetLength()) && !(n is Rest))
                    {
                        return 6; //tells the user that multiple melody lines are turned off. will NEED to return to this for tuplets.
                    }
                    if (newNote is Rest) //handles Rest/Note, Rest/Rest, Rest/Tuplet
                    {
                        return 2;
                    }
                    else if (newNote is Note && n is Note && ((newNote as Note).GetPitch() - (newNote as Note).GetAccidental()) == (n as Note).GetPitch() - (n as Note).GetAccidental()) //if both are notes and they would take up the same space in the stave
                    {
                        return 2; //Handles Note/Note
                    }
                    else if (newNote is Tuplet) //Handles Tuplet/Note, Tuplet/Rest, Tuplet/Tuplet (most likely to fail!)
                    {
                        for (int i = 0; i < (newNote as Tuplet).GetNumberOfNotes(); i++)
                        {
                            if (n is Note && (((newNote as Tuplet).GetComponent(i) is Rest) || (((newNote as Tuplet).GetComponent(i) is Note) && ((newNote as Tuplet).GetComponent(i) as Note).GetPitch() - ((newNote as Tuplet).GetComponent(i) as Note).GetAccidental() == (n as Note).GetPitch() - (n as Note).GetAccidental()))) //checks if n is a single and if they should play at the same time
                            {
                                if (((Math.Floor((double)(newNote.GetStart() + (i * (newNote.GetLength() / (newNote as Tuplet).GetNumberOfNotes())))) <= n.GetStart()) && ((Math.Floor((double)(newNote.GetStart() + ((i + 1) * (newNote.GetLength() / (newNote as Tuplet).GetNumberOfNotes())))) >= n.GetStart()))) || ((n.GetStart() <= ((Math.Floor((double)(newNote.GetStart() + (i * (newNote.GetLength() / (newNote as Tuplet).GetNumberOfNotes())))))) && ((n.GetStart() + n.GetLength()) >= ((Math.Floor((double)(newNote.GetStart() + (i * (newNote.GetLength() / (newNote as Tuplet).GetNumberOfNotes()))))))))))
                                {
                                    return 2;
                                }
                            }
                            else //Tuplet/Tuplet - nested loop to compare all notes to all other notes. the absolute worst. longest if statement of all time
                            {
                                for (int j = 0; j < (n as Tuplet).GetNumberOfNotes(); j++)
                                {
                                    if (((n as Tuplet).GetComponent(j) is Rest) || (((n as Tuplet).GetComponent(j) is Note) && ((n as Tuplet).GetComponent(j) as Note).GetPitch() - ((n as Tuplet).GetComponent(j) as Note).GetAccidental() == (((newNote as Tuplet).GetComponent(i) as Note).GetPitch() - ((newNote as Tuplet).GetComponent(i) as Note).GetAccidental()))) //checks if n is a single and if they should play at the same time
                                    {
                                        if (((Math.Floor((double)(n.GetStart() + (j * (n.GetLength() / (n as Tuplet).GetNumberOfNotes())))) <= Math.Floor((double)(newNote.GetStart() + (i * (newNote.GetLength() / (newNote as Tuplet).GetNumberOfNotes()))))) && ((Math.Floor((double)(n.GetStart() + ((j + 1) * (n.GetLength() / (n as Tuplet).GetNumberOfNotes())))) >= Math.Floor((double)(newNote.GetStart() + (i * (newNote.GetLength() / (newNote as Tuplet).GetNumberOfNotes()))))))) || ((Math.Floor((double)(newNote.GetStart() + (i * (newNote.GetLength() / (newNote as Tuplet).GetNumberOfNotes())))) <= ((Math.Floor((double)(n.GetStart() + (j * (n.GetLength() / (n as Tuplet).GetNumberOfNotes())))))) && (Math.Floor((double)(newNote.GetStart() + ((i + 1) * (newNote.GetLength() / (newNote as Tuplet).GetNumberOfNotes())))) >= ((Math.Floor((double)(n.GetStart() + (j * (n.GetLength() / (n as Tuplet).GetNumberOfNotes()))))))))))
                                        {
                                            return 2;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (n is Tuplet) //Handles Note/Tuplet
                    {
                        for (int i = 0; i < (n as Tuplet).GetNumberOfNotes(); i++)
                        {
                            if (newNote is Note && (((n as Tuplet).GetComponent(i) is Rest) || (((n as Tuplet).GetComponent(i) is Note) &&
                                    ((n as Tuplet).GetComponent(i) as Note).GetPitch() - ((n as Tuplet).GetComponent(i) as Note).GetAccidental() ==
                                    (newNote as Note).GetPitch() - (newNote as Note).GetAccidental()))) //checks if n is a single and if they should play at the same time
                            {
                                if (((Math.Floor((double)(n.GetStart() + (i * (n.GetLength() / (n as Tuplet).GetNumberOfNotes())))) <= newNote.GetStart()) &&
                                        ((Math.Floor((double)(n.GetStart() + ((i + 1) * (n.GetLength() / (n as Tuplet).GetNumberOfNotes())))) >= n.GetStart()))) ||
                                        ((newNote.GetStart() <= ((Math.Floor((double)(n.GetStart() + (i * (n.GetLength() / (n as Tuplet).GetNumberOfNotes())))))) &&
                                        ((newNote.GetStart() + newNote.GetLength()) >= ((Math.Floor((double)(n.GetStart() + (i * (n.GetLength() / (n as Tuplet).GetNumberOfNotes()))))))))))
                                {
                                    return 2;
                                }
                            }
                        }
                    }
                }
                //the line above check if the notes overlap and are the same pitch/position on the stave/one is a rest/both are rests
            }
            return 0; //0 means the change is possible
        }

        public int FindNote(int pitch, int startPoint)
        {
            List<int[]> Matches = new List<int[]>();
            for (int i = 0; i < Notes.Count; i++)
            {
                if (Notes[i] is Tuplet)
                {
                    for (int j = 0; j < (Notes[i] as Tuplet).GetNumberOfNotes(); j++)
                    {
                        if (Math.Floor((double)(Notes[i].GetStart() + (j * Notes[i].GetLength() / (Notes[i] as Tuplet).GetNumberOfNotes()))) == startPoint)
                        {
                            TupletIndex = j;
                            if ((Notes[i] as Tuplet).GetComponent(j) is Rest)
                            {
                                return i;
                            }
                            else
                            {
                                Matches.Add(new int[] { ((Notes[i] as Tuplet).GetComponent(j) as Note).GetPitch() - ((Notes[i] as Tuplet).GetComponent(j) as Note).GetAccidental(), i, j });
                            }
                        }
                    }
                }
                else if (Notes[i] is Rest && Math.Abs(startPoint - Notes[i].GetStart()) <= 1) //if it's a rest it can't check for pitch as it will fail.
                {
                    continue;
                }
                else if (Notes[i] is Note)
                {
                    if (Math.Abs((Notes[i] as Note).GetPitch() - (Notes[i] as Note).GetAccidental() - pitch) <= 1 && startPoint == Notes[i].GetStart()) //if not rest or tuplet it must be note.
                    {
                        Matches.Add(new int[] { (Notes[i] as Note).GetPitch() - (Notes[i] as Note).GetAccidental(), i, 0 });
                    }
                }
            }
            if (Matches.Count != 0)
            {
                int comparison = 10000;
                int index = 0;
                int tempTup = 0;
                for (int i = 0; i < Matches.Count; i++)
                {
                    if (Math.Abs(Matches[i][0] - pitch) < comparison)
                    {
                        comparison = Math.Abs(Matches[i][0] - pitch);
                        index = Matches[i][1];
                        tempTup = Matches[i][2];
                    }
                }
                TupletIndex = tempTup;
                return index;
            }
            return -1; //not found
        } //Called before any operation

        public int FindNote(Symbol n)
        {
            return Notes.FindIndex(x => x == n);
        }
        #endregion

        #region modifiers
        public void DeleteNote(int index)
        {
            Notes.RemoveAt(index);
            FixSpacing(false);
        } //removes a specifed note from list. Previous must call FindNote.

        public void DeleteNote(Symbol note)
        {
            Notes.Remove(note);
        }

        public void ChangeTimeSig(int newLength) //changes the length of a bar
        {
            if (newLength < MaxLength)
            {
                if (MainWindow.GenerateYesNoDialog("Confirm action", "By shortening the bar, you will delete any notes inside it which no longer fit. Are you sure you want to do this?")) //asks the user in case of any data loss
                {
                    for (int i = 0; i < Notes.Count; i++) //loops through all notes in the bar
                    {
                        if (Notes[i].GetStart() + Notes[i].GetLength() > newLength) DeleteNote(i); //deletes any note that no longer fits in the bar.
                    }
                    MaxLength = newLength;
                }
            }
            else MaxLength = newLength; //sets the length to the new value
        }

        public void FixSpacing(bool notTheLastBar)
        {
            int noiseUntil = 0;
            int fixUntil;
            bool restRemoved;
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
            for (int i = 0; i < Notes.Count; i++)
            {
                if (Notes[i].GetStart() > noiseUntil)
                {
                    FillGap(Notes[i].GetStart(), ref noiseUntil);
                    noiseUntil += Notes[i].GetLength();
                }
                else if ((Notes[i].GetStart() + Notes[i].GetLength()) > noiseUntil)
                {
                    noiseUntil = Notes[i].GetStart() + Notes[i].GetLength();
                }
            }
            if (notTheLastBar)
            {
                fixUntil = MaxLength;
            }
            else fixUntil = Length;
            if (noiseUntil < fixUntil)
            {
                FillGap(fixUntil, ref noiseUntil);
            }
            SortNotes();
        }

        private void FillGap(int fixUntil, ref int noiseUntil)
        {
            int gapLength = fixUntil - noiseUntil;
            int restLength = 0;
            for (int j = 16; j >= 2; j /= 2)
            {
                if (noiseUntil % j == 0)
                {
                    restLength = j;
                    break;
                }
            }
            if (restLength == 0)
            {
                restLength = 1;
            }
            while (gapLength - restLength >= 0)
            {
                Notes.Add(new Rest(restLength, noiseUntil));
                gapLength -= restLength;
                noiseUntil += restLength;
                while (restLength < 16 && noiseUntil % (restLength * 2) == 0)
                {
                    restLength *= 2;
                }
            }
            if (gapLength > 0)
            {
                restLength = 16;
                do
                {
                    if (gapLength - restLength >= 0)
                    {
                        Notes.Add(new Rest(restLength, noiseUntil));
                        gapLength -= restLength;
                        noiseUntil += restLength;
                    }
                    else restLength /= 2;
                } while (gapLength > 0);
            }
        }

        public int AddNote(Symbol n) //checks a new note fits and adds it to the bar if it does.
        {
            //the first part changes the note based on the key signature of the bar.
            if (n is Note)
            {
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
            tempCheck = CheckFit(n); //temporary value to check if the note fits
            if (tempCheck == 0) //if the note fits
            {
                Notes.Add(n); //add the note to the list
                if (n.GetStart() + n.GetLength() > Length)
                {
                    Length = n.GetStart() + n.GetLength();
                }//updates the current length of the bar if necessary.
            }
            StaccatoConsistency();
            return tempCheck;
        }

        public void StaccatoConsistency()
        {
            bool stac = false;
            int start = -1;
            foreach(Symbol symbol in Notes)
            {
                if(symbol is Note)
                {
                    Note note = symbol as Note;
                    if(note.GetStart() == start && note.GetStaccato() != stac)
                    {
                        note.ToggleStaccato();
                    }
                    else if (note.GetStart() != start)
                    {
                        start = note.GetStart();
                        stac = note.GetStaccato();
                    }
                }
            }
        }

        public void ToggleTie(int noteIndex, Symbol tiedNote) //used for making a note a tie, rest or staccato. MUST CHECK IF NOTE EXISTS ABOVE.
        {
            if (noteIndex != -1)
            {
                if (Notes[noteIndex] is Note)
                {
                    (Notes[noteIndex] as Note).ToggleTie(tiedNote);
                }
                if (Notes[noteIndex] is Tuplet)
                {
                    if (TupletIndex != (Notes[noteIndex] as Tuplet).GetNumberOfNotes() - 1)
                    {
                        (Notes[noteIndex] as Tuplet).ToggleTie(TupletIndex, tiedNote);
                    }
                    else
                    {
                        (Notes[noteIndex] as Tuplet).ToggleTie(tiedNote);
                    }
                }
            }
        }

        public void ToggleTiedTo(int noteIndex, Symbol TiedTo)
        {
            if (noteIndex != -1)
            {
                if (Notes[noteIndex] is Note)
                {
                    (Notes[noteIndex] as Note).SetTiedTo(TiedTo);
                }
                if (Notes[noteIndex] is Tuplet)
                {
                    if (TupletIndex != 0)
                    {
                        (Notes[noteIndex] as Tuplet).SetTiedTo(TupletIndex, TiedTo);
                    }
                    else
                    {
                        (Notes[noteIndex] as Tuplet).SetTiedTo(TiedTo);
                    }
                }
            }
        }

        public void ToggleStaccato(int noteIndex)
        {
            if (noteIndex < Notes.Count)
            {
                if (Notes[noteIndex] is Note)
                {
                    Note note = Notes[noteIndex] as Note;
                    note.ToggleStaccato();
                    foreach (Symbol notecheck in Notes)
                    {
                        if (notecheck is Note && notecheck.GetStart() == note.GetStart() && ((notecheck as Note).GetStaccato() ^ note.GetStaccato()))
                        {
                            (notecheck as Note).ToggleStaccato();
                        }
                    }

                }
                if (Notes[noteIndex] is Tuplet)
                {
                    (Notes[noteIndex] as Tuplet).ToggleStaccato(TupletIndex);
                }
            }
        }

        public int ChangeAccidental(int noteIndex, int newAccident) //checks if the new pitch works, then uses note.setaccidental
        {
            int original = 0;
            changedSymbol = Notes[noteIndex];
            if (Notes[noteIndex] is Note)
            {
                original = (changedSymbol as Note).GetAccidental();
                (changedSymbol as Note).SetAccidental(newAccident);
            }
            else if (Notes[noteIndex] is Tuplet)
            {
                original = (changedSymbol as Tuplet).GetAccidental(TupletIndex);
                (changedSymbol as Tuplet).SetAccidental(newAccident, TupletIndex);
            }
            if (!(Notes[noteIndex] is Rest))
            {
                DeleteNote(changedSymbol);
                tempCheck = CheckFit(changedSymbol); //checks that the new note it makes fits
                if (tempCheck == 0)
                {
                    Notes.Add(changedSymbol); //uses the note method to change the accidental
                }
                else
                {
                    if (Notes[noteIndex] is Note)
                    {
                        (changedSymbol as Note).SetAccidental(original);
                    }
                    else if (Notes[noteIndex] is Tuplet)
                    {
                        (changedSymbol as Tuplet).SetAccidental(original, TupletIndex);
                    }
                    Notes.Add(changedSymbol);
                }
                return tempCheck;
            }
            else return 5; //error: is rest.
        }

        public void ChangeKeySig(int newSig) //handles changing the key signature
        {
            if (newSig <= 7 && newSig >= -7)
            {
                KeySigIndex = newSig;
            }
            else MainWindow.GenerateErrorDialog("Invalid Operation", "This is not a real key signature");
        }

        public void ChangeLength(int newLength)
        {
            MaxLength = newLength;
        }
        #endregion

        #region sorting
        public static int NoteComparison(Symbol one, Symbol two) //compares one note to another for sorting in a list.
        {
            if (one.GetStart() == two.GetStart()) //if same start, compare pitch. should only happen if there are two notes.
            {
                int pitchOne = 0;
                int pitchTwo = 0;
                if (one is Note)
                {
                    pitchOne = (one as Note).GetPitch();
                }
                if (one is Tuplet)
                {
                    pitchOne = ((one as Tuplet).GetComponent(0) as Note).GetPitch();
                }
                if (two is Note)
                {
                    pitchTwo = (two as Note).GetPitch();
                }
                if (two is Tuplet)
                {
                    pitchTwo = ((two as Tuplet).GetComponent(0) as Note).GetPitch();
                }
                if (pitchOne >= pitchTwo)
                {
                    return 1;
                }//higher pitch comes first
                else
                {
                    return -1;
                }
            }
            else if (one.GetStart() > two.GetStart())
            {
                return 1;
            }//later start == larger, larger values come later in list
            else
            {
                return -1;
            }
        }

        public void SortNotes() //sorts the notes. Essentially calls a sort function with noteComparison as the comparison
        {
            Notes.Sort(NoteComparison);
        }
        #endregion
    }
}
