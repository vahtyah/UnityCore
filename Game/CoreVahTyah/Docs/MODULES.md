# Module Reference

Mỗi module là 1 `ScriptableObject` asset trong `Config/`, thêm vào `ModuleConfig.Modules`. Cột **Save key** = key trong `persistentDataPath/{key}.json`.

---

## Hạ tầng (thường là core, boot sớm)

### ModuleSave
- **Menu**: `VahTyah/Modules/Save` · **Save key**: — (quản lý mọi key)
- **Knobs**: `_autoSaveInterval` (giây, mặc định 30, 0=tắt).
- Tạo `SaveService`, add `LocalSaveProvider`, spawn `[SaveRunner]`. **Phải boot trước mọi module dùng save.**

### ModulePool
- **Menu**: `VahTyah/Modules/Pool`
- **Knobs**: `Pools` (List `PoolEntry{Prefab, Prewarm}`).
- Tạo `PoolService`, prewarm các pool, `ReturnAll()` khi `SceneUnloading`. **Boot trước ModuleParticle.**

### ModuleUIGroup
- **Menu**: `VahTyah/Modules/UIGroup`
- Tạo `UIGroupService`; nghe `ScreenRequest` → `ScreenRouter.GoTo`.

### ModuleSceneLoader
- **Menu**: `VahTyah/Modules/SceneLoader`
- **Knobs**: `EntrySceneIndex` (mặc định 1).
- Xử lý `LoadEntryScene` + `SceneLoadRequest`, phát `SceneUnloading` → `LoadSceneAsync` → `SceneLoaded`. Đọc `SceneRedirect` để hỗ trợ Play-từ-scene-bất-kỳ trong Editor.

### ModuleTransition
- **Menu**: `VahTyah/Modules/Transition`
- **Knobs**: `_transitionPrefab` (có Animator) hoặc fallback `_fadeColor/_fadeDuration/_sprite`.
- Tự che/mở màn khi chuyển scene (nghe `SceneLoadRequest` prio -1000, `SceneLoaded` prio -50).

### ModuleScreenSettings
- **Menu**: `VahTyah/Modules/Screen Settings`
- **Knobs**: `_autoFrameRate`, `_defaultFrameRate` (30/60/90/120), `_batterySaveFrameRate` (iOS low-power), `_vSyncCount`, `_sleepTimeout`.
- Set `Application.targetFrameRate`, vSync, sleep timeout lúc boot.

---

## Gameplay & Kinh tế

### ModuleLevel
- **Menu**: `VahTyah/Modules/Level` · **Save key**: `level` (`LevelSaveData{Level, Tries}`)
- **Knobs**: `_config` (`LevelConfig{TotalLevels, NonLoopLevels}`), `_enableTestLevel`+`_testLevel`, `_displayPrefab`.
- `ModuleLevel.Current` (static) = level hiện tại. `LevelIndex` = index màn để load (0-based), có **loop pool**: hết `TotalLevels` thì lặp lại, loại trừ các `NonLoopLevels` (khoảng chỉ chơi 1 lần, vd tutorial). `_displayPrefab` spawn 1 lần, sống xuyên scene.

### ModuleHeart
- **Menu**: `VahTyah/Modules/Heart` · **Save key**: `hearts` (`HeartSaveData`)
- **Knobs**: `MaxHearts` (mặc định 5), `MinutesPerHeart` (mặc định 1).
- Regen tim theo thời gian thực + chế độ vô hạn (infinity). **Anti-cheat thời gian**: dùng `Stopwatch.GetTimestamp()` (monotonic) làm nguồn chính, `DateTime.UtcNow.ToBinary()` làm neo để recalibrate khi phát hiện chỉnh giờ (timestamp lùi). Spawn `[HeartTicker]` để tick regen.

### ModuleItem
- **Menu**: `VahTyah/Modules/Item` · **Save key**: `items` (`ItemSaveData`, keyed by string)
- **Knobs**: `Items` (List `ItemDefinition{Key, Title, Icon, Prefab, StartAmount}`) + tham số animation (`SpawnRadius, StaggerDelay, Duration, CurveStrength, MoveCurve, ScaleCurve, MaxPoolSize, CanvasSortingOrder`).
- Quản lý tài nguyên theo string key. **Pending/in-flight pattern** cho animation item bay (xem EVENTS.md → Item). Tự tạo Canvas overlay riêng cho animation.

### ModuleFeature
- **Menu**: `VahTyah/Modules/Feature`
- **Knobs**: `Definitions` (`FeatureDefinition[]` với `LevelMin/LevelMax/Icon/IconDark/ConditionText`), `_unlockViewPrefab`.
- Mở khoá tính năng theo level. Sort theo `LevelMax`, tính progress bar giữa các mốc, phát `FeatureState`. Hỏi level qua `LevelGet`. **Không lưu save** — suy ra từ level hiện tại.

---

## Feedback (Audio / Visual / Haptic)

### ModuleSettingsScreen
- **Menu**: `VahTyah/Modules/SettingsScreen` · **Save key**: `settings` (`SettingsSaveData{Sound, Sfx, Haptics, MusicVolume}`)
- **Knobs**: `Prefab` (popup, có `SettingsView` + `PopupView`; instantiate 1 lần lúc boot, ẩn sẵn).
- Register `SettingsService` — **SSOT cho mọi preference audio/haptic**: `Sound` (BGM), `Sfx`, `Haptics`, `MusicVolume`. Mở popup khi `OpenSettingsRequest`; `SettingsView` đọc/ghi qua service, publish `SettingsChanged` khi toggle. **Boot sau `ModuleSave`, trước Sound/Haptic/Music.**

> **Sound/Music/Haptic dùng Service, không dùng event.** Mỗi `Module` là factory register 1 service vào `Services`; gọi qua shortcut tĩnh (`Sound`/`Music`/`Haptic`). Cờ bật/tắt + volume đọc từ `SettingsService` (không lưu riêng). **Phải boot sau `ModuleSettingsScreen`.** Xem [CONVENTIONS.md](CONVENTIONS.md).

### ModuleSound
- **Menu**: `VahTyah/Modules/Sound` · **Save key**: — (cờ SFX ở `settings.Sfx`)
- **Knobs**: `MasterVolume`, `PoolSize` (số AudioSource, mặc định 4), `CooldownMs` (chống spam per-id, mặc định 60), `Sounds` (List `SoundEntry{Id, Clip(s), Volume, Pitch}`).
- Register `SoundService`; SFX one-shot round-robin qua pool AudioSource, key `(int)SoundId` → tra O(1). Gate bằng `SettingsService.Sfx` lúc `Play`. Shortcut: `Sound.Play(id, volume, pitch, force)`.

### ModuleMusic
- **Menu**: `VahTyah/Modules/Music` · **Save key**: — (cờ ở `settings.Sound`, volume ở `settings.MusicVolume`)
- **Knobs**: `_crossfade` (giây, mặc định 0.6), `Tracks` (List `MusicEntry{Id, Clip}`).
- Register `MusicService` + spawn `[MusicPlayer]` (crossfade khi đổi track). Nhạc phát liên tục nên service **nghe `SettingsChanged`** để mute/unmute ngay. Shortcut: `Music.Play(id)`/`Stop()`/`SetVolume(v)`.

### ModuleParticle
- **Menu**: `VahTyah/Modules/Particle`
- **Knobs**: `Effects` (List `ParticleEntry{Id, Prefab, Prewarm}`).
- Spawn particle qua `Pool`. **Cần ModulePool boot trước** để prewarm (nếu không có sẽ log warning). (Particle vẫn dùng event `ParticlePlay`/`ParticlePlayUI`.)

### ModuleHaptic
- **Menu**: `VahTyah/Modules/Haptic` · **Save key**: — (cờ ở `settings.Haptics`)
- **Knobs**: `_gapMs` (nghỉ giữa haptic trong chuỗi), `_cooldownMs` (mặc định 80), `_androidIntensity` (0-2, scale tổng), `_light/_medium/_heavy` (`HapticOneShot{DurationMs, Amplitude}` — chỉnh mạnh/nhẹ từng loại, **chỉ Android**).
- Register `HapticService`; provider theo platform: `HapticProviderAndroid` / `HapticProviderIOS` / `HapticProviderDefault` (editor). Gate bằng `SettingsService.Haptics`; `Force` bỏ qua cooldown nhưng không ghi đè khi user tắt. Shortcut: `Haptic.Play(type, force)`/`PlaySequence(force, types)`.
- **Chỉnh cường độ:** Light/Medium/Heavy tune qua `DurationMs`+`Amplitude` per-type × `_androidIntensity`. `Amplitude` chỉ có tác dụng trên máy có amplitude control (init log `hasAmplitudeControl`); máy không có thì `DurationMs` là đòn bẩy duy nhất. Success/Warning/Failure là waveform cố định. **iOS không tune được** (system feedback). Editor no-op.

---

## Màn kết quả

### ModuleWinScreen / ModuleFailScreen
- **Menu**: `VahTyah/Modules/WinScreen` · `VahTyah/Modules/FailScreen`
- **Knobs**: `Prefab` (view, instantiate 1 lần lúc boot, ẩn sẵn).
- Nghe `LevelCompleted`/`LevelFailed` (prio -100): nếu `ShowScreen`, chờ `ShowDelay` rồi hiện. Tự ẩn khi `SceneLoaded`.

### ModuleTutorial
- **Menu**: `VahTyah/Modules/Tutorial` · **Save key**: `tutorial` (`TutorialSaveData`)
- **Knobs**: `_tutorials` (List `LevelTutorial{Level, Prefab}`).
- Nghe `LevelStarted`, hỏi level, spawn prefab tutorial cho level đó nếu chưa done. Prefab **phải có component `Tutorial`**. `TutorialFinished` → destroy + `MarkDone`.
