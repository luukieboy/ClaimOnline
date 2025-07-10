using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Alteruna;
using UnityEngine.SceneManagement;
using TMPro;

// Hoi future Luuk, ik was problemenen aan het oplossen hier met toevoegan aan de hand en zorgen dat alles gebeurt voor het volgende gebeurt, dus veel coroutines en updaten van bools. Kijk maar ff of je het beter kan maken, hopelijk was de vakantie leuk!



public class GameManager : AttributesSync
{
    public bool gameStarted;
    public Multiplayer multiplayer;
    public GameObject cardPrefab;
    public List<PlayerInfo> players = new List<PlayerInfo>();
    public CardSpawner cardSpawner;
    public Settings settings;
    public PlayerInfo localPlayer;
    public ushort playerNumber;


    private List<Card> cardPile;
    private int maxAmount;
    private List<PlayerRoundInfo> roundInfo = new List<PlayerRoundInfo>();
    private bool amITheHost;
    private PlayerInfo currentPlayer;
    private TMP_Text text;
    private bool roundInfoCreated = false;
    private List<Color> playerColours = new List<Color>{
            new Color(222, 238, 0),
            new Color(255, 0, 0),
            new Color(255, 0, 213),
            new Color(30, 0, 255)};

    void Start()
    {
        settings = GameObject.Find("Settings").GetComponent<Settings>();
        amITheHost = settings.amITheHost;
        multiplayer = settings.multiplayer;
        cardPile = new List<Card>();
        gameStarted = false;
    }

    void Update()
    {
        if (multiplayer == null) multiplayer = settings.multiplayer;
        amITheHost = settings.amITheHost;
    }
    public void ButtonToStartPressed()
    {
        if (amITheHost) StartCoroutine(SetupGame());
    }

    private IEnumerator SetupGame()
    {
        BroadcastRemoteMethod("ChangeScene");
        yield return new WaitForSeconds(0.05f);
        BroadcastRemoteMethod("DeletePlayers");
        cardSpawner = GameObject.Find("CardSpawner").GetComponent<CardSpawner>();
        InvokeRemoteMethod("HostSetupForGame", settings.hostId);
        yield return new WaitForSeconds(7);
        foreach (User user in multiplayer.GetUsers()) InvokeRemoteMethod("PlayerSetupForGame", user.Index);
        yield return new WaitForSeconds(2f);
        StartCoroutine(StartFirstPhase());

    }

    private IEnumerator StartFirstPhase()
    {
        Debug.Log(cardPile.Count + "   " + players[0].currentHand.Count);
        while (cardPile.Count != 0 && players[0].currentHand.Count != 0)
        {
            Card card = cardPile[0];
            cardSpawner.spawnCardInMiddle(card, 0);

            StartCoroutine(CreateRoundInfo());
            while (!roundInfoCreated) yield return new WaitForSeconds(0.3f);
            roundInfoCreated = false;

            PlayerInfo winner = DetermineWinnerFirstPhase();
            BroadcastRemoteMethod("ShowWinner", (ushort)players.IndexOf(winner));
            yield return new WaitForSeconds(3f);
            StartCoroutine(MoveCardsFirstPhase(winner.user.Index));
            yield return new WaitForSeconds(1.5f);

            // Switch order of playing
            players.Remove(winner);
            players.Insert(0, winner);
            foreach (PlayerInfo player in players) InvokeRemoteMethod("SetPlayerNumber", player.user.Index, players.IndexOf(player));

            yield return new WaitForSeconds(1f);
            cardPile.Remove(card);
        }
        SetupSecondPhase();
    }

    private void SetupSecondPhase()
    {
        BroadcastRemoteMethod("DestroyObjectForSecondPhase");
        foreach (PlayerInfo player in players) player.SetupSecondPhase();
        StartCoroutine(StartSecondPhase());
    }

    private IEnumerator StartSecondPhase()
    {
        for (int i = 0; i < maxAmount; i++)
        {
            StartCoroutine(CreateRoundInfo());
            while (!roundInfoCreated) yield return new WaitForSeconds(0.3f);
            roundInfoCreated = false;
            PlayerInfo winner = DetermineWinnerSecondPhase();
            BroadcastRemoteMethod("ShowWinner", (ushort)players.IndexOf(winner));
            yield return new WaitForSeconds(3f);
            MoveCardsSecondPhase(winner.user.Index);

            // Switch order of playing
            players.Remove(winner);
            players.Insert(0, winner);
            foreach (PlayerInfo player in players) InvokeRemoteMethod("SetPlayerNumber", player.user.Index, players.IndexOf(player));
        }
    }

    private IEnumerator CreateRoundInfo()
    {
        roundInfo = new List<PlayerRoundInfo>();
        foreach (PlayerInfo player in players)
        {
            InvokeRemoteMethod("AllowPlayerToPlay", player.user.Index, player.user.Name);
            while (player.cardPlayed == null)
            {
                yield return new WaitForSeconds(0.3f);
            }
            Card cardInfo = player.PlayCard();
            roundInfo.Add(new PlayerRoundInfo(player, cardInfo));
        }
        roundInfoCreated = true;
    }

    private IEnumerator MoveCardsFirstPhase(ushort winnerIndex)
    {
        // Move all Undead cards to the winners victory pile
        if (roundInfo.FirstOrDefault(info => info.user.user.Index == winnerIndex)?.playedCard.faction == "Undead")
        {
            foreach (PlayerRoundInfo playerRoundInfo in roundInfo)
            {
                if (playerRoundInfo.playedCard.faction == "Undead") cardSpawner.spawnCardInVictoryHand(playerRoundInfo.playedCard, winnerIndex);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Move the middle card to the winners pile
        GiveNextCardInDeckToBattleHand(winnerIndex);
        // yield return new WaitForSeconds(0.5f);

        // Give every other player a card from the pile
        foreach (PlayerInfo player in players)
        {
            if (player.user.Index != winnerIndex)
            {
                GiveNextCardInDeckToBattleHand(player.user.Index);
            }
        }

        // Destroy all cards played
        BroadcastRemoteMethod("DestroyCards");
    }

    private PlayerInfo DetermineWinnerFirstPhase()
    {
        string leadingFaction = roundInfo[0].playedCard.faction;
        int valueToBeat = roundInfo[0].playedCard.value;
        PlayerInfo winnerIndex = null;

        // Knight wins if played after Goblin
        if (leadingFaction == "Goblin")
        {
            // BUG
            foreach (PlayerRoundInfo playerRoundInfo in roundInfo)
            {
                if (playerRoundInfo.playedCard.faction == "Knight")
                {
                    Debug.Log("Knight after goblin");
                    if (winnerIndex == null || (winnerIndex != null && playerRoundInfo.playedCard.value > valueToBeat))
                    {
                        winnerIndex = playerRoundInfo.user;
                        valueToBeat = playerRoundInfo.playedCard.value;
                    }
                }
            }
            if (winnerIndex != null) return winnerIndex;
        }

        // Regular win (highest value)
        winnerIndex = roundInfo[0].user;
        foreach (PlayerRoundInfo playerRoundInfo in roundInfo)
        {
            if (playerRoundInfo.playedCard.faction == leadingFaction || playerRoundInfo.playedCard.faction == "Doppelgängers")
            {
                if (playerRoundInfo.playedCard.value > valueToBeat)
                {
                    winnerIndex = playerRoundInfo.user;
                    valueToBeat = playerRoundInfo.playedCard.value;
                }
            }
        }
        return winnerIndex;
    }

    private void MoveCardsSecondPhase(ushort winnerIndex)
    {
        ushort loserIndex = roundInfo.FirstOrDefault(info => info.user.user.Index != winnerIndex).user.user.Index;
        {
            foreach (PlayerRoundInfo playerRoundInfo in roundInfo)
            {
                // If dwarves are played, give them to the loser
                if (playerRoundInfo.playedCard.faction == "Dwarves") cardSpawner.spawnCardInVictoryHand(playerRoundInfo.playedCard, loserIndex);
                else cardSpawner.spawnCardInVictoryHand(playerRoundInfo.playedCard, winnerIndex);
            }
        }

        // Destroy all cards played
        BroadcastRemoteMethod("DestroyCards");
    }

    private PlayerInfo DetermineWinnerSecondPhase()
    {
        return DetermineWinnerFirstPhase();
    }

    private void GiveNextCardInDeckToBattleHand(ushort Id)
    {
        Card card = cardPile[0];
        cardSpawner.spawnCardInBattleHand(card, Id);
        cardPile.Remove(card);
    }

    public void ChangePlayerList()
    {
        if (amITheHost)
        {
            foreach (PlayerInfo player in players)
            {
                if (!multiplayer.GetUsers().Contains(player.user)) players.Remove(player);
            }
        }
    }

    private void createCardPile()
    {
        string[] lines;

        foreach (string chosenSet in Settings.Instance.chosenSets)
        {
            foreach (string extensionFile in Directory.GetFiles("C:/Users/Luuk/Documents/Aangenaaide bestanden/SpaceJammer/Claim/Assets/Code/Decks"))
            {
                if (extensionFile.Contains(".meta")) continue;
                lines = File.ReadAllLines(extensionFile);
                foreach (string line in lines)
                {
                    if (line.Contains(chosenSet)) addToCardPile(line);
                }
            }
        }
    }

    private void addToCardPile(string line)
    {
        string[] factionInfo = line.Split(",");
        string factionName = factionInfo[0];
        if (factionName.Contains("<s>")) factionName = factionName.Split("<s>")[1].Trim();
        foreach (string factionInfoPart in factionInfo.Skip(1))
        {
            if (factionInfoPart.Contains("-"))
            {
                int[] range = factionInfoPart.Split('-').Select(int.Parse).ToArray();
                for (int i = range[0]; i < range[1] + 1; i++)
                {
                    cardPile.Add(new Card(factionName, i));
                }
            }
            else if (factionInfoPart.Contains("x"))
            {
                int[] multiplicationInfo = factionInfoPart.Split('x').Select(int.Parse).ToArray();
                cardPile.AddRange(Enumerable.Repeat(new Card(factionName, multiplicationInfo[1]), multiplicationInfo[0]).ToList());
            }
            else cardPile.Add(new Card(factionName, int.Parse(factionInfoPart)));
        }
    }

    private IEnumerator handOutCards()
    {
        Card randomCard;

        for (int i = 0; i < maxAmount; i++)
        {
            foreach (PlayerInfo player in players)
            {
                randomCard = cardPile[UnityEngine.Random.Range(0, cardPile.Count)];
                InvokeRemoteMethod("ClientAddToHand", player.user.Index, randomCard.faction, randomCard.value);
                cardSpawner.spawnCardInHand(randomCard, player.user.Index);
                yield return new WaitForSeconds(0.1f);
                cardPile.Remove(randomCard);
            }
        }
    }

    public (GameObject objectOfInterest, bool done) getObjectWithTag(Transform parent, string tag)
    {
        (GameObject objectOfInterest, bool done) output = (gameObject, false);

        foreach (Transform child in parent)
        {
            if (child.gameObject.tag == tag)
            {
                return (child.gameObject, true);
            }
            output = getObjectWithTag(child, tag);
            if (output.done) return output;
        }
        return output;
    }

    [SynchronizableMethod]
    public void ClientAddToHand(string faction, int value)
    {
        Card newCard = new Card(faction, value);
        localPlayer.addToHand(newCard);
    }

    [SynchronizableMethod]
    public void ChangeScene()
    {
        SceneManager.LoadScene("MainGameScene", LoadSceneMode.Additive);
    }

    [SynchronizableMethod]
    public void DeletePlayers()
    {
        for (int i = settings.amountOfPeople + 1; i < 5; i++)
        {
            GameObject toDestroy = GameObject.Find("Player" + i.ToString());
            Destroy(toDestroy);
        }
    }

    [SynchronizableMethod]
    public void HostSetupForGame()
    {
        Debug.Log("Are we good?? " + (settings.amountOfPeople == players.Count));
        players.Shuffle();
        foreach (PlayerInfo player in players) InvokeRemoteMethod("SetPlayerNumber", player.user.Index, players.IndexOf(player));
        createCardPile();
        cardPile.Shuffle();
        maxAmount = cardPile.Count / (2 * Math.Max(2, Settings.Instance.amountOfPeople));
        StartCoroutine(handOutCards());
    }

    [SynchronizableMethod]
    public void PlayerSetupForGame()
    {
        gameStarted = true;
        text = getObjectWithTag(GameObject.Find("CardSpawner").transform, "playerNumber").objectOfInterest.GetComponent<TMP_Text>();
        localPlayer.SetupForGame();
        cardSpawner = GameObject.Find("CardSpawner").GetComponent<CardSpawner>();
    }

    [SynchronizableMethod]
    public void AllowPlayerToPlay(string name)
    {
        GameObject.Find("Player (" + name + ")").GetComponent<PlayerCode>().allowedToPlay = true;
    }

    [SynchronizableMethod]
    public void ShowWinner(ushort Id)
    {
        getObjectWithTag(cardSpawner.gameObject.transform, "playedCard" + Id.ToString()).objectOfInterest.GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 0f);
    }

    [SynchronizableMethod]
    public void DestroyCards()
    {
        Destroy(getObjectWithTag(cardSpawner.transform, "cardPlayingFor").objectOfInterest);
        for (int i = 0; i < multiplayer.GetUsers().Count; i++)
        {
            Destroy(getObjectWithTag(cardSpawner.transform, "playedCard" + i.ToString()).objectOfInterest);
        }
    }

    [SynchronizableMethod]
    public void SetPlayerNumber(ushort number)
    {
        playerNumber = number;
        if (text == null) text = getObjectWithTag(GameObject.Find("CardSpawner").transform, "playerNumber").objectOfInterest.GetComponent<TMP_Text>();
        text.text = "Player " + (number + 1).ToString();
        getObjectWithTag(cardSpawner.gameObject.transform, "playerMat").objectOfInterest.GetComponent<SpriteRenderer>().color = playerColours[number];
    }

    [SynchronizableMethod]
    public void DestroyObjectForSecondPhase()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject objectGameOf = getObjectWithTag(cardSpawner.gameObject.transform, "destroyable" + i.ToString()).objectOfInterest;
            if (objectGameOf != null) Destroy(objectGameOf);
        }
        
    }
}
