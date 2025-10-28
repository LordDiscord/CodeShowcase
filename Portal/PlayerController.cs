using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour, IRestartGameElement
{
	bool notComp = false;
	public Camera m_Camera;
	public Transform m_PitchController;
    float m_Yaw;
	float m_Pitch;
	public float m_YawSpeed;
	public float m_PitchSpeed;
	public float m_MinPitch;
	public float m_MaxPitch;
	public float m_Speed;
	CharacterController m_CharacterController;
	float m_VerticalSpeed=0.0f;
	public float m_FastSpeedMultiplier=1.2f;
	public float m_JumpSpeed;

	[Header("Shoot")]
	public float m_MaxShootDistance;
	public LayerMask m_ShootLayerMask;
	public GameObject m_HitParticlesPrefab;
	bool m_LockAngle;

	[Header("Keys")]
	public KeyCode m_LeftKeyCode=KeyCode.A;
	public KeyCode m_RightKeyCode=KeyCode.D;
	public KeyCode m_UpKeyCode=KeyCode.W;
	public KeyCode m_DownKeyCode=KeyCode.S;
	public KeyCode m_RunKeyCode=KeyCode.LeftShift;
	public KeyCode m_JumpKeyCode=KeyCode.Space;
	public KeyCode m_ReloadKeyCode=KeyCode.R;

	public KeyCode m_LockAngleKeyCode=KeyCode.J;

	[Header("Animation")]
	public Animation m_Animation;
	public AnimationClip m_IdleAnimationClip;
	public AnimationClip m_ShootAnimationClip;
    public AnimationClip m_StaticAnimationClip;
    public AnimationClip m_SuckAnimationClip;
    public float m_ShootFadeTime=0.1f;
	public float m_ShootOutFadeTime=0.1f;

	Vector3 m_StartPosition;
	Quaternion m_StartRotation;
	
	[Header("Teleport")]
	public float m_TeleportOffset=0.9f;
	Vector3 m_MovementDirection;
	public float m_MaxAngleToTeleport=45.0f;
	
	[Header("Portals")]
	public Portal m_BluePortal;
	public Portal m_OrangePortal;
	public Portal m_DummyPortal;

	[Header("AttachObjects")]
	public Transform m_AttachTransform;
	bool m_AttachingObject;
	bool m_AttachedObject;
	Rigidbody m_AttachObjectRigidbody;
	public float m_AttachObjectSpeed=8.0f;
	public float m_StartDistanceToRotateAttachObject=2.5f;
	public float m_DetachObjectForce=20.0f;
	Transform m_AttachedObjectPreviousParent;
	public float m_MinDistanceToAttach=1.0f;

	private void Awake()
	{
		m_CharacterController=GetComponent<CharacterController>();
	}
	void Start()
    {
		GameManager l_GameManager=GameManager.GetGameManager();
		if(l_GameManager.GetPlayer()!=null)
		{
			l_GameManager.GetPlayer().InitLevel(this);
			GameObject.Destroy(gameObject);
			return;
		}
		l_GameManager.SetPlayer(this);
		l_GameManager.AddRestartGameElement(this);

		m_StartPosition=transform.position;
		m_StartRotation=transform.rotation;

		//GameObject.DontDestroyOnLoad(gameObject);
		m_Yaw=transform.eulerAngles.y;
		m_Pitch=m_PitchController.eulerAngles.x;
		Cursor.lockState=CursorLockMode.Locked;
		m_LockAngle=false;
		Cursor.visible=false;
		SetIdleAnimation();
    }
	void InitLevel(PlayerController Player)
	{
		m_CharacterController.enabled=false;
		transform.position=Player.transform.position;
		transform.rotation=Player.transform.rotation;
		m_CharacterController.enabled=true;
	}
    void Update()
    {
        float l_HorizontalValue=Input.GetAxis("Mouse X");
		float l_VerticalValue=-Input.GetAxis("Mouse Y");
		if(!m_LockAngle)
		{
			m_Yaw=m_Yaw+l_HorizontalValue*m_YawSpeed*Time.deltaTime;
			m_Pitch=m_Pitch+l_VerticalValue*m_PitchSpeed*Time.deltaTime;
			m_Pitch=Mathf.Clamp(m_Pitch, m_MinPitch, m_MaxPitch);
		}

		transform.rotation=Quaternion.Euler(0.0f, m_Yaw, 0.0f);
		m_PitchController.localRotation=Quaternion.Euler(m_Pitch, 0.0f, 0.0f);

		float l_ForwardAngleRadians=m_Yaw*Mathf.Deg2Rad;
		float l_RightAngleRadians=(m_Yaw+90.0f)*Mathf.Deg2Rad;
		
		Vector3 l_Forward=new Vector3(Mathf.Sin(l_ForwardAngleRadians), 0.0f, Mathf.Cos(l_ForwardAngleRadians));
		Vector3 l_Right=new Vector3(Mathf.Sin(l_RightAngleRadians), 0.0f, Mathf.Cos(l_RightAngleRadians));

		m_MovementDirection=Vector3.zero;
		if(Input.GetKey(m_RightKeyCode))
			m_MovementDirection=l_Right;
		else if(Input.GetKey(m_LeftKeyCode))
			m_MovementDirection=-l_Right;
		
		if(Input.GetKey(m_UpKeyCode))
			m_MovementDirection+=l_Forward;
		else if(Input.GetKey(m_DownKeyCode))
			m_MovementDirection-=l_Forward;

		m_MovementDirection.Normalize();

		if(m_CharacterController.isGrounded && Input.GetKeyDown(m_JumpKeyCode))
			m_VerticalSpeed=m_JumpSpeed;

		m_VerticalSpeed+=Physics.gravity.y*Time.deltaTime;
		
		float l_SpeedMultiplier=1.0f;
		if(Input.GetKey(m_RunKeyCode))
			l_SpeedMultiplier=m_FastSpeedMultiplier;

		Vector3 l_Movement=m_MovementDirection*m_Speed*l_SpeedMultiplier*Time.deltaTime;
		l_Movement.y=m_VerticalSpeed*Time.deltaTime;
		CollisionFlags l_CollisionFlags=m_CharacterController.Move(l_Movement);
		if((l_CollisionFlags & CollisionFlags.Below)!=0)
			m_VerticalSpeed=0.0f;
		else if((l_CollisionFlags & CollisionFlags.Above)!=0 && m_VerticalSpeed>0.0f)
			m_VerticalSpeed=0.0f;

		if(m_AttachedObject || m_AttachingObject)
		{
			if(Input.GetMouseButtonDown(0))
				DetachObject(m_DetachObjectForce);
			else if(Input.GetMouseButtonDown(1))
				DetachObject(0.0f);
		}
		else
		{
			if(Input.GetMouseButtonDown(0))
				Shoot(m_BluePortal);
			if(Input.GetMouseButtonDown(1))
				Shoot(m_OrangePortal);
			if(Input.GetKeyDown(KeyCode.E))
				AttachObject();
		}

		if(Input.GetKeyDown(m_LockAngleKeyCode))
			m_LockAngle=!m_LockAngle;

		if(m_AttachingObject && m_AttachObjectRigidbody!=null)
			UpdateAttachingObject();
    }	
	void Shoot(Portal _Portal)
	{	
		Ray l_Ray=m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
		
		if(Physics.Raycast(l_Ray, out RaycastHit l_RaycastHit, m_MaxShootDistance, m_ShootLayerMask.value))
		{
            if (m_DummyPortal.IsValidPoint(l_RaycastHit.point, l_RaycastHit.normal) && !l_RaycastHit.collider.CompareTag("Portal"))
			{
				_Portal.transform.position=l_RaycastHit.point;
				_Portal.transform.rotation=Quaternion.LookRotation(l_RaycastHit.normal);
				_Portal.gameObject.SetActive(true);
			}
            else
                _Portal.gameObject.SetActive(false);
        }
		m_DummyPortal.gameObject.SetActive(false);
		SetShootAnimation();
	}
	void SetIdleAnimation()
	{
		m_Animation.CrossFade(m_IdleAnimationClip.name);
	}
	void SetShootAnimation()
	{
		m_Animation.CrossFade(m_ShootAnimationClip.name, m_ShootFadeTime);
		m_Animation.CrossFadeQueued(m_IdleAnimationClip.name, m_ShootOutFadeTime);
	}
	void SetStaticAnimation()
	{
		m_Animation.CrossFade(m_StaticAnimationClip.name);
	}
    void SetSuckAnimation()
    {
        m_Animation.CrossFade(m_SuckAnimationClip.name);
    }
    private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("DeadZone") || other.CompareTag("Laser"))
		{
			Debug.Log("auch");
			Kill();
		}
		if (other.CompareTag("VictoryZone"))
		{
			Debug.Log("GG!");
			WinGame();
			
		}
		else if (other.CompareTag("Portal"))
			Teleport(other.GetComponent<Portal>());
		else if (other.CompareTag("CompanionSpawner"))
			other.GetComponent<CompanionSpawner>().Spawn();
	}
	void Teleport(Portal _Portal)
	{
		float l_DotAngle=Vector3.Dot(m_MovementDirection, _Portal.m_OtherPortalTransform.forward);
		if(l_DotAngle>=Mathf.Cos(m_MaxAngleToTeleport*Mathf.Deg2Rad))
		{
			Vector3 l_Position=transform.position+m_MovementDirection*m_TeleportOffset;
			Vector3 l_LocalPosition=_Portal.m_OtherPortalTransform.InverseTransformPoint(l_Position);
			Vector3 l_WorldPosition=_Portal.m_MirrorPortal.transform.TransformPoint(l_LocalPosition);

			Vector3 l_Forward=m_MovementDirection;
			Vector3 l_LocalForward=_Portal.m_OtherPortalTransform.InverseTransformDirection(m_MovementDirection);
			Vector3 l_WorldForward=_Portal.m_MirrorPortal.transform.TransformDirection(l_LocalForward);

			m_CharacterController.enabled=false;
			transform.position=l_WorldPosition;
			transform.forward=l_WorldForward;
			m_Yaw=transform.eulerAngles.y;
			m_CharacterController.enabled=true;
		}
	}
	void Kill()
	{
		GameManager.GetGameManager().RestartGame();
	}
	void WinGame()
	{
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadSceneAsync("EndMenu");
    }
	public void RestartGame()
	{
		m_CharacterController.enabled=false;
		transform.position=m_StartPosition;
		transform.rotation=m_StartRotation;
		m_CharacterController.enabled=true;
	}
	void AttachObject()
	{
		Ray l_Ray=m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
		if (Physics.Raycast(l_Ray, out RaycastHit l_RaycastHit, m_MaxShootDistance, m_ShootLayerMask.value))
		{
			if (l_RaycastHit.collider.CompareTag("CompanionCube"))
                AttachObject(l_RaycastHit.rigidbody);
			else if (l_RaycastHit.collider.CompareTag("RefractionCube") || l_RaycastHit.collider.CompareTag("Turret"))
			{
				notComp = true;
                AttachObject(l_RaycastHit.rigidbody);
            }
            if (l_RaycastHit.collider.CompareTag("Turret"))
            {
                Turret turretScript = l_RaycastHit.collider.GetComponent<Turret>();
				turretScript.SetIsGrabbed(true);
            }
        }
	}
	void AttachObject(Rigidbody AttachObjectRigidbody)
	{
		m_AttachObjectRigidbody=AttachObjectRigidbody;
		m_AttachObjectRigidbody.isKinematic=true;
		m_AttachingObject=true;
		m_AttachedObject=false;
		m_AttachedObjectPreviousParent=m_AttachObjectRigidbody.transform.parent;
		if (notComp == false)
			m_AttachObjectRigidbody.GetComponent<CompanionCube>().SetTeleportable(false);
	}
	void DetachObject(float Force)
	{
        Ray l_Ray = m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        if (Physics.Raycast(l_Ray, out RaycastHit l_RaycastHit, m_MaxShootDistance, m_ShootLayerMask.value))
        {
            if (l_RaycastHit.collider.CompareTag("Turret"))
            {
                Turret turretScript = l_RaycastHit.collider.GetComponent<Turret>();
                turretScript.SetIsGrabbed(false);
            }
        }
        SetShootAnimation();
        m_AttachObjectRigidbody.isKinematic=false;
		m_AttachObjectRigidbody.transform.SetParent(m_AttachedObjectPreviousParent);
		m_AttachObjectRigidbody.velocity=m_AttachTransform.forward*Force;
		m_AttachingObject=false;
		m_AttachedObject=false;
		if(notComp == false)
			m_AttachObjectRigidbody.GetComponent<CompanionCube>().SetTeleportable(true);
	}
	void UpdateAttachingObject()
	{
		if(m_AttachingObject)
		{
            Vector3 l_Direction =m_AttachTransform.position-m_AttachObjectRigidbody.position;
			float l_Distance=l_Direction.magnitude;
			l_Direction/=l_Distance;
			float l_Movement=m_AttachObjectSpeed*Time.deltaTime;
			
			if(l_Movement>=l_Distance || l_Distance<m_MinDistanceToAttach)
			{
                SetStaticAnimation();
                m_AttachedObject = true;
				m_AttachingObject=false;
				m_AttachObjectRigidbody.transform.SetParent(m_AttachTransform);
				m_AttachObjectRigidbody.transform.localPosition=Vector3.zero;
				m_AttachObjectRigidbody.transform.localRotation=Quaternion.identity;
			}
			else
			{
                SetSuckAnimation();
                m_AttachObjectRigidbody.transform.position+=l_Movement*l_Direction;
				float l_Pct=Mathf.Min(1.0f, l_Distance/m_StartDistanceToRotateAttachObject);
				m_AttachObjectRigidbody.transform.rotation=Quaternion.Lerp(m_AttachTransform.rotation,
					m_AttachObjectRigidbody.transform.rotation, l_Pct);
			}
		}
	}
}
