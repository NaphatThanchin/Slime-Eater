using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// เสียงทั้งเกม: เพลงพื้นหลัง (ฉาก Bonus time สลับเป็นเพลงคำถาม) + เสียงประกอบ + เสียงกดปุ่ม UI ทุกปุ่ม
public class AudioManager : MonoBehaviour
{
    public enum Sfx { Jump, Coin, Click, Eat }
    static AudioManager instance;
    public AudioClip musicClip, bonusMusic;      // เพลงปกติ / เพลงตอนทายคำถาม
    public AudioClip[] sfxClips;                // เรียงตาม Sfx
    public float[] sfxGain = { 0.35f, 0.5f, 0.9f, 1f }; // ปรับให้ดังพอ ๆ กัน (Jump ดังมาก ลดลง / Eat เบา ใช้เต็ม)
    public float musicGain = 0.5f, bonusGain = 1f;        // เพลงหลักดังกว่าเพลงคำถาม ~2 เท่า จึงลดครึ่ง
    public Slider volumeSlider, sfxSlider;      // Slider ของฉากนี้
    public const float MusicMax = 0.13f, SfxMax = 0.18f; // ดังสุดของแถบ: แถบกลาง (0.5) = ระดับที่ทดสอบแล้วว่าพอดี เลื่อนเบาลง/ดังขึ้นได้อีกเท่าตัว
    AudioSource music, sfx, jump;               // jump แยกช่อง: กระโดดใหม่ตัดเสียงเก่า ไม่ซ้อนกัน
    float gain = 1f, fade = 1f, duck = 1f, lastCoin;

    void Awake()
    {
        if (instance != null && instance != this) { instance.Bind(volumeSlider, sfxSlider); Destroy(gameObject); return; } // มีตัวเดิมแล้ว -> ใช้ตัวเดิม
        instance = this;
        DontDestroyOnLoad(gameObject);
        music = gameObject.AddComponent<AudioSource>();
        sfx = gameObject.AddComponent<AudioSource>();
        jump = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        foreach (var s in GetComponents<AudioSource>()) { s.playOnAwake = false; s.ignoreListenerPause = true; }
        Bind(volumeSlider, sfxSlider);
        SceneManager.sceneLoaded += OnScene;
        OnScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy() { if (instance == this) SceneManager.sceneLoaded -= OnScene; }

    void OnScene(Scene s, LoadSceneMode m) // ทุกฉาก: ผูกเสียงปุ่ม + เลือกเพลง
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { b.onClick.RemoveListener(Click); b.onClick.AddListener(Click); }
        bool quiz = s.name == "BonusTimeScene";
        var clip = quiz ? bonusMusic : musicClip;
        if (clip && music.clip != clip) { StopAllCoroutines(); StartCoroutine(SwapMusic(clip, quiz ? bonusGain : musicGain)); }
    }

    IEnumerator SwapMusic(AudioClip clip, float g) // ค่อยๆ เบาลง -> เปลี่ยนเพลง -> ค่อยๆ ดังขึ้น (ไม่ตัดกระตุก)
    {
        for (; music.isPlaying && fade > 0f; fade -= Time.unscaledDeltaTime * 3f) yield return null;
        music.clip = clip;
        gain = g;
        music.Play();
        for (fade = 0f; fade < 1f; fade += Time.unscaledDeltaTime * 2f) yield return null;
        fade = 1f;
    }

    void Update() // ความดังเพลง = slider x ความดังของไฟล์ x เฟด x ลดเสียงตอนเล่นเสียงกิน
    {
        duck = Mathf.MoveTowards(duck, sfx.isPlaying && sfx.clip == Clip(Sfx.Eat) ? 0.45f : 1f, Time.unscaledDeltaTime * 3f);
        music.volume = SaveData.D.musicVolume * MusicMax * gain * fade * duck;
    }

    AudioClip Clip(Sfx s) => sfxClips != null && (int)s < sfxClips.Length ? sfxClips[(int)s] : null;

    public static void Play(Sfx s) // เรียกจากที่ไหนก็ได้ เช่น AudioManager.Play(Sfx.Coin)
    {
        var a = instance;
        var clip = a ? a.Clip(s) : null;
        if (!clip) return;
        float v = SaveData.D.sfxVolume * SfxMax * a.sfxGain[(int)s];
        if (s == Sfx.Jump) { a.jump.Stop(); a.jump.clip = clip; a.jump.volume = v; a.jump.Play(); return; }
        if (s == Sfx.Coin) { if (Time.unscaledTime - a.lastCoin < 0.06f) return; a.lastCoin = Time.unscaledTime; } // เก็บหลายเหรียญติดกันไม่ให้เสียงซ้อนจนแตก
        if (s == Sfx.Eat) { a.sfx.clip = clip; a.sfx.volume = v; a.sfx.Play(); return; }  // ใช้ช่องหลักเพื่อให้รู้ว่ากำลังเล่น (ลดเพลง)
        a.sfx.PlayOneShot(clip, v);
    }

    void Click() => Play(Sfx.Click);

    void Bind(Slider musicSlider, Slider sfxSlider) // ผูก Slider กับความดัง (ค่าบันทึกใน SaveData)
    {
        if (musicSlider) { musicSlider.SetValueWithoutNotify(SaveData.D.musicVolume); musicSlider.onValueChanged.AddListener(v => SaveData.D.musicVolume = v); }
        if (sfxSlider) { sfxSlider.SetValueWithoutNotify(SaveData.D.sfxVolume); sfxSlider.onValueChanged.AddListener(v => SaveData.D.sfxVolume = v); }
    }
}
