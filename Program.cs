using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;

namespace UnlockBrowsers
{
    internal class Program
    {
        static string userName;
        static string currentDirectory;

        static void Main(string[] args)
        {
            userName = Environment.UserName;
            currentDirectory = Environment.CurrentDirectory;

            Console.WriteLine("Fixing Permissions");
            FixFolderPerms($@"C:\Users\{userName}\AppData\Local\Mozilla");
            FixFolderPerms($@"C:\Users\{userName}\AppData\Local\Mozilla Firefox");
            FixFolderPerms($@"C:\Users\{userName}\AppData\Roaming\Mozilla");

            GetExtrator();
            
            InstallApp();
            Console.WriteLine("Program Finished, Press a key to close");
            Console.ReadKey();
        }

        static void FixFolderPerms(string folderPath)
        {
            // Get the current access control list for the folder
            DirectoryInfo di = new DirectoryInfo(folderPath);
            DirectorySecurity ds = di.GetAccessControl();

            // Get the list of access control entries for the folder
            AuthorizationRuleCollection acl = ds.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));

            // Loop through the ACEs and remove any that have the "Deny" flag set
            foreach (FileSystemAccessRule ace in acl)
            {
                if (ace.AccessControlType == AccessControlType.Deny)
                {
                    // Remove the ACE from the access control list
                    ds.RemoveAccessRule(ace);
                }
            }

            // Apply the modified access control list to the folder
            di.SetAccessControl(ds);
        }

        static void GetExtrator()
        {
            if(File.Exists($"{currentDirectory}\\7z.exe"))
                File.Delete($"{currentDirectory}\\7z.exe");
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UnlockBrowsers.7z.exe");
            FileStream fileStream = new FileStream($"{currentDirectory}\\7z.exe", FileMode.CreateNew);
            for (int i = 0; i < stream.Length; i++)
                fileStream.WriteByte((byte)stream.ReadByte());
            fileStream.Close();
        }
        
        static void InstallApp()
        {
            Console.WriteLine("Downloading Installer");
            string url = "https://download.mozilla.org/?product=firefox-latest-ssl&os=win64&lang=en-US";

            // Path to save the downloaded file
            string installerPath = $"{currentDirectory}\\FirefoxInstaller.exe";

            // Download the file using WebClient
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, installerPath);
            }

            Console.WriteLine("Checking for previous failed install");
            string extractPath = $"{currentDirectory}\\Mozilla Firefox";

            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            string extractArguments = $"x \"{installerPath}\" -o\"{extractPath}\"";

            Console.WriteLine("Extracting new install");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = $"{currentDirectory}\\7z.exe"; // path to 7-Zip executable
            startInfo.Arguments = extractArguments;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            process.WaitForExit();

            File.Delete(extractPath + "\\setup.exe");

            string sourceFolder = extractPath + "\\core";

            // Get the path of the parent folder of the source folder
            string parentFolder = Directory.GetParent(sourceFolder).FullName;

            // Move the files and folders from the source folder to the parent folder
            foreach (string file in Directory.GetFiles(sourceFolder))
            {
                File.Move(file, Path.Combine(parentFolder, Path.GetFileName(file)));
            }

            foreach (string subDirectory in Directory.GetDirectories(sourceFolder))
            {
                Directory.Move(subDirectory, Path.Combine(parentFolder, Path.GetFileName(subDirectory)));
            }

            // Delete the empty source folder
            Directory.Delete(sourceFolder);

            Console.WriteLine("Moving extracted files");

            try
            { 
                if (Directory.Exists($@"C:\Users\{userName}\AppData\Local\Mozilla Firefox"))
                {
                    Directory.Delete($@"C:\Users\{userName}\AppData\Local\Mozilla Firefox", true);
                }
                Directory.Move($"{currentDirectory}\\Mozilla Firefox", $@"C:\Users\{userName}\AppData\Local\Mozilla Firefox");
            }
            catch(Exception ex)
            {
                File.Delete($"{currentDirectory}\\7z.exe");
                Directory.Delete(extractPath, true);
                Console.WriteLine($"We got a problem: Exception:{ex}");
                Console.ReadKey();
                Environment.Exit(0);
            }

            File.Delete($"{currentDirectory}\\7z.exe");
            File.Delete(installerPath);
        }
    }
}
