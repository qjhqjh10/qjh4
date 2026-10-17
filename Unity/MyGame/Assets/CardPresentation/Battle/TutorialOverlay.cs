// TutorialOverlay.cs — **教程表现层**（原版 `battlearena1` 里那棵 `Tutorial` 子树的等价物）
//
// ============================ 判据（唯一正本 = 解包资源 + 反编译）============================
// 全部现读（2026-10-18，写手代理 W2）：
//   · 场景树：`d:/4/Unity/资料/说明书/01_战斗_对战/2D层_battlearena1全树.md` 的 `Tutorial` 那一段（`:746-753`
//     与 `:718-723`）；那一份的表头写着 **`坐标=Godot(1920x1080, y向下)`**。
//   · 逐元素出处：`资料/普查产出_1018/R2_教程族现核.md` **§4「层 × 场景 × 出现条件」表**（R2 §4 是**表**，
//     本文件是它的落地；表里每一格都有 `文件:行号`）。
//   · 枚举：`d:/2/tools/il2cpp_out/dump.cs` **TypeDefIndex 478**（`PositionReference`）·
//     **479**（`PositionRelation`）· **480**（`TutorialTipParams`）· **481**（`SmallTipData`）。
//   · 动作档：`d:/2/tools/decomp_full/AiScripted__ExecuteAction.c`（`SmallTip 80` 那一支 `:297-320`
//     → `BattleManager__ShowTutorialTip`）· `AiScripted__EnableTutorialHighlight.c`（高亮）。
//
// ---- 原版那棵树长什么样（照抄，`2D层_battlearena1全树.md`）----
//   ```
//   - Tutorial [-960,540 1920x1080]                       ← 全屏、中心锚
//     - InitTutorialTip (inactive) [-50,1030 100x100]     ← 开场两条（谁启用**查不到**）
//       - TipText [-4,1080 3x1] → TMP SubMesh / Bg / arrow1 (1)
//       - TipText (1) [2,1080 3x1] → TMP SubMesh / Bg (1) / arrow1 (2)
//     - TutorialArrows
//       - arrow1 (inactive) · arrow2 (inactive)           ← **SpriteRenderer，不是 UGUI Image**
//     - TutorialTip [-211,1006 430x150]                   ← 小提示（`TutorialTipScript`）
//       - Generic Popup Background [-214,1081 436x0] → TipText (inactive) / Bg (inactive) / ContinueText (inactive)
//     - TutorialPointerCombat (inactive) [-960,540 1920x1080]
//       - Pointer [-84,1090 100x100] → light [-84,1090 100x100]
//     - TutorialObjs ...                                  ← 战斗教学标注四块（`UnitObjs` + `EnergyText`）
//   - Bottom buttons [-343,1370 687x100] → SkipTutorial Button [22,1375 300x90] 文案 'Skip tutorial'
//   ```
//
// ============================ 🔴 三条必须写清的边界 ============================
//  ① **尺寸是判据、屏幕位置不是**（铁律 3）。上面每个方的 `w×h` 我都**照抄**了；
//     但**摆在哪**那一栏，原版这几层里 `TipText`/`Bg`/`ContinueText` **在场景里是 `(inactive)`**
//     （运行期才摆），真正的定位链是 `AiScripted.GetTipPosition(positionReference, positionRelation, referenceUnit)`
//     —— 我们把**枚举那一段**还原了（锚到哪个 HUD 元素 + 左/右/上，见 `TutAnchor` / `TutRelation`），
//     **具体像素偏移（`Gap` / `TipPx` 与标签内边距）是我们挑的**，逐处标了「我们挑的」。
//     ⛔ 那些数**不是**「原版就是这样」。
//  ② **原版那三处箭头/光标的 sprite 名查不到** —— 🔴 **2026-10-18（`Z4`）就地订正（铁律 5）**：
//     · **左右箭头 `TutorialArrows/arrow1·arrow2`**：`SpriteRenderer_1760/1762.json` 的
//       `m_Sprite` 是 **`{m_FileID:0, m_PathID:0}`（真·空引用）**，而 `TutorialTipScript__SetupArrows.c`
//       只做 `SetSpriteAlpha` + `SetActive` + `DOFade`（**不改 sprite**）
//       ⇒ **原版这一层本来就没有图**（不是「我们查不到」）。我们的替代箭头是**自造的** ——
//       数据 6 关 `showLeftArrow/showRightArrow` **全 false** ⇒ **一次都不会亮**，零可见影响。
//     · **指点光标 `Pointer` / `light`**：**名字查到了** —— 场景里序列化的是
//       **`Cursor_Space Marine_01`**（pid `-2590500462584155290`）与
//       **`Cursor_Space Marine_01Glow`**（pid `-592821608806009055`），
//       出处 = `d:/2/Warpforge_tools/data/ui_extract/battlearena1_sprite_map.json`（pid→名字的索引表）
//       + `bundle_armycursors_assets_all`（21 张 `Cursor_<阵营>_01` / `…Glow`）。
//       🔴 **但它们运行时会被换掉**：`TutorialPointer__SetupCursor.c` →
//       `ArmyUtilities.GetCursorForArmy(army, out normal, out glow)` → `Image.set_sprite(...)`
//       按**阵营**取。那张对应表在 `CursorByArmySO`（**本地没解出来**，全库只有它的 MonoScript）
//       ⇒ **阵营 → 光标**的对应**没有判据**，⛔ 不猜 ⇒ 我们**继续用 `40k_general_bt_arrow`** 当载体，
//       并如实标「真载体是光标图，不是这张」。
//     · **提示底板**：✅ **查到真名了 ⇒ 已换**：`Generic Popup Background`（pid `-7511397500040153103`）
//       = **`40k_popup`**（同一张索引表读出）。⛔ 不再是「我们挑的 `40K_display`」。
//     · **跳过钮底板**：✅ **2026-10-18（`G9`）查到真名 ⇒ 已换 = `40K_button`**。
//       原版节点 `Bottom buttons/SkipTutorial Button` 的 `Image`（`MonoBehaviour_4721.json`）里
//       `m_Sprite` = `{ m_FileID: 8, m_PathID: 5651555388418207694 }`，那个 pid 在索引表
//       `d:/2/Warpforge_tools/data/ui_extract/battlearena1_sprite_map.json` 里 = **`40K_button`**
//       （配 `m_Color` 绿染 + `m_PreserveAspect = 1` 内接进 300×90 —— 细节见 `Build()` 里那段）。
//       ⛔ **不再是「我们挑的 `40K_display`」**；原来那句「图没读到」已作废（判据链见 `Build()`）。
//       🔴 **2026-10-18（A991）：这颗钮已经搬进 `Battle/SettingsPanel.cs`** ⇒ 上面这一整条
//          （图名 / 绿染 / `PreserveAspect` / 300×90）**跟着搬过去**（`SettingsPanel` 文件头那组
//          `Skip*` 常量 + 它的 `Build`）。本件**不再建那颗钮**，这一段只当**判据留档**。
//  ③ **左右箭头本轮零可见效果**：6 关 430 条动作里 `showLeftArrow`/`showRightArrow` **全是 `false`**
//     （R2 §4 实测；我按 61 条 `SmallTip` 复核过 = 0/0）⇒ **机制照做、本轮一次都不会亮**。
//
// ============================ 批处理下没有帧循环 ============================
// 原版 `TutorialTipScript` 的淡入是 `fadeDuration = 0.2` 的一段补间 —— 我们**不走 `Update`**
//   （批处理里它不是每帧跑的），改成 **`Tick(dt)` 显式步进**，由 `BattleDriver.Update`/自检喂。
// ═════════════════════════════════════════════════════════════════════════════
//  🔴🔴 **2026-10-16（`T09` 批 3 · 本体）：图/字整批从【世界空间 quad】迁到【uGUI 子画布】**
//
//  · **原版是【三件、三个宿主】**（判据 = `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一
//    的 `TutorialOverlay` 那一行 —— 逐值抄，⛔ 别自己编）：
//      ① `Tutorial` @ `…/Safe area FrontCanvas`（`CV_2773` · `RT_3206`）→ **`HandMoving` 层 / order 1500 / 兄弟序 11/14**；
//      ② `Tutorial highlight` @ **根 `Canvas` 的直子**（`CV_2774` · `RT_3501`）→ **`HandMoving` 层 / order 601 / 兄弟序 5/8**；
//      ③ `TutorialPointerCombat` @ **`…/Tutorial`**（`CV_2766` · `RT_3572`）→ **`OverHand` 层 / order 10 / 兄弟序 3/4**。
//    ⇒ 我们落成**三块子画布**（`HudCanvas.EnsureSubCanvas`）：`Tutorial Canvas` /
//      `Tutorial highlight Canvas` / **嵌在第一块下面的** `TutorialPointerCombat Canvas`
//      —— 名字 = 下面那三个 `public const string`，⛔ 别在别处再写一份字面量。
//  · 🔴 **那三块各自的 rect【不同】**（⛔ 不是一律 `FillParent`）—— 判据 = 逐值现读 a2 的 `RectTransform`：
//      · `RT_3206`（`Tutorial`）：**中心锚 `(0.5,0.5)` + `pivot (0.5,0.5)` + `ap (0,0)` + `sd 1920×1080`**；
//      · `RT_3572`（`TutorialPointerCombat`）：**同形**（`sd` 也是 `1920×1080`，父 = `3206` ✓ 与「它在 `Tutorial` 下」自洽）；
//      · `RT_3501`（`Tutorial highlight`）：**stretch**（`aMin (0,0)` / `aMax (1,1)` / `sd (0,0)`）=
//        `EnsureSubCanvas` 的出厂形状（它父 = `2952` = 根 `Canvas`）。
//    ⇒ 前两块建完各补一句 `HudCanvas.SetPxSize(rt, 1920, 1080)`（那个口就是「中心锚 + 尺寸」），
//      ⛔ **别 `FillParent` 它们**（那是 stretch，4:3 下与原版差一截）。
//  · 🔴 **双模式（主对话裁定的硬要求）**：`Create(parent, uguiHost)` 的 `uguiHost` **传了** ⇒ 画布档；
//    **不传 / 传 null**（出厂）= **世界档 —— 与改之前逐位相同**。先例 = `Tooltip.UseHost` ·
//    `SkillPanel.UseHost` · `AlliancePanelWindow.Create(root, uguiHost:)`。
//    ⚠️ **上游登记点在 `BattleDriver.EnsureTutorialOverlay`（不在本件白名单）** ⇒ 本件只做好能力，
//    接线那一行见本轮报告 §③；**没登记 = 全跑世界档**（⛔ 不是半迁移态）。
//  · **坐标**：两档共用同一组**世界单位**常量；画布档把**世界点**经**该画布自己的**
//    `InverseTransformPoint` 折成设计 px（`CvPlace`）—— ⛔ **别写死 ÷108**：画布单位↔世界单位的
//    **两个轴系数不同、且随屏变**（`T09` §七·8），写死只在 16:9 对。
//    **尺寸**那一侧：世界档照旧「按**高**定、宽由贴图比例来」；画布档把同一个高 ×108 折成 px 写进
//    `sizeDelta`（`MakeImg`）—— 两档在 16:9 下**逐位相同**。
//  · 🔴 **「队列 → 兄弟序」换法在这件上【不适用】**：本件只有两处调过 `SetRenderQueue`
//    （高亮 601 / 光标 600），而它们**各自独占一块子画布** ⇒ 先后由**子画布的 (层, order)** 决定，
//    ⛔ 不需要「取负」那一条（那条管的是**同一容器内**的 `renderQueue` 阶梯，见 `T09` §五·补⑩ 关卡 1）。
//    画布档那两句 `SetRenderQueue` 走 `SetQ`（`Quad == null` ⇒ 空操作，层级归画布管）。
//  · ⚠️ **如实记两条差异**（`T09` §五·补⑩ 二：只按「谁的 canvas 装谁」迁，差别记着）：
//      ① **`TutorialObjs`（教学标注四块）的父链与原版不同** —— 原版它在 `Card Display/Card Display Window`
//         那一支（HUD，全树 `:578`），本件把它**建在 `Tutorial Canvas` 里**（谁建它就跟谁，⛔ 没顺手改层级）；
//      ② **画布上的件不跟 `HudRoot` 的震屏**（画布是根对象；`ShakeCamera(cam, hudRoot, …)` 只动 HUD 根）
//         —— 与 `SkillPanel` 那条同源，非 16:9 / 震屏那一档如实记。
// ═════════════════════════════════════════════════════════════════════════════
using UnityEngine;
using TMPro;

namespace CardPresentation
{
    /// <summary>原版 `PositionReference`（`dump.cs` **TypeDefIndex 478**，全文照抄）——
    /// 「这一层贴在哪个 HUD 元素上」。
    /// ⚠️ `dump.cs` 那份在**全局命名空间**里；本工程的 `Core/`（`RuleEngine`）不许引 Unity
    /// ⇒ 这边**自己定一份同名的**，值**逐条相等**（⛔ 别改值：数据里存的就是这些数）。
    /// ⚠️ 我们只落地了**用得到的那几个**（数据实测 61 条 `SmallTip` 用到 10/13/21/22/23/30/40/60/80/0，
    /// 39 条高亮用到 0/30/40/60/80/90）；其余值**认得出但不产坐标** ⇒ 见 `BattleDriver.TutorialAnchor`。</summary>
    public enum TutAnchor
    {
        Center = 0,
        EnemyWarlord = 10, EnemyWarlordMelee = 11, EnemyWarlordRange = 12, EnemyWarlordHealth = 13,
        PlayerWarlord = 20, PlayerWarlordMelee = 21, PlayerWarlordRange = 22, PlayerWarlordHealth = 23,
        EnemyMinion = 30, PlayerMinion = 40,
        EnemyCardInHand = 50, PlayerCardInHand = 60,
        EnemyMana = 70, PlayerMana = 80,
        EndTurn = 90, ActiveAbility = 100, ChatButton = 110, Cemetery = 120,
    }

    /// <summary>原版 `PositionRelation`（`dump.cs` **TypeDefIndex 479**）—— 相对锚点摆在哪边。</summary>
    public enum TutRelation { Exact = 0, LeftOf = 10, RightOf = 20, Above = 30 }

    /// <summary>
    /// **教程那一整棵表现子树**（原版 `Tutorial` 根 + `TutorialTip` + `TutorialArrows` + `Tutorial highlight`
    /// + `TutorialPointerCombat` + `TutorialObjs`）。
    ///
    /// 🔑 **为什么合成一个文件/一件**：它们共享同一套「什么时候亮」的驱动（脚本动作 + `playerAction`），
    ///    拆成六个文件只会让「谁该跟着谁关」更难查 —— 那正是本工程踩过的坑（「建了却没跟着开关隐藏」）。
    ///
    /// 🔴 **2026-10-18 就地订正（铁律 5）**：本段原来写的理由是「原版那六层**本来就挂在同一个 `Tutorial` 根下**」
    ///    —— **不成立**。逐场现读 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1`
    ///    （`RectTransform/` 按 `m_Father` 逐跳解父链）＋ `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md`，
    ///    **只有 4 层**在 `Tutorial`（pid 3013）之下：`InitTutorialTip` / `TutorialArrows` / `TutorialTip` /
    ///    `TutorialPointerCombat`（= 文件头那棵树）。另三层**都不在这棵根下**（arena1 实读的父链）：
    ///      · `TutorialObjs` ⇒ `Card Display/Card Display Window`（HUD 那一支，全树 `:578`）；
    ///      · `Tutorial highlight` ⇒ **`Canvas` 的直接子物体**（与 `FrontCanvas` 平级，全树 `:799`）；
    ///      · `SkipTutorial Button` ⇒ **`BattleSettingsPanel/Bottom buttons`**（那是**设置窗里的钮**，
    ///        `BattleSettingsPanel` 在场景里是 `(inactive)`，全树 `:673`/`:718-722`）—— **不是**教程根下的 HUD 钮。
    ///    ⇒ 合并成一件的理由是**共享同一套显隐驱动**，**不是**「同一个根」。⛔ 别再写回「六层同一个根」。
    ///
    /// 🔴 **2026-10-18（A991）：`SkipTutorial Button` 这一层已经搬出本文件** —— 它建在
    ///    `Battle/SettingsPanel.cs` 里（原版那颗的父链就是 `BattleSettingsPanel/Bottom buttons`）。
    ///    ⛔ 本件**不再建那颗钮**，全库只有 `SettingsPanel` 建。
    ///    🔴 **2026-10-18（第三会话 · 铁律 5 就地订正）**：本段原来还写着「本件只剩它那几个只读转发口
    ///    （`SkipPanel` / `SkipVisible` / `SkipWorldPos` / `ClickSkipAt`）」—— **那四个口已删**，
    ///    本件**不再有那颗钮的转发口**。经过：
    ///      · **A991 那一轮为什么留**：`Editor/BattleScene.cs` 的断言那时还在读它们，而那个宿主
    ///        不在当轮白名单里 ⇒ 先留**零像素的转发口**（⛔ 当时也没建钮）。
    ///      · **现在为什么能删**：那一批已改成直接读真身（`driver.Settings` 的 `HitSkipTutorial` /
    ///        `SkipTutorialWorldPos` / `SkipTutorialShown`）⇒ 全仓**零代码调用**（grep 证据见
    ///        `资料/普查产出_1018第三会话/W_收尾小账.md` ④；剩下几处提及全在**注释**里）。
    ///      · ⚠️ **`minTimeBeforeSkip` 那道闸【没动】** —— `MinTimeBeforeSkip` / `SetSkip` /
    ///        `SkipAllowed` / `TrySkip` / `SkipClickedByPlayer` 都还在。它是**本工程自己留的闸**
    ///        （原版那颗钮没有），且 `Editor/BattleScene.cs` 有断言钉着（已如实标为真偏离）。
    ///
    /// ⛔ **一件都不许静默失败**：拿不到的图/查不到的参数一律 `Debug.LogWarning`（且**同一条只吼一次**）。
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        // 🔴 **2026-10-18（第三会话）：这里原来有 `public SettingsPanel SkipPanel;`（A991 的转发口宿主）
        //    —— 【已删】。** 它是「跳过钮搬进 `SettingsPanel`」那一轮留下的**纯兼容转发口**
        //    （零像素、自己不建任何东西），由 `BattleDriver` 在两处幂等注入（那两处注入上一批已删）。
        //    删它的依据：全仓**零代码调用**（`BattleScene.cs` 已改读 `driver.Settings` 的真身），
        //    `grep` 逐条见 `资料/普查产出_1018第三会话/W_收尾小账.md` ④。
        //    ⛔ **别为了「接口齐整」把它加回来** —— 那颗钮的真身在 `Battle/SettingsPanel.cs`。

        // ------------------------------------------------------------------
        //  参数（哪几个是判据、哪几个是我们挑的 —— 逐条标了）
        // ------------------------------------------------------------------
        /// <summary>设计像素 → 世界单位（`LayoutSpace.DesignPxH / DesignHeight` = 108）。</summary>
        const float Px = 108f;

        /// <summary>小提示面板的**尺寸**（px）—— ✅ **判据**：`TutorialTip [-211,1006 430x150]`。</summary>
        const float TipPxW = 430f, TipPxH = 150f;
        /// <summary>开场两条提示的**尺寸**（px）—— ✅ 判据：`InitTutorialTip [-50,1030 100x100]`
        /// （那 100×100 是它的 rect；底下的 `TipText` 是 3×1 的 TMP 占位 ⇒ 实际尺寸由文本决定）。</summary>
        const float InitTipPxW = 700f, InitTipPxH = 100f;
        /// <summary>指点光标 **100×100**（px）—— ✅ 判据：`Pointer [-84,1090 100x100]`。</summary>
        const float PointerPx = 100f;

        // ── 🆕 2026-10-16（`T09` 批 3）：那几件的 **z 档** ─────────────────────────────────────
        //  ⚠️ **两档都用这几个数，但含义不同**：世界档它就是**世界 z**（大的离相机远、画在后面）；
        //    画布档它只用来登记**兄弟序**（`HudCanvas.Add`：**z 大 ⇒ 排前面 ⇒ 画在底下**，两套**同向**）。
        //  ⚠️ 值**逐位照抄改之前那几个字面量**（`0.05` / `0.04` / `0.06`）—— 世界档一个字节都没动。
        /// <summary>小提示那一层（改之前 `SetTip` 里那句 `p.z = 0.05f`）。</summary>
        const float ZTip = 0.05f;
        /// <summary>小提示**底板**的 z（**只在画布档用**）—— 世界档那件与它的两颗字**共面**（都在 `ZTip`），
        /// 而画布档必须把先后定死 ⇒ 底板取**比字大**的 z = 排在前面 = 画在**底下**（意图 = 底板在字后面）。
        /// 判据：世界档那件是底板（先建）、字压在上面（后建）—— 这个顺序就是它的等价物。</summary>
        const float ZTipBg = 0.07f;
        /// <summary>箭头 / 教学标注 / 开场两条那一档（改之前它们在根下 z 恒 0）。</summary>
        const float ZFlat = 0f;
        /// <summary>高亮那一层（改之前 `SetHighlight` 里那句 `0.04f`）—— 画布档它**独占一块画布**，
        /// 这个数只剩「在它自己那块画布里的排位」这一个意义。</summary>
        const float ZHighlight = 0.04f;
        /// <summary>指点光标（改之前 `SetPointer` 里那句 `0.06f`）—— 同上，画布档独占一块画布。</summary>
        const float ZPointer = 0.06f;

        /// <summary>一个 `Label` 的 `scale` 档在**画布档**里等于多少设计 px。
        /// <para>🔴 **为什么不是 `Label.CapPxOfScale` 那个 7**：世界档一个档 = `7 / Label.PixelsPerUnit(100)`
        /// = **0.07 世界单位**，而 HUD 的世界比例是 **108 px / 世界单位**（`LayoutData`：`DesignPxH /
        /// DesignHeight`）⇒ 渲出来是 **7.56 屏 px**。画布档一个单位 = 1 屏 px（16:9）⇒ 取 7.56 才与
        /// **画出来的**大小逐位相同；直接传整数 `scale` 会让画布档的字**小 7.4%**（那是 `Label` 那条
        /// 「跨档同数」约定的口径，⛔ 不是「渲出来一样」）。
        /// 判据 = 本批通例「两档在 16:9 下逐位相同」＋ `T09` §五·补⑨ 四。</summary>
        const float CapPxPerScale = 7f / 100f * Px;
        // 🔴 **2026-10-18（A991）：跳过钮的三样常量（`SkipPxW/SkipPxH` 300×90 · 底板图 `40K_button` ·
        //    染色 (0.3686,0.8941,0.5874,1)）跟着那颗钮一起搬进了 `Battle/SettingsPanel.cs`**
        //    （那边叫 `SkipWPx/SkipHPx` 与 `ResignTint`/`ResignBorder*`）—— 本件不再持有它们。
        //    ⛔ 别在这儿复活一份（「两处写同一条规则 = 迟早不一致」）。
        /// <summary>淡入时长（秒）—— ✅ 判据：`TutorialTipScript.fadeDuration = 0.2`。</summary>
        public const float FadeDuration = 0.2f;
        /// <summary>`minTimeBeforeSkip = 1.0`（原版 `TutorialTipScript`）—— 提示刚弹出来这 1 秒内**不许跳过**
        /// （跳过按钮的判据在 `TutorialOverlay.SkipAllowed`）。
        /// 🆕 2026-10-18（`Z6`）：`BattleDriver` 那条「消提示」的闸**复用同一个常量**
        /// （原版两处都读 `TutorialTipScript` 这一格）⇒ 改成 `public`，⛔ 别在那边另立一个 1.0。</summary>
        public const float MinTimeBeforeSkip = 1.0f;

        /// <summary>🔴 **已停用（2026-10-18 · `Z5`）**：提示层与锚点之间的空隙。
        /// 原来这里写着「原版那个偏移在 `AiScripted.GetHorizontalOffset` 里（方法体拿不到），
        /// 这个数没有判据」—— **那半句是错的**：`GetHorizontalOffset` 与 `GetTipPosition`
        /// 的方法体**都在**（`d:/2/tools/decomp_full/`），偏移量的规矩也已经逐字照抄进 `SetTip`。
        /// ⛔ 这个常量从此**不再参与摆位**（留着只为不让「删掉它」变成一次无谓的 diff）。
        /// 判据与实读值 → `SetTip` 与 `TutTipAboveFactor` 的注释。</summary>
        const float Gap = 0.14f;
        /// <summary>🔴 **我们挑的**：高亮层在锚点矩形外扩的边距（世界单位）。</summary>
        const float HighlightPad = 0.06f;
        // 🔴 **2026-10-18（A991）删掉了 `SkipAt01`**（原来是 `new Vector2(0.5f, 0.075f)`，
        //   注释写着「原版那个 `[22,1375]` 的父链偏移没能标定 ⇒ 摆位是我们挑的」）。
        //   **那一句现在有确切答案了**：在**面板坐标系**里它是 `(+171.7, −310.5)`、300×90
        //   （判据 = `menu_dump.py … "BattleSettingsPanel"` 的绝对矩形 + 面板框心，见
        //    `SettingsPanel` 文件头那组 `Skip*` 常量）。摆位随那颗钮一起搬走了 ⇒ 这一格**不再需要**。

        // ------------------------------------------------------------------
        //  层
        // ------------------------------------------------------------------
        Transform _root;
        GameObject _tipGo; Label _tipText, _tipContinue;
        /// <summary>小提示底板 —— 🔴 **2026-10-16（`T09` 批 3）：类型 `ImageQuad` → `HudPiece`**
        /// （画布档下它是 uGUI 件）。⛔ **一个件只许在一边** —— 见 `HudPiece` 文件头那条铁律。</summary>
        HudPiece _tipBg;
        GameObject _initGo; Label _initText1, _initText2;
        /// <summary>左右箭头（原版 `TutorialArrows/arrow1·arrow2`，**原版这一层本来就没有图**，见文件头 ②）。</summary>
        HudPiece _arrowL, _arrowR;
        /// <summary>高亮那一层（原版 `Tutorial highlight`，独占一块子画布）。</summary>
        HudPiece _highlight;
        /// <summary>指点光标（原版 `TutorialPointerCombat/Pointer`，独占一块子画布）。</summary>
        HudPiece _pointer;
        GameObject _annoGo; Label[] _annoLabels = new Label[4]; HudPiece[] _annoArrows = new HudPiece[4];
        // 🔴 **2026-10-18（A991）删掉了 `_skipGo` / `_skipLabel` / `_skipBg`** —— 那颗钮**搬进了
        //    `Battle/SettingsPanel.cs`**（本件**不再建它**，全库只有那一处建）。本件只留两样：
        //    `MinTimeBeforeSkip` 那一档的计时 + 玩家点过的记账。
        /// <summary>`MinTimeBeforeSkip` 那一档的计时**起点**已经起过没有（幂等闩）。
        /// 原来这一格是「那颗钮亮着没有」（`_skipGo.activeSelf`），钮搬走之后换成显式闩 ——
        /// **`SetSkip` 的语义逐字不变**（见那个方法）。</summary>
        bool _skipArmed;

        // ── 🆕 2026-10-16（`T09` 批 3）：画布那一侧（判据与口径 → 本文件头部那段）────────────────
        /// <summary>本件**主**那一块子画布的 `RectTransform`（原版 `Tutorial`，`HandMoving` / order **1500**）。
        /// <para>`null`（出厂）= **世界档** —— 与改之前**逐位相同**；非 null = **画布档**
        /// （件建在它下面、坐标 = **设计 px**）。⛔ **绝不许两边都建**（同 `HudPiece` / `Label` 那条铁律）。
        /// ⚠️ 另两块（高亮 / 光标）见 <see cref="_cvHl"/> / <see cref="_cvPtr"/>。</para></summary>
        RectTransform _cvTip;
        /// <summary>高亮那一块的子画布（原版 `Tutorial highlight`，**根 `Canvas` 的直子**，
        /// `HandMoving` / order **601**）—— 它**不是** `_cvTip` 的子件（原版就不是）。</summary>
        RectTransform _cvHl;
        /// <summary>指点光标那一块的子画布（原版 `TutorialPointerCombat`，**`Tutorial` 的子件**，
        /// `OverHand` / order **10**）⇒ 它**嵌在** <see cref="_cvTip"/> 下面（原版父链 `RT_3572.m_Father = 3206`）。</summary>
        RectTransform _cvPtr;
        /// <summary>`Build()` 跑过了没 —— `UseHost` 排在 `Build()` 之后时要**拆旧建新**（见它）。</summary>
        bool _built;

        /// <summary>主那块子画布的名字（= 原版宿主名 + `" Canvas"`，`T09` §五·补⑩ 第 1 步的口径）。
        /// 判据 = `T09c` §一 我这一行：`Tutorial` @ `…/Safe area FrontCanvas`，`HandMoving` / **1500**。
        /// ⛔ **只此一份**（建它 / 报告 / 自检找它都取这里）。</summary>
        public const string SubCanvasName = "Tutorial Canvas";
        /// <summary>高亮那块子画布的名字（原版 `Tutorial highlight` @ 根 `Canvas` 直子，`HandMoving` / **601**）。</summary>
        public const string HighlightCanvasName = "Tutorial highlight Canvas";
        /// <summary>指点光标那块子画布的名字（原版 `TutorialPointerCombat` @ `…/Tutorial`，`OverHand` / **10**）。</summary>
        public const string PointerCanvasName = "TutorialPointerCombat Canvas";
        /// <summary>三块子画布的 order（判据 → `T09c` §一 我那一行的 order 列；层见 <see cref="EnsureCanvases"/>）。</summary>
        public const int TipSortingOrder = 1500, HighlightSortingOrder = 601, PointerSortingOrder = 10;

        /// <summary>本件现在是不是**画布档**（诊断 / 断言 / 自检用；**判据只此一处**）。
        /// ⚠️ 它只看「主画布登记了没」—— 要判「件真的建在画布上」用 <see cref="OnCanvas"/>。</summary>
        public bool UguiMode { get { return _cvTip != null; } }
        /// <summary>自检用：三块子画布的 `RectTransform`（查它们的层 / order / `overrideSorting` / rect 用）。</summary>
        public RectTransform CanvasRect { get { return _cvTip; } }
        /// <inheritdoc cref="CanvasRect"/>
        public RectTransform HighlightCanvasRect { get { return _cvHl; } }
        /// <inheritdoc cref="CanvasRect"/>
        public RectTransform PointerCanvasRect { get { return _cvPtr; } }

        /// <summary>小提示里那一行 `Continue` 的**词条键**（`_tipContinue` 上的字）。
        /// = **原版词条**，出处 = `Core/Loc.cs` 那条（原版 `ContinueText` 那颗 TMP 逐字 `Continue`；
        /// EN 列照它、ZH 列「继续」来自 `数据/本地化/i18n/zh_CN.csv:84`）。
        /// ⚠️ 6 关 61 条**全是 `tipWithContinue = false`** ⇒ 这一行今天**一条都不显示**（机制照做，
        /// 见 `Editor/BattleScene.cs` 那条断言）—— 换 key 是为了「真亮起来时两档都对」。</summary>
        public const string ContinueTerm = "Battle/Tips/Continue";

        float _fade;                 // 0..1（当前不透明度）
        bool _wantVisible;           // 这一层「该不该显示」
        bool _skippedByPlayer;       // 玩家点过跳过（`BattleDriver` 读它决定要不要收摊）
        float _shownFor;             // **跳过那一档**已经亮着多久（`MinTimeBeforeSkip` 用）

        // ------------------------------------------------------------------
        //  自检读口（全部只读）
        // ------------------------------------------------------------------
        public bool TipVisible { get { return _tipGo != null && _tipGo.activeSelf; } }
        public string TipText { get { return _tipText != null ? _tipText.Text : null; } }
        public string TipContinueText { get { return _tipContinue != null ? _tipContinue.Text : null; } }
        public float TipAlpha { get { return _fade; } }
        public Vector3 TipWorldPos { get { return _tipGo != null ? _tipGo.transform.position : Vector3.zero; } }
        public bool InitTipVisible { get { return _initGo != null && _initGo.activeSelf; } }
        public bool HighlightVisible { get { return _highlight != null && _highlight.gameObject.activeSelf; } }
        public bool PointerVisible { get { return _pointer != null && _pointer.gameObject.activeSelf; } }
        public bool PointerUsesArrowArt { get { return _pointer != null; } }
        public bool AnnotationsVisible { get { return _annoGo != null && _annoGo.activeSelf; } }
        /// <summary>🆕 **2026-10-17（`N49①`）自检口**：第 `i` 颗教学标注的 `Label`（`0..3` = Melee/Ranged/Health/Energy）。
        /// 只读；`Build()` 之前 / 下标越界 ⇒ null。断「细黑描边真上到**实例材质**上」用它。</summary>
        public Label AnnoLabel(int i)
        {
            return (i >= 0 && i < _annoLabels.Length) ? _annoLabels[i] : null;
        }
        /// <summary>🆕 **2026-10-17（`A1024`③ 批 2–4）自检口**：提示条两颗字（**只给自检读**）。
        /// `0` = `TipText` · `1` = `ContinueText`；没建出来 / 下标越界 ⇒ `null`。</summary>
        public Label TipLabel(int i)
        {
            switch (i) { case 0: return _tipText; case 1: return _tipContinue; }
            return null;
        }
        /// <summary>同上的自检口：开场提示两颗字。`0` = `TipText` · `1` = `TipText (1)`。</summary>
        public Label InitLabel(int i)
        {
            switch (i) { case 0: return _initText1; case 1: return _initText2; }
            return null;
        }
        /// <summary>🔴 **2026-10-16（`T09` 批 3）：`ContinueText` 那一行的 `GameObject`**（给自检读它的**开关态**用）。
        /// <para>**为什么要开这个口**：`Editor/BattleScene.cs` 原来按**路径**找它
        /// （`ov.transform.Find("TutorialTip/ContinueText")`）—— 画布档下 `TutorialTip` 挂在
        /// `Tutorial Canvas` 下、**不再是本件根的直接子件** ⇒ 那句是 `null`、而断言写的是
        /// `ovCont != null && ovCont.gameObject.activeSelf`（**null ⇒ 恒假** ⇒ 静默失效，不是报错）。
        /// 路径字符串本来就脆 ⇒ 换成这个口，**两档都对**。</para></summary>
        public GameObject TipContinueGo { get { return _tipContinue != null ? _tipContinue.gameObject : null; } }

        // ── 🆕 2026-10-16（`T09` 批 3）：画布档那一侧的自检口 ────────────────────────────────────
        /// <summary>🔴 **自检用（`T09` 批 3 · 灭自证）**：本件**建出来的每一件**是不是都建在 uGUI 子画布上
        /// （而不是有条漏网、退回世界空间 quad）。
        /// <para>判据 = ① 三块子画布都在、且都在某块 `Canvas` 下；② **建出来的**每颗图都是 uGUI 件、
        /// 且挂在某块 `Canvas` 下；③ 三个容器节点 + **十颗字**（提示 2 / 开场 2 / 标注 4…）都是 uGUI 件。</para>
        /// <para>⚠️ 「**没建出来**」（贴图缺 ⇒ 那几颗图本来就不建）**不算失败**（那一路已经有 `LogWarning`）；
        /// 本条要抓的是**另一半**：件建成了、却建在**世界空间** —— 那种「只换半边」在 16:9 下逐位相同、
        /// 全绿，只在别的宽高比/震屏那一档现形（`T09` §五·补⑦）。⛔ **别用 `UguiMode` 替它**
        /// （那个只看宿主登记了没，不看件建在哪）。</para></summary>
        public bool OnCanvas
        {
            get
            {
                if (_cvTip == null || _cvHl == null || _cvPtr == null) return false;
                if (!UnderCanvas(_cvTip) || !UnderCanvas(_cvHl) || !UnderCanvas(_cvPtr)) return false;
                if (!ImgOnCanvas(_tipBg) || !ImgOnCanvas(_arrowL) || !ImgOnCanvas(_arrowR)
                    || !ImgOnCanvas(_highlight) || !ImgOnCanvas(_pointer)) return false;
                for (int i = 0; i < 4; i++) if (!ImgOnCanvas(_annoArrows[i])) return false;
                if (!NodeOnCanvas(_tipGo) || !NodeOnCanvas(_initGo) || !NodeOnCanvas(_annoGo)) return false;
                if (!TxtOnCanvas(_tipText) || !TxtOnCanvas(_tipContinue)) return false;
                if (!TxtOnCanvas(_initText1) || !TxtOnCanvas(_initText2)) return false;
                for (int i = 0; i < 4; i++) if (!TxtOnCanvas(_annoLabels[i])) return false;
                return true;
            }
        }
        /// <summary>那件图在不在画布上 —— ⚠️ `null`（贴图缺、本来就没建）**算通过**（见 `OnCanvas` 的 doc）。</summary>
        static bool ImgOnCanvas(HudPiece p)
        {
            return p == null || (p.IsUgui && p.Rect != null && UnderCanvas(p.Rect));
        }
        /// <summary>那颗字在不在画布上 —— ⚠️ **`null` 不算通过**（字只可能因为「TMP 拿不到」而缺，
        /// 那种情况整件都不可用）。</summary>
        static bool TxtOnCanvas(Label l)
        {
            return l != null && l.IsUgui && l.transform is RectTransform rt && UnderCanvas(rt);
        }
        static bool NodeOnCanvas(GameObject go)
        {
            return go != null && go.transform is RectTransform rt && UnderCanvas(rt);
        }
        /// <summary>这个节点挂在某块 `Canvas` 下吗（沿父链找、**不过滤激活态**）。
        /// <para>⚠️ **别图省事写无参的 `GetComponentInParent&lt;Canvas&gt;()`** —— 它等价于
        /// `includeInactive: false`，而本件**出厂就整层 `SetActive(false)`**（`HideAll`）⇒ 那样写出来的
        /// 断言答的是「它**此刻**可不可见」、不是「它建在哪」（本工程记过的坑，同 `T09` 那批的提醒）。</para></summary>
        static bool UnderCanvas(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.GetComponent<Canvas>() != null) return true;
            return false;
        }
        // 🔴 **2026-10-18（第三会话）：`SkipVisible`（转发 `SkipPanel.SkipTutorialShown`）【已删】**
        //    —— 零调用（原来那处读者 `Editor/BattleScene.cs` 已改读 `driver.Settings.SkipTutorialShown`）。
        /// <summary>玩家的「跳过」这一下**准不准**（原版 `TutorialTipScript.minTimeBeforeSkip = 1.0`）。
        /// ⚠️ **这是本工程自己留的闸**（原版那颗钮**没有**它 —— 见 `BattleDriver.SkipTutorialFromSettings`
        /// 末段）—— 本轮不动它，因为 `Editor/BattleScene.cs:14844` 把它钉住了。</summary>
        public bool SkipAllowed { get { return _shownFor >= MinTimeBeforeSkip; } }
        public bool SkipClickedByPlayer { get { return _skippedByPlayer; } }
        // 🔴 **2026-10-18（第三会话）：`SkipWorldPos`（转发 `SkipPanel.SkipTutorialWorldPos`）【已删】**
        //    —— 零调用。要那颗钮的世界坐标请直接读 `driver.Settings.SkipTutorialWorldPos`（真身）。

        // ==================================================================
        //  建
        // ==================================================================
        /// <summary>建在 <paramref name="parent"/>（= `BattleDriver.hudRoot`）下面。
        /// ⚠️ 一开始**全部 `SetActive(false)`** —— 原版那棵树里除了 `TutorialTip` 之外
        /// 每一个子件在场景里都是 `(inactive)`，是运行期按动作档亮的。
        ///
        /// <para>🔴 **2026-10-16（`T09` 批 3）：<paramref name="uguiHost"/> 是【双模式】的开关。**
        /// 传 `null`（出厂 / 既有调用点）= **世界档 —— 与改之前逐位相同**；传一块 `RectTransform`
        /// （= `HudCanvas.EnsureSubCanvas(null, SubCanvasName, HandMoving, TipSortingOrder)` 的返回值）
        /// ⇒ **画布档**（件建在画布上、坐标 = 设计 px）。先例 = `AlliancePanelWindow.Create(root, uguiHost:)`。</para>
        /// <para>⚠️ 另外两块（高亮 / 光标）由**本件自己**建齐（见 <see cref="EnsureCanvases"/>）——
        /// 因为第三块是**嵌在第一块下面**的（原版 `RT_3572.m_Father = 3206`），由调用点分三次传反而更容易写错。</para></summary>
        public static TutorialOverlay Create(Transform parent, RectTransform uguiHost = null)
        {
            var go = new GameObject("Tutorial");
            go.transform.SetParent(parent, false);
            var ov = go.AddComponent<TutorialOverlay>();
            ov.UseHost(uguiHost);          // 传 null ⇒ 世界档（`_cvTip` 留 null）
            ov.Build();
            return ov;
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：把本件接到 uGUI 档。**
        /// <paramref name="host"/> = **主那块子画布**的 `RectTransform`
        /// （= `HudCanvas.EnsureSubCanvas(null, SubCanvasName, HudCanvas.SortingLayer.HandMoving, TipSortingOrder)`）。
        ///
        /// <para>🔴 **为什么必须是「建 UI 的那一方」来登记**（同 `Tooltip.UseHost` 那条）：本件是
        /// `Create` 出来的，而**建它的那一刻**（`BattleDriver.EnsureTutorialOverlay`）根 canvas 建好了没
        /// 只有驱动层知道。</para>
        ///
        /// <para>传 `null` = **子画布没建起来**：**留在世界档**（= 与改之前逐位相同）、并**出声**
        /// （⛔ 不静默 —— 那正是「一半在画布、一半在世界」那种 16:9 下看不见的半迁移态）。</para>
        ///
        /// <para>⚠️ **可以在 `Build()` 之后再调**：那时会**拆掉世界档的件、按画布档重建**
        /// （拆旧建新是「同一个件只许在一边」那条铁律的落实，⛔ **不是**「两边都建」）。</para></summary>
        public void UseHost(RectTransform host)
        {
            if (host == null)
            {
                Debug.LogError("[Tutorial] `UseHost(null)` —— 主那块子画布（`" + SubCanvasName + "`）没建起来"
                             + " ⇒ 教程表现层**留在世界档**（画面与改之前逐位相同，"
                             + "但非 16:9 下会与已迁到画布的同族面板分家）");
                return;
            }
            if (_cvTip == host) return;
            _cvTip = host;
            if (_built)
            {
                Debug.Log("[Tutorial] `UseHost` 排在 `Build()` 之后 ⇒ 拆掉世界档的件、按画布档重建"
                        + "（⛔ 不许两边都建）。把登记挪到第一次 `Build()` 之前可以省掉这一次重建。");
                Teardown();
                Build();
            }
        }

        /// <summary>拆掉本件已经建出来的件 —— **只用于换档**（世界档 ↔ 画布档）。
        /// ⛔ **不拆那三块子画布**（它们由 `HudCanvas.EnsureSubCanvas` 按名字管、换档时复用同一块）。</summary>
        void Teardown()
        {
            KillGo(_tipGo); KillGo(_initGo); KillGo(_annoGo);
            KillGo(_arrowL != null ? _arrowL.gameObject : null);
            KillGo(_arrowR != null ? _arrowR.gameObject : null);
            KillGo(_highlight != null ? _highlight.gameObject : null);
            KillGo(_pointer != null ? _pointer.gameObject : null);
            _tipGo = null; _tipText = _tipContinue = null; _tipBg = null;
            _initGo = null; _initText1 = _initText2 = null;
            _arrowL = _arrowR = null; _highlight = null; _pointer = null;
            _annoGo = null;
            for (int i = 0; i < 4; i++) { _annoLabels[i] = null; _annoArrows[i] = null; }
            _built = false;                       // ⇒ 下一次 `Build()` 会用**当前**档重建
        }

        static void KillGo(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：建齐三块子画布**（判据 = 文件头 ① ② ③ 那三行）。
        /// 层 / order **一律照 `T09c` §一**：`HandMoving`/1500 · `HandMoving`/601 · `OverHand`/10。
        /// <para>⚠️ 拿不到（根 canvas 没立 / 名字被占）⇒ **返 false**，调用点**落回世界档**（出声，⛔ 不半迁移）。</para></summary>
        bool EnsureCanvases()
        {
            if (_cvTip == null)
                _cvTip = HudCanvas.EnsureSubCanvas(null, SubCanvasName,
                                                   HudCanvas.SortingLayer.HandMoving, TipSortingOrder);
            if (_cvHl == null)
                _cvHl = HudCanvas.EnsureSubCanvas(null, HighlightCanvasName,
                                                  HudCanvas.SortingLayer.HandMoving, HighlightSortingOrder);
            if (_cvTip == null || _cvHl == null) return false;
            // 🔴 那两块原版是**中心锚 + `sd 1920×1080`**（`RT_3206` / `RT_3572`），⛔ 不是 stretch
            //    ⇒ 建完补一句（`EnsureSubCanvas` 的出厂形状是 stretch，那是给 `Tutorial highlight` 的）。
            HudCanvas.SetPxSize(_cvTip, 1920f, 1080f);
            if (_cvPtr == null)
                _cvPtr = ChildCanvas(_cvTip, PointerCanvasName,
                                     HudCanvas.SortingLayer.OverHand, PointerSortingOrder);
            if (_cvPtr == null) return false;
            HudCanvas.SetPxSize(_cvPtr, 1920f, 1080f);
            return true;
        }

        /// <summary>建 / 取一块**嵌在某个子画布下面**的子画布（本件只有光标那一块是这种）。
        /// <para>⚠️ **为什么不能直接调 `HudCanvas.EnsureSubCanvas(parent, …)`**：它那句「按名字幂等」用的是
        /// `SubCanvas(name)` —— 而那个**只扫根 canvas 的【直接子件】**（`HudCanvas.cs` 现读）⇒ 传了 `parent`
        /// （嵌套）时它**按名字找不到已有的那一块**、**每调一次就再建一块**（静默、只在多调一次时现形）。
        /// ⇒ 这里**先按名字找、找不到才走它**（`parent.Find` 也只找直接子件，正是要的语义）。
        /// 📌 **缺口已记进本轮报告**（`HudCanvas.cs` 不在本件白名单 ⇒ 没就地改）。</para></summary>
        static RectTransform ChildCanvas(RectTransform parent, string name, int layer, int order)
        {
            if (parent == null) return null;
            var t = parent.Find(name);
            if (t != null)
            {
                var c = t.GetComponent<Canvas>();
                if (c != null)
                {
                    if (c.overrideSorting != true) c.overrideSorting = true;
                    if (c.sortingLayerID != layer) c.sortingLayerID = layer;
                    if (c.sortingOrder != order) c.sortingOrder = order;
                    return (RectTransform)t;
                }
            }
            return HudCanvas.EnsureSubCanvas(parent, name, layer, order);
        }

        // ── 建件（两档各一份，调用点只认「世界单位」那套入参）────────────────────────────────
        /// <summary>画布档：建一个只有 `RectTransform` 的**容器节点**（中心锚 + 中心枢轴 + 设计 px 尺寸）。
        /// 这些节点是原版那几个**有自己 rect 的** GO（`TutorialTip 430×150` 等）的等价物。</summary>
        static GameObject NewNode(string name, Transform parent, float wPx, float hPx, float z)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            HudCanvas.SetPxSize((RectTransform)go.transform, wPx, hPx);
            HudCanvas.Add((RectTransform)go.transform, z);
            return go;
        }

        /// <summary>世界档：建一个**裸 `GameObject`**（改之前那一版逐字相同 —— 它没有 rect，
        /// 尺寸由贴图/文字自己决定）。</summary>
        static GameObject PlainNode(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>建一块图 —— 两档入口不同、**口径同一个**：
        /// 世界档 = 旧 `ImageQuad`（**按【高】定、宽由贴图比例来**）；
        /// 画布档 = `HudCanvas.MakeImageInParent`（中心锚、「相对父中心」、**设计 px**）。
        /// <paramref name="hWorld"/> 恒为**世界单位**（判据侧的尺）—— 画布档内部 ×108 折成设计 px。
        /// <para>⚠️ **走 `MakeImageInParent` 而不是 `MakeImage`/`QuadFromTopPx`**：后两个把 `x/y` 当
        /// **屏幕绝对 px**（归一化锚）——子画布 rect 在 `Expand` 档下比 1920×1080 高，套错了会**静默摆到别处**
        /// （`T09` §七·10）。本件这几件的锚点都在父件中心 ⇒ 用中心锚那一套。</para></summary>
        HudPiece MakeImg(Transform parent, Texture2D tex, string name, float hWorld, float z)
        {
            if (UguiMode)
            {
                float hPx = hWorld * Px;
                float a = tex != null && tex.height > 0 ? tex.width / (float)tex.height : 1f;
                return HudCanvas.MakeImageInParent(parent, tex, name, 0f, 0f, hPx * a, hPx, z, Color.white);
            }
            return HudPiece.Of(ImageQuad.Create(parent, tex, Vector3.zero, hWorld, new Vector2(0.5f, 0.5f), name));
        }

        /// <summary>建一颗字 —— 两档入口不同、**渲出来一样**：
        /// 世界档 = `Label.Create`（`scale` 档 = 0.07 世界单位的大写高）；
        /// 画布档 = `HudCanvas.MakeText`（**设计 px**）＋ `SetCapHeight(scale × 7.56)`（见 `CapPxPerScale`）。
        /// <paramref name="worldOffset"/> = 相对**父件中心**的偏移（世界单位、y 向上）。
        /// <para>⚠️ 画布档 `MakeText` 在**拿不到 TMP 字体**时**返 null**（`Label.CreateUgui` 出声、
        /// ⛔ 不退化成点阵）⇒ **调用点每一处都要判空**（世界档的 `Label.Create` 不会返 null）。</para>
        /// <para>🆕 **2026-10-17（`A1024`③ 批 2–4）**：可选字体面，默认 `Cjk` ⇒ 旧调用点零行为变化。
        /// ⚠️ 必须排在定字号**之前**（画布档 `MakeText` 内部紧跟 `CreateUgui`；世界档在下面显式补一刀）——
        /// 字号按 `ActualFace` 的 cap 比例折算（判据 → `Label.TmpFontSize`）。</para></summary>
        Label MakeLbl(Transform parent, string name, Vector3 worldOffset, int scale, Color c, float z,
                      TmpFont.Face face = TmpFont.Face.Cjk)
        {
            if (UguiMode)
            {
                var l = HudCanvas.MakeText(parent, "", worldOffset.x * Px, worldOffset.y * Px, 1, c,
                                           new Vector2(0.5f, 0.5f), z, name, face);
                if (l != null) l.SetCapHeight(scale * CapPxPerScale);
                return l;
            }
            var w = Label.Create(parent, "", worldOffset, scale, c, new Vector2(0.5f, 0.5f), name);
            if (face != TmpFont.Face.Cjk)
            {
                w.SetFace(face);
                // 🔴 换面之后**必须显式重定一次字号**：`SetFace` 只换字体资产，而它内部那句 `SetText(_text)`
                //    因「同串早退」不会重算（见 `Label.SetSizes` 的注释）⇒ 不补这一刀，字号还按 CJK 的
                //    cap 比例算、换成 Asar/Pragati 后字形会静默缩小 ~10%。这里喂回**同一个 cap 高度**。
                w.SetCapHeight(w.CapHeightWorld);
            }
            return w;
        }

        /// <summary>🆕 **2026-10-17（`N49①`）**：给教学标注四块上原版那圈**细黑描边**。
        ///
        /// <para>**判据（解包现读，⛔ 不是看名字猜）**：原版这四颗挂的材质 =
        /// `bundle_fonts_assets_all/Material/Material_751232751962157263.json`
        /// 「`Asar-Regular Light Grey w outline thin`」——
        /// `_OutlineWidth = 0.10` · `_OutlineColor = (0,0,0,1)` · `_FaceDilate = 0.10` ·
        /// `_OutlineSoftness = 0` · `_FaceColor = (0.8018868,·,·,1)`（= 我们那个 `annoInk`，逐位同值）。</para>
        ///
        /// <para>**写法照 `Core/CardView.ApplyCreatedByOutline`**（同一个套路、只差参数）：
        /// 取 `TMP_Text.fontMaterial` —— 那个 getter **会实例化**一份材质（`m_fontMaterial`），
        /// ⇒ 改的是**这一颗自己的**那份，⛔ 不碰字体资产里那份共享材质（`fontSharedMaterial`）。
        /// 末尾 `UpdateMeshPadding()`：描边会画到字形方框之外，不重算留白会被裁掉一截。</para>
        ///
        /// <para>⚠️ 与 `TmpFont.ApplyOutline` 的分工：那边是「卡名 / 卡面正文」那两档固定配方；
        /// 本颗是**第三种配方（0.10 + FaceDilate 0.10）**，只在这一族用 ⇒ 就地写在这里，
        /// 不动共用件（⛔ 别在 `TmpFont` 里再开一档去服务单一件）。</para></summary>
        static void ApplyThinOutline(Label lb)
        {
            if (lb == null) return;
            var t = lb.GetComponent<TMP_Text>();
            if (t == null) return;                       // 点阵后端（字体资产缺失）⇒ 没有材质可改，不出声
            var mat = t.fontMaterial;                    // ⚠️ 实例材质，别碰共享的
            if (mat == null) return;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.10f);    // 原版 `_OutlineWidth`
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.10f);      // 原版 `_FaceDilate`
            mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);    // 原版 `_OutlineSoftness`
            t.UpdateMeshPadding();                       // 描边画到方框之外 ⇒ 重算留白（同族条目的最后一句）
        }

        /// <summary>画布档：把一件的**枢轴点**摆到那个**世界点**上。
        /// <para>🔴 走**这块画布自己的** `InverseTransformPoint` —— 它把父链的缩放与**两个轴不同的**
        /// 「画布单位↔世界单位」系数一起吃掉（`T09` §七·8）；⛔ 写死 ÷108 只在 16:9 对。</para>
        /// <para>⚠️ 写 `anchoredPosition`：本件那几件的锚点都是**父件中心** ⇒ 它就是「相对父中心的 px 偏移」。
        /// ⛔ 别写 `position`（会把件从画布平面搬走）、⛔ 别写 `localPosition.z`（同上）。</para></summary>
        static void CvPlace(GameObject go, RectTransform cv, Vector3 world)
        {
            if (go == null || cv == null) return;
            var rt = go.transform as RectTransform;
            if (rt == null) return;
            var p = cv.InverseTransformPoint(world);
            rt.anchoredPosition = new Vector2(p.x, p.y);
        }

        /// <summary>画布档：把一个**在画布所在世界帧里【量出来】的尺寸**折成**本画布的局部尺寸**（设计 px）。
        /// <para>🔴🔴 **必须按【各轴自己的系数】算，⛔ 不是一刀切 ×108** —— 画布单位↔世界单位的两个轴系数
        /// **不同、且随屏变**（批处理下实测 x 1/108 · y 1/144；`T09` §七·8），只有 16:9 才恰好两轴都是 1/108。
        /// 走 `InverseTransformVector`（= `CvPlace` 那条 `InverseTransformPoint` 的同族）⇒ 出来的是
        /// 「**覆盖同一个屏幕区域**」的那个尺寸 —— 无论输入是 uGUI 件量的还是世界 quad 量的，都成立。</para>
        /// <para>🔑 **判据 = 「这个口的输入是【设计 px】，还是【量出来的世界尺寸】」**（2026-10-16 `EW2` 现核，
        /// 坑表记着这一条）：
        /// · **输入是设计 px**（本件 `MakeImg` / `MakeLbl` / `CapPxPerScale` 那一族，值本身就是常量/设计值）
        ///   ⇒ 它的换算就是 **×108**（那是**同一个定义**的两个单位，⛔ 不是「量出来的」）；
        /// · **输入是量出来的世界尺寸**（本口；`size` 来自 `BattleDriver.TutAnchor` 取已迁件的 `WorldW/H`）
        ///   ⇒ 必须取**本画布各轴实测系数的逆**。
        /// ⚠️ **实测反例（`EW2`）**：原版 `130.7 × 80.4` px 的 `_endTurnBg`，量得 `(1.2102, 0.5583)`
        /// ⇒ 一刀切 ×108 交回 `(130.7, **60.3**)` ⇒ **高亮框比它框的那颗钮矮 25%**（而 **16:9 下恰好一致**、
        /// 看不出来）。</para></summary>
        static Vector2 CvSizePx(RectTransform cv, Vector2 worldSize)
        {
            if (cv == null) return worldSize;
            var v = cv.InverseTransformVector(new Vector3(worldSize.x, worldSize.y, 0f));
            return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        }

        /// <summary>渲染队列 —— **只在世界档有意义**。画布档的先后由所在的
        /// `(排序层, order, 兄弟序)` 决定（`ImageQuad.SetRenderQueue` / `Label.SetRenderQueue` 的 doc 同一条）
        /// ⇒ 这里 `Quad == null` 时**是空操作**（⛔ 不是「悄悄没做」，`UguiMode` 查得出来）。</summary>
        static void SetQ(HudPiece p, int q)
        {
            if (p != null && p.Quad != null) p.Quad.SetRenderQueue(q);
        }

        void Build()
        {
            if (_built) return;
            _built = true;
            _root = transform;

            // 🔴 画布档：先把三块子画布备齐。拿不到 ⇒ **出声 + 整件落回世界档**（⛔ 不留半迁移态）。
            if (UguiMode && !EnsureCanvases())
            {
                Debug.LogError("[Tutorial] 三块教程子画布没建齐（`" + SubCanvasName + "` / `" + HighlightCanvasName
                             + "` / `" + PointerCanvasName + "`；根 canvas 没立？）⇒ 教程表现层**落回世界档**"
                             + "（画面与改之前逐位相同，但非 16:9 下会与已迁到画布的同族面板分家）");
                _cvTip = null; _cvHl = null; _cvPtr = null;
            }
            // 三个宿主：画布档 = 各自那块子画布；世界档 = 都挂 `Tutorial` 根（`_root`，改之前就是这样）
            Transform tipParent = UguiMode ? (Transform)_cvTip : _root;
            Transform hlParent = UguiMode ? (Transform)_cvHl : _root;
            Transform ptrParent = UguiMode ? (Transform)_cvPtr : _root;

            var hlTex = MenuUiTex("Tutorial_Highlight");
            var arrowTex = MenuUiTex("40k_general_bt_arrow");
            if (hlTex == null)
                Debug.LogWarning("[Tutorial] 拿不到高亮图 `Tutorial_Highlight`（`Art/ui_menu/`）⇒ 高亮那一层**建不出来**"
                    + "（原版 `Tutorial highlight`，`sortingOrder 601`）。");
            if (arrowTex == null)
                Debug.LogWarning("[Tutorial] 拿不到 `40k_general_bt_arrow`（`Art/ui_menu/`）⇒ 左右箭头与指点光标**建不出来**"
                    + "（原版那三处的 sprite 名查不到/null，这一张是**我们挑的**载体）。");

            // ---- 小提示（`TutorialTip 430×150`）----
            // 🆕 2026-10-18（`Z4`）**换成真名**：原版那块底板 `Generic Popup Background` 的
            //   `sprite pid = -7511397500040153103`，在
            //   `d:/2/Warpforge_tools/data/ui_extract/battlearena1_sprite_map.json` 里逐条读出来
            //   = **`40k_popup`**（那张图在 `Art/ui_menu/` 与 `Art/ui_deck/`，`CardArt.MenuUi` 取得到）。
            //   🔴 **原来那句「名字解不出来 ⇒ 载体用 `40K_display`（我们挑的）」已作废** ——
            //   犯的是「只按 pid 去 assets_full 的 Sprite 目录找名字」那一条（pid→名字要靠
            //   **container / 索引表**，坑表 #20 的第一种载体）。
            // 🔴 **父子关系要摆对**：`_tipGo` 是那一层的根，文字与底板**都挂它下面** ——
            //    否则挪 `_tipGo` 时文字不动（第一版就写错了：文字挂在 `_root` 上）。
            // 🔴 **2026-10-16（`T09` 批 3）**：`430×150` 是**判据**（`TutorialTip [-211,1006 430x150]`）⇒
            //    画布档把这个 rect 真的写进节点（世界档那个裸 GO 没有 rect，尺寸由底板/字自己决定 —— 照旧）。
            _tipGo = UguiMode ? NewNode("TutorialTip", tipParent, TipPxW, TipPxH, ZTip)
                              : PlainNode("TutorialTip", tipParent);
            // ⚠️ **别用 `??`** —— Unity 的 `Object` 重载了 `==`，`??` 走的是真 null 判断，
            //    「资源被删掉」（伪 null）那一档它会漏过去 ⇒ 显式 `== null` 判。
            var tipBgTex = CardArt.MenuUi("40k_popup");
            if (tipBgTex == null) tipBgTex = CardArt.Ui("40K_display");   // 兜底：原版那张取不到时退回 HUD 底板
            _tipBg = tipBgTex != null ? MakeImg(_tipGo.transform, tipBgTex, "TutorialTipBg", TipPxH / Px, ZTipBg) : null;
            // 🔴 **2026-10-17（`A1024`③ 批 2–4）**：原版这两颗的面（现读 `battle1_tmp.tsv`）：
            //   · `Tutorial/TutorialTip/Generic Popup Background/TipText` = **Pragati**（fs36）⇒ `LatinCardBody`
            //   · `Tutorial/TutorialTip/ContinueText`                    = **Asar**（fs4.5）⇒ `LatinUi`
            _tipText = MakeLbl(_tipGo.transform, "TipText", new Vector3(0f, 0.12f, 0f), 3, new Color(1f, 1f, 1f), ZTip,
                               TmpFont.Face.LatinCardBody);
            _tipContinue = MakeLbl(_tipGo.transform, "ContinueText", new Vector3(0f, -0.38f, 0f), 2,
                                   new Color(1f, 0.85f, 0.35f), ZTip, TmpFont.Face.LatinUi);

            // ---- 开场那两条（`InitTutorialTip` → `TipText` + `TipText (1)`）----
            // ⚠️ 原版这一个是 `(inactive)` 的，而且**谁启用它查不到**（R2 §6·7）—— 2026-10-18（`Z8`）**再查一轮的结论**：
            //    ① `InitTutorialTip` 在 `assets_full` 全库**只有 GameObject 那 13 份**（每个战场各一份），
            //       **没有任何字符串引用**、13 个场景里**全是 `(inactive)`**；
            //    ② 最像「启用它」的那支 = **`TutorialTipScript.ShowTipPermanent`**（它把 `TutorialTip` 那个
            //       ScriptableObject 的 `tipPosition` 摆到这一层上）—— 而它在**全量反编译里零调用点**
            //       （与 `CemeteryLogManager.ClickCemeterySlider` 同一族：**只活在序列化事件里**）；
            //    ③ 数据侧：那两条的文字来自 `TutorialTipLibrary`（SO：`introTip` / `lokenIntroResponse` /
            //       `finalTip1` / `finalTip2`，`dump.cs` TypeDefIndex 833）—— **那份 SO 本地没解出来**。
            //    ⇒ **仍然是「查不到」** ⇒ 我们**建出来但不主动亮**（⛔ 不发明触发条件）。
            // 🔴 **2026-10-16（`T09` 批 3）**：⬆️ 那 100×100 是**原版的 rect**（`[-50,1030 100x100]`），
            //    而 `InitTipPxW` 那 700 是**我们挑的**（那一层不亮、也不参与布局 ⇒ 尺寸只影响这个空容器）。
            _initGo = UguiMode ? NewNode("InitTutorialTip", tipParent, InitTipPxW, InitTipPxH, ZFlat)
                               : PlainNode("InitTutorialTip", tipParent);
            // 🔴 **2026-10-17（`A1024`③ 批 2–4）**：原版 `Tutorial/InitTutorialTip/TipText` 与
            //    `…/TipText (1)` **两颗都是 Asar**（现读 `battle1_tmp.tsv`，fs5.2）⇒ `LatinUi`。
            _initText1 = MakeLbl(_initGo.transform, "TipText", Vector3.zero, 3, new Color(1f, 1f, 1f), ZTip,
                                 TmpFont.Face.LatinUi);
            _initText2 = MakeLbl(_initGo.transform, "TipText (1)", new Vector3(0f, -0.35f, 0f), 3,
                                 new Color(1f, 1f, 1f), ZTip, TmpFont.Face.LatinUi);
            if (UguiMode)
                // 画布档：同一个**世界点**（`_root.TransformPoint(ToWorld(...))` 与改之前那句
                //   `localPosition = ToWorld(...)` 摆出来的世界点**逐位相同** —— `_root` 恒在 `hudRoot` 原点）
                CvPlace(_initGo, _cvTip, _root.TransformPoint(LayoutSpace.ToWorld(0.5f, 0.72f)));
            else
                _initGo.transform.localPosition = LayoutSpace.ToWorld(0.5f, 0.72f);   // 🔴 我们挑的（原版那两条在场景里是 inactive）

            // ---- 左右箭头（`TutorialArrows/arrow1·arrow2`）----
            if (arrowTex != null)
            {
                _arrowL = MakeImg(tipParent, arrowTex, "arrow1", 0.45f, ZFlat);
                _arrowR = MakeImg(tipParent, arrowTex, "arrow2", 0.45f, ZFlat);
                if (_arrowR != null) _arrowR.transform.localScale = new Vector3(-1f, 1f, 1f);  // 镜像成另一头
            }

            // ---- 高亮（`Tutorial highlight`，`sortingOrder 601`）----
            if (hlTex != null)
            {
                _highlight = MakeImg(hlParent, hlTex, "Tutorial highlight", 1f, ZHighlight);
                if (_highlight != null)
                {
                    _highlight.SetTint(new Color(1f, 1f, 1f, 0.85f));
                    // 🔴 **原版就是 601**（R2 §4：`Overlay Canvas + Image，sortingOrder 601`）——
                    //    它必须压在**所有**战斗 HUD 之上（那一层是全屏 Overlay）。
                    // 🔴 2026-10-16（`T09` 批 3）：画布档下这**是空操作** —— 它那一整块子画布就是
                    //    `HandMoving`/601（层 + order 都照原版），先后归画布管（见 `SetQ`）。
                    SetQ(_highlight, 601);
                }
            }

            // ---- 指点光标（`TutorialPointerCombat/Pointer 100×100` + `light`）----
            if (arrowTex != null)
            {
                _pointer = MakeImg(ptrParent, arrowTex, "Pointer", PointerPx / Px, ZPointer);
                // 画布档：它那一整块子画布就是 `OverHand`/10（判据 → `T09c` §一 ③）
                SetQ(_pointer, 600);
            }

            // ---- 教学标注四块（`TutorialObjs/UnitObjs`：MeleeText/RangedText/HealthText + `EnergyText`）----
            // ⚠️ **如实记（`T09` 批 3）**：原版这一支挂 `Card Display/Card Display Window`（HUD 那一支，
            //    全树 `:578`），本件把它**建在 `Tutorial Canvas` 里**（「谁的 canvas 装谁」，
            //    ⛔ 没顺手翻父子层级 —— 那是另一笔账）。
            _annoGo = UguiMode ? NewNode("TutorialObjs", tipParent, 1920f, 1080f, ZFlat)
                               : PlainNode("TutorialObjs", tipParent);
            string[] names = { "MeleeText", "RangedText", "HealthText", "EnergyText" };
            // 🆕 **2026-10-17（界面线 `T07` · `A1753` #17）**：原版 `…/UnitObjs ▸ {Melee,Ranged,Health,Energy}Text`
            //   挂 `Asar-Regular Light Grey w outline thin`，`_FaceColor = (0.802, 0.802, 0.802)`、顶点白
            //   ⇒ 屏上是**浅灰**，不是我们原来的金色 (1, 0.85, 0.35)。
            //   🆕 **2026-10-17（界面线 `T07` · `N49①`）：细黑描边那一层已补** —— 原版材质
            //   `bundle_fonts_assets_all/Material/Material_751232751962157263.json` 现读：
            //   `_OutlineWidth 0.10` · `_OutlineColor (0,0,0,1)` · `_FaceDilate 0.10` · `_OutlineSoftness 0`
            //   ⇒ 见 `ApplyThinOutline`（**实例材质**，照 `Core/CardView.ApplyCreatedByOutline` 的写法）。
            var annoInk = new Color(0.802f, 0.802f, 0.802f, 1f);
            for (int i = 0; i < 4; i++)
            {
                // 🔴 **2026-10-17（`A1024`③ 批 2–4）**：原版 `TutorialObjs/{Melee,Ranged,Health,Energy}Text`
                //    **四颗全是 Asar**（现读 `battle1_tmp.tsv`，fs30；上面 2026-10-17 `T07` 的材质判据
                //    `Asar-Regular Light Grey w outline thin` 也指同一面）⇒ `LatinUi`。
                _annoLabels[i] = MakeLbl(_annoGo.transform, names[i], Vector3.zero, 2, annoInk, ZFlat,
                                         TmpFont.Face.LatinUi);
                ApplyThinOutline(_annoLabels[i]);
                if (arrowTex != null)
                    _annoArrows[i] = MakeImg(_annoGo.transform, arrowTex, names[i] + "Arrow", 0.3f, ZFlat);
            }

            // ---- 🔴 **2026-10-18（A991）：跳过钮【已经搬走】，本件不再建它** ----
            // 它现在建在 `Battle/SettingsPanel.cs`（`Build` 里 `SkipCxPx` 那一段）——
            // **判据 = 原版那颗钮的父链**：`SkipTutorial Button → Bottom buttons → **BattleSettingsPanel**`
            // （13/13 个战场场景同构；`menu_dump.py … "BattleSettingsPanel"` 现读的绝对矩形是
            //  `981.7,834.8 → 1281.7,924.8`，面板框心 `(960.0,569.3)` ⇒ 面板内 **(+171.7, −310.5)**）。
            // 本件原来把它建在 `Tutorial` 这棵 HUD 子树上、摆位还标着「我们挑的」（`SkipAt01`）——
            // **那是落点错**（铁律 11：查出来就照原版改）。
            // ⛔ **别把这一段建回来**（那就是「两个入口」）；本件只留：`MinTimeBeforeSkip` 那一档的计时
            //    （`SetSkip` / `SkipAllowed` / `TrySkip`）。
            //    🔴 **2026-10-18（第三会话）**：这里原来还写着「+ 几个**转发口**（`SkipPanel` 那一格）」——
            //    那几个口（`SkipPanel` / `SkipVisible` / `SkipWorldPos` / `ClickSkipAt`）**已删**，零调用。
            //
            // 📌 **顺着搬走的四样**（原来都在这一段，判据一条不丢）：
            //   · 底板图 = `40K_button`（pid `5651555388418207694`，索引表 `battlearena1_sprite_map.json` 读出）；
            //   · 染色 `m_Color = (0.3686, 0.8941, 0.5874, 1)`（**绿**，与投降钮同一档）；
            //   · `m_Type = 0(Simple)` + **`m_PreserveAspect = 1`** ⇒ 等比内接进 300×90（实绘 300×65.64 px）；
            //   · 文字 TMP 原文 `Skip tutorial`、`mTerm = Battle/Settings/SkipTutorial`（`MonoBehaviour_4307.json`）、
            //     fs **38**（base 12 · auto 12~38）、`Center/Midline`、折行 0、白。
            //   ⇒ 这些判据**一句没丢**，逐条写在 `SettingsPanel` 文件头那组 `Skip*` 常量与 `Build` 那段注释里。

            HideAll();
        }

        /// <summary>这几张教程图在 `Resources/Art/**ui_menu**/`（**不在**战斗那批 `Art/ui/`）⇒
        /// 走 <see cref="CardArt.MenuUi"/>（**它本来就查三个目录**：`ui_menu/ → ui_deck/ → ui/`，
        /// `CardArt.cs:576-591`，2026-09-22 那批菜单图一起加的）。
        ///
        /// 🔴 **2026-10-18 更正（铁律 5）**：本方法原来写的是
        /// `Resources.Load&lt;Texture2D&gt;("Art/ui_menu/" + name)` 并在注释里说
        /// 「`CardArt.Ui` 只查 `Art/ui/` ⇒ 只能直读」—— **后半句对、结论错**：
        /// 工程里**早就有** `CardArt.MenuUi` 这个专门查 `ui_menu/` 的口（同一天我又 grep 了一遍全仓才看见）。
        /// ⛔ 直读 `Resources` 还会**绕过 `CardArt` 的缓存与缺失告警** ⇒ 已改回走 `MenuUi`。
        /// ⚠️ 顺带：`40k_general_bt_arrow` 也是 `ui_menu/` 独有的（`ui/` 里没有）——
        /// 第一版我把它喂给 `CardArt.Ui` ⇒ **取到的是 null**（那两个警告会喊出来，不是静默，但功能没了）。</summary>
        static Texture2D MenuUiTex(string name) { return CardArt.MenuUi(name); }

        /// <summary>全部收起来（开局 / 非教程局 / 跳过后）。⚠️ **幂等**。</summary>
        public void HideAll()
        {
            if (_tipGo != null) _tipGo.SetActive(false);
            if (_initGo != null) _initGo.SetActive(false);
            if (_arrowL != null) _arrowL.gameObject.SetActive(false);
            if (_arrowR != null) _arrowR.gameObject.SetActive(false);
            if (_highlight != null) _highlight.gameObject.SetActive(false);
            if (_pointer != null) _pointer.gameObject.SetActive(false);
            if (_annoGo != null) _annoGo.SetActive(false);
            // 🔴 **2026-10-18（A991）**：这里原来还有 `if (_skipGo != null) _skipGo.SetActive(false);`
            //    —— 钮搬进 `SettingsPanel` 之后本件**没有 `_skipGo` 了**（它的显隐跟着设置面板开关走，
            //    由 `SettingsPanel.SetActive` 一致处理）。
            //    但**那半句还有一个副作用要保住**：老实现里「那颗钮被收起来了 ⇒ 下一次 `SetSkip(true)`
            //    又算『刚亮起来』、把 `_shownFor` 归零」—— 等价物就是下面这一句（⛔ 别删）。
            _skipArmed = false;
            _fade = 0f; _wantVisible = false;
        }

        // ==================================================================
        //  各层的开关（`BattleDriver` 按脚本动作调用）
        // ==================================================================

        /// <summary>小提示（动作档 `SmallTip 80`；原版 `BattleManager.ShowTutorialTip`）。
        /// <paramref name="center"/> = **锚点元素**的世界坐标（由 <see cref="BattleDriver.TutorialAnchor"/> 按
        /// `positionReference` 算出来）；<paramref name="dx"/> = 原版 `AiScripted.GetTipPosition` 里那个
        /// **横向偏移量**（`fVar9`，判据 → `BattleDriver.TutorialTipOffset`）。
        ///
        /// 🔴 **2026-10-18（`Z5`）就地改口径（铁律 5）**：本方法原来收的是锚点的 `Vector2 size`，
        ///   并按「半个锚点 + 半个提示面板 + 我们挑的 `Gap`」自己算偏移 —— 那**不是原版的做法**。
        ///   原版 `GetTipPosition` 的收尾就三行（`AiScripted__GetTipPosition.c` 末段，**方法体本地是有的**）：
        ///     `relation == 10 (LeftOf) ⇒ x -= fVar9` · `== 20 (RightOf) ⇒ x += fVar9` ·
        ///     `== 30 (Above) ⇒ y += fVar9 * **0.5**`（那个 `0.5` = `GameAssembly.dll` 常量池
        ///     `DAT_1834b2bb4` 的实读值）。
        ///   ⇒ 现在**逐字照抄这三条**（⛔ 不再叠 `Gap` / 半面板宽那些我们挑的量）。</summary>
        public void SetTip(bool on, string text, bool withContinue, Vector3 center, float dx, TutRelation rel)
        {
            if (_tipGo == null) return;
            if (on)
            {
                // 🔴 **先 `SetActive(true)` 再写字** —— 本工程记过的坑：**TMP 在对象没激活时量不出尺寸**
                //    （`ForceMeshUpdate` 要在激活之后调，否则 `textBounds` 是垃圾、宽度顶到上限 ⇒ 一个字看不见）。
                if (!_tipGo.activeSelf) _tipGo.SetActive(true);
                // 🔴 **2026-10-16（`T09` 批 3）：这两颗字要判空** —— 画布档的 `HudCanvas.MakeText`
                //    在**拿不到 TMP 字体**时返 `null`（世界档的 `Label.Create` 会退回点阵、不返 null）
                //    ⇒ 不判空就是一句 NRE（`MakeText` 自己已经出声，别在这儿再喊一遍）。
                if (_tipText != null) _tipText.SetText(text ?? "");
                // 🔴 **2026-10-18（双语③ 波 1·P4）改**：原来这里写死英文 `"Continue"`。
                //   键 = **原版词条** `Battle/Tips/Continue`（`Core/Loc.cs` 表里早有：EN 逐字 `Continue` /
                //   ZH「继续」，出处 = 原版 `ContinueText` 那颗 TMP + `zh_CN.csv:84`）⇒ 英文档**零变化**。
                //   ⛔ 别复用 `Battle/Mulligan/ButtonDone`（值同为 `继续`/`Continue`）—— 那是**换牌钮**那条键，
                //      两条是原版各自的词条（同 §④·4「同一个中文词可能是两条不同的键」）。
                if (_tipContinue != null)
                {
                    _tipContinue.SetText(withContinue ? Loc.T(ContinueTerm) : "");
                    _tipContinue.gameObject.SetActive(withContinue);
                }
                // 摆位：逐字照原版那三行（见上面那段更正）
                var p = center;
                if (rel == TutRelation.LeftOf) p += new Vector3(-dx, 0f, 0f);
                else if (rel == TutRelation.RightOf) p += new Vector3(dx, 0f, 0f);
                else if (rel == TutRelation.Above) p += new Vector3(0f, dx * TutTipAboveFactor, 0f);
                p.z = ZTip;
                // 🔴 **2026-10-16（`T09` 批 3）**：世界档那一句**逐字不变**；画布档走 `CvPlace`
                //    （世界点 → 本画布局部 px，写 `anchoredPosition`）。
                if (UguiMode) CvPlace(_tipGo, _cvTip, p);
                else _tipGo.transform.position = p;
            }
            else if (_tipGo.activeSelf) _tipGo.SetActive(false);
            _wantVisible = on; _shownFor = 0f;
            if (_tipBg != null) _tipBg.SetTint(new Color(1f, 1f, 1f, 0.92f));
        }

        /// <summary>原版 `GetTipPosition` 里 `Above` 那一条的系数 —— ✅ **判据是硬值**：
        /// `DAT_1834b2bb4` 在 `d:/2/unity_run_ref/GameAssembly.dll` 的常量池里实读 = **0.5**
        /// （`.rdata`，RVA `0x34b2bb4`）。⚠️ 与 `LeftOf`/`RightOf` 的**满值**不同，别看漏。</summary>
        public const float TutTipAboveFactor = 0.5f;

        /// <summary>开场那两条（`InitTutorialTip`）。
        /// 🔴 **谁启用它查不到**（R2 §6·7：`grep InitTutorialTip` 无命中、那个组件的类也没定位到）
        /// ⇒ ⛔ 我们**不假装知道**：这一层**建出来、但不主动亮**（`BattleDriver` 只在整关开始时按
        ///   `Stage.stage == 1` 的**我们自己的判断**亮一次，并出声说明这条判据缺失）。</summary>
        public void SetInitTip(bool on, string t1, string t2)
        {
            if (_initGo == null) return;
            if (on)
            {
                // ⚠️ 画布档那两颗字可能是 `null`（`MakeText` 拿不到 TMP 字体 ⇒ 出声 + 返 null）
                if (_initText1 != null) _initText1.SetText(t1 ?? "");
                if (_initText2 != null) _initText2.SetText(t2 ?? "");
            }
            if (_initGo.activeSelf != on) _initGo.SetActive(on);
        }

        /// <summary>高亮（`shouldHighlightElement`，6 关 39 条；原版 `ScreenHighlightPosition`）。</summary>
        public void SetHighlight(bool on, Vector3 center, Vector2 size)
        {
            if (_highlight == null) return;
            if (on)
            {
                // 🔴 **2026-10-16（`T09` 批 3）**：世界档那一句**逐字不变**（含那个 `0.04f`）；
                //    画布档走 `CvPlace`（它独占一块 `HandMoving`/601 的子画布，见文件头 ②）。
                var c = new Vector3(center.x, center.y, ZHighlight);
                if (UguiMode) CvPlace(_highlight.gameObject, _cvHl, c);
                else _highlight.transform.position = c;
                float w = Mathf.Max(size.x, 0.3f) + HighlightPad * 2f;
                float h = Mathf.Max(size.y, 0.3f) + HighlightPad * 2f;
                // 高亮框要**跟着锚点元素的形状**：`ImageQuad` 是等比缩放（按贴图比例给高度）
                // ⇒ 这里按「宽/高哪个更受限」内接 —— 与 `AttackSelector` 那套同一口径。
                // 🔴🔴 **2026-10-16（`T09` 批 3）就地订正（铁律 5）—— 本行原写的是**
                //    「画布档**别调 `SetWorldHeight`**（它内部写死 ×108、批处理下 y 会偏 0.75）」，
                //    而**同一段代码自己写的 `w * Px` 就是同一个 ×108** ⇒ **那半句自相矛盾、也不成立**：
                //    换不换那个口不是问题所在，**问题在「这个 `w/h` 该乘谁」**。
                // 🔑 **真判据 = 看 `w/h` 的【来路】**（全文 → `CvSizePx` 的 doc）：
                //    它俩是 `size`（**量出来的世界尺寸**：`BattleDriver.TutAnchor` 取已迁件的 `WorldW/H`）
                //    ＋ 我们挑的那个 `HighlightPad` ⇒ 画布档要取**各轴实测系数的逆**（`CvSizePx` 的
                //    `InverseTransformVector`），⛔ **不是一刀切 ×108**（那会「16:9 下刚好、别的宽高比下
                //    y 只有该有的 0.75 倍」—— `EW2` 实测 `80.4` px 的高亮框算成 `60.3` px）；⚠️ 16:9 下两者**同值**
                //    （两轴系数都 = 1/108）。
                // ⚠️ 世界档那一支**一个字都没动**（那里的 `w/h` 就是世界单位本身，不需要换算，
                //    `SetWorldHeight` + `localScale.x` 也照旧）。
                if (UguiMode)
                {
                    var rt = _highlight.transform as RectTransform;
                    if (rt != null) rt.sizeDelta = CvSizePx(_cvHl, new Vector2(w, h));
                }
                else
                {
                    _highlight.transform.localScale = Vector3.one;
                    _highlight.SetWorldHeight(h);
                    float haveW = _highlight.WorldW;
                    if (haveW > 0f) _highlight.transform.localScale = new Vector3(w / haveW, 1f, 1f);
                }
            }
            if (_highlight.gameObject.activeSelf != on) _highlight.gameObject.SetActive(on);
        }

        /// <summary>指点光标（原版 `CheckForTutorialPointer`，只在 `playerAction == 1` 的时候走）。</summary>
        public void SetPointer(bool on, Vector3 world)
        {
            if (_pointer == null) return;
            if (on)
            {
                // 🔴 **2026-10-16（`T09` 批 3）**：世界档那一句**逐字不变**（含那个 `0.06f`）；
                //    画布档走 `CvPlace`（它独占一块 `OverHand`/10 的子画布，见文件头 ③）。
                var p = new Vector3(world.x, world.y, ZPointer);
                if (UguiMode) CvPlace(_pointer.gameObject, _cvPtr, p);
                else _pointer.transform.position = p;
            }
            if (_pointer.gameObject.activeSelf != on) _pointer.gameObject.SetActive(on);
        }

        /// <summary>教学标注四块（原版 `TutorialObjs/UnitObjs`；出现时机 = 教程局、`ActivateHandCards 170` 前后）。
        /// ⚠️ **文案与那四根箭头的位置是我们挑的**：原版那四块是 TMP + `Arrow`，词条在**远端 I2 表**
        /// （本地没有）⇒ 我们按卡面语义写死四句中文/英文短语，逐条标在 `BattleDriver.TutorialAnnotations`。</summary>
        public void SetAnnotations(bool on, string[] texts, Vector3[] anchorWorlds)
        {
            if (_annoGo == null) return;
            if (on && texts != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    string t = i < texts.Length ? texts[i] : null;
                    bool has = !string.IsNullOrEmpty(t);
                    // ⚠️ 画布档那四颗字可能是 `null`（`MakeText` 拿不到 TMP 字体 ⇒ 出声 + 返 null）
                    var lb = _annoLabels[i];
                    if (lb != null)
                    {
                        lb.SetText(t ?? "");
                        if (lb.gameObject.activeSelf != has) lb.gameObject.SetActive(has);
                    }
                    if (_annoArrows[i] != null)
                    {
                        if (_annoArrows[i].gameObject.activeSelf != has) _annoArrows[i].gameObject.SetActive(has);
                        if (has && anchorWorlds != null && i < anchorWorlds.Length)
                        {
                            // 🔴 **2026-10-16（`T09` 批 3）**：世界档那一句**逐字不变**（含数组自带那个 z）；
                            //    画布档走 `CvPlace`（这一支挂 `_cvTip`，见 `Build` 里「如实记」那条）。
                            if (UguiMode) CvPlace(_annoArrows[i].gameObject, _cvTip, anchorWorlds[i]);
                            else _annoArrows[i].transform.position = anchorWorlds[i];
                        }
                    }
                }
            }
            if (_annoGo.activeSelf != on) _annoGo.SetActive(on);
        }

        /// <summary>🔴 **2026-10-18（A991）**：那颗钮**已经不在本件里**（它建在 `Battle/SettingsPanel.cs`），
        /// 所以本方法现在**只做一件事**：`MinTimeBeforeSkip` 那一档的**计时起点**。
        /// <para>语义与搬走前**逐字相同**（⛔ 别改）：`on` 第一次进来 ⇒ `_shownFor = 0`；`on == false` ⇒ 也归零。
        /// 原来那个「第一次」的前提是「那颗钮亮着没有」（`_skipGo.activeSelf`），钮搬走之后换成
        /// <see cref="_skipArmed"/> 这个显式闩 —— 两者在**所有既有调用序列**下等价
        /// （`BattleDriver` 两处都只传 `true`）。</para>
        /// <para>⚠️ 「刚出现那一下归零」的理由照旧：`minTimeBeforeSkip` 管的是**刚出现**那一刻
        /// （原版那个字段在 `TutorialTipScript` 上；我们两个入口 `SetTip` / `SetSkip` 都归零，
        /// ⛔ 别只归一个 —— 那样「提示先出、按钮后出」时闸的时间起点会不一样）。</para></summary>
        public void SetSkip(bool on)
        {
            if (on && !_skipArmed) { _skipArmed = true; _shownFor = 0f; }
            if (!on) { _skipArmed = false; _shownFor = 0f; }
        }

        // 🔴 **2026-10-18（第三会话）：`ClickSkipAt(Vector3 world)`（转发 `SkipPanel.HitSkipTutorial`）【已删】**
        //    —— 零调用。要判「点没点到那颗钮」请直接读真身：
        //    `driver.Settings.HitSkipTutorial(driver.Settings.SkipTutorialWorldPos)`（与真实输入同一条判定）。

        /// <summary>玩家按下了跳过（`BattleDriver` 调）。⚠️ `minTimeBeforeSkip` 之内**不算**
        /// （原版 `TutorialTipScript` 的闸；出声，别静默吞掉这一下）。</summary>
        public bool TrySkip()
        {
            if (!SkipAllowed)
            {
                Debug.Log($"[Tutorial] 「跳过」按早了（原版 `minTimeBeforeSkip = {MinTimeBeforeSkip}s`，"
                        + $"现在才 {_shownFor:F2}s）—— 这一下**不算**。");
                return false;
            }
            _skippedByPlayer = true;
            return true;
        }
        /// <summary>自检用：把「已经显示了多久」直接拨过去（批处理里 `Tick` 是被显式喂的）。</summary>
        public void SetShownForTest(float secs) { _shownFor = secs; }

        // ==================================================================
        //  每帧：淡入 / 淡出（原版 `fadeDuration = 0.2`）
        // ==================================================================
        /// <summary>🔴 **不挂在 `Update` 上** —— 批处理里没有帧循环（`文档` 红线），
        /// 由 `BattleDriver.Update` 与自检**显式喂 dt**。</summary>
        public void Tick(float dt)
        {
            _shownFor += Mathf.Max(0f, dt);
            float target = _wantVisible ? 1f : 0f;
            if (Mathf.Approximately(_fade, target)) return;
            float step = FadeDuration <= 0f ? 1f : Mathf.Max(0f, dt) / FadeDuration;
            _fade = Mathf.MoveTowards(_fade, target, step);
            ApplyFade();
        }

        void ApplyFade()
        {
            if (_tipBg != null) _tipBg.SetTint(new Color(1f, 1f, 1f, 0.92f * _fade));
            if (_tipText != null) _tipText.SetColor(new Color(1f, 1f, 1f, _fade));
            if (_tipContinue != null) _tipContinue.SetColor(new Color(1f, 0.85f, 0.35f, _fade));
        }
    }
}
