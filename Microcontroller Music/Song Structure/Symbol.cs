using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;

namespace Microcontroller_Music
{
    [DataContract (IsReference = true)]
    //defines basic methods for objects in a bar. Only sorts the length and start point.
    public abstract class Symbol 
    {
        [DataMember]
        //how long the note is in semiquavers
        protected int Length; 
        [DataMember]
        //where the note starts in the bar
        protected int StartPoint;

        //returns note length
        public int GetLength() 
        {
            return Length;
        }

        //returns start point
        public int GetStart() 
        {
            return StartPoint;
        }

        //sets the length, given that it is not too long
        public void SetLength(int length) 
        {
            //this should never be true, but is here just in case. The program does not support breves, longs or larges
            //condition stops the program from adding a note longer than a semibreve to the track
            if (Length > 16) 
            {
                //tells the user something has gone wrong
                MainWindow.GenerateErrorDialog("Internal Error", "The length of this note has been set too long to be represented"); 
            }
            else
            {
                //changes note length
                Length = length; 
            }
        }

        //sets start point (most error checking takes place in bar for this)
        public void SetStart(int Start) 
        {
            StartPoint = Start;
        }

        //returns a string descriptor of the note.
        public abstract string SymbolAsText();
    }
}
