using SCARA_GUI.Properties;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SCARA_GUI
{
    public partial class MainWindow : Window
    {
        #region Window and Menu
        private void WindowResized(object sender, EventArgs e) { Ui_UpdateFontSize(); }

        private void menu_OpenFile_Clicked(object sender, EventArgs args) { OpenLogFile(); }

        private void menu_Vis_Clicked(object sender, EventArgs e) { Ui_UpdateFontSize(); }

        private void menu_Out_Clicked(object sender, EventArgs e) { text_OuputLog.Text = string.Empty; }

        // Show the advaced settings window
        private void menu_Advanced_Clicked(object sender, EventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Closing += SettingsWindow_Closing;
            settingsWindow.ShowDialog();
            
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            pose.LimitsUpdated();
            Ui_UpdateMoveParams();
            Ui_UpdateFontSize();
        }

        private void menu_Help_Clicked(object sender, RoutedEventArgs e) { OpenHelpFile(); }
        
        // Log box double clicked
        private void LogBox_DoubleClicked(object sender, MouseButtonEventArgs e) { OpenLogFile(); }
        #endregion

        #region Main Buttons
        // If not connected, connect, otherwise disconnect
        private void btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!SERIALPORT_SCARA.IsOpen) ScanAndConnect();
            else Disconnect();
        }

        private void btn_Stop_Click(object sender, RoutedEventArgs e) 
        { 
            Disconnect();
        }
        
        // Validate the inputs and send MOVE command
        private void btn_Move_Click(object sender, RoutedEventArgs e)
        {
            bool valid = 
                pose.Nudge(Pose.Axis.W, txt_MoveW.Text) &&
                pose.Nudge(Pose.Axis.X, txt_MoveX.Text) &&
                pose.Nudge(Pose.Axis.Y, txt_MoveY.Text);

            if (valid) MovementHandler();
            else LogMessage("Invlid MOVE parameters", MsgType.ALT);
        }

        // Piston cmd
        private void btn_Piston_Click(object sender, RoutedEventArgs e)
        {
            if (btn_Piston.Content.ToString().Contains("UP"))
            {
                btn_Piston.Content = "DOWN";
                PORT_SCARA_Send($"AIR,{Settings.Default.air_UP},{Settings.Default.air_DELAY_P}");
            }
            else
            {
                btn_Piston.Content = "UP";
                PORT_SCARA_Send($"AIR,{Settings.Default.air_DOWN},{Settings.Default.air_DELAY_P}");
            }
        }

        // Gripper cmd
        private void btn_Gripper_Click(object sender, RoutedEventArgs e)
        {
            if (btn_Gripper.Content.ToString().Contains("OPEN"))
            {
                btn_Gripper.Content = "CLOSE";
                PORT_SCARA_Send($"AIR,{Settings.Default.air_OPEN},{Settings.Default.air_DELAY_G}");
            }
            else
            {
                btn_Gripper.Content = "OPEN";
                PORT_SCARA_Send($"AIR,{Settings.Default.air_CLOSE},{Settings.Default.air_DELAY_G}");
            }
        }
        
        // Home button
        private void btn_Home_Click(object sender, RoutedEventArgs e)
        {
            PORT_SCARA_Send("HOME");
            pose.Home();
            Ui_UpdateMoveParams();
        }
        #endregion

        #region Extra functions
        // Extra functions
        private void btn_Wait_Click(object sender, RoutedEventArgs e) { PORT_SCARA_Send($"WAIT,{sld_Wait.Value}"); }

        private void btn_ID_Click(object sender, RoutedEventArgs e) { PORT_SCARA_Send($"ID");}

        private void btn_SOffset_Click(object sender, RoutedEventArgs e) { PORT_SCARA_Send($"SOFFSET,{sld_SOffset.Value}"); }

        private void btn_ROffset_Click(object sender, RoutedEventArgs e) { PORT_SCARA_Send($"ROFFSET"); }

        private void btn_Prox_Click(object sender, RoutedEventArgs e) { PORT_SCARA_Send($"PROX"); }

        private void btn_AccelSet_Click(object sender, RoutedEventArgs args) { DoSpeedSet(); }
        private void btn_SpeedSet_Click(object sender, RoutedEventArgs args) { DoSpeedSet(); }
        private void DoSpeedSet()
        {
            if (Validate(txt_AccelSet.Text, 0, 100, out int acc))
            {
                if (Validate(txt_SpeedSet.Text, 0, 100, out int vel))
                {
                    PORT_SCARA_Send($"SPEEDSET,{vel},{acc}");
                }
            }
        }

        private void sld_Wait_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) { txt_Wait.Text = sld_Wait.Value.ToString(); }
        private void sld_Wait_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) { txt_Wait.Text = sld_Wait.Value.ToString(); }
        private void txt_Wait_LostFocus(object sender, RoutedEventArgs e) { if (Int32.TryParse(txt_Wait.Text, out int i)) sld_Wait.Value = i; }

        private void sld_SpeedSet_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) { txt_SpeedSet.Text = sld_SpeedSet.Value.ToString(); }
        private void sld_SpeedSet_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) { txt_SpeedSet.Text = sld_SpeedSet.Value.ToString(); }
        private void txt_SpeedSet_LostFocus(object sender, RoutedEventArgs e) { if (Int32.TryParse(txt_SpeedSet.Text, out int i)) sld_SpeedSet.Value = i; }

        private void sld_AccelSet_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) { txt_AccelSet.Text = sld_AccelSet.Value.ToString(); }
        private void sld_AccelSet_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) { txt_AccelSet.Text = sld_AccelSet.Value.ToString(); }
        private void txt_AccelSet_LostFocus(object sender, RoutedEventArgs e) { if (Int32.TryParse(txt_AccelSet.Text, out int i)) sld_AccelSet.Value = i; }

        private void sld_SOffset_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) { txt_SOffset.Text = sld_SOffset.Value.ToString(); }
        private void sld_SOffset_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) { txt_SOffset.Text = sld_SOffset.Value.ToString(); }
        private void txt_SOffset_LostFocus(object sender, RoutedEventArgs e) { if (Int32.TryParse(txt_SOffset.Text, out int i)) sld_SOffset.Value = i; }
        
        #endregion

        #region Movement handlers

        // Jog buttons
        private void btn_JogIncW_Click(object sender, RoutedEventArgs e) { MovementHandler(Pose.Axis.W, +1); }
        private void btn_JogIncX_Click(object sender, RoutedEventArgs e) { MovementHandler(Pose.Axis.X, +1); }
        private void btn_JogIncY_Click(object sender, RoutedEventArgs e) { MovementHandler(Pose.Axis.Y, +1); }
        private void btn_JogDecW_Click(object sender, RoutedEventArgs e) { MovementHandler(Pose.Axis.W, -1); }
        private void btn_JogDecX_Click(object sender, RoutedEventArgs e) { MovementHandler(Pose.Axis.X, -1); }
        private void btn_JogDecY_Click(object sender, RoutedEventArgs e) { MovementHandler(Pose.Axis.Y, -1); }


        // Sliders
        private void sld_MoveW_Dragging(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            txt_MoveW.Text = sld_MoveW.Value.ToString();
        }
        private void sld_MoveX_Dragging(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            txt_MoveX.Text = sld_MoveX.Value.ToString();
        }
        private void sld_MoveY_Dragging(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            txt_MoveY.Text = sld_MoveY.Value.ToString();
        }

        private void sld_MoveW_Release(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            MovementHandler(Pose.Axis.W, 0, (int)sld_MoveW.Value);
        }
        private void sld_MoveX_Release(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            MovementHandler(Pose.Axis.X, 0, (int)sld_MoveX.Value);
        }
        private void sld_MoveY_Release(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            MovementHandler(Pose.Axis.Y, 0, (int)sld_MoveY.Value);
        }
        #endregion
    }
}
