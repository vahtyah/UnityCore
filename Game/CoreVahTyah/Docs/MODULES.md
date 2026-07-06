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

### ModuleSound
- **Menu**: `VahTyah/Modules/Sound` · **Save key**: — (volume ở đâu tuỳ dự án)
- **Knobs**: `MasterVolume`, `PoolSize` (số AudioSource, mặc định 4), `CooldownMs` (chống spam per-id, mặc định 60), `Sounds` (List `SoundEntry{Id, Clip(s), Volume, Pitch}`).
- SFX one-shot, round-robin qua pool AudioSource. Key bằng `(int)SoundId` → tra O(1), reorder list không ảnh hưởng.

### ModuleMusic
- **Menu**: `VahTyah/Modules/Music` · **Save key**: `music` (`MusicSaveData{Active, Volume}`)
- **Knobs**: `_crossfade` (giây, mặc định 0.6), `Tracks` (List `MusicEntry{Id, Clip}`).
- Nhạc nền có crossfade khi đổi track. Spawn `[MusicPlayer]`.

### ModuleParticle
- **Menu**: `VahTyah/Modules/Particle`
- **Knobs**: `Effects` (List `ParticleEntry{Id, Prefab, Prewarm}`).
- Spawn particle qua `Pool`. **Cần ModulePool boot trước** để prewarm (nếu không có sẽ log warning).

### ModuleHaptic
- **Menu**: `VahTyah/Modules/Haptic` · **Save key**: `haptic` (`HapticSaveData{Active}`)
- **Knobs**: `_gapMs` (nghỉ giữa haptic trong chuỗi), `_cooldownMs` (mặc định 80), `_androidIntensity` (0-2).
- Provider theo platform: `HapticProviderAndroid` / `HapticProviderIOS` / `HapticProviderDefault` (editor). `Force` bỏ qua cooldown nhưng không ghi đè khi user tắt haptic.

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
