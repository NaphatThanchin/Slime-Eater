using TMPro;
using UnityEngine;
using UnityEngine.UI;

// หน้าภารกิจ 3 แท็บ: ประจำวัน / หลัก 9 หน้า (หน้าที่ทำครบหายไป) / ต่อเนื่อง
public class MissionPanel : MonoBehaviour
{
    public GameObject panel, pagesRoot;
    public Image[] tabs;                 // ปุ่มแท็บ 3 อัน
    public GameObject[] pageButtons;     // เลขหน้า 1-9
    public GameObject[] rows;            // 3 แถว
    public TMP_Text[] texts;
    public Image[] bars;
    public TMP_Text pageTitle, footer;
    public MenuShop shop;                // อัปเดตเหรียญหลังรับรางวัล
    static readonly Color TabOn = new Color(1f, 0.62f, 0.15f), TabOff = new Color(0.2f, 0.55f, 0.6f);
    int tab = 1, page;

    void Start() => panel.SetActive(false);

    public void Open()
    {
        MissionSystem.CheckAll();
        tab = 1;
        page = FirstOpenPage();
        panel.SetActive(true);
        Refresh();
    }

    public void Close() => panel.SetActive(false);
    public void ShowTab(int t) { tab = t; Refresh(); }
    public void ShowPage(int p) { page = p; Refresh(); }

    void Refresh()
    {
        for (int t = 0; t < tabs.Length; t++) tabs[t].color = t == tab ? TabOn : TabOff;
        pagesRoot.SetActive(tab == 1);
        for (int p = 0; p < pageButtons.Length; p++) pageButtons[p].SetActive(!MissionSystem.PageDone(p));
        if (MissionSystem.PageDone(page)) page = FirstOpenPage();
        pageTitle.text = tab == 0 ? "ภารกิจประจำวัน (สุ่มใหม่ทุกวัน)" : tab == 2 ? "ภารกิจต่อเนื่อง (ทำครบแล้วเป้าเพิ่มขึ้น)"
                       : MissionSystem.AllMainDone ? "ทำภารกิจหลักครบทุกหน้าแล้ว!" : "หน้า " + (page + 1) + " / " + MissionSystem.Pages;
        footer.text = MissionSystem.Footer(tab);
        var list = MissionSystem.Rows(tab, page);
        for (int r = 0; r < rows.Length; r++)
        {
            rows[r].SetActive(r < list.Count);
            if (r >= list.Count) continue;
            var m = list[r];
            texts[r].text = m.title + "\n<size=70%>" + (m.done ? "สำเร็จแล้ว" : m.progress.ToString("N0") + " / " + m.target.ToString("N0")) + "   รางวัล " + m.reward.ToString("N0") + " เหรียญ</size>";
            bars[r].fillAmount = m.done ? 1f : (float)m.progress / m.target;
        }
        shop.Refresh();
    }

    static int FirstOpenPage() // หน้าแรกที่ยังทำไม่ครบ
    {
        int p = 0;
        while (p < MissionSystem.Pages - 1 && MissionSystem.PageDone(p)) p++;
        return p;
    }
}
