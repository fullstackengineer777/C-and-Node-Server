   M S B A M L   `   `   `  ����N  JXtrem_Scale_Example, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullG(clr-namespace:Xtrem_Scale_Example_SerialXtrem_Scale_Example_Serial  5   	   +    %Xtrem_Scale_Example_Serial.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   :local(clr-namespace:Xtrem_Scale_Example_Serial     5   	     � Title$  GRAM Xtrem scale example��5   	   $	��450��6*   $	��800��67    � 	Resources 5
      1   +Xtrem_Scale_Example_Serial.BoolToVisibility   BoolToVisibility�� 5   
   %   &	          5      6%   5      .��5      �  � ColumnDefinitions 5   
   �� 5      $��* �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $��* �6   �� 5      $��* �6   �� 5      $��* �6   �� 5      $��* �6   .+�5    
   �� $��0��6   $��0��6   $��2��6-   $��24(�6B   $��Center=�6P   $��Center=�6k   .��5!      �  5"      �� 5#      $	��200 �6'   �� 5$      $	��200 �6'   �� 5%      $	��200 �6'    5(      �� 5)      $��* �6$   �� 5*      $��* �6$   .+�5-      �� $
��Brut��6   $��0��6'   $��0��64   �� 5.      $��1��5/      $��0��6"   ��50      �� *Scale.W_Brut+ �� UpdateSourceTrigger$ PropertyChanged=��� 53      $
��Tare��6   $��0��6'   $��1��64   �� 54      $��1��55      $��1��6"   ��56      �� *Scale.W_Tare+$ PropertyChanged=��� 59      $	��Net��6   $��0��6&   $��2��63   �� 5:      $��1��5;      $��2��6"   ��5<      �� *Scale.W_Net+$ PropertyChanged=�5?      5@      �� 5C   
   -   5         ��Name$ Weight��5C      $��1q�5D      
�������$��1��5E      $��2��6   $��2��5F      $��Center=�5G      $��Right=�5H      $��64(�5I      $
��Auto��5J      $	��600��6   ��5L      �� *Scale.W_Display+$ PropertyChanged=��� 5P   
   -   5        $��3��5Q      $��0��6   $
��Zero��5R      $	��300��5T      $��Right=�5U      $��24(�5V      �� 5Z   
   -   5        $��3��5[      $��1��6   $
��Tare��5\      $	��300��5^      $
��Left=�5_      $��24(�5`      �� 5d   
   $��4��5e      $��0��6   $��2��6*   $
��Auto��5f      $��Center=�5g      $��Center=�5h      $��Center=�5i      $��16(�5k      $4��.Error: not connected or COM port not available��5m      y�5n      �� *Scale.IsNotConnected+ �� 	Converter# [  $ PropertyChanged=��� 5q   
   $��4��5r      $��0��6   $��2��6*   $
��Auto��5s      $��Center=�5t      $��Center=�5u      $��Center=�5v      $��16(�5x      ��5z      �� *Scale.DefinitionString+$ PropertyChanged=�5      5�      