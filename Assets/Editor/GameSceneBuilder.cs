using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor.Events;

public static class GameSceneBuilder
{
    // ------------------------------------------------------------------
    //  Paths
    // ------------------------------------------------------------------
    const string ArtUI     = "Assets/Art/UI/icon/";
    const string ArtGen    = "Assets/Art/UI/Effect/";
    const string ArtScreen = "Assets/Art/UI/Screens/";
    const string ArtAnimal = "Assets/Art/Animals/";
    const string HC        = "Assets/Hyper_Casual_UI/Sprites/";
    const string Kit       = "Assets/Hyper_Casual_UI/Stylized Nature MegaKit[Standard]/FBX (Unity)/";
    const string MatDir    = "Assets/Materials/";
    const string PrefabDir = "Assets/Prefabs/";
    const string SceneDir  = "Assets/Scenes/";
    const string FontDir   = "Assets/Fonts/";
    const string AnimalSO  = "Assets/Resources/Animals/";

    static readonly Vector2 RefRes = new Vector2(1920, 1080);

    static TMP_FontAsset thaiFont;
    static Material fontOutline;
    static TMP_FontAsset titleFont;
    static Material titleOutline;
    static Material spriteMat;

    // 21 สัตว์: ชื่อ + คำใบ้ภาษาไทยแบบอ้อมๆ
    static readonly string[,] AnimalTable =
    {
        { "ant",      "ฉันตัวเล็กจิ๋วแต่อยู่รวมกันเป็นอาณาจักรใหญ่ ขยันเดินเรียงแถวขนอาหารที่หนักกว่าตัวเองหลายเท่า ฉันคือใคร" },
        { "bat",      "ฉันมีปีกบินได้ในความมืดแต่ไม่ใช่พวกนก ชอบห้อยหัวหลับพักผ่อนอยู่ในถ้ำตอนกลางวันและใช้เสียงสะท้อนนำทาง ฉันคือใคร" },
        { "bear",     "ฉันตัวใหญ่ขนหนา หูกลมเล็ก หางสั้นปุ๊ก เดินเต็มฝ่าเท้าและลุกยืนสองขาชะเง้อมองได้เหมือนคน ชอบดักจับปลาในลำธารและกินผลไม้และน้ำผึ้งป่า ฉันคือใคร" },
        { "bee",      "ฉันมีลำตัวลายเหลืองดำ บินตอมดอกไม้จากต้นนั้นไปต้นนี้เพื่อเก็บน้ำหวานมาสร้างรังหกเหลี่ยม ฉันคือใคร" },
        { "bird",     "ฉันมีจะงอยปากและขนปกคลุมลำตัว กางปีกโผบินบนท้องฟ้าและทำรังอยู่ตามกิ่งไม้สูง ฉันคือใคร" },
        { "cat",      "ฉันชอบเดินย่องเงียบกริบ มีหนวดยาวช่วยกะระยะ ชอบคลอเคลียและส่งเสียงครางเบาๆ ในลำคอเวลาอารมณ์ดี ฉันคือใคร" },
        { "fish",     "ฉันหายใจผ่านเหงือกและใช้ครีบแหวกว่ายไปมาอยู่ในสายน้ำตลอดเวลาโดยไม่เคยหลับตา ฉันคือใคร" },
        { "frog",     "ฉันมีผิวหนังชุ่มชื้น อาศัยได้ทั้งบนบกและในน้ำ มีขาหลังแข็งแรงไว้กระโดดไกลและชอบส่งเสียงร้องเวลาฝนตก ฉันคือใคร" },
        { "duck",     "ฉันมีปากแบนและเท้าพังผืดเหมือนใบพาย ลอยตัวว่ายน้ำเก่งและเดินเตาะแตะอยู่ริมสระ ฉันคือใคร" },
        { "dog",      "ฉันจมูกไวและซื่อสัตย์ต่อเจ้าของมาก เวลาดีใจมักจะกระดิกหางไปมาต้อนรับอยู่หน้าบ้าน ฉันคือใคร" },
        { "chicken",  "ฉันมีหงอนสีแดงบนหัว บินได้ไม่ไกล ชอบคุ้ยเขี่ยหาอาหารตามพื้นดินและส่งเสียงปลุกผู้คนยามเช้าตรู่ ฉันคือใคร" },
        { "cow",      "ฉันเป็นสัตว์เคี้ยวเอื้องตัวโตใจดี ชอบเล็มหญ้าอยู่ในทุ่งกว้างและผลิตน้ำนมสีขาวให้ผู้คนดื่ม ฉันคือใคร" },
        { "pig",      "ฉันมีจมูกแบนกลมไว้ดมกลิ่นและดันดิน หางม้วนเป็นเกลียวน่ารัก ชอบกินเก่งและนอนแช่โคลนคลายร้อน ฉันคือใคร" },
        { "spider",   "ฉันมีแปดขาและไม่ใช่แมลง ชอบชักใยเป็นตาข่ายลวดลายสวยงามไว้ดักจับเหยื่อตามมุมต่างๆ ฉันคือใคร" },
        { "snake",    "ฉันไม่มีทั้งแขนและขาแต่เคลื่อนที่ไปข้างหน้าได้อย่างว่องไวด้วยการเลื้อยคดเคี้ยวไปตามพื้น ฉันคือใคร" },
        { "monkey",   "ฉันแสนซนและว่องไว ใช้มือและหางโหนกิ่งไม้จากต้นหนึ่งไปอีกต้นหนึ่งได้อย่างคล่องแคล่วและชอบกินผลไม้สีเหลือง ฉันคือใคร" },
        { "lion",     "ฉันได้รับฉายาว่าเป็นเจ้าป่า ตัวผู้มีแผงคอหนาสง่างามและมีเสียงคำรามกึกก้องไปทั่วทุ่งหญ้า ฉันคือใคร" },
        { "tiger",    "ฉันเป็นนักล่าตระกูลเดียวกับแมวป่าลำตัวสีส้ม มีลายพาดกลอนสีดำเป็นเอกลักษณ์ไม่ซ้ำใคร ฉันคือใคร" },
        { "elephant", "ฉันเป็นสัตว์บกที่ตัวใหญ่ที่สุด มีใบหูแผ่นกว้างและจมูกยาวทรงพลังที่ใช้หยิบจับอาหารและพ่นน้ำได้ ฉันคือใคร" },
        { "zebra",    "ฉันรูปร่างคล้ายม้าที่วิ่งอยู่กลางทุ่งหญ้าสะวันนา แต่ทั้งตัวมีลวดลายสีขาวสลับดำสะดุดตา ฉันคือใคร" },
        { "giraffe",  "ฉันเป็นสัตว์ที่มีลำคอยาวที่สุดในโลก ลำตัวมีลายคล้ายตารางสีน้ำตาล ช่วยให้ยืดคอกินยอดไม้สูงๆ ได้สบาย ฉันคือใคร" },
    };

    [MenuItem("Tools/Slime Eater/Build All Scenes", priority = 0)]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            EditorUtility.DisplayProgressBar("Slime Eater", "Import settings", 0.1f);
            EnsureFolders();
            ConfigureImporters();
            GenerateFxSprites();
            EditorUtility.DisplayProgressBar("Slime Eater", "Thai font + materials", 0.25f);
            CreateFont();
            CreateMaterials();
            EditorUtility.DisplayProgressBar("Slime Eater", "Animal data", 0.4f);
            CreateAnimalAssets();
            EditorUtility.DisplayProgressBar("Slime Eater", "Prefabs", 0.5f);
            CreatePrefabs();
            CreateCharacters();
            EditorUtility.DisplayProgressBar("Slime Eater", "MainMenu scene", 0.6f);
            BuildMainMenu();
            EditorUtility.DisplayProgressBar("Slime Eater", "Boss scene", 0.75f);
            BuildBossScene();
            EditorUtility.DisplayProgressBar("Slime Eater", "Runner scene", 0.9f);
            BuildRunnerScene();
            SetBuildScenes();
            FrameSceneView();
            GameViewFixer.Fix();
            AssetDatabase.SaveAssets();
            Debug.Log("[Builder] สร้างเสร็จทั้งหมด: MainMenu / RunnerScene / BossScene");
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    [MenuItem("Tools/Slime Eater/Open Main Menu Scene", priority = 20)]
    static void OpenMenu() => OpenScene("MainMenu");
    [MenuItem("Tools/Slime Eater/Open Runner Scene", priority = 21)]
    static void OpenRunner() => OpenScene("RunnerScene");
    [MenuItem("Tools/Slime Eater/Open Boss Scene", priority = 22)]
    static void OpenBoss() => OpenScene("BossScene");

    static void OpenScene(string n)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(SceneDir + n + ".unity");
        FrameSceneView();
    }

    static void FrameSceneView() // ให้ Scene view มองตรงจากมุมกล้องเกม เห็นฉากทั้งหมด
    {
        var sv = SceneView.lastActiveSceneView;
        var cam = Camera.main;
        if (!sv || !cam) return;
        sv.orthographic = cam.orthographic;
        sv.LookAtDirect(cam.transform.position + cam.transform.forward * 20f, cam.transform.rotation, cam.orthographic ? cam.orthographicSize * 1.3f : 12f);
        sv.Repaint();
    }

    //  1) Folders + import settings
    static void EnsureFolders()
    {
        foreach (var p in new[] { ArtGen, MatDir, PrefabDir, SceneDir, AnimalSO, "Assets/Audio/Music/" })
            Directory.CreateDirectory(p);
        AssetDatabase.Refresh();
    }

    static void ConfigureImporters()
    {
        foreach (var dir in new[] { ArtUI, ArtScreen, ArtAnimal + "Color/", ArtAnimal + "Shadow/" })
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir.TrimEnd('/') }))
                SetSprite(AssetDatabase.GUIDToAssetPath(guid), dir.StartsWith(ArtAnimal) ? 1024 : 2048, Vector4.zero); // รูปสัตว์โชว์ไม่เกิน ~860px -> 1024 พอ (ไฟล์เว็บเล็กลง 4 เท่า)
        foreach (var dir in new[] { "Assets/Hyper_Casual_UI/Stylized Nature MegaKit[Standard]/Textures", "Assets/Character_Model" }) // พื้นผิวต้นไม้/ตัวละคร 2048 -> 1024 ภาพแทบไม่ต่าง
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (ti.maxTextureSize > 1024) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
            }
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Character_Model" }))           // โมเดลตัวละครละเอียดมาก -> บีบอัด mesh
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (mi.meshCompression != ModelImporterMeshCompression.Medium) { mi.meshCompression = ModelImporterMeshCompression.Medium; mi.SaveAndReimport(); }
        }

        // กล่องพิมพ์ใช้ 9-slice เพื่อยืดได้ไม่เพี้ยน
        SetSprite(ArtUI + "Panel_InputBox.png", 2048, new Vector4(14, 14, 14, 14));
        SetSprite(ArtUI + "Button_Green.png", 2048, new Vector4(40, 30, 40, 30));
        SetSprite(ArtUI + "Button_Teal.png", 2048, new Vector4(40, 30, 40, 30));
        SetSprite(ArtUI + "Button_Wood.png", 2048, new Vector4(40, 30, 40, 30));
        SetSprite(HC + "Panel_Sprites/Settings pannel.png", 2048, new Vector4(40, 40, 40, 40));
        SetSprite(HC + "Panel_Sprites/Main Menu pannel.png", 2048, new Vector4(50, 50, 50, 60));   // 9-slice ยืดไม่เพี้ยน
        SetSprite(HC + "Panel_Sprites/Daily Reward pannel.png", 2048, new Vector4(40, 40, 40, 40));
        SetSprite(HC + "Panel_Sprites/Frame 1584.png", 2048, new Vector4(30, 30, 30, 30));
        SetSprite(HC + "Panel_Sprites/Rectangle 358.png", 2048, new Vector4(10, 10, 10, 10));
        SetSprite(HC + "Buttons/empty_buttons/GOLDEN.png", 2048, new Vector4(30, 20, 30, 20));
        SetSprite(HC + "Buttons/empty_buttons/green.png", 2048, new Vector4(30, 20, 30, 20));
        foreach (var c in new[] { "blue", "orange", "yellow" }) SetSprite(HC + "Buttons/empty_buttons/" + c + ".png", 2048, new Vector4(30, 20, 30, 20));
    }

    static void SetSprite(string path, int maxSize, Vector4 border)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[Builder] ไม่พบรูป " + path); return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.maxTextureSize = maxSize;
        ti.spriteBorder = border;
        ti.SaveAndReimport();
    }

    static Sprite S(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning("[Builder] ไม่พบ Sprite: " + path);
        return s;
    }

    //  รูปทรงพื้นฐานที่สร้างด้วยโค้ด (วงกลม วงแหวน กล่องมุมมน หยดเหงื่อ)
    static void GenerateFxSprites()
    {
        WriteShape("Fx_Circle", 256, (x, y) => Disc(x, y, 128, 128, 126));
        WriteShape("Fx_Ring", 256, (x, y) => Mathf.Min(Disc(x, y, 128, 128, 126), 1f - Disc(x, y, 128, 128, 100)));
        WriteShape("Fx_RoundRect", 128, (x, y) =>
        {
            float r = 40f, cx = Mathf.Clamp(x, r, 128 - r), cy = Mathf.Clamp(y, r, 128 - r);
            return Disc(x, y, cx, cy, r - 1);
        }, new Vector4(42, 42, 42, 42));
        WriteShape("Fx_Drop", 128, (x, y) =>
        {
            float body = Disc(x, y, 64, 44, 36);                       // ก้นหยดกลม
            float tip = Mathf.Clamp01((36f - Mathf.Abs(x - 64f) * (120f - y) / 76f) * 0.5f);
            return y > 44 ? Mathf.Min(1f, tip) * (y < 122 ? 1 : 0) : body;
        });
        WriteShape("Fx_White", 8, (x, y) => 1f);
    }

    static float Disc(float x, float y, float cx, float cy, float r)
    {
        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        return Mathf.Clamp01(r - d + 0.5f);
    }

    static void WriteShape(string name, int size, System.Func<float, float, float> alpha, Vector4 border = default)
    {
        string path = ArtGen + name + ".png";
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha(x + 0.5f, y + 0.5f)));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        SetSprite(path, 512, border);
    }

    //  2) ฟอนต์ภาษาไทย + วัสดุ
    static void CreateFont()
    {
        string path = FontDir + "Kanit_SDF.asset";
        thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (thaiFont == null)
        {
            var src = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "Kanit-Medium.ttf");
            thaiFont = TMP_FontAsset.CreateFontAsset(src, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            thaiFont.name = "Kanit_SDF";
            AssetDatabase.CreateAsset(thaiFont, path);
            thaiFont.material.name = "Kanit_SDF Material";
            AssetDatabase.AddObjectToAsset(thaiFont.material, thaiFont);
            thaiFont.atlasTextures[0].name = "Kanit_SDF Atlas";
            AssetDatabase.AddObjectToAsset(thaiFont.atlasTextures[0], thaiFont);
        }
        // ใส่ตัวอักษรทั้งหมดที่ใช้ในเกมไว้ล่วงหน้า
        var chars = new System.Text.StringBuilder("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?/%+-_()'\"");
        for (int i = 0; i < AnimalTable.GetLength(0); i++) chars.Append(AnimalTable[i, 1]);
        chars.Append("ถึงด่านบอสแล้วตั้งค่าเล่นต่อเริ่มใหม่เมนูหลักเสียงเพลงคะแนนรวมคำกระโดดย่อปิด");
        thaiFont.TryAddCharacters(chars.ToString(), out _);
        EditorUtility.SetDirty(thaiFont);

        // ฟอนต์ตัวการ์ตูนภาษาอังกฤษ (Baloo จาก Hyper_Casual_UI) ใช้กับป้ายในหน้าเมนู
        string titlePath = FontDir + "Baloo_SDF.asset";
        titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(titlePath);
        if (titleFont == null)
        {
            var src = AssetDatabase.LoadAssetAtPath<Font>("Assets/Hyper_Casual_UI/Fonts/Baloo2-ExtraBold.ttf");
            titleFont = TMP_FontAsset.CreateFontAsset(src, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            titleFont.name = "Baloo_SDF";
            AssetDatabase.CreateAsset(titleFont, titlePath);
            titleFont.material.name = "Baloo_SDF Material"; AssetDatabase.AddObjectToAsset(titleFont.material, titleFont);
            titleFont.atlasTextures[0].name = "Baloo_SDF Atlas"; AssetDatabase.AddObjectToAsset(titleFont.atlasTextures[0], titleFont);
        }
        titleFont.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 !?", out _);
        string titleOutlinePath = FontDir + "Baloo_SDF_Outline.mat";
        titleOutline = AssetDatabase.LoadAssetAtPath<Material>(titleOutlinePath);
        if (titleOutline == null) { titleOutline = new Material(titleFont.material) { name = "Baloo_SDF_Outline" }; AssetDatabase.CreateAsset(titleOutline, titleOutlinePath); }
        titleOutline.CopyPropertiesFromMaterial(titleFont.material);
        titleOutline.EnableKeyword("OUTLINE_ON");
        titleOutline.SetFloat("_OutlineWidth", 0.3f);
        titleOutline.SetColor("_OutlineColor", new Color(0.3f, 0.18f, 0.08f));
        titleOutline.SetFloat("_FaceDilate", 0.2f);
        EditorUtility.SetDirty(titleOutline);

        string outlinePath = FontDir + "Kanit_SDF_Outline.mat";
        fontOutline = AssetDatabase.LoadAssetAtPath<Material>(outlinePath);
        if (fontOutline == null)
        {
            fontOutline = new Material(thaiFont.material) { name = "Kanit_SDF_Outline" };
            AssetDatabase.CreateAsset(fontOutline, outlinePath);
        }
        fontOutline.CopyPropertiesFromMaterial(thaiFont.material);
        fontOutline.EnableKeyword("OUTLINE_ON");
        fontOutline.SetFloat("_OutlineWidth", 0.32f);       // ขอบหนา อ่านง่ายบนพื้นหลังสีสด
        fontOutline.SetColor("_OutlineColor", new Color(0.25f, 0.12f, 0.05f));
        fontOutline.SetFloat("_FaceDilate", 0.2f);
        EditorUtility.SetDirty(fontOutline);
    }

    static Material Mat(string name, string shader, Color color, Texture tex = null)
    {
        string path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (tex && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (tex && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void CreateMaterials()
    {
        spriteMat = Mat("M_SpriteUnlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default", Color.white);

        // วัสดุเปลือกไม้จาก MegaKit สำหรับต้นไม้ล้ม
        Material bark = null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Kit + "CommonTree_1.fbx"))
            if (o is Material mm && mm.name.Contains("Bark")) bark = mm;
        var log = Mat("M_Log", "Universal Render Pipeline/Lit", new Color(0.75f, 0.5f, 0.3f));
        if (bark && bark.HasProperty("_BaseMap")) { log.SetTexture("_BaseMap", bark.GetTexture("_BaseMap")); log.SetColor("_BaseColor", Color.white); }
        log.SetTextureScale("_BaseMap", new Vector2(1f, 3f));
    }

    //  3) ข้อมูลสัตว์ 21 ตัว
    static void CreateAnimalAssets()
    {
        for (int i = 0; i < AnimalTable.GetLength(0); i++)
        {
            string n = AnimalTable[i, 0];
            string path = AnimalSO + (i + 1).ToString("00") + "_" + n + ".asset";
            var a = AssetDatabase.LoadAssetAtPath<AnimalDataSO>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<AnimalDataSO>(); AssetDatabase.CreateAsset(a, path); }
            a.englishName = n;
            a.thaiHint = AnimalTable[i, 1];
            if (a.fullColorSprite == null) a.fullColorSprite = S(ArtAnimal + "Color/" + n + ".png");
            if (a.silhouetteSprite == null) a.silhouetteSprite = S(ArtAnimal + "Shadow/" + n + "_shadow.png");
            EditorUtility.SetDirty(a);
        }
    }

    //  4) Prefabs
    static GameObject Model(string fbxPath, Transform parent, Vector3 pos, Vector3 euler, float scale)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (src == null) { Debug.LogWarning("[Builder] ไม่พบโมเดล " + fbxPath); return new GameObject("Missing"); }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = Vector3.one * scale;
        FixKitMaterials(go);
        return go;
    }

    // วัสดุใน FBX ของ MegaKit ยังไม่ได้เปิด Alpha Clipping ทำให้ใบไม้มีขอบดำ/สีเพี้ยน
    // -> สร้างสำเนาวัสดุใน Assets/Materials/Kit แล้วเปิด Alpha Clip + เรนเดอร์สองด้าน
    static readonly Dictionary<Material, Material> kitMatCache = new Dictionary<Material, Material>();

    static void FixKitMaterials(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var src = mats[i];
                if (src == null || AssetDatabase.GetAssetPath(src).StartsWith(MatDir)) continue;
                if (!kitMatCache.TryGetValue(src, out var fixedMat))
                {
                    string path = MatDir + "Kit/" + src.name + ".mat";
                    Directory.CreateDirectory(MatDir + "Kit");
                    fixedMat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (fixedMat == null) { fixedMat = new Material(src); AssetDatabase.CreateAsset(fixedMat, path); }
                    else fixedMat.CopyPropertiesFromMaterial(src);

                    var tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") as Texture2D : null;
                    var ti = tex ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter : null;
                    bool hasAlpha = ti != null && ti.DoesSourceTextureHaveAlpha();
                    if (hasAlpha)
                    {
                        fixedMat.SetFloat("_Surface", 0);
                        fixedMat.SetFloat("_AlphaClip", 1);
                        fixedMat.SetFloat("_Cutoff", 0.5f);
                        fixedMat.EnableKeyword("_ALPHATEST_ON");
                        fixedMat.SetFloat("_Cull", 0);
                        fixedMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                        if (ti.alphaIsTransparency == false) { ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
                    }
                    if (fixedMat.HasProperty("_Smoothness")) fixedMat.SetFloat("_Smoothness", 0.15f);
                    EditorUtility.SetDirty(fixedMat);
                    kitMatCache[src] = fixedMat;
                }
                mats[i] = fixedMat;
            }
            r.sharedMaterials = mats;
        }
    }

    // วางโมเดลให้ก้นติดพื้น (y=0 ของ parent)
    static void SitOnGround(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position += Vector3.up * (go.transform.parent.position.y - b.min.y);
    }

    static GameObject SavePrefab(GameObject root, string name)
    {
        var p = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + name + ".prefab");
        Object.DestroyImmediate(root);
        return p;
    }

    static RunnerConfig runnerConfig;
    static readonly string[] TreeNames = { "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5", "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5" };
    static readonly string[] PlantNames = { "Bush_Common", "Grass_Common_Short", "Grass_Common_Short", "Grass_Wispy_Short", "Grass_Common_Tall", "Grass_Wispy_Tall", "Flower_3_Group", "Flower_4_Group", "Fern_1", "Plant_1", "Plant_7", "Clover_1", "Mushroom_Common" };
    static readonly string[] EdgeNames = { "Grass_Common_Short", "Grass_Common_Tall", "Grass_Wispy_Short", "Grass_Wispy_Tall", "Clover_2", "Flower_3_Single", "Pebble_Round_1" };
    static readonly string[] CanopyNames = { "TwistedTree_1", "TwistedTree_3", "TwistedTree_4" };
    static readonly string[] RockNames ={ "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3", "Pebble_Round_2", "Pebble_Square_3" };

    static void CreateConfig() // สร้าง config ครั้งแรก (ถ้ามีแล้วใช้ค่าที่ผู้ใช้ปรับไว้)
    {
        Directory.CreateDirectory("Assets/Data");
        runnerConfig = AssetDatabase.LoadAssetAtPath<RunnerConfig>("Assets/Data/RunnerConfig.asset");
        if (runnerConfig) return;
        runnerConfig = ScriptableObject.CreateInstance<RunnerConfig>();
        AssetDatabase.CreateAsset(runnerConfig, "Assets/Data/RunnerConfig.asset");
    }

    static GameObject[] NaturePrefabs(string[] names) // โมเดล MegaKit -> prefab (วัสดุแก้แล้ว) ใช้สุ่มวางในป่าตอนเล่น
    {
        Directory.CreateDirectory(PrefabDir + "Nature");
        var list = new List<GameObject>();
        foreach (var n in names)
        {
            var root = new GameObject(n);
            SitOnGround(Model(Kit + n + ".fbx", root.transform, Vector3.zero, Vector3.zero, 1f));
            list.Add(PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "Nature/" + n + ".prefab"));
            Object.DestroyImmediate(root);
        }
        return list.ToArray();
    }

    static BoxCollider Box(GameObject go, Vector3 center, Vector3 size) // collider แบบ trigger
    {
        var c = go.AddComponent<BoxCollider>(); c.isTrigger = true; c.center = center; c.size = size;
        return c;
    }

    static void CreatePrefabs()
    {
        CreateConfig();
        // ---- หิน (กระโดด) ----
        var rock = new GameObject("Obstacle_Rock");
        SitOnGround(Model(Kit + "Rock_Medium_1.fbx", rock.transform, Vector3.zero, new Vector3(0, 35, 0), 0.5f));
        Model(Kit + "Grass_Common_Short.fbx", rock.transform, new Vector3(-0.7f, 0, -0.5f), Vector3.zero, 0.7f);
        Box(rock, new Vector3(0, 0.5f, 0), new Vector3(1.5f, 1.0f, 1.2f));
        rock.AddComponent<Obstacle>().kind = Obstacle.Kind.Low;
        SavePrefab(rock, "Obstacle_Rock");

        // ---- หินใหญ่ (สูง ข้ามไม่ได้ ต้องหลบเลน) ----
        var boulder = new GameObject("Obstacle_Boulder");
        SitOnGround(Model(Kit + "Rock_Medium_2.fbx", boulder.transform, Vector3.zero, new Vector3(0, 20, 0), 0.8f));
        Model(Kit + "Rock_Medium_3.fbx", boulder.transform, new Vector3(0.1f, 1.3f, 0), new Vector3(0, 80, 0), 0.5f);
        Model(Kit + "Fern_1.fbx", boulder.transform, new Vector3(0.8f, 0, -0.8f), Vector3.zero, 0.6f);
        Box(boulder, new Vector3(0, 1.5f, 0), new Vector3(2.0f, 3.0f, 1.8f));
        boulder.AddComponent<Obstacle>().kind = Obstacle.Kind.Block;
        SavePrefab(boulder, "Obstacle_Boulder");

        // ---- ท่อนไม้สูงพาดหินทุกเลน (สไลด์ลอด) ----
        var log = new GameObject("Obstacle_HighLog");
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        Object.DestroyImmediate(trunk.GetComponent<Collider>());
        trunk.transform.SetParent(log.transform, false);
        trunk.transform.localPosition = new Vector3(0, 1.1f, 0);
        trunk.transform.localEulerAngles = new Vector3(0, 0, 90);
        trunk.transform.localScale = new Vector3(0.65f, 4.6f, 0.65f);
        trunk.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "M_Log.mat");
        SitOnGround(Model(Kit + "Rock_Medium_2.fbx", log.transform, new Vector3(-4.6f, 0, 0), Vector3.zero, 0.5f));
        SitOnGround(Model(Kit + "Rock_Medium_3.fbx", log.transform, new Vector3(4.6f, 0, 0), new Vector3(0, 70, 0), 0.55f));
        Model(Kit + "Mushroom_Laetiporus.fbx", log.transform, new Vector3(1.2f, 1.25f, -0.25f), Vector3.zero, 0.5f);
        Model(Kit + "Fern_1.fbx", log.transform, new Vector3(-2.5f, 1.3f, 0f), new Vector3(0, 0, 10), 0.4f);
        Box(log, new Vector3(0, 1.85f, 0), new Vector3(8.4f, 2.5f, 0.8f));   // ล่างสุดสูง 0.6 -> ต้องสไลด์
        log.AddComponent<Obstacle>().kind = Obstacle.Kind.High;
        SavePrefab(log, "Obstacle_HighLog");

        // ---- ต้นไม้ล้มจากข้างทาง (ซ้าย/ขวา) ----
        MakeFallingTree("Obstacle_TreeFromRight", 1f);
        MakeFallingTree("Obstacle_TreeFromLeft", -1f);

        // ---- ไอเทม (ใช้รูปจาก Hyper_Casual_UI) ----
        MakeCollectible("Item_Coin", HC + "Icons/coin.png", Collectible.Kind.Coin, 1, 0.75f);
        MakeCollectible("Item_Heart", HC + "Icons/heartt.png", Collectible.Kind.Heal, runnerConfig.healAmount, 1.0f);
        MakeCollectible("Item_HintLeaf", ArtUI + "Icon_Leaf.png", Collectible.Kind.HintLeaf, 1, 1.0f, new Color(0.5f, 1f, 0.4f, 0.45f));
        MakeCollectible("Item_Magnet", HC + "Icons/Upgrade-StarBase.png", Collectible.Kind.Magnet, 1, 1.1f, new Color(0.4f, 0.8f, 1f, 0.45f));
        MakeCollectible("Item_Shield", ArtUI + "Icon_Shield.png", Collectible.Kind.Shield, 1, 1.1f, new Color(0.5f, 1f, 0.6f, 0.45f));
        MakeCollectible("Item_Double", HC + "Icons/Gold Treasure box.png", Collectible.Kind.Double, 1, 1.1f, new Color(1f, 0.85f, 0.3f, 0.45f));
    }

    // ต้นไม้ MegaKit ยืนข้างทาง ล้มขวาง: ช่วงลำต้นต่ำ (กระโดดข้าม) ช่วงพุ่มใบสูง (ต้องหลบ)
    static void MakeFallingTree(string name, float dir)
    {
        var root = new GameObject(name);
        var pivot = new GameObject("Pivot").transform;
        pivot.SetParent(root.transform, false);
        pivot.localPosition = new Vector3(4.7f * dir, 0.3f, 0f);
        var tree = Model(Kit + "CommonTree_3.fbx", pivot, Vector3.zero, new Vector3(0, 30, 0), 1f);
        float leafStart = 0f, top = 0f, crown = 3f;     // หาความสูงจุดเริ่มพุ่มใบจาก submesh ใบไม้
        foreach (var mf in tree.GetComponentsInChildren<MeshFilter>())
        {
            var r = mf.GetComponent<Renderer>(); var m = mf.sharedMesh;
            for (int i = 0; i < m.subMeshCount && i < r.sharedMaterials.Length; i++)
            {
                if (!r.sharedMaterials[i].name.Contains("Leaves")) continue;
                var b = m.GetSubMesh(i).bounds;
                var lo = mf.transform.TransformPoint(b.min); var hi = mf.transform.TransformPoint(b.max);
                leafStart = Mathf.Min(lo.y, hi.y) - pivot.position.y; top = Mathf.Max(lo.y, hi.y) - pivot.position.y;
                crown = Mathf.Abs(hi.x - lo.x);
            }
        }
        float s = leafStart > 0.1f ? 6.2f / leafStart : 0.8f; // ให้ช่วงลำต้นยาวพอพาด 2 เลน
        tree.transform.localScale = Vector3.one * s;
        leafStart *= s; top *= s; crown *= s * 0.8f;
        Box(pivot.gameObject, new Vector3(0, leafStart * 0.5f, 0), new Vector3(0.7f, leafStart, 1.0f));               // ลำต้น
        var crownBox = new GameObject("CrownCollider"); crownBox.transform.SetParent(pivot, false);
        Box(crownBox, new Vector3(0, (leafStart + top) * 0.5f, 0), new Vector3(crown, top - leafStart, crown));    // พุ่มใบ
        var ft = root.AddComponent<FallingTree>();
        ft.kind = Obstacle.Kind.Falling; ft.pivot = pivot; ft.fallDirection = dir; ft.config = runnerConfig;
        SavePrefab(root, name);
    }

    static void MakeCollectible(string name, string spritePath, Collectible.Kind kind, int value, float worldSize, Color? glow = null)
    {
        var root = new GameObject(name);
        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = S(spritePath);
        sr.sharedMaterial = spriteMat;
        if (sr.sprite) { float s = worldSize / Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y); vis.transform.localScale = Vector3.one * s; }
        if (glow.HasValue) // วงแสงด้านหลัง ให้ไอเทมพิเศษเด่นกว่าเหรียญ
        {
            var halo = new GameObject("Glow").AddComponent<SpriteRenderer>();
            halo.transform.SetParent(root.transform, false);
            halo.transform.localPosition = new Vector3(0, 0, 0.05f);
            halo.sprite = S(ArtGen + "Fx_Circle.png"); halo.sharedMaterial = spriteMat; halo.color = glow.Value;
            halo.transform.localScale = Vector3.one * worldSize * 1.6f / halo.sprite.bounds.size.x;
        }
        var col = root.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = worldSize * 0.5f;
        var c = root.AddComponent<Collectible>(); c.kind = kind; c.value = value;
        SavePrefab(root, name);
    }

    static T LoadPrefab<T>(string name) where T : Component
        => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + name + ".prefab").GetComponent<T>();

    //  UI helpers  (ตำแหน่ง x,y = จุดกึ่งกลางในภาพอ้างอิง 1920x1080 วัดจากมุมซ้ายบน)
    static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), C = new Vector2(0.5f, 0.5f),
                            BL = new Vector2(0, 0), BC = new Vector2(0.5f, 0), TC = new Vector2(0.5f, 1);

    static RectTransform Rect(Transform parent, string name, Vector2 anchor, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x - anchor.x * RefRes.x, (RefRes.y - y) - anchor.y * RefRes.y);
        return rt;
    }

    static RectTransform Stretch(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Image Img(Transform parent, string name, Sprite sprite, Vector2 anchor, float x, float y, float w, float h, Color? color = null)
    {
        var img = Rect(parent, name, anchor, x, y, w, h).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color ?? Color.white;
        img.raycastTarget = false;
        if (sprite && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
        return img;
    }

    static TextMeshProUGUI Txt(Transform parent, string name, string text, Vector2 anchor, float x, float y, float w, float h,
                               float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool outline = false)
    {
        var t = Rect(parent, name, anchor, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        t.font = thaiFont;
        if (outline) t.fontSharedMaterial = fontOutline;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.gameObject.AddComponent<ThaiTextFixer>();
        return t;
    }

    // ปุ่มรูป + ข้อความ
    static Button Btn(Transform parent, string name, Sprite sprite, Vector2 anchor, float x, float y, float w, float h,
                      string label, UnityAction onClick, float fontSize = 44)
    {
        var img = Img(parent, name, sprite, anchor, x, y, w, h);
        img.raycastTarget = true;
        if (!sprite) img.color = new Color(1, 1, 1, 0);
        var b = img.gameObject.AddComponent<Button>();
        if (!string.IsNullOrEmpty(label))
        {
            var t = Txt(img.transform, "Label", label, C, 960, 540, w, h, fontSize, Color.white, TextAlignmentOptions.Center, true);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        }
        if (onClick != null) UnityEventTools.AddPersistentListener(b.onClick, onClick);
        return b;
    }

    // ปุ่มใสวางทับรูปที่มีปุ่มวาดไว้แล้ว (เมาส์ชี้แล้วสว่างขึ้น)
    static Button HotspotBtn(Transform parent, string name, Vector2 anchor, float x, float y, float w, float h, UnityAction onClick)
    {
        var img = Img(parent, name, S(ArtGen + "Fx_RoundRect.png"), anchor, x, y, w, h, Color.white);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        var cb = b.colors;
        cb.normalColor = new Color(1, 1, 1, 0);
        cb.highlightedColor = new Color(1, 1, 1, 0.18f);
        cb.selectedColor = new Color(1, 1, 1, 0);
        cb.pressedColor = new Color(0, 0, 0, 0.15f);
        b.colors = cb;
        UnityEventTools.AddPersistentListener(b.onClick, onClick);
        return b;
    }

    static Canvas MakeCanvas(string name, Camera cam = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        if (cam) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 10f; }
        else canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = RefRes;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        go.AddComponent<OrientationLayout>();                    // แนวตั้ง (มือถือ) ปรับขนาด/ย้าย UI เอง
        return canvas;
    }

    static void MakeEventSystem()
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static Slider MakeSlider(Transform parent, Vector2 anchor, float x, float y, float w, float h)
    {
        var root = Rect(parent, "MusicVolumeSlider", anchor, x, y, w, h);         // แถบเสียง: ราง/แถบเหลือง/ปุ่มดาว จาก Hyper_Casual_UI
        var bg = Img(root, "Background", S(HC + "Panel_Sprites/Rectangle 358.png"), C, 960, 540, w, h * 0.5f);
        var fillArea = Stretch(root, "Fill Area"); fillArea.offsetMin = new Vector2(6, 0); fillArea.offsetMax = new Vector2(-6, 0);
        var fill = Img(fillArea, "Fill", S(HC + "Buttons/empty_buttons/yellow.png"), C, 960, 540, 0, h * 0.36f);
        fill.rectTransform.anchorMin = new Vector2(0, 0.5f); fill.rectTransform.anchorMax = new Vector2(1, 0.5f);
        fill.rectTransform.sizeDelta = new Vector2(0, h * 0.36f);
        var handleArea = Stretch(root, "Handle Slide Area"); handleArea.offsetMin = new Vector2(20, 0); handleArea.offsetMax = new Vector2(-20, 0);
        var handle = Img(handleArea, "Handle", S(HC + "Icons/star golden.png"), C, 960, 540, h * 1.3f, h * 1.3f);
        handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0, 0.5f);
        handle.raycastTarget = true;
        bg.raycastTarget = true;
        fill.pixelsPerUnitMultiplier = 2.5f;                                         // มุมโค้งพอดีแถบบาง
        var s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.minValue = 0; s.maxValue = 1; s.value = 0.7f;
        return s;
    }

    const string AudioDir = "Assets/Audio/Music/";
    static AudioClip Clip(string file) => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + file);

    static AudioManager MakeAudio(Slider music, Slider sfx) // เพลง + เสียงประกอบ (ระบุไฟล์ชัดเจน ไม่สุ่มตามชื่อในโฟลเดอร์)
    {
        var am = new GameObject("AudioManager").AddComponent<AudioManager>();
        am.volumeSlider = music; am.sfxSlider = sfx;
        am.musicClip = Clip("alex-productions-happy-and-fun-background-music(chosic.com).mp3");
        am.bossMusic = Clip("question song.mp3");
        am.sfxClips = new[] { Clip("Jump.mp3"), Clip("coin.mp3"), Clip("button_Ui.mp3"), Clip("eat-much.mp3") }; // เรียงตาม AudioManager.Sfx
        return am;
    }

    // หน้าตั้งค่า (ใช้ทั้งเมนูและในเกม)
    static GameObject MakeSettingsPanel(Transform canvas, bool inGame, UIManager ui, MenuManager menu, out Slider slider, out Slider sfxSlider)
    {
        var root = Stretch(canvas, "SettingsPanel");
        var dim = root.gameObject.AddComponent<Image>(); dim.color = new Color(0, 0, 0, 0.55f);
        float h = inGame ? 870 : 900;                                                // ความสูงพาเนล (+แถวเสียงประกอบ)
        var panel = Img(root, "Panel", S(HC + "Panel_Sprites/Settings pannel.png"), C, 960, 560, 720, h);
        var p = panel.transform;
        Img(p, "TitleBar", S(HC + "Buttons/empty_buttons/orange.png"), C, 960, 540 - h * 0.5f, 480, 120);
        Txt(p, "Title", "ตั้งค่า", C, 960, 536 - h * 0.5f, 480, 120, 62, new Color(1f, 0.96f, 0.85f), TextAlignmentOptions.Center, true);
        UnityAction close = inGame ? (UnityAction)ui.ResumeGame : menu.CloseSetting;
        Btn(p, "CloseX", S(HC + "Icons/Close.png"), C, 960 + 330, 540 - h * 0.5f + 40, 86, 86, null, close);

        float musicY = 540 - h * 0.5f + (inGame ? 170 : 175);                        // แถวเสียงเพลงในกรอบเข้ม
        Img(p, "MusicRow", S(HC + "Panel_Sprites/Frame 1584.png"), C, 960, musicY, 620, 120);
        Img(p, "MusicIcon", S(HC + "Icons/musicOn.png"), C, 715, musicY, 76, 76);
        Txt(p, "MusicLabel", "เสียงเพลง", C, 855, musicY, 180, 60, 34, Color.white, TextAlignmentOptions.Left, true);
        slider = MakeSlider(p, C, 1095, musicY, 250, 50);
        float sfxY = musicY + 128;                                                    // แถวเสียงประกอบ (กระโดด เหรียญ ปุ่ม กิน)
        Img(p, "SfxRow", S(HC + "Panel_Sprites/Frame 1584.png"), C, 960, sfxY, 620, 120);
        Img(p, "SfxIcon", S(HC + "Icons/soundon.png"), C, 715, sfxY, 76, 76);
        Txt(p, "SfxLabel", "เสียงประกอบ", C, 855, sfxY, 190, 60, 34, Color.white, TextAlignmentOptions.Left, true);
        sfxSlider = MakeSlider(p, C, 1095, sfxY, 250, 50);
        musicY = sfxY;                                                                // ปุ่มด้านล่างเลื่อนลงตามแถวที่เพิ่ม

        if (inGame)
        {
            float y0 = musicY + 165;
            IconPill(p, "ResumeButton", "green", HC + "Icons/Play (1).png", "เล่นต่อ", y0, ui.ResumeGame);
            IconPill(p, "RestartButton", "blue", HC + "Icons/codicon_debug-restart.png", "เริ่มใหม่", y0 + 135, ui.RestartGame);
            IconPill(p, "MenuButton", "orange", HC + "Icons/home_icon.png", "เมนูหลัก", y0 + 270, ui.BackToMenu);
            Txt(p, "PausedNote", "เกมหยุดชั่วคราว", C, 960, 540 + h * 0.5f - 45, 500, 50, 30, new Color(1f, 0.93f, 0.7f), TextAlignmentOptions.Center, true);
        }
        else // หน้าเมนู: บันทึกเกม + เวลาบันทึกล่าสุด + สอนเล่นอีกครั้ง / รีเซ็ตข้อมูล + ตกลง
        {
            IconPill(p, "SaveButton", "blue", HC + "Icons/upgrade.png", "บันทึกเกม", musicY + 160, menu.SaveGame);
            menu.saveStatus = Txt(p, "SaveStatus", "", C, 960, musicY + 232, 600, 44, 28, new Color(1f, 0.93f, 0.7f), TextAlignmentOptions.Center, true);
            Btn(p, "TutorialButton", S(HC + "Buttons/empty_buttons/yellow.png"), C, 805, musicY + 318, 290, 100, "สอนเล่นอีกครั้ง", menu.ReplayTutorial, 34);
            Btn(p, "ResetButton", S(HC + "Buttons/empty_buttons/orange.png"), C, 1115, musicY + 318, 290, 100, "รีเซ็ตข้อมูล", menu.AskReset, 34);
            IconPill(p, "CloseButton", "green", HC + "Icons/correct.png", "ตกลง", musicY + 445, menu.CloseSetting);
            menu.resetPanel = BuildResetPanel(root.parent, menu);
        }
        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    static GameObject BuildResetPanel(Transform canvas, MenuManager menu) // ยืนยันก่อนลบข้อมูลทั้งหมด
    {
        var root = Stretch(canvas, "ResetConfirm");
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        var p = Img(root, "Panel", S(HC + "Panel_Sprites/Settings pannel.png"), C, 960, 540, 720, 420).transform;
        Txt(p, "Title", "ลบข้อมูลทั้งหมด?", C, 960, 395, 600, 80, 54, new Color(1f, 0.6f, 0.45f), TextAlignmentOptions.Center, true);
        Txt(p, "Body", "เหรียญ ตัวละคร อัปเกรด ลีก และภารกิจจะหายทั้งหมด\nย้อนกลับไม่ได้", C, 960, 520, 660, 110, 30, Color.white, TextAlignmentOptions.Center, true);
        Btn(p, "Confirm", S(HC + "Buttons/empty_buttons/orange.png"), C, 805, 640, 260, 100, "ลบเลย", menu.ConfirmReset, 40);
        Btn(p, "Cancel", S(HC + "Buttons/empty_buttons/green.png"), C, 1115, 640, 260, 100, "ยกเลิก", menu.CancelReset, 40);
        return root.gameObject;
    }

    static void IconPill(Transform parent, string name, string color, string icon, string label, float y, UnityAction onClick) // ปุ่มทรงแคปซูล + ไอคอนซ้าย
    {
        var b = Btn(parent, name, S(HC + "Buttons/empty_buttons/" + color + ".png"), C, 960, y, 480, 110, null, onClick);
        Img(b.transform, "Icon", S(icon), C, 960 - 175, 535, 70, 70);
        Txt(b.transform, "Label", label, C, 990, 535, 320, 90, 50, Color.white, TextAlignmentOptions.Center, true);
    }

    // HUD ด้านบน (หลอดระยะทาง + หลอดเลือด + เหรียญ + เฟือง) ใช้ทั้ง 2 ฉาก
    static void MakeHud(Transform canvas, UIManager ui)
    {
        var hud = Stretch(canvas, "HUD");
        ui.hudRoot = hud.gameObject;

        var pFrame = Img(hud, "ProgressBar", S(ArtUI + "Bar_Progress_Frame.png"), TL, 352, 98, 662, 140);
        ui.progressFill = Img(pFrame.transform, "Fill", S(ArtUI + "Bar_Progress_Fill.png"), C, 960, 540, 662, 140);
        ui.progressFill.type = Image.Type.Filled;
        ui.progressFill.fillMethod = Image.FillMethod.Horizontal;
        ui.progressFill.fillOrigin = 0;
        ui.progressFillRange = FillRange(ArtUI + "Bar_Progress_Fill.png");
        ui.progressText = Txt(hud, "DistanceText", "0 m", TL, 790, 100, 200, 60, 40, Color.white, TextAlignmentOptions.Left, true);

        var hFrame = Img(hud, "HpBar", S(ArtUI + "Bar_HP_Frame.png"), TL, 361, 221, 654, 81);
        ui.hpFill = Img(hFrame.transform, "Fill", S(ArtUI + "Bar_HP_Fill.png"), C, 960, 540, 654, 81);
        ui.hpFill.type = Image.Type.Filled;
        ui.hpFill.fillMethod = Image.FillMethod.Horizontal;
        ui.hpFill.fillOrigin = 0;
        ui.hpFillRange = FillRange(ArtUI + "Bar_HP_Fill.png");
        ui.hpPopupAnchor = Rect(hud, "HpPopupAnchor", TL, 720, 250, 10, 10);

        Img(hud, "CoinIcon", S(ArtUI + "Icon_CoinGold.png"), TL, 75, 305, 60, 60);
        ui.scoreText = Txt(hud, "CoinText", "0", TL, 190, 305, 160, 60, 44, Color.white, TextAlignmentOptions.Left, true);
        Img(hud, "StageIcon", S(HC + "Icons/flag.png"), TL, 330, 305, 46, 52);
        ui.stageText = Txt(hud, "StageText", "ด่าน 1", TL, 440, 305, 180, 60, 40, Color.white, TextAlignmentOptions.Left, true);
        Img(hud, "LeafIcon", S(ArtUI + "Icon_Leaf.png"), TL, 75, 385, 58, 62);
        ui.leafText = Txt(hud, "LeafText", "x0", TL, 190, 385, 160, 60, 40, new Color(0.75f, 1f, 0.6f), TextAlignmentOptions.Left, true);

        // คะแนนรวม (กรอบ Hyper_Casual_UI + มงกุฎ)
        var scoreBox = Img(hud, "ScorePanel", S(HC + "Panel_Sprites/Frame 1584.png"), TR, 1340, 85, 420, 100);
        Img(scoreBox.transform, "Crown", S(HC + "Icons/crown.png"), C, 790, 535, 70, 62);
        ui.scoreValueText = Txt(scoreBox.transform, "ScoreValue", "0", C, 1000, 540, 300, 80, 50, Color.white, TextAlignmentOptions.Center, true);

        // วงเวลาไอเทมพลังพิเศษ (แม่เหล็ก / เกราะ / x2) ซ่อนไว้ จะโผล่ตอนเก็บได้
        string[] icons = { HC + "Icons/Upgrade-StarBase.png", ArtUI + "Icon_Shield.png", HC + "Icons/Gold Treasure box.png" };
        ui.powerUpFills = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var slot = Img(hud, "PowerUp_" + i, S(ArtGen + "Fx_Circle.png"), TL, 80, 490 + i * 110, 96, 96, new Color(0.1f, 0.2f, 0.25f, 0.7f));
            Img(slot.transform, "Icon", S(icons[i]), C, 960, 540, 62, 62);
            var ring = Img(slot.transform, "Ring", S(ArtGen + "Fx_Ring.png"), C, 960, 540, 96, 96, new Color(1f, 0.85f, 0.2f));
            ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360; ring.fillOrigin = (int)Image.Origin360.Top;
            ui.powerUpFills[i] = ring;
            slot.gameObject.SetActive(false);
        }
        ui.toast = Txt(hud, "Toast", "ภารกิจสำเร็จ", TC, 960, 230, 1200, 80, 46, new Color(1f, 0.92f, 0.4f), TextAlignmentOptions.Center, true);
        ui.toast.gameObject.SetActive(false);

        Btn(hud, "SettingsButton", S(ArtUI + "Icon_Gear.png"), TR, 1735, 118, 150, 150, null, ui.OpenSettings);
    }

    // หา % ซ้าย-ขวาของแถบสีจริงในรูป fill
    static Vector2 FillRange(string path)
    {
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));
        int minX = tex.width, maxX = 0;
        var px = tex.GetPixels32();
        for (int i = 0; i < px.Length; i++)
            if (px[i].a > 40) { int x = i % tex.width; if (x < minX) minX = x; if (x > maxX) maxX = x; }
        float w = tex.width;
        Object.DestroyImmediate(tex);
        return maxX > minX ? new Vector2(minX / w, (maxX + 1) / w) : new Vector2(0, 1);
    }

    // ป้ายประกาศ + ตัวหนังสือลอย + จอดำ + หน้าสรุปผล
    static void MakeOverlays(Transform canvas, UIManager ui)
    {
        // ตัวหนังสือลอยอยู่ Canvas เดียวกับ HUD (ตำแหน่งอ้างอิงตรงกัน)
        var popup = Txt(canvas, "PopupTemplate", "-", TL, 700, 260, 200, 90, 72, Color.red, TextAlignmentOptions.Center, true);
        ui.popupTemplate = popup.gameObject.AddComponent<FloatingPopup>();
        popup.gameObject.SetActive(false);

        // หน้าต่างที่ต้องอยู่บนสุดเสมอ (อยู่หน้าโมเดล 3D ด้วย) แยกเป็น Canvas Overlay
        var top = MakeCanvas("Canvas_Overlay (Result / Settings)");
        top.sortingOrder = 10;
        canvas = top.transform;
        ui.banner = Txt(canvas,"Banner", "ถึงด่านบอสแล้ว!", C, 960, 470, 1400, 200, 110, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center, true);
        ui.banner.gameObject.SetActive(false);

        MakeResult(canvas, ui);
        ui.settingsPanel = MakeSettingsPanel(canvas, true, ui, null, out var slider, out var sfx);
        MakeAudio(slider, sfx);
        Portrait(top, Move(ui.banner, 0, 70, 0.9f)); // แนวตั้ง: ป้ายประกาศย่อนิดหน่อย (หน้าสรุปผลเนื้อหากว้าง ~1150 พอดีจออยู่แล้ว)
        RotatePrompt(top);

        var fade = Stretch(canvas, "Fader");
        var fimg = fade.gameObject.AddComponent<Image>(); fimg.color = Color.black; fimg.raycastTarget = false;
        ui.fader = fade.gameObject.AddComponent<CanvasGroup>();
        ui.fader.alpha = 0f; ui.fader.blocksRaycasts = false; ui.fader.interactable = false;
    }

    static void MakeResult(Transform canvas, UIManager ui)
    {
        var root = Stretch(canvas, "ResultScreen");
        var dim = root.gameObject.AddComponent<Image>(); dim.color = new Color(0, 0, 0, 0.45f);
        ui.resultRoot = root.gameObject;
        ui.distanceTexts = new TMP_Text[2]; ui.bonusTexts = new TMP_Text[2]; ui.coinTexts = new TMP_Text[2];
        ui.wordTexts = new TMP_Text[2]; ui.scoreTexts = new TMP_Text[2];
        var dark = new Color(0.22f, 0.12f, 0.12f);

        for (int i = 0; i < 2; i++)
        {
            bool win = i == 0;
            var panel = Img(root, win ? "VictoryPanel" : "DefeatPanel", S(ArtScreen + (win ? "Result_Victory.png" : "Result_Defeat.png")), C, 960, 540, 1920, 1080);
            panel.preserveAspect = true;
            if (win) ui.victoryPanel = panel.gameObject; else ui.defeatPanel = panel.gameObject;
            var p = panel.transform;
            ui.distanceTexts[i] = Txt(p, "DistanceValue", "1000 m.", C, 930, 500, 700, 110, 96, dark);
            ui.bonusTexts[i]    = Txt(p, "BonusValue", "0%", C, 1080, 590, 200, 70, 52, dark, TextAlignmentOptions.Left);
            ui.coinTexts[i]     = Txt(p, "CoinValue", "0", C, 1360, 690, 200, 70, 52, dark, TextAlignmentOptions.Right);
            ui.wordTexts[i]     = Txt(p, "WordValue", "0 คำ", C, 1360, 780, 240, 70, 52, dark, TextAlignmentOptions.Right);
            ui.scoreTexts[i]    = Txt(p, "ScoreValue", "คะแนนรวม 0", C, 1190, 470, 560, 50, 30, new Color(0.45f, 0.35f, 0.3f), TextAlignmentOptions.Right); // กว้างพอให้อยู่บรรทัดเดียว
            HotspotBtn(p, "OkButton", C, 740, 925, 340, 100, ui.BackToMenu);
            HotspotBtn(p, "PlayAgainButton", C, 1128, 925, 340, 100, ui.RestartGame);
        }
        ui.defeatPanel.SetActive(false);
        root.gameObject.SetActive(false);
    }

    static Light MakeSun()
    {
        var go = new GameObject("Sun");
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.96f, 0.88f);
        l.intensity = 1.25f;
        l.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.75f, 0.85f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.6f, 0.75f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.45f, 0.4f, 0.3f);
        return l;
    }

    static Camera MakeCamera(Vector3 pos, Vector3 euler, float fov)
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.7f, 0.95f);
        cam.fieldOfView = fov;
        cam.farClipPlane = 300f;
        go.AddComponent<AudioListener>();
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        return cam;
    }



    static void Ground(Transform parent, string name, Vector3 pos, Vector3 size, Material m) // แผ่นพื้น (รับเงา)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = name;
        Object.DestroyImmediate(b.GetComponent<Collider>());
        b.transform.SetParent(parent, false);
        b.transform.localPosition = pos;
        b.transform.localScale = size;
        b.GetComponent<Renderer>().sharedMaterial = m;
    }

    static void SetupSkyAndFog(bool post = true) // ท้องฟ้า + หมอกอุ่นๆ + แสงแดดโทนการ์ตูนแบบภาพเรฟ (post = เอฟเฟกต์ภาพ)
    {
        var sky = Mat("M_Sky", "Skybox/Procedural", Color.white);
        sky.SetColor("_SkyTint", new Color(0.45f, 0.7f, 1f));
        sky.SetColor("_GroundColor", new Color(0.55f, 0.6f, 0.35f));
        sky.SetFloat("_AtmosphereThickness", 0.7f);
        sky.SetFloat("_Exposure", 1.35f);
        RenderSettings.skybox = sky;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.82f, 0.88f, 0.72f);       // หมอกเหลืองอมเขียวเหมือนแดดส่องในป่า
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 170f;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.84f, 0.98f);
        RenderSettings.ambientEquatorColor = new Color(0.64f, 0.74f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.4f, 0.33f, 0.18f);
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { l.color = new Color(1f, 0.9f, 0.72f); l.intensity = 1.6f; }
        // Post-processing: bloom แสงฟุ้ง, สีสดขึ้น, โทนอุ่น, ขอบจอมืดนิดๆ
        var cam = Camera.main;
        if (!post) return;                                              // ฉากบอสไม่ใช้ เพราะ UI อยู่ในกล้องเดียวกันจะซีด
        if (cam)
        {
            var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (!data) data = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
        }
        string path = "Assets/Data/ForestLook.asset";
        var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true); bloom.intensity.Override(0.7f); bloom.threshold.Override(0.85f);
            var col = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true); col.saturation.Override(18f); col.postExposure.Override(0.15f); col.contrast.Override(8f);
            var wb = profile.Add<UnityEngine.Rendering.Universal.WhiteBalance>(true); wb.temperature.Override(12f);
            var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true); vig.intensity.Override(0.22f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            AssetDatabase.SaveAssets();
        }
        var vol = new GameObject("PostProcess Volume").AddComponent<UnityEngine.Rendering.Volume>();
        vol.isGlobal = true; vol.sharedProfile = profile;
    }

    // ลายพื้นต่อกันได้ไม่มีรอยต่อ: ด่างอ่อน-เข้ม (value noise) + รอยแตกแบบดินแห้ง (Voronoi)
    static Texture2D GroundTexture(string name, Color baseCol, float variation, bool cracks)
    {
        string path = "Assets/Art/Material/" + name + ".png";
        Directory.CreateDirectory("Assets/Art/Material");
        const int N = 512, G = 8;
        var rnd = new System.Random(name.GetHashCode());
        var grid = new float[G, G];
        for (int i = 0; i < G; i++) for (int j = 0; j < G; j++) grid[i, j] = (float)rnd.NextDouble();
        var pts = new Vector2[70];
        for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2((float)rnd.NextDouble() * N, (float)rnd.NextDouble() * N);
        var tex = new Texture2D(N, N, TextureFormat.RGB24, true);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float gx = x * G / (float)N, gy = y * G / (float)N;
                int x0 = (int)gx, y0 = (int)gy; float fx = Mathf.SmoothStep(0, 1, gx - x0), fy = Mathf.SmoothStep(0, 1, gy - y0);
                float n = Mathf.Lerp(Mathf.Lerp(grid[x0 % G, y0 % G], grid[(x0 + 1) % G, y0 % G], fx), Mathf.Lerp(grid[x0 % G, (y0 + 1) % G], grid[(x0 + 1) % G, (y0 + 1) % G], fx), fy);
                float k = 1f + (n - 0.5f) * 2f * variation + ((float)rnd.NextDouble() - 0.5f) * 0.04f;
                if (cracks)
                {
                    float d1 = 1e9f, d2 = 1e9f;
                    foreach (var p in pts)
                    {
                        float dx = Mathf.Abs(x - p.x); dx = Mathf.Min(dx, N - dx);
                        float dy = Mathf.Abs(y - p.y); dy = Mathf.Min(dy, N - dy);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
                    }
                    if (d2 - d1 < 1.6f) k *= 0.88f;
                }
                tex.SetPixel(x, y, new Color(baseCol.r * k, baseCol.g * k, baseCol.b * k));
            }
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void AddDustMotes(Transform cam) // ละอองแสงลอยในอากาศหน้ากล้อง
    {
        var go = new GameObject("DustMotes");
        go.transform.SetParent(cam, false);
        go.transform.localPosition = new Vector3(0, 0, 8f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main; main.startLifetime = 6f; main.startSpeed = 0.2f; main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new Color(1f, 0.95f, 0.7f, 0.8f); main.maxParticles = 150; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 20f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(16f, 6f, 14f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Mat("M_Dust", "Universal Render Pipeline/Particles/Unlit", new Color(1f, 0.95f, 0.75f, 0.8f), AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        var m = r.sharedMaterial;                       // โปร่งใสแบบบวกแสง
        m.SetFloat("_Surface", 1); m.SetFloat("_ZWrite", 0);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
    }

    static void MakeMountains(Transform parent) // ภูเขา low-poly สร้างจากโค้ด อยู่ไกลๆ ไม่เลื่อน
    {
        Directory.CreateDirectory("Assets/Art/Material");
        var mats = new[] {
            Mat("M_Mountain_Green", "Universal Render Pipeline/Lit", new Color(0.36f, 0.62f, 0.32f)),
            Mat("M_Mountain_Teal", "Universal Render Pipeline/Lit", new Color(0.4f, 0.62f, 0.55f)),
            Mat("M_Mountain_Blue", "Universal Render Pipeline/Lit", new Color(0.5f, 0.62f, 0.75f)) };
        var snow = Mat("M_Mountain_Snow", "Universal Render Pipeline/Lit", new Color(0.95f, 0.97f, 1f));
        var rnd = new System.Random(3);
        var prefabs = new GameObject[6];                 // ภูเขา 6 แบบ ปั้นจาก Cube (เขียว 2 / เขียวอมฟ้า 2 / ฟ้า 2)
        for (int i = 0; i < prefabs.Length; i++) prefabs[i] = CubeMountain(i, mats[i / 2], snow, rnd);
        var root = new GameObject("Mountains").transform;
        root.SetParent(parent);
        for (int i = 0; i < 26; i++)
        {
            float z = Mathf.Lerp(230f, 320f, (float)rnd.NextDouble());
            int band = z > 290 ? 2 : (z > 260 ? 1 : 0);     // ไกลขึ้น = สีฟ้าจางขึ้น
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[band * 2 + rnd.Next(2)], root);
            go.name = "Mountain_" + i;
            go.transform.position = new Vector3(Mathf.Lerp(-260f, 260f, (float)rnd.NextDouble()), -2f, z);
            float h = Mathf.Lerp(18f, 42f, (float)rnd.NextDouble());
            go.transform.localScale = new Vector3(h * 1.6f, h, h * 1.4f);
            go.transform.rotation = Quaternion.Euler(0, rnd.Next(360), 0);
        }
    }

    // ภูเขา 1 ลูก = Cube หมุนตั้งมุมขึ้น (ฝังครึ่งล่างในดิน) ยอดหลัก 1 + ยอดเล็ก 2-3 + หิมะบนยอด -> prefab ใน Art/Material
    static GameObject CubeMountain(int index, Material rockMat, Material snowMat, System.Random rnd)
    {
        var root = new GameObject("Mountain_" + index);
        var tilt = new Vector3(45f, 0f, 35.26f);         // หมุนให้มุมของ cube ชี้ขึ้นเป็นยอดแหลม
        Peak(root.transform, "Mountain", new Vector3(0f, 0.15f, 0f), 0.75f, tilt, rockMat);
        int sides = 2 + rnd.Next(2);
        for (int i = 0; i < sides; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Peak(root.transform, "Side_" + i, new Vector3(side * (0.35f + 0.2f * (float)rnd.NextDouble()), 0f, ((float)rnd.NextDouble() - 0.5f) * 0.4f),
                 0.4f + 0.15f * (float)rnd.NextDouble(), tilt + new Vector3(0, rnd.Next(90), 0), rockMat);
        }
        float top = 0.15f + 0.75f * 0.866f;               // ความสูงยอด = ศูนย์กลาง + ครึ่งเส้นทแยงของ cube
        Peak(root.transform, "SnowCap", new Vector3(0f, top - 0.2f * 0.866f, 0f), 0.2f, tilt, snowMat);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Art/Material/Mountain_" + index + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void Peak(Transform parent, string name, Vector3 pos, float size, Vector3 euler, Material m)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = name;
        Object.DestroyImmediate(c.GetComponent<Collider>());     // ฉากหลัง ไม่ต้องชน
        c.transform.SetParent(parent, false);
        c.transform.localPosition = pos;
        c.transform.localEulerAngles = euler;
        c.transform.localScale = Vector3.one * size;
        var r = c.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    const string CharDir = "Assets/Character_Model/";
    static GameObject[] charPrefabs;

    static void CreateCharacters() // ตัวละคร 4 ตัวจาก Character_Model -> วัสดุ + prefab ขนาดเท่ากัน + รูปการ์ดในร้าน
    {
        Directory.CreateDirectory(PrefabDir + "Characters");
        charPrefabs = new GameObject[CharacterData.Count];
        for (int i = 0; i < CharacterData.Count; i++)
        {
            string fbx = null, baseTex = null, normal = null;
            foreach (var g in AssetDatabase.FindAssets("", new[] { CharDir + CharacterData.Folders[i] }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(".fbx")) fbx = p; else if (p.EndsWith("_texture.png")) baseTex = p; else if (p.EndsWith("_normal.png")) normal = p;
            }
            var mat = Mat("M_" + CharacterData.Names[i], "Universal Render Pipeline/Lit", Color.white, AssetDatabase.LoadAssetAtPath<Texture2D>(baseTex));
            mat.SetFloat("_Smoothness", 0.45f);
            if (normal != null)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(normal);
                if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal)); mat.EnableKeyword("_NORMALMAP");
            }
            var root = new GameObject("Char_" + CharacterData.Names[i]);
            var model = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            model.name = "Model";
            model.transform.rotation = Quaternion.Euler(0, 180, 0) * model.transform.rotation; // โมเดล Meshy หน้าหัน +Z -> หมุนให้หน้าหัน -Z (เข้ากล้อง)
            foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
            var b = ModelBounds(model);
            model.transform.localScale *= 1.5f / Mathf.Max(b.size.x, b.size.y, b.size.z);   // ขนาดใหญ่สุด 1.5 เท่ากันทุกตัว
            b = ModelBounds(model);
            model.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);       // วางเท้าที่ y=0 กลางตัว
            model.transform.SetParent(root.transform, true);
            charPrefabs[i] = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "Characters/" + root.name + ".prefab");
            Object.DestroyImmediate(root);
            RenderPortrait(charPrefabs[i], ArtUI + "Char_" + CharacterData.Names[i] + ".png");
        }
    }

    static Bounds ModelBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void RenderPortrait(GameObject prefab, string path) // ถ่ายรูปตัวละครพื้นใส ไว้ทำการ์ดในร้าน
    {
        var stage = new GameObject("PortraitStage").transform;
        stage.position = new Vector3(0, -1000, 0);
        var m = Object.Instantiate(prefab, stage);
        m.transform.localRotation = Quaternion.Euler(0, 160, 0);
        var cam = new GameObject("Cam").AddComponent<Camera>();
        cam.transform.SetParent(stage, false);
        cam.transform.localPosition = new Vector3(0, 1.2f, -3.6f); cam.transform.localEulerAngles = new Vector3(10, 0, 0);
        cam.fieldOfView = 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(stage, false); light.type = LightType.Directional; light.intensity = 1.3f; light.transform.localEulerAngles = new Vector3(35, -30, 0);
        var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); tex.Apply();
        RenderTexture.active = null; cam.targetTexture = null; rt.Release();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(stage.gameObject);
        AssetDatabase.ImportAsset(path);
        SetSprite(path, 256, Vector4.zero);
    }

    static CharacterModel AddCharacter(Transform parent, float yRot, float scale, bool useSelected = true) // จุดวางตัวละคร (โมเดลสร้างตามตัวที่เลือกตอนเล่น)
    {
        var holder = new GameObject("Character").transform;
        holder.SetParent(parent, false);
        holder.localEulerAngles = new Vector3(0, yRot, 0);
        holder.localScale = Vector3.one * scale;
        var cm = holder.gameObject.AddComponent<CharacterModel>();
        cm.prefabs = charPrefabs;
        cm.useSelected = useSelected;
        cm.Show(0);   // โชว์ SlimeGreen ใน Edit Mode
        return cm;
    }

    static void NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    static void Save(string n) => EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SceneDir + n + ".unity");

    //  5a) MAIN MENU (ป่า 3D + สไลม์ตัวใหญ่ + ป้ายไม้ชื่อเกม + ป้ายชี้ทาง PLAY/SETTINGS/EXIT)
    static void BuildMainMenu()
    {
        NewScene();
        var sun = MakeSun();
        sun.transform.rotation = Quaternion.Euler(38, -40, 0);
        var cam = MakeCamera(new Vector3(0.7f, 1.7f, -5.2f), new Vector3(7, 0, 0), 55);
        cam.clearFlags = CameraClearFlags.Skybox;
        SetupSkyAndFog();
        MakeEventSystem();
        var menu = new GameObject("GameManager").AddComponent<MenuManager>(); // หน้าเมนู: GameManager คุมเมนู/ร้านค้า/เริ่มเกม

        var env = new GameObject("Environment").transform;
        BuildForest(env);                                   // ไม่มี WorldScroller -> ป่าอยู่นิ่ง
        var hero = new GameObject("HeroProps").transform;   // ต้นไม้ใหญ่/หินใกล้กล้อง ให้ภาพดูเป็นป่าทึบ
        hero.SetParent(env);
        SitOnGround(Model(Kit + "CommonTree_2.fbx", hero, new Vector3(-4.2f, 0, 3f), new Vector3(0, 40, 0), 1.3f));
        SitOnGround(Model(Kit + "Pine_3.fbx", hero, new Vector3(5.2f, 0, 6f), new Vector3(0, 10, 0), 1.2f));
        SitOnGround(Model(Kit + "CommonTree_4.fbx", hero, new Vector3(-6f, 0, 12f), Vector3.zero, 1.2f));
        SitOnGround(Model(Kit + "Rock_Medium_1.fbx", hero, new Vector3(-2.6f, 0, -0.6f), new Vector3(0, 60, 0), 0.5f));
        SitOnGround(Model(Kit + "Mushroom_Common.fbx", hero, new Vector3(2.4f, 0, 0.2f), Vector3.zero, 1.2f));
        SitOnGround(Model(Kit + "Fern_1.fbx", hero, new Vector3(-3.2f, 0, 3.5f), Vector3.zero, 0.9f));
        SitOnGround(Model(Kit + "Flower_4_Group.fbx", hero, new Vector3(1.8f, 0, 1.4f), Vector3.zero, 1.2f));

        var slimeRoot = new GameObject("MenuSlime").transform;   // สไลม์ตัวใหญ่หน้ากล้อง หายใจเด้งเบาๆ
        slimeRoot.position = new Vector3(-0.1f, 0f, 0f);
        var menuChar = AddCharacter(slimeRoot, -12f, 1.2f);
        slimeRoot.gameObject.AddComponent<BossSlimeReaction>().slimeVisual = menuChar.transform;
        AddNameTag(slimeRoot, menuChar.transform, 3.2f);

        var canvas = MakeCanvas("Canvas_MainMenu");
        var wood = S(ArtUI + "Button_Wood.png");
        var rope = new Color(0.86f, 0.78f, 0.55f);
        Img(canvas.transform, "RopeL", S(ArtGen + "Fx_White.png"), TC, 680, 70, 16, 150, rope);
        Img(canvas.transform, "RopeR", S(ArtGen + "Fx_White.png"), TC, 1180, 70, 16, 150, rope);
        var title = Img(canvas.transform, "TitleBoard", wood, TC, 930, 260, 860, 340);
        title.rectTransform.localEulerAngles = new Vector3(0, 0, -1.5f);
        TitleTxt(title.transform, "Title", "SLIME\nEATER", 150, new Color(0.93f, 0.78f, 0.38f));

        var post = Img(canvas.transform, "SignPost", S(ArtGen + "Fx_RoundRect.png"), BC, 1600, 690, 70, 820, new Color(0.47f, 0.32f, 0.18f));
        post.pixelsPerUnitMultiplier = 2f;
        var play = SignButton(canvas.transform, "StartButton", "PLAY", 1590, 470, 4f, menu.StartGame);
        var setting = SignButton(canvas.transform, "SettingButton", "SETTINGS", 1590, 680, -3f, menu.OpenSetting);
        var exit = SignButton(canvas.transform, "ExitButton", "EXIT", 1575, 885, 3f, menu.ExitGame);
        menu.exitButton = exit.gameObject;
        Portrait(canvas, Move(post.rectTransform, 0, 640), Move(play, 0, 860), Move(setting, 0, 680), Move(exit, 0, 500), // แนวตั้ง: ป้ายไม้ย้ายมากลางล่าง
            Move(title, 0, -480, 0.85f), Move(canvas.transform.Find("RopeL"), -250, -330), Move(canvas.transform.Find("RopeR"), 250, -330)); // ป้ายชื่อเกมลงมาใต้กรอบสถิติ
        var menuLayout = canvas.GetComponent<OrientationLayout>();
        menuLayout.fixedCamera = cam; menuLayout.portraitCamPos = new Vector3(0.2f, 2.4f, -10.5f); menuLayout.portraitFov = 62f; // ถอยกล้อง เห็นสไลม์ทั้งตัวกลางจอ

        var shop = menu.gameObject.AddComponent<MenuShop>();
        shop.menuModel = menuChar;
        Img(canvas.transform, "BankIcon", S(HC + "Icons/coin.png"), TL, 70, 70, 70, 70);
        shop.bankText = Txt(canvas.transform, "BankText", "0", TL, 230, 70, 260, 70, 50, Color.white, TextAlignmentOptions.Left, true);
        BuildStatsPanel(canvas.transform, shop);
        RoundMenuButton(canvas.transform, "ShopButton", HC + "Icons/gift.png", "ร้านค้า", 120, shop.OpenShop);
        var missions = menu.gameObject.AddComponent<MissionPanel>();
        var league = menu.gameObject.AddComponent<LeaguePanel>();
        RoundMenuButton(canvas.transform, "MissionButton", HC + "Icons/flag.png", "ภารกิจ", 290, missions.Open);
        RoundMenuButton(canvas.transform, "LeagueButton", HC + "Icons/leaderboard.png", "ลีก", 460, league.Open);
        BuildShopPanel(canvas.transform, shop);
        BuildMissionPanel(canvas.transform, missions, shop);
        BuildLeaguePanel(canvas.transform, league);

        menu.settingPanel = MakeSettingsPanel(canvas.transform, false, null, menu, out var slider, out var sfx);
        BuildNamePanel(canvas.transform, menu);
        RotatePrompt(canvas);
        MakeAudio(slider, sfx);
        new GameObject("SwipeInput").AddComponent<SwipeInput>();
        Save("MainMenu");
    }

    // ---------- แนวตั้ง (มือถือ) ----------
    static OrientationLayout.Move Move(Component t, float x, float y, float scale = 0f, Vector2? anchor = null) // ตำแหน่งตอนแนวตั้ง (anchoredPosition)
        => new OrientationLayout.Move { target = (RectTransform)t.transform, pos = new Vector2(x, y), scale = scale, reanchor = anchor.HasValue, anchor = anchor ?? Vector2.zero };

    static void Portrait(Canvas c, params OrientationLayout.Move[] m) // เพิ่มรายการย้ายให้ Canvas นี้
    {
        var o = c.GetComponent<OrientationLayout>();
        var list = new List<OrientationLayout.Move>(o.moves); list.AddRange(m);
        o.moves = list.ToArray();
    }

    static void RotatePrompt(Canvas c) // มือถือถือแนวนอน -> ขึ้นจอบอกให้หมุนเป็นแนวตั้ง (บังการกดด้านหลัง)
    {
        var r = Stretch(c.transform, "RotatePrompt");
        r.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.12f, 0.15f, 0.96f);
        var t = Txt(r, "Text", "หมุนโทรศัพท์เป็นแนวตั้ง\nเพื่อเล่นได้ดีที่สุด", C, 960, 540, 1200, 300, 64, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.Center, true);
        c.GetComponent<OrientationLayout>().rotatePrompt = r.gameObject;
        r.gameObject.SetActive(false);
    }

    static void BuildStatsPanel(Transform canvas, MenuShop shop) // กรอบสถิติมุมซ้ายบน: คะแนนสูงสุด + ด่านสูงสุด
    {
        var p = Img(canvas, "StatsPanel", S(HC + "Panel_Sprites/Frame 1584.png"), TL, 245, 218, 440, 215).transform;
        var gold = new Color(1f, 0.85f, 0.3f);
        Img(p, "CrownIcon", S(HC + "Icons/crown.png"), C, 790, 475, 54, 48);
        Txt(p, "ScoreLabel", "คะแนนสูงสุด", C, 920, 475, 190, 50, 30, Color.white, TextAlignmentOptions.Left, true);
        shop.bestScoreText = Txt(p, "BestScore", "0", C, 1080, 475, 170, 56, 40, gold, TextAlignmentOptions.Right, true);
        Img(p, "FlagIcon", S(HC + "Icons/flag.png"), C, 790, 540, 46, 52);
        Txt(p, "StageLabel", "ด่านสูงสุด", C, 920, 540, 190, 50, 30, Color.white, TextAlignmentOptions.Left, true);
        shop.bestStageText = Txt(p, "BestStage", "0", C, 1080, 540, 170, 56, 40, gold, TextAlignmentOptions.Right, true);
        Img(p, "LeagueIcon", S(HC + "Icons/leaderboard.png"), C, 790, 605, 50, 50);
        Txt(p, "LeagueLabel", "ลีก", C, 920, 605, 190, 50, 30, Color.white, TextAlignmentOptions.Left, true);
        shop.leagueText = Txt(p, "League", "Bronze #10", C, 1060, 605, 210, 56, 34, gold, TextAlignmentOptions.Right, true);
    }

    static void BuildNamePanel(Transform canvas, MenuManager menu) // ตั้งชื่อก่อนเข้าเกม (ชื่อไปลอยบนหัวสไลม์)
    {
        var root = Stretch(canvas, "NamePanel");
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        var p = Img(root, "Panel", S(HC + "Panel_Sprites/Settings pannel.png"), C, 960, 560, 760, 500).transform;
        Img(p, "TitleBar", S(HC + "Buttons/empty_buttons/orange.png"), C, 960, 290, 480, 120);
        Txt(p, "Title", "ตั้งชื่อผู้เล่น", C, 960, 286, 480, 120, 56, new Color(1f, 0.96f, 0.85f), TextAlignmentOptions.Center, true);
        Btn(p, "CloseX", S(HC + "Icons/Close.png"), C, 1290, 330, 86, 86, null, menu.CloseName);
        Txt(p, "Label", "ชื่อนี้จะลอยอยู่บนหัวสไลม์ (สูงสุด 12 ตัวอักษร)", C, 960, 400, 640, 50, 28, Color.white, TextAlignmentOptions.Center, true);
        var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        var rt = (RectTransform)go.transform;
        rt.SetParent(p, false);
        rt.sizeDelta = new Vector2(560, 100); rt.anchoredPosition = new Vector2(0, 540 - 505);
        go.name = "NameInput";
        var bg = go.GetComponent<Image>(); bg.sprite = S(ArtGen + "Fx_RoundRect.png"); bg.type = Image.Type.Sliced; bg.color = new Color(1f, 0.97f, 0.88f);
        var input = go.GetComponent<TMP_InputField>();
        input.characterLimit = 12;
        foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true)) { t.font = thaiFont; t.fontSize = 44; t.alignment = TextAlignmentOptions.Center; }
        ((TextMeshProUGUI)input.placeholder).text = "พิมพ์ชื่อที่นี่";
        input.textComponent.color = new Color(0.25f, 0.15f, 0.1f);
        menu.nameInput = input;
        menu.nameWarning = Txt(p, "Warning", "", C, 960, 585, 600, 44, 28, new Color(1f, 0.55f, 0.45f), TextAlignmentOptions.Center, true);
        IconPill(p, "StartButton", "green", HC + "Icons/Play (1).png", "เริ่มเกม", 680, menu.ConfirmName);
        menu.namePanel = root.gameObject;
        root.gameObject.SetActive(false);
    }

    static TextMeshPro AddNameTag(Transform parent, Transform follow, float size, bool playerName = true) // ชื่อลอยบนหัว (ตัวหนังสือ 3D)
    {
        var t = new GameObject("NameTag").AddComponent<TextMeshPro>();
        t.transform.SetParent(parent, false);
        t.font = thaiFont; t.fontSharedMaterial = fontOutline;
        t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.rectTransform.sizeDelta = new Vector2(8f, 1.2f);
        t.text = "ชื่อผู้เล่น";
        t.gameObject.AddComponent<ThaiTextFixer>();
        var tag = t.gameObject.AddComponent<NameTag>();
        tag.follow = follow; tag.usePlayerName = playerName;
        return t;
    }

    static void RoundMenuButton(Transform canvas, string name, string icon, string label, float x, UnityAction onClick) // ปุ่มกลมมุมซ้ายล่าง
    {
        var b = Btn(canvas, name, S(ArtGen + "Fx_Circle.png"), BL, x, 950, 130, 130, null, onClick);
        b.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.6f, 0.92f);
        Img(b.transform, "Icon", S(icon), C, 960, 530, 78, 70);
        Txt(canvas, name + "Label", label, BL, x, 1040, 200, 50, 32, Color.white, TextAlignmentOptions.Center, true);
    }

    static Image Panel(Transform canvas, string name, string sprite, float w, float h, string title, out GameObject root) // พาเนลกลางจอ + หัวข้อป้ายไม้ + มืดด้านหลัง
    {
        var r = Stretch(canvas, name);
        r.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
        var p = Img(r, "Panel", S(sprite), C, 960, 560, w, h);
        Img(p.transform, "TitleBar", S(ArtUI + "Button_Wood.png"), C, 960, 540 - h * 0.5f, 480, 110);
        Txt(p.transform, "Title", title, C, 960, 540 - h * 0.5f - 2, 480, 110, 56, Color.white, TextAlignmentOptions.Center, true);
        root = r.gameObject;
        return p;
    }

    static void BuildShopPanel(Transform canvas, MenuShop shop) // ร้านค้า: อัปเกรด 3 อย่าง + สกิน 5 สี
    {
        var p = Panel(canvas, "ShopPanel", HC + "Panel_Sprites/Main Menu pannel.png", 1150, 820, "ร้านค้า", out var root);
        shop.shopPanel = root;
        Btn(p.transform, "Close", S(HC + "Icons/Close.png"), C, 1490, 175, 80, 80, null, shop.CloseShop);
        string[] names = { "แม่เหล็กดูดเหรียญ (+1 วิ / เลเวล)", "เกราะกันชน (+1 วิ / เลเวล)", "เลือดสูงสุด (+5 / เลเวล)" };
        string[] icons = { HC + "Icons/Upgrade-StarBase.png", ArtUI + "Icon_Shield.png", HC + "Icons/heartt.png" };
        shop.upgradeLevelTexts = new TMP_Text[3]; shop.upgradePriceTexts = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            float y = 300 + i * 125;
            Img(p.transform, "RowBg_" + i, S(HC + "Panel_Sprites/Frame 1584.png"), C, 960, y, 1000, 110);
            Img(p.transform, "UpIcon_" + i, S(icons[i]), C, 530, y, 74, 74);
            Txt(p.transform, "UpName_" + i, names[i], C, 800, y, 440, 70, 27, Color.white, TextAlignmentOptions.Left, true);
            shop.upgradeLevelTexts[i] = Txt(p.transform, "UpLevel_" + i, "Lv 0 / 10", C, 1110, y, 160, 60, 32, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.Center, true);
            var buy = Btn(p.transform, "Buy_" + i, S(HC + "Buttons/empty_buttons/GOLDEN.png"), C, 1320, y, 200, 80, null, null);
            UnityEventTools.AddIntPersistentListener(buy.onClick, shop.BuyUpgrade, i);
            Img(buy.transform, "Coin", S(HC + "Icons/coin.png"), C, 890, 540, 40, 40);
            shop.upgradePriceTexts[i] = Txt(buy.transform, "Price", "150", C, 990, 540, 120, 60, 32, Color.white, TextAlignmentOptions.Center, true);
        }
        Txt(p.transform, "CharacterLabel", "ตัวละคร", C, 960, 655, 400, 50, 36, Color.white, TextAlignmentOptions.Center, true);
        shop.charStatusTexts = new TMP_Text[CharacterData.Count];
        for (int i = 0; i < CharacterData.Count; i++) // การ์ดตัวละคร: รูป + ชื่อ + ราคา/สถานะ กดแล้วเปิดหน้าดูตัวละคร
        {
            var card = Btn(p.transform, "Character_" + i, S(HC + "Panel_Sprites/Frame 1584.png"), C, 600 + i * 240, 800, 210, 230, null, null);
            UnityEventTools.AddIntPersistentListener(card.onClick, shop.ViewCharacter, i);
            Img(card.transform, "Portrait", S(ArtUI + "Char_" + CharacterData.Names[i] + ".png"), C, 960, 495, 150, 150);
            Txt(card.transform, "Name", CharacterData.Names[i], C, 960, 590, 200, 40, 26, Color.white, TextAlignmentOptions.Center, true);
            shop.charStatusTexts[i] = Txt(card.transform, "Status", "0", C, 960, 628, 200, 40, 28, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.Center, true);
        }
        BuildCharacterPopup(root.transform, shop);
        root.SetActive(false);
    }

    static void BuildCharacterPopup(Transform shopRoot, MenuShop shop) // หน้าดูตัวละคร: โมเดล 3D หมุนโชว์ + ความสามารถ + ปุ่มซื้อ/ใช้
    {
        var r = Stretch(shopRoot, "CharacterPopup");
        r.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);   // บังปุ่มร้านค้าด้านหลัง
        shop.charPopup = r.gameObject;
        var p = Img(r, "Panel", S(HC + "Panel_Sprites/Main Menu pannel.png"), C, 960, 560, 1040, 660);
        Img(p.transform, "TitleBar", S(ArtUI + "Button_Wood.png"), C, 960, 230, 520, 110);
        shop.popName = Txt(p.transform, "Name", "SlimeGreen", C, 960, 228, 520, 110, 52, Color.white, TextAlignmentOptions.Center, true);
        Btn(p.transform, "Close", S(HC + "Icons/Close.png"), C, 1440, 265, 80, 80, null, shop.CloseCharacter);
        Img(p.transform, "PreviewFrame", S(HC + "Panel_Sprites/Frame 1584.png"), C, 700, 565, 420, 440);
        var raw = Rect(p.transform, "Preview", C, 700, 565, 390, 410).gameObject.AddComponent<RawImage>();
        raw.texture = PreviewTexture(); raw.raycastTarget = false;
        shop.popInfo = Txt(p.transform, "Info", "", C, 1165, 470, 480, 280, 28, Color.white, TextAlignmentOptions.TopLeft, true);
        var buy = Btn(p.transform, "BuyButton", S(HC + "Buttons/empty_buttons/GOLDEN.png"), C, 1165, 690, 320, 100, null, shop.ConfirmCharacter);
        shop.popCoin = Img(buy.transform, "Coin", S(HC + "Icons/coin.png"), C, 870, 540, 52, 52).gameObject;
        shop.popButtonText = Txt(buy.transform, "Label", "ใช้ตัวนี้", C, 985, 540, 220, 80, 38, Color.white, TextAlignmentOptions.Center, true);
        shop.popMessage = Txt(p.transform, "Message", "", C, 1165, 780, 480, 50, 28, new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Center, true);

        var stage = new GameObject("CharacterPreviewStage").transform;   // ฉากเล็กใต้ดิน ถ่ายโมเดลส่งเข้า RawImage
        stage.position = new Vector3(0, -200, 0);
        var cam = new GameObject("PreviewCamera").AddComponent<Camera>();
        cam.transform.SetParent(stage, false);
        cam.transform.localPosition = new Vector3(0, 1.05f, -3.9f); cam.transform.localEulerAngles = new Vector3(8, 0, 0);
        cam.fieldOfView = 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.13f, 0.33f, 0.38f);
        cam.targetTexture = (RenderTexture)raw.texture;
        var model = AddCharacter(stage, 0f, 1f, false);
        model.spin = 45f;
        shop.previewModel = model;
        shop.previewStage = stage.gameObject;
        stage.gameObject.SetActive(false);
    }

    static Material HueMaterial() // วัสดุเปลี่ยนเฉดสีของบอท (shader ต้องถูกอ้างอิงจากวัสดุ จึงติดไปตอน build)
    {
        string path = MatDir + "M_RivalHue.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("SlimeEater/HueShiftLit")); AssetDatabase.CreateAsset(m, path); }
        return m;
    }

    static RenderTexture PreviewTexture()
    {
        string path = ArtGen + "RT_CharacterPreview.renderTexture";
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (rt) return rt;
        rt = new RenderTexture(512, 540, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        AssetDatabase.CreateAsset(rt, path);
        return rt;
    }

    static void BuildMissionPanel(Transform canvas, MissionPanel m, MenuShop shop) // ภารกิจ: แท็บ 3 อัน + เลขหน้า 1-9 + 3 แถว
    {
        var p = Panel(canvas, "MissionPanel", HC + "Panel_Sprites/Daily Reward pannel.png", 1050, 860, "ภารกิจ", out var root).transform;
        m.panel = root; m.shop = shop;
        Btn(p, "Close", S(HC + "Icons/Close.png"), C, 1440, 155, 80, 80, null, m.Close);
        var brown = new Color(0.35f, 0.22f, 0.12f);
        string[] tabNames = { "ประจำวัน", "หลัก", "ต่อเนื่อง" };
        m.tabs = new Image[3];
        for (int t = 0; t < 3; t++)
        {
            var b = Btn(p, "Tab_" + t, S(ArtGen + "Fx_RoundRect.png"), C, 680 + t * 280, 230, 250, 66, tabNames[t], null, 34);
            UnityEventTools.AddIntPersistentListener(b.onClick, m.ShowTab, t);
            m.tabs[t] = b.GetComponent<Image>();
        }
        var pages = Rect(p, "Pages", C, 960, 315, 900, 70);                // เลขหน้า (หน้าที่ทำครบหายไป ที่เหลือเลื่อนมาชิดกัน)
        var hl = pages.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 16; hl.childAlignment = TextAnchor.MiddleCenter; hl.childControlWidth = hl.childControlHeight = false;
        m.pagesRoot = pages.gameObject;
        m.pageButtons = new GameObject[MissionSystem.Pages];
        for (int i = 0; i < MissionSystem.Pages; i++)
        {
            var b = Btn(pages, "Page_" + (i + 1), S(ArtGen + "Fx_Circle.png"), C, 960, 540, 66, 66, (i + 1).ToString(), null, 36);
            b.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.6f);
            UnityEventTools.AddIntPersistentListener(b.onClick, m.ShowPage, i);
            m.pageButtons[i] = b.gameObject;
        }
        m.pageTitle = Txt(p, "PageTitle", "หน้า 1 / 9", C, 960, 378, 700, 50, 34, brown, TextAlignmentOptions.Center);
        var list = Rect(p, "Missions", C, 960, 640, 900, 474);              // ภารกิจที่ทำเสร็จหายไป ที่เหลือเลื่อนขึ้น
        var vl = list.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 12; vl.childAlignment = TextAnchor.UpperCenter; vl.childControlWidth = vl.childControlHeight = false;
        m.rows = new GameObject[3]; m.texts = new TMP_Text[3]; m.bars = new Image[3];
        for (int r = 0; r < 3; r++)
        {
            var row = Rect(list, "Mission_" + r, C, 960, 540, 900, 150);
            Img(row, "Flag", S(HC + "Icons/flag.png"), C, 540, 525, 50, 56);
            m.texts[r] = Txt(row, "Text", "ภารกิจ", C, 980, 515, 780, 100, 34, brown, TextAlignmentOptions.Left);
            Img(row, "BarBg", S(HC + "Panel_Sprites/Rectangle 358.png"), C, 980, 582, 780, 22);
            var fill = Img(row, "BarFill", S(ArtGen + "Fx_White.png"), C, 980, 582, 774, 16, new Color(1f, 0.62f, 0.15f));
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            m.rows[r] = row.gameObject; m.bars[r] = fill;
        }
        m.footer = Txt(p, "Footer", "", C, 960, 905, 900, 50, 28, brown, TextAlignmentOptions.Center);
        root.SetActive(false);
    }

    static void BuildLeaguePanel(Transform canvas, LeaguePanel l) // ตารางลีก 10 อันดับ
    {
        var p = Panel(canvas, "LeaguePanel", HC + "Panel_Sprites/Main Menu pannel.png", 1050, 900, "ลีกสไลม์", out var root).transform;
        l.panel = root;
        Btn(p, "Close", S(HC + "Icons/Close.png"), C, 1440, 135, 80, 80, null, l.Close);
        l.title = Txt(p, "League", "ลีก Bronze", C, 960, 215, 900, 50, 36, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.Center, true);
        l.rowBg = new Image[10]; l.rowText = new TMP_Text[10]; l.rowPoints = new TMP_Text[10];
        for (int r = 0; r < 10; r++)
        {
            float y = 280 + r * 58;
            l.rowBg[r] = Img(p, "Row_" + r, S(ArtGen + "Fx_RoundRect.png"), C, 960, y, 900, 52);
            l.rowText[r] = Txt(l.rowBg[r].transform, "Name", "", C, 860, 540, 660, 50, 30, Color.white, TextAlignmentOptions.Left, true);
            l.rowPoints[r] = Txt(l.rowBg[r].transform, "Points", "", C, 1270, 540, 200, 50, 30, Color.white, TextAlignmentOptions.Right, true);
        }
        l.info = Txt(p, "Info", "", C, 960, 900, 940, 90, 26, Color.white, TextAlignmentOptions.Center, true);
        root.SetActive(false);
    }

    static TextMeshProUGUI TitleTxt(Transform parent, string name, string text, float size, Color color) // ตัวหนังสืออังกฤษแบบเกม (Baloo)
    {
        var t = Txt(parent, name, text, C, 960, 540, 100, 100, size, color, TextAlignmentOptions.Center);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        t.font = titleFont; t.fontSharedMaterial = titleOutline;
        t.lineSpacing = -28f;
        return t;
    }

    static Button SignButton(Transform parent, string name, string label, float x, float y, float tilt, UnityAction onClick) // ป้ายไม้ชี้ทาง = ปุ่ม
    {
        var b = Btn(parent, name, S(ArtUI + "Button_Wood.png"), BC, x, y, 500, 160, null, onClick);
        b.transform.localEulerAngles = new Vector3(0, 0, tilt);
        var cb = b.colors; cb.highlightedColor = new Color(1f, 0.95f, 0.8f); cb.pressedColor = new Color(0.8f, 0.7f, 0.6f); b.colors = cb;
        TitleTxt(b.transform, "Label", label, 78, new Color(0.97f, 0.93f, 0.8f));
        return b;
    }

    //  5b) RUNNER SCENE (3D วิ่งในป่า 3 เลน)
    static void BuildRunnerScene()
    {
        NewScene();
        var sun = MakeSun();
        sun.transform.rotation = Quaternion.Euler(42, -35, 0);
        var cam = MakeCamera(new Vector3(0, 3.2f, -6.2f), new Vector3(16, 0, 0), 60);
        cam.clearFlags = CameraClearFlags.Skybox;
        SetupSkyAndFog();
        MakeEventSystem();

        // ---------------- GameManager (คุมเกมทั้งหมด ระบบย่อยอยู่ข้างใน) ----------------
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.gameObject.AddComponent<CharacterAbility>();        // ความสามารถตัวละคร
        gm.startState = GameState.RunnerPhase;
        var scroller = new GameObject("WorldScroller").AddComponent<WorldScroller>();
        scroller.transform.SetParent(gm.transform);
        gm.scroller = scroller;

        // ---------------- Environment: พื้นป่าวนไม่รู้จบ + ภูเขาไกลๆ ----------------
        BuildForest(new GameObject("Environment").transform);

        // ---------------- Player ----------------
        var player = new GameObject("Player_Slime");
        player.transform.position = Vector3.zero;
        var rb = player.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        var box = player.AddComponent<BoxCollider>(); box.center = new Vector3(0, 0.5f, 0); box.size = new Vector3(1.0f, 1.0f, 1.0f);
        var visual = new GameObject("Visual").transform;
        visual.SetParent(player.transform, false);
        var runChar = AddCharacter(visual, 180f, 1f);         // หันหลังให้กล้อง วิ่งไปข้างหน้า
        AddNameTag(player.transform, runChar.transform, 3f);
        var lp = player.AddComponent<LanePlayer>();
        lp.config = runnerConfig; lp.hitBox = box;
        lp.motion = player.AddComponent<SlimeMotion>();          // ท่าทางชุดเดียวกับบอท
        lp.motion.config = runnerConfig; lp.motion.visual = visual;
        var bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere); // ฟองเกราะ
        bubble.name = "ShieldBubble";
        Object.DestroyImmediate(bubble.GetComponent<Collider>());
        bubble.transform.SetParent(player.transform, false);
        bubble.transform.localPosition = new Vector3(0, 0.55f, 0); bubble.transform.localScale = Vector3.one * 1.9f;
        var bm = Mat("M_ShieldBubble", "Universal Render Pipeline/Unlit", new Color(0.4f, 0.9f, 1f, 0.28f));
        bm.SetFloat("_Surface", 1); bm.SetFloat("_ZWrite", 0); bm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bm.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); bm.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bm.renderQueue = 3000;
        bubble.GetComponent<Renderer>().sharedMaterial = bm;
        bubble.SetActive(false);
        player.AddComponent<PowerUps>().shieldBubble = bubble;
        AddDustMotes(cam.transform);
        var rc = cam.gameObject.AddComponent<RunnerCamera>();
        rc.target = player.transform;
        BuildRunnerRest(gm, player);
    }

    static ForestTrack BuildForest(Transform env) // พื้นป่า + ต้นไม้/หญ้า/หิน MegaKit + ภูเขา (ใช้ทั้งฉากวิ่งและเมนู)
    {
        var trackGo = new GameObject("Forest_Group");
        trackGo.transform.SetParent(env.transform);
        var forest = trackGo.AddComponent<ForestTrack>();
        var pathMat = Mat("M_ForestPath", "Universal Render Pipeline/Lit", Color.white, GroundTexture("T_Path", new Color(0.86f, 0.72f, 0.47f), 0.09f, true));
        pathMat.SetTextureScale("_BaseMap", new Vector2(1f, 2.5f));
        var grassMat = Mat("M_ForestGrass", "Universal Render Pipeline/Lit", Color.white, GroundTexture("T_Grass", new Color(0.45f, 0.68f, 0.22f), 0.12f, false));
        grassMat.SetTextureScale("_BaseMap", new Vector2(12f, 4f));
        var edgeMat = Mat("M_ForestPathEdge", "Universal Render Pipeline/Lit", new Color(0.62f, 0.55f, 0.32f));
        float pathW = runnerConfig.laneWidth * 3f + 1.2f;
        forest.pathHalfWidth = pathW * 0.5f;
        var segs = new Transform[9];
        for (int i = 0; i < segs.Length; i++)
        {
            var seg = new GameObject("Scene_Group_" + i).transform;
            seg.SetParent(trackGo.transform);
            seg.position = new Vector3(0, 0, -20f + i * forest.segmentLength);
            Ground(seg, "Path", new Vector3(0, -0.1f, 0), new Vector3(pathW, 0.2f, forest.segmentLength + 0.02f), pathMat);
            Ground(seg, "EdgeLeft", new Vector3(-pathW * 0.5f, -0.05f, 0), new Vector3(0.4f, 0.2f, forest.segmentLength + 0.02f), edgeMat);
            Ground(seg, "EdgeRight", new Vector3(pathW * 0.5f, -0.05f, 0), new Vector3(0.4f, 0.2f, forest.segmentLength + 0.02f), edgeMat);
            Ground(seg, "GrassLeft", new Vector3(-pathW * 0.5f - 30f, -0.12f, 0), new Vector3(60f, 0.2f, forest.segmentLength + 0.02f), grassMat);
            Ground(seg, "GrassRight", new Vector3(pathW * 0.5f + 30f, -0.12f, 0), new Vector3(60f, 0.2f, forest.segmentLength + 0.02f), grassMat);
            segs[i] = seg;
        }
        forest.segments = segs;
        forest.trees = NaturePrefabs(TreeNames);
        forest.plants = NaturePrefabs(PlantNames);
        forest.rocks = NaturePrefabs(RockNames);
        forest.treesPerSide = 8; forest.plantsPerSide = 22; forest.rocksPerSide = 3;
        forest.edgePlants = NaturePrefabs(EdgeNames);
        forest.canopyTrees = NaturePrefabs(CanopyNames);
        forest.DecorateAll();
        MakeMountains(env.transform);
        return forest;
    }

    static void BuildRunnerRest(GameManager gm, GameObject player)
    {
        var systems = gm.transform;
        // ---------------- Spawner (เริ่มสร้างหลังวิ่งไป firstSpawnAt เมตร) ----------------
        var track = new GameObject("Track (spawned obstacles & items)");
        var sp = track.AddComponent<TrackSpawner>();
        sp.config = runnerConfig;
        sp.rockPrefab = LoadPrefab<Obstacle>("Obstacle_Rock");
        sp.boulderPrefab = LoadPrefab<Obstacle>("Obstacle_Boulder");
        sp.highLogPrefab = LoadPrefab<Obstacle>("Obstacle_HighLog");
        sp.treeFromRightPrefab = LoadPrefab<Obstacle>("Obstacle_TreeFromRight");
        sp.treeFromLeftPrefab = LoadPrefab<Obstacle>("Obstacle_TreeFromLeft");
        sp.coinPrefab = LoadPrefab<Collectible>("Item_Coin");
        sp.heartPrefab = LoadPrefab<Collectible>("Item_Heart");
        sp.hintLeafPrefab = LoadPrefab<Collectible>("Item_HintLeaf");
        sp.powerUpPrefabs = new[] { LoadPrefab<Collectible>("Item_Magnet"), LoadPrefab<Collectible>("Item_Shield"), LoadPrefab<Collectible>("Item_Double") };

        // ---------------- UI ----------------
        var canvas = MakeCanvas("Canvas_RunnerUI");
        var ui = canvas.gameObject.AddComponent<UIManager>();
        gm.ui = ui;
        MakeHud(canvas.transform, ui);
        var keysHint = Txt(canvas.transform, "ControlsHint", "A / D = เปลี่ยนเลน    Space = กระโดด    S = สไลด์", BC, 960, 1040, 1200, 60, 34, Color.white, TextAlignmentOptions.Center, true);
        var swipeHint = Txt(canvas.transform, "SwipeHint", "ปัดซ้าย/ขวา = เปลี่ยนเลน   ปัดขึ้น = กระโดด   ปัดลง = สไลด์", BC, 960, 1040, 1250, 60, 34, Color.white, TextAlignmentOptions.Center, true);
        var layout = canvas.GetComponent<OrientationLayout>();
        layout.desktopOnly = new[] { keysHint.gameObject };      // คอมเห็นปุ่มคีย์บอร์ด มือถือเห็นวิธีปัด
        layout.touchOnly = new[] { swipeHint.gameObject };
        new GameObject("SwipeInput").AddComponent<SwipeInput>();

        // ---------------- บอทคู่แข่ง: แถบใต้คะแนน + ตัวบอทในฉาก ----------------
        var rival = Img(ui.hudRoot.transform, "RivalPanel", S(HC + "Panel_Sprites/Frame 1584.png"), TR, 1340, 200, 420, 110);
        ui.rivalRoot = rival.gameObject;
        ui.rivalName = Txt(rival.transform, "Name", "Bot", C, 895, 515, 260, 44, 28, new Color(1f, 0.6f, 0.5f), TextAlignmentOptions.Left, true);
        ui.rivalName.textWrappingMode = TextWrappingModes.NoWrap;            // ชื่อบอทบรรทัดเดียว
        ui.rivalGap = Txt(rival.transform, "Gap", "30 m", C, 1075, 515, 150, 44, 30, Color.white, TextAlignmentOptions.Right, true);
        Img(rival.transform, "HpBg", S(HC + "Panel_Sprites/Rectangle 358.png"), C, 960, 568, 370, 20);
        ui.rivalHp = Img(rival.transform, "HpFill", S(ArtGen + "Fx_White.png"), C, 960, 568, 364, 14, new Color(0.9f, 0.3f, 0.3f));
        ui.rivalHp.type = Image.Type.Filled; ui.rivalHp.fillMethod = Image.FillMethod.Horizontal;
        var botGo = new GameObject("RivalBot");
        var bot = botGo.AddComponent<RivalBot>();
        bot.player = player.GetComponent<LanePlayer>();
        bot.laneWidth = runnerConfig.laneWidth;
        bot.model = AddCharacter(botGo.transform, 180f, 1.3f, false);       // ใหญ่กว่าผู้เล่นนิดหน่อย มองเห็นแต่ไกล
        bot.motion = botGo.AddComponent<SlimeMotion>();
        bot.motion.config = runnerConfig; bot.motion.visual = bot.model.transform;
        bot.track = track.transform;
        bot.hueMaterial = HueMaterial();
        bot.nameTag = AddNameTag(botGo.transform, bot.model.transform, 3f, false);
        bot.nameTag.color = new Color(1f, 0.55f, 0.45f);
        Portrait(canvas, Move(ui.hudRoot.transform.Find("ScorePanel"), -240, -300), Move(rival, -240, -415)); // แนวตั้ง: คะแนน/บอทชิดขวาใต้เฟือง ไม่ทับหลอดเลือด

        // ---------------- สอนเล่นรอบแรก ----------------
        var tut = systems.gameObject.AddComponent<TutorialGuide>();
        tut.player = bot.player;
        var box = Img(canvas.transform, "TutorialHint", S(ArtGen + "Fx_RoundRect.png"), C, 960, 760, 1300, 130, new Color(0.08f, 0.2f, 0.25f, 0.85f));
        tut.hintBox = box.gameObject;
        tut.hint = Txt(box.transform, "Text", "", C, 960, 540, 1260, 120, 46, new Color(1f, 0.92f, 0.5f), TextAlignmentOptions.Center, true);
        Portrait(canvas, Move(box, 0, -150, 0.95f));
        MakeOverlays(canvas.transform, ui);
        ui.SetProgress(0.0f);
        ui.SetHp(GameSession.MaxHp, GameSession.MaxHp);

        // ---------------- Debug guard ----------------
        var guard = new GameObject("DebugGuard").AddComponent<DebugGuard>();
        guard.transform.SetParent(systems);
        guard.player = player.transform;

        Save("RunnerScene");
    }

    //  5c) BOSS SCENE (ทายเงาสัตว์)
    static void BuildBossScene()
    {
        NewScene();
        MakeSun();
        var cam = MakeCamera(new Vector3(0, 1.6f, -2f), new Vector3(4, 0, 0), 55);
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = 0.05f;
        SetupSkyAndFog(false);
        MakeEventSystem();

        var gm = new GameObject("GameManager").AddComponent<GameManager>(); // คุมเกมทั้งหมด ระบบย่อยอยู่ข้างใน
        var systems = gm.gameObject;
        systems.AddComponent<CharacterAbility>();               // SlimeAngel เกิดใหม่ได้ในฉากบอสด้วย
        gm.startState = GameState.BossPhase;

        // พื้นหลังป่า 3D ชุดเดียวกับฉากวิ่ง (อยู่นิ่ง) + ละอองแสง
        BuildForest(new GameObject("Environment").transform);
        AddDustMotes(cam.transform);
        // สไลม์ตัวใหญ่ย่อส่วนไว้หน้ากล้อง (ใกล้กว่า Canvas ที่ระยะ 1 เมตร จึงอยู่หน้า UI ส่วนป่าอยู่หลัง UI)
        var holder = new GameObject("BossSlimeHolder").transform;
        holder.SetParent(cam.transform, false);
        holder.localPosition = new Vector3(-0.42f, -0.33f, 0.8f);
        holder.localScale = Vector3.one * 0.17f;
        var slimeRoot = new GameObject("BossSlime").transform;
        slimeRoot.SetParent(holder, false);
        var slimeModel = AddCharacter(slimeRoot, -12f, 1.2f);                   // หันหน้าเกือบตรง เห็นตาทั้ง 2 ข้าง
        AddNameTag(slimeRoot, slimeModel.transform, 3f);

        var canvas = MakeCanvas("Canvas_BossUI", cam);
        canvas.planeDistance = 1f;
        var ui = canvas.gameObject.AddComponent<UIManager>();
        gm.ui = ui;

        var bossGo = new GameObject("BossQuizManager");
        bossGo.transform.SetParent(systems.transform);
        var boss = bossGo.AddComponent<BossQuizManager>();
        gm.boss = boss;
        foreach (var guid in AssetDatabase.FindAssets("t:AnimalDataSO", new[] { AnimalSO.TrimEnd('/') }))
            boss.animals.Add(AssetDatabase.LoadAssetAtPath<AnimalDataSO>(AssetDatabase.GUIDToAssetPath(guid)));

        // ภาพสัตว์ตรงกลาง (เงาดำ)
        boss.animalImage = Img(canvas.transform, "AnimalImage", null, C, 960, 640, 860, 420);
        boss.animalImage.preserveAspect = true;

        MakeHud(canvas.transform, ui);
        var hud = ui.hudRoot.transform;
        Object.DestroyImmediate(ui.progressText.gameObject); // ช่วงบอสหลอดเต็มแล้ว ไม่ต้องโชว์เมตร (กันทับนาฬิกา)
        ui.progressText = null;

        // นาฬิกานับถอยหลังวงกลม
        var timer = Img(hud, "TimerBadge", S(ArtUI + "Timer_Badge.png"), TC, 880, 145, 200, 200);              // ภาพนาฬิกา (icon)
        boss.timerRing = Img(timer.transform, "RingFill", S(ArtUI + "Timer_Ring.png"), C, 960, 540, 200, 200);  // วงแดงลดตามเวลา
        boss.timerRing.type = Image.Type.Filled;
        boss.timerRing.fillMethod = Image.FillMethod.Radial360;
        boss.timerRing.fillOrigin = (int)Image.Origin360.Top;
        boss.timerRing.fillClockwise = false;
        boss.timerText = Txt(timer.transform, "TimerText", "90", C, 960, 540, 190, 190, 80, new Color(1f, 0.8f, 0.1f), TextAlignmentOptions.Center, true);

        // ปุ่ม ? คำใบ้
        var hintBtn = Btn(hud, "HintButton", null, TR, 1757, 318, 130, 130, null, boss.ToggleHint);
        Txt(hintBtn.transform, "Q", "?", C, 960, 540, 130, 130, 130, new Color(1f, 0.82f, 0.1f), TextAlignmentOptions.Center, true);

        // กล่องคำใบ้ภาษาไทย (สีฟ้าอมเขียว)
        var bubble = Img(canvas.transform, "HintBubble", S(ArtGen + "Fx_RoundRect.png"), C, 1110, 370, 1000, 170, new Color(0.13f, 0.62f, 0.68f, 0.96f));
        boss.hintBubble = bubble.gameObject;
        boss.hintText = Txt(bubble.transform, "HintText", "คำใบ้", C, 960, 540, 940, 160, 36, Color.white, TextAlignmentOptions.Center, true);

        // กล่องพิมพ์คำตอบ
        boss.typingBox = Img(canvas.transform, "TypingBox", S(ArtUI + "Panel_InputBox.png"), BC, 1170, 930, 1120, 150).rectTransform;
        var slots = Rect(boss.typingBox, "Slots", C, 920, 540, 880, 130);    // ช่องตัวอักษรอยู่ขวาของสไลม์ (คำยาวสุด 8 ตัวก็ไม่ถูกบัง)
        var layout = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 22; layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        boss.slotContainer = slots;
        var slotRt = Rect(slots, "SlotTemplate", C, 960, 540, 78, 120);
        var slot = slotRt.gameObject.AddComponent<LetterSlotView>();
        slot.letter = Txt(slotRt, "Letter", "", C, 960, 530, 78, 100, 80, Color.black);
        slot.underline = Img(slotRt, "Underline", S(ArtGen + "Fx_White.png"), C, 960, 595, 72, 8);
        boss.slotTemplate = slot;
        boss.counterText = Txt(boss.typingBox, "Counter", "0 / 4", C, 1435, 520, 150, 70, 46, new Color(0.25f, 0.15f, 0.12f), TextAlignmentOptions.Right);

        // สไลม์ตกใจ: เหงื่อ + ?
        var react = slimeRoot.gameObject.AddComponent<BossSlimeReaction>();
        react.slimeVisual = slimeModel.transform;
        var nervous = Rect(canvas.transform, "SlimeNervousIcons", BL, 300, 660, 300, 260);
        Txt(nervous, "Question", "?", C, 1100, 450, 120, 140, 120, new Color(0.25f, 0.55f, 1f), TextAlignmentOptions.Center, true);
        for (int i = 0; i < 3; i++)
            Img(nervous, "Sweat_" + i, S(ArtGen + "Fx_Drop.png"), C, 860 + i * 55, 560 + (i % 2) * 40, 36, 52, new Color(0.45f, 0.8f, 1f));
        react.nervousIcons = nervous.gameObject;
        boss.slime = react;
        boss.quotaText = Txt(hud, "QuotaText", "ทายถูก 0 / 5", TC, 880, 258, 360, 44, 34, Color.white, TextAlignmentOptions.Center, true);
        var leafBtn = Btn(hud, "HintLeafButton", S(HC + "Buttons/empty_buttons/green.png"), TR, 1720, 450, 230, 80, null, boss.UseHintLeaf);
        Img(leafBtn.transform, "Leaf", S(ArtUI + "Icon_Leaf.png"), C, 880, 540, 52, 56);
        var leafKey = Txt(leafBtn.transform, "Label", "ใช้ใบ้ (Tab)", C, 995, 540, 150, 60, 26, Color.white, TextAlignmentOptions.Center, true);
        var leafTouch = Txt(leafBtn.transform, "LabelTouch", "ใช้ใบ้", C, 995, 540, 150, 60, 30, Color.white, TextAlignmentOptions.Center, true);
        boss.leafText = ui.leafText;

        var keyboard = BuildKeyboard(canvas.transform, boss);              // แป้น A-Z บนจอ (มือถือไม่มีคีย์บอร์ดจริง)
        var orient = canvas.GetComponent<OrientationLayout>();
        orient.touchOnly = new[] { keyboard, leafTouch.gameObject };
        orient.desktopOnly = new[] { leafKey.gameObject };
        orient.slime3D = holder;
        Portrait(canvas,                                                    // แนวตั้ง: บนลงล่าง = HUD > คำใบ้ > ภาพสัตว์ > สไลม์ > ช่องคำตอบ > แป้นพิมพ์
            Move(timer, 0, -380), Move(boss.quotaText, 0, -505), Move(hud.Find("ScorePanel"), -240, -545, 0.85f), // นาฬิกาใต้หลอดเลือด คะแนนใต้ปุ่มใบ้
            Move(bubble, 0, -705, 0f, TC), Move(boss.animalImage, 0, -1030, 1f, TC), Move(boss.typingBox, 0, 505), Move(nervous, 420, 1000));

        MakeOverlays(canvas.transform, ui);
        ui.SetProgress(1f);
        ui.SetHp(GameSession.MaxHp, GameSession.MaxHp);
        boss.BuildPreview();   // แสดงเงาหมี + ช่อง _ _ _ _ ใน Edit Mode
        nervous.gameObject.SetActive(true); // โชว์ใน Edit Mode (ตอนเล่นสคริปต์จะซ่อน/แสดงเอง)

        var guard = new GameObject("DebugGuard").AddComponent<DebugGuard>();
        guard.transform.SetParent(systems.transform);
        guard.player = slimeRoot;

        Save("BossScene");
    }

    static GameObject BuildKeyboard(Transform canvas, BossQuizManager boss) // แป้นพิมพ์ QWERTY 3 แถว + ปุ่มลบ (ปุ่มละ 118x115 กดด้วยนิ้วได้)
    {
        var root = Rect(canvas, "Keyboard", BC, 960, 865, 1300, 400);
        string[] rows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
        for (int r = 0; r < 3; r++)
        {
            int n = rows[r].Length + (r == 2 ? 2 : 0);                         // แถวล่างมีปุ่มลบกว้าง 2 ช่อง
            float x0 = 960 - (n * 128 - 10) * 0.5f + 59;
            for (int i = 0; i < rows[r].Length; i++)
            {
                char ch = rows[r][i];
                var b = Btn(root, "Key_" + ch, S(ArtGen + "Fx_RoundRect.png"), C, x0 + i * 128, 410 + r * 130, 118, 115, ch.ToString(), null, 54);
                b.GetComponent<Image>().color = new Color(1f, 0.96f, 0.88f);
                b.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.3f, 0.18f, 0.1f);
                UnityEventTools.AddIntPersistentListener(b.onClick, boss.TypeLetter, ch - 'A');
            }
        }
        var del = Btn(root, "Key_Delete", S(ArtGen + "Fx_RoundRect.png"), C, 960 - (9 * 128 - 10) * 0.5f + 59 + 7 * 128 + 64, 670, 246, 115, "ลบ", boss.Backspace, 46);
        del.GetComponent<Image>().color = new Color(1f, 0.6f, 0.45f);
        return root.gameObject;
    }

    static void SetBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SceneDir + "MainMenu.unity", true),
            new EditorBuildSettingsScene(SceneDir + "RunnerScene.unity", true),
            new EditorBuildSettingsScene(SceneDir + "BossScene.unity", true),
        };
    }
}
