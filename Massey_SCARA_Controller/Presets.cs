using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using


// https://code-maze.com/introduction-system-text-json-examples/
// https://code-maze.com/csharp-write-json-into-a-file/

namespace SCARA_GUI
{
  public partial class MainWindow : Window
  {
   private struct Configuration
   {
      public int X = 0;
      public Configuration()
      {
      }

      Y, W;
      public string MOVE() { return $"MOVE,{X},{Y},{W}"; }
    }


    string file_Presets = Environment.CurrentDirectory + "\\presets.json";
    private class Preset
    {
      public string Name;
      private int W;
      private int X;
      private int Y;
      
      public Preset(string name, int w, int x, int z)
      {
        Name = name;
        W = w;
        X = x;
        Y = z;
      }
      public string MOVE() { return $"MOVE,{X},{Y},{W}"; }
      public void save()
      {

      }
      public void delete()
      {

      }
    }
  }
}
