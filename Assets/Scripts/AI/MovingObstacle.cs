using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float pauseTime = 1f;
    
    private Vector3 targetPosition;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    
    void Start()
    {
        startPosition = transform.position;
        targetPosition = endPosition;
    }
    
    void Update()
    {
        if (isPaused)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseTime)
            {
                isPaused = false;
                pauseTimer = 0f;
            }
            return;
        }
        
        // Движение к целевой позиции
        transform.position = Vector3.MoveTowards(transform.position, 
            targetPosition, speed * Time.deltaTime);
        
        // Если достигли цели, меняем направление и делаем паузу
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            targetPosition = (targetPosition == endPosition) ? startPosition : endPosition;
            isPaused = true;
        }
    }
    
    void OnDrawGizmos()
    {
        // Визуализация пути препятствия в редакторе
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(startPosition, 0.5f);
        Gizmos.DrawSphere(endPosition, 0.5f);
        Gizmos.DrawLine(startPosition, endPosition);
    }
}