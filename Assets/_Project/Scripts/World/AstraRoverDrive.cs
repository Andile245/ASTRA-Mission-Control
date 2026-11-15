using UnityEngine;
using UnityEngine.InputSystem;

namespace Astra
{
    /// <summary>Drives the outpost rover between its parking point and a nearby virtual sample site.</summary>
    public sealed class AstraRoverDrive : MonoBehaviour
    {
        [SerializeField] private float driveSpeed = 3.5f;
        [SerializeField] private float steeringSpeed = 95f;
        [SerializeField] private float sampleRadius = 2.4f;

        private AstraGameManager game;
        private AstraPlayerController player;
        private Vector3 homePosition;
        private Vector3 defaultSamplePosition;
        private Transform sampleSite;
        private Transform nasaVehicleModel;
        private Vector3 nasaVehicleRestPosition;
        private Quaternion nasaVehicleRestRotation;
        private Vector3 lastVehiclePosition;
        private bool nasaVehicleRestCaptured;
        private bool sampleTaken;
        private float scheduledProgress;
        private float scheduledDirection = 1f;

        public bool IsDriving { get; private set; }
        public bool ScheduledTraverseActive { get; private set; }
        public float DistanceToSample => sampleSite != null
            ? Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(sampleSite.position.x, 0f, sampleSite.position.z))
            : float.MaxValue;
        public bool IsAtSample => DistanceToSample <= sampleRadius;

        private void Awake()
        {
            game = FindAnyObjectByType<AstraGameManager>();
            player = FindAnyObjectByType<AstraPlayerController>();
            homePosition = transform.position;
            lastVehiclePosition = transform.position;
            CreateSampleSite();
        }

        private void Update()
        {
            AnimateImportedVehicle();
            if (ScheduledTraverseActive && game != null && game.missionStarted && !game.missionFinished && !game.missionPaused && !game.EventOpen)
                UpdateScheduledTraverse();
            if (!IsDriving || game == null || !game.CanControlPlayer) return;
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Vector2 input = Vector2.zero;
            PlayerSettingsManager settings = PlayerSettingsManager.Instance;
            if (keyboard != null)
            {
                Key forward = settings != null ? settings.GetKey(AstraBinding.MoveForward) : Key.W;
                Key back = settings != null ? settings.GetKey(AstraBinding.MoveBack) : Key.S;
                Key left = settings != null ? settings.GetKey(AstraBinding.MoveLeft) : Key.A;
                Key right = settings != null ? settings.GetKey(AstraBinding.MoveRight) : Key.D;
                if (keyboard[forward].isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard[back].isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard[right].isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard[left].isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            }
            if (gamepad != null) input += gamepad.leftStick.ReadValue() + gamepad.dpad.ReadValue();
            if (player != null) input += player.TouchMoveInput;
            input = Vector2.ClampMagnitude(input, 1f);

            transform.Rotate(Vector3.up, input.x * steeringSpeed * Time.deltaTime, Space.World);
            float travel = input.y * driveSpeed * Time.deltaTime;
            Vector3 nextPosition = transform.position + transform.forward * travel;
            if (Vector3.Distance(new Vector3(homePosition.x, 0f, homePosition.z), new Vector3(nextPosition.x, 0f, nextPosition.z)) <= 34f)
            {
                transform.position = nextPosition;
                SnapToTerrain();
            }

            Key interactKey = settings != null ? settings.GetKey(AstraBinding.Interact) : Key.E;
            bool keyboardCollect = keyboard != null && keyboard[interactKey].wasPressedThisFrame;
            bool gamepadCollect = gamepad != null && gamepad.buttonWest.wasPressedThisFrame;
            bool touchCollect = player != null && player.ConsumeTouchInteract();
            bool collectPressed = keyboardCollect || gamepadCollect || touchCollect;
            bool keyboardExit = keyboard != null && keyboard.qKey.wasPressedThisFrame;
            bool gamepadExit = gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
            bool touchExit = player != null && player.ConsumeTouchRoverExit();
            bool exitPressed = keyboardExit || gamepadExit || touchExit;
            if (collectPressed)
            {
                if (IsAtSample) CollectSample();
                else game.ShowMessage("Follow the surface-scan waypoint on your mission HUD. " + Mathf.CeilToInt(DistanceToSample) + " m to go.");
            }
            if (exitPressed) EndDrive();
        }

        private void AnimateImportedVehicle()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = Vector3.Distance(transform.position, lastVehiclePosition) / dt;
            lastVehiclePosition = transform.position;
            if (nasaVehicleModel == null)
                nasaVehicleModel = transform.Find("ASTRA NASA hardware/NASA SpaceExplorationVehicle");
            if (nasaVehicleModel == null) return;
            if (!nasaVehicleRestCaptured)
            {
                nasaVehicleRestPosition = nasaVehicleModel.localPosition;
                nasaVehicleRestRotation = nasaVehicleModel.localRotation;
                nasaVehicleRestCaptured = true;
            }
            // The NASA SEV file is a single complete vehicle mesh, without separate
            // wheel joints. Give the actual imported vehicle a restrained suspension
            // response while it traverses the terrain; do not spin hidden blockout wheels.
            float moving = Mathf.Clamp01(speed / Mathf.Max(0.1f, driveSpeed));
            float bounce = Mathf.Sin(Time.time * 8.5f) * 0.025f * moving;
            float pitch = Mathf.Sin(Time.time * 8.5f) * 1.1f * moving;
            nasaVehicleModel.localRotation = Quaternion.Slerp(nasaVehicleModel.localRotation,
                nasaVehicleRestRotation * Quaternion.Euler(pitch, 0f, 0f), 1f - Mathf.Exp(-8f * dt));
            Vector3 position = nasaVehicleModel.localPosition;
            position.y = Mathf.Lerp(position.y, nasaVehicleRestPosition.y + bounce, 1f - Mathf.Exp(-8f * dt));
            nasaVehicleModel.localPosition = position;
        }

        public void BeginDrive()
        {
            if (IsDriving || sampleTaken || sampleSite == null) return;
            ScheduledTraverseActive = false;
            IsDriving = true;
            player = FindAnyObjectByType<AstraPlayerController>();
            player?.MountRover(this);
            VisualHintManager.Instance?.SetTarget(sampleSite);
        }

        public void EndDrive()
        {
            if (!IsDriving) return;
            IsDriving = false;
            player?.DismountRover(this);
            VisualHintManager.Instance?.RefreshMissionTarget(game);
        }

        public void ForceEndDrive() => EndDrive();

        public void BeginScheduledTraverse()
        {
            if (sampleSite == null) return;
            ScheduledTraverseActive = true;
            scheduledProgress = 0f;
            scheduledDirection = 1f;
        }

        public void SetEuropaSurveyRoute(int route)
        {
            if (sampleSite == null) return;
            Vector3 offset = route == 0 ? new Vector3(14f, 0f, -11f) : new Vector3(8f, 0f, -15f);
            sampleSite.position = homePosition + offset;
        }

        public void SuspendScheduledTraverse()
        {
            ScheduledTraverseActive = false;
        }

        public void ResetForMission()
        {
            EndDrive();
            ScheduledTraverseActive = false;
            scheduledProgress = 0f;
            scheduledDirection = 1f;
            transform.position = homePosition;
            transform.rotation = Quaternion.identity;
            if (sampleSite != null) sampleSite.position = defaultSamplePosition;
            sampleTaken = false;
        }

        private void CollectSample()
        {
            if (sampleTaken || !IsAtSample) return;
            sampleTaken = true;
            EndDrive();
            game.CollectRoverSample(this);
        }

        private void CreateSampleSite()
        {
            if (sampleSite != null) return;
            Vector3 sitePosition = homePosition + new Vector3(8f, 0f, -10f);
            defaultSamplePosition = sitePosition;
            GameObject site = new GameObject("Virtual surface sample site");
            sampleSite = site.transform;
            sampleSite.position = sitePosition;
            // The sample task is a virtual regolith scan at this point in the
            // imported Apollo landing-site terrain. The guidance target is kept
            // invisible in-world; HUD navigation identifies the scan location.
        }

        private void SnapToTerrain()
        {
            GameObject terrain = GameObject.Find("Regolith terrain mesh");
            MeshCollider collider = terrain != null ? terrain.GetComponent<MeshCollider>() : null;
            if (collider == null) return;
            Ray ray = new Ray(transform.position + Vector3.up * 12f, Vector3.down);
            if (collider.Raycast(ray, out RaycastHit hit, 30f))
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }

        private void UpdateScheduledTraverse()
        {
            const float routeLength = 8f;
            const float speed = 0.8f;
            scheduledProgress += scheduledDirection * speed * Time.deltaTime;
            if (scheduledProgress >= routeLength) { scheduledProgress = routeLength; scheduledDirection = -1f; }
            else if (scheduledProgress <= 0f) { scheduledProgress = 0f; scheduledDirection = 1f; }
            Vector3 next = homePosition + Vector3.right * scheduledProgress;
            transform.position = next;
            SnapToTerrain();
        }
    }
}
