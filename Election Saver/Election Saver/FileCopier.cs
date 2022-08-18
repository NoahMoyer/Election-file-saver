using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;


namespace Election_Saver
{
    using BitLockerManager;
    using System.Windows;

    internal class FileCopier
    {
        //Destination will need to be \\city.a2\Shared\S01Usr\CLERK\Elections\$electionYear Election Information\Voter History\$electionDate\$precinctNumber
        //static string networkDestinationPath = @"\\city.a2\Shared\IT_Services\Helpdesk\Scripts\Election files\";
        //static string networkDestinationPath = @"\\city.a2\Shared\S01Usr\CLERK\Elections\2022 Election Information\Voter History\2022-08-02\";
        static string networkDestinationPath = @"\\nathans2\4tb share\electionTest";
        static string localDestinationPath = @"C:\Election_Data";
        static string sourcePath = @"E:\";
        DirectoryInfo localDir = new DirectoryInfo(localDestinationPath);
        DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
        DirectoryInfo destinationDir = new DirectoryInfo(networkDestinationPath);
        static private DriveInfo[] allDrivesArray;
        public List<DriveInfo> allDrives;
        public string settingsFileName = @"C:\Temp\settings.csv";
        public BitLockerManager bitManager;
        public string bitLockerPassword = "a2CityClerksOffice!";


        //default constructor
        public FileCopier()
        {

            allDrivesArray = DriveInfo.GetDrives();
            allDrives = new List<DriveInfo>(allDrivesArray);
            List<int> indexOfDrivesToRemove = new List<int>();
            allDrives.RemoveAll(p => p.Name.Contains("G") || p.Name.Contains("C") || p.Name.Contains("U") || p.Name.Contains("S"));
            
            //establish bitlocker
            foreach (DriveInfo drive in allDrives)
            {
                string sourceDrive = sourceDir.Root.ToString();
                if (drive.Name.Contains(sourceDrive))
                {
                    bitManager = new BitLockerManager(drive);
                }
            }

            getSettings();
        }

        /// <summary>
        /// Function to get the settings from the settings file.
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
                    bitLockerPassword = fields[1];

                }

            }

            //TODO: make sure to create a settings file if it doesn't exist
        }

        /// <summary>
        /// Funciton to change the bitlocker password in the settings file
        /// </summary>
        /// <param name="newBitLockerPassword"></param>
        public void setBitLockerPassword(string newBitLockerPassword)
        {
            bitLockerPassword = newBitLockerPassword;

            //need to write the new password to the file as well
            File.WriteAllText(settingsFileName, "bitlockerPassword," + bitLockerPassword);
        }

        /// <summary>
        /// Function to unlock the bitlocker encrypted drive
        /// </summary>
        public void unlockBitLocker()
        {

            bitManager.UnlockDriveWithPassphrase(bitLockerPassword);
        }

        /// <summary>
        /// Function to set the local file path on the computer
        /// </summary>
        /// <param name="newLocalDestinationInput"></param>
        public void setLocalDestinationPath(string newLocalDestinationInput)
        {
            localDestinationPath = newLocalDestinationInput;
        }

        /// <summary>
        /// Function to set the network file path to save files to
        /// </summary>
        /// <param name="newNetworkDestinationPath"></param>
        public void setNetworkDestinationPath(string newNetworkDestinationPath)
        {
            networkDestinationPath = newNetworkDestinationPath;
        }

        /// <summary>
        /// Function to update the drives list based on what is available on the computer.
        /// 
        /// Currenlty we exclude a number of drive letters to prevent users from copying data from a network drive or the C drive on accident.
        /// Excluded  drive letters:
        /// G, C, U, S
        /// </summary>
        public void updateDrives()
        {
            DriveInfo[] allDrivesArrayNew = DriveInfo.GetDrives();
            List<DriveInfo> allDrivesNew = new List<DriveInfo>(allDrivesArrayNew);
            allDrives.Clear();
            allDrivesNew.RemoveAll(p => p.Name.Contains("G") || p.Name.Contains("C") || p.Name.Contains("U") || p.Name.Contains("S"));

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
                if (drive == labelInputName)
                {
                    sourcePath = drive.Name;
                    sourceDir = drive.RootDirectory;
                }
            }
            //update bitlocker
            //This updates the bitManager to tell it what drive we want to unlock
            foreach (DriveInfo drive in allDrives)
            {
                string sourceDrive = sourceDir.Root.ToString();
                if (drive.Name.Contains(sourceDrive))
                {
                    try
                    {
                        bitManager = new BitLockerManager(drive);
                    }
                    catch (Exception copyError)
                    {
                        MessageBox.Show("Do you have permission to unlock BitLocker encrypted drives from the command line?", copyError.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Function to return the sourcePath variable since this is a private variable.
        /// </summary>
        /// <returns></returns>
        public string getSourcePath()
        {
            return sourcePath;
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

        public void CopyFiles(string precinct, bool allowFileOverwrite)
        {
            //if allowFileOverwrite is true it will allow files to be overwritten. If not it won't overwrite anything

            try
            {

                //This part of the function copies all files except those in the root directory.
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
                    filesList.AddRange(dir.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                    filesList.AddRange(dir.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                    filesList.AddRange(dir.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));
                }

                //call copy function for each file in the fileList
                foreach(var file in filesList)
                {
                    pathToCopyTo = Path.Combine(Path.Combine(networkDestinationPath, precinct), file.Name);
                    localPathToCopyTo = Path.Combine(Path.Combine(localDestinationPath, precinct), file.Name);

                    //copying to network path
                    try
                    {
                        File.Copy(file.FullName, pathToCopyTo, allowFileOverwrite);
                    }
                    catch (IOException copyError)
                    {
                        Console.WriteLine(copyError.Message);
                    }

                    //copying to local file path
                    try
                    {
                        File.Copy(file.FullName, localPathToCopyTo, allowFileOverwrite);
                    }
                    catch (IOException copyError)
                    {
                        Console.WriteLine(copyError.Message);
                    }

                }

                var extensions = new string[] { "*.pdf", "*.accdb", "*.csv" };
                //This part of the function copies all of the files in the root directory. 
                foreach (var ext in extensions)
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

        public string getAvailableFiles(string precinct)
        {
            //This part of the function copies all files except those in the root directory.
            
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
                DirectoryInfo[] directories = localDirecory.GetDirectories("*", System.IO.SearchOption.TopDirectoryOnly);

                //create the directory if it doesn't exist
                if (!Directory.Exists(Path.Combine(networkDestinationPath, precinct)))
                {
                    //To do: maybe throw an error or something if the file path doesn't exist?
                    //Directory.CreateDirectory(Path.Combine(networkDestinationPath, precinct));
                }

                if(localPath == @"C:\Election_Data")
                {
                    fileString = "Please enter precinct number.";
                    return fileString;
                }
                else
                {
                    //adding files into filesList
                    foreach (var dir in directories)
                    {
                        //adding each file into the fileList from sub folders to filesList
                        filesList.AddRange(dir.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                        //filesList.AddRange(dir.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                        //filesList.AddRange(dir.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));
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

        public string getFlashAvailableFiles()
        {
            List<FileInfo> filesList = new List<FileInfo>();
            string flashPath;
            flashPath = sourcePath;
            DirectoryInfo flashDirecory = new DirectoryInfo(sourcePath);
            string flashFileString = "No files present in current directory.";

            //if the local paths to copy to don't exist
            //check C:\Election_Data\{precict}
            if (!Directory.Exists(sourcePath))
            {
                flashFileString = "Flash drive not detected. Is the drive plugged in or still locked?";
                return flashFileString;
            }


            try
            {
                DirectoryInfo[] directories = flashDirecory.GetDirectories("*", System.IO.SearchOption.TopDirectoryOnly);

                

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
                        //adding each file into the fileList from sub folders to filesList
                        filesList.AddRange(dir.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                        filesList.AddRange(dir.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                        filesList.AddRange(dir.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));
                    }
                    //adding files from root directory to filesList
                    filesList.AddRange(flashDirecory.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                    filesList.AddRange(flashDirecory.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                    filesList.AddRange(flashDirecory.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));

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


            //return flashFileString;
        }

    }
}
