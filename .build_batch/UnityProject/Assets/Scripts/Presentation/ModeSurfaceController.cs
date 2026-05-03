using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Affiche le panneau UI correspondant au ModeId (quiz, mots mélangés, etc.).
    /// </summary>
    public class ModeSurfaceController : MonoBehaviour
    {
        public static ModeSurfaceController Instance { get; private set; }

        private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();
        private MiniGamePanelContent content;
        private Coroutine populateCo;

        public string CurrentModeId { get; private set; } = "quiz";

        private void Awake()
        {
            Instance = this;
            content = GetComponent<MiniGamePanelContent>();
        }

        private MiniGamePanelContent ResolvePanelContent()
        {
            if (content == null)
            {
                content = GetComponent<MiniGamePanelContent>();
            }

            return content;
        }

        public void Register(string modeId, GameObject root)
        {
            if (string.IsNullOrEmpty(modeId) || root == null) return;
            string id = modeId.Trim().ToLowerInvariant();
            panels[id] = root;
        }

        public void Apply(string modeId)
        {
            string id = string.IsNullOrEmpty(modeId) ? "quiz" : modeId.Trim().ToLowerInvariant();
            CurrentModeId = id;
            foreach (KeyValuePair<string, GameObject> kv in panels)
            {
                if (kv.Value != null)
                {
                    kv.Value.SetActive(kv.Key == id);
                }
            }

            ResolvePanelContent()?.Populate(id);
            if (populateCo != null)
            {
                StopCoroutine(populateCo);
            }

            populateCo = StartCoroutine(CoPopulateAfterLayout(id));
        }

        private IEnumerator CoPopulateAfterLayout(string modeId)
        {
            yield return null;
            yield return null;
            MiniGamePanelContent panel = ResolvePanelContent();
            if (panel != null && string.Equals(CurrentModeId, modeId, System.StringComparison.Ordinal))
            {
                panel.Populate(modeId);
                Canvas.ForceUpdateCanvases();
            }

            populateCo = null;
        }

        private void OnDestroy()
        {
            if (populateCo != null)
            {
                StopCoroutine(populateCo);
                populateCo = null;
            }

            if (Instance == this) Instance = null;
        }
    }
}
