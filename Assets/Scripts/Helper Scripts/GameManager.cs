using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private GameData gameData;

    [HideInInspector]
    public int starScore, score_Count, selected_Index;

    [HideInInspector]
    public bool[] heroes;

    [HideInInspector]
    public bool playSound = true;

    private string data_Path = "GameData.dat";

    void Awake()
    {
        MakeSingleton();
        InitializeGameData();
    }

    void MakeSingleton()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void InitializeGameData()
    {
        // StartCoroutine(LoadGameDataFromBlockchain());
    }

    private IEnumerator LoadGameDataFromBlockchain()
    {
        bool success = true;
        yield return StartCoroutine(WalletManager.Instance.GetScore((blockchainScore) =>
        {
            score_Count = (int)blockchainScore;
        }));

        yield return StartCoroutine(WalletManager.Instance.GetStarScore((blockchainStarScore) =>
        {
            starScore = (int)blockchainStarScore;
        }));

        if (success)
        {
            gameData = new GameData
            {
                StarScore = starScore,
                ScoreCount = score_Count,
                // Set other fields in gameData
            };

            // Load local data for additional info not stored on blockchain
            LoadGameData();

            // Merge blockchain and local data if necessary
            // For example, take the higher score between blockchain and local storage
            gameData.StarScore = Math.Max(gameData.StarScore, starScore);
            gameData.ScoreCount = Math.Max(gameData.ScoreCount, score_Count);

            SaveGameData();
        }
        else
        {
            Debug.LogWarning("Failed to load game data from blockchain. Using local data.");
            LoadGameData();
        }

        // Initialize other game data
        selected_Index = gameData.SelectedIndex;
        heroes = gameData.Heroes ?? new bool[9];
        if (heroes[0] == false)
        {
            heroes[0] = true;
            for (int i = 1; i < heroes.Length; i++)
            {
                heroes[i] = false;
            }
        }
    }

    public void SaveGameData()
    {
        FileStream file = null;

        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            file = File.Create(Application.persistentDataPath + data_Path);

            if (gameData != null)
            {
                gameData.Heroes = heroes;
                gameData.StarScore = starScore;
                gameData.ScoreCount = score_Count;
                gameData.SelectedIndex = selected_Index;

                bf.Serialize(file, gameData);
            }

            // Update blockchain data
            StartCoroutine(UpdateBlockchainData());
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data: {e.Message}");
        }
        finally
        {
            if (file != null)
            {
                file.Close();
            }
        }
    }

    private IEnumerator UpdateBlockchainData()
    {
        yield return StartCoroutine(WalletManager.Instance.UpdateScore((ulong)score_Count));
        // Add other blockchain updates as needed
    }

    void LoadGameData()
    {
        FileStream file = null;

        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            file = File.Open(Application.persistentDataPath + data_Path, FileMode.Open);
            gameData = (GameData)bf.Deserialize(file);

            if (gameData != null)
            {
                starScore = gameData.StarScore;
                score_Count = gameData.ScoreCount;
                heroes = gameData.Heroes;
                selected_Index = gameData.SelectedIndex;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game data: {e.Message}");
        }
        finally
        {
            if (file != null)
            {
                file.Close();
            }
        }
    }
}