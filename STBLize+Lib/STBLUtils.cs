using System.Collections;
using System.IO;

namespace Destrospean.STBLizePlus
{
    public static class STBLUtils
    {
        public static readonly string ArbitraryMaleSuffix = "{DESTROSPEAN_STBL_MALE_SUFFIX_" + System.Guid.NewGuid() + "}",
        ArbitrarySeparator = "{DESTROSPEAN_STBL_SEPARATOR_" + System.Guid.NewGuid() + "}";

        static void WriteEntryPair(BinaryWriter writer, string key, string value)
        {
            writer.Write(GetFnv64(key));
            writer.Write(value.Length);
            writer.Write(value.ToCharArray());
        }

        public static IDictionary GetEntriesWithUnhashedKeys(string stblPath, string unhashedStblPath)
        {
            using (var stblStream = File.OpenRead(stblPath))
            {
                using (var unhashedStblStream = File.OpenRead(unhashedStblPath))
                {
                    return GetEntriesWithUnhashedKeys(stblStream, unhashedStblStream);
                }
            }
        }

        public static IDictionary GetEntriesWithUnhashedKeys(Stream stblStream, Stream unhashedStblStream)
        {
            string[] suffixes = new[]
                {
                    "_Female",
                    "_FemaleFemale",
                    "_MaleFemale"
                };
            IDictionary entriesWithHashedKeys = ReadStbl(stblStream),
            entriesWithUnhashedKeys = new System.Collections.Specialized.OrderedDictionary(),
            intermediateEntries = new System.Collections.Specialized.OrderedDictionary();
            var keysToReplace = new System.Collections.Generic.List<string>();
            foreach (DictionaryEntry entry in ReadStbl(unhashedStblStream))
            {
                var value = entry.Value.ToString().Replace("/", ArbitrarySeparator + "/").Replace(":", ArbitrarySeparator + ":");
                foreach (var suffix in suffixes)
                {
                    if (value.ToLowerInvariant().EndsWith(suffix.ToLowerInvariant()))
                    {
                        keysToReplace.Add(value.Substring(0, value.ToLowerInvariant().LastIndexOf(suffix.ToLowerInvariant())));
                    }
                }
                intermediateEntries[value] = entriesWithHashedKeys[entry.Key];
            }
            foreach (DictionaryEntry entry in intermediateEntries)
            {
                if (keysToReplace.Contains(entry.Key.ToString()))
                {
                    entriesWithUnhashedKeys[entry.Key + ArbitrarySeparator + ArbitraryMaleSuffix] = entry.Value;
                    continue;
                }
                var hasNoSuffix = true;
                foreach (var suffix in suffixes)
                {
                    if (entry.Key.ToString().ToLowerInvariant().EndsWith(suffix.ToLowerInvariant()))
                    {
                        entriesWithUnhashedKeys[entry.Key.ToString().Substring(0, entry.Key.ToString().ToLowerInvariant().LastIndexOf(suffix.ToLowerInvariant())) + ArbitrarySeparator + suffix] = entry.Value;
                        hasNoSuffix = false;
                        break;
                    }
                }
                if (hasNoSuffix)
                {
                    entriesWithUnhashedKeys[entry.Key] = entry.Value;
                }
            }
            return entriesWithUnhashedKeys;
        }

        public static ulong GetFnv64(string value)
        {
            var hash = 0xCBF29CE484222325;
            foreach (var b in System.Text.Encoding.UTF8.GetBytes(value.ToLowerInvariant()))
            {
                hash *= 0x100000001B3;
                hash &= 0xFFFFFFFFFFFFFFFF;
                hash ^= b;
            }
            return hash;
        }

        public static IDictionary ReadStbl(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return ReadStbl(stream);
            }
        }

        public static IDictionary ReadStbl(Stream stream)
        {
            try
            {
                var entries = new System.Collections.Specialized.OrderedDictionary();
                using (var reader = new BinaryReader(stream, System.Text.Encoding.Unicode))
                {
                    reader.ReadBytes(7);
                    var count = reader.ReadInt32();
                    reader.ReadBytes(6);
                    for (var i = 0; i < count; i++)
                    {
                        var id = reader.ReadUInt64();
                        entries[id] = new string(reader.ReadChars(reader.ReadInt32()));
                    }
                }
                return entries;
            }
            catch
            {
                throw new System.ArgumentException("File must be a valid STBL");
            }
        }

        public static string RemoveArbitraryMaleSuffix(this string key)
        {
            return key.EndsWith(ArbitraryMaleSuffix) ? key.Substring(0, key.LastIndexOf(ArbitraryMaleSuffix)) : key;
        }

        public static void WriteStbl(string path, IDictionary entries, bool keysAsValues = false)
        {
            using (var stream = File.Create(path))
            {
                WriteStbl(stream, entries, keysAsValues);
            }
        }

        public static void WriteStbl(Stream stream, IDictionary entries, bool keysAsValues = false)
        {
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.Unicode))
            {
                writer.Write(new byte[]
                    {
                        83,
                        84,
                        66,
                        76
                    });
                writer.Write(new byte[]
                    {
                        2
                    });
                writer.Write(new byte[2]);
                writer.Write(entries.Count);
                writer.Write(new byte[6]);
                foreach (DictionaryEntry entry in entries)
                {
                    WriteEntryPair(writer, entry.Key.ToString(), keysAsValues ? entry.Key.ToString() : entry.Value.ToString());
                }
            }
        }
    }
}
