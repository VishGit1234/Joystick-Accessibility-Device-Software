using System;
using System.Collections.Generic;
using System.IO;
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

namespace Controller.Views
{
    /// <summary>
    /// Interaction logic for RecordedFileNameDialogWindow.xaml
    /// </summary>
    public partial class RecordedFileNameDialogWindow : Window
    {
        public string filename = "";
        public RecordedFileNameDialogWindow()
        {
            InitializeComponent();
        }

        private void onSaveClick(object sender, RoutedEventArgs e)
        {
            filename = FilenameBox.Text;
            try
            {
                var temp = new FileStream(filename + ".txt", FileMode.CreateNew);
                temp.Close();
                File.Delete(filename + ".txt");
                Close();
            }
            catch
            {
                MessageBox.Show("Invalid File Name", "Filename Error", MessageBoxButton.OK, MessageBoxImage.Error);
                FilenameBox.Text = "";
            }
            
        }
    }
}
