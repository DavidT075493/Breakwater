using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> lootEntries;

    [Header("Roll Settings")]
    public int minRolls = 1;
    public int maxRolls = 3;
}