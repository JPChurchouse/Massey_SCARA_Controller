using Serilog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Management;
using SCARA_GUI.Properties;

namespace SCARA_GUI
{
  public partial class MainWindow : Window
  {
    // The global SerialPort object
    private SerialPort SERIALPORT_SCARA = new SerialPort();
    private SerialPort SERIALPORT_BELT = new SerialPort();

  // Initalise serial ports
  private void InitSerial()
    {
      SERIALPORT_SCARA.RtsEnable = true;
      SERIALPORT_SCARA.DtrEnable = true;
      SERIALPORT_SCARA.ReadTimeout = 500;
      SERIALPORT_SCARA.WriteTimeout = 500;
      SERIALPORT_SCARA.NewLine = "\n";
      SERIALPORT_SCARA.BaudRate = 115200;
      SERIALPORT_SCARA.PortName = "COM0";
      SERIALPORT_SCARA.DataReceived += SERIALPORT_SCARA_DataReceived;
      SERIALPORT_SCARA.ErrorReceived += SERIALPORT_SCARA_ErrorReceived;

      SERIALPORT_BELT.RtsEnable = true;
      SERIALPORT_BELT.DtrEnable = true;
      SERIALPORT_BELT.ReadTimeout = 500;
      SERIALPORT_BELT.WriteTimeout = 500;
      SERIALPORT_BELT.NewLine = "\n";
      SERIALPORT_BELT.BaudRate = 115200;
      SERIALPORT_BELT.PortName = "COM0";
      SERIALPORT_BELT.DataReceived += SERIALPORT_BELT_DataReceived;
      SERIALPORT_BELT.ErrorReceived += SERIALPORT_BELT_ErrorReceived;
  }
  #region SCARA PORT
  // Serial Port error handler
  private void SERIALPORT_SCARA_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
      LogMessage("Serial Port Error", MsgType.ALT);
      Ui_UpdateConnectionStatus();
    }

    // Serial Port RX handler
    private void SERIALPORT_SCARA_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
      try
      {
        if (SERIALPORT_SCARA.BytesToRead == 0) return;
      
        string data = SERIALPORT_SCARA.ReadLine();
        data = data.Replace("\r", "");
        data = data.Replace("\n", "");
        Log.Information($"Received: \"{data}\"");

        if (data.Contains("RECEIVED")) 
        {
          LockoutEnd();
          return;
        }// Don't show the user the ECHO rx cmds

        LogMessage(data, MsgType.RXD);
      }
      catch (Exception exc)
      {
        Log.Debug($"Failed to read serial: {exc.ToString()}");
      }
    }

  // Serial Port error handler
  private void SERIALPORT_BELT_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
  {
   LogMessage("Serial Port Error", MsgType.ALT);
   Ui_UpdateConnectionStatus();
  }
  #endregion
  #region BELT PORT

  // Serial Port RX handler
  private void SERIALPORT_BELT_DataReceived(object sender, SerialDataReceivedEventArgs e)
  {
   try
   {
    if (SERIALPORT_BELT.BytesToRead == 0) return;

    string data = SERIALPORT_BELT.ReadLine();
    data = data.Replace("\r", "");
    data = data.Replace("\n", "");
    Log.Information($"Received: \"{data}\"");

    if (data.Contains("RECEIVED"))
    {
     LockoutEnd();
     return;
    }// Don't show the user the ECHO rx cmds

    LogMessage(data, MsgType.RXD);
   }
   catch (Exception exc)
   {
    Log.Debug($"Failed to read serial: {exc.ToString()}");
   }
  }
  #endregion

  // Scan for the right device and connect to it
  private void ScanAndConnect()
    {
      this.Dispatcher.Invoke(() =>
      {
        bool port_SCARA_connected = SERIALPORT_SCARA.IsOpen;
        bool port_BELT_connected = SERIALPORT_BELT.IsOpen;

         // If connected, return
         if (port_SCARA_connected && port_BELT_connected) return;

        // Update labels
        LogMessage("Setting up Serial PortS", MsgType.SYS);
        lbl_DeviceStatus.Content = "Connecting...";
        btn_Connect.Content = "Connecting...";

        // Update UI availibility for this function
        btn_Connect.IsEnabled = false;
        this.Cursor = Cursors.Wait;

        // Apply user settings
        SERIALPORT_SCARA.ReadTimeout = Settings.Default.ser_Tim;
        SERIALPORT_SCARA.WriteTimeout = Settings.Default.ser_Tim;
        SERIALPORT_SCARA.BaudRate = Settings.Default.ser_Baud;

       SERIALPORT_BELT.ReadTimeout = Settings.Default.ser_Tim;
       SERIALPORT_BELT.WriteTimeout = Settings.Default.ser_Tim;
       SERIALPORT_BELT.BaudRate = Settings.Default.ser_Baud;

       // Connect to ports
       try
        {
          ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_PnPEntity WHERE Caption LIKE '% (COM%'");
          ManagementObjectCollection devices = searcher.Get();

          if (devices.Count == 0) throw new Exception("No devices found");

          foreach (var dev in devices)
          {
            string caption = dev.GetPropertyValue("Caption").ToString();
            Log.Debug($"Found device: {caption}");


            if (!port_SCARA_connected)
            {
              if (caption.Contains("Arduino Uno") || caption.Contains("Arduino Due"))
              {
                SERIALPORT_SCARA.PortName = ParsePortInfo(caption);

                LogMessage("Attempting to open SCARA Port", MsgType.SYS);
                try { SERIALPORT_SCARA.Open(); }
                catch { }

                port_SCARA_connected = SERIALPORT_SCARA.IsOpen;
                LogMessage($"SCARA connection {(port_SCARA_connected ? "OPEN" : "FAILED")}", MsgType.SYS);
              }

            }

            if (!port_BELT_connected)
            {
              if (caption.Contains("CH340"))
              {
                SERIALPORT_BELT.PortName = ParsePortInfo(caption);

                LogMessage("Attempting to open BELT Port", MsgType.SYS);
                try { SERIALPORT_BELT.Open(); }
                catch { }

                port_BELT_connected = SERIALPORT_BELT.IsOpen;
                LogMessage($"BELT connection {(port_BELT_connected ? "OPEN" : "FAILED")}", MsgType.SYS);
              }

              if (port_SCARA_connected && port_BELT_connected) break;

            }
          }
        }
        catch (Exception ex) { Log.Error(ex.Message); }


        if (port_SCARA_connected)
        {
          Log.Debug($"Baudrate: {SERIALPORT_SCARA.BaudRate}");

          
          // Play alarm
          WMPLib.WindowsMediaPlayer player = new WMPLib.WindowsMediaPlayer();
          if (Settings.Default.alarm) 
          {
            player.URL = "alarm.mp3";
            player.controls.play();
          }

          // Show warning
          LogMessage("WARNING - SCARA ACTIVE", MsgType.ALT);
          MessageBox.Show(
            "The SCARA is about to become active. Press \"OK\" to proceed when the area is safe.",
            "⚠️ WARNING ⚠️");

          player.close();

          // Home all axies
          PORT_SCARA_Send("ECHO,1");
          PORT_SCARA_Send("CLEAR");
          PORT_SCARA_Send("HOME");
        }
        else
        {
          LogMessage("Unable to connect to SCARA", MsgType.SYS);
        }

        if (!port_BELT_connected) LogMessage("Unable to connect to BELT", MsgType.SYS);

        Ui_UpdateConnectionStatus();
        this.Cursor = null;
      });
    }

    // Disconnect Serial Port
    private async void Disconnect()
    {
      try
      {
        if (SERIALPORT_SCARA.IsOpen)
        {
          LogMessage("Closing Serial Port", MsgType.SYS);

          PORT_SCARA_Send("STOP");
          await Task.Delay(100);
          PORT_SCARA_Send("CLEAR");
          await Task.Delay(100);

          SERIALPORT_SCARA.Close();

          LogMessage("Serial Port closed", MsgType.SYS);
        }
        else
        {
          LogMessage("Serial Port already closed", MsgType.SYS);
        }
      }
      catch (Exception exc)
      {
        Log.Error(exc.Message);
        LogMessage("Error while closing Serial Port", MsgType.SYS);
      }
      finally
      {
        await Task.Delay(500);
        Ui_UpdateConnectionStatus();
      }

      try
      {
        SERIALPORT_BELT.Close();
      }
      catch { }
    }

    // Process sending data on the Serial Port
    private void PORT_SCARA_Send(string data)
    {
      if (!SERIALPORT_SCARA.IsOpen)
      {
        LogMessage("Connection error", MsgType.ALT);
      }

      else if (data == null || data == "")
      {
        LogMessage("Not content to send", MsgType.ALT);
      }

      else 
      { 
        LogMessage($"Sending: {data}", MsgType.TXD);
        try
        {
          SERIALPORT_SCARA.WriteLine(data);
          LockoutStart();
        }
        catch (Exception ex)
        {
          LogMessage("Unable to send command", MsgType.ALT);
          Log.Error(ex.ToString());
        }
      }

      Ui_UpdateConnectionStatus();
    }

    private void PORT_BELT_Send(string data)
    {
      if (!SERIALPORT_BELT.IsOpen)
      {
        LogMessage("Connection error", MsgType.ALT);
      }

      else if (data == null || data == "")
      {
        LogMessage("Not content to send", MsgType.ALT);
      }

      else
      {
        LogMessage($"Sending: {data}", MsgType.TXD);
        try
        {
          SERIALPORT_BELT.WriteLine(data);
          LockoutStart();
        }
        catch (Exception ex)
        {
          LogMessage("Unable to send command", MsgType.ALT);
          Log.Error(ex.ToString());
        }
      }

      Ui_UpdateConnectionStatus();
    }

    // Convert the COM Port message to a "COM x" string
    private string ParsePortInfo(string info) { return info.Substring(info.LastIndexOf("(COM")).Replace("(", string.Empty).Replace(")", string.Empty); }


    // LOCKOUT
    System.Timers.Timer timer_lockout = new System.Timers.Timer();
    private void LockoutStart()
    {
      long tim = Settings.Default.lockout;
      if (tim <= 0) return;

      Ui_SetControlsEnabled(false);
      timer_lockout.Interval = tim;
      timer_lockout.Elapsed += timer_lockout_elapsed;
      timer_lockout.Start();
    }
    private void LockoutEnd()
    {
      timer_lockout.Stop();
      Ui_SetControlsEnabled(true);
    }

    private void timer_lockout_elapsed(object sender, System.Timers.ElapsedEventArgs e) { LockoutEnd(); }
  }
}
