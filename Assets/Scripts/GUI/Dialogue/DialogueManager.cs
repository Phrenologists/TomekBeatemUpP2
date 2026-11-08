using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    //Dialogues are based on scriptable objects (Dialogue/Dialogue Part).
    //Every dialogue node ends when a choice is presented or when dialogue is supposed to end.
    //Choice can start another dialogue node or finish the dialogue.
    //
    //Instruction:
    //1. Create first dialogue node SO.
    //2. Write dialogue lines with corresponding names.
    //3. Write choices for player (skip if last dialogue node).
    //4. Create next dialogue nodes, drag them as nodes activated by choices (choice without a set node will end dialogue).
    //5. Repeat for all nodes of this interaction.
    //6. Add a trigger to an object.
    //7. Set first dialogue node of this dialogue as beginning.

    [SerializeField] private UIManager uiManager;

    [Header("Dialogue window")]
    //[SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextSentenceButton;

    [Header("Choices window")]
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private GameObject buttonsParent;
    [SerializeField] private GameObject choiceOptionButtonPrefab;

    private Queue<string> names;
    private Queue<string> sentences;
    private Dictionary<string, DialogueSO> playerChoices;
    private bool isLast;

    // Start is called before the first frame update
    void Start()
    {
        names = new Queue<string>();
        sentences = new Queue<string>();
        playerChoices = new Dictionary<string, DialogueSO>();
        choicesPanel.SetActive(false);
    }

    public void StartDialogue (DialogueSO dialogue)
    {
        names.Clear();
        sentences.Clear();
        playerChoices.Clear();

        foreach (Transform child in buttonsParent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        foreach (DialogueNode node in dialogue.dialogueNodes)
        {
            names.Enqueue(node.name);
            sentences.Enqueue(node.sentence);
        }

        foreach (ChoiceOption option in dialogue.choiceOptions)
        {
            playerChoices.Add(option.optionText, option.nextDialogue);
        }

        if (playerChoices.Count > 0) isLast = false;
        else isLast = true;

        uiManager.ShowDialogueUI();
        nextSentenceButton.interactable = true;

        DisplayNextNode();
    }

    public void DisplayNextNode()
    {
        if (sentences.Count == 0)
        {
            if (isLast)
            {
                EndDialogue();
                return;
            }
            else
            {
                ShowPlayerChoices();
                return;
            }
        }

        string currentName = names.Dequeue();
        string currentSentence = sentences.Dequeue();

        nameText.text = currentName;
        dialogueText.text = currentSentence;

    }

    public void ShowPlayerChoices()
    {
        nextSentenceButton.interactable = false;
        
        foreach (var key in playerChoices.Keys)
        {
            GameObject optionButton = Instantiate(choiceOptionButtonPrefab, buttonsParent.transform);

            optionButton.GetComponentInChildren<TMP_Text>().text = key;
            if (playerChoices[key] != null )
            {
                optionButton.GetComponent<Button>().onClick.AddListener(() => PlayerChoice(key));
            }
            else
            {
                optionButton.GetComponent<Button>().onClick.AddListener(EndDialogue);
            }
        }



        choicesPanel.SetActive(true);
    }

    public void PlayerChoice(string choiceName)
    {
        if (playerChoices[choiceName] == null)
        {
            EndDialogue();
        }
        else
        {
            choicesPanel.SetActive(false);
            StartDialogue(playerChoices[choiceName]);
        }
    }

    public void EndDialogue ()
    {
        uiManager.ShowBattleUI();
    }
}
