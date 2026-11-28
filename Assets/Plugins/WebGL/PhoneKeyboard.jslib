// แป้นพิมพ์ของโทรศัพท์ (เว็บ): ช่องพิมพ์ล่องหน + แตะจอ = เปิดแป้น (เบราว์เซอร์ยอมเปิดแป้นเฉพาะตอนแตะจอ)
mergeInto(LibraryManager.library, {
  PhoneKb_Enable: function (on) {
    var k = window.slimeKb;
    if (!k) {
      var i = document.createElement('input');
      i.type = 'text'; i.autocomplete = 'off'; i.spellcheck = false;
      i.setAttribute('autocorrect', 'off'); i.setAttribute('autocapitalize', 'characters');
      i.style.cssText = 'position:fixed;top:0;left:0;width:1px;height:1px;opacity:0;border:0;padding:0;font-size:16px;'; // 16px กัน iPhone ซูมจอ
      document.body.appendChild(i);
      k = window.slimeKb = { input: i, on: false, buf: '', prev: '  ' };
      i.value = k.prev;
      i.addEventListener('input', function () { // เทียบข้อความเก่า/ใหม่ -> ตัวที่ลบ + ตัวที่เพิ่ม
        var v = i.value, p = 0;
        while (p < v.length && p < k.prev.length && v[p] === k.prev[p]) p++;
        for (var n = k.prev.length - p; n > 0; n--) k.buf += '\b';
        k.buf += v.substring(p).replace(/\s/g, '');
        if (v.length < 2) { v = '  '; i.value = v; }       // ช่องว่าง 2 ตัวกันชน -> กดลบตอนว่างก็ยังรู้
        k.prev = v;
      });
      i.setAttribute('enterkeyhint', 'go');                     // ปุ่ม Enter ของแป้นมือถือขึ้นว่า "ไป"
      ['keydown', 'keypress', 'keyup'].forEach(function (t) { i.addEventListener(t, function (e) {
        e.stopPropagation();                                     // ไม่ให้ Unity รับซ้ำ
        if (t === 'keydown' && (e.key === 'Enter' || e.keyCode === 13)) { e.preventDefault(); k.buf += '\n'; } // กด ไป/Enter = ยืนยัน
      }); });
      var tap = function () { if (k.on) { i.blur(); i.focus(); } };  // แตะจอ = เปิดแป้นใหม่ (กรณีผู้เล่นปิดแป้นไป)
      document.addEventListener('touchend', tap, true);
    }
    k.on = !!on;
    if (k.on) k.input.focus(); else k.input.blur();
  },

  PhoneKb_Read: function () { // ตัวอักษรที่พิมพ์มาตั้งแต่เฟรมก่อน ('\b' = ลบ)
    var k = window.slimeKb;
    if (!k || !k.buf) return null;
    var s = k.buf; k.buf = '';
    var n = lengthBytesUTF8(s) + 1, p = _malloc(n);
    stringToUTF8(s, p, n);
    return p;
  }
});
