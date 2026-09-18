using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class KK_InputField : TMP_InputField
{
    enum CaseTransformation { none, toLower, toUpper }

    Coroutine inputChecker;

    protected TMP_Text placeholderText;
    protected string fallBackText; //Standard text displayed in placeholder
    protected Color32 fallBackTextColor; //Standard Color for placeholder Text

    Selectable nextField;

    [SerializeField]
    CaseTransformation caseTransformation = CaseTransformation.none;

    protected override void Awake()
    {
        base.Awake();
        placeholderText = placeholder.GetComponent<TMP_Text>();
        fallBackText = placeholderText.text;
        fallBackTextColor = placeholderText.color;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        onSelect.AddListener(OnSelect);
        onValueChanged.AddListener(OnValueChanged);
        onDeselect.AddListener((string s) => onSubmit?.Invoke(s));
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        onSelect.RemoveListener(OnSelect);
        onValueChanged.RemoveListener(OnValueChanged);
        onDeselect.RemoveAllListeners();
        if (inputChecker != null) StopCoroutine(inputChecker);
    }

    protected virtual void OnSelect(string s)
    {
        inputChecker = StartCoroutine(CustomInputCheck());
        nextField = GetComponent<Selectable>().FindSelectableOnRight();
        if (nextField == null || nextField.gameObject.activeSelf == false) nextField = GetComponent<Selectable>().FindSelectableOnDown();
    }
    void OnValueChanged(string s)
    {
        onValueChanged.RemoveListener(OnValueChanged);
        if (caseTransformation == CaseTransformation.none) return;
        if (caseTransformation == CaseTransformation.toUpper) text = text.ToUpper();
        else text = text.ToLower();
        onValueChanged.AddListener(OnValueChanged);
    }
    IEnumerator CustomInputCheck()
    {
        yield return new WaitForSeconds(0.2f);

        while (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            while (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Backspace))
            {
                if (string.IsNullOrEmpty(text)) break;
                text = TrimString(text);

                yield return new WaitForSeconds(0.2f);
            }

            if ((Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Return)) && TabOrEnter()) yield break;

            yield return null;
        }
    }

    public virtual bool TabOrEnter()
    {
        onSubmit?.Invoke(text);
        if (nextField == null || nextField.gameObject.activeSelf == false) return false;
        TMP_InputField inputField = nextField.GetComponent<TMP_InputField>();
        if (inputField != null) inputField.OnPointerClick(new PointerEventData(EventSystem.current));
        EventSystem.current.SetSelectedGameObject(nextField.gameObject, new BaseEventData(EventSystem.current));
        return true;
    }

    string TrimString(string s)
    {
        if (s[s.Length - 1] == ' ') s.Trim();

        string returnString;

        if (s.Contains(" "))
        {
            returnString = s.Substring(0, s.LastIndexOf(' '));
        }
        else
        {
            returnString = "";
        }

        return returnString;
    }
}
