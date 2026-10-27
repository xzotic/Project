using UnityEditor;
using UnityEngine;
using RealisticBodycam;

namespace RealisticBodycam.EditorTools
{
    [CustomEditor(typeof(BodycamFPSController))]
    public class BodycamFPSControllerEditor : UnityEditor.Editor
    {
        // Visual identity inspired by the green BODYCAM banner.
        static readonly Color AccentGreen = new Color(0.10f, 1.00f, 0.22f);
        static readonly Color AccentGreenHover = new Color(0.38f, 1.00f, 0.48f);
        static readonly Color DarkGreen = new Color(0.05f, 0.38f, 0.12f);
        static readonly Color SoftGreen = new Color(0.16f, 0.34f, 0.20f);
        static readonly Color SectionTint = new Color(0.09f, 0.15f, 0.10f);

        Texture2D banner;

        SerializedProperty enableMovement, enableMouseLook, enableBodycamMotion, enableHeadBob, enableIdleBreathing, enableBodyWeight, enableTurnInertia, enableStepRotation, enableFreeLook, enableWeaponCompensation;

        SerializedProperty cameraTransform, weaponHolder, groundCheck, groundMask;
        SerializedProperty walkSpeed, runSpeed, movementSmoothness;
        SerializedProperty mouseSensitivity, maxLookUp, maxLookDown;
        SerializedProperty freeLookKey, toggleMode, freeLookSensitivity, freeLookReturnSpeed, maxFreeLookAngle;
        SerializedProperty accelerationWeight, stopWeightAmount, stopWeightReturnSpeed, movementLeanAmount, movementLeanSmoothness;
        SerializedProperty jumpForce, gravity, groundCheckDistance;
        SerializedProperty bodycamHeightOffset, cameraPositionSmoothness, cameraRotationSmoothness;
        SerializedProperty lookSwayAmount, bodyRollAmount, bodyRollSpeed;
        SerializedProperty turnLagYaw, turnLagPitch, turnLagRoll, turnLagSmoothness;
        SerializedProperty walkStepPitch, walkStepRoll, runStepPitch, runStepRoll, stepRotationSmoothness;
        SerializedProperty breathingAmount, breathingSpeed;
        SerializedProperty walkBobSpeed, walkBobAmount, walkSideAmount;
        SerializedProperty runBobSpeed, runBobAmount, runSideAmount, runShakeAmount, runShakeSpeed;
        SerializedProperty enableReferenceGait, walkForwardMotion, walkGaitRoll, walkGaitPitch, walkGaitYaw;
        SerializedProperty runForwardMotion, runGaitRoll, runGaitPitch, runGaitYaw;
        SerializedProperty footImpactSharpness, gaitPositionSmoothness, gaitRotationSmoothness, strafeGaitMultiplier;
        SerializedProperty enableHeavyBodyResponse, walkFootImpact, runFootImpact, bodyImpactSpring, bodyImpactDamping;
        SerializedProperty impactPitchKick, impactRollKick, accelerationBodyLag, accelerationPitch, startMovementInertiaMultiplier;
        SerializedProperty aimGaitMultiplier, aimStopInertiaMultiplier;

        bool showToggles = true;
        bool showReferences = true;
        bool showMovement = true;
        bool showMouse = true;
        bool showFreeLook = true;
        bool showBodyWeight = true;
        bool showPhysics = true;
        bool showBodycam = true;
        bool showTurnInertia = true;
        bool showStepRotation = true;
        bool showIdle = true;
        bool showWalk = true;
        bool showRun = true;
        bool showReferenceGait = true;
        bool showHeavyBody = true;

        void OnEnable()
        {
            banner = Resources.Load<Texture2D>("bodycam_banner");

            enableMovement = serializedObject.FindProperty("enableMovement");
            enableMouseLook = serializedObject.FindProperty("enableMouseLook");
            enableBodycamMotion = serializedObject.FindProperty("enableBodycamMotion");
            enableHeadBob = serializedObject.FindProperty("enableHeadBob");
            enableIdleBreathing = serializedObject.FindProperty("enableIdleBreathing");
            enableBodyWeight = serializedObject.FindProperty("enableBodyWeight");
            enableTurnInertia = serializedObject.FindProperty("enableTurnInertia");
            enableStepRotation = serializedObject.FindProperty("enableStepRotation");
            enableFreeLook = serializedObject.FindProperty("enableFreeLook");
            enableWeaponCompensation = serializedObject.FindProperty("enableWeaponCompensation");

            cameraTransform = serializedObject.FindProperty("cameraTransform");
            weaponHolder = serializedObject.FindProperty("weaponHolder");
            groundCheck = serializedObject.FindProperty("groundCheck");
            groundMask = serializedObject.FindProperty("groundMask");

            walkSpeed = serializedObject.FindProperty("walkSpeed");
            runSpeed = serializedObject.FindProperty("runSpeed");
            movementSmoothness = serializedObject.FindProperty("movementSmoothness");

            mouseSensitivity = serializedObject.FindProperty("mouseSensitivity");
            maxLookUp = serializedObject.FindProperty("maxLookUp");
            maxLookDown = serializedObject.FindProperty("maxLookDown");

            freeLookKey = serializedObject.FindProperty("freeLookKey");
            toggleMode = serializedObject.FindProperty("toggleMode");
            freeLookSensitivity = serializedObject.FindProperty("freeLookSensitivity");
            freeLookReturnSpeed = serializedObject.FindProperty("freeLookReturnSpeed");
            maxFreeLookAngle = serializedObject.FindProperty("maxFreeLookAngle");

            accelerationWeight = serializedObject.FindProperty("accelerationWeight");
            stopWeightAmount = serializedObject.FindProperty("stopWeightAmount");
            stopWeightReturnSpeed = serializedObject.FindProperty("stopWeightReturnSpeed");
            movementLeanAmount = serializedObject.FindProperty("movementLeanAmount");
            movementLeanSmoothness = serializedObject.FindProperty("movementLeanSmoothness");

            jumpForce = serializedObject.FindProperty("jumpForce");
            gravity = serializedObject.FindProperty("gravity");
            groundCheckDistance = serializedObject.FindProperty("groundCheckDistance");

            bodycamHeightOffset = serializedObject.FindProperty("bodycamHeightOffset");
            cameraPositionSmoothness = serializedObject.FindProperty("cameraPositionSmoothness");
            cameraRotationSmoothness = serializedObject.FindProperty("cameraRotationSmoothness");

            lookSwayAmount = serializedObject.FindProperty("lookSwayAmount");
            bodyRollAmount = serializedObject.FindProperty("bodyRollAmount");
            bodyRollSpeed = serializedObject.FindProperty("bodyRollSpeed");

            turnLagYaw = serializedObject.FindProperty("turnLagYaw");
            turnLagPitch = serializedObject.FindProperty("turnLagPitch");
            turnLagRoll = serializedObject.FindProperty("turnLagRoll");
            turnLagSmoothness = serializedObject.FindProperty("turnLagSmoothness");

            walkStepPitch = serializedObject.FindProperty("walkStepPitch");
            walkStepRoll = serializedObject.FindProperty("walkStepRoll");
            runStepPitch = serializedObject.FindProperty("runStepPitch");
            runStepRoll = serializedObject.FindProperty("runStepRoll");
            stepRotationSmoothness = serializedObject.FindProperty("stepRotationSmoothness");

            breathingAmount = serializedObject.FindProperty("breathingAmount");
            breathingSpeed = serializedObject.FindProperty("breathingSpeed");

            walkBobSpeed = serializedObject.FindProperty("walkBobSpeed");
            walkBobAmount = serializedObject.FindProperty("walkBobAmount");
            walkSideAmount = serializedObject.FindProperty("walkSideAmount");

            runBobSpeed = serializedObject.FindProperty("runBobSpeed");
            runBobAmount = serializedObject.FindProperty("runBobAmount");
            runSideAmount = serializedObject.FindProperty("runSideAmount");
            runShakeAmount = serializedObject.FindProperty("runShakeAmount");
            runShakeSpeed = serializedObject.FindProperty("runShakeSpeed");

            enableReferenceGait = serializedObject.FindProperty("enableReferenceGait");
            walkForwardMotion = serializedObject.FindProperty("walkForwardMotion");
            walkGaitRoll = serializedObject.FindProperty("walkGaitRoll");
            walkGaitPitch = serializedObject.FindProperty("walkGaitPitch");
            walkGaitYaw = serializedObject.FindProperty("walkGaitYaw");
            runForwardMotion = serializedObject.FindProperty("runForwardMotion");
            runGaitRoll = serializedObject.FindProperty("runGaitRoll");
            runGaitPitch = serializedObject.FindProperty("runGaitPitch");
            runGaitYaw = serializedObject.FindProperty("runGaitYaw");
            footImpactSharpness = serializedObject.FindProperty("footImpactSharpness");
            gaitPositionSmoothness = serializedObject.FindProperty("gaitPositionSmoothness");
            gaitRotationSmoothness = serializedObject.FindProperty("gaitRotationSmoothness");
            strafeGaitMultiplier = serializedObject.FindProperty("strafeGaitMultiplier");
            enableHeavyBodyResponse = serializedObject.FindProperty("enableHeavyBodyResponse");
            walkFootImpact = serializedObject.FindProperty("walkFootImpact");
            runFootImpact = serializedObject.FindProperty("runFootImpact");
            bodyImpactSpring = serializedObject.FindProperty("bodyImpactSpring");
            bodyImpactDamping = serializedObject.FindProperty("bodyImpactDamping");
            impactPitchKick = serializedObject.FindProperty("impactPitchKick");
            impactRollKick = serializedObject.FindProperty("impactRollKick");
            accelerationBodyLag = serializedObject.FindProperty("accelerationBodyLag");
            accelerationPitch = serializedObject.FindProperty("accelerationPitch");
            startMovementInertiaMultiplier = serializedObject.FindProperty("startMovementInertiaMultiplier");
            aimGaitMultiplier = serializedObject.FindProperty("aimGaitMultiplier");
            aimStopInertiaMultiplier = serializedObject.FindProperty("aimStopInertiaMultiplier");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBanner();
            DrawTitle();
            DrawPresetButtons();
            DrawResetButton();

            DrawSection("🧩 SYSTEM TOGGLES", ref showToggles, () =>
            {
                EditorGUILayout.PropertyField(enableMovement);
                EditorGUILayout.PropertyField(enableMouseLook);
                EditorGUILayout.PropertyField(enableBodycamMotion);
                EditorGUILayout.PropertyField(enableHeadBob);
                EditorGUILayout.PropertyField(enableIdleBreathing);
                EditorGUILayout.PropertyField(enableBodyWeight);
                EditorGUILayout.PropertyField(enableTurnInertia);
                EditorGUILayout.PropertyField(enableStepRotation);
                EditorGUILayout.PropertyField(enableFreeLook);
                EditorGUILayout.PropertyField(enableWeaponCompensation);
            });

            DrawSection("🎯 REFERENCES", ref showReferences, () =>
            {
                EditorGUILayout.PropertyField(cameraTransform);
                EditorGUILayout.PropertyField(weaponHolder);
                EditorGUILayout.PropertyField(groundCheck);
                EditorGUILayout.PropertyField(groundMask);
            });

            DrawSection("🏃 MOVEMENT SETTINGS", ref showMovement, () =>
            {
                EditorGUILayout.PropertyField(walkSpeed);
                EditorGUILayout.PropertyField(runSpeed);
                EditorGUILayout.PropertyField(movementSmoothness);
            });

            DrawSection("🖱 MOUSE LOOK", ref showMouse, () =>
            {
                EditorGUILayout.PropertyField(mouseSensitivity);
                EditorGUILayout.PropertyField(maxLookUp);
                EditorGUILayout.PropertyField(maxLookDown);
            });

            DrawSection("👁 FREE LOOK", ref showFreeLook, () =>
            {
                EditorGUILayout.PropertyField(freeLookKey);
                EditorGUILayout.PropertyField(toggleMode);
                EditorGUILayout.PropertyField(freeLookSensitivity);
                EditorGUILayout.PropertyField(freeLookReturnSpeed);
                EditorGUILayout.PropertyField(maxFreeLookAngle);
            });

            DrawSection("⚖ BODY WEIGHT", ref showBodyWeight, () =>
            {
                EditorGUILayout.PropertyField(accelerationWeight);
                EditorGUILayout.PropertyField(stopWeightAmount);
                EditorGUILayout.PropertyField(stopWeightReturnSpeed);
                EditorGUILayout.PropertyField(movementLeanAmount);
                EditorGUILayout.PropertyField(movementLeanSmoothness);
            });

            DrawSection("🪂 JUMP & GRAVITY", ref showPhysics, () =>
            {
                EditorGUILayout.PropertyField(jumpForce);
                EditorGUILayout.PropertyField(gravity);
                EditorGUILayout.PropertyField(groundCheckDistance);
            });

            DrawSection("🎥 BODYCAM FEEL", ref showBodycam, () =>
            {
                EditorGUILayout.PropertyField(bodycamHeightOffset);
                EditorGUILayout.PropertyField(cameraPositionSmoothness);
                EditorGUILayout.PropertyField(cameraRotationSmoothness);
                EditorGUILayout.PropertyField(lookSwayAmount);
                EditorGUILayout.PropertyField(bodyRollAmount);
                EditorGUILayout.PropertyField(bodyRollSpeed);
            });

            DrawSection("CAMERA TURN INERTIA", ref showTurnInertia, () =>
            {
                EditorGUILayout.PropertyField(turnLagYaw);
                EditorGUILayout.PropertyField(turnLagPitch);
                EditorGUILayout.PropertyField(turnLagRoll);
                EditorGUILayout.PropertyField(turnLagSmoothness);
            });

            DrawSection("STEP ROTATION", ref showStepRotation, () =>
            {
                EditorGUILayout.PropertyField(walkStepPitch);
                EditorGUILayout.PropertyField(walkStepRoll);
                EditorGUILayout.PropertyField(runStepPitch);
                EditorGUILayout.PropertyField(runStepRoll);
                EditorGUILayout.PropertyField(stepRotationSmoothness);
            });

            DrawSection("🌬 IDLE BREATHING", ref showIdle, () =>
            {
                EditorGUILayout.PropertyField(breathingAmount);
                EditorGUILayout.PropertyField(breathingSpeed);
            });

            DrawSection("🚶 WALK CAMERA MOTION", ref showWalk, () =>
            {
                EditorGUILayout.PropertyField(walkBobSpeed);
                EditorGUILayout.PropertyField(walkBobAmount);
                EditorGUILayout.PropertyField(walkSideAmount);
            });

            DrawSection("🏃 RUN CAMERA MOTION", ref showRun, () =>
            {
                EditorGUILayout.PropertyField(runBobSpeed);
                EditorGUILayout.PropertyField(runBobAmount);
                EditorGUILayout.PropertyField(runSideAmount);
                EditorGUILayout.PropertyField(runShakeAmount);
                EditorGUILayout.PropertyField(runShakeSpeed);
            });

            DrawSection("REFERENCE WALK & RUN MOVEMENT", ref showReferenceGait, () =>
            {
                EditorGUILayout.PropertyField(enableReferenceGait);

                if (enableReferenceGait.boolValue)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Walking", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(walkForwardMotion);
                    EditorGUILayout.PropertyField(walkGaitRoll);
                    EditorGUILayout.PropertyField(walkGaitPitch);
                    EditorGUILayout.PropertyField(walkGaitYaw);

                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Running", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(runForwardMotion);
                    EditorGUILayout.PropertyField(runGaitRoll);
                    EditorGUILayout.PropertyField(runGaitPitch);
                    EditorGUILayout.PropertyField(runGaitYaw);

                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Response", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(footImpactSharpness);
                    EditorGUILayout.PropertyField(gaitPositionSmoothness);
                    EditorGUILayout.PropertyField(gaitRotationSmoothness);
                    EditorGUILayout.PropertyField(strafeGaitMultiplier);
                }
            });

            DrawSection("HEAVY BODY RESPONSE", ref showHeavyBody, () =>
            {
                EditorGUILayout.PropertyField(enableHeavyBodyResponse);

                if (enableHeavyBodyResponse.boolValue)
                {
                    EditorGUILayout.PropertyField(walkFootImpact);
                    EditorGUILayout.PropertyField(runFootImpact);
                    EditorGUILayout.PropertyField(bodyImpactSpring);
                    EditorGUILayout.PropertyField(bodyImpactDamping);
                    EditorGUILayout.PropertyField(impactPitchKick);
                    EditorGUILayout.PropertyField(impactRollKick);
                    EditorGUILayout.PropertyField(accelerationBodyLag);
                    EditorGUILayout.PropertyField(accelerationPitch);
                    EditorGUILayout.PropertyField(startMovementInertiaMultiplier, new GUIContent("Start Movement Inertia"));
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Aiming Stabilization", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(aimGaitMultiplier);
                    EditorGUILayout.PropertyField(aimStopInertiaMultiplier);
                }
            });

            serializedObject.ApplyModifiedProperties();
        }

        void DrawBanner()
        {
            if (banner == null) return;

            float width = EditorGUIUtility.currentViewWidth - 25f;
            float height = width * 0.25f;

            Rect rect = GUILayoutUtility.GetRect(width, height);
            GUI.DrawTexture(rect, banner, ScaleMode.ScaleToFit);
            GUILayout.Space(8);
        }

        void DrawTitle()
        {
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel);
            title.fontSize = 17;
            title.alignment = TextAnchor.MiddleCenter;
            title.normal.textColor = Color.white;

            GUILayout.Label("REALISTIC BODYCAM FPS CONTROLLER", title);

            Rect accentLine = GUILayoutUtility.GetRect(1f, 2f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(accentLine, AccentGreen);
            GUILayout.Space(10);
        }

        void DrawPresetButtons()
        {
            BodycamFPSController controller = (BodycamFPSController)target;

            Color oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = SectionTint;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = oldBackground;

            GUILayout.Label("Quick Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            PresetButton(controller, "Realistic", BodycamFPSController.BodycamPreset.Realistic);
            PresetButton(controller, "Cinematic", BodycamFPSController.BodycamPreset.Cinematic);
            PresetButton(controller, "Extreme", BodycamFPSController.BodycamPreset.Extreme);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            PresetButton(controller, "Horror", BodycamFPSController.BodycamPreset.Horror);
            PresetButton(controller, "Tactical", BodycamFPSController.BodycamPreset.Tactical);
            PresetButton(controller, "Found Footage", BodycamFPSController.BodycamPreset.FoundFootage);

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUI.backgroundColor = SoftGreen;
            if (GUILayout.Button("Randomize Bodycam Feel", GUILayout.Height(28)))
            {
                Undo.RecordObject(controller, "Randomize Bodycam Feel");
                controller.RandomizeSettings();
                EditorUtility.SetDirty(controller);
            }
            GUI.backgroundColor = oldBackground;

            EditorGUILayout.EndVertical();

            GUILayout.Space(6);
        }

        void PresetButton(BodycamFPSController controller, string label, BodycamFPSController.BodycamPreset preset)
        {
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                Undo.RecordObject(controller, "Apply " + label + " Preset");
                controller.ApplyPreset(preset);
                EditorUtility.SetDirty(controller);
            }
        }

        void DrawResetButton()
        {
            BodycamFPSController controller = (BodycamFPSController)target;

            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = DarkGreen;

            if (GUILayout.Button("Reset Defaults", GUILayout.Height(30)))
            {
                Undo.RecordObject(controller, "Reset Bodycam Defaults");
                controller.ApplyPreset(BodycamFPSController.BodycamPreset.Realistic);

                controller.enableMovement = true;
                controller.enableMouseLook = true;
                controller.enableBodycamMotion = true;
                controller.enableHeadBob = true;
                controller.enableIdleBreathing = true;
                controller.enableBodyWeight = true;
                controller.enableTurnInertia = true;
                controller.enableStepRotation = true;
                controller.enableFreeLook = true;
                controller.enableWeaponCompensation = true;
                controller.enableReferenceGait = true;
                controller.enableHeavyBodyResponse = true;

                controller.freeLookKey = KeyCode.LeftAlt;
                controller.toggleMode = false;
                controller.freeLookSensitivity = 1f;
                controller.freeLookReturnSpeed = 8f;
                controller.maxFreeLookAngle = 80f;

                controller.jumpForce = 1.2f;
                controller.gravity = -18f;
                controller.groundCheckDistance = 0.4f;

                EditorUtility.SetDirty(controller);
            }

            GUI.backgroundColor = oldColor;
            GUILayout.Space(10);
        }

        void DrawSection(string title, ref bool foldout, System.Action content)
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.padding = new RectOffset(10, 10, 8, 8);

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = SectionTint;

            EditorGUILayout.BeginVertical(boxStyle);

            GUI.backgroundColor = oldBg;

            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            foldoutStyle.fontSize = 12;
            foldoutStyle.normal.textColor = AccentGreen;
            foldoutStyle.onNormal.textColor = AccentGreen;
            foldoutStyle.hover.textColor = AccentGreenHover;
            foldoutStyle.onHover.textColor = AccentGreenHover;
            foldoutStyle.focused.textColor = AccentGreen;
            foldoutStyle.onFocused.textColor = AccentGreen;
            foldoutStyle.active.textColor = AccentGreenHover;
            foldoutStyle.onActive.textColor = AccentGreenHover;

            foldout = EditorGUILayout.Foldout(foldout, title, true, foldoutStyle);

            if (foldout)
            {
                GUILayout.Space(6);
                content.Invoke();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }
    }
}
