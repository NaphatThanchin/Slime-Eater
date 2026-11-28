using System.Collections.Generic;

// ชุดอุปสรรคที่ออกแบบไว้ (chunk) 4 ระดับ: แต่ละแถว 3 ตัวอักษร = เลน ซ้าย/กลาง/ขวา, | = จังหวะถัดไป (0.75 วิ)
// a หิน+เหรียญโค้ง  R หิน  B หินใหญ่  H ท่อนไม้สูงทั้งแถว (h = +เหรียญต่ำเลนนี้)  T/t ต้นไม้ล้มจากขวา/ซ้าย (+เหรียญโค้งเลนนั้น)
// P ขอนยาวขึ้นได้ (ทางลาด+เหรียญบนขอน)  M ท่อนไม้ยาวหล่นจากฟ้า (หลบเลน)  c เหรียญ  . ว่าง
public static class Chunks
{
    public static readonly string[][] Tier =
    {
        new[] { "a..", ".a.", "..a", "BcB", "cBB", "BBc", "HhH", "..T", "t..", "P..", ".P.", "..P", "c.c|.c." },          // 1 ท่าเดียว
        new[] { "aaa", "B.B|.a.", "a.B|HhH", "BcB|BcB", "Bc.|.cB", "a.a|HhH", "P.B|P.B", ".B.|a.a" },                  // 2 สลับท่า
        new[] { "M..", ".M.", "..M", "P.B|P.a", "aaa|HhH", "B.a|.Ba", "M.c|..B" },                                     // 3 ชั้นบน / ของเคลื่อนที่
        new[] { "MBc", "M.M|.h.", "aBa|BcB|HhH", "P.M|P.a", ".M.|a.a|HhH" },                                           // 4 อ่านทางเร็ว
    };
    public const float RowTime = 0.75f;   // ห่างระหว่างแถว (วินาที) > ลอยตัวตอนกระโดด 0.64 วิ

    public static bool Blocks(char c) => c == 'B' || c == 'M';               // ต้องหลบเลน
    public static bool Full(string row) => row.IndexOf('H') >= 0 || row.IndexOf('h') >= 0 || row.IndexOf('T') >= 0 || row.IndexOf('t') >= 0;

    public static int SafeLane(string chunk) // เลนเข้าที่ปลอดภัย/มีเหรียญ (ให้เส้นทางเหรียญพาไป)
    {
        string row = chunk.Split('|')[0];
        int best = 1;
        for (int i = 0; i < 3; i++)
        {
            char c = row[i];
            if (c == 'c' || c == 'a' || c == 'P' || c == 'h' || c == 'T' || c == 't') return i - 1;
            if (!Blocks(c)) best = i;
        }
        return best - 1;
    }

    public static List<string> Validate() // ตรวจทุกชุด: ทุกแถวมีเลนผ่านได้ + ทุกตัวอักษรรู้จัก
    {
        var errors = new List<string>();
        for (int t = 0; t < Tier.Length; t++)
            foreach (var chunk in Tier[t])
                foreach (var row in chunk.Split('|'))
                {
                    if (row.Length != 3) { errors.Add("tier " + (t + 1) + " '" + chunk + "' แถวไม่ใช่ 3 ตัว"); continue; }
                    int open = 0;
                    foreach (char c in row) { if ("aRBHhTtPMc.".IndexOf(c) < 0) errors.Add("ตัวอักษรไม่รู้จัก '" + c + "' ใน " + chunk); if (!Blocks(c)) open++; }
                    if (open == 0) errors.Add("tier " + (t + 1) + " '" + chunk + "' แถว " + row + " ไม่มีเลนผ่าน");
                }
        return errors;
    }
}
