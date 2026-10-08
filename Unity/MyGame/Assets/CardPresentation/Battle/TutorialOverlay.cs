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
using UnityEngine;

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
        GameObject _tipGo; Label _tipText, _tipContinue; ImageQuad _tipBg;
        GameObject _initGo; Label _initText1, _initText2;
        ImageQuad _arrowL, _arrowR;
        ImageQuad _highlight;
        ImageQuad _pointer;
        GameObject _annoGo; Label[] _annoLabels = new Label[4]; ImageQuad[] _annoArrows = new ImageQuad[4];
        // 🔴 **2026-10-18（A991）删掉了 `_skipGo` / `_skipLabel` / `_skipBg`** —— 那颗钮**搬进了
        //    `Battle/SettingsPanel.cs`**（本件**不再建它**，全库只有那一处建）。本件只留两样：
        //    `MinTimeBeforeSkip` 那一档的计时 + 玩家点过的记账。
        /// <summary>`MinTimeBeforeSkip` 那一档的计时**起点**已经起过没有（幂等闩）。
        /// 原来这一格是「那颗钮亮着没有」（`_skipGo.activeSelf`），钮搬走之后换成显式闩 ——
        /// **`SetSkip` 的语义逐字不变**（见那个方法）。</summary>
        bool _skipArmed;

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
        /// 每一个子件在场景里都是 `(inactive)`，是运行期按动作档亮的。</summary>
        public static TutorialOverlay Create(Transform parent)
        {
            var go = new GameObject("Tutorial");
            go.transform.SetParent(parent, false);
            var ov = go.AddComponent<TutorialOverlay>();
            ov.Build();
            return ov;
        }

        void Build()
        {
            _root = transform;

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
            _tipGo = new GameObject("TutorialTip");
            _tipGo.transform.SetParent(_root, false);
            // ⚠️ **别用 `??`** —— Unity 的 `Object` 重载了 `==`，`??` 走的是真 null 判断，
            //    「资源被删掉」（伪 null）那一档它会漏过去 ⇒ 显式 `== null` 判。
            var tipBgTex = CardArt.MenuUi("40k_popup");
            if (tipBgTex == null) tipBgTex = CardArt.Ui("40K_display");   // 兜底：原版那张取不到时退回 HUD 底板
            _tipBg = tipBgTex != null
                   ? ImageQuad.Create(_tipGo.transform, tipBgTex, Vector3.zero, TipPxH / Px,
                                      new Vector2(0.5f, 0.5f), "TutorialTipBg") : null;
            _tipText = Label.Create(_tipGo.transform, "", new Vector3(0f, 0.12f, 0f), 3, new Color(1f, 1f, 1f),
                                    new Vector2(0.5f, 0.5f), "TipText");
            _tipContinue = Label.Create(_tipGo.transform, "", new Vector3(0f, -0.38f, 0f), 2,
                                        new Color(1f, 0.85f, 0.35f), new Vector2(0.5f, 0.5f), "ContinueText");

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
            _initGo = new GameObject("InitTutorialTip");
            _initGo.transform.SetParent(_root, false);
            _initText1 = Label.Create(_initGo.transform, "", Vector3.zero, 3, new Color(1f, 1f, 1f),
                                      new Vector2(0.5f, 0.5f), "TipText");
            _initText2 = Label.Create(_initGo.transform, "", new Vector3(0f, -0.35f, 0f), 3,
                                      new Color(1f, 1f, 1f), new Vector2(0.5f, 0.5f), "TipText (1)");
            _initGo.transform.localPosition = LayoutSpace.ToWorld(0.5f, 0.72f);   // 🔴 我们挑的（原版那两条在场景里是 inactive）

            // ---- 左右箭头（`TutorialArrows/arrow1·arrow2`）----
            if (arrowTex != null)
            {
                _arrowL = ImageQuad.Create(_root, arrowTex, Vector3.zero, 0.45f, new Vector2(0.5f, 0.5f), "arrow1");
                _arrowR = ImageQuad.Create(_root, arrowTex, Vector3.zero, 0.45f, new Vector2(0.5f, 0.5f), "arrow2");
                if (_arrowR != null) _arrowR.transform.localScale = new Vector3(-1f, 1f, 1f);  // 镜像成另一头
            }

            // ---- 高亮（`Tutorial highlight`，`sortingOrder 601`）----
            if (hlTex != null)
            {
                _highlight = ImageQuad.Create(_root, hlTex, Vector3.zero, 1f, new Vector2(0.5f, 0.5f),
                                              "Tutorial highlight");
                if (_highlight != null)
                {
                    _highlight.SetTint(new Color(1f, 1f, 1f, 0.85f));
                    // 🔴 **原版就是 601**（R2 §4：`Overlay Canvas + Image，sortingOrder 601`）——
                    //    它必须压在**所有**战斗 HUD 之上（那一层是全屏 Overlay）。
                    _highlight.SetRenderQueue(601);
                }
            }

            // ---- 指点光标（`TutorialPointerCombat/Pointer 100×100` + `light`）----
            if (arrowTex != null)
            {
                _pointer = ImageQuad.Create(_root, arrowTex, Vector3.zero, PointerPx / Px,
                                            new Vector2(0.5f, 0.5f), "Pointer");
                if (_pointer != null) _pointer.SetRenderQueue(600);
            }

            // ---- 教学标注四块（`TutorialObjs/UnitObjs`：MeleeText/RangedText/HealthText + `EnergyText`）----
            _annoGo = new GameObject("TutorialObjs");
            _annoGo.transform.SetParent(_root, false);
            string[] names = { "MeleeText", "RangedText", "HealthText", "EnergyText" };
            for (int i = 0; i < 4; i++)
            {
                _annoLabels[i] = Label.Create(_annoGo.transform, "", Vector3.zero, 2,
                                              new Color(1f, 0.85f, 0.35f), new Vector2(0.5f, 0.5f), names[i]);
                if (arrowTex != null)
                    _annoArrows[i] = ImageQuad.Create(_annoGo.transform, arrowTex, Vector3.zero, 0.3f,
                                                      new Vector2(0.5f, 0.5f), names[i] + "Arrow");
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
                _tipText.SetText(text ?? "");
                _tipContinue.SetText(withContinue ? "Continue" : "");
                _tipContinue.gameObject.SetActive(withContinue);
                // 摆位：逐字照原版那三行（见上面那段更正）
                var p = center;
                if (rel == TutRelation.LeftOf) p += new Vector3(-dx, 0f, 0f);
                else if (rel == TutRelation.RightOf) p += new Vector3(dx, 0f, 0f);
                else if (rel == TutRelation.Above) p += new Vector3(0f, dx * TutTipAboveFactor, 0f);
                p.z = 0.05f;
                _tipGo.transform.position = p;
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
                _initText1.SetText(t1 ?? "");
                _initText2.SetText(t2 ?? "");
            }
            if (_initGo.activeSelf != on) _initGo.SetActive(on);
        }

        /// <summary>高亮（`shouldHighlightElement`，6 关 39 条；原版 `ScreenHighlightPosition`）。</summary>
        public void SetHighlight(bool on, Vector3 center, Vector2 size)
        {
            if (_highlight == null) return;
            if (on)
            {
                _highlight.transform.position = new Vector3(center.x, center.y, 0.04f);
                float w = Mathf.Max(size.x, 0.3f) + HighlightPad * 2f;
                float h = Mathf.Max(size.y, 0.3f) + HighlightPad * 2f;
                // 高亮框要**跟着锚点元素的形状**：`ImageQuad` 是等比缩放（按贴图比例给高度）
                // ⇒ 这里按「宽/高哪个更受限」内接 —— 与 `AttackSelector` 那套同一口径。
                _highlight.transform.localScale = Vector3.one;
                _highlight.SetWorldHeight(h);
                float haveW = _highlight.WorldW;
                if (haveW > 0f) _highlight.transform.localScale = new Vector3(w / haveW, 1f, 1f);
            }
            if (_highlight.gameObject.activeSelf != on) _highlight.gameObject.SetActive(on);
        }

        /// <summary>指点光标（原版 `CheckForTutorialPointer`，只在 `playerAction == 1` 的时候走）。</summary>
        public void SetPointer(bool on, Vector3 world)
        {
            if (_pointer == null) return;
            if (on) _pointer.transform.position = new Vector3(world.x, world.y, 0.06f);
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
                    _annoLabels[i].SetText(t ?? "");
                    bool has = !string.IsNullOrEmpty(t);
                    if (_annoLabels[i].gameObject.activeSelf != has) _annoLabels[i].gameObject.SetActive(has);
                    if (_annoArrows[i] != null)
                    {
                        if (_annoArrows[i].gameObject.activeSelf != has) _annoArrows[i].gameObject.SetActive(has);
                        if (has && anchorWorlds != null && i < anchorWorlds.Length)
                            _annoArrows[i].transform.position = anchorWorlds[i];
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
