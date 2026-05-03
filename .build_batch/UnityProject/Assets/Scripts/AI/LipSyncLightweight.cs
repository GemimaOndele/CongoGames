using UnityEngine;

namespace CongoGames.AI
{
    [RequireComponent(typeof(AudioSource))]
    public class LipSyncLightweight : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer faceMesh;
        [SerializeField] private int mouthBlendShapeIndex = -1;
        [SerializeField] private float gain = 420f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (faceMesh == null || mouthBlendShapeIndex < 0 || source == null || !source.isPlaying)
            {
                return;
            }

            float[] spectrum = new float[64];
            source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
            float sum = 0f;
            for (int i = 0; i < spectrum.Length; i++)
            {
                sum += spectrum[i];
            }

            float w = Mathf.Clamp(sum * gain, 0f, 100f);
            faceMesh.SetBlendShapeWeight(mouthBlendShapeIndex, w);
        }
    }
}
