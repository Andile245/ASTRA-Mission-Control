using UnityEngine;

namespace Astra
{
    /// <summary>Backfills mission facilities missing from an older saved bootstrap scene.</summary>
    public static class AstraMissionWorldSetup
    {
        public static AstraInteractable EnsureSpaceWeatherStation()
        {
            foreach (AstraInteractable facility in Object.FindObjectsByType<AstraInteractable>(FindObjectsSortMode.None))
                if (facility.facilityType == FacilityType.SpaceWeatherStation) return facility;

            GameObject outpost = GameObject.Find("SHACKLETON OUTPOST");
            GameObject root = new GameObject("Space Weather Monitoring Station");
            if (outpost != null)
            {
                root.transform.SetParent(outpost.transform, false);
                root.transform.localPosition = new Vector3(8f, 0f, 22f);
            }
            else root.transform.position = new Vector3(8f, 0f, 22f);
            AstraInteractable interactable = root.AddComponent<AstraInteractable>();
            interactable.facilityType = FacilityType.SpaceWeatherStation;
            interactable.facilityName = "Space Weather Monitoring Station";
            interactable.description = "Review the NASA DONKI event briefing and submit the post-drill inspection report.";
            interactable.interactionRange = 5f;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.05f, 0f);
            collider.size = new Vector3(1.8f, 2.2f, 1.4f);
            AddPart(PrimitiveType.Cube, "Weather console pedestal", root.transform, new Vector3(0f, 0.55f, 0f),
                new Vector3(1.65f, 1.1f, 1.15f), new Color(0.20f, 0.29f, 0.36f), Color.black);
            AddPart(PrimitiveType.Cube, "Weather monitoring screen", root.transform, new Vector3(0f, 1.48f, 0.48f),
                new Vector3(1.48f, 0.82f, 0.10f), new Color(0.04f, 0.20f, 0.29f), new Color(0.02f, 0.32f, 0.52f));
            AddPart(PrimitiveType.Cube, "Weather monitor graph", root.transform, new Vector3(0f, 1.45f, 0.54f),
                new Vector3(0.96f, 0.08f, 0.025f), new Color(0.18f, 0.91f, 0.86f), new Color(0.08f, 0.68f, 0.62f));
            AddPart(PrimitiveType.Cube, "Weather monitor status light", root.transform, new Vector3(0.54f, 0.96f, 0.59f),
                new Vector3(0.14f, 0.14f, 0.06f), new Color(0.17f, 0.86f, 0.58f), new Color(0.05f, 0.62f, 0.30f));
            return interactable;
        }

        public static AstraInteractable EnsureSpacecraft()
        {
            foreach (AstraInteractable facility in Object.FindObjectsByType<AstraInteractable>(FindObjectsSortMode.None))
                if (facility.facilityType == FacilityType.Spacecraft) return facility;

            GameObject root = new GameObject("ASTRA interplanetary spacecraft");
            root.transform.position = new Vector3(20f, 0f, 18f);
            AstraInteractable interactable = root.AddComponent<AstraInteractable>();
            interactable.facilityType = FacilityType.Spacecraft;
            interactable.facilityName = "Interplanetary spacecraft";
            interactable.description = "Repair the spacecraft before launching to the next unlocked destination.";
            interactable.interactionRange = 8f;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.25f, 0f);
            collider.size = new Vector3(9f, 3f, 5f);
            AddPart(PrimitiveType.Capsule, "Spacecraft pressure hull", root.transform, new Vector3(0f, 1.45f, 0f),
                new Vector3(2.3f, 0.95f, 5.3f), new Color(0.68f, 0.75f, 0.80f), Color.black);
            AddPart(PrimitiveType.Sphere, "Spacecraft cockpit", root.transform, new Vector3(0f, 1.5f, 2.45f),
                new Vector3(1.45f, 0.78f, 0.45f), new Color(0.08f, 0.48f, 0.68f), new Color(0.02f, 0.18f, 0.28f));
            AddPart(PrimitiveType.Cube, "Spacecraft damaged service panel", root.transform, new Vector3(1.18f, 1.42f, 0f),
                new Vector3(0.14f, 0.75f, 1.2f), new Color(0.76f, 0.20f, 0.15f), new Color(0.42f, 0.04f, 0.02f));
            for (int side = -1; side <= 1; side += 2)
            {
                AddPart(PrimitiveType.Cylinder, "Spacecraft landing strut", root.transform, new Vector3(side * 1.5f, 0.46f, -0.35f),
                    new Vector3(0.12f, 0.46f, 0.12f), new Color(0.45f, 0.52f, 0.56f), Color.black);
                AddPart(PrimitiveType.Cube, "Spacecraft solar wing", root.transform, new Vector3(side * 3.3f, 1.45f, -0.35f),
                    new Vector3(2.1f, 0.08f, 1.45f), new Color(0.08f, 0.24f, 0.49f), new Color(0.01f, 0.05f, 0.14f));
            }
            return interactable;
        }

        public static void ApplyDestination(AstraDestinationId destination)
        {
            GameObject terrain = FindSceneObject("Regolith terrain mesh");
            Renderer terrainRenderer = terrain != null ? terrain.GetComponent<Renderer>() : null;
            if (terrainRenderer != null)
            {
                Color color = destination == AstraDestinationId.Mars ? new Color(0.48f, 0.24f, 0.18f) :
                    destination == AstraDestinationId.Europa ? new Color(0.30f, 0.43f, 0.50f) : new Color(0.30f, 0.31f, 0.33f);
                terrainRenderer.material.color = color;
            }
            Camera missionCamera = Camera.main;
            if (missionCamera != null)
                missionCamera.backgroundColor = destination == AstraDestinationId.Mars ? new Color(0.055f, 0.018f, 0.012f) :
                    destination == AstraDestinationId.Europa ? new Color(0.008f, 0.025f, 0.065f) : new Color(0.004f, 0.006f, 0.016f);
            RenderSettings.ambientLight = destination == AstraDestinationId.Mars ? new Color(0.34f, 0.19f, 0.14f) :
                destination == AstraDestinationId.Europa ? new Color(0.20f, 0.29f, 0.40f) : new Color(0.25f, 0.25f, 0.27f);
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional)
                    light.color = destination == AstraDestinationId.Mars ? new Color(1f, 0.78f, 0.66f) :
                        destination == AstraDestinationId.Europa ? new Color(0.69f, 0.82f, 1f) : Color.white;
            GameObject earth = FindSceneObject("Earth in the lunar sky");
            if (earth != null) earth.SetActive(destination == AstraDestinationId.Moon);
            SetDestinationSkyBody(destination);
        }

        public static void SetSpacecraftRepaired(bool repaired)
        {
            AstraInteractable ship = EnsureSpacecraft();
            if (ship == null) return;
            Transform panel = ship.transform.Find("Spacecraft damaged service panel");
            Renderer renderer = panel != null ? panel.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                Color color = repaired ? new Color(0.12f, 0.78f, 0.38f) : new Color(0.76f, 0.20f, 0.15f);
                renderer.material.color = color;
                if (renderer.material.HasProperty("_EmissionColor"))
                {
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", color * (repaired ? 0.5f : 0.25f));
                }
            }
        }

        private static void SetDestinationSkyBody(AstraDestinationId destination)
        {
            GameObject body = FindSceneObject("ASTRA destination sky body");
            if (body == null)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.name = "ASTRA destination sky body";
                body.transform.position = new Vector3(34f, 27f, 118f);
                body.transform.localScale = Vector3.one * 10f;
                Collider collider = body.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);
            }
            Renderer renderer = body.GetComponent<Renderer>();
            renderer.material.color = destination == AstraDestinationId.Mars ? new Color(0.72f, 0.25f, 0.13f) : new Color(0.62f, 0.78f, 0.92f);
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", destination == AstraDestinationId.Mars ? new Color(0.25f, 0.05f, 0.01f) : new Color(0.14f, 0.23f, 0.36f));
            }
            body.SetActive(destination != AstraDestinationId.Moon);
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (GameObject item in Resources.FindObjectsOfTypeAll<GameObject>())
                if (item.name == objectName && item.scene.IsValid()) return item;
            return null;
        }

        public static AstraInteractable EnsureRadiationShelter()
        {
            foreach (AstraInteractable facility in Object.FindObjectsByType<AstraInteractable>(FindObjectsSortMode.None))
                if (facility.facilityType == FacilityType.RadiationShelter) return facility;

            GameObject outpost = GameObject.Find("SHACKLETON OUTPOST");
            Transform parent = outpost != null ? outpost.transform : null;
            GameObject root = new GameObject("Radiation Shelter");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = new Vector3(-8f, 0f, 19f);
            }
            AstraInteractable interactable = root.AddComponent<AstraInteractable>();
            interactable.facilityType = FacilityType.RadiationShelter;
            interactable.facilityName = "Radiation shelter";
            interactable.description = "Prepare a crew shelter for the simulated solar-particle alert. This is a fictional learning activity, not radiation-dose guidance.";
            interactable.interactionRange = 6f;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.1f, 0f);
            collider.size = new Vector3(6f, 2.8f, 6f);

            AddPart(PrimitiveType.Cube, "Shelter module", root.transform, new Vector3(0f, 1.15f, 0f), new Vector3(4.8f, 2.2f, 4.4f), new Color(0.33f, 0.40f, 0.53f), new Color(0.15f, 0.18f, 0.28f));
            AddPart(PrimitiveType.Cube, "Shelter access door", root.transform, new Vector3(0f, 0.78f, 2.23f), new Vector3(1.35f, 1.55f, 0.10f), new Color(0.15f, 0.72f, 0.83f), new Color(0.05f, 0.45f, 0.60f));
            AddPart(PrimitiveType.Cube, "Left regolith shielding berm", root.transform, new Vector3(-2.9f, 0.42f, 0f), new Vector3(0.65f, 0.84f, 5.2f), new Color(0.47f, 0.43f, 0.37f), Color.black);
            AddPart(PrimitiveType.Cube, "Right regolith shielding berm", root.transform, new Vector3(2.9f, 0.42f, 0f), new Vector3(0.65f, 0.84f, 5.2f), new Color(0.47f, 0.43f, 0.37f), Color.black);
            AddPart(PrimitiveType.Cube, "Shelter status panel", root.transform, new Vector3(1.15f, 1.55f, 2.25f), new Vector3(0.36f, 0.52f, 0.10f), new Color(0.2f, 0.82f, 0.93f), new Color(0.03f, 0.65f, 0.9f));
            AddBeacon(root.transform);
            return interactable;
        }

        public static AstraGreenhouseDisplay EnsureGreenhouseDisplay()
        {
            AstraInteractable[] facilities = Object.FindObjectsByType<AstraInteractable>(FindObjectsSortMode.None);
            foreach (AstraInteractable facility in facilities)
            {
                if (facility.facilityType != FacilityType.Greenhouse) continue;
                AstraGreenhouseDisplay display = facility.GetComponent<AstraGreenhouseDisplay>();
                if (display == null) display = facility.gameObject.AddComponent<AstraGreenhouseDisplay>();
                return display;
            }
            return null;
        }

        public static AstraRoverDrive EnsureRoverDrive()
        {
            foreach (AstraInteractable facility in Object.FindObjectsByType<AstraInteractable>(FindObjectsSortMode.None))
            {
                if (facility.facilityType != FacilityType.Rover) continue;
                AstraRoverDrive drive = facility.GetComponent<AstraRoverDrive>();
                if (drive == null) drive = facility.gameObject.AddComponent<AstraRoverDrive>();
                return drive;
            }
            return null;
        }

        private static GameObject AddPart(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 localScale, Color color, Color emission)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Renderer renderer = part.GetComponent<Renderer>();
            Material material = renderer.material;
            material.color = color;
            if (material.HasProperty("_EmissionColor") && emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            Collider childCollider = part.GetComponent<Collider>();
            if (childCollider != null) Object.Destroy(childCollider);
            return part;
        }

        private static void AddBeacon(Transform parent)
        {
            GameObject beacon = new GameObject("Radiation shelter mission beacon");
            beacon.transform.SetParent(parent, false);
            beacon.transform.localPosition = new Vector3(0f, 3.45f, 0f);
            AddPart(PrimitiveType.Cylinder, "Beacon stem", beacon.transform, new Vector3(0f, -0.25f, 0f),
                new Vector3(0.07f, 0.28f, 0.07f), new Color(0.77f, 0.62f, 0.98f), Color.black);
            AddPart(PrimitiveType.Sphere, "Beacon lamp", beacon.transform, Vector3.zero,
                Vector3.one * 0.34f, new Color(0.81f, 0.62f, 1f), new Color(0.50f, 0.20f, 1f));
            Light pointLight = beacon.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.73f, 0.49f, 1f);
            pointLight.range = 5f;
            pointLight.intensity = 1.4f;
        }
    }
}
