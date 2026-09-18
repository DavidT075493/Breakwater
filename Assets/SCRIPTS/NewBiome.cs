using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NewBiome : MonoBehaviour, IDataPersistance
{
    public static NewBiome Instance;

    public NewBiomeData[] biomeData;
    public Animation anim;
    public TextMeshProUGUI[] texts;

    private void Awake()
    {
        Instance= this;
    }

    public void VisitBiome(int currentBiome)
    {
        if (currentBiome < 0) return;

        foreach(TextMeshProUGUI t in texts)
        {
            t.color = biomeData[currentBiome].textColor;
        }
        texts[0].text = biomeData[currentBiome].name;

        anim.Play();
    }

    public void SaveData(GameData data)
    {
        if (data.visitedBiomes == null || data.visitedBiomes.Length < biomeData.Length) data.visitedBiomes = new bool[4];

        int i = 0;
        foreach (NewBiomeData d in biomeData)
        {
            data.visitedBiomes[i] = d.visited;
            i++;
        }
    }

    public void LoadData(GameData data) 
    {
        int i = 0;
        foreach (NewBiomeData d in biomeData)
        {
            d.visited = data.visitedBiomes[i];
            i++;
        }
    }


}
[System.Serializable]
public class NewBiomeData
{
    public Color textColor;
    public bool visited;
    public string name;

}
