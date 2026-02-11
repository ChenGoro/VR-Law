# VR-Law: TAUXR Template & Experiment Architecture

## 1. TAUXR Experiment Template — Summary

### Core conventions (from template + your notes)
- **Player / camera / rig:** Use `TXRPlayer.Instance` for head, hands, eye tracker, passthrough, fade, repositioning, and input (e.g. `PinchingInputManager`, `ControllersInputManager`). Do not cache the player in scene references.
- **References between objects:** Keep cross-object references in **SceneReferencer** (singleton). Components get `SceneReferencer.Instance` and use its public fields (e.g. `scenarioManager`, `scenarioPlayer`, instructions). Public references are fine **within** a single GameObject/prefab.
- **Async:** Prefer **UniTask** over Coroutines for async flows (e.g. `async UniTask`, `UniTaskCompletionSource`, `await`).
- **Data / analytics:** Do not modify **TXRDataManager** or scripts under TXRDataManager except to add **custom analytics**. New event types = new class implementing `AnalyticsDataClass` (with `TableName`) in `TXRDataManager.cs`, plus a reporter method that calls `WriteAnalyticsToFile(...)`. See `BoxesEvents`, `ScenarioInfo`, `Decisions`, `PanelsAndConfirmationEvents` for examples.
- **Entry point:** The template’s intended entry point is **SessionManager**: `Start()` → `RunSessionFlow()` → rounds → **RoundManager.RunRoundFlow(Round)** → trials → **TrialManager.RunTrialFlow(Trial)**. Your project-specific trial logic is meant to be invoked from `TrialManager.RunTrialFlow` (e.g. await your experiment steps there).

### Other patterns from the template and project
- **Singletons:** Use `TXRSingleton<T>` for global managers (e.g. `SessionManager`, `RoundManager`, `TrialManager`, `TXRPlayer`, `TXRDataManager`, `SceneReferencer`). Override `DoInAwake()` for init; avoid multiple instances (template logs and destroys duplicates).
- **Scenes:** **TXRSceneManager** handles additive loading, active scene, fade (via `TXRPlayer.Instance.FadeViewToColor`), and optional **PlayerRepositioner** per scene. Init with `Init(isProjectUsingCalibration)`. Use `SwitchActiveScene(string)` for transitions.
- **Calibration:** **EnvironmentCalibrator** uses `TXRPlayer.Instance` for pinch-to-confirm and passthrough; player repositioning is skipped when calibration is used (`TXRSceneManager.Init(false)`).
- **Input:** Pinch/hold flows use `TXRPlayer.Instance.PinchingInputManager` (e.g. `WaitForHoldAndRelease`). Hand/collider access via `TXRPlayer.Instance.GetHandFingerCollider`, `HandLeft`, `HandRight`.
- **UI / flow:** The template provides **Round** and **Trial** as minimal config containers; you extend them and plug your logic into the session/round/trial flow. The project uses a **custom top-level flow** in **MainExperiment** instead of SessionManager (see below).

---

## 2. Current Experiment Architecture & Flow

### High-level flow
The experiment does **not** use SessionManager/RoundManager/TrialManager. The **entry point** is **MainExperiment** (`Start()`), which runs a linear scenario loop and then quits.

1. **Start**
   - `MainExperiment.Start()` → `Init()` (gets `SceneReferencer`, hides instruction panels).
2. **General instructions**
   - Show `sceneReferencer.generalInstructions` until confirm; log to analytics.
3. **Scenario loop** (while `ScenarioManager.HasNextScenario()`)
   - Get next **ScenarioData** from **ScenarioManager**.
   - Log **ScenarioInfo** (TXRDataManager).
   - Call **ScenarioPlayer.PlayScenario(currentScenario, isLastScenario)**.
4. **End**
   - Show `sceneReferencer.endOfExperimentInstructions` until confirm.
   - `Application.Quit()`.

### Per-scenario flow (ScenarioPlayer.PlayScenario)
For each scenario:

1. **Legal scenario (case description)**  
   **LegalScenario**: load description from `ScenarioData`, show panel + TTS, wait for confirm. Log panel events.

2. **Boxes (evidence)**  
   **BoxesManager**: load prosecutor/attorney statements and defendant/victim photos; set box order from `ScenarioData.LayoutOrder`. Show four boxes; user opens each (open/close logged as **BoxesEvents**). After all viewed, show continue instructions, wait for continue, then hide and log.

3. **Decision**
   - **Bail**: **Bail** loads scenario (defendant name, income). Shows ROR / ROB / Jail and **AmountChooser** (slider). ROB enabled only after slider touched. On choice, log **Decisions** (choice, bail amount if ROB, RT) and **PanelsAndConfirmationEvents**.
   - **Sentencing**: **Sentence** loads scenario; shows sentence length vs fine with two **AmountChooser** sliders. Buttons enabled when respective slider touched. On choice, log **Decisions** (choice, sentence length or fine amount, RT) and panel events.

4. **Between scenarios**  
   If not last scenario: **endSenarioInstructions** shown until confirm. If last, this step is skipped.

### Data and content pipeline
- **ScenarioLibrary**: loads **ScenarioTemplate** list from a CSV in `Resources` (path in `csvFilePath`). Exposes `Templates` and `GetRandomTemplate()`. In builds, saves a copy of the CSV under persistent data with participant id.
- **ScenarioManager**: at Start, builds a list of **ScenarioData** by combining random templates with **PhotoManager** (defendant/victim queues), random names, and random **LayoutOrder** for the four boxes. Exposes `GetNextScenario()` and `HasNextScenario()`.
- **ScenarioData**: immutable per-trial data (scenario type, crime type, layout, defendant/victim photos and names, description, statements, income, scenario index). Built from **ScenarioTemplate** + photos + names.
- **SceneReferencer**: holds references to **Instructions** (general + end), **ScenarioManager**, **ScenarioPlayer**. **ScenarioPlayer** holds references to instructions, **LegalScenario**, **BoxesManager**, **Bail**, **Sentence** (all used as “stages” with Load + Show/await confirm or choice).

### Analytics (TXRDataManager)
- **ScenarioInfo**: per scenario (index, type, crime, description, statements, income, names, gender/race, photo names).
- **BoxesEvents**: per box open/close (scenario index, box type, layout order, action).
- **Decisions**: per decision (scenario index, type, decision string, bail/sentence/fine amounts, RT).
- **PanelsAndConfirmationEvents**: panel show/confirm/hidden (scenario index, panel name, action).
- **TAUXR_Logs**: free-form lines via `LogLineToFile`.

### Notable component roles
- **Instructions**: generic panel with text + confirm button; `LoadScenarioAssets` sets text from `ScenarioData.ScenarioDescription` where used for scenario-specific text; `ShowUntilConfirm()` uses UniTask + `VR_Button.VRButtonPressed`.
- **Bail / Sentence**: scenario-specific UI (titles, options, sliders); use **AmountChooser** (wraps project **Slider**) for numeric input; report decisions and RT to TXRDataManager.
- **Box**: single box (statement or photo); open/close and TTS for statements; reports **BoxesEvents** and notifies **BoxesManager** when viewed for “all boxes viewed” gate.
- **TTS**: **TtsCaller.I** used for speaking scenario text and statement content; cancel on confirm where needed.

---

*Template: TAUXR experiment template. Project: VR-Law (Unity 6000.0.40f1, Meta Quest Pro).*
