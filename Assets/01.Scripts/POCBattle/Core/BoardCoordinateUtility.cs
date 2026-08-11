using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Converts between logical board coordinates and deterministic world-space positions.
    /// </summary>
    public static class BoardCoordinateUtility
    {
        /// <summary>
        /// Converts one grid coordinate to a centered XZ world position using the requested Y value.
        /// </summary>
        public static Vector3 GridToWorld(BattleBoardSettingsSO settings, Vector2Int coordinate, float worldY)
        {
            float centerOffsetX = (settings.Columns - 1) * 0.5f;
            float centerOffsetZ = (settings.Rows - 1) * 0.5f;
            float worldX = settings.BoardCenter.x + (coordinate.x - centerOffsetX) * settings.CellStep;
            float worldZ = settings.BoardCenter.z + (coordinate.y - centerOffsetZ) * settings.CellStep;
            return new Vector3(worldX, worldY, worldZ);
        }

        /// <summary>
        /// Attempts to map a world point back to the nearest valid grid coordinate.
        /// </summary>
        public static bool TryWorldToGrid(BattleBoardSettingsSO settings, Vector3 worldPosition, out Vector2Int coordinate)
        {
            float centerOffsetX = (settings.Columns - 1) * 0.5f;
            float centerOffsetZ = (settings.Rows - 1) * 0.5f;
            float localX = (worldPosition.x - settings.BoardCenter.x) / settings.CellStep + centerOffsetX;
            float localZ = (worldPosition.z - settings.BoardCenter.z) / settings.CellStep + centerOffsetZ;
            int column = Mathf.RoundToInt(localX);
            int row = Mathf.RoundToInt(localZ);
            coordinate = new Vector2Int(column, row);
            return column >= 0 && column < settings.Columns && row >= 0 && row < settings.Rows;
        }
    }
}
