// WindowTabBase.cs — 一页的公共底座（页签机制那一族的基类）。
// 🔴 **2026-10-13 从 `RewardsWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**，
//    否则建不出脚本资产 ⇒ 场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    判据 / 出处 / 纪律见 `RewardsWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
// 🆕 **2026-10-25（复刻第 3 轮 · 代理 H）：把原版 `WindowTabBase` 的生命周期补齐** ——
//    判据 = `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/WindowTabBase.cs:54-110`（逐方法）：
//      · `baseWindow` / `Window`（:56-58）
//      · `Setup(GameWindow)` = `baseWindow = window; OnSetup();`（:64-68）
//      · `TryOpenTab()` = `gameObject.SetActive(true); OnOpen();`（:74-84）
//      · `CloseTab()` = `gameObject.SetActive(false); OnClose();`（:90-95）
//      · `OnSetup` / `OnOpen` / `OnClose` 三个虚方法（:70-72 / :86-88 / :97-99）
//      · `Toggle(bool)` = `SetActive(option)`（:101-105，private）
//      · `ToFocus()` 虚方法（:107-109）
//    ⚠️ **与原版的差异（如实记）**：
//      ① 原版 `OnClose` 是 `protected virtual`；我们**改成 `public virtual`** —— 因为仓里早有
//         `Shell/FriendsTab.cs:467 public void OnClose()`（社交线先写的）。改成 public 之后它才能 `override`
//         （已同步改成 `public override`），语义不变、可见性只是放宽。
//      ② 原版还有泛型中间层 `WindowTabBase<T> : WindowTabBase`（`Window` 强类型化）。我们没有泛型中间层
//         （仓里全部页签都直接继承本类），`Window` 统一返回基类 `GameWindow`；强类型读口由各页自行转型。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一页。原版叫 `WindowTabBase`（`MissionsTab : WindowTabBase&lt;MainMenuRewardsWindow&gt;`）。</summary>
    public abstract class WindowTabBase : MonoBehaviour
    {
        public abstract WindowTabType Type { get; }
        /// <summary>建内容。**换页时不重建**（原版是 `SetActive` 切换）。</summary>
        public virtual void Setup() { }
        /// <summary>每次切到本页时调（原版 `OnOpen`）。</summary>
        public virtual void OnOpen() { }

        // ======================================================== 原版生命周期（2026-10-25 补）

        /// <summary>原版字段 `baseWindow`（判据 `WindowTabBase.cs:56`）。</summary>
        protected GameWindow baseWindow;

        /// <summary>原版属性 `Window`（`WindowTabBase.cs:58`）。⚠️ 原版泛型中间层返回强类型；我们返回基类。</summary>
        public GameWindow Window { get { return baseWindow; } }

        /// <summary>原版 `Setup(GameWindow)`（`WindowTabBase.cs:64-68`）：记窗口 + 调 `OnSetup()`。
        /// <para>⚠️ 与本类既有的无参 `Setup()` 是**两个东西**：无参那个是「把内容建出来」（我们自己的入口，
        /// 由各窗 `BuildTabContents` 调）；原版 `Setup(GameWindow)` 只做绑定 + `OnSetup` 钩子。</para></summary>
        public virtual void Setup(GameWindow window)
        {
            baseWindow = window;
            OnSetup();
        }

        /// <summary>原版 `OnSetup()`（:70-72）。</summary>
        protected virtual void OnSetup() { }

        /// <summary>原版 `TryOpenTab()`（:74-84）：**先 `SetActive(true)`、再 `OnOpen()`**。</summary>
        public virtual void TryOpenTab()
        {
            if (gameObject == null) throw new System.NullReferenceException();
            gameObject.SetActive(true);
            OnOpen();
        }

        /// <summary>原版 `CloseTab()`（:90-95）：**先 `SetActive(false)`、再 `OnClose()`**。
        /// ⚠️ 原版**不清**窗口的 `CurrentTab` 字段（`GameWindowWithTabs__Close` 也不清）⇒ 藏起来再开回来仍是这一页。</summary>
        public void CloseTab()
        {
            if (gameObject == null) return;
            gameObject.SetActive(false);
            OnClose();
        }

        /// <summary>原版 `OnClose()`（:97-99）。</summary>
        public virtual void OnClose() { }

        /// <summary>原版 private `Toggle(bool)`（:101-105）：只切 `activeSelf`，**不调 `OnOpen/OnClose`**。
        /// ⚠️ 与原版同形（private）；本类内部目前没有调用点 —— 由 `TabButtons` 的按键接线在下一轮使用。</summary>
        void Toggle(bool option)
        {
            if (gameObject != null) gameObject.SetActive(option);
        }

        /// <summary>原版 `ToFocus()`（:107-109）。</summary>
        public virtual void ToFocus() { }
    }
}
