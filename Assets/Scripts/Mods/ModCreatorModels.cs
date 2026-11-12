using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madness.Mods
{
    public enum ModCreatorAssetKind
    {
        Prefab,
        Sprite,
        Material,
        Audio,
        ScriptableObject,
        Scene,
        Other
    }

    [Serializable]
    public sealed class ModCreatorAssetEntry
    {
        public string id = "content/item";
        public UnityEngine.Object asset;
        public ModCreatorAssetKind kind = ModCreatorAssetKind.Prefab;
        public string replaceId = "";
    }

    [Serializable]
    public sealed class ModCreatorDialogueLine
    {
        public string speakerName = "";
        [TextArea(2, 5)] public string text = "";
    }

    [Serializable]
    public sealed class ModCreatorDialogueEntry
    {
        public string eventId = "dialogue/example";
        public string speakerName = "";
        public bool replaceAllLines = true;
        public List<ModCreatorDialogueLine> lines = new List<ModCreatorDialogueLine>();
    }

    [Serializable]
    public sealed class ModCreatorSceneEntry
    {
        public string sceneId = "scene/example";
        public UnityEngine.Object scene;
        public bool additive;
    }

    [CreateAssetMenu(fileName = "ModCreatorProject", menuName = "Madness/Mods/Mod Creator Project")]
    public sealed class ModCreatorProject : ScriptableObject
    {
        [Header("Mod kimliği")]
        public string id = "my_mod";
        public string displayName = "Yeni Mod";
        [TextArea(2, 5)] public string description = "";
        public string version = "1.0.0";
        public string author = "";
        public int loadOrder;
        public bool enabledByDefault = true;
        public bool totalConversion;
        public UnityEngine.Object mainScene;

        [Header("DLL - opsiyonel")]
        [Tooltip("Tek ana DLL. DLL, manifestte entryScript olarak paketlenir.")]
        public string dllPath = "";

        [Header("Panel ayarları")]
        public List<ModSettingDefinition> settings = new List<ModSettingDefinition>();

        [Header("Asset değişiklikleri ve yeni içerik")]
        public List<ModCreatorAssetEntry> assets = new List<ModCreatorAssetEntry>();

        [Header("Diyalog değişiklikleri")]
        public List<ModCreatorDialogueEntry> dialogues = new List<ModCreatorDialogueEntry>();

        [Header("Ek sahneler")]
        public List<ModCreatorSceneEntry> scenes = new List<ModCreatorSceneEntry>();
    }

    [Serializable]
    public sealed class ModCreatorContentManifest
    {
        public int schemaVersion = 1;
        public List<ModCreatorContentItem> assets = new List<ModCreatorContentItem>();
        public List<ModCreatorDialoguePatch> dialogues = new List<ModCreatorDialoguePatch>();
    }

    [Serializable]
    public sealed class ModCreatorContentItem
    {
        public string id = "";
        public string assetName = "";
        public string kind = "Other";
        public string replaceId = "";
    }

    [Serializable]
    public sealed class ModCreatorDialoguePatch
    {
        public string eventId = "";
        public string speakerName = "";
        public bool replaceAllLines = true;
        public List<ModCreatorDialogueLine> lines = new List<ModCreatorDialogueLine>();
    }
}
