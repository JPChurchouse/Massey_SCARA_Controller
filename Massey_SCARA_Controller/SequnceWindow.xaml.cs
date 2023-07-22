using Massey_SCARA_Controller.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Massey_SCARA_Controller
{
  /// <summary>
  /// Interaction logic for SequnceWindow.xaml
  /// </summary>
  public partial class SequnceWindow : Window
  {
    public SequnceWindow()
    {
      InitializeComponent();
      MainWindow = (MainWindow)Application.Current.MainWindow;

      Init();
    }

    private MainWindow MainWindow;
    private void Init()
    {
      string x, y, w;
      x = MainWindow.txt_MoveX.Text;
      y = MainWindow.txt_MoveY.Text;
      w = MainWindow.txt_MoveW.Text;
      lbl_Move.Content = $"X:{x}, Y:{y}, W:{w}";

      lbl_Gripper.Content = MainWindow.btn_Gripper.Content.ToString().Contains("OPEN") ? "Close" : "Open";
      lbl_Piston.Content = MainWindow.btn_Piston.Content.ToString().Contains("UP") ? "Down" : "Up";

      string step, sped, dir;
      step = MainWindow.txt_ConvSteps.Text;
      sped = MainWindow.txt_ConvSpeed.Text;
      dir = (MainWindow.chk_ConvReverse.IsChecked ?? false) ? "Rev" : "For";
      lbl_Conveyor.Content = $"Steps:{step}, Speed:{sped}, Dir:{dir}";


    }

    private void SequenceWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {

        }

    private void btn_Confirm_Click(object sender, RoutedEventArgs e)
    {
      if (txt_Name.Text == "") return;
      Step p = new Step();

      p.Name = txt_Name.Text;

      string x, y, w;
      x = MainWindow.txt_MoveX.Text;
      y = MainWindow.txt_MoveY.Text;
      w = MainWindow.txt_MoveW.Text;
      if (chk_Move.IsChecked ?? false) p.Move = $"MOVE,{x},{y},{w}";

      if (chk_Piston.IsChecked ?? false) p.Piston = GetPiston();
      if (chk_Gripper.IsChecked ?? false) p.Gripper = GetGripper();

      if (chk_Conveyor.IsChecked ?? false) p.Conveyor = GetConveyor();
      if (txt_Wait.Text.Length > 0) 
      {
        if (Int32.TryParse(txt_Wait.Text, out int i))
        {
          if (i > 0 && i < 30000)
          {
            p.Wait = $"WAIT,{i}";
          }
        }
      }

      MainWindow.mySequence.Add(p);
      this.Close();
    }

    // Piston cmd
    private string GetPiston()
    {
      if (MainWindow.btn_Piston.Content.ToString().Contains("UP"))
      {
        return $"AIR,{Settings.Default.air_DOWN}";
      }
      else
      {
        return $"AIR,{Settings.Default.air_UP}";
      }
    }

    // Gripper cmd
    private string GetGripper()
    {
      if (MainWindow.btn_Gripper.Content.ToString().Contains("OPEN"))
      {
        return $"AIR,{Settings.Default.air_CLOSE}";
      }
      else
      {
        return $"AIR,{Settings.Default.air_OPEN}";
      }
      
    }
    private string GetConveyor()
    {
      return $"CONV,{MainWindow.txt_ConvSteps},{MainWindow.txt_ConvSpeed}";
    }
  }
}
