using System;
using UnityEngine;

public enum DialogueSpeaker { Operator, Pod }

// One line of a briefing, debrief, failure or certification conversation
[Serializable]
public struct DialogueLine
{
    public DialogueSpeaker speaker;
    [TextArea(1, 4)]
    public string text;

    public DialogueLine(DialogueSpeaker speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }

    public static DialogueLine Operator(string text) => new DialogueLine(DialogueSpeaker.Operator, text);
    public static DialogueLine Pod(string text) => new DialogueLine(DialogueSpeaker.Pod, text);
}
