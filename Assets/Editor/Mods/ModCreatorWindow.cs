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
        private int activeTab;
        private string searchText = "";

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

            DrawProjectSummary();
            DrawToolbar();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            switch (activeTab)
            {
                case 0: DrawIdentity(); break;
                case 1: DrawSettings(); break;
                case 2: DrawAssets(); break;
                case 3: DrawDialogues(); break;
                case 4: DrawScenes(); break;
                default: DrawBuildActions(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawProjectSummary()
        {
            int assetCount = project.assets == null ? 0 : project.assets.Count;
            int dialogueCount = project.dialogues == null ? 0 : project.dialogues.Count;
            int sceneCount = project.scenes == null ? 0 : project.scenes.Count;
            int settingCount = project.settings == null ? 0 : project.settings.Count;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(project.displayName) ? "Yeni Mod" : project.displayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("ID: " + (string.IsNullOrWhiteSpace(project.id) ? "(belirlenmedi)" : project.id));
            EditorGUILayout.LabelField("İçerik: " + assetCount + "  |  Ayar: " + settingCount + "  |  Diyalog: " + dialogueCount + "  |  Sahne: " + sceneCount);
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            string[] tabs = { "Kimlik", "Ayarlar", "İçerikler", "Diyaloglar", "Sahneler", "Paketle" };
            activeTab = GUILayout.Toolbar(activeTab, tabs);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filtre", GUILayout.Width(42));
            searchText = EditorGUILayout.TextField(searchText, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.textField);
            if (GUILayout.Button("Temizle", GUILayout.Width(55))) searchText = "";
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrWhiteSpace(searchText))
                EditorGUILayout.HelpBox("Filtre yalnızca içerik, ayar ve diyalog listelerinde kullanılır.", MessageType.None);
        }

        private bool MatchesSearch(params string[] values)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return true;
            string query = searchText.Trim();
            for (int i = 0; i < values.Length; i++)
                if (!string.IsNullOrEmpty(values[i]) && values[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
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
            EditorGUILayout.HelpBox("Anahtar kod tarafındaki addır; etiket oyuncunun gördüğü addır. Burada tanımlanan ayarlar oyun içindeki mod panelinde görünür.", MessageType.Info);
            if (project.settings == null) project.settings = new List<ModSettingDefinition>();
            for (int i = 0; i < project.settings.Count; i++)
            {
                ModSettingDefinition setting = project.settings[i];
                if (setting == null) { project.settings[i] = new ModSettingDefinition(); setting = project.settings[i]; }
                if (!MatchesSearch(setting.key, setting.label, setting.type)) continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Ayar " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("Sil", GUILayout.Width(45))) { project.settings.RemoveAt(i); i--; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                setting.key = EditorGUILayout.TextField("Anahtar", setting.key);
                setting.label = EditorGUILayout.TextField("Panel etiketi", setting.label);
                string[] settingTypes = { "bool", "int", "float", "string", "dropdown" };
                int typeIndex = Array.IndexOf(settingTypes, setting.type);
                if (typeIndex < 0) typeIndex = 2;
                setting.type = settingTypes[EditorGUILayout.Popup("Tip", typeIndex, settingTypes)];
                setting.defaultValue = EditorGUILayout.TextField("Varsayılan", setting.defaultValue);
                if (setting.type == "int" || setting.type == "float")
                {
                    setting.min = EditorGUILayout.FloatField("Minimum", setting.min);
                    setting.max = EditorGUILayout.FloatField("Maksimum", setting.max);
                }
                if (setting.type == "dropdown") DrawDropdownOptions(setting);
                setting.restartRequired = EditorGUILayout.Toggle("Yeniden başlatma gerekli", setting.restartRequired);
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Ayar ekle")) project.settings.Add(new ModSettingDefinition { key = "newSetting", label = "Yeni ayar", type = "float", defaultValue = "1" });
            MarkDirty();
        }

        private void DrawDropdownOptions(ModSettingDefinition setting)
        {
            if (setting.options == null) setting.options = new List<string>();
            EditorGUILayout.LabelField("Seçenekler", EditorStyles.miniBoldLabel);
            for (int i = 0; i < setting.options.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                setting.options[i] = EditorGUILayout.TextField("Seçenek " + (i + 1), setting.options[i]);
                if (GUILayout.Button("Sil", GUILayout.Width(45))) { setting.options.RemoveAt(i); i--; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Dropdown seçeneği ekle")) setting.options.Add("newOption");
        }

        private void DrawAssets()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("3. Asset / görünüm / yeni içerik", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Kaynak asset, pakete koyacağınız yeni dosyadır. Hedef ID boşsa yeni içerik eklenir; hedef ID doluysa mevcut içerik o asset ile değiştirilir.", MessageType.Info);
            if (project.assets == null) project.assets = new List<ModCreatorAssetEntry>();
            for (int i = 0; i < project.assets.Count; i++)
            {
                ModCreatorAssetEntry entry = project.assets[i];
                if (entry == null) { project.assets[i] = new ModCreatorAssetEntry(); entry = project.assets[i]; }
                string assetPath = entry.asset == null ? "" : AssetDatabase.GetAssetPath(entry.asset);
                if (!MatchesSearch(entry.id, entry.replaceId, entry.kind.ToString(), assetPath)) continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("İçerik " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("Sil", GUILayout.Width(45))) { project.assets.RemoveAt(i); i--; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                entry.id = EditorGUILayout.TextField("İçerik ID", entry.id);
                entry.asset = EditorGUILayout.ObjectField("Asset", entry.asset, typeof(UnityEngine.Object), false);
                entry.kind = (ModCreatorAssetKind)EditorGUILayout.EnumPopup("Tür", entry.kind);
                if (entry.asset != null) EditorGUILayout.LabelField("Kaynak yolu", assetPath);
                entry.replaceId = EditorGUILayout.TextField("Hedef / değiştirilecek ID", entry.replaceId);
                EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(entry.replaceId) ? "İŞLEM: Yeni içerik ekle\nKULLANIM: ModAPI.GetModContent<T>(ID) veya registry üzerinden alınır." : "İŞLEM: Mevcut içeriği değiştir\nHEDEF: " + entry.replaceId, MessageType.None);
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Asset ekle")) project.assets.Add(new ModCreatorAssetEntry());
            MarkDirty();
        }

        private void DrawDialogues()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("4. Diyalog değişiklikleri", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Event ID, değiştirmek istediğiniz diyalog olayının hedefidir. Satırları değiştir seçiliyse eski satırlar silinir; değilse yeni satırlar sona eklenir.", MessageType.Info);
            if (project.dialogues == null) project.dialogues = new List<ModCreatorDialogueEntry>();
            for (int i = 0; i < project.dialogues.Count; i++)
            {
                ModCreatorDialogueEntry entry = project.dialogues[i];
                if (entry == null) { project.dialogues[i] = new ModCreatorDialogueEntry(); entry = project.dialogues[i]; }
                if (!MatchesSearch(entry.eventId, entry.speakerName)) continue;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Diyalog " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("Sil", GUILayout.Width(45))) { project.dialogues.RemoveAt(i); i--; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                entry.eventId = EditorGUILayout.TextField("Event ID", entry.eventId);
                EditorGUILayout.LabelField("Hedef olay", string.IsNullOrWhiteSpace(entry.eventId) ? "(belirlenmedi)" : entry.eventId);
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
            if (project.settings == null) project.settings = new List<ModSettingDefinition>();
            HashSet<string> settingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModSettingDefinition setting in project.settings)
            {
                if (setting == null || string.IsNullOrWhiteSpace(setting.key)) return "Ayar anahtarı boş bırakılamaz.";
                if (!settingKeys.Add(setting.key)) return "Ayar anahtarı tekrar ediyor: " + setting.key;
                if (setting.type == "dropdown" && (setting.options == null || setting.options.Count == 0)) return "Dropdown ayarında en az bir seçenek olmalı: " + setting.key;
                if ((setting.type == "int" || setting.type == "float") && setting.min > setting.max) return "Ayar minimumu maksimumdan büyük olamaz: " + setting.key;
                if ((setting.type == "int" || setting.type == "float") && !float.TryParse(setting.defaultValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                    return "Sayısal ayarın varsayılan değeri geçersiz: " + setting.key;
            }
            if (project.dialogues == null) project.dialogues = new List<ModCreatorDialogueEntry>();
            foreach (ModCreatorDialogueEntry dialogue in project.dialogues)
                if (dialogue == null || string.IsNullOrWhiteSpace(dialogue.eventId)) return "Diyalog Event ID boş bırakılamaz.";
            if (project.scenes == null) project.scenes = new List<ModCreatorSceneEntry>();
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
