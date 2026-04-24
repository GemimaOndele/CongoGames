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

            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "VirtualShowStage_RT" };
            _rt.Create();

            _root = new GameObject("VirtualShowStage3D");

            GameObject camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(_root.transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.15f, -3.4f), Quaternion.Euler(2f, 0f, 0f));
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 50f;
            _cam.fieldOfView = 48f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.015f, 0.04f, 0.1f, 1f);
            _cam.cullingMask = 1 << StageLayer;
            _cam.targetTexture = _rt;
            _cam.allowHDR = true;
            UrpSetup(_cam);

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
            if (_key != null) _key.intensity = 0.88f + Mathf.Sin(_t * 0.42f) * 0.1f;
            if (_spin != null) _spin.Rotate(0f, 14f * Time.unscaledDeltaTime, 0f, Space.Self);
        }

        private void BuildGeometry(string modeId)
        {
            if (_root == null) return;
            ColorSet c = ColorSet.ForMode(modeId);
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetLayerRec(floor, StageLayer);
            floor.name = "Floor";
            floor.transform.SetParent(_root.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            floor.transform.localScale = new Vector3(6.5f, 0.1f, 3.2f);
            MatLit(floor, new Color(0.07f, 0.1f, 0.12f, 1f), Color.black, sh, 0.35f, 0.15f);

            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetLayerRec(plinth, StageLayer);
            plinth.name = "Plinth";
            plinth.transform.SetParent(_root.transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.08f, 0.02f);
            plinth.transform.localScale = new Vector3(1.5f, 0.1f, 0.32f);
            MatLit(plinth, new Color(0.04f, 0.04f, 0.04f, 1f), new Color(0.2f, 0.1f, 0.02f, 0.4f), sh, 0.22f, 0.4f);

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetLayerRec(frame, StageLayer);
            frame.name = "ScreenFrame";
            frame.transform.SetParent(_root.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0.85f, -0.1f);
            frame.transform.localScale = new Vector3(2.3f, 1.1f, 0.1f);
            MatLit(frame, new Color(0.01f, 0.01f, 0.02f, 1f), c.EmiFrame, sh, 0.2f, 0.2f);

            GameObject inner = GameObject.CreatePrimitive(PrimitiveType.Quad);
            SetLayerRec(inner, StageLayer);
            inner.name = "ScreenInner";
            inner.transform.SetParent(_root.transform, false);
            inner.transform.localPosition = new Vector3(0f, 0.85f, 0.02f);
            inner.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            inner.transform.localScale = new Vector3(2.0f, 0.9f, 1f);
            MatLit(inner, new Color(0.01f, 0.01f, 0.02f, 1f), new Color(0.02f, 0.05f, 0.08f, 0.15f), sh, 0.08f, 0.1f);

            MakePillar("PillarL", c.Left, new Vector3(-0.5f, 0.15f, 0.08f), sh);
            MakePillar("PillarM", c.Mid, new Vector3(0f, 0.15f, 0.08f), sh);
            MakePillar("PillarR", c.Right, new Vector3(0.5f, 0.15f, 0.08f), sh);

            GameObject spinGo = new GameObject("SpinTrophies");
            _spin = spinGo.transform;
            _spin.SetParent(_root.transform, false);
            _spin.localPosition = new Vector3(0.75f, 0, 0.15f);
            for (int i = 0; i < 2; i++)
            {
                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                SetLayerRec(orb, StageLayer);
                orb.name = "Orb" + i;
                orb.transform.SetParent(_spin, false);
                orb.transform.localPosition = new Vector3(0f, 0.1f, i * 0.12f);
                orb.transform.localScale = Vector3.one * 0.07f;
                MatLit(orb, new Color(0.4f, 0.3f, 0.1f, 1f), new Color(0.1f, 0.1f, 0.05f, 0.2f), sh, 0.45f, 0.2f);
            }

            _key = new GameObject("KeyLight").AddComponent<Light>();
            _key.transform.SetParent(_root.transform, false);
            _key.transform.SetLocalPositionAndRotation(new Vector3(-1.2f, 1.3f, 0.2f), Quaternion.Euler(22f, 18f, 0f));
            _key.type = LightType.Spot;
            _key.spotAngle = 55f;
            _key.innerSpotAngle = 8f;
            _key.intensity = 0.9f;
            _key.range = 14f;
            _key.cullingMask = 1 << StageLayer;
            _key.color = new Color(1f, 0.95f, 0.9f, 1f);

            Light fill = new GameObject("FillLight").AddComponent<Light>();
            fill.transform.SetParent(_root.transform, false);
            fill.transform.SetLocalPositionAndRotation(new Vector3(0.9f, 0.3f, 0.2f), Quaternion.Euler(8f, -25f, 0f));
            fill.type = LightType.Point;
            fill.intensity = 0.18f;
            fill.range = 4f;
            fill.cullingMask = 1 << StageLayer;
            fill.color = new Color(0.45f, 0.5f, 0.6f, 1f);
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

        private struct ColorSet
        {
            public Color Left;
            public Color Mid;
            public Color Right;
            public Color EmiFrame;

            public static ColorSet ForMode(string id)
            {
                string s = (id ?? "").ToLowerInvariant();
                if (s.Contains("blind") || s.Contains("chrono") || s.Contains("speed")) return new ColorSet
                {
                    Left = new Color(0.2f, 0.3f, 0.2f, 1f),
                    Mid = new Color(0.35f, 0.1f, 0.35f, 1f),
                    Right = new Color(0.1f, 0.2f, 0.45f, 1f),
                    EmiFrame = new Color(0.1f, 0.05f, 0.15f, 0.12f)
                };

                if (s.Contains("image")) return new ColorSet
                {
                    Left = new Color(0.25f, 0.15f, 0.08f, 1f),
                    Mid = new Color(0.85f, 0.45f, 0.08f, 1f),
                    Right = new Color(0.1f, 0.2f, 0.12f, 1f),
                    EmiFrame = new Color(0.2f, 0.1f, 0, 0.1f)
                };

                return new ColorSet
                {
                    Left = new Color(0f, 0.38f, 0.1f, 1f),
                    Mid = new Color(0.75f, 0.65f, 0.08f, 1f),
                    Right = new Color(0.7f, 0.1f, 0.1f, 1f),
                    EmiFrame = new Color(0.04f, 0.04f, 0.04f, 0.1f)
                };
            }
        }
    }
}
