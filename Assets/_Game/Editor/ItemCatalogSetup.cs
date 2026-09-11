using System;
using System.IO;
using System.Linq;
using Game.Client.Interactions;
using Game.SOAP.Config;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class ItemCatalogSetup
    {
        public const string Path = "Assets/_Game/Content/Resources/Items/ItemCatalog.asset";
        [InitializeOnLoadMethod]
        private static void Initialize() => EditorApplication.update += CheckRequest;
        private static void CheckRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode ||
                !File.Exists("Temp/ImportItemCatalog.request")) return;
            File.Delete("Temp/ImportItemCatalog.request");
            try { Import(); File.WriteAllText("Temp/ImportItemCatalog.done", "PASS"); }
            catch (Exception e) { File.WriteAllText("Temp/ImportItemCatalog.error", e.ToString()); Debug.LogException(e); }
        }

        [MenuItem("Tools/Game/Items/Import New Collection Items Into Catalog")]
        public static void Import()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(Path);
            if (catalog == null)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                catalog = ScriptableObject.CreateInstance<ItemCatalogSO>();
                AssetDatabase.CreateAsset(catalog, Path);
            }
            var manifest = JsonUtility.FromJson<ItemCollectionBuilder.Manifest>(File.ReadAllText(ItemCollectionBuilder.Folder + "/ItemCollection.json"));
            foreach (var entry in manifest.items)
            {
                var category = catalog.categories.FirstOrDefault(c => c.id == entry.category);
                if (category == null)
                {
                    category = new ItemCatalogSO.Category { id = entry.category, label = entry.categoryName, enabled = entry.category != "reserve" };
                    catalog.categories.Add(category);
                }
                var id = "i" + entry.id.Substring(entry.id.LastIndexOf('_') + 1);
                if (catalog.categories.Any(c => c.items.Any(i => i.id == id))) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ItemCollectionBuilder.Folder}/Prefabs/{entry.category}/{entry.id}.prefab");
                if (prefab == null || prefab.GetComponent<CarryableItem>() == null) throw new InvalidOperationException("Missing carryable prefab: " + entry.id);
                category.items.Add(new ItemCatalogSO.Item { id = id, displayName = entry.displayName, prefab = prefab });
            }
            catalog.Apply();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
        }
    }
}
