using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microcontroller_Music;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;

namespace Microcontroller_Music
{
    [DataContract]
    public class Note : Symbol
    {
        [DataMember]
        //integer distance of the note from A0
        private int Pitch;
        [DataMember]
        //whether the note is staccato
        private bool Staccato;
        [DataMember]
        //whether the note is tied to the next
        private Symbol Tie;
        [DataMember]
        //whether the note is tied to the previous
        private Symbol TiedTo;
        [DataMember]
        //can be 0, -1 or 1 based on natural, flat or sharp
        private int Accidental;

        //note constructor. not likely to be used often as only one note ever really exists
        public Note(int length = 2, int Start = 0, int pitch = 0, int accidental = 0)
        {
            //sets to desired length
            Length = length;
            Pitch = pitch;
            StartPoint = Start;
            Staccato = false;
            Tie = null;
            TiedTo = null;
            //0 for natural, 1 for sharp, -1 for flat
            Accidental = accidental;
        }

        #region getters
        //gets integer semitone distance from a0
        public int GetPitch()
        {
            return Pitch;
        }

        //gets staccato
        public bool GetStaccato()
        {
            return Staccato;
        }

        //checks if start of tie or slur
        public Symbol GetTie()
        {
            return Tie;
        }

        //checks if end of tie/slur
        public Symbol GetTiedTo()
        {
            return TiedTo;
        }

        //gives the accidental of the note
        public int GetAccidental()
        {
            return Accidental;
        }

        //returns the note as text in a similar fashion to what is needed in menus
        public override string SymbolAsText()
        {
            string noteString = "";
            int length = Length;
            //if the note isn't a multiple of 2 then it must be dotted
            if (Math.Log(length, 2) % 1 != 0)
            {
                noteString += "Dotted ";
                length = (int)(length / 1.5);
            }
            //get the name of the note from its undotted length
            switch (length)
            {
                case 1:
                    noteString += "Semiquaver ";
                    break;
                case 2:
                    noteString += "Quaver ";
                    break;
                case 4:
                    noteString += "Crotchet ";
                    break;
                case 8:
                    noteString += "Minim ";
                    break;
                case 16:
                    noteString += "Semibreve ";
                    break;
            }
            //remove the accidental from the pitch to find its letter value
            //also handle a few cases where flats or sharps don't exist for the note
            int pitch = Pitch - Accidental;
            int acc = Accidental;
            int octLetter = pitch % 12;
            switch (octLetter)
            {
                case 1:
                    noteString += "A";
                    break;
                case 3:
                    switch (acc)
                    {
                        case 1:
                            noteString += "C";
                            break;
                        case 0:
                            noteString += "B";
                            break;
                        case -1:
                            noteString += "Bb";
                            break;
                    }
                    break;
                case 4:
                    switch (acc)
                    {
                        case 1:
                            noteString += "C#";
                            break;
                        case 0:
                            noteString += "C";
                            break;
                        case -1:
                            noteString += "B";
                            break;
                    }
                    break;
                case 6:
                    noteString += "D";
                    break;
                case 8:
                    switch (acc)
                    {
                        case 1:
                            noteString += "F";
                            break;
                        case 0:
                            noteString += "E";
                            break;
                        case -1:
                            noteString += "Eb";
                            break;
                    }
                    break;
                case 9:
                    switch (acc)
                    {
                        case 1:
                            noteString += "F#";
                            break;
                        case 0:
                            noteString += "F";
                            break;
                        case -1:
                            noteString += "E";
                            break;
                    }
                    break;
                case 11:
                    noteString += "G";
                    break;
            }
            //if the accidental isn't already handled then add it on here
            if (octLetter != 3 && octLetter != 4 && octLetter != 8 && octLetter != 9)
            {
                switch (Accidental)
                {
                    case 1:
                        noteString += "#";
                        break;
                    case -1:
                        noteString += "b";
                        break;
                }
            }
            //then calculate which octave the note is in.
            int temporaryValue = ((Pitch - Pitch % 12) / 12);
            if (Pitch % 12 > 3) temporaryValue++;
            noteString += temporaryValue;
            return noteString;
        }
        #endregion

        #region setters
        //changes value of staccato to whatever it isn't
        public void ToggleStaccato()
        {
            //if the note leads directly into another one it cannot be a staccato
            if (Tie != null)
            {
                //so send the user an error message and do not go ahead.
                MainWindow.GenerateErrorDialog("Invalid Operation", "This note cannot be made into a staccato as it is tied to the next one"); //shows the user an error message if tie
            }
            else
            {
                //sets the staccato to the opposite value
                Staccato = !Staccato;
            }
        }

        //changes value of the note it is leads into
        public void ToggleTie(Symbol tiedNote)
        {
            if (Staccato)
            {
                //shows the user an error message if staccato, as a staccato note cannot lead into another note.
                MainWindow.GenerateErrorDialog("Invalid Operation", "This note cannot be made into a tie as it is a staccato");
            }
            else
            {
                //sets the tie to whatever the argument is
                Tie = tiedNote;
            }
        }

        //sets a new note to be tied to. validation performed higher up.
        public void SetTiedTo(Symbol noteTiedTo)
        {
            //performs a basic setter function
            TiedTo = noteTiedTo;
        }

        //changes value of accidental
        public void SetAccidental(int accidental)
        {
            //gets the semtone difference between the current accidental and the new one, and adds it to the pitch
            Pitch += (accidental - Accidental);
            //replaces the old accidental with the new one 
            Accidental = accidental;
        }

        //changes the pitch of the note. for intended use, this shouldn't cause any errors but may need to be returned to
        public void SetPitch(int pitch)
        {
            Pitch = pitch;
        }
        #endregion
    }
}
