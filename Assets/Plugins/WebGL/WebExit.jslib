// ปุ่ม EXIT บนเว็บ: เรียกฟังก์ชันในหน้า index.html (template SlimeFull) ให้ปิดเกมแล้วโชว์หน้าออกจากเกม
mergeInto(LibraryManager.library, {
  SlimeExit: function () {
    if (typeof window.slimeExit === 'function') setTimeout(window.slimeExit, 0); // รอให้ Unity จบเฟรมนี้ก่อนปิด
  }
});
