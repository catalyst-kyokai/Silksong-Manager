# Документация: Аудио система

## Обзор

Аудио система в Silksong включает:

- **AudioManager** - главный менеджер звука
- **AudioEvent** - звуковые события
- **MusicCue** - музыкальные треки
- **AtmosCue** - атмосферные звуки

## AudioManager (AudioManager.cs)

Файл: `silksong_source_code/AudioManager.cs`

### Основные свойства

```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    // Громкости
    public float masterVolume;
    public float musicVolume;
    public float soundVolume;
    public float masterSoundVolume;
    
    // Аудио миксеры
    public AudioMixer masterMixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup soundGroup;
    public AudioMixerGroup atmosGroup;
}
```

### Основные методы

```csharp
// Воспроизведение звука
public void PlaySound(AudioClip clip);
public void PlaySound(AudioClip clip, float volume);
public void PlaySoundAtPoint(AudioClip clip, Vector3 position);

// Музыка
public void PlayMusic(MusicCue musicCue);
public void StopMusic();
public void PauseMusic();
public void ResumeMusic();

// Атмосфера
public void PlayAtmos(AtmosCue atmosCue);
public void StopAtmos();

// Снэпшоты
public void ApplyMasterSnapshot(AudioMixerSnapshot snapshot, float time);
```

## AudioEvent (AudioEvent.cs)

Контейнер для звуковых эффектов.

```csharp
public class AudioEvent : MonoBehaviour
{
    public AudioClip clip;
    public float volume = 1f;
    public float pitchMin = 1f;
    public float pitchMax = 1f;
    
    public void Play();
    public void PlayOneShot();
    public void PlayAtPoint(Vector3 point);
}
```

### AudioEventRandom

```csharp
public class AudioEventRandom : MonoBehaviour
{
    public AudioClip[] clips;
    public float volume = 1f;
    public float pitchVariation = 0.1f;
    
    public void Play()
    {
        if (clips.Length == 0) return;
        
        var clip = clips[Random.Range(0, clips.Length)];
        var pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        
        // Воспроизвести
    }
}
```

## MusicCue (MusicCue.cs)

Музыкальный трек.

```csharp
public class MusicCue : ScriptableObject
{
    public string cueName;
    public AudioClip introClip;
    public AudioClip loopClip;
    public float fadeInTime = 1f;
    public float fadeOutTime = 1f;
    
    public MusicChannelSync channelSync;
}

public enum MusicChannelSync
{
    None,
    ExploreAndAction,
    BossIntro
}
```

## AtmosCue (AtmosCue.cs)

Атмосферные звуки.

```csharp
public class AtmosCue : ScriptableObject
{
    public string cueName;
    public AudioClip[] clips;
    public float volume = 1f;
    public bool loop = true;
    public float fadeTime = 0.5f;
}
```

## MusicRegion (MusicRegion.cs)

Регион, включающий определённую музыку.

```csharp
public class MusicRegion : MonoBehaviour
{
    public MusicCue musicCue;
    public float enterDelay = 0f;
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            AudioManager.Instance.PlayMusic(musicCue);
        }
    }
}
```

## AtmosRegion (AtmosRegion.cs)

Регион атмосферных звуков.

```csharp
public class AtmosRegion : MonoBehaviour
{
    public AtmosCue atmosCue;
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            AudioManager.Instance.PlayAtmos(atmosCue);
        }
    }
}
```

## Примеры использования

### Воспроизведение звука

```csharp
public void PlayHitSound()
{
    var clip = Resources.Load<AudioClip>("Sounds/Hit");
    AudioManager.Instance.PlaySound(clip);
}
```

### Воспроизведение звука в позиции

```csharp
public void PlaySoundAt(AudioClip clip, Vector3 position, float volume = 1f)
{
    AudioSource.PlayClipAtPoint(clip, position, volume);
}
```

### Управление музыкой

```csharp
public void SetMusicVolume(float volume)
{
    AudioManager.Instance.musicVolume = volume;
}

public void FadeOutMusic(float duration)
{
    StartCoroutine(FadeMusicCoroutine(duration));
}

IEnumerator FadeMusicCoroutine(float duration)
{
    float startVolume = AudioManager.Instance.musicVolume;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        AudioManager.Instance.musicVolume = 
            Mathf.Lerp(startVolume, 0f, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    AudioManager.Instance.StopMusic();
}
```

## HeroAudioController (HeroAudioController.cs)

Аудио контроллер персонажа.

```csharp
public class HeroAudioController : MonoBehaviour
{
    public AudioClip footstepClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip dashClip;
    public AudioClip attackClip;
    public AudioClip hurtClip;
    
    public void PlayFootstep();
    public void PlayJump();
    public void PlayLand();
    public void PlayDash();
    public void PlayAttack();
    public void PlayHurt();
}
```

## Связанные файлы

- `AudioManager.cs` - Главный менеджер
- `AudioEvent.cs` - Звуковые события
- `AudioEventRandom.cs` - Случайные звуки
- `MusicCue.cs` - Музыкальные треки
- `AtmosCue.cs` - Атмосфера
- `MusicRegion.cs` - Музыкальные регионы
- `AtmosRegion.cs` - Атмосферные регионы
- `HeroAudioController.cs` - Аудио персонажа
