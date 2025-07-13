using UnityEngine;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public Card card;

    private TMP_Text text;
    private Vector3 position;

    public void initialize(Card cardInfo)
    {
        card = cardInfo;
        text = gameObject.GetComponentInChildren<TMP_Text>();
        position = gameObject.GetComponent<Transform>().position;
        text.text = cardInfo.faction + " : " + cardInfo.value.ToString();
    }

    public void DestroyCard()
    {
        Destroy(gameObject);
    }
}
