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

namespace Massey_SCARA_Controller
{
  public partial class MainWindow : Window
  {
    private struct Step
    {
      public Step() { }

      public string Name = "";
      public string Value = "";
      public bool Conveyor = false;
    }

    private ScriptHandler scriptHandler = new ScriptHandler();
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
        File.WriteAllText(filepath, json);
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
      public List<Step> List = new List<Step>();
      public string[] GetUserFriendlyList()
      {
        List<string> list = new List<string>();
        foreach (var step in this.List)
        {
          list.Add(step.Name);
        }
        return list.ToArray();
      }
      public string[] GetMachineList()
      {
        List<string> list = new List<string>();
        foreach (var step in this.List)
        {
          list.Add(step.Value);
        }
        return list.ToArray();
      }
      public void MakeNew(string name)
      {
        throw new NotImplementedException();
      }

      public void Append(string data)
      {
        Step step = new Step();
        step.Value = data;
        
        if (data.Contains("MOVE"))
        {
          step.Name = "Movement";
        }
        else if (data.Contains("AIR"))
        {
          step.Name = "Pneumatic";
        }
        else if(data.Contains("CONV"))
        {
          step.Name = "Conveyor";
        }
        else
        {
          step.Name = "err";
        }
        this.List.Add(step);
      }
    }

    private void SequenceEnd()
    {

    }


    #region Buffer

    internal class Buffer
    {
      public enum Status { Empty, Available, Full };
      private string[] Instructions;
      private int size = 10;
      private int index_read = 0; // Index to read next cmd from
      private int index_write = 0; // Index to write next cmd to

      // Create buffer
      public Buffer(int _size)
      {
        size = _size;
        Instructions = new string[size];
        Reset();
      }

      // Append and instruction
      public bool Append(string what)
      {
        // If the string is null or the buffer is full, return fail
        if (what == null || status() == Status.Full) return false;

        // Add the instruction and increment the write buffer
        Instructions[index_write] = what;
        index_write++;

        // Return write index to 0 if at end
        if (index_write >= size) index_write = 0;

        // Return success
        return true;
      }

      // Reset
      public void Reset()
      {
        index_write = 0;
        index_read = 0;
        for (int i = 0; i < size; i++) Instructions[i] = null;
      }

      // Read next instruction
      public string Acquire()
      {
        if (status() == Status.Empty) return null;
        string cmd = Instructions[index_read];
        Instructions[index_read++] = null;
        if (index_read >= size) index_read = 0;
        return cmd; ;
      }

      // Get status of buffer
      public Status status()
      {
        if (index_read == index_write)
        {
          if (Instructions[index_read] == null) return Status.Empty;
          else return Status.Full;
        }
        else return Status.Available;
      }
    }

    private Buffer LocalCommandBuffer = new Buffer(64);
    private bool flag_CommandsWaiting_Local = false;
    private bool flag_CommandsWaiting_Controller = false;
    private void SendTxBuffer()
    {
      Log.Debug("Sending TX buffer");
      if (LocalCommandBuffer.status() == Buffer.Status.Empty)
      {
        Log.Debug("TX buffer empty");
        flag_CommandsWaiting_Local = false;
        SequenceEnd();
        return;
      }

      string info = LocalCommandBuffer.Acquire();
      if (info.Contains("Conv"))
      {
        PORT_BELT_Send(info);
      }
      else
      {
        PORT_SCARA_Send(info);
      }

      flag_CommandsWaiting_Controller = true;
      flag_CommandsWaiting_Local = LocalCommandBuffer.status() != Buffer.Status.Empty;
    }

    // Add a command to the buffer ready to be sent to the controller
    private void AddToTxBuffer(string cmd)
    {
      if (cmd == null || cmd == "") return;
      LocalCommandBuffer.Append(cmd);
      flag_CommandsWaiting_Local = true;
    }
    private void AddToTxBuffer(string[] cmds)
    {
      for (int i = 0; i < cmds.Length; i++)
        AddToTxBuffer(cmds[i]);
    }
    #endregion
  }
}
