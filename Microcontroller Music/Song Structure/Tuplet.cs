using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Microcontroller_Music
{
    [DataContract]
    public class Tuplet : Symbol
    {
        [DataMember]
        private readonly Symbol[] Components; //a tuplet is just a collection of notes that take place over a certain length
        [DataMember]
        private readonly Symbol DefaultNote; //stores the original value of each component in the triplet
        [DataMember]
        private Symbol Tie;
        [DataMember]
        private Symbol TiedTo;

        public Tuplet(int length, int notes, int startpoint, Symbol GenericNote)
        {
            Components = new Symbol[notes];
            Length = length;
            StartPoint = startpoint;
            Tie = null;
            TiedTo = null;
            DefaultNote = GenericNote; //stored in case the user wants to change one component to a rest and then change it back
            DefaultNote.SetLength(0); //note length is negligable as is startpoint, as they are handled by the tuplet.
            for(int i = 0; i < Components.Length; i++)
            {
                Components[i] = GenericNote; //sets all components to default value.
            }
        }

        public override string SymbolAsText()
        {
            return "Tuplet";
        }

        public Symbol GetComponent(int compIndex) //most changes made to the triplet are handled by the components that make up the triplet, hence another small class.
        {
            return Components[compIndex]; //a specific note in the triplet. whatever calls this will have to find this by using a fraction of length
        }

        public void ToggleSymbolType(int index) //used to make one component of the tuplet a rest or make the rest component a note again
        {
            if(Components[index] is Rest) //if the component reports itself to be a note
            {
                if (index == Components.Length - 1) ToggleTie(null);
                Components[index] = new Rest(0, 0); //turns the chosen note into a lengthless rest.
            }
            else //if not a note then it can only be a rest, so make it a note.
            {
                Components[index] = DefaultNote; //returns the chosen rest to its default state.
            }
        }

        public void ToggleStaccato(int index) //changes value of staccato
        {
            if (Components[index] is Rest)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "This note cannot be made into a staccato as it is a rest"); //shows the user an error message if rest
            }
            else
            {
                (Components[index] as Note).ToggleStaccato();
            }
        }

        public void ToggleTie(int index, Symbol tiedNote) //changes value of tie
        {
            if (Components[index] is Rest)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "This note cannot be made into a tie as it is a rest"); //shows the user an error message if rest
            }
            else
            {
                (Components[index] as Note).ToggleTie(tiedNote);
            }
        }

        public void SetTiedTo(int index, Symbol tiedNote) //changes value of tie
        {
            if (Components[index] is Rest)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "This note cannot be made into a tie as it is a rest"); //shows the user an error message if rest
            }
            else
            {
                (Components[index] as Note).SetTiedTo(tiedNote);
            }
        }

        public void SetTiedTo(Symbol tiedNote)
        {
            if (Components[0] is Note && (Components[Components.Length - 1] as Note).GetStaccato() || Components[0] is Rest)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "The first note in this tuplet is staccato or rest and therefore a tie cannot be formed");
            }
            else
            {
                TiedTo = tiedNote;
            }
        }
        public void ToggleTie(Symbol tiedNote)
        {
            if(Components[Components.Length - 1] is Note && (Components[Components.Length - 1] as Note).GetStaccato() || Components[Components.Length-1] is Rest)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "The last note in this tuplet is staccato or rest and therefore a tie cannot be formed");
            }
            else
            {
                Tie = tiedNote;
            }
        }

        public void SetAccidental(int accidental, int index) //changes value of accidental
        {
            if (Components[index] is Rest)
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "This note is a rest and therefore cannot have an accidental"); //shows the user an error message if rest
            }
            else
            {
                (Components[index] as Note).SetAccidental(accidental);
            }
        }

        public int GetAccidental(int index)
        {
            if(Components[index] is Note)
            {
                return (Components[index] as Note).GetAccidental();
            }
            else
            {
                MainWindow.GenerateErrorDialog("Invalid Operation", "This note is a rest and therefore cannot have an accidental");
                return -2;//shows the user an error message if rest
            }
        }

        //returns the number of notes in the tuplet.
        public int GetNumberOfNotes() 
        {
            return Components.Length;
        }

        public Symbol GetTie()
        {
            return Tie;
        }

        public Symbol GetTiedTo()
        {
            return TiedTo;
        }
    }
}
