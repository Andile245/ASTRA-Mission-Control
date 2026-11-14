namespace Astra
{
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
        public string title;
        public bool complete;

        public MissionObjective(string title)
        {
            this.title = title;
            complete = false;
        }
    }
}
