using System;

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
}
