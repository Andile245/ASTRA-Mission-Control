using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astra
{
    /// <summary>Turns a completed maintenance task into a visible outpost change.</summary>
    public sealed class AstraFacilityFeedback : MonoBehaviour
    {
        private static readonly List<Light> OutpostLights = new List<Light>();
        private static bool lightingBuilt;
        private Renderer[] renderers;
        private Light localLight;
        private readonly Dictionary<Material, Color> originalEmission = new Dictionary<Material, Color>();
        private readonly Dictionary<Material, bool> originalEmissionKeyword = new Dictionary<Material, bool>();
        private readonly Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();

        public void Play(AstraInteractable facility)
        {
            if (facility == null) return;
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            Color accent = AccentFor(facility.facilityType);
            foreach (Renderer item in renderers)
            {
                if (item == null) continue;
                Material material = item.material;
                if (material.HasProperty("_EmissionColor"))
                {
                    if (!originalEmission.ContainsKey(material))
                    {
                        originalEmission[material] = material.GetColor("_EmissionColor");
                        originalEmissionKeyword[material] = material.IsKeywordEnabled("_EMISSION");
                    }
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", accent * 0.35f);
                }
            }
            if (localLight == null)
            {
                GameObject lightObject = new GameObject("Activated facility light");
                lightObject.transform.SetParent(transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 2.1f, 0.2f);
                localLight = lightObject.AddComponent<Light>();
                localLight.type = LightType.Point;
                localLight.range = 7f;
                localLight.color = accent;
                localLight.intensity = 0f;
            }
            StopAllCoroutines();
            StartCoroutine(FadeOn(localLight));
            if (facility.facilityType == FacilityType.SolarArray) ActivateOutpostLighting();
            AnimateFacility(facility.facilityType);
        }

        public void ResetVisuals()
        {
            StopAllCoroutines();
            if (localLight != null) localLight.intensity = 0f;
            if (renderers != null)
                foreach (Renderer item in renderers)
                {
                    if (item == null) continue;
                    Material material = item.material;
                    if (material.HasProperty("_EmissionColor") && originalEmission.TryGetValue(material, out Color emission))
                        material.SetColor("_EmissionColor", emission);
                    if (originalEmissionKeyword.TryGetValue(material, out bool keywordEnabled) && !keywordEnabled)
                        material.DisableKeyword("_EMISSION");
                }
            foreach (KeyValuePair<Transform, Quaternion> pair in originalRotations)
                if (pair.Key != null) pair.Key.localRotation = pair.Value;
            Transform fan = FindNamedChild(transform, "Activated circulation fan");
            if (fan != null) fan.gameObject.SetActive(false);
            foreach (Light light in OutpostLights)
                if (light != null) light.intensity = 0f;
        }

        private IEnumerator FadeOn(Light light)
        {
            if (light == null) yield break;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                light.intensity = Mathf.Lerp(0f, 1.7f, t / 1.5f);
                yield return null;
            }
            light.intensity = 1.7f;
        }

        private static void ActivateOutpostLighting()
        {
            if (!lightingBuilt)
            {
                lightingBuilt = true;
                Vector3[] positions =
                {
                    new Vector3(0f, 4.2f, 10f), new Vector3(-10f, 3.3f, 5f),
                    new Vector3(10f, 3.5f, 5f), new Vector3(-10f, 3.2f, -8f),
                    new Vector3(0f, 3.2f, -12f), new Vector3(10f, 3.2f, -9f),
                    new Vector3(-8f, 3.6f, 19f), new Vector3(0f, 3.4f, 21f)
                };
                for (int i = 0; i < positions.Length; i++)
                {
                    GameObject lamp = new GameObject("Outpost light " + (i + 1));
                    lamp.transform.position = positions[i];
                    Light light = lamp.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.78f, 0.48f);
                    light.range = 12f;
                    light.intensity = 0f;
                    OutpostLights.Add(light);
                }
            }
            foreach (Light light in OutpostLights)
                if (light != null) light.intensity = 2.2f;
        }

        private void AnimateFacility(FacilityType type)
        {
            switch (type)
            {
                case FacilityType.Communications:
                    Transform dish = FindNamedChild(transform, "Relay dish");
                    if (dish != null) { RememberRotation(dish); StartCoroutine(RotateTo(dish, Quaternion.Euler(24f, -28f, 0f))); }
                    break;
                case FacilityType.SolarArray:
                    Transform[] panels = FindNamedChildren(transform, "Photovoltaic panel");
                    foreach (Transform panel in panels) { RememberRotation(panel); StartCoroutine(RotateTo(panel, Quaternion.Euler(28f, 0f, 0f))); }
                    break;
                case FacilityType.Rover:
                    foreach (Transform wheel in FindNamedChildren(transform, "Rover wheel"))
                    { RememberRotation(wheel); StartCoroutine(Spin(wheel, 1.15f, Vector3.up)); }
                    break;
                case FacilityType.LifeSupport:
                    Transform fan = CreateFan();
                    fan.gameObject.SetActive(true);
                    StartCoroutine(Spin(fan, 2.2f, Vector3.up));
                    break;
                case FacilityType.Greenhouse:
                    foreach (Renderer item in renderers)
                    {
                        if (item == null || !item.name.Contains("Leaf")) continue;
                        Material material = item.material;
                        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", new Color(0.20f, 1f, 0.38f) * 0.75f);
                    }
                    break;
            }
        }

        private IEnumerator RotateTo(Transform target, Quaternion localTarget)
        {
            if (target == null) yield break;
            Quaternion start = target.localRotation;
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                target.localRotation = Quaternion.Slerp(start, localTarget, t / 2f);
                yield return null;
            }
            target.localRotation = localTarget;
        }

        private IEnumerator Spin(Transform target, float seconds, Vector3 axis)
        {
            float elapsed = 0f;
            while (target != null && elapsed < seconds)
            {
                target.Rotate(axis, 420f * Time.deltaTime, Space.Self);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private Transform CreateFan()
        {
            Transform existing = FindNamedChild(transform, "Activated circulation fan");
            if (existing != null) return existing;
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Activated circulation fan";
            hub.transform.SetParent(transform, false);
            hub.transform.localPosition = new Vector3(0f, 2.35f, 0.96f);
            hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hub.transform.localScale = new Vector3(0.16f, 0.06f, 0.16f);
            Collider collider = hub.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            hub.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "Fan blade";
                blade.transform.SetParent(hub.transform, false);
                blade.transform.localPosition = new Vector3(0f, 0.34f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
                blade.transform.localScale = new Vector3(0.12f, 0.46f, 0.035f);
                Renderer bladeRenderer = blade.GetComponent<Renderer>();
                bladeRenderer.material.color = new Color(0.62f, 0.77f, 0.81f);
                Collider bladeCollider = blade.GetComponent<Collider>();
                if (bladeCollider != null) Destroy(bladeCollider);
            }
            return hub.transform;
        }

        private static Transform FindNamedChild(Transform parent, string childName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }

        private void RememberRotation(Transform target)
        {
            if (target != null && !originalRotations.ContainsKey(target)) originalRotations[target] = target.localRotation;
        }

        private static Transform[] FindNamedChildren(Transform parent, string childName)
        {
            List<Transform> found = new List<Transform>();
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) found.Add(child);
            return found.ToArray();
        }

        private static Color AccentFor(FacilityType type)
        {
            switch (type)
            {
                case FacilityType.Communications: return new Color(0.15f, 0.8f, 1f);
                case FacilityType.SolarArray: return new Color(1f, 0.78f, 0.4f);
                case FacilityType.Greenhouse: return new Color(0.35f, 1f, 0.48f);
                case FacilityType.Rover: return new Color(1f, 0.68f, 0.26f);
                case FacilityType.RadiationShelter: return new Color(0.75f, 0.52f, 1f);
                default: return new Color(0.18f, 0.88f, 0.82f);
            }
        }
    }
}
