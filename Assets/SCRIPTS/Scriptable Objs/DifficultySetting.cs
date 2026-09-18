using System;
using UnityEngine;

[Serializable]
public class DifficultySetting
{
    public int health = 50;
    public int damage = 5;
    public float receiveKnockback = 1;
    public float speed = 3;
    public float agility = 1;
    public float range = 12;
    public float chaseRange = 18;
    public Vector2 fireRate = new Vector2(1,2);
    public float stopDist = 2.8f;
    public float retreatDist = 1.9f;
    public float attackTime = 1;
}
