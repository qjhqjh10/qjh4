// ErrorMessageBanner.cs — 原版 **`UI Error Message Controller (MUST BE ENABLED)`**
// （`UIMessageController` TypeDefIndex 2762 + `UIErrorMessageItem` 2761）**整件**（§8b · 2b，2026-10-18）
//
// ============================ 这件是干什么的 ============================
// 屏幕上方那一条**错误横幅**：底条（`40k_bt_underbutton` 九宫格）＋ 一行暗红文字，
// **5 条一池、轮转复用**；每条 = 缩放 0.3×→1× 入场（0.15 s）→ 停 2.0 s → 淡出 0.25 s（淡完关自身）。
// 战斗里的**唯一触发** = 疲劳（`Battle/Tips/DamageFatigue{,Enemy}`，见 `BattleDriver.NoteFatigueBanner`）。
//
// ============================ 判据（全是实读） ============================
// ① **类 / 字段 / 方法表** = `d:/2/tools/il2cpp_out/dump.cs:104535`（`UIErrorMessageItem`）与 `:104584`
//    （`UIMessageController`）：
//    · `UIMessageController`：`NUMBER_OF_MESSAGES = 5`（const）· `errorMessageItemRef +0x20` ·
//      `messageColor +0x28`(白) · `errorColor +0x38` = **(0.8,0,0,1)** · `currentMessageIndex +0x48` ·
//      `messageItems +0x50`(List)；方法 `Awake` / `ShowMessage` / `ShowError`。
//    · `UIErrorMessageItem`：`text +0x20` · `canvasGroup +0x28` · `background +0x30` ·
//      `appearScaleFromMultiplier +0x38` · `appearAnimationTime +0x3C` · `timeToStartFading +0x40` ·
//      `timeToFade +0x44` · `cacheRectTransform +0x48` · `cacheScaleTween +0x50` · `cacheFadeTween +0x58`；
//      方法 `Show` / `LayoutRefresh` / `Toggle`。
// ② **场景序列化值**（`assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/`）：
//    · `MonoBehaviour_5029.json`（挂 GO 481）：`errorMessageItemRef → 4710` ·
//      `messageColor = (1,1,1,1)` · `errorColor = (0.800000011920929, 0, 0, 1)`。
//    · `MonoBehaviour_4710.json`（挂 GO 632 = `Error Mensage_Ref`）：`text → 3740` ·
//      `canvasGroup → 3577` · `background → 2989` · **`appearScaleFromMultiplier = 0.3` ·
//      `appearAnimationTime = 0.15` · `timeToStartFading = 2.0` · `timeToFade = 0.25`**。
// ③ **几何**（同目录 `RectTransform_*.json` + `GameObject/*.json` + 运行期 dump
//    `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv:1172-1191`）：
//    · 根 `UI Error Message Controller (MUST BE ENABLED)`（GO 481 / RT 3373，父 = Canvas 直子）：
//      `anchor (0.5,1)` · `pivot (0.5,1)` · `ap (0, −213.60)` · `sd 1920.70 × 336.64`
//      ⇒ 绝对矩形 x[−0.35, 1920.35] y[213.6, 550.24]，**中心 (960, 381.92)**。
//    · `Container`（GO 578 / RT 3154 / MB 4106 = VerticalLayoutGroup）：
//      `anchor (0,0)-(1,0)` · `ap (0, 133.15)` · `sd (0, 266.29)` · `pivot (0.5,0.5)`
//      ⇒ 绝对 x[−0.35, 1920.35] y[**283.945**, 550.235]；layout：`padding 0` ·
//      `m_ChildAlignment = 7`(LowerCenter) · `m_Spacing = 0` · `m_ChildControlWidth/Height = 0`。
//    · `Error Mensage_Ref`（GO 632 / RT 2823）＝**模板**（`m_IsActive = False`，`Awake` 用它克隆 5 份）：
//      `anchor (0,1)` · `ap (960.35, −241.29)` · `sd 1439.16 × 80.00` · `pivot (0.5,0.5)`。
//      ⚠️ 运行期 dump 里 **6 个 item（模板 + 5 克隆）全都是这一组值** —— 因为**当时一个都没亮**，
//         布局组没跑（判据见下面「🔴 位置那一格」）。
//    · `Background`（GO 477 / RT 2989 / MB 5101 = Image · MB 4467 = HorizontalLayoutGroup ·
//      MB 4344 = ContentSizeFitter）：`anchor/pivot (0.5,0.5)` · `ap (0,0)` · `sd 870.98 × 60.85` ·
//      图 **`40k_bt_underbutton`**（485×83 · `m_Border (240,0,240,0)` · `m_Type = 1`(Sliced) ·
//      `m_Color (0.3585,0.3585,0.3585,1)` 灰 · `RaycastTarget = 0`）；
//      内层 layout：`padding (L120, R120, T4, B0)` · `m_ChildAlignment = 4`(MiddleCenter)；
//      ContentSizeFitter `H = 1`(PreferredSize) · `V = 1`(PreferredSize)。
//    · `ErrorMesage Text`（GO 1099 / RT 2942 / MB 3740 TMP）：`sd 630.98 × 56.85` · **`m_fontSize = 60`**
//      （`enableAutoSizing = 0`）· `m_fontColor = (0.8019, 0, 0, 1)` · 对齐 H=2(Center)/V=512(Middle) ·
//      材质 `Pragati Thick Outline White` · 父 = Background。
// ④ **🔴 位置那一格（本件把「读字段」推进到「读布局算法」）**：`Error Mensage_Ref` 的
//    `ap (960.35, −241.29)` **是模板的存档值、不是运行时值** —— 六个 item 在 dump 里同值，正是
//    「布局组**只排活动子件**、当时全没亮」的证据（`Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/
//    UI/Core/Layout/LayoutGroup.cs:59`：`if (rect == null || !rect.gameObject.activeInHierarchy) continue;`）。
//    真正生效的位置 = 那套布局算法（**读的是本工程里的 uGUI 源码**，逐句）：
//      · `GetStartOffset(1, 项数×80)` = `padding.top + (容器高 − 需要高) × alignmentOnAxis`
//        （`LayoutGroup.cs:193-200`），而 LowerCenter 在**竖轴**上 `alignmentOnAxis = ((int)7 / 3) * 0.5 = 1.0`
//        （`:207-213`）；
//      · `SetChildAlongAxisWithScale` 先把子件的 `anchorMin/Max` 设成 `Vector2.up`(0,1)，再写
//        `anchoredPosition.y = −pos − sizeDelta.y × (1 − pivot.y)`（`:249-269`，竖轴那一支的**负号**是关键）。
//    ⇒ 单条亮着时：`pos = 266.2900085449219 − 80 = 186.2900085449219`
//      ⇒ `ap.y = −186.2900085449219 − 40 = −226.2900085449219`
//      ⇒ **中心（自上而下）= 283.94973754882813 + 186.2900085449219 + 40 = 510.23974609375**、
//      横轴 `= 960.0`（`= 容器左 −0.3499755859375 + 960.3499755859375`）。
//    🔴 **与「查证报告写的 525.25」差 15 px**，错因 = 报告那份 `chain_rect` 是**按存档值**算的
//      （283.94973754882813 + 241.29000854492188 = 525.23974609375）。**本件取算出来的 510.23974609375**
//      （实况算法 > 存档字段，铁律 4）。报告那条已按铁律 5 记进交接报告的「顺手发现」。
//    ⇒ **多条的堆叠**：`pos_j = (266.2900085449219 − 80N) + 80j`（N = 活动条数、j = 在**子件序**里的名次）
//      ⇒ 中心 = `590.23974609375 − 80 × (N − j)`：**最后一条永远在 510.23974609375、前一条高 80 px**。
// ⑤ **`Awake`**（`UIMessageController__Awake.c` 逐句）：单例守卫（已有一个 ⇒ `Destroy(this.gameObject)`）⇒
//    `errorMessageItemRef.gameObject.SetActive(false)` ⇒ `parent = 它的 transform.parent` ⇒
//    **`Instantiate(ref, parent)` 五次**加到 `messageItems`（循环 `iVar9++` / `if (4 < iVar9) return` = 0..4 共 5 次）。
//    ⇒ **模板自己也留在容器里**（inactive），连同 5 个克隆 = 容器下 6 个 item
//    （运行期 dump `:1174-1191` 逐条印证：`Error Mensage_Ref` + 5 × `Error Mensage_Ref(Clone)`）。
// ⑥ **`ShowMessage`**（`UIMessageController__ShowMessage.c` 逐句）：
//    `if (IsNullOrEmpty(串)) { CustomDebug.LogError(); return; }` ⇒
//    `item = messageItems[currentMessageIndex]` ⇒ `item.Show(串, 色, localize)` ⇒
//    `currentMessageIndex = ++; if (4 < 它) 归 0`（**轮转 0→1→2→3→4→0**）⇒
//    再遍历 `messageItems` 逐个 `LayoutRebuilder.ForceRebuildLayoutImmediate(item.background)`。
//    `ShowError` = 传 `errorColor` 的 `ShowMessage`（`__ShowError.c` 只有一句转调）。
// ⑦ **`UIErrorMessageItem.Show`**（`__Show.c` 逐句）：
//    `cacheRectTransform = GetComponent<RectTransform>()`（没有就取一次并缓存）⇒
//    `cacheRectTransform.SetAsLastSibling()` ⇒ `gameObject.SetActive(true)` ⇒
//    `if (localize) 串 = LocalizationManager.GetTranslation(串)` ⇒ `text.text = 串` ⇒ `text.color = 入参色` ⇒
//    `canvasGroup.alpha = 1` ⇒ `transform.localScale = Vector3.one` ⇒
//    **第一次**（`cacheScaleTween == null`）：`localScale = Vector3.one × appearScaleFromMultiplier(0.3)` 然后
//      `DOScale(one, appearAnimationTime).From().SetAutoKill(false)` ＋
//      `DOFade(0, timeToFade).SetDelay(timeToStartFading).SetAutoKill(false).OnComplete(→关自身).OnKill(→关自身)`；
//    **之后**：两条 tween 各自 `Restart(true)`。
//    ⚠️ `From()` 的起点 = 调用前那个 `localScale`（= 0.3×one）⇒ 就是「0.3 → 1」。
//    ⚠️ 两个闭包 `b__10_0` / `b__10_1`（`OnComplete` / `OnKill`）在 Ghidra 里被印成了同名桩
//      `BattleAlliancePanel__CloseButtonClick`，但**体是清楚可读的**：`gameObject.SetActive(0)`
//      （与 `UIErrorMessageItem__Toggle.c` 同体）⇒ **不是推断，是实读**（⛔ 查证报告把它标成「推断」，本件更正）。
// ⑧ **底图**：`40k_bt_underbutton` —— 判据 = `bundle_duplicateassetisolation_assets_all/Sprite/
//    40k_bt_underbutton.json`（485×83 · `m_Border (240,0,240,0)` · `m_PixelsToUnits 100` ·
//    pivot (0.5,0.5)）+ MB 5101 的 `m_Type = 1`(Sliced) / `m_Color`。本工程那张 PNG 已在
//    `Resources/Art/ui/40k_bt_underbutton.png`（**485×83**，逐像素尺寸核过）。
// ⑨ **色**：`errorColor = (0.8, 0, 0, 1)` **就是运行时那一档** —— 它由 `Show` 写进 `text.color`，
//    **盖过** TMP 自己序列化的 `m_fontColor (0.8019, 0, 0, 1)`（两处不是同一个数，别混）。
// ⑩ **动画曲线**：原版这两条 tween **没有显式 `SetEase`** ⇒ 吃 DOTween 的 `defaultEaseType`。
//    本工程 `Assets/Resources/DOTweenSettings.asset` 里是 **`defaultEaseType: 6` = `Ease.OutQuad`**
//    （`DG.Tweening.Ease`：`Unset 0` `Linear 1` `InSine 2` `OutSine 3` `InOutSine 4` `InQuad 5`
//     **`OutQuad 6`** …… 与 `BattleCameraSreenSize.cs` 文件头 ⑤ 那份枚举同源）—— 也正是 DOTween 的出厂默认。
//    ⚠️ **原版自己的 `DOTweenSettings` 资产本地没有**（`assets_full` 全盘按名找不到）⇒
//      **「原版=OutQuad」这一条是推断**，如实标；我们按 OutQuad 写（`EaseOut`，见下）。
// ⑪ 🔴 **两场景的子树【不是】逐字段相同 —— 「只有根 `ap` 不同」那句话是错的**（2026-10-09 现读订正）。
//    ⚠️ 上面 ③④ 那一组是 **`battlearena1` 那一档**的实读值；原版同一件控制器在 **13 个战场**与
//    **主菜单**里**不是一个尺寸**。上一版（`Shell/MessageToast.cs` 的 2026-10-09 初版 +
//    `资料/普查产出_第八会话/E7_弹窗星号与toast.md:110`）写「两场景的控制器子树逐字段相同，
//    只有根自己的 `m_AnchoredPosition` 不同」——**那句话只对【根 / 容器 / 时序 MB】三处成立**：
//    | 项 | 战斗（`bundle_scenes_scenes_battlearena1`） | 主菜单（`…_mainmenuwarpforge`） |
//    |---|---|---|
//    | 根 `m_AnchoredPosition` | `(0, **−213.5997314453125**)`（`RT 3373`） | `(**−0.00010299999848939478**, **0.0**)`（`RT 1206`） |
//    | 根 `sd` | `1920.699951171875 × 336.6400146484375` | **同**（✔ 这一半上一版是对的） |
//    | 容器 `RT` 四件 | `RT 3154`：`anchor (0,0)-(1,0)` · `ap (0,133.14500427246094)` · `sd (0,266.2900085449219)` · `pivot (0.5,0.5)` | `RT 1205`：**逐位相同**（✔） |
//    | 容器 `VerticalLayoutGroup` | `MB 4106`：`m_ChildAlignment 7` · `spacing 0` · **`m_ChildControlWidth/Height = 0`** | `MB 2086`：**逐位相同**（✔） |
//    | item `sd` | `RT 2823`：**1439.1600341796875 × 80** | `RT 1208`：**1310 × 50**（🔴 **不同 ⇒ 本件开档位**） |
//    | item `ap`（存档） | `RT 2823`：`(960.3499755859375, −241.29000854492188)` | `RT 1208`：**逐位相同**（✔） |
//    | 文字 `m_fontSize` | `MB 3740`：**60** | `MB 1740`：**36**（正好 0.6×） |
//    | 文字 `sd.y`（= 原版单行高） | `RT 2942`：**56.849998474121094** | `RT 1207`：**34.11000061035156** |
//    | `Background` `sd` | `RT 2989`：**870.97998046875 × 60.849998474121094** | `RT 1204`：**618.5899658203125 × 38.11000061035156** |
//    | 根上两颗（`Canvas` / 控制器 `MB`） | `Canvas 2577`（`RenderMode 2` · `m_OverrideSorting 1` · `SortingOrder 1`）· `MB 5029` | `Canvas 1076` **逐位相同** · `MB 2356` 的 `messageColor (1,1,1,1)` / `errorColor (0.8,0,0,1)` **逐位相同**（✔） |
//
//    🔴 **item 的 `sd.y` 那一格为什么是「位置」的一半**：容器那个 `VerticalLayoutGroup` 是
//    **`m_ChildControlHeight = 0`** ⇒ uGUI 取**子件自己的 `sizeDelta`** 当它这一轴的长度
//    （`LayoutGroup.GetChildSizes`：`if (!controlSize) { min = child.sizeDelta[axis]; … }`，
//    即 `Build()`/`RelayoutActive()` 里那个 `LayoutPreset.ItemHPx` = **布局的堆叠步长**）。
//    ⇒ 菜单档的项高是 **50**、不是 80 ⇒ 单条条心比「照战斗档算」再**往下 `ItemHPx/2 = 15 px`**。
//
//    **旁证（很硬，2026-10-09）**：菜单那条 item 的**存档** `ap.y = −241.29000854492188` **正是
//    h = 50、N = 1 时布局算法会写出来的那个值**（`−(266.2900085449219 − 50) − 50 × 0.5`）——
//    若 h = 80 它该是 `−226.29`。而且菜单那份 item 的 GO 是 **`m_IsActive = true`**
//    （战斗那份的模板是 `false`）⇒ 菜单场景里**布局是真跑过的**，那个存档值就是它的输出。
//    ⛔ 所以「照 h = 80 算出 296.64」是错的，**菜单档单条条心（自上而下）**：
//      `容器上缘(=0 + 336.6400146484375 − 266.2900085449219 = 70.3500061035156)` +
//      `(266.2900085449219 − 50)` + `25` = **311.6400146484375** px
//      （= `根上缘 + RootH − ItemHPx/2`；战斗档同式 = `213.5997314453125 + 336.6400146484375 − 40`
//       = **510.23974609375** ✔ 与本件 ④ 那个值逐位相同 ⇒ **战斗档一字未动**）。
//    ⇒ **做法 = 开「档位」而不是改常量**：随场景变的那六项收进 `LayoutPreset`
//      （`BattlePreset` = 本文件原来那一组、`MenuPreset` = 主菜单那一组）。
//      不传档的 `Create(parent)` 行为与加档之前**逐位相同**（`BattleDriver` 那条路不传）。

// ============================ 我们做的三处「等价物」（⛔ 不当原版） ============================
// A. **`ContentSizeFitter` 那一跳**：原版底条是 **Hug**（ContentSizeFitter PreferredSize + 内层
//    LayoutGroup padding L120/R120/T4/B0）⇒ **底条宽高跟着文字走**：`宽 = 文字宽 + 240`、
//    `高 = 文字高 + 4`。⚠️ 所以场景里那组 `870.98 × 60.85` 是**参考串 `ERROR MESSAGE CONTENT` 的
//    量出来的结果**、⛔ **不是固定尺寸**（查证报告给的是这一组数，本件照**规则**实现、并在
//    `BarSizeOf` 上留读数）。我们不是 uGUI ⇒ 自己量文字（`Label.WorldW/WorldH`）再重建九宫格。
//    🔴 **高度那一格我们掺了一个原版常量**：`LayoutPreset.RefLineHeightPx`（战斗档 `56.85`，= 参考串在原版下的
//    TMP preferred 高度，取自 `RT 2942` 的 `sd.y`；菜单档 `34.11`，取自 `RT 1207` —— 见文件头 ⑪）。
//    **为什么**：原版 `高 = TMP preferred 高度 + 4`，
//    而 preferred 高度是**字体的行高**（与串无关）；本工程用的是 NotoSerifCJK（行高 **1.437 em**），
//    原版那条字是 Pragati 族（**0.9475 em**）⇒ 直接拿我们的行高会**高 40%**。取法 = 两条取**大**：
//    `底条高 = max(56.85, 我们量到的文字高) + 4` —— 英文句子上与原版**逐位相同（60.85）**，
//    中文（汉字占 1 em = 60 px > 56.85）才撑到不被切。⛔ 这一条**是我们的取舍**，如实标。
// B. **`SetAsLastSibling` / `ForceRebuildLayoutImmediate` 那一跳**：原版靠 uGUI 的兄弟序与布局系统
//    把「刚弹的那条压在最上面」＋「底条贴合文字」。我们**没有 uGUI** ⇒ 等价物 = **渲染队列**
//    （`CLAUDE.md` §三：分层要用队列、别靠 z）：本件整条在 **`Q = 4001`**（高于日志面板的 4000、
//    也高于 tooltip 的 3607 ⇒ 等价于原版那个 `SortingOrder = 1` 的独立 overlay Canvas），
//    底条 `Q` / 文字 `Q + 1`（= 原版「Background 先、Text 后」的兄弟序）。
// C. **时间轴自推**：原版靠 DOTween 帧循环；**批处理下没有帧循环** ⇒ 本件把三段
//    （`0.15` / `2.0` / `0.25`）做成**能被显式推的** `Advance(dt)`，挂在 `BattleDriver.AdvanceTimeline`
//    这个泵上（同 `TickOvertime` / `TickTargetSelection` 那一族）。
//
// ============================ 我们没做 / 做不到的（如实） ============================
// D. **`I2.Loc.LocalizationManager.GetTranslation` 那条本地化**：原版 `Show(..., localize: true)`
//    走 I2 表。我们是 `Core/Loc.cs` ⇒ 等价物 = 「表里有这条键就 `Loc.T(键)`，否则**调用方给的兜底句**」
//    （同 `BattleDriver.HandFullText` 那两态）。⚠️ `Battle/Tips/*` 那族键**本地一个 value 都没有**
//    （I2 表在远端 CCD）⇒ 今天实际走的**永远是兜底句**。
// E. **`OnKill` 那一支的触发时机**：原版 `SetAutoKill(false)` ⇒ 那条 fade tween **活到下次
//    `Restart`**；我们手推的版本没有「kill」这回事 ⇒ 等价物 = <see cref="HideAll"/>（收工/换局用）。
//    ⚠️ 生产路径今天**没有**任何地方调它（原版也没有「换局清横幅」这一句）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 原版 **`UI Error Message Controller (MUST BE ENABLED)`**（`UIMessageController` + `UIErrorMessageItem`）。
    /// <para>5 条一池、轮转；每条 0.3×→1× 入场 0.15 s、停 2.0 s、淡出 0.25 s。判据逐条在本文件头部。</para>
    /// </summary>
    public class ErrorMessageBanner : MonoBehaviour
    {
        // ==================================================================
        //  原版值（出处见文件头 ②③⑧⑨）
        // ==================================================================

        /// <summary>原版 `UIMessageController.NUMBER_OF_MESSAGES`（const）。</summary>
        public const int NumberOfMessages = 5;

        /// <summary>原版 `messageColor`（场景值 (1,1,1,1)）—— `ShowMessage` 那一档。</summary>
        public static readonly Color MessageColor = new Color(1f, 1f, 1f, 1f);

        /// <summary>原版 `errorColor`（场景值 (0.8, 0, 0, 1)）—— `ShowError` 那一档，**也是运行时真正落到文字上的色**
        /// （它由 `Show` 写进 `text.color`，盖过 TMP 自己序列化的 (0.8019,0,0,1)，见文件头 ⑨）。</summary>
        public static readonly Color ErrorColor = new Color(0.8f, 0f, 0f, 1f);

        /// <summary>原版 `UIErrorMessageItem.appearScaleFromMultiplier` = 0.3。</summary>
        public const float AppearScaleFromMultiplier = 0.3f;
        /// <summary>原版 `appearAnimationTime` = 0.15 s。</summary>
        public const float AppearAnimationTime = 0.15f;
        /// <summary>原版 `timeToStartFading` = 2.0 s。</summary>
        public const float TimeToStartFading = 2.0f;
        /// <summary>原版 `timeToFade` = 0.25 s。</summary>
        public const float TimeToFade = 0.25f;

        /// <summary>整条横幅活着的总时长（= 停 + 淡，原版两条 tween 各自跑完 ⇒ 取大的那一条）。</summary>
        public const float Lifetime = TimeToStartFading + TimeToFade;   // 2.25 s

        // ---- 几何（原版 1920×1080 绝对 px，y **从上**；文件头 ③④⑪）
        //      🔴 **全部写 `RectTransform_*.json` 的未取整值**（查证报告那张表是取整到 0.01/0.1 的写法；
        //      本件按原值写，与 `BattleDriver` 里 A513/A531 那一档口径一致）。
        //      🔴 **两场景（战斗 / 主菜单）不是同一个尺寸** ⇒ 随场景变的六项收进 `LayoutPreset`（文件头 ⑪）。

        /// <summary>根矩形 `sd`（`RT 3373` / `RT 1206`，**两档逐位相同**）：`1920.699951171875 × 336.6400146484375`。
        /// `anchor/pivot = (0.5,1)` ⇒ 上缘 = `LayoutPreset.RootTopPx`、x 中心 = <see cref="RootCx"/>。</summary>
        const float RootW = 1920.699951171875f, RootH = 336.6400146484375f;
        /// <summary>根 x 中心（两档同：`ap.x` 战斗 `0` / 菜单 `−0.00010299999848939478`
        /// ⇒ 差 0.0001 px，**不给它开档位**，如实记在文件头 ⑪）。</summary>
        const float RootCx = 960f;
        /// <summary>**战斗档**的根上缘（屏幕自上而下 px）= `−ap.y` = `213.5997314453125`（`RT 3373`）。
        /// 菜单档是 `0`（`RT 1206`）⇒ 见 <see cref="MenuPreset"/>。战斗档的根中心 y = `381.91973876953125`。</summary>
        const float BattleRootTopPx = 213.5997314453125f;
        /// <summary>容器（`RT 3154` / `RT 1205`，**两档逐位相同**）：stretch 横（宽 = 父宽 1920.699951171875、
        /// 左缘 **−0.3499755859375**）· 高 266.2900085449219 · `ap (0, 133.14500427246094)` + `anchor (0,0)-(1,0)`。</summary>
        const float ContW = 1920.699951171875f, ContH = 266.2900085449219f;
        const float ContLeftX = -0.3499755859375f;
        /// <summary>容器上缘相对**根上缘**的距离（px，向下为正）= `RootH − ContH` = **70.3500061035156**。
        /// 两档同（容器是 stretch 横 + 贴根底）⇒ **不随档变**；战斗档上缘 = `283.94973754882813`。</summary>
        const float ContTopFromRootTop = RootH - ContH;
        /// <summary>item 的**存档** `anchoredPosition`（`RT 2823` / `RT 1208`，**两档逐位相同** ·
        /// `anchor (0,1)`）—— 只有模板/未激活时是它（文件头 ④：真正亮起来时由布局算法覆写）。</summary>
        const float ItemApX = 960.3499755859375f, ItemApY = -241.29000854492188f;
        /// <summary>底条内层 layout 的 padding（`MB 4467`）= (L120, R120, T4, B0)。**两档同。**</summary>
        const float PadL = 120f, PadR = 120f, PadT = 4f, PadB = 0f;

        /// <summary>
        /// **两场景不同的那六项几何**（2026-10-09 实测；判据逐条、含旁证 → 文件头 ⑪）。
        /// 原版同一件控制器在 13 个战场与主菜单里**不是一个尺寸**：主菜单那套是
        /// **根上缘 0 · 项高 50 · 字号 36** 的小一号版本。
        /// <para>🔴 **为什么位置也要靠它**：条目位置是**布局算法**按 `ItemHPx` 算出来的
        /// （`LayoutGroup` 那套，文件头 ④）⇒ 项高一改，条心就跟着走 `ItemHPx/2`。</para>
        /// <para>⛔ 别拿战斗那一档去顶壳里那颗（那正是 2026-10-09 初版落在屏顶下 510.24、
        /// 而主菜单那一档是 311.64 的原因）。</para>
        /// </summary>
        public struct LayoutPreset
        {
            /// <summary>根上缘（屏幕自上而下 px）= `−根.m_AnchoredPosition.y`。
            /// 战斗 `213.5997314453125`（`RT 3373`）· 菜单 `0`（`RT 1206`）。
            /// ⚠️ 它同时决定**容器上缘**（= 本值 + <see cref="ContTopFromRootTop"/>）。</summary>
            public float RootTopPx;
            /// <summary>item 节点的矩形（`anchor (0,1)` ⇒ 这两格就是 `sd`）。
            /// 战斗 `1439.1600341796875 × 80`（`RT 2823`）· 菜单 `1310 × 50`（`RT 1208`）。
            /// 🔴 **高那一格 = 布局的堆叠步长**：容器的 `VerticalLayoutGroup` 是
            /// `m_ChildControlHeight = 0` ⇒ uGUI 取**子件自己的 `sizeDelta`**
            /// （`LayoutGroup.GetChildSizes` 的 `if (!controlSize)` 那一支）；宽那一格不参与布局结果
            /// （`LowerCenter` ⇒ 条心 x 恒 = 容器中心 960，与项宽无关），留着是为了如实记下原版值。</summary>
            public float ItemWPx, ItemHPx;
            /// <summary>模板底条（`Background`）**参考串量出来的**那一组：战斗 `RT 2989` =
            /// `870.97998046875 × 60.849998474121094` · 菜单 `RT 1204` = `618.5899658203125 × 38.11000061035156`。
            /// ⚠️ ⛔ **不是固定尺寸**（真规则是 Hug，见文件头 A）—— 只在两处用：模板那条、以及文字量不出时的兜底。</summary>
            public float RefBarWPx, RefBarHPx;
            /// <summary>文字 `m_fontSize`（`enableAutoSizing = 0`）：战斗 `60`（`MB 3740`）· 菜单 `36`（`MB 1740`）。</summary>
            public float FontPx;
            /// <summary>原版字体的**单行高**（= 参考串的 TMP preferred 高度 = 文字 `RT` 的 `sd.y`）：
            /// 战斗 `56.849998474121094`（`RT 2942`）· 菜单 `34.11000061035156`（`RT 1207`）。
            /// 用途与取舍见文件头 A（底条高 = `max(它, 我们量到的文字高) + 4`）。</summary>
            public float RefLineHeightPx;
        }

        /// <summary>**战斗档** = 原版 13 个 `battlearena*`（`RT 3373` 那一族）。
        /// 🔴 也是**本件加档之前的唯一那一档** ⇒ 不传档的 <see cref="Create(Transform)"/>
        /// 与加档之前**逐位相同**（`BattleDriver.BuildErrorBanner` 那条路就是不传档的）。</summary>
        public static readonly LayoutPreset BattlePreset = new LayoutPreset
        {
            RootTopPx = BattleRootTopPx,
            ItemWPx = 1439.1600341796875f, ItemHPx = 80.00f,
            RefBarWPx = 870.97998046875f, RefBarHPx = 60.849998474121094f,
            FontPx = 60f, RefLineHeightPx = 56.849998474121094f,
        };

        /// <summary>**主菜单档** = 原版 `bundle_scenes_scenes_mainmenuwarpforge` 那一颗（`RT 1206` 那一族）。
        /// 🔴 **外壳侧的 `Shell/MessageToast` 用这一档**（原版那条 toast 就是从主菜单喊的）。
        /// 判据（含 `RT 1208` / `MB 1740` / `RT 1207` / `RT 1204` 四处）→ 文件头 ⑪。
        /// <para>⇒ 单条条心（自上而下）= `0 + 336.6400146484375 − 25` = **311.6400146484375** px。</para></summary>
        public static readonly LayoutPreset MenuPreset = new LayoutPreset
        {
            RootTopPx = 0f,
            ItemWPx = 1310f, ItemHPx = 50.00f,
            RefBarWPx = 618.5899658203125f, RefBarHPx = 38.11000061035156f,
            FontPx = 36f, RefLineHeightPx = 34.11000061035156f,
        };

        /// <summary>本件这一颗用的是哪一档（自检要能分辨「壳里那颗取的是菜单档」）。</summary>
        public LayoutPreset Preset { get { return _preset; } }

        /// <summary>本件这一档的根上缘（自上而下 px）—— 自检口（战斗 `213.5997314453125` / 菜单 `0`）。</summary>
        public float RootTopPx { get { return _preset.RootTopPx; } }

        /// <summary>本件这一档的**容器上缘**（自上而下 px）= 根上缘 + `RootH − ContH`
        /// （战斗 `283.94973754882813` / 菜单 `70.3500061035156`）。</summary>
        public float ContainerTopPx { get { return _preset.RootTopPx + ContTopFromRootTop; } }

        /// <summary>本件这一档的**项高**（= 布局的堆叠步长，自检口：战斗 `80` / 菜单 `50`）。</summary>
        public float ItemHeightPx { get { return _preset.ItemHPx; } }

        /// <summary>**只亮一条时**那一条的条心（自上而下 px）—— 自检口，也是本档「位置」的那一个数。
        /// <para>算法：`条心 = 容器上缘 + (容器高 − 项高) + 项高/2 = 根上缘 + RootH − 项高/2`。</para>
        /// ⇒ 战斗档 = `213.5997314453125 + 336.6400146484375 − 40` = **510.23974609375**（与实测逐位相同）·
        /// 菜单档 = `0 + 336.6400146484375 − 25` = **311.6400146484375**（文件头 ⑪）。
        /// ⛔ 别拿 `−ItemApY`（= 525.24 / 241.29）当它 —— 那是**存档值**，不是布局算出来的位置（文件头 ④）。</summary>
        public float SingleItemCenterYpx { get { return _preset.RootTopPx + RootH - _preset.ItemHPx * 0.5f; } }

        // ---- 底图 `40k_bt_underbutton`（文件头 ⑧） ----
        const string BarArt = "40k_bt_underbutton";
        const float BarTexW = 485f, BarTexH = 83f;
        static readonly Vector4 BarBorder = new Vector4(240f, 0f, 240f, 0f);   // (左, 下, 右, 上)
        /// <summary>底图那颗 `Image.m_Color`（`MonoBehaviour_5101.json` 实读 = **0.35849058628082275**，
        /// 三个通道同值 · α 1 · `m_Type = 1`(Sliced) · `m_FillCenter = 1` ·
        /// **`m_PixelsPerUnitMultiplier = 1.0`** ⇒ 角块 = `m_Border` 原值 240 px，⛔ 不再除任何系数
        /// （与 `SettingsPanel.LstPanelBorderOut` 那条 `÷ 1.09` 不同）。</summary>
        public static readonly Color BarTint = new Color(0.3584906f, 0.3584906f, 0.3584906f, 1f);

        // ---- 渲染队列（等价物 B） ----
        /// <summary>整条横幅的队列：高于日志面板（4000）与 tooltip（3607）⇒ 等价于原版那个 `SortingOrder = 1`。</summary>
        public const int Q = 4001;
        /// <summary>底条队列；文字比它大 1（= 原版兄弟序 Background → Text）。</summary>
        public const int QBar = Q, QText = Q + 1;

        /// <summary>内层子件的 z（相机看 +Z ⇒ **z 越小越靠前**）：底条 0、文字 −0.01。
        /// ⚠️ 分层**主要**靠上面的渲染队列，z 只是同队列内的次序（同 `BattleDriver.HudImageZ` 的注释）。</summary>
        const float ZBar = 0f, ZText = -0.01f;

        /// <summary>px → 世界单位（= `1/108`；判据 = `LayoutSpace.Px`）。</summary>
        static float U(float px) { return LayoutSpace.Px(px); }

        // ==================================================================
        //  池与条目
        // ==================================================================

        /// <summary>池里的一条（= 原版一个 `UIErrorMessageItem` 实例）。</summary>
        class Item
        {
            public Transform root;              // `Error Mensage_Ref(Clone)`（缩放 + 淡出都作用在它身上）
            public GameObject barRoot;          // `Background` 九宫格根（每次按文字尺寸重建）
            public List<ImageQuad> barParts;    // 九宫格那几块（染色/读回用）
            public Label text;                  // `ErrorMesage Text`
            public float t;                     // 本条的存活时间（秒）
            public bool active;                 // 这一条正在演吗
        }

        readonly List<Item> _items = new List<Item>();
        Transform _container;
        Item _template;
        int _currentIndex;
        bool _built;
        /// <summary>这一颗用的**场景档**（默认 <see cref="BattlePreset"/> ⇒ 与加档之前逐位相同）。
        /// 由 <see cref="Create(Transform, LayoutPreset)"/> 在 `Build()` **之前**赋值 —— ⛔ 别在 `Build` 之后再改，
        /// 位置/尺寸已经写进节点了（那份是「按档算一次」的口径，同原版序列化值）。</summary>
        LayoutPreset _preset = BattlePreset;

        /// <summary>模板那一条（原版 `Error Mensage_Ref`，永远不显示；自检可以用它量「参考串的底条多大」）。</summary>
        public bool TemplateBuilt { get { return _template != null && _template.barRoot != null && _template.text != null; } }

        /// <summary>模板底条画出来的尺寸（px）—— 与本档 `LayoutPreset.RefBarWPx/HPx`（战斗 `870.98 × 60.85` /
        /// 菜单 `618.59 × 38.11`）对比，差值就是**我们 TMP 度量 vs 原版** 的差
        /// （⛔ 不是缺陷，别拿它当红的判据 —— 模板底条是照那两个常量建的，逐位相同）。</summary>
        public Vector2 TemplateBarSizePx
        {
            get
            {
                if (_template == null || _template.barParts == null || _template.barParts.Count == 0)
                    return new Vector2(-1f, -1f);
                return BarSizeOfParts(_template.barParts);
            }
        }

        /// <summary>池里几条（应当恒 = <see cref="NumberOfMessages"/>）。</summary>
        public int ItemCount { get { return _items.Count; } }
        /// <summary>下一条会用池里的第几个（原版 `currentMessageIndex`，轮转 0→…→4→0）。</summary>
        public int CurrentIndex { get { return _currentIndex; } }
        /// <summary>`ShowMessage` 一共弹过几条（自检用）。</summary>
        public int ShowCount { get; private set; }
        /// <summary>最近一条的**原始**文案（⛔ 不是本地化之后的；自检要能分辨「原文进来了没有」）。</summary>
        public string LastRawText { get; private set; }
        /// <summary>最近一条**真正喂给 Label 的**文案（本地化 + 参数替换之后）。</summary>
        public string LastShownText { get; private set; }

        /// <summary>现在亮着几条。</summary>
        public int VisibleCount
        {
            get { int n = 0; for (int i = 0; i < _items.Count; i++) if (_items[i].active) n++; return n; }
        }

        // ==================================================================
        //  建件（= 原版 `Awake` 那一段：模板 → 关掉 → 克隆 5 份）
        // ==================================================================

        /// <summary>建在 `parent`（= HUD 根 = 原版 `BattleHud` 那一级）下，用**战斗档**（<see cref="BattlePreset"/>）。
        /// <para>⚠️ 原版这一颗挂在 **Canvas 直子**上（`BattleHud/Canvas/…`）；我们这套没有 uGUI Canvas 节点
        /// （`BattleDriver.BuildHud` 的 `HudRoot` 就是原版 `BattleHud` 那一级）⇒ 挂 `parent` 下。</para></summary>
        public static ErrorMessageBanner Create(Transform parent)
        {
            return Create(parent, BattlePreset);
        }

        /// <summary>同上，但**指定场景档**（文件头 ⑪）。
        /// <para>🔴 谁该传哪一档：战场侧（`BattleDriver.BuildErrorBanner`）**不传** ⇒ 战斗档；
        /// 外壳侧（`Shell/MessageToast`，= 原版**主菜单**里那颗的同族）传 <see cref="MenuPreset"/>。</para>
        /// <para>⛔ **档里没有的东西都按「两档相同」处理**（根 `sd` / 容器四件 / item 的存档 `ap` /
        /// padding / 池容量 / 四段时长）—— 那几项是两场景**逐位相同**的实读值（文件头 ⑪ 那张表）。</para></summary>
        public static ErrorMessageBanner Create(Transform parent, LayoutPreset preset)
        {
            var go = new GameObject("UI Error Message Controller (MUST BE ENABLED)", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, RootW, RootH);
            // 根的位置 = 它的**矩形中心**（anchor (0.5,1) + ap (0,−档.RootTopPx) ⇒ 中心 (960, 档上缘 + RootH/2)）
            go.transform.localPosition = LayoutSpace.FromPixel(RootCx, preset.RootTopPx + RootH * 0.5f);
            var b = go.AddComponent<ErrorMessageBanner>();
            b._preset = preset;
            b.Build();
            return b;
        }

        void Build()
        {
            if (_built) return;
            _built = true;

            var container = new GameObject("Container", typeof(RectTransform));
            container.transform.SetParent(transform, false);
            // 容器：stretch 横 + 高 266.29 ⇒ 矩形 x[−0.35,1920.35] y[档上缘+70.35, 档上缘+336.64]、
            // 中心 (960, 档上缘 + 203.67)；战斗档 = y[283.945,550.235] 中心 (960, 417.09)（文件头 ③⑪）
            MenuDraw.SetPxSize(container.transform, ContW, ContH);
            container.transform.localPosition =
                LayoutSpace.FromPixel(ContLeftX + ContW * 0.5f, _preset.RootTopPx + ContTopFromRootTop + ContH * 0.5f)
                - transform.localPosition;
            _container = container.transform;

            // ① 模板（= 原版 `Error Mensage_Ref`，`Awake` 第一句把它关掉、再拿它 clone 5 份）。
            //    ⚠️ 原版**模板自己也带着 `Background` + `ErrorMesage Text` 两颗子件**（运行期 dump
            //    `:1174-1176` 实读）⇒ 这里照建（用**参考串** `ERROR MESSAGE CONTENT` 的那组实读尺寸）。
            //    它**永不被 Show**（原版也是：模板只当克隆源；`Awake` 里 `SetActive(false)`）。
            var tmplItem = new Item { root = BuildItemNode(container.transform, "Error Mensage_Ref"), active = false };
            _template = tmplItem;
            BuildTemplateChildren(tmplItem);
            // 模板**停在存档位**上（= 原版 `ap (960.3499755859375, −241.29000854492188)` + `anchor (0,1)`
            // 换算出来的那个点：容器左缘 −0.3499755859375 + 960.3499755859375 = **960.0**、
            // 上缘 + 241.29000854492188 = **525.23974609375**）。
            // ⚠️ 它**不是**布局算出来的位置（存档值只在「一条都没亮、布局没跑」时成立 —— 运行期 dump
            // 里六个 item 全是这一组值，正是那个状态）⇒ 模板永远不亮、也就永远停在这一档。
            // 🔴 这一组 `ap` **两档逐位相同**（战斗 `RT 2823` = 菜单 `RT 1208`）⇒ 用常量、不随档变
            //    （文件头 ⑪ 那张表；⛔ 别拿它跟「条心」混 —— 条心是亮起来时布局算的，另一回事）。
            tmplItem.root.localPosition =
                _container.InverseTransformPoint(
                    LayoutSpace.FromPixel(ContLeftX + ItemApX, _preset.RootTopPx + ContTopFromRootTop - ItemApY));
            tmplItem.root.gameObject.SetActive(false);

            // ② 5 个克隆（原版 `Instantiate(ref, parent)` 跑五次）
            for (int i = 0; i < NumberOfMessages; i++)
            {
                var t = BuildItemNode(container.transform, "Error Mensage_Ref(Clone)");
                _items.Add(new Item { root = t, active = false });
            }

            RelayoutActive();          // 摆到「一条都不亮」时的位置（= 存档位；亮起来时再按算法重排）
            _currentIndex = 0;
        }

        /// <summary>模板那两颗子件（原版实读值）：`Background` = `40k_bt_underbutton` 按**本档**的参考串尺寸
        /// （战斗 `870.98 × 60.85` / 菜单 `618.59 × 38.11`，见 `LayoutPreset.RefBarW/H`）、
        /// 文字 = 参考串 `ERROR MESSAGE CONTENT`（战斗 `sd 630.98 × 56.85` · `m_fontSize 60` ·
        /// `m_fontColor (0.8019,0,0,1)`；菜单那档的 `m_fontSize` 是 36）。
        /// ⚠️ 文字色那一档已被 `Show` 覆盖（文件头 ⑨）⇒ 模板这颗照 **它自己的序列化值**画
        /// （模板本来就不走 `Show`）。</summary>
        void BuildTemplateChildren(Item t)
        {
            var tex = CardArt.Ui(BarArt);
            if (tex != null)
            {
                t.barRoot = ImageQuad.CreateNineSlice(t.root, tex, BarBorder, tex.width, tex.height,
                                                      new Vector3(0f, 0f, ZBar), U(_preset.RefBarWPx), U(_preset.RefBarHPx), "Background");
                if (t.barRoot != null)
                {
                    t.barParts = new List<ImageQuad>(t.barRoot.GetComponentsInChildren<ImageQuad>(true));
                    for (int i = 0; i < t.barParts.Count; i++)
                    {
                        t.barParts[i].SetTint(BarTint);
                        t.barParts[i].SetRenderQueue(QBar);
                    }
                }
            }
            else Debug.LogWarning($"[ErrorMessageBanner] 找不到 `{BarArt}` ⇒ 模板那颗 `Background` 没建");

            t.text = Label.Create(t.root, "ERROR MESSAGE CONTENT",
                                  new Vector3(0f, U(-(PadT - PadB) * 0.5f), ZText), 1,
                                  new Color(0.801886796951294f, 0f, 0f, 1f), new Vector2(0.5f, 0.5f), "ErrorMesage Text");
            if (t.text != null) { t.text.SetScriptHeight("ERROR MESSAGE CONTENT", _preset.FontPx, 108f); t.text.SetRenderQueue(QText); }
        }

        /// <summary>建一个 item 节点（只有 `RectTransform`，尺寸写**本档**的 `sd`：战斗 `1439.16 × 80.00`
        /// / 菜单 `1310 × 50` —— 高那一格是**布局的堆叠步长**，见 `LayoutPreset.ItemHPx`；
        /// `Background`/`ErrorMesage Text` 两颗子件在第一次 `Show` 时按**那一条的文案**建 —— 模板例外，
        /// 它按参考串预先建好，见 <see cref="BuildTemplateChildren"/>）。</summary>
        Transform BuildItemNode(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, _preset.ItemWPx, _preset.ItemHPx);
            return go.transform;
        }

        // ==================================================================
        //  生产口：ShowMessage / ShowError（= 原版那两个）
        // ==================================================================

        /// <summary>= 原版 `UIMessageController.ShowError(串, localize)`（本质是 `ShowMessage(串, errorColor, …)`）。</summary>
        /// <param name="text">文案（`localize = true` 时是**词条键**）。</param>
        /// <param name="localize">要不要过词条表。⚠️ 表里**没有**这条键时**不出键名** —— 见
        /// <paramref name="fallback"/>（同 `BattleDriver.HintForCodeWithKey` 那两态）。</param>
        /// <param name="fallback">键不在表里时用的整句（⛔ 不许留空：空串 = 静默失败）。
        /// 表里有键时这一项**不被使用**。</param>
        public void ShowError(string text, bool localize = false, string fallback = null)
        {
            ShowMessage(text, ErrorColor, localize, fallback);
        }

        /// <summary>= 原版 `UIMessageController.ShowMessage(串, 色, localize)`（文件头 ⑥⑦）。</summary>
        public void ShowMessage(string text, Color color, bool localize = false, string fallback = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                // 原版：`CustomDebug.LogError` 后直接返回（不出声的话，上层会以为弹出去了）
                Debug.LogError("[ErrorMessageBanner] `ShowMessage` 收到空串 ⇒ **什么都没弹**"
                             + "（原版这里也是 `LogError` + return，见本文件头 ⑥）");
                return;
            }
            if (!_built || _items.Count == 0) { Debug.LogError("[ErrorMessageBanner] 还没建件就调 `ShowMessage`"); return; }

            LastRawText = text;
            string shown = text;
            if (localize)
            {
                // 等价物 D：原版走 I2；我们＝「表里有这条键才取值，否则用调用方给的兜底句」
                if (Loc.HasEntry(text)) shown = Loc.T(text);
                else if (!string.IsNullOrEmpty(fallback)) shown = fallback;
                else
                {
                    // 没有兜底句 ⇒ 不能印键名（那是本工程最忌讳的那种退化）⇒ 出声 + 退回键名
                    Debug.LogWarning($"[ErrorMessageBanner] 词条 `{text}` 不在语言表里、调用方也没给兜底句"
                                   + " ⇒ 界面上会印**键名本身**（本文件头 D 段：`Battle/Tips/*` 那族本地全无 value）。");
                }
            }
            LastShownText = shown;

            var it = _items[_currentIndex];
            _currentIndex++;
            if (_currentIndex > NumberOfMessages - 1) _currentIndex = 0;   // 原版：`if (4 < i) i = 0`

            ShowItem(it, shown, color);
            ShowCount++;
        }

        /// <summary>把一个 item 拉起来并摆好（= 原版 `UIErrorMessageItem.Show` 的等价物，文件头 ⑦）。
        /// <para>原版那两句**没有**等价物、如实标：`SetAsLastSibling()`（我们用渲染队列，见文件头 B）与
        /// `ForceRebuildLayoutImmediate(background)`（我们没有 uGUI 布局系统 ⇒ 位置/尺寸自己算）。</para></summary>
        void ShowItem(Item it, string text, Color color)
        {
            it.root.gameObject.SetActive(true);         // 原版第 2 句
            it.active = true;
            it.t = 0f;                                  // 两条 tween 从 0 开始（`From()` 的起点见文件头 ⑦）

            // ---- 文字：换文案 + 按语种定字号（`SetScriptHeight` 是全工程唯一那份语种判据） ----
            if (it.text == null)
            {
                it.text = Label.Create(it.root, text, new Vector3(0f, 0f, ZText), 1,
                                       color, new Vector2(0.5f, 0.5f), "ErrorMesage Text");
                if (it.text != null) it.text.SetRenderQueue(QText);
            }
            else it.text.SetText(text);
            if (it.text != null)
            {
                it.text.SetScriptHeight(text, _preset.FontPx, 108f);      // 战斗 60 / 菜单 36（文件头 ⑪）
                it.text.SetColor(color);
            }

            // ---- 底条：按【文字实测】重建（等价物 A：原版是 ContentSizeFitter 的 Hug） ----
            float textWpx = it.text != null ? it.text.WorldW * 108f : 0f;
            float textHpx = it.text != null ? it.text.WorldH * 108f : 0f;
            // 🔴 防守（**不许静默**）：TMP 在「没量出来」时会回一个天文数字（`Label` 的 doc 记着实测
            //    `tmpW = 4.29e9`）—— 直接拿它建底条会画出一条几万 px 宽的灰带。不合理就退回**本档参考串那一档**
            //    （`RefBarWPx − 240` / `RefBarHPx − 4`）并出声。
            if (!(textWpx > 0f) || !(textHpx > 0f) || textWpx > 20000f || textHpx > 20000f)
            {
                Debug.LogWarning($"[ErrorMessageBanner] 文字量出来的尺寸不可信（{textWpx}×{textHpx} px）"
                               + " ⇒ 底条退回**本档参考串那一档**（`{_preset.RefBarWPx - PadL - PadR} × {_preset.RefBarHPx - PadT - PadB}`）。"
                               + "多半是 TMP 在对象未激活时量了尺寸（见本文件头 A 与 `Label` 那条坑）。");
                textWpx = _preset.RefBarWPx - PadL - PadR;
                textHpx = _preset.RefBarHPx - PadT - PadB;
            }
            float barW = textWpx + PadL + PadR;
            float barH = Mathf.Max(_preset.RefLineHeightPx, textHpx) + PadT + PadB;   // 战斗 56.85 / 菜单 34.11
            BuildBar(it, barW, barH);

            // 文字在底条里**居中**、但内层 padding 是 (T4, B0) 不对称 ⇒ 中心下沉 (4−0)/2 = 2 px
            // （判据：原版 `ErrorMesage Text` 的绝对中心比 `Background` 低 2.0 px，见文件头 ③）
            if (it.text != null)
                it.text.transform.localPosition = new Vector3(0f, U(-(PadT - PadB) * 0.5f), ZText);

            RelayoutActive();
            ApplyVisual(it, color);
        }

        /// <summary>按 `w × h`（**px**）重建这一条的底条九宫格（原版是 `Image.Type = Sliced` + ContentSizeFitter）。
        /// ⚠️ 每次弹都重建：原版那条 Hug 也是每次改 `sizeDelta` 重新生成九宫格几何。</summary>
        void BuildBar(Item it, float wPx, float hPx)
        {
            if (it.barRoot != null)
            {
                // 批处理没有帧循环：`Destroy` 不生效（CLAUDE.md §三）⇒ 走本仓那条既有写法
                if (Application.isPlaying) Destroy(it.barRoot); else DestroyImmediate(it.barRoot);
                it.barRoot = null;
                it.barParts = null;
            }
            var tex = CardArt.Ui(BarArt);
            if (tex == null)
            {
                Debug.LogWarning($"[ErrorMessageBanner] 找不到 `{BarArt}`（`Resources/Art/ui/`）⇒ **底条画不出来**，"
                               + "只有文字（不许静默：原版这一层是可见的）");
                return;
            }
            it.barRoot = ImageQuad.CreateNineSlice(it.root, tex, BarBorder,
                                                   tex.width, tex.height,
                                                   new Vector3(0f, 0f, ZBar), U(wPx), U(hPx), "Background");
            if (it.barRoot == null) return;
            it.barParts = new List<ImageQuad>(it.barRoot.GetComponentsInChildren<ImageQuad>(true));
            for (int i = 0; i < it.barParts.Count; i++)
            {
                it.barParts[i].SetTint(BarTint);
                it.barParts[i].SetRenderQueue(QBar);
            }
        }

        // ==================================================================
        //  布局（= 原版那套 VerticalLayoutGroup 的算法；文件头 ④）
        // ==================================================================

        /// <summary>把**活动**的条目按原版布局算法重排（`LowerCenter` ⇒ 从容器底往上、间距 = 项高 = **本档的 `ItemHPx`**）。
        /// ⚠️ 只算**活动**的（`activeInHierarchy` 那一道闸，判据见文件头 ④）。
        /// 🔴 **项高取自档位**：容器 `VerticalLayoutGroup` 是 `m_ChildControlHeight = 0` ⇒ uGUI 用子件自己的
        /// `sizeDelta`（战斗 80 / 菜单 50）⇒ 条心 = `根上缘 + RootH − 项高/2`（战斗 510.24 / 菜单 311.64，文件头 ⑪）。</summary>
        void RelayoutActive()
        {
            if (_container == null) return;
            int n = VisibleCount;
            if (n == 0) return;
            float itemH = _preset.ItemHPx;
            float contTopY = _preset.RootTopPx + ContTopFromRootTop;
            int j = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                if (!it.active || it.root == null) continue;
                // pos = (容器高 − 项高×N) + 项高×j（从容器**上缘**往下量）
                float pos = (ContH - itemH * n) + itemH * j;
                float centerY = contTopY + pos + itemH * 0.5f;      // 自上而下的 px
                // 子节点的 localPosition 是相对**容器**的（容器自己也有偏移，⛔ 别当成相对根）
                it.root.localPosition =
                    _container.InverseTransformPoint(LayoutSpace.FromPixel(ContLeftX + ContW * 0.5f, centerY));
                j++;
            }
        }

        // ==================================================================
        //  推进（等价物 C：原版靠 DOTween 帧循环，我们手推）
        // ==================================================================

        /// <summary>推进 `dt` 秒（`BattleDriver.AdvanceTimeline` 那个泵会调；自检也能直接推）。
        /// 三段：0.15 s 缩放入场 → 停到 2.0 s → 0.25 s 淡出 → <c>SetActive(false)</c>。</summary>
        public void Advance(float dt)
        {
            if (_items.Count == 0) return;
            bool relayout = false;
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                if (!it.active) continue;
                it.t += dt;
                if (it.t >= Lifetime)
                {
                    // 原版的 `OnComplete` ⇒ `SetActive(false)`（文件头 ⑦）
                    it.t = 0f;
                    it.active = false;
                    if (it.root != null) it.root.gameObject.SetActive(false);
                    relayout = true;
                    continue;
                }
                ApplyVisual(it, _lastColorOf(it));
            }
            if (relayout) RelayoutActive();     // 少了一条 ⇒ 剩下的按 LowerCenter 往下顶
        }

        // 每条的色（`Show` 时记下；`ApplyVisual` 每帧要用它重建带 alpha 的色）
        readonly Dictionary<Item, Color> _colorOf = new Dictionary<Item, Color>();
        Color _lastColorOf(Item it)
        {
            Color c;
            return _colorOf.TryGetValue(it, out c) ? c : ErrorColor;
        }

        /// <summary>把「这一条现在该是什么样」落到对象上（缩放 + 两层的 alpha）。</summary>
        void ApplyVisual(Item it, Color color)
        {
            _colorOf[it] = color;

            float s = it.t < AppearAnimationTime
                    ? Mathf.Lerp(AppearScaleFromMultiplier, 1f, EaseOut(it.t / AppearAnimationTime))
                    : 1f;
            float a = it.t <= TimeToStartFading
                    ? 1f
                    : Mathf.Clamp01(1f - EaseOut((it.t - TimeToStartFading) / TimeToFade));

            if (it.root != null) it.root.localScale = new Vector3(s, s, s);
            if (it.barParts != null)
                for (int i = 0; i < it.barParts.Count; i++)
                    if (it.barParts[i] != null)
                        it.barParts[i].SetTint(new Color(BarTint.r, BarTint.g, BarTint.b, BarTint.a * a));
            if (it.text != null) it.text.SetColor(new Color(color.r, color.g, color.b, color.a * a));
        }

        /// <summary>那两条 tween 的缓动（文件头 ⑩：原版没写 `SetEase` ⇒ 吃 DOTween `defaultEaseType`
        /// = **`Ease.OutQuad`** = `1 − (1−x)²`）。⚠️ 「原版就是 OutQuad」是**推断**（原版那份
        /// `DOTweenSettings` 本地找不到），本仓自己的那份读出来也是 6 = OutQuad。</summary>
        public static float EaseOut(float x)
        {
            x = Mathf.Clamp01(x);
            return 1f - (1f - x) * (1f - x);
        }

        // ==================================================================
        //  自检口（⛔ 生产路径不碰）
        // ==================================================================

        /// <summary>第 i 条现在亮着吗。</summary>
        public bool ActiveOf(int i) { return i >= 0 && i < _items.Count && _items[i].active; }
        /// <summary>第 i 条的存活时间（秒）。</summary>
        public float TimeOf(int i) { return i >= 0 && i < _items.Count ? _items[i].t : -1f; }
        /// <summary>第 i 条现在**实际挂在节点上**的缩放（读回真值 = `localScale.x`）。</summary>
        public float ScaleOf(int i) { return i >= 0 && i < _items.Count ? _items[i].root.localScale.x : -1f; }
        /// <summary>第 i 条的文案（`Label` 上那一份）。</summary>
        public string TextOf(int i) { return i >= 0 && i < _items.Count && _items[i].text != null
                                            ? _items[i].text.Text : null; }
        /// <summary>第 i 条**底条**上现在实际那一份色（读回 `ImageQuad.Tint` 的真值，⛔ 不是记账位）。</summary>
        public Color BarTintOf(int i)
        {
            if (i < 0 || i >= _items.Count || _items[i].barParts == null || _items[i].barParts.Count == 0)
                return new Color(0f, 0f, 0f, -1f);
            return _items[i].barParts[0].Tint;
        }
        /// <summary>第 i 条**文字**上现在实际那一份色（读回 `Label.color` 的真值）。</summary>
        public Color TextColorOf(int i)
        {
            if (i < 0 || i >= _items.Count || _items[i].text == null) return new Color(0f, 0f, 0f, -1f);
            return _items[i].text.color;
        }
        /// <summary>第 i 条底条**画出来**的尺寸（px）= 九宫格子块的**并集包围盒** ⇒ 用来钉
        /// 「宽 = 文字宽 + 240 / 高 = max(56.85, 文字高) + 4」那条规则。
        /// ⚠️ 由子块并集算（根节点自己没有 `ImageQuad`，见 `SettingsPanel.RectContains` 那条同款注释）。</summary>
        public Vector2 BarSizeOf(int i)
        {
            if (i < 0 || i >= _items.Count) return new Vector2(-1f, -1f);
            return BarSizeOfParts(_items[i].barParts);
        }

        /// <summary>九宫格子块的并集包围盒（px）。</summary>
        static Vector2 BarSizeOfParts(List<ImageQuad> parts)
        {
            if (parts == null || parts.Count == 0) return new Vector2(-1f, -1f);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            int n = 0;
            foreach (var q in parts)
            {
                if (q == null) continue;
                var l = q.transform.localPosition;
                minX = Mathf.Min(minX, l.x - q.WorldW * 0.5f); maxX = Mathf.Max(maxX, l.x + q.WorldW * 0.5f);
                minY = Mathf.Min(minY, l.y - q.WorldH * 0.5f); maxY = Mathf.Max(maxY, l.y + q.WorldH * 0.5f);
                n++;
            }
            if (n == 0) return new Vector2(-1f, -1f);
            return new Vector2((maxX - minX) * 108f, (maxY - minY) * 108f);
        }

        /// <summary>第 i 条**根节点**的自上而下中心（px）。用它钉布局算法（文件头 ④⑪：单条时
        /// 战斗档 y = `510.23974609375`、菜单档 y = `311.6400146484375`）。</summary>
        public Vector2 ItemCenterPxOf(int i)
        {
            if (i < 0 || i >= _items.Count || _items[i].root == null) return new Vector2(-9999f, -9999f);
            return LayoutSpace.ToPixel(_items[i].root.position);     // 世界坐标 ⇒ 不受父链偏移影响
        }
        /// <summary>第 i 条的底条/文字是不是**被关着的时候也留着**（自检要能分辨「藏了」与「根本没建」）。</summary>
        public bool BarBuiltOf(int i) { return i >= 0 && i < _items.Count && _items[i].barRoot != null; }

        /// <summary>把池里所有条收干净（= 原版 `OnKill` 那一支的等价物，文件头 E）。
        /// ⚠️ 生产路径今天没有调用点（原版也没有「换局清横幅」这一句）。</summary>
        public void HideAll()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                it.t = 0f;
                it.active = false;
                if (it.root != null)
                {
                    it.root.localScale = Vector3.one;
                    it.root.gameObject.SetActive(false);
                }
            }
            RelayoutActive();
        }
    }
}
