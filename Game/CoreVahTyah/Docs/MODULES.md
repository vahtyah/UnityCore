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

### ModuleConsent
- **Menu**: `VahTyah/Modules/Consent` · **Save key**: `consent` (`ConsentSaveData{UMPGranted, ATTGranted}`)
- **Knobs**: — (không có; boot ngay sau `ModuleSave` nhờ `[ModuleRequires(ModuleSave)]`).
- Lưu + trả 2 cờ đồng ý quảng cáo: **UMP/GDPR** (cá nhân hoá ads, người dùng EU) và **ATT** (App Tracking Transparency, iOS 14.5+). Nghe `ConsentUMPGranted`/`ConsentATTGranted` theo pattern set-hoặc-get (xem [EVENTS.md](EVENTS.md)). Nền tảng ≠ iOS → ATT luôn trả `true`.

> ⚠️ **ĐỌC TRƯỚC KHI ĐỘNG VÀO / KHI TÍCH HỢP SDK ADS.** Module này **chỉ là state store** — nó lưu và trả về cờ, **KHÔNG hiện popup UMP/ATT**. Do đó bản thân nó **chưa đủ để phát hành**: build gửi Voodoo vẫn cần consent flow thật.
>
> Popup UMP và prompt ATT do **SDK ads bên ngoài** (Voodoo/AppLovin, hoặc `ATTrackingManager` của iOS) gọi — phần đó hiện **chưa có** trong cả Core lẫn CoreVahTyah. Khi tích hợp SDK, bên wiring SDK phải:
> 1. Gọi UMP consent form + ATT prompt thật lúc khởi động.
> 2. `EventBus.Publish(new ConsentUMPGranted { Value = <kết quả> })` và tương tự cho ATT → `ModuleConsent` persist cờ.
> 3. Các module ads đọc lại cờ qua `ConsentUMPGranted`/`ConsentATTGranted` (không truyền `Value`) trước khi request ads.
> 4. **Cập nhật lại chính mục này** khi hành vi/flow đổi — nêu rõ SDK nào gọi, gọi ở đâu, lúc nào.

### ModuleNotifications
- **Menu**: `VahTyah/Modules/Notifications` · **Save key**: `notifications` (`NotificationsSaveData{LastPlayBinary, LastScheduledBinary}`)
- **Knobs**: `_androidChannelId/Name/Description`; `_androidIcons` (List `NotificationIcon{Id, IsSmall, Texture}` — icon bake vào Project Settings); `_androidSmallIconId` (dropdown chọn 1 Small icon đã đăng ký, hoặc **Auto**); `_reEngagement` (List `ReEngagementEntry{Id, Title, Body, DelayHours}`, mặc định 10 mục 2h→~28 ngày); `_maxScheduled` (1-10); `_rescheduleCooldownHours`.
- Lên lịch/huỷ local notification qua event `NotificationSchedule`/`Cancel`/`CancelAll`. **Tự động re-engagement**: nghe `AppPaused`/`AppQuitting` → lên lịch chuỗi (có cooldown chống lên lịch lại liên tục), nghe `AppResumed` + lúc boot → huỷ hết. Trừu tượng nền tảng qua `INotificationProvider`. Timestamp lưu dạng `DateTime.ToBinary()` (JsonUtility không lưu `DateTime`).

> ⚠️ **PROVIDER & PHỤ THUỘC PACKAGE.** Trong **Editor** provider luôn là **NO-OP** (`NotificationProviderDefault`) — logic lịch/cooldown chạy đúng nhưng không có notification thật; điều này là cố ý (test không spam OS).
>
> Provider thật nằm ở `Modules/Notifications/Platform/` (`AndroidNotificationProvider`, `IOSNotificationProvider`, `NotificationRegistrar`), bọc **Unity Mobile Notifications** (`com.unity.mobile.notifications`). Guard: `#if UNITY_ANDROID/UNITY_IOS && !UNITY_EDITOR && VAHTYAH_MOBILE_NOTIFICATIONS`. `NotificationRegistrar` `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` tự chọn provider trước khi module init.
>
> **Cài bằng 1 nút**: chọn asset module trong Inspector → nút **"Install Unity Mobile Notifications"** (`[Button]`). Nó `Client.Add` package **rồi tự thêm scripting define `VAHTYAH_MOBILE_NOTIFICATIONS`** (Android + iOS) → Unity recompile → provider thật bật lên. Không cần thao tác Package Manager tay. Nút này chỉ hiện khi **chưa** có define (`#if !VAHTYAH_MOBILE_NOTIFICATIONS`) → cài xong tự biến mất.
>
> **Chưa cài package thì build có sao không? KHÔNG.** Define chưa bật → code provider bị loại khỏi compile → registrar trả null → factory fallback no-op. Build vẫn chạy bình thường, chỉ là không có notification thật. (Đây là lý do dùng define chứ không chỉ `#if UNITY_ANDROID`.)
>
> **Android small icon** (gotcha): Android **bắt buộc** một small icon (status bar) là **silhouette đơn sắc** trắng-trên-trong-suốt (chỉ render alpha channel; màu bị bỏ). Thiếu nó → fallback app icon → nhiều máy hiện ô xám hoặc **KHÔNG hiện notification**. iOS không cần (tự dùng app icon).
> - **Đăng ký icon bằng 1 nút**: fill mảng `_androidIcons` (mỗi entry `Id` + `IsSmall` + `Texture`) → nút **"Setup Notification Icons"** ghi **thẳng** vào Project Settings → Mobile Notifications qua reflection (`NotificationSettingsManager.DrawableResources`), thay entry trùng `Id`. Reflection có try/catch + fallback mở settings để thêm tay nếu API package đổi. (Thay cho nút "Open Settings" cũ.)
> - **Chọn icon dùng runtime**: `_androidSmallIconId` là dropdown (attribute `[SmallIconId]`) liệt kê các Small icon đã đăng ký + option **Auto**. Runtime resolve theo `ModuleNotifications.ResolveAndroidSmallIconId()`: có chọn rõ → dùng cái đó; **Auto** + đúng 1 Small icon → tự lấy; Auto + nhiều Small icon → trả `null` → fallback app icon (buộc phải chọn rõ khi có nhiều). Id không khớp mảng → dropdown hiện `(missing!)`.
>
> **Chỉ là local notification** (lịch trên máy). Muốn gửi campaign từ server (segment/A-B nội dung) là **push** — cần SDK riêng (FCM/OneSignal), thêm kênh riêng, không thay thế module này.
>
> **Phụ thuộc**: dựa trên event vòng đời `AppPaused/AppResumed/AppQuitting` do `Bootstrap` phát (xem [EVENTS.md](EVENTS.md) → App lifecycle). Config hiện đọc thẳng từ field asset; khi có RemoteConfig có thể overlay (bản Core cũ làm vậy qua `SADataProvider`).

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
- **Shortcut tĩnh** `Item` (bọc EventBus): `Item.Get(key)`, `Item.TrySpend(key, value)`, `Item.Add(key, value, pending)`, `Item.Collect(key, from, value)`, `Item.FlyPending(key, from, value)`. Query đọc được ngay vì handler chạy sync — khỏi tự tạo biến hứng `Reply`.

### ModuleFeature
- **Menu**: `VahTyah/Modules/Feature`
- **Knobs**: `Definitions` (`FeatureDefinition[]` với `LevelMin/LevelMax/Icon/IconDark/ConditionText`), `_unlockViewPrefab`.
- Mở khoá tính năng theo level. Sort theo `LevelMax`, tính progress bar giữa các mốc, phát `FeatureState`. Hỏi level qua `LevelGet`. **Không lưu save** — suy ra từ level hiện tại.

---

## Feedback (Audio / Visual / Haptic)

### ModuleSettingsScreen
- **Menu**: `VahTyah/Modules/SettingsScreen` · **Save key**: `settings` (`SettingsSaveData{Sound, Sfx, Haptics, MusicVolume}`)
- **Knobs**: `Prefab` (popup, có `SettingsView` + `PopupAnimator`; instantiate 1 lần lúc boot, ẩn sẵn).
- Register `SettingsService` — **SSOT cho mọi preference audio/haptic**: `Sound` (BGM), `Sfx`, `Haptics`, `MusicVolume`. Mở popup khi `OpenSettingsRequest`; `SettingsView` đọc/ghi qua service, publish `SettingsChanged` khi toggle. **Boot sau `ModuleSave`, trước Sound/Haptic/Music.**

> **Sound/Music/Haptic dùng Service, không dùng event.** Mỗi `Module` là factory register 1 service vào `Services`; gọi qua shortcut tĩnh (`Sound`/`Music`/`Haptic`). Cờ bật/tắt + volume đọc từ `SettingsService` (không lưu riêng). **Phải boot sau `ModuleSettingsScreen`.** Xem [CONVENTIONS.md](CONVENTIONS.md).

### ModuleSound
- **Menu**: `VahTyah/Modules/Sound` · **Save key**: — (cờ SFX ở `settings.Sfx`)
- **Knobs**: `MasterVolume`, `PoolSize` (số AudioSource, mặc định 4), `CooldownMs` (chống spam per-id, mặc định 60), `Sounds` (List `SoundEntry{Id, Clip(s), Volume, Pitch}`).
- Register `SoundService`; SFX one-shot round-robin qua pool AudioSource, key `(int)SoundId` → tra O(1). Gate bằng `SettingsService.Sfx` lúc `Play`. Shortcut: `Sound.Play(id, volume, pitch, force)`.

### ModuleMusic
- **Menu**: `VahTyah/Modules/Music` · **Save key**: — (cờ ở `settings.Sound`, volume ở `settings.MusicVolume`)
- **Knobs**: `_crossfade` (giây, mặc định 0.6), `Tracks` (List `MusicEntry{Id, Clip}`).
- Register `MusicService` + spawn `[MusicPlayer]` (crossfade khi đổi track). Nhạc phát liên tục nên service **nghe `SettingsChanged`** để mute/unmute ngay. Shortcut: `Music.Play(id)`/`Stop()`/`SetVolume(v)`/`Pause()`/`Resume()`/`Duck(factor,dur)`/`Unduck(dur)`.
- Fade dùng **LitMotion** (`MotionScheduler.UpdateIgnoreTimeScale`) → chạy cả khi `timeScale = 0`, huỷ được. Gain **sống** (volume × duck × cờ Sound) áp ngay cả khi đang fade — đổi volume/mute giữa crossfade không bị bỏ qua.
- `Pause`/`Resume`: tạm dừng giữ vị trí phát (game pause). `Duck`/`Unduck`: hạ/trả gain nhạc nền (vd hạ khi mở popup) — caller tự gọi vì Core chưa có event panel-shown.

### ModuleParticle
- **Menu**: `VahTyah/Modules/Particle`
- **Knobs**: `Effects` (List `ParticleEntry{Id, Prefab, Prewarm}`).
- Register `ParticleService`; spawn qua shortcut **`Particles.Play(id, pos)`** (không dùng event nữa — như Sound/Haptic). Overload có `rotation` và `parent` (particle follow object). Prewarm pool lúc boot — **cần ModulePool boot trước** (nếu thiếu sẽ log warning, `Play` trả null). `ParticleService` **tự AddComponent `PooledParticle` lúc spawn** nếu prefab chưa có → không cần gắn tay (component tự lái qua OnEnable/OnDisable: Play khi spawn, Stop+Clear khi despawn, tự despawn khi particle dừng). Muốn đặt `_maxLifetime > 0` (despawn cưỡng bức cho looping/leak) thì gắn sẵn `PooledParticle` trên prefab — instance auto-add luôn có lifetime = 0.

### ModuleHaptic
- **Menu**: `VahTyah/Modules/Haptic` · **Save key**: — (cờ ở `settings.Haptics`)
- **Knobs**: `_gapMs` (nghỉ giữa haptic trong chuỗi), `_cooldownMs` (mặc định 80), `_androidIntensity` (0-2, scale tổng), `_light/_medium/_heavy` (`HapticOneShot{DurationMs, Amplitude}` — chỉnh mạnh/nhẹ từng loại, **chỉ Android**).
- Register `HapticService`; provider theo platform: `HapticProviderAndroid` / `HapticProviderIOS` / `HapticProviderDefault` (editor). Gate bằng `SettingsService.Haptics`; `Force` bỏ qua cooldown nhưng không ghi đè khi user tắt. Shortcut: `Haptic.Play(type, force)`/`PlaySequence(force, types)`.
- **Chỉnh cường độ:** Light/Medium/Heavy tune qua `DurationMs`+`Amplitude` per-type × `_androidIntensity`. `Amplitude` chỉ có tác dụng trên máy có amplitude control (init log `hasAmplitudeControl`); máy không có thì `DurationMs` là đòn bẩy duy nhất. Success/Warning/Failure là waveform cố định. **iOS không tune được** (system feedback). Editor no-op.

### ModuleInteractable
- **Menu**: `VahTyah/Modules/Interactable` · **Save key**: — (không lưu)
- **Knobs**: `_styles` (List `InteractableStyleProfile` keyed theo `InteractableStyleId`: `PressDuration/ReleaseDuration/PressedScale`, `BounceScale/BounceUp/BounceDown`, 4 easing curve, + `ClickHaptic`/`ClickSound`).
- Factory mỏng: register `InteractableStyleService` (bảng tra O(1), fallback `Default`). `InteractableFeedback` (dùng chung cho button/toggle/...) chọn 1 `InteractableStyleId`, đọc qua shortcut `InteractableStyle.Get(id)`; thiếu module → code-default. **Thuần feedback**: scale nhấn/thả/nảy + haptic/sound theo pointer, KHÔNG tự gọi hành động — logic do Unity Button/Toggle cùng GameObject xử lý. Feedback click qua `Haptic.Play`/`Sound.Play` (gate sẵn bởi `SettingsService`).

### ModulePanel
- **Menu**: `VahTyah/Modules/Panel` · **Save key**: — (không lưu)
- **Knobs**: `_popupStyles` (List `PopupStyle` keyed `PopupStyleId`: `ScaleCurve`+`FadeInDuration` mở; `HasCloseAnimation`/`CloseDuration`/`CloseScale`/`CloseEase` đóng) + `_fadeStyles` (List `FadeStyle` keyed `FadeStyleId`: `FadeIn`/`FadeInDuration`/`FadeOut`/`FadeOutDuration`).
- Factory mỏng: register `PanelStyleService` (2 bảng tra O(1), mỗi loại fallback `Default`). Quản lý animation cho mọi `IPanelAnimator`: `PopupAnimator` (scale+fade) đọc `PanelStyle.Popup(_style)`, `FadeAnimator` (fade-only) đọc `PanelStyle.Fade(_style)`. Cả hai **chỉ giữ `_style`** (+ `_panel` cho Popup) — không field style cục bộ, một nguồn sự thật. Thiếu `ModulePanel`/profile → fallback code-default.

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
