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
    /// + `TutorialPointerCombat` + `TutorialObjs` + `SkipTutorial Button`）。
    ///
    /// 🔑 **为什么合成一个文件/一件**：原版那六层**本来就挂在同一个 `Tutorial` 根下**（见文件头那棵树），
    ///    它们共享同一套「什么时候亮」的驱动（脚本动作 + `playerAction`），拆成六个文件只会让
    ///    「谁该跟着谁关」更难查 —— 那正是本工程踩过的坑（「建了却没跟着开关隐藏」）。
    ///
    /// ⛔ **一件都不许静默失败**：拿不到的图/查不到的参数一律 `Debug.LogWarning`（且**同一条只吼一次**）。
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
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
        /// <summary>跳过按钮 **300×90**（px）—— ✅ 判据：`SkipTutorial Button [22,1375 300x90]`。</summary>
        const float SkipPxW = 300f, SkipPxH = 90f;
        /// <summary>跳过钮底板的**染色** —— ✅ 原版实读：`MonoBehaviour_4721.json`（`SkipTutorial Button`
        /// 那颗 uGUI `Image`）的 `m_Color` = **(0.36862749, 0.89411765, 0.58743727, 1)**。
        /// 🔴 **2026-10-18（`G9`）**：底板换成真图 `40K_button` 之后，这一格**必须一起照抄** ——
        /// 原版那张图本身是**中性灰**，绿色是**染上去的**（与投降钮同一档：
        /// `SettingsPanel.ResignTint` 是同三个小数）。
        /// ⚠️ **不做 `.linear` 换算**：走 `ImageQuad.Tint`（= 材质的 color 属性），
        /// Unity 在**线性色彩空间**下自己会把 sRGB 值转过去；同族的 `SettingsPanel` 也是直填原值。</summary>
        static readonly Color SkipTint = new Color(0.3686f, 0.8941f, 0.5874f, 1f);
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
        /// <summary>🔴 **我们挑的**：跳过按钮摆在屏幕底部中间偏左（见 `Build` 里的注释 —— 原版那个
        /// `[22,1375]` 的父链偏移没能标定）。</summary>
        static readonly Vector2 SkipAt01 = new Vector2(0.5f, 0.075f);

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
        GameObject _skipGo; Label _skipLabel; ImageQuad _skipBg;

        float _fade;                 // 0..1（当前不透明度）
        bool _wantVisible;           // 这一层「该不该显示」
        bool _skippedByPlayer;       // 玩家点过跳过（`BattleDriver` 读它决定要不要收摊）
        float _shownFor;             // 本层已经显示了多久（`MinTimeBeforeSkip` 用）

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
        public bool SkipVisible { get { return _skipGo != null && _skipGo.activeSelf; } }
        /// <summary>玩家的「跳过」这一下**准不准**（原版 `TutorialTipScript.minTimeBeforeSkip = 1.0`）。</summary>
        public bool SkipAllowed { get { return _shownFor >= MinTimeBeforeSkip; } }
        public bool SkipClickedByPlayer { get { return _skippedByPlayer; } }
        /// <summary>自检用：跳过按钮的世界坐标（批处理里 `WorldPointer()` 是死点，要显式喂）。</summary>
        public Vector3 SkipWorldPos { get { return _skipGo != null ? _skipGo.transform.position : Vector3.zero; } }
        public Vector3 SkipWorldSize { get { return new Vector3(SkipPxW / Px, SkipPxH / Px, 0f); } }

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

            // ---- 跳过按钮（`SkipTutorial Button 300×90`，文案 `'Skip tutorial'`）----
            // 🔴 父子同 `TutorialTip`：文字与底板都挂 `_skipGo` 下面（否则挪它时文字不动）。
            // 🔴 **2026-10-18（`G9`）就地订正（铁律 5）：底板换成了真名 `40K_button`。**
            //   本节点原来写「原版那个节点的图**没读到** ⇒ 仍是 `40K_display`（我们挑的）」——
            //   **读到了**（`Z4` 那轮解开两条素材悬案用的是**同一条路**：pid→名字要走**索引表**，
            //   ⛔ 不是去 `assets_full` 的 `Sprite/` 目录按 pid 找名字）。三样判据：
            //     ① 节点 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/GameObject/SkipTutorial Button.json`
            //        （组件 `RectTransform_3271` · `CanvasRenderer_2439` · `MonoBehaviour_4721`(uGUI `Image`) ·
            //         `MonoBehaviour_4945`(uGUI `Button`) · `MonoBehaviour_4610`(`Canvas`)）；
            //     ② `MonoBehaviour_4721.json` 的 `m_Sprite` = `{ m_FileID: 8, m_PathID: 5651555388418207694 }`；
            //     ③ 那个 pid 在**索引表** `d:/2/Warpforge_tools/data/ui_extract/battlearena1_sprite_map.json`
            //        （pid→名字，96 条）里逐条读出来 = **`40K_button`**
            //        （图本体在 `CardPresentation/Resources/Art/ui_menu/40K_button.png` 与 `Art/ui/` 两份，
            //         `489×107`；`CardArt.Ui("40K_button")` 取得到）。
            //   ⚠️ **同一份 `MonoBehaviour_4721.json` 还给了三个必须照抄的字段**：
            //     · `m_Color = (0.36862749, 0.89411765, 0.58743727, 1)` —— **绿染**（与投降钮同一档，
            //       见 `SettingsPanel.ResignTint`）；
            //     · `m_Type = 0 (Simple)` + `m_PreserveAspect = 1` + `m_PixelsPerUnitMultiplier = 1`
            //       ⇒ **等比内接进 300×90 那个框**（`ImageQuad.FitHeight` 就是这条 `GetDrawingDimensions` 算法）。
            //       图 `489×107` 的比例 4.570 > 框的 300/90 = 3.333 ⇒ **按宽定**：实绘 300×65.64 px
            //       （⛔ 直接拿 `SkipPxH` 当高会画成 411×90，比原版宽 111 px）。
            //   ⚠️ 命中区**仍是** `SkipPxW × SkipPxH`（= 原版 `RectTransform_3271` 的 `m_SizeDelta` 300×90 本身，
            //     图内接小于它是版式本来的样子）—— `HitTest` 读的就是这一对，**不跟着图缩**。
            _skipGo = new GameObject("SkipTutorial Button");
            _skipGo.transform.SetParent(_root, false);
            var skipTex = CardArt.Ui("40K_button");
            if (skipTex == null) skipTex = CardArt.MenuUi("40K_button");   // 兜底：另一条取图路（同一张图两份）
            if (skipTex != null)
            {
                float skipH = ImageQuad.FitHeight(SkipPxW, SkipPxH, skipTex.width / (float)skipTex.height) / Px;
                _skipBg = ImageQuad.Create(_skipGo.transform, skipTex, Vector3.zero, skipH,
                                           new Vector2(0.5f, 0.5f), "SkipTutorialBg");
                if (_skipBg != null) _skipBg.SetTint(SkipTint);
            }
            else
            {
                _skipBg = null;
                Debug.LogWarning("[Tutorial] 拿不到跳过钮底板 `40K_button`（`Art/ui/` 与 `Art/ui_menu/` 都没有）"
                               + " ⇒ **只画字、不画底板**（原版 `Bottom buttons/SkipTutorial Button` 的 "
                               + "`m_Sprite` 实读就是 `40K_button`；⛔ 不摆白方块、也不退回别张图）。");
            }
            _skipLabel = Label.Create(_skipGo.transform, "Skip tutorial", Vector3.zero, 3, new Color(1f, 1f, 1f),
                                      new Vector2(0.5f, 0.5f), "SkipTutorialLabel");

            // 摆位（跳过按钮那一处是**我们挑的**，见 `SkipAt01` 的注释）
            _skipGo.transform.localPosition = LayoutSpace.ToWorld(SkipAt01.x, SkipAt01.y);

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
            if (_skipGo != null) _skipGo.SetActive(false);
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

        /// <summary>跳过按钮（`Bottom buttons/SkipTutorial Button`；教程局**常显**）。</summary>
        public void SetSkip(bool on)
        {
            if (_skipGo == null) return;
            // 「刚亮起来」那一帧把计时归零 —— `minTimeBeforeSkip` 管的是**刚出现那一下**
            //（原版那个字段在 `TutorialTipScript` 上；我们两个入口 `SetTip` / `SetSkip` 都归零，
            //  ⛔ 别只归一个：那样「提示先出、按钮后出」时闸的时间起点会不一样）
            if (on && !_skipGo.activeSelf) _shownFor = 0f;
            if (!on) _shownFor = 0f;
            if (_skipGo.activeSelf != on) _skipGo.SetActive(on);
        }

        /// <summary>自检/产品共用：**这一下是不是点在跳过按钮上**（抬起沿）。
        /// 返回 true = 玩家要跳过 ⇒ 调用方负责收摊（我们只记账）。</summary>
        public bool ClickSkipAt(Vector3 world)
        {
            if (_skipGo == null || !_skipGo.activeSelf) return false;
            var d = world - _skipGo.transform.position;
            return Mathf.Abs(d.x) <= SkipPxW / Px * 0.5f && Mathf.Abs(d.y) <= SkipPxH / Px * 0.5f;
        }

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
