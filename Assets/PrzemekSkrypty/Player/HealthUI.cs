using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;
using ElementumDefense.Enemies;
using ElementumDefense.Players;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HealthUI : MonoBehaviour
    {
        private VisualElement root;

        private Label myHealthName;
        private Label myHealthText;
        private VisualElement myHealthFill;
        private Label enemyHealthName;
        private Label enemyHealthText;
        private VisualElement enemyHealthFill;
        private VisualElement enemyHealthPanel;

        private PlayerHealth myHealth;
        private PlayerHealth enemyHealth;

        private bool isInitialized;
        private float retryTimer;
        private const float RETRY_INTERVAL = 1f;

        private void Start()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            root = uiDoc.rootVisualElement;
            QueryElements();
        }

        private void Update()
        {
            if (!isInitialized)
            {
                retryTimer += Time.deltaTime;
                if (retryTimer >= RETRY_INTERVAL)
                {
                    retryTimer = 0f;
                    TryFindPlayers();
                }
            }
        }

        private void QueryElements()
        {
            myHealthName =
                root.Q<Label>("my-health-name");
            myHealthText =
                root.Q<Label>("my-health-text");
            myHealthFill =
                root.Q<VisualElement>(
                    "my-health-fill");

            enemyHealthName =
                root.Q<Label>("enemy-health-name");
            enemyHealthText =
                root.Q<Label>("enemy-health-text");
            enemyHealthFill =
                root.Q<VisualElement>(
                    "enemy-health-fill");
            enemyHealthPanel =
                root.Q<VisualElement>(
                    "enemy-health-panel");

            if (myHealthName != null)
            {
                string name =
                    PhotonNetwork.NickName ?? "YOU";
                myHealthName.text = name.ToUpper();
            }
        }

        private void TryFindPlayers()
        {
            PlayerHealth[] allPlayers =
                FindObjectsByType<PlayerHealth>(
                    FindObjectsSortMode.None);

            foreach (PlayerHealth player in allPlayers)
            {
                PhotonView pv =
                    player.GetPhotonView();
                if (pv == null || pv.ViewID == 0)
                    continue;

                if (pv.IsMine && myHealth == null)
                {
                    myHealth = player;
                    myHealth.OnHealthChanged +=
                        UpdateMyHealth;
                    UpdateMyHealth(
                        myHealth.CurrentHealth,
                        myHealth.MaxHealth);
                }
                else if (!pv.IsMine &&
                         enemyHealth == null)
                {
                    enemyHealth = player;
                    enemyHealth.OnHealthChanged +=
                        UpdateEnemyHealth;

                    string enemyName =
                        pv.Owner?.NickName ?? "ENEMY";
                    if (enemyHealthName != null)
                        enemyHealthName.text =
                            enemyName.ToUpper();

                    UpdateEnemyHealth(
                        enemyHealth.CurrentHealth,
                        enemyHealth.MaxHealth);
                }
            }

            if (myHealth != null &&
                enemyHealth != null)
            {
                isInitialized = true;
            }
            else if (myHealth != null)
            {
                int total =
                    PhotonNetwork.CurrentRoom
                        ?.PlayerCount ?? 1;
                if (total == 1)
                {
                    isInitialized = true;
                    enemyHealthPanel
                        ?.AddToClassList("hidden");
                }
            }
        }

        private void UpdateMyHealth(
            int current, int max)
        {
            if (myHealthText != null)
                myHealthText.text =
                    $"{current} / {max}";

            if (myHealthFill != null)
            {
                float pct = max > 0
                    ? (float)current / max * 100f
                    : 0f;

                myHealthFill.style.width =
                    new StyleLength(
                        new Length(pct,
                            LengthUnit.Percent));

                myHealthFill.RemoveFromClassList(
                    "health-fill-warning");
                myHealthFill.RemoveFromClassList(
                    "health-fill-critical");

                if (current <= max * 0.25f)
                    myHealthFill.AddToClassList(
                        "health-fill-critical");
                else if (current <= max * 0.5f)
                    myHealthFill.AddToClassList(
                        "health-fill-warning");
            }
        }

        private void UpdateEnemyHealth(
            int current, int max)
        {
            if (enemyHealthText != null)
                enemyHealthText.text =
                    $"{current} / {max}";

            if (enemyHealthFill != null)
            {
                float pct = max > 0
                    ? (float)current / max * 100f
                    : 0f;

                enemyHealthFill.style.width =
                    new StyleLength(
                        new Length(pct,
                            LengthUnit.Percent));
            }
        }

        private void OnDestroy()
        {
            if (myHealth != null)
                myHealth.OnHealthChanged -=
                    UpdateMyHealth;
            if (enemyHealth != null)
                enemyHealth.OnHealthChanged -=
                    UpdateEnemyHealth;
        }
    }
}
