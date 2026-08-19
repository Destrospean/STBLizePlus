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
            public enum Names
            {
                OutputDirectory,
                OutputFilename,
                UnhashedFilename,
                NoUnhashed,
                UnhashedOnly,
                XmlOnly,
                YamlOnly
            }

            static readonly System.Collections.Generic.IDictionary<Names, string[]> sDictionary = new System.Collections.Generic.Dictionary<Names, string[]>
            {
                {
                    Names.OutputDirectory,
                    new[]
                    {
                        "Specify the output directory name",
                        "-d",
                        "--dir",
                        "--directory"
                    }
                },
                {
                    Names.OutputFilename,
                    new[]
                    {
                        "Specify the filename(s) for the output STBL or XML and/or YAML",
                        "-o",
                        "--out",
                        "--output-filename"
                    }
                },
                {
                    Names.UnhashedFilename,
                    new[]
                    {
                        "Specify the filename of the unhashed keys STBL",
                        "-u",
                        "--unhashed",
                        "--unhashed-filename"
                    }
                },
                {
                    Names.NoUnhashed,
                    new[]
                    {
                        "Do not create the unhashed keys STBL",
                        "-nu",
                        "--no-unhashed"
                    }
                },
                {
                    Names.UnhashedOnly,
                    new[]
                    {
                        "Only create an unhashed keys STBL",
                        "-uo",
                        "--unhashed-only"
                    }
                },
                {
                    Names.XmlOnly,
                    new[]
                    {
                        "Only create an XML",
                        "-xo",
                        "--xml",
                        "--xml-only"
                    }
                },
                {
                    Names.YamlOnly,
                    new[]
                    {
                        "Only create a YAML",
                        "-yo",
                        "--yaml",
                        "--yaml-only"
                    }
                }
            };

            public bool NoUnhashed = false,
            UnhashedOnly = false,
            XmlOnly = false,
            YamlOnly = false;

            public string OutputDirectory,
            OutputFilename,
            UnhashedFilename = "Unhashed.stbl";

            public void Check(Names name, string arg, ref bool skip)
            {
                for (var i = 1; !skip && i < sDictionary[name].Length; i++)
                {
                    if (arg == sDictionary[name][i])
                    {
                        GetType().GetField(name.ToString()).SetValue(this, true);
                        skip = true;
                        break;
                    }
                }
            }

            public void CheckForValue(Names name, string arg, string value, ref bool skip)
            {
                for (var i = 1; !skip && i < sDictionary[name].Length; i++)
                {
                    if (arg == sDictionary[name][i])
                    {
                        GetType().GetField(name.ToString()).SetValue(this, value);
                        skip = true;
                        break;
                    }
                }
            }

            public static void Print()
            {
                Console.WriteLine("Usage: " + AppDomain.CurrentDomain.FriendlyName + " <Input Filename> [Options]" + Environment.NewLine);
                var maxLength = 0;
                foreach (Names option in Enum.GetValues(typeof(Names)))
                {
                    var text = string.Join(", ", sDictionary[option]);
                    text = text.Substring(text.IndexOf(",") + 2);
                    if (text.Length > maxLength)
                    {
                        maxLength = text.Length;
                    }
                }
                foreach (Names option in Enum.GetValues(typeof(Names)))
                {
                    string text = string.Join(", ", sDictionary[option]),
                    whitespace = null;
                    text = text.Substring(text.IndexOf(",") + 2);
                    for (var i = 0; i < maxLength - text.Length; i++)
                    {
                        whitespace += " ";
                    }
                    Console.WriteLine("    " + text + whitespace + "    " + sDictionary[option][0]);
                }
                Console.WriteLine();
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Options.Print();
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
                        options.CheckForValue(Options.Names.OutputDirectory, args[i - 1], args[i], ref skip);
                        options.CheckForValue(Options.Names.OutputFilename, args[i - 1], args[i], ref skip);
                        options.CheckForValue(Options.Names.UnhashedFilename, args[i - 1], args[i], ref skip);
                    }
                    options.Check(Options.Names.NoUnhashed, args[i], ref skip);
                    options.Check(Options.Names.UnhashedOnly, args[i], ref skip);
                    options.Check(Options.Names.XmlOnly, args[i], ref skip);
                    options.Check(Options.Names.YamlOnly, args[i], ref skip);
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
    }
}
