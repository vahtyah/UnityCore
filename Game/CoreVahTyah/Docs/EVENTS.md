# Event Catalog

API surface để module giao tiếp. Tất cả là `struct : IEvent`. Publish bằng `EventBus.Publish(new X{...}).Forget()` (hoặc `await` nếu cần chờ handler async).

**Ký hiệu:**
- **Cmd** = command (ra lệnh) · **Notify** = thông báo (đã xảy ra) · **Query** = hỏi giá trị (có `Reply`)
- Query dùng `Action<T> Reply`, đọc được ngay sau `Publish` vì handler chạy đồng bộ.

---

## Boot & Scene

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `BootIntroReady` | Notify | — | LoadingScreen phát khi bar chạy xong intro (0→intro target); Bootstrap chờ event này rồi mới load scene |
| `BootCompleted` | Notify | — | Boot xong; LoadingScreen fade out |
| `SceneLoadRequest` | Cmd | `int Index` | Yêu cầu load scene theo build index |
| `LoadEntryScene` | Cmd | — | Vào game lần đầu; SceneLoader tự chọn index |
| `SceneUnloading` | Notify | — | Trước khi load scene (Pool `ReturnAll` tại đây) |
| `SceneLoaded` | Notify | `int Index` | Sau khi scene đã activate xong |

## Screen / UI

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `ScreenRequest` | Cmd | `UIGroupId Screen` | Yêu cầu chuyển màn (ai cũng publish được) |
| `ScreenChanged` | Notify | `UIGroupId Screen` | Đã đổi màn; ScreenRouter phát |

## Transition

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `TransitionRequest` | Cmd | `bool Cover` | `true` = che màn, `false` = mở màn |

## Level

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `LevelStarted` | Notify | `Dictionary<string,object> Extra` | Bắt đầu màn; Level tăng `Tries` |
| `LevelCompleted` | Notify | `bool ShowScreen; float ShowDelay; Dictionary Extra` | Thắng màn; `ShowScreen`→ WinScreen |
| `LevelFailed` | Notify | `bool ShowScreen; float ShowDelay; Dictionary Extra` | Thua màn; `ShowScreen`→ FailScreen |
| `LevelSet` | Cmd | `int Level` | Set level cụ thể (reset Tries) |
| `LevelChanged` | Notify | `int Level` | Level đã đổi |
| `LevelGet` | Query | `Action<int> Reply` | Level hiện tại (số đếm, bắt đầu từ 1) |
| `LevelGetIndex` | Query | `Action<int> Reply` | Index 0-based (đã tính loop pool) |
| `LevelGetTries` | Query | `Action<int> Reply` | Số lần thử màn hiện tại |

## Heart (mạng / năng lượng)

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `HeartAdd` | Cmd | `int Value; bool Direct` | Cộng tim |
| `HeartUse` | Query | `int Value; Action<bool> Reply` | Trừ tim; Reply=false nếu không đủ |
| `HeartGet` | Query | `Action<int> Reply` | Số tim hiện tại |
| `HeartIsFull` | Query | `Action<bool> Reply` | Đã đầy tim? |
| `HeartIsInfinity` | Query | `Action<bool> Reply` | Đang ở chế độ vô hạn? |
| `HeartGetTimer` | Query | `Action<string> Reply` | Chuỗi hiển thị timer hồi tim |
| `HeartAddInfinity` | Cmd | `float Minutes` | Cộng thời gian tim vô hạn |
| `HeartGetInfinityTimer` | Query | `Action<string> Reply` | Chuỗi hiển thị timer vô hạn |
| `HeartChanged` | Notify | — | Số tim đổi |
| `HeartInfinityChanged` | Notify | — | Trạng thái vô hạn đổi |

## Item (tài nguyên: coin, gem, ...)

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `ItemAdd` | Cmd | `string Key; int Value; bool Pending` | Cộng item; `Pending`→ cộng vào hàng chờ |
| `ItemGet` | Query | `string Key; bool Pending; Action<int> Reply` | Lấy số lượng (trừ `_inFlight` nếu Pending) |
| `ItemCommitPending` | Cmd | `string Key; int Value` | Chuyển pending → current |
| `ItemChanged` | Notify | `string Key` | Số lượng item đổi |
| `ItemFlyPending` | Cmd | `string Key; Transform From; int Value` | Bay pending vào counter + commit. `Value>0`→ bay đúng Value (clamp theo pending chưa bay); `Value<=0`→ bay TẤT CẢ pending chưa bay của Key |
| `ItemCollect` | Cmd | `string Key; Transform From; int Value` | Combo an toàn: add pending + `ItemFlyPending` trong 1 nhịp (tránh desync). Dùng khi thu item có sẵn (không cần secure trước) |
| `ItemTrySpend` | Query | `string Key; int Value; Action<bool> Reply` | Tiêu nguyên tử: đủ → trừ + Reply(true); thiếu → không trừ + Reply(false) |

> **Pending pattern**: khi thưởng item có animation bay, cộng vào `Pending` + đưa `_inFlight`; animation chạy xong mới `CommitPending` sang `Current`. `ItemGet{Pending}` trừ phần đang bay để hiển thị mượt.
>
> **Không mất thưởng**: bơm quyền sở hữu vào `Pending` sớm bằng `ItemAdd{Pending=true}` (persist ngay), rồi `ItemFlyPending{Key}` (không truyền `Value`) lúc muốn bay. Kill giữa chừng → boot fold `Pending→Current`. Nguồn số lượng luôn là `Pending` nên không desync.

## Feature (mở khoá tính năng theo level)

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `FeatureRefresh` | Cmd | — | Tính lại feature active theo level hiện tại |
| `FeatureState` | Notify | `Active, Index, ProgressMin/Max, Unlocked, Icon, IconDark, ConditionText` | Trạng thái để view vẽ progress |
| `FeatureUnlocked` | Notify | `int Index` | Feature vừa đủ điều kiện mở |
| `FeaturePendingUnlock` | Notify | `int Index` | Có unlock đang chờ show |
| `FeatureConsumePending` | Query | `Action<bool> Reply` | Lấy + clear pending unlock |

## Feedback / VFX — KHÔNG dùng event

> **Sound / Music / Haptic / Particle KHÔNG dùng event** (đổi từ 2026-07). Là command tần suất cao, đúng 1 nơi xử lý → gọi trực tiếp qua service + shortcut tĩnh, không qua EventBus:
> ```csharp
> Sound.Play(SoundId.Click);
> Music.Play(MusicId.Home);  Music.Stop();  Music.SetVolume(0.5f);
> Haptic.Play(HapticType.Light);  await Haptic.PlaySequence(false, HapticType.Light, HapticType.Heavy);
> Particles.Play(ParticleId.Explosion, worldPos);  // trả GameObject; overload có rotation / parent (follow)
> ```
> Bật/tắt + volume đọc từ `SettingsService` (SSOT). Xem [CONVENTIONS.md](CONVENTIONS.md) → "Command → Service hay Event?" và [MODULES.md](MODULES.md).

## Settings

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `OpenSettingsRequest` | Cmd | — | Yêu cầu mở popup Settings |
| `SettingsChanged` | Notify | `bool Sound; bool Sfx; bool Haptics` | Toggle preference đổi; `SettingsService` phát. `MusicService` nghe để mute/unmute BGM ngay (SFX/Haptic gate lúc Play nên không cần nghe) |

## Tutorial

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `TutorialFinished` | Notify | — | Tutorial màn hiện tại xong; module đánh dấu done + destroy |

---

## Ví dụ dùng

```csharp
// Command qua EventBus (cross-module, low-frequency)
EventBus.Publish(new ScreenRequest { Screen = UIGroupId.Shop }).Forget();

// Command qua Service (hot-path feedback — KHÔNG qua EventBus)
Sound.Play(SoundId.Click);
Haptic.Play(HapticType.Light);

// Query (đọc ngay vì sync)
int hearts = 0;
EventBus.Publish(new HeartGet { Reply = v => hearts = v }).Forget();

// Command có reply bool
bool ok = false;
EventBus.Publish(new HeartUse { Value = 1, Reply = success => ok = success }).Forget();
if (!ok) { /* không đủ tim */ }

// Nghe trong MonoBehaviour (auto-cleanup khi destroy)
this.On<HeartChanged>(_ => RefreshHeartUI());

// Nghe trong Module.Subscribe() (sống suốt đời game)
EventBus.On<LevelCompleted>(OnCompleted, priority: -100);
```
