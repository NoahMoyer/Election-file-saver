using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;



namespace Election_file_saver
{
    using BitLockerManager;
    internal class FileCopier
    {
        //Destination will need to be \\city.a2\Shared\S01Usr\CLERK\Elections\$electionYear Election Information\Voter History\$electionDate\$precinctNumber
        //static string destinationPath = @"\\city.a2\Shared\IT_Services\Helpdesk\Scripts\Election files\";
        static string networkDestinationPath = @"\\city.a2\Shared\S01Usr\CLERK\Elections\2022 Election Information\Voter History\2022-08-02\";
        static string localDestinationPath = @"C:\Election_Data";
        static string sourcePath = @"E:\";
        DirectoryInfo localDir = new DirectoryInfo(localDestinationPath);
        DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
        DirectoryInfo destinationDir = new DirectoryInfo(networkDestinationPath);
        static private DriveInfo[] allDrivesArray;
        public List<DriveInfo> allDrives;
        public string settingsFileName = "settings.csv";
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
        }
        public void setBitLockerPassword(string newBitLockerPassword)
        {
            bitLockerPassword = newBitLockerPassword;

            //need to write the new password to the file as well
            File.WriteAllText(settingsFileName, "bitlockerPassword," + bitLockerPassword);
        }
        public void unlockBitLocker()
        {

            bitManager.UnlockDriveWithPassphrase(bitLockerPassword);
        }

        public void setLocalDestinationPath(string newLocalDestinationInput)
        {
            localDestinationPath = newLocalDestinationInput;
        }

        public void setNetworkDestinationPath(string newNetworkDestinationPath)
        {
            networkDestinationPath = newNetworkDestinationPath;
        }

        public void updateDrives()
        {
            DriveInfo[] allDrivesArrayNew = DriveInfo.GetDrives();
            List<DriveInfo> allDrivesNew = new List<DriveInfo>(allDrivesArrayNew);
            allDrives.Clear();
            allDrivesNew.RemoveAll(p => p.Name.Contains("G") || p.Name.Contains("C") || p.Name.Contains("U") || p.Name.Contains("S"));

            allDrives = allDrivesNew;
            
            
        }

        public void setSourcePath(object labelInputName)
        {
            //update drive to copy from
            foreach (var drive in allDrives)
            {
                if (drive == labelInputName)
                {
                    //sourcePath = drive.VolumeLabel;
                    sourceDir = drive.RootDirectory;
                }
            }
            //update bitlocker
            foreach (DriveInfo drive in allDrives)
            {
                string sourceDrive = sourceDir.Root.ToString();
                if (drive.Name.Contains(sourceDrive))
                {
                    bitManager = new BitLockerManager(drive);
                }
            }
        }

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
                    filesList.AddRange(dir.GetFiles("*.pdf", System.IO.SearchOption.TopDirectoryOnly));
                    filesList.AddRange(dir.GetFiles("*.accdb", System.IO.SearchOption.TopDirectoryOnly));
                    filesList.AddRange(dir.GetFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly));
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

                var extensions = new string[] { "*.pdf", "*.accdb", "*.csv" };
                //root directory files
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
