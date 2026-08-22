using System;
using System.Collections;
using System.IO;

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
                    entries = new YamlDotNet.Serialization.DeserializerBuilder().Build().Deserialize<IDictionary>(reader).Flatten("");
                }
                return true;
            }
            catch (YamlDotNet.Core.YamlException)
            {
                return false;
            }
        }

        public static void WriteJson(IDictionary entries, StreamWriter writer)
        {
            WriteJson(entries, writer, 4);
        }

        public static void WriteJson(IDictionary entries, StreamWriter writer, int indent = 4, int level = 0)
        {
            if (level == 0)
            {
                writer.WriteLine("{");
                WriteJson(entries, writer, 4, 1);
                return;
            }
            var indentation = "";
            for (var i = 0; i < indent * level; i++)
            {
                indentation += " ";
            }
            var index = 0;
            foreach (DictionaryEntry entry in entries)
            {
                var dictionary = entry.Value as IDictionary;
                if (dictionary == null)
                {
                    writer.WriteLine(indentation + "\"" + entry.Key + "\": \"" + entry.Value + "\"" + (index++ < entries.Count - 1 ? "," : ""));
                    continue;
                }
                writer.WriteLine(indentation + "\"" + entry.Key + "\": {");
                WriteJson(dictionary, writer, indent, level + 1);
                writer.WriteLine(index++ < entries.Count - 1 ? "," : "");
            }
            writer.Write(indentation.Remove(0, indent) + "}");
        }

        public static void WriteXml(IDictionary entries, StreamWriter writer)
        {
            writer.WriteLine("<?xml version=\"1.0\" ?>" + Environment.NewLine + "<TEXT>" + Environment.NewLine);
            foreach (DictionaryEntry entry in entries)
            {
                writer.WriteLine("<KEY>" + entry.Key + "</KEY>" + Environment.NewLine + "<STR>" + entry.Value + "</STR>" + Environment.NewLine);
            }
            writer.WriteLine("</TEXT>");
        }

        public static void WriteYaml(IDictionary entries, StreamWriter writer)
        {
            WriteYaml(entries, writer, 4);
        }

        public static void WriteYaml(IDictionary entries, StreamWriter writer, int indent = 4, int level = 0)
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
                    writer.WriteLine(indentation + "\"" + entry.Key + "\": \"" + entry.Value + "\"");
                    continue;
                }
                writer.WriteLine(indentation + "\"" + entry.Key + "\":");
                WriteYaml(dictionary, writer, indent, level + 1);
            }
        }
    }
}
