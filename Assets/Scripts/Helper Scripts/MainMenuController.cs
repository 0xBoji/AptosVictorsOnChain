using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class MainMenuController : MonoBehaviour
{
    public GameObject hero_Menu;
    public Text starScoreText;

    public Image music_Img;
    public Sprite music_Off, music_On;

    public void Start()
    {
        StartCoroutine(InitializeWallet());
    }


    public void OnEnable(){
        WalletManager.Instance.OnGetBalance += OnGetBalance;
    }

    private void OnGetBalance(float obj)
    {
        if(obj < 0.1f){
            StartCoroutine(WalletManager.Instance.FundAccount(10000000)); // 1 APT

        }
    }

    private IEnumerator InitializeWallet()
    {
        // Initialize wallet
        WalletManager.Instance.InitWalletFromCache();

        if (string.IsNullOrEmpty(WalletManager.Instance.GetCurrentWalletAddress()))
        {
            WalletManager.Instance.GenerateAccount();
        }

        // Fund the account if needed
        WalletManager.Instance.LoadCurrentWalletBalance();


        // Update UI with star score
        yield return StartCoroutine(WalletManager.Instance.GetStarScore((score) =>
        {
            GameManager.instance.starScore = (int)score;
            UpdateStarScoreUI();
        }));
    }

    public void PlayGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        bool isInitialized = false;
        yield return StartCoroutine(WalletManager.Instance.IsInitialized((initialized) => isInitialized = initialized));

        if (!isInitialized)
        {
            yield return StartCoroutine(WalletManager.Instance.InitializeGame());
        }

        yield return StartCoroutine(WalletManager.Instance.StartGame());

        SceneManager.LoadScene("Gameplay");
    }

    public void HeroMenu()
    {
        hero_Menu.SetActive(true);
        UpdateStarScoreUI();
    }

    public void HomeButton()
    {
        hero_Menu.SetActive(false);
    }

    public void MusicButton()
    {
        if (GameManager.instance.playSound)
        {
            music_Img.sprite = music_Off;
            GameManager.instance.playSound = false;
        }
        else
        {
            music_Img.sprite = music_On;
            GameManager.instance.playSound = true;
        }
    }

    private void UpdateStarScoreUI()
    {
        starScoreText.text = GameManager.instance.starScore.ToString();
    }
}