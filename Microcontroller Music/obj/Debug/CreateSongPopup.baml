   M S B A M L   `   `   `  ����P  LMicrocontroller Music, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null=#clr-namespace:Microcontroller_MusicMicrocontroller_Music  5   	   +    %Microcontroller_Music.CreateSongPopup   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   5local#clr-namespace:Microcontroller_Music     5   	     � Title$  CreateSongPopup��5   	   $��187.297��6!   $��618.578��62   .��5	      � $	��167��6   $	��Top=�6   .+�5   
   �� -   5         ��Name$ label��5      $��Title��6   $
��Left=�6/   $��	10,10,0,0q�6J   $	��Top=�6]    J� RenderTransformOrigin$ -0.342,-0.379&�6u   �� 5   
   -   5        $ label1��5      $��Key Signature:��6    $
��Left=�69   $��	10,41,0,0q�6T   $	��Top=�6g   �� 5   
   -   5        $ label1_Copy1��5      $��Time Signature:��6&   $
��Left=�6@   $��
10,104,0,0q�6[   $	��Top=�6o   �� 5   
   -   5        $ label1_Copy��5      $��First Track Name:��6%   $
��Left=�6A   $��
308,51,0,0q�6\   $	��Top=�6p   �� 5   
   -   5        $ label1_Copy2��5      $��First Track Key:��6&   $
��Left=�6A   $��
308,77,0,0q�6\   $	��Top=�6p   $��27��6�   �� 5   
   -   5         ��Name$ 	SongTitle��5      $
��Left=�6%   $��23��6@   $��
114,13,0,0q�6L    �� TextWrapping$
 Wrap=�6`   $��Song 1��6t   $	��Top=�6�   $	��487��6�   �� 5   
   -   5        $ 
TrackTitle��5      $
��Left=�6&   $��23��6A   $��
413,54,0,0q�6M   $
 Wrap=�6a   $��Track1��6u   $	��Top=�6�   $	��188��6�   �� 5   
   -   5         ��Name$	 Key��5      $
��Left=�6    $��
413,82,0,0q�6;   $	��Top=�6O   $	��107��6g    �� SelectedIndex$ 0��6s   .@�5      �� $��Treble��6   �� 5      $
��Bass��6   5      �� 5   
   -	   5        $ 
TimeSigTop��5      $
��Left=�6'   $��114,101,0,0q�6B   $	��Top=�6W   $	��120��6o   $ 0.494,-0.088&�6{   $��23��6�   $ 2��6�   .@�5       �� $��2��6   �� 5!      $��3��6   �� 5"      $��4��6   5#      �� 5%   
   -
   5        $ TimeSigBottom��5%      $
��Left=�6*   $��114,124,0,0q�6E   $	��Top=�6Z   $	��120��6r   $ 1��6~   .@�5&      �� $��2��6   �� 5'      $��4��6   �� 5(      $��8��6   5)      �� 5+   
   -   5        $ KeySig��5+      $
��Left=�6#   $��
114,41,0,0q�6>   $	��Top=�6R   $	��163��6j   $ 7��6v   .@�5,      �� $��Cb Major (7b)��6   �� 5-      $��Gb Major / Eb Minor (6b)��6   �� 5.      $��Db Major / Bb Minor (5b)��6   �� 5/      $��Ab Major / F Minor (4b)��6   �� 50      $��Eb Major / C Minor (3b)��6   �� 51      $��Bb Major / G Minor (2b)��6   �� 52      $��F Major / D Minor (1b)��6   �� 53      $��C Major / A Minor��6   �� 54      $��G Major / E Minor (1#)��6   �� 55      $��D Major / B Minor (2#)��6   �� 56      $��A Major / F# Minor (3#)��6   �� 57      $��E Major / C# Minor (4#)��6   �� 58      $��B Major / G# Minor (5#)��6   �� 59      $��F# Major / D# Minor (6#)��6   �� 5:      $��C# Major (7#)��6   5;      �� 5=   
   -   5         ��Name$ OKButton��5=      $��OK��6#   $
��Left=�60   $��501,124,0,0q�6K   $	��Top=�6`   $��75��6x   6�   �� 5?   
   -   5        $ Tempo��5?      $
��Left=�6!   $��
114,68,0,0q�6<   $	��Top=�6P   $	��143��6h   $	��120��6t   �� 5A   
   -   5        $ label1_Copy3��5A      $��Tempo:��6&   $
��Left=�67   $��	10,67,0,0q�6R   $	��Top=�6e   5C      5D      