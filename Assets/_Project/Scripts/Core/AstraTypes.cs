using UnityEngine;

namespace Astra
{
    public enum AstraMissionId { FirstLight = 0, FoodForTheLongStay = 1, StormWatch = 2, ShadowScout = 3 }
    public enum AstraDestinationId { Moon = 0, Mars = 1, Europa = 2 }

    public enum AstraObjectiveId
    {
        Communications, Solar, LifeSupport, Shelter, CropHarvest, StormResponse, RoverSample, OxygenRestored, WaterRestored,
        WeatherReportReviewed, ProtectedAreaReadiness, PostDrillInspection
    }

    public sealed class AstraMissionDefinition
    {
        public readonly AstraMissionId Id;
        public readonly AstraDestinationId Destination;
        public readonly string Title, Subtitle, Briefing, Lesson, SourceLabel, SourceUrl;
        public readonly AstraObjectiveId[] Objectives;
        public readonly int CropHarvestsRequired;

        public AstraMissionDefinition(AstraMissionId id, AstraDestinationId destination, string title, string subtitle,
            string briefing, string lesson, string sourceLabel, string sourceUrl, int cropHarvestsRequired,
            params AstraObjectiveId[] objectives)
        {
            Id = id; Destination = destination; Title = title; Subtitle = subtitle; Briefing = briefing;
            Lesson = lesson; SourceLabel = sourceLabel; SourceUrl = sourceUrl;
            CropHarvestsRequired = cropHarvestsRequired; Objectives = objectives;
        }
    }

    public static class AstraMissionCatalog
    {
        private static readonly AstraMissionDefinition[] Missions =
        {
            new AstraMissionDefinition(AstraMissionId.FirstLight, AstraDestinationId.Moon, "First Light", "Stabilize the lunar outpost",
                "Review the outpost readings, choose how to align the solar array, operate the water and oxygen systems, prepare the shelter, respond to a practice alert, then drive the rover and classify a virtual sample.",
                "NASA ECLSS connects water recovery, air revitalization, and oxygen generation. NASA reports about 90% water recovery aboard the ISS. ASTRA simplifies these systems into fictional game meters. The solar choices, alert, and sample are learning activities, not operating procedures or dose forecasts.",
                "NASA ECLSS", "https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/", 1,
                AstraObjectiveId.Communications, AstraObjectiveId.Solar, AstraObjectiveId.LifeSupport,
                AstraObjectiveId.WaterRestored, AstraObjectiveId.OxygenRestored, AstraObjectiveId.Shelter,
                AstraObjectiveId.StormResponse, AstraObjectiveId.RoverSample),
            new AstraMissionDefinition(AstraMissionId.FoodForTheLongStay, AstraDestinationId.Moon, "Moon Greenhouse", "Grow and harvest a lunar crop",
                "Run the greenhouse as its own mission: choose between two crop plans, manage the water and power budget, observe plant growth, and complete two harvest cycles.",
                "NASA studies plant growth as a way to provide fresh food and support future long-duration missions. ASTRA's crop choices, times, yields, and costs are simplified fictional game values, not horticulture instructions.",
                "NASA Space Crops", "https://science.nasa.gov/biological-physical/space-crops/", 2,
                AstraObjectiveId.Solar, AstraObjectiveId.LifeSupport, AstraObjectiveId.CropHarvest),
            new AstraMissionDefinition(AstraMissionId.StormWatch, AstraDestinationId.Mars, "Storm Watch: Protect the Crew", "Review, shelter, and inspect the Mars outpost",
                "Review a NASA DONKI space-weather event at the monitoring station. Check power, life support, communications, and the protected area. Suspend the planned rover traverse, guide the astronaut to shelter during a simulated event, then inspect the outpost and submit a post-drill report.",
                "NASA DONKI records analyzed space-weather observations and event context. Its records do not provide a Mars surface dose or a crew medical forecast. ASTRA's alert, countdown, and exposure index are fictional training values.",
                "NASA CCMC DONKI", "https://ccmc.gsfc.nasa.gov/tools/DONKI/", 1,
                AstraObjectiveId.WeatherReportReviewed, AstraObjectiveId.ProtectedAreaReadiness,
                AstraObjectiveId.StormResponse, AstraObjectiveId.PostDrillInspection),
            new AstraMissionDefinition(AstraMissionId.ShadowScout, AstraDestinationId.Europa, "Shadow Scout", "Survey an icy moon",
                "Establish the Europa outpost link, choose a rover route, drive to the marked survey site, and classify a virtual surface sample from its clues.",
                "Europa is an icy moon with a global ocean beneath its crust. The ASTRA rover route and sample are fictional; a surface sample cannot be treated as a sample of the subsurface ocean.",
                "NASA Europa", "https://science.nasa.gov/jupiter/moons/europa/", 1,
                AstraObjectiveId.Communications, AstraObjectiveId.Solar, AstraObjectiveId.RoverSample)
        };

        public static int Count => Missions.Length;
        public static AstraMissionDefinition Get(AstraMissionId id) => Missions[Mathf.Clamp((int)id, 0, Missions.Length - 1)];
        public static AstraMissionDefinition Get(int index) => Missions[Mathf.Clamp(index, 0, Missions.Length - 1)];

        public static string DestinationName(AstraDestinationId id)
        {
            switch (id) { case AstraDestinationId.Mars: return "MARS"; case AstraDestinationId.Europa: return "EUROPA"; default: return "MOON"; }
        }

        public static string ObjectiveTitle(AstraObjectiveId id)
        {
            switch (id)
            {
                case AstraObjectiveId.Communications: return "Activate the communications station";
                case AstraObjectiveId.Solar: return "Bring the solar array online";
                case AstraObjectiveId.LifeSupport: return "Inspect life-support systems";
                case AstraObjectiveId.Shelter: return "Prepare the radiation shelter";
                case AstraObjectiveId.CropHarvest: return "Complete the crop harvest";
                case AstraObjectiveId.StormResponse: return "Respond to the solar-particle drill";
                case AstraObjectiveId.RoverSample: return "Collect and classify a virtual sample with the rover";
                case AstraObjectiveId.OxygenRestored: return "Generate breathing oxygen";
                case AstraObjectiveId.WaterRestored: return "Recover clean water";
                case AstraObjectiveId.WeatherReportReviewed: return "Review the space-weather report at the monitoring station";
                case AstraObjectiveId.ProtectedAreaReadiness: return "Check power, life support, communications, and shelter readiness";
                case AstraObjectiveId.PostDrillInspection: return "Inspect the outpost and submit the post-drill report";
                default: return id.ToString();
            }
        }
    }

    public static class AstraCampaignProgress
    {
        private const string UnlockedKey = "ASTRA_CAMPAIGN_UNLOCKED_V2";
        private const string DestinationKey = "ASTRA_DESTINATION_REACHED_V2";
        public static int HighestUnlocked => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 0), 0, AstraMissionCatalog.Count - 1);
        public static AstraDestinationId HighestDestinationReached => (AstraDestinationId)Mathf.Clamp(PlayerPrefs.GetInt(DestinationKey, 0), 0, 2);
        public static int BestScore(AstraMissionId mission) => PlayerPrefs.GetInt("ASTRA_BEST_SCORE_V2_" + (int)mission, 0);
        public static bool IsUnlocked(AstraMissionId mission) => (int)mission <= HighestUnlocked;
        public static bool IsMissionAvailable(AstraMissionId mission)
        {
            AstraMissionDefinition definition = AstraMissionCatalog.Get(mission);
            return IsUnlocked(mission) && (int)definition.Destination <= (int)HighestDestinationReached;
        }

        public static void RecordSuccess(AstraMissionId mission, int score)
        {
            string scoreKey = "ASTRA_BEST_SCORE_V2_" + (int)mission;
            if (score > PlayerPrefs.GetInt(scoreKey, 0)) PlayerPrefs.SetInt(scoreKey, score);
            int nextUnlocked = Mathf.Min(AstraMissionCatalog.Count - 1, (int)mission + 1);
            if (nextUnlocked > HighestUnlocked) PlayerPrefs.SetInt(UnlockedKey, nextUnlocked);
            PlayerPrefs.Save();
        }

        public static void MarkDestinationReached(AstraDestinationId destination)
        {
            if ((int)destination > (int)HighestDestinationReached)
                PlayerPrefs.SetInt(DestinationKey, (int)destination);
            PlayerPrefs.Save();
        }
    }

    public enum FacilityType
    {
        Communications, SolarArray, LifeSupport, Habitat, Greenhouse, OxygenProcessor,
        WaterRecycler, ResearchLab, Rover, RadiationShelter, Spacecraft, SpaceWeatherStation
    }

    [System.Serializable]
    public class MissionObjective
    {
        public AstraObjectiveId id;
        public string title;
        public bool complete;
        public MissionObjective(AstraObjectiveId id) { this.id = id; title = AstraMissionCatalog.ObjectiveTitle(id); complete = false; }
    }
}
