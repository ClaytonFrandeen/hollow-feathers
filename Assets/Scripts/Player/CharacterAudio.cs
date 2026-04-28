using UnityEngine;

public class CharacterAudio : MonoBehaviour
{
    
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip swordSwingClip;
    [SerializeField] private AudioClip swordHitClip;
    
    // Called from animation event on footstep frames
    public void PlayFootstep()
    {
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip, 0.8f);
    }

    // Called from animation event at the start of a swing
    public void PlaySwordSwing()
    {
        audioSource.PlayOneShot(swordSwingClip);
    }
}
