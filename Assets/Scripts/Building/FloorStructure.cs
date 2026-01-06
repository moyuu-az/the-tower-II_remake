using System.Collections.Generic;
using UnityEngine;
using TowerGame.Grid;

namespace TowerGame.Building
{
    /// <summary>
    /// Segment occupancy info on a floor
    /// </summary>
    [System.Serializable]
    public class SegmentOccupancy
    {
        public int segmentX;
        public bool isOccupied;
        public GameObject occupant; // Tenant occupying this segment
    }

    /// <summary>
    /// FloorStructure - Horizontal space for placing tenants (The Tower II style)
    /// Must be placed on 2F+ with full support from floor below
    /// Tenants (Office, Shop, etc.) can only be placed on existing FloorStructures
    /// </summary>
    public class FloorStructure : MonoBehaviour
    {
        [Header("Floor Settings")]
        [SerializeField] private int floorNumber; // 0-indexed (0 = 1F, 1 = 2F)
        [SerializeField] private int widthSegments = 9;
        [SerializeField] private int towerId = 0;

        [Header("Visual Settings")]
        [SerializeField] private Color floorColor = new Color(0.7f, 0.7f, 0.75f); // Gray

        [Header("Runtime (Read Only)")]
        [SerializeField] private int leftBoundary;
        [SerializeField] private int rightBoundary;
        [SerializeField] private List<SegmentOccupancy> occupancy = new List<SegmentOccupancy>();

        // Properties
        public int FloorNumber => floorNumber;
        public int DisplayFloor => floorNumber + 1;
        public int WidthSegments => widthSegments;
        public int TowerId => towerId;
        public int LeftBoundary => leftBoundary;
        public int RightBoundary => rightBoundary;

        /// <summary>
        /// Initialize the floor structure
        /// </summary>
        public void Initialize(int centerSegmentX, int floor, int width, int assignedTowerId)
        {
            floorNumber = floor;
            widthSegments = width;
            towerId = assignedTowerId;

            // Calculate boundaries
            int halfWidth = width / 2;
            leftBoundary = centerSegmentX - halfWidth;
            rightBoundary = centerSegmentX + halfWidth;

            // Initialize occupancy tracking
            occupancy.Clear();
            for (int x = leftBoundary; x <= rightBoundary; x++)
            {
                occupancy.Add(new SegmentOccupancy
                {
                    segmentX = x,
                    isOccupied = false,
                    occupant = null
                });
            }

            Debug.Log($"[FloorStructure] Initialized: {DisplayFloor}F, Tower {towerId}, Segments [{leftBoundary} to {rightBoundary}]");
        }

        /// <summary>
        /// Check if a segment is within this floor's boundary
        /// </summary>
        public bool IsWithinBoundary(int segmentX)
        {
            return segmentX >= leftBoundary && segmentX <= rightBoundary;
        }

        /// <summary>
        /// Check if a range of segments can be occupied by a tenant
        /// </summary>
        public bool CanOccupyRange(int startSegment, int endSegment)
        {
            // Check boundary
            if (startSegment < leftBoundary || endSegment > rightBoundary)
            {
                return false;
            }

            // Check if all segments are available
            for (int x = startSegment; x <= endSegment; x++)
            {
                var seg = GetSegmentOccupancy(x);
                if (seg == null || seg.isOccupied)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Occupy segments with a tenant
        /// </summary>
        public bool OccupySegments(int startSegment, int endSegment, GameObject tenant)
        {
            if (!CanOccupyRange(startSegment, endSegment))
            {
                return false;
            }

            for (int x = startSegment; x <= endSegment; x++)
            {
                var seg = GetSegmentOccupancy(x);
                if (seg != null)
                {
                    seg.isOccupied = true;
                    seg.occupant = tenant;
                }
            }

            Debug.Log($"[FloorStructure] {DisplayFloor}F: Segments [{startSegment} to {endSegment}] occupied by {tenant.name}");
            return true;
        }

        /// <summary>
        /// Release segments from a tenant
        /// </summary>
        public void ReleaseSegments(int startSegment, int endSegment)
        {
            for (int x = startSegment; x <= endSegment; x++)
            {
                var seg = GetSegmentOccupancy(x);
                if (seg != null)
                {
                    seg.isOccupied = false;
                    seg.occupant = null;
                }
            }

            Debug.Log($"[FloorStructure] {DisplayFloor}F: Segments [{startSegment} to {endSegment}] released");
        }

        /// <summary>
        /// Get total available (unoccupied) segments
        /// </summary>
        public int GetAvailableSegmentCount()
        {
            int count = 0;
            foreach (var seg in occupancy)
            {
                if (!seg.isOccupied)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Find a contiguous range of available segments
        /// </summary>
        public bool FindAvailableRange(int requiredWidth, out int startSegment)
        {
            startSegment = leftBoundary;
            int consecutiveAvailable = 0;

            for (int x = leftBoundary; x <= rightBoundary; x++)
            {
                var seg = GetSegmentOccupancy(x);
                if (seg != null && !seg.isOccupied)
                {
                    if (consecutiveAvailable == 0)
                    {
                        startSegment = x;
                    }
                    consecutiveAvailable++;

                    if (consecutiveAvailable >= requiredWidth)
                    {
                        return true;
                    }
                }
                else
                {
                    consecutiveAvailable = 0;
                }
            }

            return false;
        }

        private SegmentOccupancy GetSegmentOccupancy(int segmentX)
        {
            foreach (var seg in occupancy)
            {
                if (seg.segmentX == segmentX)
                {
                    return seg;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the world position for a work location on this floor
        /// </summary>
        public Vector2 GetWorkPosition(int segmentX)
        {
            if (GridManager.Instance == null || GridManager.Instance.Config == null)
            {
                return transform.position;
            }

            var config = GridManager.Instance.Config;
            float worldX = segmentX * config.segmentWidth;
            float worldY = config.groundLevel + (floorNumber * config.floorHeight) + (config.floorHeight / 2f);

            return new Vector2(worldX, worldY);
        }

        private void OnDrawGizmos()
        {
            if (GridManager.Instance == null || GridManager.Instance.Config == null) return;

            var config = GridManager.Instance.Config;

            // Draw floor boundary
            float leftX = leftBoundary * config.segmentWidth;
            float rightX = (rightBoundary + 1) * config.segmentWidth;
            float bottomY = config.groundLevel + (floorNumber * config.floorHeight);
            float topY = bottomY + config.floorHeight;

            // Draw rectangle (blue for floor)
            Gizmos.color = new Color(0.3f, 0.5f, 0.8f, 0.5f);
            Gizmos.DrawCube(
                new Vector3((leftX + rightX) / 2f, (bottomY + topY) / 2f, 0),
                new Vector3(rightX - leftX, topY - bottomY, 0.1f)
            );

            // Draw segment divisions
            Gizmos.color = Color.gray;
            for (int x = leftBoundary; x <= rightBoundary + 1; x++)
            {
                float segX = x * config.segmentWidth;
                Gizmos.DrawLine(new Vector3(segX, bottomY, 0), new Vector3(segX, topY, 0));
            }

            // Draw occupied segments in red
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.3f);
            foreach (var seg in occupancy)
            {
                if (seg.isOccupied)
                {
                    float segLeftX = seg.segmentX * config.segmentWidth;
                    float segRightX = (seg.segmentX + 1) * config.segmentWidth;
                    Gizmos.DrawCube(
                        new Vector3((segLeftX + segRightX) / 2f, (bottomY + topY) / 2f, 0),
                        new Vector3(segRightX - segLeftX, topY - bottomY, 0.05f)
                    );
                }
            }
        }
    }
}
