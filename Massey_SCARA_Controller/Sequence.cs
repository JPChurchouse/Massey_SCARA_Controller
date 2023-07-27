using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace Massey_SCARA_Controller
{
    struct Operation
    {
        public Operation() { }
        public Operation(int w, int x, int y) 
        {
            W = w; X = x; Y = y;
        }
        public Operation(bool? down = null, bool? close = null, ushort? wait = null) 
        {
            if (down != null) Down = (bool)down;
            if (close != null) Close = (bool)close;
            if (wait != null) Wait = (ushort)wait;
        }
        public Operation(ushort steps, bool forward = true, ushort speed = 100)
        {
            Steps = steps;
            Forward = forward;
            Speed = speed;
        }


        public int W;
        public int X;
        public int Y;

        public bool Down;
        public bool Close;
        public ushort Wait;

        public ushort Steps;
        public bool Forward;
        public ushort Speed;
    }

  public partial class MainWindow : Window
  {
        private List<Operation> list_operations = new List<Operation>();
        public void ReadInOpeartionList(string filepath)
        {
            list_operations.Clear();

            string json = File.ReadAllText(filepath);

            try
            {
                var output = JsonConvert.DeserializeObject<Operation>(json);
                if (output == null) throw new Exception();
            }
            catch
            {
            }

        }

  }
}
