using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Destrospean.STBLizePlus
{
    public class Program
    {
        public static readonly string ArbitraryMaleSuffix = "{DESTROSPEAN_STBL_MALE_SUFFIX_" + Guid.NewGuid() + "}",
        ArbitrarySeparator = "{DESTROSPEAN_STBL_SEPARATOR_" + Guid.NewGuid() + "}",
        OutputDirectoryFilename = ".IS_CREATED_STBLIZE+_DIR";

        public static ulong CurrentTime
        {
            get
            {
                return ulong.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
        }

        public static readonly IDictionary<OptionNames, string[]> OptionsDictionary = new Dictionary<OptionNames, string[]>
        {
            {
                OptionNames.OutputDirectory,
                new[]
                {
                    "Specify the output directory name",
                    "-d",
                    "--dir",
                    "--directory"
                }
            },
            {
                OptionNames.OutputFilename,
                new[]
                {
                    "Specify the filename(s) for the output STBL or XML and/or YAML",
                    "-o",
                    "--out",
                    "--output-filename"
                }
            },
            {
                OptionNames.UnhashedFilename,
                new[]
                {
                    "Specify the filename of the unhashed keys STBL",
                    "-u",
                    "--unhashed",
                    "--unhashed-filename"
                }
            },
            {
                OptionNames.NoUnhashed,
                new[]
                {
                    "Do not create the unhashed keys STBL",
                    "-nu",
                    "--no-unhashed"
                }
            },
            {
                OptionNames.UnhashedOnly,
                new[]
                {
                    "Only create an unhashed keys STBL",
                    "-uo",
                    "--unhashed-only"
                }
            },
            {
                OptionNames.XmlOnly,
                new[]
                {
                    "Only create an XML",
                    "-xo",
                    "--xml",
                    "--xml-only"
                }
            },
            {
                OptionNames.YamlOnly,
                new[]
                {
                    "Only create a YAML",
                    "-yo",
                    "--yaml",
                    "--yaml-only"
                }
            }
        };

        public class Options
        {
            public bool NoUnhashed = false,
            UnhashedOnly = false,
            XmlOnly = false,
            YamlOnly = false;

            public string OutputDirectory,
            OutputFilename,
            UnhashedFilename = "Unhashed.stbl";
        }

        public enum OptionNames
        {
            OutputDirectory,
            OutputFilename,
            UnhashedFilename,
            NoUnhashed,
            UnhashedOnly,
            XmlOnly,
            YamlOnly
        }

        public static ulong GetFNV64(string value)
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

        public static string GetOutputPath(string[] paths, Options options)
        {
            var pathWithoutExtension = Path.GetFullPath(paths[0].Contains(".") ? paths[0].Substring(0, paths[0].LastIndexOf(".")) : paths[0]);
            var outputPath = options.OutputDirectory ?? (File.Exists(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + OutputDirectoryFilename) ? Path.GetDirectoryName(Path.GetDirectoryName(pathWithoutExtension)) : Path.GetDirectoryName(pathWithoutExtension)) + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + "_STBL_" + CurrentTime;
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                var createdDirectoryFilePath = outputPath + Path.DirectorySeparatorChar + OutputDirectoryFilename;
                using (var output = File.Create(createdDirectoryFilePath))
                {
                }
                switch ((int)Environment.OSVersion.Platform)
                {
                    case 4:
                    case 128:
                        break;
                    default:
                        using (var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                    {
                                        Arguments = "+h \"" + createdDirectoryFilePath + "\"",
                                        CreateNoWindow = true,
                                        FileName = "attrib",
                                        RedirectStandardError = true,
                                        RedirectStandardOutput = true,
                                        UseShellExecute = false
                                    }
                            })
                        {
                            process.Start();
                            process.WaitForExit();
                        }
                        break;
                }
            }
            return outputPath + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + ".stbl";
        }

        public static IDictionary ReadInputFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path must not be null or empty", path);
            }
            try
            {
                // Everything here executes if the file is a valid XML
                var entries = new OrderedDictionary();
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var reader = XmlReader.Create(input);
                    reader.ReadStartElement("TEXT");
                    while (reader.ReadToNextSibling("KEY"))
                    {
                        var key = reader.ReadElementContentAsString();
                        if (reader.ReadToNextSibling("STR"))
                        {
                            entries[key] = reader.ReadElementContentAsString();
                        }
                    }
                    return entries;
                }
            }
            catch (XmlException)
            {
            }
            try
            {
                // Everything here executes if the file is a valid YAML
                return new DeserializerBuilder().Build().Deserialize<IDictionary>(File.ReadAllText(path)).Flatten("");
            }
            catch (YamlException)
            {
            }
            throw new ArgumentException("File must be a valid JSON, XML, or YAML", path);
        }

        public static IDictionary ReadStbl(string path)
        {
            try
            {
                var entries = new OrderedDictionary();
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
            writer.Write(GetFNV64(key));
            writer.Write(value.Length);
            writer.Write(value.ToCharArray());
        }

        public static void WriteErrorLog(string[] paths, Exception ex)
        {
            using (var output = new FileStream(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(paths[0])), string.IsNullOrEmpty(paths[0]) ? "stbl.log" : Path.GetFileNameWithoutExtension(paths[0]) + ".log"), FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(output, Encoding.UTF8))
                {
                    writer.WriteLine(ex.GetType().Name + " - " + ex.Message);
                    writer.WriteLine(ex.StackTrace);
                }
            }
        }

        public static void WritePlaintext(string[] paths, Options options)
        {
            string[] suffixes = new[]
                {
                    "_Female",
                    "_FemaleFemale",
                    "_MaleFemale"
                };
            var pathWithoutExtension = Path.GetFullPath(paths[0].Contains(".") ? paths[0].Substring(0, paths[0].LastIndexOf(".")) : paths[0]);
            var stblEntries = ReadStbl(paths[0]);
            IDictionary entries = new OrderedDictionary(),
            newEntries = new OrderedDictionary();
            var keysToReplace = new List<string>();
            foreach (DictionaryEntry entry in ReadStbl(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + options.UnhashedFilename))
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
            var outputPath = options.OutputDirectory ?? (File.Exists(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + OutputDirectoryFilename) ? Path.GetDirectoryName(Path.GetDirectoryName(pathWithoutExtension)) : Path.GetDirectoryName(pathWithoutExtension)) + Path.DirectorySeparatorChar + Path.GetFileName(pathWithoutExtension) + "_XML+YAML_" + CurrentTime;
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                var createdDirectoryFilePath = outputPath + Path.DirectorySeparatorChar + OutputDirectoryFilename;
                using (var output = File.Create(createdDirectoryFilePath))
                {
                }
                switch ((int)Environment.OSVersion.Platform)
                {
                    case 4:
                    case 128:
                        break;
                    default:
                        using (var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                    {
                                        Arguments = "+h \"" + createdDirectoryFilePath + "\"",
                                        CreateNoWindow = true,
                                        FileName = "attrib",
                                        RedirectStandardError = true,
                                        RedirectStandardOutput = true,
                                        UseShellExecute = false
                                    }
                            })
                        {
                            process.Start();
                            process.WaitForExit();
                        }
                        break;
                }
            }
            var filename = options.OutputFilename ?? Path.GetFileName(pathWithoutExtension);
            if (!options.XmlOnly && !options.YamlOnly)
            {
                filename = Path.GetFileNameWithoutExtension(filename);
            }
            // Write the XML file
            if (!options.YamlOnly)
            {
                using (var output = new FileStream(outputPath + Path.DirectorySeparatorChar + filename + (options.XmlOnly ? "" : ".xml"), FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(output, Encoding.UTF8))
                    {
                        WriteXml(newEntries, writer);
                    }
                }
            }
            // Write the YAML file
            if (!options.XmlOnly)
            {
                using (var output = new FileStream(outputPath + Path.DirectorySeparatorChar + filename + (options.YamlOnly ? "" : ".yaml"), FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(output, Encoding.UTF8))
                    {
                        WriteYaml(newEntries.Unflatten(ArbitrarySeparator), writer);
                    }
                }
            }
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

        public static void WriteXml(IDictionary entries, StreamWriter writer)
        {
            writer.WriteLine("<?xml version=\"1.0\" ?>" + Environment.NewLine + "<TEXT>" + Environment.NewLine);
            foreach (DictionaryEntry entry in entries)
            {
                writer.WriteLine("<KEY>" + (entry.Key.ToString().EndsWith(ArbitraryMaleSuffix) ? entry.Key.ToString().Substring(0, entry.Key.ToString().LastIndexOf(ArbitraryMaleSuffix)) : entry.Key.ToString()).Replace(ArbitrarySeparator, "") + "</KEY>" + Environment.NewLine + "<STR>" + entry.Value + "</STR>" + Environment.NewLine);
            }
            writer.WriteLine("</TEXT>");
        }

        public static void WriteYaml(IDictionary entries, StreamWriter writer, int level = 0, int indent = 4)
        {
            var indentation = "";
            for (var i = 0; i < indent * level; i++)
            {
                indentation += " ";
            }
            foreach (DictionaryEntry entry in entries)
            {
                var dictionary = entry.Value as IDictionary;
                if (dictionary == null)
                {
                    writer.WriteLine(indentation + "\"" + (entry.Key.ToString().EndsWith(ArbitraryMaleSuffix) ? entry.Key.ToString().Substring(0, entry.Key.ToString().LastIndexOf(ArbitraryMaleSuffix)) : entry.Key.ToString()) + "\": \"" + entry.Value + "\"");
                    continue;
                }
                writer.WriteLine(indentation + "\"" + entry.Key + "\":");
                WriteYaml(dictionary, writer, level + 1, indent);
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: " + AppDomain.CurrentDomain.FriendlyName + " <Input Filename> [Options]" + Environment.NewLine);
                var maxLength = 0;
                foreach (OptionNames option in Enum.GetValues(typeof(OptionNames)))
                {
                    var text = string.Join(", ", OptionsDictionary[option]);
                    text = text.Substring(text.IndexOf(",") + 2);
                    if (text.Length > maxLength)
                    {
                        maxLength = text.Length;
                    }
                }
                foreach (OptionNames option in Enum.GetValues(typeof(OptionNames)))
                {
                    string text = string.Join(", ", OptionsDictionary[option]),
                    whitespace = null;
                    text = text.Substring(text.IndexOf(",") + 2);
                    for (var i = 0; i < maxLength - text.Length; i++)
                    {
                        whitespace += " ";
                    }
                    Console.WriteLine("    " + text + whitespace + "    " + OptionsDictionary[option][0]);
                }
                Console.WriteLine();
            }
            var options = new Options();
            var paths = new List<string>();
            try
            {
                if (args.Length > 0)
                {
                    for (var i = 0; i < args.Length; i++)
                    {
                        var skip = false;
                        if (i > 0 && args[i - 1].StartsWith("-"))
                        {
                            for (var j = 1; !skip && j < OptionsDictionary[OptionNames.OutputDirectory].Length; j++)
                            {
                                if (args[i - 1] == OptionsDictionary[OptionNames.OutputDirectory][j])
                                {
                                    options.OutputDirectory = args[i];
                                    skip = true;
                                    break;
                                }
                            }
                            for (var j = 1; !skip && j < OptionsDictionary[OptionNames.OutputFilename].Length; j++)
                            {
                                if (args[i - 1] == OptionsDictionary[OptionNames.OutputFilename][j])
                                {
                                    options.OutputFilename = args[i];
                                    skip = true;
                                    break;
                                }
                            }
                            for (var j = 1; !skip && j < OptionsDictionary[OptionNames.UnhashedFilename].Length; j++)
                            {
                                if (args[i - 1] == OptionsDictionary[OptionNames.UnhashedFilename][j])
                                {
                                    options.UnhashedFilename = args[i];
                                    skip = true;
                                    break;
                                }
                            }
                        }
                        for (var j = 1; !skip && j < OptionsDictionary[OptionNames.NoUnhashed].Length; j++)
                        {
                            if (args[i] == OptionsDictionary[OptionNames.NoUnhashed][j])
                            {
                                skip = options.NoUnhashed = true;
                                break;
                            }
                        }
                        for (var j = 1; !skip && j < OptionsDictionary[OptionNames.UnhashedOnly].Length; j++)
                        {
                            if (args[i] == OptionsDictionary[OptionNames.UnhashedOnly][j])
                            {
                                skip = options.UnhashedOnly = true;
                                break;
                            }
                        }
                        for (var j = 1; !skip && j < OptionsDictionary[OptionNames.XmlOnly].Length; j++)
                        {
                            if (args[i] == OptionsDictionary[OptionNames.XmlOnly][j])
                            {
                                skip = options.XmlOnly = true;
                                break;
                            }
                        }
                        for (var j = 1; !skip && j < OptionsDictionary[OptionNames.YamlOnly].Length; j++)
                        {
                            if (args[i] == OptionsDictionary[OptionNames.YamlOnly][j])
                            {
                                skip = options.YamlOnly = true;
                                break;
                            }
                        }
                        if (!skip && !args[i].StartsWith("-"))
                        {
                            paths.Add(args[i]);
                        }
                    }
                }
                if (paths.Count == 0)
                {
                    Console.Error.WriteLine("No input filename specified");
                    return;
                }
                using (var input = new FileStream(paths[0], FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(input, Encoding.Unicode))
                    {
                        if (reader.ReadUInt32() == 0x4C425453)
                        {
                            WritePlaintext(paths.ToArray(), options);
                            return;
                        }
                    }
                }
                var entries = ReadInputFile(paths[0]);
                var outputPath = GetOutputPath(paths.ToArray(), options);
                if (options.OutputFilename == null)
                {
                    options.OutputFilename = Path.GetFileName(outputPath);
                }
                if (!options.UnhashedOnly)
                {
                    WriteStbl(Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar + options.OutputFilename, entries);
                }
                if (!options.NoUnhashed)
                {
                    WriteStbl(Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar + options.UnhashedFilename, entries, true);
                }
                Console.WriteLine(Path.GetFullPath(Path.GetDirectoryName(outputPath)));
            }
            catch (Exception ex)
            {
                WriteErrorLog(paths.ToArray(), ex);
                throw;
            }
        }
    }
}
