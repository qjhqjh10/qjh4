// ProbeTab.cs — `ShellScene` 的自检探针页（`WindowTabBase` 的最小子类，只记账、不建内容）
//
// 🔴 **2026-10-27（第八十五会话 · 代理 D5SH）从 `Editor/ProbeTab.cs` 搬到运行时代码** ——
//    原因：`AddComponent<T>()` **不能挂编辑器程序集里的 MonoBehaviour**。`ShellScene.Run` 的
//    ④ 段（`Editor/ShellScene.cs` 的「页签生命周期」）用 `new GameObject(…).AddComponent<ProbeTab>()`
//    造两条夹具页，而本类当时在 `Editor/` 目录下 ⇒ 属于 `Assembly-CSharp-Editor` ⇒ Unity 直接拒绝：
//      `Can't add script behaviour 'ProbeTab' because it is an editor script.
//       To attach a script it needs to be outside the 'Editor' folder.`
//    ⇒ 返回 **null** ⇒ 下一句 `a.type = …` NRE ⇒ 崩溃点之后的断言（72 处调用点）**一次都没跑过**、
//      宿主合计也从未打印。实据 = `_tmp_view/wf_ShellScene.Run2.log:219555`。
//    ⇒ 修法 = 搬到 `CardPresentation/Shell/`（运行时程序集）：MonoBehaviour 必须在**非 `Editor/`** 目录。
//      ⚠️ 同时**必须 `public`** —— 编辑器程序集（`ShellScene` 那边）要能看见它，`internal` 跨程序集不可见
//      （旧版在 `Editor/` 里默认可见，是因为两个类同处 `Assembly-CSharp-Editor`）。
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）原来从 `Editor/ShellScene.cs` 的嵌套类拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；**嵌套类 / 异名文件里的 MonoBehaviour 建不出资产**
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`；根因与先例见 `Shell/WindowTabBase.cs` 文件头（A1506）。
//    ✅ 本文件**类名 == 文件名**，也满足那条（现在两个条件同时成立：非 Editor 目录 + 同名文件）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>🆕 2026-10-25（复刻第 3 轮）：页签生命周期的**自检探针页**。
    /// 只记账、不建内容 ⇒ 可以在批处理里安全地 `AddComponent`（不碰美术/数据）。
    /// ⚠️ 2026-10-27 起在运行时程序集（`public`）；唯一的实例化处 = `Editor/ShellScene.cs` 的 ④ 段自检夹具。</summary>
    public sealed class ProbeTab : WindowTabBase
    {
        public WindowTabType type;
        public int setups, opened, closed, focused;
        public override WindowTabType Type { get { return type; } }
        public override void Setup(GameWindow window) { setups++; base.Setup(window); }
        public override void OnOpen() { opened++; }
        public override void OnClose() { closed++; }
        public override void ToFocus() { focused++; }
    }
}
