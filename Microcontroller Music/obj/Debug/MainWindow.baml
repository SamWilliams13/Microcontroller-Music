   M S B A M L   `   `   `  ����P  LMicrocontroller Music, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null=#clr-namespace:Microcontroller_MusicMicrocontroller_Music  5   	   &     Microcontroller_Music.MainWindow   5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   5local#clr-namespace:Microcontroller_Music     5   	   -   5          � Title$  Microcontroller Music��5	   	   $	��600��5
      $
��1000��6    � 
ResizeMode$ 	CanResize=�5       � WindowState$ 	Maximized=�5      .��5      � -   5         �Name$ 
WindowGrid��5      
������� � RowDefinitions 5   
   �� 5      -   5         ��Name$ 
MenuHeight��5      $��25 �61   6?   �� 5      -   5        $ ScrollHeight��5      64   �� 5      -   5        $ StatusHeight��5      $��30 �62    � ColumnDefinitions 5   
   �� 5      -   5         ��Name$ ScrollWidth��5      �� 5      -   5        $ ToolbarWidth��5      $��80 �65   .+�5    
   �� -   5         ��Name$ 
MainScroll��5    $   $��1��6   $��Stretch=�68   $	��Top=�6V   $
��Auto=�6n   $
��Auto=�6�   .��5#      �� -	   5        	 ��Name$	 
SheetMusic��5#      $��Center=�5$      ���#FFFFFE$
��1800��5&      5*      5+      �� 5-   
   -
   5        
 ��Name$
 	statusBar��5-   !   $��2��6   $��Stretch=�64   $��Bottom=�6R   $��2��6m   .@�5/      �� -   5         ��Name$ 
statusItem��5/      50      51      o� 54   
   -   5         o�Name$ TopBar��54      $��0��6   $��0��6,   $��2��6<   .@�56      m� $��_File��6   .@�58      m� -   5        $��_New...��58      6,   m� 59      -   5        $��_Open...��59      6-   �� 5:      m� 5;      -   5        $��_Save...��5;      6-   m� 5<      -   5        $��_Save As...��5<      60   5=      m� 5>      $��_Song��6   .@�5@      m� -   5         m�Name$ AddTrack��5@      $��	Add Track��6-   6@   m� 5A      -   5        $	 Bpm��5A      $��BPM: 120��6)   6;   m� 5B      -   5        $ ChangeTitle��5B      $��Change Title��61   6G   5C      m� 5E      $��_Export��6   .@�5F      m� -   5        $��Bitmap��5F      6+   �� 5G      m� 5H      -   5        $ BBCMPOut��5H      $��Micro:Bit - MicroPython��6-   
 � Tag$ 0��6c   m� 5I      -   5        $ 
ArduinoOut��5I      $��Arduino Uno - C++��6/   $ 1��6_   m� 5J      -   5        $ PicoOut��5J      $%��Raspberry Pi Pico - MicroPython��6,   $ 2��6j   5K      �� 5M      -   5         ��Name$
 Zoom��5M       �� Orientation$ 
Horizontal=�6#    � Value$
 1800Q�6<    � Maximum$
 3600Q�6I    � Minimum$	 100Q�6X   $	��100��6�   �� 5O      -   5         ��Name$ Play_Button��5O      $
��Play��6*   69   5P      �� 5S   
   -   5        $ ToolBoxScroll��5S      $��1��6.   $��1��6>   $
��Auto=�6K   .��5U      �� -   5         ��Name$ ToolBox��5U      $��1��6*   $��1��6:   $��2��6G   .+�5V      	� -   5         	� 	GroupName$ NoteType��5V      #��[Bd�63   $ 
Semiquaver��6�    � ToolTip$ 
Semiquaver��6�   .��5W      �� $8��2pack://siteoforigin:,,,/source/semiquaver note.png��6   $��Uniform=�6X   5X      	� 5Y      -   5        $ NoteType��5Y      #��[Bd�63   $ Quaver��6�   $ Quaver��6�    d� 	IsChecked$
 TrueY�6�   .��5Z      �� $4��.pack://siteoforigin:,,,/source/quaver note.png��6   $��Uniform=�6T   5[      	� 5\      -   5        $ NoteType��5\      #��[Bd�63   $ Crotchet��6�   $ Crotchet��6�   .��5]      �� $6��0pack://siteoforigin:,,,/source/crotchet note.png��6   $��Uniform=�6V   5^      	� 5_      -   5        $ NoteType��5_      #��[Bd�63   $ Minim��6�   $ Minim��6�   .��5`      �� $3��-pack://siteoforigin:,,,/source/minim note.png��6   $��Uniform=�6S   5a      	� 5b      -    5        $ NoteType��5b      #��[Bd�64   $ 	Semibreve��6�   $ 	Semibreve��6�   .��5c      �� $7��1pack://siteoforigin:,,,/source/semibreve note.png��6   $��Uniform=�6W   5d      	� 5e      -!   5        $ NoteType��5e      #��[Bd�63   $ Dotted Quaver��6�   $ Dotted Quaver��6�   .��5f      �� $6��0pack://siteoforigin:,,,/source/dotted quaver.png��6   $��Uniform=�6V   5g      	� 5h      -"   5        $ NoteType��5h      #��[Bd�63   $ Dotted Crotchet��6�   $ Dotted Crotchet��6�   .��5i      �� $8��2pack://siteoforigin:,,,/source/dotted crotchet.png��6   $��Uniform=�6X   5j      	� 5k      -#   5        $ NoteType��5k      #��[Bd�63   $ Dotted Minim��6�   $ Dotted Minim��6�   .��5l      �� $5��/pack://siteoforigin:,,,/source/dotted minim.png��6   $��Uniform=�6U   5m      5n      5o      5p      5q      