using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ตั้งหน้าต่าง Game เป็น 16:9 และซูมให้พอดี (เห็นภาพเต็มจอไม่โดนตัดขอบ)
public static class GameViewFixer
{
    [MenuItem("Tools/Slime Eater/Fix Game View (16:9 Fit)", priority = 31)]
    public static string Fix()
    {
        var log = "";
        try
        {
            var asm = typeof(EditorWindow).Assembly;
            var gvType = asm.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gvType, false, null, false);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = single.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { (int)sizesType.GetProperty("currentGroupType").GetValue(sizes) });
            var texts = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
            int idx = Array.FindIndex(texts, t => t.StartsWith("16:9"));
            if (idx >= 0) { gvType.GetMethod("SizeSelectionCallback", F).Invoke(gv, new object[] { idx, null }); log += "size=" + texts[idx] + " "; }
            var snap = gvType.GetMethod("SnapZoom", F);
            var minScale = gvType.GetProperty("minScale", F);
            if (snap != null && minScale != null) { snap.Invoke(gv, new object[] { (float)minScale.GetValue(gv) }); log += "zoom=fit"; }
            gv.Repaint();
        }
        catch (Exception e) { log += "ตั้งค่าอัตโนมัติไม่ได้: " + e.Message; }
        Debug.Log("[GameViewFixer] " + log);
        return log;
    }
}
