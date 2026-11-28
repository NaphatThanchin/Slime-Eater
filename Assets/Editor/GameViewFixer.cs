using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ตั้งขนาดหน้าต่าง Game: 16:9 ซูมพอดี (คอม) หรือขนาดจอมือถือแนวตั้งไว้ทดสอบ
public static class GameViewFixer
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static readonly Assembly Asm = typeof(EditorWindow).Assembly;

    [MenuItem("Tools/Slime Eater/Fix Game View (16:9 Fit)", priority = 31)]
    public static string Fix() => Select(t => t.StartsWith("16:9"), null);

    [MenuItem("Tools/Slime Eater/Game View Phone Portrait (1080x2340)", priority = 32)]
    public static string Phone() => SetSize(1080, 2340);

    public static string SetSize(int w, int h) => Select(t => t.Contains(w + "x" + h), new Vector2Int(w, h)); // ไม่มีขนาดนี้ -> เพิ่มให้

    static string Select(Func<string, bool> match, Vector2Int? add)
    {
        try
        {
            var gvType = Asm.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gvType, false, null, false);
            var sizesType = Asm.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { (int)sizesType.GetProperty("currentGroupType").GetValue(sizes) });
            string[] Texts() => (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
            int idx = Array.FindIndex(Texts(), t => match(t));
            if (idx < 0 && add.HasValue)
            {
                var typeEnum = Asm.GetType("UnityEditor.GameViewSizeType");
                var size = Asm.GetType("UnityEditor.GameViewSize").GetConstructor(new[] { typeEnum, typeof(int), typeof(int), typeof(string) })
                    .Invoke(new object[] { Enum.ToObject(typeEnum, 1), add.Value.x, add.Value.y, "Phone" });   // 1 = ขนาดตายตัว
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                idx = Array.FindIndex(Texts(), t => match(t));
            }
            if (idx < 0) return "ไม่พบขนาด";
            gvType.GetMethod("SizeSelectionCallback", F).Invoke(gv, new object[] { idx, null });
            var snap = gvType.GetMethod("SnapZoom", F);
            var minScale = gvType.GetProperty("minScale", F);
            if (snap != null && minScale != null) snap.Invoke(gv, new object[] { (float)minScale.GetValue(gv) }); // ซูมให้เห็นทั้งจอ
            gv.Repaint();
            return "size=" + Texts()[idx];
        }
        catch (Exception e) { return "ตั้งค่าอัตโนมัติไม่ได้: " + e.Message; }
    }
}
