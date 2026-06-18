namespace VahTyah
{
    /// <summary>
    /// Cầu nối runtime cho cơ chế "Play từ scene bất kỳ".
    /// Editor (SceneRedirectEditor) ghi index scene đang sửa vào SessionState;
    /// boot đọc lại để load đúng scene đó thay vì scene mặc định.
    /// Ngoài Editor luôn trả -1 (build dùng cấu hình mặc định).
    /// </summary>
    public static class SceneRedirect
    {
#if UNITY_EDITOR
        public const string SessionKey = "VahTyah.RedirectSceneIndex";
#endif

        /// <summary>Scene index nên load khi boot. -1 = dùng GameSceneIndex mặc định.</summary>
        public static int OverrideSceneIndex
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetInt(SessionKey, -1);
#else
                return -1;
#endif
            }
        }
    }
}
