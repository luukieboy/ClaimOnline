using UnityEngine;
using Alteruna;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerCode : AttributesSync
{
    public GameObject cardPrefab;
    public PlayerInfo playerInfo;
    public bool allowedToPlay = false;

    private GameManager gameManager;
    private Alteruna.Avatar avatar;
    private Transform camera;
    private bool setupDone;
    private Multiplayer multi;
    private List<Transform> touchedObjects = new List<Transform>();
    private CardSpawner cardSpawner;

    void Start()
    {
        avatar = GetComponent<Alteruna.Avatar>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        multi = gameManager.multiplayer;
        if (avatar.Owner == multi.Me)
        {
            setupDone = false;
            playerInfo = new PlayerInfo(avatar.Owner);
            gameManager.localPlayer = playerInfo;
            InvokeRemoteMethod("AddToPlayerList", gameManager.settings.hostId);
        }
    }

    void Update()
    {
        if (multi == null) multi = gameManager.multiplayer;
        if (avatar.Owner == multi.Me)
        {
            if (gameManager.gameStarted)
            {
                if (!setupDone)
                {
                    multi = gameManager.settings.multiplayer;
                    setupDone = true;
                }

                Vector2 mousePosition = GameObject.Find("secondCamera").GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
                Vector2 direction = Vector2.zero; // zero vector for point-check (click/hover detection)
                RaycastHit2D hit = Physics2D.Raycast(mousePosition, direction);

                if (hit.collider != null)
                {
                    if (hit.collider.CompareTag("Card"))
                    {
                        hit.collider.transform.localScale = new Vector3(3.5f, 5, 1);
                        // highlight card, trigger animation, etc.
                        if (touchedObjects.Count > 0 && touchedObjects[touchedObjects.Count - 1].position != hit.collider.transform.position)
                        {
                            resetHits();
                        }
                        if (playerInfo.cardPlayed == null || (playerInfo.cardPlayed != null && playerInfo.cardPlayed != hit.collider.gameObject))
                        {
                            touchedObjects.Add(hit.collider.transform);
                        }
                    }
                }
                else
                {
                    resetHits();
                }

                if (hit.collider != null && Input.GetMouseButtonDown(0) && hit.collider.CompareTag("Card") && allowedToPlay)
                {
                    if (playerInfo.cardPlayed != null) touchedObjects.Add(playerInfo.cardPlayed.transform);
                    playerInfo.cardPlayed = hit.collider.gameObject;
                    Card card = playerInfo.cardPlayed.GetComponent<CardDisplay>().card;
                    gameManager.cardSpawner.MoveCardToMiddle(gameManager.playerNumber, playerInfo.cardPlayed);
                    while (touchedObjects.Contains(hit.collider.transform))
                    {
                        touchedObjects.Remove(hit.collider.transform);
                    }
                    allowedToPlay = false;
                }
            }
        }
    }

    private void resetHits()
    {
        foreach (Transform touchy in touchedObjects)
        {
            touchy.localScale = new Vector3(3, 4, 1);
            touchedObjects = new List<Transform>();
        }
    }

    [SynchronizableMethod]
    public void AddToPlayerList()
    {
        PlayerInfo newPlayerInfo = new PlayerInfo(avatar.Owner);
        gameManager.players.Add(newPlayerInfo);
    }
}
