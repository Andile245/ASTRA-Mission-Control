using System.Collections.Generic;
using UnityEngine;

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
        public bool CanControlPlayer => missionStarted && !missionPaused && !missionFinished && !eventOpen;
        public bool EventOpen => eventOpen;
        public string LatestMessage { get; private set; } = "Outpost systems awaiting crew inspection.";
        public string EventFeedback { get; private set; } = string.Empty;

        private readonly List<MissionObjective> objectives = new List<MissionObjective>
        {
            new MissionObjective("Activate the communications station"),
            new MissionObjective("Bring the solar array online"),
            new MissionObjective("Inspect life-support systems"),
            new MissionObjective("Resolve the power shortage"),
            new MissionObjective("Collect a virtual sample with the rover")
        };

        private AstraHUD hud;
        private float resourceTick;
        private bool eventOpen;
        private bool safeDecision;
        private bool failure;

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
            if (!eventOpen && !eventResolved && communicationsOnline && solarOnline && missionSeconds >= 28f)
                OpenPowerEvent();
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
                    if (!TrySpendPower(5f)) LatestMessage = "Insufficient power for the greenhouse harvest. Bring the solar array online.";
                    else
                    {
                        resources.AddFood(12f);
                        LatestMessage = "Greenhouse harvest logged: food +12, power −5. These are game values.";
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
                    if (!sampleCollected)
                    {
                        if (!TrySpendPower(6f))
                        {
                            LatestMessage = "Rover launch needs 6 power units. Restore power before collecting the sample.";
                            break;
                        }
                        sampleCollected = true;
                        CompleteObjective(4);
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

        private void OpenPowerEvent()
        {
            eventOpen = true;
            missionPaused = true;
            resources.AddPower(-14f);
            EventFeedback = string.Empty;
            LatestMessage = "Power fluctuation detected. Review the alert and choose a response.";
            hud?.SetEventVisible(true);
            hud?.SetCursorForMission(false);
        }

        public void ResolvePowerEvent(bool shedNonessentialLoad)
        {
            if (!eventOpen) return;
            eventOpen = false;
            eventResolved = true;
            missionPaused = false;
            safeDecision = shedNonessentialLoad;
            if (shedNonessentialLoad)
            {
                resources.AddPower(14f);
                resources.AddFood(-4f);
                EventFeedback = "Good call. Pausing a nonessential load protects life support while the array recovers. Food production briefly slows.";
                LatestMessage = "Power stabilized by shedding greenhouse load. Life support remains prioritized.";
            }
            else
            {
                resources.AddPower(-18f);
                resources.DamageHabitat(12f);
                EventFeedback = "The full-system restart consumed reserve power. On a real outpost, crews would prioritize critical life-support loads.";
                LatestMessage = "Systems restarted, but reserve power and habitat condition fell.";
            }
            CompleteObjective(3);
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
            if (failure) return "Life-support supplies and habitat condition are connected. Monitor the dashboard and act before critical reserves are exhausted.";
            return safeDecision
                ? "Outposts balance limited power across critical and nonessential systems. Protecting life support can mean temporarily reducing another system's output."
                : "Outpost systems compete for limited power. Prioritizing life support helps protect crew safety while nonessential loads can be reduced.";
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
