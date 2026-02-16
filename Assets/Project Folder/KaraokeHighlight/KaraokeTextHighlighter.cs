using System;
using System.Text;
using UnityEngine;
using TMPro;
using NaughtyAttributes;

[DisallowMultipleComponent]
public class KaraokeTextHighlighter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TMP text to display the highlighted words.")]
    public TextMeshPro textComponent;

    [Tooltip("AudioSource that plays the corresponding audio.")]
    public AudioSource audioSource;

    [Tooltip("JSON file with 'words' and 'wordTimes' exported from Python.")]
    public TextAsset wordTimesJson;

    [Header("Playback")]
    [Tooltip("If true, playback starts automatically in Start() when data is loaded.")]
    public bool playOnStart = false;

    [Tooltip("Use AudioSettings.dspTime + PlayScheduled for more stable timing.")]
    public bool useDspTime = true;

    [Tooltip("Lead time (seconds) before scheduled playback starts when useDspTime is true.")]
    public float scheduleLeadTime = 0.1f;

    [Tooltip("Global timing offset for the text. Positive = text later, negative = text earlier.")]
    public float globalOffset = 0.0f;

    [Header("Colors")]
    public Color pastColor = new Color(0.7f, 0.7f, 0.7f);
    public Color currentColor = Color.yellow;
    public Color futureColor = Color.white;

    // Internal data
    private string[] _words;
    private float[] _wordTimes;
    private int _currentWordIndex = -1;
    private bool _isLoaded = false;
    private bool _isPlaying = false;
    private double _dspStartTime = 0.0;

    // Optional metadata
    private string _fullText;
    private string _language;
    private float _durationSeconds;

    /// <summary>Cached start index of each word in _fullText; -1 if not found. Used to preserve tabs/newlines when rebuilding.</summary>
    private int[] _wordStartIndices;
    /// <summary>Length of each word's match in _fullText (may be less than word length when JSON has trailing punctuation not in text).</summary>
    private int[] _wordLengthsInText;

    /// <summary>Spaces used to represent a tab when building display text. TMP often renders \t as 0 width if the font asset has no Tab Width; this makes indents visible.</summary>
    private const int SpacesPerTab = 4;

    /// <summary>Replace tab characters with spaces so indents are visible in TMP when the font asset has no Tab Width.</summary>
    private static string ExpandTabs(string segment)
    {
        if (string.IsNullOrEmpty(segment) || segment.IndexOf('\t') < 0)
            return segment;
        return segment.Replace("\t", new string(' ', SpacesPerTab));
    }

    [Serializable]
    private class WordTimingData
    {
        public string text;
        public string language;
        public float duration;
        public string[] words;
        public float[] wordTimes;
    }

    private void Awake()
    {
        if (textComponent == null)
        {
            Debug.LogError($"[KaraokeTextHighlighter] No TextMeshPro component assigned on {name}.");
        }

        if (audioSource == null)
        {
            Debug.LogError($"[KaraokeTextHighlighter] No AudioSource assigned on {name}.");
        }

        if (wordTimesJson != null)
        {
            LoadFromJson(wordTimesJson.text);
        }
        else
        {
            Debug.LogWarning($"[KaraokeTextHighlighter] No wordTimesJson assigned on {name}.");
        }
    }

    private void Start()
    {
        if (playOnStart && _isLoaded && audioSource != null && audioSource.clip != null)
        {
            StartPlayback();
        }
    }

    private void Update()
    {
        if (!_isPlaying || !_isLoaded || audioSource == null)
            return;

        float t = GetPlaybackTime() + globalOffset;

        if (t < 0f)
            return;

        int newIndex = ComputeWordIndexForTime(t);

        if (newIndex != _currentWordIndex)
        {
            _currentWordIndex = newIndex;
            RebuildTextWithHighlight();
        }
    }

    /// <summary>
    /// Load the word timing data from a JSON string (as exported from Python).
    /// </summary>
    /// <param name="json">JSON string with text, words, wordTimes.</param>
    /// <param name="debugLabel">Optional label for [DebugDynamicTexts] logs (e.g. "LegalScenario", "AttorneyStatement").</param>
    public void LoadFromJson(string json, string debugLabel = null)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[KaraokeTextHighlighter] Empty JSON string.");
            return;
        }

        WordTimingData data;
        try
        {
            data = JsonUtility.FromJson<WordTimingData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[KaraokeTextHighlighter] Failed to parse JSON: {e}");
            return;
        }

        if (data == null || data.words == null || data.wordTimes == null)
        {
            Debug.LogError("[KaraokeTextHighlighter] JSON missing 'words' or 'wordTimes'.");
            return;
        }

        if (data.words.Length != data.wordTimes.Length)
        {
            Debug.LogError($"[KaraokeTextHighlighter] words and wordTimes length mismatch: " +
                           $"{data.words.Length} vs {data.wordTimes.Length}");
            return;
        }

        _words = data.words;
        _wordTimes = data.wordTimes;
        _fullText = string.IsNullOrEmpty(data.text) ? string.Join(" ", _words) : data.text;
        _language = data.language;
        _durationSeconds = data.duration;

        ComputeWordStartIndices(_fullText, _words, out _wordStartIndices, out _wordLengthsInText);

        if (textComponent != null)
        {
            textComponent.text = _fullText;
        }

        _currentWordIndex = -1;
        _isLoaded = true;

        // Initial render with all words in "future" color
        RebuildTextWithHighlight();

        if (ShouldLogDynamicTexts())
            LogDynamicTextStages(string.IsNullOrEmpty(debugLabel) ? gameObject.name : debugLabel);
    }

    private static bool ShouldLogDynamicTexts() =>
        SceneReferencer.Instance != null && SceneReferencer.Instance.DebugLogDynamicTexts;

    private void LogDynamicTextStages(string label)
    {
        bool useFull = !string.IsNullOrEmpty(_fullText) && _wordStartIndices != null && _wordLengthsInText != null
            && _wordStartIndices.Length == _words.Length && _wordLengthsInText.Length == _words.Length
            && Array.TrueForAll(_wordStartIndices, i => i >= 0) && Array.TrueForAll(_wordLengthsInText, i => i > 0);
        string finalText = textComponent != null ? textComponent.text : "(no TMP)";
        Debug.Log($"[DebugDynamicTexts] {label} JSON text ({_fullText?.Length ?? 0} chars). Has \\n: {_fullText?.Contains("\n") ?? false}, Has \\t: {_fullText?.Contains("\t") ?? false}\n{Repr(_fullText)}");
        Debug.Log($"[DebugDynamicTexts] {label} TIMINGS: words={_words?.Length ?? 0}, duration={_durationSeconds:F2}s, useFullTextLayout={useFull}");
        Debug.Log($"[DebugDynamicTexts] {label} FINAL TMP ({finalText?.Length ?? 0} chars). Has \\n: {finalText?.Contains("\n") ?? false}, Has \\t: {finalText?.Contains("\t") ?? false}\n{Repr(finalText)}");
    }

    private static string Repr(string s)
    {
        if (s == null) return "(null)";
        return s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }

    /// <summary>
    /// Start playback and begin highlighting words.
    /// If useDspTime is true, uses PlayScheduled for more accurate sync.
    /// </summary>
    [Button]
    public void StartPlayback()
    {
        Debug.Log("[KaraokeTextHighlighter] Starting playback.");
        if (!_isLoaded)
        {
            Debug.LogWarning("[KaraokeTextHighlighter] Cannot start playback; JSON data not loaded.");
            return;
        }

        if (audioSource == null || audioSource.clip == null)
        {
            Debug.LogWarning("[KaraokeTextHighlighter] Cannot start playback; missing AudioSource or clip.");
            return;
        }

        _currentWordIndex = -1;
        RebuildTextWithHighlight();

        if (useDspTime)
        {
            _dspStartTime = AudioSettings.dspTime + scheduleLeadTime;
            audioSource.PlayScheduled(_dspStartTime);
        }
        else
        {
            audioSource.Play();
            _dspStartTime = AudioSettings.dspTime; // reference for non-scheduled mode
        }

        _isPlaying = true;
    }

    /// <summary>
    /// Stop playback and highlighting.
    /// </summary>
    public void StopPlayback()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        _isPlaying = false;
    }

    /// <summary>
    /// Get the current playback time in seconds relative to audio start,
    /// using dspTime (if enabled) or AudioSource.time.
    /// </summary>
    private float GetPlaybackTime()
    {
        if (audioSource == null)
            return 0f;

        if (useDspTime)
        {
            double now = AudioSettings.dspTime;
            return (float)(now - _dspStartTime);
        }
        else
        {
            return audioSource.time;
        }
    }

    /// <summary>
    /// Given a time t (seconds), find the index of the current word.
    /// </summary>
    private int ComputeWordIndexForTime(float t)
    {
        if (_wordTimes == null || _wordTimes.Length == 0)
            return -1;

        // If before the first word, we can return -1 (no current) or 0.
        if (t < _wordTimes[0])
            return -1;

        // If after the last word time, lock to the last word.
        if (t >= _wordTimes[_wordTimes.Length - 1])
            return _wordTimes.Length - 1;

        int idx = _currentWordIndex;

        // If we don't have a current index yet, start from 0.
        if (idx < 0 || idx >= _wordTimes.Length)
            idx = 0;

        // Move forward while we have not yet reached the target time.
        while (idx + 1 < _wordTimes.Length && t >= _wordTimes[idx + 1])
        {
            idx++;
        }

        // If we overshot (e.g. scrubbed backwards), move backwards.
        while (idx > 0 && t < _wordTimes[idx])
        {
            idx--;
        }

        return idx;
    }

    /// <summary>
    /// Find start index and matched length of each word in fullText. When the JSON word has trailing punctuation not in text (e.g. "victim," vs "victim "), try matching without it so we keep the full-text path and preserve newlines.
    /// </summary>
    private static void ComputeWordStartIndices(string fullText, string[] words, out int[] indices, out int[] lengths)
    {
        indices = null;
        lengths = null;
        if (fullText == null || words == null || words.Length == 0)
            return;

        indices = new int[words.Length];
        lengths = new int[words.Length];
        int searchStart = 0;

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (string.IsNullOrEmpty(word))
            {
                indices[i] = -1;
                continue;
            }

            int idx = fullText.IndexOf(word, searchStart, StringComparison.Ordinal);
            int len = word.Length;
            if (idx < 0)
            {
                string trimmed = TrimTrailingPunctuation(word);
                if (trimmed.Length > 0)
                    idx = fullText.IndexOf(trimmed, searchStart, StringComparison.Ordinal);
                if (idx >= 0)
                    len = trimmed.Length;
            }
            if (idx < 0)
            {
                indices[i] = -1;
                continue;
            }

            indices[i] = idx;
            lengths[i] = len;
            searchStart = idx + len;
        }
    }

    private static string TrimTrailingPunctuation(string w)
    {
        if (string.IsNullOrEmpty(w)) return w;
        int n = w.Length;
        while (n > 0 && char.IsPunctuation(w[n - 1]))
            n--;
        return n == w.Length ? w : w.Substring(0, n);
    }

    /// <summary>
    /// Rebuild the TMP text string with rich text color tags
    /// for past/current/future words. Preserves tabs/newlines from _fullText when positions are available.
    /// </summary>
    private void RebuildTextWithHighlight()
    {
        if (textComponent == null || _words == null || _words.Length == 0)
            return;

        string pastHex = ColorUtility.ToHtmlStringRGB(pastColor);
        string currentHex = ColorUtility.ToHtmlStringRGB(currentColor);
        string futureHex = ColorUtility.ToHtmlStringRGB(futureColor);

        bool useFullTextLayout = !string.IsNullOrEmpty(_fullText) && _wordStartIndices != null && _wordLengthsInText != null
            && _wordStartIndices.Length == _words.Length && _wordLengthsInText.Length == _words.Length;
        for (int i = 0; useFullTextLayout && i < _wordStartIndices.Length; i++)
        {
            if (_wordStartIndices[i] < 0 || _wordLengthsInText[i] <= 0)
            {
                useFullTextLayout = false;
                break;
            }
        }

        var sb = new StringBuilder();

        if (useFullTextLayout)
        {
            int prevEnd = 0;
            for (int i = 0; i < _words.Length; i++)
            {
                int start = _wordStartIndices[i];
                int wordLen = _wordLengthsInText[i];
                int end = start + wordLen;

                sb.Append(ExpandTabs(_fullText.Substring(prevEnd, start - prevEnd)));

                string colorHex;
                if (_currentWordIndex < 0)
                    colorHex = futureHex;
                else if (i < _currentWordIndex)
                    colorHex = pastHex;
                else if (i == _currentWordIndex)
                    colorHex = currentHex;
                else
                    colorHex = futureHex;

                sb.Append("<color=#").Append(colorHex).Append(">");
                sb.Append(_words[i]);
                sb.Append("</color>");

                prevEnd = end;
            }

            if (prevEnd < _fullText.Length)
                sb.Append(ExpandTabs(_fullText.Substring(prevEnd)));
        }
        else
        {
            for (int i = 0; i < _words.Length; i++)
            {
                if (i > 0)
                    sb.Append(" ");

                string colorHex;
                if (_currentWordIndex < 0)
                    colorHex = futureHex;
                else if (i < _currentWordIndex)
                    colorHex = pastHex;
                else if (i == _currentWordIndex)
                    colorHex = currentHex;
                else
                    colorHex = futureHex;

                sb.Append("<color=#").Append(colorHex).Append(">");
                sb.Append(_words[i]);
                sb.Append("</color>");
            }
        }

        textComponent.text = sb.ToString();
    }

    // Optional helpers if you want to query from outside:

    public bool IsLoaded => _isLoaded;
    public bool IsPlaying => _isPlaying;
    public int CurrentWordIndex => _currentWordIndex;
    public string CurrentWord => (_words != null && _currentWordIndex >= 0 && _currentWordIndex < _words.Length)
        ? _words[_currentWordIndex]
        : null;
    public float[] WordTimes => _wordTimes;
    public string[] Words => _words;
}
