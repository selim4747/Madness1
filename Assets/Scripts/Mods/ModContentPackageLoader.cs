using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// Mod Creator tarafından üretilen content.json + content.bundle paketlerini yükler.
    /// Yeni içerikleri registry'ye ekler, replaceId verilen içerikleri override eder ve
    /// diyalog patch'lerini ModDialogue event bus üzerinden uygular.
    /// </summary>
    public static class ModContentPackageLoader
    {
        private const string ManifestFile = "content.json";
        private const string BundleFile = "content.bundle";
        private static readonly HashSet<string> loadedOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void LoadAll(ModRuntime runtime)
        {
            if (runtime == null) return;
            UnloadAll();

            foreach (ModManifest manifest in runtime.LoadedManifests)
            {
                if (manifest == null || !manifest.enabled) continue;
                string dependencyError = runtime.GetDependencyError(manifest);
                if (!string.IsNullOrEmpty(dependencyError)) continue;

                string directory = runtime.GetModDirectory(manifest.id);
                if (string.IsNullOrEmpty(directory)) continue;

                string jsonPath = Path.Combine(directory, ManifestFile);
                if (!File.Exists(jsonPath)) continue;

                try
                {
                    ModCreatorContentManifest content = JsonUtility.FromJson<ModCreatorContentManifest>(File.ReadAllText(jsonPath));
                    if (content == null) continue;
                    LoadAssets(manifest.id, content, directory);
                    RegisterDialoguePatches(manifest.id, content.dialogues);
                }
                catch (Exception ex)
                {
                    runtime.ReportDiagnostic(manifest.id, "İçerik paketi yüklenemedi: " + ex.Message, true);
                }
            }
        }

        private static void LoadAssets(string modId, ModCreatorContentManifest content, string directory)
        {
            if (content.assets == null) content.assets = new List<ModCreatorContentItem>();
            loadedOwners.Add(modId);
            bool hasAssetEntries = content.assets.Count > 0;
            AssetBundle bundle = hasAssetEntries ? ModAssetLoader.LoadBundle(modId, BundleFile) : null;
            if (hasAssetEntries && bundle == null)
            {
                Debug.LogWarning("[ModContent] " + modId + " için content.bundle bulunamadı.");
                return;
            }

            for (int i = 0; i < content.assets.Count; i++)
            {
                ModCreatorContentItem item = content.assets[i];
                if (item == null || string.IsNullOrWhiteSpace(item.id) || string.IsNullOrWhiteSpace(item.assetName)) continue;

                UnityEngine.Object asset = bundle.LoadAsset<UnityEngine.Object>(item.assetName);
                if (asset == null)
                {
                    Debug.LogWarning("[ModContent] Asset bulunamadı: " + modId + "/" + item.assetName);
                    continue;
                }

                string targetId = string.IsNullOrWhiteSpace(item.replaceId) ? item.id : item.replaceId;
                if (!string.IsNullOrWhiteSpace(item.replaceId))
                    ModContentOverride.Register(modId, targetId, asset);
                else
                    ModContentRegistry.Replace(modId, targetId, asset);
            }
        }

        private static void RegisterDialoguePatches(string modId, List<ModCreatorDialoguePatch> patches)
        {
            if (patches == null) return;
            loadedOwners.Add(modId);
            for (int i = 0; i < patches.Count; i++)
            {
                ModCreatorDialoguePatch patch = patches[i];
                if (patch == null || string.IsNullOrWhiteSpace(patch.eventId)) continue;
                ModEventBus.Subscribe<DialogueContext>(ctx =>
                {
                    if (ctx == null || !string.Equals(ctx.EventId, patch.eventId, StringComparison.OrdinalIgnoreCase)) return;
                    if (patch.replaceAllLines)
                    {
                        ctx.Lines = ConvertLines(patch.lines);
                    }
                    else
                    {
                        ctx.Lines.AddRange(ConvertLines(patch.lines));
                    }
                    if (!string.IsNullOrWhiteSpace(patch.speakerName)) ctx.SpeakerName = patch.speakerName;
                }, 0, modId);
            }
        }

        private static List<DialogueLine> ConvertLines(List<ModCreatorDialogueLine> source)
        {
            List<DialogueLine> result = new List<DialogueLine>();
            if (source == null) return result;
            for (int i = 0; i < source.Count; i++)
            {
                ModCreatorDialogueLine line = source[i];
                if (line == null) continue;
                result.Add(new DialogueLine
                {
                    speakerName = line.speakerName ?? "",
                    text = line.text ?? "",
                    ekDiller = new List<DilMetniGirisi>()
                });
            }
            return result;
        }

        public static void UnloadAll()
        {
            // Önce callback ve registry kayıtlarını temizle, sonra bundle'ları kapat.
            List<string> owners = new List<string>(loadedOwners);
            foreach (string owner in owners) ModEventBus.UnsubscribeOwner(owner);
            foreach (string owner in owners)
            {
                ModContentOverride.RemoveOwner(owner);
                ModContentRegistry.RemoveOwner(owner);
            }
            loadedOwners.Clear();
            ModAssetLoader.UnloadAll();
        }
    }
}
