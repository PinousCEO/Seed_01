using UnityEngine;

namespace PuzzleBattle
{
    [CreateAssetMenu(menuName = "Puzzle Battle/Orb Motion Profile", fileName = "OrbMotionProfile")]
    public sealed class OrbMotionProfile : ScriptableObject
    {
        [SerializeField] private float swapDuration = 0.08f;
        [SerializeField] private float fallDurationPerCell = 0.07f;
        [SerializeField] private float popDuration = 0.18f;
        [SerializeField] private float selectedScale = 1.12f;
        [SerializeField] private float selectedPulseAmplitude = 0.03f;
        [SerializeField] private float selectedPulseSpeed = 10f;
        [SerializeField] private AnimationCurve moveCurve = null;
        [SerializeField] private AnimationCurve popCurve = null;

        public float SwapDuration => Mathf.Max(0.01f, swapDuration);
        public float FallDurationPerCell => Mathf.Max(0.02f, fallDurationPerCell);
        public float PopDuration => Mathf.Max(0.02f, popDuration);
        public float SelectedScale => Mathf.Max(1f, selectedScale);
        public float SelectedPulseAmplitude => Mathf.Max(0f, selectedPulseAmplitude);
        public float SelectedPulseSpeed => Mathf.Max(0f, selectedPulseSpeed);
        public AnimationCurve MoveCurve => moveCurve == null || moveCurve.length == 0
            ? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
            : moveCurve;
        public AnimationCurve PopCurve => popCurve == null || popCurve.length == 0
            ? AnimationCurve.EaseInOut(0f, 1f, 1f, 0f)
            : popCurve;

        public void SetAuthoringDefaults()
        {
            ApplyDefaults(HideFlags.None);
        }

        public void SetRuntimeDefaults()
        {
            ApplyDefaults(HideFlags.DontSave);
        }

        private void ApplyDefaults(HideFlags flags)
        {
            swapDuration = 0.08f;
            fallDurationPerCell = 0.07f;
            popDuration = 0.18f;
            selectedScale = 1.12f;
            selectedPulseAmplitude = 0.03f;
            selectedPulseSpeed = 10f;
            moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            popCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            hideFlags = flags;
        }
    }
}
