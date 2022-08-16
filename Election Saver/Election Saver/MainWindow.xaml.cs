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
using System.Threading;

namespace Election_Saver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        int fileCount;
        int waitTimeINSecondsBetweenPrints;
        int copyProgressBarWaitTime;
        double waitPeriodToPercentageInterval;
        FileCopier fileCopier; 
        string currentPrecintWhenButtonPressed;
        //string bitLockerPassword = "a2CityClerksOffice!";
        public MainWindow()
        {
            InitializeComponent();
            fileCopier = new FileCopier();
            waitTimeINSecondsBetweenPrints = 10;
            copyProgressBarWaitTime = 2;
            waitPeriodToPercentageInterval = 100 / copyProgressBarWaitTime;
            foreach (var drive in fileCopier.allDrives)
            {
                driveSelector.Items.Add(drive);
            }
            driveSelector.Text = fileCopier.getSourcePath();

            if(driveSelector.SelectedItem == null)
            {
                unlockBitlockerButton.IsEnabled = false;
            }
            else if(driveSelector.SelectedItem != null)
            {
                unlockBitlockerButton.IsEnabled = true;
            }
            progressBar.Visibility = Visibility.Hidden;
            progressBarLabel.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CopyFilesButton_Click(object sender, RoutedEventArgs e)
        {
            progressBarLabel.Visibility = Visibility.Visible;
            progressBar.Visibility = Visibility.Visible;
            progressBar.Value = 0;
            currentPrecintWhenButtonPressed = PreceintTextBox.Text;


            for (int i = 0; i < copyProgressBarWaitTime; i++)
            {
                await Task.Delay(1000);
                progressBar.Value = (i + 1) * waitPeriodToPercentageInterval;
            }

            progressBar.Value = 100;
            fileCopier.CopyFiles(currentPrecintWhenButtonPressed, allowOverwriteCheckBox.IsChecked == true);
            await Task.Delay(copyProgressBarWaitTime * 1000);

            
            progressBar.Visibility = Visibility.Hidden;
            progressBarLabel.Visibility = Visibility.Hidden;


            //Pupulating the files available to copy text block
            string currentPrecinct = PreceintTextBox.Text;
            currentPrecinct = fileCopier.getAvailableFiles(currentPrecinct);
            localFilesTextBlock.Text = currentPrecinct;
        }

        private void PreceintTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CopyFilesButton.IsEnabled = true;
            printButton.IsEnabled = true;  

            if (PreceintTextBox.Text == "")
            {
                CopyFilesButton.IsEnabled = false;
                printButton.IsEnabled = false;
            }


            //Pupulating the files available to copy text block
            string currentPrecinct = PreceintTextBox.Text;
            currentPrecinct = fileCopier.getAvailableFiles(currentPrecinct);
            localFilesTextBlock.Text = currentPrecinct;
        }

        
        private async void printButton_Click(object sender, RoutedEventArgs e)
        {
            progressBarLabel.Visibility = Visibility.Visible;
            progressBar.Visibility = Visibility.Visible;
            progressBar.Value = 0;
            
            currentPrecintWhenButtonPressed = PreceintTextBox.Text;
            fileCopier.PrintFiles(waitTimeINSecondsBetweenPrints, currentPrecintWhenButtonPressed);
            await Task.Delay(2000);
            progressBar.Value = 25;
            await Task.Delay(2000);
            progressBar.Value = 50;
            await Task.Delay(2000);
            progressBar.Value = 75;
            await Task.Delay(2000);
            progressBar.Value = 100;
            await Task.Delay(5000);
            progressBar.Visibility = Visibility.Hidden;
            progressBarLabel.Visibility = Visibility.Hidden;

            //for (int i = 0; i < fileCount; i++)
            //{
            //    progressBar.Value = 0;
            //    await Application.Current.Dispatcher.InvokeAsync(() =>
            //    {
            //        for (int j = 0; j < waitTimeINSecondsBetweenPrints; j++ )
            //        {
            //            progressBar.Value = ((j + 1) / waitTimeINSecondsBetweenPrints) * 100;
            //            System.Threading.Thread.Sleep(1000); //wait 1 second
            //        }
            //    });
            //}

        }

        private void timeBetweenPrintsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //waitTimeINSecondsBetweenPrints = Int32.Parse(timeBox.Text);
            
        }

        private void driveSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            fileCopier.setSourcePath(driveSelector.SelectedItem);
            unlockBitlockerButton.IsEnabled = true;
        }

        private void refreshDrivesButton_Click(object sender, RoutedEventArgs e)
        {
            fileCopier.updateDrives();
            driveSelector.Items.Clear();
            foreach (var drive in fileCopier.allDrives)
            {
                driveSelector.Items.Add(drive);
            }
            //driveSelector.Text = fileCopier.getSourcePath();

            if (driveSelector.SelectedItem == null)
            {
                unlockBitlockerButton.IsEnabled = false;
            }
            else if (driveSelector.SelectedItem != null)
            {
                unlockBitlockerButton.IsEnabled = true;
            }
        }

        private void unlockBitlockerButton_Click(object sender, RoutedEventArgs e)
        {
            if (driveSelector.SelectedItem != null)
            {
                fileCopier.unlockBitLocker();
            }
        }

        private void updateBitLockerPassword_Click(object sender, RoutedEventArgs e)
        {
            fileCopier.setBitLockerPassword(bitLockerPasswordTextBox.Password.ToString());
        }
    }
}
