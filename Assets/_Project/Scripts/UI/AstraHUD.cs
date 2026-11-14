using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Astra
{
    public class AstraHUD : MonoBehaviour
    {
        private static readonly Color Navy = new Color(0.025f, 0.055f, 0.10f, 0.94f);
        private static readonly Color Cyan = new Color(0.15f, 0.86f, 1f, 1f);
        private static readonly Color Muted = new Color(0.61f, 0.72f, 0.82f, 1f);
        private AstraGameManager game;
        private Canvas canvas;
        private Font font;
        private Text clockText, statusText, objectiveText, progressText, facilityText, messageText;
        private Text missionCodeText, missionTitleText, missionDescriptionText;
        private Text reportTitleText, reportScoreText, reportBodyText, reportLearningText;
        private Text briefingBodyText;
        private Text oxygenText, waterText, foodText, powerText;
        private Image oxygenFill, waterFill, foodFill, powerFill, progressFill;
        private GameObject introPanel, pausePanel, eventPanel, reportPanel, briefingPanel, facilityPanel, tutorialPanel, resourceAlertPanel;
        private Text resourceAlertText;
        private Text tutorialTitleText, tutorialBodyText, tutorialCounterText, tutorialKeysText;
        private Text scienceDataText, eventCopyText;
        private int tutorialStep;
        private bool resumeMissionAfterTutorial;
        private static readonly string[] TutorialTitles = { "Move around the outpost.", "Walk up to a facility.", "Watch your resources." };
        private static readonly string[] TutorialBodies = {
            "Use the keys to explore the landing site. Take your time; there is no rush.",
            "Visit communications, solar power, and life support. When a facility is close, press E, gamepad X, or the joystick trigger to interact.",
            "Check oxygen, water, food, and power. Follow the objectives and choose a response when an alert appears."
        };
        private static readonly string[] TutorialKeys = {
            "WASD / LEFT STICK MOVE     SHIFT / LB SPRINT     SPACE / A HOP",
            "E / X INTERACT     MOUSE / RIGHT STICK LOOK",
            "OBJECTIVES     RESOURCE BAR     CHOOSE A RESPONSE"
        };
        private RectTransform root;
        private bool built;
        private readonly Dictionary<Text, int> baseTextSizes = new Dictionary<Text, int>();
        private readonly Dictionary<Image, Color> basePanelColors = new Dictionary<Image, Color>();
        private float refreshClock;

        private void Awake()
        {
            Build();
        }

        private void Start()
        {
            game = AstraGameManager.Instance != null ? AstraGameManager.Instance : FindFirstObjectByType<AstraGameManager>();
            Refresh(game);
            SetCursorForMission(false);
        }

        private void Update()
        {
            if (!built || game == null) return;
            refreshClock += Time.unscaledDeltaTime;
            if (refreshClock < 0.2f) return;
            refreshClock = 0f;
            Refresh(game);
            UpdateFacilityPrompt();
        }

        private void Build()
        {
            if (built) return;
            built = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            GameObject canvasObject = new GameObject("ASTRA_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Keep the authored 720p layout close to native pixels on common desktop displays.
            // The previous 960x540 reference scaled every offset and label up too far, so HUD
            // panels ran into one another on 1080p and wide Game views.
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            root = canvasObject.GetComponent<RectTransform>();

            GameObject eventSystemObject = new GameObject("ASTRA_EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            // InputSystemUIInputModule assigns Unity's default pointer and navigation actions on enable.

            BuildTopBar();
            BuildResourceAlert();
            BuildObjectives();
            BuildBottomBar();
            BuildFacilityPanel();
            BuildMessages();
            BuildEventPanel();
            BuildOverlayPanels();
            BuildBriefingPanel();
            introPanel.SetActive(false);
        }

        private void BuildTopBar()
        {
            GameObject bar = Panel("TopBar", root, Navy, new Vector2(0.02f, 1f), new Vector2(0.98f, 1f), new Vector2(0f, -106f), new Vector2(0f, -16f));
            Text brand = TextBlock(bar.transform, "ASTRA  /  MISSION CONTROL", 25, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(brand.rectTransform, new Vector2(0f, 0f), new Vector2(0.34f, 1f), new Vector2(28f, 0f), new Vector2(0f, 0f));
            missionCodeText = TextBlock(bar.transform, "ARTEMIS OUTPOST  •  MISSION 01", 15, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            SetRect(missionCodeText.rectTransform, new Vector2(0.34f, 0f), new Vector2(0.60f, 1f), Vector2.zero, Vector2.zero);
            clockText = TextBlock(bar.transform, "00:00", 22, Cyan, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(clockText.rectTransform, new Vector2(0.86f, 0f), new Vector2(0.98f, 1f), Vector2.zero, new Vector2(-22f, 0f));

            GameObject resources = Panel("ResourceBar", root, new Color(0.035f, 0.075f, 0.12f, 0.88f), new Vector2(0.09f, 1f), new Vector2(0.91f, 1f), new Vector2(0f, -186f), new Vector2(0f, -119f));
            oxygenText = AddResource(resources.transform, "O₂   OXYGEN", 0.015f, out oxygenFill);
            waterText = AddResource(resources.transform, "H₂O   WATER", 0.265f, out waterFill);
            foodText = AddResource(resources.transform, "▤   FOOD", 0.515f, out foodFill);
            powerText = AddResource(resources.transform, "ϟ   POWER", 0.765f, out powerFill);
            statusText = TextBlock(bar.transform, "OUTPOST  /  INITIALIZING", 13, new Color(0.45f, 1f, 0.75f), FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(statusText.rectTransform, new Vector2(0.60f, 0f), new Vector2(0.86f, 1f), Vector2.zero, Vector2.zero);
        }

        private void BuildResourceAlert()
        {
            resourceAlertPanel = Panel("ResourceWarning", root, new Color(0.34f, 0.10f, 0.04f, 0.94f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(730f, 40f));
            resourceAlertPanel.GetComponent<Image>().raycastTarget = false;
            resourceAlertText = TextBlock(resourceAlertPanel.transform, "", 14, new Color(1f, 0.82f, 0.58f), FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(resourceAlertText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 2f), new Vector2(-12f, -2f));
            resourceAlertPanel.SetActive(false);
        }

        private Text AddResource(Transform parent, string label, float x, out Image fill)
        {
            GameObject slot = new GameObject("Resource", typeof(RectTransform));
            slot.transform.SetParent(parent, false);
            RectTransform rt = slot.GetComponent<RectTransform>();
            SetRect(rt, new Vector2(x, 0.08f), new Vector2(x + 0.235f, 0.92f), Vector2.zero, Vector2.zero);
            Text labelText = TextBlock(slot.transform, label, 13, Muted, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(labelText.rectTransform, new Vector2(0f, 0.46f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);
            Text valueText = TextBlock(slot.transform, "100%", 16, Color.white, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(valueText.rectTransform, new Vector2(0.72f, 0.46f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            GameObject track = Panel("MeterTrack", slot.transform, new Color(0.18f, 0.25f, 0.33f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 4f), new Vector2(0f, 7f));
            fill = Panel("MeterFill", track.transform, Cyan, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            return valueText;
        }

        private void BuildObjectives()
        {
            GameObject panel = Panel("Objectives", root, Navy, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -207f), new Vector2(360f, 358f));
            Text kicker = TextBlock(panel.transform, "MISSION 01   /   ARRIVAL", 13, Cyan, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(kicker.rectTransform, new Vector2(0f, 0.83f), new Vector2(0.66f, 1f), new Vector2(22f, -4f), Vector2.zero);
            MakeButton(panel.transform, "SCIENCE / DATA  ↗", () => briefingPanel?.SetActive(true), new Vector2(0.65f, 0.86f), new Vector2(0.96f, 0.98f), new Color(0.04f, 0.18f, 0.24f), 11);
            missionTitleText = TextBlock(panel.transform, "First Light", 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(missionTitleText.rectTransform, new Vector2(0f, 0.70f), new Vector2(1f, 0.85f), new Vector2(22f, 0f), new Vector2(-18f, 0f));
            missionDescriptionText = TextBlock(panel.transform, "Bring the outpost online and keep critical systems stable.", 14, Muted, FontStyle.Normal, TextAnchor.UpperLeft);
            missionDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(missionDescriptionText.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 0.72f), new Vector2(22f, 0f), new Vector2(-18f, 0f));
            objectiveText = TextBlock(panel.transform, "", 15, Color.white, FontStyle.Normal, TextAnchor.UpperLeft);
            objectiveText.lineSpacing = 1.18f;
            SetRect(objectiveText.rectTransform, new Vector2(0f, 0.13f), new Vector2(1f, 0.54f), new Vector2(22f, -4f), new Vector2(-18f, 0f));
            progressText = TextBlock(panel.transform, "MISSION PROGRESS     0 / 5", 12, Muted, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(progressText.rectTransform, new Vector2(0f, 0.04f), new Vector2(0.9f, 0.13f), new Vector2(22f, 0f), Vector2.zero);
            GameObject track = Panel("ProgressTrack", panel.transform, new Color(0.18f, 0.25f, 0.33f, 1f), new Vector2(0f, 0f), new Vector2(0.9f, 0f), new Vector2(22f, 12f), new Vector2(-16f, 19f));
            progressFill = Panel("ProgressFill", track.transform, Cyan, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
        }

        private void BuildBottomBar()
        {
            GameObject bar = Panel("BottomBar", root, new Color(0.025f, 0.055f, 0.10f, 0.85f), new Vector2(0.02f, 0f), new Vector2(0.98f, 0f), new Vector2(0f, 18f), new Vector2(0f, 65f));
            Text controls = TextBlock(bar.transform, "WASD / LEFT STICK MOVE    SHIFT / LB SPRINT    SPACE / A HOP    E / X USE    MOUSE / RIGHT STICK LOOK", 12, Muted, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(controls.rectTransform, new Vector2(0f, 0f), new Vector2(0.82f, 1f), new Vector2(18f, 0f), Vector2.zero);
            Text comms = TextBlock(bar.transform, "COMMS  /  STANDBY", 13, Cyan, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(comms.rectTransform, new Vector2(0.82f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-20f, 0f));
        }

        private void BuildFacilityPanel()
        {
            facilityPanel = Panel("FacilityPrompt", root, Navy, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 82f), new Vector2(350f, 122f));
            Text eyebrow = TextBlock(facilityPanel.transform, "FACILITY IN RANGE", 12, Cyan, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(eyebrow.rectTransform, new Vector2(0f, 0.7f), new Vector2(1f, 1f), new Vector2(18f, 0f), new Vector2(-15f, 0f));
            facilityText = TextBlock(facilityPanel.transform, "", 15, Color.white, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(facilityText.rectTransform, new Vector2(0f, 0.1f), new Vector2(1f, 0.72f), new Vector2(18f, 0f), new Vector2(-15f, 0f));
            facilityPanel.SetActive(false);
        }

        private void BuildMessages()
        {
            GameObject banner = Panel("MessageBanner", root, new Color(0.02f, 0.06f, 0.10f, 0.88f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(700f, 46f));
            banner.GetComponent<Image>().raycastTarget = false;
            messageText = TextBlock(banner.transform, "", 16, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(messageText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 2f), new Vector2(-12f, -2f));
            banner.SetActive(false);
        }

        private void BuildEventPanel()
        {
            eventPanel = Panel("PowerEvent", root, new Color(0.025f, 0.055f, 0.10f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 460f));
            Text kicker = TextBlock(eventPanel.transform, "!   SOLAR-PARTICLE DRILL  /  TRAINING SCENARIO", 14, new Color(1f, 0.68f, 0.30f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(kicker.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), new Vector2(28f, 0f), new Vector2(-28f, 0f));
            Text title = TextBlock(eventPanel.transform, "Prepare for a solar-particle event", 29, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 0.69f), new Vector2(1f, 0.85f), new Vector2(28f, 0f), new Vector2(-28f, 0f));
            eventCopyText = TextBlock(eventPanel.transform, "This is a practice scenario, not a live warning. DONKI records solar events; it does not give this lunar outpost's radiation dose.", 16, Muted, FontStyle.Normal, TextAnchor.UpperLeft);
            eventCopyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(eventCopyText.rectTransform, new Vector2(0f, 0.48f), new Vector2(1f, 0.68f), new Vector2(28f, 0f), new Vector2(-28f, 0f));
            MakeButton(eventPanel.transform, "SHELTER CREW  ·  SPEND POWER, LOWER SIMULATED EXPOSURE", () => game?.ResolveRadiationDrill(true),
                new Vector2(0.06f, 0.29f), new Vector2(0.94f, 0.43f), new Color(0.03f, 0.42f, 0.47f), 14);
            MakeButton(eventPanel.transform, "KEEP OUTSIDE OPERATIONS  ·  KEEP POWER, ACCEPT MORE RISK", () => game?.ResolveRadiationDrill(false),
                new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.26f), new Color(0.15f, 0.21f, 0.29f), 14);
            Text note = TextBlock(eventPanel.transform, "FICTIONAL EXPOSURE INDEX  ·  NOT A DOSE OR MEDICAL MEASUREMENT", 11, Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(note.rectTransform, new Vector2(0f, 0.015f), new Vector2(1f, 0.10f), Vector2.zero, Vector2.zero);
            eventPanel.SetActive(false);
        }

        private void BuildOverlayPanels()
        {
            introPanel = CreateFullscreenDim("MissionBriefing");
            Text overline = TextBlock(introPanel.transform, "JUNIOR ASTRONAUT MISSION TRAINER", 14, Cyan, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(overline.rectTransform, new Vector2(0.29f, 0.75f), new Vector2(0.71f, 0.81f), Vector2.zero, Vector2.zero);
            Text title = TextBlock(introPanel.transform, "ASTRA  /  FIRST LIGHT", 45, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.29f, 0.62f), new Vector2(0.71f, 0.76f), Vector2.zero, Vector2.zero);
            Text description = TextBlock(introPanel.transform, "Your crew has arrived at Shackleton Outpost. Bring communications and solar power online, inspect life support, then make a call when the array output drops.", 19, Muted, FontStyle.Normal, TextAnchor.UpperLeft);
            if (PlayerPrefs.HasKey("ASTRA_TUTORIAL_STATUS")) description.text += "\n\nWelcome back — choose the guide or jump straight in.";
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(description.rectTransform, new Vector2(0.29f, 0.46f), new Vector2(0.71f, 0.60f), Vector2.zero, Vector2.zero);
            MakeButton(introPanel.transform, "SHOW ME HOW TO PLAY", () => ShowTutorialStep(0), new Vector2(0.29f, 0.32f), new Vector2(0.51f, 0.42f), new Color(0.02f, 0.45f, 0.52f), 15);
            MakeButton(introPanel.transform, "I KNOW HOW TO PLAY — SKIP GUIDE", SkipTutorial, new Vector2(0.52f, 0.32f), new Vector2(0.75f, 0.42f), new Color(0.12f, 0.20f, 0.28f), 12);
            Text note = TextBlock(introPanel.transform, "KEYBOARD: WASD move · Shift sprint · Space hop · E use · right-drag look\nGAMEPAD: left stick move · right stick look · LB sprint · A hop · X use · Start pause\nJOYSTICK: main stick move · twist look · trigger use", 11, Muted, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(note.rectTransform, new Vector2(0.29f, 0.18f), new Vector2(0.71f, 0.28f), Vector2.zero, Vector2.zero);

            tutorialPanel = CreateFullscreenDim("QuickPlayGuide");
            Text guideKicker = TextBlock(tutorialPanel.transform, "ASTRA  /  QUICK GUIDE", 14, Cyan, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(guideKicker.rectTransform, new Vector2(0.29f, 0.75f), new Vector2(0.53f, 0.81f), Vector2.zero, Vector2.zero);
            tutorialCounterText = TextBlock(tutorialPanel.transform, "STEP 1 OF 3", 13, Muted, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(tutorialCounterText.rectTransform, new Vector2(0.55f, 0.75f), new Vector2(0.71f, 0.81f), Vector2.zero, Vector2.zero);
            tutorialTitleText = TextBlock(tutorialPanel.transform, "", 34, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(tutorialTitleText.rectTransform, new Vector2(0.29f, 0.59f), new Vector2(0.71f, 0.72f), Vector2.zero, Vector2.zero);
            tutorialBodyText = TextBlock(tutorialPanel.transform, "", 19, Muted, FontStyle.Normal, TextAnchor.UpperLeft);
            tutorialBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(tutorialBodyText.rectTransform, new Vector2(0.29f, 0.43f), new Vector2(0.71f, 0.57f), Vector2.zero, Vector2.zero);
            tutorialKeysText = TextBlock(tutorialPanel.transform, "", 15, Cyan, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(tutorialKeysText.rectTransform, new Vector2(0.29f, 0.32f), new Vector2(0.71f, 0.40f), Vector2.zero, Vector2.zero);
            MakeButton(tutorialPanel.transform, "BACK", () => ShowTutorialStep(tutorialStep - 1), new Vector2(0.29f, 0.20f), new Vector2(0.40f, 0.29f), new Color(0.12f, 0.20f, 0.28f), 14);
            MakeButton(tutorialPanel.transform, "SKIP GUIDE", SkipTutorial, new Vector2(0.42f, 0.20f), new Vector2(0.57f, 0.29f), new Color(0.12f, 0.20f, 0.28f), 13);
            MakeButton(tutorialPanel.transform, "NEXT  →", AdvanceTutorial, new Vector2(0.59f, 0.20f), new Vector2(0.71f, 0.29f), new Color(0.02f, 0.45f, 0.52f), 14);
            tutorialPanel.SetActive(false);

            pausePanel = CreateFullscreenDim("Paused");
            Text paused = TextBlock(pausePanel.transform, "MISSION PAUSED", 38, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(paused.rectTransform, new Vector2(0.35f, 0.57f), new Vector2(0.65f, 0.69f), Vector2.zero, Vector2.zero);
            MakeButton(pausePanel.transform, "RESUME MISSION", () => game?.TogglePause(), new Vector2(0.39f, 0.43f), new Vector2(0.61f, 0.52f), new Color(0.02f, 0.45f, 0.52f), 16);
            MakeButton(pausePanel.transform, "RESTART", () => game?.RestartMission(), new Vector2(0.39f, 0.31f), new Vector2(0.61f, 0.40f), new Color(0.15f, 0.21f, 0.29f), 15);
            MakeButton(pausePanel.transform, "ACCESSIBILITY SETTINGS", () => OnboardingManager.Instance?.ShowSettingsFromPause(), new Vector2(0.39f, 0.19f), new Vector2(0.61f, 0.28f), new Color(0.04f, 0.30f, 0.40f), 15);

            reportPanel = CreateFullscreenDim("MissionReport");
            Text reportKicker = TextBlock(reportPanel.transform, "ASTRA  /  FLIGHT DEBRIEF", 14, Cyan, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(reportKicker.rectTransform, new Vector2(0.29f, 0.76f), new Vector2(0.71f, 0.82f), Vector2.zero, Vector2.zero);
            reportTitleText = TextBlock(reportPanel.transform, "Mission complete", 38, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(reportTitleText.rectTransform, new Vector2(0.29f, 0.65f), new Vector2(0.71f, 0.77f), Vector2.zero, Vector2.zero);
            reportScoreText = TextBlock(reportPanel.transform, "MISSION SCORE   000", 20, new Color(1f, 0.8f, 0.36f), FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(reportScoreText.rectTransform, new Vector2(0.29f, 0.54f), new Vector2(0.71f, 0.64f), Vector2.zero, Vector2.zero);
            reportBodyText = TextBlock(reportPanel.transform, "", 17, Muted, FontStyle.Normal, TextAnchor.UpperLeft);
            reportBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(reportBodyText.rectTransform, new Vector2(0.29f, 0.39f), new Vector2(0.71f, 0.52f), Vector2.zero, Vector2.zero);
            reportLearningText = TextBlock(reportPanel.transform, "WHAT YOU LEARNED\n", 14, Color.white, FontStyle.Bold, TextAnchor.UpperLeft);
            reportLearningText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(reportLearningText.rectTransform, new Vector2(0.29f, 0.21f), new Vector2(0.71f, 0.39f), Vector2.zero, Vector2.zero);
            MakeButton(reportPanel.transform, "REPLAY MISSION    ↻", () => game?.RestartMission(), new Vector2(0.29f, 0.08f), new Vector2(0.49f, 0.16f), new Color(0.02f, 0.45f, 0.52f), 15);
            MakeButton(reportPanel.transform, "MISSION BOARD", ReturnToMissionBoard, new Vector2(0.51f, 0.08f), new Vector2(0.71f, 0.16f), new Color(0.04f, 0.30f, 0.40f), 15);
            reportPanel.SetActive(false);
            pausePanel.SetActive(false);
        }

        private void BuildBriefingPanel()
        {
            briefingPanel = CreateFullscreenDim("BriefingDetails");
            Text title = TextBlock(briefingPanel.transform, "SCIENCE  /  CHALLENGE DATA", 32, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.28f, 0.65f), new Vector2(0.72f, 0.77f), Vector2.zero, Vector2.zero);
            briefingBodyText = TextBlock(briefingPanel.transform, "Mission learning note appears here.", 17, Muted, FontStyle.Normal, TextAnchor.UpperLeft);
            briefingBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(briefingBodyText.rectTransform, new Vector2(0.28f, 0.43f), new Vector2(0.72f, 0.66f), Vector2.zero, Vector2.zero);
            scienceDataText = TextBlock(briefingPanel.transform, "Loading DONKI context…", 15, Cyan, FontStyle.Bold, TextAnchor.UpperLeft);
            scienceDataText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(scienceDataText.rectTransform, new Vector2(0.28f, 0.34f), new Vector2(0.72f, 0.42f), Vector2.zero, Vector2.zero);
            MakeButton(briefingPanel.transform, "OPEN MISSION SOURCE", OpenMissionSource, new Vector2(0.28f, 0.23f), new Vector2(0.46f, 0.31f), new Color(0.04f, 0.30f, 0.40f), 12);
            MakeButton(briefingPanel.transform, "OPEN CHALLENGE PAGE", () => Application.OpenURL("https://www.spaceappschallenge.org/2026/challenges/junior-astronaut-mission-trainer/?tab=resources"), new Vector2(0.47f, 0.23f), new Vector2(0.64f, 0.31f), new Color(0.04f, 0.30f, 0.40f), 12);
            MakeButton(briefingPanel.transform, "BACK TO MISSION", () => briefingPanel.SetActive(false), new Vector2(0.60f, 0.23f), new Vector2(0.72f, 0.31f), new Color(0.02f, 0.45f, 0.52f), 12);
            MakeButton(briefingPanel.transform, "REPLAY GUIDE", () => ShowTutorialStep(0), new Vector2(0.40f, 0.11f), new Vector2(0.60f, 0.19f), new Color(0.12f, 0.20f, 0.28f), 12);
            briefingPanel.SetActive(false);
        }

        private void ShowTutorialStep(int step)
        {
            if (step == 0 && !resumeMissionAfterTutorial && game != null && game.CanControlPlayer)
            {
                resumeMissionAfterTutorial = true;
                game.TogglePause();
                pausePanel.SetActive(false);
            }
            tutorialStep = Mathf.Clamp(step, 0, TutorialTitles.Length - 1);
            tutorialTitleText.text = TutorialTitles[tutorialStep];
            tutorialBodyText.text = TutorialBodies[tutorialStep];
            tutorialKeysText.text = TutorialKeys[tutorialStep];
            tutorialCounterText.text = "STEP " + (tutorialStep + 1) + " OF " + TutorialTitles.Length;
            Text[] labels = tutorialPanel.GetComponentsInChildren<Text>(true);
            foreach (Text label in labels)
                if (label.text == "NEXT  →" || label.text == "I’M READY — START")
                    label.text = tutorialStep == TutorialTitles.Length - 1 ? "I’M READY — START" : "NEXT  →";
            introPanel.SetActive(false);
            briefingPanel?.SetActive(false);
            tutorialPanel.SetActive(true);
        }

        private void RecordTutorial(string status)
        {
            PlayerPrefs.SetString("ASTRA_TUTORIAL_STATUS", status);
            PlayerPrefs.Save();
        }

        private void SkipTutorial()
        {
            RecordTutorial("skipped");
            tutorialPanel.SetActive(false);
            introPanel.SetActive(false);
            FinishTutorialAndContinue();
        }

        private void AdvanceTutorial()
        {
            if (tutorialStep < TutorialTitles.Length - 1) ShowTutorialStep(tutorialStep + 1);
            else
            {
                RecordTutorial("completed");
                tutorialPanel.SetActive(false);
                FinishTutorialAndContinue();
            }
        }

        private void FinishTutorialAndContinue()
        {
            if (resumeMissionAfterTutorial)
            {
                resumeMissionAfterTutorial = false;
                game?.TogglePause();
            }
            else if (game != null) game.BeginMission(game.CurrentMission);
        }

        private GameObject CreateFullscreenDim(string name)
        {
            GameObject panel = Panel(name, root, new Color(0.008f, 0.022f, 0.045f, 0.91f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return panel;
        }

        private GameObject Panel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            if (anchorMin == anchorMax)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = anchorMin;
                rt.anchoredPosition = offsetMin;
                rt.sizeDelta = offsetMax;
            }
            else SetRect(rt, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            basePanelColors[image] = color;
            return panel;
        }

        private Text TextBlock(Transform parent, string content, int size, Color color, FontStyle style, TextAnchor align)
        {
            GameObject item = new GameObject("Text", typeof(RectTransform), typeof(Text));
            item.transform.SetParent(parent, false);
            Text text = item.GetComponent<Text>();
            text.font = font;
            text.text = content;
            // Preserve each element's designed hierarchy. A large global minimum made compact
            // labels expand beyond their slots and overlap neighboring HUD content.
            text.fontSize = Mathf.Max(size, 15);
            baseTextSizes[text] = text.fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private void MakeButton(Transform parent, string caption, Action action, Vector2 anchorMin, Vector2 anchorMax, Color color, int size)
        {
            GameObject buttonObject = Panel("Button_" + caption, parent, color, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            Text text = TextBlock(buttonObject.transform, caption, size, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, -2f));
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public void Refresh(AstraGameManager state)
        {
            if (state == null || !built) return;
            int missionNumber = (int)state.CurrentMission + 1;
            missionCodeText.text = "ARTEMIS OUTPOST  •  MISSION " + missionNumber.ToString("00") + " / " + AstraMissionCatalog.Count.ToString("00");
            missionTitleText.text = state.MissionDefinition.Title;
            missionDescriptionText.text = state.MissionDefinition.Subtitle;
            clockText.text = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(state.missionSeconds / 60f), Mathf.FloorToInt(state.missionSeconds % 60f));
            statusText.text = "OUTPOST  /  " + state.GetOutpostStatus() + "   ·   HAB " + Mathf.RoundToInt(state.habitatCondition) + "%";
            UpdateResource(oxygenText, oxygenFill, state.oxygen);
            UpdateResource(waterText, waterFill, state.water);
            UpdateResource(foodText, foodFill, state.food);
            UpdateResource(powerText, powerFill, state.power);
            if (briefingBodyText != null) briefingBodyText.text = state.MissionDefinition.Lesson;
            if (scienceDataText != null)
            {
                if (state.CurrentMission == AstraMissionId.AirAndWater)
                    scienceDataText.text = "NASA's ISS reference reports about 90% water recovery. This is not a lunar outpost specification; ASTRA values are fictional.";
                else if (state.CurrentMission == AstraMissionId.FirstLight || state.CurrentMission == AstraMissionId.StormWatch)
                    scienceDataText.text = state.DonkiSummary + "\nASTRA simulated exposure index: " + Mathf.RoundToInt(state.radiationExposureIndex) + "/100 (fictional; not dose).";
                else
                    scienceDataText.text = state.MissionDefinition.SourceLabel + " is the reference for this mission. All local resource amounts are simplified game values.";
            }
            string resourceWarning = state.Resources != null ? state.Resources.GetWarning() : string.Empty;
            bool showResourceWarning = state.missionStarted && !state.missionFinished && !string.IsNullOrEmpty(resourceWarning);
            resourceAlertPanel.SetActive(showResourceWarning);
            if (showResourceWarning)
            {
                bool critical = resourceWarning.StartsWith("CRITICAL") || resourceWarning.StartsWith("HABITAT CONDITION LOW");
                resourceAlertPanel.GetComponent<Image>().color = critical
                    ? new Color(0.36f, 0.07f, 0.06f, 0.97f)
                    : new Color(0.34f, 0.20f, 0.05f, 0.96f);
                resourceAlertText.color = critical ? new Color(1f, 0.72f, 0.68f) : new Color(1f, 0.86f, 0.60f);
                resourceAlertText.text = (critical ? "!  " : "!  RESOURCE ADVISORY  ·  ") + resourceWarning;
            }
            StringBuilder list = new StringBuilder();
            int complete = 0;
            bool noRead = PlayerSettingsManager.Instance != null && PlayerSettingsManager.Instance.NoReadingMode;
            string[] objectiveIcons = { "◉", "☀", "◎", "ϟ", "◉" };
            for (int i = 0; i < state.Objectives.Count; i++)
            {
                MissionObjective objective = state.Objectives[i];
                if (objective.complete) complete++;
                list.Append(objective.complete ? "<color=#5ce3bd>✓</color>  " : "<color=#7e9aad>○</color>  ");
                list.Append(noRead ? objectiveIcons[Mathf.Min(i, objectiveIcons.Length - 1)] : objective.title);
                if (i < state.Objectives.Count - 1) list.Append('\n');
            }
            objectiveText.supportRichText = true;
            objectiveText.text = list.ToString();
            progressText.text = "MISSION PROGRESS     " + complete + " / " + state.Objectives.Count;
            progressFill.fillAmount = state.Objectives.Count == 0 ? 0f : (float)complete / state.Objectives.Count;
        }

        private void OpenMissionSource()
        {
            if (game != null && !string.IsNullOrEmpty(game.MissionDefinition.SourceUrl))
                Application.OpenURL(game.MissionDefinition.SourceUrl);
        }

        private void UpdateResource(Text value, Image fill, float amount)
        {
            value.text = Mathf.RoundToInt(amount) + "%";
            fill.fillAmount = Mathf.Clamp01(amount / 100f);
            fill.color = amount < 15f ? new Color(1f, 0.25f, 0.24f) : amount < 35f ? new Color(1f, 0.59f, 0.19f) : amount < 55f ? new Color(1f, 0.82f, 0.34f) : Cyan;
        }

        private void UpdateFacilityPrompt()
        {
            AstraPlayerController player = FindFirstObjectByType<AstraPlayerController>();
            AstraInteractable nearest = player != null ? player.GetNearestFacility() : null;
            facilityPanel.SetActive(game != null && game.CanControlPlayer && nearest != null);
            if (nearest != null)
                facilityText.text = nearest.facilityName + "\n<color=#9db2c3>Press <b>E / X / trigger</b> to interact</color>";
            facilityText.supportRichText = true;
        }

        public void SetIntroVisible(bool visible) => introPanel.SetActive(visible);
        public void SetPauseVisible(bool visible) => pausePanel.SetActive(visible);
        public void SetEventVisible(bool visible)
        {
            eventPanel.SetActive(visible);
            if (visible && eventCopyText != null && game != null)
            {
                string shelterCost = game.shelterPrepared ? "6" : "18";
                eventCopyText.text = "Training scenario only; this is not a live warning. NASA DONKI solar-particle records give event context, not this lunar crew's dose. Shelter costs " + shelterCost + " game power and adds less to ASTRA's fictional exposure index. Staying outside keeps power but adds more to that index.";
            }
        }
        public void SetMissionHudVisible(bool visible) { if (canvas != null) canvas.gameObject.SetActive(visible); }

        public void ShowMessage(string message)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            StopCoroutine(nameof(HideMessage));
            StartCoroutine(HideMessage());
        }

        private IEnumerator HideMessage()
        {
            yield return new WaitForSecondsRealtime(3.5f);
            if (messageText != null) messageText.gameObject.SetActive(false);
        }

        public void ShowReport(int score, bool success, string learning, AstraMissionDefinition definition, int bestScore, bool nextUnlocked)
        {
            reportTitleText.text = success ? definition.Title + " complete" : "Mission ended early";
            reportScoreText.text = "MISSION SCORE   " + score.ToString("000") + "     ·     BEST " + Mathf.Max(score, bestScore).ToString("000");
            reportBodyText.text = success
                ? (nextUnlocked ? "Mission cleared. The next campaign mission is now unlocked." : "Campaign complete. You can replay any mission from the board.")
                : "Critical reserves were exhausted before this mission's goals were complete. Review your choices and try again.";
            reportLearningText.text = "WHAT YOU LEARNED\n" + learning;
            reportPanel.SetActive(true);
        }

        public void HideReport()
        {
            if (reportPanel != null) reportPanel.SetActive(false);
        }

        private void ReturnToMissionBoard()
        {
            reportPanel.SetActive(false);
            OnboardingManager.Instance?.ShowMissionSelect();
        }

        public void SetCursorForMission(bool missionActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ApplyAccessibilitySettings()
        {
            if (baseTextSizes.Count == 0) return;
            PlayerSettingsManager settings = PlayerSettingsManager.Instance;
            float scale = settings != null ? settings.TextScale : 1f;
            foreach (var pair in baseTextSizes)
                if (pair.Key != null) pair.Key.fontSize = Mathf.Clamp(Mathf.RoundToInt(pair.Value * scale), 14, 54);
            bool high = settings != null && settings.HighContrast;
            foreach (var pair in basePanelColors)
                if (pair.Key != null)
                    pair.Key.color = high && pair.Value.a > 0.72f
                        ? new Color(0f, 0f, 0f, pair.Value.a)
                        : pair.Value;
        }
    }
}
