# Slime Eater

เกมวิ่ง 3D ในป่า 3 เลน + Bonus time ทายชื่อสัตว์ภาษาอังกฤษจากเงา เล่นได้ทั้งคอมและมือถือ (บนเว็บ)
โปรเจกต์ Final Project รายวิชา MI226

## ▶ เล่นบนเว็บเลย (คอม + มือถือ)
**https://naphat-thanchin.itch.io/slimeeater**
(มือถือถือแนวตั้ง แตะ Run game แล้วเล่นได้ทันที ไม่ต้องติดตั้ง)

- **Unity:** 6000.3.24f1 (Unity 6.3) + URP

## โหลดมาเปิดใน Unity
1. หน้า GitHub นี้ เลือก branch **Final-Game1** (ปุ่มเลือก branch มุมซ้ายบนของรายการไฟล์) → ปุ่มเขียว **Code → Download ZIP** แล้วแตกไฟล์
   หรือใช้ Git: `git clone -b Final-Game1 https://github.com/NaphatThanchin/Slime-Eater.git`
2. ติดตั้ง Unity **6000.3.24f1** ผ่าน Unity Hub (ถ้าจะ Build เว็บให้ติ๊ก WebGL Build Support ด้วย)
3. Unity Hub → **Add → Add project from disk** → เลือกโฟลเดอร์ที่แตกไว้ (ครั้งแรกรอ Unity สร้าง Library + โหลดแพ็กเกจ 5–15 นาที)
4. เปิด `Assets/Scenes/MainMenu.unity` แล้วกด **Play** (เกมเริ่มที่เมนูเสมอ)
4. ถ้าฉากหาย/เสีย: เมนู **Tools > Slime Eater > Build All Scenes** สร้างฉากทั้ง 3 ใหม่จาก `Assets/Editor/GameSceneBuilder.cs`

## วิธีเล่น
| การกระทำ | คอม | มือถือ |
|---|---|---|
| เปลี่ยนเลน | A/D หรือ ←/→ | ปัดซ้าย/ขวา |
| กระโดด | Space / W / ↑ | ปัดขึ้น |
| สไลด์ | S / ↓ / Shift | ปัดลง |
| พิมพ์คำตอบ (Bonus time) | พิมพ์ A–Z, Backspace ลบ | แป้นพิมพ์ของโทรศัพท์ขึ้นเอง (หรือแตะจอ 1 ครั้ง) |
| ใช้ใบไม้ใบ้ | Tab / 1 | ปุ่ม "ใช้ใบ้" |

1. วิ่งหลบหิน ต้นไม้ล้ม ท่อนไม้ เก็บเหรียญ/ไอเทม เลือดลดเรื่อยๆ ตามระยะ
2. วิ่งไม่รู้จบ (ฉากเปลี่ยนทุก 1,000 m) เก็บตัวอักษรชื่อสัตว์ที่มุมขวาบน (เกิดทุก 400 m) ให้ครบ = เข้า **Bonus time** ทันที แล้วกลับมาวิ่งต่อ: ดูเงาสัตว์แล้วพิมพ์ชื่อภาษาอังกฤษ (30 วิทุกรอบ ไม่เสียเลือด ผิด = -2 วิ ได้เหรียญ/แต้มตามจำนวนข้อที่ถูก ครบทุกข้อ ×1.5)
3. จบ Bonus time = กลับมาวิ่งต่อ (ฉากกลางวัน → เย็น → กลางคืน ทุก 1,000 m) ความเร็ว +5% ทุก 10 วิ ถึง 2 นาที เลือดลดตามเวลา ไล่ชนกินบอทที่วิ่งนำ (เลือด +20%) เลือดหมด = ใช้ยาฟื้นเล่นต่อ หรือจบรอบ เหรียญเข้ากระเป๋า
4. ไล่ชนกินสไลม์บอทที่วิ่งนำ, ซื้อตัวละคร/อัปเกรด, ทำภารกิจแล้วกดรับรางวัลที่เมนู

## โครงสร้างโฟลเดอร์
| โฟลเดอร์ | เนื้อหา |
|---|---|
| `Assets/Scripts/Core` | GameManager, GameSession, AudioManager |
| `Assets/Scripts/Menu` | เมนูหลัก, ร้านค้า, หน้าภารกิจ |
| `Assets/Scripts/Runner/Player` | ผู้เล่น, กล้อง, สอนเล่น |
| `Assets/Scripts/Runner/Track` | วางชุดอุปสรรค (Chunks), พื้นป่า, ความเร็ว, ท้องฟ้า |
| `Assets/Scripts/Runner/Obstacles` | หิน/ท่อนไม้/ต้นไม้ล้ม/ท่อนไม้หล่น/ขอนยาว |
| `Assets/Scripts/Runner/Items` | เหรียญ/หัวใจ/ตัวอักษร/ไอเทมพลังพิเศษ |
| `Assets/Scripts/Runner/Rival` | บอทคู่แข่งวิ่งนำ |
| `Assets/Scripts/BonusTime` | ทายเงาสัตว์ (BonusTimeScene), สิ่งรบกวน, ช่องตัวอักษร |
| `Assets/Scripts/Input` | ปัดจอ, แป้นพิมพ์มือถือ (ชื่อ + Bonus time) |
| `Assets/Scripts/Character` | ข้อมูล/ความสามารถ/ท่าทางตัวละคร, ป้ายชื่อ |
| `Assets/Scripts/Save` | บันทึกเกม, ภารกิจ |
| `Assets/Scripts/UI` | HUD, ตัวหนังสือลอย, จัดจอแนวตั้ง, แก้สระไทย |
| `Assets/Scripts/Dev` | AutoPilot ทดสอบอัตโนมัติ, DebugGuard (เฉพาะทดสอบ) |
| `Assets/Art` | ภาพ UI/สัตว์, โมเดลตัวละคร (`Models`), พื้นผิวที่สร้างอัตโนมัติ (`Generated`) |
| `Assets/ThirdParty` | Hyper Casual UI + Stylized Nature MegaKit, TextMesh Pro |
| `Assets/Editor` | ตัวสร้างฉากอัตโนมัติ + เครื่องมือ Game View |
| `Assets/Plugins/WebGL` | ปลั๊กอิน JavaScript: แป้นพิมพ์มือถือ, ปุ่ม EXIT บนเว็บ |
| `Assets/WebGLTemplates/SlimeFull` | หน้าเว็บของเกม (ภาพ 1920×1080 / 1080×1920, ปุ่มเต็มจอ, หน้าออกจากเกม) |

## Build ขึ้น itch.io
1. File → Build Profiles → Web (WebGL) → Build ลงโฟลเดอร์ `Builds/WebGL` (ถูก .gitignore ไว้)
2. zip ไฟล์ข้างใน (ให้ `index.html` อยู่ชั้นนอกสุดของ zip)
3. itch.io: Kind of project = **HTML**, ติ๊ก *This file will be played in the browser*, Viewport 1280×720, ติ๊ก **Mobile friendly**, Orientation = **Portrait**, ติ๊ก Fullscreen button
