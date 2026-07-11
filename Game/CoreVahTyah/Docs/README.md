# CoreVahTyah — Tài liệu framework

> **Đọc file này đầu tiên.** Đây là core framework Unity (namespace `VahTyah`) cho game Super Casual: Hybrid Puzzle của Percas Studio. Tài liệu viết cho AI/dev nắm nhanh hệ thống trước khi sửa hoặc mở rộng code.

## Mental model 60 giây

CoreVahTyah là kiến trúc **module hoá + event-driven**. Ba ý cốt lõi:

1. **Mỗi tính năng = 1 `Module`** (là `ScriptableObject`). Bật/tắt tính năng bằng cách thêm/bớt asset trong `ModuleConfig`, **không đụng code**.
2. **Module không biết nhau.** Chúng giao tiếp gián tiếp qua `EventBus` (event là `struct : IEvent`). Không có reference trực tiếp giữa các module.
3. **Phụ thuộc chạy qua 2 kênh:**
   - `EventBus` — cho lệnh/thông báo/query cross-module (loose coupling, **mặc định**).
   - `Services` (Service Locator) — cho service gọi trực tiếp, tần suất cao (`SaveService`, `PoolService`, `UIGroupService`, và feedback `SoundService`/`MusicService`/`HapticService`, `SettingsService`).
   - Quy tắc chọn kênh: xem [CONVENTIONS.md](CONVENTIONS.md) → "Command → Service hay Event?".

## Luồng boot (rất quan trọng)

```
Scene 0 (boot scene)
  ├─ LoadingScreen (giữ root qua DontDestroyOnLoad)
  │     Phase 1: bar tự chạy 0→0.85 theo thời gian (_introDuration) → phát BootIntroReady
  │     Phase 2: giữ bar ở 0.85 khi load scene, rồi fill nốt 100% khi BootCompleted
  └─ Bootstrap (Singleton, DontDestroyOnLoad)
        └─ BootAsync():
             1. await UniTask.Yield()                       // để listener kịp Subscribe
             2. Với mỗi Module trong Config.Modules (THỨ TỰ quan trọng):
                  await module.InitializeAsync(transform); module.Subscribe()
             3. ĐỢI BootIntroReady                          // chờ bar chạy xong intro (0.85)
             4. await Publish(LoadEntryScene)               // ModuleSceneLoader load game scene
             5. Publish BootCompleted                       → LoadingScreen fade out
```

**Hệ quả THỨ TỰ MODULE trong `ModuleConfig` quan trọng.** Ví dụ `ModulePool` phải đứng trước `ModuleParticle` (particle cần Pool để prewarm); mọi module dùng save phải đứng sau `ModuleSave`.

## Bản đồ tài liệu

| File | Nội dung |
|------|----------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Nền tảng: Bootstrap, Module, EventBus, Services, Save, Pool, UIGroup — cơ chế và lý do thiết kế |
| [EVENTS.md](EVENTS.md) | **Event catalog đầy đủ** — API surface để giao tiếp giữa các module (command / notify / query) |
| [MODULES.md](MODULES.md) | Reference từng module: trách nhiệm, config knobs, save key, event xử lý |
| [CONVENTIONS.md](CONVENTIONS.md) | Quy ước code + **cách thêm module mới** + các cạm bẫy (gotchas) đã gặp |

## Vị trí code

```
Assets/Game/CoreVahTyah/
├─ Scripts/
│  ├─ Bootstrap.cs, Module.cs, ModuleConfig.cs, Services.cs, CoreModuleAttribute.cs
│  ├─ EventBus/        (EventBus, IEvent, BusCleaner)
│  ├─ Common/          (Singleton, Debug, SafeArea, CanvasScaler, Effects, SceneRedirect)
│  ├─ Loading/         (LoadingScreen, BootEvents)
│  └─ Modules/         (mỗi tính năng 1 folder: Save, Level, Heart, Pool, UIGroup, ...)
├─ Config/             (các asset .asset: ModuleConfig + Module_*.asset)
└─ Docs/               (tài liệu này)
```

## Dependency ngoài

- **UniTask** (`Cysharp.Threading.Tasks`) — async bắt buộc, dùng thay `Task`/coroutine cho luồng chính.
- **TextMeshPro** — dùng ở LoadingScreen và các view.
