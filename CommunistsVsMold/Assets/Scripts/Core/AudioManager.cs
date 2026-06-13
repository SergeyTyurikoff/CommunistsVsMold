using UnityEngine;

namespace Kommunisty
{
    /// <summary>
    /// Звук игры: фоновая музыка (плейлист треш-метала по кругу) + SFX (выстрелы,
    /// удары/смерть, прыжок, портал). Синглтон; методы вызываются null-safe как
    /// AudioManager.Instance?.PlayXxx(). Клипы назначаются в инспекторе.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Музыка (плейлист по кругу)")]
        [SerializeField] AudioClip[] musicTracks;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.5f;

        [Header("SFX — выстрелы")]
        [SerializeField] AudioClip shotPistol;
        [SerializeField] AudioClip shotRifle;
        [SerializeField] AudioClip shotSmg;
        [SerializeField] AudioClip shotShotgun;
        [SerializeField] AudioClip shotGas;
        [SerializeField] AudioClip swingSabre;

        [Header("SFX — бой / прочее")]
        [SerializeField] AudioClip enemyHit;
        [SerializeField] AudioClip enemyDeath;
        [SerializeField] AudioClip playerHit;
        [SerializeField] AudioClip playerDown;
        [SerializeField] AudioClip jump;
        [SerializeField] AudioClip portal;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.9f;

        AudioSource music;
        AudioSource sfx;
        int trackIndex;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false; music.loop = false; music.volume = musicVolume;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false; sfx.volume = sfxVolume;
        }

        void Start() => PlayNextTrack();

        void Update()
        {
            // Плейлист по кругу: трек кончился — запускаем следующий.
            if (music != null && !music.isPlaying && musicTracks != null && musicTracks.Length > 0)
                PlayNextTrack();
        }

        void PlayNextTrack()
        {
            if (music == null || musicTracks == null || musicTracks.Length == 0) return;
            var clip = musicTracks[trackIndex % musicTracks.Length];
            trackIndex++;
            if (clip == null) return;
            music.clip = clip;
            music.volume = musicVolume;
            music.Play();
        }

        /// <summary>Разовый SFX (с учётом общей громкости).</summary>
        public void Sfx(AudioClip clip, float volScale = 1f)
        {
            if (clip == null || sfx == null) return;
            sfx.PlayOneShot(clip, sfxVolume * volScale);
        }

        /// <summary>Звук выстрела по типу оружия/патрона.</summary>
        public void PlayShot(WeaponKind kind, AmmoKind ammo)
        {
            AudioClip c;
            if (kind == WeaponKind.Melee) c = swingSabre;
            else if (kind == WeaponKind.Gas) c = shotGas;
            else if (kind == WeaponKind.Shotgun) c = shotShotgun;
            else
            {
                switch (ammo)
                {
                    case AmmoKind.Rifle: c = shotRifle; break;
                    case AmmoKind.Machinegun: c = shotSmg; break;
                    case AmmoKind.Shells: c = shotShotgun; break;
                    default: c = shotPistol; break;
                }
            }
            Sfx(c);
        }

        public void PlayEnemyHit()   => Sfx(enemyHit);
        public void PlayEnemyDeath() => Sfx(enemyDeath);
        public void PlayPlayerHit()  => Sfx(playerHit);
        public void PlayPlayerDown() => Sfx(playerDown);
        public void PlayJump()       => Sfx(jump, 0.7f);
        public void PlayPortal()     => Sfx(portal);
    }
}
