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
        private GameObject sampleBeacon;
        private GameObject sampleObject;
        private Transform[] wheels;
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
            wheels = FindWheels();
            CreateSampleSite();
        }

        private void Update()
        {
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
                SpinWheels(travel);
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
                else game.ShowMessage("Drive toward the orange sample beacon. " + Mathf.CeilToInt(DistanceToSample) + " m to go.");
            }
            if (exitPressed) EndDrive();
        }

        public void BeginDrive()
        {
            if (IsDriving || sampleTaken || sampleSite == null) return;
            ScheduledTraverseActive = false;
            IsDriving = true;
            player = FindAnyObjectByType<AstraPlayerController>();
            player?.MountRover(this);
            sampleBeacon?.SetActive(true);
            VisualHintManager.Instance?.SetTarget(sampleBeacon != null ? sampleBeacon.transform : sampleSite);
        }

        public void EndDrive()
        {
            if (!IsDriving) return;
            IsDriving = false;
            player?.DismountRover(this);
            if (sampleBeacon != null) sampleBeacon.SetActive(!sampleTaken);
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
            if (sampleSite == null || sampleBeacon == null) return;
            Vector3 offset = route == 0 ? new Vector3(14f, 0f, -11f) : new Vector3(8f, 0f, -15f);
            sampleSite.position = homePosition + offset;
            sampleBeacon.transform.position = sampleSite.position;
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
            if (sampleBeacon != null) sampleBeacon.transform.position = defaultSamplePosition;
            sampleTaken = false;
            if (sampleObject != null) sampleObject.SetActive(true);
            if (sampleBeacon != null) sampleBeacon.SetActive(false);
        }

        private void CollectSample()
        {
            if (sampleTaken || !IsAtSample) return;
            sampleTaken = true;
            if (sampleObject != null) sampleObject.SetActive(false);
            if (sampleBeacon != null) sampleBeacon.SetActive(false);
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
            sampleBeacon = new GameObject("Orange rover sample beacon");
            sampleBeacon.transform.position = sitePosition;
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Sample landing target";
            ring.transform.SetParent(sampleBeacon.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            ring.transform.localScale = new Vector3(2.6f, 0.035f, 2.6f);
            SetPrimitive(ring, new Color(0.98f, 0.47f, 0.12f), false);
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Sample beacon lamp";
            beacon.transform.SetParent(sampleBeacon.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            beacon.transform.localScale = Vector3.one * 0.42f;
            SetPrimitive(beacon, new Color(1f, 0.62f, 0.16f), false);
            Light light = sampleBeacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.48f, 0.12f);
            light.range = 8f;
            light.intensity = 2f;
            sampleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sampleObject.name = "Virtual surface sample";
            sampleObject.transform.SetParent(sampleBeacon.transform, false);
            sampleObject.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            sampleObject.transform.localScale = new Vector3(0.65f, 0.42f, 0.58f);
            SetPrimitive(sampleObject, new Color(0.78f, 0.68f, 0.51f), false);
            sampleBeacon.SetActive(false);
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

        private Transform[] FindWheels()
        {
            System.Collections.Generic.List<Transform> result = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (child.name == "Rover wheel") result.Add(child);
            return result.ToArray();
        }

        private void SpinWheels(float travel)
        {
            if (Mathf.Abs(travel) < 0.001f) return;
            float angle = travel / 0.39f * Mathf.Rad2Deg;
            foreach (Transform wheel in wheels)
                if (wheel != null) wheel.Rotate(Vector3.up, angle, Space.Self);
        }

        private void UpdateScheduledTraverse()
        {
            const float routeLength = 8f;
            const float speed = 0.8f;
            scheduledProgress += scheduledDirection * speed * Time.deltaTime;
            if (scheduledProgress >= routeLength) { scheduledProgress = routeLength; scheduledDirection = -1f; }
            else if (scheduledProgress <= 0f) { scheduledProgress = 0f; scheduledDirection = 1f; }
            Vector3 next = homePosition + Vector3.right * scheduledProgress;
            float travel = Vector3.Distance(transform.position, next);
            transform.position = next;
            SnapToTerrain();
            SpinWheels(travel * scheduledDirection);
        }

        private static void SetPrimitive(GameObject item, Color color, bool keepCollider)
        {
            item.GetComponent<Renderer>().material.color = color;
            Collider collider = item.GetComponent<Collider>();
            if (!keepCollider && collider != null) Destroy(collider);
        }
    }
}
