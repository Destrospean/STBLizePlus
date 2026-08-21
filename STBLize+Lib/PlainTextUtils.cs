using System;
using System.Collections;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Destrospean.STBLizePlus
{
    public class PlainTextUtils
    {
        public static bool TryReadXml(string path, out IDictionary entries)
        {
            entries = null;
            using (var stream = File.OpenRead(path))
            {
                return TryReadXml(stream, out entries);
            }
        }

        public static bool TryReadXml(Stream stream, out IDictionary entries)
        {
            entries = null;
            try
            {
                entries = new System.Collections.Specialized.OrderedDictionary();
                var reader = System.Xml.XmlReader.Create(stream);
                reader.ReadStartElement("TEXT");
                while (reader.ReadToNextSibling("KEY"))
                {
                    var key = reader.ReadElementContentAsString();
                    if (reader.ReadToNextSibling("STR"))
                    {
                        entries[key] = reader.ReadElementContentAsString();
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
            using (var stream = File.OpenRead(path))
            {
                return TryReadYaml(stream, out entries);
            }
        }

        public static bool TryReadYaml(Stream stream, out IDictionary entries)
        {
            entries = null;
            try
            {
                using (var reader = new StreamReader(stream))
                {
                    entries = new DeserializerBuilder().Build().Deserialize<IDictionary>(reader).Flatten("");
                }
                return true;
            }
            catch (YamlException)
            {
                return false;
            }
        }

        public static void WriteFile(IDictionary entries, string directory, string filename, string fileType, Action<IDictionary, StreamWriter> writeFileCallback, bool outputFileNameUndefined = true, params string[] fileTypes)
        {
            if (fileTypes.Length > 1)
            {
                filename = Path.GetFileNameWithoutExtension(filename);
            }
            if (!Array.Exists(fileTypes, x => x == fileType))
            {
                return;
            }
            using (var stream = File.Create(directory + Path.DirectorySeparatorChar + filename + ((outputFileNameUndefined || fileTypes.Length > 1) && !filename.ToLowerInvariant().EndsWith("." + fileType.ToLowerInvariant()) ? "." + fileType.ToLowerInvariant() : "")))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writeFileCallback(entries, writer);
                }
            }
        }

        public static void WriteFiles(string inputPath, string unhashedFilename, string outputDirectory, string outputFilename, Action<string, string, IDictionary> writeFilesCallback, params string[] outputFileTypes)
        {
            var pathWithoutExtension = Path.GetFullPath(inputPath.Contains(".") ? inputPath.Substring(0, inputPath.LastIndexOf(".")) : inputPath);
            if (!Directory.Exists(outputDirectory))
            {
                FileSystemUtils.CreateSTBLizePlusDirectoryFile(outputDirectory);
            }
            writeFilesCallback(outputDirectory, outputFilename ?? Path.GetFileName(pathWithoutExtension), STBLUtils.GetEntriesWithUnhashedKeys(inputPath, Path.GetDirectoryName(pathWithoutExtension) + Path.DirectorySeparatorChar + unhashedFilename));
        }

        public static void WriteXml(IDictionary entries, StreamWriter writer)
        {
            writer.WriteLine("<?xml version=\"1.0\" ?>" + Environment.NewLine + "<TEXT>" + Environment.NewLine);
            foreach (DictionaryEntry entry in entries)
            {
                writer.WriteLine("<KEY>" + entry.Key.ToString().RemoveArbitraryMaleSuffix().Replace(STBLUtils.ArbitrarySeparator, "") + "</KEY>" + Environment.NewLine + "<STR>" + entry.Value + "</STR>" + Environment.NewLine);
            }
            writer.WriteLine("</TEXT>");
        }

        public static void WriteYaml(IDictionary entries, StreamWriter writer)
        {
            WriteYaml(entries, writer, 0, 4);
        }

        public static void WriteYaml(IDictionary entries, StreamWriter writer, int level, int indent)
        {
            entries = entries.Unflatten(STBLUtils.ArbitrarySeparator);
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
                    writer.WriteLine(indentation + "\"" + entry.Key.ToString().RemoveArbitraryMaleSuffix() + "\": \"" + entry.Value + "\"");
                    continue;
                }
                writer.WriteLine(indentation + "\"" + entry.Key + "\":");
                WriteYaml(dictionary, writer, level + 1, indent);
            }
        }
    }
}
