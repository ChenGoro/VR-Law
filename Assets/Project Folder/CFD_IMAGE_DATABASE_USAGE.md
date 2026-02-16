# CFD Image Database – What Was Done and How to Use It

## What Was Done

1. **CFD database (ScriptableObject)**  
   - **File:** `Scripts/Scenario Builder/CFDImageDatabase.cs`  
   - Defines `CFDImageEntry` (Id, Gender, Race, Attractiveness, Expression, Sprite) and `CFDImageDatabase` ScriptableObject with a list of entries.  
   - You can also create a new database via **Assets > Create > VR-Law > CFD Image Database**.

2. **Editor tool: Load CFD Images**  
   - **File:** `Editor/CFDImageDatabaseLoader.cs`  
   - Menu: **Tools > VR-Law > Load CFD Images**.  
   - Reads all Sprites from `Resources/CFD_Images`, parses filenames (format `CFD-BF-043-003-N` → CFD, Black Female, person 043, attractiveness 003, expression N), and fills the database asset at `Assets/Project Folder/CFDImageDatabase.asset`.  
   - Run this whenever you add/remove/rename images in `Assets/Resources/CFD_Images` (before play or build).

3. **Photo class**  
   - **File:** `Scripts/Scenario Builder/Photo.cs`  
   - Added: `Id` (string, for analytics), `Attractiveness` (int).  
   - New constructor: `Photo(CFDImageEntry entry)` for runtime use from the database.  
   - Kept: `Photo(Sprite sprite)` for the legacy `Photos` folder (e.g. `F_B_001`).

4. **PhotoManager**  
   - **File:** `Scripts/Scenario Builder/PhotoManager.cs`  
   - New field: **Image Database** (assign the CFD database asset).  
   - **Init():** If Image Database is set and has entries, builds photos from the database; otherwise uses the legacy **Resources** folder (`resourcesFolder`, default `"Photos"`).  
   - **BuildQueues** logic unchanged (gender split, 50/50 male defendant/victim, etc.). A comment in code marks where to change randomization rules later.

5. **Analytics**  
   - **File:** `TXRDataManager.cs` (TAUXR)  
   - ScenarioInfo now uses `Photo.Id` for DefandantsPhoto and VictimsPhoto so exported CSVs use stable CFD IDs.

---

## How to Use It

1. **Put CFD images in Unity**  
   - Place images (e.g. `CFD-BF-043-003-N.jpg`) under **Assets/Resources/CFD_Images**.  
   - Ensure they are imported as **Sprite** (Select asset → Inspector → Texture Type: **Sprite (2D and UI)**).

2. **Build the database (editor)**  
   - Menu: **Tools > VR-Law > Load CFD Images**.  
   - This creates or updates `Assets/Project Folder/CFDImageDatabase.asset` and fills it with parsed entries.  
   - Open that asset in the Inspector to review Id, Gender, Race, Attractiveness, Expression.  
   - Re-run **Load CFD Images** whenever you change the contents of `CFD_Images`.

3. **Use the database at runtime**  
   - Select the GameObject that has **PhotoManager** (e.g. in the scene or prefab that holds scenario setup).  
   - In the Inspector, assign **Image Database** to your `CFDImageDatabase` asset.  
   - On play, PhotoManager will load from the database instead of scanning the legacy `Photos` folder.

4. **Legacy fallback**  
   - If **Image Database** is left empty, PhotoManager uses **Resources** path `resourcesFolder` (default `"Photos"`) and the old name format (`F_B_001`), so existing setups keep working until you switch.

5. **Changing randomization rules later**  
   - All assignment/splitting logic is in **PhotoManager.BuildQueues**.  
   - When you add new rules (e.g. attractiveness, race balance), edit only that method (or a helper it calls); loading and Photo/PhotoQueue stay as they are.
