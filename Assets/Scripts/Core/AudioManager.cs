using UnityEngine;
using UnityEngine.UI;

// เพลงประกอบ: มีตัวเดียวทั้งเกม เล่นต่อเนื่องข้ามฉาก ปรับเสียงด้วย Slider ในหน้าตั้งค่า
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    static AudioManager instance;
    public AudioClip musicClip;
    public Slider volumeSlider;   // Slider ของฉากนี้
    AudioSource source;

    void Awake()
    {
        if (instance != null && instance != this) { instance.BindSlider(volumeSlider); Destroy(gameObject); return; } // มีตัวเดิมแล้ว -> ใช้ตัวเดิม
        instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.ignoreListenerPause = true;
        source.volume = SaveData.D.musicVolume;
        if (musicClip) { source.clip = musicClip; source.Play(); }
        BindSlider(volumeSlider);
    }

    void BindSlider(Slider s) // ผูก Slider กับความดัง
    {
        if (!s) return;
        s.SetValueWithoutNotify(source.volume);
        s.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float v) { source.volume = v; SaveData.D.musicVolume = v; } // บันทึกตอนปิดหน้าตั้งค่า
}
