using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Dialogue_Bubble : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text questionText;

    [Header("Choice UI")]
    [SerializeField] private GameObject choicePanel;

    [SerializeField] private Button optionButton1;
    [SerializeField] private Button optionButton2;

    [SerializeField] private TMP_Text optionButton1Text;
    [SerializeField] private TMP_Text optionButton2Text;

    public Button OptionButton1 => optionButton1;
    public Button OptionButton2 => optionButton2;

    public void ShowDialogueText(string message)
    {
        if (dialogueText.transform.parent != null)
        {
            dialogueText.transform.parent.gameObject.SetActive(true);
        }

        dialogueText.text = message;
        questionText.text = "";

        choicePanel.SetActive(false);
        gameObject.SetActive(true);
    }

    public void HideChoices()
    {
        questionText.text = "";
        choicePanel.SetActive(false);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void HideBubble()
    {
        dialogueText.text = "";
        questionText.text = "";

        if (dialogueText.transform.parent != null)
        {
            dialogueText.transform.parent.gameObject.SetActive(false);
        }

        choicePanel.SetActive(false);
        gameObject.SetActive(false);
    }

    public void ShowChoices(string question, string option1, string option2)
    {
        dialogueText.text = "";
        questionText.text = question;

        // Hide bubble background/text saat pilihan muncul
        if (dialogueText.transform.parent != null)
        {
            dialogueText.transform.parent.gameObject.SetActive(false);
        }

        optionButton1Text.text = option1;
        optionButton1.gameObject.SetActive(true);

        if (string.IsNullOrEmpty(option2))
        {
            optionButton2.gameObject.SetActive(false);
        }
        else
        {
            optionButton2Text.text = option2;
            optionButton2.gameObject.SetActive(true);
        }

        choicePanel.SetActive(true);
        gameObject.SetActive(true);
    }
}