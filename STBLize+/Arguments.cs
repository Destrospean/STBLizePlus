using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Destrospean
{
    public abstract class Arguments
    {
        protected enum Names
        {
        }

        protected abstract IDictionary<object, string[]> Dictionary
        {
            get;
        }

        void Check(object name, string arg, ref bool skip)
        {
            for (var i = 1; !skip && i < Dictionary[name].Length; i++)
            {
                if (arg == Dictionary[name][i])
                {
                    GetType().GetField(name.ToString()).SetValue(this, true);
                    skip = true;
                    break;
                }
            }
        }

        void CheckForValue(object name, string arg, string value, ref bool skip)
        {
            for (var i = 1; !skip && i < Dictionary[name].Length; i++)
            {
                if (arg == Dictionary[name][i])
                {
                    GetType().GetField(name.ToString()).SetValue(this, value);
                    skip = true;
                    break;
                }
            }
        }

        public void CheckAll(string[] args, out string[] positionalArgs)
        {
            var positionalList = new List<string>();
            for (var i = 0; i < args.Length; i++)
            {
                var skip = false;
                if (i > 0 && args[i - 1].StartsWith("-"))
                {
                    foreach (object name in Enum.GetValues(GetType().GetNestedType("Names", BindingFlags.NonPublic)))
                    {
                        if (GetType().GetField(name.ToString()).FieldType != typeof(bool))
                        {
                            CheckForValue(name, args[i - 1], args[i], ref skip);
                        }
                    }
                }
                foreach (object name in Enum.GetValues(GetType().GetNestedType("Names", BindingFlags.NonPublic)))
                {
                    if (GetType().GetField(name.ToString()).FieldType == typeof(bool))
                    {
                        Check(name, args[i], ref skip);
                    }
                }
                if (!skip && !args[i].StartsWith("-"))
                {
                    positionalList.Add(args[i]);
                }
            }
            positionalArgs = positionalList.ToArray();
        }

        public void PrintAll()
        {
            string entryCommand = null;
            switch ((int)Environment.OSVersion.Platform)
            {
                case 4:
                case 6:
                case 128:
                    // The following code executes if the OS is Unix-like.
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
                    break;
            }
            Console.WriteLine("Usage: " + (entryCommand ?? System.IO.Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)) + " <Input Filename> [Options]" + Environment.NewLine);
            var argLists = new Dictionary<object, string>();
            var maxLength = 0;
            foreach (var name in Enum.GetValues(GetType().GetNestedType("Names", BindingFlags.NonPublic)))
            {
                argLists[name] = string.Join(", ", new List<string>(Dictionary[name]).GetRange(1, Dictionary[name].Length - 1));
                if (argLists[name].Length > maxLength)
                {
                    maxLength = argLists[name].Length;
                }
            }
            var lineGroups = new List<List<string>>();
            foreach (var name in Enum.GetValues(GetType().GetNestedType("Names", BindingFlags.NonPublic)))
            {
                var gap = "";
                for (var i = 0; i < maxLength - argLists[name].Length; i++)
                {
                    gap += " ";
                }
                lineGroups.Add(new List<string>
                    {
                        "    " + argLists[name] + gap + "    " + Dictionary[name][0]
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

        public Arguments()
        {
        }

        public Arguments(string[] args, out string[] positionalArgs)
        {
            CheckAll(args, out positionalArgs);
        }
    }
}
