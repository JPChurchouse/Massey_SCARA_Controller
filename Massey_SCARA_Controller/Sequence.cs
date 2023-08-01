using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Documents;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Windows.Controls;
using Serilog;
using System.Xml.Serialization;
using Massey_SCARA_Controller.Properties;

namespace Massey_SCARA_Controller
{
  public partial class MainWindow : Window
  {
    private string Seq_FilePath = Environment.CurrentDirectory + "\\sequences\\my_sequence.txt";
    public List<string> Seq_List = new List<string>();


    public void Seq_Import(string filepath = "")
    {
      if (filepath != "") Seq_FilePath = filepath;
      if (!File.Exists(Seq_FilePath)) return;

      string info = File.ReadAllText(Seq_FilePath);
      if (info == null) return;

      Seq_List.Clear();

      var lines = File.ReadLines(Seq_FilePath);
      foreach (var line in lines)
      {
        Seq_List.Add(line);
      }

      Seq_UpdatePanel();
    }

    public void Seq_Export(string filepath = "")
    {
      if (!Directory.Exists(Environment.CurrentDirectory + "\\sequences"))
        Directory.CreateDirectory(Environment.CurrentDirectory + "\\sequences");

      if (filepath != "") Seq_FilePath = filepath;

      string info = "";
      foreach (var item in Seq_List)
      {
        info += item + "\n";
      }
      File.WriteAllText(Seq_FilePath, info);
    }
    public void Seq_SelectFile()
    {
      OpenFileDialog window = new OpenFileDialog();
      window.Title = "Select File";
      window.InitialDirectory = Directory.GetCurrentDirectory() + "\\sequences\\";
      window.Filter = "Sequence Files|*.json|All Files|*.*";
      window.FilterIndex = 2;
      window.ShowDialog();
      if (window.FileName != "")
      {
        Seq_FilePath = window.FileName;
        Seq_Import();
      }
      else { }

      Seq_UpdatePanel();
    }

      
    public void Seq_MakeNew(string name)
    {
      Seq_Export();
      Seq_List.Clear();
      Seq_FilePath = Environment.CurrentDirectory + $"\\sequences\\{name}.txt";

      Seq_UpdatePanel();
    }

    public void Seq_Append(string command)
    {
      if (command.Contains("MOVE"))
      {
        int end = Seq_List.Count - 1;
        if (Seq_List[end].Contains("MOVE"))
        {
          Seq_List[end] = command;
          return;
        }
      }

      Seq_List.Add(command);

      Seq_UpdatePanel();
    }

    private bool Seq_Recording = false;
    private bool Seq_Executing = false;
    private bool Seq_Waiting = false;

    private void Seq_RecordThisStep(string command)
    {
      if (!Seq_Recording) return;
      Seq_Append(command);

      Seq_UpdatePanel();
    }

    private async Task Seq_SendCommandList(List<string> list)
    {
      Seq_Executing = true;
      Ui_SetControlsEnabled(false);

      foreach (var step in list)
      {
        Seq_Waiting = true;

        step.Replace("\n", "").Replace("\r", "");

        // Belt command
        if (
          step.Contains(Settings.Default.conv_For) || 
          step.Contains(Settings.Default.conv_Rev))
        {
          PORT_BELT_Send(step);
        }

        // SCARA command
        else
        {
          PORT_SCARA_Send(step);
          PORT_SCARA_Send("DING,DONE");
        }

        // Delay
        int timeout = Settings.Default.seq_Delay;
        int increment = 10;
        for (int i = 0; i < timeout; i+= increment)
        {
          if (!Seq_Executing) return; // Sequence terminated elsewhere
          if (!Seq_Waiting) break;  // Sequence timeout cleared elsewhere

          await Task.Delay(increment);
        }
        continue;
      }

      Seq_End();
      return;
    }

    private void Seq_UpdatePanel()
    {
      txt_NewScript.Text = Seq_FilePath.Substring(Seq_FilePath.LastIndexOf("\\")+1).Replace(".txt", "");

      list_Sequence.Items.Clear();
      foreach(string item in Seq_List)
      {
        list_Sequence.Items.Add(item);
      }
    }

    private void Seq_End()
    {
      Seq_Executing = false;
      Ui_SetControlsEnabled(true);
    }
  }
}
