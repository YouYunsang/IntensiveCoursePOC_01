#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PocBattle.Data;
using PocBattle.Presentation;
using PocBattle.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocBattle.Editor
{
    /// <summary>
    /// Incremental POC installer for heal, persistent runtime deck/gold, five-stage run flow, world loot, and OnGUI looting.
    /// It preserves existing scene/gameplay assets unless a new run field must be added or repaired.
    /// </summary>
    public static class PocBattleRunStagePatchInstaller
    {
        /// <summary>Stable generated POC data directory.</summary>
        private const string DATA_ROOT = "Assets/07.Data/POCBattle";

        /// <summary>Stable generated POC prefab directory.</summary>
        private const string PREFAB_ROOT = "Assets/02.Prefabs/POCBattle";

        /// <summary>Existing POC scene repaired in place by this incremental patch.</summary>
        private const string SCENE_PATH = "Assets/00.Scenes/POCBattleScene.unity";

        /// <summary>Existing generic block prefab reused by floating loot presentation.</summary>
        private const string BLOCK_PREFAB_PATH = PREFAB_ROOT + "/Block.prefab";

        /// <summary>
        /// Creates/updates only run-stage data and required scene components, then validates the result.
        /// </summary>
        [MenuItem("Tools/POC Battle/Apply Run & Stage Patch")]
        public static void ApplyRunStagePatch()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(DATA_ROOT))
            {
                Debug.LogError($"Run/stage patch requires the existing POC data folder at {DATA_ROOT}.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) == null)
            {
                Debug.LogError($"Run/stage patch requires the existing POC scene at {SCENE_PATH}.");
                return;
            }

            if (!TryLoadExistingAssets(out ExistingAssets existing))
            {
                return;
            }

            RunAssets runAssets = CreateOrUpdateRunAssets(existing);
            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            RepairRunScene(scene, existing, runAssets);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool valid = ValidateRunStageSetup(scene, existing, runAssets, true);
            Debug.Log(valid
                ? "POC Run & Stage patch applied. Five-stage run, heal loot, runtime deck capacity, gold, loot/fade presentation, and defeat restart flow are ready."
                : "POC Run & Stage patch applied, but validation found errors. Check Console messages above.");
        }

        /// <summary>Validates existing run-stage data and scene references without rewriting them.</summary>
        [MenuItem("Tools/POC Battle/Validate Run & Stage Setup")]
        public static void ValidateRunStageSetupMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!TryLoadExistingAssets(out ExistingAssets existing))
            {
                return;
            }

            RunAssets runAssets = LoadRunAssets();
            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            bool valid = ValidateRunStageSetup(scene, existing, runAssets, true);
            if (valid)
            {
                Debug.Log("POC Run & Stage setup validation passed.");
            }
        }

        /// <summary>Loads immutable assets already created by previous POC patches.</summary>
        private static bool TryLoadExistingAssets(out ExistingAssets assets)
        {
            BattleEventChannelSO eventChannel = AssetDatabase.LoadAssetAtPath<BattleEventChannelSO>($"{DATA_ROOT}/BattleEventChannel.asset");
            BattleBoardSettingsSO boardSettings = AssetDatabase.LoadAssetAtPath<BattleBoardSettingsSO>($"{DATA_ROOT}/BattleBoardSettings.asset");
            BattlePresentationSettingsSO presentationSettings = AssetDatabase.LoadAssetAtPath<BattlePresentationSettingsSO>($"{DATA_ROOT}/BattlePresentationSettings.asset");
            PlayerBaseStatsSO playerStats = AssetDatabase.LoadAssetAtPath<PlayerBaseStatsSO>($"{DATA_ROOT}/PlayerBaseStats.asset");
            DeckDefinitionSO startingDeck = AssetDatabase.LoadAssetAtPath<DeckDefinitionSO>($"{DATA_ROOT}/Deck_POC.asset");
            EncounterDefinitionSO encounterA = AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>($"{DATA_ROOT}/Encounter_EnemyA.asset");
            EncounterDefinitionSO encounterB = AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>($"{DATA_ROOT}/Encounter_EnemyB.asset");
            EnemyDefinitionSO enemyA = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{DATA_ROOT}/Enemy_A.asset");
            EnemyDefinitionSO enemyB = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>($"{DATA_ROOT}/Enemy_B.asset");
            BlockDefinitionSO attack2 = AssetDatabase.LoadAssetAtPath<BlockDefinitionSO>($"{DATA_ROOT}/Block_Attack_2.asset");
            BlockDefinitionSO attack4 = AssetDatabase.LoadAssetAtPath<BlockDefinitionSO>($"{DATA_ROOT}/Block_Attack_4.asset");
            BlockDefinitionSO shield3 = AssetDatabase.LoadAssetAtPath<BlockDefinitionSO>($"{DATA_ROOT}/Block_Shield_3.asset");
            BlockDefinitionSO shield5 = AssetDatabase.LoadAssetAtPath<BlockDefinitionSO>($"{DATA_ROOT}/Block_Shield_5.asset");
            BlockDefinitionSO critical = AssetDatabase.LoadAssetAtPath<BlockDefinitionSO>($"{DATA_ROOT}/Block_Critical.asset");
            BlockView blockPrefab = LoadPrefabComponent<BlockView>(BLOCK_PREFAB_PATH);

            bool valid = true;
            valid &= RequireAsset(eventChannel, "BattleEventChannel.asset");
            valid &= RequireAsset(boardSettings, "BattleBoardSettings.asset");
            valid &= RequireAsset(presentationSettings, "BattlePresentationSettings.asset");
            valid &= RequireAsset(playerStats, "PlayerBaseStats.asset");
            valid &= RequireAsset(startingDeck, "Deck_POC.asset");
            valid &= RequireAsset(encounterA, "Encounter_EnemyA.asset");
            valid &= RequireAsset(encounterB, "Encounter_EnemyB.asset");
            valid &= RequireAsset(enemyA, "Enemy_A.asset");
            valid &= RequireAsset(enemyB, "Enemy_B.asset");
            valid &= RequireAsset(attack2, "Block_Attack_2.asset");
            valid &= RequireAsset(attack4, "Block_Attack_4.asset");
            valid &= RequireAsset(shield3, "Block_Shield_3.asset");
            valid &= RequireAsset(shield5, "Block_Shield_5.asset");
            valid &= RequireAsset(critical, "Block_Critical.asset");
            valid &= RequireAsset(blockPrefab, BLOCK_PREFAB_PATH);

            assets = new ExistingAssets(
                eventChannel,
                boardSettings,
                presentationSettings,
                playerStats,
                startingDeck,
                encounterA,
                encounterB,
                enemyA,
                enemyB,
                attack2,
                attack4,
                shield3,
                shield5,
                critical,
                blockPrefab);
            return valid;
        }

        /// <summary>Creates heal, five stage assets, and the top-level run asset while preserving existing battle content.</summary>
        private static RunAssets CreateOrUpdateRunAssets(ExistingAssets existing)
        {
            HealBlockEffectSO healEffect = LoadOrCreateAsset<HealBlockEffectSO>($"{DATA_ROOT}/Effect_Heal_3.asset");
            SetInt(healEffect, "_amount", 3);

            BlockDefinitionSO healBlock = LoadOrCreateAsset<BlockDefinitionSO>($"{DATA_ROOT}/Block_Heal_3.asset");
            SetString(healBlock, "_displayName", "Heal");
            SetColor(healBlock, "_displayColor", new Color(0.2f, 0.85f, 0.35f, 1f));
            SetBool(healBlock, "_isTrap", false);
            SetObjectArray(healBlock, "_effects", new UnityEngine.Object[] { healEffect });

            SetInt(existing.PlayerStats, "_deckCapacity", 10);
            SetInt(existing.EnemyA, "_goldReward", 10);
            SetInt(existing.EnemyB, "_goldReward", 15);
            ConfigurePresentationDefaults(existing.PresentationSettings);

            StageDefinitionSO[] stages = new StageDefinitionSO[5];
            stages[0] = ConfigureStage(
                1,
                existing,
                new RewardConfig(existing.Attack2, 4),
                new RewardConfig(existing.Shield3, 4),
                new RewardConfig(healBlock, 3));
            stages[1] = ConfigureStage(
                2,
                existing,
                new RewardConfig(existing.Attack2, 3),
                new RewardConfig(existing.Attack4, 2),
                new RewardConfig(existing.Shield3, 3),
                new RewardConfig(healBlock, 3),
                new RewardConfig(existing.Critical, 1));
            stages[2] = ConfigureStage(
                3,
                existing,
                new RewardConfig(existing.Attack4, 3),
                new RewardConfig(existing.Shield5, 3),
                new RewardConfig(healBlock, 3),
                new RewardConfig(existing.Critical, 2),
                new RewardConfig(existing.Attack2, 1),
                new RewardConfig(existing.Shield3, 1));
            stages[3] = ConfigureStage(
                4,
                existing,
                new RewardConfig(existing.Attack4, 4),
                new RewardConfig(existing.Shield5, 4),
                new RewardConfig(healBlock, 3),
                new RewardConfig(existing.Critical, 2));
            stages[4] = ConfigureStage(
                5,
                existing,
                new RewardConfig(existing.Attack4, 4),
                new RewardConfig(existing.Shield5, 4),
                new RewardConfig(healBlock, 3),
                new RewardConfig(existing.Critical, 3));

            RunDefinitionSO runDefinition = LoadOrCreateAsset<RunDefinitionSO>($"{DATA_ROOT}/Run_POC.asset");
            UnityEngine.Object[] stageObjects = new UnityEngine.Object[stages.Length];
            for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
            {
                stageObjects[stageIndex] = stages[stageIndex];
            }
            SetObjectArray(runDefinition, "_stages", stageObjects);
            SetBool(runDefinition, "_useFixedRandomSeed", false);
            SetInt(runDefinition, "_fixedRandomSeed", 24680);

            EditorUtility.SetDirty(existing.PlayerStats);
            EditorUtility.SetDirty(existing.EnemyA);
            EditorUtility.SetDirty(existing.EnemyB);
            EditorUtility.SetDirty(existing.PresentationSettings);
            AssetDatabase.SaveAssets();
            return new RunAssets(healEffect, healBlock, stages, runDefinition);
        }

        /// <summary>Creates/updates one stage with the POC one-enemy encounter pool and configured weighted loot entries.</summary>
        private static StageDefinitionSO ConfigureStage(
            int stageNumber,
            ExistingAssets existing,
            params RewardConfig[] rewards)
        {
            StageDefinitionSO stage = LoadOrCreateAsset<StageDefinitionSO>($"{DATA_ROOT}/Stage_{stageNumber:00}.asset");
            SetObjectArray(stage, "_encounterPool", new UnityEngine.Object[] { existing.EncounterA, existing.EncounterB });

            SerializedObject serializedStage = new SerializedObject(stage);
            SerializedProperty lootPool = serializedStage.FindProperty("_lootPool");
            lootPool.arraySize = rewards.Length;
            for (int rewardIndex = 0; rewardIndex < rewards.Length; rewardIndex++)
            {
                SerializedProperty entry = lootPool.GetArrayElementAtIndex(rewardIndex);
                entry.FindPropertyRelative("_block").objectReferenceValue = rewards[rewardIndex].Block;
                entry.FindPropertyRelative("_weight").intValue = Mathf.Max(1, rewards[rewardIndex].Weight);
            }
            serializedStage.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            return stage;
        }

        /// <summary>Writes explicit POC loot/death/fade defaults into the existing presentation data asset.</summary>
        private static void ConfigurePresentationDefaults(BattlePresentationSettingsSO settings)
        {
            SerializedObject serialized = new SerializedObject(settings);
            serialized.FindProperty("_enemyDeathDuration").floatValue = 0.3f;
            serialized.FindProperty("_lootBlockScaleMultiplier").floatValue = 1.15f;
            serialized.FindProperty("_lootHeightOffset").floatValue = 0.4f;
            serialized.FindProperty("_lootFloatHeight").floatValue = 0.25f;
            serialized.FindProperty("_lootFloatDuration").floatValue = 0.8f;
            serialized.FindProperty("_lootGlowIntensity").floatValue = 2.5f;
            serialized.FindProperty("_lootGlowRange").floatValue = 3f;
            serialized.FindProperty("_lootGlowPulseDuration").floatValue = 0.65f;
            serialized.FindProperty("_lootClickRadiusPixels").floatValue = 90f;
            serialized.FindProperty("_stageFadeDuration").floatValue = 0.45f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Repairs only run-related scene composition while preserving existing board/player/enemy hierarchy and data choices.</summary>
        private static void RepairRunScene(Scene scene, ExistingAssets existing, RunAssets runAssets)
        {
            BattleController battleController = GetFirstSceneComponent<BattleController>(scene);
            if (battleController == null)
            {
                throw new InvalidOperationException("POCBattleScene is missing BattleController. Run the previous scene repair patch first.");
            }

            SetBool(battleController, "_autoStartStandalone", false);
            GameObject battleRoot = battleController.gameObject;

            RunController runController = battleRoot.GetComponent<RunController>();
            if (runController == null)
            {
                runController = battleRoot.AddComponent<RunController>();
            }
            SetObject(runController, "_battleController", battleController);
            SetObject(runController, "_playerBaseStats", existing.PlayerStats);
            SetObject(runController, "_startingDeck", existing.StartingDeck);
            SetObject(runController, "_runDefinition", runAssets.RunDefinition);
            SetObject(runController, "_boardSettings", existing.BoardSettings);
            SetObject(runController, "_presentationSettings", existing.PresentationSettings);
            SetObject(runController, "_eventChannel", existing.EventChannel);

            RunOnGuiPresenter runGui = battleRoot.GetComponent<RunOnGuiPresenter>();
            if (runGui == null)
            {
                runGui = battleRoot.AddComponent<RunOnGuiPresenter>();
            }
            SetObject(runGui, "_eventChannel", existing.EventChannel);
            SetObject(runGui, "_presentationSettings", existing.PresentationSettings);

            Camera camera = GetFirstSceneComponent<Camera>(scene);
            if (camera == null)
            {
                throw new InvalidOperationException("POCBattleScene is missing Main Camera.");
            }

            LootPointerInput lootPointer = camera.GetComponent<LootPointerInput>();
            if (lootPointer == null)
            {
                lootPointer = camera.gameObject.AddComponent<LootPointerInput>();
            }
            SetObject(lootPointer, "_eventChannel", existing.EventChannel);
            SetObject(lootPointer, "_presentationSettings", existing.PresentationSettings);
            SetObject(lootPointer, "_camera", camera);

            GameObject lootRoot = GetOrCreateRoot(scene, "LootRoot");
            LootDropPresenter lootPresenter = lootRoot.GetComponent<LootDropPresenter>();
            if (lootPresenter == null)
            {
                lootPresenter = lootRoot.AddComponent<LootDropPresenter>();
            }
            SetObject(lootPresenter, "_eventChannel", existing.EventChannel);
            SetObject(lootPresenter, "_boardSettings", existing.BoardSettings);
            SetObject(lootPresenter, "_presentationSettings", existing.PresentationSettings);
            SetObject(lootPresenter, "_blockPrefab", existing.BlockPrefab);

            EditorUtility.SetDirty(battleController);
            EditorUtility.SetDirty(runController);
            EditorUtility.SetDirty(runGui);
            EditorUtility.SetDirty(lootPointer);
            EditorUtility.SetDirty(lootPresenter);
        }

        /// <summary>Checks generated assets, deck/board capacity, and required run scene components/references.</summary>
        private static bool ValidateRunStageSetup(Scene scene, ExistingAssets existing, RunAssets runAssets, bool logErrors)
        {
            bool valid = true;
            valid &= Validate(runAssets.HealEffect != null, "Heal effect asset is missing.", logErrors);
            valid &= Validate(runAssets.HealBlock != null, "Heal block asset is missing.", logErrors);
            valid &= Validate(runAssets.RunDefinition != null, "Run_POC asset is missing.", logErrors);
            valid &= Validate(runAssets.Stages != null && runAssets.Stages.Length == 5, "POC run must contain five stage assets.", logErrors);
            valid &= Validate(existing.PlayerStats.DeckCapacity >= existing.StartingDeck.GetTotalBlockCount(), "Deck capacity is smaller than the current starting deck.", logErrors);
            valid &= Validate(existing.PlayerStats.DeckCapacity <= existing.BoardSettings.MaxDeckBlockCount, "Player deck capacity exceeds board MaxDeckBlockCount.", logErrors);

            if (runAssets.RunDefinition != null)
            {
                valid &= Validate(runAssets.RunDefinition.Stages.Count == 5, "Run_POC must reference exactly five default stages.", logErrors);
            }

            for (int stageIndex = 0; runAssets.Stages != null && stageIndex < runAssets.Stages.Length; stageIndex++)
            {
                StageDefinitionSO stage = runAssets.Stages[stageIndex];
                valid &= Validate(stage != null, $"Stage {stageIndex + 1} asset is missing.", logErrors);
                if (stage == null)
                {
                    continue;
                }

                valid &= Validate(stage.EncounterPool.Count > 0, $"Stage {stageIndex + 1} has no encounter candidates.", logErrors);
                valid &= Validate(stage.LootPool.Count > 0, $"Stage {stageIndex + 1} has no loot candidates.", logErrors);

                int guaranteedBoardCapacity = existing.BoardSettings.Columns * existing.BoardSettings.Rows - 4 - 1 - 1;
                for (int encounterIndex = 0; encounterIndex < stage.EncounterPool.Count; encounterIndex++)
                {
                    EncounterDefinitionSO encounter = stage.EncounterPool[encounterIndex];
                    valid &= Validate(encounter != null,
                        $"Stage {stageIndex + 1} contains a null encounter at index {encounterIndex}.", logErrors);
                    if (encounter == null)
                    {
                        continue;
                    }

                    valid &= Validate(encounter.Enemies.Count == 1,
                        $"Stage {stageIndex + 1} encounter '{encounter.name}' must contain exactly one enemy for the current POC content.", logErrors);
                    int trapCount = encounter.TrapDefinition != null ? encounter.TrapCount : 0;
                    valid &= Validate(existing.PlayerStats.DeckCapacity + trapCount <= guaranteedBoardCapacity,
                        $"Stage {stageIndex + 1} encounter '{encounter.name}' cannot place a full deck ({existing.PlayerStats.DeckCapacity}) plus {trapCount} traps while preserving one guaranteed escape cell.",
                        logErrors);
                }

                for (int lootIndex = 0; lootIndex < stage.LootPool.Count; lootIndex++)
                {
                    WeightedBlockRewardEntry entry = stage.LootPool[lootIndex];
                    valid &= Validate(entry != null && entry.Block != null && !entry.Block.IsTrap && entry.Weight > 0,
                        $"Stage {stageIndex + 1} contains an invalid/trap/non-positive loot entry at index {lootIndex}.", logErrors);
                }
            }

            BattleController battleController = GetFirstSceneComponent<BattleController>(scene);
            RunController runController = GetFirstSceneComponent<RunController>(scene);
            RunOnGuiPresenter runGui = GetFirstSceneComponent<RunOnGuiPresenter>(scene);
            LootDropPresenter lootPresenter = GetFirstSceneComponent<LootDropPresenter>(scene);
            LootPointerInput lootPointer = GetFirstSceneComponent<LootPointerInput>(scene);

            valid &= Validate(battleController != null, "Scene is missing BattleController.", logErrors);
            valid &= Validate(runController != null, "Scene is missing RunController.", logErrors);
            valid &= Validate(runGui != null, "Scene is missing RunOnGuiPresenter.", logErrors);
            valid &= Validate(lootPresenter != null, "Scene is missing LootDropPresenter/LootRoot.", logErrors);
            valid &= Validate(lootPointer != null, "Main Camera is missing LootPointerInput.", logErrors);
            return valid;
        }

        /// <summary>Loads run assets without creating them, used by validation menu.</summary>
        private static RunAssets LoadRunAssets()
        {
            HealBlockEffectSO healEffect = AssetDatabase.LoadAssetAtPath<HealBlockEffectSO>($"{DATA_ROOT}/Effect_Heal_3.asset");
            BlockDefinitionSO healBlock = AssetDatabase.LoadAssetAtPath<BlockDefinitionSO>($"{DATA_ROOT}/Block_Heal_3.asset");
            StageDefinitionSO[] stages = new StageDefinitionSO[5];
            for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
            {
                stages[stageIndex] = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"{DATA_ROOT}/Stage_{stageIndex + 1:00}.asset");
            }
            RunDefinitionSO runDefinition = AssetDatabase.LoadAssetAtPath<RunDefinitionSO>($"{DATA_ROOT}/Run_POC.asset");
            return new RunAssets(healEffect, healBlock, stages, runDefinition);
        }

        /// <summary>Loads or creates one concrete ScriptableObject asset at a stable generated path.</summary>
        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>Loads one component from a prefab root/children.</summary>
        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponentInChildren<T>(true) : null;
        }

        /// <summary>Returns the first scene component without using GameObject.Find/FindObjectOfType.</summary>
        private static T GetFirstSceneComponent<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                T component = roots[rootIndex].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>Gets an existing named root or creates one directly in the requested scene.</summary>
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

        /// <summary>Writes one object reference into a serialized private field.</summary>
        private static void SetObject(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException($"Could not find serialized field '{fieldName}' on {target.GetType().Name}.");
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Writes one object array into a serialized private field.</summary>
        private static void SetObjectArray(UnityEngine.Object target, string fieldName, IReadOnlyList<UnityEngine.Object> values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException($"Could not find serialized array '{fieldName}' on {target.GetType().Name}.");
            }

            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Writes one integer serialized private field.</summary>
        private static void SetInt(UnityEngine.Object target, string fieldName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Writes one boolean serialized private field.</summary>
        private static void SetBool(UnityEngine.Object target, string fieldName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Writes one string serialized private field.</summary>
        private static void SetString(UnityEngine.Object target, string fieldName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Writes one color serialized private field.</summary>
        private static void SetColor(UnityEngine.Object target, string fieldName, Color value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            property.colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Logs one validation error only when requested.</summary>
        private static bool Validate(bool condition, string message, bool logErrors)
        {
            if (condition)
            {
                return true;
            }

            if (logErrors)
            {
                Debug.LogError("POC Run/Stage validation: " + message);
            }
            return false;
        }

        /// <summary>Logs a missing required existing asset and returns false.</summary>
        private static bool RequireAsset(UnityEngine.Object asset, string name)
        {
            if (asset != null)
            {
                return true;
            }

            Debug.LogError($"POC Run/Stage patch requires existing asset '{name}' under the current POC project setup.");
            return false;
        }

        /// <summary>Immutable existing POC asset bundle used by the incremental installer.</summary>
        private readonly struct ExistingAssets
        {
            private readonly BattleEventChannelSO _eventChannel;
            private readonly BattleBoardSettingsSO _boardSettings;
            private readonly BattlePresentationSettingsSO _presentationSettings;
            private readonly PlayerBaseStatsSO _playerStats;
            private readonly DeckDefinitionSO _startingDeck;
            private readonly EncounterDefinitionSO _encounterA;
            private readonly EncounterDefinitionSO _encounterB;
            private readonly EnemyDefinitionSO _enemyA;
            private readonly EnemyDefinitionSO _enemyB;
            private readonly BlockDefinitionSO _attack2;
            private readonly BlockDefinitionSO _attack4;
            private readonly BlockDefinitionSO _shield3;
            private readonly BlockDefinitionSO _shield5;
            private readonly BlockDefinitionSO _critical;
            private readonly BlockView _blockPrefab;

            public BattleEventChannelSO EventChannel => _eventChannel;
            public BattleBoardSettingsSO BoardSettings => _boardSettings;
            public BattlePresentationSettingsSO PresentationSettings => _presentationSettings;
            public PlayerBaseStatsSO PlayerStats => _playerStats;
            public DeckDefinitionSO StartingDeck => _startingDeck;
            public EncounterDefinitionSO EncounterA => _encounterA;
            public EncounterDefinitionSO EncounterB => _encounterB;
            public EnemyDefinitionSO EnemyA => _enemyA;
            public EnemyDefinitionSO EnemyB => _enemyB;
            public BlockDefinitionSO Attack2 => _attack2;
            public BlockDefinitionSO Attack4 => _attack4;
            public BlockDefinitionSO Shield3 => _shield3;
            public BlockDefinitionSO Shield5 => _shield5;
            public BlockDefinitionSO Critical => _critical;
            public BlockView BlockPrefab => _blockPrefab;

            public ExistingAssets(
                BattleEventChannelSO eventChannel,
                BattleBoardSettingsSO boardSettings,
                BattlePresentationSettingsSO presentationSettings,
                PlayerBaseStatsSO playerStats,
                DeckDefinitionSO startingDeck,
                EncounterDefinitionSO encounterA,
                EncounterDefinitionSO encounterB,
                EnemyDefinitionSO enemyA,
                EnemyDefinitionSO enemyB,
                BlockDefinitionSO attack2,
                BlockDefinitionSO attack4,
                BlockDefinitionSO shield3,
                BlockDefinitionSO shield5,
                BlockDefinitionSO critical,
                BlockView blockPrefab)
            {
                _eventChannel = eventChannel;
                _boardSettings = boardSettings;
                _presentationSettings = presentationSettings;
                _playerStats = playerStats;
                _startingDeck = startingDeck;
                _encounterA = encounterA;
                _encounterB = encounterB;
                _enemyA = enemyA;
                _enemyB = enemyB;
                _attack2 = attack2;
                _attack4 = attack4;
                _shield3 = shield3;
                _shield5 = shield5;
                _critical = critical;
                _blockPrefab = blockPrefab;
            }
        }

        /// <summary>Immutable generated run asset bundle.</summary>
        private readonly struct RunAssets
        {
            private readonly HealBlockEffectSO _healEffect;
            private readonly BlockDefinitionSO _healBlock;
            private readonly StageDefinitionSO[] _stages;
            private readonly RunDefinitionSO _runDefinition;

            public HealBlockEffectSO HealEffect => _healEffect;
            public BlockDefinitionSO HealBlock => _healBlock;
            public StageDefinitionSO[] Stages => _stages;
            public RunDefinitionSO RunDefinition => _runDefinition;

            public RunAssets(HealBlockEffectSO healEffect, BlockDefinitionSO healBlock, StageDefinitionSO[] stages, RunDefinitionSO runDefinition)
            {
                _healEffect = healEffect;
                _healBlock = healBlock;
                _stages = stages;
                _runDefinition = runDefinition;
            }
        }

        /// <summary>Small editor-only value used to configure weighted loot arrays.</summary>
        private readonly struct RewardConfig
        {
            private readonly BlockDefinitionSO _block;
            private readonly int _weight;

            public BlockDefinitionSO Block => _block;
            public int Weight => _weight;

            public RewardConfig(BlockDefinitionSO block, int weight)
            {
                _block = block;
                _weight = weight;
            }
        }
    }
}
#endif
