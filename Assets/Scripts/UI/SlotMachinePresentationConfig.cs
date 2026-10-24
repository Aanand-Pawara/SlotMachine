using UnityEngine;
using SlotMachine.UI;

[CreateAssetMenu(fileName = "SlotMachinePresentation", menuName = "Slot Machine/Presentation")]
public sealed class SlotMachinePresentationConfig : ScriptableObject
{
    [Header("Prefabs")]
    [SerializeField] private BettingPopupController bettingPopupPrefab;
    [SerializeField] private FloatingNotificationController notificationPrefab;
    [SerializeField] private GameObject betOptionPrefab;

    [Header("Sounds")]
    [SerializeField] private AudioClip leverPull;
    [SerializeField] private AudioClip reelSpin;
    [SerializeField] private AudioClip reelStop;
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip jackpot;

    [Header("Display")]
    [SerializeField] private string currencySymbol = "$";
    [SerializeField] private Color winColor = new(0.3f, 1f, 0.5f);
    [SerializeField] private Color lossColor = new(1f, 0.35f, 0.35f);
    [SerializeField] private Color messageColor = new(0.9f, 0.75f, 0.3f);
    [SerializeField, Min(0.1f)] private float notificationDuration = 1.4f;

    public BettingPopupController BettingPopupPrefab => bettingPopupPrefab;
    public FloatingNotificationController NotificationPrefab => notificationPrefab;
    public GameObject BetOptionPrefab => betOptionPrefab;
    public AudioClip LeverPull => leverPull;
    public AudioClip ReelSpin => reelSpin;
    public AudioClip ReelStop => reelStop;
    public AudioClip ButtonClick => buttonClick;
    public AudioClip Win => win;
    public AudioClip Jackpot => jackpot;
    public string CurrencySymbol => currencySymbol;
    public Color WinColor => winColor;
    public Color LossColor => lossColor;
    public Color MessageColor => messageColor;
    public float NotificationDuration => notificationDuration;
}
