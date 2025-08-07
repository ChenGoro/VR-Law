using System.Collections.Generic;
using UnityEngine;

public class PhotoManager : MonoBehaviour
    /*
     * in charge of loading the photo assets, putting them in queues and shuffles them
     */
{
    public string resourcesFolder = "Photos"; // Folder under Resources/

    public PhotoQueue DefendantQueue { get; private set; }
    public PhotoQueue VictimQueue { get; private set; }

    public void Init()
    {
        List<Photo> allPhotos = LoadAllPhotos(resourcesFolder);
        BuildQueues(allPhotos);
    }

    private List<Photo> LoadAllPhotos(string folder)
    {
        List<Photo> photoList = new List<Photo>();
        Object[] loadedAssets = Resources.LoadAll(folder, typeof(Sprite));

        foreach (Object asset in loadedAssets)
        {
            if (asset is Sprite sprite)
            {
                Photo photo = new Photo(sprite);
                photoList.Add(photo);
            }
        }

        return photoList;
    }

    private void BuildQueues(List<Photo> allPhotos)
    {
        List<Photo> femalePhotos = allPhotos.FindAll(p => p.Gender == Gender.Female);
        List<Photo> malePhotos = allPhotos.FindAll(p => p.Gender == Gender.Male);

        ShuffleList(malePhotos); // Randomize male photos
        ShuffleList(femalePhotos);

        int halfMaleCount = malePhotos.Count / 2;


        //TODO figure out the correct way so separate the number of males to defendant and victim 
        // If odd number of male photos, leave one out and log it
        if (malePhotos.Count % 2 != 0)
        {
            Photo dropped = malePhotos[malePhotos.Count - 1];
            Debug.LogWarning($"Dropped photo to balance queues: {dropped.FullPhotoName}");
            malePhotos.RemoveAt(malePhotos.Count - 1);
        }

        List<Photo> defendantPhotos = malePhotos.GetRange(0, halfMaleCount);
        List<Photo> victimPhotos = new List<Photo>(femalePhotos);
        victimPhotos.AddRange(malePhotos.GetRange(halfMaleCount, halfMaleCount));

        // Make sure both lists are the same length
        int finalCount = Mathf.Min(defendantPhotos.Count, victimPhotos.Count);

        DefendantQueue = new PhotoQueue(defendantPhotos.GetRange(0, finalCount));
        VictimQueue = new PhotoQueue(victimPhotos.GetRange(0, finalCount));
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}
