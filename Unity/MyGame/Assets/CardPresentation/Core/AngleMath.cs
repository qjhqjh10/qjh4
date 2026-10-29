// AngleMath.cs —— **小角度量角器（全仓唯一一份实现）**
//
// ============================ 为什么需要它（不是口味，是硬口径）============================
// `UnityEngine.Quaternion.Angle(a,b)` **不是**连续量角器：它先过 `IsEqualUsingDot` ——
// 本机现读（ILSpy 反编译 `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll`
// 的 `UnityEngine.Quaternion`，2026-10-29 B95_A2B17）：
//   ```csharp
//   public const float kEpsilon = 1E-06f;                                   // :42
//   private static bool IsEqualUsingDot(float dot) { return dot > 0.999999f; }   // :449-452
//   public static float Angle(Quaternion a, Quaternion b)                   // :513-517
//       => IsEqualUsingDot(Min(Abs(Dot(a,b)), 1f)) ? 0f : Acos(·) * 2f * 57.29578f;
//   ```
// ⇒ **夹角 < `2·acos(1 − 1e-6)` = 0.16205695° 时，`Quaternion.Angle` 恒返回【精确的 0】。**
//
// 🔴 **`Quaternion` 的 `==` / `!=` 是同一个病**（`:437-451`：`operator ==` 就是
// `IsEqualUsingDot(Dot(lhs, rhs))`）⇒ 「逐位相同」那一族断言在 0.162° 以内**也判相等**。
// ⇒ 判「转没转 / 归零没有 / 复原没有」时，**一律走本函数**，⛔ 别用 `Quaternion.Angle` / `==` 量小角。
//
// 实测对照（float32 逐位模拟 Unity 语义；真转角 → 读到的值）：
//   | 真值 | `Quaternion.Angle` | 本函数 |
//   |---|---|---|
//   | 0.05° | **0.000000** | 0.050000 |
//   | 0.10° | **0.000000** | 0.100000 |
//   | 0.16° | **0.000000** | 0.160000 |
//   | 0.17° | 0.167859 ← **acos 量化误差 ~0.002°** | 0.170000 |
//   | 5.00° | 4.999970 | 5.000000 |
// （盲区上限 `2·acos(1−1e-6)` = 0.16205695041535692°。）
//
// ============================ 算式 ============================
// `θ = 2·atan2(|xyz|, |w|)`（对相对四元数 `d = a * Inverse(b)`）。取 `|w|` 折掉 `q / −q` 的双覆盖；
// 不设任何 dot 阈值 ⇒ 小角连续、大角与 `Angle` 数值一致（差在浮点末位，实测 5° 处差 3e-5°）。
//
// ============================ 为什么落在【运行时程序集】（`Core/`），不落 `Editor/` ============================
// 调用方里有一条是**生产码**（`Core/CardView.cs` 的「转过却带裁切 ⇒ 出声告警」那道闸），
// 而 `工具/typecheck.sh` / `gen_csc_rsp.py`（`:67-72` / `:110-115`）把 `Assets/` 下**排除 `Editor/`** 的
// `.cs` 编成**运行时程序集**、`Editor/` 另编一个编辑器程序集 ⇒ 编辑器程序集引用得到运行时、**反之不行**。
// ⇒ 唯一实现只能放运行时侧（本文件）；`Editor/BattleScene.cs` / `Editor/B94X1_Assert.cs` /
// `Editor/CardFaceProbe.cs` 各留一个**同名一行转调**（保住那三处的调用点与账落点）。
//
// ⚠️ 历史：本算式此前散成三份（`Editor/BattleScene.cs:48500` · `Editor/B94X1_Assert.cs:342` ·
// `Editor/CardFaceProbe.cs:3208`），2026-10-29 **B95_A2B17（批 2）** 收口到这里，三处改转调。
using UnityEngine;

/// <summary>小角度量角器（度）。与 `UnityEngine.Quaternion.Angle` 的唯一差别：**不设 dot 阈值**
/// ⇒ 0.16205695° 以下不再被读成 0。详见文件头（含本机 `UnityEngine.CoreModule.dll` 的 IL 实证）。</summary>
public static class AngleMath
{
    /// <summary>`a` 与 `b` 之间的夹角（度），**任意小都连续**。
    /// <para>⛔ 别用 `Quaternion.Angle` / `==` 量「该为 0」或「必须完全相等」的场合 —— 它们在 0.162° 以内
    /// 恒判 0 / 判相等（`IsEqualUsingDot(dot) =&gt; dot &gt; 0.999999f`）。</para></summary>
    public static float AngleDegTiny(Quaternion a, Quaternion b)
    {
        Quaternion d = a * Quaternion.Inverse(b);
        return 2f * Mathf.Atan2(new Vector3(d.x, d.y, d.z).magnitude, Mathf.Abs(d.w)) * Mathf.Rad2Deg;
    }
}
