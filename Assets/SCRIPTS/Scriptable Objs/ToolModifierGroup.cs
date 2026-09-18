using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ToolModifierGroup")]
public class ToolModifierGroup : ScriptableObject
{
    public List<ToolModifier> modifiers = new List<ToolModifier>();
    public List<ToolModifier> negativeModifiers = new List<ToolModifier>();
}

[System.Serializable]
public class ToolModifier
{
    public string displayName;
    public string id;
    public int percentMod;
    public int percentPerTier;
}
