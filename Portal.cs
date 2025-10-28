using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
	public Transform m_OtherPortalTransform;
	public Camera m_Camera;
	public Portal m_MirrorPortal;
	public float m_OffsetCamera=0.6f;
	public float m_ValidOffset=0.03f;
	public LayerMask m_ValidLayerMask;
	public float m_MaxValidAngle=5.0f;
	public List<Transform> m_ValidPoints;

	private void Update()
	{
		Camera l_CameraPlayerController=GameManager.GetGameManager().GetPlayer().m_Camera;
		Vector3 l_Position=l_CameraPlayerController.transform.position;
		Vector3 l_Forward=l_CameraPlayerController.transform.forward;
		Vector3 l_LocalPosition=m_OtherPortalTransform.InverseTransformPoint(l_Position);
		Vector3 l_LocalForward=m_OtherPortalTransform.InverseTransformDirection(l_Forward);

		Vector3 l_WorldPosition=m_MirrorPortal.transform.TransformPoint(l_LocalPosition);
		Vector3 l_WorldForward=m_MirrorPortal.transform.TransformDirection(l_LocalForward);
		m_MirrorPortal.m_Camera.transform.position=l_WorldPosition;
		m_MirrorPortal.m_Camera.transform.forward=l_WorldForward;

		float l_DistanceToPortal=Vector3.Distance(l_WorldPosition, m_MirrorPortal.transform.position);
		float l_DistanceNearClipPlane=m_OffsetCamera+l_DistanceToPortal;
		m_MirrorPortal.m_Camera.nearClipPlane=l_DistanceNearClipPlane;
	}
	public bool IsValidPoint(Vector3 Position, Vector3 Normal)
	{
		transform.position=Position;
		transform.rotation=Quaternion.LookRotation(Normal);

		bool l_IsValid=true;
		Vector3 l_PlayerCameraPosition=GameManager.GetGameManager().GetPlayer().m_Camera.transform.position;
		for(int i=0; i<m_ValidPoints.Count; ++i)
		{
			Color l_Color=Color.green;
			Vector3 l_Direction=m_ValidPoints[i].position-l_PlayerCameraPosition;
			float l_Distance=l_Direction.magnitude;
			l_Direction/=l_Distance;
			Ray l_Ray=new Ray(l_PlayerCameraPosition, l_Direction);
			if(Physics.Raycast(l_Ray, out RaycastHit l_RaycastHit, l_Distance+m_ValidOffset, m_ValidLayerMask.value))
			{
				if(l_RaycastHit.collider.CompareTag("Drawable"))
				{
					float l_DistanceToHitPoint=(l_RaycastHit.point-m_ValidPoints[i].position).magnitude;
					if(l_DistanceToHitPoint<m_ValidOffset)
					{
						float l_DotAngle=Vector3.Dot(l_RaycastHit.normal, Normal);
						if(l_DotAngle<Mathf.Cos(m_MaxValidAngle*Mathf.Deg2Rad))
						{
							l_IsValid=false;
							l_Color=Color.yellow;
						}
					}
					else
					{
						l_IsValid=false;
						l_Color=Color.cyan;
					}
				}
				else
				{
					l_IsValid=false;
					l_Color=Color.magenta;
				}
			}
			else
			{
				l_IsValid=false;
				l_Color=Color.red;
			}

			Debug.DrawLine(l_PlayerCameraPosition, m_ValidPoints[i].position, l_Color, 3.0f);
		}

		return l_IsValid;

	}
}
