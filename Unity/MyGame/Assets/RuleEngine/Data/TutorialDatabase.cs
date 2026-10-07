// TutorialDatabase.cs — 教程关卡数据的 **Unity 侧装载器**（`Resources` + `JsonUtility`）
//
// 来历（2026-10-18，A941）：原来这一跳写在 `Core/TutorialScript.cs` 里，用 `#if UNITY_5_3_OR_NEWER`
//   把 `Resources.Load` / `JsonUtility.FromJson` 包起来 —— **编得过，但破了本工程的成文规矩**：
//   **`Core/` 只许碰 `UnityEngine.Debug` / `UnityEngine.Random`**
//   （判据 = `Data/CardDatabase.cs` 文件头；`工具/ruleprobe` 就靠这条，用一份极简桩把 `Core/**`
//    在 net8 下单独编起来跑真解析器）。⇒ 搬到本文件（与 `CardDatabase` / `DeckStore` 同族）。
//
// 两边怎么接上（**这半页是唯一的说明，改之前先读它**）：
//   · 本文件与 `Core/TutorialScript.cs` 编在**同一个程序集**（本工程没有 asmdef）
//     ⇒ `TutorialData` 的 `partial` 两个半边都在 ⇒ `LoadFromUnity` 有实现
//     ⇒ `TutorialData.Stages` 第一次被读时真去 `Resources` 装载。
//   · **离屏探针**（`工具/ruleprobe`）只编 `Core/*.cs`，**本文件不在编译集里**
//     ⇒ `LoadFromUnity` 只有声明 ⇒ C# 把声明与调用一起抹掉 ⇒ 走 `TutorialData.Load()` 里的
//       「出声 + 空数组」兜底（= 老 `#else` 那条路），探针照旧能编能跑。
//   ⛔ 所以：**别把本文件挪进 `Core/`**，也**别在 `Core/` 里写回 `Resources` / `JsonUtility`**
//      （`Editor/RuleEngineTest.cs` 的 `TestCoreLayerPurity` 有一条源文扫描断言盯着 `Core/*.cs`）。
using System;
using UnityEngine;

namespace RuleEngine
{
    /// <summary>
    /// `Assets/RuleEngine/Resources/tutorial_stages.json` → **纯数据**（DTO 在 `Core/TutorialScript.cs`，
    /// 字段名逐字照产物；`工具/gen_tutorial_stages.py` 生成，**别手改那个 json**）。
    /// ⚠️ 失败一律**出声 + 返回空数组**（⛔ 不抛异常、⛔ 不返回 null 让调用方猜）。
    /// </summary>
    public static class TutorialDatabase
    {
        /// <summary>从 `Resources` 读 6 关。返回的一律非 null（失败时是**空数组**）。</summary>
        public static TutorialStageData[] LoadFromResources()
        {
            // ⚠️ 路径只有一处（`TutorialData.StagesResourcePath`）——`BattleDriver` 的报错文案也用那一份。
            var asset = Resources.Load<TextAsset>(TutorialData.StagesResourcePath);
            if (asset == null)
            {
                Debug.LogError("[Tutorial] 找不到 `Resources/" + TutorialData.StagesResourcePath + ".json` —— "
                    + "跑一下 `\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_tutorial_stages.py`");
                return new TutorialStageData[0];
            }
            var f = JsonUtility.FromJson<TutorialStageFile>(asset.text);
            if (f == null || f.stages == null || f.stages.Length == 0)
            {
                Debug.LogError("[Tutorial] `" + TutorialData.StagesResourcePath + ".json` 解出来是空的"
                    + "（format=" + (f == null ? -1 : f.format) + "）—— 教程关一关都开不了");
                return new TutorialStageData[0];
            }
            return f.stages;
        }
    }

    /// <summary>
    /// `TutorialData` 的 **Unity 半边**（另半边在 `Core/TutorialScript.cs`，那边是纯数据 + 闸门/执行器）。
    /// 🔴 这里只干一件事：把 `Data/` 层装出来的数据 `Install` 回 `Core/` 那一半
    ///    —— 见 `Core/TutorialScript.cs` 的 `TutorialData` 类头注那张「谁是哪一半」的表。
    /// </summary>
    public static partial class TutorialData
    {
        /// <summary>`Core/TutorialScript.cs` 里那个 `static partial void LoadFromUnity();` 的实现。
        /// ⚠️ 装载失败时**照样要 `Install(空数组)`** —— 留 `_stages == null` 的话，
        ///    `Core` 那半边的 `Load()` 会再报一次「这个环境里没有 Resources」，把上面那条
        ///    真正的病因（文件缺失 / json 空）淹掉。</summary>
        static partial void LoadFromUnity()
        {
            Install(TutorialDatabase.LoadFromResources());
        }
    }
}
