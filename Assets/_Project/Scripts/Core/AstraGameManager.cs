using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Astra
{
    public class AstraGameManager : MonoBehaviour
    {
        public static AstraGameManager Instance { get; private set; }

        private AstraResourceManager resources;
        public float oxygen => resources != null ? resources.Oxygen : 100f;
        public float water => resources != null ? resources.Water : 100f;
        public float food => resources != null ? resources.Food : 100f;
        public float power => resources != null ? resources.Power : 82f;
        public float habitatCondition => resources != null ? resources.HabitatCondition : 100f;
        public float radiationExposureIndex { get; private set; }
        public bool shelterPrepared { get; private set; }
        public bool foodHarvested { get; private set; }
        public string DonkiSummary { get; private set; } = "Loading NASA DONKI solar-particle event context…";

        public bool communicationsOnline { get; private set; }
        public bool solarOnline { get; private set; }
        public bool lifeSupportChecked { get; private set; }
        public bool eventResolved { get; private set; }
        public bool sampleCollected { get; private set; }
        public bool missionStarted { get; private set; }
        public bool missionPaused { get; private set; }
        public bool missionFinished { get; private set; }
        public float missionSeconds { get; private set; }

        public IReadOnlyList<MissionObjective> Objectives => objectives;
        public AstraResourceManager Resources => resources;
        public bool CanControlPlayer =>
            (missionStarted && !missionPaused && !missionFinished && !eventOpen) ||
            (OnboardingManager.Instance != null && OnboardingManager.Instance.AllowPlayerControl);
        public bool EventOpen => eventOpen;
        public string LatestMessage { get; private set; } = "Outpost systems awaiting crew inspection.";
        public string EventFeedback { get; private set; } = string.Empty;

        private readonly List<MissionObjective> objectives = new List<MissionObjective>
        {
            new MissionObjective("Activate the communications station"),
            new MissionObjective("Bring the solar array online"),
            new MissionObjective("Inspect life-support systems"),
            new MissionObjective("Prepare the radiation shelter"),
            new MissionObjective("Grow and harvest a food batch"),
            new MissionObjective("Respond to the solar-particle drill"),
            new MissionObjective("Collect a virtual sample with the rover")
        };

        private AstraHUD hud;
        private float resourceTick;
        private bool eventOpen;
        private bool safeDecision;
        private bool failure;
        private bool foodCycleStarted;
        private bool foodBatchReady;
        private float foodCycleElapsed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            resources = GetComponent<AstraResourceManager>();
            if (resources == null) resources = gameObject.AddComponent<AstraResourceManager>();
        }

        private void Start()
        {
            hud = FindFirstObjectByType<AstraHUD>();
            hud?.Refresh(this);
        }

        private void Update()
        {
            if (!missionStarted || missionPaused || missionFinished) return;
            missionSeconds += Time.deltaTime;
            resourceTick += Time.deltaTime;
            if (resourceTick >= 10f)
            {
                resourceTick -= 10f;
                TickResources();
            }
            if (foodCycleStarted && !foodBatchReady && !foodHarvested)
            {
                foodCycleElapsed += Time.deltaTime;
                if (foodCycleElapsed >= 20f)
                {
                    foodBatchReady = true;
                    LatestMessage = "Greenhouse crop cycle is ready. Return to the greenhouse to harvest it. This is a shortened game cycle.";
                    hud?.ShowMessage(LatestMessage);
                }
            }
            if (!eventOpen && !eventResolved && communicationsOnline && solarOnline && missionSeconds >= 55f)
                OpenRadiationDrill();
            if (oxygen <= 0f || water <= 0f || habitatCondition <= 0f)
                FailMission();
        }

        public void BeginMission()
        {
            if (missionFinished) return;
            missionStarted = true;
            missionPaused = false;
            missionSeconds = 0f;
            resourceTick = 0f;
            StartCoroutine(LoadDonkiContext());
            LatestMessage = "Mission clock started. Proceed to the communications station.";
            hud?.SetIntroVisible(false);
            hud?.SetPauseVisible(false);
            hud?.ShowMessage(LatestMessage);
            hud?.SetCursorForMission(true);
        }

        public void TogglePause()
        {
            if (!missionStarted || missionFinished || eventOpen) return;
            missionPaused = !missionPaused;
            hud?.SetPauseVisible(missionPaused);
            hud?.SetCursorForMission(!missionPaused);
        }

        public void TryInteract(Vector3 playerPosition)
        {
            AstraPlayerController player = FindFirstObjectByType<AstraPlayerController>();
            AstraInteractable nearest = player != null ? player.GetNearestFacility() : null;
            if (OnboardingManager.Instance != null && OnboardingManager.Instance.IsPracticeActive)
            {
                if (nearest == null)
                {
                    VoiceGuideManager.Instance?.Say("tutorial.interact.body", "Walk to the glowing practice panel and press the interaction key.", "hint_interact", true);
                    return;
                }
                OnboardingManager.Instance.HandlePracticeInteraction(nearest);
                return;
            }
            if (nearest == null)
            {
                hud?.ShowMessage("No facility in range. Follow the cyan markers.");
                return;
            }
            nearest.Interact(this);
        }

        public void InteractWith(AstraInteractable facility)
        {
            if (facility == null || !missionStarted || missionPaused || eventOpen) return;
            switch (facility.facilityType)
            {
                case FacilityType.Communications:
                    if (!communicationsOnline)
                    {
                        communicationsOnline = true;
                        facility.isOnline = true;
                        LatestMessage = "Comms link established. Mission Control is receiving your telemetry.";
                    }
                    else LatestMessage = "Communications are online. Mission Control confirms signal quality is nominal.";
                    CompleteObjective(0);
                    break;
                case FacilityType.SolarArray:
                    if (!solarOnline)
                    {
                        solarOnline = true;
                        facility.isOnline = true;
                        resources.AddPower(8f);
                        LatestMessage = "Solar array is tracking the Sun. The outpost battery is charging.";
                    }
                    else LatestMessage = "Solar array is online and supplying the outpost.";
                    CompleteObjective(1);
                    break;
                case FacilityType.LifeSupport:
                    if (!lifeSupportChecked)
                    {
                        lifeSupportChecked = true;
                        resources.AddOxygen(4f);
                        resources.AddWater(4f);
                        LatestMessage = "Life-support loop checked. Oxygen and water reserves are within safe game limits.";
                    }
                    else LatestMessage = "Life-support loop is stable. Keep an eye on the resource dashboard.";
                    CompleteObjective(2);
                    break;
                case FacilityType.Habitat:
                    resources.RepairHabitat(3f);
                    LatestMessage = "Habitat seal inspection complete. Condition is " + Mathf.RoundToInt(habitatCondition) + "%.";
                    break;
                case FacilityType.Greenhouse:
                    if (foodHarvested)
                        LatestMessage = "The greenhouse batch has been harvested. The CSA learning link is in BRIEFING / SCIENCE.";
                    else if (foodBatchReady)
                    {
                        resources.AddFood(18f);
                        foodHarvested = true;
                        foodBatchReady = false;
                        CompleteObjective(4);
                        LatestMessage = "Food batch harvested: food +18 game units. The crop cycle cost water and power; these values are fictional.";
                    }
                    else if (foodCycleStarted)
                    {
                        LatestMessage = "The crop is growing. Return when the greenhouse indicator says the batch is ready.";
                    }
                    else if (resources.Power < 8f || resources.Water < 8f)
                    {
                        LatestMessage = "A crop cycle needs 8 game power and 8 game water. Restore reserves, then try again.";
                    }
                    else
                    {
                        resources.TrySpendPower(8f);
                        resources.TrySpendWater(8f);
                        foodCycleStarted = true;
                        foodCycleElapsed = 0f;
                        LatestMessage = "Crop cycle started: 8 game water and 8 game power committed. Return after the shortened 20-second game cycle.";
                    }
                    break;
                case FacilityType.RadiationShelter:
                    if (shelterPrepared)
                        LatestMessage = "Shelter is prepared. Using it during the simulated solar-particle drill costs less power and lowers ASTRA's fictional exposure index.";
                    else if (!TrySpendPower(8f))
                        LatestMessage = "Shelter preparation needs 8 game power. Bring the solar array online first.";
                    else
                    {
                        shelterPrepared = true;
                        CompleteObjective(3);
                        LatestMessage = "Radiation shelter prepared. Regolith can help shield a shelter, but this game does not calculate real dose or shielding performance.";
                    }
                    break;
                case FacilityType.OxygenProcessor:
                    if (!TrySpendPower(4f)) LatestMessage = "The oxygen processor needs 4 power units. Bring the solar array online.";
                    else
                    {
                        resources.AddOxygen(10f);
                        LatestMessage = "Oxygen processor cycle complete: oxygen +10, power −4. These are game values.";
                    }
                    break;
                case FacilityType.WaterRecycler:
                    if (!TrySpendPower(4f)) LatestMessage = "The water recycler needs 4 power units. Bring the solar array online.";
                    else
                    {
                        resources.AddWater(10f);
                        LatestMessage = "Water recycling cycle complete: water +10, power −4. These are game values.";
                    }
                    break;
                case FacilityType.ResearchLab:
                    LatestMessage = "Research note: lunar samples can reveal how the Moon formed and changed. This mission sample is virtual.";
                    break;
                case FacilityType.Rover:
                    if (!eventResolved)
                    {
                        LatestMessage = "Stabilize the outpost before starting the rover sample run.";
                        break;
                    }
                    if (!shelterPrepared)
                    {
                        LatestMessage = "Prepare the radiation shelter before the rover leaves. Visit the marked Radiation Shelter.";
                        break;
                    }
                    if (!foodHarvested)
                    {
                        LatestMessage = "Complete the greenhouse crop cycle and harvest food before launching the rover.";
                        break;
                    }
                    if (!sampleCollected)
                    {
                        if (!TrySpendPower(6f))
                        {
                            LatestMessage = "Rover launch needs 6 power units. Restore power before collecting the sample.";
                            break;
                        }
                        sampleCollected = true;
                        CompleteObjective(6);
                        LatestMessage = "Virtual regolith sample secured. Rover telemetry uploaded to the lab.";
                        FinishMission();
                    }
                    else LatestMessage = "Sample container sealed and ready for lab analysis.";
                    break;
            }
            hud?.ShowMessage(LatestMessage);
            hud?.Refresh(this);
        }

        private void TickResources()
        {
            string oldWarning = resources.GetWarning();
            resources.AdvanceTimeStep(solarOnline, lifeSupportChecked);
            string warning = resources.GetWarning();
            if (!string.IsNullOrEmpty(warning) && warning != oldWarning)
            {
                LatestMessage = warning;
                hud?.ShowMessage(warning);
            }
            else if (string.IsNullOrEmpty(warning) && !string.IsNullOrEmpty(oldWarning))
            {
                LatestMessage = "Resource reserves have recovered to a stable game range.";
                hud?.ShowMessage(LatestMessage);
            }
        }

        private bool TrySpendPower(float amount) => resources.TrySpendPower(amount);

        private void OpenRadiationDrill()
        {
            eventOpen = true;
            missionPaused = true;
            EventFeedback = string.Empty;
            LatestMessage = "Training alert: simulated solar-particle event. Review the NASA data context, then choose a response.";
            hud?.SetEventVisible(true);
            hud?.SetCursorForMission(false);
        }

        public void ResolveRadiationDrill(bool shelterCrew)
        {
            if (!eventOpen) return;
            if (shelterCrew)
            {
                float powerCost = shelterPrepared ? 6f : 18f;
                if (!TrySpendPower(powerCost))
                {
                    LatestMessage = "Not enough game power to move the crew to shelter. Choose continue outside, or restart and prepare more power.";
                    hud?.ShowMessage(LatestMessage);
                    return;
                }
                radiationExposureIndex = Mathf.Min(100f, radiationExposureIndex + (shelterPrepared ? 5f : 15f));
                resources.AddFood(-3f);
                safeDecision = true;
                EventFeedback = "Sheltering costs power and briefly interrupts greenhouse work. In this fictional drill, the prepared shelter adds less to the exposure index. The index is not a measured dose.";
                LatestMessage = "Crew sheltered. Power was used and food work paused during the training scenario.";
            }
            else
            {
                radiationExposureIndex = Mathf.Min(100f, radiationExposureIndex + 30f);
                resources.DamageHabitat(8f);
                EventFeedback = "The crew remained outside to keep tasks running. The training scenario adds more to its fictional exposure index. DONKI events are not lunar radiation-dose readings.";
                LatestMessage = "Outside operations continued during the simulated event. Check the exposure index in the science briefing.";
            }
            eventOpen = false;
            eventResolved = true;
            missionPaused = false;
            CompleteObjective(5);
            hud?.SetEventVisible(false);
            hud?.SetCursorForMission(true);
            hud?.ShowMessage(EventFeedback);
            hud?.Refresh(this);
        }

        private void CompleteObjective(int index)
        {
            if (index >= 0 && index < objectives.Count) objectives[index].complete = true;
        }

        private void FailMission()
        {
            if (missionFinished) return;
            failure = true;
            missionFinished = true;
            missionPaused = true;
            hud?.ShowReport(CalculateScore(), false, BuildLearningSummary());
            hud?.SetCursorForMission(false);
        }

        private void FinishMission()
        {
            missionFinished = true;
            missionPaused = true;
            hud?.ShowReport(CalculateScore(), !failure, BuildLearningSummary());
            hud?.SetCursorForMission(false);
        }

        public void RestartMission()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        public int CalculateScore()
        {
            int objectiveScore = 0;
            foreach (MissionObjective objective in objectives) if (objective.complete) objectiveScore += 120;
            float resources = (oxygen + water + food + power) / 4f;
            int conditionScore = Mathf.RoundToInt(habitatCondition * 1.6f + resources * 1.2f);
            int decisionScore = safeDecision ? 80 : eventResolved ? 20 : 0;
            return Mathf.Clamp(objectiveScore + conditionScore + decisionScore, 0, 1000);
        }

        private string BuildLearningSummary()
        {
            string science = "Food growing used water and power; the Canadian Space Agency youth food-production toolkit explains why growing food for spaceflight is challenging. NASA DONKI records solar-particle events, but does not provide this lunar shelter's dose. ASTRA's exposure index and costs are fictional game values.";
            if (failure) return "Life-support supplies and habitat condition are connected. Monitor reserves and shelter choices. " + science;
            return (safeDecision
                ? "You used shelter at a power and food cost, trading resources for a lower simulated exposure index. "
                : "You kept operations running but accepted a higher simulated exposure index. ") + science;
        }

        [Serializable] private class DonkiSepEvent { public string activityID; public string eventTime; public string link; }
        [Serializable] private class DonkiSepList { public DonkiSepEvent[] items; }

        private IEnumerator LoadDonkiContext()
        {
            DateTime end = DateTime.UtcNow.Date;
            DateTime start = end.AddDays(-30);
            string startText = start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            string endText = end.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            string url = "https://ccmc.gsfc.nasa.gov/DONKI-API/get/SEP?startDate=" + startText + "&endDate=" + endText;
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 8;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    DonkiSummary = "NASA DONKI is offline or unavailable. The alert remains a fictional practice scenario; no live data is substituted.";
                    yield break;
                }
                try
                {
                    string json = request.downloadHandler.text;
                    DonkiSepList parsed = JsonUtility.FromJson<DonkiSepList>("{\"items\":" + json + "}");
                    DonkiSepEvent[] events = parsed != null ? parsed.items : null;
                    int count = events != null ? events.Length : 0;
                    string latest = count > 0 ? " Latest record: " + events[count - 1].activityID + " at " + events[count - 1].eventTime + "." : " No SEP records were returned for this interval.";
                    DonkiSummary = "NASA DONKI SEP records, " + startText + " to " + endText + ": " + count + "." + latest + " These are solar-event records, not radiation dose at the Moon or a crew-health forecast.";
                }
                catch (Exception)
                {
                    DonkiSummary = "NASA DONKI request returned data that ASTRA could not read. The alert remains a fictional practice scenario.";
                }
            }
        }

        public void SetPausedFromHud(bool paused)
        {
            missionPaused = paused;
            hud?.SetPauseVisible(paused);
            hud?.SetCursorForMission(!paused);
        }

        public string GetOutpostStatus()
        {
            if (!string.IsNullOrEmpty(resources.GetWarning())) return "ATTENTION";
            if (!communicationsOnline || !solarOnline) return "INITIALIZING";
            return "NOMINAL";
        }
    }
}
