using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WalletUI : MonoBehaviour
{
    public Button GenerateButton;
    public Button CloseModalButton;
    public Button OpenModalButton;
    public Button GetAirdrop;
    public GameObject WalletModal;

    public TMP_Text AddressText;
    
    public TMP_Text PrivateKeyText;
    public TMP_Text BalanceApt;

    void OnEnable()
    {
        WalletManager.Instance.OnGetBalance += OnGetWalletBalance;
    }

    void OnDisable()
    {
        WalletManager.Instance.OnGetBalance -= OnGetWalletBalance;
    }

    private void OnGetWalletBalance(float balance)
    {
        BalanceApt.text = balance.ToString("F8");  // Display balance with 8 decimal places
    }

    void Start()
    {
        GenerateButton.onClick.AddListener(GenerateNewWallet);
        CloseModalButton.onClick.AddListener(() => WalletModal.SetActive(false));
        OpenModalButton.onClick.AddListener(OpenWalletModal);
        GetAirdrop.onClick.AddListener(RequestAirdrop);

        if (!PlayerPrefs.HasKey("MnemonicsKey"))
        {
            WalletModal.SetActive(true);
            GenerateNewWallet();
        }
        else
        {
            WalletModal.SetActive(false);
            WalletManager.Instance.InitWalletFromCache();
            UpdateWalletInfo();
        }
    }

    private void GenerateNewWallet()
    {
        WalletManager.Instance.GenerateAccount();
        UpdateWalletInfo();
    }

    private void OpenWalletModal()
    {
        WalletModal.SetActive(true);
        UpdateWalletInfo();
    }

    private void UpdateWalletInfo()
    {
        AddressText.text = WalletManager.Instance.GetCurrentWalletAddress();
        PrivateKeyText.text = PlayerPrefs.GetString("MnemonicsKey");
        WalletManager.Instance.LoadCurrentWalletBalance();
    }

    private void RequestAirdrop()
    {
        StartCoroutine(WalletManager.Instance.FundAccount(1000000));
    }
}