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

    public Photo(Sprite sprite)
    {
        Sprite = sprite;
        FullPhotoName = sprite.name;

        // Parse name, e.g. "F_B_001"
        string[] parts = sprite.name.Split('_');
        if (parts.Length == 3)
        {
            Gender = parts[0] == "F" ? Gender.Female : Gender.Male;
            Race = parts[1] == "B" ? Race.Black : Race.White;
            Number = int.Parse(parts[2]);
        }
        else
        {
            Debug.LogWarning($"Photo name format invalid: {sprite.name}");
        }
    }
}