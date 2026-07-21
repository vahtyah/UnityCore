# Kiến trúc CoreVahTyah

Chi tiết cơ chế của các thành phần nền tảng. Xem [README.md](README.md) cho mental model trước.

---

## 1. Bootstrap — điểm khởi động duy nhất

`Bootstrap.cs` — `Singleton<Bootstrap>`, đặt trong boot scene (scene 0), `DontDestroyOnLoad`.

- Giữ 1 ref `ModuleConfig Config` (kéo asset vào Inspector).
- `OnInitialize()` → `BootAsync()`:
  1. `await UniTask.Yield()` — nhường 1 frame để listener kịp Subscribe.
  2. `InitModules()`: lặp `Config.Modules`, mỗi module `await InitializeAsync(transform)` rồi `Subscribe()`. **Có try/catch từng module** — 1 module lỗi không làm sập cả boot, chỉ log error.
  3. **Đợi `BootIntroReady`** (LoadingScreen báo bar chạy xong intro) qua `EventBus.WaitFor<BootIntroReady>()`.
  4. `await Publish(LoadEntryScene)` — chờ scene game load xong.
  5. Publish `BootCompleted`.
- Bootstrap **không phát tiến độ** — thanh loading do LoadingScreen tự lo hoàn toàn (intro theo thời gian, fill nốt khi `BootCompleted`).
- Bootstrap **forward vòng đời app vào EventBus**: `OnApplicationPause` → `AppPaused`/`AppResumed`, `OnApplicationQuit` → `AppQuitting` (xem `AppEvents.cs`). Không listener → no-op (fast-path). Dùng bởi `ModuleNotifications` (re-engagement) và mọi module cần biết app rời/về.
- `transform` của Bootstrap được truyền vào `InitializeAsync` làm **holder** — module gắn runner GameObject vào đây để sống xuyên scene.

## 2. Module — đơn vị tính năng

`Module.cs` — `abstract ScriptableObject`, 3 hook virtual:

```csharp
UniTask InitializeAsync(Transform holder);  // tạo service, load save, spawn runner GameObject
void    Subscribe();                         // đăng ký EventBus handler
void    Unsubscribe();                        // (hiện chưa được Bootstrap gọi)
```

- Mỗi module cụ thể có `[CreateAssetMenu(menuName = "VahTyah/Modules/...")]` → tạo asset trong `Config/`.
- Config của module = các field `[SerializeField]` trên chính asset đó (không cần class config riêng).
- `CoreModuleAttribute` (`[CoreModule]`): đánh dấu module bắt buộc — `ModuleConfigEditor` ẩn nút Remove và Doctor offer-add nếu thiếu (hiện gắn trên `ModuleSave`).
- `ModuleRequiresAttribute` (`[ModuleRequires(typeof(...))]`): khai báo module cần init trước; editor topo-sort mảng `Modules` theo đó (xem CONVENTIONS.md).
- **State không lưu trong file cs**; module load state từ `SaveService` lúc `InitializeAsync` và giữ ref tới save-data object.

`ModuleConfig.cs` — `ScriptableObject` chứa `Module[] Modules` + cờ `DebugLogs`. Đây là "bảng lắp ráp" toàn game.

## 3. EventBus — xương sống giao tiếp

`EventBus.cs` — static class, type-safe, tối ưu GC.

### API

```csharp
object On<T>(Action<T> handler, int priority = 0)               // handler đồng bộ
object OnAsync<T>(Func<T,UniTask> handler, int prio=0, bool waitFor=true)  // handler async
void   Off<T>(object tag)                                        // huỷ (tag = giá trị trả về của On)
UniTask Publish<T>(in T evt)                                     // phát event
UniTask<T> WaitFor<T>()                                          // await 1 lần event tiếp theo
bool   HasListeners<T>()
void   Reset()                                                    // clear toàn bộ (dùng khi restart)
```

### Cơ chế & đặc điểm

- Event **bắt buộc là `struct : IEvent`** → không box, không alloc khi publish.
- Mỗi loại event có 1 channel riêng qua generic `ChannelOf<T>` (không lookup dictionary theo type lúc publish).
- **Priority**: handler được insert theo priority tăng dần (số nhỏ chạy trước). Dùng để ép thứ tự xử lý giữa các module (vd `-1000`, `-100`, `-10`).
- **Fast-path sync**: nếu channel không có handler async nào → dispatch đồng bộ, trả `UniTask.CompletedTask`, zero alloc.
- **`waitFor`**: với handler async, `waitFor=true` (mặc định) → `Publish` chờ handler xong; `false` → fire-and-forget (`.Forget`).
- **Snapshot pooling**: lúc publish, danh sách handler được copy vào list mượn từ pool rồi trả lại → an toàn khi handler tự add/remove listener trong lúc dispatch, không GC.
- Exception trong 1 handler được catch + log, không chặn các handler khác.

### 3 loại event (quy ước — xem EVENTS.md)

| Loại | Hình dạng | Ví dụ |
|------|-----------|-------|
| **Command** | `struct { field vào }` | `HeartUse{Value}`, `ScreenRequest{Screen}` |
| **Notify** | `struct {}` hoặc field kết quả | `HeartChanged`, `LevelChanged{Level}` |
| **Query (request/reply)** | `struct { Action<T> Reply }` | `HeartGet{Reply}`, `LevelGet{Reply}` |

**Query pattern**: publisher tạo event với callback `Reply`, module xử lý gọi `e.Reply?.Invoke(value)`. Vì dispatch đồng bộ nên đọc được giá trị ngay sau `Publish`:

```csharp
int level = 0;
EventBus.Publish(new LevelGet { Reply = v => level = v }).Forget();
// level đã có giá trị ở đây (handler chạy sync)
```

### Auto-cleanup cho MonoBehaviour

`EventBusExtensions` cung cấp `this.On<T>(...)` / `this.OnAsync<T>(...)` cho MonoBehaviour. Nó tự gắn component `BusCleaner` (ẩn) — khi GameObject `OnDestroy`, mọi listener tự `Off`. **Luôn dùng `this.On` trong MonoBehaviour** thay vì `EventBus.On` để tránh leak listener.

> Module (ScriptableObject) dùng thẳng `EventBus.On` trong `Subscribe()` vì nó sống suốt đời game (không destroy).

## 4. Services — Service Locator

`Services.cs` — static dictionary `Type → instance`.

```csharp
Services.Register<T>(instance);   // overwrite + warn nếu trùng
Services.Get<T>();                 // null + log error nếu chưa đăng ký
Services.TryGet<T>(out T);
Services.Has<T>();
Services.Remove<T>(); / Reset();
```

Dùng khi cần gọi trực tiếp, hiệu năng cao, không hợp với event: `SaveService`, `PoolService`, `UIGroupService`. Module đăng ký service trong `InitializeAsync`.

## 5. Save — lưu trữ

`SaveService` (đăng ký vào `Services`) + provider pattern.

- **`ISaveProvider`**: `LocalSaveProvider` = JSON file trong `Application.persistentDataPath`. Ghi **atomic** (viết `.tmp` → delete → move) trên threadpool. Kiến trúc sẵn sàng thêm cloud provider (`AddProvider` + `SetActiveProvider`).
- **Cache + dirty flag**: `Load<T>(key)` cache kết quả; `Set(key, data)` cập nhật cache + đánh dấu dirty; `SaveAllAsync` chỉ ghi khi dirty.
- **`ISaveData`** (tuỳ chọn): save-data class implement để có `Version` + `OnAfterLoad`/`OnBeforeSave` (chỗ migrate schema).
- **`SaveRunner`** (MonoBehaviour): auto-save theo interval (`ModuleSave._autoSaveInterval`, mặc định 30s) + save khi `OnApplicationPause`/`OnApplicationQuit`.
- API sync `Load<T>` (blocking) dùng cho code không async được; `LoadAsync<T>` cho luồng async.

**Quy ước**: mỗi module có `private const string SaveKey = "..."` riêng (vd `"hearts"`, `"level"`, `"items"`). Data lưu ở `persistentDataPath/{key}.json`.

## 6. Pool — object pooling

`PoolService` (đăng ký vào `Services`) + static shortcut `Pool` + extension `go.Despawn()`.

- Pool theo **prefab-key**, `Spawn`/`Despawn` O(1), track `instance → pool` để Despawn tự tìm đúng pool.
- Gọi `IPoolable.OnSpawnFromPool()` / `OnReturnToPool()` để reset state.
- Code phòng thủ: chống despawn trùng, xử lý Unity-dead object (đã destroy ngoài pool), object lạ (destroy thay vì trả), hoạt động cả edit-mode.
- `ModulePool` prewarm các pool lúc boot + `ReturnAll()` khi `SceneUnloading` (dọn object scene cũ **trước** khi load scene mới — tránh despawn nhầm object scene mới).

## 7. UIGroup — quản lý màn hình

`UIGroupService` (đăng ký vào `Services`) + `ScreenRouter` (static) + component `UIGroup`.

- Gắn component `UIGroup` lên GameObject, gán 1+ `UIGroupId`. Object **hiện nếu BẤT KỲ group nào của nó đang shown** (OR semantics).
- `ShowExclusive(id)` — tắt mọi group khác, chỉ hiện group này (chuyển màn full-screen).
- `ScreenRouter.GoTo(id)` gọi `ShowExclusive` + publish `ScreenChanged`. Publish `ScreenRequest{Screen}` để yêu cầu đổi màn (ai cũng gọi được, không cần biết ScreenRouter).
- `ScreenOnStart` (đặt trong scene) tự publish `ScreenRequest` ở `Start()` (không phải Awake — để mọi `UIGroup` kịp register).
- `UIGroupId` là enum (`None/MainMenu/Gameplay/Shop`) — thêm màn = thêm enum value.

## 8. Loading & Scene

- `LoadingScreen` (boot scene, `DontDestroyOnLoad`): sở hữu **toàn bộ** thanh bar, chạy **hai phase**:
  - **Phase 1 — intro (`0 → _introTarget`, mặc định 0.85)**: tự chạy theo THỜI GIAN (`_introDuration` giây), độc lập init module; hình dạng do `_introCurve` (AnimationCurve) — linear/ease-in-out/ease-out/overshoot tuỳ chỉnh Inspector. Chạm `_introTarget` → phát `BootIntroReady`. dt cap `1/30s` để frame boot dài không làm bar nhảy.
  - **Phase 2 — chờ + fill cuối**: giữ bar ở `_introTarget` trong lúc scene load; khi `BootCompleted`, `FinishRoutine` fill nốt `_introTarget → 1` bằng ease-out lerp (`1-exp(-_fillSpeed·dt)`, snap ở 0.999) rồi fade.
  `_minLoadingTime` là ngưỡng tối thiểu trước khi fade. Fade + `Destroy(root)` khi `BootCompleted` và bar chạm 100%. Không ai gọi trực tiếp.
  > **Giữ mạng qua `DontDestroyOnLoad(transform.root.gameObject)`** — DDOL chỉ hiệu lực trên root, mà LoadingScreen thường nằm trên object con của Canvas; giữ nhầm object con → màn loading bị destroy ngay khi scene game activate.
  > **Gate boot**: `Bootstrap` gọi `EventBus.WaitFor<BootIntroReady>()` **trước** `InitModules` (WaitFor subscribe ngay → không lỡ event) rồi `await` **sau** — chờ CẢ hai: module init xong VÀ bar chạy hết intro, cái nào trễ hơn. `_introTarget` là **nguồn chân lý duy nhất** cho mốc bàn giao (Bootstrap không còn `ModuleInitWeight`). Yêu cầu: scene boot phải có LoadingScreen (nếu không, không ai phát `BootIntroReady` → treo).
- `ModuleSceneLoader`: xử lý `SceneLoadRequest{Index}` + `LoadEntryScene`, phát `SceneUnloading` → `LoadSceneAsync` (`await op`) → `SceneLoaded`. Frame activate block main thread (instantiate cả scene) nhưng lúc đó bar đứng yên ở `_introTarget` (loading screen còn che) nên không lộ giật.
- `SceneRedirect`: trong Editor, cho phép "Play từ scene bất kỳ" — editor ghi index scene đang mở vào `SessionState`, boot đọc lại để load đúng scene đó. Ngoài build luôn trả `-1` (dùng `EntrySceneIndex` mặc định).
- `ModuleTransition`: nghe `SceneLoadRequest` (priority `-1000`, che màn) và `SceneLoaded` (priority `-50`, mở màn) → tự động fade khi chuyển scene. **Chỉ hoạt động sau `BootCompleted`** — trong lúc boot, LoadingScreen đã che toàn màn nên Transition bỏ qua để tránh fade chồng fade (nhấp nháy) ở lần load scene game đầu tiên.

## 9. Debug

`Common/Debug.cs` — shim static class `Debug` (che `UnityEngine.Debug`) với `[Conditional("UNITY_EDITOR")]` + `[Conditional("DEVELOP_BUILD")]`. **Log/Warning/Error bị strip khỏi production build**; chỉ `LogException` giữ lại để crash reporting bắt lỗi thật.
