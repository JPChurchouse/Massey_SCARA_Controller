using SCARA_GUI.Properties;
using System;
using System.Windows;

namespace SCARA_GUI
{
  /// <summary>
  /// Interaction logic for SettingsWindow.xaml
  /// </summary>
  public partial class SettingsWindow : Window
  {
    private bool unsaved = false;
    public SettingsWindow()
    {
      InitializeComponent();
      SettingsToUi();
    }

    private void Any_Changed(object sender, EventArgs e)
    {
      unsaved = true;
      btn_Confirm.IsEnabled = unsaved;
    }

    private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      ValidateAll();
    }
    private void Confirm_Click(object sender, EventArgs e)
    {
      ValidateAll();
      this.Close();
    }

    private void ValidateAll()
    {
      if (!unsaved) return;

      int changes = 0;

      // Piston
      //txt_PistonInactive.Text = Settings.Default.air_UP;
      //txt_PistonActive.Text = Settings.Default.air_DOWN;
      
      if (!Validate(txt_PistonDelay.Text, 0, 10000))
      {
        txt_PistonDelay.Text = Settings.Default.air_DELAY_P.ToString();
        changes++;
      }

      // Gripper
      //txt_GripperInactive.Text = Settings.Default.air_OPEN;
      //txt_GripperActive.Text = Settings.Default.air_CLOSE;

      if (!Validate(txt_GripperDelay.Text, 0, 10000))
      {
        txt_GripperDelay.Text = Settings.Default.air_DELAY_G.ToString();
        changes++;
      }

      // Maxes
      if (!Validate(txt_MaxW.Text, 0, 200))
      {
        txt_MaxW.Text = Settings.Default.max_W.ToString();
        changes++;
      }

      if (!Validate(txt_MaxX.Text, 0, 200))
      {
        txt_MaxX.Text = Settings.Default.max_X.ToString();
        changes++;
      }

      if (!Validate(txt_MaxY.Text, 0, 200))
      {
        txt_MaxY.Text = Settings.Default.max_Y.ToString();
        changes++;
      }

      // Mins
      if (!Validate(txt_MinW.Text, -200, 0))
      {
        txt_MinW.Text = Settings.Default.min_W.ToString();
        changes++;
      }

      if (!Validate(txt_MinX.Text, -200, 0))
      {
        txt_MinX.Text = Settings.Default.min_X.ToString();
        changes++;
      }

      if (!Validate(txt_MinY.Text, -200, 0))
      {
        txt_MinY.Text = Settings.Default.min_Y.ToString();
        changes++;
      }

      // Serial
      if (!Validate(txt_Baudrate.Text, 0, 999999999))
      {
        txt_Baudrate.Text = Settings.Default.ser_Baud.ToString();
        changes++;
      }
      if (!Validate(txt_Timeout.Text, 0, 2000))
      {
        txt_Timeout.Text = Settings.Default.ser_Tim.ToString();
        changes++;
      }
      if (!Validate(txt_Lockout.Text, 0, 5000))
      {
        txt_Lockout.Text = Settings.Default.ser_Tim.ToString();
        changes++;
      }

      // Speed
      if (!Validate(txt_Speed.Text, 1, 100))
      {
       txt_Speed.Text = Settings.Default.spd_Vel.ToString();
       changes++;
      }
      if (!Validate(txt_Acceleration.Text, 1, 100))
      {
       txt_Acceleration.Text = Settings.Default.spd_Acc.ToString();
       changes++;
      }


   if (MessageBox.Show(
        (changes > 0 ? $"{changes} settings were outside of parameters and reverted.\n":"") +
        "Are you sure you want to save these settings?\n" +
        "The manufacturer takes no responsibility for any dammages as a result of changed settings.",
        "Save settings?",
        MessageBoxButton.YesNoCancel,
        MessageBoxImage.Question,
        MessageBoxResult.Cancel)
        != MessageBoxResult.Yes)
      {
        return;
      }
      UiToSettings();
    }

    private void SettingsToUi()
    {
      // Piston
      txt_PistonInactive.Text = Settings.Default.air_UP;
      txt_PistonActive.Text = Settings.Default.air_DOWN;
      txt_PistonDelay.Text = Settings.Default.air_DELAY_P.ToString();

      // Gripper
      txt_GripperInactive.Text = Settings.Default.air_OPEN;
      txt_GripperActive.Text = Settings.Default.air_CLOSE;
      txt_GripperDelay.Text = Settings.Default.air_DELAY_G.ToString();

      // Max
      txt_MaxW.Text = Settings.Default.max_W.ToString();
      txt_MaxX.Text = Settings.Default.max_X.ToString();
      txt_MaxY.Text = Settings.Default.max_Y.ToString();

      // Min
      txt_MinW.Text = Settings.Default.min_W.ToString();
      txt_MinX.Text = Settings.Default.min_X.ToString();
      txt_MinY.Text = Settings.Default.min_Y.ToString();

      // Serial
      txt_Baudrate.Text = Settings.Default.ser_Baud.ToString();
      txt_Timeout.Text = Settings.Default.ser_Tim.ToString();
      txt_Lockout.Text = Settings.Default.lockout.ToString();

      // Speed
      txt_Speed.Text = Settings.Default.spd_Vel.ToString();
      txt_Acceleration.Text = Settings.Default.spd_Acc.ToString();

      // Misc
      chk_Alarm.IsChecked = Settings.Default.alarm;
    }

    private void UiToSettings() 
    {
      try
      {
        // Piston
        Settings.Default.air_UP   = txt_PistonInactive.Text;
        Settings.Default.air_DOWN  = txt_PistonActive.Text;
        Settings.Default.air_DELAY_P = StoI(txt_PistonDelay.Text);

        // Gripper
        Settings.Default.air_OPEN  = txt_GripperInactive.Text;
        Settings.Default.air_CLOSE  = txt_GripperActive.Text;
        Settings.Default.air_DELAY_G = StoI(txt_GripperDelay.Text);

        // Max
        Settings.Default.max_W   = StoI(txt_MaxW.Text);
        Settings.Default.max_X   = StoI(txt_MaxX.Text);
        Settings.Default.max_Y   = StoI(txt_MaxY.Text);

        // Min
        Settings.Default.min_W   = StoI(txt_MinW.Text);
        Settings.Default.min_X   = StoI(txt_MinX.Text);
        Settings.Default.min_Y   = StoI(txt_MinY.Text);

        // Serial
        Settings.Default.ser_Baud  = StoI(txt_Baudrate.Text);
        Settings.Default.ser_Tim  = StoI(txt_Timeout.Text);
        Settings.Default.lockout  = StoI(txt_Lockout.Text);

        // Speed
        Settings.Default.spd_Vel = StoI(txt_Speed.Text);
        Settings.Default.spd_Acc = StoI(txt_Acceleration.Text);

        // Misc
        Settings.Default.alarm   = (bool)chk_Alarm.IsChecked;
      }
      catch 
      {
        MessageBox.Show("Error", "Unable to save one or more settings!");
      }

      Settings.Default.Save();

      unsaved = false;
      btn_Confirm.IsEnabled = unsaved;
    }

    private int StoI(string s)
    {
      if (Int32.TryParse(s, out int i)) return i;
      else throw new Exception("Couldn't convert");
    }
  
    private bool Validate(string s, int min, int max)
    {
      try
      {
        // Test for null
        if (s == null) throw new ArgumentNullException ();

        // Test for NaN
        if (Int32.TryParse(s, out int i))
        {
          // Test for OutOfBounds
          if (i > max || i < min) throw new ArgumentOutOfRangeException();
          
          // Validated
          return true;
        }
        else throw new FormatException();
      }
      catch
      {
        return false;
      }
    }
  }
}
