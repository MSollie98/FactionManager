using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using Verse;

namespace FactionManager
{
    namespace ModSupport
    {
        public static class InfinityStorageSupport
        {
            public static bool InfinityStorageActive() => ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name.Contains("Infinite Storage"));

            public static bool FindThingsOfTypeNextTo_Prefix(Map map, IntVec3 position, int distance, ref IEnumerable<Thing> __result)
            {
                if (map == null) {
                    __result = new List<Thing>();
                    return false;
                }
                return true;
            }
        }

        public static class PrepareSaveGame
        {
            public static void FixComponentDictionary(XmlDocument xmlDocument, XmlDocument mapXmlDocument, string componentName, string nodeName, string storeName)
            {
                XmlNode src = mapXmlDocument.SelectSingleNode($"/mapsave/{storeName}");
                XmlElement components = xmlDocument.DocumentElement["game"]["components"];

                if (src == null || components == null)
                    return;

                foreach (XmlElement li in components.ChildNodes)
                {
                    if (li.GetAttribute("Class") == componentName)
                    {
                        Dictionary<XmlNode, XmlNode> forAdd = new Dictionary<XmlNode, XmlNode>();
                        XmlNodeList destKeys = li[nodeName]["keys"].ChildNodes;
                        XmlNodeList srcKeys = src[nodeName]["keys"].ChildNodes;
                        XmlNodeList srcValues = src[nodeName]["values"].ChildNodes;

                        for (int i = 0; i < srcKeys.Count; ++i)
                        {
                            var key = srcKeys[i];
                            if (destKeys.Cast<XmlNode>().Count(x => x.InnerText == key.InnerText) == 0)
                            {
                                forAdd.Add(key, srcValues[i]);
                            }
                        }

                        foreach (var x in forAdd)
                        {
                            li[nodeName]["keys"].AppendChild( xmlDocument.ImportNode(x.Key, true) );
                            li[nodeName]["values"].AppendChild( xmlDocument.ImportNode(x.Value, true) );
                        }

                        if (forAdd.Count > 0)
                            Log.Warning($"[FactionManager] Added {forAdd.Count} for {componentName}");
                    }
                }
            }

            public static void FixBetterPawnControl(XmlDocument xmlDocument, XmlDocument mapXmlDocument, string keyName, string fieldName)
            {
                XmlNode src = mapXmlDocument.SelectSingleNode($"/mapsave/BetterPawnControl");
                XmlElement worldObjects = xmlDocument.DocumentElement["game"]["world"]["worldObjects"]["worldObjects"];

                if (src == null || worldObjects == null)
                    return;

                foreach (XmlElement li in worldObjects.ChildNodes)
                {
                    if (li.GetAttribute("Class").Contains("BetterPawnControl"))
                    {
                        List<XmlNode> forAdd = new List<XmlNode>();
                        XmlNodeList destKeys = li[keyName].ChildNodes;
                        XmlNodeList srcKeys = src[keyName].ChildNodes;

                        for (int i = 0; i < srcKeys.Count; ++i)
                        {
                            var key = srcKeys[i];
                            if (destKeys.Cast<XmlNode>().Count(x => x[fieldName].InnerText == key[fieldName].InnerText) == 0)
                            {
                                forAdd.Add(key);
                            }
                        }

                        foreach (var x in forAdd)
                        {
                            li[keyName].AppendChild( xmlDocument.ImportNode(x, true) );
                        }

                        if (forAdd.Count > 0)
                            Log.Warning($"[FactionManager] Added {forAdd.Count} for BetterPawnControl({keyName})");

                        // remove null entries after unload map
                        var childs = li[keyName].ChildNodes;
                        for (int i = childs.Count - 1; i >= 0; i--)
                        {
                            var node = childs[i];
                            if (node[fieldName].InnerText == "null")
                            {
                                Log.Warning($"[FactionManager] Removed null {fieldName} for BetterPawnControl");
                                node.ParentNode.RemoveChild(node);
                            }
                        }
                    }
                }
            }
        }

        public static class Savemap
        {
            public static void StoreComponentDictionary(string componentName, string storeName)
            {
                GameComponent gc = Current.Game.components.Find(x => x.GetType().ToString() == componentName);
                if (gc != null)
                    Scribe_Deep.Look(ref gc, storeName);
            }
        }
    }
}