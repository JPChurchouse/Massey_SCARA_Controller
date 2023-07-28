﻿using Newtonsoft.Json;
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

namespace Massey_SCARA_Controller
{
  public partial class MainWindow : Window
  {

    private ScriptHandler scriptHandler = new ScriptHandler(Environment.CurrentDirectory + "\\sequences\\my_sequence.json");
    private class ScriptHandler
    {
      public ScriptHandler() { }
      public ScriptHandler(string filepath)
      {
        this.FilePath = filepath;
        Import();
      }

      public void Import(string filepath = "")
      {
        if (filepath != "") FilePath = filepath;
        if (!File.Exists(FilePath)) return;

        string json = File.ReadAllText(FilePath);
        if (json == null) return;

        ScriptHandler info;

        try
        {
          info = JsonConvert.DeserializeObject<ScriptHandler>(json);
          if (info == null) throw new Exception();
        }
        catch
        {
          return;
        }

        this.List.Clear();
        this.List = info.List;
      }

      public void Export(string filepath = "")
      {
        if (filepath != "") FilePath = filepath;

        string json = JsonConvert.SerializeObject(this);
        File.WriteAllText(FilePath, json);
      }
      public void SelectFile()
      {
        OpenFileDialog window = new OpenFileDialog();
        window.Title = "Select File";
        window.InitialDirectory = Directory.GetCurrentDirectory() + "\\sequences\\";
        window.Filter = "Sequence Files|*.json|All Files|*.*";
        window.FilterIndex = 2;
        window.ShowDialog();
        if (window.FileName != "")
        {
          this.FilePath = window.FileName;
        }
        else { }
      }

      private string FilePath = "";
      public List<string> List = new List<string>();
      public string[] GetMachineList()
      {
        List<string> list = new List<string>();
        foreach (var step in this.List)
        {
          list.Add(step);
        }
        return list.ToArray();
      }
      public void MakeNew(string name)
      {
        Export();
        List.Clear();
        FilePath = Environment.CurrentDirectory + $"\\sequences\\{name}.json";
      }

      public void Append(string command)
      {
        if (command.Contains("MOVE"))
        {
          int end = List.Count - 1;
          if (List[end].Contains("MOVE"))
          {
            List[end] = command;
            return;
          }
        }

        this.List.Add(command);
      }
    }

    private void RecordThisStep(string command)
    {
      if (!Recording) return;
      scriptHandler.Append(command);
      UpdateScriptPanel();
    }

    private async Task SendCommandList(List<string> list)
    {
      Ui_SetControlsEnabled(false);

      foreach (var step in list)
      {
        if (
          step.Contains(Properties.Settings.Default.conv_For) || 
          step.Contains(Properties.Settings.Default.conv_Rev))
        {
          PORT_BELT_Send(step);
        }
        else
        {
          PORT_SCARA_Send(step);
          await Task.Delay(500);
        }
      }

      Ui_SetControlsEnabled(true);
      return;
    }

    private void UpdateScriptPanel()
    {
      list_Sequence.Items.Clear();
      foreach(string item in scriptHandler.List)
      {
        list_Sequence.Items.Add(item);
      }
    }
  }
}
