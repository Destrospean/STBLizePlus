using System;
using System.IO;

namespace Destrospean.STBLizePlus
{
    public class FileSystemUtils
    {
        public enum OS
        {
            Windows,
            Unix
        }

        const string kSTBLizePlusDirectoryFilename = ".IS_CREATED_STBLIZE+_DIR";

        public static ulong CurrentTime
        {
            get
            {
                return ulong.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
        }

        public static OS OperatingSystem
        {
            get
            {
                switch ((int)Environment.OSVersion.Platform)
                {
                    case 4:
                    case 128:
                        return OS.Unix;
                }
                return OS.Windows;
            }
        }

        public static void CreateSTBLizePlusDirectoryFile(string directory)
        {
            Directory.CreateDirectory(directory);
            var createdDirectoryFilePath = directory + Path.DirectorySeparatorChar + kSTBLizePlusDirectoryFilename;
            using (var output = File.Create(createdDirectoryFilePath))
            {
            }
            if (OperatingSystem == OS.Windows)
            {
                File.SetAttributes(createdDirectoryFilePath, File.GetAttributes(createdDirectoryFilePath) | FileAttributes.Hidden);
            }
        }

        public static string GetOutputDirectory(string inputPath, string baseOutputDirectory, params string[] outputFileTypes)
        {
            var pathWithoutExtension = Path.GetFullPath(inputPath.Contains(".") ? inputPath.Substring(0, inputPath.LastIndexOf(".")) : inputPath);
            return baseOutputDirectory ?? (File.Exists(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + kSTBLizePlusDirectoryFilename) ? Path.GetDirectoryName(Path.GetDirectoryName(pathWithoutExtension)) : Path.GetDirectoryName(pathWithoutExtension)) + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + "_" + string.Join("+", outputFileTypes) + "_" + CurrentTime;
        }
    }
}
