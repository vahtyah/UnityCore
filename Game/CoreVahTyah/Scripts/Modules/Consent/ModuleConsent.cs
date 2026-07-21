using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VahTyah
{
    /// <summary>
    /// State store cho đồng ý người dùng: UMP/GDPR (cá nhân hoá quảng cáo) + ATT (tracking, iOS).
    /// CHỈ lưu + trả cờ đã lưu — KHÔNG hiện popup nào.
    ///
    /// ⚠️ CHƯA ĐỦ ĐỂ PHÁT HÀNH. Popup UMP và prompt ATT thật do SDK ads (Voodoo/AppLovin) gọi,
    /// KHÔNG nằm ở đây. Khi tích hợp SDK, bên wiring SDK phải: (1) gọi UMP/ATT prompt thật,
    /// (2) publish ConsentUMPGranted/ConsentATTGranted{ Value = kết quả } để module này persist,
    /// (3) cập nhật Docs/MODULES.md → ModuleConsent. Xem mục ⚠️ trong doc đó.
    ///
    /// [ModuleRequires(ModuleSave)] → boot ngay sau ModuleSave (sớm, trước module cần hỏi consent).
    /// </summary>
    [CreateAssetMenu(menuName = "VahTyah/Modules/Consent", fileName = "Module_Consent")]
    [ModuleRequires(typeof(ModuleSave))]
    public sealed class ModuleConsent : Module
    {
        private const string SaveKey = "consent";
        private ConsentSaveData _save;

        public override UniTask InitializeAsync(Transform holder)
        {
            _save = Services.Get<SaveService>().Load<ConsentSaveData>(SaveKey);
            return UniTask.CompletedTask;
        }

        public override void Subscribe()
        {
            EventBus.On<ConsentUMPGranted>(OnUMP);
            EventBus.On<ConsentATTGranted>(OnATT);
        }

        private void OnUMP(ConsentUMPGranted e)
        {
            if (e.Value.HasValue) SetUMP(e.Value.Value);
            e.Reply?.Invoke(_save.UMPGranted);
        }

        private void OnATT(ConsentATTGranted e)
        {
            // ATT chỉ áp dụng cho iOS; nền tảng khác coi như đã đồng ý.
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                e.Reply?.Invoke(true);
                return;
            }

            if (e.Value.HasValue) SetATT(e.Value.Value);
            e.Reply?.Invoke(_save.ATTGranted);
        }

        private void SetUMP(bool granted)
        {
            _save.UMPGranted = granted;
            Persist();
        }

        private void SetATT(bool granted)
        {
            _save.ATTGranted = granted;
            Persist();
        }

        private void Persist() => Services.Get<SaveService>().Set(SaveKey, _save);
    }
}
