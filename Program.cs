using System;
using System.Collections;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Destrospean.STBLizePlus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Options.Print();
            }
            var options = new Options();
            var positionalArgs = new System.Collections.Generic.List<string>();
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
                            var outputFileTypes = new System.Collections.Generic.List<string>
                                {
                                    "XML",
                                    "YAML"
                                };
                            if (options.XmlOnly)
                            {
                                outputFileTypes.RemoveAll(x => x != "XML");
                            }
                            else if (options.YamlOnly)
                            {
                                outputFileTypes.RemoveAll(x => x != "YAML");
                            }
                            Utils.WritePlaintext(path, options.UnhashedFilename, options.OutputDirectory, options.OutputFilename, (directory, filename, newEntries) => 
                                {
                                    if (!options.XmlOnly && !options.YamlOnly)
                                    {
                                        filename = Path.GetFileNameWithoutExtension(filename);
                                    }
                                    if (outputFileTypes.Contains("XML"))
                                    {
                                        using (var output = new FileStream(directory + Path.DirectorySeparatorChar + filename + ((options.OutputFilename == null || outputFileTypes.Count > 1) && !filename.ToLowerInvariant().EndsWith(".xml") ? ".xml" : ""), FileMode.Create, FileAccess.Write))
                                        {
                                            using (var writer = new StreamWriter(output, System.Text.Encoding.UTF8))
                                            {
                                                WriteXml(newEntries, writer);
                                            }
                                        }
                                    }
                                    if (outputFileTypes.Contains("YAML"))
                                    {
                                        using (var output = new FileStream(directory + Path.DirectorySeparatorChar + filename + ((options.OutputFilename == null || outputFileTypes.Count > 1) && !filename.ToLowerInvariant().EndsWith(".yaml") ? ".yaml" : ""), FileMode.Create, FileAccess.Write))
                                        {
                                            using (var writer = new StreamWriter(output, System.Text.Encoding.UTF8))
                                            {
                                                WriteYaml(newEntries.Unflatten(Utils.ArbitrarySeparator), writer);
                                            }
                                        }
                                    }
                                }, outputFileTypes.ToArray());
                            return;
                        }
                    }
                }
                var entries = ReadInputFile(path);
                var outputPath = Utils.GetStblOutputPath(path, options.OutputDirectory);
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
