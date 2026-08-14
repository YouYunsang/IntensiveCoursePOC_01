#if UNITY_EDITOR
using System;
using PocBattle.Data;
using UnityEditor;
using UnityEngine;

namespace PocBattle.Editor
{
    /// <summary>
    /// Incremental installer for editable cell effects and the first stage-owned row/column activation field effect.
    /// Existing run/deck/stage content is preserved and only missing references/defaults are appended.
    /// </summary>
    public static class PocBattleStageFieldPatchInstaller
    {
        private const string DATA_ROOT = "Assets/07.Data/POCBattle";
        private const string FIELD_EFFECT_PATH = DATA_ROOT + "/FieldEffect_RowColumnActivation.asset";
        private const string DIRECTION_EFFECT_PATH = DATA_ROOT + "/CellEffect_DirectionChange.asset";
        private const string PRESENTATION_SETTINGS_PATH = DATA_ROOT + "/BattlePresentationSettings.asset";

        /// <summary>Applies stage 3-5 field effect defaults plus editable cell-effect presentation defaults.</summary>
        [MenuItem("Tools/POC Battle/Apply Stage Field Effect Patch")]
        public static void ApplyStageFieldEffectPatch()
        {
            if (!ValidateRequiredBaseAssets(true))
            {
                return;
            }

            RowColumnActivationFieldEffectSO fieldEffect = CreateOrUpdateFieldEffect();
            ConfigureDirectionChangeVisualOffset();
            ConfigureCellEffectEditDefaults();
            AddFieldEffectToDefaultStages(fieldEffect);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool valid = ValidateStageFieldEffectSetup(fieldEffect, true);
            Debug.Log(valid
                ? "POC Stage Field patch applied. Stage 3-5 now use one fixed random special row/column and cell effects are placement-editable."
                : "POC Stage Field patch applied, but validation found errors. Check Console messages above.");
        }

        /// <summary>Validates data references without mutating project content.</summary>
        [MenuItem("Tools/POC Battle/Validate Stage Field Effect Setup")]
        public static void ValidateStageFieldEffectSetupMenu()
        {
            RowColumnActivationFieldEffectSO fieldEffect = AssetDatabase.LoadAssetAtPath<RowColumnActivationFieldEffectSO>(FIELD_EFFECT_PATH);
            if (ValidateStageFieldEffectSetup(fieldEffect, true))
            {
                Debug.Log("POC Stage Field Effect setup validation passed.");
            }
        }

        /// <summary>Creates or refreshes the first stage-owned row/column field definition.</summary>
        private static RowColumnActivationFieldEffectSO CreateOrUpdateFieldEffect()
        {
            RowColumnActivationFieldEffectSO fieldEffect = AssetDatabase.LoadAssetAtPath<RowColumnActivationFieldEffectSO>(FIELD_EFFECT_PATH);
            if (fieldEffect == null)
            {
                fieldEffect = ScriptableObject.CreateInstance<RowColumnActivationFieldEffectSO>();
                AssetDatabase.CreateAsset(fieldEffect, FIELD_EFFECT_PATH);
            }

            SerializedObject serialized = new SerializedObject(fieldEffect);
            SetStringIfPresent(serialized, "_displayName", "Row / Column Chain");
            SetColorIfPresent(serialized, "_rowHighlightColor", new Color(0.22f, 0.72f, 1f, 1f));
            SetColorIfPresent(serialized, "_columnHighlightColor", new Color(0.76f, 0.34f, 1f, 1f));
            SetColorIfPresent(serialized, "_intersectionHighlightColor", new Color(1f, 0.56f, 0.18f, 1f));
            SetFloatIfPresent(serialized, "_idlePulseStrength", 0.18f);
            SetFloatIfPresent(serialized, "_idlePulseDuration", 0.8f);
            SetColorIfPresent(serialized, "_activationFlashColor", Color.white);
            SetFloatIfPresent(serialized, "_activationFlashDuration", 0.24f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fieldEffect);
            return fieldEffect;
        }

        /// <summary>Raises only the current Direction Change board sprite by default while keeping the field user-tunable.</summary>
        private static void ConfigureDirectionChangeVisualOffset()
        {
            DirectionChangeCellEffectSO directionEffect = AssetDatabase.LoadAssetAtPath<DirectionChangeCellEffectSO>(DIRECTION_EFFECT_PATH);
            if (directionEffect == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(directionEffect);
            SerializedProperty property = serialized.FindProperty("_boardVisualOffset");
            if (property != null && property.vector3Value == Vector3.zero)
            {
                property.vector3Value = new Vector3(0f, 0.4f, 0f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(directionEffect);
            }
        }

        /// <summary>Writes explicit cell-effect drag defaults only when the newly introduced properties are present.</summary>
        private static void ConfigureCellEffectEditDefaults()
        {
            BattlePresentationSettingsSO settings = AssetDatabase.LoadAssetAtPath<BattlePresentationSettingsSO>(PRESENTATION_SETTINGS_PATH);
            if (settings == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(settings);
            SetFloatIfPresent(serialized, "_cellEffectDragLiftHeight", 0.35f);
            SetFloatIfPresent(serialized, "_cellEffectDragLiftDuration", 0.1f);
            SetFloatIfPresent(serialized, "_cellEffectDragScale", 1.12f);
            SetFloatIfPresent(serialized, "_cellEffectDropDuration", 0.14f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        /// <summary>Adds the field effect to stages 3-5 without deleting any existing stage field entries.</summary>
        private static void AddFieldEffectToDefaultStages(RowColumnActivationFieldEffectSO fieldEffect)
        {
            for (int stageNumber = 3; stageNumber <= 5; stageNumber++)
            {
                StageDefinitionSO stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"{DATA_ROOT}/Stage_{stageNumber:00}.asset");
                if (stage == null)
                {
                    throw new InvalidOperationException($"Stage_{stageNumber:00}.asset is missing.");
                }

                SerializedObject serialized = new SerializedObject(stage);
                SerializedProperty fieldEffects = serialized.FindProperty("_fieldEffects");
                bool alreadyPresent = false;
                for (int effectIndex = 0; effectIndex < fieldEffects.arraySize; effectIndex++)
                {
                    if (fieldEffects.GetArrayElementAtIndex(effectIndex).objectReferenceValue == fieldEffect)
                    {
                        alreadyPresent = true;
                        break;
                    }
                }

                if (!alreadyPresent)
                {
                    int newIndex = fieldEffects.arraySize;
                    fieldEffects.arraySize++;
                    fieldEffects.GetArrayElementAtIndex(newIndex).objectReferenceValue = fieldEffect;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stage);
            }
        }

        /// <summary>Checks required definitions and default stage references.</summary>
        private static bool ValidateStageFieldEffectSetup(RowColumnActivationFieldEffectSO fieldEffect, bool logErrors)
        {
            bool valid = ValidateRequiredBaseAssets(logErrors);
            valid &= Validate(fieldEffect != null, "FieldEffect_RowColumnActivation.asset is missing.", logErrors);

            for (int stageNumber = 3; stageNumber <= 5; stageNumber++)
            {
                StageDefinitionSO stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"{DATA_ROOT}/Stage_{stageNumber:00}.asset");
                valid &= Validate(stage != null, $"Stage_{stageNumber:00}.asset is missing.", logErrors);
                if (stage == null || fieldEffect == null)
                {
                    continue;
                }

                bool found = false;
                for (int effectIndex = 0; effectIndex < stage.FieldEffects.Count; effectIndex++)
                {
                    if (stage.FieldEffects[effectIndex] == fieldEffect)
                    {
                        found = true;
                        break;
                    }
                }
                valid &= Validate(found, $"Stage {stageNumber} does not reference the row/column field effect.", logErrors);
            }

            return valid;
        }

        /// <summary>Checks assets required by this incremental patch.</summary>
        private static bool ValidateRequiredBaseAssets(bool logErrors)
        {
            bool valid = true;
            valid &= Validate(AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"{DATA_ROOT}/Stage_03.asset") != null, "Stage_03.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"{DATA_ROOT}/Stage_04.asset") != null, "Stage_04.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<StageDefinitionSO>($"{DATA_ROOT}/Stage_05.asset") != null, "Stage_05.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<BattlePresentationSettingsSO>(PRESENTATION_SETTINGS_PATH) != null, "BattlePresentationSettings.asset is missing.", logErrors);
            valid &= Validate(AssetDatabase.LoadAssetAtPath<DirectionChangeCellEffectSO>(DIRECTION_EFFECT_PATH) != null, "CellEffect_DirectionChange.asset is missing. Apply the previous cell-effect patch first.", logErrors);
            return valid;
        }

        /// <summary>Writes a string property when it exists on the serialized object.</summary>
        private static void SetStringIfPresent(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.stringValue = value;
        }

        /// <summary>Writes a color property when it exists on the serialized object.</summary>
        private static void SetColorIfPresent(SerializedObject serialized, string propertyName, Color value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.colorValue = value;
        }

        /// <summary>Writes a float property when it exists on the serialized object.</summary>
        private static void SetFloatIfPresent(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        /// <summary>Logs one validation failure while returning the condition for aggregation.</summary>
        private static bool Validate(bool condition, string message, bool logErrors)
        {
            if (!condition && logErrors)
            {
                Debug.LogError(message);
            }
            return condition;
        }
    }
}
#endif
