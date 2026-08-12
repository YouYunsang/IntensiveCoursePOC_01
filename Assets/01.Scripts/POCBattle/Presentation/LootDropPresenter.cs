using DG.Tweening;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Owns the single reusable world-space loot visual: generic block view, floating tween, and pulsing point light.
    /// </summary>
    public sealed class LootDropPresenter : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared event hub used for loot appearance without direct RunController references.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Board size used to derive loot block dimensions from the existing generic block prefab.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Loot float/glow and block visual tuning.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Existing generic Block prefab reused as the visible loot reward.")]
        private BlockView _blockPrefab;

        /// <summary>Single reusable block view instantiated on first loot drop.</summary>
        private BlockView _lootBlockView;

        /// <summary>Single reusable point light owned by this presenter.</summary>
        private Light _glowLight;

        /// <summary>Looping loot float tween.</summary>
        private Tween _floatTween;

        /// <summary>Looping glow intensity tween.</summary>
        private Tween _glowTween;

        /// <summary>Subtle looping scale pulse that keeps loot visibly radiant even when URP additional lights are disabled.</summary>
        private Tween _pulseTween;

        /// <summary>Subscribes to loot visibility snapshots.</summary>
        private void OnEnable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.LootDropChanged += HandleLootDropChanged;
            }
        }

        /// <summary>Removes subscription and stops presentation loops.</summary>
        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.LootDropChanged -= HandleLootDropChanged;
            }

            HideLoot();
        }

        /// <summary>Creates/reuses or hides the floating world reward from immutable loot data.</summary>
        private void HandleLootDropChanged(LootDropSnapshot snapshot)
        {
            if (!snapshot.IsVisible || snapshot.Definition == null)
            {
                HideLoot();
                return;
            }

            EnsurePresentationObjects();
            KillTweens();

            Vector3 baseScale = new Vector3(
                _boardSettings.CellSize * _presentationSettings.BlockFootprintRatio,
                _presentationSettings.BlockHeight,
                _boardSettings.CellSize * _presentationSettings.BlockFootprintRatio)
                * _presentationSettings.LootBlockScaleMultiplier;
            Vector3 basePosition = snapshot.WorldPosition + Vector3.up * _presentationSettings.LootHeightOffset;

            _lootBlockView.gameObject.SetActive(true);
            _lootBlockView.Configure(snapshot.Definition, baseScale, _presentationSettings.CriticalMultiplier);
            _lootBlockView.SnapTo(basePosition);

            _glowLight.gameObject.SetActive(true);
            _glowLight.color = snapshot.Definition.DisplayColor;
            _glowLight.range = _presentationSettings.LootGlowRange;
            _glowLight.intensity = _presentationSettings.LootGlowIntensity;

            _floatTween = _lootBlockView.transform
                .DOMoveY(basePosition.y + _presentationSettings.LootFloatHeight, _presentationSettings.LootFloatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            _pulseTween = _lootBlockView.transform
                .DOScale(baseScale * 1.06f, _presentationSettings.LootGlowPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            float lowIntensity = _presentationSettings.LootGlowIntensity * 0.55f;
            _glowTween = DOTween.To(
                    () => _glowLight.intensity,
                    value => _glowLight.intensity = value,
                    lowIntensity,
                    _presentationSettings.LootGlowPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>Creates presentation-owned child objects once and reuses them for every stage reward.</summary>
        private void EnsurePresentationObjects()
        {
            if (_lootBlockView == null)
            {
                _lootBlockView = Instantiate(_blockPrefab, transform);
                _lootBlockView.name = "LootBlockView";
            }

            if (_glowLight == null)
            {
                GameObject lightObject = new GameObject("LootGlow");
                lightObject.transform.SetParent(_lootBlockView.transform, false);
                _glowLight = lightObject.AddComponent<Light>();
                _glowLight.type = LightType.Point;
                _glowLight.shadows = LightShadows.None;
            }
        }

        /// <summary>Stops loops and hides the reusable loot presentation.</summary>
        private void HideLoot()
        {
            KillTweens();
            if (_lootBlockView != null)
            {
                _lootBlockView.gameObject.SetActive(false);
            }

            if (_glowLight != null)
            {
                _glowLight.gameObject.SetActive(false);
            }
        }

        /// <summary>Kills only loot-owned looping tweens.</summary>
        private void KillTweens()
        {
            if (_floatTween != null && _floatTween.IsActive())
            {
                _floatTween.Kill();
            }
            _floatTween = null;

            if (_glowTween != null && _glowTween.IsActive())
            {
                _glowTween.Kill();
            }
            _glowTween = null;

            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
            }
            _pulseTween = null;
        }
    }
}
