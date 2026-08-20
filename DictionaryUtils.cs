using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Destrospean.STBLizePlus
{
    public static class DictionaryUtils
    {
        static string ConstructKey(string previousKey, string separator, object newKey, string replaceSeparators = null)
        {
            if (replaceSeparators != null)
            {
                newKey = newKey.ToString().Replace(separator, replaceSeparators);
            }
            return string.IsNullOrEmpty(previousKey) ? newKey.ToString() : string.Format("{0}{1}{2}", previousKey, separator, newKey);
        }

        public static IDictionary Flatten(this IDictionary dictionary, string separator = ".", IEnumerable<string> rootKeysToIgnore = null, string replaceSeparators = null, bool flattenLists = true)
        {
            if (rootKeysToIgnore == null)
            {
                rootKeysToIgnore = new HashSet<string>();
            }
            if (dictionary.Count == 0)
            {
                return new OrderedDictionary();
            }
            var flattenedDictionary = new OrderedDictionary();
            Action<object, string> flatten = null;
            flatten = (dictionaryObject, key) =>
                {
                    if (dictionaryObject as IEnumerable == null || dictionaryObject is string)
                    {
                        flattenedDictionary[key] = dictionaryObject.ToString();
                        return;
                    }
                    var tempDictionary = dictionaryObject as IDictionary;
                    if (tempDictionary != null)
                    {
                        foreach (var objectKey in tempDictionary.Keys)
                        {
                            if (!string.IsNullOrEmpty(key) || !rootKeysToIgnore.Contains(objectKey.ToString()))
                            {
                                flatten(tempDictionary[objectKey], ConstructKey(key, separator, objectKey, replaceSeparators));
                            }
                        }
                        return;
                    }
                    if (flattenLists && (dictionaryObject.GetType().IsArray || dictionaryObject is IList || typeof(ISet<>).IsAssignableFrom(dictionaryObject.GetType()) || typeof(Tuple<>).IsAssignableFrom(dictionaryObject.GetType())))
                    {
                        var i = 0;
                        foreach (var item in dictionaryObject as IEnumerable)
                        {
                            flatten(item, ConstructKey(key, separator, i++, replaceSeparators));
                        }
                        return;
                    }
                    flattenedDictionary[key] = dictionaryObject.ToString();
                };
            flatten(dictionary, null);
            return flattenedDictionary;
        }

        public static IDictionary Unflatten(this IDictionary dictionary, string separator = ".", bool listsAreFlat = true)
        {
            var unflattenedDictionary = new OrderedDictionary();
            Action<IDictionary, IEnumerable<string>, object> unflatten = null;
            unflatten = (dictionaryObject, keys, value) =>
                {
                    foreach (var key in keys.ToList().GetRange(0, keys.Count() - 1))
                    {
                        dictionaryObject = (IDictionary)(dictionaryObject.Contains(key) ? dictionaryObject[key] : dictionaryObject[key] = new OrderedDictionary());
                    }
                    dictionaryObject[keys.ToArray()[keys.Count() - 1]] = value;
                };
            var keyList = new List<string>(dictionary.Keys.Cast<string>());
            for (var i = 0; i < keyList.Count; i++)
            {
                if (i == keyList.Count - 1)
                {
                    unflatten(unflattenedDictionary, keyList[i].Split(new[]
                        {
                            separator
                        }, StringSplitOptions.None), dictionary[keyList[i]]);
                    continue;
                }
                var nextSplitKey = keyList[i + 1].Split(new[]
                    {
                        separator
                    }, StringSplitOptions.None);
                if (!keyList[i].Split(new[]
                    {
                        separator
                    }, StringSplitOptions.None).SequenceEqual(nextSplitKey.ToList().GetRange(0, nextSplitKey.Length - 1)))
                {
                    unflatten(unflattenedDictionary, keyList[i].Split(new[]
                        {
                            separator
                        }, StringSplitOptions.None), dictionary[keyList[i]]);
                }
            };
            return unflattenedDictionary;
        }
    }
}
