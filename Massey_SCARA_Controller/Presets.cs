using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using System.Windows.Automation.Peers;
using Serilog;


// https://code-maze.com/introduction-system-text-json-examples/
// https://code-maze.com/csharp-write-json-into-a-file/

namespace Massey_SCARA_Controller
{
  public partial class MainWindow : Window
  {


    string file_Presets = Environment.CurrentDirectory + "\\presets.json";
    private class Preset
    {
      public string Name = "New Pose";
      public string? Move;
      public string? Piston;
      public string? Gripper;
      public string? Belt;

      public Preset(){}
      public List<string> GetList()
      {
        List<string> list = new List<string>();
        if (Name != null) list.Add(Name);
        if (Move != null) list.Add(Move);
        if (Piston != null) list.Add(Piston);
        if (Gripper != null) list.Add(Gripper);
        if (Belt != null) list.Add(Belt);
        return list;
      }
    }

    private class Sequence
    {
      public string Name = "New Sequence";
      public List<Preset> List = new List<Preset>();
      public Sequence() { }
      
      public void Remove(string item)
      {
        foreach (Preset p in List)
        {
          if (p.Name == item)
          {
            List.Remove(p);
            return;
          }
        }
      }
      public void Add(Preset p)
      {
        List.Add(p); 
        return;
      }
      public bool Move(string item, bool up = true)
      {
        int[] pos = GetIndex(item);
        if (!up && pos[0] + 1 >= pos[1]) return false; // cannot go beyond end of list
        if (up && pos[0] == 0) return false; // cannot go before start of list

        Preset target = List[pos[0]];
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
        if (command.Contains("BELT"))
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
  }
}
