using System.Collections;
using UnityEngine;

namespace AdvancedRogueLikeandPuzzleSystem
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;
        public AudioSource audioSource;
        public AudioClip audioClip_CoinUpdate;
        public AudioClip audioClip_ObjectiveCompleted;
        public AudioClip audioClip_ObjectiveNew;
        public AudioClip audioClip_WoodenDoor;
        public AudioClip audioClip_MetalDoor;
        public AudioClip audioClip_Door_Close;
        public AudioClip audioClip_Grab;
        public AudioClip audioClip_PutOnWeapon;
        public AudioClip audioClip_PutOffWeapon;
        public AudioClip[] audioClips_Hit;
        public AudioClip[] audioClips_Whoosh;
        public AudioSource audioSource_Swimming;
        public AudioClip[] audioClip_ShieldImpact;
        public AudioClip[] audioClip_Die;
        public AudioClip audioClip_Unlock;
        public AudioClip audioClip_ForceLockedDoor;
        public AudioClip audioClip_Potion;
        public AudioClip audioClip_Sprint;
        public AudioClip audioClip_knifeOut;
        public AudioClip audioClip_DoorExplosion;
        public AudioClip audioClip_Chest_Open;
        public AudioClip audioClip_Door_Open_With_Light;
        public AudioClip audioClip_Arm_Pulling;
        public AudioClip audioClip_ButtonPressing;
        public AudioClip[] audioClip_Footsteps;

        private void Awake()
        {
            instance = this;
        }

        private void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (audioSource == null || clip == null) return;
            audioSource.PlayOneShot(clip, volume);
        }

        private static AudioClip PickRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public void Play_ButtonPressing()
        {
            PlayClip(audioClip_ButtonPressing);
        }

        public void Play_Arm_Pulling()
        {
            PlayClip(audioClip_Arm_Pulling);
        }

        public void Play_Door_Open_With_Light()
        {
            PlayClip(audioClip_Door_Open_With_Light);
        }

        public void Play_Door_UnLock()
        {
            PlayClip(audioClip_Unlock);
        }

        public void Play_Chest_Open()
        {
            PlayClip(audioClip_Chest_Open);
        }

        public void Play_Door_Explosion()
        {
            PlayClip(audioClip_DoorExplosion);
        }

        public void Play_Door_KnifeOut()
        {
            PlayClip(audioClip_knifeOut);
        }

        public void Play_Sprint()
        {
            PlayClip(audioClip_Sprint);
        }

        public void Play_Potion_Dring()
        {
            PlayClip(audioClip_Potion);
        }

        public void Play_Door_TryOpen()
        {
            PlayClip(audioClip_ForceLockedDoor);
        }

        public void Play_Door_Open(string material)
        {
            if (material == "Metal")
            {
                PlayClip(audioClip_MetalDoor);
            }
            else if (material == "Wooden")
            {
                PlayClip(audioClip_WoodenDoor);
            }
        }

        public void Play_Door_Close()
        {
            PlayClip(audioClip_Door_Close);
        }

        public void Play_Unlocking()
        {
            PlayClip(audioClip_Unlock);
        }

        public void Play_Coin()
        {
            PlayClip(audioClip_CoinUpdate);
        }

        public void Play_ObjectiveCompleted()
        {
            PlayClip(audioClip_ObjectiveCompleted);
        }

        public void Play_ObjectiveNew()
        {
            PlayClip(audioClip_ObjectiveNew);
        }

        public void Play_ShieldImpact()
        {
            PlayClip(PickRandomClip(audioClip_ShieldImpact));
        }

        public void Play_Die()
        {
            PlayClip(PickRandomClip(audioClip_Die));
        }

        public void Play_Grab()
        {
            PlayClip(audioClip_Grab);
        }

        public void Play_PutOnWeapon()
        {
            PlayClip(audioClip_PutOnWeapon);
        }

        public void Play_PutOffWeapon()
        {
            PlayClip(audioClip_PutOffWeapon);
        }

        public void Play_WeaponHit()
        {
            PlayClip(PickRandomClip(audioClips_Hit), 0.5f);
        }

        public void Play_WeaponWhoosh()
        {
            StartCoroutine(Play_WeaponWhooshAsync());
        }

        IEnumerator Play_WeaponWhooshAsync()
        {
            yield return new WaitForSeconds(0.4f);
            PlayClip(PickRandomClip(audioClips_Whoosh), 0.5f);
        }

        public AudioClip Get_RandomFootstep()
        {
            return PickRandomClip(audioClip_Footsteps);
        }

        public void Play_Swimming()
        {
            if (audioSource_Swimming == null) return;
            if (!audioSource_Swimming.isPlaying)
            {
                audioSource_Swimming.loop = true;
                audioSource_Swimming.Play();
            }
        }

        public void Stop_Swimming()
        {
            if (audioSource_Swimming == null) return;
            if (audioSource_Swimming.isPlaying)
            {
                audioSource_Swimming.loop = false;
            }
        }
    }
}
