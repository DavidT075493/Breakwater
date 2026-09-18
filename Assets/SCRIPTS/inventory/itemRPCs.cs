using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class itemRPCs : MonoBehaviour
{
    public static itemRPCs Instance;

    public SerializableDictionary<string, itemPickup> items = new SerializableDictionary<string, itemPickup>();

    private void Awake()
    {
        Instance = this;
    }


}
