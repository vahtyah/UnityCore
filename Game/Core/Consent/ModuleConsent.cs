using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Module quản lý đồng ý người dùng (UMP/GDPR và ATT trên iOS).
    /// Lưu trạng thái granted vào PlayerPrefs và trả kết quả qua callback của event.
    /// Priority thấp (-100) để khởi tạo/đăng ký sớm trước các module khác.
    /// </summary>
    [CreateAssetMenu(menuName = "SA/Modules/Consent", fileName = "Module_Consent", order = 5)]
    internal sealed class ModuleConsent : SAModule
    {
        public string UMPGrantedKey = "consent.ump.granted";

        public string ATTGrantedKey = "consent.att.granted";

        public ModuleConsent()
        {
            Priority = -100;
        }

        public override void Subscribe()
        {
            SATypedBus.On<Ev.ConsentUMPGranted>(e =>
            {
                bool? value = e.Value;
                if (value.HasValue)
                {
                    SetConsentUMP(value.Value);
                }
                bool result = PlayerPrefs.GetInt(UMPGrantedKey, 0) == 1;
                e.Reply?.Invoke(result);
            });

            SATypedBus.On<Ev.ConsentATTGranted>(e =>
            {
                // ATT chỉ áp dụng cho iOS; nền tảng khác coi như đã đồng ý.
                if (Application.platform != RuntimePlatform.IPhonePlayer)
                {
                    e.Reply?.Invoke(true);
                    return;
                }
                bool? value = e.Value;
                if (value.HasValue)
                {
                    SetConsentATT(value.Value);
                }
                bool result = PlayerPrefs.GetInt(ATTGrantedKey, 0) == 1;
                e.Reply?.Invoke(result);
            });
        }

        private void SetConsentUMP(bool granted)
        {
            PlayerPrefs.SetInt(UMPGrantedKey, granted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SetConsentATT(bool granted)
        {
            PlayerPrefs.SetInt(ATTGrantedKey, granted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
