using System;
using System.IO;

namespace Destrospean.STBLizePlus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Options.PrintAll();
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
                            var outputDirectory = FileSystemUtils.GetOutputDirectory(path, options.OutputDirectory, outputFileTypes.ToArray());
                            PlainTextUtils.Write(path, options.UnhashedFilename, outputDirectory, options.OutputFilename, (directory, filename, newEntries) => 
                                {
                                    PlainTextUtils.Write(newEntries, directory, filename, "XML", outputFileTypes.ToArray(), PlainTextUtils.WriteXml, options.OutputFilename == null);
                                    PlainTextUtils.Write(newEntries, directory, filename, "YAML", outputFileTypes.ToArray(), PlainTextUtils.WriteYaml, options.OutputFilename == null);
                                }, outputFileTypes.ToArray());
                            Console.WriteLine(Path.GetFullPath(outputDirectory));
                            return;
                        }
                    }
                }
                System.Collections.IDictionary entries;
                if (!PlainTextUtils.TryReadXml(path, out entries) && !PlainTextUtils.TryReadYaml(path, out entries))
                {
                    throw new ArgumentException("File must be a valid JSON, XML, or YAML", path);
                }
                var outputPath = FileSystemUtils.GetStblOutputPath(path, options.OutputDirectory);
                if (options.OutputFilename == null)
                {
                    options.OutputFilename = Path.GetFileName(outputPath);
                }
                if (!options.UnhashedOnly)
                {
                    STBLUtils.WriteStbl(Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar + options.OutputFilename, entries);
                }
                if (!options.NoUnhashed)
                {
                    STBLUtils.WriteStbl(Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar + options.UnhashedFilename, entries, true);
                }
                Console.WriteLine(Path.GetFullPath(Path.GetDirectoryName(outputPath)));
            }
            catch (Exception ex)
            {
                WriteErrorLog(path, ex);
                throw;
            }
        }

        public static void WriteErrorLog(string path, Exception ex)
        {
            using (var output = new FileStream(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), string.IsNullOrEmpty(path) ? "stbl.log" : Path.GetFileNameWithoutExtension(path) + ".log"), FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(output, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(ex.GetType().Name + " - " + ex.Message);
                    writer.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}
