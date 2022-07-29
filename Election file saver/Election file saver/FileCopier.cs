using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Election_file_saver
{
    internal class FileCopier
    {
        //Destination will need to be \\city.a2\Shared\S01Usr\CLERK\Elections\$electionYear Election Information\Voter History\$electionDate\$precinctNumber
        static string destinationPath = @"\\city.a2\Shared\IT_Services\Helpdesk\Scripts\Election files\";
        //static string destinationPath = @"\\city.a2\Shared\S01Usr\CLERK\Elections\2022 Election Information\Voter History\2022-08-02\";
        static string localDestinationPath = @"C:\Election_Data";
        static string sourcePath = @"D:\";
        DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
        DirectoryInfo destinationDir = new DirectoryInfo(destinationPath);

        
        

        //default constructor
        public FileCopier()
            {
                
                
            }



        //copy files from flash drive to network, locally
        //files need to be in a folder based on preceint name
        public void CopyFiles(string precinct, bool allowFileOverwrite)
        {
            //if allowFileOverwrite is true it will allow files to be overwritten. If not it won't overwrite anything

            try
            {

                //all files except root files
                DirectoryInfo[] directories = sourceDir.GetDirectories("*",SearchOption.AllDirectories);
                List<FileInfo> filesList = new List<FileInfo>();
                string pathToCopyTo;
                string localPathToCopyTo;
                //create the directory if it doesn't exist
                if (!Directory.Exists(Path.Combine(destinationPath,precinct)))
                {
                    Directory.CreateDirectory(Path.Combine(destinationPath, precinct));
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
                    filesList.AddRange(dir.GetFiles());
                }

                foreach(var file in filesList)
                {
                    pathToCopyTo = Path.Combine(Path.Combine(destinationPath, precinct), file.Name);
                    localPathToCopyTo = Path.Combine(Path.Combine(localDestinationPath, precinct), file.Name);

                    try
                    {
                        File.Copy(file.FullName, pathToCopyTo, allowFileOverwrite);
                        File.Copy(file.FullName, localPathToCopyTo, allowFileOverwrite);
                    }
                    catch (IOException copyError)
                    {
                        Console.WriteLine(copyError.Message);
                    }
                    
                }

                //root directory files
                foreach(var file in sourceDir.GetFiles())
                {
                    pathToCopyTo = Path.Combine(Path.Combine(destinationPath, precinct), file.Name);
                    localPathToCopyTo = Path.Combine(Path.Combine(localDestinationPath, precinct), file.Name);

                    try
                    {
                        File.Copy(file.FullName, pathToCopyTo, allowFileOverwrite);
                        File.Copy(file.FullName, localPathToCopyTo, allowFileOverwrite);
                    }
                    catch(IOException copyError)
                    {
                        Console.WriteLine(copyError.Message);
                    }
                    
                }
    
            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }

            
        }

        //print files
        public void PrintFiles()
        {
            //System.Diagnostics.Process process = new System.Diagnostics.Process();
            //System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            //startInfo.FileName = "cmd.exe";
            //const string quote = "\"";
            //string arg = "/C PDFtoPrinter.exe \"D:\\drivecloner.pdf\"";
            //startInfo.Arguments = arg;
            //process.StartInfo = startInfo;
            //process.Start();

            try
            {

                //all files except root files
                DirectoryInfo[] directories = sourceDir.GetDirectories("*", SearchOption.AllDirectories);
                List<FileInfo> filesList = new List<FileInfo>();


                foreach (var dir in directories)
                {
                    filesList.AddRange(dir.GetFiles());
                }

                //string printArgument ;
                foreach (var file in filesList)
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    const string quote = "\"";
                    string arg = "/C PDFtoPrinter.exe \"" + file.FullName + "\" & timeout 15"; //want to try to add wait to improve printing. If that doesn't work maybe try a way to combime pdfs the print one large file to print
                    startInfo.Arguments = arg;
                    //startInfo.Verb = "runas";
                    process.StartInfo = startInfo;
                    process.Start();
                }

                


            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }
        }

    }
}
