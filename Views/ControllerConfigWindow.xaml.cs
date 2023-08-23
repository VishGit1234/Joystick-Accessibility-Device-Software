using Controller.Models;
using Controller.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Controller.Views
{
    /// <summary>
    /// Interaction logic for ControllerConfigWindow.xaml
    /// </summary>
    public partial class ControllerConfigWindow : Window
    {

        public ControllerConfigWindow(string _mode, int controllerCount)
        {
            InitializeComponent();
            ControllerConfigViewModel viewModel = new ControllerConfigViewModel(_mode, controllerCount);
            DataContext = viewModel;
            Closed += viewModel.OnWindowClosed;
        }

        public void dgvCombobox_Loaded(object sender, RoutedEventArgs e)
        {
            ((ComboBox)sender).DropDownClosed -= ComboBox_OnDropDownClosed!;
            ((ComboBox)sender).DropDownClosed += new System.EventHandler(ComboBox_OnDropDownClosed!);
        }

        void ComboBox_OnDropDownClosed(object sender, System.EventArgs e)
        {
            dgvFieldsMapping.Focus();
        }
    }
}
