using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        static readonly Label statusLabel = new Label();
        private readonly ContextMenu contextMenu = new ContextMenu();

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

        public void ExportBitmap(string filename)
        {
            FileStream fileStream = new FileStream(filename, FileMode.Create);
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)SheetMusic.Width, (int)SheetMusic.Height, 1 / 96, 1 / 96, PixelFormats.Pbgra32);
            BitmapEncoder bitmapEncoder = new TiffBitmapEncoder();
            renderTargetBitmap.Render(SheetMusic);
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            bitmapEncoder.Save(fileStream);
            fileStream.Close();
            //uses the ratio of width to height to decide if the page should be split.
            int pageHeight = drawer.GetPageHeight(SheetMusic);
            if (pageHeight < bitmapEncoder.Frames[0].Height)
            {
                int numberOfPages = (int)Math.Ceiling(bitmapEncoder.Frames[0].Height / (double)pageHeight);
                for (int i = 0; i < numberOfPages; i++)
                {
                    string theName = filename.Split('\\').Last();
                    string croppedFilename = filename.Substring(0, filename.Length - theName.Length);
                    theName = theName.Substring(0, theName.Length - 4);
                    Directory.CreateDirectory(croppedFilename + theName);
                    FileStream pageStream = new FileStream(croppedFilename + theName + "\\" + theName + "_" + (i + 1) + ".bmp", FileMode.Create);
                    int startY;
                    int yHeight;
                    int titleHeight = drawer.GetTitleHeight(ref SheetMusic);
                    if (i == 0)
                    {
                        startY = 0;
                        yHeight = titleHeight + pageHeight;
                    }
                    else if (i == numberOfPages - 1)
                    {
                        startY = i * pageHeight + titleHeight;
                        yHeight = (int)(bitmapEncoder.Frames[0].Height - titleHeight) % pageHeight;
                    }
                    else
                    {
                        startY = i * pageHeight + titleHeight;
                        yHeight = pageHeight;
                    }
                    CroppedBitmap croppedBitmap = new CroppedBitmap((BitmapSource)new BitmapImage(new Uri(filename)), new Int32Rect(0, startY, 8000, yHeight));
                    BitmapEncoder pageEncoder = new TiffBitmapEncoder();
                    pageEncoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                    pageEncoder.Save(pageStream);
                    pageStream.Close();
                }
            }
            drawer.Zoom(SheetMusic, (int)Zoom.Value);
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
            bool key = (Key == 0);
            s.NewTrack(track1title, keySig, top, bottom, key);
            writer = new MIDIWriter(s);
            drawer = new Drawer(s);
            drawer.DrawPage(ref SheetMusic, 1800);
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
                drawer.Zoom(SheetMusic, (int)Zoom.Value);
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
            if (GenerateYesNoDialog("Closing App", "Would you like to save?"))
            {
                SaveFile();
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
                    if (drawer.FindMouseLeft(ref SheetMusic, ref track, ref bar, ref semipos, ref pitch, noteLength, e))
                    {
                        s.AddNote(track, bar, new Note(noteLength, semipos, pitch));
                        drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
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
                drawer.FindMouse(ref SheetMusic, ref tempTrack, ref tempBar, ref tempSemipos, ref tempPitch, noteLength, e);
            }
        }

        private void CreateNewFile(object sender, RoutedEventArgs e)
        {
            if (GenerateYesNoDialog("Closing File", "Would you like to save?"))
            {
                SaveFile();
            }
            filePath = "";
            if (Play_Button.Content.ToString() == "Stop")
            {
                writer.Stop();
                Play_Button.Content = "Play";
            }
            CreateSongPopup createSong = new CreateSongPopup();
            if (createSong.ShowDialog() == true)
            {
                int.TryParse(createSong.Tempo.Text, out int tempo);
                NewFile(createSong.SongTitle.Text, tempo, createSong.KeySig.SelectedIndex - 7, createSong.TimeSigTop.SelectedIndex + 2,
                    (int)Math.Pow(2, createSong.TimeSigBottom.SelectedIndex + 1), createSong.TrackTitle.Text, createSong.Key.SelectedIndex);
                Bpm.Header = "BPM: " + s.GetBPM();
            }
        }

        private void OpenExistingFile(object sender, RoutedEventArgs e)
        {
            if (GenerateYesNoDialog("Closing File", "Would you like to save?"))
            {
                SaveFile();
            }
            filePath = "";
            OpenFileDialog fileOpen = new OpenFileDialog
            {
                Filter = "Microcontroller Music Files (*.mmf)|*.mmf"
            };
            if (fileOpen.ShowDialog() == true)
            {
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

        private void SaveFile()
        {
            if (filePath == "")
            {
                SaveInNewFilePath();
            }
            else
            {
                WriteFile(s, filePath);
            }
        }

        private void SaveInNewFilePath()
        {
            SaveFileDialog saveFile = new SaveFileDialog
            {
                Filter = "Microcontroller Music Files (*.mmf)|*.mmf"
            };
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
                drawer.FindMouseRight(ref SheetMusic, ref track, ref bar, ref semipos, ref pitch, noteLength, e);
                MakeContextMenu();
            }
        }

        private void MakeContextMenu()
        {
            contextMenu.Items.Clear();
            contextMenu.PlacementTarget = SheetMusic;
            if (track != -1)
            {
                MenuItem trackMenu = new MenuItem
                {
                    Header = s.GetTrackTitle(track)
                };
                #region delete
                MenuItem trackDeleteMenu = new MenuItem
                {
                    Header = "Delete"
                };
                trackDeleteMenu.Click += new RoutedEventHandler(TrackDeleteClick);
                trackMenu.Items.Add(trackDeleteMenu);
                #endregion
                #region insert bar
                MenuItem insertBarMenu = new MenuItem
                {
                    Header = "Insert Bar",
                    Tag = bar
                };
                insertBarMenu.Click += InsertBarMenu_Click;
                trackMenu.Items.Add(insertBarMenu);
                #endregion
                contextMenu.Items.Add(trackMenu);
            }
            if (bar != -1)
            {
                MenuItem barMenu = new MenuItem
                {
                    Header = "Bar " + (bar + 1)
                };
                #region delete
                MenuItem barDeleteMenu = new MenuItem
                {
                    Header = "Delete"
                };
                barDeleteMenu.Click += new RoutedEventHandler(BarDeleteClick);
                barMenu.Items.Add(barDeleteMenu);
                #endregion
                #region time sig
                MenuItem timeSigChanger = new MenuItem
                {
                    Header = "Time Sig: " + s.GetTimeSigs(bar).top + "/" + s.GetTimeSigs(bar).bottom,
                    Tag = bar
                };
                timeSigChanger.Click += TimeSigChanger_Click;
                barMenu.Items.Add(timeSigChanger);
                #endregion
                #region key signatures
                string[] keySigNames = { "Cb Major (7b)", "Gb Major / Eb Minor (6b)" ,"Db Major / Bb Minor (5b)", "Ab Major / F Minor (4b)", "Eb Major / C Minor (3b)",
                "Bb Major / G Minor (2b)", "F Major / D Minor (1b)", "C Major / A Minor", "G Major / E Minor (1#)", "D Major / B Minor (2#)", "A Major / F# Minor (3#)",
                "E Major / C# Minor (4#)", "B Major / G# Minor (5#)", "F# Major / D# Minor (6#)", "C# Major (7#)"};
                MenuItem keySigChanger = new MenuItem
                {
                    Header = keySigNames[s.GetKeySigs(bar) + 7]
                };
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
                #endregion
                contextMenu.Items.Add(barMenu);
            }
            if (semipos != -1 && pitch != -1 && s.FindNote(track, bar, pitch, semipos) != -1)
            {
                noteIndex = s.FindNote(track, bar, pitch, semipos);
                Note note = s.GetTracks(track).GetBars(bar).GetNotes(noteIndex) as Note;
                MenuItem noteMenu = new MenuItem
                {
                    Header = note.SymbolAsText()
                };

                #region delete
                MenuItem deleteMenu = new MenuItem
                {
                    Header = "Delete"
                };
                deleteMenu.Click += new RoutedEventHandler(DeleteClick);
                noteMenu.Items.Add(deleteMenu);
                #endregion

                #region accidental
                MenuItem acciedentalMenu = new MenuItem
                {
                    Header = "Accidental"
                };
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
                MenuItem staccatoMenu = new MenuItem
                {
                    Header = "Staccato"
                };
                if (note.GetStaccato()) staccatoMenu.IsChecked = true;
                staccatoMenu.Click += new RoutedEventHandler(StaccatoClick);
                noteMenu.Items.Add(staccatoMenu);
                #endregion

                #region tie
                List<Symbol> availableTies = s.GetNotesToTie(track, bar, noteIndex);
                if (availableTies.Count > 0)
                {
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
                    foreach (Symbol tieNote in availableTies)
                    {
                        MenuItem tieOption = new MenuItem
                        {
                            Header = tieNote.SymbolAsText()
                        };
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

        private void InsertBarMenu_Click(object sender, RoutedEventArgs e)
        {
            s.InsertBarAt(Convert.ToInt32((sender as MenuItem).Tag));
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void SelectableKeySig_Click(object sender, RoutedEventArgs e)
        {
            s.ChangeKeySig(Convert.ToInt32((sender as MenuItem).Tag), bar);
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void TimeSigChanger_Click(object sender, RoutedEventArgs e)
        {
            TimeSigChange timeChange = new TimeSigChange();
            if (timeChange.ShowDialog() == true)
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
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void TrackDeleteClick(object sender, RoutedEventArgs e)
        {
            if (GenerateYesNoDialog("Confirm Action", "Are you sure you want to delete this track?"))
            {
                if (!s.DeleteTrack(track))
                {
                    AddTrack_Click(sender, e);
                    s.DeleteTrack(track);
                }
                drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
            }
        }

        private void BarDeleteClick(object sender, RoutedEventArgs e)
        {
            if (GenerateYesNoDialog("Confirm Action", "Are you sure you want to delete this bar?"))
            {
                if (!s.DeleteBar(bar))
                {
                    s.NewBar(0);
                    s.DeleteBar(bar);
                }
                drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
            }
        }

        private void StaccatoClick(object sender, RoutedEventArgs e)
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

        private void AddTrack_Click(object sender, RoutedEventArgs e)
        {
            CreateTrackPopup createTrack = new CreateTrackPopup();
            if (createTrack.ShowDialog() == true)
            {
                s.NewTrack(createTrack.GetTitle(), s.GetKeySigs(0), s.GetTimeSigs(0).top, s.GetTimeSigs(0).bottom, createTrack.GetTreble());
            }
            drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
        }

        private void Bpm_Click(object sender, RoutedEventArgs e)
        {
            BPMChange newBPMDialog = new BPMChange();
            if (newBPMDialog.ShowDialog() == true)
            {
                int newBPM = 120;
                if (newBPMDialog.GetNewBPM(ref newBPM))
                {
                    if (newBPM < 30 || newBPM > 300)
                    {
                        GenerateErrorDialog("Error", "Tempo out of accepted range (30-300)");
                    }
                    else
                    {
                        s.SetBPM(newBPM);
                        Bpm.Header = "BPM: " + newBPM;
                    }
                }
                else
                {
                    GenerateErrorDialog("Error", "Not a number.");
                }
            }
        }

        private void ChangeTitle_Click(object sender, RoutedEventArgs e)
        {
            TitleChange newTitleDialog = new TitleChange();
            if (newTitleDialog.ShowDialog() == true)
            {
                s.SetTitle(newTitleDialog.GetNewTItle());
                drawer.DrawPage(ref SheetMusic, (int)Zoom.Value);
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
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

        private void BitmapExport_Click(object sender, RoutedEventArgs e)
        {
            drawer.MakePreviewInvisible();
            SaveFileDialog saveFile = new SaveFileDialog
            {
                Filter = "Bitmap Image (*.bmp)|*.bmp"
            };
            if (saveFile.ShowDialog() == true)
            {
                Action<string> bmp = ExportBitmap;
                drawer.Zoom(SheetMusic, 8000);
                this.Dispatcher.Invoke(bmp, System.Windows.Threading.DispatcherPriority.Loaded, saveFile.FileName);
            }
        }
    }
}
