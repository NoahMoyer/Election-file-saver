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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace Election_file_saver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       
        
        FileCopier fileCopier = new FileCopier();
        string currentPrecintWhenButtonPressed;
        public MainWindow()
        {
            InitializeComponent();

        }

        private void CopyFilesButton_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Value = 100;
            currentPrecintWhenButtonPressed = PreceintTextBox.Text;
            
            fileCopier.CopyFiles(currentPrecintWhenButtonPressed, allowOverwriteCheckBox.IsChecked == true);
            allowOverwriteCheckBox.IsChecked = false;
        }

        private void PreceintTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CopyFilesButton.IsEnabled = true;
        }

        private void printButton_Click(object sender, RoutedEventArgs e)
        {
            fileCopier.PrintFiles();
        }
    }
}
