using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoxCollisionSound : MonoBehaviour
{
    [SerializeField] private AudioClip[] impactClips;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float collisionVelocityThreshold = 0.4f;
    [SerializeField] private float fullVolumeSpeed = 4f;
    [SerializeField] private float minVolume = 0.35f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private float cooldownTime = 0.1f;
    [SerializeField] private bool debugLogs = false;

    private Rigidbody _rb;
    private float _lastPlayTime = -10f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (audioSource == null || audioSource.GetComponentInParent<Rigidbody>() != _rb)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.priority = 64;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (impactClips == null || impactClips.Length == 0) return;
        if (Time.time - _lastPlayTime < cooldownTime) return;

        float speed = collision.relativeVelocity.magnitude;

        if (speed < collisionVelocityThreshold)
        {
            if (debugLogs)
                Debug.Log($"[BoxSound] '{name}' too soft vs '{collision.gameObject.name}': {speed:F2}");
            return;
        }

        AudioClip clip = impactClips[Random.Range(0, impactClips.Length)];
        if (clip == null) return;

        float t = Mathf.Clamp01(speed / Mathf.Max(0.01f, fullVolumeSpeed));
        float volume = Mathf.Lerp(minVolume, maxVolume, t);

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
        _lastPlayTime = Time.time;

        if (debugLogs)
            Debug.Log($"[BoxSound] '{name}' played '{clip.name}' speed {speed:F2} vol {volume:F2}");
    }
}