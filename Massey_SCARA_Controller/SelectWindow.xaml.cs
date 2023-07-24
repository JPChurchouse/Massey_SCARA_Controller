using Newtonsoft.Json;
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
using System.IO;

namespace Massey_SCARA_Controller
{
  /// <summary>
  /// Interaction logic for SelectWindow.xaml
  /// </summary>
  public partial class SelectWindow : Window
  {
    public SelectWindow(string dir,ref string sel)
    {
      InitializeComponent();

      selected = sel;
      directory = dir;

      Init();
    }

    private void Init()
    {
      foreach(string file in Directory.GetFiles(directory))
      {
        lst_Sequences.Items.Add(file.Replace(".json",""));
      }
    }


    private string selected = "", directory;


    private void lst_Sequences_Selected(object sender, RoutedEventArgs e)
    {

        }

    private void btn_Open_Click(object sender, RoutedEventArgs e)
    {
      string? s = lst_Sequences.SelectedItem?.ToString();
      selected = s ?? "";
      this.Close();
    }

    private void btn_Cancel_Click(object sender, RoutedEventArgs e)
    {
      selected = "";
      this.Close();
        }
    }
}
