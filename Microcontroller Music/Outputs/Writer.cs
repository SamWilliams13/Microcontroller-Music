using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microcontroller_Music
{
    //abstract parent class for all creators of outputs
    public abstract class Writer
    {
        //a song to convert
        protected Song songToConvert;

        //constructor
        protected Writer(Song s)
        {
            //sets the song to convert to the argument
            songToConvert = s;
        }

        //collects information required for the song to be made
        public abstract bool GetDetails();

        //carries out the process to make a form of output
        public abstract void Write();
    }
}
