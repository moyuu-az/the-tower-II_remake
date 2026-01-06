using System.Collections.Generic;
using UnityEngine;
using TowerGame.Building;

namespace TowerGame.People
{
    /// <summary>
    /// Spawns and manages employees
    /// </summary>
    public class PersonSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int employeeCount = 3;
        [SerializeField] private Vector2 spawnPoint = new Vector2(-15f, -3f); // Ground level (Y=-3), left side
        [SerializeField] private float employeeSpacing = 1.0f; // Horizontal spacing between employees

        [Header("Employee Visual Settings")]
        [SerializeField] private Vector2 employeeSize = new Vector2(0.5f, 0.8f);
        [SerializeField] private Color[] employeeColors = new Color[]
        {
            new Color(0.2f, 0.4f, 0.8f), // Blue
            new Color(0.8f, 0.3f, 0.3f), // Red
            new Color(0.3f, 0.7f, 0.3f), // Green
            new Color(0.8f, 0.6f, 0.2f), // Orange
            new Color(0.6f, 0.3f, 0.7f)  // Purple
        };

        [Header("References")]
        [SerializeField] private OfficeBuilding targetOffice;

        [Header("Runtime (Read Only)")]
        [SerializeField] private List<Employee> spawnedEmployees = new List<Employee>();

        // Properties
        public List<Employee> Employees => spawnedEmployees;
        public int EmployeeCount => employeeCount;

        private void Start()
        {
            // Auto-find an available office if not set
            if (targetOffice == null)
            {
                targetOffice = FindAvailableOffice();
            }

            if (targetOffice == null)
            {
                Debug.Log("[PersonSpawner] No office available - employees will wait for one to be placed");
            }

            SpawnEmployees();
        }

        private void Update()
        {
            // If no target office, try to find any available office (for when player places first office)
            if (targetOffice == null)
            {
                targetOffice = FindAvailableOffice();
                if (targetOffice != null)
                {
                    Debug.Log($"[PersonSpawner] Found new office on {targetOffice.DisplayFloor}F! Assigning to employees.");
                    AssignOfficeToEmployees(targetOffice);
                }
            }
        }

        /// <summary>
        /// Find any available office (employees can use elevator for upper floors)
        /// </summary>
        private OfficeBuilding FindAvailableOffice()
        {
            OfficeBuilding[] offices = FindObjectsByType<OfficeBuilding>(FindObjectsSortMode.None);

            // First, try to find a ground floor office (easier access)
            foreach (var office in offices)
            {
                if (office.IsGroundFloor && !office.IsFull)
                {
                    return office;
                }
            }

            // If no ground floor office, find any available office (upper floors need elevator)
            foreach (var office in offices)
            {
                if (!office.IsFull)
                {
                    Debug.Log($"[PersonSpawner] Using upper floor office on {office.DisplayFloor}F (employees will use elevator)");
                    return office;
                }
            }

            return null;
        }

        /// <summary>
        /// Assign an office to all employees
        /// </summary>
        private void AssignOfficeToEmployees(OfficeBuilding office)
        {
            foreach (var emp in spawnedEmployees)
            {
                emp.AssignOffice(office);
            }
        }

        /// <summary>
        /// Spawn all employees
        /// </summary>
        private void SpawnEmployees()
        {
            for (int i = 0; i < employeeCount; i++)
            {
                SpawnEmployee(i);
            }

            Debug.Log($"[PersonSpawner] Spawned {employeeCount} employees");
        }

        /// <summary>
        /// Spawn a single employee
        /// </summary>
        private Employee SpawnEmployee(int index)
        {
            // Create employee GameObject
            GameObject empGO = new GameObject($"Employee_{index}");
            empGO.transform.SetParent(transform);

            // Add and setup SpriteRenderer
            SpriteRenderer sr = empGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateEmployeeSprite();
            sr.color = GetEmployeeColor(index);
            sr.sortingOrder = 10; // Above ground and building

            // Set size
            empGO.transform.localScale = new Vector3(employeeSize.x, employeeSize.y, 1f);

            // Calculate individual home position with offset to prevent overlap
            Vector2 homePos = spawnPoint + new Vector2(index * employeeSpacing, 0);

            // Add Employee component
            Employee emp = empGO.AddComponent<Employee>();
            emp.Initialize(index, targetOffice, homePos);

            spawnedEmployees.Add(emp);

            return emp;
        }

        /// <summary>
        /// Create a simple sprite for employees
        /// </summary>
        private Sprite CreateEmployeeSprite()
        {
            // Create a simple white texture
            Texture2D tex = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Create a simple person shape
                    float centerX = 16;
                    float centerY = 16;

                    // Head (circle at top)
                    float headDist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, 24));
                    bool isHead = headDist < 6;

                    // Body (rectangle)
                    bool isBody = x >= 10 && x <= 22 && y >= 4 && y <= 18;

                    colors[y * 32 + x] = (isHead || isBody) ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }

        /// <summary>
        /// Get color for employee by index
        /// </summary>
        private Color GetEmployeeColor(int index)
        {
            if (employeeColors == null || employeeColors.Length == 0)
            {
                return Color.white;
            }
            return employeeColors[index % employeeColors.Length];
        }

        /// <summary>
        /// Get status of all employees
        /// </summary>
        public string GetAllEmployeeStatus()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var emp in spawnedEmployees)
            {
                sb.AppendLine(emp.GetStatusString());
            }
            return sb.ToString();
        }

        private void OnDrawGizmos()
        {
            // Draw spawn point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint, 0.5f);
            Gizmos.DrawLine(spawnPoint, spawnPoint + Vector2.right * 2f);
        }
    }
}
