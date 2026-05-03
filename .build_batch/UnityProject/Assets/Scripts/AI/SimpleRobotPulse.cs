using UnityEngine;

namespace CongoGames.AI
{
    public class SimpleRobotPulse : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float idleScale = 1f;
        [SerializeField] private float talkScale = 1.08f;
        [SerializeField] private float smooth = 8f;

        private float target = 1f;
        private float bobBaseY;

        public void BindVisual(Transform root)
        {
            visualRoot = root != null ? root : transform;
            bobBaseY = visualRoot.localPosition.y;
        }

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            bobBaseY = visualRoot.localPosition.y;
        }

        private void OnEnable()
        {
            AIHostManager.OnSpeakingChanged += OnSpeaking;
        }

        private void OnDisable()
        {
            AIHostManager.OnSpeakingChanged -= OnSpeaking;
        }

        private void OnSpeaking(bool speaking)
        {
            target = speaking ? talkScale : idleScale;
        }

        private void Update()
        {
            if (visualRoot == null) return;
            float s = Mathf.Lerp(visualRoot.localScale.x, target, Time.deltaTime * smooth);
            visualRoot.localScale = new Vector3(s, s, s);
            float bob = Mathf.Sin(Time.time * 2.1f) * 4f;
            Vector3 lp = visualRoot.localPosition;
            lp.y = bobBaseY + bob;
            visualRoot.localPosition = lp;
        }
    }
}
