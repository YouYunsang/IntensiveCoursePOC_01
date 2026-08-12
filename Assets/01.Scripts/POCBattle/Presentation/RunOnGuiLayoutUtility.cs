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

        /// <summary>Persistent stage/gold/deck panel width.</summary>
        private const float RUN_STATUS_WIDTH = 360f;

        /// <summary>Persistent stage/gold/deck panel height.</summary>
        private const float RUN_STATUS_HEIGHT = 48f;

        /// <summary>Standard reward/terminal modal width.</summary>
        private const float MODAL_WIDTH = 520f;

        /// <summary>Standard reward modal height.</summary>
        private const float MODAL_HEIGHT = 300f;

        /// <summary>Expanded discard-selection modal width.</summary>
        private const float DECK_MODAL_WIDTH = 660f;

        /// <summary>Expanded discard-selection modal height.</summary>
        private const float DECK_MODAL_HEIGHT = 560f;

        /// <summary>Gets top-center persistent run status panel.</summary>
        public static Rect GetRunStatusRect()
        {
            return new Rect((Screen.width - RUN_STATUS_WIDTH) * 0.5f, MARGIN, RUN_STATUS_WIDTH, RUN_STATUS_HEIGHT);
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
        /// Returns true when a pointer overlaps the persistent run status region.
        /// LootPointerInput only accepts world loot clicks outside this region.
        /// </summary>
        public static bool IsPointerOverPersistentGui(Vector2 guiPosition)
        {
            return GetRunStatusRect().Contains(guiPosition);
        }
    }
}
