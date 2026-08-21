using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Centralizes POC run/loot IMGUI rectangles so input gating and presentation use the same screen regions.
    /// </summary>
    public static class RunOnGuiLayoutUtility
    {
        /// <summary>Shared distance from screen edges for POC run panels.</summary>
        private const float MARGIN = 18f;

        /// <summary>Preferred persistent stage/gold/deck panel width on normal desktop resolutions.</summary>
        private const float RUN_STATUS_PREFERRED_WIDTH = 570f;

        /// <summary>Minimum preferred stage/gold/deck panel width before screen-edge clamping.</summary>
        private const float RUN_STATUS_MIN_WIDTH = 420f;

        /// <summary>Screen-width ratio used to keep the run status panel readable across resolutions.</summary>
        private const float RUN_STATUS_WIDTH_RATIO = 0.42f;

        /// <summary>Persistent stage/gold/deck panel height.</summary>
        private const float RUN_STATUS_HEIGHT = 72f;

        /// <summary>Preferred development-only stage navigation panel width.</summary>
        private const float STAGE_DEBUG_PREFERRED_WIDTH = 460f;

        /// <summary>Development-only stage navigation panel height.</summary>
        private const float STAGE_DEBUG_HEIGHT = 82f;

        /// <summary>Standard reward/terminal modal width.</summary>
        private const float MODAL_WIDTH = 520f;

        /// <summary>Standard reward modal height.</summary>
        private const float MODAL_HEIGHT = 300f;

        /// <summary>Expanded discard-selection modal width.</summary>
        private const float DECK_MODAL_WIDTH = 660f;

        /// <summary>Expanded discard-selection modal height.</summary>
        private const float DECK_MODAL_HEIGHT = 560f;

        /// <summary>Gets a responsive top-center persistent run status panel.</summary>
        public static Rect GetRunStatusRect()
        {
            float availableWidth = Mathf.Max(1f, Screen.width - MARGIN * 2f);
            float responsiveWidth = Mathf.Clamp(
                Screen.width * RUN_STATUS_WIDTH_RATIO,
                RUN_STATUS_MIN_WIDTH,
                RUN_STATUS_PREFERRED_WIDTH);
            float width = Mathf.Min(availableWidth, responsiveWidth);
            return new Rect((Screen.width - width) * 0.5f, MARGIN, width, RUN_STATUS_HEIGHT);
        }

        /// <summary>Gets the bottom-left development-only stage navigation panel.</summary>
        public static Rect GetStageDebugRect()
        {
            float availableWidth = Mathf.Max(1f, Screen.width - MARGIN * 2f);
            float width = Mathf.Min(STAGE_DEBUG_PREFERRED_WIDTH, availableWidth);
            float y = Mathf.Max(MARGIN, Screen.height - MARGIN - STAGE_DEBUG_HEIGHT);
            return new Rect(MARGIN, y, width, STAGE_DEBUG_HEIGHT);
        }

        /// <summary>Gets centered reward/full-deck decision modal.</summary>
        public static Rect GetLootDecisionRect()
        {
            return new Rect(
                (Screen.width - MODAL_WIDTH) * 0.5f,
                (Screen.height - MODAL_HEIGHT) * 0.5f,
                MODAL_WIDTH,
                MODAL_HEIGHT);
        }

        /// <summary>Gets larger centered runtime-deck discard modal.</summary>
        public static Rect GetDeckDiscardRect()
        {
            return new Rect(
                (Screen.width - DECK_MODAL_WIDTH) * 0.5f,
                (Screen.height - DECK_MODAL_HEIGHT) * 0.5f,
                DECK_MODAL_WIDTH,
                DECK_MODAL_HEIGHT);
        }

        /// <summary>Gets centered terminal run prompt.</summary>
        public static Rect GetTerminalRect()
        {
            return new Rect(
                (Screen.width - MODAL_WIDTH) * 0.5f,
                (Screen.height - 240f) * 0.5f,
                MODAL_WIDTH,
                240f);
        }

        /// <summary>
        /// Returns true when a pointer overlaps a persistent run/debug IMGUI region.
        /// World board and loot pointer readers share this check so visual and blocked regions never drift apart.
        /// </summary>
        public static bool IsPointerOverPersistentGui(Vector2 guiPosition)
        {
            if (GetRunStatusRect().Contains(guiPosition))
            {
                return true;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return GetStageDebugRect().Contains(guiPosition);
#else
            return false;
#endif
        }
    }
}
