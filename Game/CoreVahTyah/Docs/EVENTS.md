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
| `ItemAnimationPlay` | Cmd | `string Key; Transform From; int Value` | Chạy animation item bay (coin fly) |

> **Pending pattern**: khi thưởng item có animation bay, cộng vào `Pending` + đưa `_inFlight`; animation chạy xong mới `CommitPending` sang `Current`. `ItemGet{Pending}` trừ phần đang bay để hiển thị mượt.

## Feature (mở khoá tính năng theo level)

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `FeatureRefresh` | Cmd | — | Tính lại feature active theo level hiện tại |
| `FeatureState` | Notify | `Active, Index, ProgressMin/Max, Unlocked, Icon, IconDark, ConditionText` | Trạng thái để view vẽ progress |
| `FeatureUnlocked` | Notify | `int Index` | Feature vừa đủ điều kiện mở |
| `FeaturePendingUnlock` | Notify | `int Index` | Có unlock đang chờ show |
| `FeatureConsumePending` | Query | `Action<bool> Reply` | Lấy + clear pending unlock |

## Sound / Music / Particle / Haptic

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `SoundPlay` | Cmd | `SoundId Id; float Volume; float Pitch` | SFX one-shot; `<=0`→ mặc định 1. Có cooldown per-id |
| `MusicPlay` | Cmd | `MusicId Id` | Đổi track (crossfade) |
| `MusicStop` | Cmd | — | Fade out |
| `MusicSetVolume` | Cmd | `float Volume` | Set + lưu volume |
| `MusicSetActive` | Cmd | `bool Active` | Bật/tắt + lưu |
| `MusicChanged` | Notify | `bool Active; float Volume` | Trạng thái nhạc đổi |
| `MusicGet` | Query | `Action<bool,float> Reply` | (active, volume) |
| `ParticlePlay` | Cmd | `ParticleId Id; Vector3 Position` | Spawn particle (world) qua Pool |
| `ParticlePlayUI` | Cmd | `ParticleId Id; Vector3 Position` | Particle trên UI |
| `HapticPlay` | Cmd | `HapticType Type; bool Force` | Rung 1 lần; `Force`→ bỏ qua cooldown |
| `HapticSequence` | Cmd | `HapticType[] Types; bool Force` | Rung tuần tự (async, có gap) |
| `HapticSetActive` | Cmd | `bool Active` | Bật/tắt + lưu |
| `HapticChanged` | Notify | `bool Active` | Trạng thái rung đổi |
| `HapticGet` | Query | `Action<bool> Reply` | Đang bật rung? |

## Tutorial

| Event | Loại | Field | Ghi chú |
|-------|------|-------|---------|
| `TutorialFinished` | Notify | — | Tutorial màn hiện tại xong; module đánh dấu done + destroy |

---

## Ví dụ dùng

```csharp
// Command
EventBus.Publish(new SoundPlay { Id = SoundId.Click }).Forget();
EventBus.Publish(new ScreenRequest { Screen = UIGroupId.Shop }).Forget();

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
