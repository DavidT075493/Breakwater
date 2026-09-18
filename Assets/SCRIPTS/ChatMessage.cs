using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatMessage : MonoBehaviour
{
    public TextMeshProUGUI senderText, messageText;
    [SerializeField] CanvasGroup group;

    private void Start()
    {
        group.DOFade(1, 0.2f);
        transform.localScale = new Vector2(0.7f,0.5f);
        transform.DOScale(Vector3.one, 0.3f);
    }

}
