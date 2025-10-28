using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour, IRestartGameElement
{
    public Camera m_Camera;
    //l_ variable local del metodo
    //m_ variable de miembro de la clase
    public Transform m_pitchController;
    float m_Yaw;
    float m_Pitch;
    public float m_YawSpeed;
    public float m_PitchSpeed;
    public float m_MinPitch;
    public float m_MaxPitch;
    public float m_Speed;
    CharacterController m_CharacterController;
    float m_VerticalSpeed = 0.0f;
    public float m_FastSpeedMultiplayer = 1.2f;
    public float m_JumpSpeed;

    [Header("Shoot")]
    public float m_MaxShootDistance;
    public LayerMask m_ShootLayerMask;
    public GameObject m_HitParticlesPrefab;
    bool m_LockAngle;
    CPoolElement m_PoolElement;
    [Header("Audio")]
    public AudioSource shootAudio;//audioSource
    public AudioClip shootSound;//clip de audio
    public AudioClip reloadSound;//clip de audio

    [Header("Keys")]
    public KeyCode m_LeftKeyCode = KeyCode.A;
    public KeyCode m_RightKeyCode = KeyCode.D;
    public KeyCode m_UpKeyCode = KeyCode.W;
    public KeyCode m_DownKeyCode = KeyCode.S;
    public KeyCode m_RunKeyCode = KeyCode.LeftShift;
    public KeyCode m_JumpKeyCode = KeyCode.Space;
    public KeyCode m_ReloadKeyCode = KeyCode.R;

    //teclas de debug para editar in game
    public KeyCode m_LockAngleKeyCode = KeyCode.J;

    [Header("Animation")]
    public Animation m_Animation;
    public AnimationClip m_IdleAnimationClip;
    public AnimationClip m_ShootAnimationClip;
    public AnimationClip m_ReloadAnimationClip;
    public float m_ShootFadeTime = 0.1f;
    public float m_ShootOutFadeTime = 0.1f;
    Vector3 m_StartPosition;
    Quaternion m_StartRotation;

    [Header("Bullets")]
    public int maxAmmoInMag = 25;  // Balas máximas por cargador
    public int maxTotalAmmo = 100; // Balas totales máximas
    public TMP_Text ammoInMagText;      // Para mostrar el número de balas en el cargador
    public TMP_Text totalAmmoText;
    public TMP_Text lifeText;// Para mostrar el número total de balas
    private int currentAmmoInMag;
    public int currentTotalAmmo;
    private float reloadAnimationTime;
    private bool isReloading = false;

    [Header("Shooting Range")]
    public int currentPoints;
    public GameObject scoreTextPrefab; // Prefab del texto 3D
    private GameObject currentScoreText; // Referencia al texto en el juego

    [Header("Stats")]
    public float vidaMaxima = 100;
    public float vidaActual;

    private void Awake()
    {
        m_CharacterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        GameManager l_GameManager = GameManager.GetGameManager();
        if (l_GameManager.GetPlayer() != null)
        {
            l_GameManager.GetPlayer().InitLevel(this);
            GameObject.Destroy(gameObject);
            return;
        }
        l_GameManager.SetPlayer(this);
        l_GameManager.AddRestartGameElement(this);

        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;

        GameObject.DontDestroyOnLoad(gameObject);
        m_PoolElement = new CPoolElement(25, m_HitParticlesPrefab);
        m_Yaw = transform.eulerAngles.y;
        m_Pitch = m_pitchController.eulerAngles.x;
        m_LockAngle = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetIdleAnimation();

        currentPoints = 0;
        currentAmmoInMag = maxAmmoInMag;
        currentTotalAmmo = maxTotalAmmo;
        UpdateAmmoUI();
        reloadAnimationTime = m_ReloadAnimationClip.length;
        Vector3 rotation = new Vector3(0, 190, 0);
        currentScoreText = Instantiate(scoreTextPrefab, new Vector3(-22.15f, 6.97f, -100.93f), Quaternion.Euler(rotation));
        UpdateScoreText(); // Actualiza el texto inicial
        vidaActual = vidaMaxima;
    }
    void InitLevel(PlayerController Player)
    {
        m_CharacterController.enabled = false;
        transform.position = Player.transform.position;
        transform.rotation = Player.transform.rotation;
        m_CharacterController.enabled = true;
    }

    void Update()
    {
        float l_HorizontalValue = Input.GetAxis("Mouse X");
        float l_VerticalValue = -Input.GetAxis("Mouse Y");

        if(vidaActual <= 0)
        {
            Kill();
        }

        if (!m_LockAngle)
        {
            m_Yaw = m_Yaw + l_HorizontalValue * m_YawSpeed * Time.deltaTime;
            m_Pitch = m_Pitch + l_VerticalValue * m_PitchSpeed * Time.deltaTime;
            m_Pitch = Mathf.Clamp(m_Pitch, m_MinPitch, m_MaxPitch);
        }

        transform.rotation = Quaternion.Euler(0.0f, m_Yaw, 0.0f);
        m_pitchController.localRotation = Quaternion.Euler(m_Pitch, 0.0f, 0.0f);

        float lForwardAngleRadians = m_Yaw * Mathf.Deg2Rad;
        float lRightAngleRadians = (m_Yaw + 90.0f) * Mathf.Deg2Rad;

        Vector3 l_Forward = new Vector3(Mathf.Sin(lForwardAngleRadians), 0.0f, Mathf.Cos(lForwardAngleRadians));
        Vector3 l_Right = new Vector3(Mathf.Sin(lRightAngleRadians), 0.0f, Mathf.Cos(lRightAngleRadians));

        Vector3 l_Movement = Vector3.zero;

        if (Input.GetKey(m_RightKeyCode))
        {
            l_Movement = l_Right;
        }
        else if (Input.GetKey(m_LeftKeyCode))
        {
            l_Movement = -l_Right;
        }
        if (Input.GetKey(m_UpKeyCode))
        {
            l_Movement += l_Forward;
        }
        else if (Input.GetKey(m_DownKeyCode))
        {
            l_Movement -= l_Forward;
        }

        l_Movement.Normalize();

        if (m_CharacterController.isGrounded && Input.GetKeyDown(m_JumpKeyCode))
        {
            m_VerticalSpeed = m_JumpSpeed;
        }

        m_VerticalSpeed += Physics.gravity.y * Time.deltaTime;
        float l_SpeedMultiplier = 1.0f;

        if (Input.GetKey(m_RunKeyCode))
        {
            l_SpeedMultiplier = m_FastSpeedMultiplayer;
        }

        l_Movement *= m_Speed * l_SpeedMultiplier * Time.deltaTime;
        l_Movement.y = m_VerticalSpeed * Time.deltaTime;

        CollisionFlags l_CollisionFlags = m_CharacterController.Move(l_Movement);
        if ((l_CollisionFlags & CollisionFlags.Below) != 0)
        {
            m_VerticalSpeed = 0.0f;
        }
        else if ((l_CollisionFlags & CollisionFlags.Below) != 0 && m_VerticalSpeed > 0.0f)
        {
            m_VerticalSpeed = 0.0f;
        }
        if (CanShoot() && !isReloading)
        {
            Shoot();
        }
        if (Input.GetKeyDown(m_LockAngleKeyCode))
        {
            m_LockAngle = !m_LockAngle;
        }
        if (CanReload())
            Reload();

        UpdateAmmoUI();
        UpdateScoreText();
    }

    bool CanReload()
    {
        return Input.GetKeyDown(m_ReloadKeyCode) && currentAmmoInMag < maxAmmoInMag && currentTotalAmmo > 0;
    }
    void Reload()
    {
        SetReloadAnimation();
        shootAudio.PlayOneShot(reloadSound);
        StartCoroutine(ReloadCoroutine());
    }
    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadAnimationTime); // Esperar a que termine la animación de recarga

        int ammoNeeded = maxAmmoInMag - currentAmmoInMag;
        int ammoToReload = Mathf.Min(ammoNeeded, currentTotalAmmo);

        // Rellenar el cargador y restar del total de balas
        currentAmmoInMag += ammoToReload;
        currentTotalAmmo -= ammoToReload;

        // Actualizar UI después de recargar
        UpdateAmmoUI();
        isReloading = false;
    }

    bool CanShoot()
    {
        return Input.GetMouseButtonDown(0) && currentAmmoInMag > 0;
    }
    void Shoot()
    {
        if (currentAmmoInMag > 0)
        {
            Ray l_Ray = m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));

            if (Physics.Raycast(l_Ray, out RaycastHit l_RaycastHit, m_MaxShootDistance, m_ShootLayerMask.value))
            {
                if (l_RaycastHit.collider.CompareTag("ShootingRange"))
                {
                    currentAmmoInMag = maxAmmoInMag;
                    currentTotalAmmo = maxTotalAmmo;
                    currentPoints = 0;
                    ShootingRange shootingRange = l_RaycastHit.collider.GetComponent<ShootingRange>();
                    if (shootingRange != null)
                    {
                        shootingRange.StartShootingRange();
                    }
                }
                if (l_RaycastHit.collider.CompareTag("Diana"))
                {
                    Target diana = l_RaycastHit.collider.GetComponent<Target>();
                    Destroy(diana.gameObject);
                    currentPoints++;
                }
                if (l_RaycastHit.collider.CompareTag("HitCollider"))
                    l_RaycastHit.collider.GetComponent<HitCollider>().Hit();
                else
                    CreateHitParticles(l_RaycastHit.point, l_RaycastHit.normal);
            }
            shootAudio.PlayOneShot(shootSound);
            SetShootAnimation();

            // Restar una bala del cargador
            currentAmmoInMag--;
            UpdateAmmoUI();
        }
        else
        {
            Debug.Log("No ammo! Reload!");
        }
    }

    void CreateHitParticles(Vector3 Position, Vector3 Normal)
    {
        GameObject l_HitParticles = m_PoolElement.GetNextElement();
        l_HitParticles.transform.position = Position;
        l_HitParticles.transform.rotation = Quaternion.LookRotation(Normal);
        l_HitParticles.SetActive(true);
    }

    //animaciones
    void SetIdleAnimation()
    {
        m_Animation.CrossFade(m_IdleAnimationClip.name);
    }
    void SetShootAnimation()
    {
        m_Animation.CrossFade(m_ShootAnimationClip.name, m_ShootFadeTime);
        m_Animation.CrossFadeQueued(m_IdleAnimationClip.name, m_ShootOutFadeTime);
    }
    void SetReloadAnimation()
    {
        m_Animation.CrossFade(m_ReloadAnimationClip.name);
        m_Animation.CrossFadeQueued(m_IdleAnimationClip.name);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            Item l_Item = other.GetComponent<Item>();
            if (l_Item.CanPick())
            {
                l_Item.Pick();
            }
        }
        else if (other.CompareTag("DeadZone"))
            Kill();
        else if (other.CompareTag("Puerta"))
        {
            Puerta puerta = other.GetComponent<Puerta>();
            if (puerta != null)
            {
                puerta.ElevarPuerta(); // Eleva la puerta
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Puerta"))
        {
            Puerta puerta = other.GetComponent<Puerta>();
            if (puerta != null)
            {
                puerta.BajarPuerta(); // Baja la puerta
            }
        }
    }
    void Kill()
    {
        GameManager.GetGameManager().RestartGame();
    }
    public void RestartGame()
    {
        m_CharacterController.enabled = false;
        transform.position = m_StartPosition;
        transform.rotation = m_StartRotation;
        m_CharacterController.enabled = true;

        currentAmmoInMag = maxAmmoInMag;
        currentTotalAmmo = maxTotalAmmo;
    }
    void UpdateAmmoUI()
    {
        if (ammoInMagText != null)
        {
            ammoInMagText.text = $"Cargador: {currentAmmoInMag}";  // Actualiza el texto de balas en el cargador
        }
        if (totalAmmoText != null)
        {
            totalAmmoText.text = $"Total: {currentTotalAmmo}";      // Actualiza el texto de balas totales
        }
        if (lifeText != null)
        {
            lifeText.text = $"HP: {vidaActual}";      // Actualiza el texto de balas totales
        }
    }
    private void UpdateScoreText()
    {
        TMP_Text textMeshPro = currentScoreText.GetComponent<TMP_Text>();
        if (textMeshPro != null)
        {
            textMeshPro.text = "Puntos: " + currentPoints.ToString(); // Actualiza el texto
        }
    }
    public void TakeDamage(float damage)
    {
        vidaActual = vidaActual - damage;
    }
}

