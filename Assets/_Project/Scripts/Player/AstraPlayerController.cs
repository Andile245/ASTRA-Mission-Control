using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Astra
{
    [RequireComponent(typeof(CharacterController))]
    public class AstraPlayerController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintMultiplier = 1.55f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float cameraDistance = 7.2f;
        [SerializeField] private float cameraSmoothTime = 0.08f;
        [SerializeField] private float cameraCollisionRadius = 0.22f;

        private CharacterController controller;
        private Camera followCamera;
        private AstraGameManager game;
        private float yaw;
        private float pitch = 18f;
        private float verticalSpeed;
        private bool rightDragging;
        private float moveAmount;
        private bool sprinting;
        private Vector3 cameraVelocity;
        private Vector3 clickMoveTarget;
        private bool hasClickMoveTarget;

        public float MoveAmount => moveAmount;
        public bool IsSprinting => sprinting;
        public bool IsGrounded => controller != null && controller.isGrounded;
        public float VerticalSpeed => verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            followCamera = Camera.main;
            game = FindFirstObjectByType<AstraGameManager>();
            if (cameraTarget == null) cameraTarget = transform;
            if (followCamera != null)
            {
                Vector3 euler = followCamera.transform.eulerAngles;
                yaw = euler.y;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame && game != null)
                game.TogglePause();

            if (game == null || !game.CanControlPlayer)
            {
                moveAmount = 0f;
                sprinting = false;
                return;
            }

            rightDragging = Mouse.current.rightButton.isPressed;
            if (rightDragging)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, 8f, 45f);
                if (delta.sqrMagnitude > 1f) TutorialManager.Instance?.RecordLook(delta.magnitude);
            }

            Vector2 move = Vector2.zero;
            PlayerSettingsManager settings = PlayerSettingsManager.Instance;
            Key forward = settings != null ? settings.GetKey(AstraBinding.MoveForward) : Key.W;
            Key back = settings != null ? settings.GetKey(AstraBinding.MoveBack) : Key.S;
            Key left = settings != null ? settings.GetKey(AstraBinding.MoveLeft) : Key.A;
            Key right = settings != null ? settings.GetKey(AstraBinding.MoveRight) : Key.D;
            if (Keyboard.current[forward].isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
            if (Keyboard.current[back].isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
            if (Keyboard.current[right].isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;
            if (Keyboard.current[left].isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;

            if (settings != null && settings.ClickToMove && Mouse.current.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                Camera clickCamera = followCamera != null ? followCamera : Camera.main;
                if (clickCamera != null)
                {
                    Ray clickRay = clickCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (Physics.Raycast(clickRay, out RaycastHit groundHit, 240f)) { clickMoveTarget = groundHit.point; hasClickMoveTarget = true; }
                }
            }
            if (hasClickMoveTarget && move.sqrMagnitude < 0.001f)
            {
                Vector3 toClick = clickMoveTarget - transform.position;
                toClick.y = 0f;
                if (toClick.magnitude < 0.45f) hasClickMoveTarget = false;
                else move = Vector2.ClampMagnitude(new Vector2(toClick.x, toClick.z), 1f);
            }
            move = Vector2.ClampMagnitude(move, 1f);
            moveAmount = move.magnitude;

            Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
            Vector3 direction = facing * new Vector3(move.x, 0f, move.y);
            sprinting = moveAmount > 0.05f && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            float speed = walkSpeed * (sprinting ? sprintMultiplier : 1f);

            if (controller.isGrounded)
            {
                verticalSpeed = -1.5f;
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    verticalSpeed = Mathf.Sqrt(jumpHeight * 2f * LunarGravity);
            }
            else verticalSpeed -= LunarGravity * Time.deltaTime;

            Vector3 previousPosition = transform.position;
            controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            TutorialManager.Instance?.RecordMovement(Vector3.Distance(previousPosition, transform.position));
            if (direction.sqrMagnitude > 0.02f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);

            Key interact = settings != null ? settings.GetKey(AstraBinding.Interact) : Key.E;
            if (Keyboard.current[interact].wasPressedThisFrame)
                game.TryInteract(transform.position);

        }

        private const float LunarGravity = 1.62f;

        private void LateUpdate()
        {
            if (game != null && game.CanControlPlayer) UpdateCamera();
        }

        private void UpdateCamera()
        {
            if (followCamera == null) return;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = cameraTarget.position + Vector3.up * 1.1f;
            Vector3 offset = rotation * new Vector3(0f, 0f, -cameraDistance);
            Vector3 desiredPosition = focus + offset;
            Vector3 travel = desiredPosition - focus;
            float travelDistance = travel.magnitude;
            if (travelDistance > 0.01f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(focus, cameraCollisionRadius, travel / travelDistance,
                    travelDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float nearestBlockDistance = travelDistance;
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                    if (hit.distance < nearestBlockDistance) nearestBlockDistance = hit.distance;
                }
                if (nearestBlockDistance < travelDistance)
                    desiredPosition = focus + travel.normalized * Mathf.Max(1.2f, nearestBlockDistance - 0.12f);
            }
            followCamera.transform.position = Vector3.SmoothDamp(followCamera.transform.position, desiredPosition, ref cameraVelocity, cameraSmoothTime);
            followCamera.transform.rotation = rotation;
            followCamera.transform.LookAt(focus);
        }

        public AstraInteractable GetNearestFacility()
        {
            AstraInteractable[] facilities = FindObjectsByType<AstraInteractable>(FindObjectsSortMode.None);
            AstraInteractable nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (AstraInteractable facility in facilities)
            {
                Collider facilityCollider = facility.GetComponent<Collider>();
                Vector3 point = facilityCollider != null ? facilityCollider.ClosestPoint(transform.position + Vector3.up * 1.1f) : facility.transform.position;
                float distance = Vector3.Distance(transform.position, point);
                if (distance <= facility.interactionRange && distance < nearestDistance)
                {
                    Vector3 origin = transform.position + Vector3.up * 1.1f;
                    Vector3 towardFacility = point - origin;
                    if (towardFacility.sqrMagnitude > 0.05f && Physics.Raycast(origin, towardFacility.normalized, out RaycastHit obstruction,
                            towardFacility.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                        obstruction.transform != facility.transform && !obstruction.transform.IsChildOf(facility.transform))
                        continue;
                    nearest = facility;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        public void ResetForMission()
        {
            hasClickMoveTarget = false;
            verticalSpeed = 0f;
            controller.enabled = false;
            transform.position = new Vector3(0f, 0.05f, -1.5f);
            transform.rotation = Quaternion.identity;
            controller.enabled = true;
            cameraVelocity = Vector3.zero;
            yaw = 0f;
            pitch = 18f;
        }
    }
}
