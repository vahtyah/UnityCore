using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace StandardAssets.Tests
{
    /// <summary>
    /// Test khói cho SATypedBus. Cách dùng:
    ///   1. Tạo GameObject rỗng trong scene (Create Empty).
    ///   2. Add Component -> SATypedBus Smoke Test.
    ///   3. Bấm Play, xem Console: mỗi dòng [PASS]/[FAIL]; cuối cùng có tổng kết.
    /// Script này tự định nghĩa event riêng (TestXxx) nên KHÔNG phụ thuộc module nào.
    /// </summary>
    public class SATypedBusSmokeTest : MonoBehaviour
    {
        // ---- Event riêng cho test (struct, không box) ----
        private struct TestPing : ISAEvent { public int Value; }          // pub/sub + priority
        private struct TestAsk : ISAEvent { public Action<int> Reply; }   // request/response
        private struct TestAsync : ISAEvent { }                           // async await

        private int _passed;
        private int _failed;

        private async void Start()
        {
            Debug.Log("===== SATypedBus Smoke Test bắt đầu =====");

            Test1_PubSub();
            Test2_Priority();
            Test3_RequestResponse();
            await Test4_AsyncOrder();
            Test5_Off();

            Debug.Log($"===== KẾT THÚC: {_passed} PASS / {_failed} FAIL =====");
        }

        // 1) Publish -> handler nhận đúng payload định kiểu.
        private void Test1_PubSub()
        {
            int received = -1;
            Action<TestPing> handler = e => received = e.Value;
            SATypedBus.On(handler);

            SATypedBus.Publish(new TestPing { Value = 42 });

            Check("1. Pub/Sub", received == 42, $"nhận được {received}, mong đợi 42");
            SATypedBus.Off<TestPing>(handler);
        }

        // 2) Priority: số âm hơn chạy trước.
        private void Test2_Priority()
        {
            var order = new List<string>();
            Action<TestPing> low  = _ => order.Add("low(-10)");
            Action<TestPing> high = _ => order.Add("high(10)");

            // Đăng ký high trước nhưng priority lớn hơn -> phải chạy SAU.
            SATypedBus.On(high, 10);
            SATypedBus.On(low, -10);

            SATypedBus.Publish(new TestPing { Value = 0 });

            bool ok = order.Count == 2 && order[0] == "low(-10)" && order[1] == "high(10)";
            Check("2. Priority (âm chạy trước)", ok, $"thứ tự = [{string.Join(", ", order)}]");

            SATypedBus.Off<TestPing>(low);
            SATypedBus.Off<TestPing>(high);
        }

        // 3) Request/response: hỏi qua Reply, lấy kết quả ngay (đồng bộ).
        private void Test3_RequestResponse()
        {
            Action<TestAsk> responder = e => e.Reply?.Invoke(7);
            SATypedBus.On(responder);

            int answer = 0;
            SATypedBus.Publish(new TestAsk { Reply = v => answer = v });

            Check("3. Request/Response", answer == 7, $"đáp về {answer}, mong đợi 7");
            SATypedBus.Off<TestAsk>(responder);
        }

        // 4) Async: Publish phải CHỜ handler async xong mới chạy tiếp.
        private async Task Test4_AsyncOrder()
        {
            bool asyncDone = false;
            Func<TestAsync, Task> handler = async _ =>
            {
                await Task.Yield();          // nhường 1 nhịp -> thực sự bất đồng bộ
                await Task.Delay(50);        // giả lập việc tốn thời gian
                asyncDone = true;
            };
            SATypedBus.OnAsync(handler);

            await SATypedBus.Publish(new TestAsync());   // await chuỗi async

            Check("4. Async await", asyncDone, "handler async chưa xong khi Publish trả về");
            SATypedBus.Off<TestAsync>(handler);
        }

        // 5) Off: sau khi huỷ đăng ký thì không nhận nữa.
        private void Test5_Off()
        {
            int count = 0;
            Action<TestPing> handler = _ => count++;
            SATypedBus.On(handler);
            SATypedBus.Off<TestPing>(handler);

            SATypedBus.Publish(new TestPing { Value = 1 });

            Check("5. Off (huỷ đăng ký)", count == 0, $"vẫn nhận {count} lần sau khi Off");
        }

        private void Check(string name, bool ok, string failDetail)
        {
            if (ok) { _passed++; Debug.Log($"[PASS] {name}"); }
            else    { _failed++; Debug.LogError($"[FAIL] {name} — {failDetail}"); }
        }
    }
}
