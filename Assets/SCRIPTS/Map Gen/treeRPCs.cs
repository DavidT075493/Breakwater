
using Fusion;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

//script for the tree manager that houses all trees and ores and applies damage
public class treeRPCs : NetworkBehaviour, IDataPersistance
{
    public static treeRPCs Instance;

    public List<treeChop> trees;
    public List<treeChop> ores;
    public List<itemPickup> items;
    public List<itemPickup> uniqueItems;
    public List<treeChop> otherDamagables;
    public List<Bell> bells;
    public NetworkObject view;
    public GameObject itemPrefab;

    int currentNaturalItemID;
    public IslandFeatures[] islandFeatures = new IslandFeatures[MapGenerator.islandCount + 1];

    const int MaxBytes = 400;
    public bool generatingTreeHealths;

    //featuretype 0 = trees, 1 = ores
    SerializableDictionary<Vector2Int, Vector2Int> treeHealths = new SerializableDictionary<Vector2Int, Vector2Int>();
    SerializableDictionary<Vector2Int, int> oreHealths = new SerializableDictionary<Vector2Int, int>();
    SerializableDictionary<Vector2Int, int> otherHealths = new SerializableDictionary<Vector2Int, int>();
    public SerializableDictionary<int, bool> collectedItems = new SerializableDictionary<int, bool>();
    public SerializableDictionary<Vector2, bool> destroyedRocks = new SerializableDictionary<Vector2, bool>();

    private void Awake()
    {
        Instance = this;
        treeChop.lastId = 0;
    }

    public void FlipScale(int x, int y)
    {
        RPC_flipScale(x, y);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_flipScale(int x, int y)
    {
        //get tree at speci =ed position
        treeChop t = trees.FirstOrDefault(treeChop => treeChop != null && treeChop.gameObject.transform.position.x == x && treeChop.gameObject.transform.position.y == y);

        if (t)
            t.flipScale();
        else
            Debug.LogWarning("Failed to flip scale. Ore not found at position: " + x + ", " + y);
    }

    public void CreateAllTrees(List<NewFeature> newFeatures, short featureType, short islandIndex)
    {
        generatingTreeHealths = true;

        var instance = featurePlacer.Instance;

        var features = instance.features;
        var minerals = instance.minerals;

        var treeHolder = instance.treeHolder;
        var mineralHolder = instance.mineralHolder;

        var island = islandFeatures[islandIndex];

        List<treeChop> localChops = trees;
        List<treeChop> localOres = ores;

        switch (featureType)
        {
            case 0:
                {
                    for (int i = 0; i < newFeatures.Count; i++)
                    {
                        var f = newFeatures[i];
                        if (f == null) continue;

                        var prefab = features[f.fI].gameObjects[f.i];

                        var chop = Instantiate(prefab, f.p, Quaternion.identity, treeHolder)
                            .GetComponent<treeChop>();

                        if (chop == null) continue;

                        if (f.f)
                            chop.flipScale();

                        localChops.Add(chop);
                        instance.treesList.Add(chop.gameObject);
                        island.treeChops.Add(chop);
                    }
                    break;
                }

            case 1:
                {
                    for (int i = 0; i < newFeatures.Count; i++)
                    {
                        var f = newFeatures[i];
                        if (f == null) continue;

                        var prefab = minerals[f.fI].gameObjects[f.i];

                        var obj = Instantiate(prefab, f.p, Quaternion.identity, mineralHolder);
                        var chop = obj.GetComponent<treeChop>();

                        if (chop == null) continue;

                        localOres.Add(chop);
                        instance.mineralsList.Add(obj);
                        island.treeChops.Add(chop);
                    }
                    break;
                }

            case 2:
                {
                    var shrubs = island.shrubs;

                    for (int i = 0; i < newFeatures.Count; i++)
                    {
                        var f = newFeatures[i];
                        if (f == null) continue;

                        var prefab = minerals[f.fI].gameObjects[f.i];

                        var obj = Instantiate(prefab, f.p, Quaternion.identity, mineralHolder);
                        var bell = obj.GetComponent<Bell>();

                        if (bell == null) continue;

                        bells.Add(bell);
                        shrubs.Add(obj);
                    }
                    break;
                }
        }

        generatingTreeHealths = false;
    }

    public void CreateOceanRocks(List<NewFeature> newFeatures)
    {
        var instance = featurePlacer.Instance;

        var destroyed = destroyedRocks;

        for (int i = 0; i < newFeatures.Count; i++)
        {
            if (destroyed.ContainsKey(newFeatures[i].p))
                continue;

            var f = newFeatures[i];
            if (f == null) continue;

            var prefab = featurePlacer.Instance.oceanStuff[f.fI].gameObjects[f.i];

            var newRock = Instantiate(prefab, f.p, Quaternion.identity, featurePlacer.Instance.shrubHolder);

            if (f.f)
                newRock.transform.localScale = new Vector3(-1, 1, 1);

            instance.oceanStuffList.Add(newRock);
        }

    }


    public void CreateAllItems(List<NewItem> newItems, string itemId, short islandIndex)
    {
        var instance = featurePlacer.Instance;

        var holder = instance.itemHolder;
        var itemsList = instance.itemsList;

        var island = islandFeatures[islandIndex];

        var itemData = gameManager.instance.itemDictionary[itemId];

        int localId = currentNaturalItemID;

        for (int i = 0; i < newItems.Count; i++)
        {
            var ni = newItems[i];
            if (ni == null) continue;

            var item = Instantiate(itemPrefab, ni.pos, Quaternion.identity, holder)
                .GetComponent<itemPickup>();

            item.Item = itemData;
            item.count = ni.amt;

            item.naturalItemID = localId++;

            items.Add(item);
            itemsList.Add(item.gameObject);
            island.items.Add(item);

        }

        currentNaturalItemID = localId;
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = false)]
    public void RPC_RequestCollectItem(int id, NetworkId playerId)
    {
        if (!SingletonRunner.runner.IsSharedModeMasterClient) return;

        RPC_CollectItem(id, playerId);
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = false)]
    void RPC_CollectItem(int id, NetworkId playerId)
    {
        if (items[id] == null) return;

        if (player.instance.view.Id == playerId)
        {
            if(inventory.instance.gameObject.activeSelf)
                inventory.instance.AddItem(items[id].Item, items[id].count, items[id].durability, new ItemData(0));
            else
                ValeManager.instance.AddInventoryItem(items[id].Item, items[id].count, false);
        }
        if (collectedItems == null)
            collectedItems = new SerializableDictionary<int, bool>();
        collectedItems.TryAdd(id, true);

        Destroy(items[id].gameObject);
    }


    public void HitTreeAtPos(int damage, string itemIndex, int x, int y, short featureType, bool correctTool, float dropsMult)
    {
        RPC_TreeHit(damage, itemIndex, x, y, featureType,correctTool, dropsMult);
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_TreeHit(int damage, string itemIndex, int x, int y, short featureType, bool correctTool, float dropsMult)
    {
        switch (featureType)
        {
            case 0:
                treeChop t = trees.FirstOrDefault(treeChop => treeChop != null && treeChop.gameObject.transform.position.x == x && treeChop.gameObject.transform.position.y == y);
                if (t)
                {
                    t.gameObject.SetActive(true);
                    t.Hit(damage, itemIndex, correctTool, dropsMult);

                    treeHealths[new Vector2Int(x, y)] = new Vector2Int(t.currentHp, t.stumpHp);
                }
                else
                    Debug.LogWarning("Failed to apply damage. Tree not found at position: " + x + ", " + y);

                break;
            case 1:
                treeChop o = ores.FirstOrDefault(treeChop => treeChop != null && treeChop.gameObject.transform.position.x == x && treeChop.gameObject.transform.position.y == y);
                if (o)
                {
                    o.gameObject.SetActive(true);
                    o.Hit(damage, itemIndex, correctTool, dropsMult);

                    oreHealths[new Vector2Int(x, y)] = o.currentHp;
                }
                else
                    Debug.LogWarning("Failed to apply damage. Ore not found at position: " + x + ", " + y);


                break;

            case 2:
                treeChop b = otherDamagables.FirstOrDefault(treeChop => treeChop != null && treeChop.gameObject.transform.position.x == x && treeChop.gameObject.transform.position.y == y);
                if (b)
                {
                    b.gameObject.SetActive(true);
                    b.Hit(damage, itemIndex, correctTool, dropsMult);

                    otherHealths[new Vector2Int(x, y)] = b.currentHp;
                }
                else
                    Debug.LogWarning("Failed to apply damage. Other not found at position: " + x + ", " + y);

                break;
            case 3:
                Bell bell = bells.FirstOrDefault(b => b != null && b.gameObject.transform.position.x == x && b.gameObject.transform.position.y == y);

                if (bell && bell.gameObject.activeSelf)
                {
                    bell.Ring();
                }
                else
                    Debug.LogWarning("Failed to apply damage. Bell not found at position: " + x + ", " + y);
                break;
        }


    }

    public void TreeDie(int x, int y, short featureType, bool smelt, float dropsMultiplier)
    {
        RPC_TreeDie(x, y, featureType, smelt, dropsMultiplier);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_TreeDie(int x, int y, short featureType, bool smelt, float dropsMultiplier)
    {
        switch (featureType)
        {
            case 0:
                treeChop t = trees.FirstOrDefault(treeChop => treeChop != null && treeChop.gameObject.transform.position.x == x && treeChop.gameObject.transform.position.y == y);
                if (t)
                    StartCoroutine(t._Die(false, false, smelt, dropsMultiplier));
                else
                    Debug.LogWarning("Failed to destroy. Tree not found at position: " + x + ", " + y);

                break;
            case 1:
                treeChop o = ores.FirstOrDefault(treeChop => treeChop != null && treeChop.gameObject.transform.position.x == x && treeChop.gameObject.transform.position.y == y);
                if (o)
                    StartCoroutine(o._Die(false, false, smelt, dropsMultiplier));
                else
                    Debug.LogWarning("Failed to destroy. Ore not found at position: " + x + ", " + y);


                break;
            case 2:
                treeChop b = otherDamagables.FirstOrDefault(treeChop => treeChop != null && treeChop.gameObject.transform.position.x == x && treeChop.gameObject.transform.position.y == y);
                if (b)
                    StartCoroutine(b._Die(false, false, smelt, dropsMultiplier));
                else
                    Debug.LogWarning("Failed to destroy. Other not found at position: " + x + ", " + y);

                break;

        }

    }

    // === Public entry point ===
    public void SendHealths()
    {
        SendDictionaryInChunks(treeHealths, RPC_SetTreeHealths, DictionaryToString);
        SendDictionaryInChunks(oreHealths, RPC_SetOreHealths, DictionaryToString);
        SendDictionaryInChunks(otherHealths, RPC_SetOtherHealths, DictionaryToString);
        SendDictionaryInChunks(collectedItems, RPC_SetCollectedItems, DictionaryToString);

        RPC_FinishSettingHealths();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_FinishSettingHealths()
    {
        generatingTreeHealths = false;
    }

    void SendDictionaryInChunks<TKey, TValue>(
    SerializableDictionary<TKey, TValue> dict,
    Action<string> rpcMethod,
    Func<SerializableDictionary<TKey, TValue>, string> serializer)
    {
        if (dict == null || dict.Count == 0)
            return;

        var chunk = new SerializableDictionary<TKey, TValue>();

        foreach (var kv in dict)
        {
            chunk.Add(kv.Key, kv.Value);

            string serialized = serializer(chunk);
            int byteCount = Encoding.UTF8.GetByteCount(serialized);

            if (byteCount > MaxBytes)
            {
                // remove the last entry and send the chunk
                chunk.Remove(kv.Key);

                string safeSerialized = serializer(chunk);
                rpcMethod(safeSerialized);

                // start a new chunk with this entry
                chunk.Clear();
                chunk.Add(kv.Key, kv.Value);
            }
        }

        if (chunk.Count > 0)
        {
            string serialized = serializer(chunk);
            rpcMethod(serialized);
        }
    }

    // === RPCs (separated per dictionary type) ===
    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_SetTreeHealths(string treeHealthsString)
    {
        var decoded = StringToDictionary<Vector2Int, Vector2Int>(treeHealthsString);
        foreach (var healths in decoded)
        {
            var t = trees.FirstOrDefault(treeChop =>
                treeChop != null &&
                treeChop.transform.position.x == healths.Key.x &&
                treeChop.transform.position.y == healths.Key.y);

            if (!t) continue;

            if (healths.Value.y <= 0 && healths.Value.x <= 0)
                Destroy(t.gameObject);

            t.currentHp = healths.Value.x;
            t.UpdateHealth(true);

            if (t.currentHp <= 0)
            {
                t.dead = true;
                StartCoroutine(t._Die(true, true));
                t.stumpHp = healths.Value.y;
            }
        }

    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_SetOreHealths(string oreHealthsString)
    {
        var decoded = StringToDictionary<Vector2Int, int>(oreHealthsString);
        foreach (var healths in decoded)
        {
            var t = ores.FirstOrDefault(treeChop =>
                treeChop != null &&
                treeChop.transform.position.x == healths.Key.x &&
                treeChop.transform.position.y == healths.Key.y);

            if (!t) continue;

            if (healths.Value <= 0)
                Destroy(t.gameObject);

            t.currentHp = healths.Value;
            t.UpdateHealth(true);

            if (t.currentHp <= 0)
            {
                t.dead = true;
                StartCoroutine(t._Die(true, true));
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_SetOtherHealths(string otherHealthsString)
    {
        var decoded = StringToDictionary<Vector2Int, int>(otherHealthsString);
        foreach (var healths in decoded)
        {
            var t = otherDamagables.FirstOrDefault(treeChop =>
                treeChop != null &&
                treeChop.transform.position.x == healths.Key.x &&
                treeChop.transform.position.y == healths.Key.y);

            if (!t) continue;

            if (healths.Value <= 0)
                Destroy(t.gameObject);

            t.currentHp = healths.Value;
            t.UpdateHealth(true);

            if (t.currentHp <= 0)
            {
                t.dead = true;
                StartCoroutine(t._Die(true, true));
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = true, TickAligned = true)]
    void RPC_SetCollectedItems(string collectedItemsString)
    {
        var decoded = StringToDictionary<int, bool>(collectedItemsString);
        foreach (var collected in decoded)
        {
            itemPickup i;

            if (collected.Key >= 0)
            {
                if (items.Count <= collected.Key) continue;
                i = items[collected.Key];
                if (!i) continue;
                Destroy(i.gameObject);
            }
            else
            {
                if (uniqueItems.Count > collected.Key * -1 - 1)
                {
                    i = uniqueItems[collected.Key * -1 - 1];
                    if (!i) continue;
                    i.Disable();
                }
            }
        }
    }


    public static string ArrayToString<T>(T[] array)
    {
        List<string> serializedStrings = new List<string>();

        foreach (T item in array)
        {
            serializedStrings.Add(JsonUtility.ToJson(item));
        }

        string combinedString = string.Join(";", serializedStrings.ToArray());

        byte[] compressedBytes;
        using (MemoryStream compressedStream = new MemoryStream())
        {
            using (BrotliStream brotliStream = new BrotliStream(compressedStream, CompressionLevel.Optimal))
            using (StreamWriter writer = new StreamWriter(brotliStream))
            {
                writer.Write(combinedString);
            }

            compressedBytes = compressedStream.ToArray();
        }

        return Convert.ToBase64String(compressedBytes);
    }

    public static T[] StringToArray<T>(string compressedString)
    {
        byte[] compressedBytes = Convert.FromBase64String(compressedString);

        string decompressedString;
        using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
        {
            using (BrotliStream brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(brotliStream))
            {
                decompressedString = reader.ReadToEnd();
            }
        }

        string[] itemStrings = decompressedString.Split(';');
        List<T> items = new List<T>();

        foreach (string itemStr in itemStrings)
        {
            T obj = JsonUtility.FromJson<T>(itemStr);
            items.Add(obj);
        }

        return items.ToArray();
    }



    public static string DictionaryToString<TKey, TValue>(SerializableDictionary<TKey, TValue> dictionary)
    {
        if (dictionary == null)
        {
            return "";
        }
        List<string> pairStrings = new List<string>();

        foreach (var kvp in dictionary)
        {
            SerializableKeyValuePair<TKey, TValue> serializablePair = new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value);
            string pairString = JsonUtility.ToJson(serializablePair);
            pairStrings.Add(pairString);
        }

        string combinedString = string.Join(";", pairStrings.ToArray());

        byte[] compressedBytes;
        using (MemoryStream compressedStream = new MemoryStream())
        {
            using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress))
            {
                using (StreamWriter writer = new StreamWriter(deflateStream))
                {
                    writer.Write(combinedString);
                }
            }
            compressedBytes = compressedStream.ToArray();
        }

        string compressedString = System.Convert.ToBase64String(compressedBytes);

        return compressedString;
    }

    public static SerializableDictionary<TKey, TValue> StringToDictionary<TKey, TValue>(string compressedDictionaryString)
    {
        byte[] compressedBytes = System.Convert.FromBase64String(compressedDictionaryString);

        string decompressedString;
        using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
        {
            using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                using (StreamReader reader = new StreamReader(deflateStream))
                {
                    decompressedString = reader.ReadToEnd();
                }
            }
        }

        string[] pairStrings = decompressedString.Split(';');
        SerializableDictionary<TKey, TValue> dictionary = new SerializableDictionary<TKey, TValue>();

        foreach (string pairString in pairStrings)
        {
            SerializableKeyValuePair<TKey, TValue> serializablePair = JsonUtility.FromJson<SerializableKeyValuePair<TKey, TValue>>(pairString);
            if (serializablePair != null)
                dictionary.Add(serializablePair.Key, serializablePair.Value);
        }

        return dictionary;
    }

    [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false, TickAligned = true)]
    void RPC_ReceiveDestroyedRocks(string data)
    {
        destroyedRocks = StringToDictionary<Vector2, bool>(data);
    }

    public void SaveData(GameData data)
    {
        if (Runner.IsSharedModeMasterClient)
        {
            data.treeHealths = treeHealths;
            data.oreHealths = oreHealths;
            data.otherHealths = otherHealths;
            data.naturalItemsCollected = collectedItems;
            data.destroyedRocks = destroyedRocks;
        }
    }
    public void LoadData(GameData data)
    {
        treeHealths = data.treeHealths;
        oreHealths = data.oreHealths;
        otherHealths = data.otherHealths;
        collectedItems = data.naturalItemsCollected;
        destroyedRocks = data.destroyedRocks;

        if (Runner.IsSharedModeMasterClient)
        {
            RPC_ReceiveDestroyedRocks(DictionaryToString(destroyedRocks));
        }

    }
}
public class NewFeature
{
    //position
    public Vector2 p;
    //index
    public short i;
    //feature index
    public short fI;
    //flip
    public bool f;


    public NewFeature(Vector2 position, short treeIndex, short currentFeatureIndex, bool flip)
    {
        p = position;
        i = treeIndex;
        fI = currentFeatureIndex;
        f = flip;
    }
}
public class NewItem
{
    public Vector2 pos;
    public int amt;

    public NewItem(Vector2 pos, int count)
    {
        this.pos = pos;
        amt = count;
    }

}
[Serializable]
public class IslandFeatures
{
    public bool loaded;
    public List<GameObject> shrubs = new List<GameObject>();
    public List<treeChop> treeChops = new List<treeChop>();
    public List<itemPickup> items = new List<itemPickup>();

}

[System.Serializable]
public class SerializableKeyValuePair<TKey, TValue>
{
    [SerializeField]
    private TKey key;

    [SerializeField]
    private TValue value;

    public TKey Key
    {
        get { return key; }
        set { key = value; }
    }

    public TValue Value
    {
        get { return value; }
        set { this.value = value; }
    }

    public SerializableKeyValuePair(TKey key, TValue value)
    {
        this.key = key;
        this.value = value;
    }
}