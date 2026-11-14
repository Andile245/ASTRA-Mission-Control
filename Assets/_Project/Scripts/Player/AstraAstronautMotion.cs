using UnityEngine;

namespace Astra
{
    /// <summary>Lightweight procedural suit motion for the block-built junior astronaut.</summary>
    public sealed class AstraAstronautMotion : MonoBehaviour
    {
        [SerializeField] private AstraPlayerController player;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform leftArmPivot;
        [SerializeField] private Transform rightArmPivot;
        [SerializeField] private Transform leftLegPivot;
        [SerializeField] private Transform rightLegPivot;

        private Quaternion leftArmRest;
        private Quaternion rightArmRest;
        private Quaternion leftLegRest;
        private Quaternion rightLegRest;
        private Vector3 modelRestPosition;
        private float animationClock;
        private bool working;

        public void SetWorkingMotion(bool value) => working = value;

        private void Awake()
        {
            if (player == null) player = GetComponent<AstraPlayerController>();
            if (modelRoot != null) modelRestPosition = modelRoot.localPosition;
            if (leftArmPivot != null) leftArmRest = leftArmPivot.localRotation;
            if (rightArmPivot != null) rightArmRest = rightArmPivot.localRotation;
            if (leftLegPivot != null) leftLegRest = leftLegPivot.localRotation;
            if (rightLegPivot != null) rightLegRest = rightLegPivot.localRotation;
        }

        private void Update()
        {
            if (player == null) return;

            if (working)
            {
                float work = Mathf.Sin(Time.time * 7f) * 9f;
                if (leftArmPivot != null) leftArmPivot.localRotation = leftArmRest * Quaternion.Euler(-58f + work, 0f, -12f);
                if (rightArmPivot != null) rightArmPivot.localRotation = rightArmRest * Quaternion.Euler(-68f - work, 0f, 12f);
                if (leftLegPivot != null) leftLegPivot.localRotation = leftLegRest;
                if (rightLegPivot != null) rightLegPivot.localRotation = rightLegRest;
                if (modelRoot != null) modelRoot.localPosition = modelRestPosition + Vector3.up * Mathf.Sin(Time.time * 7f) * 0.012f;
                return;
            }

            float move = player.MoveAmount;
            bool airborne = !player.IsGrounded;
            float cadence = player.IsSprinting ? 8.4f : 5.8f;
            animationClock += Time.deltaTime * cadence;
            float gait = Mathf.Sin(animationClock) * move;
            float idle = Mathf.Sin(Time.time * 1.35f);
            float armSwing = airborne ? -16f : gait * 25f;
            float legSwing = airborne ? 24f : gait * 22f;

            if (leftArmPivot != null)
                leftArmPivot.localRotation = leftArmRest * Quaternion.Euler(armSwing - idle * 2f, 0f, 0f);
            if (rightArmPivot != null)
                rightArmPivot.localRotation = rightArmRest * Quaternion.Euler(-armSwing + idle * 2f, 0f, 0f);
            if (leftLegPivot != null)
                leftLegPivot.localRotation = leftLegRest * Quaternion.Euler(legSwing, 0f, 0f);
            if (rightLegPivot != null)
                rightLegPivot.localRotation = rightLegRest * Quaternion.Euler(-legSwing, 0f, 0f);

            if (modelRoot != null)
            {
                float stepBob = player.IsGrounded ? Mathf.Abs(Mathf.Sin(animationClock * 2f)) * move * 0.035f : 0.02f;
                float breathing = Mathf.Sin(Time.time * 1.2f) * 0.008f;
                modelRoot.localPosition = modelRestPosition + Vector3.up * (stepBob + breathing);
            }
        }
    }
}
