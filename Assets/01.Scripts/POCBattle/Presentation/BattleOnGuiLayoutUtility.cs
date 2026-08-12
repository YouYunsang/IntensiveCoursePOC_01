using PocBattle.Core;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// Centralizes POC IMGUI rectangles so rendering and world-pointer input use the exact same hit regions.
    /// </summary>
    public static class BattleOnGuiLayoutUtility
    {
        /// <summary>Common screen edge margin in pixels.</summary>
        private const float SCREEN_MARGIN = 18f;

        /// <summary>Main player/battle status panel width.</summary>
        private const float STATUS_PANEL_WIDTH = 520f;

        /// <summary>Main player/battle status panel height.</summary>
        private const float STATUS_PANEL_HEIGHT = 178f;

        /// <summary>Enemy status panel width.</summary>
        private const float ENEMY_PANEL_WIDTH = 320f;

        /// <summary>Enemy status panel height.</summary>
        private const float ENEMY_PANEL_HEIGHT = 86f;

        /// <summary>Vertical gap between enemy panels.</summary>
        private const float ENEMY_PANEL_GAP = 8f;

        /// <summary>Context-sensitive end-phase button width.</summary>
        private const float END_PHASE_BUTTON_WIDTH = 180f;

        /// <summary>Context-sensitive end-phase button height.</summary>
        private const float END_PHASE_BUTTON_HEIGHT = 50f;

        /// <summary>Battle result box width.</summary>
        private const float RESULT_BOX_WIDTH = 360f;

        /// <summary>Battle result box height.</summary>
        private const float RESULT_BOX_HEIGHT = 170f;

        /// <summary>Restart button width.</summary>
        private const float RESTART_BUTTON_WIDTH = 180f;

        /// <summary>Restart button height.</summary>
        private const float RESTART_BUTTON_HEIGHT = 48f;

        /// <summary>
        /// Returns the top-left information panel rectangle.
        /// </summary>
        public static Rect GetStatusPanelRect()
        {
            return new Rect(SCREEN_MARGIN, SCREEN_MARGIN, STATUS_PANEL_WIDTH, STATUS_PANEL_HEIGHT);
        }

        /// <summary>
        /// Returns one top-right enemy information panel rectangle by display order.
        /// </summary>
        public static Rect GetEnemyPanelRect(int displayIndex)
        {
            float x = Screen.width - SCREEN_MARGIN - ENEMY_PANEL_WIDTH;
            float y = SCREEN_MARGIN + displayIndex * (ENEMY_PANEL_HEIGHT + ENEMY_PANEL_GAP);
            return new Rect(x, y, ENEMY_PANEL_WIDTH, ENEMY_PANEL_HEIGHT);
        }

        /// <summary>
        /// Returns the bottom-right manual phase completion button rectangle.
        /// </summary>
        public static Rect GetEndPhaseButtonRect()
        {
            return new Rect(
                Screen.width - SCREEN_MARGIN - END_PHASE_BUTTON_WIDTH,
                Screen.height - SCREEN_MARGIN - END_PHASE_BUTTON_HEIGHT,
                END_PHASE_BUTTON_WIDTH,
                END_PHASE_BUTTON_HEIGHT);
        }

        /// <summary>
        /// Returns the centered battle result panel rectangle.
        /// </summary>
        public static Rect GetResultPanelRect()
        {
            return new Rect(
                (Screen.width - RESULT_BOX_WIDTH) * 0.5f,
                (Screen.height - RESULT_BOX_HEIGHT) * 0.5f,
                RESULT_BOX_WIDTH,
                RESULT_BOX_HEIGHT);
        }

        /// <summary>
        /// Returns the restart button rectangle inside the centered result panel.
        /// </summary>
        public static Rect GetRestartButtonRect()
        {
            Rect resultRect = GetResultPanelRect();
            return new Rect(
                resultRect.center.x - RESTART_BUTTON_WIDTH * 0.5f,
                resultRect.yMax - RESTART_BUTTON_HEIGHT - 20f,
                RESTART_BUTTON_WIDTH,
                RESTART_BUTTON_HEIGHT);
        }

        /// <summary>
        /// Returns true when the current phase exposes the manual end-phase control.
        /// </summary>
        public static bool CanManuallyEndPhase(BattlePhase phase)
        {
            return phase == BattlePhase.PlacementEdit || phase == BattlePhase.Movement;
        }

        /// <summary>
        /// Returns true when a GUI-space pointer is over any visible POC IMGUI area.
        /// Board raycasts use this to prevent click-through behind status panels and controls.
        /// </summary>
        public static bool IsPointerOverVisibleGui(
            Vector2 guiPointerPosition,
            BattlePhase phase,
            BattleResult battleResult,
            int activeEnemyCount)
        {
            if (GetStatusPanelRect().Contains(guiPointerPosition))
            {
                return true;
            }

            for (int enemyIndex = 0; enemyIndex < activeEnemyCount; enemyIndex++)
            {
                if (GetEnemyPanelRect(enemyIndex).Contains(guiPointerPosition))
                {
                    return true;
                }
            }

            if (CanManuallyEndPhase(phase) && GetEndPhaseButtonRect().Contains(guiPointerPosition))
            {
                return true;
            }

            return battleResult != BattleResult.None && GetResultPanelRect().Contains(guiPointerPosition);
        }
    }
}
