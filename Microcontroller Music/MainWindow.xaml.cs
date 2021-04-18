using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using Microsoft.Win32;

namespace Microcontroller_Music
{
    // <summary>
    // Interaction logic for MainWindow.xaml
    // </summary>
    public partial class MainWindow : Window
    {
        //the song the user is editing/listening to/exporting
        private Song s;
        //the drawer that does all the canvas updating and tracking
        private Drawer drawer;
        //a writer for the MIDI output
        private MIDIWriter writer;
        //the array holding all microcontroller outputs - these are used identically by this class
        private readonly Writer[] microcontrollerOutput = new Writer[3];
        //the filepath where the file is being saved
        private string filePath = "";
        //the index of the track & bar the user touching
        private int track = -1;
        private int bar = -1;
        //the semiquaver position in the bar the user is closest to
        private int semipos = -1;
        //the pitch of the note that would exist where the user is touching
        private int pitch = -1;
        //the index of any note the user would be touching
        private int noteIndex = -1;
        //the length of the note type the user is currently placing
        private int noteLength = 2;
        //a label to print error messages to
        static readonly Label statusLabel = new Label();
        //a context menu that appears when you right click on the canvas
        private readonly ContextMenu contextMenu = new ContextMenu();
        //boolean to track whether the user is in state of adding a repeat and where the repeat was started
        private int repeatStart = -1;

        //makes a new window at launch
        public MainWindow()
        {
            //make window
            InitializeComponent();
            //adds the status label to the status bar at the bottom and makes it the right size.
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

        //makes a bitmap or series of bitmaps of the canvas
        public void ExportBitmap(string filename)
        {
            //used to write to a file
            FileStream fileStream = new FileStream(filename, FileMode.Create);
            //used to make the canvas into a bitmap with the correct width, height, and dpi
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)SheetMusic.Width, (int)SheetMusic.Height, 1 / 96, 1 / 96, PixelFormats.Pbgra32);
            //encodes the rendered bitmap
            BitmapEncoder bitmapEncoder = new TiffBitmapEncoder();
            //render the canvas
            renderTargetBitmap.Render(SheetMusic);
            //make a frame from the rendered canvas
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            //save the frame to the file opened in filestream
            bitmapEncoder.Save(fileStream);
            //close the file
            fileStream.Close();
            //calculates the height a page should be
            int pageHeight = drawer.GetPageHeight(SheetMusic);
            //if the single image is taller than the height then split it up
            if (pageHeight < bitmapEncoder.Frames[0].Height)
            {
                //calculate how many pages worth of content there are
                int numberOfPages = (int)Math.Ceiling(bitmapEncoder.Frames[0].Height / (double)pageHeight);
                //loop through, making an image each time
                for (int i = 0; i < numberOfPages; i++)
                {
                    //gets the name of the file the user wanted to create with .bmp
                    string theName = filename.Split('\\').Last();
                    //gets the file path without the name of the file
                    string croppedFilename = filename.Substring(0, filename.Length - theName.Length);
                    //removes .bmp from the name of the file
                    theName = theName.Substring(0, theName.Length - 4);
                    //makes a new folder with the same name as the file to store the multiple pages in
                    Directory.CreateDirectory(croppedFilename + theName);
                    //makes a new numbered file in the folder 
                    FileStream pageStream = new FileStream(croppedFilename + theName + "\\" + theName + "_" + (i + 1) + ".bmp", FileMode.Create);
                    int startY;
                    int yHeight;
                    //needed for the first page to be long enough to have the title and however many bars
                    int titleHeight = drawer.GetTitleHeight(ref SheetMusic);
                    //first page needs to have extra height for the title
                    if (i == 0)
                    {
                        startY = 0;
                        yHeight = titleHeight + pageHeight;
                    }
                    //last page has to be cut off at the end of the original to avoid an error
                    else if (i == numberOfPages - 1)
                    {
                        startY = i * pageHeight + titleHeight;
                        yHeight = (int)(bitmapEncoder.Frames[0].Height - titleHeight) % pageHeight;
                    }
                    //all other pages are the normal height and start accounting for the title
                    else
                    {
                        startY = i * pageHeight + titleHeight;
                        yHeight = pageHeight;
                    }
                    //make a new bitmap using the main bitmap and the coordinates just calculated
                    CroppedBitmap croppedBitmap = new CroppedBitmap((BitmapSource)new BitmapImage(new Uri(filename)), new Int32Rect(0, startY, 8000, yHeight));
                    //new encoder to do essentially the same thing as was done for the main file.
                    BitmapEncoder pageEncoder = new TiffBitmapEncoder();
                    pageEncoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                    pageEncoder.Save(pageStream);
                    pageStream.Close();
                }
            }
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
            //sets up all the outputs properly for the new song
            filePath = FilePath;
            SetupExports();
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

        //makes a new song and sets up all outputs
        //default values allow the program to start with a basic song in place
        public void NewFile(string name = "", int tempo = 120, int keySig = 0, int top = 4, int bottom = 4, string track1title = "Track1", int Key = 0)
        {
            //use arguments to make a song as requested
            s = new Song(name, tempo, keySig, top, bottom);
            //if key index is 0 it is treble
            bool key = (Key == 0);
            //sets up the first track as requested
            s.NewTrack(track1title, keySig, top, bottom, key);
            //sets up outputs
            SetupExports();
            writer = new MIDIWriter(s);
            drawer = new Drawer(s);
            drawer.DrawPage(ref SheetMusic, 1800);
        }

        //this is designed to get a boolean response from the user 
        public static bool GenerateYesNoDialog(string messageTitle, string messageContent)
        {
            //displays what is described in arguments with yes and no buttons
            MessageBoxResult boxResult = MessageBox.Show(messageContent, messageTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
            //checks the user input and returns it where needed.
            if (boxResult == MessageBoxResult.Yes)
            {
                return true;
            }
            else return false;
        }

        //zooms the canvas in and out when the zoom slider is moved
        private void Zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SheetMusic.IsLoaded)
            {
                drawer.Zoom(SheetMusic, (int)Zoom.Value);
            }
        }

        //when the play button is pressed, midi playback needs to begin or end
        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            //if the button says play then it needs to start the song
            if (Play_Button.Content.ToString() == "Play")
            {
                //make sure midi writer has correct info
                writer.Update(s);
                //in an if statement in case user quits out of dialog.
                //otherwise .play() displays the dialog and plays the song
                if (writer.Play())
                {
                    //change content od button to reflect new state
                    Play_Button.Content = "Stop";
                }
            }
            //if button says stop then the playback needs to stop
            else
            {
                //change the button to reflect the changed state
                Play_Button.Content = "Play";
                writer.Stop();
            }
        }

        //stops the preview from continuing after the program has closed
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if the midi player is playing then stop it from playing
            if (Play_Button.Content.ToString() == "Stop")
            {
                writer.Stop();
            }
            //ask user if they want to save before they quit. useful.
            if (GenerateYesNoDialog("Closing App", "Would you like to save?"))
            {
                SaveFile();
            }
        }

        //when the user left clicks on the canvas, a note should be placed where they click
        private void SheetMusic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //make sure that the user isn't looking at the context menu when they click
            if (!contextMenu.IsOpen)
            {
                //reset the values of the position trackers
                track = -1;
                bar = -1;
                semipos = -1;
                pitch = -1;
                //prevents a couple of errors when you click too close to edge
                if (!(e.GetPosition(MainScroll).Y > (int)MainScroll.Height - 20) && !(e.GetPosition(MainScroll).X > (int)MainScroll.Width - 20))
                {
                    //drawer finds where in the song the user has clicked, and if it represents an actual place a note can be added
                    if (drawer.FindMouseLeft(ref SheetMusic, ref track, ref bar, ref semipos, ref pitch, noteLength, e))
                    {
                        //try to add the note to the song
                        s.AddNote(track, bar, new Note(noteLength, semipos, pitch));
                        //redraw the song to reflect the changes
                        //if it might create a new bar then update the whole canvas
                        if (semipos + noteLength >= s.GetTracks(0).GetBars(bar).GetMaxLength() && bar == s.GetTotalBars() - 2)
                        {
                            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
                        }
                        //otherwise just update the bar
                        else
                        {
                            drawer.DrawBar(ref SheetMusic, bar, track);
                        }
                    }
                }
            }
        }

        //when the mouse is moved outside of a context menu then find where the user is pointing and put the preview note there
        private void SheetMusic_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!contextMenu.IsOpen)
            {
                int tempTrack = -1;
                int tempBar = -1;
                int tempSemipos = -1;
                int tempPitch = -1;
                drawer.FindMouse(ref SheetMusic, ref tempTrack, ref tempBar, ref tempSemipos, ref tempPitch, noteLength, e);
            }
        }

        //interaction for the new file button
        private void CreateNewFile(object sender, RoutedEventArgs e)
        {
            //ask user to save previous file
            if (GenerateYesNoDialog("Closing File", "Would you like to save?"))
            {
                SaveFile();
            }
            //reset filepath to prevent overwriting old file
            filePath = "";
            //stop the midi writer from playing anything, closing the clock and output
            if (Play_Button.Content.ToString() == "Stop")
            {
                writer.Stop();
                Play_Button.Content = "Play";
            }
            //open the dialog to ask user to enter the required info
            CreateSongPopup createSong = new CreateSongPopup();
            //if they enter evrything properly
            if (createSong.ShowDialog() == true)
            {
                //make sure the tempo entered is an integer
                int.TryParse(createSong.Tempo.Text, out int tempo);
                //make a new file with the given info. key sig selected index can be converted to an integer key sig, same for timesig. check their respective dialogs to see how it is done.
                NewFile(createSong.SongTitle.Text, tempo, createSong.KeySig.SelectedIndex - 7, createSong.TimeSigTop.SelectedIndex + 2,
                    (int)Math.Pow(2, createSong.TimeSigBottom.SelectedIndex + 1), createSong.TrackTitle.Text, createSong.Key.SelectedIndex);
                //update the bpm button to reflect the new bpm
                Bpm.Header = "BPM: " + s.GetBPM();
            }
        }

        //interaction for the open button
        private void OpenExistingFile(object sender, RoutedEventArgs e)
        {
            //ask user to save previous file
            if (GenerateYesNoDialog("Closing File", "Would you like to save?"))
            {
                SaveFile();
            }
            filePath = "";
            //lets user find an mmf file in their documents
            OpenFileDialog fileOpen = new OpenFileDialog
            {
                Filter = "Microcontroller Music Files (*.mmf)|*.mmf"
            };
            //if they select one then open the file and update bpm button to reflect new bpm
            if (fileOpen.ShowDialog() == true)
            {
                if (Play_Button.Content.ToString() == "Stop")
                {
                    writer.Stop();
                    Play_Button.Content = "Play";
                }
                OpenFile(fileOpen.FileName);
                Bpm.Header = "BPM: " + s.GetBPM();
            }
        }

        //event handler for the save button
        //actual process removed so it can be called at other points in program.
        private void SaveCurrentFile(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        //event handler for the save as button
        private void SaveCurrentFileAs(object sender, RoutedEventArgs e)
        {
            SaveInNewFilePath();
        }

        //saves the file to the filepath.
        private void SaveFile()
        {
            //if there is no file path then prompt the user to find one
            if (filePath == "")
            {
                SaveInNewFilePath();
            }
            else
            {
                WriteFile(s, filePath);
            }
        }

        //allows user to save to a new file
        private void SaveInNewFilePath()
        {
            //open dialog for user to find file
            SaveFileDialog saveFile = new SaveFileDialog
            {
                Filter = "Microcontroller Music Files (*.mmf)|*.mmf"
            };
            //if they do make a file then save to that file
            if (saveFile.ShowDialog() == true)
            {
                WriteFile(s, saveFile.FileName);
            }
        }

        //context menu on right click. find where the mouse is then use that info to make a context menu with options on it
        private void SheetMusic_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!contextMenu.IsOpen)
            {
                track = -1;
                bar = -1;
                semipos = -1;
                pitch = -1;
                drawer.FindMouseLeft(ref SheetMusic, ref track, ref bar, ref semipos, ref pitch, noteLength, e);
                MakeContextMenu();
            }
        }

        //makes the context menu based on what has been clicked on
        private void MakeContextMenu()
        {
            //delete everything that was in the menu
            contextMenu.Items.Clear();
            //make sure the context menu is set to appear on top of canvas
            contextMenu.PlacementTarget = SheetMusic;
            //if the mouse was over a track, give options to do things to the track
            if (track != -1)
            {
                contextMenu.Items.Add(MakeTrackContextMenu());
            }
            //if the mouse was over a bar, give options to change the bar
            if (bar != -1)
            {
                contextMenu.Items.Add(MakeBarContextMenu());
            }
            //if there is a note under the mouse, give some options to edit the note
            if (semipos != -1 && pitch != -1 && s.FindNote(track, bar, pitch, semipos) != -1)
            {
                contextMenu.Items.Add(MakeNoteContextMenu());
            }
            //show menu
            contextMenu.IsOpen = true;
        }

        //makes the track options in the context menu, each with their own click methods
        public MenuItem MakeTrackContextMenu()
        {
            MenuItem trackMenu = new MenuItem
            {
                Header = s.GetTrackTitle(track)
            };

            //delete button
            MenuItem trackDeleteMenu = new MenuItem
            {
                Header = "Delete"
            };
            trackDeleteMenu.Click += new RoutedEventHandler(TrackDeleteClick);
            trackMenu.Items.Add(trackDeleteMenu);

            //if they ckicked on a bar they can use track to add a new bar after that one
            if (bar != -1)
            {
                MenuItem insertBarMenu = new MenuItem
                {
                    Header = "Insert Bar",
                    Tag = bar
                };
                insertBarMenu.Click += InsertBarMenu_Click;
                trackMenu.Items.Add(insertBarMenu);
            }
            return trackMenu;
        }

        //makes the bar options in context menu, each with click methods
        public MenuItem MakeBarContextMenu()
        {
            //shows the number of the bar in human counting numbers
            MenuItem barMenu = new MenuItem
            {
                Header = "Bar " + (bar + 1)
            };

            //delete - removes the bar from the song
            MenuItem barDeleteMenu = new MenuItem
            {
                Header = "Delete"
            };
            barDeleteMenu.Click += new RoutedEventHandler(BarDeleteClick);
            barMenu.Items.Add(barDeleteMenu);

            //change time signature
            MenuItem timeSigChanger = new MenuItem
            {
                Header = "Time Sig: " + s.GetTimeSigs(bar).top + "/" + s.GetTimeSigs(bar).bottom,
                Tag = bar
            };
            timeSigChanger.Click += TimeSigChanger_Click;
            barMenu.Items.Add(timeSigChanger);

            //change key signature, displays all possible signatures so you can just click on the one you want
            string[] keySigNames = { "Cb Major (7b)", "Gb Major / Eb Minor (6b)" ,"Db Major / Bb Minor (5b)", "Ab Major / F Minor (4b)", "Eb Major / C Minor (3b)",
                "Bb Major / G Minor (2b)", "F Major / D Minor (1b)", "C Major / A Minor", "G Major / E Minor (1#)", "D Major / B Minor (2#)", "A Major / F# Minor (3#)",
                "E Major / C# Minor (4#)", "B Major / G# Minor (5#)", "F# Major / D# Minor (6#)", "C# Major (7#)"};
            MenuItem keySigChanger = new MenuItem
            {
                Header = keySigNames[s.GetKeySigs(bar) + 7]
            };
            //menu with 15 key signatures that are available, the one that the bar is will be checked.
            for (int i = 0; i < keySigNames.Length; i++)
            {
                MenuItem selectableKeySig = new MenuItem
                {
                    Header = keySigNames[i],
                    Tag = i - 7
                };
                selectableKeySig.Click += SelectableKeySig_Click;
                keySigChanger.Items.Add(selectableKeySig);
            }
            barMenu.Items.Add(keySigChanger);
            //this one is for adding/removing repeats
            MenuItem repeatHandler = new MenuItem();
            int canIt = s.CanARepeatGoHere(bar);
            //this means that there is no repeat present here
            if (canIt == -1)
            {
                repeatHandler.Tag = bar;
                //the repeat selection process is just beginning...
                if (repeatStart == -1)
                {
                    repeatHandler.Header = "Start Repeat";
                    repeatHandler.Click += new RoutedEventHandler(StartRepeat_Click);
                }
                //the repeat selection process has already begun
                else
                {
                    repeatHandler.Header = "End Repeat";
                    repeatHandler.Click += new RoutedEventHandler(EndRepeat_Click);
                }
            }
            //include the repeat index as tag so it can be removed from the song when clicked.
            else
            {
                repeatHandler.Tag = canIt;
                repeatHandler.Header = "Delete Repeat";
                repeatHandler.Click += new RoutedEventHandler(DeleteRepeat_Click);
            }
            barMenu.Items.Add(repeatHandler);
            return barMenu;
        }

        //makes the menu with all the note options, each item with its own click method
        public MenuItem MakeNoteContextMenu()
        {
            //must find the note first to give info
            noteIndex = s.FindNote(track, bar, pitch, semipos);
            Note note = s.GetTracks(track).GetBars(bar).GetNotes(noteIndex) as Note;
            MenuItem noteMenu = new MenuItem
            {
                //get the note description from the note class 
                Header = note.SymbolAsText()
            };

            //delete the note
            MenuItem deleteMenu = new MenuItem
            {
                Header = "Delete"
            };
            deleteMenu.Click += new RoutedEventHandler(DeleteClick);
            noteMenu.Items.Add(deleteMenu);

            //change the note accidental; the one that it is is already checked
            MenuItem acciedentalMenu = new MenuItem
            {
                Header = "Accidental"
            };
            //submenu items
            MenuItem sharpMenu = new MenuItem() { Header = "Sharp" };
            if (note.GetAccidental() == 1) sharpMenu.IsChecked = true;
            MenuItem naturalMenu = new MenuItem() { Header = "Natural" };
            if (note.GetAccidental() == 0) naturalMenu.IsChecked = true;
            MenuItem flatMenu = new MenuItem() { Header = "Flat" };
            if (note.GetAccidental() == -1) flatMenu.IsChecked = true;
            //add methods
            sharpMenu.Click += AccidentalClick;
            naturalMenu.Click += AccidentalClick;
            flatMenu.Click += AccidentalClick;
            //add to higher menus
            acciedentalMenu.Items.Add(sharpMenu);
            acciedentalMenu.Items.Add(naturalMenu);
            acciedentalMenu.Items.Add(flatMenu);
            noteMenu.Items.Add(acciedentalMenu);


            //staccato - checked if true, click to toggle
            MenuItem staccatoMenu = new MenuItem
            {
                Header = "Staccato"
            };
            if (note.GetStaccato()) staccatoMenu.IsChecked = true;
            staccatoMenu.Click += new RoutedEventHandler(StaccatoClick);
            noteMenu.Items.Add(staccatoMenu);


            //connection
            //grab possible connections from the track
            List<Symbol> availableTies = s.GetNotesToTie(track, bar, noteIndex);
            //if there are possible connections then show this menu
            if (availableTies.Count > 0)
            {
                //new connect to option, checked if there is a forward connection
                MenuItem tieMenu = new MenuItem
                {
                    Header = "Connect To..."
                };
                if (note.GetTie() != null)
                {
                    tieMenu.IsChecked = true;
                }
                else
                {
                    tieMenu.IsChecked = false;
                }
                //add submenu item for all notes to connect
                foreach (Symbol tieNote in availableTies)
                {
                    MenuItem tieOption = new MenuItem
                    {
                        Header = tieNote.SymbolAsText()
                    };
                    //if the note is already connected then check it.
                    if (note.GetTie() == tieNote) tieOption.IsChecked = true;
                    //add method
                    tieOption.Click += TieOption_Click;
                    //put the note in tag so it can be used in the click method
                    tieOption.Tag = tieNote;
                    //add to higher menus
                    tieMenu.Items.Add(tieOption);
                }
                noteMenu.Items.Add(tieMenu);
            }
            return noteMenu;

        }

        //menu click - updates the state of repeatStart to hold the start of the note being created
        private void StartRepeat_Click(object sender, RoutedEventArgs e)
        {
            repeatStart = Convert.ToInt32((sender as MenuItem).Tag);
            //if statement to prevent repeats splitting connections.
            if (s.ZeroNoteIsTied(track, bar))
            {
                GenerateErrorDialog("Error", "A repeat cannot be started here as it splits a connection.");
                repeatStart = -1;
            }
            else
            {
                GenerateErrorDialog("Adding repeat", "starting at bar " + (repeatStart + 1));
            }
        }

        //menu click - adds a new repeat to the song using repeatStart and the bar just accessed
        private void EndRepeat_Click(object sender, RoutedEventArgs e)
        {
            //get the bar index from tags
            int repeatEnd = Convert.ToInt32((sender as MenuItem).Tag);
            if (repeatEnd < s.GetTotalBars() - 1 && s.ZeroNoteIsTied(track, bar + 1))
            {
                GenerateErrorDialog("Error", "A repeat cannot be started here as it splits a connection.");
            }
            else
            {
                //get the number of repeats from dialog
                AddRepeat addRepeat = new AddRepeat();
                if (addRepeat.ShowDialog() == true)
                {
                    s.AddRepeat(repeatStart, repeatEnd, addRepeat.GetRepeatCount());
                    repeatStart = -1;
                    drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
                }
            }
        }

        //menu click - removes the repeat that encapsulates the bar clicked on
        private void DeleteRepeat_Click(object sender, RoutedEventArgs e)
        {
            int repeatIndex = Convert.ToInt32((sender as MenuItem).Tag);
            s.RemoveRepeat(repeatIndex);
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        //menu click - calls insert bar at to add a new bar after the one selected - updates the canvas to reflect change
        private void InsertBarMenu_Click(object sender, RoutedEventArgs e)
        {
            s.InsertBarAt(Convert.ToInt32((sender as MenuItem).Tag));
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        //menu click - changes the key sig of the selected bar to the one that was clicked on by user - updates canvas to reflect change
        private void SelectableKeySig_Click(object sender, RoutedEventArgs e)
        {
            s.ChangeKeySig(Convert.ToInt32((sender as MenuItem).Tag), bar);
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        //menu click - changes the time sig of the selected bar
        private void TimeSigChanger_Click(object sender, RoutedEventArgs e)
        {
            //open a dialog to get a new time sig
            TimeSigChange timeChange = new TimeSigChange();
            if (timeChange.ShowDialog() == true)
            {
                //change the time sig of the selected bar
                s.ChangeBarLength((int)(sender as MenuItem).Tag, timeChange.GetTopNumber(), timeChange.GetBottomNumber());
                //update canvas to reflect change
                drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
            }
        }

        //menu click - connects the selected note to the one clicked on in the context menu
        private void TieOption_Click(object sender, RoutedEventArgs e)
        {
            //gets the menu item that called it so you can access tags
            MenuItem producer = sender as MenuItem;
            //ischecked means it was already connected, so the connection needs to be removed - tag not needed as no new connection made
            if (producer.IsChecked)
            {
                s.RemoveConnection(track, bar, noteIndex, true);
            }
            //not checked means a new connection is being created - the tag contains the symbol to connect to. 
            else
            {
                s.CreateConnection(track, bar, noteIndex, producer.Tag as Symbol, true);
            }
            //update canvas to reflect the change
            drawer.DrawBar(ref SheetMusic, bar, track);
        }

        //menu click - deletes the selected track
        private void TrackDeleteClick(object sender, RoutedEventArgs e)
        {
            //possibly catastrophic if done on accident, so ask the user if they are sure
            if (GenerateYesNoDialog("Confirm Action", "Are you sure you want to delete this track?"))
            {
                //if it fails that means a new track must be created first so that canvas drawing doesn't crash
                if (!s.DeleteTrack(track))
                {
                    //send the user to the same method called by add track in the song menu
                    AddTrack_Click(sender, e);
                    //make the track look clear to user by deleting all but 1 bar
                    if(s.GetTotalBars() > 1)
                    {
                        for(int i = 0; i < s.GetTotalBars(); i++)
                        {
                            s.DeleteBar(1);
                        }
                    }
                    //if only one track left, all tracks must be removed
                    s.ClearRepeats();
                    //then you can remove the track from the song
                    s.DeleteTrack(track);
                }
                //update canvas to reflect change
                drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
            }
        }

        //menu click - deletes the selected bar
        private void BarDeleteClick(object sender, RoutedEventArgs e)
        {
            //problematic if done by accident - ask user if sure.
            if (GenerateYesNoDialog("Confirm Action", "Are you sure you want to delete this bar?"))
            {
                //method will fail if it is the only bar - so add a bar and try again
                if (!s.DeleteBar(bar))
                {
                    s.NewBar(0);
                    s.DeleteBar(bar);
                }
                //update canvas to reflect change
                drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
            }
        }

        //menu click - toggles staccato on a note
        private void StaccatoClick(object sender, RoutedEventArgs e)
        {
            s.ToggleStaccato(track, bar, noteIndex);
            //update canvas to reflect change
            drawer.DrawBar(ref SheetMusic, track, bar);
        }

        //menu click - changes the accidental of the selected note
        private void AccidentalClick(object sender, RoutedEventArgs e)
        {
            //uses the header of the menu item selected to change the accidental to the correct value
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
            //update canvas to reflect the changes
            drawer.DrawBar(ref SheetMusic, bar, track);
        }

        //menu click - deletes the selected note and updates the canvas to reflect the change
        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            bool redrawPrevious = s.ZeroNoteIsTied(track, bar);
            s.DeleteNote(track, bar, noteIndex);
            if(redrawPrevious)
            {
                drawer.DrawBar(ref SheetMusic, bar - 1, track);
            }
            drawer.DrawBar(ref SheetMusic, bar, track);
        }

        //menu click - adds a track to the song
        private void AddTrack_Click(object sender, RoutedEventArgs e)
        {
            //opens a dialog box to ask for a title and key
            CreateTrackPopup createTrack = new CreateTrackPopup();
            if (createTrack.ShowDialog() == true)
            {
                //makes the new track using info from the list of time sigs and key sigs, and the dialog box
                s.NewTrack(createTrack.GetTitle(), s.GetKeySigs(0), s.GetTimeSigs(0).top, s.GetTimeSigs(0).bottom, createTrack.GetTreble());
            }
            //update canvas to reflect change
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        //change bpm - accessed from song menu
        private void Bpm_Click(object sender, RoutedEventArgs e)
        {
            //opens a dialog box with text box to get new bpm
            BPMChange newBPMDialog = new BPMChange();
            if (newBPMDialog.ShowDialog() == true)
            {
                int newBPM = s.GetBPM();
                //get the bpm from the dialog box - fails if not an integer
                if (newBPMDialog.GetNewBPM(ref newBPM))
                {
                    //if the bpm is not within range, call an error
                    if (newBPM < 30 || newBPM > 300)
                    {
                        GenerateErrorDialog("Error", "Tempo out of accepted range (30-300)");
                    }
                    //if in range, update bpm and the menu item that shows it
                    else
                    {
                        s.SetBPM(newBPM);
                        Bpm.Header = "BPM: " + newBPM;
                        drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
                        //update the midi player
                        Play_Button.Content = "Play";
                        writer.UpdateBPM();
                    }
                }
                //if it isn't a number, call an error
                else
                {
                    GenerateErrorDialog("Error", "Not a number.");
                }
            }
        }

        //accessed from song menu - changes the title of the song
        private void ChangeTitle_Click(object sender, RoutedEventArgs e)
        {
            //opens a dialog with a text box to enter a new title
            TitleChange newTitleDialog = new TitleChange();
            if (newTitleDialog.ShowDialog() == true)
            {
                //changes the title and updates the canvas to reflect the change
                s.SetTitle(newTitleDialog.GetNewTItle());
                drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
            }
        }

        //handles the note selectors on the right side of the screen.
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        { 
            //sender is null on startup - so this prevents it crashing before the window is rendered
            if (sender != null)
            {
                //use the tags in the radio buttons to decide which note has been selected and therefore the length of the note that will be added
                switch ((sender as RadioButton).Tag.ToString())
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

        //accessed from export menu
        private void BitmapExport_Click(object sender, RoutedEventArgs e)
        {
            //removes the preview note from the image
            drawer.MakePreviewInvisible();
            //makes the user find a place to save the file
            SaveFileDialog saveFile = new SaveFileDialog
            {
                Filter = "Bitmap Image (*.bmp)|*.bmp"
            };
            if (saveFile.ShowDialog() == true)
            {
                //this uses invoke so that the canvas can be zoomed in before it is converted to bitmap
                Action<string> bmp = ExportBitmap;
                //zoom in so the image is higher quality
                drawer.Zoom(SheetMusic, 8000);
                //wait until that is done to save the bitmap
                this.Dispatcher.Invoke(bmp, System.Windows.Threading.DispatcherPriority.Loaded, saveFile.FileName);
                //zoom it out back to where it was
                drawer.Zoom(SheetMusic, (int)Zoom.Value);
            }
        }

        //writes the code for a microcontroller defined by the tag of the menu item chosen
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            microcontrollerOutput[Convert.ToInt32((sender as MenuItem).Tag)].Write();
        }

        //sets up all the microcontroller writers stored in the array
        public void SetupExports()
        {
            microcontrollerOutput[0] = new BBCMicroPythonWriter(s);
            microcontrollerOutput[1] = new ArduinoWriter(s);
            microcontrollerOutput[2] = new PicoWriter(s);
        }
    }
}
