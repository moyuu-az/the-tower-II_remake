using UnityEngine;

namespace TowerGame.People
{
    /// <summary>
    /// Base states for all people
    /// </summary>
    public enum PersonState
    {
        Idle,
        Walking,
        Working,
        Commuting
    }

    /// <summary>
    /// Base class for all people in the simulation
    /// The Tower II style discrete/tile-based movement
    /// </summary>
    public class Person : MonoBehaviour
    {
        [Header("Movement Settings (The Tower II Style)")]
        [SerializeField] protected float stepSize = 0.5f; // Units per step
        [SerializeField] protected float stepInterval = 0.1f; // Seconds between steps
        [SerializeField] protected float arrivalThreshold = 0.1f;

        [Header("Current State (Read Only)")]
        [SerializeField] protected PersonState currentState = PersonState.Idle;
        [SerializeField] protected Vector2 targetPosition;
        [SerializeField] protected bool isMoving;

        // Discrete movement timer
        private float stepTimer = 0f;

        // Properties
        public PersonState CurrentState => currentState;
        public Vector2 Position => transform.position;
        public bool IsMoving => isMoving;
        public float StepSize
        {
            get => stepSize;
            set => stepSize = Mathf.Max(0.1f, value);
        }

        protected SpriteRenderer spriteRenderer;

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        protected virtual void Update()
        {
            if (isMoving)
            {
                stepTimer += Time.deltaTime;
                if (stepTimer >= stepInterval)
                {
                    stepTimer = 0f;
                    MoveOneStep();
                }
            }
        }

        /// <summary>
        /// Move one discrete step towards target (The Tower II style)
        /// </summary>
        protected virtual void MoveOneStep()
        {
            Vector2 currentPos = transform.position;
            Vector2 diff = targetPosition - currentPos;
            float distance = diff.magnitude;

            if (distance <= arrivalThreshold)
            {
                // Arrived at destination - snap to exact position
                transform.position = targetPosition;
                isMoving = false;
                OnReachedDestination();
                return;
            }

            // Calculate one step movement
            Vector2 direction = diff.normalized;
            float moveDistance = Mathf.Min(stepSize, distance);
            Vector2 newPos = currentPos + direction * moveDistance;

            // Snap to position instantly (discrete movement)
            transform.position = newPos;

            // Flip sprite based on movement direction
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.1f)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }

        /// <summary>
        /// Start moving to a target position
        /// </summary>
        public virtual void MoveTo(Vector2 target)
        {
            targetPosition = target;
            isMoving = true;
            stepTimer = 0f; // Reset step timer
            currentState = PersonState.Walking;

            // Flip sprite immediately towards target
            Vector2 direction = target - (Vector2)transform.position;
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.1f)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }

        /// <summary>
        /// Called when the person reaches their destination
        /// </summary>
        protected virtual void OnReachedDestination()
        {
            currentState = PersonState.Idle;
            Debug.Log($"[Person] {gameObject.name} reached destination");
        }

        /// <summary>
        /// Stop all movement
        /// </summary>
        public virtual void Stop()
        {
            isMoving = false;
            currentState = PersonState.Idle;
        }

        /// <summary>
        /// Teleport to a position instantly
        /// </summary>
        public virtual void TeleportTo(Vector2 position)
        {
            transform.position = position;
            targetPosition = position;
            isMoving = false;
        }

        /// <summary>
        /// Set the visual color of this person
        /// </summary>
        public void SetColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }
    }
}
