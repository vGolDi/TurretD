using UnityEngine;
using Cinemachine;
using Photon.Pun;

public class CameraController : MonoBehaviour
{
    [Header("Ustawienia Edge Scrolling")]
    [Tooltip("Szeroko�� paska przy kraw�dzi ekranu (w pikselach), kt�ry aktywuje obr�t.")]
    [SerializeField] private float edgeScrollSize = 40f;
    [Tooltip("Pr�dko�� obrotu kamery przy kraw�dzi ekranu.")]
    [SerializeField] private float edgeScrollSpeed = 20f;

    private PlayerInputManager inputManager;
    private CinemachineVirtualCamera virtualCamera;
    private PhotonView photonView;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        inputManager = GetComponentInParent<PlayerInputManager>();
        photonView = GetComponentInParent<PhotonView>();
    }

    private void Start()
    {
        // Wy��cz t� kamer�, je�li nie nale�y do lokalnego gracza
        if (!photonView.IsMine)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine || inputManager == null) return;

        if (inputManager.IsInBuildMode)
        {
            HandleEdgeScrolling();
        }
    }

    private void HandleEdgeScrolling()
    {
        float horizontalInput = 0f;

        // Sprawd� pozycj� myszy
        if (Input.mousePosition.x < edgeScrollSize)
        {
            horizontalInput = -1f; // Obr�t w lewo
        }
        else if (Input.mousePosition.x > Screen.width - edgeScrollSize)
        {
            horizontalInput = 1f; // Obr�t w prawo
        }

        // Obracamy "orbit�" kamery Cinemachine
        var orbit = virtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        if (orbit != null)
        {
            orbit.m_XAxis.Value += horizontalInput * edgeScrollSpeed * Time.deltaTime;
        }
    }
}