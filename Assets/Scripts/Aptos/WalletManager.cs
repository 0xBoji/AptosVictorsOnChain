using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aptos.Unity.Rest;
using Aptos.Unity.Rest.Model;
using Aptos.Accounts;
using Aptos.HdWallet;
using NBitcoin;
using Aptos.BCS;
using Transaction = Aptos.Unity.Rest.Model.Transaction;
using UnityEngine.Networking;
using System.Linq;
using Newtonsoft.Json.Linq;

public class WalletManager : MonoBehaviour
{
 public static WalletManager Instance { get; private set; }

    // private RestClient.Instance RestClient.Instance;
    private Wallet wallet;
    
    private const string MODULE_ADDRESS = "0xd36ee9d2883da4b1eb018b1f6d7eab57588e5e273c49c919927a8c54b4c647b9"; // Replace with your actual module address
    private const string MODULE_NAME = "gameplaymanager";

    [SerializeField] private int accountNumLimit = 10;
    public List<string> addressList;

    private const string MNEMONICS_KEY = "MnemonicsKey";
    private const string CURRENT_ADDRESS_INDEX_KEY = "CurrentAddressIndexKey";

    public event Action<float> OnGetBalance;
   void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // RestClient.Instance = RestClient.Instance.Instance;
        // StartCoroutine(InitializeRestClient.Instance());
    }


    void Start(){
                // RestClient.Instance = RestClient.Instance.Instance;
                        RestClient.Instance.SetEndPoint(Constants.DEVNET_BASE_URL);

    }

    // private IEnumerator InitializeRestClient.Instance()
    // {
    //     yield return StartCoroutine(RestClient.Instance.SetUp());
    //     InitializeWallet();
    // }

    private void InitializeWallet()
    {
        string cachedMnemonics = PlayerPrefs.GetString(MNEMONICS_KEY);
        if (!string.IsNullOrEmpty(cachedMnemonics))
        {
            RestoreWallet(cachedMnemonics);
        }
        else
        {
            GenerateAccount();
        }
    }

    public void InitWalletFromCache()
    {
        string cachedMnemonics = PlayerPrefs.GetString(MNEMONICS_KEY);
        if (!string.IsNullOrEmpty(cachedMnemonics))
        {
            RestoreWallet(cachedMnemonics);
        }
        else
        {
            Debug.LogWarning("No wallet found in cache. Creating a new one.");
            GenerateAccount();
        }
    }

    public bool GenerateAccount()
    {
        Mnemonic mnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
        wallet = new Wallet(mnemo);

        PlayerPrefs.SetString(MNEMONICS_KEY, mnemo.ToString());
        PlayerPrefs.SetInt(CURRENT_ADDRESS_INDEX_KEY, 0);

        GetWalletAddresses();
        LoadCurrentWalletBalance();

        return !string.IsNullOrEmpty(mnemo.ToString());
    }

    public bool RestoreWallet(string mnemonics)
    {
        try
        {
            wallet = new Wallet(mnemonics);
            PlayerPrefs.SetString(MNEMONICS_KEY, mnemonics);
            PlayerPrefs.SetInt(CURRENT_ADDRESS_INDEX_KEY, 0);

            GetWalletAddresses();
            LoadCurrentWalletBalance();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to restore wallet: {e.Message}");
            return false;
        }
    }

    public List<string> GetWalletAddresses()
    {
        addressList = new List<string>();

        for (int i = 0; i < accountNumLimit; i++)
        {
            var account = wallet.GetAccount(i);
            var addr = account.AccountAddress.ToString();
            addressList.Add(addr);
        }

        return addressList;
    }

    public string GetCurrentWalletAddress()
    {
        int currentIndex = PlayerPrefs.GetInt(CURRENT_ADDRESS_INDEX_KEY);
        return addressList[0];
    }

    public string GetPrivateKey()
    {
        int currentIndex = PlayerPrefs.GetInt(CURRENT_ADDRESS_INDEX_KEY);
        return wallet.GetAccount(currentIndex).PrivateKey;
    }
 public void LoadCurrentWalletBalance()
    {
        int currentIndex = PlayerPrefs.GetInt(CURRENT_ADDRESS_INDEX_KEY);
        var currentAccount = wallet.GetAccount(currentIndex);
        string address = currentAccount.AccountAddress.ToString();

        StartCoroutine(GetAccountBalance(address));
    }

    private IEnumerator GetAccountBalance(string address)
    {
        string url = $"{Constants.DEVNET_BASE_URL}/accounts/{address}/resource/0x1::coin::CoinStore<0x1::aptos_coin::AptosCoin>";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string response = webRequest.downloadHandler.text;
                JObject jsonResponse = JObject.Parse(response);

                if (jsonResponse["data"] != null && jsonResponse["data"]["coin"] != null && jsonResponse["data"]["coin"]["value"] != null)
                {
                    string balanceString = jsonResponse["data"]["coin"]["value"].ToString();
                    if (ulong.TryParse(balanceString, out ulong balanceInOctas))
                    {
                        float balanceInApt = balanceInOctas / 100000000f; // Convert Octas to APT
                        OnGetBalance?.Invoke(balanceInApt);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse balance value");
                        OnGetBalance?.Invoke(0f);
                    }
                }
                else
                {
                    Debug.LogError("Unexpected JSON structure in the response");
                    OnGetBalance?.Invoke(0f);
                }
            }
            else
            {
                Debug.LogError($"Error fetching balance: {webRequest.error}");
                OnGetBalance?.Invoke(0f);
            }
        }
    }


    private const string DEVNET_FAUCET_URL = "https://faucet.devnet.aptoslabs.com/mint";

    public IEnumerator FundAccount(uint amount)
    {
        string address = GetCurrentWalletAddress();
        string url = $"{DEVNET_FAUCET_URL}?amount={amount}&address={address}";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Faucet request failed: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
            }
            else
            {
                Debug.Log("Faucet request successful!");
                Debug.Log($"Response: {www.downloadHandler.text}");
                
                // Wait a bit for the transaction to be processed
                yield return new WaitForSeconds(5);
                
                // Refresh the balance
                LoadCurrentWalletBalance();
            }
        }
    }

    public IEnumerator Transfer(string toAddress, long amount)
    {
        int currentIndex = PlayerPrefs.GetInt(CURRENT_ADDRESS_INDEX_KEY);
        var currentAccount = wallet.GetAccount(currentIndex);

        Transaction transaction = null;
        ResponseInfo responseInfo = new ResponseInfo();

        yield return StartCoroutine(RestClient.Instance.Transfer((txn, response) =>
        {
            transaction = txn;
            responseInfo = response;
        }, currentAccount, toAddress, amount));

        if (responseInfo.status == ResponseInfo.Status.Success)
        {
            yield return StartCoroutine(WaitForTransaction(transaction.Hash));
            LoadCurrentWalletBalance();
        }
        else
        {
            Debug.LogError($"Transfer failed: {responseInfo.message}");
        }
    }

    // Game-specific methods

  public IEnumerator IsInitialized(Action<bool> callback)
    {
        int currentIndex = PlayerPrefs.GetInt(CURRENT_ADDRESS_INDEX_KEY);
        var currentAccount = wallet.GetAccount(0);
        string address = currentAccount.AccountAddress.ToString();

        string resourceType = $"{MODULE_ADDRESS}::{MODULE_NAME}::GameState";
        string url = $"{Constants.DEVNET_BASE_URL}/accounts/{address}/resource/{resourceType}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            bool initialized = false;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string response = webRequest.downloadHandler.text;
                JObject jsonResponse = JObject.Parse(response);

                // If we can successfully parse the response and find the data field,
                // we consider the account initialized
                initialized = jsonResponse["data"] != null;
            }
            else
            {
                // If we get a 404 Not Found, it means the resource doesn't exist,
                // which indicates the account is not initialized
                initialized = webRequest.responseCode != 404;

                if (webRequest.responseCode != 404)
                {
                    Debug.LogError($"Error checking initialization: {webRequest.error}");
                }
            }

            callback(initialized);
        }
    }

    public IEnumerator InitializeGame()
    {
        yield return StartCoroutine(ExecuteTransaction("initialize_game", new List<object>()));
    }

    public IEnumerator StartGame()
    {
        StartCoroutine(IsInitialized((isInitialized) => {
            if(!isInitialized){
                StartCoroutine(InitializeGame());
            }
            
        }));
        yield return StartCoroutine(ExecuteTransaction("start_game", new List<object>()));
    }

    public IEnumerator UpdateScore(ulong newScore)
    {
        yield return StartCoroutine(ExecuteTransaction("update_score", new List<object> { newScore }));
    }

    public IEnumerator CollectStar()
    {
        yield return StartCoroutine(ExecuteTransaction("collect_star", new List<object>()));
    }

    public IEnumerator EndGame()
    {
        yield return StartCoroutine(ExecuteTransaction("end_game", new List<object>()));
    }

    public IEnumerator MakePurchase(ulong itemCost)
    {
        yield return StartCoroutine(ExecuteTransaction("make_purchase", new List<object> { itemCost }));
    }

   private IEnumerator ExecuteTransaction(string function, List<object> args)
    {
        if (RestClient.Instance == null)
        {
            Debug.LogError("RestClient.Instance is not initialized. Please wait for initialization to complete.");
            yield break;
        }

        var currentAccount = wallet.GetAccount(0);

        yield return StartCoroutine(ExecuteTransactionCoroutine(currentAccount, function, args));
    }

      private IEnumerator ExecuteTransactionCoroutine(Account account, string function, List<object> args)
    {
        Transaction transaction = null;
        ResponseInfo responseInfo = new ResponseInfo();

        yield return StartCoroutine(RestClient.Instance.SubmitTransaction((txn, response) =>
        {
            transaction = txn;
            responseInfo = response;
        }, account, new EntryFunction(
            new ModuleId(AccountAddress.FromHex(MODULE_ADDRESS), MODULE_NAME),
            function,
            new Aptos.BCS.TagSequence(new ISerializableTag[] { }),
            new Aptos.BCS.Sequence(args.OfType<ISerializable>().ToArray())
        )));

        if (responseInfo.status == ResponseInfo.Status.Success)
        {
            if (transaction != null && !string.IsNullOrEmpty(transaction.Hash))
            {
                Debug.Log($"Transaction {function} submitted. Hash: {transaction.Hash}");
                yield return StartCoroutine(WaitForTransaction(transaction.Hash));
            }
            else
            {
                Debug.LogError($"Transaction {function} submitted successfully, but no hash was returned.");
            }
        }
        else
        {
            Debug.LogError($"Failed to submit transaction {function}: {responseInfo.message}");
        }
    }
    
    
        private IEnumerator ViewFunction(string function, Action<ulong> callback)
    {
        if (RestClient.Instance == null)
        {
            Debug.LogError("RestClient.Instance is not initialized. Please wait for initialization to complete.");
            callback(0);
            yield break;
        }

        var currentAccount = wallet.GetAccount(0);

        yield return StartCoroutine(ViewFunctionCoroutine(currentAccount, function, callback));
    }

    private IEnumerator ViewFunctionCoroutine(Account account, string function, Action<ulong> callback)
    {
        string[] result = null;
        ResponseInfo responseInfo = new ResponseInfo();

        yield return StartCoroutine(RestClient.Instance.View((data, response) =>
        {
            result = data;
            responseInfo = response;
        }, new ViewRequest
        {
            Function = $"{MODULE_ADDRESS}::{MODULE_NAME}::{function}",
            TypeArguments = new string[0],
            Arguments = new string[] { account.AccountAddress.ToString() }
        }));

        if (responseInfo.status == ResponseInfo.Status.Success && result != null && result.Length > 0)
        {
            if (ulong.TryParse(result[0], out ulong value))
            {
                callback(value);
            }
            else
            {
                Debug.LogError($"Failed to parse result for {function}: {result[0]}");
                callback(0);
            }
        }
        else
        {
            Debug.LogError($"Failed to get {function}: {responseInfo.message}");
            callback(0);
        }
    }        private IEnumerator WaitForTransaction(string txnHash)
    {
        bool txnSuccess = false;
        ResponseInfo responseInfo = new ResponseInfo();

        yield return StartCoroutine(RestClient.Instance.WaitForTransaction((success, response) =>
        {
            txnSuccess = success;
            responseInfo = response;
        }, txnHash));

        if (txnSuccess)
        {
            Debug.Log($"Transaction {txnHash} completed successfully.");
        }
        else
        {
            Debug.LogError($"Transaction {txnHash} failed or timed out: {responseInfo.message}");
        }
    }

    public IEnumerator GetScore(Action<ulong> callback)
    {
        yield return StartCoroutine(ViewFunction("get_score", callback));
    }

    public IEnumerator GetStarScore(Action<ulong> callback)
    {
        yield return StartCoroutine(ViewFunction("get_star_score", callback));
    }

    public IEnumerator GetBestScore(Action<ulong> callback)
    {
        yield return StartCoroutine(ViewFunction("get_best_score", callback));
    }


    // Utility methods

    public float AptosTokenToFloat(float token)
    {
        return token / 100000000f;
    }

    public long AptosFloatToToken(float amount)
    {
        return Convert.ToInt64(amount * 100000000);
    }
}