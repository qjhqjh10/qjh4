// ProbeTab.cs — `ShellScene` 的自检探针页（**本是它的嵌套类**，2026-10-26 拆成顶层类）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `Editor/ShellScene.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；**嵌套类 / 异名文件里的 MonoBehaviour 建不出资产**
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`；根因与先例见 `Shell/WindowTabBase.cs` 文件头（A1506）。
//    ⚠️ 本块**逐字搬**，未改一字。**唯一的形态变化**：原为 `ShellScene` 的**嵌套私有**类，
//       顶层类不能声明 `private` ⇒ 现在是**文件内可见（internal）**。调用点只有 `ShellScene` 自己
//       （本文件夹 5 处），语义等价。
using CardPresentation;

/// <summary>🆕 2026-10-25（复刻第 3 轮）：页签生命周期的**自检探针页**。
/// 只记账、不建内容 ⇒ 可以在批处理里安全地 `AddComponent`（不碰美术/数据）。</summary>
sealed class ProbeTab : WindowTabBase
{
    public WindowTabType type;
    public int setups, opened, closed, focused;
    public override WindowTabType Type { get { return type; } }
    public override void Setup(GameWindow window) { setups++; base.Setup(window); }
    public override void OnOpen() { opened++; }
    public override void OnClose() { closed++; }
    public override void ToFocus() { focused++; }
}
