using UnityEngine;

[ExecuteAlways]
public class seed : MonoBehaviour
{
    public static seed instance;

    public string seedInput;
    public int currentSeed;

    public void SetSeed(string newSeed)
    {
        seedInput = newSeed;
        currentSeed = newSeed.GetHashCode();

        Random.InitState(currentSeed);

        FindFirstObjectByType<MapGenerator>().CreateMap(currentSeed);
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnValidate()
    {
        instance = this;
        currentSeed = seedInput.GetHashCode();
    }



}
