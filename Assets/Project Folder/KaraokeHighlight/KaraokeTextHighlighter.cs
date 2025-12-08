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
    public void LoadFromJson(string json)
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

        if (textComponent != null)
        {
            textComponent.text = _fullText;
        }

        _currentWordIndex = -1;
        _isLoaded = true;

        // Initial render with all words in "future" color
        RebuildTextWithHighlight();

        // Optional: debug info
        // Debug.Log($"[KaraokeTextHighlighter] Loaded {_words.Length} words, language={_language}, duration={_durationSeconds:F2}s");
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
    /// Rebuild the TMP text string with rich text color tags
    /// for past/current/future words.
    /// </summary>
    private void RebuildTextWithHighlight()
    {
        if (textComponent == null || _words == null || _words.Length == 0)
            return;

        var sb = new StringBuilder();

        string pastHex = ColorUtility.ToHtmlStringRGB(pastColor);
        string currentHex = ColorUtility.ToHtmlStringRGB(currentColor);
        string futureHex = ColorUtility.ToHtmlStringRGB(futureColor);

        for (int i = 0; i < _words.Length; i++)
        {
            if (i > 0)
                sb.Append(" ");

            string colorHex;

            if (_currentWordIndex < 0)
            {
                // No current word yet: everything is "future"
                colorHex = futureHex;
            }
            else if (i < _currentWordIndex)
            {
                colorHex = pastHex;
            }
            else if (i == _currentWordIndex)
            {
                colorHex = currentHex;
            }
            else
            {
                colorHex = futureHex;
            }

            sb.Append("<color=#").Append(colorHex).Append(">");
            sb.Append(_words[i]);
            sb.Append("</color>");
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
