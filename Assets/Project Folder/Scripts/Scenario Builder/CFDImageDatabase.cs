using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single CFD image entry: parsed attributes + sprite reference.
/// Id = full image name (no extension), e.g. CFD-BF-043-003-N.
/// </summary>
[Serializable]
public class CFDImageEntry
{
    public string Id;
    public Gender Gender;
    public Race Race;
    public int Attractiveness;
    public string Expression;
    public Sprite Sprite;

    public CFDImageEntry(string id, Gender gender, Race race, int attractiveness, string expression, Sprite sprite)
    {
        Id = id;
        Gender = gender;
        Race = race;
        Attractiveness = attractiveness;
        Expression = expression ?? "";
        Sprite = sprite;
    }
}

/// <summary>
/// Database of CFD images, populated by the editor tool "Load CFD Images".
/// Assign this asset to PhotoManager so runtime uses it instead of scanning Resources.
/// </summary>
[CreateAssetMenu(fileName = "CFDImageDatabase", menuName = "VR-Law/CFD Image Database", order = 0)]
public class CFDImageDatabase : ScriptableObject
{
    [Tooltip("Resources subfolder used by the Load CFD Images editor tool (e.g. CFD_Images).")]
    public string resourcesSubfolder = "CFD_Images";

    [SerializeField] private List<CFDImageEntry> entries = new List<CFDImageEntry>();

    public IReadOnlyList<CFDImageEntry> Entries => entries;

    public void SetEntries(List<CFDImageEntry> newEntries)
    {
        entries = newEntries ?? new List<CFDImageEntry>();
    }
}
