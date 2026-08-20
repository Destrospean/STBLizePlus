using System;
using System.IO;

namespace Destrospean.STBLizePlus
{
    public class FileSystemUtils
    {
        const string kSTBLizePlusDirectoryFilename = ".IS_CREATED_STBLIZE+_DIR";

        public static ulong CurrentTime
        {
            get
            {
                return ulong.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
        }

        public static void CreateSTBLizePlusDirectoryFile(string directory)
        {
            Directory.CreateDirectory(directory);
            var createdDirectoryFilePath = directory + Path.DirectorySeparatorChar + kSTBLizePlusDirectoryFilename;
            using (var output = File.Create(createdDirectoryFilePath))
            {
            }
            switch ((int)Environment.OSVersion.Platform)
            {
                case 4:
                case 128:
                    break;
                default:
                    File.SetAttributes(createdDirectoryFilePath, File.GetAttributes(createdDirectoryFilePath) | FileAttributes.Hidden);
                    break;
            }
        }

        public static string GetOutputDirectory(string inputPath, string baseOutputDirectory, params string[] outputFileTypes)
        {
            var pathWithoutExtension = Path.GetFullPath(inputPath.Contains(".") ? inputPath.Substring(0, inputPath.LastIndexOf(".")) : inputPath);
            return (baseOutputDirectory ?? (File.Exists(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + kSTBLizePlusDirectoryFilename) ? Path.GetDirectoryName(Path.GetDirectoryName(pathWithoutExtension)) : Path.GetDirectoryName(pathWithoutExtension)) + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + "_" + string.Join("+", outputFileTypes) + "_" + CurrentTime);
        }

        public static string GetStblOutputPath(string inputPath, string baseOutputDirectory)
        {
            var pathWithoutExtension = Path.GetFullPath(inputPath.Contains(".") ? inputPath.Substring(0, inputPath.LastIndexOf(".")) : inputPath);
            var outputDirectory = GetOutputDirectory(inputPath, baseOutputDirectory, "STBL");
            if (!Directory.Exists(outputDirectory))
            {
                CreateSTBLizePlusDirectoryFile(outputDirectory);
            }
            return outputDirectory + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + ".stbl";
        }
    }
}
