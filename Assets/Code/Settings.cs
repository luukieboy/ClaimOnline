using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Alteruna;

public class Settings : AttributesSync
{
    // Most of the things that happen in the main screen is handled here
    public static Settings Instance;

    public List<string> chosenSets;
    public bool amITheHost = false;
    public Multiplayer multiplayer;
    public GameObject togglePrefab;
    public ushort hostId;

    private List<string> doubleSets = new List<string>();
    private TMP_Text playerText;
    private Vector3 singleCardLocationSpawn = new Vector3(-1.65f, 3.35f, -0.1f);
    private Vector3 doubleCardLocationSpawn = new Vector3(-7.8f, 3.35f, -0.1f);
    private List<Toggle> toggleObjects;
    private List<string> cardNames;
    private TMP_Text amountOfCardsText;

    [SynchronizableField]
    public int amountOfCardsNeeded;
    [SynchronizableField]
    public int amountOfPeople;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    void Start()
    {
        GameManager gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        doubleSets = new List<string>();
        playerText = gameManager.getObjectWithTag(gameObject.GetComponent<Transform>(), "playerText").objectOfInterest.GetComponent<TMP_Text>();
        amountOfCardsText = gameManager.getObjectWithTag(gameObject.GetComponent<Transform>(), "cardPicker").objectOfInterest.GetComponent<TMP_Text>();
        multiplayer = GameObject.Find("NetWorkManager").GetComponent<Multiplayer>();
        cardNames = GetAllCardNames();
    }

    void Update()
    {
        if (multiplayer == null) multiplayer = GameObject.Find("NetWorkManager").GetComponent<Multiplayer>();
        amITheHost = multiplayer.InRoom && multiplayer.Me.IsHost;
        amountOfPeople = multiplayer.GetUsers().Count;
    }

    public void SetupWhenJoining()
    {
        if (amITheHost)
        {
            // Hosts recalculates how many decks are needed and the player text
            CalculateAmountOfCards();
            BroadcastRemoteMethod("UpdatePlayersJoined");
        }
        // Create the check boxes only when you have joined a room
        createToggles();
        foreach (User user in multiplayer.GetUsers()) if (user.IsHost) hostId = user.Index;
    }

    public void ResetOnLeave()
    {
        // Called when player leaves the room
        DestroyToggles();
    }

    public void UpdateOnChangingUsers()
    {
        // Called when the Room list updates (alteruna)
        amountOfPeople = multiplayer.GetUsers().Count;
        if (amITheHost)
        {
            CalculateAmountOfCards();
            BroadcastRemoteMethod("UpdatePlayersJoined");
        }
    }

    public void UpdateToggle(int toggleId, bool toggleMode)
    {
        // When ticking a tickbox, change it for all other users
        foreach (User user in multiplayer.GetUsers())
        {
            if (user.Index != multiplayer.Me.Index) InvokeRemoteMethod("UpdateToggleOnClient", user.Index, toggleId, toggleMode);
        }
    }


    public void changeCardsInDeck(string cardName)
    {
        if (chosenSets.Contains(cardName)) chosenSets.Remove(cardName);
        else chosenSets.Add(cardName);
        CalculateAmountOfCards();
    }

    public void CalculateAmountOfCards()
    {
        amountOfCardsNeeded = (amountOfPeople == 2 || amountOfPeople == 1) ? 3 : 5;
        amountOfCardsNeeded -= CountSingleCardDecks();
        BroadcastRemoteMethod("SetAmountOfCardsNeeded");
    }

    private int CountSingleCardDecks()
    {
        int i = 0;

        foreach (string chosenSet in chosenSets)
        {
            if (!doubleSets.Contains(chosenSet.Trim())) i++;
        }
        return i;
    }

    public void createToggles()
    {
        toggleObjects = new List<Toggle>();

        foreach (string cardName in cardNames)
        {
            CreateAndNameToggle(cardName);
        }

        singleCardLocationSpawn = new Vector3(-1.65f, 3.35f, -0.1f);
        doubleCardLocationSpawn = new Vector3(-7.8f, 3.35f, -0.1f);
    }

    public void DestroyToggles()
    {
        foreach (Toggle toggle in toggleObjects) Destroy(toggle.gameObject);
    }

    private List<string> GetAllCardNames()
    {
        List<string> cardNames = new List<string>();
        string[] lines;
        string multipleCardName;

        foreach (string extensionFile in Directory.GetFiles(Directory.GetCurrentDirectory() + "/Assets/Code/Decks"))
        {
            multipleCardName = "";
            if (extensionFile.Contains(".meta") || extensionFile.Contains("claim2")) continue;
            lines = File.ReadAllLines(extensionFile);
            foreach (string line in lines)
            {
                if (line.Contains("<s>"))
                {
                    // Combine the two linked decks
                    if (multipleCardName != "") multipleCardName += " & ";
                    multipleCardName += line.Split("<s>")[1].Split(",")[0].Trim();
                    doubleSets.Add(line.Split("<s>")[1].Split(",")[0].Trim());
                    continue;
                }
                cardNames.Add(line.Split(",")[0].Trim());
            }
            cardNames.Add(multipleCardName);
        }
        return cardNames;
    }

    public void CreateAndNameToggle(string cardName)
    {
        GameObject toggle;
        Vector3 correction = new Vector3(33.68f, 0.34f, -0.04f);
        float distanceBetweenToggles = 0.5f;

        if (cardName.Contains("&"))
        {
            // Spawn linked decks on the left
            toggle = Instantiate(togglePrefab, doubleCardLocationSpawn - correction, Quaternion.identity);
            doubleCardLocationSpawn.y -= distanceBetweenToggles;
        }
        else
        {
            // Spawn single decks on the right
            toggle = Instantiate(togglePrefab, singleCardLocationSpawn - correction, Quaternion.identity);
            singleCardLocationSpawn.y -= distanceBetweenToggles;
        }
        toggle.transform.SetParent(transform);
        toggle.GetComponent<RectTransform>().localScale = new Vector3(0.02f, 0.02f, 1);
        toggle.GetComponentInChildren<Text>().text = cardName;
        toggleObjects.Add(toggle.GetComponent<Toggle>());
        toggle.name = toggleObjects.IndexOf(toggle.GetComponent<Toggle>()).ToString();
    }

    [SynchronizableMethod]
    public void CreateToggleForNewPlayer(GameObject toggle)
    {
        GameObject newToggle = Instantiate(toggle, toggle.transform.localScale, Quaternion.identity);
    }

    [SynchronizableMethod]
    public void SetAmountOfCardsNeeded()
    {
        amountOfCardsText.text = "Pick " + amountOfCardsNeeded.ToString() + " more";
    }

    [SynchronizableMethod]
    public void UpdatePlayersJoined()
    {
        playerText.text = "Players joined : " + amountOfPeople.ToString();
    }

    [SynchronizableMethod]
    public void UpdateToggleOnClient(int toggleId, bool toggleMode)
    {
        toggleObjects[toggleId].isOn = toggleMode;
    }
}
