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
// Ff kieken waarvoor gamemanager de info van players nodig heeft en of ik dat niet gewoon locaal kan doen.


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
    public bool readyToContinue = false;


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

    [SynchronizableField]
    private bool firstRound = false;
    [SynchronizableField]
    private int numberToBeat = 0;
    [SynchronizableField]
    private int valueToBeat = 0;
    [SynchronizableField]
    private int winnerPlayerIndex = 0;


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
        BroadcastRemoteMethod("ChangeScene", "MainGameScene");
        yield return new WaitForSeconds(0.2f);
        foreach (User user in multiplayer.GetUsers()) InvokeRemoteMethod("PlayerSetupForGame", user.Index);
        BroadcastRemoteMethod("DeletePlayers");
        cardSpawner = GameObject.Find("CardSpawner").GetComponent<CardSpawner>();
        InvokeRemoteMethod("HostSetupForGame", settings.hostId);
        while (!readyToContinue) yield return new WaitForSeconds(0.01f);
        readyToContinue = false;
        StartCoroutine(StartFirstPhase());
    }

    private IEnumerator StartFirstPhase()
    {
        firstRound = true;
        while (cardPile.Count != 0 && localPlayer.currentHand.Count != 0)
        {
            Card card = cardPile[0];
            BroadcastRemoteMethod("SpawnCardSomewhere", card.faction, card.value, (ushort)0, 4);

            StartCoroutine(CreateRoundInfo());
            while (!roundInfoCreated) yield return new WaitForSeconds(0.1f);
            roundInfoCreated = false;

            PlayerInfo winner = DetermineWinnerFirstPhase();
            BroadcastRemoteMethod("ShowWinner", (ushort)players.IndexOf(winner));
            yield return new WaitForSeconds(3f);
            StartCoroutine(MoveCardsFirstPhase(winner.user.Index));
            yield return new WaitForSeconds(0.5f);

            // Switch order of playing
            players.Remove(winner);
            players.Insert(0, winner);
            foreach (PlayerInfo player in players) InvokeRemoteMethod("SetPlayerNumber", player.user.Index, players.IndexOf(player));

            yield return new WaitForSeconds(0.2f);
        }
        yield return new WaitForSeconds(1f);
        firstRound = false;
        SetupSecondPhase();
    }

    private void SetupSecondPhase()
    {
        BroadcastRemoteMethod("DestroyObjectForSecondPhase");
        foreach (PlayerInfo player in players) InvokeRemoteMethod("SetupForSecondPhasePlayer", player.user.Index);
        StartCoroutine(StartSecondPhase());
    }

    private IEnumerator StartSecondPhase()
    {
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < maxAmount; i++)
        {
            StartCoroutine(CreateRoundInfo());
            while (!roundInfoCreated) yield return new WaitForSeconds(0.1f);
            roundInfoCreated = false;

            PlayerInfo winner = DetermineWinnerSecondPhase();
            BroadcastRemoteMethod("ShowWinner", (ushort)players.IndexOf(winner));
            yield return new WaitForSeconds(3f);
            MoveCardsSecondPhase(winner.user.Index);
            yield return new WaitForSeconds(0.5f);


            // Switch order of playing
            players.Remove(winner);
            players.Insert(0, winner);
            foreach (PlayerInfo player in players) InvokeRemoteMethod("SetPlayerNumber", player.user.Index, players.IndexOf(player));
            yield return new WaitForSeconds(0.2f);
        }
        gameStarted = false;
        BroadcastRemoteMethod("DeletePlayerMats");
        StartCoroutine(DetermineFinalWinner());
        yield return new WaitForSeconds(20f);
        BroadcastRemoteMethod("UnloadScenes");
        ResetGame();
    }

    private void ResetGame()
    {
        numberToBeat = 0;
        valueToBeat = 0;
        winnerPlayerIndex = 0;
        cardPile = new List<Card>();
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
            roundInfo.Add(new PlayerRoundInfo(player, player.cardPlayed));
            player.cardPlayed = null;
        }
        roundInfoCreated = true;
    }

    private IEnumerator MoveCardsFirstPhase(ushort winnerIndex)
    {
        // Move all played Undead cards to the winners victory pile
        foreach (PlayerRoundInfo playerRoundInfo in roundInfo)
        {
            if (playerRoundInfo.playedCard.faction == "Undead") InvokeRemoteMethod("SpawnCardSomewhere", winnerIndex, playerRoundInfo.playedCard.faction, playerRoundInfo.playedCard.value, winnerIndex, 3);
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
        if (leadingFaction == "Goblins")
        {
            foreach (PlayerRoundInfo playerRoundInfo in roundInfo)
            {
                if (playerRoundInfo.playedCard.faction == "Knights")
                {
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
        // DEBUGGING / SINGLEPLAYER MODE
        // ushort loserIndex = winnerIndex;
        ushort loserIndex = roundInfo.FirstOrDefault(info => info.user.user.Index != winnerIndex).user.user.Index;
        {
            foreach (PlayerRoundInfo playerRoundInfo in roundInfo)
            {
                // If dwarves are played, give them to the loser
                if (playerRoundInfo.playedCard.faction == "Dwarves") InvokeRemoteMethod("SpawnCardSomewhere", loserIndex, playerRoundInfo.playedCard.faction, playerRoundInfo.playedCard.value, loserIndex, 3);
                else InvokeRemoteMethod("SpawnCardSomewhere", winnerIndex, playerRoundInfo.playedCard.faction, playerRoundInfo.playedCard.value, winnerIndex, 3);
            }
        }

        // Destroy all cards played
        BroadcastRemoteMethod("DestroyCards");
    }

    private PlayerInfo DetermineWinnerSecondPhase()
    {
        return DetermineWinnerFirstPhase();
    }

    public IEnumerator DetermineFinalWinner()
    {
        foreach (string currentFaction in settings.chosenSets)
        {
            PlayerInfo winnerPlayer = null;
            foreach (PlayerInfo player in players)
            {
                InvokeRemoteMethod("GetFactionNumbers", player.user.Index, currentFaction);
                yield return new WaitForSeconds(0.2f);
                if (player.user.Index == winnerPlayerIndex) winnerPlayer = player;
            }
            winnerPlayer.winningSets.Add(currentFaction);
        }

        ushort winnerIndex = (ushort)0;
        int winningAmount = 0;
        BroadcastRemoteMethod("ChangeScene", "VictoryScene");
        yield return new WaitForSeconds(0.2f);
        foreach (PlayerInfo player in players)
        {
            if (player.winningSets.Count > winningAmount)
                {
                    winningAmount = player.winningSets.Count;
                    winnerIndex = player.user.Index;
                }
        }

        foreach (PlayerInfo player in players)
        {
            string victoryMessage = "";
            if (player.user.Index == winnerIndex) victoryMessage = "You won! \n";
            else victoryMessage = "You lost :( \n";

            string wonSetsText = "Decks you won : ";
            foreach (string factionWon in player.winningSets) wonSetsText += factionWon + ", ";
            if (player.winningSets.Count == 0) wonSetsText += "none";
            InvokeRemoteMethod("WriteVictoryMessage", player.user.Index, victoryMessage, wonSetsText);
        }
    }

    private void GiveNextCardInDeckToBattleHand(ushort Id)
    {
        Card card = cardPile[0];
        InvokeRemoteMethod("SpawnCardSomewhere", Id, card.faction, card.value, Id, 2);
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
                InvokeRemoteMethod("SpawnCardSomewhere", player.user.Index, randomCard.faction, randomCard.value, player.user.Index, 1);
                yield return new WaitForSeconds(0.1f);
                cardPile.Remove(randomCard);
            }
        }
        readyToContinue = true;
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

    public void MoveCardToMiddle(Card cardInfo, ushort Id)
    {
        BroadcastRemoteMethod("SpawnCardSomewhere", cardInfo.faction, cardInfo.value, (ushort)(Id + 1), 4);
    }

    [SynchronizableMethod]
    public void UnloadScenes()
    {
        SceneManager.UnloadScene("MainGameScene");
        SceneManager.UnloadScene("VictoryScene");
    }
    
    [SynchronizableMethod]
    public void DeletePlayerMats()
    {
        Destroy(GameObject.Find("PlayerMat"));
    }

    [SynchronizableMethod]
    public void GetFactionNumbers(string currentFaction)
    {
        int playerNumberOfCards = 0;
        int playerHighestValue = 0;

        foreach (Card card in localPlayer.victoryPile)
        {
            if (card.faction == currentFaction) playerNumberOfCards += 1;
            if (card.value > playerHighestValue) playerHighestValue = card.value;
        }

        if (playerNumberOfCards > numberToBeat || (playerNumberOfCards == numberToBeat && playerHighestValue > valueToBeat))
        {
            numberToBeat = playerNumberOfCards;
            numberToBeat = playerHighestValue;
            winnerPlayerIndex = playerNumber;
        }
    }

    [SynchronizableMethod]
    public void WriteVictoryMessage(string victoryMessage, string wonSetsText)
    {
        GameObject.Find("VictoryText").GetComponent<TMP_Text>().text = victoryMessage;
        GameObject.Find("VictoryText").GetComponent<TMP_Text>().color = Color.white;
        GameObject.Find("WonSets").GetComponent<TMP_Text>().text = wonSetsText;
        GameObject.Find("WonSets").GetComponent<TMP_Text>().color = Color.white;
    }

    [SynchronizableMethod]
    public void ClientAddToHand(string faction, int value)
    {
        Card newCard = new Card(faction, value);
        localPlayer.addToHand(newCard);
    }

    [SynchronizableMethod]
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
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
        // Debug.Log("Are we good?? " + (settings.amountOfPeople == players.Count));
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
        cardSpawner = GameObject.Find("CardSpawner").GetComponent<CardSpawner>();
        text = getObjectWithTag(cardSpawner.transform, "playerNumber").objectOfInterest.GetComponent<TMP_Text>();
        localPlayer.SetupForGame();
    }

    [SynchronizableMethod]
    public void AllowPlayerToPlay(string name)
    {
        GameObject.Find("Player (" + name + ")").GetComponent<PlayerCode>().allowedToPlay = true;
    }

    [SynchronizableMethod]
    public void ShowWinner(ushort Id)
    {
        getObjectWithTag(cardSpawner.transform, "playedCard" + Id.ToString()).objectOfInterest.GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 0f);
    }

    [SynchronizableMethod]
    public void DestroyCards()
    {
        if (firstRound) Destroy(getObjectWithTag(cardSpawner.transform, "cardPlayingFor").objectOfInterest);
        for (int i = 0; i < multiplayer.GetUsers().Count; i++)
        {
            Destroy(getObjectWithTag(cardSpawner.transform, "playedCard" + i.ToString()).objectOfInterest);
        }
    }

    [SynchronizableMethod]
    public void SetPlayerNumber(ushort number)
    {
        playerNumber = number;
        if (text == null) text = getObjectWithTag(cardSpawner.transform, "playerNumber").objectOfInterest.GetComponent<TMP_Text>();
        text.text = "Player " + (number + 1).ToString();
        getObjectWithTag(cardSpawner.transform, "playerMat").objectOfInterest.GetComponent<SpriteRenderer>().color = playerColours[number];
    }

    [SynchronizableMethod]
    public void DestroyObjectForSecondPhase()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject objectGameOf = getObjectWithTag(cardSpawner.transform, "destroyable" + i.ToString()).objectOfInterest;
            if (objectGameOf != null) Destroy(objectGameOf);
        }
        Transform victoryPileTrans = GameObject.Find("Victory pile").transform;
        victoryPileTrans.localScale = new Vector3(0.307f, 2f, 1);
        victoryPileTrans.position = new Vector3(7.07f, 0.7f, 0);
    }

    [SynchronizableMethod]
    public void SpawnCardSomewhere(string faction, int value, ushort Id, int place)
    {
        if (place == 1) cardSpawner.spawnCardInHand(new Card(faction, value), Id);
        else if (place == 2) cardSpawner.spawnCardInBattleHand(new Card(faction, value), Id);
        else if (place == 3) cardSpawner.spawnCardInVictoryHand(new Card(faction, value), Id);
        else if (place == 4) cardSpawner.spawnCardInMiddle(new Card(faction, value), Id);
    }

    [SynchronizableMethod]
    public void SetupForSecondPhasePlayer()
    {
        localPlayer.currentHand = new List<Card>();
        foreach (Card card in localPlayer.armyPile)
        {
            SpawnCardSomewhere(card.faction, card.value, playerNumber, 1);
            card.DestroyCard();
        }
        localPlayer.armyPile = new List<Card>();
    }
}
