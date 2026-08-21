using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Destrospean.STBLizePlus
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

        static readonly IDictionary<Names, string[]> sDictionary = new Dictionary<Names, string[]>
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

        public static void PrintAll()
        {
            string entryCommand = null;
            if (FileSystemUtils.OperatingSystem == FileSystemUtils.OS.Unix)
            {
                var processId = Process.GetCurrentProcess().Id.ToString();
                using (var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "pgrep",
                        Arguments = "-a -p " + processId,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }))
                {
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        entryCommand = output.Substring(output.IndexOf(processId) + processId.Length + 1);
                        entryCommand = entryCommand.EndsWith("\n") ? entryCommand.Remove(entryCommand.Length - 1) : entryCommand;
                    }
                }
            }
            Console.WriteLine("Usage: " + (entryCommand ?? System.IO.Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)) + " <Input Filename> [Options]" + Environment.NewLine);
            var argLists = new Dictionary<Names, string>();
            var maxLength = 0;
            foreach (Names name in Enum.GetValues(typeof(Names)))
            {
                argLists[name] = string.Join(", ", new List<string>(sDictionary[name]).GetRange(1, sDictionary[name].Length - 1));
                if (argLists[name].Length > maxLength)
                {
                    maxLength = argLists[name].Length;
                }
            }
            var lineGroups = new List<List<string>>();
            foreach (Names name in Enum.GetValues(typeof(Names)))
            {
                var gap = "";
                for (var i = 0; i < maxLength - argLists[name].Length; i++)
                {
                    gap += " ";
                }
                lineGroups.Add(new List<string>
                    {
                        "    " + argLists[name] + gap + "    " + sDictionary[name][0]
                    });
            }
            var indentation = "";
            for (var i = 0; i < maxLength + 7; i++)
            {
                indentation += " ";
            }
            foreach (var lines in lineGroups)
            {
                var lastIndex = 0;
                while (Console.WindowWidth > maxLength + 7 && lines[lastIndex].Length > Console.WindowWidth)
                {
                    var offset = Console.WindowWidth - lines[lastIndex].Substring(0, Console.WindowWidth + 1).LastIndexOf(" ");
                    lines.Add(indentation + lines[lastIndex].Substring(Console.WindowWidth - offset));
                    lines[lastIndex] = lines[lastIndex++].Remove(Console.WindowWidth - offset);
                }
                foreach (var line in lines)
                {
                    Console.WriteLine(line);
                }
            }
            Console.WriteLine();
        }
    }
}
