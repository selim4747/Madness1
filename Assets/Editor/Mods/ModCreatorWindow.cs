#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Madness.Mods;

namespace Madness.Mods.Editor
{
    /// <summary>
    /// Madness > Mod Creator menüsünden açılır.
    /// Asset, görünüm, ayar, diyalog, sahne ve tek ana DLL içeren .madnessmod paketi üretir.
    /// </summary>
    public sealed class ModCreatorWindow : EditorWindow
    {
        private ModCreatorProject project;
        private Vector2 scroll;
        private string lastResult = "";

        [MenuItem("Madness/Mod Creator", priority = 20)]
        public static void Open()
        {
            GetWindow<ModCreatorWindow>("Madness Mod Creator");
        }

        [MenuItem("Assets/Create/Madness/Mod Creator Project", priority = 20)]
        private static void CreateProjectAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Mod Creator projesi", "NewModCreatorProject", "asset", "Mod projesi oluştur");
            if (string.IsNullOrEmpty(path)) return;
            ModCreatorProject asset = CreateInstance<ModCreatorProject>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Bu pencere Unity içindeki seçtiğiniz assetleri, diyalogları, sahneleri ve opsiyonel DLL'i tek bir .madnessmod paketi yapar. Hazır modlar da aynı paket formatıyla oyundaki ModImportUI üzerinden eklenebilir.", MessageType.Info);
            project = (ModCreatorProject)EditorGUILayout.ObjectField("Mod Creator Projesi", project, typeof(ModCreatorProject), false);

            if (project == null)
            {
                if (GUILayout.Button("Yeni Mod Creator Projesi Oluştur")) CreateProjectAsset();
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawIdentity();
            DrawSettings();
            DrawAssets();
            DrawDialogues();
            DrawScenes();
            DrawBuildActions();
            EditorGUILayout.EndScrollView();
        }

        private void DrawIdentity()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("1. Mod kimliği", EditorStyles.boldLabel);
            project.id = EditorGUILayout.TextField("ID", project.id);
            project.displayName = EditorGUILayout.TextField("Görünen ad", project.displayName);
            project.author = EditorGUILayout.TextField("Yazar", project.author);
            project.version = EditorGUILayout.TextField("Sürüm", project.version);
            project.loadOrder = EditorGUILayout.IntField("Yükleme sırası", project.loadOrder);
            project.enabledByDefault = EditorGUILayout.Toggle("Varsayılan açık", project.enabledByDefault);
            project.description = EditorGUILayout.TextArea(project.description, GUILayout.MinHeight(50));
            project.totalConversion = EditorGUILayout.Toggle("Total conversion", project.totalConversion);
            project.mainScene = EditorGUILayout.ObjectField("Ana sahne", project.mainScene, typeof(SceneAsset), false);

            EditorGUILayout.BeginHorizontal();
            project.dllPath = EditorGUILayout.TextField("Ana DLL yolu", project.dllPath);
            if (GUILayout.Button("Seç", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFilePanel("DLL seç", Application.dataPath, "dll");
                if (!string.IsNullOrEmpty(selected)) project.dllPath = selected;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("DLL güvenilmeyen kod çalıştırabilir. Yalnızca kaynağına güvendiğiniz DLL'leri paketleyin.", MessageType.Warning);
            MarkDirty();
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("2. Mod paneli ayarları", EditorStyles.boldLabel);
            if (project.settings == null) project.settings = new List<ModSettingDefinition>();
            for (int i = 0; i < project.settings.Count; i++)
            {
                ModSettingDefinition setting = project.settings[i];
                if (setting == null) { project.settings[i] = new ModSettingDefinition(); setting = project.settings[i]; }
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Ayar " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("Sil", GUILayout.Width(45))) { project.settings.RemoveAt(i); i--; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                setting.key = EditorGUILayout.TextField("Anahtar", setting.key);
                setting.label = EditorGUILayout.TextField("Panel etiketi", setting.label);
                setting.type = EditorGUILayout.TextField("Tip (bool/int/float/string/dropdown)", setting.type);
                setting.defaultValue = EditorGUILayout.TextField("Varsayılan", setting.defaultValue);
                if (setting.type == "int" || setting.type == "float")
                {
                    setting.min = EditorGUILayout.FloatField("Minimum", setting.min);
                    setting.max = EditorGUILayout.FloatField("Maksimum", setting.max);
                }
                setting.restartRequired = EditorGUILayout.Toggle("Yeniden başlatma gerekli", setting.restartRequired);
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Ayar ekle")) project.settings.Add(new ModSettingDefinition { key = "newSetting", label = "Yeni ayar", type = "float", defaultValue = "1" });
            MarkDirty();
        }

        private void DrawAssets()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("3. Asset / görünüm / yeni içerik", EditorStyles.boldLabel);
            if (project.assets == null) project.assets = new List<ModCreatorAssetEntry>();
            for (int i = 0; i < project.assets.Count; i++)
            {
                ModCreatorAssetEntry entry = project.assets[i];
                if (entry == null) { project.assets[i] = new ModCreatorAssetEntry(); entry = project.assets[i]; }
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("İçerik " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("Sil", GUILayout.Width(45))) { project.assets.RemoveAt(i); i--; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                entry.id = EditorGUILayout.TextField("İçerik ID", entry.id);
                entry.asset = EditorGUILayout.ObjectField("Asset", entry.asset, typeof(UnityEngine.Object), false);
                entry.kind = (ModCreatorAssetKind)EditorGUILayout.EnumPopup("Tür", entry.kind);
                entry.replaceId = EditorGUILayout.TextField("Değiştirilecek ID (opsiyonel)", entry.replaceId);
                EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(entry.replaceId) ? "Yeni içerik olarak eklenir. Mod kodu ModAPI.GetModContent<T>(ID) ile alabilir." : "Mevcut içerik ID'sinin yerine geçer.", MessageType.None);
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Asset ekle")) project.assets.Add(new ModCreatorAssetEntry());
            MarkDirty();
        }

        private void DrawDialogues()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("4. Diyalog değişiklikleri", EditorStyles.boldLabel);
            if (project.dialogues == null) project.dialogues = new List<ModCreatorDialogueEntry>();
            for (int i = 0; i < project.dialogues.Count; i++)
            {
                ModCreatorDialogueEntry entry = project.dialogues[i];
                if (entry == null) { project.dialogues[i] = new ModCreatorDialogueEntry(); entry = project.dialogues[i]; }
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Diyalog " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("Sil", GUILayout.Width(45))) { project.dialogues.RemoveAt(i); i--; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                entry.eventId = EditorGUILayout.TextField("Event ID", entry.eventId);
                entry.speakerName = EditorGUILayout.TextField("Konuşmacı override", entry.speakerName);
                entry.replaceAllLines = EditorGUILayout.Toggle("Mevcut satırları değiştir", entry.replaceAllLines);
                if (entry.lines == null) entry.lines = new List<ModCreatorDialogueLine>();
                for (int j = 0; j < entry.lines.Count; j++)
                {
                    ModCreatorDialogueLine line = entry.lines[j];
                    if (line == null) { entry.lines[j] = new ModCreatorDialogueLine(); line = entry.lines[j]; }
                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Satır " + (j + 1));
                    if (GUILayout.Button("Sil", GUILayout.Width(45))) { entry.lines.RemoveAt(j); j--; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                    EditorGUILayout.EndHorizontal();
                    line.speakerName = EditorGUILayout.TextField("İsim", line.speakerName);
                    line.text = EditorGUILayout.TextArea(line.text, GUILayout.MinHeight(45));
                    EditorGUILayout.EndVertical();
                }
                if (GUILayout.Button("Diyalog satırı ekle")) entry.lines.Add(new ModCreatorDialogueLine());
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Diyalog ekle")) project.dialogues.Add(new ModCreatorDialogueEntry());
            MarkDirty();
        }

        private void DrawScenes()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("5. Ek sahneler", EditorStyles.boldLabel);
            if (project.scenes == null) project.scenes = new List<ModCreatorSceneEntry>();
            for (int i = 0; i < project.scenes.Count; i++)
            {
                ModCreatorSceneEntry entry = project.scenes[i];
                if (entry == null) { project.scenes[i] = new ModCreatorSceneEntry(); entry = project.scenes[i]; }
                EditorGUILayout.BeginHorizontal("box");
                entry.sceneId = EditorGUILayout.TextField(entry.sceneId, GUILayout.Width(150));
                entry.scene = EditorGUILayout.ObjectField(entry.scene, typeof(SceneAsset), false);
                entry.additive = EditorGUILayout.ToggleLeft("Additive", entry.additive, GUILayout.Width(85));
                if (GUILayout.Button("Sil", GUILayout.Width(45))) { project.scenes.RemoveAt(i); i--; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Sahne ekle")) project.scenes.Add(new ModCreatorSceneEntry());
            MarkDirty();
        }

        private void DrawBuildActions()
        {
            EditorGUILayout.Space(12);
            if (!string.IsNullOrEmpty(lastResult)) EditorGUILayout.HelpBox(lastResult, MessageType.Info);
            GUI.backgroundColor = new Color(0.35f, 0.85f, 0.45f);
            if (GUILayout.Button(".madnessmod paketi oluştur", GUILayout.Height(36))) BuildPackage();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox("Oluşan paket, oyundaki ModImportUI > Import Button üzerinden içeri alınabilir. Paket açıldığında manifest, content.json, content.bundle ve varsa ana DLL bulunur.", MessageType.None);
        }

        private void BuildPackage()
        {
            string error = ValidateProject();
            if (!string.IsNullOrEmpty(error)) { lastResult = error; return; }

            string output = EditorUtility.SaveFilePanel("Madness mod paketi", "", project.id + ".madnessmod", "madnessmod");
            if (string.IsNullOrEmpty(output)) return;
            if (!output.EndsWith(".madnessmod", StringComparison.OrdinalIgnoreCase)) output += ".madnessmod";

            string buildRoot = Path.Combine("Library", "MadnessModCreator", project.id + "_" + Guid.NewGuid().ToString("N"));
            string bundleDir = Path.Combine(buildRoot, "bundle_build");
            string packageDir = Path.Combine(buildRoot, "package");
            try
            {
                Directory.CreateDirectory(bundleDir);
                Directory.CreateDirectory(packageDir);
                List<string> assetPaths = CollectAssetPaths();
                if (assetPaths.Count > 0)
                {
                    AssetBundleBuild build = new AssetBundleBuild
                    {
                        assetBundleName = "content.bundle",
                        assetNames = assetPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    };
                    AssetBundleManifest bundleManifest = BuildPipeline.BuildAssetBundles(
                        bundleDir,
                        new[] { build },
                        BuildAssetBundleOptions.None,
                        EditorUserBuildSettings.activeBuildTarget);
                    string builtBundlePath = Path.Combine(bundleDir, "content.bundle");
                    if (bundleManifest == null || !File.Exists(builtBundlePath))
                        throw new InvalidOperationException("content.bundle oluşturulamadı. Console'daki AssetBundle hatasını kontrol edin.");
                    File.Copy(builtBundlePath, Path.Combine(packageDir, "content.bundle"), true);
                }

                ModManifest manifest = new ModManifest
                {
                    id = project.id.Trim(), name = project.displayName, description = project.description,
                    version = project.version, apiVersion = ModRuntime.CurrentApiVersion.ToString(),
                    author = project.author, enabled = project.enabledByDefault, loadOrder = project.loadOrder,
                    totalConversion = project.totalConversion
                };
                string mainScenePath = AssetDatabase.GetAssetPath(project.mainScene);
                if (!string.IsNullOrEmpty(mainScenePath)) manifest.mainScene = Path.GetFileNameWithoutExtension(mainScenePath);

                if (!string.IsNullOrEmpty(project.dllPath))
                {
                    string dllName = Path.GetFileName(project.dllPath);
                    File.Copy(project.dllPath, Path.Combine(packageDir, dllName), true);
                    manifest.entryScript = dllName;
                }
                manifest.settings = project.settings ?? new List<ModSettingDefinition>();
                File.WriteAllText(Path.Combine(packageDir, "manifest.json"), JsonUtility.ToJson(manifest, true));
                File.WriteAllText(Path.Combine(packageDir, "content.json"), JsonUtility.ToJson(BuildContentManifest(), true));

                if (File.Exists(output)) File.Delete(output);
                ZipFile.CreateFromDirectory(packageDir, output, System.IO.Compression.CompressionLevel.Optimal, false);
                lastResult = "Paket hazır: " + output;
                EditorUtility.RevealInFinder(output);
            }
            catch (Exception ex)
            {
                lastResult = "Paket oluşturulamadı: " + ex.Message;
                Debug.LogException(ex);
            }
            finally
            {
                if (Directory.Exists(buildRoot)) Directory.Delete(buildRoot, true);
            }
        }

        private ModCreatorContentManifest BuildContentManifest()
        {
            ModCreatorContentManifest result = new ModCreatorContentManifest();
            for (int i = 0; i < project.assets.Count; i++)
            {
                ModCreatorAssetEntry entry = project.assets[i];
                string path = AssetDatabase.GetAssetPath(entry.asset);
                if (string.IsNullOrEmpty(path)) continue;
                result.assets.Add(new ModCreatorContentItem
                {
                    id = entry.id,
                    assetName = path,
                    kind = entry.kind.ToString(),
                    replaceId = entry.replaceId
                });
            }
            for (int i = 0; i < project.dialogues.Count; i++)
            {
                ModCreatorDialogueEntry entry = project.dialogues[i];
                result.dialogues.Add(new ModCreatorDialoguePatch
                {
                    eventId = entry.eventId,
                    speakerName = entry.speakerName,
                    replaceAllLines = entry.replaceAllLines,
                    lines = entry.lines ?? new List<ModCreatorDialogueLine>()
                });
            }
            return result;
        }

        private List<string> CollectAssetPaths()
        {
            List<string> paths = new List<string>();
            for (int i = 0; i < project.assets.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(project.assets[i].asset);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }
            string main = AssetDatabase.GetAssetPath(project.mainScene);
            if (!string.IsNullOrEmpty(main)) paths.Add(main);
            for (int i = 0; i < project.scenes.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(project.scenes[i].scene);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }
            return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private string ValidateProject()
        {
            if (project == null) return "Önce bir Mod Creator Project seçin.";
            if (!ModRuntime.IsSafeModId(project.id)) return "ID yalnızca harf, rakam, nokta, tire ve alt çizgi içerebilir.";
            if (string.IsNullOrWhiteSpace(project.displayName)) return "Görünen ad boş olamaz.";
            if (project.assets == null) project.assets = new List<ModCreatorAssetEntry>();
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModCreatorAssetEntry item in project.assets)
            {
                if (item == null || item.asset == null) return "Asset listesinde boş asset var.";
                if (string.IsNullOrWhiteSpace(item.id) || !ids.Add(item.id)) return "Asset ID boş veya tekrar ediyor: " + (item == null ? "null" : item.id);
            }
            if (!string.IsNullOrEmpty(project.dllPath) && !File.Exists(project.dllPath)) return "DLL dosyası bulunamadı: " + project.dllPath;
            if (project.totalConversion && project.mainScene == null) return "Total conversion için ana sahne seçin.";
            return null;
        }

        private void MarkDirty()
        {
            if (project == null) return;
            EditorUtility.SetDirty(project);
        }
    }
}
#endif
