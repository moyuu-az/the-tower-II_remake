using System.Collections.Generic;
using UnityEngine;
using TowerGame.Grid;

namespace TowerGame.Building
{
    /// <summary>
    /// Lobby - The foundation of a tower (The Tower II style)
    /// Must be placed on 1F first before any other buildings
    /// Defines the building's horizontal boundary
    /// </summary>
    public class Lobby : MonoBehaviour
    {
        [Header("Lobby Settings")]
        [SerializeField] private int widthSegments = 9;
        [SerializeField] private int towerId = 0;

        [Header("Visual Settings")]
        [SerializeField] private Color lobbyColor = new Color(0.9f, 0.85f, 0.7f); // Warm beige

        // Runtime data
        private int leftBoundary;
        private int rightBoundary;
        private Vector2 entrancePosition;
        private List<Transform> transportationPoints = new List<Transform>();

        // Properties
        public int WidthSegments => widthSegments;
        public int TowerId => towerId;
        public int LeftBoundary => leftBoundary;
        public int RightBoundary => rightBoundary;
        public Vector2 EntrancePosition => entrancePosition;

        /// <summary>
        /// Initialize the lobby with position and width
        /// </summary>
        public void Initialize(int centerSegmentX, int width, int assignedTowerId)
        {
            widthSegments = width;
            towerId = assignedTowerId;

            // Calculate boundaries
            int halfWidth = width / 2;
            leftBoundary = centerSegmentX - halfWidth;
            rightBoundary = centerSegmentX + halfWidth;

            // Set entrance position at center bottom
            if (GridManager.Instance != null && GridManager.Instance.Config != null)
            {
                var config = GridManager.Instance.Config;
                entrancePosition = new Vector2(
                    centerSegmentX * config.segmentWidth,
                    config.groundLevel
                );
            }

            Debug.Log($"[Lobby] Initialized: Tower {towerId}, Segments [{leftBoundary} to {rightBoundary}], Width {widthSegments}");
        }

        /// <summary>
        /// Check if a position is within this lobby's boundary
        /// </summary>
        public bool IsWithinBoundary(int segmentX)
        {
            return segmentX >= leftBoundary && segmentX <= rightBoundary;
        }

        /// <summary>
        /// Check if a range fits within this lobby's boundary
        /// </summary>
        public bool CanFitRange(int startSegment, int endSegment)
        {
            return startSegment >= leftBoundary && endSegment <= rightBoundary;
        }

        /// <summary>
        /// Get the maximum width for buildings above this lobby
        /// </summary>
        public int GetMaxWidthForFloor(int floor)
        {
            // For now, upper floors can use full lobby width
            // This can be modified later for stepped building designs
            return widthSegments;
        }

        /// <summary>
        /// Register a transportation point (elevator, stairs)
        /// </summary>
        public void RegisterTransportation(Transform transportPoint)
        {
            if (!transportationPoints.Contains(transportPoint))
            {
                transportationPoints.Add(transportPoint);
                Debug.Log($"[Lobby] Registered transportation at {transportPoint.position}");
            }
        }

        /// <summary>
        /// Get the nearest entrance point for a given position
        /// </summary>
        public Vector2 GetNearestEntrancePoint(Vector2 fromPosition)
        {
            // For now, return center entrance
            // Later can add multiple entrances
            return entrancePosition;
        }

        private void OnDrawGizmos()
        {
            if (GridManager.Instance == null || GridManager.Instance.Config == null) return;

            var config = GridManager.Instance.Config;

            // Draw lobby boundary
            Gizmos.color = Color.yellow;
            float leftX = leftBoundary * config.segmentWidth;
            float rightX = (rightBoundary + 1) * config.segmentWidth;
            float bottomY = config.groundLevel;
            float topY = bottomY + config.floorHeight;

            // Draw rectangle
            Gizmos.DrawLine(new Vector3(leftX, bottomY, 0), new Vector3(rightX, bottomY, 0));
            Gizmos.DrawLine(new Vector3(rightX, bottomY, 0), new Vector3(rightX, topY, 0));
            Gizmos.DrawLine(new Vector3(rightX, topY, 0), new Vector3(leftX, topY, 0));
            Gizmos.DrawLine(new Vector3(leftX, topY, 0), new Vector3(leftX, bottomY, 0));

            // Draw entrance
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(entrancePosition, 0.5f);
        }
    }
}
