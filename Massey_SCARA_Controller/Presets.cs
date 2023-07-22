using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;


// https://code-maze.com/introduction-system-text-json-examples/
// https://code-maze.com/csharp-write-json-into-a-file/

namespace Massey_SCARA_Controller
{
  public class Step
    {
      public string Name = "New Pose";
      public string? Move;
      public string? Piston;
      public string? Gripper;
      public string? Conveyor;
      public string? Wait;

      public Step(){}
      public List<string> GetList()
      {
        List<string> list = new List<string>();
        if (Name != null) list.Add(Name);
        if (Move != null) list.Add(Move);
        if (Piston != null) list.Add(Piston);
        if (Gripper != null) list.Add(Gripper);
        if (Conveyor != null) list.Add(Conveyor);
        if (Wait != null) list.Add(Wait);
        return list;
      }
    }

    public class Sequence
    {
      public string Name = "New Sequence";
      public List<Step> List = new List<Step>();
      public Sequence() { }

    public void SaveTo(string directory)
    {
      string json = JsonConvert.SerializeObject(this);
      File.WriteAllText($"{directory}\\{Name}.json", json);
    }
    public bool InitFromFile(string file)
    {
      string json = File.ReadAllText(file);
      Sequence? s;

      try
      {
        s = JsonConvert.DeserializeObject<Sequence>(json);
        if (s == null) throw new Exception();
      }
      catch
      {
        return false;
      }
      Name = s.Name;
      List = s.List;

      return true;
    }

    public void Remove(string item)
      {
        foreach (Step p in List)
        {
          if (p.Name == item)
          {
            List.Remove(p);
            return;
          }
        }
      }
      public void Add(Step p)
      {
        List.Add(p); 
        return;
      }
      public bool Move(string item, bool up = true)
      {
        int[] pos = GetIndex(item);
        if (!up && pos[0] + 1 >= pos[1]) return false; // cannot go beyond end of list
        if (up && pos[0] == 0) return false; // cannot go before start of list

        Step target = List[pos[0]];
        int moveto = up ? pos[0] + 1 : pos[0] - 1;
        List[pos[0]] = List[pos[moveto]];
        List[pos[moveto]] = target;
        return true;
      }
      public int[] GetIndex(string item)
      {
        int[] ret = new int[2] {-1, List.Count };

        for (int i = 0; i < List.Count; i++)
        {
          if (List[i].Name == item)
          {
            ret[0] = i;
            break;
          }
        }
        return ret;
      }
    }

  public partial class MainWindow : Window
  {
    string dire_Sequences = Environment.CurrentDirectory + "\\sequences";

    public Sequence? mySequence;


    #region Buffers
    private Buffer LocalCommandBuffer = new Buffer(90);
    private bool flag_CommandsWaiting_Local = false;
    private bool flag_CommandsWaiting_Controller = false;
    private void SendTxBuffer()
    {
      Log.Debug("Sending TX buffer");
      while (LocalCommandBuffer.status() != Buffer.Status.Empty)
      {
        flag_CommandsWaiting_Controller = true;
        flag_CommandsWaiting_Local = true;

        string command = LocalCommandBuffer.Acquire();
        if (command.Contains("CONV"))
        {
          PORT_BELT_Send(command);
        }
        else
        {
          PORT_SCARA_Send(command);
        }
      }
      Log.Debug("TX buffer empty");
      flag_CommandsWaiting_Local = false;
      return;
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

    class Buffer
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
  }
}
