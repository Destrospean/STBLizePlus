using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Destrospean.STBLizePlus
{
    public static class Utils
    {
        public static readonly string ArbitraryMaleSuffix = "{DESTROSPEAN_STBL_MALE_SUFFIX_" + Guid.NewGuid() + "}",
        ArbitrarySeparator = "{DESTROSPEAN_STBL_SEPARATOR_" + Guid.NewGuid() + "}",
        STBLizePlusDirectoryFilename = ".IS_CREATED_STBLIZE+_DIR";

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
            var createdDirectoryFilePath = directory + Path.DirectorySeparatorChar + STBLizePlusDirectoryFilename;
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

        public static ulong GetFnv64(string value)
        {
            var hash = 0xCBF29CE484222325;
            foreach (var b in Encoding.UTF8.GetBytes(value.ToLowerInvariant()))
            {
                hash *= 0x100000001B3;
                hash &= 0xFFFFFFFFFFFFFFFF;
                hash ^= b;
            }
            return hash;
        }

        public static string GetOutputPath(string inputPath, string outputDirectory)
        {
            var pathWithoutExtension = Path.GetFullPath(inputPath.Contains(".") ? inputPath.Substring(0, inputPath.LastIndexOf(".")) : inputPath);
            var outputPath = outputDirectory ?? (File.Exists(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + STBLizePlusDirectoryFilename) ? Path.GetDirectoryName(Path.GetDirectoryName(pathWithoutExtension)) : Path.GetDirectoryName(pathWithoutExtension)) + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + "_STBL_" + CurrentTime;
            if (!Directory.Exists(outputPath))
            {
                CreateSTBLizePlusDirectoryFile(outputPath);
            }
            return outputPath + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + ".stbl";
        }

        public static IDictionary ReadStbl(string path)
        {
            try
            {
                var entries = new System.Collections.Specialized.OrderedDictionary();
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(input, Encoding.Unicode))
                    {
                        reader.ReadBytes(7);
                        var count = reader.ReadInt32();
                        reader.ReadBytes(6);
                        for (var i = 0; i < count; i++)
                        {
                            var id = reader.ReadUInt64();
                            entries[id] = new string(reader.ReadChars(reader.ReadInt32()));
                        }
                    }
                }
                return entries;
            }
            catch
            {
                throw new ArgumentException("File must be a valid STBL");
            }
        }

        public static void WriteEntryPair(BinaryWriter writer, string key, string value)
        {
            writer.Write(GetFnv64(key));
            writer.Write(value.Length);
            writer.Write(value.ToCharArray());
        }

        public static void WriteErrorLog(string path, Exception ex)
        {
            using (var output = new FileStream(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), string.IsNullOrEmpty(path) ? "stbl.log" : Path.GetFileNameWithoutExtension(path) + ".log"), FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(output, Encoding.UTF8))
                {
                    writer.WriteLine(ex.GetType().Name + " - " + ex.Message);
                    writer.WriteLine(ex.StackTrace);
                }
            }
        }

        public static void WritePlaintext(string inputPath, string unhashedFilename, string outputDirectory, string outputFilename, Action<string, string, IDictionary> writeFileCallback)
        {
            string[] suffixes = new[]
                {
                    "_Female",
                    "_FemaleFemale",
                    "_MaleFemale"
                };
            var pathWithoutExtension = Path.GetFullPath(inputPath.Contains(".") ? inputPath.Substring(0, inputPath.LastIndexOf(".")) : inputPath);
            var stblEntries = ReadStbl(inputPath);
            IDictionary entries = new System.Collections.Specialized.OrderedDictionary(),
            newEntries = new System.Collections.Specialized.OrderedDictionary();
            var keysToReplace = new System.Collections.Generic.List<string>();
            foreach (DictionaryEntry entry in ReadStbl(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + unhashedFilename))
            {
                var value = entry.Value.ToString().Replace("/", ArbitrarySeparator + "/").Replace(":", ArbitrarySeparator + ":");
                foreach (var suffix in suffixes)
                {
                    if (value.ToLowerInvariant().EndsWith(suffix.ToLowerInvariant()))
                    {
                        keysToReplace.Add(value.Substring(0, value.ToLowerInvariant().LastIndexOf(suffix.ToLowerInvariant())));
                    }
                }
                entries[value] = stblEntries[entry.Key];
            }
            foreach (DictionaryEntry entry in entries)
            {
                if (keysToReplace.Contains(entry.Key.ToString()))
                {
                    newEntries[entry.Key + ArbitrarySeparator + ArbitraryMaleSuffix] = entry.Value;
                    continue;
                }
                var hasNoSuffix = true;
                foreach (var suffix in suffixes)
                {
                    if (entry.Key.ToString().ToLowerInvariant().EndsWith(suffix.ToLowerInvariant()))
                    {
                        newEntries[entry.Key.ToString().Substring(0, entry.Key.ToString().ToLowerInvariant().LastIndexOf(suffix.ToLowerInvariant())) + ArbitrarySeparator + suffix] = entry.Value;
                        hasNoSuffix = false;
                        break;
                    }
                }
                if (hasNoSuffix)
                {
                    newEntries[entry.Key] = entry.Value;
                }
            }
            var outputPath = outputDirectory ?? (File.Exists(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + STBLizePlusDirectoryFilename) ? Path.GetDirectoryName(Path.GetDirectoryName(pathWithoutExtension)) : Path.GetDirectoryName(pathWithoutExtension)) + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + "_XML+YAML_" + CurrentTime;
            if (!Directory.Exists(outputPath))
            {
                CreateSTBLizePlusDirectoryFile(outputPath);
            }
            writeFileCallback(outputPath, outputFilename ?? Path.GetFileName(pathWithoutExtension), newEntries);
            Console.WriteLine(Path.GetFullPath(outputPath));
        }

        public static void WriteStbl(string path, IDictionary entries, bool keysAsValues = false)
        {
            using (var output = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(output, Encoding.Unicode))
                {
                    writer.Write(new byte[]
                        {
                            83,
                            84,
                            66,
                            76
                        });
                    writer.Write(new byte[]
                        {
                            2
                        });
                    writer.Write(new byte[2]);
                    writer.Write(entries.Count);
                    writer.Write(new byte[6]);
                    foreach (DictionaryEntry entry in entries)
                    {
                        WriteEntryPair(writer, entry.Key.ToString(), keysAsValues ? entry.Key.ToString() : entry.Value.ToString());
                    }
                }
            }
        }
    }
}
