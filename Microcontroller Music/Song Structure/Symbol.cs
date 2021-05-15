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

        //returns a string descriptor of the note.
        public abstract string SymbolAsText();
    }
}
