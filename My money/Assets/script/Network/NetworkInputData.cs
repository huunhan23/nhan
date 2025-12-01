using Fusion;
using UnityEngine;

// 1. Struct này "đóng gói" tất cả input bạn cần gửi qua mạng
public struct NetworkInputData : INetworkInput
{
    public Vector2 moveDirection;
    public NetworkButtons buttons; // Fusion sẽ tự xử lý các nút bấm
    public Vector3 aimPoint;
    public float rotationY;      // THÊM: Điểm nhắm cho skill
}

// 2. Định nghĩa các nút bấm
public enum EInputButtons
{
    JUMP,     // Nhảy (Space)
    SPRINT,   // Chạy nhanh (Shift)
    WALK,     // Đi bộ (Ctrl) <-- THÊM
    
    ATTACK,   // Lệnh R (Tấn công)
    SKILL1,   // Lệnh T
    SKILL2,   // Lệnh Y
    MOUNT,    // Lệnh F (Cưỡi)
    CALL_PET, // Lệnh E
    SUMMON    // Lệnh H
}