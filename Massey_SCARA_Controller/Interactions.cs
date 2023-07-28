using Massey_SCARA_Controller.Properties;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Massey_SCARA_Controller
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

    #region Movement handlers

    private void btn_Wait_Click(object sender, RoutedEventArgs e)
    {
      if (Validate(txt_Wait.Text, 1, 10000, out int hello))
      {
        PORT_SCARA_Send($"WAIT,{hello}");
      }
    }

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

    #region Script functionality
    private void list_Sequence_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      
    }

    private bool Recording = false;
    private void btn_RecordScript_Click(object sender, RoutedEventArgs e)
    {
      if (btn_RecordScript.Content.ToString().Contains("Record"))
      {
        btn_RecordScript.Content = "Stop";
        Recording = true;
      }
      else
      {
        btn_RecordScript.Content = "Record";
        Recording = false;
        scriptHandler.Export();
        UpdateScriptPanel();
      }
    }

    private void btn_ClearScript_Click(object sender, RoutedEventArgs e)
    {
      scriptHandler.List.Clear();
      UpdateScriptPanel();
    }


    private void btn_RunScript_Click(object sender, RoutedEventArgs e)
    {
      _ = SendCommandList(scriptHandler.List);
    }
    private void btn_SelectScript_Click(object sender, RoutedEventArgs e)
    {
      scriptHandler.SelectFile();
      UpdateScriptPanel();
    }

    private void list_Sequence_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
    }

    private void btn_NewScript_Click(object sender, RoutedEventArgs e)
    {
      scriptHandler.MakeNew(txt_NewScript.Text);
      UpdateScriptPanel();
    }


    #endregion
    #region Conveyor Belt
    private void btn_BeltRev_Click(object sender, RoutedEventArgs e)
    {
        PORT_BELT_Send($"{Settings.Default.conv_For},{Settings.Default.conv_Dist}");
    }

    private void btn_BeltFor_Click(object sender, RoutedEventArgs e)
    {
        PORT_BELT_Send($"{Settings.Default.conv_Rev},{Settings.Default.conv_Dist}");
    }
        #endregion
    }
}
