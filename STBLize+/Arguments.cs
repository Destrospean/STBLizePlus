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

        public void CheckAll(string[] args, out string[] positionalArgs)
        {
            var positionalList = new List<string>();
            for (var i = 0; i < args.Length; i++)
            {
                if (i > 0 && args[i - 1].StartsWith("-"))
                {
                    foreach (object name in Enum.GetValues(GetType().GetNestedType("Names", BindingFlags.NonPublic)))
                    {
                        for (var j = 1; GetType().GetField(name.ToString()).FieldType != typeof(bool) && j < Dictionary[name].Length; j++)
                        {
                            if (args[i - 1] == Dictionary[name][j])
                            {
                                GetType().GetField(name.ToString()).SetValue(this, args[i]);
                                goto skip;
                            }
                        }
                    }
                }
                foreach (object name in Enum.GetValues(GetType().GetNestedType("Names", BindingFlags.NonPublic)))
                {
                    for (var j = 1; GetType().GetField(name.ToString()).FieldType == typeof(bool) && j < Dictionary[name].Length; j++)
                    {
                        if (args[i] == Dictionary[name][j])
                        {
                            GetType().GetField(name.ToString()).SetValue(this, true);
                            goto skip;
                        }
                    }
                }
                if (!args[i].StartsWith("-"))
                {
                    positionalList.Add(args[i]);
                }
                skip:
                continue;
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
                    var offset = Console.WindowWidth - lines[lastIndex].Substring(0, Console.WindowWidth).LastIndexOf(" ");
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
