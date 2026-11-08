using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Dialogue/Dialogue Part")]
public class DialogueSO : ScriptableObject
{
    public DialogueNode[] dialogueNodes;

    public ChoiceOption[] choiceOptions;
}

[System.Serializable]
public class DialogueNode
{
    public string name;
    [TextArea(3, 5)]
    public string sentence;
}

[System.Serializable]
public class ChoiceOption
{
    public string optionText;
    public DialogueSO nextDialogue;
}
