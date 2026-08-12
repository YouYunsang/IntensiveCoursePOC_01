#if UNITY_EDITOR
using System;
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
    /// Incremental installer for generalized deck items and the first direction-change cell effect.
    /// Existing battle/run content is preserved; only missing cell-effect data, prefab, references, and reward entries are added.
    /// </summary>
    public static class PocBattleCellEffectPatchInstaller
    {
        private const string DATA_ROOT = "Assets/07.Data/POCBattle";
        private const string PREFAB_ROOT = "Assets/02.Prefabs/POCBattle";
        private const string SPRITE_ROOT = "Assets/05.Sprites/POCBattle";
        private const string SCENE_PATH = "Assets/00.Scenes/POCBattleScene.unity";
        private const string DIRECTION_EFFECT_PATH = DATA_ROOT + "/CellEffect_DirectionChange.asset";
        private const string CELL_EFFECT_PREFAB_PATH = PREFAB_ROOT + "/CellEffectView.prefab";
        private const string ARROW_SPRITE_PATH = SPRITE_ROOT + "/Arrow.png";
        private const string STARTING_DECK_PATH = DATA_ROOT + "/Deck_POC.asset";
        private const int DEFAULT_STARTING_COPIES = 1;

        /// <summary>Default low-to-medium direction-change reward weights for the five POC stages.</summary>
        private static readonly int[] STAGE_REWARD_WEIGHTS = { 1, 2, 2, 3, 3 };

        /// <summary>Applies all data/prefab/scene changes without rebuilding the existing POC scene or run assets.</summary>
        [MenuItem("Tools/POC Battle/Apply Cell Effect Patch")]
        public static void ApplyCellEffectPatch()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder("Assets", "05.Sprites");
            EnsureFolder("Assets/05.Sprites", "POCBattle");

            if (!ValidateRequiredBaseAssets(true))
            {
                return;
            }

            Sprite arrowSprite = ConfigureArrowSpriteImport();
            DirectionChangeCellEffectSO directionEffect = CreateOrUpdateDirectionEffect(arrowSprite);
            CellEffectView cellEffectPrefab = CreateOrRepairCellEffectPrefab();
            AddDirectionEffectToStartingDeck(directionEffect);
            AddDirectionEffectToStageLootPools(directionEffect);
            ConfigurePresentationDefaults();

            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            RepairScene(scene, cellEffectPrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool valid = ValidateCellEffectSetup(scene, directionEffect, cellEffectPrefab, true);
            Debug.Log(valid
                ? "POC Cell Effect patch applied. Direction Change is in the runtime deck/loot system, randomly placed per turn, and visualized with Arrow.png."
                : "POC Cell Effect patch applied, but validation found errors. Check Console messages above.");
        }

        /// <summary>Validates the patch without mutating data or scene composition.</summary>
        [MenuItem("Tools/POC Battle/Validate Cell Effect Setup")]
        public static void ValidateCellEffectSetupMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!ValidateRequiredBaseAssets(true))
            {
                return;
            }

            DirectionChangeCellEffectSO directionEffect = AssetDatabase.LoadAssetAtPath<DirectionChangeCellEffectSO>(DIRECTION_EFFECT_PATH);
            CellEffectView cellEffectPrefab = LoadPrefabComponent<CellEffectView>(CELL_EFFECT_PREFAB_PATH);
            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            if (ValidateCellEffectSetup(scene, directionEffect, cellEffectPrefab, true))
            {
                Debug.Log("POC Cell Effect setup validation passed.");
            }
        }

        /// <summary>Configures the provided 32x32 arrow as a point-filtered single Sprite asset.</summary>
        private static Sprite ConfigureArrowSpriteImport()
        {
            TextureImporter importer = AssetImporter.GetAtPath(ARROW_SPRITE_PATH) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Arrow sprite is missing at {ARROW_SPRITE_PATH}. Re-extract the patch ZIP into the project root.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ARROW_SPRITE_PATH);
            if (sprite == null)
            {
                throw new InvalidOperationException("Arrow.png was imported, but Unity did not produce a Sprite sub-asset.");
            }

            return sprite;
        }

        /// <summary>Creates/updates the collectible Direction Change definition while keeping all tunable fields in SO data.</summary>
        private static DirectionChangeCellEffectSO CreateOrUpdateDirectionEffect(Sprite arrowSprite)
        {
            DirectionChangeCellEffectSO effect = LoadOrCreateAsset<DirectionChangeCellEffectSO>(DIRECTION_EFFECT_PATH);
            SerializedObject serialized = new SerializedObject(effect);
            serialized.FindProperty("_displayName").stringValue = "Direction Change";
            serialized.FindProperty("_displayColor").colorValue = Color.white;
            serialized.FindProperty("_displaySprite").objectReferenceValue = arrowSprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
            return effect;
        }

        /// <summary>Adds exactly one starting Direction Change copy without replacing any existing deck entry.</summary>
        private static void AddDirectionEffectToStartingDeck(DirectionChangeCellEffectSO directionEffect)
        {
            DeckDefinitionSO deck = AssetDatabase.LoadAssetAtPath<DeckDefinitionSO>(STARTING_DECK_PATH);
            SerializedObject serialized = new SerializedObject(deck);
            SerializedProperty entries = serialized.FindProperty("_entries");

            for (int entryIndex = 0; entryIndex < entries.arraySize; entryIndex++)
            {
                SerializedProperty itemProperty = entries.GetArrayElementAtIndex(entryIndex).FindPropertyRelative("_block");
                if (itemProperty.objectReferenceValue == directionEffect)
                {
                    return;
                }
            }

            int newIndex = entries.arraySize;
            entries.arraySize++;
            SerializedProperty newEntry = entries.GetArrayElementAtIndex(newIndex);
            newEntry.FindPropertyRelative("_block").objectReferenceValue = directionEffect;
            newEntry.FindPropertyRelative("_count").intValue = DEFAULT_STARTING_COPIES;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(deck);
        }

        /// <summary>Adds Direction Change to every existing POC stage with stage-specific weight, preserving all prior reward entries.</summary>
        private static void AddDirectionEffectToStageLootPools(DirectionChangeCellEffectSO directionEffect)
        {
            for (int stageIndex = 0; stageIndex < STAGE_REWARD_WEIGHTS.Length; stageIndex++)
            {
                string path = $"{DATA_ROOT}/Stage_{stageIndex + 1:00}.asset";
                StageDefinitionSO stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(path);
                if (stage == null)
                {
                    throw new InvalidOperationException($"Required POC stage asset is missing: {path}");
                }

                SerializedObject serialized = new SerializedObject(stage);
                SerializedProperty lootPool = serialized.FindProperty("_lootPool");
                bool found = false;
                for (int lootIndex = 0; lootIndex < lootPool.arraySize; lootIndex++)
                {
                    SerializedProperty entry = lootPool.GetArrayElementAtIndex(lootIndex);
                    if (entry.FindPropertyRelative("_block").objectReferenceValue != directionEffect)
                    {
                        continue;
                    }

                    entry.FindPropertyRelative("_weight").intValue = STAGE_REWARD_WEIGHTS[stageIndex];
                    found = true;
                    break;
                }

                if (!found)
                {
                    int newIndex = lootPool.arraySize;
                    lootPool.arraySize++;
                    SerializedProperty entry = lootPool.GetArrayElementAtIndex(newIndex);
                    entry.FindPropertyRelative("_block").objectReferenceValue = directionEffect;
                    entry.FindPropertyRelative("_weight").intValue = STAGE_REWARD_WEIGHTS[stageIndex];
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stage);
            }
        }

        /// <summary>Creates or repairs the collider-free world-space cell-effect view prefab.</summary>
        private static CellEffectView CreateOrRepairCellEffectPrefab()
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CELL_EFFECT_PREFAB_PATH);
            GameObject root = existingPrefab != null
                ? PrefabUtility.LoadPrefabContents(CELL_EFFECT_PREFAB_PATH)
                : new GameObject("CellEffectView");

            try
            {
                SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = root.AddComponent<SpriteRenderer>();
                }
                spriteRenderer.sortingOrder = 10;

                Collider collider = root.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }

                CellEffectView view = root.GetComponent<CellEffectView>();
                if (view == null)
                {
                    view = root.AddComponent<CellEffectView>();
                }

                SetObject(view, "_spriteRenderer", spriteRenderer);
                PrefabUtility.SaveAsPrefabAsset(root, CELL_EFFECT_PREFAB_PATH);
            }
            finally
            {
                if (existingPrefab != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            return LoadPrefabComponent<CellEffectView>(CELL_EFFECT_PREFAB_PATH);
        }

        /// <summary>Repairs only cell-effect scene composition and loot prefab references in place.</summary>
        private static void RepairScene(Scene scene, CellEffectView cellEffectPrefab)
        {
            BattleEventChannelSO eventChannel = AssetDatabase.LoadAssetAtPath<BattleEventChannelSO>($"{DATA_ROOT}/BattleEventChannel.asset");
            BattleBoardSettingsSO boardSettings = AssetDatabase.LoadAssetAtPath<BattleBoardSettingsSO>($"{DATA_ROOT}/BattleBoardSettings.asset");
            BattlePresentationSettingsSO presentationSettings = AssetDatabase.LoadAssetAtPath<BattlePresentationSettingsSO>($"{DATA_ROOT}/BattlePresentationSettings.asset");

            BoardView boardView = GetFirstSceneComponent<BoardView>(scene);
            if (boardView == null)
            {
                throw new InvalidOperationException("POCBattleScene is missing BoardView. Apply the previous scene repair patch first.");
            }

            CellEffectLayerView layerView = boardView.GetComponent<CellEffectLayerView>();
            if (layerView == null)
            {
                layerView = boardView.gameObject.AddComponent<CellEffectLayerView>();
            }
            SetObject(layerView, "_boardSettings", boardSettings);
            SetObject(layerView, "_presentationSettings", presentationSettings);
            SetObject(layerView, "_eventChannel", eventChannel);
            SetObject(layerView, "_cellEffectPrefab", cellEffectPrefab);

            LootDropPresenter lootPresenter = GetFirstSceneComponent<LootDropPresenter>(scene);
            if (lootPresenter == null)
            {
                throw new InvalidOperationException("POCBattleScene is missing LootDropPresenter. Apply the run/stage patch first.");
            }
            SetObject(lootPresenter, "_cellEffectPrefab", cellEffectPrefab);

            EditorUtility.SetDirty(layerView);
            EditorUtility.SetDirty(lootPresenter);
        }

        /// <summary>Writes explicit defaults only for the newly added presentation fields.</summary>
        private static void ConfigurePresentationDefaults()
        {
            BattlePresentationSettingsSO settings = AssetDatabase.LoadAssetAtPath<BattlePresentationSettingsSO>($"{DATA_ROOT}/BattlePresentationSettings.asset");
            SerializedObject serialized = new SerializedObject(settings);
            serialized.FindProperty("_cellEffectSurfaceOffset").floatValue = 0.03f;
            serialized.FindProperty("_cellEffectFootprintRatio").floatValue = 0.55f;
            serialized.FindProperty("_cellEffectInactiveAlpha").floatValue = 0.22f;
            serialized.FindProperty("_cellEffectTriggerPulseScale").floatValue = 1.22f;
            serialized.FindProperty("_cellEffectTriggerPulseDuration").floatValue = 0.18f;
            serialized.FindProperty("_lootCellEffectScaleMultiplier").floatValue = 1.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        /// <summary>Checks data migration, prefab safety, reward integration, and in-place scene references.</summary>
        private static bool ValidateCellEffectSetup(
            Scene scene,
            DirectionChangeCellEffectSO directionEffect,
            CellEffectView cellEffectPrefab,
            bool logErrors)
        {
            bool valid = true;
            valid &= Validate(directionEffect != null, "Direction Change asset is missing.", logErrors);
            valid &= Validate(directionEffect != null && directionEffect.DisplaySprite != null, "Direction Change asset has no Arrow sprite.", logErrors);
            valid &= Validate(cellEffectPrefab != null, "CellEffectView prefab is missing.", logErrors);
            valid &= Validate(cellEffectPrefab != null && cellEffectPrefab.GetComponent<Collider>() == null,
                "CellEffectView prefab must remain collider-free so placement drag raycasts cannot select cell effects.", logErrors);

            DeckDefinitionSO deck = AssetDatabase.LoadAssetAtPath<DeckDefinitionSO>(STARTING_DECK_PATH);
            PlayerBaseStatsSO playerStats = AssetDatabase.LoadAssetAtPath<PlayerBaseStatsSO>($"{DATA_ROOT}/PlayerBaseStats.asset");
            valid &= Validate(deck != null && ContainsDeckItem(deck, directionEffect), "Starting deck does not contain Direction Change.", logErrors);
            valid &= Validate(deck != null && playerStats != null && deck.GetTotalItemCount() <= playerStats.DeckCapacity,
                "Starting deck item count exceeds PlayerBaseStats deck capacity after adding Direction Change.", logErrors);

            for (int stageIndex = 0; stageIndex < STAGE_REWARD_WEIGHTS.Length; stageIndex++)
            {
                StageDefinitionSO stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"{DATA_ROOT}/Stage_{stageIndex + 1:00}.asset");
                valid &= Validate(stage != null && ContainsLootItem(stage, directionEffect),
                    $"Stage {stageIndex + 1} loot pool does not contain Direction Change.", logErrors);
            }

            CellEffectLayerView layerView = GetFirstSceneComponent<CellEffectLayerView>(scene);
            LootDropPresenter lootPresenter = GetFirstSceneComponent<LootDropPresenter>(scene);
            valid &= Validate(layerView != null, "Scene BoardView is missing CellEffectLayerView.", logErrors);
            valid &= Validate(lootPresenter != null, "Scene is missing LootDropPresenter.", logErrors);
            return valid;
        }

        /// <summary>Returns whether immutable starting deck data already references one item definition.</summary>
        private static bool ContainsDeckItem(DeckDefinitionSO deck, DeckItemDefinitionSO item)
        {
            for (int entryIndex = 0; entryIndex < deck.Entries.Count; entryIndex++)
            {
                DeckBlockEntry entry = deck.Entries[entryIndex];
                if (entry != null && entry.Item == item)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Returns whether one stage loot pool already references a deck item definition.</summary>
        private static bool ContainsLootItem(StageDefinitionSO stage, DeckItemDefinitionSO item)
        {
            for (int lootIndex = 0; lootIndex < stage.LootPool.Count; lootIndex++)
            {
                WeightedBlockRewardEntry entry = stage.LootPool[lootIndex];
                if (entry != null && entry.Item == item && entry.Weight > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Checks existing project assets required by this incremental patch.</summary>
        private static bool ValidateRequiredBaseAssets(bool logErrors)
        {
            bool valid = true;
            valid &= Validate(AssetDatabase.LoadAssetAtPath<DeckDefinitionSO>(STARTING_DECK_PATH) != null, "Deck_POC.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<BattleEventChannelSO>($"{DATA_ROOT}/BattleEventChannel.asset") != null, "BattleEventChannel.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<BattleBoardSettingsSO>($"{DATA_ROOT}/BattleBoardSettings.asset") != null, "BattleBoardSettings.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<BattlePresentationSettingsSO>($"{DATA_ROOT}/BattlePresentationSettings.asset") != null, "BattlePresentationSettings.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<PlayerBaseStatsSO>($"{DATA_ROOT}/PlayerBaseStats.asset") != null, "PlayerBaseStats.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH) != null, $"POC scene is missing at {SCENE_PATH}.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<Texture2D>(ARROW_SPRITE_PATH) != null, $"Arrow.png is missing at {ARROW_SPRITE_PATH}.", logErrors);
            for (int stageIndex = 0; stageIndex < STAGE_REWARD_WEIGHTS.Length; stageIndex++)
            {
                valid &= Validate(AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"{DATA_ROOT}/Stage_{stageIndex + 1:00}.asset") != null,
                    $"Stage_{stageIndex + 1:00}.asset is missing.", logErrors);
            }
            return valid;
        }

        /// <summary>Loads or creates one concrete ScriptableObject asset at a stable path.</summary>
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

        /// <summary>Returns the first scene component without GameObject.Find/FindObjectOfType.</summary>
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

        /// <summary>Creates one child folder when absent.</summary>
        private static void EnsureFolder(string parent, string childName)
        {
            string path = parent + "/" + childName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, childName);
            }
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
                Debug.LogError("POC Cell Effect validation: " + message);
            }
            return false;
        }
    }
}
#endif
