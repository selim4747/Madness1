using System;
using UnityEngine;
using UnityEngine.UI;

namespace Madness.Mods
{
    public sealed class ModImportUI : MonoBehaviour
    {
        private Button importButton;
        private Text statusText;
        private bool busy;

        public static ModImportUI Create(Transform parent)
        {
            GameObject root = new GameObject("ModImportPanel", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            ModImportUI ui = root.AddComponent<ModImportUI>();
            ui.Build(parent);
            return ui;
        }

        // Ana menü akışını asla bozmaz: herhangi bir hata loglanır, menü normal çalışmaya devam eder.
        public static ModImportUI CreateIfPossible()
        {
            try
            {
                if (GameObject.Find("ModImportPanel") != null) return null;
                Canvas canvas = FindBestCanvas();
                return canvas == null ? null : Create(canvas.transform);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ModImportUI] Mod butonu oluşturulamadı, menü etkilenmedi: " + ex.Message);
                return null;
            }
        }

        private static Canvas FindBestCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas best = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas c = canvases[i];
                if (c == null || !c.isRootCanvas || !c.gameObject.activeInHierarchy) continue;
                if (best == null) { best = c; continue; }
                bool cOverlay = c.renderMode == RenderMode.ScreenSpaceOverlay;
                bool bOverlay = best.renderMode == RenderMode.ScreenSpaceOverlay;
                if (cOverlay && !bOverlay) best = c;
                else if (cOverlay == bOverlay && c.sortingOrder > best.sortingOrder) best = c;
            }
            return best;
        }

        // Unity 2022.2+ içinde "Arial.ttf" geçersiz (exception atar); "LegacyRuntime.ttf" kullanılır.
        private static Font LoadBuiltinFont()
        {
            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (Exception) { }
            try { return Font.CreateDynamicFontFromOSFont("Arial", 16); }
            catch (Exception) { }
            return null;
        }

        private void Build(Transform parent)
        {
            GameObject buttonObject = new GameObject("ModFileAddButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-32f, -32f);
            rect.sizeDelta = new Vector2(250f, 58f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.32f, 0.48f, 0.96f);
            importButton = buttonObject.GetComponent<Button>();
            importButton.onClick.AddListener(ChooseFile);
            AddLabel(buttonObject.transform, "MOD DOSYASI EKLE");

            GameObject statusObject = new GameObject("ModImportStatus", typeof(RectTransform));
            statusObject.transform.SetParent(parent, false);
            RectTransform statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(1f, 1f);
            statusRect.anchoredPosition = new Vector2(-32f, -98f);
            statusRect.sizeDelta = new Vector2(360f, 70f);
            statusText = statusObject.AddComponent<Text>();
            statusText.alignment = TextAnchor.UpperRight;
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.verticalOverflow = VerticalWrapMode.Overflow;
            statusText.font = LoadBuiltinFont();
            statusText.raycastTarget = false;
            statusText.fontSize = 18;
            statusText.color = Color.white;
            statusText.text = "Kurulu mod: " + ModRuntime.EnsureExists().LoadedManifests.Count;
        }

        private Text AddLabel(Transform parent, string message)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);
            Text label = labelObject.AddComponent<Text>();
            label.font = LoadBuiltinFont();
            label.raycastTarget = false;
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = message;
            return label;
        }

        private void ChooseFile()
        {
            Debug.Log("[ModImportUI] MOD DOSYASI EKLE butonuna basıldı. busy=" + busy);
            if (busy) return;
            busy = true;
            importButton.interactable = false;
            SetStatus("Mod dosyası seçiliyor...");
            ModFilePicker.Open(OnFileSelected);
        }

        private void OnFileSelected(string path, string pickerError)
        {
            if (!string.IsNullOrEmpty(pickerError))
            {
                Finish("Hata: " + pickerError, false);
                return;
            }
            if (string.IsNullOrEmpty(path))
            {
                Finish("Dosya seçilmedi.", true);
                return;
            }
            SetStatus("Mod doğrulanıyor ve aktarılıyor...");
            Debug.Log("[ModImportUI] İçe aktarma başlıyor: " + path);
            ModImportResult result = ModPackageImporter.ImportPackage(path);
            Debug.Log("[ModImportUI] Sonuç: " + result.Success + " — " + result.Message);
            Finish(result.Message, result.Success);
        }

        private void Finish(string message, bool success)
        {
            busy = false;
            if (importButton != null) importButton.interactable = true;
            SetStatus(message + "\nKurulu mod: " + ModRuntime.EnsureExists().LoadedManifests.Count);
            if (!success) Debug.LogWarning("[ModImportUI] " + message);
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
