using System;
using System.Collections;
using System.IO;

namespace Destrospean.STBLizePlus
{
    public class Program
    {
        public class Arguments : Destrospean.Arguments
        {
            protected new enum Names
            {
                OutputDirectory,
                OutputFilename,
                UnhashedFilename,
                NoUnhashed,
                UnhashedOnly,
                XmlOnly,
                YamlOnly
            }

            protected override System.Collections.Generic.IDictionary<object, string[]> Dictionary
            {
                get
                {
                    return new System.Collections.Generic.Dictionary<object, string[]>
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
                                "Specify the filename for the output STBL or XML and/or YAML (for multiple plaintext files, specify it without the extension)",
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
                }
            }

            public bool NoUnhashed = false,
            UnhashedOnly = false,
            XmlOnly = false,
            YamlOnly = false;

            public string OutputDirectory,
            OutputFilename,
            UnhashedFilename = "Unhashed.stbl";

            public Arguments() : base()
            {
            }

            public Arguments(string[] args, out string[] positionalArgs) : base(args, out positionalArgs)
            {
            }
        }

        public static void Main(string[] args)
        {
            string[] positionalArgs;
            var arguments = new Arguments(args, out positionalArgs);
            if (args.Length == 0)
            {
                arguments.PrintAll();
            }
            if (positionalArgs.Length == 0)
            {
                Console.Error.WriteLine("No input filename specified");
                return;
            }
            var path = positionalArgs[0];
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    using (var reader = new BinaryReader(stream, System.Text.Encoding.Unicode))
                    {
                        if (reader.ReadInt32() == STBLUtils.FourCC)
                        {
                            var outputFileTypes = new System.Collections.Generic.List<string>
                                {
                                    "XML",
                                    "YAML"
                                };
                            if (arguments.XmlOnly)
                            {
                                outputFileTypes.RemoveAll(x => x != "XML");
                            }
                            else if (arguments.YamlOnly)
                            {
                                outputFileTypes.RemoveAll(x => x != "YAML");
                            }
                            var outputDirectory = FileSystemUtils.GetOutputDirectory(path, arguments.OutputDirectory, outputFileTypes.ToArray());
                            WriteFiles(path, arguments.UnhashedFilename, outputDirectory, arguments.OutputFilename, (outputPath, entries) => 
                                {
                                    WriteFile(outputPath, "XML", entries, PlainTextUtils.WriteXml, arguments.OutputFilename == null, outputFileTypes.ToArray());
                                    WriteFile(outputPath, "YAML", entries, PlainTextUtils.WriteYaml, arguments.OutputFilename == null, true, outputFileTypes.ToArray());
                                },  outputFileTypes.ToArray());
                            Console.WriteLine(Path.GetFullPath(outputDirectory));
                            return;
                        }
                    }
                }
                IDictionary dictionary;
                if (!PlainTextUtils.TryReadXml(path, out dictionary) && !PlainTextUtils.TryReadYaml(path, out dictionary))
                {
                    throw new ArgumentException("File must be a valid JSON, XML, or YAML", path);
                }
                var outputFolder = FileSystemUtils.GetOutputDirectory(path, arguments.OutputDirectory, "STBL");
                if (!Directory.Exists(outputFolder))
                {
                    FileSystemUtils.CreateSTBLizePlusDirectoryFile(outputFolder);
                }
                arguments.OutputFilename = arguments.OutputFilename ?? Path.GetFileNameWithoutExtension(path) + ".stbl";
                if (!arguments.UnhashedOnly)
                {
                    STBLUtils.WriteStbl(outputFolder + Path.DirectorySeparatorChar + arguments.OutputFilename, dictionary);
                }
                if (!arguments.NoUnhashed)
                {
                    STBLUtils.WriteStbl(outputFolder + Path.DirectorySeparatorChar + arguments.UnhashedFilename, dictionary, true);
                }
                Console.WriteLine(Path.GetFullPath(outputFolder));
            }
            catch (Exception ex)
            {
                WriteErrorLog(path, ex);
                Console.Error.WriteLine(ex.Message);
            }
        }

        public static void WriteErrorLog(string path, Exception ex)
        {
            using (var output = File.Create(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), string.IsNullOrEmpty(path) ? Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName) + ".log" : Path.GetFileNameWithoutExtension(path) + ".log")))
            {
                using (var writer = new StreamWriter(output))
                {
                    writer.WriteLine(ex.GetType().Name + " - " + ex.Message);
                    writer.WriteLine(ex.StackTrace);
                }
            }
        }

        public static void WriteFile(string path, string fileType, IDictionary entries, Action<IDictionary, StreamWriter> writeFileCallback, bool outputFileNameUndefined = true, params string[] fileTypes)
        {
            WriteFile(path, fileType, entries, writeFileCallback, outputFileNameUndefined, false, fileTypes);
        }

        public static void WriteFile(string path, string fileType, IDictionary entries, Action<IDictionary, StreamWriter> writeFileCallback, bool outputFileNameUndefined = true, bool unflatten = false, params string[] fileTypes)
        {
            string directory = Path.GetDirectoryName(path),
            filename = fileTypes.Length > 1 ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path);
            if (!Array.Exists(fileTypes, x => x == fileType))
            {
                return;
            }
            using (var stream = File.Create(directory + Path.DirectorySeparatorChar + filename + ((outputFileNameUndefined || fileTypes.Length > 1) && !filename.ToLowerInvariant().EndsWith("." + fileType.ToLowerInvariant()) ? "." + fileType.ToLowerInvariant() : "")))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writeFileCallback(unflatten ? entries.Unflatten() : entries, writer);
                }
            }
        }

        public static void WriteFiles(string inputPath, string unhashedFilename, string outputDirectory, string outputFilename, Action<string, IDictionary> writeFilesCallback, params string[] outputFileTypes)
        {
            var pathWithoutExtension = Path.GetFullPath(inputPath.Contains(".") ? inputPath.Substring(0, inputPath.LastIndexOf(".")) : inputPath);
            if (!Directory.Exists(outputDirectory))
            {
                FileSystemUtils.CreateSTBLizePlusDirectoryFile(outputDirectory);
            }
            writeFilesCallback(outputDirectory + Path.DirectorySeparatorChar + (outputFilename ?? Path.GetFileName(pathWithoutExtension)), STBLUtils.UnhashKeys(inputPath, Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + unhashedFilename));
        }
    }
}
