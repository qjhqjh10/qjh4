// VarsGlobalTypes.cs — `VarsGlobal` 两条展示位公式的**返回值类型**（⛔ 不引 UnityEngine）。
//
// 🔴 为什么单独建这两个 struct（2026-10-27）：`RuleEngine/Core/` 只许碰 `UnityEngine.Debug` / `Random`
//   —— `RuleEngineTest.TestCoreLayerPurity`（`Editor/RuleEngineTest.cs:29595`，A941 / 2026-10-18）守这条线，
//   理由是 `工具/ruleprobe` 只编 `Core/*.cs` + 一份极简桩（`工具/ruleprobe/Stub.cs`）在 net8 下跑真解析器：
//   `Core/` 一旦引 Unity 的值类型，`check` / `play` / `ai` 全部 `CS0234` 编不过。
//   `GameplayVariables.VarsGlobal` 的两条公式原来返回 `UnityEngine.Vector3` / `Vector2` ⇒ 破了这条线。
//   判据 → `资料/完全复刻/报告/D1_ruleengine两条红.md` §2·2。
//
//   ⚠️ **语义逐字等价，不是「自己设计的行为」**：只是换掉一个 Unity 值类型，算术仍只在
//   `GameplayVariables.VarsGlobal` 里（⛔ 别往这里加运算符/换算 —— 那不是「形状」）。
//   按铁律 18 ⛔②「命名/结构不同但语义一致」⇒ 不打 `🔶 自己设计` 标。
//
//   🔑 名字自带单位（本仓被「14.835 px/世界单位 vs 108 px/单位」坑过）：
//     · `PxOffset`   = **设计像素**位移（1080p 基准 · y 向下为正，同 `LayoutSpace.FromPixel`）
//     · `WorldPoint` = **世界坐标**（Unity 世界单位）
namespace RuleEngine
{
    /// <summary>屏幕位移，单位 = **1080p 设计像素**（y 向下为正，同 `LayoutSpace.FromPixel`）。</summary>
    public struct PxOffset
    {
        public float x;
        public float y;
        public PxOffset(float x, float y) { this.x = x; this.y = y; }
    }

    /// <summary>世界坐标（Unity 世界单位）。</summary>
    public struct WorldPoint
    {
        public float x;
        public float y;
        public float z;
        public WorldPoint(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
}
