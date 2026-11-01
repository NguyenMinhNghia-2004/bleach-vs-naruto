# Bleach vs Naruto 3.7 - Unity & C# Online Server

## Cấu trúc dự án
- `BVN-Unity/`: Dự án game Unity (hỗ trợ Unity 6000.0.x / Unity 6). Chứa toàn bộ source code điều khiển, cơ chế chiến đấu đối kháng, lockstep sync và WebGL Native WebSocket Transport.
- `Server/`: Mã nguồn máy chủ Relay độc lập viết bằng C# .NET 8 (`BleachVsNarutoServer`). Hỗ trợ kết nối song song TCP (Client Desktop) và WebSocket (Client WebGL).
- `tools/`: Các công cụ hỗ trợ biên dịch nhanh, kiểm thử tự động và tunnel Cloudflare.
- `docs/`: Tài liệu kiến trúc và kế hoạch chuyển đổi.
- `start_server.bat`: File khởi động nhanh máy chủ Relay và Cloudflare Tunnel.

## Hướng dẫn cài đặt trên máy mới

### 1. Yêu cầu môi trường
- Git
- Unity Hub & Unity Editor phiên bản `6000.0.x` (khuyên dùng `6000.0.55f1` với WebGL Build Support)
- .NET 8.0 SDK (để biên dịch hoặc chạy Server)

### 2. Mở dự án Unity
1. Mở **Unity Hub** -> Chọn **Add** -> Trỏ tới thư mục `BVN-Unity`.
2. Chọn phiên bản Unity `6000.0.55f1` (hoặc Unity 6 tương đương).
3. Đợi Unity tự động tái tạo thư mục `Library/` và nạp Assets.
4. Mở Scene chính tại `Assets/Scenes/Main.unity` (hoặc `SampleScene.unity`).

### 3. Khởi động Máy chủ Relay
Chạy file `start_server.bat` trên Windows, hoặc chạy lệnh:
```bash
dotnet run --project Server/BleachVsNarutoServer.csproj 7777
```
