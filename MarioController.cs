using System.Collections;
using UnityEditor.Animations;
using UnityEngine;

public class MarioController : MonoBehaviour, IRestartGameElement, IPlayerManager
{
    public enum TPunchType
    {
        RIGHT_HAND = 0,
        LEFT_HAND,
        KICK
    }
    CharacterController m_CharacterController;
    Animator m_Animator;
    public Camera m_Camera;
    private HUD m_HUD;
    public float m_WalkSpeed = 2.0f;
    public float m_RunSpeed = 8.0f;
    public float m_LerpRotationPct = 0.8f;
    float m_VerticalSpeed = 0.0f;

    Vector3 m_StartPosition;
    Quaternion m_StartRotation;
    Checkpoint m_CurrentCheckpoint;

    public float m_BridgeForce = 10.0f;

    [Header("Lives")]
    public int m_MaxTries = 3;
    public int m_CurrentTries;
    public int m_MaxLives = 8; 
    public int m_CurrentLives;
    public event System.Action<IPlayerManager> triesChangedDelegate;

    [Header("Wall Jump")]
    public float WallJumpHorizontalForce = 6.0f; 
    public float WallJumpVerticalForce = 8.0f; 

    [Header("Long Jump")]
    public float LongJumpSpeed = 10.0f; 
    public float LongJumpVerticalSpeed = 8.0f; 

    [Header("Jump")]
    private int m_JumpCount = 0; 
    public int MaxJumpCount = 3;
    public float DoubleJumpVerticalSpeed = 6.0f;
    public float TripleJumpVerticalSpeed = 7.5f;
    public float m_JumpVerticalSpeed = 5.0f;
    public float m_KillJumpVerticalSpeed = 8.0f;
    public float m_WaitStartJumpTime = 0.12f;
    public float m_MaxAngleNeededToKillGoomba = 15.0f;
    public float m_MinVerticalSpeedToKillGoomba = -1.0f;

    [Header("Punch")]
    public float m_PunchComboAvailableTime = 0.6f;
    int m_CurrentPunchId = 0;
    float m_LastPunchTime;

    [Header("Punch Colliders")]
    public GameObject m_LeftHandPunchHitCollider;
    public GameObject m_RightHandPunchHitCollider;
    public GameObject m_RightFootKickHitCollider;

    [Header("Input")]
    public KeyCode m_LeftKeycode = KeyCode.A;
    public KeyCode m_RightKeycode = KeyCode.D;
    public KeyCode m_UpKeycode = KeyCode.W;
    public KeyCode m_DownKeycode = KeyCode.S;
    public KeyCode m_RunKeycode = KeyCode.LeftShift;
    public KeyCode m_JumpKeycode = KeyCode.Space;
    public KeyCode m_PunchHitButton = KeyCode.Mouse0;//revisar

    [Header("Elevator")]
    public float m_MaxAngleToAttachElevator = 8.0f;
    Collider m_CurrentElevator = null;

    private void Awake()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_Animator = GetComponent<Animator>();
    }
    void Start()
    {
        m_HUD = FindObjectOfType<HUD>();
        if (m_HUD == null)
        {
            Debug.LogError("No se encontró ningún HUD en la escena.");
        }
        m_CurrentTries = m_MaxTries;
        m_CurrentLives = m_MaxLives;
        LockCursor();
        m_LeftHandPunchHitCollider.gameObject.SetActive(false);
        m_RightHandPunchHitCollider.gameObject.SetActive(false);
        m_RightFootKickHitCollider.gameObject.SetActive(false);
        GameManager.GetGameManager().AddRestartGameElement(this);
        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;
    }
    void Update()
    {
        Vector3 l_Forward = m_Camera.transform.forward;
        Vector3 l_Right = m_Camera.transform.right;
        l_Forward.y = 0.0f;
        l_Right.y = 0.0f;
        l_Forward.Normalize();
        l_Right.Normalize();
        bool l_HasMovement = false;

        Vector3 l_Movement = Vector3.zero;
        if (Input.GetKey(m_RightKeycode))
        {
            l_Movement = l_Right;
            l_HasMovement = true;
        }
        else if (Input.GetKey(m_LeftKeycode))
        {
            l_Movement = -l_Right;
            l_HasMovement = true;
        }

        if (Input.GetKey(m_UpKeycode))
        {
            l_Movement += l_Forward;
            l_HasMovement = true;
        }
        else if (Input.GetKey(m_DownKeycode))
        {
            l_Movement -= l_Forward;
            l_HasMovement = true;
        }
        l_Movement.Normalize();
        float l_Speed = 0.0f;
        if (l_HasMovement)
        {
            if (Input.GetKey(m_RunKeycode))
            {
                l_Speed = m_RunSpeed;
                m_Animator.SetFloat("Speed", 1.0f);
            }
            else
            {
                l_Speed = m_WalkSpeed;
                m_Animator.SetFloat("Speed", 0.2f);
            }
            Quaternion l_DesiredRotation = Quaternion.LookRotation(l_Movement);
            transform.rotation = Quaternion.Lerp(transform.rotation, l_DesiredRotation,
                m_LerpRotationPct * Time.deltaTime);
        }
        else
            m_Animator.SetFloat("Speed", 0.0f);
        if (!m_CharacterController.isGrounded && Input.GetKeyDown(m_JumpKeycode) && CanWallJump())
        {
            PerformWallJump();
        }
        else if (Input.GetKey(m_RunKeycode) && Input.GetKeyDown(m_JumpKeycode) && CanLongJump())
        {
            PerformLongJump();
        }
        else if(Input.GetKeyDown(m_JumpKeycode) && CanJump())
                PerformJump();

        l_Movement = l_Movement * l_Speed * Time.deltaTime;

        m_VerticalSpeed += Physics.gravity.y * Time.deltaTime;

        l_Movement.y = m_VerticalSpeed * Time.deltaTime;

        CollisionFlags l_CollisionFlags = m_CharacterController.Move(l_Movement);
        if ((l_CollisionFlags & CollisionFlags.Below) != 0 && m_VerticalSpeed < 0.0f)
        {
            m_Animator.SetBool("Falling", false);
            m_JumpCount = 0; 
        }
        else
            m_Animator.SetBool("Falling", true);

        if (((l_CollisionFlags & CollisionFlags.Below) != 0 && m_VerticalSpeed < 0.0f) ||
            (l_CollisionFlags & CollisionFlags.Above) != 0 && m_VerticalSpeed > 0.0f)
            m_VerticalSpeed = 0.0f;

        if (Time.time - m_LastPunchTime > m_PunchComboAvailableTime && m_CurrentPunchId != 0)
        {
            ResetPunchCombo();
        }
        UpdatePunch();

        UpdateElevator();
    }
    public void PerformJump()
    {
        m_JumpCount++;

        if (m_JumpCount == 1)
        {
            m_Animator.SetTrigger("Jump");
            m_VerticalSpeed = m_JumpVerticalSpeed;
        }
        else if (m_JumpCount == 2)
        {
            m_Animator.SetTrigger("DoubleJump");
            m_VerticalSpeed = DoubleJumpVerticalSpeed;
        }
        else if (m_JumpCount == 3)
        {
            m_Animator.SetTrigger("TripleJump");
            m_VerticalSpeed = TripleJumpVerticalSpeed;
        }
        if (m_JumpCount >= MaxJumpCount)
        {
            m_JumpCount = MaxJumpCount;
        }
    }
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void LateUpdate()
    {
        Vector3 l_Angles = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0.0f, l_Angles.y, 0.0f);
    }
    bool CanWallJump()
    {
        return (m_CharacterController.collisionFlags & CollisionFlags.Sides) != 0;
    }
    bool CanLongJump()
    {
        return (m_CharacterController.isGrounded && m_Animator.GetFloat("Speed") > 0.8f);
    }
    bool CanJump()
    {
        return m_JumpCount < MaxJumpCount;
    }
    void PerformWallJump()
    {
        m_VerticalSpeed = WallJumpVerticalForce;
        Vector3 wallNormal = GetWallNormal();
        Vector3 jumpDirection = wallNormal * WallJumpHorizontalForce;
        m_CharacterController.Move(jumpDirection * Time.deltaTime);
    }
    Vector3 GetWallNormal()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1.0f))
        {
            return -hit.normal; 
        }
        return -transform.forward;
    }
    void PerformLongJump()
    {
        m_JumpCount++;
        m_Animator.SetTrigger("LongJump");
        m_VerticalSpeed = LongJumpVerticalSpeed;

        Vector3 forward = transform.forward * LongJumpSpeed;
        m_CharacterController.Move(forward * Time.deltaTime);
    }
    void Jump()
    {
        m_Animator.SetTrigger("Jump");
        StartCoroutine(ExecuteJump());
    }
    IEnumerator ExecuteJump()
    {
        yield return new WaitForSeconds(m_WaitStartJumpTime);
        m_VerticalSpeed = m_JumpVerticalSpeed;
        m_Animator.SetBool("Falling", false);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
       
    }
    public bool IsFalling()
    {
        return m_VerticalSpeed < 0.0f;
    }
    public void SetJump()
    {
        m_JumpCount = 0;
    }

    private void JumpOverEnemy()
    {
        m_VerticalSpeed = m_KillJumpVerticalSpeed; 
        m_Animator.SetTrigger("Jump"); 
    }
    bool IsUpperHit(Transform GoombaTransform)
    {
        Vector3 l_GoombaDirection = transform.position - GoombaTransform.position;
        l_GoombaDirection.Normalize();
        float l_DotAngle = Vector3.Dot(l_GoombaDirection, Vector3.up);
        if (l_DotAngle >= Mathf.Cos(m_MaxAngleNeededToKillGoomba * Mathf.Deg2Rad) &&
            m_VerticalSpeed <= m_MinVerticalSpeedToKillGoomba)
            return true;
        return false;
    }
    void UpdatePunch()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m_Animator.SetBool("PunchCheck", true);
            if (CanPunch())
                PunchCombo();
        }
    }
    bool CanPunch()
    {
        return true;
    }
    void ResetPunchCombo()
    {
        m_Animator.SetBool("PunchCheck", false);
        m_CurrentPunchId = 0;
        m_Animator.SetInteger("PunchCombo", 0);
    }
    void PunchCombo()
    {
        m_Animator.SetTrigger("Punch");
        float l_DiffTime = Time.time - m_LastPunchTime;
        if (l_DiffTime <= m_PunchComboAvailableTime)
        {
            m_CurrentPunchId = m_CurrentPunchId + 1;
            if (m_CurrentPunchId >= 3)
                m_CurrentPunchId = 0;
        }
        else
            m_CurrentPunchId = 0;
        m_LastPunchTime = Time.time;
        m_Animator.SetInteger("PunchCombo", m_CurrentPunchId);
    }
    public void EnableHitCollider(TPunchType PunchType, bool Active)
    {
        switch (PunchType)
        {
            case TPunchType.LEFT_HAND:
                m_LeftHandPunchHitCollider.SetActive(Active);
                break;
            case TPunchType.RIGHT_HAND:
                m_RightHandPunchHitCollider.SetActive(Active);
                break;
            case TPunchType.KICK:
                m_RightFootKickHitCollider.SetActive(Active);
                break;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
            m_CurrentCheckpoint = other.GetComponent<Checkpoint>();
        else if (other.CompareTag("Elevator"))
        {
            if (CanAttachElevator(other))
                AttachElevator(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Elevator") && other == m_CurrentElevator)
            DetachElevator();
    }
    bool CanAttachElevator(Collider Elevator)
    {
        if (m_CurrentElevator != null)
            return false;
        return IsAttachableElevator(Elevator);
    }
    bool IsAttachableElevator(Collider Elevator)
    {
        float l_DotAngle = Vector3.Dot(Elevator.transform.forward, Vector3.up);
        if (l_DotAngle >= Mathf.Cos(m_MaxAngleToAttachElevator * Mathf.Deg2Rad))
            return true;
        return false;
    }
    void AttachElevator(Collider Elevator)
    {
        transform.SetParent(Elevator.transform.parent);
        m_CurrentElevator = Elevator;
        Debug.Log("attach " + Elevator.name);
    }
    void DetachElevator()
    {
        Debug.Log("Dettach " + m_CurrentElevator.name);
        m_CurrentElevator = null;
        transform.SetParent(null);
    }
    void UpdateElevator()
    {
        if (m_CurrentElevator == null)
            return;
        if (!IsAttachableElevator(m_CurrentElevator))
            DetachElevator();
    }
    public void TakeDamage(int damage)
    {
        m_CurrentLives -= damage;
        m_Animator.SetTrigger("Damaged");
        if (m_CurrentLives <= 0)
        {
            m_CurrentLives = 0;
            LoseTry();
            Kill(); 
        }
        else
        {
            Debug.Log("Mario took damage! Remaining lives: " + m_CurrentLives);
            
        }
    }

    public void TakeHP(int i)
    {
        m_CurrentLives += i;
        Mathf.Clamp(m_CurrentLives, 0, 8);
        Debug.Log(m_CurrentLives);
    }
    public void Kill()
    {
        m_Animator.SetTrigger("Dead");
        Debug.Log("Mario ha muerto.");
        FindObjectOfType<GameOverManager>().ShowGameOver();
    }
    public void RestartGame()
    {
        m_CurrentLives = m_MaxLives;
        m_CharacterController.enabled = false;
        if (m_CurrentCheckpoint == null)
        {
            transform.position = m_StartPosition;
            transform.rotation = m_StartRotation;
        }
        else
        {
            transform.position = m_CurrentCheckpoint.m_RespawnPosition.position;
            transform.rotation = m_CurrentCheckpoint.m_RespawnPosition.rotation;
        }
        m_CharacterController.enabled = true;
    }
    public void Step(AnimationEvent _AnimationEvent)
    {
        AudioClip l_AudioClip = (AudioClip)_AnimationEvent.objectReferenceParameter;
        Debug.Log("mario step " + _AnimationEvent.intParameter + " audio " + l_AudioClip);
    }
    public int GetCurrentTries()
    {
        return m_CurrentTries;
    }
    public int GetCurrentLives()
    {
        return m_CurrentLives;
    }

    public void LoseTry()
    {
        m_CurrentTries = Mathf.Max(0, m_CurrentTries - 1);
        Debug.Log($"Mario perdió un intento. Intentos restantes: {m_CurrentTries}");
    }
}
