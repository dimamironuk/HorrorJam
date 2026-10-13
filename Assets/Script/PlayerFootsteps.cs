using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip sprintClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    [Header("Settings")]
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float sprintVolume = 0.8f;
    [SerializeField] private float jumpVolume = 0.7f;
    [SerializeField] private float landVolume = 0.7f;

    private bool _wasGrounded = true;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (audioSource != null)
            audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (audioSource == null || characterController == null) return;

        bool isGrounded = characterController.isGrounded;

        // Звук приземлення (коли щойно торкнулися землі)
        if (isGrounded && !_wasGrounded)
        {
            if (landClip != null)
            {
                audioSource.PlayOneShot(landClip, landVolume);
            }
        }

        // Звук стрибка (при натисканні пробілу на землі)
        if (isGrounded && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)))
        {
            if (jumpClip != null)
            {
                audioSource.PlayOneShot(jumpClip, jumpVolume);
            }
        }

        // Логіка кроків (працює ТІЛЬКИ на землі)
        if (isGrounded)
        {
            bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;

            if (isMoving)
            {
                bool isSprinting = Input.GetKey(KeyCode.LeftShift);
                AudioClip targetClip = isSprinting ? sprintClip : walkClip;
                if (targetClip == null) targetClip = walkClip;

                float targetVolume = isSprinting ? sprintVolume : walkVolume;

                if (targetClip != null)
                {
                    if (!audioSource.isPlaying || audioSource.clip != targetClip)
                    {
                        audioSource.clip = targetClip;
                        audioSource.volume = targetVolume;
                        audioSource.loop = true;
                        audioSource.Play();
                    }
                    else
                    {
                        audioSource.volume = targetVolume;
                    }
                }
            }
            else
            {
                StopFootsteps();
            }
        }
        else
        {
            StopFootsteps();
        }

        _wasGrounded = isGrounded;
    }

    private void StopFootsteps()
    {
        if (audioSource.isPlaying && audioSource.loop)
        {
            audioSource.Stop();
        }
    }
}