using UnityEngine;

namespace Astra
{
    /// <summary>Procedural, readable suit animation for the generated astronaut rig.</summary>
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
        private Quaternion modelRestRotation;
        private Vector3 modelRestPosition;
        private float animationClock;
        private float blendedMove;
        private bool working;

        public void SetWorkingMotion(bool value) => working = value;

        private void Awake()
        {
            if (player == null) player = GetComponent<AstraPlayerController>();
            if (modelRoot != null)
            {
                modelRestPosition = modelRoot.localPosition;
                modelRestRotation = modelRoot.localRotation;
            }
            if (leftArmPivot != null) leftArmRest = leftArmPivot.localRotation;
            if (rightArmPivot != null) rightArmRest = rightArmPivot.localRotation;
            if (leftLegPivot != null) leftLegRest = leftLegPivot.localRotation;
            if (rightLegPivot != null) rightLegRest = rightLegPivot.localRotation;
        }

        private void Update()
        {
            if (player == null) return;

            float dt = Time.deltaTime;
            blendedMove = Mathf.MoveTowards(blendedMove, working ? 0f : player.MoveAmount, dt * 5f);
            float cadence = player.IsSprinting ? 10f : 7.2f;
            animationClock += dt * Mathf.Lerp(1.8f, cadence, blendedMove);
            float breath = Mathf.Sin(Time.time * 1.65f);

            if (working)
            {
                float toolStroke = Mathf.Sin(Time.time * 9f);
                SetRotation(leftArmPivot, leftArmRest * Quaternion.Euler(-64f + toolStroke * 17f, 0f, -14f), dt, 16f);
                SetRotation(rightArmPivot, rightArmRest * Quaternion.Euler(-76f - toolStroke * 17f, 0f, 16f), dt, 16f);
                SetRotation(leftLegPivot, leftLegRest, dt, 12f);
                SetRotation(rightLegPivot, rightLegRest, dt, 12f);
                SetModelPose(new Vector3(0f, Mathf.Sin(Time.time * 9f) * 0.018f, 0f), new Vector3(7f, 0f, 0f), dt, 14f);
                return;
            }

            float gait = Mathf.Sin(animationClock);
            bool airborne = !player.IsGrounded;
            float armSwing = gait * 38f * blendedMove;
            float legSwing = gait * 31f * blendedMove;
            float idleArm = breath * 4f * (1f - blendedMove);
            float airBlend = airborne ? 1f : 0f;

            SetRotation(leftArmPivot, leftArmRest * Quaternion.Euler(armSwing - idleArm - airBlend * 24f, 0f, -airBlend * 10f), dt, 15f);
            SetRotation(rightArmPivot, rightArmRest * Quaternion.Euler(-armSwing + idleArm - airBlend * 24f, 0f, airBlend * 10f), dt, 15f);
            SetRotation(leftLegPivot, leftLegRest * Quaternion.Euler(legSwing + airBlend * 28f, 0f, 0f), dt, 15f);
            SetRotation(rightLegPivot, rightLegRest * Quaternion.Euler(-legSwing + airBlend * 28f, 0f, 0f), dt, 15f);

            float stepBob = Mathf.Abs(Mathf.Sin(animationClock * 2f)) * blendedMove * 0.075f;
            float idleBreathing = (0.012f + breath * 0.012f) * (1f - blendedMove);
            float jumpFloat = airborne ? 0.045f : 0f;
            float lean = Mathf.Lerp(0f, player.IsSprinting ? 12f : 7f, blendedMove) + airBlend * -5f;
            float playfulSway = Mathf.Sin(animationClock) * blendedMove * 4f + breath * (1f - blendedMove) * 1.5f;
            SetModelPose(new Vector3(0f, stepBob + idleBreathing + jumpFloat, 0f),
                new Vector3(lean, playfulSway, -playfulSway * 0.45f), dt, 12f);
        }

        private void SetRotation(Transform target, Quaternion rotation, float dt, float speed)
        {
            if (target != null)
                target.localRotation = Quaternion.Slerp(target.localRotation, rotation, 1f - Mathf.Exp(-speed * dt));
        }

        private void SetModelPose(Vector3 positionOffset, Vector3 rotationOffset, float dt, float speed)
        {
            if (modelRoot == null) return;
            float blend = 1f - Mathf.Exp(-speed * dt);
            modelRoot.localPosition = Vector3.Lerp(modelRoot.localPosition, modelRestPosition + positionOffset, blend);
            modelRoot.localRotation = Quaternion.Slerp(modelRoot.localRotation,
                modelRestRotation * Quaternion.Euler(rotationOffset), blend);
        }
    }
}
