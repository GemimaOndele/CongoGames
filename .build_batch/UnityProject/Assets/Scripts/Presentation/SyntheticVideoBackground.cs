using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Fond animé « type vidéo » sans fichier : caméra dédiée + bandes défilantes (par mode).
    /// Rendu dans une RenderTexture branchée sur le RawImage du thème.
    /// </summary>
    public sealed class SyntheticVideoBackground : MonoBehaviour
    {
        private GameObject rig;
        private Camera cam;
        private RenderTexture rt;
        private Material[] mats;
        private MeshRenderer[] rends;
        private Transform[] accentCubes;
        private Material[] cubeMats;
        private Coroutine animCo;
        private int synthLayer;

        public void Play(string modeId, RawImage target, int width, int height)
        {
            Stop();
            synthLayer = LayerMask.NameToLayer("CongoSynthBg");
            if (synthLayer < 0)
            {
                synthLayer = 0;
            }

            rt = new RenderTexture(Mathf.Max(512, width), Mathf.Max(288, height), 0, RenderTextureFormat.ARGB32);
            rt.Create();
            target.texture = rt;
            target.color = Color.white;
            target.uvRect = new Rect(0f, 0f, 1f, 1f);
            target.rectTransform.localScale = Vector3.one;

            Vector3 origin = new Vector3(2500f, 2500f, 2500f);
            rig = new GameObject("SyntheticVideoRig");
            rig.transform.SetPositionAndRotation(origin, Quaternion.identity);
            rig.transform.SetParent(transform, true);

            cam = rig.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BaseTint(modeId);
            cam.orthographic = true;
            cam.orthographicSize = 3.35f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = PresentationConfig.Tier == PresentationQualityTier.Cinematic ? 120f : 80f;
            cam.targetTexture = rt;
            cam.cullingMask = 1 << synthLayer;
            cam.depth = -100f;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.transform.localPosition = new Vector3(0f, 0f, -10f);
            cam.transform.LookAt(rig.transform.position);

            int seed = string.IsNullOrEmpty(modeId) ? 0 : Mathf.Abs(modeId.GetHashCode());
            float rich = PresentationConfig.SceneRichness;
            cam.orthographicSize = (2.85f + (seed % 11) * 0.11f) * (PresentationConfig.Tier == PresentationQualityTier.Cinematic ? 1.14f : 1f);
            int nStripes = Mathf.Clamp(Mathf.RoundToInt((5 + (seed % 6)) * rich), 4, 18);
            mats = new Material[nStripes];
            rends = new MeshRenderer[nStripes];
            Shader sh = Shader.Find("Unlit/Texture");
            if (sh == null)
            {
                sh = Shader.Find("Sprites/Default");
            }

            if (sh == null)
            {
                Debug.LogWarning("SyntheticVideoBackground: aucun shader Unlit/Texture — fond synthétique désactivé.");
                Stop();
                return;
            }

            for (int i = 0; i < nStripes; i++)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(quad.GetComponent<Collider>());
                quad.name = "Stripe" + i;
                quad.transform.SetParent(rig.transform, false);
                float y = (i - (nStripes - 1) * 0.5f) * 0.95f;
                float z = i * 0.06f;
                quad.transform.localPosition = new Vector3(0f, y, z);
                quad.transform.localRotation = Quaternion.Euler(0f, 0f, (seed + i * 17) % 11 * 3f);
                quad.transform.localScale = new Vector3(9f + i * 0.35f, 0.72f + (i % 3) * 0.1f, 1f);
                quad.layer = synthLayer;
                MeshRenderer mr = quad.GetComponent<MeshRenderer>();
                Texture2D tex = BuildStripeTexture(modeId, i, seed);
                Material m = new Material(sh);
                m.mainTexture = tex;
                mr.sharedMaterial = m;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mats[i] = m;
                rends[i] = mr;
            }

            Shader shColor = Shader.Find("Sprites/Default");
            if (shColor == null) shColor = sh;
            int nc = Mathf.Clamp(Mathf.RoundToInt(6f * rich), 4, 15);
            accentCubes = new Transform[nc];
            cubeMats = new Material[nc];
            for (int i = 0; i < nc; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(cube.GetComponent<Collider>());
                cube.name = "AccentCube" + i;
                cube.transform.SetParent(rig.transform, false);
                cube.layer = synthLayer;
                float sc = 0.18f + (i % 3) * 0.07f;
                cube.transform.localScale = new Vector3(sc, sc * 1.35f, sc * 0.85f);
                MeshRenderer cmr = cube.GetComponent<MeshRenderer>();
                Material cm = new Material(shColor);
                Color baseCol = HueShift(new Color(0.92f, 0.72f, 0.12f, 1f), (seed + i * 19) * 0.02f);
                if (i % 2 == 0) baseCol = HueShift(new Color(0.1f, 0.55f, 0.28f, 1f), (seed + i * 7) * 0.015f);
                cm.color = baseCol;
                cmr.sharedMaterial = cm;
                cmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                cmr.receiveShadows = false;
                accentCubes[i] = cube.transform;
                cubeMats[i] = cm;
            }

            animCo = StartCoroutine(CoScroll(seed));
        }

        public void Stop()
        {
            if (animCo != null)
            {
                StopCoroutine(animCo);
                animCo = null;
            }

            if (mats != null)
            {
                foreach (Material m in mats)
                {
                    if (m != null)
                    {
                        if (m.mainTexture != null)
                        {
                            Destroy(m.mainTexture);
                        }

                        Destroy(m);
                    }
                }
            }

            if (cubeMats != null)
            {
                foreach (Material m in cubeMats)
                {
                    if (m != null) Destroy(m);
                }
            }

            cubeMats = null;
            if (accentCubes != null)
            {
                foreach (Transform tr in accentCubes)
                {
                    if (tr != null) Destroy(tr.gameObject);
                }
            }

            accentCubes = null;
            mats = null;
            rends = null;

            if (cam != null)
            {
                Destroy(cam);
                cam = null;
            }

            if (rig != null)
            {
                Destroy(rig);
                rig = null;
            }

            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
                rt = null;
            }
        }

        private IEnumerator CoScroll(int seed)
        {
            float t = 0f;
            float[] speedsU = new float[mats.Length];
            float[] speedsV = new float[mats.Length];
            float seedF = seed * 0.001f;
            for (int i = 0; i < speedsU.Length; i++)
            {
                speedsU[i] = 0.045f + ((seed + i * 11) % 17) * 0.012f + seedF;
                speedsV[i] = 0.012f + ((seed + i * 7) % 11) * 0.008f + seedF * 0.5f;
            }

            Color baseC = cam != null ? cam.backgroundColor : Color.black;
            while (enabled && mats != null)
            {
                t += Time.deltaTime;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null)
                    {
                        mats[i].mainTextureOffset = new Vector2(t * speedsU[i], t * speedsV[i]);
                    }
                }

                for (int i = 0; i < rends.Length; i++)
                {
                    if (rends[i] != null)
                    {
                        float w = Mathf.Sin(t * (1.15f + i * 0.08f) + seed * 0.01f) * 2.2f;
                        rends[i].transform.localRotation = Quaternion.Euler(0f, 0f, (seed + i * 17) % 11 * 3f + w);
                    }
                }

                if (cam != null)
                {
                    float pulse = Mathf.Sin(t * 0.55f) * 0.5f + 0.5f;
                    cam.backgroundColor = Color.Lerp(baseC * 0.92f, baseC * 1.12f, pulse);
                }

                if (accentCubes != null)
                {
                    for (int i = 0; i < accentCubes.Length; i++)
                    {
                        if (accentCubes[i] == null) continue;
                        float ang = t * (0.75f + i * 0.11f) + i * 1.9f + seed * 0.01f;
                        float rad = 2.35f + i * 0.28f;
                        float bob = Mathf.Sin(t * 2.1f + i) * 0.22f;
                        accentCubes[i].localPosition = new Vector3(
                            Mathf.Cos(ang) * rad,
                            bob + Mathf.Sin(ang * 0.65f) * 0.35f,
                            Mathf.Sin(ang * 0.88f) * 0.55f + i * 0.05f);
                        accentCubes[i].localRotation = Quaternion.Euler(
                            t * 38f + i * 17f,
                            t * 52f + seed * 0.02f,
                            8f + i * 5f);
                    }
                }

                yield return null;
            }
        }

        private static Color BaseTint(string modeId)
        {
            int h = string.IsNullOrEmpty(modeId) ? 0 : Mathf.Abs(modeId.GetHashCode());
            int kind = (h % 7);
            float hue = (0.08f + kind * 0.12f + (h % 50) * 0.002f) % 1f;
            float sat = 0.22f + (kind * 0.05f);
            float val = 0.06f + ((h >> 4) % 8) * 0.035f;
            return Color.HSVToRGB(hue, Mathf.Clamp01(sat), Mathf.Clamp01(val));
        }

        private static Texture2D BuildStripeTexture(string modeId, int stripeIndex, int seed)
        {
            const int w = 256;
            const int h = 72;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            int h0 = string.IsNullOrEmpty(modeId) ? seed : Mathf.Abs((modeId + stripeIndex.ToString()).GetHashCode());
            Color c0 = HueShift(new Color(0.12f, 0.58f, 0.32f, 1f), (h0 % 100) * 0.003f);
            Color c1 = HueShift(new Color(0.98f, 0.82f, 0.18f, 1f), (h0 >> 3 % 100) * 0.003f);
            Color c2 = HueShift(new Color(0.88f, 0.14f, 0.16f, 1f), (h0 >> 6 % 100) * 0.003f);
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)Mathf.Max(1, h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)Mathf.Max(1, w - 1);
                    float bands = Mathf.Sin((u * 16f + v * 7f + stripeIndex * 0.5f + seed * 0.0001f) * Mathf.PI) * 0.5f + 0.5f;
                    Color a = Color.Lerp(c0, c1, Mathf.PingPong(u * 2.8f + v * 0.6f + bands * 0.15f, 1f));
                    Color b = Color.Lerp(a, c2, bands * 0.42f);
                    tex.SetPixel(x, y, b);
                }
            }

            tex.Apply(false, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static Color HueShift(Color c, float amount)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            h = (h + amount) % 1f;
            return Color.HSVToRGB(h, s, v);
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
