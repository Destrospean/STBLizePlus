using System;
using System.Collections;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Destrospean.STBLizePlus
{
    public static class Program
    {
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

        public static readonly System.Collections.Generic.IDictionary<OptionNames, string[]> OptionsDictionary = new System.Collections.Generic.Dictionary<OptionNames, string[]>
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

        public static void CheckForOption(this Options options, OptionNames option, string current, ref bool skip)
        {
            for (var i = 1; !skip && i < OptionsDictionary[option].Length; i++)
            {
                if (current == OptionsDictionary[option][i])
                {
                    options.GetType().GetField(option.ToString()).SetValue(options, true);
                    skip = true;
                    break;
                }
            }
        }

        public static void CheckForValue(this Options options, OptionNames option, string current, string previous, ref bool skip)
        {
            for (var i = 1; !skip && i < OptionsDictionary[option].Length; i++)
            {
                if (previous == OptionsDictionary[option][i])
                {
                    options.GetType().GetField(option.ToString()).SetValue(options, current);
                    skip = true;
                    break;
                }
            }
        }

        public static void PrintArguments()
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

        public static IDictionary ReadInputFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path must not be null or empty", path);
            }
            try
            {
                // Everything here executes if the file is a valid XML
                var entries = new System.Collections.Specialized.OrderedDictionary();
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var reader = System.Xml.XmlReader.Create(input);
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
            catch (System.Xml.XmlException)
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

        public static void WriteXml(IDictionary entries, StreamWriter writer)
        {
            writer.WriteLine("<?xml version=\"1.0\" ?>" + Environment.NewLine + "<TEXT>" + Environment.NewLine);
            foreach (DictionaryEntry entry in entries)
            {
                writer.WriteLine("<KEY>" + (entry.Key.ToString().EndsWith(Utils.ArbitraryMaleSuffix) ? entry.Key.ToString().Substring(0, entry.Key.ToString().LastIndexOf(Utils.ArbitraryMaleSuffix)) : entry.Key.ToString()).Replace(Utils.ArbitrarySeparator, "") + "</KEY>" + Environment.NewLine + "<STR>" + entry.Value + "</STR>" + Environment.NewLine);
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
                    writer.WriteLine(indentation + "\"" + (entry.Key.ToString().EndsWith(Utils.ArbitraryMaleSuffix) ? entry.Key.ToString().Substring(0, entry.Key.ToString().LastIndexOf(Utils.ArbitraryMaleSuffix)) : entry.Key.ToString()) + "\": \"" + entry.Value + "\"");
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
                PrintArguments();
            }
            var options = new Options();
            var positionalArgs = new System.Collections.Generic.List<string>();
            if (args.Length > 0)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    var skip = false;
                    if (i > 0 && args[i - 1].StartsWith("-"))
                    {
                        options.CheckForValue(OptionNames.OutputDirectory, args[i], args[i - 1], ref skip);
                        options.CheckForValue(OptionNames.OutputFilename, args[i], args[i - 1], ref skip);
                        options.CheckForValue(OptionNames.UnhashedFilename, args[i], args[i - 1], ref skip);
                    }
                    options.CheckForOption(OptionNames.NoUnhashed, args[i], ref skip);
                    options.CheckForOption(OptionNames.UnhashedOnly, args[i], ref skip);
                    options.CheckForOption(OptionNames.XmlOnly, args[i], ref skip);
                    options.CheckForOption(OptionNames.YamlOnly, args[i], ref skip);
                    if (!skip && !args[i].StartsWith("-"))
                    {
                        positionalArgs.Add(args[i]);
                    }
                }
            }
            if (positionalArgs.Count == 0)
            {
                Console.Error.WriteLine("No input filename specified");
                return;
            }
            var path = positionalArgs[0];
            try
            {
                using (var input = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(input, System.Text.Encoding.Unicode))
                    {
                        if (reader.ReadUInt32() == 0x4C425453)
                        {
                            Utils.WritePlaintext(path, options.UnhashedFilename, options.OutputDirectory, options.OutputFilename, (directory, filename, newEntries) => 
                                {
                                    if (!options.XmlOnly && !options.YamlOnly)
                                    {
                                        filename = Path.GetFileNameWithoutExtension(filename);
                                    }
                                    if (!options.YamlOnly)
                                    {
                                        using (var output = new FileStream(directory + Path.DirectorySeparatorChar + filename + (options.XmlOnly ? "" : ".xml"), FileMode.Create, FileAccess.Write))
                                        {
                                            using (var writer = new StreamWriter(output, System.Text.Encoding.UTF8))
                                            {
                                                WriteXml(newEntries, writer);
                                            }
                                        }
                                    }
                                    if (!options.XmlOnly)
                                    {
                                        using (var output = new FileStream(directory + Path.DirectorySeparatorChar + filename + (options.YamlOnly ? "" : ".yaml"), FileMode.Create, FileAccess.Write))
                                        {
                                            using (var writer = new StreamWriter(output, System.Text.Encoding.UTF8))
                                            {
                                                WriteYaml(newEntries.Unflatten(Utils.ArbitrarySeparator), writer);
                                            }
                                        }
                                    }
                                });
                            return;
                        }
                    }
                }
                var entries = ReadInputFile(path);
                var outputPath = Utils.GetOutputPath(path, options.OutputDirectory);
                if (options.OutputFilename == null)
                {
                    options.OutputFilename = Path.GetFileName(outputPath);
                }
                if (!options.UnhashedOnly)
                {
                    Utils.WriteStbl(Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar + options.OutputFilename, entries);
                }
                if (!options.NoUnhashed)
                {
                    Utils.WriteStbl(Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar + options.UnhashedFilename, entries, true);
                }
                Console.WriteLine(Path.GetFullPath(Path.GetDirectoryName(outputPath)));
            }
            catch (Exception ex)
            {
                Utils.WriteErrorLog(path, ex);
                throw;
            }
        }
    }
}
