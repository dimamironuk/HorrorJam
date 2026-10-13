using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class LightningController : MonoBehaviour
{
    [Header("Audio Settings - Lightning")]
    [SerializeField] private AudioSource thunderSource;
    [SerializeField] private AudioClip[] thunderClips;
    [SerializeField] private Vector2 thunderDelay = new Vector2(0.3f, 1.5f);

    [Header("Audio Settings - Monster Scare")]
    [SerializeField] private AudioSource scareAudioSource;
    [SerializeField] private AudioClip[] scareClips;

    [Header("Audio Settings - Ambient Darkness Screams")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioClip[] ambientDarknessClips;
    [SerializeField] private Vector2 ambientInterval = new Vector2(8f, 20f); // Інтервал фонових криків у темряві

    [Header("Lighting Settings")]
    [SerializeField] private Light flashLight;
    [SerializeField] private float flashIntensity = 100000f;
    [SerializeField] private Vector2 flashCount = new Vector2(1, 3);

    [Header("Interval Settings")]
    [SerializeField] private Vector2 interval = new Vector2(4f, 9f);

    [Header("Player & Monster Dynamic Spawn")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private GameObject monsterObject;
    [SerializeField, Range(0f, 1f)] private float scareChance = 0.6f;
    [SerializeField] private Vector2 spawnDistanceRange = new Vector2(7f, 14f);
    [SerializeField] private LayerMask obstacleMask;

    [Header("State")]
    [SerializeField] private bool canSpawnMonster = false;

    private HDAdditionalLightData _hdLightData;
    private Coroutine _ambientCoroutine;

    private void Awake()
    {
        if (flashLight == null)
            flashLight = GetComponent<Light>();

        if (flashLight != null)
            flashLight.TryGetComponent(out _hdLightData);
    }

    private void Start()
    {
        SetIntensity(0f);
        StartCoroutine(Loop());
    }

    public void EnableMonsterSpawns()
    {
        if (!canSpawnMonster)
        {
            canSpawnMonster = true;
            if (_ambientCoroutine != null) StopCoroutine(_ambientCoroutine);
            _ambientCoroutine = StartCoroutine(AmbientDarknessLoop());
        }
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(interval.x, interval.y));

            if (flashLight != null)
            {
                flashLight.transform.rotation = Quaternion.Euler(
                    Random.Range(20f, 70f), Random.Range(0f, 360f), 0f);
            }

            StartCoroutine(PlayThunder());
            yield return StartCoroutine(Flash());
        }
    }

    private IEnumerator AmbientDarknessLoop()
    {
        while (canSpawnMonster)
        {
            yield return new WaitForSeconds(Random.Range(ambientInterval.x, ambientInterval.y));

            if (ambientAudioSource != null && ambientDarknessClips != null && ambientDarknessClips.Length > 0)
            {
                AudioClip clip = ambientDarknessClips[Random.Range(0, ambientDarknessClips.Length)];
                if (clip != null)
                {
                    ambientAudioSource.pitch = Random.Range(0.85f, 1.1f);
                    ambientAudioSource.PlayOneShot(clip, Random.Range(0.6f, 1f));
                }
            }
        }
    }

    private IEnumerator Flash()
    {
        bool showMonster = canSpawnMonster && monsterObject != null && playerCamera != null && Random.value <= scareChance;

        if (showMonster)
        {
            PositionMonsterInFrontOfPlayer();
            monsterObject.SetActive(true);
            PlayScareSfx();
        }

        int count = Random.Range((int)flashCount.x, (int)flashCount.y + 1);
        for (int i = 0; i < count; i++)
        {
            SetIntensity(flashIntensity * Random.Range(0.6f, 1f));
            yield return new WaitForSeconds(Random.Range(0.04f, 0.1f));
            SetIntensity(flashIntensity * 0.1f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
        }

        float t = 0f;
        float start = flashIntensity * 0.3f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            SetIntensity(Mathf.Lerp(start, 0f, t / 0.25f));
            yield return null;
        }
        SetIntensity(0f);

        if (showMonster)
        {
            monsterObject.SetActive(false);
        }
    }

    private void PlayScareSfx()
    {
        if (scareAudioSource != null && scareClips != null && scareClips.Length > 0)
        {
            AudioClip clip = scareClips[Random.Range(0, scareClips.Length)];
            if (clip != null)
            {
                scareAudioSource.pitch = Random.Range(0.95f, 1.05f);
                scareAudioSource.PlayOneShot(clip);
            }
        }
    }

    private void PositionMonsterInFrontOfPlayer()
    {
        float targetDistance = Random.Range(spawnDistanceRange.x, spawnDistanceRange.y);

        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 spawnPos = playerCamera.position + forward * targetDistance;

        if (Physics.Raycast(playerCamera.position, forward, out RaycastHit hit, targetDistance, obstacleMask))
        {
            spawnPos = hit.point - forward * 0.8f;
        }

        if (Physics.Raycast(spawnPos + Vector3.up * 3f, Vector3.down, out RaycastHit groundHit, 6f, obstacleMask))
        {
            spawnPos.y = groundHit.point.y;
        }

        monsterObject.transform.position = spawnPos;

        Vector3 lookDir = playerCamera.position - spawnPos;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
        {
            monsterObject.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    private IEnumerator PlayThunder()
    {
        yield return new WaitForSeconds(Random.Range(thunderDelay.x, thunderDelay.y));

        if (thunderSource != null && thunderClips != null && thunderClips.Length > 0)
        {
            var clip = thunderClips[Random.Range(0, thunderClips.Length)];
            if (clip != null)
            {
                thunderSource.pitch = Random.Range(0.9f, 1.05f);
                thunderSource.PlayOneShot(clip, Random.Range(0.7f, 1f));
            }
        }
    }

    private void SetIntensity(float value)
    {
        if (_hdLightData != null)
            _hdLightData.SetIntensity(value);
        else if (flashLight != null)
            flashLight.intensity = value;
    }
}