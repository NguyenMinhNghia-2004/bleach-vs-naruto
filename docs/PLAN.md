# Kế hoạch chuyển Bleach vs Naruto sang Unity 6

## Mục tiêu
Dựng lại BVN trên **Unity 6 (6000.3.14f1)**, dùng **toàn bộ tài nguyên từ source gốc** (XFL), chuyển **tự động** bằng công cụ, rồi tự kiểm tra từng giai đoạn để đạt bản hoàn thiện.

## Nguồn
| Thư mục | Nội dung | Vai trò |
|---|---|---|
| `BleachVsNaruto/` (develop, 3.7) | Code AS3 engine + `BleachVsNaruto_FlashSrc` (UI, effect, quái XFL) | **Tham chiếu logic** để port sang C#, cùng UI và hiệu ứng |
| `OpenBleachVsNaruto/` (3.4) | XFL của 38 nhân vật, 37 trợ thủ, 15 map | **Nguồn asset chính** cho nhân vật và map |
| `TagAssets/` (3.7, 12/2024) | SWF đã biên dịch, face, nhạc, config | Nhạc, ảnh mặt, config, nhân vật chỉ có bản SWF |

## Kiến trúc
```
XFL (DOMDocument + LIBRARY/*.xml + bin/*.dat)
   │  tools/xfl2unity (Python)
   ▼
StreamingAssets/bvn/<loại>/<id>/
   library.json   – symbol, timeline, layer, frame, element, matrix, color, label
   scripts.json   – frame script AS3 → AST
   atlas_N.png    – bitmap + shape đã raster, đóng gói atlas
   sounds/*.wav|mp3
   │  Unity runtime (C#)
   ▼
Flash runtime (display list: MovieClip, gotoAndPlay, label, children theo tên)
   + Script VM (thông dịch AST, host object $mc_ctrler/$effect_ctrler/$fighter_ctrler/$attacker_ctrler)
   + Game core (port từ CORE_KernelLogic: Fighter, Hit/Hurt, Collision, Camera, Round, AI, UI)
```
Lý do giữ mô hình display list thay vì "nướng" ra sprite sheet: engine gốc dựa vào instance con có tên (`bdmn`, `*atm`, `AImain`, `*mc` cho attacker), nhãn frame, và MC con tự chạy. Giữ nguyên mô hình thì logic port 1:1 và hitbox chính xác.

### Quy ước hệ toạ độ
- 1 pixel Flash = 0,01 unit Unity. Trục y Flash hướng xuống, khi render thì lật lại; logic game giữ toạ độ Flash để port công thức nguyên vẹn.
- Logic chạy 60 FPS (`FixedUpdate` cố định, tất định), animation 30 FPS, giống `GameConfig` gốc.

## Giai đoạn và cổng kiểm tra (tự review)
| # | Giai đoạn | Sản phẩm | Cổng kiểm tra |
|---|---|---|---|
| 1 | Bộ chuyển đổi asset | Giải mã `.dat` (ARGB/palette/JPEG), raster shape, atlas, export timeline, âm thanh | Render preview PNG bằng Python cho từng action; 0 lỗi parse trên toàn bộ 38+37+15 |
| 2 | Script → AST | Parser AS3-subset | 100% trong ~2.000 script duy nhất parse được, có báo cáo câu không hỗ trợ |
| 3 | Unity: Flash runtime + Viewer | Project Unity 6, loader, MovieClip runtime, scene xem nhân vật | Unity batchmode compile 0 lỗi; screenshot viewer khớp preview |
| 4 | Script VM + Fighter core | Di chuyển, nhảy, dash, đánh, combo, đỡ, bị đánh, ngã, năng lượng, bisha | Test tự động (EditMode) cho VM + mô phỏng trận headless |
| 5 | Trận đấu đầy đủ | Round/KO/thời gian, HUD, camera, map, trợ thủ, attacker/đạn, hiệu ứng, âm thanh | Bot vs bot chạy N trận không lỗi, không exception |
| 6 | AI + menu | AI port, Title/Chọn nhân vật/Chọn map/Setting, 1P vs CPU, 2P | Chơi thử end-to-end bằng batchmode + screenshot |
| 7 | Hoàn thiện | Build Windows `.exe`, review code, tối ưu, tài liệu | Build thành công, chạy được, REVIEW.md |
| 8 (mở rộng) | Chế độ vô song, LAN/rollback, nhân vật chỉ có SWF 3.7 | — | — |

## Rủi ro đã biết
- Asset nhân vật là bản 3.4, engine là 3.7, nên port giữ tương thích API timeline mà 3.4 dùng (đã khảo sát: `idle`, `endAct`, `move*`, `damping*`, `set*` hành động, `addAttacker`, `fire`, `justHitToPlay`…).
- Filter Flash (Glow/Blur/AdjustColor) và blend mode: hỗ trợ blend chính (add/screen/lighten/multiply), filter làm gần đúng.
- Motion tween (722 frame): nội suy ma trận + màu tuyến tính; custom ease làm gần đúng.
- Bản quyền nhân vật thuộc Shueisha, code GPL-3.0. Dự án chỉ dùng phi thương mại.
