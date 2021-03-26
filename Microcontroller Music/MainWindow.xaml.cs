﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.Win32;

namespace Microcontroller_Music
{
    // <summary>
    // Interaction logic for MainWindow.xaml
    // </summary>
    public partial class MainWindow : Window
    {
        public Song s;
        public Drawer drawer;
        MIDIWriter writer;
        string filePath;
        private int track = -1;
        private int bar = -1;
        private int semipos = -1;
        private int pitch = -1;
        private int noteIndex = -1;
        private int noteLength = 2;
        private bool isSelectingConnection = false;
        static Label statusLabel = new Label();
        private ContextMenu contextMenu = new ContextMenu();
        //makes a new window at launch
        public MainWindow()
        {
            //make window
            InitializeComponent();
            //save demo song to file
            //WriteFile(s, "miiPlaza");
            filePath = "";
            statusItem.Content = statusLabel;
            statusLabel.Height = 30;
        }

        //writes a song to file
        public void WriteFile(Song song, string filename)
        {
            //makes a serialiser for songs
            DataContractSerializer serializer = new DataContractSerializer(typeof(Song));
            //makes it a little more compact, adds indents for readability
            XmlWriterSettings settings = new XmlWriterSettings { Indent = true, IndentChars = "      ", NewLineOnAttributes = false };
            //makes a writer to create a file
            XmlWriter writer = XmlWriter.Create(filename, settings);
            //makes a serialised form of the song ad writes it to file
            serializer.WriteObject(writer, song);
            //closes the file
            writer.Close();
            filePath = filename;

        }

        //makes a song out of a file
        public void OpenFile(string FilePath)
        {
            s = new Song("temp", 0, 0);
            //used to read the file into the right format for the program.
            DataContractSerializer serializer = new DataContractSerializer(s.GetType());
            //to temporarily store the data for reading
            MemoryStream memoryStream = new MemoryStream();
            //makes it into a nice form to memorystream
            byte[] data = Encoding.UTF8.GetBytes(File.ReadAllText(FilePath));
            //adds the file data into a stream so you can read it with a serializer
            memoryStream.Write(data, 0, data.Length);
            //puts the stream to the start so it reads correctly in the next line
            memoryStream.Position = 0;
            //makes a song out of the XML with the default methods
            s = (Song)serializer.ReadObject(memoryStream);
            filePath = FilePath;
            writer = new MIDIWriter(s);
            drawer = new Drawer(s);
            drawer.DrawPage(ref SheetMusic, 1800);
        }

        //this allows other classes in the file to throw error boxes. 
        public static void GenerateErrorDialog(string errorTitle, string errorMessage)
        {
            //shows the given error message and title
            statusLabel.Content = errorTitle + ": " + errorMessage;
        }

        public void NewFile(string name = "", int tempo = 120, int keySig = 0, int top = 4, int bottom = 4, string track1title = "Track1", int Key = 0)
        {
            s = new Song(name, tempo, keySig, top, bottom);
            bool key = (Key == 0) ? true : false;

            s.NewTrack(track1title, keySig, top, bottom, key);
            writer = new MIDIWriter(s);
            drawer = new Drawer(s);
            drawer.DrawPage(ref SheetMusic, 1800);
            //this part needs to have dialog boxes for the user! it also needs to have other things! like anything!
        }

        //this is designed to get a boolean response from the user 
        public static bool GenerateYesNoDialog(string messageTitle, string messageContent)
        {
            //displays what I want along with yes and no buttons
            MessageBoxResult boxResult = MessageBox.Show(messageContent, messageTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
            //checks the user input and returns it to me where I want it.
            if (boxResult == MessageBoxResult.Yes)
            {
                return true;
            }
            else return false;
        }
        private void Zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SheetMusic.IsLoaded)
            {
                drawer.Zoom(ref SheetMusic, (int)Zoom.Value);
            }
        }

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Play_Button.Content.ToString() == "Play")
            {
                writer.Update(s);
                if (writer.Play())
                {
                    Play_Button.Content = "Stop";
                }
            }
            else
            {
                Play_Button.Content = "Play";
                writer.Stop();
            }
        }

        //stops the preview from continuing after the program has closed
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Play_Button.Content.ToString() == "Stop")
            {
                writer.Stop();
            }
        }
        private void SheetMusic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!contextMenu.IsOpen)
            {
                track = -1;
                bar = -1;
                semipos = -1;
                pitch = -1;
                if (!(e.GetPosition(MainScroll).Y > (int)MainScroll.Height - 20) && !(e.GetPosition(MainScroll).X > (int)MainScroll.Width - 20))
                {
                    if (drawer.FindMouseLeft(ref SheetMusic, ref track, ref bar, ref semipos, ref pitch, noteLength, e, (int)Zoom.Value))
                    {
                        s.AddNote(track, bar, new Note(noteLength, semipos, pitch));
                        drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
                    }
                    else
                    {
                    }
                }
            }
        }

        private void SheetMusic_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!contextMenu.IsOpen)
            {
                int tempTrack = -1;
                int tempBar = -1;
                int tempSemipos = -1;
                int tempPitch = -1;
                drawer.FindMouse(ref SheetMusic, ref tempTrack, ref tempBar, ref tempSemipos, ref tempPitch, noteLength, e, (int)Zoom.Value);
            }
        }

        private void createNewFile(object sender, RoutedEventArgs e)
        {
            if (Play_Button.Content.ToString() == "Stop")
            {
                writer.Stop();
                Play_Button.Content = "Play";
            }
            CreateSongPopup createSong = new CreateSongPopup();
            if (createSong.ShowDialog() == true)
            {
                int tempo = 120;
                int.TryParse(createSong.Tempo.Text, out tempo);
                NewFile(createSong.SongTitle.Text, tempo, createSong.KeySig.SelectedIndex - 7, createSong.TimeSigTop.SelectedIndex + 2,
                    (int)Math.Pow(2, createSong.TimeSigBottom.SelectedIndex + 1), createSong.TrackTitle.Text, createSong.Key.SelectedIndex);
            }
        }

        private void openExistingFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileOpen = new OpenFileDialog();
            fileOpen.Filter = "Microcontroller Music Files (*.mmf)|*.mmf";
            if (fileOpen.ShowDialog() == true)
            {
                OpenFile(fileOpen.FileName);
            }
        }

        private void saveCurrentFile(object sender, RoutedEventArgs e)
        {
            if (filePath == "")
            {
                saveCurrentFileAs(sender, e);
            }
            else
            {
                WriteFile(s, filePath);
            }
        }

        private void saveCurrentFileAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Microcontroller Music Files (*.mmf)|*.mmf";
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (saveFile.ShowDialog() == true)
            {
                WriteFile(s, saveFile.FileName);
            }
        }

        //context menu on right click?
        private void SheetMusic_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!contextMenu.IsOpen)
            {
                track = -1;
                bar = -1;
                semipos = -1;
                pitch = -1;
                drawer.FindMouseRight(ref SheetMusic, ref track, ref bar, ref semipos, ref pitch, noteLength, e, (int)Zoom.Value);
                MakeContextMenu();
            }
        }

        private void MakeContextMenu()
        {
            contextMenu.Items.Clear();
            contextMenu.PlacementTarget = SheetMusic;
            if (track != -1)
            {
                MenuItem trackMenu = new MenuItem();
                trackMenu.Header = s.GetTracks(track).GetName();
                #region delete
                MenuItem trackDeleteMenu = new MenuItem();
                trackDeleteMenu.Header = "Delete";
                trackDeleteMenu.Click += new RoutedEventHandler(TrackDeleteClick);
                trackMenu.Items.Add(trackDeleteMenu);
                #endregion
                contextMenu.Items.Add(trackMenu);
            }
            if (bar != -1)
            {
                MenuItem barMenu = new MenuItem();
                barMenu.Header = "Bar " + (bar + 1);
                #region delete
                MenuItem barDeleteMenu = new MenuItem();
                barDeleteMenu.Header = "Delete";
                barDeleteMenu.Click += new RoutedEventHandler(BarDeleteClick);
                barMenu.Items.Add(barDeleteMenu);
                #endregion
                #region time sig
                MenuItem timeSigChanger = new MenuItem();
                timeSigChanger.Header = "Time Sig: " + s.GetTimeSigs()[bar].top + "/" + s.GetTimeSigs()[bar].bottom;
                timeSigChanger.Tag = bar;
                timeSigChanger.Click += TimeSigChanger_Click;
                barMenu.Items.Add(timeSigChanger);
                #endregion
                #region key signatures
                string[] keySigNames = { "Cb Major (7b)", "Gb Major / Eb Minor (6b)" ,"Db Major / Bb Minor (5b)", "Ab Major / F Minor (4b)", "Eb Major / C Minor (3b)",
                "Bb Major / G Minor (2b)", "F Major / D Minor (1b)", "C Major / A Minor", "G Major / E Minor (1#)", "D Major / B Minor (2#)", "A Major / F# Minor (3#)",
                "E Major / C# Minor (4#)", "B Major / G# Minor (5#)", "F# Major / D# Minor (6#)", "C# Major (7#)"};
                MenuItem keySigChanger = new MenuItem();
                keySigChanger.Header = keySigNames[s.GetKeySigs(bar) + 7];
                for(int i = 0; i < keySigNames.Length; i++)
                {
                    MenuItem selectableKeySig = new MenuItem();
                    selectableKeySig.Header = keySigNames[i];
                    selectableKeySig.Tag = i - 7;
                    selectableKeySig.Click += SelectableKeySig_Click;
                    keySigChanger.Items.Add(selectableKeySig);
                }
                barMenu.Items.Add(keySigChanger);
                #endregion
                contextMenu.Items.Add(barMenu);
            }
            if (semipos != -1 && pitch != -1 && s.FindNote(track, bar, pitch, semipos) != -1)
            {
                noteIndex = s.FindNote(track, bar, pitch, semipos);
                Note note = s.GetTracks(track).GetBars(bar).GetNotes(noteIndex) as Note;
                MenuItem noteMenu = new MenuItem();
                noteMenu.Header = note.SymbolAsText();

                #region delete
                MenuItem deleteMenu = new MenuItem();
                deleteMenu.Header = "Delete";
                deleteMenu.Click += new RoutedEventHandler(DeleteClick);
                noteMenu.Items.Add(deleteMenu);
                #endregion

                #region accidental
                MenuItem acciedentalMenu = new MenuItem();
                acciedentalMenu.Header = "Accidental";
                MenuItem sharpMenu = new MenuItem() { Header = "Sharp" };
                if (note.GetAccidental() == 1) sharpMenu.IsChecked = true;
                MenuItem naturalMenu = new MenuItem() { Header = "Natural" };
                if (note.GetAccidental() == 0) naturalMenu.IsChecked = true;
                MenuItem flatMenu = new MenuItem() { Header = "Flat" };
                if (note.GetAccidental() == -1) flatMenu.IsChecked = true;
                sharpMenu.Click += AccidentalClick;
                naturalMenu.Click += AccidentalClick;
                flatMenu.Click += AccidentalClick;
                acciedentalMenu.Items.Add(sharpMenu);
                acciedentalMenu.Items.Add(naturalMenu);
                acciedentalMenu.Items.Add(flatMenu);
                noteMenu.Items.Add(acciedentalMenu);
                #endregion

                #region staccato
                MenuItem staccatoMenu = new MenuItem();
                staccatoMenu.Header = "Staccato";
                if (note.GetStaccato()) staccatoMenu.IsChecked = true;
                staccatoMenu.Click += new RoutedEventHandler(staccatoClick);
                noteMenu.Items.Add(staccatoMenu);
                #endregion

                #region tie
                List<Symbol> availableTies = s.GetNotesToTie(track, bar, noteIndex);
                if (availableTies.Count > 0)
                {
                    MenuItem tieMenu = new MenuItem();
                    tieMenu.Header = "Connect To...";
                    if (note.GetTie() != null)
                    {
                        tieMenu.IsChecked = true;
                    }
                    else
                    {
                        tieMenu.IsChecked = false;
                    }
                    foreach (Symbol tieNote in availableTies)
                    {
                        MenuItem tieOption = new MenuItem();
                        tieOption.Header = tieNote.SymbolAsText();
                        if (note.GetTie() == tieNote) tieOption.IsChecked = true;
                        tieOption.Click += TieOption_Click;
                        tieOption.Tag = tieNote;
                        tieMenu.Items.Add(tieOption);
                    }
                    noteMenu.Items.Add(tieMenu);
                }
                #endregion
                contextMenu.Items.Add(noteMenu);
            }
            contextMenu.IsOpen = true;
        }

        private void SelectableKeySig_Click(object sender, RoutedEventArgs e)
        {
            s.ChangeKeySig(Convert.ToInt32((sender as MenuItem).Tag), bar);
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void TimeSigChanger_Click(object sender, RoutedEventArgs e)
        {
            TimeSigChange timeChange = new TimeSigChange();
            if(timeChange.ShowDialog() == true)
            {
                s.ChangeBarLength((int)(sender as MenuItem).Tag, timeChange.GetTopNumber(), timeChange.GetBottomNumber());
                drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
            }
        }

        private void TieOption_Click(object sender, RoutedEventArgs e)
        {
            MenuItem producer = sender as MenuItem;
            if (producer.IsChecked)
            {
                s.RemoveConnection(track, bar, noteIndex, true);
            }
            else
            {
                s.CreateConnection(track, bar, noteIndex, producer.Tag as Symbol, true);
            }
        }

        private void TrackDeleteClick(object sender, RoutedEventArgs e)
        {
            if (!s.DeleteTrack(track))
            {
                addtrack_Click(sender, e);
                TrackDeleteClick(sender, e);
            }
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void BarDeleteClick(object sender, RoutedEventArgs e)
        {
            if (!s.DeleteBar(bar))
            {
                s.NewBar(0);
                s.DeleteBar(bar);
            }
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void staccatoClick(object sender, RoutedEventArgs e)
        {
            s.ToggleStaccato(track, bar, noteIndex);
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void AccidentalClick(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuItem).Header.ToString() == "Sharp")
            {
                s.ChangeAccidental(track, bar, noteIndex, 1);
            }
            else if ((sender as MenuItem).Header.ToString() == "Natural")
            {
                s.ChangeAccidental(track, bar, noteIndex, 0);
            }
            else if ((sender as MenuItem).Header.ToString() == "Flat")
            {
                s.ChangeAccidental(track, bar, noteIndex, -1);
            }
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            s.DeleteNote(track, bar, noteIndex);
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private string NoteToText(Note note)
        {
            string noteString = "";
            int length = note.GetLength();
            if (Math.Log(length, 2) % 1 != 0)
            {
                noteString += "Dotted ";
                length = (int)(length / 1.5);
            }
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
            int pitch = note.GetPitch() - note.GetAccidental();
            int acc = note.GetAccidental();
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
            if (octLetter != 3 && octLetter != 4 && octLetter != 8 && octLetter != 9)
            {
                switch (note.GetAccidental())
                {
                    case 1:
                        noteString += "#";
                        break;
                    case -1:
                        noteString += "b";
                        break;
                }
            }
            noteString = (octLetter >= 4) ? noteString + (((pitch - octLetter) / 12) + 1) : noteString + ((pitch - octLetter) / 12);
            return noteString;
        }

        private void addtrack_Click(object sender, RoutedEventArgs e)
        {
            CreateTrackPopup createTrack = new CreateTrackPopup();
            if (createTrack.ShowDialog() == true)
            {
                s.NewTrack(createTrack.GetTitle(), s.GetTracks(0).GetBars(0).GetKeySig(), s.GetTimeSigs()[0].top, s.GetTimeSigs()[0].bottom, createTrack.GetTreble());
            }
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void bpm_Click(object sender, RoutedEventArgs e)
        {

        }

        private void changetitle_Click(object sender, RoutedEventArgs e)
        {

        }

        private void changekeysig_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                switch ((sender as RadioButton).Content.ToString())
                {
                    case "Semiquaver":
                        noteLength = 1;
                        break;
                    case "Quaver":
                        noteLength = 2;
                        break;
                    case "Crotchet":
                        noteLength = 4;
                        break;
                    case "Minim":
                        noteLength = 8;
                        break;
                    case "Semibreve":
                        noteLength = 16;
                        break;
                    case "Dotted Quaver":
                        noteLength = 3;
                        break;
                    case "Dotted Crotchet":
                        noteLength = 6;
                        break;
                    case "Dotted Minim":
                        noteLength = 12;
                        break;
                }
            }
        }
    }
}
