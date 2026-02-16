using UnityEngine;

public enum Gender { Male, Female }
public enum Race { Black, White }

public class Photo
{
    public Sprite Sprite { get; private set; }
    public Gender Gender { get; private set; }
    public Race Race { get; private set; }
    public int Number { get; private set; }
    public string FullPhotoName { get; private set; }
    /// <summary>Stable image ID (e.g. CFD-BF-043-003-N). Use for analytics.</summary>
    public string Id { get; private set; }
    public int Attractiveness { get; private set; }

    /// <summary>Build from CFD database entry (primary path at runtime).</summary>
    public Photo(CFDImageEntry entry)
    {
        Sprite = entry.Sprite;
        Id = entry.Id;
        FullPhotoName = entry.Id;
        Gender = entry.Gender;
        Race = entry.Race;
        Attractiveness = entry.Attractiveness;
        Number = 0;
    }

    /// <summary>Legacy: build from sprite and parse name (e.g. F_B_001 or old formats).</summary>
    public Photo(Sprite sprite)
    {
        Sprite = sprite;
        FullPhotoName = sprite.name;
        Id = sprite.name;
        Attractiveness = -1;

        string[] parts = sprite.name.Split('_');
        if (parts.Length == 3)
        {
            Gender = parts[0] == "F" ? Gender.Female : Gender.Male;
            Race = parts[1] == "B" ? Race.Black : Race.White;
            Number = int.TryParse(parts[2], out int n) ? n : 0;
        }
        else
        {
            Debug.LogWarning($"Photo name format invalid: {sprite.name}");
        }
    }
}