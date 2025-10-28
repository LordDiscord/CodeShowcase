using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IRestartGameElement
{
    enum IState
    {
        IDLE = 0,
        ALERT,
        CHASE,
        PATROL,
        ATTACK,
        HIT,
        DIE
    }
    [Header("Audio")]
    public AudioSource shootAudio;//audioSource
    public AudioClip shootSound;//clip de audio

    NavMeshAgent m_NavMeshAgent;
    IState m_State;

    public List<Transform> m_PatrolPositions;
    public float m_MinDistanceToAlert = 5.0f;
    public float m_MinDistanceToAttack = 3.0f;
    public float m_MaxDistanceToAlert = 7.0f;
    public float m_MaxDistanceToAttack = 15.0f;
    public float m_ConAngle = 60.0f;
    public LayerMask m_SightLayerMask;
    public float attackDamage = 10.0f;  // Daño que el enemigo hace al jugador
    public float attackCooldown = 2.0f; // Tiempo de espera entre ataques
    private float lastAttackTime; // Último momento de ataque

    int m_CurrentPatrolPositionId = 0;

    public Transform m_LifeBaUIPosition;
    public EnemyLifeBarUI m_EnemyLifeBarUI;
    public float m_MaxLife = 50.0f;
    float m_Life;
    Vector3 m_StartPosition;
    Quaternion m_StartRotation;

    private void Awake()
    {
        m_Life = m_MaxLife;
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        lastAttackTime = 0f; // Inicializar el tiempo del último ataque
    }

    private void Start()
    {
        GameManager.GetGameManager().AddRestartGameElement(this);
        SetIdleState();
        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;
    }

    void SetIdleState() { m_State = IState.IDLE; }
    void SetAlertState() { m_State = IState.ALERT; m_NavMeshAgent.isStopped = true; }
    void SetChaseState() { m_State = IState.CHASE; }
    void SetPatrolState()
    {
        m_State = IState.PATROL;
        m_NavMeshAgent.isStopped = false;
        m_CurrentPatrolPositionId = GetClosestPatrolPositionId();
        MoveToNextPatrolPosition();
    }
    void SetAttackState() { m_State = IState.ATTACK; }
    void SetHitState() { m_State = IState.HIT; }
    void SetDieState()
    {
        m_State = IState.DIE;
        m_EnemyLifeBarUI.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    void Update()
    {
        switch (m_State)
        {
            case IState.IDLE:
                UpdateIdleState();
                break;
            case IState.ALERT:
                UpdateAlertState();
                break;
            case IState.CHASE:
                UpdateChaseState();
                break;
            case IState.PATROL:
                UpdatePatrolState();
                break;
            case IState.ATTACK:
                UpdateAttackState();
                break;
            case IState.HIT:
                UpdateHitState();
                break;
            case IState.DIE:
                UpdateDieState();
                break;
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        Vector3 playerPosition = GetPlayerPosition();
        float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);

        if (distanceToPlayer > 30.0f)
        {
            m_EnemyLifeBarUI.gameObject.SetActive(false);
        }
        else
        {
            m_EnemyLifeBarUI.gameObject.SetActive(true);
            Vector3 l_ViewportPosition = GameManager.GetGameManager().GetPlayer().m_Camera.WorldToViewportPoint(m_LifeBaUIPosition.position);
            m_EnemyLifeBarUI.SetLifeBarUI(l_ViewportPosition.x, l_ViewportPosition.y, GetLife() / m_MaxLife, l_ViewportPosition.z >= 0.0f);
        }
    }

    float GetLife() { return m_Life; }

    public void AddLife(float LifePoints)
    {
        m_Life = Mathf.Clamp(m_Life + LifePoints, 0.0f, m_MaxLife);
        if (m_Life == 0.0f) SetDieState();
    }

    void UpdateIdleState()
    {
        SetPatrolState();
    }

    void UpdateAlertState()
    {
        // Girar hacia el jugador
        UpdateRotationTowardsPlayer();

        // Comprobar si ve al jugador mientras se gira
       if (!HearsPlayer())
        {
            // Si no oye al jugador, volver a patrullar
            SetPatrolState();
        }
    }

    void UpdateChaseState()
    {
        // Perseguir al jugador
        SetNextChasePosition();

        // Si no ve al jugador y no está en rango, volver a patrullar
        if (!HearsPlayer())
        {
            SetPatrolState();
        }
    }

    void UpdatePatrolState()
    {
        if (!m_NavMeshAgent.hasPath && m_NavMeshAgent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            m_CurrentPatrolPositionId = GetCurrentPatrolPositionId(m_CurrentPatrolPositionId);
            MoveToNextPatrolPosition();
        }

        // Si oye al jugador, cambiar a estado de alerta
        if (HearsPlayer())
        {
            SetAlertState();
        }
    }
    void UpdateAttackState()
    {
        if (Vector3.Distance(transform.position, GetPlayerPosition()) <= m_MinDistanceToAttack)
        {
            if (Time.time >= lastAttackTime + attackCooldown) // Comprobar si ha pasado el tiempo de espera
            {
                shootAudio.PlayOneShot(shootSound);
                AttackPlayer(); // Atacar al jugador
                lastAttackTime = Time.time; // Actualizar el tiempo del último ataque
            }
        }
        else
        {
            SetPatrolState(); // Si no está en rango de ataque, vuelve a patrullar
        }
    }

    void UpdateHitState() { /* Implementar lógica de golpe */ }

    void UpdateDieState() { /* Implementar lógica de muerte */ }

    void MoveToNextPatrolPosition()
    {
        Vector3 l_NextPatrolPosition = m_PatrolPositions[m_CurrentPatrolPositionId].position;
        m_NavMeshAgent.destination = l_NextPatrolPosition;
    }

    bool HearsPlayer()
    {
        Vector3 l_PlayerPosition = GetPlayerPosition();
        Vector3 l_EnemyPosition = transform.position;
        return Vector3.Distance(l_PlayerPosition, l_EnemyPosition) < m_MinDistanceToAlert;
    }

    void UpdateRotationTowardsPlayer()
    {
        Vector3 playerPosition = GetPlayerPosition();
        Vector3 direction = (playerPosition - transform.position).normalized;

        // Rotar más rápido hacia el jugador
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5 * Time.deltaTime);
    }

    void SetNextChasePosition()
    {
        Vector3 l_PlayerPosition = GetPlayerPosition();
        Vector3 l_DirectionToPlayer = (l_PlayerPosition - transform.position).normalized;
        Vector3 l_DesiredPosition = l_PlayerPosition - l_DirectionToPlayer * m_MinDistanceToAttack;
        m_NavMeshAgent.destination = l_DesiredPosition;
    }

    // Método para atacar al jugador
    void AttackPlayer()
    {
        // Aquí puedes implementar la lógica para reducir la vida del jugador
        PlayerController player = GameManager.GetGameManager().GetPlayer();
        player.TakeDamage(attackDamage); // Método que asume que el jugador tiene "TakeDamage"
        Debug.Log("Atacando al jugador y quitando vida");
    }

    public void RestartGame()
    {
        gameObject.SetActive(true);
        m_NavMeshAgent.isStopped = true;
        transform.position = m_StartPosition;
        transform.rotation = m_StartRotation;
        m_Life = m_MaxLife;
        SetIdleState();
    }

    int GetClosestPatrolPositionId()
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < m_PatrolPositions.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, m_PatrolPositions[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    int GetCurrentPatrolPositionId(int currentPatrolPositionId)
    {
        return GetNextCurrentPatrolPosition(currentPatrolPositionId);
    }

    int GetNextCurrentPatrolPosition(int currentPatrolPositionId)
    {
        currentPatrolPositionId++;
        if (currentPatrolPositionId >= m_PatrolPositions.Count)
            currentPatrolPositionId = 0; // Loop back to the start
        return currentPatrolPositionId;
    }

    Vector3 GetPlayerPosition()
    {
        return GameManager.GetGameManager().GetPlayer().transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Verifica si el objeto que entra en el trigger es el jugador
        {
            // Cambiar al estado de ataque
            SetAttackState();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) // Verifica si el objeto que sale del trigger es el jugador
        {
            SetPatrolState(); // Vuelve al estado de patrullaje
        }
    }
}