using System.Collections.Generic;

public class PhotoQueue
    /*
     * iterable container for a list of photos, already shuffled and loaded by photo manager.
     * gets the next photo in line
     */
{
    private List<Photo> photos;
    private List<int> randomOrder;
    private int currentIndex = 0;
    public int RemainingCount => randomOrder.Count - currentIndex;

    public PhotoQueue(List<Photo> inputPhotos)
    {
        photos = inputPhotos;
        randomOrder = CreateRandomOrder(photos.Count);
    }

    public Photo Next()
    {
        if (!HasNext()) return null;
        Photo result = photos[randomOrder[currentIndex]];
        currentIndex++;
        return result;
    }

    public bool HasNext()
    {
        return currentIndex < randomOrder.Count;
    }

    private List<int> CreateRandomOrder(int count)
    {
        List<int> order = new List<int>();
        for (int i = 0; i < count; i++) order.Add(i);

        for (int i = 0; i < order.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, order.Count);
            (order[i], order[rand]) = (order[rand], order[i]);
        }

        return order;
    }
}
