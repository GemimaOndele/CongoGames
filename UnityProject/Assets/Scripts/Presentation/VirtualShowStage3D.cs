using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Plateau TV 3D (URP) → <see cref="RenderTexture"/> sur le <see cref="RawImage"/> de fond.
    /// Primitives + tricolore + lumières (aucun .fbx). Désactiver : <c>PlayerPrefs CongoUseVirtual3D=0</c>.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public class VirtualShowStage3D : MonoBehaviour
    {
        private const int StageLayer = 8;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        private RawImage _target;
        private GameObject _root;
        private Camera _cam;
        private Vector3 _camPosBase;
        private Quaternion _camRotBase;
        private RenderTexture _rt;
        private Light _key;
        private Transform _spin;
        private float _t;

        private void Awake()
        {
            _target = GetComponent<RawImage>();
        }

        private void OnDestroy()
        {
            Teardown();
        }

        public void Teardown()
        {
            if (_cam != null)
            {
                _cam.targetTexture = null;
            }

            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }

            if (_root != null)
            {
                Destroy(_root);
                _root = null;
            }

            _cam = null;
            _key = null;
            _spin = null;
        }

        public void RebuildForMode(string modeId, RawImage raw = null)
        {
            if (raw != null) _target = raw;
            if (_target == null) return;

            Teardown();

            int w = PresentationConfig.VirtualStageWidth;
            int h = PresentationConfig.VirtualStageHeight;
            if (w < 256) w = 1280;
            if (h < 256) h = 720;

            int depthBits = 24;
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL + URP : RT HDR / profondeur 24 peut donner un rendu noir ; sans ombres, plus fiable.
            depthBits = 16;
#endif
            _rt = new RenderTexture(w, h, depthBits, RenderTextureFormat.ARGB32) { name = "VirtualShowStage_RT" };
            _rt.Create();

            _root = new GameObject("VirtualShowStage3D");

            GameObject camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(_root.transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.12f, -3.55f), Quaternion.Euler(3.5f, 0f, 0f));
            _camPosBase = _cam.transform.localPosition;
            _camRotBase = _cam.transform.localRotation;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 58f;
            _cam.fieldOfView = 46f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.008f, 0.02f, 0.06f, 1f);
            _cam.cullingMask = 1 << StageLayer;
            _cam.targetTexture = _rt;
            _cam.allowHDR = true;
            _cam.depth = -4f;
            UrpSetup(_cam);
#if UNITY_WEBGL && !UNITY_EDITOR
            _cam.allowHDR = false;
            UadpWebGlSafeCamera(_cam);
#endif

            BuildGeometry(modeId);

            _target.texture = _rt;
            _target.color = Color.white;
            if (_target.rectTransform != null) _target.rectTransform.localScale = Vector3.one;
            _target.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        private void Update()
        {
            if (_root == null) return;
            _t += Time.unscaledDeltaTime;
            if (_key != null) _key.intensity = 0.9f + Mathf.Sin(_t * 0.42f) * 0.12f;
            if (_spin != null) _spin.Rotate(0f, 16f * Time.unscaledDeltaTime, 0f, Space.Self);
            if (_cam != null)
            {
                float r = PresentationConfig.SceneRichness;
                float wob = 0.08f * Mathf.Min(r, 1.35f);
                float sway = Mathf.Sin(_t * 0.21f) * wob;
                float rise = Mathf.Sin(_t * 0.35f) * 0.03f;
                float yaw = Mathf.Sin(_t * 0.17f) * 0.6f;
                _cam.transform.localPosition = _camPosBase + new Vector3(sway, rise, 0f);
                _cam.transform.localRotation = _camRotBase * Quaternion.Euler(0f, yaw, 0f);
            }
        }

        private void BuildGeometry(string modeId)
        {
            if (_root == null) return;
            ColorSet c = ColorSet.ForMode(modeId);
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            float rich = PresentationConfig.SceneRichness;
            PresentationQualityTier q = PresentationConfig.Tier;
            int seed = string.IsNullOrEmpty(modeId) ? 0 : Mathf.Abs(modeId.GetHashCode());

            // Sol — large « plateau esport »
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetLayerRec(floor, StageLayer);
            floor.name = "Floor";
            floor.transform.SetParent(_root.transform, false);
            float floorW = 7.2f + 0.35f * rich;
            float floorD = 3.5f + 0.2f * rich;
            floor.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            floor.transform.localScale = new Vector3(floorW, 0.1f, floorD);
            MatLit(floor, new Color(0.05f, 0.08f, 0.1f, 1f), new Color(0.02f, 0.04f, 0.06f, 0.08f), sh, 0.5f, 0.32f);
            RigidbodySuppress(floor);

            // Grille de scène (lignes de LED au sol, procédurales)
            int nLed = q == PresentationQualityTier.Compact ? 6 : q == PresentationQualityTier.Standard ? 10 : 16;
            nLed = Mathf.Clamp(Mathf.RoundToInt(nLed * (0.9f + rich * 0.1f)), 4, 22);
            for (int i = 0; i < nLed; i++)
            {
                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                SetLayerRec(seg, StageLayer);
                seg.name = "FloorLed" + i;
                seg.transform.SetParent(_root.transform, false);
                float t01 = nLed <= 1 ? 0.5f : i / Mathf.Max(1f, nLed - 1);
                float x = Mathf.Lerp(-floorW * 0.4f, floorW * 0.4f, t01);
                seg.transform.localPosition = new Vector3(x, 0.055f, 0.45f);
                seg.transform.localScale = new Vector3(0.08f, 0.02f, 0.65f);
                Color led = c.LedForStrip(t01);
                MatLit(seg, led * 0.35f, led * 0.55f, sh, 0.55f, 0.25f);
                RigidbodySuppress(seg);
            }

            // Fond d’arène (profondeur, ambiance « salle sombre »)
            if (q != PresentationQualityTier.Compact)
            {
                GameObject back = GameObject.CreatePrimitive(PrimitiveType.Quad);
                SetLayerRec(back, StageLayer);
                back.name = "ArenaBack";
                back.transform.SetParent(_root.transform, false);
                back.transform.localPosition = new Vector3(0f, 0.7f, 0.7f);
                back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                back.transform.localScale = new Vector3(5.5f, 2.2f, 1f);
                MatLit(back, new Color(0.02f, 0.03f, 0.05f, 1f), c.ArenaGlow * 0.15f, sh, 0.12f, 0.2f);
                RigidbodySuppress(back);
            }

            // Gradins suggérés (cubes discrets) — côté public
            int risers = q == PresentationQualityTier.Cinematic ? 3 : 2;
            for (int r = 0; r < risers; r++)
            {
                for (int s = 0; s < 5; s++)
                {
                    float z = 0.55f + r * 0.18f;
                    float xo = (s - 2) * 0.85f;
                    if (q == PresentationQualityTier.Compact && (s + r) % 2 == 0) continue;
                    GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    SetLayerRec(seat, StageLayer);
                    seat.name = $"Riser_r{r}_s{s}";
                    seat.transform.SetParent(_root.transform, false);
                    seat.transform.localPosition = new Vector3(xo, 0.1f + r * 0.04f, z);
                    seat.transform.localScale = new Vector3(0.35f, 0.06f, 0.1f);
                    float hue = 0.35f + 0.15f * Mathf.Sin(seed * 0.01f + s + r * 2f);
                    MatLit(seat, new Color(0.04f, 0.04f, 0.05f, 1f) * (0.7f + hue * 0.15f), c.SeatGlow * (0.1f + 0.05f * (s % 3)), sh, 0.25f, 0.15f);
                    RigidbodySuppress(seat);
                }
            }

            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetLayerRec(plinth, StageLayer);
            plinth.name = "Plinth";
            plinth.transform.SetParent(_root.transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.08f, 0.02f);
            plinth.transform.localScale = new Vector3(1.6f, 0.1f, 0.35f);
            MatLit(plinth, new Color(0.04f, 0.04f, 0.04f, 1f), new Color(0.2f, 0.1f, 0.02f, 0.4f), sh, 0.22f, 0.4f);
            RigidbodySuppress(plinth);

            // Écran + cadre (jumbotron)
            float screenBoost = 1f + 0.05f * (rich - 1f);
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetLayerRec(frame, StageLayer);
            frame.name = "ScreenFrame";
            frame.transform.SetParent(_root.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0.9f, -0.1f);
            frame.transform.localScale = new Vector3(2.45f * screenBoost, 1.2f * screenBoost, 0.1f);
            MatLit(frame, new Color(0.01f, 0.01f, 0.02f, 1f), c.EmiFrame, sh, 0.2f, 0.2f);
            RigidbodySuppress(frame);

            GameObject inner = GameObject.CreatePrimitive(PrimitiveType.Quad);
            SetLayerRec(inner, StageLayer);
            inner.name = "ScreenInner";
            inner.transform.SetParent(_root.transform, false);
            inner.transform.localPosition = new Vector3(0f, 0.9f, 0.04f);
            inner.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            inner.transform.localScale = new Vector3(2.15f * screenBoost, 0.95f * screenBoost, 1f);
            MatLit(inner, c.ScreenTint, c.ScreenEmi, sh, 0.2f, 0.1f);
            RigidbodySuppress(inner);

            MakePillar("PillarL", c.Left, new Vector3(-0.55f, 0.16f, 0.1f), sh);
            MakePillar("PillarM", c.Mid, new Vector3(0f, 0.16f, 0.1f), sh);
            MakePillar("PillarR", c.Right, new Vector3(0.55f, 0.16f, 0.1f), sh);

            // Rubans LED plafond (effet show TV)
            if (q == PresentationQualityTier.Cinematic)
            {
                for (int b = 0; b < 3; b++)
                {
                    GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    SetLayerRec(beam, StageLayer);
                    beam.name = "CeilLed" + b;
                    beam.transform.SetParent(_root.transform, false);
                    float bx = (b - 1) * 1.4f;
                    beam.transform.localPosition = new Vector3(bx, 1.25f, 0.0f);
                    beam.transform.localScale = new Vector3(0.9f, 0.04f, 0.04f);
                    Color ce = b == 1 ? c.Mid : c.Left;
                    MatLit(beam, ce * 0.2f, ce * 0.8f, sh, 0.5f, 0.1f);
                    RigidbodySuppress(beam);
                }
            }

            GameObject spinGo = new GameObject("SpinTrophies");
            _spin = spinGo.transform;
            _spin.SetParent(_root.transform, false);
            _spin.localPosition = new Vector3(0.82f, 0, 0.18f);
            int nOrbs = 2 + Mathf.Clamp(Mathf.FloorToInt(rich * 0.5f), 0, 3);
            for (int i = 0; i < nOrbs; i++)
            {
                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                SetLayerRec(orb, StageLayer);
                orb.name = "Orb" + i;
                orb.transform.SetParent(_spin, false);
                orb.transform.localPosition = new Vector3(0f, 0.1f, i * 0.12f);
                orb.transform.localScale = Vector3.one * (0.07f + i * 0.01f);
                MatLit(orb, new Color(0.4f, 0.3f, 0.1f, 1f), new Color(0.2f, 0.2f, 0.1f, 0.5f), sh, 0.5f, 0.3f);
                RigidbodySuppress(orb);
            }

            _key = new GameObject("KeyLight").AddComponent<Light>();
            _key.transform.SetParent(_root.transform, false);
            _key.transform.SetLocalPositionAndRotation(new Vector3(-1.25f, 1.4f, 0.1f), Quaternion.Euler(24f, 20f, 0f));
            _key.type = LightType.Spot;
            _key.spotAngle = 58f;
            _key.innerSpotAngle = 10f;
            _key.intensity = 0.95f;
            _key.range = 16f;
            _key.cullingMask = 1 << StageLayer;
            _key.color = new Color(1f, 0.96f, 0.9f, 1f);
            _key.shadows = LightShadows.Soft;
            _key.shadowStrength = 0.55f;

            Light fill = new GameObject("FillLight").AddComponent<Light>();
            fill.transform.SetParent(_root.transform, false);
            fill.transform.SetLocalPositionAndRotation(new Vector3(0.95f, 0.35f, 0.25f), Quaternion.Euler(8f, -25f, 0f));
            fill.type = LightType.Point;
            fill.intensity = 0.22f;
            fill.range = 4.2f;
            fill.cullingMask = 1 << StageLayer;
            fill.color = new Color(0.45f, 0.5f, 0.6f, 1f);

            // Rampe côté jardin (lumière d’ambiance)
            Light rim = new GameObject("RimLight").AddComponent<Light>();
            rim.transform.SetParent(_root.transform, false);
            rim.transform.SetLocalPositionAndRotation(new Vector3(1.6f, 0.5f, -0.3f), Quaternion.Euler(5f, -50f, 0f));
            rim.type = LightType.Spot;
            rim.spotAngle = 40f;
            rim.innerSpotAngle = 5f;
            rim.intensity = 0.35f * Mathf.Clamp(rich, 0.7f, 1.5f);
            rim.range = 10f;
            rim.cullingMask = 1 << StageLayer;
            rim.color = c.Rim;

            if (q == PresentationQualityTier.Cinematic)
            {
                for (int p = 0; p < 2; p++)
                {
                    Light pc = new GameObject("PitLight" + p).AddComponent<Light>();
                    pc.transform.SetParent(_root.transform, false);
                    float px = p == 0 ? -1.3f : 1.3f;
                    pc.transform.SetLocalPositionAndRotation(new Vector3(px, 0.25f, 0.35f), Quaternion.Euler(20f, p == 0 ? 20f : -20f, 0f));
                    pc.type = LightType.Point;
                    pc.intensity = 0.12f;
                    pc.range = 2.2f;
                    pc.cullingMask = 1 << StageLayer;
                    pc.color = Color.Lerp(c.Left, c.Right, p);
                }
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (_key != null) _key.shadows = LightShadows.None;
#endif
        }

        private void MakePillar(string name, Color c, Vector3 pos, Shader sh)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            g.name = name;
            g.transform.SetParent(_root.transform, false);
            g.transform.localPosition = pos;
            g.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
            SetLayerRec(g, StageLayer);
            MatLit(g, c * 0.45f, c, sh, 0.2f, 0.1f);
            RigidbodySuppress(g);
        }

        /// <summary>Évite toute interaction physique / raycasts sur le décor scène (primitives 3D).</summary>
        private static void RigidbodySuppress(GameObject go)
        {
            if (go == null) return;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private static void SetLayerRec(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRec(t.gameObject, layer);
        }

        private static void MatLit(GameObject go, Color albedo, Color emi, Shader sh, float sm, float met)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r == null) return;
            Material m = sh != null ? new Material(sh) : new Material(Shader.Find("Standard"));
            m.SetColor(BaseColor, albedo);
            m.SetFloat("_Smoothness", sm);
            m.SetFloat("_Metallic", met);
            if (m.HasProperty(Emission))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor(Emission, emi);
            }

            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
        }

        private static void UrpSetup(Camera c)
        {
            if (c == null) return;
            UniversalAdditionalCameraData d = c.GetUniversalAdditionalCameraData();
            d.renderType = CameraRenderType.Base;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static void UadpWebGlSafeCamera(Camera c)
        {
            if (c == null) return;
            UniversalAdditionalCameraData d = c.GetUniversalAdditionalCameraData();
            d.renderPostProcessing = false;
        }
#endif

        private struct ColorSet
        {
            public Color Left;
            public Color Mid;
            public Color Right;
            public Color EmiFrame;
            public Color ArenaGlow;
            public Color SeatGlow;
            public Color ScreenTint;
            public Color ScreenEmi;
            public Color Rim;

            public Color LedForStrip(float t01)
            {
                Color led = Color.Lerp(Color.Lerp(Left, Mid, t01), Right, t01 * 0.22f);
                led.r = Mathf.Min(led.r, 0.5f);
                led.g = Mathf.Max(led.g, 0.12f);
                led.b = Mathf.Min(Mathf.Max(led.b, 0.18f), 0.55f);
                return led;
            }

            public static ColorSet ForMode(string id)
            {
                string s = (id ?? "").Trim().ToLowerInvariant();
                // Une palette distincte par mode (plateau 3D « show TV » animé en temps réel).
                switch (s)
                {
                    case "speed-chrono": return ChronoStyle();
                    case "image-guess": return ImageStyle();
                    case "blind-test": return BlindStyle();
                    case "mystery-word": return MysteryStyle();
                    case "memory": return MemoryStyle();
                    case "semantic": return SemanticStyle();
                    case "word-scramble": return WordScrambleStyle();
                    case "crossword-lite": return CrosswordStyle();
                    case "quiz": return QuizCongoStyle();
                }

                if (s.Contains("blind") || s.Contains("chrono") || s.Contains("speed")) return ChronoStyle();
                if (s.Contains("image")) return ImageStyle();
                return QuizCongoStyle();
            }

            private static ColorSet ChronoStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0.2f, 0.3f, 0.2f, 1f),
                    Mid = new Color(0.35f, 0.1f, 0.35f, 1f),
                    Right = new Color(0.1f, 0.2f, 0.45f, 1f),
                    EmiFrame = new Color(0.1f, 0.05f, 0.15f, 0.12f),
                    ArenaGlow = new Color(0.12f, 0.04f, 0.2f, 0.1f),
                    SeatGlow = new Color(0.15f, 0.2f, 0.4f, 0.2f),
                    ScreenTint = new Color(0.02f, 0.04f, 0.1f, 1f),
                    ScreenEmi = new Color(0.08f, 0.12f, 0.35f, 0.35f),
                    Rim = new Color(0.4f, 0.6f, 0.95f, 1f)
                };
            }

            private static ColorSet ImageStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0.25f, 0.15f, 0.08f, 1f),
                    Mid = new Color(0.85f, 0.45f, 0.08f, 1f),
                    Right = new Color(0.1f, 0.2f, 0.12f, 1f),
                    EmiFrame = new Color(0.2f, 0.1f, 0, 0.1f),
                    ArenaGlow = new Color(0.15f, 0.08f, 0.02f, 0.12f),
                    SeatGlow = new Color(0.3f, 0.2f, 0.05f, 0.2f),
                    ScreenTint = new Color(0.08f, 0.04f, 0.02f, 1f),
                    ScreenEmi = new Color(0.3f, 0.2f, 0.04f, 0.2f),
                    Rim = new Color(0.95f, 0.55f, 0.15f, 1f)
                };
            }

            private static ColorSet BlindStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0.2f, 0.1f, 0.3f, 1f),
                    Mid = new Color(0.6f, 0.35f, 0.1f, 1f),
                    Right = new Color(0.12f, 0.2f, 0.4f, 1f),
                    EmiFrame = new Color(0.15f, 0.08f, 0.2f, 0.14f),
                    ArenaGlow = new Color(0.1f, 0.05f, 0.15f, 0.12f),
                    SeatGlow = new Color(0.2f, 0.3f, 0.4f, 0.2f),
                    ScreenTint = new Color(0.04f, 0.02f, 0.08f, 1f),
                    ScreenEmi = new Color(0.2f, 0.1f, 0.3f, 0.3f),
                    Rim = new Color(0.8f, 0.4f, 0.2f, 1f)
                };
            }

            private static ColorSet MysteryStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0.1f, 0.12f, 0.28f, 1f),
                    Mid = new Color(0.45f, 0.4f, 0.1f, 1f),
                    Right = new Color(0.28f, 0.1f, 0.2f, 1f),
                    EmiFrame = new Color(0.05f, 0.1f, 0.2f, 0.12f),
                    ArenaGlow = new Color(0.04f, 0.04f, 0.1f, 0.1f),
                    SeatGlow = new Color(0.1f, 0.1f, 0.2f, 0.2f),
                    ScreenTint = new Color(0.02f, 0.03f, 0.08f, 1f),
                    ScreenEmi = new Color(0.1f, 0.1f, 0.2f, 0.3f),
                    Rim = new Color(0.5f, 0.45f, 0.9f, 1f)
                };
            }

            private static ColorSet MemoryStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0.05f, 0.2f, 0.1f, 1f),
                    Mid = new Color(0.15f, 0.45f, 0.18f, 1f),
                    Right = new Color(0.35f, 0.12f, 0.1f, 1f),
                    EmiFrame = new Color(0.04f, 0.1f, 0.05f, 0.1f),
                    ArenaGlow = new Color(0.02f, 0.08f, 0.04f, 0.1f),
                    SeatGlow = new Color(0.1f, 0.2f, 0.1f, 0.2f),
                    ScreenTint = new Color(0.02f, 0.05f, 0.02f, 1f),
                    ScreenEmi = new Color(0.05f, 0.2f, 0.1f, 0.25f),
                    Rim = new Color(0.3f, 0.85f, 0.35f, 1f)
                };
            }

            private static ColorSet SemanticStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0.1f, 0.2f, 0.4f, 1f),
                    Mid = new Color(0.12f, 0.5f, 0.45f, 1f),
                    Right = new Color(0.05f, 0.15f, 0.2f, 1f),
                    EmiFrame = new Color(0.05f, 0.1f, 0.12f, 0.1f),
                    ArenaGlow = new Color(0.02f, 0.08f, 0.1f, 0.1f),
                    SeatGlow = new Color(0.1f, 0.2f, 0.25f, 0.2f),
                    ScreenTint = new Color(0.02f, 0.05f, 0.08f, 1f),
                    ScreenEmi = new Color(0.05f, 0.2f, 0.2f, 0.3f),
                    Rim = new Color(0.3f, 0.7f, 0.9f, 1f)
                };
            }

            private static ColorSet WordScrambleStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0.2f, 0.1f, 0.35f, 1f),
                    Mid = new Color(0.45f, 0.2f, 0.5f, 1f),
                    Right = new Color(0.1f, 0.3f, 0.4f, 1f),
                    EmiFrame = new Color(0.12f, 0.05f, 0.15f, 0.12f),
                    ArenaGlow = new Color(0.08f, 0.04f, 0.12f, 0.1f),
                    SeatGlow = new Color(0.2f, 0.1f, 0.2f, 0.2f),
                    ScreenTint = new Color(0.05f, 0.02f, 0.06f, 1f),
                    ScreenEmi = new Color(0.2f, 0.1f, 0.25f, 0.3f),
                    Rim = new Color(0.7f, 0.35f, 0.9f, 1f)
                };
            }

            private static ColorSet CrosswordStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0.2f, 0.18f, 0.12f, 1f),
                    Mid = new Color(0.5f, 0.45f, 0.25f, 1f),
                    Right = new Color(0.15f, 0.2f, 0.18f, 1f),
                    EmiFrame = new Color(0.1f, 0.08f, 0.04f, 0.1f),
                    ArenaGlow = new Color(0.12f, 0.1f, 0.05f, 0.1f),
                    SeatGlow = new Color(0.2f, 0.2f, 0.1f, 0.2f),
                    ScreenTint = new Color(0.05f, 0.04f, 0.02f, 1f),
                    ScreenEmi = new Color(0.2f, 0.2f, 0.1f, 0.2f),
                    Rim = new Color(0.6f, 0.5f, 0.2f, 1f)
                };
            }

            private static ColorSet QuizCongoStyle()
            {
                return new ColorSet
                {
                    Left = new Color(0f, 0.38f, 0.1f, 1f),
                    Mid = new Color(0.75f, 0.65f, 0.08f, 1f),
                    Right = new Color(0.7f, 0.1f, 0.1f, 1f),
                    EmiFrame = new Color(0.04f, 0.04f, 0.04f, 0.1f),
                    ArenaGlow = new Color(0.02f, 0.08f, 0.04f, 0.1f),
                    SeatGlow = new Color(0.1f, 0.2f, 0.1f, 0.2f),
                    ScreenTint = new Color(0.02f, 0.04f, 0.02f, 1f),
                    ScreenEmi = new Color(0.1f, 0.2f, 0.1f, 0.2f),
                    Rim = new Color(0.3f, 0.7f, 0.3f, 1f)
                };
            }
        }
    }
}
