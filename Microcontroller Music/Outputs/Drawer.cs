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
        readonly SolidColorBrush black;
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
            //deletes all the currently held information about how many bars are in each line
            barsPerLine.Clear();
            //resets the current bar in line to be at the start
            currentBarInLine = 0;
            //deletes all the objects currently on the canvas so they can be replaced.
            canvas.Children.Clear();
            #region determine the heights of lines and how many bars per line and the lengths of bars
            //sets the canvas width - equal to the maximum length of the bars on one line and the space to the left, and 2 semiquavers worth of space to the right
            canvasWidth = semiquaverwidth * maxLengthPerLine + reservedForTrackTitles + 2 * semiquaverwidth;
            //updates the total number of tracks in the song
            totalInstruments = SongToDraw.GetTrackCount();
            //sets the height of each line to 
            lineHeight = (maxLinesAbove * 2 + 5) * lineGap;
            //variable used to show the y coordinate of the current line being looked at - starts at -
            int lineStarter = extraHeight;
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
            }
            //add the final line to the list
            barsPerLine.Add(barsThisLine);
            //update total lines so it can be used to count in the loop
            totalLines = barsPerLine.Count;
            //initialise line starts with the number of lines required.
            lineStarts = new int[totalLines];
            //calculate the height of the canvas using the title height added to the total number of lines for each track x number of tracks x height of each line;
            canvasHeight = extraHeight + (totalLines * lineHeight * totalInstruments);
            //update the height of the canvas
            canvas.Height = canvasHeight;
            //add the preview note and each of the ledger lines to the canvas so they can move around and be vidible without updating the whole page.
            canvas.Children.Add(Preview);
            for (int i = 0; i < 4; i++)
            {
                canvas.Children.Add(previewLines[i]);
            }
            //call the scaling function so the amount of zoom is maintained when drawing the page again.
            Zoom(canvas, zoomValue);
            #endregion
            #region draw and populate bars
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
                #region draw track grouping line
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
                #endregion
                //this part loops through each instrument for the line
                for (currentInstrument = 0; currentInstrument < totalInstruments; currentInstrument++)
                {
                    //linestart is a temporary variable that stores the y position of the bottom line of the stave
                    int lineStart = lineStarts[currentLine] + (lineHeight * currentInstrument) + ((maxLinesAbove + 4) * lineGap);
                    //barlength is the pixel length of the bar so far to draw in the bar dividers
                    int barLength = semiquaverwidth;
                    #region draw clef
                    Image clef = new Image();
                    //if it's a treble clef
                    if (SongToDraw.GetTracks(currentInstrument).GetTreble())
                    {
                        //locate the treble image from the folder
                        clef.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\TrebleClef.png"), UriKind.Absolute));
                        //set top of image to be the top of the stave and a bit higher to look nice. 2 is fudge number
                        //must swirl around g
                        Canvas.SetTop(clef, lineStart - lineGap * 5 - 2);
                        //places the clef immediately after the padding
                        Canvas.SetLeft(clef, reservedForTrackTitles);
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
                        Canvas.SetLeft(clef, reservedForTrackTitles + 10);
                        //bass clef is quite a bit smaller
                        clef.Height = lineGap * 3;
                    }
                    //add clef to canvas.
                    canvas.Children.Add(clef);
                    #endregion
                    //this part loops through each bar in a line
                    for (currentBarInLine = 0; currentBarInLine < barsPerLine[currentLine]; currentBarInLine++)
                    {
                        //increase the length of the bar covered to include the current length as well (canvas units)
                        barLength += barLengths[barsDone + currentBarInLine] * semiquaverwidth;
                        #region draw bar dividers
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
                        #endregion
                        //variable used is the start point of the first note in the bar (used as a relative position to draw the time signature and key signature)
                        int barStart = reservedForTrackTitles + (barStarts[barsDone + currentBarInLine] * semiquaverwidth);
                        #region drawing time signature
                        //boolean to track whether the time sig has been drawn this bar, used for positioning the key sig.
                        bool drewTimeSig = false;
                        //if it is the first bar in the song or a new time signature (different to previous)
                        if (barsDone + currentBarInLine == 0 || barsDone + currentBarInLine > 0 && SongToDraw.TimeSigIsDifferentToPrevious(barsDone + currentBarInLine))
                        {
                            //it draws the time sig
                            drewTimeSig = true;
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
                        #endregion
                        #region drawing key signatures
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
                        #endregion
                        //main part of this method. Draws all the notes, rests, beams etc.
                        DrawBar(ref canvas, currentBarInLine + barsDone, currentInstrument, currentLine);
                    }
                    #region draw the stave
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
                        canvas.Children.Add(staveLine);
                    }
                    #endregion
                }
                //increase the number of bars done in previous lines so that the index of bar can be used when looping.
                barsDone += barsPerLine[currentLine];
            }
            #endregion
        }

        //a function that loops through symbols in a bar, groups together notes as appropriate and calls other functions to draw stems, note heads and rests
        public void DrawBar(ref Canvas canvas, int bar, int track, int line)
        {
            //recalculate barStart for use in drawing methods.
            int barStart = reservedForTrackTitles + (barStarts[bar] * semiquaverwidth);
            //recalculate lineStart for use in drawing
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
            //last note was a tuplet is used so nothing beams into or out of a tuplet.
            bool lastNoteWasATuplet = false;
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
                    //beam logic - if the last symbol was a tuplet they cannot beam
                    //draw the previous beam and start a new one with the start and end index of that note.
                    if (beamStart != -1 && lastNoteWasATuplet)
                    {
                        DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd, lastNoteWasATuplet);
                        beamStart = currentNote;
                        beamEnd = currentNote;
                        lastNoteWasATuplet = false;
                        thisBeamContainsASemiQuaver = (note.GetLength() == 1);
                    }
                    //central case. whether the beam should carry on or not 
                    else if (beamStart != -1)
                    {
                        //this checks if the note is happening at the same time as a previous note, in which case no checks need to take place
                        if ((SongToDraw.GetTracks(track).GetBars(bar).GetNotes(beamEnd) as Note).GetStart() == note.GetStart())
                        {
                            beamEnd = currentNote;
                        }
                        //this checks that if the note should be a single due to its length it is - if it is crotchet or longer it shouldn't beam
                        else if ((SongToDraw.GetTracks(track).GetBars(bar).GetNotes(beamEnd) as Note).GetLength() >= 4 || note.GetLength() >= 4)
                        {
                            DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd, lastNoteWasATuplet);
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
                                DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd, lastNoteWasATuplet);
                                beamStart = currentNote;
                                beamEnd = currentNote;
                                thisBeamContainsASemiQuaver = (note.GetLength() == 1);
                            }
                            //nice piece of logic here. if it ends after the midpoint or starts after the midpoint but not both --> it crosses the midpoint of the bar. big no no except in 3/4?
                            else if (note.GetLength() + note.GetStart() > midpoint ^ (SongToDraw.GetTracks(track).GetBars(bar).GetNotes(beamStart) as Note).GetStart() >= midpoint)
                            {
                                DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd, lastNoteWasATuplet);
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
                        DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd, lastNoteWasATuplet);
                        beamStart = -1;
                        beamEnd = 0;
                        lastNoteWasATuplet = false;
                    }
                    DrawRest(ref canvas, barStart, lineStart, track, bar, currentNote);
                }
                //handles the tuplet logic.
                else
                {
                    //logic for beaming - if previous note was not a tuplet then they cannot beam, draw previous, start a new one
                    if (beamStart != -1 && !lastNoteWasATuplet)
                    {
                        DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd, false);
                        beamStart = currentNote;
                        beamEnd = currentNote;
                        lastNoteWasATuplet = true;
                    }
                    //if last symbol was also a tuplet then they can continue on.
                    else if (lastNoteWasATuplet)
                    {
                        beamEnd = currentNote;
                    }
                    //otherwise it is the first note in the bar and should be treated the same way notes are, except tuplet is true.
                    else
                    {
                        beamStart = currentNote;
                        beamEnd = currentNote;
                        lastNoteWasATuplet = true;
                    }
                    //draw tuplet logic here...
                }
            }
            //if there is an unfinished beam once the loop is over then draw that one.
            if (beamStart != -1)
            {
                DrawStems(ref canvas, barStart, lineStart, track, bar, beamStart, beamEnd, lastNoteWasATuplet);
            }
        }
        #region separate functions for drawing certain important things

        //draws the title at the top of the page
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
            //adds it to the canvas.
            canvas.Children.Add(songTitle);
        }

        //checks if an accidental should be drawn on a note.
        public bool CheckDrawAccidental(ref List<int> usedPitchAccidentals, ref List<int> usedPitches, int KeySigIndex, int pitch, int accidental)
        {
            bool drawAccidental = false;
            if (usedPitches.Contains(pitch - accidental))
            {
                if (usedPitchAccidentals[usedPitches.IndexOf(pitch - accidental)] == accidental)
                {
                    drawAccidental = false;
                }
                else
                {
                    usedPitchAccidentals[usedPitches.IndexOf(pitch - accidental)] = accidental;
                    drawAccidental = true;
                }
            }
            else
            {
                usedPitches.Add(pitch - accidental);
                usedPitchAccidentals.Add(accidental);

                bool matchesKeySig = false;
                for (int n = 1; n < 8; n++)
                {
                    if (KeySigIndex >= n && (pitch - accidental) % 12 == (7 * n + 2) % 12)
                    {
                        matchesKeySig = true;
                        if (accidental != 1)
                        {
                            drawAccidental = true;
                        }
                    }
                    if (KeySigIndex <= (-1 * n) && (pitch - accidental) % 12 == Math.Abs((5 * n + 10) % 12))
                    {
                        matchesKeySig = true;
                        if (accidental != -1)
                        {
                            drawAccidental = true;
                        }
                    }
                }
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
        public void DrawStems(ref Canvas canvas, int barStart, int lineStart, int trackIndex, int barIndex, int startIndex, int endIndex, bool tuplet)
        {
            if (tuplet)
            {
                //oooh fancy time for fancy people
                //tuplets are in dire need of reconstruction which is fab. i'll get back to them one day but eh
                //big plans; you'll love it but it does mean that a lot of the logic is going to be left until the end
            }
            else
            {
                int[] pitches = new int[endIndex - startIndex + 1];
                int[] startPoints = new int[endIndex - startIndex + 1];
                int[] lengths = new int[endIndex - startIndex + 1];
                bool[] staccatos = new bool[endIndex - startIndex + 1];
                int averagePitch = 0;
                int highestPitch = -100;
                int lowestPitch = 100;
                int closestPitch = 100000000;
                bool treble = SongToDraw.GetTracks(trackIndex).GetTreble();
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
                averagePitch /= lengths.Length;
                if (SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(startIndex).GetStart() == SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(endIndex).GetStart() && SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(endIndex).GetLength() < 16)
                {
                    //note is a single and should have a flag
                    //all this has to do is draw a line from the furthest from centre to an octave away from opposite note
                    Line singleLine = new Line
                    {
                        Stroke = black,
                        StrokeThickness = 2
                    };
                    Image flag = new Image
                    {
                        Height = 3 * lineGap
                    };
                    Ellipse staccatoCircle = new Ellipse
                    {
                        Fill = black,
                        Height = lineGap * 0.4
                    };
                    staccatoCircle.Width = staccatoCircle.Height;
                    Canvas.SetLeft(staccatoCircle, barStart + semiquaverwidth * startPoints[0] + semiquaverwidth / 2 - staccatoCircle.Width / 4);
                    if (highestPitch < 3 || Math.Abs(highestPitch - 3) < Math.Abs(lowestPitch - 3))
                    {
                        //stem goes up - draw line on right of notes; draw line from middle of lowest to an octave from highest -- unless lowest is more than an octave from middle line (3)
                        singleLine.X1 = barStart + semiquaverwidth * startPoints[0] + noteHeadWidth + 1 + (semiquaverwidth - noteHeadWidth) / 2;
                        singleLine.X2 = singleLine.X1;
                        singleLine.Y1 = lineStart - (lowestPitch) * lineGap / 2;
                        singleLine.Y2 = (Math.Abs(highestPitch - 3) > 7) ? lineStart - lineGap : lineStart - (highestPitch + 7) * lineGap / 2;
                        Canvas.SetTop(flag, singleLine.Y2);
                        if (lengths[0] < 4)
                        {
                            double flagLen = (Math.Log(lengths[0], 2) % 1 != 0) ? lengths[0] / 1.5 : lengths[0];
                            flag.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\up" + flagLen + "flag.png"), UriKind.Absolute));
                        }
                        if (staccatos[0])
                        {
                            Canvas.SetTop(staccatoCircle, lineStart - (lowestPitch - 1) * lineGap / 2 + lineGap / 4);
                            canvas.Children.Add(staccatoCircle);
                        }
                        lastBeamUp = true;
                    }
                    else
                    {
                        //stem goes down
                        singleLine.X1 = barStart + semiquaverwidth * startPoints[0] + 4 + (semiquaverwidth - noteHeadWidth) / 2;
                        singleLine.X2 = singleLine.X1;
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
                        }
                        lastBeamUp = false;
                    }
                    Canvas.SetLeft(flag, singleLine.X1);
                    canvas.Children.Add(singleLine);
                    if (lengths[0] < 4)
                    {
                        canvas.Children.Add(flag);
                    }
                }
                else if (SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(endIndex).GetLength() < 16)
                {
                    //this is a group of notes and should be treated so
                    //it is ALSO the reason that i killed off multiple melody lines. 

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
                    bool previousNoteWasSemi = false;
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
                            if (lastBeamUp && (i == 0 || startPoints[i - 1] != startPoints[i]))
                            {
                                //draw a staccato below
                                Canvas.SetTop(staccatoCircle, lineStart - (pitches[i] - 1) * lineGap / 2 + lineGap / 4);
                                canvas.Children.Add(staccatoCircle);

                            }
                            if (!lastBeamUp && (i == startPoints.Length - 1 || startPoints[i + 1] != startPoints[i]))
                            {
                                //draw staccato above
                                Canvas.SetTop(staccatoCircle, lineStart - (pitches[i] + 2) * lineGap / 2 - lineGap / 4);
                                canvas.Children.Add(staccatoCircle);
                            }
                        }
                        //add a second line for semiquavers and notes after dots.
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
                        }
                        //handles when the note to the left isn't a semiquaver but neither is the one to the right so it has to go half to the right to 
                        else if (!previousNoteWasSemi && lengths[i] == 1 && startPoints[i + 1] != startPoints[i] && lengths[i + 1] != 1)
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
                        }
                        if (lengths[i] == 0 && i < lengths.Length - 1 && startPoints[i + 1] != startPoints[i])
                        {
                            previousNoteWasSemi = true;
                        }
                        else if (lengths[i] != 0)
                        {
                            previousNoteWasSemi = false;
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
                    }
                }
            }
        }
        //regarding the parameters:
        //canvas so you can add things to the image
        //note so that it can see everything it needs to draw it. tie will be used to draw the lines, but ones that bridge over bars WILL be a problem
        //due to the whole "things go inbetween and it isnt a consistent distance apart" situation
        //barstart gives the x coord to add to to place the note
        //lineStart in this instance... it would be nice to give it as the E4 in treble and G2 in bass. then you can compare.
        public void DrawNote(ref Canvas canvas, int barStart, int lineStart, int trackIndex, int barIndex, int noteIndex, bool drawAccidental, double startDisplacement)
        {
            Note note = SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(noteIndex) as Note;

            int pitch = note.GetPitch() - note.GetAccidental();
            Console.WriteLine(pitch + ", " + note.GetPitch() + ", " + note.GetAccidental());
            int spaceDifferenceFromBottomLine = FindLineDifferenceFromMiddleC(pitch, (SongToDraw.GetTracks(trackIndex).GetTreble()));
            bool drawDot;
            if (Math.Log(note.GetLength(), 2) % 1 != 0)
            {
                drawDot = true;
            }
            else
            {
                drawDot = false;
            }
            if (note.GetTie() != null)
            {
                DrawConnection(ref canvas, note, barIndex, trackIndex, barStart, lineStart);
            }
            Ellipse noteCircle = new Ellipse
            {
                Fill = black,
                Height = lineGap,
                Width = noteHeadWidth
            };
            Canvas.SetLeft(noteCircle, barStart + (semiquaverwidth * (note.GetStart() + startDisplacement)) + (semiquaverwidth - noteHeadWidth) / 2);
            Canvas.SetTop(noteCircle, lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 4);
            noteCircle.RenderTransform = new RotateTransform(-20);
            canvas.Children.Add(noteCircle);
            if (note.GetLength() >= 8 && note.GetLength() < 16)
            {
                Ellipse minimGap = new Ellipse
                {
                    Fill = white,
                    Height = lineGap / 2,
                    Width = noteHeadWidth - 1
                };
                Canvas.SetLeft(minimGap, barStart + (semiquaverwidth * note.GetStart()) + 2 + (semiquaverwidth - noteHeadWidth) / 2);
                Canvas.SetTop(minimGap, lineStart - spaceDifferenceFromBottomLine * lineGap / 2);
                minimGap.RenderTransform = new RotateTransform(-20);
                canvas.Children.Add(minimGap);
            }
            else if (note.GetLength() >= 16)
            {
                noteCircle.RenderTransform = null;
                Canvas.SetTop(noteCircle, lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 2);
                Ellipse semiBreveGap = new Ellipse
                {
                    Fill = white,
                    Height = lineGap - 4,
                    Width = noteHeadWidth / 1.75
                };
                Canvas.SetLeft(semiBreveGap, barStart + (semiquaverwidth * note.GetStart()) + lineGap / 2 + (semiquaverwidth - noteHeadWidth) / 2);
                Canvas.SetTop(semiBreveGap, lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 2);
                semiBreveGap.RenderTransform = new RotateTransform(20);
                canvas.Children.Add(semiBreveGap);
            }
            if (drawDot)
            {
                Ellipse dottedNoteDot = new Ellipse
                {
                    Fill = black,
                    Height = lineGap * 0.4
                };
                dottedNoteDot.Width = dottedNoteDot.Height;
                Canvas.SetLeft(dottedNoteDot, barStart + (semiquaverwidth * (note.GetStart() + 1)) - lineGap / 4);
                Canvas.SetTop(dottedNoteDot, lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 4);
                canvas.Children.Add(dottedNoteDot);
            }
            if (spaceDifferenceFromBottomLine > 7)
            {
                for (int i = lineStart - 4 * lineGap - lineGap / 2; i > lineStart - spaceDifferenceFromBottomLine * lineGap / 2 - lineGap / 2; i -= lineGap)
                {
                    Line staveLine = new Line
                    {
                        Stroke = black,
                        StrokeThickness = staveThickness
                    };
                    int leftShift = 0;
                    if (note.GetLength() >= 16)
                    {
                        leftShift = 3;
                    }
                    staveLine.X1 = barStart + (semiquaverwidth * (note.GetStart() + startDisplacement)) + lineGap / 2 - 10 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    staveLine.X2 = barStart + semiquaverwidth * (note.GetStart() + startDisplacement) + lineGap / 2 + noteHeadWidth - 3 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    staveLine.Y1 = i;
                    staveLine.Y2 = i;
                    canvas.Children.Add(staveLine);
                }
            }
            else if (spaceDifferenceFromBottomLine < -1)
            {
                for (int i = lineStart + lineGap + lineGap / 2; i < lineStart - spaceDifferenceFromBottomLine * lineGap / 2 + lineGap / 2; i += lineGap)
                {
                    Line staveLine = new Line
                    {
                        Stroke = black,
                        StrokeThickness = staveThickness
                    };
                    int leftShift = 0;
                    if (note.GetLength() >= 16)
                    {
                        leftShift = 3;
                    }
                    staveLine.X1 = barStart + (semiquaverwidth * (note.GetStart() + startDisplacement)) + lineGap / 2 - 10 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    staveLine.X2 = barStart + semiquaverwidth * (note.GetStart() + startDisplacement) + lineGap / 2 + noteHeadWidth - 3 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    staveLine.Y1 = i;
                    staveLine.Y2 = i;
                    canvas.Children.Add(staveLine);
                }
            }
            if (drawAccidental)
            {
                Image accidentalImage = new Image
                {
                    Height = lineGap * 3
                };
                if (note.GetAccidental() == -1)
                {
                    accidentalImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\Flat.png"), UriKind.Absolute));
                    Canvas.SetTop(accidentalImage, lineStart - (spaceDifferenceFromBottomLine + 3) * lineGap / 2 - lineGap / 4);
                    accidentalImage.Height = lineGap * 2.5;
                }
                else if (note.GetAccidental() == 0)
                {
                    accidentalImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\Natural.png"), UriKind.Absolute));
                    Canvas.SetTop(accidentalImage, lineStart - (spaceDifferenceFromBottomLine + 2) * lineGap / 2 - lineGap / 2.3);
                }
                else
                {
                    accidentalImage.Source = new BitmapImage(new Uri(string.Concat(exePath, "\\source\\Sharp.png"), UriKind.Absolute));
                    Canvas.SetTop(accidentalImage, lineStart - (spaceDifferenceFromBottomLine + 3) * lineGap / 2);
                }
                Canvas.SetLeft(accidentalImage, barStart + (semiquaverwidth) * (note.GetStart() + startDisplacement));
                canvas.Children.Add(accidentalImage);
            }
        }

        private void DrawConnection(ref Canvas canvas, Note note, int bar, int track, int barStart, int lineStart)
        {
            Note tieNote = note.GetTie() as Note;
            ArcSegment blackCurve = new ArcSegment();
            PathFigure connectionPathFigure = new PathFigure();
            PathSegmentCollection connectionPathSegmentCollection = new PathSegmentCollection();
            PathFigureCollection connectionPathFigureCollection = new PathFigureCollection();
            PathGeometry connectionPathGeometry = new PathGeometry();
            Path connectionPath = new Path();
            double noteY = lineStart - FindLineDifferenceFromMiddleC(note.GetPitch() - note.GetAccidental(), (SongToDraw.GetTracks(track).GetTreble())) * lineGap / 2;
            double tieY = lineStart - FindLineDifferenceFromMiddleC(tieNote.GetPitch() - tieNote.GetAccidental(), (SongToDraw.GetTracks(track).GetTreble())) * lineGap / 2;
            if (-1 * (noteY - lineStart) < 4 * lineGap / 2)
            {
                noteY += lineGap * 0.75;
                tieY += lineGap * 0.75;
                blackCurve.SweepDirection = SweepDirection.Counterclockwise;
            }
            else
            {
                noteY -= lineGap * 0.75;
                tieY -= lineGap * 0.75;
                blackCurve.SweepDirection = SweepDirection.Clockwise;
            }
            connectionPathFigure.StartPoint = new Point(barStart + (note.GetStart()) * semiquaverwidth + semiquaverwidth / 2 + 10, noteY);
            if (tieNote.GetStart() <= note.GetStart())
            {
                if (barStarts[bar] >= barStarts[bar + 1])
                {
                    blackCurve.Point = new Point(barStart + (SongToDraw.GetTracks(0).GetBars(bar).GetMaxLength() + 1) * semiquaverwidth - 9, (noteY + tieY) / 2);
                }
                else
                {
                    blackCurve.Point = new Point(barStarts[bar + 1] * semiquaverwidth + reservedForTrackTitles + semiquaverwidth / 2 - 9, tieY);
                }
            }
            else
            {
                blackCurve.Point = new Point(barStart + tieNote.GetStart() * semiquaverwidth + semiquaverwidth / 2 - 9, tieY);
            }
            blackCurve.Size = new Size(Math.Abs(blackCurve.Point.X - connectionPathFigure.StartPoint.X), 70);
            if (-1 * (noteY - lineStart) < 4 * lineGap / 2)
            {
                blackCurve.SweepDirection = SweepDirection.Counterclockwise;
            }
            else
            {
                blackCurve.SweepDirection = SweepDirection.Clockwise;
            }
            connectionPathSegmentCollection.Add(blackCurve);
            connectionPathFigure.Segments = connectionPathSegmentCollection;
            connectionPathFigureCollection.Add(connectionPathFigure);
            connectionPathGeometry.Figures = connectionPathFigureCollection;
            connectionPath.Stroke = black;
            connectionPath.StrokeThickness = 3;
            connectionPath.Data = connectionPathGeometry;
            canvas.Children.Add(connectionPath);
        }

        public void DrawRest(ref Canvas canvas, int barStart, int lineStart, int trackIndex, int barIndex, int noteIndex)
        {
            Rest rest = SongToDraw.GetTracks(trackIndex).GetBars(barIndex).GetNotes(noteIndex) as Rest;
            Image restImage = new Image();
            Rectangle restRectangle = new Rectangle
            {
                Height = lineGap / 2,
                Width = semiquaverwidth / 1.5
            };
            switch (rest.GetLength())
            {
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
                case 8:
                    Canvas.SetTop(restRectangle, lineStart - lineGap * 2);
                    restRectangle.Fill = black;
                    break;
                case 16:
                    Canvas.SetTop(restRectangle, lineStart - lineGap * 1.5);
                    restRectangle.Fill = black;
                    break;
            }
            Canvas.SetLeft(restImage, barStart + (semiquaverwidth * rest.GetStart()) + (semiquaverwidth - noteHeadWidth) / 2);
            Canvas.SetLeft(restRectangle, barStart + (semiquaverwidth * rest.GetStart()) + (semiquaverwidth - noteHeadWidth) / 3);
            canvas.Children.Add(restImage);
            canvas.Children.Add(restRectangle);
        }

        public int FindLineDifferenceFromMiddleC(int pitch, bool treble)
        {
            int distanceFromC = pitch - 40;
            int lineDiff;
            int mod12 = distanceFromC % 12;
            if (mod12 < 0)
            {
                mod12 += 12;
            }

            lineDiff = 7 * (int)Math.Round((distanceFromC - (mod12)) / 12.0) + pitchToNote[mod12];
            lineDiff = (treble) ? lineDiff - 3 : lineDiff + 9;
            return lineDiff;
        }
        #endregion

        #region scaling
        public void Zoom(Canvas canvas, int chosenWidth)
        {
            canvas.Width = chosenWidth;
            double newHeight = ((double)chosenWidth / canvasWidth) * canvasHeight;
            canvas.Height = newHeight;
            canvas.RenderTransform = new ScaleTransform((double)chosenWidth / canvasWidth, (double)chosenWidth / canvasWidth);
            Rectangle rectangle = new Rectangle
            {
                Fill = white,
                Height = canvasHeight,
                Width = canvasWidth
            };
            Canvas.SetZIndex(rectangle, -1);
            canvas.Children.Add(rectangle);
        }
        #endregion

        #region finding the mouse
        public bool FindMouseLeft(ref Canvas canvas, ref int trackIndex, ref int barIndex, ref int notePos, ref int pitch, int length, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(canvas);
            bool worked = WhereAmI(ref trackIndex, ref barIndex, ref notePos, ref pitch, length, position);
            return worked;
        }

        public bool FindMouseRight(ref Canvas canvas, ref int trackIndex, ref int barIndex, ref int notePos, ref int pitch, int length, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(canvas);
            bool worked = WhereAmI(ref trackIndex, ref barIndex, ref notePos, ref pitch, length, position);
            return worked;
        }

        public bool FindMouse(ref Canvas canvas, ref int trackIndex, ref int barIndex, ref int notePos, ref int pitch, int length, MouseEventArgs e)
        {
            var position = e.GetPosition(canvas);
            bool worked = WhereAmI(ref trackIndex, ref barIndex, ref notePos, ref pitch, length, position);
            return worked;
        }
        public bool WhereAmI(ref int trackIndex, ref int barIndex, ref int notePos, ref int pitch, int length, Point position)
        {
            //if statement to make sure notes can't be placed in the space at top of page, and extra linegap / 4 to make sure placement rules are consistent with
            //the other lines.
            if (position.Y < extraHeight + lineGap / 4)
            {
                return false;
            }
            int line = (int)((position.Y - extraHeight) / lineHeight);
            trackIndex = line % totalInstruments;
            int lineIndex = (line - trackIndex) / totalInstruments;
            if (lineIndex > barsPerLine.Count)
            {
                return false;
            }
            int barsBefore = 0;
            for (int i = 0; i < lineIndex; i++)
            {
                barsBefore += barsPerLine[i];
            }
            double xSemiPos = (position.X - reservedForTrackTitles) / semiquaverwidth;
            bool barFound = false;
            for (int i = barsPerLine[lineIndex] - 1; i >= 0; i--)
            {
                if (xSemiPos >= barStarts[barsBefore + i])
                {
                    if (xSemiPos - barStarts[barsBefore + i] > SongToDraw.GetTracks(trackIndex).GetBars(barsBefore + i).GetMaxLength())
                    {
                        return false;
                    }
                    barIndex = barsBefore + i;
                    barFound = true;
                    break;
                }
            }
            if (!barFound)
            {
                return false;
            }
            notePos = (int)Math.Floor(xSemiPos - barStarts[barIndex]);
            pitch = ReverseFindLineDifferenceFromMiddleC((int)position.Y - (lineStarts[lineIndex] + (lineHeight * (trackIndex)) + ((maxLinesAbove + 4) * lineGap)), SongToDraw.GetTracks(trackIndex).GetTreble());
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
            Preview.Fill = previewBrush;
            Canvas.SetLeft(Preview, barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + (semiquaverwidth * (notePos)) + (semiquaverwidth - noteHeadWidth) / 2);
            int previewPitch = FindLineDifferenceFromMiddleC(pitch, SongToDraw.GetTracks(trackIndex).GetTreble());
            if (previewPitch > 7)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (previewPitch > 8 + 2 * i)
                    {
                        previewLines[i].Visibility = Visibility.Visible;
                    }
                    else
                    {
                        previewLines[i].Visibility = Visibility.Hidden;
                    }
                    previewLines[i].Stroke = black;
                    previewLines[i].StrokeThickness = staveThickness;
                    int leftShift = 0;
                    if (length >= 16)
                    {
                        leftShift = 3;
                    }
                    previewLines[i].X1 = barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + (semiquaverwidth * notePos) + lineGap / 2 - 10 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    previewLines[i].X2 = barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + semiquaverwidth * (notePos) + lineGap / 2 + noteHeadWidth - 3 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    previewLines[i].Y1 = (lineStarts[lineIndex] + (lineHeight * (trackIndex)) + ((maxLinesAbove) * lineGap)) - lineGap / 2 - i * lineGap;
                    previewLines[i].Y2 = previewLines[i].Y1;

                }
            }
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
                    int leftShift = 0;
                    if (length >= 16)
                    {
                        leftShift = 3;
                    }
                    previewLines[i].X1 = barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + (semiquaverwidth * notePos) + lineGap / 2 - 10 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    previewLines[i].X2 = barStarts[barIndex] * semiquaverwidth + reservedForTrackTitles + semiquaverwidth * (notePos) + lineGap / 2 + noteHeadWidth - 3 - leftShift + (semiquaverwidth - noteHeadWidth) / 2;
                    previewLines[i].Y1 = lineStarts[lineIndex] + (lineHeight * (trackIndex)) + ((maxLinesAbove + 5.5) * lineGap) + i * lineGap;
                    previewLines[i].Y2 = previewLines[i].Y1;
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    previewLines[i].Visibility = Visibility.Hidden;
                }
            }
            Canvas.SetTop(Preview, (lineStarts[lineIndex] + (lineHeight * (trackIndex)) + ((maxLinesAbove + 4) * lineGap)) - previewPitch * lineGap / 2 - lineGap / 4);
            Preview.RenderTransform = new RotateTransform(-20);
            Preview.Visibility = Visibility.Visible;
            return true;
        }

        public void MakePreviewInvisible()
        {
            foreach (Line line in previewLines)
            {
                line.Visibility = Visibility.Hidden;
            }
            Preview.Visibility = Visibility.Hidden;
        }

        public int ReverseFindLineDifferenceFromMiddleC(double lineDiff, bool treble)
        {
            lineDiff = (treble) ? -2 * lineDiff / lineGap + 3 : -2 * lineDiff / lineGap - 9;
            double mod7 = lineDiff % 7;
            if (mod7 < 0)
            {
                mod7 += 7;
            }
            int mod12 = pitchToNote.First(x => x.Value == (int)mod7).Key;
            if (lineDiff < 0)
            {
                mod12 -= 12;
            }
            return (int)((lineDiff - (lineDiff % 7)) / 7) * 12 + mod12 + 40;
        }

        public int GetTitleHeight(ref Canvas canvas)
        {
            return (int)(extraHeight * canvas.Width / canvasWidth);
        }
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
