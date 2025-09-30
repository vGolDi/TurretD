using UnityEngine;
using Cinemachine;
using Photon.Pun;

public class CameraController : MonoBehaviour
{
    [Header("Ustawienia Edge Scrolling")]
    [Tooltip("Szerokoœæ paska przy krawêdzi ekranu (w pikselach), który aktywuje obrót.")]
    [SerializeField] private float edgeScrollSize = 40f;
    [Tooltip("Prêdkoœæ obrotu kamery przy krawêdzi ekranu.")]
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
        // Wy³¹cz tê kamerê, jeœli nie nale¿y do lokalnego gracza
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

        // SprawdŸ pozycjê myszy
        if (Input.mousePosition.x < edgeScrollSize)
        {
            horizontalInput = -1f; // Obrót w lewo
        }
        else if (Input.mousePosition.x > Screen.width - edgeScrollSize)
        {
            horizontalInput = 1f; // Obrót w prawo
        }

        // Obracamy "orbit¹" kamery Cinemachine
        var orbit = virtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        if (orbit != null)
        {
            orbit.m_XAxis.Value += horizontalInput * edgeScrollSpeed * Time.deltaTime;
        }
    }
}