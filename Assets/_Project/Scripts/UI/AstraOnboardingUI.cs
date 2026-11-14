using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astra
{
    /// <summary>Large, high-contrast onboarding and profile screens built for keyboard and pointer input.</summary>
    public sealed class AstraOnboardingUI : MonoBehaviour
    {
        private static readonly Color Deep = new Color(0.015f, 0.035f, 0.075f, 0.98f);
        private static readonly Color Card = new Color(0.035f, 0.09f, 0.15f, 0.98f);
        private static readonly Color Blue = new Color(0.04f, 0.38f, 0.52f, 1f);
        private static readonly Color Teal = new Color(0.08f, 0.72f, 0.70f, 1f);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.28f, 1f);
        private static readonly Color White = Color.white;
        private static readonly Color Soft = new Color(0.77f, 0.86f, 0.94f, 1f);

        public string CurrentScreen { get; private set; } = "welcome";
        private Canvas canvas;
        private Font font;
        private readonly Dictionary<Text, int> baseSizes = new Dictionary<Text, int>();
        private readonly Dictionary<AstraBinding, Text> bindingLabels = new Dictionary<AstraBinding, Text>();
        private readonly Dictionary<string, Text> settingLabelTexts = new Dictionary<string, Text>();
        private readonly List<Image> cards = new List<Image>();
        private GameObject welcomePanel, settingsPanel, controlsPanel, tutorialPanel, equipmentPanel, completePanel, missionPanel;
        private readonly Button[] missionButtons = new Button[AstraMissionCatalog.Count];
        private readonly Text[] missionButtonLabels = new Text[AstraMissionCatalog.Count];
        private Text missionDetailTitle, missionDetailSummary, missionDetailStatus, missionBriefingText;
        private Button missionSourceButton, launchSelectedMissionButton;
        private Text welcomeTitle, welcomeBody, setupLabel;
        private Text languageValue, readingValue, assistanceValue, noReadingValue, textSizeValue, subtitlesValue, contrastValue, volumeValue, clickMoveValue;
        private Text controlsStatus;
        private Text tutorialStepText, tutorialTitle, tutorialBody, tutorialIcon, tutorialSpeech, tutorialHint, tutorialNextLabel;
        private Text equipmentIcon, equipmentBody, completeTitle, completeSpeech;
        private Image completeBadge;
        private float completeTime;
        private int equipmentIndex;
        private string settingsReturn = "welcome";

        private void Awake()
        {
            Build();
        }

        private void OnEnable()
        {
            if (VoiceGuideManager.Instance != null) VoiceGuideManager.Instance.LineChanged += SetSpeech;
            if (PlayerSettingsManager.Instance != null) PlayerSettingsManager.Instance.RebindFinished += SetControlsStatus;
        }

        private void OnDisable()
        {
            if (VoiceGuideManager.Instance != null) VoiceGuideManager.Instance.LineChanged -= SetSpeech;
            if (PlayerSettingsManager.Instance != null) PlayerSettingsManager.Instance.RebindFinished -= SetControlsStatus;
        }

        private void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            GameObject canvasObject = new GameObject("ASTRA Inclusive Onboarding", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Match the gameplay HUD's 720p reference so the mission board, settings,
            // and tutorial keep the same readable size and spacing as the game view.
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasObject.transform;

            BuildWelcome(root);
            BuildSettings(root);
            BuildControls(root);
            BuildTutorial(root);
            BuildComplete(root);
            BuildMissionSelect(root);
            HideAll();
            ApplyAccessibilitySettings();
        }

        private void BuildWelcome(Transform root)
        {
            welcomePanel = FullPanel("Welcome", root, Deep);
            MakeText(welcomePanel.transform, "ASTRA  /  JUNIOR ASTRONAUT TRAINER", 18, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.12f, 0.86f), new Vector2(0.64f, 0.94f));
            welcomeTitle = MakeText(welcomePanel.transform, "HELLO, ASTRONAUT!", 32, White, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.12f, 0.72f), new Vector2(0.72f, 0.85f));
            welcomeBody = MakeText(welcomePanel.transform, "ASTRO is your space buddy. Learn one step at a time. Replay or skip practice whenever you like.", 22, Soft, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(0.12f, 0.51f), new Vector2(0.56f, 0.70f));
            welcomeBody.horizontalOverflow = HorizontalWrapMode.Wrap;

            // A simple, friendly robot portrait built from UI shapes so no external art is required.
            Image halo = MakeImage(welcomePanel.transform, new Color(0.05f, 0.33f, 0.48f, 0.92f), new Vector2(0.72f, 0.49f), new Vector2(0.94f, 0.85f));
            halo.rectTransform.sizeDelta = new Vector2(230, 230);
            halo.rectTransform.anchoredPosition = new Vector2(-15, 0);
            Image face = MakeImage(welcomePanel.transform, new Color(0.56f, 0.85f, 0.92f), new Vector2(0.765f, 0.58f), new Vector2(0.91f, 0.79f));
            face.rectTransform.sizeDelta = new Vector2(156, 150);
            Text eyes = MakeText(welcomePanel.transform, "●      ●\n\n     ◡", 25, new Color(0.02f, 0.17f, 0.28f), FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.775f, 0.59f), new Vector2(0.90f, 0.78f));
            eyes.lineSpacing = 1.2f;
            MakeText(welcomePanel.transform, "ASTRO", 18, Gold, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.76f, 0.49f), new Vector2(0.91f, 0.55f));

            Image symbols = MakeImage(welcomePanel.transform, new Color(0.035f, 0.09f, 0.15f, 1f), new Vector2(0.12f, 0.34f), new Vector2(0.66f, 0.47f));
            Text iconRow = MakeText(symbols.transform, "O₂     H₂O     ☀     FOOD     RADIO", 21, Gold, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            SetRect(iconRow.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));

            setupLabel = MakeText(welcomePanel.transform, "", 15, Soft, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.26f), new Vector2(0.66f, 0.32f));
            MakeButton(welcomePanel.transform, "LET'S GET READY", () => OnboardingManager.Instance?.BeginSetup(),
                new Vector2(0.12f, 0.14f), new Vector2(0.40f, 0.24f), Teal, 22);
            MakeButton(welcomePanel.transform, "I KNOW HOW TO PLAY — SKIP TUTORIAL", () => OnboardingManager.Instance?.SkipTutorial(),
                new Vector2(0.42f, 0.14f), new Vector2(0.70f, 0.24f), Blue, 17);
            MakeButton(welcomePanel.transform, "⚙  ACCESSIBILITY SETTINGS", () => OnboardingManager.Instance?.OpenSettings(),
                new Vector2(0.72f, 0.12f), new Vector2(0.94f, 0.22f), new Color(0.13f, 0.20f, 0.29f), 17);
        }

        private void BuildSettings(Transform root)
        {
            settingsPanel = FullPanel("Accessibility Setup", root, Deep);
            GameObject card = MakePanel(settingsPanel.transform, Card, new Vector2(0.12f, 0.055f), new Vector2(0.88f, 0.95f));
            cards.Add(card.GetComponent<Image>());
            MakeText(card.transform, "ASTRO  /  ACCESSIBILITY", 16, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.90f), new Vector2(0.70f, 0.98f));
            MakeText(card.transform, "MAKE ASTRA FIT YOU", 37, White, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.81f), new Vector2(0.86f, 0.91f));
            MakeText(card.transform, "Pick a few things that help. You can change them whenever you like.", 18, Soft, FontStyle.Normal, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.76f), new Vector2(0.90f, 0.82f));

            languageValue = AddChoiceRow(card.transform, "LANGUAGE", 0.68f, CycleLanguage);
            readingValue = AddChoiceRow(card.transform, "HOW ASTRO HELPS", 0.605f, CycleReadingMode);
            assistanceValue = AddChoiceRow(card.transform, "HELP LEVEL", 0.53f, CycleAssistance);
            noReadingValue = AddChoiceRow(card.transform, "NO-READING MODE", 0.455f, ToggleNoReading);
            textSizeValue = AddChoiceRow(card.transform, "TEXT SIZE", 0.38f, () => AdjustText(0.1f));
            subtitlesValue = AddChoiceRow(card.transform, "SUBTITLES", 0.305f, ToggleSubtitles);
            contrastValue = AddChoiceRow(card.transform, "HIGH CONTRAST", 0.23f, ToggleContrast);
            volumeValue = AddChoiceRow(card.transform, "VOICE VOLUME", 0.155f, () => AdjustVolume(0.1f));
            clickMoveValue = AddChoiceRow(card.transform, "CLICK-TO-WALK", 0.08f, ToggleClickMove);
            MakeButton(card.transform, "KEYBOARD / GAMEPAD CONTROLS", () => ShowControls(), new Vector2(0.04f, -0.035f), new Vector2(0.46f, 0.035f), new Color(0.13f, 0.20f, 0.29f), 14);
            MakeButton(card.transform, "SAVE AND CONTINUE", () => OnboardingManager.Instance?.CloseSettings(), new Vector2(0.54f, -0.035f), new Vector2(0.96f, 0.035f), Teal, 18);
            UpdateSettingsValues();
        }

        private void BuildControls(Transform root)
        {
            controlsPanel = FullPanel("Keyboard & Gamepad Controls", root, Deep);
            GameObject card = MakePanel(controlsPanel.transform, Card, new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f));
            cards.Add(card.GetComponent<Image>());
            MakeText(card.transform, "ASTRO  /  CHANGE CONTROLS", 16, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.96f));
            MakeText(card.transform, "Change keyboard keys below. Gamepad and joystick controls work automatically.\nGamepad: LS move · RS look · LB sprint · A hop · X use · Start pause.\nJoystick: main stick move · twist look · trigger use.", 16, White, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.70f), new Vector2(0.94f, 0.86f));
            controlsStatus = MakeText(card.transform, "W A S D and E are ready.", 17, Gold, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.07f), new Vector2(0.94f, 0.17f));
            AddBindingRow(card.transform, "WALK FORWARD", AstraBinding.MoveForward, 0.62f);
            AddBindingRow(card.transform, "WALK BACK", AstraBinding.MoveBack, 0.51f);
            AddBindingRow(card.transform, "WALK LEFT", AstraBinding.MoveLeft, 0.40f);
            AddBindingRow(card.transform, "WALK RIGHT", AstraBinding.MoveRight, 0.29f);
            AddBindingRow(card.transform, "USE / INTERACT", AstraBinding.Interact, 0.18f);
            MakeButton(card.transform, "BACK TO ACCESSIBILITY", () => ShowSettings(settingsReturn), new Vector2(0.60f, 0.01f), new Vector2(0.94f, 0.10f), Blue, 15);
        }

        private void BuildTutorial(Transform root)
        {
            tutorialPanel = FullPanel("ASTRO Practice Guide", root, new Color(0.005f, 0.018f, 0.04f, 0.72f));
            // Keep the world clickable outside the tutorial card so click-to-walk still works during practice.
            tutorialPanel.GetComponent<Image>().raycastTarget = false;
            GameObject card = MakePanel(tutorialPanel.transform, new Color(0.018f, 0.055f, 0.10f, 0.96f), new Vector2(0.025f, 0.11f), new Vector2(0.40f, 0.91f));
            cards.Add(card.GetComponent<Image>());
            tutorialStepText = MakeText(card.transform, "ACTIVITY 1 / 6", 16, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.07f, 0.88f), new Vector2(0.93f, 0.97f));
            tutorialTitle = MakeText(card.transform, "MEET YOUR ASTRONAUT", 20, White, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.07f, 0.76f), new Vector2(0.93f, 0.88f));
            tutorialTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            tutorialBody = MakeText(card.transform, "Take your time. Every try helps you learn.", 18, Soft, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(0.07f, 0.48f), new Vector2(0.93f, 0.75f));
            tutorialBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            tutorialIcon = MakeText(tutorialPanel.transform, "◉   ASTRO\n\n       ↓", 58, Gold, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.48f, 0.36f), new Vector2(0.92f, 0.77f));
            tutorialIcon.lineSpacing = 1.25f;
            tutorialSpeech = MakeText(tutorialPanel.transform, "", 18, White, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.45f, 0.79f), new Vector2(0.93f, 0.90f));
            tutorialHint = MakeText(card.transform, "", 16, Gold, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.07f, 0.405f), new Vector2(0.93f, 0.47f));

            MakeButton(card.transform, "← BACK", () => OnboardingManager.Instance?.PreviousTutorialStep(), new Vector2(0.07f, 0.24f), new Vector2(0.34f, 0.34f), new Color(0.12f, 0.20f, 0.29f), 15);
            MakeButton(card.transform, "REPEAT ASTRO", () => OnboardingManager.Instance?.RepeatCurrentStep(), new Vector2(0.38f, 0.24f), new Vector2(0.93f, 0.34f), Blue, 15);
            MakeButton(card.transform, "SKIP TUTORIAL", () => OnboardingManager.Instance?.SkipTutorial(), new Vector2(0.07f, 0.10f), new Vector2(0.41f, 0.20f), new Color(0.12f, 0.20f, 0.29f), 15);
            Button next = MakeButton(card.transform, "I DID IT — NEXT", () => OnboardingManager.Instance?.NextTutorialStep(), new Vector2(0.45f, 0.10f), new Vector2(0.93f, 0.20f), Teal, 16);
            tutorialNextLabel = next.GetComponentInChildren<Text>();

            equipmentPanel = MakePanel(tutorialPanel.transform, new Color(0.03f, 0.10f, 0.16f, 0.98f), new Vector2(0.48f, 0.16f), new Vector2(0.93f, 0.36f));
            Text equipmentTitle = MakeText(equipmentPanel.transform, "EQUIPMENT", 13, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.77f), new Vector2(0.96f, 0.98f));
            equipmentIcon = MakeText(equipmentPanel.transform, "O₂", 44, Gold, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.22f), new Vector2(0.22f, 0.78f));
            equipmentBody = MakeText(equipmentPanel.transform, "OXYGEN · The processor adds breathable air in our game.", 16, White, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.24f, 0.30f), new Vector2(0.95f, 0.78f));
            equipmentBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            MakeButton(equipmentPanel.transform, "BACK", () => OnboardingManager.Instance?.ShowEquipment((equipmentIndex + 4) % 5), new Vector2(0.03f, 0.02f), new Vector2(0.24f, 0.27f), Blue, 13);
            MakeButton(equipmentPanel.transform, "NEXT", () => OnboardingManager.Instance?.ShowEquipment((equipmentIndex + 1) % 5), new Vector2(0.72f, 0.02f), new Vector2(0.97f, 0.27f), Blue, 13);
            equipmentPanel.SetActive(false);
        }

        private void BuildComplete(Transform root)
        {
            completePanel = FullPanel("Practice Complete", root, Deep);
            MakeText(completePanel.transform, "ASTRO  /  PRACTICE COMPLETE", 16, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.18f, 0.82f), new Vector2(0.82f, 0.90f));
            completeBadge = MakeImage(completePanel.transform, new Color(0.05f, 0.32f, 0.39f, 1f), new Vector2(0.43f, 0.48f), new Vector2(0.57f, 0.73f));
            Text check = MakeText(completePanel.transform, "✓", 74, Gold, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.43f, 0.48f), new Vector2(0.57f, 0.73f));
            completeTitle = MakeText(completePanel.transform, "GREAT WORK!", 38, White, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.19f, 0.37f), new Vector2(0.81f, 0.47f));
            completeSpeech = MakeText(completePanel.transform, "Great work, astronaut! You're ready for your first mission. Let's head to the Moon!", 22, Soft, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.19f, 0.26f), new Vector2(0.81f, 0.37f));
            completeSpeech.horizontalOverflow = HorizontalWrapMode.Wrap;
            MakeButton(completePanel.transform, "CHOOSE YOUR MISSION", () => OnboardingManager.Instance?.CloseCompletionToMissionSelect(),
                new Vector2(0.32f, 0.12f), new Vector2(0.68f, 0.22f), Teal, 20);
            completeTime = 0f;
        }

        private void BuildMissionSelect(Transform root)
        {
            missionPanel = FullPanel("Mission Selection", root, Deep);
            MakeText(missionPanel.transform, "ASTRA  /  MISSION CONTROL", 17, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.10f, 0.87f), new Vector2(0.90f, 0.94f));
            MakeText(missionPanel.transform, "MISSION BOARD", 38, White, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.10f, 0.76f), new Vector2(0.90f, 0.86f));

            GameObject listPanel = MakePanel(missionPanel.transform, Card, new Vector2(0.10f, 0.24f), new Vector2(0.39f, 0.72f));
            cards.Add(listPanel.GetComponent<Image>());
            MakeText(listPanel.transform, "CAMPAIGN MISSIONS", 13, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.98f));
            for (int i = 0; i < AstraMissionCatalog.Count; i++)
            {
                float top = 0.82f - i * 0.16f;
                float bottom = top - 0.135f;
                int index = i;
                missionButtons[i] = MakeButton(listPanel.transform, "", () => OnboardingManager.Instance?.SelectMission(index),
                    new Vector2(0.035f, bottom), new Vector2(0.965f, top), new Color(0.07f, 0.15f, 0.23f), 14);
                missionButtonLabels[i] = missionButtons[i].GetComponentInChildren<Text>();
                missionButtonLabels[i].alignment = TextAnchor.MiddleLeft;
            }

            GameObject detailPanel = MakePanel(missionPanel.transform, Card, new Vector2(0.42f, 0.24f), new Vector2(0.90f, 0.72f));
            cards.Add(detailPanel.GetComponent<Image>());
            missionDetailTitle = MakeText(detailPanel.transform, "FIRST LIGHT", 25, White, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.79f), new Vector2(0.94f, 0.96f));
            missionDetailSummary = MakeText(detailPanel.transform, "", 16, Soft, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(0.06f, 0.53f), new Vector2(0.94f, 0.78f));
            missionDetailStatus = MakeText(detailPanel.transform, "", 13, Gold, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.43f), new Vector2(0.94f, 0.53f));
            Text briefing = MakeText(detailPanel.transform, "MISSION BRIEFING", 12, Teal, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.44f));
            briefing.name = "MissionBriefingLabel";
            missionBriefingText = MakeText(detailPanel.transform, "", 14, Soft, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.36f));
            missionBriefingText.name = "MissionBriefingBody";
            missionBriefingText.horizontalOverflow = HorizontalWrapMode.Wrap;
            missionSourceButton = MakeButton(detailPanel.transform, "LEARN MORE  ↗", () => OpenSelectedMissionSource(),
                new Vector2(0.06f, 0.045f), new Vector2(0.94f, 0.14f), new Color(0.04f, 0.30f, 0.40f), 12);

            MakeButton(missionPanel.transform, "PRACTISE WITH ASTRO", () => OnboardingManager.Instance?.ReplayTutorial(),
                new Vector2(0.10f, 0.08f), new Vector2(0.34f, 0.19f), new Color(0.13f, 0.20f, 0.29f), 14);
            MakeButton(missionPanel.transform, "ACCESSIBILITY SETTINGS", () => OnboardingManager.Instance?.OpenSettings(),
                new Vector2(0.36f, 0.08f), new Vector2(0.62f, 0.19f), Blue, 14);
            launchSelectedMissionButton = MakeButton(missionPanel.transform, "LAUNCH MISSION", () => OnboardingManager.Instance?.LaunchMission(),
                new Vector2(0.64f, 0.08f), new Vector2(0.90f, 0.19f), Teal, 16);
        }

        private Text AddChoiceRow(Transform parent, string label, float y, Action action)
        {
            MakeText(parent, label, 17, Soft, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.05f, y), new Vector2(0.42f, y + 0.07f));
            Button button = MakeButton(parent, "", action, new Vector2(0.45f, y), new Vector2(0.95f, y + 0.07f), new Color(0.10f, 0.20f, 0.29f), 15);
            return button.GetComponentInChildren<Text>();
        }

        private void AddBindingRow(Transform parent, string label, AstraBinding binding, float y)
        {
            MakeText(parent, label, 16, Soft, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0.06f, y), new Vector2(0.57f, y + 0.09f));
            Text value = MakeButton(parent, "", () => PlayerSettingsManager.Instance?.BeginRebind(binding), new Vector2(0.63f, y), new Vector2(0.94f, y + 0.09f), Blue, 16)
                .GetComponentInChildren<Text>();
            value.text = PlayerSettingsManager.Instance != null ? PlayerSettingsManager.Instance.GetKeyLabel(binding) : "W";
            bindingLabels[binding] = value;
        }

        private void CycleLanguage() { var s = PlayerSettingsManager.Instance; if (s == null) return; s.SetLanguage((AstraLanguage)(((int)s.Language + 1) % 3)); UpdateSettingsValues(); }
        private void CycleReadingMode() { var s = PlayerSettingsManager.Instance; if (s == null) return; s.SetReadingMode((ReadingSupport)(((int)s.ReadingMode + 1) % 3)); UpdateSettingsValues(); }
        private void CycleAssistance() { var s = PlayerSettingsManager.Instance; if (s == null) return; s.SetAssistance((AssistanceLevel)(((int)s.Assistance + 1) % 3)); UpdateSettingsValues(); }
        private void ToggleNoReading() { var s = PlayerSettingsManager.Instance; if (s != null) s.SetNoReading(!s.NoReadingMode); UpdateSettingsValues(); }
        private void AdjustText(float delta) { var s = PlayerSettingsManager.Instance; if (s != null) s.SetTextScale(s.TextScale >= 1.45f ? 0.85f : s.TextScale + 0.15f); UpdateSettingsValues(); ApplyAccessibilitySettings(); }
        private void ToggleSubtitles() { var s = PlayerSettingsManager.Instance; if (s != null) s.SetSubtitles(!s.Subtitles); UpdateSettingsValues(); }
        private void ToggleContrast() { var s = PlayerSettingsManager.Instance; if (s != null) s.SetHighContrast(!s.HighContrast); UpdateSettingsValues(); ApplyAccessibilitySettings(); }
        private void AdjustVolume(float delta) { var s = PlayerSettingsManager.Instance; if (s != null) s.SetVoiceVolume(s.VoiceVolume >= 0.95f ? 0f : s.VoiceVolume + delta); UpdateSettingsValues(); }
        private void ToggleClickMove() { var s = PlayerSettingsManager.Instance; if (s != null) s.SetClickToMove(!s.ClickToMove); UpdateSettingsValues(); }

        private void UpdateSettingsValues()
        {
            PlayerSettingsManager s = PlayerSettingsManager.Instance;
            if (s == null || languageValue == null) return;
            languageValue.text = new[] { "LANGUAGE · ENGLISH  ›", "ULIMI · ISIZULU  ›", "TAAL · AFRIKAANS  ›" }[(int)s.Language];
            string[][] reading = { new[] { "VOICE", "IZWI", "STEM" }, new[] { "PICTURES", "IZITHOMBE", "PRENTE" }, new[] { "VOICE + PICTURES", "IZWI + IZITHOMBE", "STEM + PRENTE" } };
            readingValue.text = reading[(int)s.ReadingMode][(int)s.Language] + "  ›";
            string[][] help = { new[] { "MORE GUIDANCE", "USIZO OLWENGEZIWE", "MEER LEIDING" }, new[] { "SOME HINTS", "AMACEBO", "SOMMIGE WENKE" }, new[] { "EXPLORE", "ZIHLOLELE", "VERKEN" } };
            assistanceValue.text = help[(int)s.Assistance][(int)s.Language] + "  ›";
            noReadingValue.text = s.NoReadingMode ? "ON  ·  ICON HELP" : "OFF  ·  ICON HELP";
            textSizeValue.text = Mathf.RoundToInt(s.TextScale * 100f) + "%  +";
            subtitlesValue.text = s.Subtitles ? "ON" : "OFF";
            contrastValue.text = s.HighContrast ? "ON" : "OFF";
            volumeValue.text = Mathf.RoundToInt(s.VoiceVolume * 100f) + "%  +";
            if (clickMoveValue != null) clickMoveValue.text = s.ClickToMove ? "ON" : "OFF";
            languageValue.supportRichText = true;
            MarkOn(noReadingValue, s.NoReadingMode);
        }

        private void MarkOn(Text label, bool on)
        {
            if (label != null) label.color = on ? new Color(0.62f, 1f, 0.82f) : White;
        }

        private void ShowControls()
        {
            settingsReturn = CurrentScreen == "settings" ? settingsReturn : CurrentScreen;
            CurrentScreen = "controls";
            HideAll(); controlsPanel.SetActive(true);
        }

        public void ShowWelcome(bool returning)
        {
            CurrentScreen = "welcome"; HideAll(); welcomePanel.SetActive(true);
            welcomeTitle.text = LanguageManager.Get("welcome.title", "HELLO, ASTRONAUT!");
            welcomeBody.text = LanguageManager.Get("welcome.body", "ASTRO is your space buddy. Learn one step at a time. Replay or skip practice whenever you like.");
            setupLabel.text = LanguageManager.Get(returning ? "welcome.returning" : "welcome.setup", returning
                ? "WELCOME BACK · PRACTISE OR CHOOSE A MISSION"
                : "MEET ASTRO · PRACTISE · CHOOSE A MISSION");
            UpdateButtonCaption(welcomePanel, "LET'S GET READY", LanguageManager.Get("welcome.start", "LET'S GET READY"));
            UpdateButtonCaption(welcomePanel, "I KNOW HOW TO PLAY — SKIP TUTORIAL", LanguageManager.Get("welcome.skip", "I KNOW HOW TO PLAY — SKIP TUTORIAL"));
            UpdateButtonCaption(welcomePanel, "⚙  ACCESSIBILITY SETTINGS", LanguageManager.Get("mission.access", "ACCESSIBILITY SETTINGS"));
        }

        public void ShowSettings(string returnScreen)
        {
            settingsReturn = string.IsNullOrEmpty(returnScreen) ? "welcome" : returnScreen;
            CurrentScreen = "settings"; HideAll(); settingsPanel.SetActive(true); UpdateSettingsValues();
            UpdateButtonCaption(settingsPanel, "SAVE AND CONTINUE", LanguageManager.Get("settings.done", "SAVE AND CONTINUE"));
        }

        public void ShowScreen(string name)
        {
            switch (name)
            {
                case "tutorial": ShowTutorial(TutorialManager.Instance != null ? TutorialManager.Instance.CurrentStep : 0); break;
                case "mission": ShowMissionSelect(); break;
                case "paused": CurrentScreen = "paused"; HideAll(); break;
                case "welcome": ShowWelcome(true); break;
                default: ShowWelcome(true); break;
            }
        }

        public void ShowTutorial(int step)
        {
            CurrentScreen = "tutorial"; HideAll(); tutorialPanel.SetActive(true);
            ApplyTutorialStep(step);
        }

        public void RefreshTutorialStep(int step) => ApplyTutorialStep(step);

        private void ApplyTutorialStep(int step)
        {
            TutorialManager manager = TutorialManager.Instance;
            if (tutorialPanel == null || manager == null) return;
            TutorialActivity activity = manager.CurrentActivity;
            tutorialStepText.text = LanguageManager.Get("tutorial.step", "ACTIVITY") + "  " + (step + 1) + " / " + manager.StepCount;
            tutorialTitle.text = LanguageManager.Get(activity.titleKey, activity.titleFallback);
            tutorialBody.text = LanguageManager.Get(activity.bodyKey, activity.bodyFallback);
            tutorialHint.text = "";
            bool noRead = PlayerSettingsManager.Instance != null && PlayerSettingsManager.Instance.NoReadingMode;
            tutorialBody.gameObject.SetActive(!noRead);
            tutorialIcon.text = step == 0 ? "◉\n\nASTRO" : step == 1 ? "    W\n  A   S   D\n  ↑   ↓   ←   →" :
                step == 2 ? "[RMB]  ↻\n\nLOOK" : step == 3 ? "▼\n\n[E]" : step == 4 ? "O₂  H₂O\n☀  FOOD\nRADIO" : "☀  +  [E]\n\n✓";
            tutorialNextLabel.text = step == manager.StepCount - 1
                ? LanguageManager.Get("tutorial.hint.button", "SHOW ME WHAT TO DO")
                : LanguageManager.Get("tutorial.next", "I DID IT — NEXT");
            equipmentPanel.SetActive(step == 4);
            if (step == 4) ShowEquipmentCard(equipmentIndex, LanguageManager.Get("equipment.oxygen", "OXYGEN · The system adds breathable air in our game."));
        }

        public void ShowEquipmentCard(int index, string copy)
        {
            equipmentIndex = (index + 5) % 5;
            if (equipmentPanel == null) return;
            string[] icons = { "O₂", "H₂O", "☀", "FOOD", "RADIO" };
            equipmentIcon.text = icons[equipmentIndex];
            equipmentBody.text = copy;
            UpdateButtonCaption(equipmentPanel, "BACK", LanguageManager.Get("equipment.previous", "BACK"));
            UpdateButtonCaption(equipmentPanel, "NEXT", LanguageManager.Get("equipment.next", "NEXT"));
            bool noRead = PlayerSettingsManager.Instance != null && PlayerSettingsManager.Instance.NoReadingMode;
            equipmentBody.gameObject.SetActive(!noRead);
            equipmentPanel.SetActive(CurrentScreen == "tutorial");
        }

        public void HideEquipmentCard() { if (equipmentPanel != null) equipmentPanel.SetActive(false); }

        public void ShowBuddyHint(string message)
        {
            if (tutorialHint == null || CurrentScreen != "tutorial") return;
            tutorialHint.text = PlayerSettingsManager.Instance != null && PlayerSettingsManager.Instance.NoReadingMode ? "" : message;
        }

        public void ShowTutorialComplete()
        {
            CurrentScreen = "complete"; HideAll(); completePanel.SetActive(true); completeTime = 0f;
            completeTitle.text = LanguageManager.Get("tutorial.complete", "GREAT WORK!");
            completeSpeech.text = LanguageManager.Get("ready", "Great work, astronaut! You're ready for your first mission. Let's head to the Moon!");
        }

        public void ShowMissionSelect()
        {
            int selected = OnboardingManager.Instance != null ? OnboardingManager.Instance.SelectedMissionIndex : 0;
            ShowMissionSelect(selected, AstraCampaignProgress.HighestUnlocked);
        }

        public void ShowMissionSelect(int selectedIndex, int highestUnlocked)
        {
            CurrentScreen = "mission"; HideAll(); missionPanel.SetActive(true);
            UpdateMissionCaptions();
            UpdateMissionSelection(selectedIndex, highestUnlocked);
        }

        public void UpdateMissionSelection(int selectedIndex, int highestUnlocked)
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, AstraMissionCatalog.Count - 1);
            AstraMissionDefinition mission = AstraMissionCatalog.Get(selectedIndex);
            missionDetailTitle.text = (selectedIndex + 1).ToString("00") + "  /  " + AstraMissionCatalog.DestinationName(mission.Destination) + "  /  " + mission.Title.ToUpperInvariant();
            missionDetailSummary.text = mission.Subtitle + "\nDestination: " + AstraMissionCatalog.DestinationName(mission.Destination);
            missionBriefingText.text = mission.Briefing;
            int best = AstraCampaignProgress.BestScore(mission.Id);
            bool unlocked = AstraCampaignProgress.IsMissionAvailable(mission.Id);
            bool missionUnlocked = AstraCampaignProgress.IsUnlocked(mission.Id);
            missionDetailStatus.text = unlocked
                ? (best > 0 ? AstraMissionCatalog.DestinationName(mission.Destination) + "   ·   PERSONAL BEST " + best.ToString("000") : AstraMissionCatalog.DestinationName(mission.Destination) + "   ·   UNLOCKED")
                : missionUnlocked ? "LOCKED   ·   REPAIR SHIP TO REACH " + AstraMissionCatalog.DestinationName(mission.Destination)
                : "LOCKED   ·   COMPLETE MISSION " + selectedIndex.ToString("00") + " TO UNLOCK";
            missionSourceButton.GetComponentInChildren<Text>().text = "LEARN MORE  ·  " + mission.SourceLabel.ToUpperInvariant() + "  ↗";
            missionBriefingUrl = mission.SourceUrl;
            launchSelectedMissionButton.interactable = unlocked;
            Text launchLabel = launchSelectedMissionButton.GetComponentInChildren<Text>();
            launchLabel.text = unlocked ? "LAUNCH " + mission.Title.ToUpperInvariant() : missionUnlocked ? "DESTINATION LOCKED" : "MISSION LOCKED";

            for (int i = 0; i < missionButtons.Length; i++)
            {
                bool selected = i == selectedIndex;
                AstraMissionDefinition item = AstraMissionCatalog.Get(i);
                bool itemUnlocked = AstraCampaignProgress.IsMissionAvailable(item.Id);
                bool slotUnlocked = AstraCampaignProgress.IsUnlocked(item.Id);
                missionButtonLabels[i].text = (i + 1).ToString("00") + "   " + AstraMissionCatalog.DestinationName(item.Destination) + "  /  " + item.Title.ToUpperInvariant() + (itemUnlocked ? "" : slotUnlocked ? "   ·   SHIP REPAIR" : "   ·   LOCKED");
            missionButtonLabels[i].color = itemUnlocked ? White : Soft;
                ColorBlock colors = missionButtons[i].colors;
                colors.normalColor = selected ? new Color(0.04f, 0.38f, 0.46f) : new Color(0.07f, 0.15f, 0.23f);
                colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.18f);
                missionButtons[i].colors = colors;
            }
        }

        private string missionBriefingUrl;

        private void OpenSelectedMissionSource()
        {
            if (!string.IsNullOrEmpty(missionBriefingUrl)) Application.OpenURL(missionBriefingUrl);
        }

        public void ShowMissionLockedMessage()
        {
            missionDetailStatus.text = "LOCKED   ·   COMPLETE EARLIER MISSIONS TO CONTINUE";
        }

        public void HideAll()
        {
            if (welcomePanel != null) welcomePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (completePanel != null) completePanel.SetActive(false);
            if (missionPanel != null) missionPanel.SetActive(false);
        }

        public void ApplyAccessibilitySettings()
        {
            if (baseSizes.Count == 0) return;
            PlayerSettingsManager settings = PlayerSettingsManager.Instance;
            float scale = settings != null ? settings.TextScale : 1f;
            foreach (var pair in baseSizes)
                if (pair.Key != null) pair.Key.fontSize = Mathf.Clamp(Mathf.RoundToInt(pair.Value * scale), 14, 60);
            bool high = settings != null && settings.HighContrast;
            foreach (Image image in cards)
                if (image != null) image.color = high ? new Color(0f, 0f, 0f, 0.99f) : Card;
            if (tutorialBody != null) tutorialBody.color = high ? Color.white : Soft;
            if (welcomeBody != null) welcomeBody.color = high ? Color.white : Soft;
        }

        private void UpdateMissionCaptions()
        {
            UpdateButtonCaption(missionPanel, "PRACTISE WITH ASTRO", LanguageManager.Get("mission.replay", "PRACTISE WITH ASTRO"));
            UpdateButtonCaption(missionPanel, "ACCESSIBILITY SETTINGS", LanguageManager.Get("mission.access", "ACCESSIBILITY SETTINGS"));
            UpdateButtonCaption(missionPanel, "LAUNCH FIRST LIGHT", LanguageManager.Get("mission.launch", "LAUNCH MISSION"));
        }

        private void SetSpeech(string line)
        {
            if (tutorialSpeech == null || CurrentScreen != "tutorial") return;
            bool voiceMissing = VoiceGuideManager.Instance == null || !VoiceGuideManager.Instance.LastVoiceAvailable;
            bool show = PlayerSettingsManager.Instance == null || PlayerSettingsManager.Instance.Subtitles ||
                PlayerSettingsManager.Instance.ReadingMode != ReadingSupport.VoiceGuided || voiceMissing;
            tutorialSpeech.text = show ? line : "";
        }

        private void SetControlsStatus(string text)
        {
            if (controlsStatus != null) controlsStatus.text = string.IsNullOrEmpty(text) ? "Press a control button to choose a new key." : text;
            RefreshBindingCaptions();
        }

        private void RefreshBindingCaptions()
        {
            PlayerSettingsManager s = PlayerSettingsManager.Instance;
            if (controlsPanel == null || s == null) return;
            AstraBinding[] bindings = { AstraBinding.MoveForward, AstraBinding.MoveBack, AstraBinding.MoveLeft, AstraBinding.MoveRight, AstraBinding.Interact };
            foreach (AstraBinding binding in bindings)
                if (bindingLabels.TryGetValue(binding, out Text label) && label != null)
                    label.text = s.PendingRebind == "KEY_" + binding.ToString().ToUpperInvariant() ? "PRESS KEY…" : s.GetKeyLabel(binding);
        }

        private void Update()
        {
            if (completePanel != null && completePanel.activeSelf)
            {
                completeTime += Time.unscaledDeltaTime;
                completeBadge.transform.localScale = Vector3.one * (1f + Mathf.Sin(completeTime * 3f) * 0.06f);
            }
            if (CurrentScreen == "controls") RefreshBindingCaptions();
        }

        private void UpdateButtonCaption(GameObject parent, string existing, string translated)
        {
            if (parent == null) return;
            foreach (Text text in parent.GetComponentsInChildren<Text>(true))
                if (text.text == existing) { text.text = translated; break; }
        }

        private GameObject FullPanel(string name, Transform parent, Color color)
        {
            GameObject panel = MakePanel(parent, color, Vector2.zero, Vector2.one);
            panel.name = name;
            return panel;
        }

        private GameObject MakePanel(Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, min, max, Vector2.zero, Vector2.zero);
            Image image = go.GetComponent<Image>(); image.color = color;
            image.raycastTarget = true;
            return go;
        }

        private Image MakeImage(Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject go = MakePanel(parent, color, min, max);
            return go.GetComponent<Image>();
        }

        private Text MakeText(Transform parent, string value, int size, Color color, FontStyle style, TextAnchor anchor, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.fontStyle = style; text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            SetRect(text.rectTransform, min, max, new Vector2(6, 3), new Vector2(-6, -3));
            baseSizes[text] = size;
            return text;
        }

        private Button MakeButton(Transform parent, string caption, Action action, Vector2 min, Vector2 max, Color color, int size)
        {
            GameObject go = MakePanel(parent, color, min, max);
            go.name = "Button_" + caption;
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors; colors.normalColor = color; colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f); colors.selectedColor = colors.highlightedColor; button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            Text label = MakeText(go.transform, caption, size, White, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            label.rectTransform.offsetMin = new Vector2(8, 3); label.rectTransform.offsetMax = new Vector2(-8, -3);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }
    }
}
