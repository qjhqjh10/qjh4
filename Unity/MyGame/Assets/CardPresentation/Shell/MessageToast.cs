// MessageToast.cs — 外壳（主菜单）侧那条「瞬时提示」（toast）通道。
//
// ============================ 这是什么 ============================
// 原版从**主菜单**里的界面喊一句「一句话、自己会消失」的提示时，走的是 **`UIMessageController`**
// —— 屏幕上方那条**错误横幅**（5 条一池、轮转、入场 0.15s / 停 2.0s / 淡出 0.25s）。
// 本通道 = 把那条通道在**壳**这一侧接起来。⛔ 它**不重新实现**那套横幅。
//
// ============================ 判据（全是实读） ============================
// ① **谁在调它**（这条通道的起因）= `decomp_full/DeckInfoPopup__ShareDeck.c:16-24` 三句：
//    ① `MakeDeckString()` → ② `UnityEngine.GUIUtility.set_systemCopyBuffer(串)` →
//    ③ `UIMessageController.ShowMessage(instance, <词条键>, 1, 0)`。
//    🔴 **尾参那个 `0` = IL2CPP 的 `MethodInfo*`，不是第 4 个实参**（同族铁证：`__ShowError.c` 只有
//    一句 `ShowMessage();`，而 `DeckEditingWindow__OnCardClick.c:27` / `__CheckCardDrag.c:92` /
//    `DeckInfoPopup__EditDeck.c:40` 三处调 `ShowError` 都写成 `(instance, 串, 0, 0)` —— 第 3 个 `0`
//    就是 `localize`，第 4 个 `0` 恒是 `MethodInfo*`）⇒ **公开重载是 `ShowMessage(string, bool)`**
//    （`dump.cs:104628`），`localize = 1`；带 `Color` 的那一条是 **private**
//    （`dump.cs:104632  private void ShowMessage(string messageToShow, bool localize, Color color)`）。
//    ⇒ **色档 = `messageColor`**（场景值 `(1,1,1,1)` 白），**不是** `errorColor`（`ShowError` 才用它）。
// ② **原版有两个实例、同一个类**（全库 14 份，`assets_full/<bundle>/GameObject/
//    UI Error Message Controller (MUST BE ENABLED).json`）：
//    **13 个 `battlearena*`** + **`bundle_scenes_scenes_mainmenuwarpforge`**（= 主菜单）。
//    ⇒ 本件在壳里建的那一颗，就是原版主菜单里那一颗的同族。
// ③ **主菜单里它的父节点 = `Safe area Only Horizontal`**（`bundle_scenes_scenes_mainmenuwarpforge`：
//    `RectTransform_1206`(控制器根) 的 `m_Father = 1540` = `Safe area Only Horizontal`；
//    1540 → `MainMenu_38`(1114) → `Main  Canvas`(1514) → `Game UI`(1314)，四层全是 stretch 满屏、
//    `ap = (0,0)` ⇒ **横幅上缘贴安全区顶**）。本件按它挂。
// ④ 🔴 **两场景的控制器子树【不是】逐字段相同**（2026-10-09 现读订正）。
//    ⚠️ 本文件初版与 `资料/普查产出_第八会话/E7_弹窗星号与toast.md:110` 都写「**两场景的控制器子树
//    逐字段相同，只有根自己的 `m_AnchoredPosition` 不同**」——**那句话只对【根 / 容器 / 时序 MB】三处成立**，
//    条目与文字那两颗**两档不同**（逐条实读见 `Battle/ErrorMessageBanner.cs` 文件头 ⑪ 那张表）：
//    · **根 `m_AnchoredPosition`**：战斗 `(0, −213.5997314453125)` · 菜单 `(−0.00010299999848939478, 0.0)`
//      （`RectTransform_3373` / `RectTransform_1206`；**`sd` 两档相同** = 1920.699951171875 × 336.6400146484375）；
//    · **item `sd`**：战斗 `1439.1600341796875 × 80`（`RT_2823`）· 菜单 `1310 × 50`（`RT_1208`）
//      —— 🔴 **高那一格 = 布局的堆叠步长**（容器 `MB 4106`/`MB 2086` 都是 `m_ChildControlHeight = 0`
//      ⇒ uGUI 取**子件自己的 `sizeDelta`**：`LayoutGroup.GetChildSizes` 的 `if (!controlSize)` 那一支）；
//    · **文字 `m_fontSize`**：战斗 `60`（`MB_3740`）· 菜单 `36`（`MB_1740`）；文字 `sd.y`（= 单行高）：
//      `56.849998474121094`（`RT_2942`）· `34.11000061035156`（`RT_1207`）；
//    · **`Background` `sd`**：`870.97998046875 × 60.849998474121094`（`RT_2989`）· `618.5899658203125 × 38.11000061035156`（`RT_1204`）；
//    · 容器四件（`RT_1205` = `RT_3154`）· item 存档 `ap (960.3499755859375, −241.29000854492188)`
//      （`RT_1208` = `RT_2823`）· 四段时长（`MB_2087` = `MB_4710`）**两档逐位相同** ✔。
//    🔴 **硬旁证**：菜单那条 item 的**存档 `ap.y = −241.29000854492188` 正是 h = 50、N = 1 时布局算法
//      会写出来的那个值**（`−(266.2900085449219 − 50) − 50 × 0.5`）；若 h = 80 该是 `−226.29`。
//      而且菜单那份 item 的 GO 是 **`m_IsActive = true`**（战斗那份的模板是 `false`）⇒ 菜单场景里
//      **布局是真跑过的**，存档值就是它的输出 ⇒ 菜单档项高**确实**是 50。
//    ⇒ **条心（自上而下）**：战斗 `213.5997314453125 + 336.6400146484375 − 40` = **510.23974609375** px ·
//      菜单 `0 + 336.6400146484375 − 25` = **311.6400146484375** px
//      （= `档.根上缘 + RootH − 档.项高/2`）。
//      ⚠️ 上一版写的「菜单 = `0 + 226.2897… = 296.63974609375`」**是按项高 80 算的**（＝拿战斗档的项高
//      去套菜单档）⇒ 少了 `ItemH/2 = 15 px`，已作废。
// ⑤ **`m_IsActive`**：13 个战场里是 `true`；**`mainmenuwarpforge` 那份是 `false`**。
//    ⇒ ⚠️ **仍然没查清**（2026-10-09 又看了一遍，新增一条旁证但**仍推不出解释**）：
//      `UIMessageController.Instance` 是 `Awake` 里赋的静态单例（`__Awake.c` 单例守卫 +
//      `__set_Instance`），场景里 `act=F` 的 GO **不跑 `Awake`**；而主菜单侧有一大票脚本在读
//      `Instance`（`DeckInfoPopup__{ShareDeck,EditDeck,__DeleteDeck_b__27_3}` ·
//      `DeckEditing{Panel__AddCard,Window__OnCardClick,Window__CheckCardDrag}` ·
//      `DuelPopupWindow__CanPlay` · `ProfileTab…__Initialize_b__0` ·
//      `ExpansionPassProgressClaimButtonController__BuyPremiumClick` —— 全库 14 个文件，逐个 `grep` 过），
//      而且 `DeckInfoPopup__ShareDeck.c:21` 对 null 走的是**抛异常**那一支（`FUN_1803f47a0`）⇒ 运行时
//      `Instance` 必然非 null ⇒ 那颗 GO 在运行时必须是活的。
//      🆕 **新旁证（只解释了一半）**：菜单那份 **item 子件的 `m_IsActive` 是 `true`**（战斗是 `false`），
//      而它的**存档 `ap.y` = h = 50 时布局的输出** ⇒ **编辑器里布局跑过** —— 布局只对**激活**的子树跑，
//      ⇒ 「根曾经是活的、后来被关掉（而布局写下的值留在节点上）」**与资源自洽**，
//      但它**说不出运行时是谁把它打开的**。⛔ 本地没有主菜单的实况 dump ⇒ **不猜**：
//      如实记「本地资源与代码推不出这一格的解释」。
//
// ============================ 🔴 本件复用哪一件 ============================
// **复用 `Battle/ErrorMessageBanner.cs`**（= 原版 `UIMessageController` 的整件实现：5 条一池 / 轮转 /
// 入场 0.15s / 停 2.0s / 淡出 0.25s / 底图 `40k_bt_underbutton`）。本文件**只做外壳侧的宿主**
// —— 挂到哪、谁泵、怎么取、**取哪一档**。理由 = 本项目铁律「两处写同一条规则 = 迟早不一致」：
// 原版**只有这一个类**，再造一份实现就是第二份。
//
// ✅ **那 213.6 px 的位置偏离已消掉**（2026-10-09）：`ErrorMessageBanner` 现在有
// **场景档** `LayoutPreset`（根上缘 / 项高 / 参考底条 / 字号 / 单行高六项），
// `BattlePreset`（= 13 个战场那一档、也是**加档之前那一档**）与 `MenuPreset`（= 主菜单那一档）。
// 本件在建件时**显式传 `MenuPreset`**；战场侧（`BattleDriver.BuildErrorBanner`）**不传** ⇒ 战斗档，
// **行为一字未动**（判据与那张表 → `Battle/ErrorMessageBanner.cs` 文件头 ⑪）。

using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 外壳侧的**瞬时提示（toast）**通道 —— 宿主是 <see cref="ErrorMessageBanner"/>（= 原版
    /// `UIMessageController`），**取主菜单那一档**（<see cref="ErrorMessageBanner.MenuPreset"/>）。
    /// 判据、复用的理由 → 本文件头。
    /// <para>用法：<c>MessageToast.Show("MenuDeck/Share/ExportSuccesful", localize: true,
    /// fallback: 兜底句)</c>。⛔ 生产路径**只走这一个口** —— 别在调用点自己 `Create` 一颗横幅。</para>
    /// <para>⚠️ **2026-10-09（`P1` · `A1153`）**：分享那条路**已经不再是这个写法** —— 那个词条的值现在是
    /// **带 `{0}` 的模板**，调用点先判 `Loc.HasEntry` 两态、再 `string.Format(Loc.T(键), s.Length)`
    /// （见 `Shell/DeckInfoPopup.cs` 分享那一处）。上面这行只在**值不带占位符**的词条上照旧成立。</para>
    /// </summary>
    public class MessageToast : MonoBehaviour
    {
        /// <summary>节点名**照原版**（`assets_full` 里那 14 份 GO 的名字）——
        /// 自检按它找，⛔ 别改名。</summary>
        public const string NodeName = "UI Error Message Controller (MUST BE ENABLED)";

        /// <summary>原版那份的父节点名（主菜单里就是它，判据 → 本文件头 ③）。</summary>
        public const string ParentNodeName = "Safe area Only Horizontal";

        /// <summary>壳里的那一颗（没建过 ⇒ null）。</summary>
        public static MessageToast Instance { get; private set; }

        ErrorMessageBanner _banner;

        /// <summary>底下那件横幅（自检口；⛔ 生产路径别拿它绕过本类去开第 2 颗）。</summary>
        public ErrorMessageBanner Banner { get { return _banner; } }

        /// <summary>壳里这颗用的是**哪一档**（自检要能分辨「取的是菜单档、不是战斗档」，判据 → 文件头 ④）。</summary>
        public ErrorMessageBanner.LayoutPreset Preset
        {
            get { return _banner != null ? _banner.Preset : ErrorMessageBanner.MenuPreset; }
        }

        /// <summary>**只亮一条时**那一条的条心（自上而下 px）—— 外壳这一档应当是 **311.6400146484375**
        /// （转发 <see cref="ErrorMessageBanner.SingleItemCenterYpx"/>；没建件 ⇒ `NaN`）。
        /// ⛔ 别拿档里的存档 `ap.y`（241.29）当它 —— 那是存档值，不是布局算出来的位置（文件头 ④）。</summary>
        public float SingleItemCenterYpx
        {
            get { return _banner != null ? _banner.SingleItemCenterYpx : float.NaN; }
        }

        /// <summary>建出来了没有（`Create` 跑过 = 池子建好了）。</summary>
        public bool Built { get { return _banner != null && _banner.ItemCount > 0; } }

        /// <summary>这一颗一共弹过几条（转发 <see cref="ErrorMessageBanner.ShowCount"/>）。</summary>
        public int ShowCount { get { return _banner != null ? _banner.ShowCount : 0; } }

        /// <summary>最近一条**真正喂给 Label 的**文案（转发；没弹过 ⇒ null）。</summary>
        public string LastShownText { get { return _banner != null ? _banner.LastShownText : null; } }

        /// <summary>最近一条的**原始**入参（= 词条键或明文，转发；没弹过 ⇒ null）。</summary>
        public string LastRawText { get { return _banner != null ? _banner.LastRawText : null; } }

        // ==================================================================
        //  建件
        // ==================================================================

        /// <summary>取（没有就建）壳里那一颗。**幂等**。
        /// <para>父节点：① 先找 `parent`（或壳根）下的 <see cref="ParentNodeName"/>（**照原版**，
        /// 见本文件头 ③）；② 没有那个节点 ⇒ 退回 `parent` 本身；③ `parent` 也没有（没壳、也没人
        /// 调过 `WindowsManager.EnsureHost`）⇒ 建在场景根上**并出声**（⛔ 不静默）。</para></summary>
        public static MessageToast Ensure(Transform parent = null)
        {
            if (Instance != null) return Instance;

            Transform host = parent;
            if (host == null && ShellRuntime.Instance != null) host = ShellRuntime.Instance.transform;
            if (host == null && WindowsManager.Instance != null) host = WindowsManager.Instance.transform.parent;

            Transform where = host;
            if (host != null)
            {
                // 只有**直接子件**会被找到（`Transform.Find` 的语义）—— `Safe area Only Horizontal`
                // 就是壳根的直接子件（`ShellRuntime.Build` 里 `safeH.SetParent(root, false)`）。
                var safe = host.Find(ParentNodeName);
                if (safe != null) where = safe;
            }
            if (where == null)
                Debug.LogWarning($"[MessageToast] 场景里既没有壳（`ShellRuntime`）也没有 `WindowsManager` ⇒ "
                               + $"`{NodeName}` 建在**场景根**上（没有父节点 ⇒ 它跟着当前场景走、不跨场景）。");

            var go = new GameObject(NodeName, typeof(RectTransform));
            go.transform.SetParent(where, false);
            var t = go.AddComponent<MessageToast>();
            Instance = t;                     // 🔴 显式登记 —— 编辑模式（自检）**不跑 `Awake`**
            // 🔴 **取【主菜单】那一档**（原版主菜单里那颗就是这个尺寸/位置；判据 → 文件头 ④，
            //    档的定义 → `Battle/ErrorMessageBanner.cs` 文件头 ⑪）。⛔ 别写成不传档 ——
            //    不传 = 战斗档（条心 510.24，比主菜单那颗低 198.6 px）。
            t._banner = ErrorMessageBanner.Create(t.transform, ErrorMessageBanner.MenuPreset);
            if (t._banner == null)
                Debug.LogError("[MessageToast] `ErrorMessageBanner.Create` 没交出横幅 —— 提示通道不可用（⛔ 不许静默）");
            return t;
        }

        // ==================================================================
        //  生产口
        // ==================================================================

        /// <summary>弹一条提示。
        /// <para>色档 = `messageColor`（白）—— 原版这条链走的就是 `ShowMessage` 那一档（本文件头 ①）。</para></summary>
        /// <param name="text">文案；<paramref name="localize"/> 为真时它是**词条键**。</param>
        /// <param name="localize">过不过词条表（原版 `ShowMessage(键, localize: 1)` ⇒ 传真）。</param>
        /// <param name="fallback">键不在表里时用的整句（⛔ 别留空：空串 + 表里没键 = 界面上印键名）。
        /// 表里有键时这一项**不被使用**（判据与两态行为 → `ErrorMessageBanner.ShowMessage`）。</param>
        /// <returns>弹出去了没有（没建出来 / 空串 ⇒ false）。</returns>
        public static bool Show(string text, bool localize = false, string fallback = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                // 原版 `ShowMessage` 收空串也是 `LogError` + return（`__ShowMessage.c` 第 1 句）
                Debug.LogError("[MessageToast] `Show` 收到空串 ⇒ **什么都没弹**（出声，不静默）");
                return false;
            }
            var t = Ensure();
            if (t == null || t._banner == null) return false;
            t._banner.ShowMessage(text, ErrorMessageBanner.MessageColor, localize, fallback);
            return true;
        }

        // ==================================================================
        //  泵
        // ==================================================================

        /// <summary>推进 `dt` 秒（转发 <see cref="ErrorMessageBanner.Advance"/>）。
        /// 🔴 **批处理里没有帧循环 ⇒ 自检必须自己调它**（`Step` 就是为此存在的口）；真机由 `Update` 喂。</summary>
        public void Step(float dt)
        {
            if (_banner != null) _banner.Advance(dt);
        }

        void Update()
        {
            // 真机每帧推一次（同 `BattleDriver.Update` 里喂 `AdvanceTimeline` 那一档）。
            Step(Time.deltaTime);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ==================================================================
        //  自检口（⛔ 生产路径不碰）
        // ==================================================================

        /// <summary>把这一颗清掉（连带节点），`Instance` 归 null —— 只为自检「从零再建」用。</summary>
        public static void DisposeForTest()
        {
            if (Instance == null) return;
            var go = Instance.gameObject;
            Instance = null;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }
}
