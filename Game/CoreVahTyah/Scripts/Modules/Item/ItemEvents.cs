using System;
using UnityEngine;

namespace VahTyah
{
    public struct ItemAdd : IEvent { public string Key; public int Value; public bool Pending; }
    public struct ItemGet : IEvent { public string Key; public bool Pending; public Action<int> Reply; }
    public struct ItemCommitPending : IEvent { public string Key; public int Value; }
    public struct ItemChanged : IEvent { public string Key; }
    /// <summary>Bay pending của Key vào counter rồi commit sang Current.
    /// Value &gt; 0 → bay đúng Value (clamp theo pending chưa bay); Value &lt;= 0 → bay TẤT CẢ pending chưa bay của Key.
    /// From = điểm xuất phát (null → giữa màn hình).</summary>
    public struct ItemFlyPending : IEvent { public string Key; public Transform From; public int Value; }

    /// <summary>Thu item có animation bay: tự add pending + play animation + commit khi coin tới đích.
    /// Dùng cái này thay vì tự ghép ItemAdd{Pending}+ItemFlyPending (dễ desync pending).</summary>
    public struct ItemCollect : IEvent { public string Key; public Transform From; public int Value; }

    /// <summary>Tiêu item nguyên tử: đủ thì trừ + Reply(true), thiếu thì không trừ + Reply(false).</summary>
    public struct ItemTrySpend : IEvent { public string Key; public int Value; public Action<bool> Reply; }
}
