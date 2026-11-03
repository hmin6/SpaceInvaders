using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Shows a conversation inside a fixed area: each line gets a small speaker label (the Pod's in orange)
// and types out after the previous one finishes. Lines that don't fit go on further pages.
// NierScreens drives it: CompletePage() on any key, NextPage() on Space/Enter.
public class DialogueView : MonoBehaviour
{
    private class LineUI
    {
        public RectTransform root;
        public TextMeshProUGUI label;
        public TextMeshProUGUI text;
        public TypewriterText typer;
    }

    private const float LabelHeight = 24f;
    private const float LabelGap = 2f;
    private const float BlockGap = 14f;

    private RectTransform area;
    private TMP_FontAsset font;
    private float fontSize;
    private string operatorName = "Operator";
    private string podName = "Pod";

    private readonly List<LineUI> pool = new List<LineUI>();
    private readonly List<DialogueLine> lines = new List<DialogueLine>();
    private readonly List<int> pageStarts = new List<int>();
    private int page;
    private int current; // index into lines of the line that's typing

    public float TypingSpeed { get; set; } = 45f;

    public bool HasNextPage => page < pageStarts.Count - 1;
    public bool IsPageComplete => lines.Count == 0 || (current >= PageEnd(page) - 1 && LineFor(current).typer.IsComplete);
    public bool IsFinished => IsPageComplete && !HasNextPage;

    public static DialogueView Create(Transform parent, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
    {
        RectTransform rect = NierStyle.CreateRect("Dialogue", parent);
        NierStyle.Place(rect, new Vector2(.5f, .5f), position, size);
        DialogueView view = rect.gameObject.AddComponent<DialogueView>();
        view.area = rect;
        view.fontSize = fontSize;
        view.font = font;
        return view;
    }

    public void SetNames(string operatorLabel, string podLabel)
    {
        operatorName = operatorLabel;
        podName = podLabel;
    }

    public void Show(IReadOnlyList<DialogueLine> conversation)
    {
        lines.Clear();
        if (conversation != null)
        {
            foreach (DialogueLine line in conversation)
            {
                if (!string.IsNullOrEmpty(line.text))
                {
                    lines.Add(line);
                }
            }
        }
        Paginate();
        ShowPage(0);
    }

    // Shows the rest of the current page at once
    public void CompletePage()
    {
        int end = PageEnd(page);
        for (int i = current; i < end; i++)
        {
            if (i > current)
            {
                StartLine(i);
            }
            LineFor(i).typer.Complete();
        }
        current = Mathf.Max(current, end - 1);
    }

    public void NextPage()
    {
        if (HasNextPage)
        {
            ShowPage(page + 1);
        }
    }

    void Update()
    {
        // When a line finishes typing, start the next one on this page
        if (lines.Count == 0 || current >= PageEnd(page) - 1 || !LineFor(current).typer.IsComplete)
        {
            return;
        }
        current++;
        StartLine(current);
    }

    // ---------- Pages ----------

    void Paginate()
    {
        pageStarts.Clear();
        pageStarts.Add(0);
        float used = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            float height = BlockHeight(lines[i].text);
            if (used > 0 && used + height > area.rect.height)
            {
                pageStarts.Add(i);
                used = 0;
            }
            used += height + BlockGap;
        }
    }

    int PageEnd(int pageIndex)
    {
        return pageIndex + 1 < pageStarts.Count ? pageStarts[pageIndex + 1] : lines.Count;
    }

    void ShowPage(int pageIndex)
    {
        page = pageIndex;
        foreach (LineUI ui in pool)
        {
            ui.root.gameObject.SetActive(false);
        }
        if (lines.Count == 0)
        {
            current = 0;
            return;
        }

        int start = pageStarts[page];
        int end = PageEnd(page);
        float y = 0;
        for (int i = start; i < end; i++)
        {
            LineUI ui = LineFor(i);
            float height = BlockHeight(lines[i].text);
            ui.root.gameObject.SetActive(true);
            ui.root.anchoredPosition = new Vector2(0, -y);
            ui.root.sizeDelta = new Vector2(0, height);

            bool pod = lines[i].speaker == DialogueSpeaker.Pod;
            ui.label.text = pod ? podName : operatorName;
            ui.label.color = pod ? NierStyle.Accent : NierStyle.Ink;
            ui.label.enabled = false; // shown when the line starts typing
            ui.typer.Play("");
            y += height + BlockGap;
        }

        current = start;
        StartLine(current);
    }

    void StartLine(int index)
    {
        LineUI ui = LineFor(index);
        ui.label.enabled = true;
        ui.typer.CharactersPerSecond = TypingSpeed;
        ui.typer.Play(lines[index].text);
    }

    float BlockHeight(string text)
    {
        LineUI measure = GetPooled(0);
        return LabelHeight + LabelGap + measure.text.GetPreferredValues(text, area.rect.width, 0).y;
    }

    // ---------- Line objects ----------

    LineUI LineFor(int lineIndex)
    {
        return GetPooled(lineIndex - pageStarts[page]);
    }

    LineUI GetPooled(int slot)
    {
        while (pool.Count <= slot)
        {
            pool.Add(CreateLine());
        }
        return pool[slot];
    }

    LineUI CreateLine()
    {
        LineUI ui = new LineUI();
        ui.root = NierStyle.CreateRect("Line", area);
        ui.root.anchorMin = new Vector2(0, 1);
        ui.root.anchorMax = new Vector2(1, 1);
        ui.root.pivot = new Vector2(.5f, 1);

        ui.label = NierStyle.CreateText("Speaker", ui.root, "", 18, NierStyle.Ink, TextAlignmentOptions.TopLeft, font, 8);
        RectTransform labelRect = ui.label.rectTransform;
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(.5f, 1);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0, LabelHeight);

        ui.text = NierStyle.CreateText("Text", ui.root, "", fontSize, NierStyle.InkDark, TextAlignmentOptions.TopLeft, font, 2, false);
        ui.text.textWrappingMode = TextWrappingModes.Normal;
        ui.text.lineSpacing = 8;
        RectTransform textRect = ui.text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = new Vector2(0, -(LabelHeight + LabelGap));
        ui.typer = ui.text.gameObject.AddComponent<TypewriterText>();

        ui.root.gameObject.SetActive(false);
        return ui;
    }
}
