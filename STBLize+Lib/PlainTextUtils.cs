using System;
using System.Collections;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Destrospean.STBLizePlus
{
    public class PlainTextUtils
    {
        static readonly string sArbitraryMaleSuffix = "{DESTROSPEAN_STBL_MALE_SUFFIX_" + Guid.NewGuid() + "}",
        sArbitrarySeparator = "{DESTROSPEAN_STBL_SEPARATOR_" + Guid.NewGuid() + "}";

        public static bool TryReadXml(string path, out IDictionary entries)
        {
            entries = null;
            try
            {
                entries = new System.Collections.Specialized.OrderedDictionary();
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
                }
                return true;
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }
        }

        public static bool TryReadYaml(string path, out IDictionary entries)
        {
            entries = null;
            try
            {
                entries = new DeserializerBuilder().Build().Deserialize<IDictionary>(File.ReadAllText(path)).Flatten("");
                return true;
            }
            catch (YamlException)
            {
                return false;
            }
        }

        public static void Write(IDictionary entries, string directory, string filename, string fileType, string[] fileTypes, Action<IDictionary, StreamWriter> writeFileCallback, bool outputFileNameUndefined = true)
        {
            if (fileTypes.Length > 1)
            {
                filename = Path.GetFileNameWithoutExtension(filename);
            }
            if (!Array.Exists(fileTypes, x => x == fileType))
            {
                return;
            }
            using (var output = new FileStream(directory + Path.DirectorySeparatorChar + filename + ((outputFileNameUndefined || fileTypes.Length > 1) && !filename.ToLowerInvariant().EndsWith("." + fileType.ToLowerInvariant()) ? "." + fileType.ToLowerInvariant() : ""), FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(output, System.Text.Encoding.UTF8))
                {
                    writeFileCallback(entries, writer);
                }
            }
        }

        public static void Write(string inputPath, string unhashedFilename, string outputDirectory, string outputFilename, Action<string, string, IDictionary> writeFilesCallback, params string[] outputFileTypes)
        {
            string[] suffixes = new[]
                {
                    "_Female",
                    "_FemaleFemale",
                    "_MaleFemale"
                };
            var pathWithoutExtension = Path.GetFullPath(inputPath.Contains(".") ? inputPath.Substring(0, inputPath.LastIndexOf(".")) : inputPath);
            var stblEntries = STBLUtils.ReadStbl(inputPath);
            IDictionary entries = new System.Collections.Specialized.OrderedDictionary(),
            newEntries = new System.Collections.Specialized.OrderedDictionary();
            var keysToReplace = new System.Collections.Generic.List<string>();
            foreach (DictionaryEntry entry in STBLUtils.ReadStbl(Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + unhashedFilename))
            {
                var value = entry.Value.ToString().Replace("/", sArbitrarySeparator + "/").Replace(":", sArbitrarySeparator + ":");
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
                    newEntries[entry.Key + sArbitrarySeparator + sArbitraryMaleSuffix] = entry.Value;
                    continue;
                }
                var hasNoSuffix = true;
                foreach (var suffix in suffixes)
                {
                    if (entry.Key.ToString().ToLowerInvariant().EndsWith(suffix.ToLowerInvariant()))
                    {
                        newEntries[entry.Key.ToString().Substring(0, entry.Key.ToString().ToLowerInvariant().LastIndexOf(suffix.ToLowerInvariant())) + sArbitrarySeparator + suffix] = entry.Value;
                        hasNoSuffix = false;
                        break;
                    }
                }
                if (hasNoSuffix)
                {
                    newEntries[entry.Key] = entry.Value;
                }
            }
            if (!Directory.Exists(outputDirectory))
            {
                FileSystemUtils.CreateSTBLizePlusDirectoryFile(outputDirectory);
            }
            writeFilesCallback(outputDirectory, outputFilename ?? Path.GetFileName(pathWithoutExtension), newEntries);
        }

        public static void WriteXml(IDictionary entries, StreamWriter writer)
        {
            writer.WriteLine("<?xml version=\"1.0\" ?>" + Environment.NewLine + "<TEXT>" + Environment.NewLine);
            foreach (DictionaryEntry entry in entries)
            {
                writer.WriteLine("<KEY>" + (entry.Key.ToString().EndsWith(sArbitraryMaleSuffix) ? entry.Key.ToString().Substring(0, entry.Key.ToString().LastIndexOf(sArbitraryMaleSuffix)) : entry.Key.ToString()).Replace(sArbitrarySeparator, "") + "</KEY>" + Environment.NewLine + "<STR>" + entry.Value + "</STR>" + Environment.NewLine);
            }
            writer.WriteLine("</TEXT>");
        }

        public static void WriteYaml(IDictionary entries, StreamWriter writer)
        {
            WriteYaml(entries, writer, 0, 4);
        }

        public static void WriteYaml(IDictionary entries, StreamWriter writer, int level, int indent)
        {
            entries = entries.Unflatten(sArbitrarySeparator);
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
                    writer.WriteLine(indentation + "\"" + (entry.Key.ToString().EndsWith(sArbitraryMaleSuffix) ? entry.Key.ToString().Substring(0, entry.Key.ToString().LastIndexOf(sArbitraryMaleSuffix)) : entry.Key.ToString()) + "\": \"" + entry.Value + "\"");
                    continue;
                }
                writer.WriteLine(indentation + "\"" + entry.Key + "\":");
                WriteYaml(dictionary, writer, level + 1, indent);
            }
        }
    }
}

