using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform m_FollowObject;
    float m_Yaw = 0.0f;
    float m_Pitch = 0.0f;
    public float m_MinPitch = -60.0f;
    public float m_MaxPitch = 60.0f;
    public float m_MinCameraDistance = 5.0f;
    public float m_MaxCameraDistance = 15.0f;
    public float m_YawSpeed = 0.0f;
    public float m_PitchSpeed = 0.0f;
    public LayerMask m_LayerMask;
    public float m_OffsetHit = 0.1f;

    void LateUpdate()
    {
        float l_HorizontalAxis = Input.GetAxis("Mouse X");
        float l_VerticalAxis = Input.GetAxis("Mouse Y");

        Vector3 l_LookDirection = m_FollowObject.position - transform.position;
        float l_DistanceToPlayer = l_LookDirection.magnitude;
        l_LookDirection.y = 0.0f;
        l_LookDirection.Normalize();

        m_Yaw = Mathf.Atan2(l_LookDirection.x, l_LookDirection.z) * Mathf.Rad2Deg;

        m_Yaw += l_HorizontalAxis * m_YawSpeed * Time.deltaTime;
        m_Pitch += l_VerticalAxis * m_PitchSpeed * Time.deltaTime;

        m_Pitch = Mathf.Clamp(m_Pitch, m_MinPitch, m_MaxPitch);
        float l_YawInRadians = m_Yaw * Mathf.Deg2Rad;
        float l_PitchInRadians = m_Pitch * Mathf.Deg2Rad;

        Vector3 l_CameraForward = new Vector3(Mathf.Sin(l_YawInRadians) * Mathf.Cos(l_PitchInRadians),
            Mathf.Sin(l_PitchInRadians),
            Mathf.Cos(l_YawInRadians) * Mathf.Cos(l_PitchInRadians));
        l_DistanceToPlayer = Mathf.Clamp(l_DistanceToPlayer, m_MinCameraDistance, m_MaxCameraDistance);
        Vector3 l_DesiredPosition = m_FollowObject.position - l_CameraForward * l_DistanceToPlayer;

        Ray l_Ray = new Ray(m_FollowObject.position, -l_CameraForward);
        if (Physics.Raycast(l_Ray, out RaycastHit l_RaycastHit, l_DistanceToPlayer, m_LayerMask.value))
        {
            l_DesiredPosition = l_RaycastHit.point + l_CameraForward * m_OffsetHit;
        }

        transform.position = l_DesiredPosition;
        transform.LookAt(m_FollowObject.position);
    }
}
