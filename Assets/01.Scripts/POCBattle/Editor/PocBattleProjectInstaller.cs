#if UNITY_EDITOR
using System;
using System.IO;
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
    /// Editor-only POC installer that creates data, prefabs, and a separate test scene without modifying the existing GameScene.
    /// </summary>
    public static class PocBattleProjectInstaller
    {
        /// <summary>Root data folder for generated POC assets.</summary>
        private const string DATA_ROOT = "Assets/07.Data/POCBattle";

        /// <summary>Root prefab folder for generated POC prefabs.</summary>
        private const string PREFAB_ROOT = "Assets/02.Prefabs/POCBattle";

        /// <summary>Root art folder for generated POC placeholder art/materials.</summary>
        private const string ART_ROOT = "Assets/03.Art/POCBattle";

        /// <summary>Generated test scene path.</summary>
        private const string SCENE_PATH = "Assets/00.Scenes/POCBattleScene.unity";

        /// <summary>Default POC board columns.</summary>
        private const int DEFAULT_COLUMNS = 8;

        /// <summary>Default POC board rows.</summary>
        private const int DEFAULT_ROWS = 5;

        /// <summary>Default player maximum HP.</summary>
        private const int DEFAULT_PLAYER_HP = 20;

        /// <summary>Default player moves per turn.</summary>
        private const int DEFAULT_MOVE_COUNT = 3;

        /// <summary>Default player edits per turn.</summary>
        private const int DEFAULT_EDIT_COUNT = 2;

        /// <summary>Default encounter trap count.</summary>
        private const int DEFAULT_TRAP_COUNT = 2;

        /// <summary>
        /// Creates or updates all POC content and opens the generated battle scene.
        /// </summary>
        [MenuItem("Tools/POC Battle/Build POC Battle Scene")]
        public static void BuildPocBattleScene()
        {
            EnsureProjectFolders();
            GeneratedAssets assets = CreateOrUpdateDataAssets();
            GeneratedPrefabs prefabs = CreateOrUpdatePrefabs(assets);
            CreateOrReplaceScene(assets, prefabs);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"POC Battle setup complete. Scene created at {SCENE_PATH}");
        }


        /// <summary>
        /// Repairs only the generated ScriptableObject assets that were created without a valid MonoScript reference.
        /// Existing scene and prefab assets are not rebuilt by this command.
        /// </summary>
        [MenuItem("Tools/POC Battle/Repair Missing ScriptableObject Assets")]
        public static void RepairMissingScriptableObjectAssets()
        {
            EnsureProjectFolders();

            AttackBlockEffectSO attackTwoEffect = LoadOrCreateAsset<AttackBlockEffectSO>($"{DATA_ROOT}/Effect_Attack_2.asset");
            AttackBlockEffectSO attackFourEffect = LoadOrCreateAsset<AttackBlockEffectSO>($"{DATA_ROOT}/Effect_Attack_4.asset");
            ShieldBlockEffectSO shieldThreeEffect = LoadOrCreateAsset<ShieldBlockEffectSO>($"{DATA_ROOT}/Effect_Shield_3.asset");
            ShieldBlockEffectSO shieldFiveEffect = LoadOrCreateAsset<ShieldBlockEffectSO>($"{DATA_ROOT}/Effect_Shield_5.asset");
            CriticalBlockEffectSO criticalEffect = LoadOrCreateAsset<CriticalBlockEffectSO>($"{DATA_ROOT}/Effect_Critical.asset");
            ImmediateDamageBlockEffectSO trapDamageEffect = LoadOrCreateAsset<ImmediateDamageBlockEffectSO>($"{DATA_ROOT}/Effect_TrapDamage_3.asset");

            SetInt(attackTwoEffect, "_amount", 2);
            SetInt(attackFourEffect, "_amount", 4);
            SetInt(shieldThreeEffect, "_amount", 3);
            SetInt(shieldFiveEffect, "_amount", 5);
            SetInt(criticalEffect, "_stackCount", 1);
            SetInt(trapDamageEffect, "_damage", 3);

            RelinkBlockEffect("Block_Attack_2", attackTwoEffect);
            RelinkBlockEffect("Block_Attack_4", attackFourEffect);
            RelinkBlockEffect("Block_Shield_3", shieldThreeEffect);
            RelinkBlockEffect("Block_Shield_5", shieldFiveEffect);
            RelinkBlockEffect("Block_Critical", criticalEffect);
            RelinkBlockEffect("Block_Trap", trapDamageEffect);

            EnemyAttackActionSO attackThree = CreateEnemyAttackAction("EnemyAction_Attack_3", 3);
            EnemyAttackActionSO attackFour = CreateEnemyAttackAction("EnemyAction_Attack_4", 4);
            EnemyAttackActionSO attackFive = CreateEnemyAttackAction("EnemyAction_Attack_5", 5);
            EnemyAttackActionSO attackSeven = CreateEnemyAttackAction("EnemyAction_Attack_7", 7);
            EnemyShieldActionSO shieldThree = CreateEnemyShieldAction("EnemyAction_Shield_3", 3);
            EnemyShieldActionSO shieldFour = CreateEnemyShieldAction("EnemyAction_Shield_4", 4);

            EnemyActionPatternDefinitionSO patternA = AssetDatabase.LoadAssetAtPath<EnemyActionPatternDefinitionSO>($"{DATA_ROOT}/EnemyPattern_A.asset");
            if (patternA != null)
            {
                ConfigurePattern(patternA, new EnemyActionDefinitionSO[] { attackFour, shieldThree, attackFive });
            }

            EnemyActionPatternDefinitionSO patternB = AssetDatabase.LoadAssetAtPath<EnemyActionPatternDefinitionSO>($"{DATA_ROOT}/EnemyPattern_B.asset");
            if (patternB != null)
            {
                ConfigurePattern(patternB, new EnemyActionDefinitionSO[] { attackThree, attackSeven, shieldFour });
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("POC Battle missing ScriptableObject assets repaired. Scene and prefabs were not rebuilt.");
        }

        /// <summary>
        /// Reconnects one generated block definition to its repaired effect without changing its other presentation data.
        /// </summary>
        private static void RelinkBlockEffect(string blockFileName, BlockEffectDefinitionSO effect)
        {
            BlockDefinitionSO block = AssetDatabase.LoadAssetAtPath<BlockDefinitionSO>($"{DATA_ROOT}/{blockFileName}.asset");
            if (block == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(block);
            SerializedProperty effects = serializedObject.FindProperty("_effects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = effect;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(block);
        }

        /// <summary>
        /// Ensures all project folders used by the installer exist while preserving existing project content.
        /// </summary>
        private static void EnsureProjectFolders()
        {
            EnsureFolder("Assets", "07.Data");
            EnsureFolder("Assets/07.Data", "POCBattle");
            EnsureFolder("Assets", "02.Prefabs");
            EnsureFolder("Assets/02.Prefabs", "POCBattle");
            EnsureFolder("Assets", "03.Art");
            EnsureFolder("Assets/03.Art", "POCBattle");
            EnsureFolder("Assets", "00.Scenes");
        }

        /// <summary>
        /// Creates one child folder only when it does not already exist.
        /// </summary>
        private static void EnsureFolder(string parentPath, string childName)
        {
            string fullPath = $"{parentPath}/{childName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentPath, childName);
            }
        }

        /// <summary>
        /// Creates or updates every ScriptableObject required by the POC, including both enemy A and B encounter assets.
        /// </summary>
        private static GeneratedAssets CreateOrUpdateDataAssets()
        {
            BattleEventChannelSO eventChannel = LoadOrCreateAsset<BattleEventChannelSO>($"{DATA_ROOT}/BattleEventChannel.asset");
            BattleBoardSettingsSO boardSettings = LoadOrCreateAsset<BattleBoardSettingsSO>($"{DATA_ROOT}/BattleBoardSettings.asset");
            PlayerBaseStatsSO playerStats = LoadOrCreateAsset<PlayerBaseStatsSO>($"{DATA_ROOT}/PlayerBaseStats.asset");
            BattlePresentationSettingsSO presentationSettings = LoadOrCreateAsset<BattlePresentationSettingsSO>($"{DATA_ROOT}/BattlePresentationSettings.asset");

            ConfigureBoardSettings(boardSettings);
            ConfigurePlayerStats(playerStats);
            ConfigurePresentationSettings(presentationSettings);

            AttackBlockEffectSO attackTwoEffect = LoadOrCreateAsset<AttackBlockEffectSO>($"{DATA_ROOT}/Effect_Attack_2.asset");
            AttackBlockEffectSO attackFourEffect = LoadOrCreateAsset<AttackBlockEffectSO>($"{DATA_ROOT}/Effect_Attack_4.asset");
            ShieldBlockEffectSO shieldThreeEffect = LoadOrCreateAsset<ShieldBlockEffectSO>($"{DATA_ROOT}/Effect_Shield_3.asset");
            ShieldBlockEffectSO shieldFiveEffect = LoadOrCreateAsset<ShieldBlockEffectSO>($"{DATA_ROOT}/Effect_Shield_5.asset");
            CriticalBlockEffectSO criticalEffect = LoadOrCreateAsset<CriticalBlockEffectSO>($"{DATA_ROOT}/Effect_Critical.asset");
            ImmediateDamageBlockEffectSO trapDamageEffect = LoadOrCreateAsset<ImmediateDamageBlockEffectSO>($"{DATA_ROOT}/Effect_TrapDamage_3.asset");

            SetInt(attackTwoEffect, "_amount", 2);
            SetInt(attackFourEffect, "_amount", 4);
            SetInt(shieldThreeEffect, "_amount", 3);
            SetInt(shieldFiveEffect, "_amount", 5);
            SetInt(criticalEffect, "_stackCount", 1);
            SetInt(trapDamageEffect, "_damage", 3);

            BlockDefinitionSO attackTwoBlock = CreateBlock(
                "Block_Attack_2",
                "Attack +2",
                new Color(0.88f, 0.22f, 0.18f, 1f),
                false,
                attackTwoEffect);
            BlockDefinitionSO attackFourBlock = CreateBlock(
                "Block_Attack_4",
                "Attack +4",
                new Color(1f, 0.42f, 0.12f, 1f),
                false,
                attackFourEffect);
            BlockDefinitionSO shieldThreeBlock = CreateBlock(
                "Block_Shield_3",
                "Shield +3",
                new Color(0.15f, 0.45f, 0.95f, 1f),
                false,
                shieldThreeEffect);
            BlockDefinitionSO shieldFiveBlock = CreateBlock(
                "Block_Shield_5",
                "Shield +5",
                new Color(0.12f, 0.75f, 0.9f, 1f),
                false,
                shieldFiveEffect);
            BlockDefinitionSO criticalBlock = CreateBlock(
                "Block_Critical",
                "Critical x1.5",
                new Color(0.95f, 0.82f, 0.12f, 1f),
                false,
                criticalEffect);
            BlockDefinitionSO trapBlock = CreateBlock(
                "Block_Trap",
                "Trap -3 HP",
                new Color(0.65f, 0.18f, 0.82f, 1f),
                true,
                trapDamageEffect);

            DeckDefinitionSO deck = LoadOrCreateAsset<DeckDefinitionSO>($"{DATA_ROOT}/Deck_POC.asset");
            ConfigureDeck(deck, attackTwoBlock, attackFourBlock, shieldThreeBlock, shieldFiveBlock, criticalBlock);

            EnemyAttackActionSO attackThree = CreateEnemyAttackAction("EnemyAction_Attack_3", 3);
            EnemyAttackActionSO attackFour = CreateEnemyAttackAction("EnemyAction_Attack_4", 4);
            EnemyAttackActionSO attackFive = CreateEnemyAttackAction("EnemyAction_Attack_5", 5);
            EnemyAttackActionSO attackSeven = CreateEnemyAttackAction("EnemyAction_Attack_7", 7);
            EnemyShieldActionSO shieldThree = CreateEnemyShieldAction("EnemyAction_Shield_3", 3);
            EnemyShieldActionSO shieldFour = CreateEnemyShieldAction("EnemyAction_Shield_4", 4);

            EnemyActionPatternDefinitionSO patternA = LoadOrCreateAsset<EnemyActionPatternDefinitionSO>($"{DATA_ROOT}/EnemyPattern_A.asset");
            ConfigurePattern(patternA, new EnemyActionDefinitionSO[] { attackFour, shieldThree, attackFive });
            EnemyActionPatternDefinitionSO patternB = LoadOrCreateAsset<EnemyActionPatternDefinitionSO>($"{DATA_ROOT}/EnemyPattern_B.asset");
            ConfigurePattern(patternB, new EnemyActionDefinitionSO[] { attackThree, attackSeven, shieldFour });

            EnemyDefinitionSO enemyA = CreateEnemyDefinition(
                "Enemy_A",
                "Enemy A",
                20,
                new Color(0.95f, 0.35f, 0.35f, 1f),
                patternA);
            EnemyDefinitionSO enemyB = CreateEnemyDefinition(
                "Enemy_B",
                "Enemy B",
                26,
                new Color(0.55f, 0.35f, 0.95f, 1f),
                patternB);

            EncounterDefinitionSO encounterA = CreateEncounter("Encounter_EnemyA", enemyA, trapBlock, DEFAULT_TRAP_COUNT);
            EncounterDefinitionSO encounterB = CreateEncounter("Encounter_EnemyB", enemyB, trapBlock, DEFAULT_TRAP_COUNT);

            AssetDatabase.SaveAssets();
            return new GeneratedAssets(
                eventChannel,
                boardSettings,
                playerStats,
                presentationSettings,
                deck,
                encounterA,
                encounterB);
        }

        /// <summary>
        /// Applies default board gameplay data through serialized fields so the generated SO remains fully editable afterward.
        /// </summary>
        private static void ConfigureBoardSettings(BattleBoardSettingsSO settings)
        {
            SerializedObject serializedObject = new SerializedObject(settings);
            serializedObject.FindProperty("_columns").intValue = DEFAULT_COLUMNS;
            serializedObject.FindProperty("_rows").intValue = DEFAULT_ROWS;
            serializedObject.FindProperty("_cellSize").floatValue = 1f;
            serializedObject.FindProperty("_cellGap").floatValue = 0.08f;
            serializedObject.FindProperty("_boardCenter").vector3Value = Vector3.zero;
            serializedObject.FindProperty("_playerStartPosition").vector2IntValue = new Vector2Int(3, 2);
            serializedObject.FindProperty("_maxDeckBlockCount").intValue = 20;
            serializedObject.FindProperty("_allowBlockSwap").boolValue = true;
            serializedObject.FindProperty("_useFixedRandomSeed").boolValue = false;
            serializedObject.FindProperty("_fixedRandomSeed").intValue = 12345;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// Applies default POC player stats while keeping all values editable as SO data.
        /// </summary>
        private static void ConfigurePlayerStats(PlayerBaseStatsSO stats)
        {
            SerializedObject serializedObject = new SerializedObject(stats);
            serializedObject.FindProperty("_maxHealth").intValue = DEFAULT_PLAYER_HP;
            serializedObject.FindProperty("_baseMoveCount").intValue = DEFAULT_MOVE_COUNT;
            serializedObject.FindProperty("_baseEditCount").intValue = DEFAULT_EDIT_COUNT;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stats);
        }

        /// <summary>
        /// Applies all world-space, tween, critical, enemy, and pointer presentation defaults as editable data.
        /// </summary>
        private static void ConfigurePresentationSettings(BattlePresentationSettingsSO settings)
        {
            SerializedObject serializedObject = new SerializedObject(settings);
            serializedObject.FindProperty("_cellHeight").floatValue = 0.1f;
            serializedObject.FindProperty("_wallHeight").floatValue = 1f;
            serializedObject.FindProperty("_cellCenterY").floatValue = 0f;
            serializedObject.FindProperty("_blockCenterY").floatValue = 0.45f;
            serializedObject.FindProperty("_blockFootprintRatio").floatValue = 0.82f;
            serializedObject.FindProperty("_blockHeight").floatValue = 0.7f;
            serializedObject.FindProperty("_selectedBlockScale").floatValue = 1.12f;
            serializedObject.FindProperty("_blockMoveDuration").floatValue = 0.18f;
            serializedObject.FindProperty("_normalCellColor").colorValue = new Color(0.12f, 0.12f, 0.14f, 1f);
            serializedObject.FindProperty("_wallCellColor").colorValue = new Color(0.28f, 0.3f, 0.34f, 1f);
            serializedObject.FindProperty("_playerCenterY").floatValue = 0.75f;
            serializedObject.FindProperty("_moveSecondsPerCell").floatValue = 0.08f;
            serializedObject.FindProperty("_invalidMoveDistance").floatValue = 0.2f;
            serializedObject.FindProperty("_invalidMoveDuration").floatValue = 0.16f;
            serializedObject.FindProperty("_criticalMultiplier").floatValue = 1.5f;
            serializedObject.FindProperty("_playerResolveDelay").floatValue = 0.45f;
            serializedObject.FindProperty("_enemyResolveDelay").floatValue = 0.65f;
            serializedObject.FindProperty("_enemyHitDuration").floatValue = 0.2f;
            serializedObject.FindProperty("_enemyHitStrength").floatValue = 0.16f;
            serializedObject.FindProperty("_playerHitStrength").floatValue = 0.12f;
            serializedObject.FindProperty("_hitPunchVibrato").intValue = 6;
            serializedObject.FindProperty("_hitPunchElasticity").floatValue = 0.5f;
            serializedObject.FindProperty("_enemyStartPosition").vector3Value = new Vector3(0f, 2.25f, 4.1f);
            serializedObject.FindProperty("_enemySpacing").floatValue = 3f;
            serializedObject.FindProperty("_enemyEulerAngles").vector3Value = new Vector3(45f, 0f, 0f);
            serializedObject.FindProperty("_enemyScale").floatValue = 1.7f;
            serializedObject.FindProperty("_pointerRayDistance").floatValue = 100f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// Creates or updates one block definition containing a composable effect asset array.
        /// </summary>
        private static BlockDefinitionSO CreateBlock(string fileName, string displayName, Color color, bool isTrap, params BlockEffectDefinitionSO[] effects)
        {
            BlockDefinitionSO block = LoadOrCreateAsset<BlockDefinitionSO>($"{DATA_ROOT}/{fileName}.asset");
            SerializedObject serializedObject = new SerializedObject(block);
            serializedObject.FindProperty("_displayName").stringValue = displayName;
            serializedObject.FindProperty("_displayColor").colorValue = color;
            serializedObject.FindProperty("_isTrap").boolValue = isTrap;
            SerializedProperty effectArray = serializedObject.FindProperty("_effects");
            effectArray.arraySize = effects.Length;

            for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
            {
                effectArray.GetArrayElementAtIndex(effectIndex).objectReferenceValue = effects[effectIndex];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(block);
            return block;
        }

        /// <summary>
        /// Configures the complete player deck; every physical copy will be placed every player turn.
        /// </summary>
        private static void ConfigureDeck(
            DeckDefinitionSO deck,
            BlockDefinitionSO attackTwo,
            BlockDefinitionSO attackFour,
            BlockDefinitionSO shieldThree,
            BlockDefinitionSO shieldFive,
            BlockDefinitionSO critical)
        {
            BlockDefinitionSO[] blocks = { attackTwo, attackFour, shieldThree, shieldFive, critical };
            int[] counts = { 3, 2, 2, 2, 1 };
            SerializedObject serializedObject = new SerializedObject(deck);
            SerializedProperty entries = serializedObject.FindProperty("_entries");
            entries.arraySize = blocks.Length;

            for (int entryIndex = 0; entryIndex < blocks.Length; entryIndex++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(entryIndex);
                entry.FindPropertyRelative("_block").objectReferenceValue = blocks[entryIndex];
                entry.FindPropertyRelative("_count").intValue = counts[entryIndex];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(deck);
        }

        /// <summary>
        /// Creates or updates a reusable enemy attack action data asset.
        /// </summary>
        private static EnemyAttackActionSO CreateEnemyAttackAction(string fileName, int damage)
        {
            EnemyAttackActionSO action = LoadOrCreateAsset<EnemyAttackActionSO>($"{DATA_ROOT}/{fileName}.asset");
            SetInt(action, "_damage", damage);
            return action;
        }

        /// <summary>
        /// Creates or updates a reusable enemy shield action data asset.
        /// </summary>
        private static EnemyShieldActionSO CreateEnemyShieldAction(string fileName, int shield)
        {
            EnemyShieldActionSO action = LoadOrCreateAsset<EnemyShieldActionSO>($"{DATA_ROOT}/{fileName}.asset");
            SetInt(action, "_shield", shield);
            return action;
        }

        /// <summary>
        /// Configures a sequential looping pattern with one action per step for the current POC.
        /// </summary>
        private static void ConfigurePattern(EnemyActionPatternDefinitionSO pattern, EnemyActionDefinitionSO[] orderedActions)
        {
            SerializedObject serializedObject = new SerializedObject(pattern);
            serializedObject.FindProperty("_loop").boolValue = true;
            SerializedProperty steps = serializedObject.FindProperty("_steps");
            steps.arraySize = orderedActions.Length;

            for (int stepIndex = 0; stepIndex < orderedActions.Length; stepIndex++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(stepIndex);
                SerializedProperty actions = step.FindPropertyRelative("_actions");
                actions.arraySize = 1;
                actions.GetArrayElementAtIndex(0).objectReferenceValue = orderedActions[stepIndex];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pattern);
        }

        /// <summary>
        /// Creates or updates one enemy definition.
        /// </summary>
        private static EnemyDefinitionSO CreateEnemyDefinition(
            string fileName,
            string displayName,
            int maxHealth,
            Color displayColor,
            EnemyActionPatternDefinitionSO pattern)
        {
            EnemyDefinitionSO enemy = LoadOrCreateAsset<EnemyDefinitionSO>($"{DATA_ROOT}/{fileName}.asset");
            SerializedObject serializedObject = new SerializedObject(enemy);
            serializedObject.FindProperty("_displayName").stringValue = displayName;
            serializedObject.FindProperty("_maxHealth").intValue = maxHealth;
            serializedObject.FindProperty("_displayColor").colorValue = displayColor;
            serializedObject.FindProperty("_pattern").objectReferenceValue = pattern;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        /// <summary>
        /// Creates or updates one single-enemy POC encounter while preserving 1:N-capable array data.
        /// </summary>
        private static EncounterDefinitionSO CreateEncounter(
            string fileName,
            EnemyDefinitionSO enemy,
            BlockDefinitionSO trap,
            int trapCount)
        {
            EncounterDefinitionSO encounter = LoadOrCreateAsset<EncounterDefinitionSO>($"{DATA_ROOT}/{fileName}.asset");
            SerializedObject serializedObject = new SerializedObject(encounter);
            SerializedProperty enemies = serializedObject.FindProperty("_enemies");
            enemies.arraySize = 1;
            enemies.GetArrayElementAtIndex(0).objectReferenceValue = enemy;
            serializedObject.FindProperty("_trapDefinition").objectReferenceValue = trap;
            serializedObject.FindProperty("_trapCount").intValue = trapCount;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        /// <summary>
        /// Creates or updates generic cell/block/player/enemy prefabs used by the generated scene.
        /// POC HUD presentation is rendered by BattleOnGuiPresenter and therefore needs no Canvas prefab.
        /// </summary>
        private static GeneratedPrefabs CreateOrUpdatePrefabs(GeneratedAssets assets)
        {
            Material baseMaterial = CreateOrUpdateMaterial($"{ART_ROOT}/PocBattle_Base.mat", new Color(0.8f, 0.8f, 0.8f, 1f));
            Material playerMaterial = CreateOrUpdateMaterial($"{ART_ROOT}/PocBattle_Player.mat", new Color(0.2f, 0.95f, 0.45f, 1f));
            Sprite enemySprite = CreateOrUpdatePlaceholderSprite();
            BoardCellView cellPrefab = CreateCellPrefab(baseMaterial);
            BlockView blockPrefab = CreateBlockPrefab(baseMaterial);
            PlayerController playerPrefab = CreatePlayerPrefab(playerMaterial, assets);
            EnemyView enemyPrefab = CreateEnemyPrefab(enemySprite);
            return new GeneratedPrefabs(cellPrefab, blockPrefab, playerPrefab, enemyPrefab);
        }

        /// <summary>
        /// Creates a shared URP-compatible material or updates its color when it already exists.
        /// </summary>
        private static Material CreateOrUpdateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Generates a simple white square sprite asset that enemy definitions tint at runtime.
        /// </summary>
        private static Sprite CreateOrUpdatePlaceholderSprite()
        {
            string texturePath = $"{ART_ROOT}/EnemyPlaceholder.png";
            if (!File.Exists(texturePath))
            {
                const int textureSize = 32;
                Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[textureSize * textureSize];
                for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
                {
                    pixels[pixelIndex] = Color.white;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            }

            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        }

        /// <summary>
        /// Creates the generic cell prefab used for both normal cells and the four corner wall cells.
        /// </summary>
        private static BoardCellView CreateCellPrefab(Material material)
        {
            string path = $"{PREFAB_ROOT}/GridCell.prefab";
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = "GridCell";
            MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            BoardCellView view = instance.AddComponent<BoardCellView>();
            SetObjectReference(view, "_renderer", renderer);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab.GetComponent<BoardCellView>();
        }

        /// <summary>
        /// Creates one generic 3D block prefab; all block types are data-driven through BlockDefinitionSO.
        /// </summary>
        private static BlockView CreateBlockPrefab(Material material)
        {
            string path = $"{PREFAB_ROOT}/Block.prefab";
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = "Block";
            MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            BlockView view = instance.AddComponent<BlockView>();
            TMP_Text effectText = CreateBlockEffectText(instance.transform);
            SetObjectReference(view, "_renderer", renderer);
            SetObjectReference(view, "_effectText", effectText);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab.GetComponent<BlockView>();
        }

        /// <summary>
        /// Creates the world-space TMP child used by every generic block to display its data-driven effect text.
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
        /// Creates a simple 3D capsule player prefab with input/controller and movement components on the same GameObject.
        /// </summary>
        private static PlayerController CreatePlayerPrefab(Material playerMaterial, GeneratedAssets assets)
        {
            string path = $"{PREFAB_ROOT}/Player.prefab";
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            instance.name = "Player";
            instance.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = playerMaterial;
            PlayerMovement movement = instance.AddComponent<PlayerMovement>();
            PlayerController controller = instance.AddComponent<PlayerController>();
            SetObjectReference(controller, "_eventChannel", assets.EventChannel);
            SetObjectReference(controller, "_boardSettings", assets.BoardSettings);
            SetObjectReference(controller, "_presentationSettings", assets.PresentationSettings);
            SetObjectReference(controller, "_movement", movement);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab.GetComponent<PlayerController>();
        }

        /// <summary>
        /// Creates a 2D SpriteRenderer-only enemy prefab. POC HP/shield/intent information is rendered through OnGUI.
        /// </summary>
        private static EnemyView CreateEnemyPrefab(Sprite sprite)
        {
            string path = $"{PREFAB_ROOT}/EnemyView.prefab";
            GameObject instance = new GameObject("EnemyView");
            SpriteRenderer spriteRenderer = instance.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 1;
            EnemyView view = instance.AddComponent<EnemyView>();
            SetObjectReference(view, "_spriteRenderer", spriteRenderer);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab.GetComponent<EnemyView>();
        }

        /// <summary>
        /// Creates a standalone test scene using event-channel composition and OnGUI POC presentation.
        /// The user's existing GameScene remains untouched.
        /// </summary>
        private static void CreateOrReplaceScene(GeneratedAssets assets, GeneratedPrefabs prefabs)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(assets);
            CreateDirectionalLight();
            CreateBattleRoot(assets);
            CreateBoardRoot(assets, prefabs);
            CreatePlayerRoot(scene, assets, prefabs);
            CreateEnemyRoot(assets, prefabs);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
        }

        /// <summary>
        /// Creates an angled orthographic main camera and places BattlePointerInput on the same GameObject for cached Camera access.
        /// </summary>
        private static void CreateCamera(GeneratedAssets assets)
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 10f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.035f, 0.045f, 1f);

            BattlePointerInput pointerInput = cameraObject.AddComponent<BattlePointerInput>();
            SetObjectReference(pointerInput, "_eventChannel", assets.EventChannel);
            SetObjectReference(pointerInput, "_boardSettings", assets.BoardSettings);
            SetObjectReference(pointerInput, "_presentationSettings", assets.PresentationSettings);
            SetObjectReference(pointerInput, "_camera", camera);
        }

        /// <summary>
        /// Creates one directional light for URP 3D board readability.
        /// </summary>
        private static void CreateDirectionalLight()
        {
            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        /// <summary>
        /// Creates the battle composition root and its event-driven POC OnGUI presenter.
        /// </summary>
        private static void CreateBattleRoot(GeneratedAssets assets)
        {
            GameObject battleRoot = new GameObject("BattleRoot");
            BattleController battleController = battleRoot.AddComponent<BattleController>();
            SetObjectReference(battleController, "_boardSettings", assets.BoardSettings);
            SetObjectReference(battleController, "_playerBaseStats", assets.PlayerStats);
            SetObjectReference(battleController, "_presentationSettings", assets.PresentationSettings);
            SetObjectReference(battleController, "_deck", assets.Deck);
            SetObjectReference(battleController, "_encounter", assets.EncounterA);
            SetObjectReference(battleController, "_eventChannel", assets.EventChannel);

            BattleOnGuiPresenter onGuiPresenter = battleRoot.AddComponent<BattleOnGuiPresenter>();
            SetObjectReference(onGuiPresenter, "_eventChannel", assets.EventChannel);
        }

        /// <summary>
        /// Creates a dedicated player scene root and instantiates the already-configured 3D Player prefab before play begins.
        /// </summary>
        private static void CreatePlayerRoot(Scene scene, GeneratedAssets assets, GeneratedPrefabs prefabs)
        {
            GameObject playerRoot = new GameObject("PlayerRoot");
            GameObject playerObject = PrefabUtility.InstantiatePrefab(prefabs.PlayerPrefab.gameObject, scene) as GameObject;
            if (playerObject == null)
            {
                throw new InvalidOperationException("Failed to instantiate generated Player prefab.");
            }

            playerObject.transform.SetParent(playerRoot.transform, true);
            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            SetObjectReference(playerController, "_eventChannel", assets.EventChannel);
            SetObjectReference(playerController, "_boardSettings", assets.BoardSettings);
            SetObjectReference(playerController, "_presentationSettings", assets.PresentationSettings);
        }

        /// <summary>
        /// Creates world-space board presentation root with generic cell/block prefabs.
        /// </summary>
        private static void CreateBoardRoot(GeneratedAssets assets, GeneratedPrefabs prefabs)
        {
            GameObject boardRoot = new GameObject("BoardRoot");
            BoardView boardView = boardRoot.AddComponent<BoardView>();
            SetObjectReference(boardView, "_boardSettings", assets.BoardSettings);
            SetObjectReference(boardView, "_presentationSettings", assets.PresentationSettings);
            SetObjectReference(boardView, "_eventChannel", assets.EventChannel);
            SetObjectReference(boardView, "_cellPrefab", prefabs.CellPrefab);
            SetObjectReference(boardView, "_blockPrefab", prefabs.BlockPrefab);
        }

        /// <summary>
        /// Creates enemy presentation root that receives world-view setup and hit feedback only through the shared event channel.
        /// </summary>
        private static void CreateEnemyRoot(GeneratedAssets assets, GeneratedPrefabs prefabs)
        {
            GameObject enemyRoot = new GameObject("EnemyRoot");
            EnemyPartyView enemyPartyView = enemyRoot.AddComponent<EnemyPartyView>();
            SetObjectReference(enemyPartyView, "_eventChannel", assets.EventChannel);
            SetObjectReference(enemyPartyView, "_presentationSettings", assets.PresentationSettings);
            SetObjectReference(enemyPartyView, "_enemyViewPrefab", prefabs.EnemyPrefab);
        }

        /// <summary>
        /// Loads an existing ScriptableObject asset or creates it if absent so rebuilding the POC is idempotent.
        /// </summary>
        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && MonoScript.FromScriptableObject(asset) != null)
            {
                return asset;
            }

            // Generated assets created from a concrete ScriptableObject whose class did not have
            // a matching .cs file can contain m_Script: {fileID: 0}. Those assets cannot be repaired
            // by merely assigning serialized fields, so recreate only the invalid generated asset.
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path))
            {
                RemoveInvalidGeneratedAsset(path);
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>
        /// Removes one invalid auto-generated asset before recreating it with a valid MonoScript reference.
        /// </summary>
        private static void RemoveInvalidGeneratedAsset(string path)
        {
            if (AssetDatabase.DeleteAsset(path))
            {
                return;
            }

            // Fallback for a missing-script YAML asset that Unity cannot load as a main asset.
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            string metaPath = $"{path}.meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Assigns one integer serialized field and marks its asset dirty.
        /// </summary>
        private static void SetInt(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Assigns one Unity Object reference serialized field on either an asset, prefab component, or scene component.
        /// </summary>
        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property {propertyName} was not found on {target.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Immutable bundle of generated ScriptableObject references used during prefab/scene composition.
        /// </summary>
        private readonly struct GeneratedAssets
        {
            /// <summary>Shared battle event channel.</summary>
            public BattleEventChannelSO EventChannel { get; }

            /// <summary>Board settings asset.</summary>
            public BattleBoardSettingsSO BoardSettings { get; }

            /// <summary>Player base stats asset.</summary>
            public PlayerBaseStatsSO PlayerStats { get; }

            /// <summary>Presentation settings asset.</summary>
            public BattlePresentationSettingsSO PresentationSettings { get; }

            /// <summary>Default player deck asset.</summary>
            public DeckDefinitionSO Deck { get; }

            /// <summary>Default Enemy A encounter asset used by generated scene.</summary>
            public EncounterDefinitionSO EncounterA { get; }

            /// <summary>Alternative Enemy B encounter asset for inspector testing.</summary>
            public EncounterDefinitionSO EncounterB { get; }

            /// <summary>
            /// Creates a generated data bundle.
            /// </summary>
            public GeneratedAssets(
                BattleEventChannelSO eventChannel,
                BattleBoardSettingsSO boardSettings,
                PlayerBaseStatsSO playerStats,
                BattlePresentationSettingsSO presentationSettings,
                DeckDefinitionSO deck,
                EncounterDefinitionSO encounterA,
                EncounterDefinitionSO encounterB)
            {
                EventChannel = eventChannel;
                BoardSettings = boardSettings;
                PlayerStats = playerStats;
                PresentationSettings = presentationSettings;
                Deck = deck;
                EncounterA = encounterA;
                EncounterB = encounterB;
            }
        }

        /// <summary>
        /// Immutable bundle of generated prefab references used during scene composition.
        /// </summary>
        private readonly struct GeneratedPrefabs
        {
            /// <summary>Generic cell prefab.</summary>
            public BoardCellView CellPrefab { get; }

            /// <summary>Generic data-driven block prefab.</summary>
            public BlockView BlockPrefab { get; }

            /// <summary>3D player prefab.</summary>
            public PlayerController PlayerPrefab { get; }

            /// <summary>2D sprite enemy prefab.</summary>
            public EnemyView EnemyPrefab { get; }

            /// <summary>
            /// Creates a generated prefab bundle.
            /// </summary>
            public GeneratedPrefabs(
                BoardCellView cellPrefab,
                BlockView blockPrefab,
                PlayerController playerPrefab,
                EnemyView enemyPrefab)
            {
                CellPrefab = cellPrefab;
                BlockPrefab = blockPrefab;
                PlayerPrefab = playerPrefab;
                EnemyPrefab = enemyPrefab;
            }
        }
    }
}
#endif
