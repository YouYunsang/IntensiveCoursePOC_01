#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PocBattle.Data;
using PocBattle.Presentation;
using PocBattle.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocBattle.Editor
{
    /// <summary>
    /// Repairs only POC scene composition and presentation references without rewriting gameplay tuning assets.
    /// </summary>
    public static class PocBattleSceneRepairUtility
    {
        /// <summary>Generated POC data folder.</summary>
        private const string DATA_ROOT = "Assets/07.Data/POCBattle";

        /// <summary>Generated POC prefab folder.</summary>
        private const string PREFAB_ROOT = "Assets/02.Prefabs/POCBattle";

        /// <summary>POC battle scene repaired in place.</summary>
        private const string SCENE_PATH = "Assets/00.Scenes/POCBattleScene.unity";

        /// <summary>Generated player prefab path.</summary>
        private const string PLAYER_PREFAB_PATH = PREFAB_ROOT + "/Player.prefab";

        /// <summary>Generated enemy view prefab path.</summary>
        private const string ENEMY_PREFAB_PATH = PREFAB_ROOT + "/EnemyView.prefab";

        /// <summary>Current generated block prefab path.</summary>
        private const string BLOCK_PREFAB_PATH = PREFAB_ROOT + "/Block.prefab";

        /// <summary>New installer cell prefab path.</summary>
        private const string GRID_CELL_PREFAB_PATH = PREFAB_ROOT + "/GridCell.prefab";

        /// <summary>Legacy/current exported cell prefab path retained for in-place repair compatibility.</summary>
        private const string LEGACY_CELL_PREFAB_PATH = PREFAB_ROOT + "/CellPrefab.prefab";

        /// <summary>
        /// Repairs missing Player/Enemy scene composition, converts POC HUD presentation to OnGUI, and reconnects required references.
        /// Gameplay SO values and encounter pattern contents are not rewritten.
        /// </summary>
        [MenuItem("Tools/POC Battle/Repair POC Battle Scene (OnGUI)")]
        public static void RepairPocBattleScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null)
            {
                Debug.LogError($"POC Battle repair failed because scene does not exist at {SCENE_PATH}.");
                return;
            }

            if (!TryLoadRequiredAssets(out RepairAssets assets))
            {
                return;
            }

            RepairEnemyViewPrefab();
            RepairBlockViewPrefab();

            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            // Prefab save/unload and single-scene loading can replace Unity's native asset objects.
            // Refresh the dependency bundle so later .gameObject access never uses a destroyed component handle.
            if (!TryLoadRequiredAssets(out assets))
            {
                return;
            }

            RemoveLegacyGeneratedUi(scene);

            BattleController battleController = RepairBattleRoot(scene, assets);
            BattleEventChannelSO eventChannel = GetObjectReference<BattleEventChannelSO>(battleController, "_eventChannel");
            BattleBoardSettingsSO boardSettings = GetObjectReference<BattleBoardSettingsSO>(battleController, "_boardSettings");
            BattlePresentationSettingsSO presentationSettings =
                GetObjectReference<BattlePresentationSettingsSO>(battleController, "_presentationSettings");

            RepairBoardRoot(scene, assets, eventChannel, boardSettings, presentationSettings);
            RepairPlayerRoot(scene, assets, eventChannel, boardSettings, presentationSettings);
            RepairEnemyRoot(scene, assets, eventChannel, presentationSettings);
            RepairMainCamera(scene, eventChannel, boardSettings, presentationSettings);
            RepairOnGuiPresenter(battleController.gameObject, eventChannel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool valid = ValidateSceneSetup(scene, true);
            Debug.Log(valid
                ? "POC Battle scene repair complete. Player/Enemy world views and OnGUI POC HUD are ready."
                : "POC Battle scene repair completed, but validation still found errors. Check the Console messages above.");
        }

        /// <summary>
        /// Validates the currently stored POC scene composition without modifying gameplay data.
        /// </summary>
        [MenuItem("Tools/POC Battle/Validate POC Battle Setup")]
        public static void ValidatePocBattleSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null)
            {
                Debug.LogError($"POC Battle validation failed because scene does not exist at {SCENE_PATH}.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            bool valid = ValidateSceneSetup(scene, true);
            if (valid)
            {
                Debug.Log("POC Battle setup validation passed.");
            }
        }

        /// <summary>
        /// Loads only existing generated assets required to repair scene composition.
        /// No gameplay asset is created or reconfigured here.
        /// </summary>
        private static bool TryLoadRequiredAssets(out RepairAssets assets)
        {
            BattleEventChannelSO eventChannel = AssetDatabase.LoadAssetAtPath<BattleEventChannelSO>($"{DATA_ROOT}/BattleEventChannel.asset");
            BattleBoardSettingsSO boardSettings = AssetDatabase.LoadAssetAtPath<BattleBoardSettingsSO>($"{DATA_ROOT}/BattleBoardSettings.asset");
            PlayerBaseStatsSO playerStats = AssetDatabase.LoadAssetAtPath<PlayerBaseStatsSO>($"{DATA_ROOT}/PlayerBaseStats.asset");
            BattlePresentationSettingsSO presentationSettings =
                AssetDatabase.LoadAssetAtPath<BattlePresentationSettingsSO>($"{DATA_ROOT}/BattlePresentationSettings.asset");
            DeckDefinitionSO deck = AssetDatabase.LoadAssetAtPath<DeckDefinitionSO>($"{DATA_ROOT}/Deck_POC.asset");
            EncounterDefinitionSO defaultEncounter = AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>($"{DATA_ROOT}/Encounter_EnemyA.asset");
            PlayerController playerPrefab = LoadPrefabComponent<PlayerController>(PLAYER_PREFAB_PATH);
            EnemyView enemyPrefab = LoadPrefabComponent<EnemyView>(ENEMY_PREFAB_PATH);
            BoardCellView cellPrefab = LoadPrefabComponent<BoardCellView>(GRID_CELL_PREFAB_PATH);
            if (cellPrefab == null)
            {
                cellPrefab = LoadPrefabComponent<BoardCellView>(LEGACY_CELL_PREFAB_PATH);
            }

            BlockView blockPrefab = LoadPrefabComponent<BlockView>(BLOCK_PREFAB_PATH);

            bool valid = true;
            valid &= RequireAsset(eventChannel, $"{DATA_ROOT}/BattleEventChannel.asset");
            valid &= RequireAsset(boardSettings, $"{DATA_ROOT}/BattleBoardSettings.asset");
            valid &= RequireAsset(playerStats, $"{DATA_ROOT}/PlayerBaseStats.asset");
            valid &= RequireAsset(presentationSettings, $"{DATA_ROOT}/BattlePresentationSettings.asset");
            valid &= RequireAsset(deck, $"{DATA_ROOT}/Deck_POC.asset");
            valid &= RequireAsset(defaultEncounter, $"{DATA_ROOT}/Encounter_EnemyA.asset");
            valid &= RequireAsset(playerPrefab, PLAYER_PREFAB_PATH);
            valid &= RequireAsset(enemyPrefab, ENEMY_PREFAB_PATH);
            valid &= RequireAsset(cellPrefab, $"{GRID_CELL_PREFAB_PATH} or {LEGACY_CELL_PREFAB_PATH}");
            valid &= RequireAsset(blockPrefab, BLOCK_PREFAB_PATH);

            assets = new RepairAssets(
                eventChannel,
                boardSettings,
                playerStats,
                presentationSettings,
                deck,
                defaultEncounter,
                playerPrefab,
                enemyPrefab,
                cellPrefab,
                blockPrefab);
            return valid;
        }

        /// <summary>
        /// Logs a missing required POC asset and returns false when the reference is null.
        /// </summary>
        private static bool RequireAsset(UnityEngine.Object asset, string path)
        {
            if (asset != null)
            {
                return true;
            }

            Debug.LogError($"POC Battle repair requires an existing asset at {path}.");
            return false;
        }

        /// <summary>
        /// Removes generated Canvas HUD/EventSystem objects because POC UI is now rendered by OnGUI.
        /// Other scene roots are preserved; only the generated root names BattleHud and EventSystem are removed.
        /// </summary>
        private static void RemoveLegacyGeneratedUi(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = roots.Length - 1; rootIndex >= 0; rootIndex--)
            {
                GameObject root = roots[rootIndex];
                if (root.name == "BattleHud" || root.name == "EventSystem")
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        /// <summary>
        /// Repairs/creates BattleRoot and fills only missing core data references, preserving a manually selected encounter when present.
        /// </summary>
        private static BattleController RepairBattleRoot(Scene scene, RepairAssets assets)
        {
            List<BattleController> controllers = GetSceneComponents<BattleController>(scene);
            BattleController battleController;
            if (controllers.Count > 0)
            {
                battleController = controllers[0];
            }
            else
            {
                GameObject battleRoot = GetOrCreateRoot(scene, "BattleRoot");
                battleController = battleRoot.AddComponent<BattleController>();
            }

            SetObjectReferenceIfMissing(battleController, "_boardSettings", assets.BoardSettings);
            SetObjectReferenceIfMissing(battleController, "_playerBaseStats", assets.PlayerStats);
            SetObjectReferenceIfMissing(battleController, "_presentationSettings", assets.PresentationSettings);
            SetObjectReferenceIfMissing(battleController, "_deck", assets.Deck);
            SetObjectReferenceIfMissing(battleController, "_encounter", assets.DefaultEncounter);
            SetObjectReferenceIfMissing(battleController, "_eventChannel", assets.EventChannel);
            EnsureBehaviourActive(battleController);
            return battleController;
        }

        /// <summary>
        /// Repairs/creates BoardRoot and reconnects board presentation to the same canonical battle channel/settings.
        /// Existing valid cell/block prefab choices are preserved.
        /// </summary>
        private static void RepairBoardRoot(
            Scene scene,
            RepairAssets assets,
            BattleEventChannelSO eventChannel,
            BattleBoardSettingsSO boardSettings,
            BattlePresentationSettingsSO presentationSettings)
        {
            List<BoardView> boardViews = GetSceneComponents<BoardView>(scene);
            BoardView boardView;
            if (boardViews.Count > 0)
            {
                boardView = boardViews[0];
            }
            else
            {
                GameObject boardRoot = GetOrCreateRoot(scene, "BoardRoot");
                boardView = boardRoot.AddComponent<BoardView>();
            }

            SetObjectReference(boardView, "_eventChannel", eventChannel);
            SetObjectReference(boardView, "_boardSettings", boardSettings);
            SetObjectReference(boardView, "_presentationSettings", presentationSettings);
            SetObjectReferenceIfMissing(boardView, "_cellPrefab", assets.CellPrefab);
            SetObjectReferenceIfMissing(boardView, "_blockPrefab", assets.BlockPrefab);
            EnsureBehaviourActive(boardView);
        }

        /// <summary>
        /// Ensures exactly one usable scene Player exists before BattleController.Start publishes its initial position snapshot.
        /// </summary>
        private static void RepairPlayerRoot(
            Scene scene,
            RepairAssets assets,
            BattleEventChannelSO eventChannel,
            BattleBoardSettingsSO boardSettings,
            BattlePresentationSettingsSO presentationSettings)
        {
            List<PlayerController> players = GetSceneComponents<PlayerController>(scene);
            PlayerController player;
            if (players.Count > 0)
            {
                player = players[0];
            }
            else
            {
                UnityEngine.Object instance = PrefabUtility.InstantiatePrefab(assets.PlayerPrefab.gameObject, scene);
                GameObject playerObject = instance as GameObject;
                if (playerObject == null)
                {
                    throw new InvalidOperationException("Failed to instantiate POC Player prefab during scene repair.");
                }

                GameObject playerRoot = GetOrCreateRoot(scene, "PlayerRoot");
                playerObject.transform.SetParent(playerRoot.transform, true);
                player = playerObject.GetComponent<PlayerController>();
            }

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement == null)
            {
                movement = player.gameObject.AddComponent<PlayerMovement>();
            }

            SetObjectReference(player, "_eventChannel", eventChannel);
            SetObjectReference(player, "_boardSettings", boardSettings);
            SetObjectReference(player, "_presentationSettings", presentationSettings);
            SetObjectReference(player, "_movement", movement);
            EnsureBehaviourActive(movement);
            EnsureBehaviourActive(player);
        }

        /// <summary>
        /// Ensures EnemyPartyView exists before BattleController.Start publishes the encounter setup event.
        /// </summary>
        private static void RepairEnemyRoot(
            Scene scene,
            RepairAssets assets,
            BattleEventChannelSO eventChannel,
            BattlePresentationSettingsSO presentationSettings)
        {
            List<EnemyPartyView> enemyPartyViews = GetSceneComponents<EnemyPartyView>(scene);
            EnemyPartyView enemyPartyView;
            if (enemyPartyViews.Count > 0)
            {
                enemyPartyView = enemyPartyViews[0];
            }
            else
            {
                GameObject enemyRoot = GetOrCreateRoot(scene, "EnemyRoot");
                enemyPartyView = enemyRoot.AddComponent<EnemyPartyView>();
            }

            SetObjectReference(enemyPartyView, "_eventChannel", eventChannel);
            SetObjectReference(enemyPartyView, "_presentationSettings", presentationSettings);
            SetObjectReference(enemyPartyView, "_enemyViewPrefab", assets.EnemyPrefab);
            EnsureBehaviourActive(enemyPartyView);
        }

        /// <summary>
        /// Repairs the main camera pointer input and removes the previous EventSystem dependency from scene composition.
        /// </summary>
        private static void RepairMainCamera(
            Scene scene,
            BattleEventChannelSO eventChannel,
            BattleBoardSettingsSO boardSettings,
            BattlePresentationSettingsSO presentationSettings)
        {
            Camera camera = FindPreferredCamera(scene);
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 10f, -10f);
                cameraObject.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
                camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 6.7f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.035f, 0.045f, 1f);
            }

            BattlePointerInput pointerInput = camera.GetComponent<BattlePointerInput>();
            if (pointerInput == null)
            {
                pointerInput = camera.gameObject.AddComponent<BattlePointerInput>();
            }

            SetObjectReference(pointerInput, "_eventChannel", eventChannel);
            SetObjectReference(pointerInput, "_boardSettings", boardSettings);
            SetObjectReference(pointerInput, "_presentationSettings", presentationSettings);
            SetObjectReference(pointerInput, "_camera", camera);
            EnsureBehaviourActive(camera);
            EnsureBehaviourActive(pointerInput);
        }

        /// <summary>
        /// Adds/reuses the POC OnGUI presenter on BattleRoot and connects only the shared event channel.
        /// </summary>
        private static void RepairOnGuiPresenter(GameObject battleRoot, BattleEventChannelSO eventChannel)
        {
            BattleOnGuiPresenter presenter = battleRoot.GetComponent<BattleOnGuiPresenter>();
            if (presenter == null)
            {
                presenter = battleRoot.AddComponent<BattleOnGuiPresenter>();
            }

            SetObjectReference(presenter, "_eventChannel", eventChannel);
            EnsureBehaviourActive(presenter);
        }

        /// <summary>
        /// Removes legacy world TextMesh children from EnemyView prefab while preserving its sprite/material/transform setup.
        /// </summary>
        private static void RepairEnemyViewPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH) == null)
            {
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(ENEMY_PREFAB_PATH);
            try
            {
                EnemyView enemyView = prefabRoot.GetComponent<EnemyView>();
                if (enemyView == null)
                {
                    enemyView = prefabRoot.AddComponent<EnemyView>();
                }

                SpriteRenderer spriteRenderer = prefabRoot.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = prefabRoot.AddComponent<SpriteRenderer>();
                }

                TextMesh[] textMeshes = prefabRoot.GetComponentsInChildren<TextMesh>(true);
                for (int textIndex = 0; textIndex < textMeshes.Length; textIndex++)
                {
                    TextMesh textMesh = textMeshes[textIndex];
                    if (textMesh == null || textMesh.gameObject == prefabRoot)
                    {
                        continue;
                    }

                    string objectName = textMesh.gameObject.name;
                    if (objectName == "StatusText" || objectName == "IntentText")
                    {
                        UnityEngine.Object.DestroyImmediate(textMesh.gameObject);
                    }
                }

                prefabRoot.SetActive(true);
                enemyView.enabled = true;
                spriteRenderer.enabled = true;
                SetObjectReference(enemyView, "_spriteRenderer", spriteRenderer);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, ENEMY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// Repairs the generic Block prefab's world-space TMP effect label reference without changing gameplay data assets.
        /// </summary>
        private static void RepairBlockViewPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BLOCK_PREFAB_PATH) == null)
            {
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(BLOCK_PREFAB_PATH);
            try
            {
                BlockView blockView = prefabRoot.GetComponent<BlockView>();
                if (blockView == null)
                {
                    blockView = prefabRoot.AddComponent<BlockView>();
                }

                Renderer renderer = prefabRoot.GetComponent<Renderer>();
                TMP_Text effectText = prefabRoot.GetComponentInChildren<TMP_Text>(true);
                if (effectText == null)
                {
                    effectText = CreateBlockEffectText(prefabRoot.transform);
                }

                SetObjectReference(blockView, "_renderer", renderer);
                SetObjectReference(blockView, "_effectText", effectText);
                prefabRoot.SetActive(true);
                blockView.enabled = true;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, BLOCK_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// Creates a minimal world-space TMP block label only when the current prefab does not already contain one.
        /// </summary>
        private static TMP_Text CreateBlockEffectText(Transform parent)
        {
            const float TEXT_LOCAL_Y = 0.66f;
            const float TEXT_RECT_SIZE = 0.8f;
            const float TEXT_FONT_SIZE_MIN = 2f;
            const float TEXT_FONT_SIZE_MAX = 4f;

            GameObject textObject = new GameObject("Text (TMP)", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rectTransform.localPosition = new Vector3(0f, TEXT_LOCAL_Y, 0f);
            rectTransform.sizeDelta = new Vector2(TEXT_RECT_SIZE, TEXT_RECT_SIZE);

            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.text = "Block";
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = TEXT_FONT_SIZE_MIN;
            text.fontSizeMax = TEXT_FONT_SIZE_MAX;
            return text;
        }

        /// <summary>
        /// Validates required scene composition, shared event references, world view prefabs, and absence of generated legacy UI.
        /// </summary>
        private static bool ValidateSceneSetup(Scene scene, bool logDetails)
        {
            bool valid = true;

            List<BattleController> controllers = GetSceneComponents<BattleController>(scene);
            List<BoardView> boardViews = GetSceneComponents<BoardView>(scene);
            List<PlayerController> players = GetSceneComponents<PlayerController>(scene);
            List<EnemyPartyView> enemyPartyViews = GetSceneComponents<EnemyPartyView>(scene);
            List<BattleOnGuiPresenter> onGuiPresenters = GetSceneComponents<BattleOnGuiPresenter>(scene);
            List<BattlePointerInput> pointerInputs = GetSceneComponents<BattlePointerInput>(scene);

            valid &= ValidateExactCount("BattleController", controllers.Count, 1);
            valid &= ValidateExactCount("BoardView", boardViews.Count, 1);
            valid &= ValidateExactCount("PlayerController", players.Count, 1);
            valid &= ValidateExactCount("EnemyPartyView", enemyPartyViews.Count, 1);
            valid &= ValidateExactCount("BattleOnGuiPresenter", onGuiPresenters.Count, 1);
            valid &= ValidateExactCount("BattlePointerInput", pointerInputs.Count, 1);

            if (controllers.Count == 1)
            {
                valid &= ValidateBehaviourActive("BattleController", controllers[0]);
            }

            if (boardViews.Count == 1)
            {
                valid &= ValidateBehaviourActive("BoardView", boardViews[0]);
            }

            if (players.Count == 1)
            {
                valid &= ValidateBehaviourActive("PlayerController", players[0]);
                PlayerMovement movement = players[0].GetComponent<PlayerMovement>();
                valid &= ValidateBehaviourActive("PlayerMovement", movement);
            }

            if (enemyPartyViews.Count == 1)
            {
                valid &= ValidateBehaviourActive("EnemyPartyView", enemyPartyViews[0]);
            }

            if (onGuiPresenters.Count == 1)
            {
                valid &= ValidateBehaviourActive("BattleOnGuiPresenter", onGuiPresenters[0]);
            }

            if (pointerInputs.Count == 1)
            {
                valid &= ValidateBehaviourActive("BattlePointerInput", pointerInputs[0]);
            }

            if (HasRootNamed(scene, "BattleHud"))
            {
                Debug.LogError("POC Battle validation: legacy BattleHud root still exists in the scene. Run OnGUI scene repair.");
                valid = false;
            }

            if (HasRootNamed(scene, "EventSystem"))
            {
                Debug.LogWarning("POC Battle validation: generated EventSystem root still exists. It is not required by the POC OnGUI setup.");
            }

            if (controllers.Count == 1)
            {
                BattleController controller = controllers[0];
                BattleEventChannelSO eventChannel = GetObjectReference<BattleEventChannelSO>(controller, "_eventChannel");
                BattleBoardSettingsSO boardSettings = GetObjectReference<BattleBoardSettingsSO>(controller, "_boardSettings");
                BattlePresentationSettingsSO presentationSettings =
                    GetObjectReference<BattlePresentationSettingsSO>(controller, "_presentationSettings");
                PlayerBaseStatsSO playerStats = GetObjectReference<PlayerBaseStatsSO>(controller, "_playerBaseStats");
                DeckDefinitionSO deck = GetObjectReference<DeckDefinitionSO>(controller, "_deck");
                EncounterDefinitionSO encounter = GetObjectReference<EncounterDefinitionSO>(controller, "_encounter");

                valid &= ValidateAssigned("BattleController._eventChannel", eventChannel);
                valid &= ValidateAssigned("BattleController._boardSettings", boardSettings);
                valid &= ValidateAssigned("BattleController._presentationSettings", presentationSettings);
                valid &= ValidateAssigned("BattleController._playerBaseStats", playerStats);
                valid &= ValidateAssigned("BattleController._deck", deck);
                valid &= ValidateAssigned("BattleController._encounter", encounter);

                if (encounter != null && encounter.Enemies.Count == 0)
                {
                    Debug.LogError("POC Battle validation: current EncounterDefinitionSO contains no enemies.");
                    valid = false;
                }

                if (boardViews.Count == 1)
                {
                    valid &= ValidateSameReference("BoardView._eventChannel", GetObjectReference<BattleEventChannelSO>(boardViews[0], "_eventChannel"), eventChannel);
                    valid &= ValidateSameReference("BoardView._boardSettings", GetObjectReference<BattleBoardSettingsSO>(boardViews[0], "_boardSettings"), boardSettings);
                    valid &= ValidateSameReference(
                        "BoardView._presentationSettings",
                        GetObjectReference<BattlePresentationSettingsSO>(boardViews[0], "_presentationSettings"),
                        presentationSettings);
                    valid &= ValidateAssigned("BoardView._cellPrefab", GetObjectReference<BoardCellView>(boardViews[0], "_cellPrefab"));
                    valid &= ValidateAssigned("BoardView._blockPrefab", GetObjectReference<BlockView>(boardViews[0], "_blockPrefab"));
                }

                if (players.Count == 1)
                {
                    PlayerController player = players[0];
                    valid &= ValidateSameReference("PlayerController._eventChannel", GetObjectReference<BattleEventChannelSO>(player, "_eventChannel"), eventChannel);
                    valid &= ValidateSameReference("PlayerController._boardSettings", GetObjectReference<BattleBoardSettingsSO>(player, "_boardSettings"), boardSettings);
                    valid &= ValidateSameReference(
                        "PlayerController._presentationSettings",
                        GetObjectReference<BattlePresentationSettingsSO>(player, "_presentationSettings"),
                        presentationSettings);
                    valid &= ValidateAssigned("PlayerController._movement", GetObjectReference<PlayerMovement>(player, "_movement"));
                }

                if (enemyPartyViews.Count == 1)
                {
                    EnemyPartyView enemyPartyView = enemyPartyViews[0];
                    valid &= ValidateSameReference(
                        "EnemyPartyView._eventChannel",
                        GetObjectReference<BattleEventChannelSO>(enemyPartyView, "_eventChannel"),
                        eventChannel);
                    valid &= ValidateSameReference(
                        "EnemyPartyView._presentationSettings",
                        GetObjectReference<BattlePresentationSettingsSO>(enemyPartyView, "_presentationSettings"),
                        presentationSettings);
                    valid &= ValidateAssigned("EnemyPartyView._enemyViewPrefab", GetObjectReference<EnemyView>(enemyPartyView, "_enemyViewPrefab"));
                }

                if (onGuiPresenters.Count == 1)
                {
                    valid &= ValidateSameReference(
                        "BattleOnGuiPresenter._eventChannel",
                        GetObjectReference<BattleEventChannelSO>(onGuiPresenters[0], "_eventChannel"),
                        eventChannel);
                }

                if (pointerInputs.Count == 1)
                {
                    BattlePointerInput pointerInput = pointerInputs[0];
                    valid &= ValidateSameReference(
                        "BattlePointerInput._eventChannel",
                        GetObjectReference<BattleEventChannelSO>(pointerInput, "_eventChannel"),
                        eventChannel);
                    valid &= ValidateSameReference(
                        "BattlePointerInput._boardSettings",
                        GetObjectReference<BattleBoardSettingsSO>(pointerInput, "_boardSettings"),
                        boardSettings);
                    valid &= ValidateSameReference(
                        "BattlePointerInput._presentationSettings",
                        GetObjectReference<BattlePresentationSettingsSO>(pointerInput, "_presentationSettings"),
                        presentationSettings);
                    valid &= ValidateAssigned("BattlePointerInput._camera", GetObjectReference<Camera>(pointerInput, "_camera"));
                }
            }

            EnemyView enemyPrefab = LoadPrefabComponent<EnemyView>(ENEMY_PREFAB_PATH);
            if (enemyPrefab == null)
            {
                Debug.LogError($"POC Battle validation: EnemyView prefab is missing at {ENEMY_PREFAB_PATH}.");
                valid = false;
            }
            else
            {
                TextMesh[] textMeshes = enemyPrefab.GetComponentsInChildren<TextMesh>(true);
                for (int textIndex = 0; textIndex < textMeshes.Length; textIndex++)
                {
                    TextMesh textMesh = textMeshes[textIndex];
                    if (textMesh != null && (textMesh.gameObject.name == "StatusText" || textMesh.gameObject.name == "IntentText"))
                    {
                        Debug.LogError("POC Battle validation: EnemyView prefab still contains legacy StatusText/IntentText UI children.");
                        valid = false;
                        break;
                    }
                }

                valid &= ValidateAssigned("EnemyView._spriteRenderer", GetObjectReference<SpriteRenderer>(enemyPrefab, "_spriteRenderer"));
            }

            BlockView blockPrefab = LoadPrefabComponent<BlockView>(BLOCK_PREFAB_PATH);
            if (blockPrefab == null)
            {
                Debug.LogError($"POC Battle validation: Block prefab is missing at {BLOCK_PREFAB_PATH}.");
                valid = false;
            }
            else
            {
                valid &= ValidateAssigned("BlockView._renderer", GetObjectReference<Renderer>(blockPrefab, "_renderer"));
                valid &= ValidateAssigned("BlockView._effectText", GetObjectReference<TMP_Text>(blockPrefab, "_effectText"));
            }

            if (logDetails && valid)
            {
                Debug.Log("POC Battle validation: world views, data-driven block TMP labels, OnGUI presenter, pointer references, and shared event channel are valid.");
            }

            return valid;
        }

        /// <summary>
        /// Loads one component from a prefab asset through its root GameObject.
        /// </summary>
        private static T LoadPrefabComponent<T>(string prefabPath) where T : Component
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return prefabRoot != null ? prefabRoot.GetComponent<T>() : null;
        }

        /// <summary>
        /// Returns the preferred scene Camera, favoring the MainCamera tag without using runtime Find APIs.
        /// </summary>
        private static Camera FindPreferredCamera(Scene scene)
        {
            List<Camera> cameras = GetSceneComponents<Camera>(scene);
            for (int cameraIndex = 0; cameraIndex < cameras.Count; cameraIndex++)
            {
                Camera camera = cameras[cameraIndex];
                if (camera != null && camera.CompareTag("MainCamera"))
                {
                    return camera;
                }
            }

            return cameras.Count > 0 ? cameras[0] : null;
        }

        /// <summary>
        /// Returns true when the target scene contains a root GameObject with the requested exact name.
        /// </summary>
        private static bool HasRootNamed(Scene scene, string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (roots[rootIndex].name == rootName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a named root GameObject or creates one inside the target scene.
        /// </summary>
        private static GameObject GetOrCreateRoot(Scene scene, string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (roots[rootIndex].name == rootName)
                {
                    return roots[rootIndex];
                }
            }

            GameObject root = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        /// <summary>
        /// Collects scene components by traversing root hierarchies; no GameObject.Find/FindObjectOfType is used.
        /// </summary>
        private static List<T> GetSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> results = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                T[] components = roots[rootIndex].GetComponentsInChildren<T>(true);
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    results.Add(components[componentIndex]);
                }
            }

            return results;
        }

        /// <summary>
        /// Reads one private serialized object reference through Unity serialization without runtime reflection coupling.
        /// </summary>
        private static T GetObjectReference<T>(UnityEngine.Object target, string propertyName) where T : UnityEngine.Object
        {
            if (target == null)
            {
                return null;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        /// <summary>
        /// Assigns one private serialized object reference and marks its owner dirty.
        /// </summary>
        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"POC Battle repair could not find serialized property {target.GetType().Name}.{propertyName}.", target);
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Assigns a private serialized object reference only when the existing reference is missing.
        /// </summary>
        private static void SetObjectReferenceIfMissing(UnityEngine.Object target, string propertyName, UnityEngine.Object fallbackValue)
        {
            if (GetObjectReference<UnityEngine.Object>(target, propertyName) == null)
            {
                SetObjectReference(target, propertyName, fallbackValue);
            }
        }

        /// <summary>
        /// Activates a required Behaviour and every parent GameObject so scene event subscriptions/Start execute reliably.
        /// </summary>
        private static void EnsureBehaviourActive(Behaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            Transform current = behaviour.transform;
            while (current != null)
            {
                current.gameObject.SetActive(true);
                current = current.parent;
            }

            behaviour.enabled = true;
            EditorUtility.SetDirty(behaviour);
        }

        /// <summary>
        /// Validates that a required Behaviour is enabled and active in the scene hierarchy.
        /// </summary>
        private static bool ValidateBehaviourActive(string label, Behaviour behaviour)
        {
            if (behaviour != null && behaviour.enabled && behaviour.gameObject.activeInHierarchy)
            {
                return true;
            }

            Debug.LogError($"POC Battle validation: {label} is missing, disabled, or inactive in hierarchy.");
            return false;
        }

        /// <summary>
        /// Validates an exact scene-component count.
        /// </summary>
        private static bool ValidateExactCount(string label, int actualCount, int expectedCount)
        {
            if (actualCount == expectedCount)
            {
                return true;
            }

            Debug.LogError($"POC Battle validation: expected {expectedCount} {label}, but found {actualCount}.");
            return false;
        }

        /// <summary>
        /// Validates that a required serialized object reference is assigned.
        /// </summary>
        private static bool ValidateAssigned(string label, UnityEngine.Object value)
        {
            if (value != null)
            {
                return true;
            }

            Debug.LogError($"POC Battle validation: {label} is None.");
            return false;
        }

        /// <summary>
        /// Validates that a presentation component uses the same ScriptableObject reference as BattleController.
        /// </summary>
        private static bool ValidateSameReference(string label, UnityEngine.Object actual, UnityEngine.Object expected)
        {
            if (actual == expected && actual != null)
            {
                return true;
            }

            Debug.LogError($"POC Battle validation: {label} does not match the BattleController canonical reference.");
            return false;
        }

        /// <summary>
        /// Immutable editor-only bundle of existing scene repair dependencies.
        /// </summary>
        private readonly struct RepairAssets
        {
            /// <summary>Shared battle event channel.</summary>
            public BattleEventChannelSO EventChannel { get; }

            /// <summary>Board gameplay settings.</summary>
            public BattleBoardSettingsSO BoardSettings { get; }

            /// <summary>Player base stats.</summary>
            public PlayerBaseStatsSO PlayerStats { get; }

            /// <summary>Presentation tuning.</summary>
            public BattlePresentationSettingsSO PresentationSettings { get; }

            /// <summary>POC player deck.</summary>
            public DeckDefinitionSO Deck { get; }

            /// <summary>Fallback encounter used only when BattleController encounter reference is missing.</summary>
            public EncounterDefinitionSO DefaultEncounter { get; }

            /// <summary>Generated 3D player prefab.</summary>
            public PlayerController PlayerPrefab { get; }

            /// <summary>Generated 2D enemy sprite prefab.</summary>
            public EnemyView EnemyPrefab { get; }

            /// <summary>Generated board cell prefab.</summary>
            public BoardCellView CellPrefab { get; }

            /// <summary>Generated block prefab.</summary>
            public BlockView BlockPrefab { get; }

            /// <summary>
            /// Creates one immutable scene-repair dependency bundle.
            /// </summary>
            public RepairAssets(
                BattleEventChannelSO eventChannel,
                BattleBoardSettingsSO boardSettings,
                PlayerBaseStatsSO playerStats,
                BattlePresentationSettingsSO presentationSettings,
                DeckDefinitionSO deck,
                EncounterDefinitionSO defaultEncounter,
                PlayerController playerPrefab,
                EnemyView enemyPrefab,
                BoardCellView cellPrefab,
                BlockView blockPrefab)
            {
                EventChannel = eventChannel;
                BoardSettings = boardSettings;
                PlayerStats = playerStats;
                PresentationSettings = presentationSettings;
                Deck = deck;
                DefaultEncounter = defaultEncounter;
                PlayerPrefab = playerPrefab;
                EnemyPrefab = enemyPrefab;
                CellPrefab = cellPrefab;
                BlockPrefab = blockPrefab;
            }
        }
    }
}
#endif
