using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    List<TKey> keys = new List<TKey>();
    [SerializeField]
    List<TValue> values = new List<TValue>();

    public bool modify = true;

    public void OnBeforeSerialize()
    {
        if (!modify) return;
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }

    }
    public void OnAfterDeserialize()
    {
        if (!modify) return;

        this.Clear();

        if (keys.Count != values.Count)
        {
            Debug.LogError("ummm somethings wrong with the dictionary");
        }
        else
        {
            for (int i = 0; i < keys.Count; i++)
            {
                this.Add(keys[i], values[i]);
            }

        }


    }

    public SerializableDictionary<TKey, TValue> Clone()
    {
        SerializableDictionary<TKey, TValue> copy =
            new SerializableDictionary<TKey, TValue>();

        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            copy.Add(pair.Key, pair.Value);
        }

        return copy;
    }

}
