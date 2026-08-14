using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Base immutable stage-environment rule definition. Stage field effects do not occupy cells and are never player deck items.
    /// </summary>
    public abstract class StageFieldEffectDefinitionSO : ScriptableObject
    {
        [SerializeField, Tooltip("Human-readable field effect name used by debugging and future stage presentation.")]
        private string _displayName = "Stage Field Effect";

        /// <summary>Gets the field effect display name.</summary>
        public string DisplayName => _displayName;
    }
}
