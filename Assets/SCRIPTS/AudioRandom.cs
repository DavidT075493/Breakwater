using UnityEngine;

public class AudioRandom : MonoBehaviour
{
    public AudioSource source;
    public float pitchVariance = 0.12f;
    public float volumeVariance = 0.1f;

    void Start()
    {
        source.pitch += Random.Range(-pitchVariance, pitchVariance);
        source.volume += Random.Range(-volumeVariance, volumeVariance);

        source.Play();
    }

}
