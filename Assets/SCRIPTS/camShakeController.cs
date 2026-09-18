
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Unity.Cinemachine.CinemachineImpulseSource))]
public class camShakeController : MonoBehaviour
{
    public Unity.Cinemachine.CinemachineImpulseSource source;

    
    private void Start()
    {
        source = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
    }

    public void shake(float magnitude)
    {
        
        //shake
        source.GenerateImpulse(magnitude / 150);
        //print((range - Vector2.Distance(transform.position, player.instance.transform.position) / range * magnitude));

    }



}
