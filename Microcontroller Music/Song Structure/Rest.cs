using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public override string SymbolAsText()
        {
            string restAsText = "";
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
            restAsText += " Rest";
            return restAsText;
        }
    }
}
