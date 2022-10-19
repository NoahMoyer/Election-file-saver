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
        static string networkDestinationPath; //= @"\\city.a2\Shared\IT_Services\Helpdesk\Scripts\Election files\";//static string networkDestinationPath = @"\\city.a2\Shared\S01Usr\CLERK\Elections\2022 Election Information\Voter History\2022-08-02\";
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
        Encryption encryptor = new Encryption();

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
            //allDrives.RemoveAll(p => !p.IsReady); Edge case that isn't working as expected. TODO fix exclusion of drives that are not useable.

            
            

            
           

            //establish bitlocker
            foreach (DriveInfo drive in allDrives)
            {
                string sourceDrive = sourceDir.Root.ToString();
                if (drive.Name.Contains(sourceDrive)/* && drive.IsReady || getDriveLockStatus(drive) == "Locked"*/)
                {
                    bitManager = new BitLockerManager(drive);
                }
            }
        }
        /// <summary>
        /// Function to get the settings from the settings file. Settings file is stored in C:\Temp\settings.csv
        /// </summary>
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
                    bitLockerPassword = encryptor.Decrypt(fields[1]);
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

            
        }
        /// <summary>
        /// This function is desinged to set the current settings so that they are saved and can be referenced when needed. 
        /// This function should be called any time a setting is changed so that we are keeping the settings up to date
        /// </summary>
        public void setSettings()
        {
            string encryptedPassword = encryptor.encrypt(bitLockerPassword);
            File.WriteAllText(settingsFileName, "bitlockerPassword," + encryptedPassword);
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
        /// <summary>
        /// Funciton to change the bitlocker password in the settings file
        /// </summary>
        /// <param name="newBitLockerPassword"></param>
        public void setBitLockerPassword(string newBitLockerPassword)
        {
            bitLockerPassword = newBitLockerPassword;
            setSettings();
        }
        /// <summary>
        /// Function to unlock the bitlocker encrypted drive
        /// </summary>
        public void unlockBitLocker()
        {
            try
            {
                bitManager.UnlockDriveWithPassphrase(bitLockerPassword);
            }
            catch(Exception Error)
            {
                MessageBox.Show("Drive unlock error. Is the password correct?", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Needs summary from Nathan
        /// </summary>
        /// <returns></returns>
        public string getDriveLockStatus()
        {
            BitlockerManager.Enums.LockStatus status;
            string stringStatus = "YES";
            bitManager.GetLockStatus(out status);
            stringStatus = Enum.GetName(status.GetType(), status);




            return stringStatus;
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <returns></returns>
        public List<string> getListOfFileExtensionsToCopy()
        {
            return listOfFileExtensionsToCopy;
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <param name="fileExtenstionToRemoveFromCopy"></param>
        public void removeSpecificFileExtensionToCopy(string fileExtenstionToRemoveFromCopy)
        {
            listOfFileExtensionsToCopy.Remove(fileExtenstionToRemoveFromCopy);
            setSettings();
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <param name="newListOfFileExtensionsToCopy"></param>
        public void setFileExtensionsToCopy(List<string> newListOfFileExtensionsToCopy)
        {
            listOfFileExtensionsToCopy.Clear();
            listOfFileExtensionsToCopy = newListOfFileExtensionsToCopy;
            setSettings();
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <param name="newFileExtensionToCopy"></param>
        public void addFileExtensionToCopy(string newFileExtensionToCopy)
        {
            listOfFileExtensionsToCopy.Add(newFileExtensionToCopy);
            setSettings();
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <returns></returns>
        public List<string> getDriveLettersToExclude()
        {
            return listOfDriveLettersToExlude;
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <param name="newListOfDriveLettersToExclude"></param>
        public void setDriveLettersToExclude(List<string> newListOfDriveLettersToExclude)
        {
            listOfDriveLettersToExlude.Clear();
            listOfDriveLettersToExlude = newListOfDriveLettersToExclude;
            setSettings();
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <param name="driverLetterToRemoveFromExclusion"></param>
        public void removeSepcicDriveLetterToExclude(string driverLetterToRemoveFromExclusion)
        {
            listOfDriveLettersToExlude.Remove(driverLetterToRemoveFromExclusion);
            setSettings();
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <param name="newSourcePath"></param>
        public void setSourcePath(string newSourcePath)
        {
            sourcePath = newSourcePath;
            sourceDir = new DirectoryInfo(sourcePath);
            setSettings();
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <returns></returns>
        public string getLocalDestinationPath()
        {
            return localDestinationPath;
        }
        /// <summary>
        /// Function to set the local file path on the computer
        /// </summary>
        /// <param name="newLocalDestinationInput"></param>
        public void setLocalDestinationPath(string newLocalDestinationInput)
        {
            localDestinationPath = newLocalDestinationInput;
            localDir = new DirectoryInfo(localDestinationPath);
            setSettings();
        }
        /// <summary>
        /// Needs summary from Noah
        /// </summary>
        /// <returns></returns>
        public string getNetworkDestinationPath()
        {
            return networkDestinationPath;
        }
        /// <summary>
        /// Function to set the network file path to save files to
        /// </summary>
        /// <param name="newNetworkDestinationPath"></param>
        public void setNetworkDestinationPath(string newNetworkDestinationPath)
        {
            networkDestinationPath = newNetworkDestinationPath;
            destinationDir = new DirectoryInfo(newNetworkDestinationPath);
            setSettings();
        }
        /// <summary>
        /// Function to update the drives list based on what is available on the computer.
        /// 
        /// 
        /// </summary>
        public void updateDrives()
        {
            DriveInfo[] allDrivesArrayNew = DriveInfo.GetDrives();
            List<DriveInfo> allDrivesNew = new List<DriveInfo>(allDrivesArrayNew);
            allDrives.Clear();
            foreach (string driveLetter in listOfDriveLettersToExlude)
            {
                allDrivesNew.RemoveAll(p => p.Name.Contains(driveLetter));
            }
            //allDrives.RemoveAll(p => !p.IsReady); Edge case that isn't working as expected. TODO fix exclusion of drives that are not useable.
            allDrives = allDrivesNew;
            
            
        }
        /// <summary>
        /// Funciton to set he source path of the drive the files will be copied from
        /// </summary>
        /// <param name="labelInputName"></param>
        public void setSourcePath(object labelInputName)
        {
            //update drive to copy from
            //This look updaes the list to display to the user
            foreach (var drive in allDrives)
            {
                if (drive == labelInputName/* && drive.IsReady*/)
                {
                    //sourcePath = drive.VolumeLabel; //this line was not commented out in Nathan's version. If there are issue will need to take a look at this
                    sourceDir = drive.RootDirectory;
                }
            }
            //update bitlocker
            //This updates the bitManager to tell it what drive we want to unlock
            foreach (DriveInfo drive in allDrives)
            {
                string sourceDrive = sourceDir.Root.ToString();
                if (drive.Name.Contains(sourceDrive)/* && drive.IsReady*/)
                {
                    try
                    {
                        bitManager = new BitLockerManager(drive);
                    }
                    catch (Exception copyError)
                    {
                        MessageBox.Show("Do you have permission to unlock BitLocker encrypted drives from the command line?", copyError.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        /// <summary>
        /// copy files from flash drive to network, locally
        /// files need to be in a folder based on preceint name
        /// 
        /// Currenlty only copies the following file types:
        /// .pdf, .accdb, and .csv
        /// 
        /// Does not keep file structure of the source drive. It pulss all files of the specified file types, creates a folder based on the precinct, and
        /// copies them to the root of that folder it created
        /// </summary>
        /// <param name="precinct"></param>
        /// <param name="allowFileOverwrite"></param>
        public string getSourcePath()
        {
            return sourcePath;
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


                //if the local paths to copy to don't exist
                //create C:\Election_Data\{precict}
                if (!Directory.Exists(Path.Combine(localDestinationPath, precinct)))
                {
                    Directory.CreateDirectory(Path.Combine(localDestinationPath, precinct));
                }
                //create C:\Election_Data
                if (!Directory.Exists(localDestinationPath))
                {
                    Directory.CreateDirectory(localDestinationPath);
                }
                //adding each file into the fileList
                foreach (var dir in directories)
                {
                    foreach(var ext in listOfFileExtensionsToCopy)
                    {
                        filesList.AddRange(dir.GetFiles(ext, System.IO.SearchOption.TopDirectoryOnly));
                    }
                }

                //call copy function for each file in the fileList
                foreach (var file in filesList)
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

                //This part of the function copies all of the files in the root directory. 
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
                MessageBox.Show("The below error was thrown by the program: \n \n" + dirNotFound.Message + "\n \nMake sure the drive is plugged in, you have the correct drive selected, and the drive is unlocked.", "Copy ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            
        }

        /// <summary>
        /// Funciton to print all .pdf files.
        /// 
        /// Currenlty prints from the local file path on the computer based on the precinct in the text box. This is to increase print speed and reliability.
        /// 
        /// The waitTimeInSeconds is an integer variable used to wait between each print to prevent overloading the printer queue.
        /// If documents enter the queue too fast for the printer to process it can get missed or fail.
        /// </summary>
        /// <param name="waitTimeInSeconds"></param>
        /// <param name="precinct"></param>
        public async void PrintFiles(int waitTimeInSeconds, string precinct)
        {
            
            //int fileCount = 0; //using so we know how many times to run the counter for the progress bar
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

        /// <summary>
        /// Function that checks what files are currently in the local computer directory the program copies to.
        /// </summary>
        /// <param name="precinct"></param>
        /// <returns></returns>
        public string getAvailableFiles(string precinct)
        {
            List<FileInfo> filesList = new List<FileInfo>();
            string localPath;
            localPath = Path.Combine(localDestinationPath, precinct);
            DirectoryInfo localDirecory = new DirectoryInfo(localPath);
            string fileString = "No files present in current directory.";

            //check if C:\Election_Data exists
            if (!Directory.Exists(localDestinationPath))
            {
                fileString = "Election data folder not detected. Have you copied from the flash drive?";
                return fileString;
            }
            //if the local paths to copy to don't exist
            //check C:\Election_Data\{precict}
            else if (!Directory.Exists(localPath))
            {
                fileString = "Precint folder not detected. Have you copied from the flash drive?";
                return fileString;
            }


            try
            {
                //Creating DirectoryInfo based on the localPath folder.
                DirectoryInfo[] directories = localDirecory.GetDirectories("*", System.IO.SearchOption.TopDirectoryOnly);

                //Check if the localDesitnationPath is the localPath. This tells us if the precint text box is empty. If it's empty we don't want to populate the filesList.
                if (localPath == localDestinationPath)
                {
                    fileString = "Please enter precinct number.";
                    return fileString;
                }
                else
                {
                    //adding files into filesList
                    //.accdb and .csv are currently commented out since we don't print them anyway.
                    foreach (var dir in directories)
                    {
                        //adding each file into the fileList from sub folders to filesList
                        filesList.AddRange(dir.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                        ////filesList.AddRange(dir.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                        ////filesList.AddRange(dir.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));
                    }
                    //adding files from root directory to filesList
                    filesList.AddRange(localDirecory.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                    //filesList.AddRange(localDirecory.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                    //filesList.AddRange(localDirecory.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));

                    fileString = string.Join(",", filesList);
                }

            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }

            //format string to be more readable
            //String should format in the following example:
            //file1.pdf
            //file2.pdf
            string[] input = fileString.Split(new string[] { "," }, StringSplitOptions.None); //delimite string by commas
            string output = string.Join("\n", input); //Join array with new line inbetween each element
            return output;
        }

        /// <summary>
        /// Function that checks what files are on the flash drive and returns them as a string.
        /// </summary>
        /// <returns></returns>
        public string getFlashAvailableFiles()
        {
            List<FileInfo> filesList = new List<FileInfo>();
            string flashPath;
            flashPath = sourcePath;
            DirectoryInfo flashDirecory = new DirectoryInfo(sourceDir.Root.ToString());
            string flashFileString = "No files present in current directory.";

            //Checking if the flash drive is detected.If not we don't want to pupulcate the filesList
            if (!Directory.Exists(sourceDir.Root.ToString()))
            {
                flashFileString = "Flash drive not detected. Is the drive plugged in or still locked?";
                return flashFileString;
            }


            try
            {
                //Creating DirectoryInfo based on the localPath folder
                DirectoryInfo[] directories = flashDirecory.GetDirectories("*", System.IO.SearchOption.TopDirectoryOnly);

                // Check if the sourcePath is the flashPath. This tells us if the flash drive is not selected.If it's empty we don't want to populate the filesList.
                //I'm not sure if this first if fucntion works or even makes sense to have since we are checking if the drive is connected before this.
                if (sourcePath == @"")
                {
                    flashFileString = "Please enter precinct number.";
                    return flashFileString;
                }
                else
                {
                    //adding files into filesList
                    foreach (var dir in directories)
                    {
                        foreach (string fileExtension in listOfFileExtensionsToCopy)
                        {
                            filesList.AddRange(dir.GetFiles(fileExtension, System.IO.SearchOption.TopDirectoryOnly));
                        }
                    }
                    foreach (string fileExtension in listOfFileExtensionsToCopy)
                    {
                        filesList.AddRange(flashDirecory.GetFiles(fileExtension, System.IO.SearchOption.TopDirectoryOnly));
                    }


                    //foreach (var dir in directories)
                    //{
                    //    //adding each file into the fileList from sub folders to filesList
                    //    filesList.AddRange(dir.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                    //    filesList.AddRange(dir.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                    //    filesList.AddRange(dir.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));
                    //}
                    ////adding files from root directory to filesList
                    //filesList.AddRange(flashDirecory.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                    //filesList.AddRange(flashDirecory.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                    //filesList.AddRange(flashDirecory.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));

                    //convert the fileList to a string
                    flashFileString = string.Join(",", filesList);
                }

            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }

            //format string to be more readable
            //String should format in the following example:
            //file1.pdf
            //file2.pdf
            string[] input = flashFileString.Split(new string[] { "," }, StringSplitOptions.None); //delimite string by commas
            string output = string.Join("\n", input); //Join array with new line inbetween each element
            return output;
        }
    }
}
