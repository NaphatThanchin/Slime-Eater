using UnityEngine;

// สิ่งกีดขวาง: Low = หิน (กระโดด), High = ท่อนไม้สูง (สไลด์), Block = หินใหญ่ (หลบเลน), Falling = ต้นไม้ล้ม, Platform = ขอนยาวขึ้นได้
public class Obstacle : ScrollingObject
{
    public enum Kind { Low, High, Block, Falling, Platform }
    public Kind kind = Kind.Low;
}
