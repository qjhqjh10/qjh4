// MainMenuWindow.cs — **常驻主菜单那一扇窗**（原版 `MainMenuWindow : GameWindow`）。
//
// 🔴 **2026-10-12（`A1629`）新建** —— 起因是用户 2026-10-12 的三句话：
//   「打开别的界面的时候，主界面的那些模式卡应该看不到才对」·「很多地方的界面好像是没有层级一样，
//     按照顺序依次打开不同地方的时候它们的界面相互重叠」·「看界面要看全图」。
//   根因 = **我们的主菜单不是 `WindowsManager` 的窗** —— `Shell/WindowsManager.cs` 的
//   `ShowPreviousWindow` 里原本白纸黑字写着「🔴 **我们没有常驻 `baseMenu`（第 2 步）** … 这一步**什么都不做**」。
//
// ============================================================ 原版判据（全部本地可复现）
//
// ① **主菜单把自己登记进窗口栈**（`d:/2/tools/decomp_full/MainMenuWindow__Initialize.c` 末尾两句）：
//      `WindowsManager.Instance.SetBaseMenu(this);` → `WindowsManager.Instance.OpenWindow(this);`
//    ⇒ 「常驻」不是「一直在场」，而是**它在 `openWindows` 里、并且被记成 `baseMenu`**。
// ② 那一扇的序列化字段（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1927.json`；
//    它 `m_Script.m_PathID = 6585963289051879589` 对得上 monoscript 里的 `MainMenuWindow`，逐字段实读）：
//      `type = 0`(**Fullscreen**) · `windowsPlacement = 0`(**None**) · `closeOnESC = 0` ·
//      `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0` · `priority = 2`
//    ⚠️ `windowsPlacement = 0`(None) 是**合法值**，语义 = **「不挂 Holder」** —— 这一扇的父级是它自己那棵树，
//      不归三个锚点管（`WindowsManager.RegisterAnchor` 那条报错文案里早就写着这句，但 `GetWindowAnchor`
//      从前把它当缺陷报错 ⇒ 本笔一并订正，见那边的注释）。
// ③ **它到底是哪一棵**（同一份实读：`m_GameObject.m_PathID = 7`，逐级解 `m_Father` 得到）：
//      `Game UI` → `Main  Canvas` → `MainMenu`(pid **38**) → **`MainMenu`(pid 7 = 这一扇)**
//         ├ `Background`
//         └ `Safe area All` → `GameModes` → `Viewport` → `Content` …      ← **模式卡**
//      而 `Upper bar`(顶栏) · `Navigation Panel`(左导航) · `ChatPreview` · 三个 `WindowHolder`
//      是 pid 38 那棵下的**兄弟**（不是这一扇的孩子）
//      ⇒ **顶栏与左栏不会跟着它一起消失** —— 正是用户那张原版实拍
//        （`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`）里的样子：开子界面时顶栏还在、
//        左导航栏与全部模式卡一个都看不见。
// ④ **藏与放**（两条都是逐句读出来的）：
//      · `MainMenuWindow.ToBackground()`（`MainMenuWindow__ToBackground.c`）：
//          `if (state == Background) return;` → `screenBaker.Toggle(true)` → `base.ToBackground()`（state = 2）
//          → **`gameObject.SetActive(false)`**
//      · `MainMenuWindow.ToFocus()`（`MainMenuWindow__ToFocus.c`）：
//          **`gameObject.SetActive(true)`** + `NavigationPanelController.Instance.Toggle(true, true)`（左栏放回来）
//          + `scrollList` 刷一次
// ⑤ 于是原版整条链是（**我们照做的就是这一条**）：
//      开全屏窗 ⇒ `OpenWindow` 的 `HideAllWindows()` 把「还开着」的全 `Hide()` 掉 —— **主菜单也在表里 ⇒ 一起被藏**；
//      开弹窗   ⇒ `currentWindow.ToBackground()`（主菜单被压到背景 ⇒ `SetActive(false)`）；
//      关掉上面那扇 ⇒ `ShowPreviousWindow()` 里 `openWindows` 空了 ⇒ **`OpenWindow(baseMenu)` 把它开回来**。
//
// ============================================================ 我们这边怎么挂
//
//   `Shell/MainMenuRuntime.Build()` 建完「背景 + 模式卡」之后，在本文件的类上登记：
//     `AttachToAnchor`（None ⇒ 不换父）→ `wm.SetBaseMenu(win)` → `wm.OpenWindow(win)`。
//   「背景」与「模式卡」都建在**这一扇的物体底下**（`MainMenuRuntime.Build` 里那个
//   `new GameObject("MainMenu")` 子节点），所以 `SetActive(false)` 一次就够。
//
// ⚠️ **两处如实标注（铁律 3，不实现就别写成实现了）**：
//   · `screenBaker`（`UIBakeScreen.Toggle`，原版给主菜单做离屏烘焙的那个优化）**本仓没有对应物** ⇒ 不实现；
//   · `priority`（`InitializationPriority`）本仓没有初始化管线 ⇒ 不实现，原版值只记在 ② 里。
//   ⛔ 别把这两条写成「照原版做了」。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>常驻主菜单那一扇 —— **原版 `MainMenuWindow` 的等价物**。
    /// 它**不铺任何内容**：那一棵树是 `Shell/MainMenuRuntime.cs` 建的，本类只负责
    /// **把自己放进窗口栈**（`SetBaseMenu` + `OpenWindow`）并按原版覆写「压到背景」那一拍。
    /// 判据全文（含引用的每一份 .c / .json）→ 本文件头。</summary>
    public class MainMenuWindow : GameWindow
    {
        /// <summary>原版 `MainMenuWindow.ToBackground()` —— **覆写基类那一句「只写状态、不关物体」**。
        /// <list type="number">
        /// <item>`if (CurrentState == Background) return;`（原版首句，逐字）；</item>
        /// <item>`base.ToBackground()` = `CurrentState = Background`；</item>
        /// <item>**`gameObject.SetActive(false)`** = 背景 + 模式卡整棵关掉。</item>
        /// </list>
        /// <para>⚠️ **为什么必须覆写**：基类那一份（`GameWindow.ToBackground`）**不关物体** ——
        /// 原版对**别的**窗也是这样（指针层那条注释：`ToBackground()` 只写状态），
        /// **只有主菜单这一族**才顺手把物体关掉（因为它那棵树是「整个主界面」）。
        /// 少了这一句 ⇒ 开弹窗（`type = Popup`，如练习窗）时主菜单**照样露在外面** ——
        /// 用户 2026-10-12 实拍 `_tmp_view/s21_05_practice.png` 就是这一态
        /// （练习窗底下透出模式卡，那行「游戏模式」还和壳的 `DRAFT MODE` 逐字重叠）。</para>
        /// <para>**回来的那一半**不需要覆写：`TryOpen` 的 `Background` 支本来就 `gameObject.SetActive(true)`
        /// （`WindowsManager.GameWindow.OpenByState` 第 ③ 支），`Closed` 支也有一句 —— 两条路都盖得住。</para></summary>
        public override void ToBackground()
        {
            if (CurrentState == WindowState.Background) return;   // 原版首句（幂等）
            base.ToBackground();                                  // = `CurrentState = Background`
            gameObject.SetActive(false);                           // 原版末句：整棵关掉
        }

        /// <summary>原版 `MainMenuWindow.ESCPressed()` = `base.ESCPressed(); SettingsMenu.ExitGamePopup();`
        /// （`d:/2/tools/decomp_full/MainMenuWindow__ESCPressed.c`，**全 body 就两句**）。
        /// 而 `base` 那一跳在**这一扇上什么都不做** —— 它 `closeOnESC = 0`（见文件头 ②）⇒ 门槛不过。
        /// 所以等价物就是「叫出退出游戏弹窗」，那一条**早已实现**在 `MainMenuRuntime.EscapePressed()`
        /// （判据、三个文案键、退出动作的出处都在那个方法的 doc 里，⛔ 别在这儿抄第二份）。
        /// ⚠️ **返回值口径**：原版 `void`、本仓 `bool`（=「这一下有没有被用掉」，唯一调用点
        /// `PointerLayer.KeyCancel` 要用），与 `MainMenuRuntime.EscapePressed` 同一条口径。</summary>
        public override bool ESCPressed()
        {
            // 原版第一句 `base.ESCPressed()` 在 `closeOnEsc == 0` 下是空转 ⇒ 不出声、不调它
            //（调了反而会因「输入层停用」那道门槛打一行与事实不符的日志）。
            return MainMenuRuntime.EscapePressed();
        }
    }
}
