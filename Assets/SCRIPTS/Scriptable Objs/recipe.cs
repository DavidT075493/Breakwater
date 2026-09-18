using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
[System.Serializable]
public class recipe: ScriptableObject
{
    public ingredient[] Ingredients;
    public ingredient result;
    public int priority = 0;
    public bool disableRecipe;
}
[Serializable]
public class ingredient
{
    public item Item;
    public int count = 1;
}
