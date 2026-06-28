using UnityEngine;

public class NPC_Dialogue : MonoBehaviour
{
    private enum DialogueState
    {
        Opening,
        FirstChoice,
        FirstChoiceResponse,
        SecondChoice,
        SecondChoiceResponse,
        PostBattle,
        PostBattleChoice,
        PostBattleResponse,
        SplitDialogue,
        Finished
    }

    [Header("Bubble")]
    [SerializeField] private Dialogue_Bubble dialogueBubble;

    [Header("Opening Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string[] openingLines;

    [Header("First Question")]
    [SerializeField] private string firstQuestion = "Siapa namamu?";
    [SerializeField] private string firstOption1 = "Namaku Yobel.";
    [SerializeField] private string firstOption2 = "Aku lebih suka merahasiakannya.";

    [Header("First Option 1 Response")]
    [TextArea(2, 4)]
    [SerializeField] private string[] firstOption1ResponseLines;

    [Header("First Option 2 Response")]
    [TextArea(2, 4)]
    [SerializeField] private string[] firstOption2ResponseLines;

    [Header("Second Question")]
    [SerializeField] private string secondQuestion = "Maukah kau membantuku?";
    [SerializeField] private string secondOption1 = "";
    [SerializeField] private string secondOption2 = "";

    [Header("Second Option 1 Response")]
    [TextArea(2, 4)]
    [SerializeField] private string[] secondOption1ResponseLines;

    [Header("Second Option 2 Response")]
    [TextArea(2, 4)]
    [SerializeField] private string[] secondOption2ResponseLines;

    [Header("Post-Battle Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string[] postBattleLines;
    [SerializeField] private string postBattleQuestion = "Terima kasih telah membantuku!";
    [SerializeField] private string postBattleOption1 = "Itu tanggung jawabku.";
    [SerializeField] private string postBattleOption2 = "Saatnya kita pergi.";
    [TextArea(2, 4)]
    [SerializeField] private string[] postBattleOption1ResponseLines;
    [TextArea(2, 4)]
    [SerializeField] private string[] postBattleOption2ResponseLines;

    [Header("Split Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string[] splitLines;

    [Header("After Dialogue")]
    [SerializeField] private NPC_Follower npcFollower;
    [SerializeField] private bool startFollowAfterDialogue;

    private DialogueState currentState;
    private string[] currentLines;
    private int currentLineIndex;
    private bool hasFinishedDialogue;
    private bool hasShownPostBattleDialogue;
    private bool hasShownSplitDialogue;
    private float autoAdvanceTimer;
    private float autoAdvanceDelay = 2.5f; // seconds between auto-advance
    private bool isAutoAdvancing;

    private void Awake()
    {
        dialogueBubble.OptionButton1.onClick.AddListener(ChooseOption1);
        dialogueBubble.OptionButton2.onClick.AddListener(ChooseOption2);

        dialogueBubble.HideBubble();
    }

    private void Update()
    {
        if (!isAutoAdvancing)
            return;

        autoAdvanceTimer += Time.deltaTime;
        if (autoAdvanceTimer >= autoAdvanceDelay)
        {
            autoAdvanceTimer = 0f;
            ShowNextLine();
        }
    }

    public void StartDialogue()
    {
        if (hasFinishedDialogue)
        {
            return;
        }

        currentState = DialogueState.Opening;
        SetDialogueLines(openingLines);
    }

    public bool CanStartDialogue()
    {
        return !hasFinishedDialogue;
    }

    public void StartPostBattleDialogue()
    {
        if (hasShownPostBattleDialogue)
        {
            return;
        }

        hasShownPostBattleDialogue = true;
        currentState = DialogueState.PostBattle;
        isAutoAdvancing = true;
        autoAdvanceTimer = 0f;
        SetDialogueLines(postBattleLines);
    }

    public bool CanStartPostBattleDialogue()
    {
        return !hasShownPostBattleDialogue;
    }

    public void StartSplitDialogue()
    {
        if (hasShownSplitDialogue)
            return;

        hasShownSplitDialogue = true;
        currentState = DialogueState.SplitDialogue;
        isAutoAdvancing = true;
        autoAdvanceTimer = 0f;

        if (splitLines == null || splitLines.Length == 0)
        {
            splitLines = new string[]
            {
                "Village ada di depan. Ikuti jalur ini, ya.",
                "Ayo, kita lanjut jalan bareng sampai di sana."
            };
        }

        SetDialogueLines(splitLines);
    }

    public bool CanStartSplitDialogue()
    {
        return !hasShownSplitDialogue;
    }

    public bool IsDialogueFinished()
    {
        return currentState == DialogueState.Finished;
    }

    private void SetDialogueLines(string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            FinishDialogue();
            return;
        }

        currentLines = lines;
        currentLineIndex = 0;

        ShowCurrentLine();
    }

    public bool ShowNextLine()
    {
        if (currentState == DialogueState.Finished)
        {
            return false;
        }

        if (IsWaitingForChoice())
        {
            return true;
        }

        if (currentLines == null || currentLines.Length == 0)
        {
            FinishDialogue();
            return false;
        }

        currentLineIndex++;

        if (currentLineIndex >= currentLines.Length)
        {
            HandleCurrentDialogueFinished();
            return currentState != DialogueState.Finished;
        }

        ShowCurrentLine();
        return true;
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null || currentLines.Length == 0)
        {
            FinishDialogue();
            return;
        }

        if (currentLineIndex < 0 || currentLineIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        dialogueBubble.ShowDialogueText(currentLines[currentLineIndex]);
    }

    private void HandleCurrentDialogueFinished()
    {
        if (currentState == DialogueState.Opening)
        {
            currentState = DialogueState.FirstChoice;
            dialogueBubble.ShowChoices(firstQuestion, firstOption1, firstOption2);
            return;
        }

        if (currentState == DialogueState.FirstChoiceResponse)
        {
            currentState = DialogueState.SecondChoice;
            dialogueBubble.ShowChoices(secondQuestion, secondOption1, secondOption2);
            return;
        }

        if (currentState == DialogueState.SecondChoiceResponse)
        {
            FinishDialogue();
        }

        if (currentState == DialogueState.PostBattle)
        {
            currentState = DialogueState.PostBattleChoice;
            isAutoAdvancing = false; // Stop auto-advance when showing choices
            dialogueBubble.ShowChoices(postBattleQuestion, postBattleOption1, postBattleOption2);
            return;
        }

        if (currentState == DialogueState.PostBattleResponse)
        {
            FinishDialogue();
        }

        if (currentState == DialogueState.SplitDialogue)
        {
            FinishDialogue();
            return;
        }
    }

    private void ChooseOption1()
    {
        dialogueBubble.HideChoices();

        if (currentState == DialogueState.FirstChoice)
        {
            currentState = DialogueState.FirstChoiceResponse;
            SetDialogueLines(firstOption1ResponseLines);
            return;
        }

        if (currentState == DialogueState.SecondChoice)
        {
            currentState = DialogueState.SecondChoiceResponse;
            SetDialogueLines(secondOption1ResponseLines);
            return;
        }

        if (currentState == DialogueState.PostBattleChoice)
        {
            currentState = DialogueState.PostBattleResponse;
            isAutoAdvancing = true; // Re-enable auto-advance for response
            autoAdvanceTimer = 0f;
            SetDialogueLines(postBattleOption1ResponseLines);
        }
    }

    private void ChooseOption2()
    {
        dialogueBubble.HideChoices();

        if (currentState == DialogueState.FirstChoice)
        {
            currentState = DialogueState.FirstChoiceResponse;
            SetDialogueLines(firstOption2ResponseLines);
            return;
        }

        if (currentState == DialogueState.SecondChoice)
        {
            currentState = DialogueState.SecondChoiceResponse;
            SetDialogueLines(secondOption2ResponseLines);
            return;
        }

        if (currentState == DialogueState.PostBattleChoice)
        {
            currentState = DialogueState.PostBattleResponse;
            isAutoAdvancing = true; // Re-enable auto-advance for response
            autoAdvanceTimer = 0f;
            SetDialogueLines(postBattleOption2ResponseLines);
        }
    }

    private bool IsWaitingForChoice()
    {
        return currentState == DialogueState.FirstChoice ||
            currentState == DialogueState.SecondChoice ||
            currentState == DialogueState.PostBattleChoice;
    }

    private void FinishDialogue()
    {
        hasFinishedDialogue = true;
        isAutoAdvancing = false;

        bool shouldFollow = startFollowAfterDialogue &&
            npcFollower != null &&
            currentState != DialogueState.SplitDialogue;

        currentState = DialogueState.Finished;
        dialogueBubble.HideBubble();

        if (shouldFollow)
        {
            npcFollower.StartFollowing();
        }
    }
}