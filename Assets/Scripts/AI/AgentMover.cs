using UnityEngine;
using UnityEngine.AI; // Важно подключить пространство имен для навигации

public class AgentMover : MonoBehaviour
{
    [SerializeField] private Transform target; // Цель для движения
    [SerializeField] private bool patrolMode = false; // Режим патрулирования
    [SerializeField] private Transform[] patrolPoints; // Массив точек патрулирования
    [SerializeField] private float waypointThreshold = 1f; // Расстояние до точки, при котором она считается достигнутой
    
    private NavMeshAgent agent;
    private int currentPatrolIndex = 0;
    private bool isPatrolForward = true;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent не найден на объекте: " + gameObject.name);
            return;
        }
        
        // Если режим патрулирования выключен, двигаемся к цели
        if (!patrolMode && target != null)
        {
            SetDestination(target.position);
        }
    }
    
    void Update()
    {
        if (patrolMode && patrolPoints.Length > 0)
        {
            Patrol();
        }
        
        // Отладочная визуализация пути (только в редакторе)
        #if UNITY_EDITOR
        if (agent.hasPath)
        {
            Debug.DrawLine(transform.position, agent.destination, Color.yellow);
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Debug.DrawLine(agent.path.corners[i], agent.path.corners[i + 1], Color.red);
            }
        }
        #endif
    }
    
    // Установка новой цели
    public void SetDestination(Vector3 destination)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.SetDestination(destination);
            Debug.Log("Новая цель установлена: " + destination);
        }
    }
    
    // Патрулирование между точками
    void Patrol()
    {
        if (patrolPoints.Length == 0) return;
        
        // Проверяем, достигли ли текущей точки
        float distanceToPoint = Vector3.Distance(transform.position, 
            patrolPoints[currentPatrolIndex].position);
        
        if (distanceToPoint <= waypointThreshold)
        {
            // Переходим к следующей точке
            if (isPatrolForward)
            {
                currentPatrolIndex++;
                if (currentPatrolIndex >= patrolPoints.Length)
                {
                    currentPatrolIndex = patrolPoints.Length - 2;
                    isPatrolForward = false;
                }
            }
            else
            {
                currentPatrolIndex--;
                if (currentPatrolIndex < 0)
                {
                    currentPatrolIndex = 1;
                    isPatrolForward = true;
                }
            }
            
            // Устанавливаем новую цель
            SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }
    
    // Остановка агента
    public void Stop()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }
    
    // Возобновление движения
    public void Resume()
    {
        if (agent != null && agent.isStopped)
        {
            agent.isStopped = false;
        }
    }
    
    // Проверка, достиг ли агент цели
    public bool HasReachedDestination()
    {
        if (agent == null) return false;
        
        return !agent.pathPending 
            && agent.remainingDistance <= agent.stoppingDistance 
            && (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
    }
}
