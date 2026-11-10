using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Madness.Mods
{
    /// <summary>
    /// Mod panelini açar/kapatır. Panel açıkken: arkaya karartma perdesi koyar (arkadaki butonlara
    /// tıklanmaz), istersen seçtiğin nesneleri gizler, paneli en öne getirir. Kapanınca her şeyi geri koyar.
    /// Panelin KENDİSİNE değil, hep aktif bir nesneye (Canvas veya boş GameObject) eklenmelidir.
    /// Panel başka bir yolla (OnClick SetActive vb.) açılıp kapansa da otomatik senkron kalır.
    /// </summary>
    public sealed class ModPanelController : MonoBehaviour
    {
        [Header("Bağlantılar")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Davranış")]
        [SerializeField] private bool startHidden = true;
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private bool bringToFront = true;

        [Header("Arka plan perdesi")]
        [SerializeField] private bool blockBackground = true;
        [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private bool closeWhenDimmerClicked = true;

        [Header("Panel açıkken gizlenecek nesneler (örn. ana menü butonları/paneli)")]
        [SerializeField] private GameObject[] hideWhileOpen;

        private GameObject dimmer;
        private Transform topAncestor;
        private readonly List<GameObject> hidden = new List<GameObject>();
        private bool lastOpen;

        private void Start()
        {
            if (panel == null) { Debug.LogWarning("[ModPanelController] Panel atanmamış."); return; }
            if (panel == gameObject) Debug.LogWarning("[ModPanelController] Bu bileşen panelin kendisinde olmamalı; kapanınca çalışmayı bırakır.");
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            try { BuildDimmer(); } catch (System.Exception ex) { Debug.LogException(ex); }
            if (startHidden) panel.SetActive(false);
            lastOpen = panel.activeSelf;
            ApplyState(lastOpen);
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        private void OnDisable() { RestoreHidden(); }

        private void BuildDimmer()
        {
            if (!blockBackground || panel == null) return;
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Transform root = canvas.rootCanvas.transform;
            Transform t = panel.transform;
            while (t != null && t.parent != root) t = t.parent;
            if (t == null) return;
            topAncestor = t;

            dimmer = new GameObject("ModPanelDimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            dimmer.transform.SetParent(root, false);
            RectTransform r = dimmer.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            Image img = dimmer.GetComponent<Image>();
            img.color = dimColor;
            img.raycastTarget = true;
            Button b = dimmer.GetComponent<Button>();
            b.transition = Selectable.Transition.None;
            if (closeWhenDimmerClicked) b.onClick.AddListener(Close);
            dimmer.SetActive(false);
        }

        private void Update()
        {
            if (panel == null) return;
            bool open = panel.activeSelf;
            if (open != lastOpen)
            {
                lastOpen = open;
                try { ApplyState(open); } catch (System.Exception ex) { Debug.LogException(ex); }
            }
#if ENABLE_INPUT_SYSTEM
            if (closeOnEscape && open)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
            }
#endif
        }

        private void ApplyState(bool open)
        {
            if (open)
            {
                if (dimmer != null)
                {
                    dimmer.SetActive(true);
                    if (bringToFront) dimmer.transform.SetAsLastSibling();
                }
                if (bringToFront)
                {
                    if (topAncestor != null) topAncestor.SetAsLastSibling();
                    else panel.transform.SetAsLastSibling();
                }
                HideObjects();
            }
            else
            {
                if (dimmer != null) dimmer.SetActive(false);
                RestoreHidden();
            }
        }

        private void HideObjects()
        {
            RestoreHidden();
            if (hideWhileOpen == null) return;
            for (int i = 0; i < hideWhileOpen.Length; i++)
            {
                GameObject go = hideWhileOpen[i];
                if (go == null || !go.activeSelf) continue;
                // Panelin kendisini ya da onu içeren/içindeki nesneleri gizleme.
                if (panel.transform.IsChildOf(go.transform) || go.transform.IsChildOf(panel.transform)) continue;
                if (go == gameObject) continue;
                go.SetActive(false);
                hidden.Add(go);
            }
        }

        private void RestoreHidden()
        {
            for (int i = 0; i < hidden.Count; i++)
                if (hidden[i] != null) hidden[i].SetActive(true);
            hidden.Clear();
        }

        public void Open()   { if (panel != null) panel.SetActive(true); }
        public void Close()  { if (panel != null) panel.SetActive(false); }
        public void Toggle() { if (panel != null) panel.SetActive(!panel.activeSelf); }
    }
}
