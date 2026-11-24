using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    const string VolumeKey = "MusicVolume";
    static AudioManager instance;  // ตัวเดียวทั้งเกม เพลงเล่นต่อเนื่องข้ามฉาก
    public AudioClip musicClip;    // เพลงประกอบ
    public Slider volumeSlider;    // Slider ในหน้าตั้งค่า
    [Range(0f, 1f)] public float defaultVolume = 0.4f;
    AudioSource source;

    void Awake()
    {
        if (instance != null && instance != this) // มีตัวเดิมแล้ว -> ให้ตัวเดิมใช้ Slider ของฉากนี้
        {
            instance.BindSlider(volumeSlider);
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.ignoreListenerPause = true;
        source.volume = PlayerPrefs.GetFloat(VolumeKey, defaultVolume);
        if (musicClip) { source.clip = musicClip; source.Play(); }
        else Debug.Log("[Audio] ยังไม่ได้ใส่เพลง (Music Clip ว่าง)");
        BindSlider(volumeSlider);
    }

    void BindSlider(Slider s) // ผูก Slider กับความดังเพลง
    {
        if (!s) return;
        s.SetValueWithoutNotify(source.volume);
        s.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float v)
    {
        source.volume = v;
        PlayerPrefs.SetFloat(VolumeKey, v);
    }
}
