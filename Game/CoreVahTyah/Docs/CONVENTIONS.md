# Conventions & Gotchas

Quy ước code khi làm việc với CoreVahTyah + cách mở rộng + cạm bẫy đã gặp. **Đọc trước khi thêm/sửa module.**

---

## Quy ước cốt lõi

1. **Module không reference nhau.** Muốn nói chuyện với module khác → `EventBus`. Muốn dùng service dùng chung → `Services.Get<T>()`. Không bao giờ `FindObjectOfType` hay static ref chéo module.
2. **Event là `struct : IEvent`.** Không dùng class (tránh alloc). Đặt tên theo loại: `XxxRequest`/`XxxSet`/`XxxAdd` (command), `XxxChanged`/`XxxCompleted` (notify), `XxxGet`/`XxxIs...` (query, có `Reply`).
3. **UniTask, không `Task`/coroutine** cho luồng async chính. Fire-and-forget bằng `.Forget()`.
4. **Async work → threadpool rồi về main thread**: `await UniTask.SwitchToThreadPool()` ... `await UniTask.SwitchToMainThread()` (xem `LocalSaveProvider`).
5. **Save**: mỗi module 1 `const string SaveKey`; load qua `SaveService.Load<T>` trong `InitializeAsync`; `Set(key, data)` sau khi đổi (đánh dấu dirty, SaveRunner tự ghi). Save-data là class `[Serializable]` (JsonUtility — **không hỗ trợ Dictionary/property, chỉ field public + List**).
6. **Dùng `Debug.Log/Warning/Error`** (shim trong `Common/Debug.cs`) — tự strip khỏi production. Chỉ `Debug.LogException` sống ở release.
7. **Log prefix** `[TênModule]` cho dễ lọc (`[Save]`, `[Pool]`, `[Heart]`...).

## Nghe event: MonoBehaviour vs Module

| Nơi | Cách | Cleanup |
|-----|------|---------|
| **MonoBehaviour** (view, controller trong scene) | `this.On<T>(handler)` / `this.OnAsync<T>` | Tự động khi `OnDestroy` (BusCleaner) |
| **Module** (ScriptableObject, sống suốt game) | `EventBus.On<T>(handler)` trong `Subscribe()` | Không cần (không destroy) |

> **Gotcha**: dùng `EventBus.On` (thay vì `this.On`) trong MonoBehaviour → leak listener + gọi handler trên object đã destroy. Luôn `this.On` trong MonoBehaviour.

## Priority — khi thứ tự handler quan trọng

Số nhỏ chạy trước. Quy ước đang dùng:
- `-1000`: Transition che màn (chạy sớm nhất khi load scene).
- `-100`: WinScreen/FailScreen, view phản ứng level.
- `-50`: Transition mở màn sau load.
- `-10`: module "chủ" xử lý logic gốc của event (Heart/Level/Item).
- `0` (mặc định): phần lớn listener.

Nếu 2 handler phải chạy đúng thứ tự → đặt priority rõ ràng, đừng dựa vào thứ tự đăng ký.

## Query pattern (đọc giá trị đồng bộ)

```csharp
int level = 0;
EventBus.Publish(new LevelGet { Reply = v => level = v }).Forget();
// dùng level ngay — handler chạy sync nên đã set xong
```
Chỉ đúng khi channel **không có handler async** (sync fast-path). Với module core hiện tại, các query đều là handler sync → an toàn.

## Command → Service hay Event?

Mặc định giao tiếp cross-module đi qua **EventBus** (loose coupling): Level/Heart/Item/Feature/Screen dùng command/notify/query dạng event. Nhưng có **ngoại lệ**: một số command đi qua **Service (gọi trực tiếp)**.

Chọn **Service** (POCO register vào `Services` + shortcut tĩnh) khi CẢ 3 đúng:
1. Tần suất cao / gọi mỗi tap–mỗi frame (feedback).
2. Đúng **1 nơi xử lý**, không cần fan-out.
3. Cần giá trị trả về / await tự nhiên, hoặc muốn stack trace liền mạch.

→ Áp dụng cho **Sound / Music / Haptic / Particle**: `Sound.Play`, `Music.Play/Stop/SetVolume`, `Haptic.Play/PlaySequence`, `Particles.Play` gọi thẳng `Services.Get<XService>()`. `ModuleX` chỉ là factory register service.

Chọn **Event** khi: 0..N listener quan tâm, "đã xảy ra" (notify), cần decouple ai phản ứng, hoặc low-frequency. **Đừng** giả lập query bằng event `XGet{Reply=Action<>}` cho các service kiểu này — đọc property trên service.

> **Notify vẫn là Event.** Service phát trạng thái đổi qua EventBus để nơi khác nghe. Ví dụ đổi setting → `SettingsChanged`; `MusicService` (nhạc phát liên tục) nghe event này để mute/unmute ngay, còn SFX/Haptic (one-shot) chỉ gate lúc Play.

## Preference vs Gameplay state — lưu ở đâu

Ranh giới theo **loại dữ liệu**, không theo module:

| Loại | Ví dụ | Lưu ở |
|------|-------|-------|
| **User preference** (Settings screen chỉnh) | mute BGM/SFX, haptic on/off, music volume | **chung** → `SettingsService` (SSOT, key `settings`); các service audio/haptic đọc từ đây |
| **Gameplay state** (tiến trình chơi) | hearts, level, inventory, tutorial done | **riêng** mỗi module (`hearts`/`level`/...) |

Hệ quả: `HapticService/SoundService/MusicService` **không giữ save riêng** — cờ bật/tắt đọc từ `SettingsService`. Vì cache `SettingsService` lúc init, **`ModuleSettingsScreen` phải boot TRƯỚC** Sound/Haptic/Music (và cả 3 sau `ModuleSave`).

---

## Cách thêm 1 Module mới

Ví dụ thêm `ModuleDailyReward`:

1. **Folder** `Scripts/Modules/DailyReward/`.
2. **Events** `DailyRewardEvents.cs`:
   ```csharp
   public struct DailyRewardClaim : IEvent { public Action<bool> Reply; }
   public struct DailyRewardChanged : IEvent { }
   ```
3. **Save data** (nếu cần) `DailyRewardSaveData.cs` — class `[Serializable]`, field public.
4. **Module** `ModuleDailyReward.cs`:
   ```csharp
   [CreateAssetMenu(menuName = "VahTyah/Modules/DailyReward", fileName = "Module_DailyReward")]
   [ModuleRequires(typeof(ModuleSave))]   // cần ModuleSave init trước → editor tự sắp, không sắp tay
   public sealed class ModuleDailyReward : Module
   {
       [SerializeField] private int _rewardCoin = 100;
       private const string SaveKey = "daily_reward";
       private DailyRewardSaveData _save;

       public override UniTask InitializeAsync(Transform holder)
       {
           _save = Services.Get<SaveService>().Load<DailyRewardSaveData>(SaveKey);
           // spawn runner nếu cần tick: new GameObject parent = holder
           return UniTask.CompletedTask;
       }

       public override void Subscribe()
       {
           EventBus.On<DailyRewardClaim>(OnClaim);
       }

       private void OnClaim(DailyRewardClaim e) { /* ... */ Persist(); e.Reply?.Invoke(true); }
       private void Persist() => Services.Get<SaveService>().Set(SaveKey, _save);
   }
   ```
5. **Add vào config**: chọn asset `ModuleConfig` → Inspector → **Add Module ▾** → `Modules/DailyReward`. Editor tạo module làm **sub-asset nhúng trong `ModuleConfig.asset`** (không sinh file `.asset` rời), tự thêm vào mảng và **auto-sort** theo `[ModuleRequires]`. Không kéo thả, không sắp tay.
6. **Document**: thêm event vào [EVENTS.md](EVENTS.md), module vào [MODULES.md](MODULES.md).

### Thứ tự boot = data, không phải thao tác tay
Ràng buộc thứ tự khai báo bằng `[ModuleRequires(typeof(...))]` trên chính class module; `ModuleConfigEditor` topo-sort ổn định theo đồ thị đó, nút **Doctor** (hiện ngay trong Inspector) cảnh báo nếu order vi phạm / thiếu `[CoreModule]` / trùng type / null. Các ràng buộc hiện đã encode:
- `ModuleSettingsScreen`, `ModuleHeart`, `ModuleLevel`, `ModuleItem`, `ModuleTutorial`, `ModuleConsent` → `[ModuleRequires(ModuleSave)]`.
- `ModuleSound`/`ModuleMusic`/`ModuleHaptic` → `[ModuleRequires(ModuleSettingsScreen)]` (⇒ transitively sau `ModuleSave`).
- `ModuleParticle` → `[ModuleRequires(ModulePool)]`.
- `ModuleSave` gắn `[CoreModule]` (bắt buộc, ẩn nút Remove; Doctor tự offer add nếu thiếu).

> **`[ModuleRequires]` vs thứ tự mảng**: mảng vẫn là nguồn chân lý runtime (Bootstrap lặp `Config.Modules` tuần tự). `[ModuleRequires]` chỉ điều khiển editor sắp mảng cho đúng + kiểm tra. Module không ràng buộc giữ nguyên thứ tự tương đối hiện có (topo-sort ổn định) — vd thứ tự nội bộ nhóm Feedback do bạn kéo `▲`/`▼`.

---

## Gotchas (cạm bẫy thực tế)

- **Thứ tự module = thứ tự init.** Sai thứ tự → `Services.Get<T>()` trả null + log error. Đây là lỗi hay gặp nhất.
- **JsonUtility giới hạn**: save-data không dùng được `Dictionary`, property, hay type đa hình. Dùng field public + `List` + parallel arrays. (`ItemSaveData` tự cài cấu trúc key-value bằng list.)
- **Pool `ReturnAll` ở `SceneUnloading`, KHÔNG `SceneLoaded`** — nếu dọn sau khi scene mới load thì sẽ despawn nhầm object scene mới vừa spawn lúc init.
- **`ScreenOnStart` ở `Start()` không `Awake()`** — để mọi `UIGroup` trong scene kịp `Awake`+register vào service trước khi đổi màn.
- **UIGroup OR semantics**: object hiện nếu BẤT KỲ group nào của nó shown. Muốn đúng 1 màn → `ShowExclusive`/`ScreenRouter.GoTo`.
- **Heart chống chỉnh giờ**: đừng thay `Stopwatch.GetTimestamp()` bằng `DateTime.Now` — timestamp monotonic là điểm mấu chốt chống cheat. `DateTime` chỉ dùng làm neo recalibrate.
- **Module là ScriptableObject → asset chia sẻ state.** State runtime giữ trong field của asset; chỉ có 1 instance mỗi module nên OK, nhưng **đừng để giá trị runtime "dính" vào asset trong Editor** (state nên load từ save, không hard-code trên asset). `ModuleLevel.Current` là `static` để tránh vấn đề này khi truy cập chéo.
- **Init lỗi không sập boot** (try/catch mỗi module) — nhưng module đó không hoạt động. Kiểm tra Console khi 1 tính năng "im lặng không chạy".
- **`DontDestroyOnLoad` chỉ chạy trên ROOT object.** Gọi trên object con (vd LoadingScreen nằm dưới Canvas) → Unity warning + no-op, object bị destroy khi đổi scene. Dùng `DontDestroyOnLoad(transform.root.gameObject)` và destroy lại đúng root đó.
- **Animation theo thời gian lúc boot phải cap `deltaTime`.** Frame đầu (load scene 0 + init module dồn 1 frame) làm `Time.unscaledDeltaTime` phình to → `MoveTowards`/lerp nhảy vọt. LoadingScreen cap dt ở `1/30s` để bar chạy mượt từ 0. Áp dụng cho mọi UI chạy theo thời gian trong lúc boot/đổi scene.
- **Boot nhường 1 frame trước khi phát event.** `BootAsync` bắt đầu bằng `await UniTask.Yield()` để listener trong scene boot (LoadingScreen) kịp `Subscribe` trước khi boot logic phát event. Đăng ký listener boot ở `Awake`, không phải `Start`.
- **`Bootstrap.Unsubscribe()` chưa được gọi** — nếu cần reset toàn cục (vd restart game từ đầu), dùng `EventBus.Reset()` + `Services.Reset()` thủ công.
