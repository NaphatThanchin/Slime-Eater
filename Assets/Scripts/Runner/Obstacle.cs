using UnityEngine;

// สิ่งกีดขวาง: Low = หิน (กระโดด), High = ท่อนไม้สูง (สไลด์), Block = หินใหญ่ (หลบเลน), Falling = ต้นไม้ล้ม
public class Obstacle : ScrollingObject
{
    public enum Kind { Low, High, Block, Falling }
    public Kind kind = Kind.Low;
}
