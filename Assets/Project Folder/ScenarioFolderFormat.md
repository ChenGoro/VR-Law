# Scenario folder format (expected input for VR-Law)

This document describes the expected layout and file format for scenario content. Use it when creating or updating scenario folders.

## About the experiment

VR-Law is a VR experiment (Meta Quest) that studies how participants react to and decide on different law cases. Each **scenario** is one case: the participant reads (and hears) a short description, then reviews four boxes (prosecutor statement, attorney statement, defendant photo, victim photo), and finally makes a decision—either a **bail** choice (e.g. ROR, ROB, jail) or a **sentencing** choice (sentence length or fine), depending on the scenario type. One scenario folder supplies all text, audio, and metadata for that single case.

## Directory layout

- **Root:** Place all scenario folders under `Assets/Resources/Scenarios/`.
- **One folder per scenario.** Name each folder with a unique id (e.g. `01`, `02`, `scenario_bail_01`). Order is determined by the `ScenarioList.txt` manifest (see below).
- **Manifest:** Create a file `Assets/Resources/Scenarios/ScenarioList.txt` that lists scenario folder names, one per line (no extension). Empty lines are ignored. Example:
  ```
  01
  02
  03
  ```

## Required files per scenario folder

All paths below are relative to the scenario folder (e.g. `Resources/Scenarios/01/`).

### Text files (UTF-8)

- **Description.txt** – Scenario description shown in the legal scenario panel. Content is used as-is; tabs, spaces, and newlines are preserved in the displayed text.
- **ProsecutorStatement.txt** – Prosecutor statement (box content). Same rules: preserve tabs, spaces, newlines.
- **AttorneyStatement.txt** – Attorney statement (box content). Same rules.
- **ScenarioType.txt** – Single line only. Must be exactly one of: `Bail`, `Sentencing`.
- **CrimeType.txt** – Single line only. Must be exactly one of: `Burglary`, `Murder`, `Rape`, `VehicularManslaughter`, `Fraud`.
- **DefendantAnnualIncome.txt** – Single line: numeric value (e.g. `45000`).

### Pre-recorded audio

One file per narrated segment. Supported extensions: `.mp3`, `.wav`, `.ogg`. Base name must match the corresponding text file (without `.txt`).

- **Description.** + extension (e.g. `Description.mp3`)
- **ProsecutorStatement.** + extension
- **AttorneyStatement.** + extension

### Word timings (Karaoke)

One JSON file per narrated segment, used for word-by-word highlighting in sync with the audio.

- **Description_wordtimes.json**
- **ProsecutorStatement_wordtimes.json**
- **AttorneyStatement_wordtimes.json**

**JSON format** (must match `KaraokeTextHighlighter.WordTimingData`):

- **Required:** `words` (array of strings), `wordTimes` (array of numbers, seconds from start of audio). Both arrays must have the same length.
- **Optional:** `text` – full display text. If you want the shown text to match the .txt file exactly (including line breaks and tabs), set `"text"` to that exact content. If omitted, the highlighter uses `words` joined by spaces.
- **Optional:** `language`, `duration`.

Example (minimal):

```json
{
  "words": ["The", "defendant", "is", "charged"],
  "wordTimes": [0.0, 0.3, 0.6, 0.9]
}
```

## Name substitution

If you use placeholders in the .txt content, they will be replaced at runtime when building the scenario:

- `*defandantFirstName*`, `*defandantLastName*`
- `*victimFirstName*`, `*victimLastName*` (if used)
- `John` → defendant first name, `Doe` → defendant last name

## Example hierarchy

```
Assets/Resources/Scenarios/
  ScenarioList.txt
  01/
    Description.txt
    Description.mp3
    Description_wordtimes.json
    ProsecutorStatement.txt
    ProsecutorStatement.mp3
    ProsecutorStatement_wordtimes.json
    AttorneyStatement.txt
    AttorneyStatement.mp3
    AttorneyStatement_wordtimes.json
    ScenarioType.txt
    CrimeType.txt
    DefendantAnnualIncome.txt
  02/
    ...
```

## Summary

| File | Purpose | Possible values |
|------|--------|-----------------|
| ScenarioList.txt | List of scenario folder names (one per line). | Any folder name, e.g. `01`, `02`, `scenario_bail_01`. |
| **/Description.txt** | Legal scenario panel text. | Free text (tabs, spaces, newlines preserved). |
| **/ProsecutorStatement.txt** | Prosecutor box text. | Free text (tabs, spaces, newlines preserved). |
| **/AttorneyStatement.txt** | Attorney box text. | Free text (tabs, spaces, newlines preserved). |
| **/ScenarioType.txt** | Type of decision in this scenario. | `Bail` \| `Sentencing` |
| **/CrimeType.txt** | Crime category. | `Burglary` \| `Murder` \| `Rape` \| `VehicularManslaughter` \| `Fraud` |
| **/DefendantAnnualIncome.txt** | Defendant’s annual income (number). | A number, e.g. `45000`. |
| **/Description.(mp3\|wav\|ogg)** | Description audio. | Any of .mp3, .wav, .ogg. |
| **/ProsecutorStatement.(mp3\|wav\|ogg)** | Prosecutor audio. | Any of .mp3, .wav, .ogg. |
| **/AttorneyStatement.(mp3\|wav\|ogg)** | Attorney audio. | Any of .mp3, .wav, .ogg. |
| **/*_wordtimes.json** | Word timings for Karaoke. | JSON: `words` (string[]), `wordTimes` (float[]); optional `text`, `language`, `duration`. |
