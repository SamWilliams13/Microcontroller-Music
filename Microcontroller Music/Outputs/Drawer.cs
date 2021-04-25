using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Microcontroller_Music
{
    public class Drawer
    {

        #region constants and variables
        //the stroke thickness of a bar divider
        private const int barDividerThickness = 4;
        //height reserved for the title at the top of screen
        private const int extraHeight = 100;
        //the amount of space each symbol in a key signature takes up relative to a semiquaver.
        private const double keySigWidthPerSign = 0.4;
        //the distance between 2 stave lines
        private const int lineGap = 20;
        //the number of semiquaver spaces allowed to be used per line (includes whitespace at start and end of bars, key sigs and time sigs.
        private const int maxLengthPerLine = 58;
        //max number of ledger lines above and below the stave
        private const int maxLinesAbove = 4;
        //width of a note head
        private const int noteHeadWidth = 30;
        //space to the left kept free to write track titles in
        private const int reservedForTrackTitles = 100;
        //space taken up by a semiquaver
        private const int semiquaverwidth = 60;
        // stroke thickness of a stave line
        private const int staveThickness = 2;
        //font family used for the title at top of page
        private const string titleFont = "Times New Roman";
        //font size (pt.) of title
        private const int titleFontSize = 60;
        //used to store the number of semiquavers allocated to each bar in the song
        private int[] barLengths;
        //used to store the number of bars which appear in each line
        private readonly List<int> barsPerLine;
        //used to store the start point (in semiquavers) of each bar in their respective line
        private int[] barStarts;
        //brush used to make black objects appear on canvas.
        private readonly SolidColorBrush black;
        //used to store the height of the canvas, so it can be adjuste to fit the whole song on page.
        private double canvasHeight;
        //used to set the width of the canvas. changed by the zoom functions.
        private int canvasWidth;
        //the space taken up by 1 line in the y axis
        private int lineHeight;
        //the y coordinate of each line.
        private int[] lineStarts;
        //value used to check if a beam should go up or down. called "lastBeamUp" because if there is a tie, the next beam takes the same value as previous
        private bool lastBeamUp = true;
        //stores the memory address of the currently open file.
        private readonly Song SongToDraw;
        //the number of tracks in the song
        private int totalInstruments;
        //the number of lines in the song
        private int totalLines;
        //a brush to make white objects appear on canvas (e.g. the centre of minims)
        readonly SolidColorBrush white;
        //the path of the folder the executable is stored in
        private readonly string exePath;
        //an integer used to loop through which line is being drawn
        private int currentLine;
        //an integer used to loop through which bar is being drawn in the line (tends to max out at 3)
        private int currentBarInLine;
        //an integer used to tell which track is being drawn (though it does one line of each track in that fashion)
        private int currentInstrument;
        //an integer used to count which note in the bar is being looked at
        private int currentNote;
        //A dictionary used to convert from the 12 unique semitones to the 7 unique notes and vice versa.
        private readonly Dictionary<int, int> pitchToNote = new Dictionary<int, int>();
        //a circle which is drawn where the mouse snaps to.
        private readonly Ellipse Preview;
        //an array of ledger lines that appear when the preview note is above or below the stave
        private readonly Line[] previewLines;
        //counts how many bars have been done in previous lines.
        private int barsDone = 0;
        //list of bars where a repeat starts
        private readonly List<int> repeatStarts = new List<int>();
        //list of bars where a repeat ends
        private readonly List<int> repeatEnds = new List<int>();
        //height of the label for bpm
        private readonly int bpmHeight = 40;
        //contains all the notes and rests so they can be independently cleared
        private readonly List<List<List<UIElement>>> barContents = new List<List<List<UIElement>>>();
        #endregion

        public Drawer(Song song)
        {
            //updates the song stored in the class to be the same as the one being edited (possibly irrelevant)
            SongToDraw = song;
            //inititalise the black brush to be black
            black = new SolidColorBrush
            {
                Color = Colors.Black
            };
            //initialise the white brush to be white (but not 100% white - really light grey)
            white = new SolidColorBrush
            {
                Color = (Color)ColorConverter.ConvertFromString("#FEFEFE")
            };
            //initialises the bars per line list
            barsPerLine = new List<int>();
            //finds the path of the executable, so that assets can be located
            exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            //sets up the dictionary to convert semitones to notes
            pitchToNote.Add(0, 0);
            pitchToNote.Add(2, 1);
            pitchToNote.Add(4, 2);
            pitchToNote.Add(5, 3);
            pitchToNote.Add(7, 4);
            pitchToNote.Add(9, 5);
            pitchToNote.Add(11, 6);
            //initialises the preview note head
            Preview = new Ellipse
            {
                //makes the note head black
                Fill = black,
                //sets the height and width of preview to the appropriate values
                Height = lineGap,
                Width = noteHeadWidth,
                //makes the preview note invisible until it should be visible
                Visibility = Visibility.Hidden
            };
            //initialises the array of ledger lines;
            previewLines = new Line[4];
            //initialises the individual lines in the array and makes them hidden by default
            for (int i = 0; i < 4; i++)
            {
                previewLines[i] = new Line
                {
                    Visibility = Visibility.Hidden
                };
            }

        }

        //the main method to draw the entire page at once.
        public void DrawPage(ref Canvas canvas, int zoomValue)
        {
            //initialises the bar starts and lengths arrays to be the same length as the number of bars in the song
            barStarts = new int[SongToDraw.GetTotalBars()];
            barLengths = new int[SongToDraw.GetTotalBars()];
            //variable used to show the y coordinate of the current line being looked at - starts at -
            int lineStarter = extraHeight;
            //deletes all the currently held information about how many bars are in each line
            barsPerLine.Clear();
            //resets the current bar in line to be at the start
            currentBarInLine = 0;
            //deletes all the objects currently on the canvas so they can be replaced.
            canvas.Children.Clear();
            //calculate where the bars should go and how much space they take
            CalculateBarsPerLineEtc(ref canvas);
            //add the preview note and each of the ledger lines to the canvas so they can move around and be vidible without updating the whole page.
            canvas.Children.Add(Preview);
            for (int i = 0; i < 4; i++)
            {
                canvas.Children.Add(previewLines[i]);
            }
            //call the scaling function so the amount of zoom is maintained when drawing the page again.
            Zoom(canvas, zoomValue);
            //AddTitle is used here to draw the title at the top of the page
            AddTitle(ref canvas);
            //reset barsDone so that it can be used when looping through later
            barsDone = 0;
            //this part loops through all the lines in the song
            for (currentLine = 0; currentLine < totalLines; currentLine++)
            {
                //the line about to be drawn is given a startpoint from a cumulative variable
                lineStarts[currentLine] = lineStarter;
                //increase the cumulative variable the make the next line for the first instrument appears below the line for the last instrument
                lineStarter += lineHeight * totalInstruments;
                //draw the lines on the left of the page that go through multiple tracks
                DrawTrackGroupingLine(ref canvas);
                //bool to track if the repeat has been drawn. important for positioning the clef
                for (currentInstrument = 0; currentInstrument < totalInstruments; currentInstrument++)
                {
                    //linestart is a temporary variable that stores the y position of the bottom line of the stave
                    int lineStart = lineStarts[currentLine] + (lineHeight * currentInstrument) + ((maxLinesAbove + 4) * lineGap);
                    //barlength is the pixel length of the bar so far to draw in the bar dividers
                    int barLength = semiquaverwidth;
                    //this part loops through each bar in a line
                    for (currentBarInLine = 0; currentBarInLine < barsPerLine[currentLine]; currentBarInLine++)
                    {
                        //increase the length of the bar covered to include the current length as well (canvas units)
                        barLength += barLengths[barsDone + currentBarInLine] * semiquaverwidth;
                        //draws lines to split up bars
                        DrawBarDividers(ref canvas, barLength);
                        //variable used is the start point of the first note in the bar (used as a relative position to draw the time signature and key signature)
                        int barStart = reservedForTrackTitles + (barStarts[barsDone + currentBarInLine] * semiquaverwidth);
                        //boolean to track whether the time sig has been drawn this bar, used for positioning the key sig.
                        bool drewTimeSig = false;
                        //if it is the first bar in the song or a new time signature (different to previous)
                        if (barsDone + currentBarInLine == 0 || barsDone + currentBarInLine > 0 && SongToDraw.TimeSigIsDifferentToPrevious(barsDone + currentBarInLine))
                        {
                            //it draws the time sig
                            drewTimeSig = true;
                            DrawTimeSig(ref canvas, lineStart, barStart);
                        }
                        //if a repeat starts on this bar it must be added to the canvas
                        if (repeatStarts.Contains(barsDone + currentBarInLine))
                        {
                            DrawRepeatStart(ref canvas, lineStart);
                        }
                        //same for repeat ending on the bar
                        if (repeatEnds.Contains(barsDone + currentBarInLine))
                        {
                            DrawRepeatEnd(ref canvas, lineStart);
                        }
                        DrawKeySig(ref canvas, lineStart, barStart, drewTimeSig);
                        //main part of this method. Draws all the notes, rests, beams etc.
                        DrawBar(ref canvas, currentBarInLine + barsDone, currentInstrument, currentLine);
                    }
                    //draw the clef at start of line
                    DrawClef(ref canvas, lineStart);
                    //draw the stave
                    DrawStaveLines(ref canvas, barLength);
                }
                //increase the number of bars done in previous lines so that the index of bar can be used when looping.
                barsDone += barsPerLine[currentLine];
            }
        }

        //draws the repeat symbol at the start of a bar
        private void DrawRepeatStart(ref Canvas canvas, int lineStart)
        {
            Image repeatImage = new Image()
            {
                //find the image in the folder
                Source = new BitmapImage(new Uri(exePath + "/source/repeatStart.png")),
                //height of the stave
                Height = 4 * lineGap
            };
            //if the first bar in the line, it cannot rely on the previous bar but is easier to calculate x
            if (currentBarInLine == 0)
            {
                Canvas.SetLeft(repeatImage, reservedForTrackTitles);
            }
            //otherwise the x value is just immediately after the end of the previous bar
            else
            {
                int startLeft = reservedForTrackTitles + (barStarts[barsDone + currentBarInLine - 1] + SongToDraw.GetTracks(0).GetBars(currentBarInLine + barsDone - 1).GetMaxLength() + 1) * semiquaverwidth;
                if (repeatEnds.Contains(currentBarInLine + barsDone - 1)) startLeft += semiquaverwidth;
                Canvas.SetLeft(repeatImage, startLeft);
            }
            //top is just enough up from linestart
            Canvas.SetTop(repeatImage, lineStart - lineGap * 3.5);
            //add to canvas
            canvas.Children.Add(repeatImage);
        }

        //draws the repeat symbol at the end of a bar
        private void DrawRepeatEnd(ref Canvas canvas, int lineStart)
        {
            Image repeatImage = new Image()
            {
                //find the image in the folder
                Source = new BitmapImage(new Uri(exePath + "/source/repeatEnd.png")),
                //height of the stave
                Height = 4 * lineGap
            };
            //this doesn't change based on where it is on line, but has some number fudging to get it at the right place. cannot rely on barlengths as that includes the space before the bar starts
            int canvasLeft = reservedForTrackTitles + (barStarts[barsDone + currentBarInLine] + SongToDraw.GetTracks(0).GetBars(currentBarInLine + barsDone).GetMaxLength() + 1) * semiquaverwidth + 8;
            Canvas.SetLeft(repeatImage, canvasLeft);
            //top is just enough up from linestart
            Canvas.SetTop(repeatImage, lineStart - lineGap * 3.5);
            //add to canvas
            canvas.Children.Add(repeatImage);
            //how many times it repeats - 1 less than how many times it plays
            int numberAbove = SongToDraw.GetNumberOfRepeatsAtEndIndex(barsDone + currentBarInLine);
            //if it repeats more than once it needs to display how many times
            if (numberAbove > 1)
            {
                //label with properties so it appears properly
                Label numRepeatLabel = new Label()
                {
                    Content = numberAbove.ToString(),
                    Height = 100,
                    Width = semiquaverwidth,
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    FontFamily = new FontFamily(titleFont),
                    FontSize = 30,
                    Foreground = black
                };
                //just above the repeat end symbol
                Canvas.SetTop(numRepeatLabel, lineStart - 5.5 * lineGap);
                Canvas.SetLeft(numRepeatLabel, canvasLeft);
                //add to canvas
                canvas.Children.Add(numRepeatLabel);
            }
        }

        //draws the horizontal stave lines
        private void DrawStaveLines(ref Canvas canvas, int barLength)
        {
            //loop through 5 times to draw each horizontal stave line. uses barlength that was totalled in the loop to draw all the way along line
            for (int k = 0; k < 5; k++)
            {
                //make a new line
                Line staveLine = new Line
                {
                    //x1 value is just after padding
                    X1 = reservedForTrackTitles,
                    //x2 goes to the end of the line calculated in loop
                    X2 = barLength + reservedForTrackTitles,
                    //uses the same calculation for the top of the stave as others. adds a k value to step downwards to draw equally spaced lines.
                    Y1 = lineStarts[currentLine] + (lineHeight * currentInstrument) + ((maxLinesAbove + k) * lineGap) + lineGap / 2
                };
                //horizontal
                staveLine.Y2 = staveLine.Y1;
                //give the line some drawing properties and add it to canvas.
                staveLine.Stroke = black;
                staveLine.StrokeThickness = staveThickness;
                Canvas.SetZIndex(staveLine, 3);
                canvas.Children.Add(staveLine);
            }
        }

        //draws the key sig at the start of necessary bars
        private void DrawKeySig(ref Canvas canvas, int lineStart, int barStart, bool drewTimeSig)
        {
            //arrays that store the positions of all the notes in a key signature for sharp and flat in bass clef
            int[] bassSharps = { 0, 8, 5, 9, 6, 3, 7, 4 };
            int[] bassFlats = { 0, 4, 7, 3, 6, 2, 5, 1 };
            //stores the key signature of the current bar.
            int keySig = SongToDraw.GetKeySigs(currentBarInLine + barsDone);
            //logic for whether the key sig should be displayed (first bar in line or different to previous)
            if (currentBarInLine == 0 || keySig != SongToDraw.GetKeySigs(currentBarInLine + barsDone - 1))
            {
                //this is used so that little extra code is needed for moving to c major.
                //sets the name of the image so sharp or flat can become natural when needed
                string symbolType = "";
                if (keySig > 0)
                {
                    symbolType = "Sharp";
                }
                else if (keySig < 0)
                {
                    symbolType = "Flat";
                }
                else if (currentBarInLine + barsDone > 0)
                {
                    //key sig is changed so that there is enough space to fit the naturals.
                    symbolType = "Natural";
                    keySig = SongToDraw.GetKeySigs(barsDone + currentBarInLine - 1);
                }
                //if key signature has sharps
                if (keySig > 0 || (keySig == 0 && barsDone + currentBarInLine > 0))
                {
                    //loop through each symbol in the key signature
                    for (int m = 1; m <= keySig; m++)
                    {
                        //make a new image
                        Image sharperImage = new Image
                        {
                            //find the asset for a sharp
                            Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\" + symbolType + ".png"), UriKind.Absolute)),
                            //make the sharp the right height
                            Height = lineGap * 3
                        };
                        //if treble then draw the sharps 2 positions above where it would be in bass
                        if (SongToDraw.GetTracks(currentInstrument).GetTreble())
                        {
                            Canvas.SetTop(sharperImage, lineStart - (2 + bassSharps[m]) * lineGap / 2);
                        }
                        else
                        {
                            Canvas.SetTop(sharperImage, lineStart - bassSharps[m] * lineGap / 2);
                        }
                        //if the time sig was drawn then draw the sharps 1 semiquaver to the left of where they should be
                        //which is just to the left of the start of the notes, with some padding, and 1 symbol space to the right of the previous drawn symbol.
                        if (drewTimeSig)
                        {
                            Canvas.SetLeft(sharperImage, barStart - (keySig - m) * semiquaverwidth * keySigWidthPerSign - 2 * semiquaverwidth);
                        }
                        else
                        {
                            Canvas.SetLeft(sharperImage, barStart - (keySig - m) * semiquaverwidth * keySigWidthPerSign - 1 * semiquaverwidth);
                        }
                        //add to canvas.
                        canvas.Children.Add(sharperImage);
                    }
                }
                //if its flats
                else if (keySig < 0 || (keySig == 0 && barsDone + currentBarInLine > 0))
                {
                    //loop through all symbols
                    for (int m = 1; m <= -1 * keySig; m++)
                    {
                        //make a new image for the symbol
                        Image flatterImage = new Image
                        {
                            //find the flat image in the folder
                            Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\" + symbolType + ".png"), UriKind.Absolute)),
                            //give it the appropriate height
                            Height = lineGap * 3
                        };
                        //if treble then place it 2 positions above where it would be in bass (stored in array)
                        if (SongToDraw.GetTracks(currentInstrument).GetTreble())
                        {
                            Canvas.SetTop(flatterImage, lineStart - (2 + bassFlats[m]) * lineGap / 2 - lineGap / 2);
                        }
                        else
                        {
                            Canvas.SetTop(flatterImage, lineStart - bassFlats[m] * lineGap / 2 - lineGap / 2);
                        }
                        //same logic as treble
                        if (drewTimeSig)
                        {
                            Canvas.SetLeft(flatterImage, barStart - (-1 * keySig - m) * semiquaverwidth * keySigWidthPerSign - 2 * semiquaverwidth);
                        }
                        else
                        {
                            Canvas.SetLeft(flatterImage, barStart - (-1 * keySig - m) * semiquaverwidth * keySigWidthPerSign - 1 * semiquaverwidth);
                        }
                        //add symbol to canvas.
                        canvas.Children.Add(flatterImage);
                    }
                }
            }
        }

        //draws the time signature
        private void DrawTimeSig(ref Canvas canvas, int lineStart, int barStart)
        {

            //draw the top number in the key signature
            Image topNumber = new Image
            {
                //find the image based on the number in the folder
                Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\" +
                SongToDraw.GetTimeSigs(barsDone + currentBarInLine).top + ".png"), UriKind.Absolute)),
                //set height to fit in 2 line gaps (top half of stave)
                Height = lineGap * 1.98
            };
            //an adjustment value to make sure that 2 digit numbers appear centered above one digit numbers (i.e. 2 digit ones appear further to the left)
            double doubleAdjust = (SongToDraw.GetTimeSigs(barsDone + currentBarInLine).top >= 10) ? semiquaverwidth * 0.2 : 0;
            //sets to start at the top of the stave
            Canvas.SetTop(topNumber, lineStart - lineGap * 3.5);
            //sets to be just to the left of the first note in bar.
            Canvas.SetLeft(topNumber, barStart - semiquaverwidth - doubleAdjust);
            //add to canvas
            canvas.Children.Add(topNumber);
            //draw the bottom number - same as top number save for y position.
            Image bottomNumber = new Image
            {
                Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\" +
                SongToDraw.GetTimeSigs(barsDone + currentBarInLine).bottom + ".png"), UriKind.Absolute)),
                Height = lineGap * 1.98
            };
            doubleAdjust = (SongToDraw.GetTimeSigs(barsDone + currentBarInLine).bottom >= 10) ? semiquaverwidth * 0.2 : 0;
            //sets the top of the number to be the middle of the stave
            Canvas.SetTop(bottomNumber, lineStart - lineGap * 1.5);
            Canvas.SetLeft(bottomNumber, barStart - semiquaverwidth - doubleAdjust);
            canvas.Children.Add(bottomNumber);

        }

        //draws the lines that split up bars
        private void DrawBarDividers(ref Canvas canvas, int barLength)
        {
            //draws the divide between bars (vertical line)
            Line barDivide = new Line
            {
                //x position is the point at the end of bar
                X1 = barLength + reservedForTrackTitles
            };
            barDivide.X2 = barDivide.X1;
            //y position is top and bottom of 1 stave
            barDivide.Y1 = lineStarts[currentLine] + (lineHeight * currentInstrument) + (maxLinesAbove * lineGap) + lineGap / 2;
            barDivide.Y2 = lineStarts[currentLine] + lineHeight * currentInstrument + (maxLinesAbove + 5) * lineGap - lineGap / 2;
            //add line properties to divider and add to canvas.
            barDivide.Stroke = black;
            barDivide.StrokeThickness = barDividerThickness;
            canvas.Children.Add(barDivide);
        }

        //draws the long line on left of page
        private void DrawTrackGroupingLine(ref Canvas canvas)
        {
            //instrumentGrouping is the long line on the left that goes through all the tracks that would play concurrently.
            Line instrumentGrouping = new Line
            {
                //starts after the padding
                X1 = reservedForTrackTitles
            };
            //is vertical
            instrumentGrouping.X2 = instrumentGrouping.X1;
            //starts at the top of the first stave (the linegap/2 is so that it is in line with the top stave line instead of just above it)
            instrumentGrouping.Y1 = lineStarts[currentLine] + maxLinesAbove * lineGap + lineGap / 2;
            //ends at the bottom of the last stave
            instrumentGrouping.Y2 = lineStarts[currentLine] + lineHeight * (totalInstruments - 1) + (maxLinesAbove + 5) * lineGap - lineGap / 2;
            //assign drawing properties to the line and add line to the canvas.
            instrumentGrouping.Stroke = black;
            instrumentGrouping.StrokeThickness = barDividerThickness;
            canvas.Children.Add(instrumentGrouping);
        }

        //calculates where bars should be and how many per line and how much space they take up etc
        private void CalculateBarsPerLineEtc(ref Canvas canvas)
        {
            repeatEnds.Clear();
            repeatStarts.Clear();
            //sets the canvas width - equal to the maximum length of the bars on one line and the space to the left, and 2 semiquavers worth of space to the right
            canvasWidth = semiquaverwidth * maxLengthPerLine + reservedForTrackTitles + 2 * semiquaverwidth;
            //updates the total number of tracks in the song
            totalInstruments = SongToDraw.GetTrackCount();
            //make empty lists to store all the bar contents
            barContents.Clear();
            for (int i = 0; i < totalInstruments; i++)
            {
                barContents.Add(new List<List<UIElement>>());
                for (int j = 0; j < SongToDraw.GetTotalBars(); j++)
                {
                    barContents[i].Add(new List<UIElement>());
                }
            }
            //sets the height of each line
            lineHeight = (maxLinesAbove * 2 + 5) * lineGap;
            //barsthisline is used to store the value to be added to bars per line, while the number of bars that fit is being calculated.
            int barsThisLine = 0;
            //current bar length is used to add to the length of the bar for extra features such as key sigs
            int currentBarLength;
            //current line length stores the cumulative total 
            int currentLineLength = 0;
            //loop through all bars to get the length required to display them and assign bars to lines.
            for (int i = 0; i < SongToDraw.GetTotalBars(); i++)
            {
                //booleans to check whether the time signature or key signature needs to be displayed on this bar.
                bool changeTimeSig = false;
                bool changeKeySig = false;
                //bar length defaults to the length of the bar in semiquavers with one semiquaver either side for padding
                currentBarLength = SongToDraw.GetSigLength(i) + 2;
                //if it is the first bar or the time signature changes, then the time sig must be changed, add space for that in bar. 
                if ((i == 0) || (i > 0 && SongToDraw.TimeSigIsDifferentToPrevious(i)))
                {
                    currentBarLength += 1;
                    changeTimeSig = true;
                }
                //if a repeat starts on the bar, some space is taken by the sign
                if (SongToDraw.DoesARepeatStartorEndOn(i, 0))
                {
                    currentBarLength++;
                    repeatStarts.Add(i);
                }
                if (SongToDraw.DoesARepeatStartorEndOn(i, 1))
                {
                    currentBarLength++;
                    repeatEnds.Add(i);
                }
                //if it is the first bar or the key signature changes (not supported)
                if ((i == 0) || (i > 0 &&
                    SongToDraw.GetKeySigs(i) != SongToDraw.GetKeySigs(i - 1)))
                {
                    //calculate the amount of space needed for the key signature (integer key signature multiplied by the space required for 1 symbol)
                    //add that value to the length of the current bar
                    //this is so you can have the naturals that appear to differentiate the "no sharps" c major.
                    if (i != 0 && SongToDraw.GetKeySigs(i) == 0)
                    {
                        currentBarLength += (int)Math.Ceiling(Math.Abs(keySigWidthPerSign * SongToDraw.GetKeySigs(i - 1)));
                    }
                    else
                    {
                        currentBarLength += (int)Math.Ceiling(Math.Abs(keySigWidthPerSign * SongToDraw.GetKeySigs(i)));
                    }
                    //keysig has been displayed this bar.
                    changeKeySig = true;
                }
                //if the length of this bar can fit on the current line
                if (currentBarLength + currentLineLength <= maxLengthPerLine)
                {
                    //sets the starting x position of the bar to be 2 semiquavers after the previous one for padding
                    barStarts[i] = currentLineLength + 2;
                    //update the length of the current line
                    currentLineLength += currentBarLength;
                    //store the length of the bar in this array
                    barLengths[i] = currentBarLength;
                    //increase the number of bars to be drawn on this line.
                    barsThisLine++;
                }
                //if the bar doesn't fit onto the current line, then add the previous line to the list and make a new one
                else
                {
                    //set the start of the bar to be 2 padding semiquavers after the reserved space on the left.
                    barStarts[i] = 2;
                    //add the number of bars on the line (the ones up to but not including this bar) to the list of bars on each line 
                    barsPerLine.Add(barsThisLine);
                    //if it isn't already drawing the key signature, then it must do so
                    if (!changeKeySig)
                    {
                        //add the space for the key signature with the same calculation as before
                        currentBarLength += (int)Math.Ceiling(Math.Abs(keySigWidthPerSign * SongToDraw.GetKeySigs(i)));
                        //make sure the start point of the bar is adjusted to fit later.
                        changeKeySig = true;
                    }
                    //add the length of the bar to a fresh line length
                    currentLineLength = currentBarLength;
                    //add the length of the bar to the array
                    barLengths[i] = currentBarLength;
                    //reset the barsThisLine count
                    barsThisLine = 1;
                }
                //if the key signature or time sig is displayed, move the start point of the notes in the bar to make room. Uses the same calculation as the one for bar length
                //this must be done later as the bar start might be moved to a new line.
                if (changeKeySig) barStarts[i] += (int)Math.Ceiling(Math.Abs(keySigWidthPerSign * SongToDraw.GetKeySigs(i)));
                //this can be done as the previous line adds nothing in this instance. however it needs to add enough space to fit the natural signs to get the signature back to 0.
                if (changeKeySig && i > 0 && SongToDraw.GetKeySigs(i) == 0) barStarts[i] += (int)Math.Ceiling(Math.Abs(keySigWidthPerSign * SongToDraw.GetKeySigs(i - 1)));
                if (changeTimeSig) barStarts[i] += 1;
                //adds the extra space required for repeats
                if (repeatStarts.Contains(i)) barStarts[i]++;
            }
            //add the final line to the list
            barsPerLine.Add(barsThisLine);
            //update total lines so it can be used to count in the loop
            totalLines = barsPerLine.Count;
            //initialise line starts with the number of lines required.
            lineStarts = new int[totalLines];
            //calculate the height of the canvas using the title height added to the total number of lines for each track x number of tracks x height of each line with some extra height at bottom.
            canvasHeight = 2 * extraHeight + (totalLines * lineHeight * totalInstruments);
            //update the height of the canvas
            canvas.Height = canvasHeight;
        }

        //a function to draw the clef where needed
        private void DrawClef(ref Canvas canvas, int lineStart)
        {
            Image clef = new Image();
            //extra is used to move the clef to the right to fit the repeat sign in
            int extra = 0;
            if (repeatStarts.Contains(barsDone)) extra += semiquaverwidth;
            //if it's a treble clef
            if (SongToDraw.GetTracks(currentInstrument).GetTreble())
            {
                //locate the treble image from the folder
                clef.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\TrebleClef.png"), UriKind.Absolute));
                //set top of image to be the top of the stave and a bit higher to look nice. 2 is fudge number
                //must swirl around g
                Canvas.SetTop(clef, lineStart - lineGap * 5 - 2);
                //places the clef immediately after the padding
                Canvas.SetLeft(clef, reservedForTrackTitles + extra);
                //makes the clef appropriate size
                clef.Height = lineGap * 7.5;
            }
            //otherwise it is a bass clef
            else
            {
                //find the source image in folder
                clef.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\FClef.png"), UriKind.Absolute));
                //make the bass clef appear in the middle of stave
                //must colon around f
                Canvas.SetTop(clef, lineStart - lineGap * 3.5);
                //offset the clef a bit to the right so the spacing looks ok
                Canvas.SetLeft(clef, reservedForTrackTitles + 10 + extra);
                //bass clef is quite a bit smaller
                clef.Height = lineGap * 3;
            }
            //add clef to canvas.
            canvas.Children.Add(clef);
        }

        //a function that loops through symbols in a bar, groups together notes as appropriate and calls other functions to draw stems, note heads and rests
        public void DrawBar(ref Canvas canvas, int bar, int track, int line = -1)
        {
            //clears the bar on the canvas
            if (barContents[track][bar].Count > 0)
            {
                foreach (UIElement uIElement in barContents[track][bar])
                {
                    canvas.Children.Remove(uIElement);
                }
                barContents[track][bar].Clear();
            }
            //recalculate barStart for use in drawing methods.
            int barStart = reservedForTrackTitles + (barStarts[bar] * semiquaverwidth);
            //recalculate lineStart for use in drawing
            //other parts of the program will not know which line they are looking at, so require this section to find it
            if (line == -1)
            {
                bool barFound = false;
                int iterator = 0;
                int barsChecked = 0;
                do
                {
                    //checks if the bar index is contained within the line
                    if (bar < barsPerLine[iterator] + barsChecked)
                    {
                        line = iterator;
                        barFound = true;
                    }
                    //otherwise increments the count
                    else
                    {
                        barsChecked += barsPerLine[iterator];
                        iterator++;
                    }
                    //carry on until found - prevents infinite loop but does not prevent crash
                } while (!barFound && iterator < barsPerLine.Count);
            }
            int lineStart = lineStarts[line] + (lineHeight * track) + ((maxLinesAbove + 4) * lineGap);
            //two separate lists referring to the same thing.
            //the first stores a list of all the notes that have appeared in the bar before
            //the second stores a list of the most recent accidental used on the note at the same index in the first list.
            //these are used to accurately draw accidentals in bar.
            List<int> usedPitches = new List<int>();
            List<int> usedPitchAccidentals = new List<int>();
            //beamstart is the index of the first note in a beam. -1 means there is no beam being looked at at the moment.
            int beamStart = -1;
            //beamend is the last note in the beam. must be updated after every new note.
            int beamEnd = 0;
            //thisbeam contains a semiquaver is to limit beams with semiquavers to 1 crotchet length.
            bool thisBeamContainsASemiQuaver = false;
            //keysigindex is the integer value of the keysignature of the bar. used to check whether accidental should be drawn
            int KeySigIndex = SongToDraw.GetTracks(track).GetBars(bar).GetKeySig();
            //midpoint is the middle of the bar, used to make sure that beams do not cross the middle of a bar (strange music theory rule)
            int midpoint = SongToDraw.GetTracks(track).GetBars(bar).GetMaxLength() / 2;
            //loop through all symbols in the bar.
            for (currentNote = 0; currentNote < SongToDraw.GetTracks(track).GetBars(bar).GetNoteCount(); currentNote++)
            {
                //if current symbol is a note
                if (SongToDraw.GetTracks(track).GetBars(bar).GetNotes(currentNote) is Note)
                {
                    //removes some "as" lines.
                    Note note = SongToDraw.GetTracks(track).GetBars(bar).GetNotes(currentNote) as Note;
                    //make sure that if the beam has a semiquaver then it is shown in bool.
                    if (note.GetLength() == 1)
                    {
                        thisBeamContainsASemiQuaver = true;
                    }
                    //central case. whether the beam should carry on or not 
                    if (beamStart != -1)
                    {
                        //this checks if the note is happening at the same time as a previous note, in which case no checks need to take place
                        if ((SongToDraw.GetTracks(track).GetBars(bar).GetNotes(beamEnd) as Note).GetStart() == note.GetStart())
                        {
                            beamEnd = currentNote;
                        }
                        //this checks that if the note should be a single due to its length it is - if it is crotchet or longer it shouldn't beam
                        else if ((SongToDraw.GetTracks(track).GetBars(bar).GetNotes(beamEnd) as Note).GetLength() >= 4 || note.GetLength() >= 4)
                        {
                            DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd);
                            beamStart = currentNote;
                            beamEnd = currentNote;
                            thisBeamContainsASemiQuaver = (note.GetLength() == 1);
                        }
                        //beams containing 16ths should only be grouped in length 4
                        //beams not containing 16th should not be grouped over the half way mark (there are other rules i might implement someday)
                        else
                        {
                            if (thisBeamContainsASemiQuaver && note.GetLength() + note.GetStart() - (SongToDraw.GetTracks(track).GetBars(bar).GetNotes(beamStart) as Note).GetStart() > 4)
                            {
                                DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd);
                                beamStart = currentNote;
                                beamEnd = currentNote;
                                thisBeamContainsASemiQuaver = (note.GetLength() == 1);
                            }
                            //nice piece of logic here. if it ends after the midpoint or starts after the midpoint but not both --> it crosses the midpoint of the bar. big no no except in 3/4?
                            else if (note.GetLength() + note.GetStart() > midpoint ^ (SongToDraw.GetTracks(track).GetBars(bar).GetNotes(beamStart) as Note).GetStart() >= midpoint)
                            {
                                DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd);
                                beamStart = currentNote;
                                beamEnd = currentNote;
                                thisBeamContainsASemiQuaver = (note.GetLength() == 1);
                            }
                            //the part where nothing interesting happens so the beam keeps going.
                            else
                            {
                                beamEnd = currentNote;
                            }
                        }
                    }
                    //if it is the first note in the bar then it will certainly be the start of a beam, and no other beam should be drawn.
                    else
                    {
                        beamStart = currentNote;
                        beamEnd = currentNote;
                    }
                    //draws the note that was just being looked at, passing the result of checkdrawaccidental - which has a self explanatory title.
                    DrawNote(ref canvas, barStart, lineStart, track, bar, currentNote,
                        CheckDrawAccidental(ref usedPitchAccidentals, ref usedPitches, KeySigIndex, note.GetPitch(), note.GetAccidental()), 0);
                }
                //if it is a rest it will end the previous beam, reset start and end to default and draw the rest.
                else if (SongToDraw.GetTracks(track).GetBars(bar).GetNotes(currentNote) is Rest)
                {
                    //beam logic
                    if (beamStart != -1)
                    {
                        DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd);
                        beamStart = -1;
                        beamEnd = 0;
                    }
                    DrawRest(ref canvas, barStart, lineStart, track, bar, currentNote);
                }
            }
            //if there is an unfinished beam once the loop is over then draw that one.
            if (beamStart != -1)
            {
                DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd);
            }
        }

        //draws the title at the top of the page, as well as BPM
        public void AddTitle(ref Canvas canvas)
        {
            //makes a label
            Label songTitle = new Label
            {
                //puts the title in it
                Content = SongToDraw.GetTitle(),
                //makes it the right height and width and centres it.
                Width = canvasWidth,
                Height = extraHeight,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                //gives the text the right properties
                Foreground = black,
                FontFamily = new FontFamily(titleFont),
                FontSize = titleFontSize
            };
            //shows the bpm (forced crotchets per minute)
            Label bpmLabel = new Label
            {
                Content = "= " + SongToDraw.GetBPM(),
                //makes it the right height and width and centres it.
                Width = 3 * bpmHeight,
                Height = bpmHeight,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                //gives the text the right properties
                Foreground = black,
                FontFamily = new FontFamily(titleFont),
                FontSize = 25
            };
            Canvas.SetLeft(bpmLabel, reservedForTrackTitles);
            Canvas.SetTop(bpmLabel, extraHeight - bpmHeight);
            Image crotchetPicture = new Image()
            {
                Source = new BitmapImage(new Uri(exePath + "/source/crotchet note.png")),
                Height = bpmHeight
            };
            Canvas.SetLeft(crotchetPicture, reservedForTrackTitles);
            Canvas.SetTop(crotchetPicture, extraHeight - bpmHeight);
            //adds it to the canvas.
            canvas.Children.Add(crotchetPicture);
            canvas.Children.Add(bpmLabel);
            canvas.Children.Add(songTitle);
        }

        //checks if an accidental should be drawn on a note.
        private bool CheckDrawAccidental(ref List<int> usedPitchAccidentals, ref List<int> usedPitches, int KeySigIndex, int pitch, int accidental)
        {
            bool drawAccidental = false;
            //if the pitch is already present in the bar...
            if (usedPitches.Contains(pitch - accidental))
            {
                //...and the accidental hasn't changed since it last appeared...
                if (usedPitchAccidentals[usedPitches.IndexOf(pitch - accidental)] == accidental)
                {
                    //...draw the accidental
                    drawAccidental = false;
                }
                //...and the accidental has changed since it last appeared...
                else
                {
                    //draw the accidental, and update the accidental stored for that pitch
                    usedPitchAccidentals[usedPitches.IndexOf(pitch - accidental)] = accidental;
                    drawAccidental = true;
                }
            }
            //if the pitch hasn't previously appeared
            else
            {
                //add the pitch and accidental to their respecive lists
                usedPitches.Add(pitch - accidental);
                usedPitchAccidentals.Add(accidental);
                //loop through the integer keysigs to see if the pitch matches one that should be sharpened or flattened normally
                bool matchesKeySig = false;
                for (int n = 1; n < 8; n++)
                {
                    if (KeySigIndex >= n && (pitch - accidental) % 12 == (7 * n + 2) % 12)
                    {
                        //if it should be sharpened and isnt in the song, draw the accidental
                        matchesKeySig = true;
                        if (accidental != 1)
                        {
                            drawAccidental = true;
                        }
                    }
                    if (KeySigIndex <= (-1 * n) && (pitch - accidental) % 12 == Math.Abs((5 * n + 10) % 12))
                    {
                        //if it should be flattened but isn't then draw the accidental.
                        matchesKeySig = true;
                        if (accidental != -1)
                        {
                            drawAccidental = true;
                        }
                    }
                }
                //if the pitch doesn't match the key signature and isnt a natural then draw the accidental
                if (!matchesKeySig)
                {
                    if (accidental != 0)
                    {
                        drawAccidental = true;
                    }
                }
            }
            return drawAccidental;
        }

        //draws the lines between groups of notes - very long and somewhat repetitive
        private void DrawStems(ref Canvas canvas, int barStart, int lineStart, int trackIndex, int barIndex, int startIndex, int endIndex)
        {
            //arrays to store various pieces of information about the notes in the group
            int[] pitches = new int[endIndex - startIndex + 1];
            int[] startPoints = new int[endIndex - startIndex + 1];
            int[] lengths = new int[endIndex - startIndex + 1];
            bool[] staccatos = new bool[endIndex - startIndex + 1];
            //ingtegers to store more infor about the group
            int averagePitch = 0;
            int highestPitch = -100;
            int lowestPitch = 100;
            int closestPitch = 100000000;
            bool treble = SongToDraw.GetTracks(trackIndex).GetTreble();
            //loop through all the notes in the group to fill the arrays of data about the notes
            for (int i = 0; i <= endIndex - startIndex; i++)
            {
                Note note = SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(startIndex + i) as Note;
                pitches[i] = FindLineDifferenceFromMiddleC(note.GetPitch() - note.GetAccidental(), treble);
                startPoints[i] = note.GetStart();
                lengths[i] = note.GetLength();
                staccatos[i] = note.GetStaccato();
                averagePitch += pitches[i];
                if (highestPitch < pitches[i] || highestPitch == -100)
                {
                    highestPitch = pitches[i];
                }
                if (lowestPitch > pitches[i] || lowestPitch == 100)
                {
                    lowestPitch = pitches[i];
                }
                if (Math.Abs(closestPitch) > Math.Abs(pitches[i] - 3))
                {
                    closestPitch = pitches[i];
                }
            }
            //calculate the average pitch for use in the beam line
            averagePitch /= lengths.Length;
            //if the group starts and ends at the same place it is a single
            if (SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(startIndex).GetStart() == SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(endIndex).GetStart() && SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(endIndex).GetLength() < 16)
            {
                //note is a single and should have a flag
                //all this has to do is draw a line from the furthest from centre to an octave away from opposite note
                //set up stem
                Line singleLine = new Line
                {
                    Stroke = black,
                    StrokeThickness = 2
                };
                //set up flag
                Image flag = new Image
                {
                    Height = 3 * lineGap
                };
                //set up staccato
                Ellipse staccatoCircle = new Ellipse
                {
                    Fill = black,
                    Height = lineGap * 0.4
                };
                staccatoCircle.Width = staccatoCircle.Height;
                //put the staccato in the right place, just don't make it visible yet
                Canvas.SetLeft(staccatoCircle, barStart + semiquaverwidth * startPoints[0] + semiquaverwidth / 2 - staccatoCircle.Width / 4);
                if (highestPitch < 3 || Math.Abs(highestPitch - 3) < Math.Abs(lowestPitch - 3))
                {
                    lastBeamUp = true;
                    //stem goes up - draw line on right of notes; draw line from middle of lowest to an octave from highest -- unless lowest is more than an octave from middle line (3)
                    singleLine.X1 = barStart + semiquaverwidth * startPoints[0] + noteHeadWidth + 1 + (semiquaverwidth - noteHeadWidth) / 2;
                    singleLine.X2 = singleLine.X1;
                    singleLine.Y1 = lineStart - (lowestPitch) * lineGap / 2;
                    //if line is enough ledger lines away it goes to a specific point instead of an octave up
                    singleLine.Y2 = (Math.Abs(highestPitch - 3) > 7) ? lineStart - lineGap : lineStart - (highestPitch + 7) * lineGap / 2;
                    //puts the flag at top of stem
                    Canvas.SetTop(flag, singleLine.Y2);
                    //if it is shorter than a crotchet it will have a flag, so find the image source
                    if (lengths[0] < 4)
                    {
                        //makes sure that dotted notes will also point to the right file
                        double flagLen = (Math.Log(lengths[0], 2) % 1 != 0) ? lengths[0] / 1.5 : lengths[0];
                        flag.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\up" + flagLen + "flag.png"), UriKind.Absolute));
                    }
                    //if the notes are staccato place the circle at the bottom and make it visible
                    if (staccatos[0])
                    {
                        Canvas.SetTop(staccatoCircle, lineStart - (lowestPitch - 1) * lineGap / 2 + lineGap / 4);
                        canvas.Children.Add(staccatoCircle);
                        barContents[trackIndex][barIndex].Add(staccatoCircle);
                    }
                }
                else
                {
                    //stem goes down
                    lastBeamUp = false;
                    //similar to above but with different values
                    //stem is to left of note instead of right
                    singleLine.X1 = barStart + semiquaverwidth * startPoints[0] + 4 + (semiquaverwidth - noteHeadWidth) / 2;
                    singleLine.X2 = singleLine.X1;
                    //similar logic to when step goes up but the note 
                    singleLine.Y1 = lineStart - (highestPitch) * lineGap / 2;
                    singleLine.Y2 = (Math.Abs(lowestPitch - 3) > 7) ? lineStart - lineGap - lineGap / 2 : lineStart - (lowestPitch - 7) * lineGap / 2;
                    Canvas.SetTop(flag, singleLine.Y2 - 3 * lineGap);
                    if (lengths[0] < 4)
                    {
                        double flagLen = (Math.Log(lengths[0], 2) % 1 != 0) ? lengths[0] / 1.5 : lengths[0];
                        flag.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\down" + flagLen + "flag.png"), UriKind.Absolute));
                    }
                    if (staccatos[0])
                    {
                        Canvas.SetTop(staccatoCircle, lineStart - (highestPitch + 2) * lineGap / 2 - lineGap / 4);
                        canvas.Children.Add(staccatoCircle);
                        barContents[trackIndex][barIndex].Add(staccatoCircle);
                    }
                }
                Canvas.SetLeft(flag, singleLine.X1);
                canvas.Children.Add(singleLine);
                barContents[trackIndex][barIndex].Add(singleLine);
                if (lengths[0] < 4)
                {
                    canvas.Children.Add(flag);
                    barContents[trackIndex][barIndex].Add(flag);
                }
            }
            else if (SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(endIndex).GetLength() < 16)
            {
                //this is a group of notes and should be treated so

                //step by step:
                //calculate least squares regression gradient of beam using startpoints and noteheights
                double numerator = 0;
                double denominator = 0;
                double averageStart = (startPoints[0] + startPoints[startPoints.Length - 1]) / 2;
                for (int i = 0; i <= endIndex - startIndex; i++)
                {
                    numerator += (startPoints[i] - averageStart) * (pitches[i] - averagePitch);
                    denominator += Math.Pow((startPoints[i] - averageStart), 2);
                }
                double beamGradient = -1 * numerator / denominator;
                //calculate if notes go above or below using average (and a fight between min and max notes to see if they need to go above or below - you'll see; it's gonna be great)
                if (Math.Abs(highestPitch - averagePitch) > 6 ^ Math.Abs(lowestPitch - averagePitch) > 6) //another xor but we actually need to know which it is
                {
                    lastBeamUp = Math.Abs(lowestPitch - averagePitch) > 6;
                }
                else
                {
                    lastBeamUp = (averagePitch <= 3);
                }
                //calculate height of beam using average distance from gradient and min (target 3.5, min 2?)
                double beamDifference = averagePitch + averageStart * beamGradient;
                beamDifference = (lastBeamUp) ? beamDifference + 7 : beamDifference - 7;
                //loop through all the notes to make sure none are too close to the beam
                for (int i = 0; i < startPoints.Length; i++)
                {
                    if (lastBeamUp && beamDifference - beamGradient * startPoints[i] - pitches[i] < 6)
                    {
                        beamDifference = pitches[i] + 6 + startPoints[i] * beamGradient;
                    }
                    else if (!lastBeamUp && pitches[i] - (beamDifference - beamGradient * startPoints[i]) < 6)
                    {
                        beamDifference = pitches[i] - 6 + startPoints[i] * beamGradient;
                    }
                }
                //beamDifference = defaultbeamStart;
                //draw the lines
                Polygon beam = new Polygon
                {
                    Fill = black
                };
                Point p1;
                Point p2;
                int lineStartFromSemiStart;
                int beamUpAdjustment = 0;
                //use the new gradient to draw the beam
                if (lastBeamUp)
                {
                    p2 = new Point(barStart + startPoints[startPoints.Length - 1] * semiquaverwidth + noteHeadWidth + 1 + (semiquaverwidth - noteHeadWidth) / 2, lineStart - (beamDifference - beamGradient * startPoints[startPoints.Length - 1]) * lineGap / 2);
                    p1 = new Point(barStart + startPoints[0] * semiquaverwidth + noteHeadWidth + 1 + (semiquaverwidth - noteHeadWidth) / 2, lineStart - (beamDifference - beamGradient * startPoints[0]) * lineGap / 2);
                    lineStartFromSemiStart = noteHeadWidth + 1 + (semiquaverwidth - noteHeadWidth) / 2;
                    beamUpAdjustment = 1;
                }
                else
                {
                    p1 = new Point(barStart + startPoints[0] * semiquaverwidth + 4 + (semiquaverwidth - noteHeadWidth) / 2, lineStart - (beamDifference - beamGradient * startPoints[0]) * lineGap / 2);
                    p2 = new Point(barStart + startPoints[startPoints.Length - 1] * semiquaverwidth + 4 + (semiquaverwidth - noteHeadWidth) / 2, lineStart - (beamDifference - beamGradient * startPoints[startPoints.Length - 1]) * lineGap / 2);
                    lineStartFromSemiStart = 4 + (semiquaverwidth - noteHeadWidth) / 2;
                }
                beam.Points.Add(p1);
                beam.Points.Add(p2);
                beam.Points.Add(new Point(p2.X, p2.Y - lineGap / 2));
                beam.Points.Add(new Point(p1.X, p1.Y - lineGap / 2));
                canvas.Children.Add(beam);
                barContents[trackIndex][barIndex].Add(beam);
                for (int i = 0; i <= endIndex - startIndex; i++)
                {
                    //draw note stems
                    Line stem = new Line
                    {
                        Stroke = black,
                        StrokeThickness = 2,
                        X1 = barStart + startPoints[i] * semiquaverwidth + lineStartFromSemiStart
                    };
                    stem.X2 = stem.X1;
                    stem.Y1 = lineStart - (pitches[i]) * lineGap / 2;
                    stem.Y2 = lineStart - (beamDifference - beamGradient * startPoints[i] + beamUpAdjustment) * lineGap / 2;
                    canvas.Children.Add(stem);
                    barContents[trackIndex][barIndex].Add(stem);
                    //handle staccatos
                    if (staccatos[i])
                    {
                        Ellipse staccatoCircle = new Ellipse
                        {
                            Fill = black,
                            Height = lineGap * 0.4
                        };
                        staccatoCircle.Width = staccatoCircle.Height;
                        Canvas.SetLeft(staccatoCircle, barStart + semiquaverwidth * startPoints[i] + semiquaverwidth / 2 - staccatoCircle.Width / 4);
                        if (!lastBeamUp && (i == 0 || startPoints[i - 1] != startPoints[i]))
                        {
                            //draw staccato above
                            Canvas.SetTop(staccatoCircle, lineStart - (pitches[i] + 2) * lineGap / 2 - lineGap / 4);
                            canvas.Children.Add(staccatoCircle);
                            barContents[trackIndex][barIndex].Add(staccatoCircle);
                        }
                        if (lastBeamUp && (i == startPoints.Length - 1 || startPoints[i + 1] != startPoints[i]))
                        {
                            //draw a staccato below
                            Canvas.SetTop(staccatoCircle, lineStart - (pitches[i] - 1) * lineGap / 2 + lineGap / 4);
                            canvas.Children.Add(staccatoCircle);
                            barContents[trackIndex][barIndex].Add(staccatoCircle);
                        }
                    }
                    //add a second line for semiquavers and notes after dots.
                    //use the gradient line and intercept to draw these lines. polygons instead of lines or rectangles so the lines can be vertical
                    double semiBottomY = stem.Y2;
                    semiBottomY = (lastBeamUp) ? semiBottomY + lineGap * 0.75 : semiBottomY - lineGap * 0.75;
                    double semiTopY = (lastBeamUp) ? semiBottomY + lineGap / 2 : semiBottomY - lineGap / 2;
                    //handles beaming to the left for instances where there is nothing to the right to prove it is a semiquaver
                    if (lengths[i] == 1 && startPoints[i] == startPoints[startPoints.Length - 1])
                    {
                        Polygon semiBeam = new Polygon
                        {
                            Fill = black
                        };
                        semiBeam.Points.Add(new Point(stem.X1, semiBottomY));
                        semiBeam.Points.Add(new Point(stem.X1, semiTopY));
                        semiBeam.Points.Add(new Point(stem.X1 - noteHeadWidth, semiTopY - ((noteHeadWidth / (double)semiquaverwidth) * beamGradient) * lineGap / 2));
                        semiBeam.Points.Add(new Point(stem.X1 - noteHeadWidth, semiBottomY - ((noteHeadWidth / (double)semiquaverwidth) * beamGradient) * lineGap / 2));
                        canvas.Children.Add(semiBeam);
                        barContents[trackIndex][barIndex].Add(semiBeam);
                    }
                    //handles the regular case where 2 or more semiuavers are grouped
                    else if (lengths[i] == 1 && startPoints[i + 1] != startPoints[i] && lengths[i + 1] == 1)
                    {
                        Polygon semiBeamR = new Polygon
                        {
                            Fill = black
                        };
                        semiBeamR.Points.Add(new Point(stem.X1, semiBottomY));
                        semiBeamR.Points.Add(new Point(stem.X1, semiTopY));
                        if (lengths[i + 1] < 2)
                        {
                            semiBeamR.Points.Add(new Point(stem.X1 + semiquaverwidth, semiTopY + (beamGradient) * lineGap / 2));
                            semiBeamR.Points.Add(new Point(stem.X1 + semiquaverwidth, semiBottomY + (beamGradient) * lineGap / 2));
                        }
                        else
                        {
                            semiBeamR.Points.Add(new Point(stem.X1 + noteHeadWidth, semiTopY + ((noteHeadWidth / (double)semiquaverwidth) * beamGradient) * lineGap / 2));
                            semiBeamR.Points.Add(new Point(stem.X1 + noteHeadWidth, semiBottomY + ((noteHeadWidth / (double)semiquaverwidth) * beamGradient) * lineGap / 2));
                        }
                        canvas.Children.Add(semiBeamR);
                        barContents[trackIndex][barIndex].Add(semiBeamR);
                    }
                    //handles when the note to the left isn't a semiquaver but neither is the one to the right so it has to go half to the right to 
                    else if ((i==0 || lengths[i-1] != 1) && lengths[i] == 1 && startPoints[i + 1] != startPoints[i] && lengths[i + 1] != 1)
                    {
                        Polygon semiBeamHalf = new Polygon
                        {
                            Fill = black
                        };
                        semiBeamHalf.Points.Add(new Point(stem.X1, semiBottomY));
                        semiBeamHalf.Points.Add(new Point(stem.X1, semiTopY));
                        semiBeamHalf.Points.Add(new Point(stem.X1 + noteHeadWidth, semiTopY + ((noteHeadWidth / (double)semiquaverwidth) * beamGradient) * lineGap / 2));
                        semiBeamHalf.Points.Add(new Point(stem.X1 + noteHeadWidth, semiBottomY + ((noteHeadWidth / (double)semiquaverwidth) * beamGradient) * lineGap / 2));
                        canvas.Children.Add(semiBeamHalf);
                        barContents[trackIndex][barIndex].Add(semiBeamHalf);
                    }
                }
            }
            else
            {
                //handles staccatos for semibreves.
                Ellipse staccatoCircle = new Ellipse
                {
                    Fill = black,
                    Height = lineGap * 0.4
                };
                staccatoCircle.Width = staccatoCircle.Height;
                Canvas.SetLeft(staccatoCircle, barStart + semiquaverwidth * startPoints[0] + semiquaverwidth / 2 - staccatoCircle.Width / 4 - 3);
                if (staccatos[startPoints.Length - 1])
                {
                    Canvas.SetTop(staccatoCircle, lineStart - (lowestPitch - 1.5) * lineGap / 2);
                    canvas.Children.Add(staccatoCircle);
                    barContents[trackIndex][barIndex].Add(staccatoCircle);
                }
            }
        }

        //draws the head of the note and certain things surrounding it (staccato and accidental)
        //regarding the parameters:
        //canvas so you can add things to the image
        //barstart gives the x coord to add to to place the note
        //lineStart in this instance... it would be nice to give it as the E4 in treble and G2 in bass. then you can compare.
        private void DrawNote(ref Canvas canvas, int barStart, int lineStart, int trackIndex, int barIndex, int noteIndex, bool drawAccidental, double startDisplacement)
        {
            //gets the note being drawn
            Note note = SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(noteIndex) as Note;
            //calculates the pitch of the note ignoring the accidental
            int pitch = note.GetPitch() - note.GetAccidental();
            //gets the distance from the bottom of stave that the circle needs to be drawn
            int spaceDifferenceFromBottomLine = FindLineDifferenceFromMiddleC(pitch, (SongToDraw.GetTracks(trackIndex).GetTreble()));
            bool drawDot;
            //if the length cannot be divided by 2 it must be a dotted note
            if (Math.Log(note.GetLength(), 2) % 1 != 0)
            {
                drawDot = true;
            }
            else
            {
                drawDot = false;
            }
            //if the note has a forward connection, draw it now
            if (note.GetTie() != null)
            {
                DrawConnection(ref canvas, note, barIndex, trackIndex, barStart, lineStart);
            }
            //the main circle used to draw the note head, set up a couple properties and dimensions
            Ellipse noteCircle = new Ellipse
            {
                Fill = black,
                Height = lineGap,
                Width = noteHeadWidth
            };
            //position the note head
            Canvas.SetLeft(noteCircle, barStart + (semiquaverwidth * (note.GetStart() + startDisplacement)) + (semiquaverwidth - noteHeadWidth) / 2);
            Canvas.SetTop(noteCircle, lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 4);
            //rotate the note head to make it look better
            noteCircle.RenderTransform = new RotateTransform(-20);
            //add the note head to the canvas
            canvas.Children.Add(noteCircle);
            barContents[trackIndex][barIndex].Add(noteCircle);
            //if it is a minim (or dotted) then draw a white sircle on top of the note to make it look hollow
            if (note.GetLength() >= 8 && note.GetLength() < 16)
            {
                //set up the white circle - it is thinner than the main circle
                Ellipse minimGap = new Ellipse
                {
                    Fill = white,
                    Height = lineGap / 2,
                    Width = noteHeadWidth - 1
                };
                //position the circle on the canvas inside of the previous one
                Canvas.SetLeft(minimGap, barStart + (semiquaverwidth * note.GetStart()) + 2 + (semiquaverwidth - noteHeadWidth) / 2);
                Canvas.SetTop(minimGap, lineStart - spaceDifferenceFromBottomLine * lineGap / 2);
                //rotate to match
                minimGap.RenderTransform = new RotateTransform(-20);
                //add to canvas
                canvas.Children.Add(minimGap);
                barContents[trackIndex][barIndex].Add(minimGap);
            }
            //if the note is a semibreve
            else if (note.GetLength() >= 16)
            {
                //take the main note and un-rotate it
                noteCircle.RenderTransform = null;
                //move the note to be better positioned given its new rotation
                Canvas.SetTop(noteCircle, lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 2);
                //set up a new white circle to make the note look hollow
                //unlike the minim circle this one is designed to be thinner than the main circle
                Ellipse semiBreveGap = new Ellipse
                {
                    Fill = white,
                    Height = lineGap - 4,
                    Width = noteHeadWidth / 1.75
                };
                //position it in the middle of the main note head circle
                Canvas.SetLeft(semiBreveGap, barStart + (semiquaverwidth * note.GetStart()) + lineGap / 2 + (semiquaverwidth - noteHeadWidth) / 2);
                Canvas.SetTop(semiBreveGap, lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 2);
                //rotate to make it look pretty
                semiBreveGap.RenderTransform = new RotateTransform(20);
                //add to canvas
                canvas.Children.Add(semiBreveGap);
                barContents[trackIndex][barIndex].Add(semiBreveGap);
            }
            //if the note is dotted, dot it here
            if (drawDot)
            {
                //set up the little circle
                Ellipse dottedNoteDot = new Ellipse
                {
                    Fill = black,
                    Height = lineGap * 0.4
                };
                dottedNoteDot.Width = dottedNoteDot.Height;
                //position it to the right and in the middle (vertically) of the note head
                Canvas.SetLeft(dottedNoteDot, barStart + (semiquaverwidth * (note.GetStart() + 1)) - lineGap / 4);
                Canvas.SetTop(dottedNoteDot, lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 4);
                //add to canvas
                canvas.Children.Add(dottedNoteDot);
                barContents[trackIndex][barIndex].Add(dottedNoteDot);
            }
            //if it's high enough up some ledger lines need drawing
            if (spaceDifferenceFromBottomLine > 7)
            {
                //loop through each step where a line would be drawn until you reach the y coords of the note
                //the loop forces linegap distances between each line
                for (int i = lineStart - 4 * lineGap - lineGap / 2; i > lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 2; i -= lineGap)
                {
                    //draw a semiquaverwidth line over the width of the note
                    Line staveLine = new Line
                    {
                        Stroke = black,
                        StrokeThickness = staveThickness
                    };
                    int leftShift = 0;
                    //if its a semibreve the ledger lines need to be moved to the left a bit
                    if (note.GetLength() >= 16)
                    {
                        leftShift = 3;
                    }
                    //draw a horizontal line across width
                    staveLine.X1 = barStart + (semiquaverwidth * (note.GetStart() + startDisplacement)) + lineGap / 2 - 10 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    staveLine.X2 = barStart + semiquaverwidth * (note.GetStart() + startDisplacement) + lineGap / 2 + noteHeadWidth - 3 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    staveLine.Y1 = i;
                    staveLine.Y2 = i;
                    //add the ledger line to canvas
                    canvas.Children.Add(staveLine);
                    barContents[trackIndex][barIndex].Add(staveLine);
                }
            }
            //same as above but with different loop so it can go down below the stave
            else if (spaceDifferenceFromBottomLine < -1)
            {
                for (int i = lineStart + lineGap + lineGap / 2; i < lineStart - spaceDifferenceFromBottomLine * lineGap / 2 + lineGap / 2; i += lineGap)
                {
                    //draw a semiquaverwidth line over the width of the note
                    Line staveLine = new Line
                    {
                        Stroke = black,
                        StrokeThickness = staveThickness
                    };
                    int leftShift = 0;
                    //if its a semibreve the ledger lines need to be moved to the left a bit
                    if (note.GetLength() >= 16)
                    {
                        leftShift = 3;
                    }
                    //draw a horizontal line across width
                    staveLine.X1 = barStart + (semiquaverwidth * (note.GetStart() + startDisplacement)) + lineGap / 2 - 10 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    staveLine.X2 = barStart + semiquaverwidth * (note.GetStart() + startDisplacement) + lineGap / 2 + noteHeadWidth - 3 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    staveLine.Y1 = i;
                    staveLine.Y2 = i;
                    //add the ledger line to canvas
                    canvas.Children.Add(staveLine);
                    barContents[trackIndex][barIndex].Add(staveLine);
                }
            }
            //if an accidental has to be drawn, draw it here
            if (drawAccidental)
            {
                //set up the image to be the right height
                Image accidentalImage = new Image
                {
                    Height = lineGap * 3
                };
                //if it's flat find the flat image in source and set it up to be positioned correctly and the right size
                if (note.GetAccidental() == -1)
                {
                    accidentalImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\Flat.png"), UriKind.Absolute));
                    Canvas.SetTop(accidentalImage, lineStart - (spaceDifferenceFromBottomLine + 3) * lineGap / 2 - lineGap / 4);
                    accidentalImage.Height = lineGap * 2.5;
                }
                //if it's natural find the natural image in source and set it up to be positioned correctly (it is already right size at top)
                else if (note.GetAccidental() == 0)
                {
                    accidentalImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\Natural.png"), UriKind.Absolute));
                    Canvas.SetTop(accidentalImage, lineStart - (spaceDifferenceFromBottomLine + 2) * lineGap / 2 - lineGap / 2.3);
                }
                //if it's sharp find the sharp image in source and set it up to be positioned correctly (it is already right size at top)
                else
                {
                    accidentalImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\Sharp.png"), UriKind.Absolute));
                    Canvas.SetTop(accidentalImage, lineStart - (spaceDifferenceFromBottomLine + 3) * lineGap / 2);
                }
                //set the accidental to be just to the left of the note head and add it to canvas
                Canvas.SetLeft(accidentalImage, barStart + (semiquaverwidth) * (note.GetStart() + startDisplacement));
                canvas.Children.Add(accidentalImage);
                barContents[trackIndex][barIndex].Add(accidentalImage);
            }
        }

        //draws ties and slurs between note
        private void DrawConnection(ref Canvas canvas, Note note, int bar, int track, int barStart, int lineStart)
        {
            //the whole strange process to draw an arc on a canvas
            //get the note to connect to
            Note tieNote = note.GetTie() as Note;
            //arc segment is the info on the arc (end and curve)
            ArcSegment blackCurve = new ArcSegment();
            //path figure has the start point and a collection of segments
            PathFigure connectionPathFigure = new PathFigure();
            //holds the arcsegment
            PathSegmentCollection connectionPathSegmentCollection = new PathSegmentCollection();
            //holds the path figure
            PathFigureCollection connectionPathFigureCollection = new PathFigureCollection();
            //holds the figure collection
            PathGeometry connectionPathGeometry = new PathGeometry();
            //has the geometry and can be drawn
            Path connectionPath = new Path();
            //finds the midpoint y of the note heads
            double noteY = lineStart - FindLineDifferenceFromMiddleC(note.GetPitch() - note.GetAccidental(), (SongToDraw.GetTracks(track).GetTreble())) * lineGap / 2;
            double tieY = lineStart - FindLineDifferenceFromMiddleC(tieNote.GetPitch() - tieNote.GetAccidental(), (SongToDraw.GetTracks(track).GetTreble())) * lineGap / 2;
            //if the note is low enough on the stave, the connection will be concave upwards
            if (-1 * (noteY - lineStart) < 4 * lineGap / 2)
            {
                //therefore the connection will be moved to below the note head
                noteY += lineGap * 0.75;
                tieY += lineGap * 0.75;
                //concave up
                blackCurve.SweepDirection = SweepDirection.Counterclockwise;
            }
            else
            {
                //above the note heads
                noteY -= lineGap * 0.75;
                tieY -= lineGap * 0.75;
                //concave down
                blackCurve.SweepDirection = SweepDirection.Clockwise;
            }
            //start point is just to the right of the middle of the note head, and uses the precalculated y val
            connectionPathFigure.StartPoint = new Point(barStart + (note.GetStart()) * semiquaverwidth + semiquaverwidth / 2 + 10, noteY);
            //if the tie starts before the note it must be in the next bar
            if (tieNote.GetStart() <= note.GetStart())
            {
                //if the bar starts after the next one, it must be on a new line
                if (barStarts[bar] >= barStarts[bar + 1])
                {
                    //therefore just draw the tie to the end of the bar
                    blackCurve.Point = new Point(barStart + (SongToDraw.GetTracks(0).GetBars(bar).GetMaxLength() + 1) * semiquaverwidth - 9, (noteY + tieY) / 2);
                }
                else
                {
                    //otherwise the x value of the endpoint must be put into the next bar
                    blackCurve.Point = new Point(barStarts[bar + 1] * semiquaverwidth + reservedForTrackTitles + semiquaverwidth / 2 - 9, tieY);
                }
            }
            else
            {
                //make the curve end just the left of the second note
                blackCurve.Point = new Point(barStart + tieNote.GetStart() * semiquaverwidth + semiquaverwidth / 2 - 9, tieY);
            }
            //sets up the size so it curves nicely over the distance
            blackCurve.Size = new Size(Math.Abs(blackCurve.Point.X - connectionPathFigure.StartPoint.X), 70);
            //add all the objects to the object just above it until there is something drawable
            connectionPathSegmentCollection.Add(blackCurve);
            connectionPathFigure.Segments = connectionPathSegmentCollection;
            connectionPathFigureCollection.Add(connectionPathFigure);
            connectionPathGeometry.Figures = connectionPathFigureCollection;
            //set up drawing properties
            connectionPath.Stroke = black;
            connectionPath.StrokeThickness = 3;
            connectionPath.Data = connectionPathGeometry;
            //add to canvas
            canvas.Children.Add(connectionPath);
            barContents[track][bar].Add(connectionPath);
        }

        //draws a rest on canvas using an image
        private void DrawRest(ref Canvas canvas, int barStart, int lineStart, int trackIndex, int barIndex, int noteIndex)
        {
            //gets the rest for information
            Rest rest = SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(noteIndex) as Rest;
            //makes an image to be added ot canvas
            Image restImage = new Image();
            //makes a rectangle for minim and semibreve rests
            Rectangle restRectangle = new Rectangle
            {
                Height = lineGap / 2,
                Width = semiquaverwidth / 1.5
            };
            //sets up the rest depending on its length
            switch (rest.GetLength())
            {
                //if it is shorter than a minim it must find the image source in the folder, set it up to be the right size and y position in bar
                case 1:
                    Canvas.SetTop(restImage, lineStart - lineGap * 2.25);
                    restImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\16th_rest.png"), UriKind.Absolute));
                    restImage.Height = lineGap * 3;
                    break;
                case 2:
                    Canvas.SetTop(restImage, lineStart - lineGap * 3.5);
                    restImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\8thRest.png"), UriKind.Absolute));
                    restImage.Height = lineGap * 4;
                    break;
                case 4:
                    Canvas.SetTop(restImage, lineStart - lineGap * 3);
                    restImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\Crotchet.png"), UriKind.Absolute));
                    restImage.Height = lineGap * 3;
                    break;
                //if it is a minim or longer it must set the rectangle to be drawn and give it the right y position
                case 8:
                    Canvas.SetTop(restRectangle, lineStart - lineGap * 2);
                    restRectangle.Fill = black;
                    break;
                case 16:
                    Canvas.SetTop(restRectangle, lineStart - lineGap * 1.5);
                    restRectangle.Fill = black;
                    break;
            }
            //all images or rectangles are set to same x position so this can be done outside of the switch
            Canvas.SetLeft(restImage, barStart + (semiquaverwidth * rest.GetStart()) + (semiquaverwidth - noteHeadWidth) / 2);
            Canvas.SetLeft(restRectangle, barStart + (semiquaverwidth * rest.GetStart()) + (semiquaverwidth - noteHeadWidth) / 3);
            //add both to the canvas, in any case only one will be visible
            canvas.Children.Add(restImage);
            canvas.Children.Add(restRectangle);
            barContents[trackIndex][barIndex].Add(restImage);
            barContents[trackIndex][barIndex].Add(restRectangle);
        }

        //takes the note and finds how far from the bottom line of the stave it should be (in note positions, which are linegap / 2)
        private int FindLineDifferenceFromMiddleC(int pitch, bool treble)
        {
            //middle c is 40, so this is the difference in pitch
            int distanceFromC = pitch - 40;
            int lineDiff;
            //mod12 so that it can be placed into a dictionary to convert the semitone difference to a note position difference.
            int mod12 = distanceFromC % 12;
            //makes sure mod12 is positive
            if (mod12 < 0)
            {
                mod12 += 12;
            }
            //adds how many octaves up to how many note positions up
            lineDiff = 7 * (int)Math.Round((distanceFromC - (mod12)) / 12.0) + pitchToNote[mod12];
            //place it correctly on stave based on where middle c would be
            lineDiff = (treble) ? lineDiff - 3 : lineDiff + 9;
            return lineDiff;
        }

        #region UI Interactions
        //scales the canvas
        public void Zoom(Canvas canvas, int chosenWidth)
        {
            //makes a new width
            canvas.Width = chosenWidth;
            //scales the height based on the old width and the preset width used when drawing
            double newHeight = ((double)chosenWidth / canvasWidth) * canvasHeight;
            canvas.Height = newHeight;
            //scales the objects on the canvas to the new zoom
            canvas.RenderTransform = new ScaleTransform((double)chosenWidth / canvasWidth, (double)chosenWidth / canvasWidth);
            //makes a rectangle and adds it to the background to prevent strange graphical errors
            Rectangle rectangle = new Rectangle
            {
                Fill = white,
                Height = canvasHeight,
                Width = canvasWidth
            };
            Canvas.SetZIndex(rectangle, -1);
            canvas.Children.Add(rectangle);
        }

        //calls whereami using the mouse position given when clicking
        public bool FindMouseLeft(ref Canvas canvas, ref int trackIndex, ref int barIndex, ref int notePos, ref int pitch, int length, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(canvas);
            bool worked = WhereAmI(ref trackIndex, ref barIndex, ref notePos, ref pitch, length, position);
            return worked;
        }

        //calls whereami using the mouse position given when hovering
        public bool FindMouse(ref Canvas canvas, ref int trackIndex, ref int barIndex, ref int notePos, ref int pitch, int length, MouseEventArgs e)
        {
            var position = e.GetPosition(canvas);
            bool worked = WhereAmI(ref trackIndex, ref barIndex, ref notePos, ref pitch, length, position);
            return worked;
        }

        //used to find the track, bar, pitch and semiquaver position of the mouse, as well as whether a note will fit there
        private bool WhereAmI(ref int trackIndex, ref int barIndex, ref int notePos, ref int pitch, int length, Point position)
        {
            //if statement to make sure notes can't be placed in the space at top of page, and extra linegap / 4 to make sure placement rules are consistent with
            //the other lines.
            if (position.Y < extraHeight + lineGap / 4)
            {
                //no notes at top of page
                return false;
            }
            //calculate which line the user is on
            int line = (int)((position.Y - extraHeight) / lineHeight);
            //calculate which track the line relates to
            trackIndex = line % totalInstruments;
            //calculate which line group the user is on
            int lineIndex = (line - trackIndex) / totalInstruments;
            //if it is after all the lines with bars on them stop here
            if (lineIndex >= barsPerLine.Count)
            {
                return false;
            }
            //count up all the bars before the the line the user is on
            int barsBefore = 0;
            for (int i = 0; i < lineIndex; i++)
            {
                barsBefore += barsPerLine[i];
            }
            //get how many semiquavers into the line
            double xSemiPos = (position.X - reservedForTrackTitles) / semiquaverwidth;
            bool barFound = false;
            //loop through all the bars in the line, right to left
            for (int i = barsPerLine[lineIndex] - 1; i >= 0; i--)
            {
                //if the mouse is to the right of the bar start then the bar has been found
                if (xSemiPos >= barStarts[barsBefore + i])
                {
                    //unless the mouse position is after the end of the bar, in which case it failed
                    if (xSemiPos - barStarts[barsBefore + i] > SongToDraw.GetTracks(trackIndex).GetBars(barsBefore + i).GetMaxLength())
                    {
                        return false;
                    }
                    barIndex = barsBefore + i;
                    barFound = true;
                    break;
                }
            }
            //if the bar was not found then fail out
            if (!barFound)
            {
                return false;
            }
            //subtract the start of the bar from mouse pos to get the semiquavers into the bar
            notePos = (int)Math.Floor(xSemiPos - barStarts[barIndex]);
            //converts the note position of the pitch into a semitone position
            pitch = ReverseFindLineDifferenceFromMiddleC((int)position.Y - (lineStarts[lineIndex] + (lineHeight * (trackIndex)) + ((maxLinesAbove + 4) * lineGap)), SongToDraw.GetTracks(trackIndex).GetTreble());
            //preview brush is set to a colour based on whether or not it can be placed
            SolidColorBrush previewBrush;
            if (SongToDraw.GetTracks(trackIndex).GetBars(barIndex).CheckFit(new Note(length, notePos, pitch)) != 0)
            {
                previewBrush = new SolidColorBrush()
                {
                    Color = Colors.Gray
                };
            }
            else
            {
                previewBrush = black;
            }
            //set preview note colour
            Preview.Fill = previewBrush;
            //move the preview note to where the mouse is
            Canvas.SetLeft(Preview, barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + (semiquaverwidth * (notePos)) + (semiquaverwidth - noteHeadWidth) / 2);
            int previewPitch = FindLineDifferenceFromMiddleC(pitch, SongToDraw.GetTracks(trackIndex).GetTreble());
            //if high/low enough, add preview ledger lines. see drawnote for how this works
            if (previewPitch > 7)
            {
                //for each possible position, if note is above that, make the line visible
                for (int i = 0; i < 4; i++)
                {
                    if (previewPitch > 8 + 2 * i)
                    {
                        previewLines[i].Visibility = Visibility.Visible;
                    }
                    //otherwise make it invisible
                    else
                    {
                        previewLines[i].Visibility = Visibility.Hidden;
                    }
                    //set up some line properties
                    previewLines[i].Stroke = black;
                    previewLines[i].StrokeThickness = staveThickness;
                    //move the line to be over the same x as the mouse
                    previewLines[i].X1 = barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + (semiquaverwidth * notePos) + lineGap / 2 - 10 + (semiquaverwidth - noteHeadWidth) / 2;
                    previewLines[i].X2 = barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + semiquaverwidth * (notePos) + lineGap / 2 + noteHeadWidth - 3 + (semiquaverwidth - noteHeadWidth) / 2;
                    //adjust y accordingly
                    previewLines[i].Y1 = (lineStarts[lineIndex] + (lineHeight * (trackIndex)) + ((maxLinesAbove) * lineGap)) - lineGap / 2 - i * lineGap;
                    previewLines[i].Y2 = previewLines[i].Y1;

                }
            }
            //same as above but for below the stave
            else if (previewPitch < -1)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (previewPitch < -2 - 2 * i)
                    {
                        previewLines[i].Visibility = Visibility.Visible;
                    }
                    else
                    {
                        previewLines[i].Visibility = Visibility.Hidden;
                    }
                    previewLines[i].Stroke = black;
                    previewLines[i].StrokeThickness = staveThickness;
                    //calculate start and end pints 
                    previewLines[i].X1 = barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + (semiquaverwidth * notePos) + lineGap / 2 - 10 + (semiquaverwidth - noteHeadWidth) / 2;
                    previewLines[i].X2 = barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + semiquaverwidth * (notePos) + lineGap / 2 + noteHeadWidth - 3 + (semiquaverwidth - noteHeadWidth) / 2;
                    previewLines[i].Y1 = lineStarts[lineIndex] + (lineHeight * (trackIndex)) + ((maxLinesAbove + 5.5) * lineGap) + i * lineGap;
                    previewLines[i].Y2 = previewLines[i].Y1;
                }
            }
            //if it is within the stave make all the ledger lines invisible
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    previewLines[i].Visibility = Visibility.Hidden;
                }
            }
            //set the preview note head to be at the right height for the mouse
            Canvas.SetTop(Preview, (lineStarts[lineIndex] + (lineHeight * (trackIndex)) + ((maxLinesAbove + 4) * lineGap)) - previewPitch * lineGap / 2 - lineGap / 4);
            //rotate to make it look nice
            Preview.RenderTransform = new RotateTransform(-20);
            //make it visible
            Preview.Visibility = Visibility.Visible;
            return true;
        }

        //loops through all preview ledger lines and makes those and the note head invisible
        public void MakePreviewInvisible()
        {
            foreach (Line line in previewLines)
            {
                line.Visibility = Visibility.Hidden;
            }
            Preview.Visibility = Visibility.Hidden;
        }

        //does FindDifferenceFromMiddleC backwards
        private int ReverseFindLineDifferenceFromMiddleC(double lineDiff, bool treble)
        {
            //reverses changes made to adjust where middle c is in different keys
            lineDiff = (treble) ? -2 * lineDiff / lineGap + 3 : -2 * lineDiff / lineGap - 9;
            //gets the note outside of octave
            double mod7 = lineDiff % 7;
            if (mod7 < 0)
            {
                mod7 += 7;
            }
            //runs mod7 backwards through the disctionary to find its semitone
            int mod12 = pitchToNote.First(x => x.Value == (int)mod7).Key;
            //accounts for the += 12 in the forwards one
            if (lineDiff < 0)
            {
                mod12 -= 12;
            }
            //puts all the components together to reverse the effects of findlindifferencefrommiddlec
            return (int)((lineDiff - (lineDiff % 7)) / 7) * 12 + mod12 + 40;
        }

        //returns the space reserved for the title - used for bitmap export
        public int GetTitleHeight(ref Canvas canvas)
        {
            return (int)(extraHeight * canvas.Width / canvasWidth);
        }

        //calculates the height in pixels of the lines that would be put onto 1 page
        public int GetPageHeight(Canvas canvas)
        {
            if (totalLines * totalInstruments < 15)
            {
                return (int)canvas.Height;
            }
            else
            {
                return (int)(Math.Floor((double)15 / totalInstruments) * totalInstruments * lineHeight / canvasWidth * canvas.Width);
            }
        }
        #endregion
    }
}
