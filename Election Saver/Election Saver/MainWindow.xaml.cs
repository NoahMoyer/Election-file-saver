﻿using System;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text.RegularExpressions;

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
            //Noah: comment what we are doing here
            networkSaveLocationLabelDispalay.Content = fileCopier.getNetworkDestinationPath();
            localSaveLocationLabelDispalay.Content = fileCopier.getLocalDestinationPath();
            currentDefaultDriveLetterLabel1.Content = fileCopier.getSourcePath();
            //Noah: comment what we are doing here
            foreach(var letter in fileCopier.getDriveLettersToExclude())
            {
                listOfDriveLettersToExludeBox.Items.Add(letter);
            }
            //Noah: comment what we are doing here
            foreach (var extension in fileCopier.getListOfFileExtensionsToCopy())
            {
                listOfFileExtensionsToCopyBox.Items.Add(extension);
            }

            //Poplulating the files available to copy text block
            string currentPrecinctFlashFiles = PreceintTextBox.Text;
            currentPrecinctFlashFiles = fileCopier.getFlashAvailableFiles();
            flashFilesTextBlock.Text = currentPrecinctFlashFiles;

            //Pupulating the files available to print text block
            string currentPrecinctLocalFiles = PreceintTextBox.Text;
            currentPrecinctLocalFiles = fileCopier.getAvailableFiles(currentPrecinctLocalFiles);
            localFilesTextBlock.Text = currentPrecinctLocalFiles;

            //Get drive lock status
            driveLockStatusLable.Content = fileCopier.getDriveLockStatus();
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

            //Pupulating the files available to print text block
            string currentPrecinctLocalFiles = PreceintTextBox.Text;
            currentPrecinctLocalFiles = fileCopier.getAvailableFiles(currentPrecinctLocalFiles);
            localFilesTextBlock.Text = currentPrecinctLocalFiles;
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

            //Get drive lock status
            driveLockStatusLable.Content = fileCopier.getDriveLockStatus();

            //Poplulating the files available to copy text block
            string currentPrecinctFlashFiles = PreceintTextBox.Text;
            currentPrecinctFlashFiles = fileCopier.getFlashAvailableFiles();
            flashFilesTextBlock.Text = currentPrecinctFlashFiles;

            if (driveLockStatusLable.Content == "Locked")
            {
                CopyFilesButton.IsEnabled = false;
                printButton.IsEnabled = false;
            }
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

            //Poplulating the files available to copy text block
            string currentPrecinctFlashFiles = PreceintTextBox.Text;
            currentPrecinctFlashFiles = fileCopier.getFlashAvailableFiles();
            flashFilesTextBlock.Text = currentPrecinctFlashFiles;

            //Get drive lock status
            driveLockStatusLable.Content = fileCopier.getDriveLockStatus();

            if (driveLockStatusLable.Content == "Locked")
            {
                CopyFilesButton.IsEnabled = false;
                printButton.IsEnabled = false;
            }
        }

        private void unlockBitlockerButton_Click(object sender, RoutedEventArgs e)
        {
            if (driveSelector.SelectedItem != null)
            {
                fileCopier.unlockBitLocker();
            }
            else
            {
                MessageBox.Show("Please select a drive to unlock", "Input Error");
            }

            //Get drive lock status
            driveLockStatusLable.Content = fileCopier.getDriveLockStatus();

            //Poplulating the files available to copy text block
            string currentPrecinctFlashFiles = PreceintTextBox.Text;
            currentPrecinctFlashFiles = fileCopier.getFlashAvailableFiles();
            flashFilesTextBlock.Text = currentPrecinctFlashFiles;
        }

        private void updateBitLockerPassword_Click(object sender, RoutedEventArgs e)
        {
            if(bitLockerPasswordTextBox.Password != null)
            {
                fileCopier.setBitLockerPassword(bitLockerPasswordTextBox.Password.ToString());
            }
            else
            {
                MessageBox.Show("No text entered, please input something", "Input Error");
            }
            updateBitLockerPasswordButton.IsDefault = false;
        }

        private void changeNetworkLocationButton_Click(object sender, RoutedEventArgs e)
        {
            
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result.ToString() == "Ok")
            {
                fileCopier.setNetworkDestinationPath(dialog.FileName);
                networkSaveLocationLabelDispalay.Content = dialog.FileName;
            }
            

        }

        private void changeLocalLocationButton_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result.ToString() == "Ok")
            {
                fileCopier.setLocalDestinationPath(dialog.FileName);
                localSaveLocationLabelDispalay.Content = dialog.FileName;
            }
        }

        private void changeDefaultDriveLetterButtton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result.ToString() == "Ok")
            {
                fileCopier.setSourcePath(dialog.FileName);
                currentDefaultDriveLetterLabel1.Content = dialog.FileName;
            }
        }

        private void letterToAddToDrivesToExcludeButton_Click(object sender, RoutedEventArgs e)
        {
            if(letterToAddToDrivesToExcludeTextBox.Text != null && letterToAddToDrivesToExcludeTextBox.Text != "")
            {
                if (!listOfDriveLettersToExludeBox.Items.Contains(letterToAddToDrivesToExcludeTextBox.Text))
                {
                    string text = letterToAddToDrivesToExcludeTextBox.Text.ToString();
                    if (Regex.IsMatch(text, "^[A-Z]{1}$"))
                    {
                        listOfDriveLettersToExludeBox.Items.Add(letterToAddToDrivesToExcludeTextBox.Text);
                        List<string> listOfNewLettersToExclude = new List<string>();
                        foreach (var letter in listOfDriveLettersToExludeBox.Items)
                        {
                            listOfNewLettersToExclude.Add(letter.ToString());
                        }
                        fileCopier.setDriveLettersToExclude(listOfNewLettersToExclude);
                    }
                    else
                    {
                        MessageBox.Show("Please only enter a single uppercase letter", "Input Error");
                    }
                }
                else
                {
                    MessageBox.Show("Drive is already excluded", "Input Error");
                }
                
            }
            else
            {
                MessageBox.Show("No text entered, please input something", "Input Error");
            }
            letterToAddToDrivesToExcludeButton.IsDefault = false;
        }

        private void removeSelectedDriveLetterButton_Click(object sender, RoutedEventArgs e)
        {
            if(listOfDriveLettersToExludeBox.SelectedItem != null)
            {
                fileCopier.removeSepcicDriveLetterToExclude(listOfDriveLettersToExludeBox.SelectedItem.ToString());
                listOfDriveLettersToExludeBox.Items.Remove(listOfDriveLettersToExludeBox.SelectedItem);
            }
            else
            {
                MessageBox.Show("Please select an item to remove", "Input Error");
            }
            removeSelectedDriveLetterButton.IsDefault = false;

        }

        private void removeSelectedFileExtension_Click(object sender, RoutedEventArgs e)
        {
            if(listOfFileExtensionsToCopyBox.SelectedItem != null)
            {
                fileCopier.removeSpecificFileExtensionToCopy(listOfFileExtensionsToCopyBox.SelectedItem.ToString());
                listOfFileExtensionsToCopyBox.Items.Remove(listOfFileExtensionsToCopyBox.SelectedItem);
            }
            else
            {
                MessageBox.Show("Please select an item to remove", "Input Error");
            }
            removeSelectedFileExtensionButton.IsDefault = false;
        }

        private void fileExtensionToAddButton_Click(object sender, RoutedEventArgs e)
        {
            if(fileExtensionToAddTextBox.Text != null && fileExtensionToAddTextBox.Text != "")
            {
                if (!listOfFileExtensionsToCopyBox.Items.Contains("*." + fileExtensionToAddTextBox.Text))
                {
                    //TODO: make sure regular expression only allows lower case letters
                    if (Regex.IsMatch(fileExtensionToAddTextBox.Text.ToString(), "^[a-z]+$"))
                    {
                        listOfFileExtensionsToCopyBox.Items.Add("*." + fileExtensionToAddTextBox.Text);
                        fileCopier.addFileExtensionToCopy("*." + fileExtensionToAddTextBox.Text);
                    }
                    else
                    {
                        MessageBox.Show("Please enter lowercase letters only", "Input Error");
                    }
                }
                else
                {
                    MessageBox.Show("File extension is already included", "Input Error");
                }
                
                
            }
            else
            {
                MessageBox.Show("No text entered, please input something", "Input Error");
            }
            fileExtensionToAddButton.IsDefault = false;
        }

        private void listOfDriveLettersToExludeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateBitLockerPasswordButton.IsDefault = false;
            letterToAddToDrivesToExcludeButton.IsDefault = false;
            removeSelectedFileExtensionButton.IsDefault = false;
            fileExtensionToAddButton.IsDefault = false;
            removeSelectedDriveLetterButton.IsDefault = true;
        }

        private void letterToAddToDrivesToExcludeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateBitLockerPasswordButton.IsDefault = false;
            removeSelectedDriveLetterButton.IsDefault = false;
            removeSelectedFileExtensionButton.IsDefault = false;
            fileExtensionToAddButton.IsDefault = false;
            letterToAddToDrivesToExcludeButton.IsDefault = true;
        }

        private void listOfFileExtensionsToCopyBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateBitLockerPasswordButton.IsDefault = false;
            letterToAddToDrivesToExcludeButton.IsDefault = false;
            removeSelectedDriveLetterButton.IsDefault = false;
            fileExtensionToAddButton.IsDefault = false;
            removeSelectedFileExtensionButton.IsDefault = true;
        }

        private void fileExtensionToAddTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateBitLockerPasswordButton.IsDefault = false;
            letterToAddToDrivesToExcludeButton.IsDefault = false;
            removeSelectedDriveLetterButton.IsDefault = false;
            removeSelectedFileExtensionButton.IsDefault = false;
            fileExtensionToAddButton.IsDefault = true;
        }

        private void bitlockerPasswordChanged(object sender, RoutedEventArgs e)
        {
            letterToAddToDrivesToExcludeButton.IsDefault = false;
            removeSelectedDriveLetterButton.IsDefault = false;
            removeSelectedFileExtensionButton.IsDefault = false;
            fileExtensionToAddButton.IsDefault = false;
            updateBitLockerPasswordButton.IsDefault = true;
        }
    }
}