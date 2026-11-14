using UnityEngine;

namespace Astra
{
    public enum AstraMissionId
    {
        FirstLight = 0,
        AirAndWater = 1,
        ShadowScout = 2,
        StormWatch = 3,
        FoodForTheLongStay = 4
    }

    public enum AstraObjectiveId
    {
        Communications,
        Solar,
        LifeSupport,
        Shelter,
        CropHarvest,
        StormResponse,
        RoverSample,
        OxygenRestored,
        WaterRestored
    }

    public sealed class AstraMissionDefinition
    {
        public readonly AstraMissionId Id;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string Briefing;
        public readonly string Lesson;
        public readonly string SourceLabel;
        public readonly string SourceUrl;
        public readonly AstraObjectiveId[] Objectives;
        public readonly int CropHarvestsRequired;

        public AstraMissionDefinition(AstraMissionId id, string title, string subtitle, string briefing, string lesson,
            string sourceLabel, string sourceUrl, int cropHarvestsRequired, params AstraObjectiveId[] objectives)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            Briefing = briefing;
            Lesson = lesson;
            SourceLabel = sourceLabel;
            SourceUrl = sourceUrl;
            CropHarvestsRequired = cropHarvestsRequired;
            Objectives = objectives;
        }
    }

    public static class AstraMissionCatalog
    {
        private static readonly AstraMissionDefinition[] Missions =
        {
            new AstraMissionDefinition(AstraMissionId.FirstLight, "First Light", "Bring the outpost online",
                "Activate communications and solar power, inspect life support, prepare shelter, grow food, respond to a practice alert, and secure a virtual sample.",
                "Outpost systems depend on one another. Solar power supports equipment, while sheltering and food production use limited reserves. All ASTRA quantities are game values.",
                "NASA Moon Base Systems", "https://www.nasa.gov/moonbase-systems/", 1,
                AstraObjectiveId.Communications, AstraObjectiveId.Solar, AstraObjectiveId.LifeSupport, AstraObjectiveId.Shelter,
                AstraObjectiveId.CropHarvest, AstraObjectiveId.StormResponse, AstraObjectiveId.RoverSample),
            new AstraMissionDefinition(AstraMissionId.AirAndWater, "Air & Water", "Rebuild the habitat life-support loop",
                "Oxygen and water are running low. Bring solar power online, inspect life support, recover clean water, then use some of that water to generate oxygen.",
                "NASA ECLSS connects water recovery, air revitalization, and oxygen generation. Water Recovery cleans wastewater before reuse; Oxygen Generation uses electrolysis to split water into oxygen and hydrogen. NASA reports about 90% water recovery aboard the ISS. ASTRA simplifies these systems into game meters; its values are not lunar engineering data.",
                "NASA ECLSS", "https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/", 1,
                AstraObjectiveId.Solar, AstraObjectiveId.LifeSupport, AstraObjectiveId.WaterRestored, AstraObjectiveId.OxygenRestored),
            new AstraMissionDefinition(AstraMissionId.ShadowScout, "Shadow Scout", "Scout a permanently shadowed region",
                "Establish the comms link, charge the rover from the solar array, and collect a virtual sample near the crater shadow line.",
                "Some lunar south-pole crater interiors stay in permanent shadow. Scientists study these cold places because water ice may be preserved there; the rover sample in ASTRA is virtual.",
                "NASA Shackleton Crater Resource", "https://science.nasa.gov/resource/shackleton-craters-illuminated-rim-shadowed-interior/", 1,
                AstraObjectiveId.Communications, AstraObjectiveId.Solar, AstraObjectiveId.RoverSample),
            new AstraMissionDefinition(AstraMissionId.StormWatch, "Storm Watch", "Respond to a solar-particle drill",
                "Power the outpost, prepare the shelter, and choose whether to pause outdoor work during a clearly labeled practice alert.",
                "NASA DONKI catalogs analyzed space-weather events. Those records are event context, not a lunar crew dose or medical forecast. ASTRA's alert and exposure index are fictional.",
                "NASA CCMC DONKI", "https://ccmc.gsfc.nasa.gov/tools/DONKI/", 1,
                AstraObjectiveId.Communications, AstraObjectiveId.Solar, AstraObjectiveId.Shelter, AstraObjectiveId.StormResponse),
            new AstraMissionDefinition(AstraMissionId.FoodForTheLongStay, "Food for the Long Stay", "Grow a sustainable food supply",
                "Stabilize the greenhouse, protect its water and power budget, and complete two crop cycles before supplies run short.",
                "NASA studies plant growth as a way to provide fresh food and support future long-duration missions. ASTRA's crop times, yields, and costs are shortened fictional game values.",
                "NASA Space Crops", "https://science.nasa.gov/biological-physical/space-crops/", 2,
                AstraObjectiveId.Solar, AstraObjectiveId.LifeSupport, AstraObjectiveId.CropHarvest)
        };

        public static int Count => Missions.Length;
        public static AstraMissionDefinition Get(AstraMissionId id) => Missions[Mathf.Clamp((int)id, 0, Missions.Length - 1)];
        public static AstraMissionDefinition Get(int index) => Missions[Mathf.Clamp(index, 0, Missions.Length - 1)];

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
                case AstraObjectiveId.RoverSample: return "Collect a virtual sample with the rover";
                case AstraObjectiveId.OxygenRestored: return "Generate breathing oxygen";
                case AstraObjectiveId.WaterRestored: return "Recover clean water";
                default: return id.ToString();
            }
        }
    }

    public static class AstraCampaignProgress
    {
        private const string UnlockedKey = "ASTRA_CAMPAIGN_UNLOCKED";
        // Keep Mission 2 available as a playable learning route while Mission 1 remains the recommended introduction.
        public static int HighestUnlocked => Mathf.Max(1, Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 0), 0, AstraMissionCatalog.Count - 1));
        public static int BestScore(AstraMissionId mission) => PlayerPrefs.GetInt("ASTRA_BEST_SCORE_" + (int)mission, 0);

        public static bool IsUnlocked(AstraMissionId mission) => (int)mission <= HighestUnlocked;

        public static void RecordSuccess(AstraMissionId mission, int score)
        {
            string scoreKey = "ASTRA_BEST_SCORE_" + (int)mission;
            if (score > PlayerPrefs.GetInt(scoreKey, 0)) PlayerPrefs.SetInt(scoreKey, score);
            int nextUnlocked = Mathf.Min(AstraMissionCatalog.Count - 1, (int)mission + 1);
            if (nextUnlocked > HighestUnlocked) PlayerPrefs.SetInt(UnlockedKey, nextUnlocked);
            PlayerPrefs.Save();
        }
    }

    public enum FacilityType
    {
        Communications,
        SolarArray,
        LifeSupport,
        Habitat,
        Greenhouse,
        OxygenProcessor,
        WaterRecycler,
        ResearchLab,
        Rover,
        RadiationShelter
    }

    [System.Serializable]
    public class MissionObjective
    {
        public AstraObjectiveId id;
        public string title;
        public bool complete;

        public MissionObjective(AstraObjectiveId id)
        {
            this.id = id;
            title = AstraMissionCatalog.ObjectiveTitle(id);
            complete = false;
        }
    }
}
