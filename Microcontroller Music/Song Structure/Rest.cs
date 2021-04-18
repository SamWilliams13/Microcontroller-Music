using System.Runtime.Serialization;

namespace Microcontroller_Music
{
    [DataContract]
    //rests are used only in drawing, and serve little function except to fill space.
    public class Rest : Symbol
    {
        //makes a new rest. rests only have start and length, so not much outside symbol.
        public Rest(int length, int startpoint)
        {
            //sets values from arguments
            Length = length;
            StartPoint = startpoint;
        }

        //returns the note as string
        public override string SymbolAsText()
        {
            string restAsText = "";
            //adds on the name based on its length
            switch (Length)
            {
                case 1:
                    restAsText += "Semiquaver";
                    break;
                case 2:
                    restAsText += "Quaver";
                    break;
                case 4:
                    restAsText += "Crotchet";
                    break;
                case 8:
                    restAsText += "Minim";
                    break;
                case 16:
                    restAsText += "Semibreve";
                    break;
            }
            //it's a rest
            restAsText += " Rest";
            //return the complete string
            return restAsText;
        }
    }
}
