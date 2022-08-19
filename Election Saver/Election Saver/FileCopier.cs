using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;


namespace Election_Saver
{
    using BitLockerManager;
    using System.Windows;
    

    internal class FileCopier
    {
        //Destination will need to be \\city.a2\Shared\S01Usr\CLERK\Elections\$electionYear Election Information\Voter History\$electionDate\$precinctNumber
        static string networkDestinationPath; //= @"\\city.a2\Shared\IT_Services\Helpdesk\Scripts\Election files\";
        //static string networkDestinationPath = @"\\city.a2\Shared\S01Usr\CLERK\Elections\2022 Election Information\Voter History\2022-08-02\";
        static string localDestinationPath;
        static string sourcePath;// = @"E:\";
        DirectoryInfo localDir;
        DirectoryInfo sourceDir;
        DirectoryInfo destinationDir;
        static private DriveInfo[] allDrivesArray;
        public List<DriveInfo> allDrives;
        public List<string> listOfDriveLettersToExlude = new List<string>();
        public List<string> listOfFileExtensionsToCopy = new List<string>();
        string extensionPrefix = "*.";
        public string settingsFileName = @"C:\Temp\settings.csv";
        public BitLockerManager bitManager;
        public string bitLockerPassword = "a2CityClerksOffice!";


        //default constructor
        public FileCopier()
        {
            getSettings();

            destinationDir = new DirectoryInfo(networkDestinationPath);
            localDir = new DirectoryInfo(localDestinationPath);
            sourceDir = new DirectoryInfo(sourcePath);

            allDrivesArray = DriveInfo.GetDrives();
            allDrives = new List<DriveInfo>(allDrivesArray);
            List<int> indexOfDrivesToRemove = new List<int>();
            foreach (string driveLetter in listOfDriveLettersToExlude)
            {
                allDrives.RemoveAll(p => p.Name.Contains(driveLetter));
            }
            allDrives.RemoveAll(p => !p.IsReady);

            

            
           

            //establish bitlocker
            foreach (DriveInfo drive in allDrives)
            {
                string sourceDrive = sourceDir.Root.ToString();
                if (drive.Name.Contains(sourceDrive) && drive.IsReady)
                {
                    bitManager = new BitLockerManager(drive);
                }
            }
        }

        public void getSettings()
        {
            if (File.Exists(settingsFileName))
            {
                using (TextFieldParser csvParser = new TextFieldParser(settingsFileName))
                {
                    csvParser.CommentTokens = new string[] { "#" };
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = true;
                    string[] fields;

                    //read line and add each field to a entry in the array
                    fields = csvParser.ReadFields();//bitlocker password
                    bitLockerPassword = fields[1];
                    fields = csvParser.ReadFields(); //network destination
                    networkDestinationPath = fields[1];
                    fields = csvParser.ReadFields(); //local destination
                    localDestinationPath = fields[1];
                    fields = csvParser.ReadFields(); //default drive letter
                    sourcePath =  fields[1];
                    fields = csvParser.ReadFields(); //drive letters to exlude
                    listOfDriveLettersToExlude.Clear();//want to clear the list before we make it again
                    foreach (var letter in fields)
                    {
                        if(letter.Length == 1)
                        {
                            listOfDriveLettersToExlude.Add(letter);
                        }
                        
                    }
                    fields = csvParser.ReadFields(); //files extentions to copy
                    for (int i = 1; i < fields.Length; i++)
                    {
                            listOfFileExtensionsToCopy.Add(extensionPrefix + fields[i]);
                    }


                }

            }
            else
            {
                MessageBox.Show("No settings file found at C:\\Temp\\settings.csv" +
                    "\nSettings file with format like this needs to be created:" +
                    "\n\nbitlockerPassword,a2CityClerksOffice!" +
                    "\nNetworkDestination,S:\\Helpdesk\\Scripts\\Election files" +
                    "\nLocalDestination,C:\\Election_Data" +
                    "\ndefaultSourceDrive,D:\\" +
                    "\ndriveLettersToExclude,G,C,U,S" +
                    "\nfileExtensionsToCopy,accdb,csv,pdf" +
                    "\n\nfirst column is just the name/description of which setting it is. Needs to be in this order. " +
                    "\nSecond columnis the actual setting." +
                    "\nCreate this file then you can use the application.", "No settings file");
                System.Environment.Exit(1);
            }

            //TODO: make sure to create a settings file if it doesn't exist
        }
        /// <summary>
        /// This function is desinged to set the current settings so that they are saved and can be referenced when needed. 
        /// This function should be called any time a setting is changed so that we are keeping the settings up to date
        /// </summary>
        public void setSettings()
        {
            File.WriteAllText(settingsFileName, "bitlockerPassword," + bitLockerPassword);
            File.AppendAllText(settingsFileName, "\nNetworkDestination," + networkDestinationPath);
            File.AppendAllText(settingsFileName, "\nLocalDestination," + localDestinationPath);
            File.AppendAllText(settingsFileName, "\ndefaultSourceDrive," + sourcePath);
            File.AppendAllText(settingsFileName, "\ndriveLettersToExclude"); 
            foreach(var letter in listOfDriveLettersToExlude)
            {
                File.AppendAllText(settingsFileName, "," + letter);
            }
            File.AppendAllText(settingsFileName, "\nfileExtensionsToCopy");
            foreach(var extension in listOfFileExtensionsToCopy)
            {
                if (extension.StartsWith(extensionPrefix))
                {
                    File.AppendAllText(settingsFileName, "," + extension.Substring(extensionPrefix.Length));
                }
                else if (Regex.IsMatch(extension, "[a-z]"))
                {
                    File.AppendAllText(settingsFileName, "," + extension);
                }
                
            }

        }
        public void setBitLockerPassword(string newBitLockerPassword)
        {
            bitLockerPassword = newBitLockerPassword;
            setSettings();
            
        }
        public void unlockBitLocker()
        {

            bitManager.UnlockDriveWithPassphrase(bitLockerPassword);
        }
        public List<string> getListOfFileExtensionsToCopy()
        {
            return listOfFileExtensionsToCopy;
        }
        public void removeSpecificFileExtensionToCopy(string fileExtenstionToRemoveFromCopy)
        {
            listOfFileExtensionsToCopy.Remove(fileExtenstionToRemoveFromCopy);
            setSettings();
        }
        public void setFileExtensionsToCopy(List<string> newListOfFileExtensionsToCopy)
        {
            listOfFileExtensionsToCopy.Clear();
            listOfFileExtensionsToCopy = newListOfFileExtensionsToCopy;
            setSettings();
        }
        public void addFileExtensionToCopy(string newFileExtensionToCopy)
        {
            listOfFileExtensionsToCopy.Add(newFileExtensionToCopy);
            setSettings();
        }
        public List<string> getDriveLettersToExclude()
        {
            return listOfDriveLettersToExlude;
        }
        public void setDriveLettersToExclude(List<string> newListOfDriveLettersToExclude)
        {
            listOfDriveLettersToExlude.Clear();
            listOfDriveLettersToExlude = newListOfDriveLettersToExclude;
            setSettings();
        }
        public void removeSepcicDriveLetterToExclude(string driverLetterToRemoveFromExclusion)
        {
            listOfDriveLettersToExlude.Remove(driverLetterToRemoveFromExclusion);
            setSettings();
        }
        public string getSourcePath()
        {
            return sourcePath;
        }
        public void setSourcePath(string newSourcePath)
        {
            sourcePath = newSourcePath;
            sourceDir = new DirectoryInfo(sourcePath);
            setSettings();
        }
        public string getLocalDestinationPath()
        {
            return localDestinationPath;
        }

        public void setLocalDestinationPath(string newLocalDestinationInput)
        {
            localDestinationPath = newLocalDestinationInput;
            localDir = new DirectoryInfo(localDestinationPath);
            setSettings();
        }

        public string getNetworkDestinationPath()
        {
            return networkDestinationPath;
        }

        public void setNetworkDestinationPath(string newNetworkDestinationPath)
        {
            networkDestinationPath = newNetworkDestinationPath;
            destinationDir = new DirectoryInfo(newNetworkDestinationPath);
            setSettings();
        }

        public void updateDrives()
        {
            DriveInfo[] allDrivesArrayNew = DriveInfo.GetDrives();
            List<DriveInfo> allDrivesNew = new List<DriveInfo>(allDrivesArrayNew);
            allDrives.Clear();
            foreach (string driveLetter in listOfDriveLettersToExlude)
            {
                allDrivesNew.RemoveAll(p => p.Name.Contains(driveLetter));
            }
            allDrivesNew.RemoveAll(p => !p.IsReady);
            allDrives = allDrivesNew;
            
            
        }

        public void setSourcePath(object labelInputName)
        {
            //update drive to copy from
            foreach (var drive in allDrives)
            {
                if (drive == labelInputName && drive.IsReady)
                {
                    //sourcePath = drive.VolumeLabel;
                    sourceDir = drive.RootDirectory;
                }
            }
            //update bitlocker
            foreach (DriveInfo drive in allDrives)
            {
                string sourceDrive = sourceDir.Root.ToString();
                if (drive.Name.Contains(sourceDrive) && drive.IsReady)
                {
                    bitManager = new BitLockerManager(drive);
                }
            }
        }



        //copy files from flash drive to network, locally
        //files need to be in a folder based on preceint name
        public void CopyFiles(string precinct, bool allowFileOverwrite)
        {
            //if allowFileOverwrite is true it will allow files to be overwritten. If not it won't overwrite anything

            try
            {

                //all files except root files
                DirectoryInfo[] directories = sourceDir.GetDirectories("*", System.IO.SearchOption.AllDirectories);
                List<FileInfo> filesList = new List<FileInfo>();
                string pathToCopyTo;
                string localPathToCopyTo;
                //create the directory if it doesn't exist
                if (!Directory.Exists(Path.Combine(networkDestinationPath,precinct)))
                {
                    Directory.CreateDirectory(Path.Combine(networkDestinationPath, precinct));
                }
                

                //if local paths don't exist
                if (!Directory.Exists(Path.Combine(localDestinationPath, precinct)))
                {
                    Directory.CreateDirectory(Path.Combine(localDestinationPath, precinct));
                }
                
                if (!Directory.Exists(localDestinationPath))
                {
                    Directory.CreateDirectory(localDestinationPath);
                }

                foreach (var dir in directories)
                {
                    foreach(var ext in listOfFileExtensionsToCopy)
                    {
                        filesList.AddRange(dir.GetFiles(ext, System.IO.SearchOption.TopDirectoryOnly));
                    }
                    //filesList.AddRange(dir.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                    //filesList.AddRange(dir.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                    //filesList.AddRange(dir.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));
                }

                foreach(var file in filesList)
                {
                    pathToCopyTo = Path.Combine(Path.Combine(networkDestinationPath, precinct), file.Name);
                    localPathToCopyTo = Path.Combine(Path.Combine(localDestinationPath, precinct), file.Name);

                    try
                    {
                        File.Copy(file.FullName, pathToCopyTo, allowFileOverwrite);
                    }
                    catch (IOException copyError)
                    {
                        Console.WriteLine(copyError.Message);
                    }


                    try
                    {
                        File.Copy(file.FullName, localPathToCopyTo, allowFileOverwrite);
                    }
                    catch (IOException copyError)
                    {
                        Console.WriteLine(copyError.Message);
                    }

                }

                //var extensions = new string[] { "*.pdf", "*.accdb", "*.csv" };
                //root directory files
                foreach (var ext in listOfFileExtensionsToCopy)
                {
                    foreach (var file in sourceDir.GetFiles(ext, System.IO.SearchOption.TopDirectoryOnly))
                    {
                        pathToCopyTo = Path.Combine(Path.Combine(networkDestinationPath, precinct), file.Name);
                        localPathToCopyTo = Path.Combine(Path.Combine(localDestinationPath, precinct), file.Name);

                        try
                        {
                            File.Copy(file.FullName, pathToCopyTo, allowFileOverwrite);
                        }
                        catch (IOException copyError)
                        {
                            Console.WriteLine(copyError.Message);
                        }


                        try
                        {
                            File.Copy(file.FullName, localPathToCopyTo, allowFileOverwrite);
                        }
                        catch (IOException copyError)
                        {
                            Console.WriteLine(copyError.Message);
                        }

                    }
                }
                
    
            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }

            
        }
       
        //print files
        public async void PrintFiles(int waitTimeInSeconds, string precinct)
        {
            
            int fileCount = 0; //using so we know how many times to run the counter for the progress bar
            string precintPath = Path.Combine(localDestinationPath, precinct);
            try
            {
                
                //all files except root files
                DirectoryInfo localPrecinct = new DirectoryInfo(precintPath);
                DirectoryInfo[] directories = localPrecinct.GetDirectories("*", System.IO.SearchOption.AllDirectories);
                List<FileInfo> filesList = new List<FileInfo>();


                foreach (var dir in directories)
                {
                    //filesList.AddRange(dir.GetFiles("*.pdf", SearchOption.TopDirectoryOnly));
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    //const string quote = "\"";
                    string arg = $"/C PDFtoPrinter.exe \"{dir.FullName}\\*.pdf\""; //want to try to add wait to improve printing. If that doesn't work maybe try a way to combime pdfs the print one large file to print
                    startInfo.Arguments = arg;
                    //startInfo.Verb = "runas";
                    process.StartInfo = startInfo;
                    process.Start();
                    await Task.Delay(waitTimeInSeconds * 1000);
                }

                int i = 0;
                
                //files in all directories other than root
                if(filesList.Count > 0) //check if there was anything in the sub directories
                {
                    do
                    {
                        //var task = Task.Run(() =>
                        //{

                            //System.Diagnostics.Process process = new System.Diagnostics.Process();
                            //System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                            //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            //startInfo.FileName = "cmd.exe";
                            ////const string quote = "\"";
                            //string arg = "/C PDFtoPrinter.exe \"" + filesList.ElementAt(i).FullName + "\""; //want to try to add wait to improve printing. If that doesn't work maybe try a way to combime pdfs the print one large file to print
                            //startInfo.Arguments = arg;
                            ////startInfo.Verb = "runas";
                            //process.StartInfo = startInfo;
                            //process.Start();
                            //System.Threading.Thread.Sleep(waitTimeInSeconds * 1000);
                            //fileCount++;
                            //i++;

                        //});
                        //taskList.Add(task);
                    } while (i < filesList.Count);
                }



                //files in root directory
                if(true)
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    //const string quote = "\"";
                    string arg = $"/C PDFtoPrinter.exe \"{localPrecinct}\\*.pdf\""; //want to try to add wait to improve printing. If that doesn't work maybe try a way to combime pdfs the print one large file to print
                    startInfo.Arguments = arg;
                    //startInfo.Verb = "runas";
                    process.StartInfo = startInfo;
                    process.Start();
                    await Task.Delay(waitTimeInSeconds * 1000);
                }
                


                //FileInfo[] rootFiles = localPrecinct.GetFiles("*.pdf", SearchOption.TopDirectoryOnly);

                //int j = 0;
                //do
                //{
                //    //var task = Task.Run(() =>
                //    //{
                //    System.Diagnostics.Process process = new System.Diagnostics.Process();
                //    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                //    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                //    startInfo.FileName = "cmd.exe";
                //    //const string quote = "\"";
                //    string arg = "/C PDFtoPrinter.exe \"" + rootFiles.ElementAt(j).FullName + "\""; //want to try to add wait to improve printing. If that doesn't work maybe try a way to combime pdfs the print one large file to print
                //    startInfo.Arguments = arg;
                //    //startInfo.Verb = "runas";
                //    process.StartInfo = startInfo;
                //    process.Start();
                //    System.Threading.Thread.Sleep(waitTimeInSeconds * 1000);
                //    fileCount++;
                //    j++;

                //    //});
                //    //taskList.Add(task);
                //} while (j < rootFiles.Length);


            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }

            
        }

    }
}
