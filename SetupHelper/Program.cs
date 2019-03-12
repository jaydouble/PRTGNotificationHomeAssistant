using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetupHelper
{
    class Program
    {
        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        static void CreateDirectoryRecursively(string path)
        {
            string[] pathParts = path.Split('\\');

            String parentPath = String.Join("\\", pathParts.Take(pathParts.Length - 1));
            //Console.WriteLine(parentPath);
            if (!Directory.Exists(parentPath))
            {
                CreateDirectoryRecursively(parentPath);
            }
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
            }
        }

        static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine("No arguments applied");
            }
            else
            {
                // now we have some arguments. We only need 1. The installation location
                String installLocation = args[0].Replace("\"","");
                // Now we can add a PRTG Notification helper to the right directory.
                String outDir = ProgramFilesx86() + "\\PRTG Network Monitor\\Notifications\\EXE\\";
                CreateDirectoryRecursively(outDir);
                // now the directory exists, we can create the file we need:
                String path = outDir + "PRTGNotificationHomeAssistant.bat";
                String createText = "@echo off\r\nREM this is just a wrapper for the exe file\r\n"+
                    "FOR /F \"tokens=1-12 delims= \" %%A in (\"%*\") do (\r\n"+
                    "\t \""+ installLocation +"\\PRTGNotificationHomeAssistant.exe\" %%A %%B %%C %%D %%E %%F %%G %%H %%I %%J %%K %%L\r\n"+
                    ") ";
                try
                {
                    File.WriteAllText(path, createText);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
            }
        }
    }
}
