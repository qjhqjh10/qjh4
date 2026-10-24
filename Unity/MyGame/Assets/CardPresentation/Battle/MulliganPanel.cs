// MulliganPanel.cs — 开局换牌（原版 `Mulligan` 子树 / `MulliganManager` / `MulliganFrame`）
//
// **为什么有它**：原版抽完起手牌会进 `_SetupMulliganPhase`，双方换完才 `StartBattlePhase`；
// 规则书 :46「换牌（Mulligan）| **可弃回任意起手牌后重洗补抽**」。我们原来**全仓 0 命中**。
//
// ---- 结构照运行时 dump（`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv:437-450`）----
//   Mulligan（整屏，FrontCanvas 下）
//     ├ MulliganAnchor              —— 卡片的布局锚（**只有 Transform、没有 RectTransform**，
//     │                                  `m_LocalPosition = (0, 29, 0)`；原版把起手牌**搬进它那一行**）
//     ├ ButtonsGroup
//     │   ├ MulliganContinueButton  —— 容器 x[1062.9,1895.8] y[921.5,1039]
//     │   │     （底右锚 `pos(-24.2,41.0)`、`size(832.9,117.5)` → 绝对值由锚点算出）
//     │   │     ├ Button      IMG `40k_bt_underbutton` 577.5×63.8 @中心 (1611.45, 980.25)
//     │   │     ├ Text        368.9×62.2 @中心 (1525.05, 981.05)
//     │   │     └ CircleButton IMG `40k_UI_bt_play` 80.5×79.6 @中心 (1763.45, 980.25)
//     │   └ HideMulliganButton IMG `40k_ui_bt_eye` 82.9×79.6 @中心 (184.45, 908.5)
//     └ MulliganText（提示行）1344×79.4 @中心 (967, 106.5)（里面还有一行 `TurnText`）
//
// ---- ⚠️ 哪些是我们的 ----
//  · **每张牌的「替换」按钮**（原版 `MulliganFrame.changeCardButton`）：它是**运行时按卡生成的**，
//    dump 是静态树、**没有它** → 原来位置和大小是我们排的（贴在手牌下缘、宽 = 卡宽）。
//    🔴 **2026-10-13 拿到判据并照它改了**：原版那个 prefab 是
//    `bundle_battlesharedresources_assets_all/GameObject/CardMulliganButtonFrame.json`
//    （内部 `Change Card Button` `sd = (1.30, 0.33)`、中心 `ap = (0, −1.7513)`、那层框 `2.3335 × 4.167`）
//    —— 即**卡宽的 55.7%、压在卡下沿下方一点**，而且它是**拉伸**画的（框比例 3.94 ≠ 图 410×124 的 3.31）。
//    几何判据全文写在 `BtnWFactor/BtnHFactor/BtnBelowCardPx` 那三个常量的 doc 里。
//    钮上的字走原版词条 `Battle/Mulligan/Replace` ⇄ `Undo`（判据 → `CardBtnWord`）。
//  · 🔴 **点手牌本身 = 「看牌」，不是标记**（`CardScript__OnTouchUpAsButton.c:52-58`：换牌期
//    `cardState ∈ {0xD,0xE}` 这一段只做「置 `displayingCardFlag(+0x24a)` → `BattleManager.DisplayCard(bm, this, 1, 1)`」，
//    第 4 参 = 强制展示；指针移开由 `OnTouchExit.c:15-16` 收）。换牌期卡的碰撞体是**开着且原始尺寸**
//    （`CardCollider2D__SetColliderNotInHand.c:11-24`）—— 那张牌就是**给点着看的**。
//    **标记只走每张卡上那颗「换」钮**（`MulliganFrame__ChangeCardButtonOnClick.c:5` →
//    `CardScript.ClickMulliganSelected`，闸 = `UIstate==0xb` + `isPlayer` + `mulliganOption != 0x1e`，
//    在 `0xD ⇄ 0xE` 之间翻转 + 高亮 + 音效）。
//    🔴 **2026-10-12（`A1663`，用户真机点名「不是点击手卡替换」）**：本文件原来**多了一条**
//    「点卡本身也能标记/取消」（注释自己招过「点卡这条路是我们加的」）⇒ 把原版「点卡 = 放大展示」
//    整段盖住了。**已删**；点卡现在只回调 `OnCardSeen`、由驱动层转发原版那条展示链
//    （`BattleDriver.HandleMulligan` → `OnMulliganCardSeen` → `OnCardTapped`）。
//    ⛔ **别再加回来**（`Editor/BattleScene.cs` 17·3 那两条**成对**的断言就是防它的）。
//  · **遮罩**：原版这一阶段有 `Shade.SwitchShade`（`_SetupMulliganPhase` 里调的）。
//    🔴 **2026-10-22（W62 · `D6`）就地订正（铁律 5）**：本行原文写「**颜色/透明度没查到** → 用了和战斗日志面板
//    同款的一层压暗，**这是我们挑的**」—— **已不成立**：原版那两样都读得到，而且我们那层的**层序**有问题
//    （它把换牌卡一起压暗了）。判据与取值 → <see cref="ShadeAlpha"/> / <see cref="QMulliganShade"/> 那两条 doc
//    （源码：`MonoBehaviour_4399.onAlphaLevel = 0.8` · `ShadePanel.Image.m_Color = (0,0,0,0)`）。
//    🔴🔴 **2026-10-24（第七十三会话 · `M1`）**：W62 那次**没修好** —— 真机复拍换牌卡区 max = 123
//    （线性 0.198 = 1 − 0.8），卡仍被压暗层盖着。根因 = **画布排序层先于 `renderQueue` 比较**，
//    所以「把卡抬到 3001」压不过 `OverHand` 画布。⇒ 压暗层改回**世界空间 quad**（`QMulliganShade` 3006）、
//    换牌卡 = **3007**；判据与实测见那两条 const 的 doc，断言在 `Editor/BattleScene.cs` 的 `D6` 段。
//  · **提示行文案**：⚠️ **2026-10-12 就地订正（铁律 5）**：原来这里写「「选择要换掉的牌」是我们起的
//    （原版 `mulliganTextLocalize` 的 I2 词条本地没有）」—— **已不成立**：2026-10-18（W6）起提示行走
//    **原版词条** `Battle/Mulligan/Instructions`（TMP 原文 `Choose cards to replace in first hand`，
//    判据 → `Core/Loc.cs` 那条键）。
//  · **倒计时**：🔴 **2026-09-17 变更** —— 原版在**离线练习局（matchType 0x32）会整段跳过**倒计时
//    （`BattleManager.<MulliganCountdown>` 开头对 `0x32` 直接 return；另一个 `MulliganFallbackCountdown`
//    是**联网掉包**兜底，与离线无关）。我们原来据此**不做**。
//    **用户 2026-09-17 要求「一切按原版、把倒计时做出来」** ⇒ 现在做了（`BattleDriver.TickMulligan`）：
//    逐秒 −1 → **`<10` 秒**把剩余秒数写到「完成换牌」那颗钮上（原版 `MulliganManager.SetMulliganTimer`
//    写的正是 `mulliganButtonText`，见该类第 1 个字段）→ **`<1` 秒**自动完成（`ProcessMulliganDone`）。
//    ⚠️ 总秒数字段名 = **`VarsGlobal.mulliganTimeLimit`**，**值 = 25.0 秒**（原版资产在
//    `Warpforge_Data/sharedassets0.assets`，读法 `工具/read_varsglobal.py`）；
//    显示格式 = **`"0:0" + 秒数`**（字面量 `StringLiteral_20746` = `"0:0"`，2026-09-17 用
//    `Il2CppDumper` 的 `script.json → ScriptString[20745]` 查到）⇒ 最后十秒显示 **`0:09`…`0:01`**。
//
// ---- 2026-09-17 照反编译（`Warpforge_tools/data/decomp_il2cpp_0827/`）逐条核实过 ----
//  · **键盘 `Enter`/`空格` = 完成换牌** —— **原版本来就有**：`MulliganManager__Update.c` 读
//    `GetKeyDown(0x20=Space)` / `(0xd=Enter)` → 门控 `buttonsGroup.activeSelf` → `ProcessMulliganDone`。
//    ⚠️ 我们原来注释写「原版只有按钮」，**已更正**（错因：只看了按钮处理器、没看 `Update`）。
//  · **「眼睛」= 开关**（`ToggleMulliganVisibility` 读 `activeInHierarchy` 取反 → `ShowMulliganElements`），
//    一次收：每张卡的换牌按钮（`ShowMulliganCards` 是循环）+ **压暗层**（`Shade.SwitchShade`）。
//    ⚠️ 我们原来**只收按钮、压暗还盖着** —— 是**漏做**，2026-09-17 补上（`HandleClick` 的 HitEye 分支）。
//  · **倒计时**：`MulliganCountdown` 对 `matchType == 0x32`（离线练习）**直接 return** ⇒ 离线局确实没有；
//    另一个 `MulliganFallbackCountdown` **不是**离线用的 —— 它调 `RequestMulliganResend` /
//    `ConfirmMulliganReceived` / `CancelMatchWithVictory`，是**联网掉包**的兜底（2026-09-17 查实）。
//  · **对手换牌**：原版单机局会调 `AI.GetAiMulliganCards(hand)`（`BattleManager__ClickMulliganDone.c` 单机分支，
//    存 `matchData+0x88`），但 **`AI` 类的方法体一个都没被反编译**（71 个类里没有它）⇒ **规则无从照抄**。
//    我们让 AI 一张不换 = **有据的偏离**（见 `BattleDriver.OpenMulligan` 的注释）。
// ═════════════════════════════════════════════════════════════════════════════
//  🔴 **2026-10-16（`T09` 批 3 · `OverHand` 组）：图/字整批从【世界空间 quad】迁到【uGUI 子画布】**
//  · **子画布** = `HudCanvas.EnsureSubCanvas(null, SubCanvasName, OverHand, 0)` —— 层/order **照原版那一行**
//    （`资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一：我这一颗 = `Mulligan`
//     @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`，**`OverHand` / order 0 / 兄弟序 3/14**）。
//  · **画布上那个整屏帧** = `Mulligan`（`HudCanvas.FillParent`，= 原版那个 `anchor (0,0)-(1,1)` + `sd (0,0)`）：
//    本面板**所有图与字**的父节点就是它。**它的中心 = 屏心、局部单位 = 设计 px**
//    ⇒ 面板内坐标 = `HudCanvas.PosPx(cx, cy)`（原版那套「左上原点、y 向下」的设计 px，原样可搬）。
//  · **退场的口**（逐项 → `T09` §五·补④ 那张映射表）：
//      `ImageQuad.Create(...)` → **`HudCanvas.MakeImageInParent(...)`**（⚠️ 中心锚，**不是** `MakeImage`
//        那套归一化锚 —— 面板帧在非 16:9 下比 1920×1080 高，归一化锚会把件**静默摆到别处**）；
//      `Label.Create(...)` → **`HudCanvas.MakeText(...)`**（**设计 px**）；
//      面板帧的 `MenuDraw.SetPxSize(1920,1080)` → **`HudCanvas.FillParent`**（⛔ `SetPxSize` 内部 ÷108）；
//      `localPosition += (0,0,Z)` → 建件口那个 `z` 形参（画布上 z **只用来排兄弟序**，写进 `localPosition`
//        会把件推离画布平面）；
//      读回 `transform.position` + `LayoutSpace.PxX/PxY` → **`_frame.InverseTransformPoint`**（见 `PxOf`）。
//  · 🔴 **有意【不迁】的两样**（⛔ 不是漏迁）：
//    ① **世界空间那层壳**（`transform`，即 `MulliganPanel` 自己那个 `RectTransform`）—— 它是
//       **「换牌区那一行」的换算基准**（`RowLocalAt` / `RowWorldPosAt` / `RowLocalPos`）：那几张起手牌
//       **还留在世界空间**，而 `BattleDriver` 正是拿这几个口
//       给卡定位的（`:13315` `RowWorldPos`）⇒ 保它 ⇒ **卡的世界位逐位不变**（16:9 / 非 16:9 都是）。
//       🔴 **2026-10-19（`T09` 步骤 B · `W57` · B2a）就地订正**：上面原来写「（`T09` 第 5 步才走原版
//       `Physics2D` 那条）」—— **命中判定这一半已经做了**（`CardAt` → `CardInteraction.Frontmost` →
//       `CardView.HitByCollider`，真 `BoxCollider2D` + `Physics2D.SyncTransforms`）；留在世界空间的
//       是**卡体本身**（它的坐标/缩放/扇形都由世界位驱动）⇒ 这一条**不是「等第 5 步」的欠账**。
//       它就是「卡体那一族还没迁」这条边界的显式落地点（同 `BattleLogPanel` 那个根节点）。
//    ② **卡体本身**（`CardView`）—— 同上：B1/B2a 只把**命中判定**换成真 `BoxCollider2D`
//       （挂在原版 `2DCard` 那一级），卡体、坐标帧与渲染那一套**一个字没动**。
//  · ⚠️ **如实记着的中间态**（本次**没做**、也不该在这里做）：非 16:9 下「画布上的钮」与「世界空间的卡」
//    本来就是**两个帧**（`T09` 正文那条「4:3 下整体偏大 1.333 倍」）⇒ 那颗「替换」钮会与卡错开一点
//    （4:3 下最多 ≈90 px；算式在报告 §6）。**卡一迁（第 5 步）这一档自然消失** —— 属"只换半边"的已知代价。
// ═════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    public class MulliganPanel : MonoBehaviour
    {
        /// <summary>面板开着吗。开着时**吃掉点击**，别让底下的棋盘/手牌也响应。</summary>
        /// <summary>「完成换牌」那颗钮上的默认文字 = **原版词条** `Battle/Mulligan/ButtonDone`。
        /// 🔴 **2026-10-18（第十二轮 · W6）就地订正（铁律 5）**：原来这里写「**文案是我们起的**
        /// （原版是 I2 词条 `Battle/Mulligan/ButtonDone`，**词条内容本地没有**）」—— **后半句不成立**：
        /// 键在本地、**TMP 英文原文也在本地**（13 个 arena 各 3 颗同键，TMP 逐字 `Continue`；
        /// 判据全文 → `Core/Loc.cs` 那一块）。
        /// 倒计时进最后 10 秒时会把它换成剩余秒数（原版 `MulliganManager.SetMulliganTimer`，见 `BattleDriver.TickMulligan`）</summary>
        public static string DoneLabel { get { return Loc.T(DoneTerm); } }
        /// <summary>完成钮的词条键 —— **只此一份**。</summary>
        public const string DoneTerm = "Battle/Mulligan/ButtonDone";

        /// <summary>把「完成换牌」那颗钮上的字换掉（原版 `MulliganManager.mulliganButtonText`）。
        /// 原版倒计时 `&lt;10` 秒时**每秒**刷一次这个字（`MulliganCountdown` → `SetMulliganTimer`）。</summary>
        public void SetDoneText(string s)
        {
            if (_doneText == null) return;
            _doneText.SetText(s);
            // 🔴 **2026-10-18（W6）**：字号跟着**这一串**的语种走 —— 倒计时那几秒写的是**数字**
            //    （拉丁 ⇒ `SetCapHeight(0.72em)`），而默认那串在中文档是汉字（⇒ `SetGlyphHeight(1em)`）。
            //    写死一种的话，另一档不是小 28% 就是大 39%（本工程记过的那条：`Loc.HasCjk` 是唯一一份判据）。
            SetScriptFont(_doneText, s, DoneFsBase);
        }

        /// <summary>自检用：那颗钮上现在写的是什么。
        /// 🔴 **A245：没建出来时返回 `null`**（原来返 `"&lt;无>"`）—— 同族一个口径，见 `PromptText`。</summary>
        public string DoneText { get { return _doneText != null ? _doneText.Text : null; } }

        /// <summary>「你先手 / 你后手」那一行现在显示什么（自检用）。
        /// 🔴 **A245：没建出来时返回 `null`**（原来返 `"&lt;无>"`）—— 同族一个口径，见 `PromptText`。</summary>
        public string TurnText { get { return _turnText != null ? _turnText.Text : null; } }

        /// <summary>这一行那个 label（**自检要量它的实际位置** —— 判据是原版 `TurnText` 的 rect，不是我们的常量）。</summary>
        public Label TurnLabel { get { return _turnText; } }

        /// <summary>提示行那个 label（**自检要量它的颜色** —— 判据 = 原版 `m_fontColor32 = 4294967295` 纯白）。</summary>
        public Label PromptLabel { get { return _prompt; } }
        /// <summary>🆕 2026-10-16（`A2102④`）：钮上那颗 label（自检量**渲染尺寸 ≤ 框**用）。</summary>
        public Label DoneLabelView { get { return _doneText; } }

        /// <summary>按「这一局谁先手」设那一行。<paramref name="playerGoesSecond"/> = 我方是后手。
        /// 判据 → `资料/加时与冲突模式_原版规格.md` §2.8（原版 `MulliganManager.ActivateMulligan` 的二选一）。</summary>
        public void SetTurnText(bool playerGoesSecond)
        {
            // 🔴 **2026-10-13（`A1751` · 用户真机点名「第二行没显示」）**：**只记下这一档，写字的活交给 `ApplyTurnText()`**。
            //   为什么不能在这里直接写：调用点在 `BattleDriver.OpenMulligan` 里**排在 `Open()` 之前**
            //   ⇒ 这一下是在**面板还关着**（`SetVisible(false)`）的时候写的，而**未激活时 `ForceMeshUpdate()`
            //   是空操作** ⇒ `textBounds` 是哨兵 `4.29e9` ⇒ `RefreshBounds()` 会把 TMP 子节点扔到 2.1e9 之外
            //   —— **画面上什么都没有、也不报错**（这条机制本仓 `Label.cs:1284` 早就记过）。
            //   ⇒ 现在 `SetVisible(true)` 那一刻（对象真激活了）**再写一遍**，这时量得准、摆得对。
            _turnSecond = playerGoesSecond;
            ApplyTurnText();
        }

        /// <summary>把记住的那一句（先手 / 后手）写到那一行上。
        /// ⚠️ 只在**对象已激活**时量得准 —— `SetVisible(true)` 会再调一次，所以两头都不漏。</summary>
        void ApplyTurnText()
        {
            if (_turnText == null) return;
            string t = _turnSecond ? TurnSecond : TurnFirst;
            _turnText.SetText(t);
            // 🔴 **字号按【这一串字】的语种选** —— 汉字 ≈ 1 em、拉丁大写 ≈ 0.72 em
            //    （判据 = `Battle/Label.SetCapHeight`/`SetGlyphHeight` 的 doc，`Loc.HasCjk` 是那条判据的**唯一一份**）。
            //    原来这里写死 `SetGlyphHeight`：中文档对，但英文档（`You go second`）会**大 39%**。
            SetScriptFont(_turnText, t, TurnPx);
        }

        /// <summary>提示行那条的词条键 —— **只此一份**（`Build()` 与自检都取它）。
        /// ⚠️ 原版的 `WaitText`（`Battle/Mulligan/WaitEnemy`）是**另一条**键，挂在等待横幅上，
        /// ⛔ 别把两条并成一条（它们挂在不同节点、`BattleDriver` 里那个等待横幅节点根本没有对应词条）。</summary>
        public const string PromptTerm = "Battle/Mulligan/Instructions";

        /// <summary>每张牌上那颗「换」钮的词条键 —— **只此一份**（判据全文 → `Core/Loc.cs`）。</summary>
        public const string ReplaceTerm = "Battle/Mulligan/Replace";

        /// <summary>🆕 **2026-10-18（第十三轮 · G2b）**：同一颗钮上**第二档**文字的词条
        /// —— 那张牌**已经被标记要换**时改印这条（原版那颗钮是**同一个对象换 term**，不是第二颗钮）。
        /// <para>判据（**唯一一处**，2026-10-18 亲读）：`MulliganFrame.ChangeCardButtonOnClick`
        /// 与 `MulliganFrame.SetupMulligan` / `MulliganFrame.UpdateButtonText`
        /// （`d:/2/tools/decomp_full/MulliganFrame__{ChangeCardButtonOnClick,SetupMulligan,UpdateButtonText}.c`）
        /// 三处逐字相同的三行：
        /// <c>uVar = "Battle/Mulligan/Replace"; if (*(int*)(card + 0x228) == 0xe) uVar = "Battle/Mulligan/Undo";</c>
        /// 然后 <c>Localize.set_Term(那颗钮, uVar)</c>。
        /// 那个 `0xe` 是 `CardScript` 上「这张已被选作换掉」的状态（同一次点击刚调过
        /// `CardScript.ClickMulliganSelected(true)`）⇒ **选中 = `Undo`、没选中 = `Replace`**。
        /// ⚠️ 文案两列**都自拟**（该键只在代码字面量里，值在远端 I2 表；`zh_CN.csv` 里
        /// **没有 `Undo` 这个英文串** —— 2026-10-18 按第一列精确查过）—— 如实标在 `Core/Loc.cs`。</para>
        /// <para>🔴 **2026-10-19（战斗线 `T17`）就地订正（铁律 5）—— 上面那句「两列都自拟 / 值在远端」【已过期】**：
        /// **原话** = 「两列都自拟（值在远端 I2 表；`zh_CN.csv` 没有 `Undo`）」·
        /// **现值** = 官方 I2 表 2026-10-14 已在本地，`Battle/Mulligan/Undo` = **`Undo` / `撤销`**，
        /// `Core/Loc.cs` 那条两列**就是官方值**（2026-10-14 核）·
        /// **错因** = 那句写于官方表解出之前；`zh_CN.csv` 0 命中只证明「不在那份旧表里」，不等于原版没有。</para></summary>
        public const string UndoTerm = "Battle/Mulligan/Undo";

        /// <summary>那颗钮现在该写什么 —— **判据只此一处**（建钮和 `ApplyMarks` 都取它，
        /// ⛔ 别在两处各判一次 `marked`）。</summary>
        public static string CardBtnWord(bool marked)
        {
            return Loc.T(marked ? UndoTerm : ReplaceTerm);
        }

        /// <summary>按**这段文本的语种**定字号 —— 转发到 `Label.SetScriptHeight`（那条判据的**唯一一份**实现，
        /// 内部接的是 `Loc.HasCjk`）；本件只负责「px → 本档局部单位」那一跳。
        /// 同族的另一处调用点见 `Battle/SettingsPanel.ApplyLangFont`（它现在也是转发）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）就地改一个数（铁律 5）**：`pxPerUnit` 从 **108 → 1** ——
        /// 本面板的字**已经全部在画布上**（`Label.CreateUgui` 那一路，局部单位 = 设计 px，
        /// `Label.PxPerUnit` = 1）⇒ 再传 108 会让字号**小 108 倍**（而画面只是「小得看不见」、**一个字都不报**）。
        /// 先例 = `SettingsPanel.ApplyLangFont`（批 2 同一处理）。
        /// ⛔ **这条只对本件成立**：世界档的调用点照旧传 108（本仓 89 个 `Label.Create` 调用点一个没动）。</para></summary>
        internal static void SetScriptFont(Label l, string text, float px)
        {
            if (l == null) return;
            l.SetScriptHeight(text, px, 1f);
        }

        /// <summary>先手 / 后手那两句 = **原版词条**（各一条，⛔ 别合并）。
        /// 🔴 **2026-10-18（第十二轮 · W6）改**：原来这两个是写死的**中文字面量**，注释写
        /// 「后手那句照原版英文兜底译；先手那句**原版英文没查到**（只有词条名 `GoFirst`）⇒ 我们译的」。
        /// **键那一半现在查清了**：`Battle/Tips/GoFirst` 与 `Battle/Mulligan/secondTurn` **都在本地能读到**
        /// （前者只在**代码字面量**里 —— `d:/2/tools/il2cpp_out/stringliteral.json` `0x428A128`，
        /// prefab 上零 `Localize`；后者是 prefab `mTerm`，TMP 原文 `You go second`）。
        /// ⚠️ **文案那一半没变**：`Battle/Tips/GoFirst` 的英文原文**本地取不到**（远端 I2 表）
        /// ⇒ `Loc` 表里那一条 EN/ZH **都自拟**（如实标着，⛔ 别写成「照抄原版」）。</summary>
        /// <para>🔴 **2026-10-19（战斗线 `T17`）就地订正（铁律 5）—— 上一句【已过期】**：
        /// **原话** = 「英文原文本地取不到（远端 I2 表）⇒ EN/ZH 都自拟」·
        /// **现值** = 官方表 `Battle/Tips/GoFirst` = **`You go first` / `你先行`**，`Core/Loc.cs` 那条
        /// **两列都是官方值**（EN 于 2026-10-14 `A1803` 订正、ZH 另有用户实拍旁证）·
        /// **错因** = 那句写于官方表解出之前，之后没有回来改（同一句在 `Core/Loc.cs:1138` 已有订正）。</para></summary>
        public static string TurnFirst { get { return Loc.T(TurnFirstTerm); } }
        public static string TurnSecond { get { return Loc.T(TurnSecondTerm); } }
        /// <summary>先手 / 后手那两条词条键 —— **只此一份**。</summary>
        public const string TurnFirstTerm = "Battle/Tips/GoFirst";
        public const string TurnSecondTerm = "Battle/Mulligan/secondTurn";

        public bool Visible { get; private set; }

        /// <summary>点「完成换牌」→ 回调「要换掉的手牌下标」（可能为空 = 不换）</summary>
        public Action<List<int>> OnDone;

        /// <summary>🔴 **2026-10-12（`A1663`）**：点了**手牌本身**（不是那颗「换」钮）→ 回调那张卡。
        ///
        /// 判据（原版，唯一一处）= `CardScript__OnTouchUpAsButton.c:52-58`：换牌期
        /// `cardState ∈ {0xD,0xE}` 就只做「置 `displayingCardFlag` → `BattleManager.DisplayCard(card,1,1)`」
        /// ⇒ **这一下是「看牌」，不是标记**。本面板**不知道**展示窗是谁（分层：面板只负责命中与回调），
        /// 由驱动层接着做（`BattleDriver.OnMulliganCardSeen` → `OnCardTapped`）。
        /// ⛔ 原来这里那条「点卡也能标记」是**我们自己加的**，已删 —— 别再加回来。</summary>
        public Action<CardView> OnCardSeen;

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：类型 `ImageQuad` → `HudPiece`** —— 这一族现在**多数是
        /// uGUI 件**（`RectTransform + Image`）。⛔ **一个件只许在一边**（要么旧 quad、要么 uGUI）。
        /// <para>🔴🔴 **2026-10-24（第七十三会话 · `M1`）就地订正（铁律 5）**：本条原来写「它**留在画布上**、
        /// 层序也对 —— 病灶在换牌卡那一侧」—— **已不成立**：画布排序层**先于** `renderQueue` 比较
        /// （`Battle/HudCanvas.cs:21`），所以画布压暗层**必然**盖住世界空间的换牌卡（真机实测卡区 max = 123
        /// = 线性 0.198 = 1 − 0.8）。⇒ `_shade` 现在是**世界空间 `ImageQuad`**（`HudPiece` 的 quad 支），
        /// 靠 `QMulliganShade`(3006) < `QMulliganCard`(3007) 排先后。其余件仍全在画布上（见 `OnCanvas`）。</para></summary>
        HudPiece _shade;
        HudPiece _bar, _play, _eye;      // Continue 的底 / 圆形播放钮 / 眼睛
        Label _prompt, _doneText;
        /// <summary>🆕 2026-09-26：「你先手 / 你后手」那一行（原版 `MulliganText/TurnText`）。</summary>
        Label _turnText;
        /// <summary>🔴 **2026-10-13（`A1751`）**：那一行该显示哪一档（`true` = 我方后手）。
        /// 由 `SetTurnText` 记下、`SetVisible(true)` 再写一遍 —— 见 `ApplyTurnText` 的注释。</summary>
        bool _turnSecond;
        readonly List<HudPiece> _cardBtns = new List<HudPiece>();

        // ================= 🆕 2026-10-19（B4 · `N70` P0-2）：换牌面板的按钮反馈 =================
        // 判据（原版 `battlearena1` 实读）：
        //   · `Continue Button`（完成换牌那条底）= **t=1 ColorTint**（默认灰）
        //   · `HideMulliganButton`（眼睛）= **t=2 SpriteSwap**：`40k_UI_bt_eye_hover/_pressed`
        //   · 每张牌那颗「替换」钮 = **t=2 SpriteSwap**：`UI_Button_Mulligan_hover/_Pressed`
        //     ⚠️ 它的 **Normal 随「已标记」变**（`UI_Button_Mulligan` ↔ `_Pressed`）⇒ 用 `SetNormal` 同步。
        //     🔴 **2026-10-20（W57 · `T09` A3-e1c）逐组件现读复核**（`bundle_battlesharedresources_assets_all`）：
        //        prefab = `CardMulliganButtonFrame` 里的 `Change Card Button`（`MB -3194195439979639558`）：
        //        `m_Transition = **2**`（Unity 枚举 `None=0/ColorTint=1/**SpriteSwap=2**/Animation=3`，
        //        见本机 `com.unity.ugui/Runtime/UGUI/UI/Core/Selectable.cs:133-152`）·
        //        `m_HighlightedSprite = 4884069812420410470`（= `UI_Button_Mulligan_hover`）·
        //        `m_PressedSprite = 1985575716887326321`（= `UI_Button_Mulligan_Pressed`）·
        //        `m_Colors` = Unity **默认四格**（t=2 下不生效）· `customColorTintTransitions = 0`
        //        （⇒ `EverguildButton__DoStateTransition.c` 那段自定义着色也跳过）。
        //        ⇒ 我们 `BattleButtonFx.Swap(...)` 与选牌面板**同族同图**，⛔ 别改成 ColorTint。
        BattleButtonFx _fxBar, _fxEye;
        readonly List<BattleButtonFx> _fxCardBtns = new List<BattleButtonFx>();
        readonly List<Label> _cardTexts = new List<Label>();
        readonly List<CardView> _cards = new List<CardView>();
        /// <summary>🔴 **2026-10-13**：那几张牌**搬进换牌区之前**的位姿（`Open` 时存、`ClearCardButtons` 还）。
        /// 不复位的话换牌结束后它们会**留在屏幕中央** —— `RefreshAll()` 只按 `_dealt`（这一轮新进手牌）
        /// 重摆，**不保证覆盖到这几张**（`HandLayout.Place` 的调用面看的是那一份）。
        /// 🔴 **2026-10-21（第六十一会话 · 换牌残余 ②）**：改成存**父空间局部位**（`localPosition`）
        /// + **绕 Z 角**，归还走 `CardView.SetPose` —— 见 <see cref="ClearCardButtons"/> 里那段「为什么必须是
        /// `SetPose`」的理由（`CardView._baseScale` 只有 `SetPose` 会写，直接写 `transform.localScale`
        /// 会让它停在**手牌尺度**上，之后任何一次 `ApplyScale()` 都把卡缩回手牌大小）。</summary>
        readonly List<Vector3> _savedPos = new List<Vector3>();
        readonly List<float> _savedRotZ = new List<float>();
        readonly List<Vector3> _savedScale = new List<Vector3>();
        /// <summary>🆕 **2026-10-22（W62 · `D6`）**：这几张牌**搬进换牌区之前**的渲染队列
        /// （`ClearCardButtons` 还回去；为什么抬 → <see cref="QMulliganCard"/>）。与 `_savedPos` 成对。</summary>
        readonly List<int> _savedQueue = new List<int>();
        readonly List<int> _marked = new List<int>();

        // ── 🆕 2026-10-16（`T09` 批 3）：画布那一侧的两个节点 ──────────────────────────────────
        /// <summary>本面板那块 **uGUI 子画布**的 `RectTransform`（`OverHand` / order 0；建不出来 ⇒ `null`，
        /// 那一档 `Create` 直接**不建面板**）。</summary>
        RectTransform _cv;
        /// <summary>🔴 **本面板所有图与字的父节点** = 画布上那个**整屏帧** `Mulligan`（`FillParent`）。
        /// <para>**它的中心 = 屏心、局部单位 = 设计 px（y 向上）** ⇒ 面板内设计 px 坐标走
        /// <see cref="HudCanvas.PosPx"/>；命中判定/读回走它的 `InverseTransformPoint`（见 <see cref="PxOf"/>）。</para>
        /// <para>⛔ **与 `transform`（世界空间那层壳）不是一回事**：壳的单位是**世界单位**、只装换牌区那一行的
        /// 世界位换算（见文件头那条「有意不迁」）。面板内的件**一律挂 `_frame`**。</para></summary>
        RectTransform _frame;
        /// <summary>自检用：本面板那块**子画布**的 `RectTransform`（查它的层/order/`overrideSorting` 用）。</summary>
        public RectTransform CanvasRect { get { return _cv; } }
        /// <summary>🔴 **自检用（`T09` 批 3）**：画布上那层**面板帧**（`Mulligan`）。
        /// 自检要量「件的世界位 vs 原版 rect」、或要「1 设计 px 的世界向量」时用它
        /// （⛔ 不是 `transform` —— 那是世界空间那层壳，非 16:9 下两个帧差 1.333 倍）。</summary>
        public RectTransform FrameRect { get { return _frame; } }

        // ---- 原版绝对坐标（1920×1080，y 从**上**）----
        const float PromptCx = 967f, PromptCy = 106.5f, PromptW = 1344f, PromptH = 79.4f;

        /// <summary>🔴 **2026-10-16（`A2102④` · `A1104` 同族）：提示行那颗 TMP【自己有框与字号】。**
        /// 判据 = **主对话现读**：`MulliganText` 容器的 `m_Children`（`RectTransform_2828.json`，
        /// `bundle_scenes_scenes_battlearena1`）→ 子件 **`Text`（RT 3442）**，`m_SizeDelta = (−36.936, −10.0)`
        /// + 父框 1344×79.44 ⇒ **框 1307.064 × 69.44**；它身上那颗 TMP 逐字段：
        /// `m_text = "Choose cards to replace in first hand"` · `m_fontSize = 65` · `m_fontSizeMin = 18` ·
        /// `m_fontSizeMax = 65` · **`m_fontSizeBase = 36`** · `m_enableAutoSizing = 1`。
        /// ⚠️ 宽 **1307.064** 与 `TurnText` 那颗**同值**（同一份 `m_SizeDelta.x` —— 同一个容器，不是巧合）。
        /// ✅ 这**回答了**本类 §`TurnCx` 那条 doc 里留的问题（「按 auto 逼高、还是钉运行期值」）——
        ///   **与 `TurnText` 那次同一口径：按 auto 逼高**，⛔ **不抄运行期实渲值 53.7**。
        ///   原文还写「提示行反过来（我们 43.67 · 原版 53.7）」—— 那正是**我们没开自适应**的结果，
        ///   本轮按判据开了。</summary>
        const float PromptBoxW = 1307.064f, PromptBoxH = 69.44f;
        /// <summary>提示行那颗 TMP 的自适应三档（画布 px）：`min 18` · `max 65` · **`base 36`**。
        /// ⚠️ `base` **不是** max（这一颗原版写的就是 36）—— ⛔ 别照 `TurnText` 那次把 base 写成 max。</summary>
        const float PromptFsMin = 18f, PromptFsMax = 65f, PromptFsBase = 36f;

        /// <summary>🔴 **2026-10-16（`A2102④` 的另一半）：「继续」那颗钮上的字【自己有框与字号】。**
        /// 判据 = **主对话现读** `bundle_scenes_scenes_battlearena1`：`MulliganContinueButton` 的子件
        /// **`Text`**（RT `m_SizeDelta = (368.929, 62.242)` —— 它是**锚定档**、不是拉伸子件 ⇒
        /// **框就是这两个数本身**，⛔ 别再去加父框）；它身上那颗 TMP：
        /// `m_text = "Continue"` · `m_fontSize = 45` · `m_fontSizeMin = 18` · `m_fontSizeMax = 45` ·
        /// `m_fontSizeBase = 36` · `m_enableAutoSizing = 1`。
        /// ⚠️ 本类 `TurnCx` 那条 doc 里写的「钮上「继续」**45.0**」= 这颗的 **`m_fontSize`（= max）**，
        ///   **不是运行期实渲值** —— 两回事，别再混。
        /// ⚠️ 钮上那串字**会被换成倒计时秒数**（原版行为）⇒ 框与三档**只在建件时设一次**，
        ///   换字走 `SetDoneText` 的按语种那一句（见它）。</summary>
        const float DoneBoxW = 368.929f, DoneBoxH = 62.242f;
        const float DoneFsMin = 18f, DoneFsMax = 45f, DoneFsBase = 36f;
        /// <summary>🆕 2026-09-26：**「你先手 / 你后手」那一行**（原版节点 `MulliganText/TurnText`）。
        /// 🔴 **2026-09-29 取到真 rect、已照它改**：原版 `TurnText` = **左上 (312.50, 129.42) · 1307.06×54.17
        /// · 中心 (966.03, 156.50)**。出处：`bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3403.json`
        /// （父 `MulliganText` = `_2828.json`：左上 (295.00, 66.78) · 1344×79.44，**它自己的中心是 (967, 106.5)**）
        /// —— **序列化在场景里**，运行期 dump 逐位相同、**不在任何 clip/DOTween 里**
        /// （全包 grep `Animation/Animator/CanvasGroup` 零命中，`MulliganText` 上连脚本组件都没有）。
        /// 🔴 **2026-10-18（第六会话 · `A1076`）按现读逐字段复核 = 上面那串数逐位正确**（复核，非转抄）：
        /// · RT 类资产按 **PathID** 命名（`RectTransform_&lt;pid&gt;.json`）、**没有名字索引** ⇒ 按「TurnText」
        ///   搜文件名必然零命中。⚠️ **原来记的「RT 没取到、不在那 988 个 RT 里、怀疑在预制体那边」就是这么来的**
        ///   —— **把「搜不到」记成了「本地没有」**（铁律 2 / 铁律 5 的又一例）；节点名在
        ///   `GameObject/TurnText_293.json`（它的 `m_Component[0]` 就是 RT_3403）。
        /// 🔑 **「是这一行、不是别的」的判据**：原版 `MulliganManager`（`MonoBehaviour_4352.json`，挂在同级 GO `Mulligan` 上）
        ///   有字段 `mulliganTurnTextLocalize` → MB **5183**，**正是这颗 `TurnText` 身上的 `Localize`**
        ///   （`mTerm = "Battle/Mulligan/secondTurn"`）；对照它另两颗 `mulliganTextLocalize` → MB 5197（提示行）、
        ///   `mulliganTextObj` → **GO 319 = `MulliganText` 容器**。（比「按名字找节点」强得多，2026-10-18 现读。）
        /// · **13 个战场场景逐字段相同**（arena1 `RT_3403` · arena2 `RT_3598` · … · tauviorla `RT_4000`；
        ///   全为 `ap(-0.971924,-49.999969) sd(-36.936001,-25.271999) anchor(0,0)-(1,1) piv(.5,.5)`）。
        /// · **运行期实况 = 静态逐位相同**：`资料/原版参照图/Unity参照管线_0825/data/panel_0914/runtime_rect_mulligan.tsv:13`
        ///   · `panel_0914b/p3_mulligan_tree.tsv:15`（sd −36.94,−25.27 · rect 1307.06,54.17 · ap −0.97,−50.00）。
        /// ⚠️ 沿革：我们自己原先那版中心在 y=178（比原版低 21.5 px），该值今日已无从复核。
        /// 字号：原版 TMP `m_fontSize = **55**`（auto 18–55，Center/Middle）—— 用 `SetGlyphHeight(55px)` 表达
        /// （`MenuDraw.Text` 传的 `fontPx` 就是这一个口；`SetCapHeight` 那半高写法会偏小）。
        /// 🔴 **2026-10-18 顺手查出、本轮未改**：那一颗 TMP 是 **autoSize=1（18~55）**，运行期**实测渲染字号 = 41.8**
        /// （同一棵树：提示行 53.7 · 钮上「继续」45.0）⇒ 我们写死 55 **比原版实渲大 ≈32%**；提示行反过来
        /// （我们 43.67 · 原版 53.7）。**改法要先定「按 auto 逼高、还是钉运行期值」**（auto 的产物随语种/字体变），
        /// 故**不擅自改** —— 记在 `资料/普查产出_第六会话/W_换牌那行rect_A1076.md`。
        /// ✅ **颜色那句 2026-10-18 订正（铁律 5）**：这里原写「已知未改：原版纯白、我们用的是暖色 (1,0.94,0.82)」
        /// —— **已不成立**：2026-09-30 两行都改成 `Color.white` 了（本文件 `Create()` 里那两处
        /// `Label.Create(…, Color.white, …)`；判据 = `MonoBehaviour_3731.json` / `MonoBehaviour_3856.json`
        /// 的 `m_fontColor32 = 4294967295`）。断言在 `Editor/BattleScene` 里那条断 `m_fontColor32` 的检查。</summary>
        const float TurnCx = 966.03f, TurnCy = 156.5f, TurnW = 1307.06f, TurnH = 54.17f, TurnPx = 55f;
        /// <summary>🔴 **2026-10-21（第六十一会话 · 换牌残余 ④）**：那一颗 TMP 的 **`m_fontSizeBase`**。
        /// 判据 = 现读 `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3856.json`：
        /// `m_fontSize = 55`（= 我们把 `TurnPx` 当标称字号那一档）· **`m_fontSizeBase = 36`** ·
        /// `m_fontSizeMin = 18` · `m_fontSizeMax = 55` · `m_enableAutoSizing = 1`。
        /// ⚠️ 原来 `SetAutoFitBox` 的 base 实参写的是 `TurnPx`（55）—— **与字段不符**（35%）。
        /// 它只改自适应二分的**起点**（TMP 从 `m_fontSizeMax` 起步、`base` 是回写值），视觉差异小，
        /// 但「原版字段是什么就写什么」（铁律 3），⛔ 别再拿 `TurnPx` 顶它。
        /// <para>运行期实渲（原版中文档同一棵树、同一串「你是第二个行动」）= **41.8**（提示行 53.7 ·
        /// 钮上「继续」45.0，出处 `panel_0914b/p3_mulligan_tree.tsv:13-15`）—— 那个数是**自适应在
        /// 原版字体资产上的产物**，⛔ 不是可以钉进来的常量（`A1104` 已裁定：复刻 auto + 上下限 + 框）。
        /// ⇒ 本条收口 = **框 1307.064×54.168 + 18/55/36 + auto=1** 四格与原版逐字段一致。</para></summary>
        const float TurnFsBase = 36f;
        /// <summary>「完成换牌」那颗钮（原版 `MulliganContinueButton/Button`）的中心与框：
        /// `BarCx/BarCy` = 原版那颗的框中心（`RT_2659` 的绝对矩形 1322.7–1900.2 × 948.3–1012.2 ⇒
        /// (1611.45, 980.25)，与 `工具/menu_dump.py bundle_scenes_scenes_battlearena1 "Mulligan"`
        /// 那行逐位相同）；`BarW × BarH` = 它的 `m_SizeDelta`。
        /// 🔴 **2026-10-19（A1301③）**：`BarH` 从 `63.8f` 改成 **`63.84f`** —— 它同时是那颗钮的
        /// **框高**与**画出来的高**（原版那颗 `Image` 是 `m_PreserveAspect = 0` ⇒ 画出来的 = 框），
        /// 而原版 `m_SizeDelta.y` 逐位是 **63.84000015258789**
        /// （`bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2659.json`）。
        /// 原来那个 `63.8` 是四舍五入写下来的（差 0.04 px，`Editor/BattleScene.cs` 那条容差 1.5 的断言
        /// 放过它了）⇒ 现在一个数同时管「画多大」与「命中框多大」（⛔ 别再拆成两个近似值）。</summary>
        const float BarCx = 1611.45f, BarCy = 980.25f, BarW = 577.5f, BarH = 63.84f;
        /// <summary>🔴 **2026-10-19（A1301③）**：原版那颗 `Button` 的 **`m_RaycastPadding`**
        /// （分量序 **L,B,R,T**；**负值 = 外扩**）= **(0, −40, 0, −40)** ⇒ 命中框 **577.5 × 143.84**
        /// （上下各外扩 40、横向不动）。实据 =
        /// `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_5292.json`（`m_RaycastTarget = 1`）。
        /// ⚠️ 同一个 pad 在 `MB_4202`（`BattleContinueButton` / `Generic Multi Card Display Combat`，
        /// 见 `Battle/MultiCardDisplay.cs`，A1061 已收）上还有一份 —— 两处**同值**，判据只此一份。</summary>
        const float DonePadL = 0f, DonePadB = -40f, DonePadR = 0f, DonePadT = -40f;
        const float PlayCx = 1763.45f, PlayCy = 980.25f, PlayH = 79.6f;
        const float DoneCx = 1525.05f, DoneCy = 981.05f, DoneW = 368.9f, DoneH = 62.2f;
        const float EyeCx = 184.45f, EyeCy = 908.5f, EyeH = 79.6f;

        /// <summary>面板内**设计 px** → **世界单位**（= **世界空间那一套帧**的口径，恒 ÷108）。
        /// 🔴 **2026-10-16（`T09` 批 3）：本件只剩【换牌区那一行】在用**（`RowLocalAt` —— 那几张卡还留在
        /// 世界空间，见文件头那两条「有意不迁」）。**画布那一侧的件一律直接用 `px`**
        /// （`_frame` 的局部单位**就是**设计 px，见 `HudCanvas` 文件头那条推导）——
        /// ⛔ **别再拿 `U()` 去喂画布上的建件口**：那会小 108 倍，**而画面只是「小得看不见」、一个字都不报**。</summary>
        static float U(float px) { return px / 108f; }

        /// <summary>设计 px（左上原点、y 向下）→ **世界坐标**（**世界空间那一套帧**）。
        /// 🔴 **2026-10-16（`T09` 批 3）：本件只剩 `RowLocalAt` 与自检的探测器在用**（理由同 `U()`）。
        /// 画布上的件**不在这一帧里** —— 它们的坐标走 <see cref="HudCanvas.PosPx"/>。⛔ 别混。</summary>
        static Vector3 At(float cx, float cy) { return LayoutSpace.ToWorld(cx / 1920f, 1f - cy / 1080f); }

        // ────────────────────────────────────────────────────────────────────
        //  🆕 2026-10-16（`T09` 批 3）：画布那一侧的口（**判据各只此一份**）
        // ────────────────────────────────────────────────────────────────────

        /// <summary>🔴 本面板那块 **uGUI 子画布**的名字（= 原版宿主名 + `" Canvas"`，见 `T09` §五·补⑩ 第 1 步）。
        /// 判据 = `T09c` §一 我这一行：`Mulligan` @ `…/FrontCanvas/Safe area FrontCanvas`，
        /// **`OverHand` / order 0**。⛔ **只此一份**（建它、自检找它都取这里）。</summary>
        public const string SubCanvasName = "Mulligan Canvas";

        /// <summary>换牌区第 i 张（共 n 张）**相对行中心的横向偏移**（**设计 px**）。
        /// <para>🔑 **只此一份** —— 世界那条（<see cref="RowLocalAt"/>，给卡用）把它过一遍 `U()`；
        /// 画布那条（钮那一侧）直接用。⛔ **别在两处各推一遍**（本仓红线：两处写同一条规则 = 迟早不一致）。</para></summary>
        static float RowDX(int i, int n) { return RowSpacingPx * (i - (n - 1) * 0.5f); }

        /// <summary>面板内**设计 px** → 画布帧 `_frame` 的**局部坐标**（中心原点、**y 向上**）。
        /// = <see cref="HudCanvas.PosPx"/>（`_frame` 是 `FillParent` ⇒ 中心 = 屏心，两者逐位同值）。
        /// ⚠️ 给**字**（`MakeText` 收 y 向上）与 `localPosition` 用；给**图**那一族用 <see cref="Img"/>
        /// （`MakeImageInParent` 那两个形参的口径是「相对父中心、**y 向下**」—— 见它自己的 doc）。</summary>
        static Vector3 Px(float cx, float cy) { return HudCanvas.PosPx(cx, cy); }

        /// <summary>建一块图：位置 = 面板内**设计 px**（左上原点、y 向下，= 原版 prefab 那一套），
        /// `hPx` = 画出来的**高**、**宽由贴图比例来**（= 旧 `ImageQuad.Create(…, worldH, …)` 的语义；
        /// 要盖掉比例的建完自己调 `SetAspect` —— 原版 `PA=0` 那几颗就是这么干的）。
        /// <para>🔑 **走 `MakeImageInParent`（中心锚）而不是 `MakeImage`/`QuadFromTopPx`（归一化锚）**：
        /// 后两个把 `x/y` 当**屏幕绝对 px**，只在父 rect 恰好是 1920×1080 时才等于「相对父中心」——
        /// 而本帧在非 16:9 下比它高（`CanvasScaler(Expand)`）⇒ 套错了会**静默摆到别处**（`T09` §七·10）。</para>
        /// <para>⚠️ 贴图取不到 ⇒ 返回 `null`（⛔ 不静默建一块白图）。</para></summary>
        static HudPiece Img(Transform parent, Texture2D tex, string name, float cx, float cy, float hPx, float z,
                            Color? tint = null)
        {
            if (tex == null) return null;
            float w = hPx * tex.width / Mathf.Max(1f, tex.height);
            var l = Px(cx, cy);
            return HudCanvas.MakeImageInParent(parent, tex, name, l.x, -l.y, w, hPx, z, tint ?? Color.white);
        }

        /// <summary>把面板内**设计 px**（左上原点、y 向下）换算成**这一件真正画在哪**（世界坐标）。
        /// <para>🔴 **判据只此一份** —— 命中判定、`*WorldPos` 那一族、自检的探测器全走它。
        /// ⛔ **不是 `At()`**：`At()` 交的是**世界空间那一帧**的坐标，而画布上的件**不在那一帧里**
        /// （非 16:9 下两帧差 1.333 倍 ⇒ 用错了就是「看得见的地方点不到」）。</para>
        /// <para>没建出来（`_frame == null`）⇒ `Vector3.zero`（同 `CardBtnWorldPos` 那一档的口径）。</para></summary>
        Vector3 WorldOf(float cx, float cy)
        {
            if (_frame == null) return Vector3.zero;
            return _frame.TransformPoint(Px(cx, cy));
        }

        /// <summary>🔴 **`WorldOf` 的逆**：世界坐标 → 面板内**设计 px**（左上原点、y 向下）。
        /// 命中判定（<see cref="HitDone"/>）与自检「量一个件的世界位 vs 原版 rect」那条都用它。
        /// <para>⚠️ **别用 `LayoutSpace.PxX/PxY`** 读画布上的件 —— 那两个是**世界空间那一帧**的逆
        /// （y 恒 108px/单位），而画布在非 16:9 下**两轴系数不同**（`T09` §七·8）⇒ 量出来是假数。</para>
        /// <para>没建出来 ⇒ `(NaN, NaN)`（⛔ 不返 `(0,0)`：那与「真的在屏心」分不开，
        /// 同 `CardButtonLocalZ` 返 `NaN` 那条口径）。</para></summary>
        public Vector2 PxOf(Vector3 world)
        {
            if (_frame == null) return new Vector2(float.NaN, float.NaN);
            var l = _frame.InverseTransformPoint(world);       // 面板局部（= 设计 px、y 向上）
            return new Vector2(HudCanvas.ReferenceResolution.x * 0.5f + l.x,
                               HudCanvas.ReferenceResolution.y * 0.5f - l.y);
        }

        /// <summary>本面板各层的**层深度**（旧那套是**世界 z**：**大的在后**、先画）。
        /// 和日志面板一样推到 **−0.9** 一带（`−0.5` 那层场上的粒子会飘到面板上面 —— 那个坑记在
        /// `BattleLogPanel` 的注释里）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）**：本面板的件**已经在画布上** ⇒ 这个数现在**只用来登记
        /// 兄弟序**（`HudCanvas.Add`，语义**同向**：z 大的排前面 = 画在底下；同 z 按建件先后）
        /// —— ⛔ **别再把它写进 `localPosition.z`**（画布上写 z 会把件推离画布平面）。
        /// ⚠️ 它还留着**另一个用处**：`RowWorldPosAt` 给**卡**的局部位里那一份 `Z`
        /// （卡还在世界空间，那一份是**真的世界 z**）—— 两处共用这一个常量是**有意的**（同一层）。</para></summary>
        const float Z = -0.9f;

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-22（W62 · 战斗线 `D6`）；🔴 **2026-10-24（第七十三会话 · `M1`）整段重写 —— 号段反了**
        //     —— 换牌卡的**渲染队列**（`QMulliganCard`）· 压暗层的**队列**（`QMulliganShade`）· 压暗层的**透明度**
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>换牌期那层**整屏压暗**的渲染队列号 = **3006**。
        /// <para>**它必须是世界空间 `MeshRenderer` 的队列**（`ImageQuad`）—— **不能是画布件**：
        /// `Canvas` 的排序层**先于** `renderQueue` 比较（`Battle/HudCanvas.cs:21`），换牌卡与世界件都在
        /// `Default` 层 ⇒ 任何排序层高于 `Default` 的画布都会盖住卡（2026-10-24 真机实测：队列 3001 的
        /// 换牌卡被 `OverHand` 画布上的压暗层盖着，透射 0.198 = 1 − 0.8）。</para>
        /// <para>**下界 = 战场的最大队列**：战场几何是 `Default` 层、`Queue = 3000 + _QueueOffset`
        /// （`WarpforgeArena1/Editor/ArenaBuilder.cs:5207`）。13 场 manifest 逐场数（`meshes`+`particles`
        /// 的 `matQueue`）：>3000 的**一共 18 件**，只有 **3001×3 / 3003×3 / 3004×8 / 3005×4**，最大 **3005**
        /// （`battlearena1` 与 `battlearena2` 都有；**9 场一件都没有**）⇒ 取 **3006** 才能把
        /// **整个战场**（含粒子）一起压暗。</para>
        /// <para>**上界**：必须 **&lt; `QMulliganCard`** —— 否则换牌卡又被压暗。</para></summary>
        public const int QMulliganShade = 3006;

        /// <summary>换牌期这几张卡用的**渲染队列号** = **3007**（= 压暗层之上、放大窗之下）。
        /// <para>🔴 **2026-10-24（第七十三会话 · `M1`）就地订正（铁律 5）**：本条原来写 **3001**、理由是
        /// 「`renderQueue` 比较**先于**排序层 ⇒ 抬到 3001 就画在画布压暗层之上」。**那条理由不成立**
        /// （W62 未经验证的推断）：真机上 3001 的卡**仍然被压暗**（换牌卡区 max = 123 ≈ 线性 0.198 = 1 − 0.8）。
        /// 正确规则 = **画布排序层先比、队列轮不到**（同 `HudCanvas.cs:21`）⇒ 压暗层必须也在世界空间
        /// （见 <see cref="QMulliganShade"/>），卡只要**高于它**即可。
        /// 卡从 3001 抬到 **3007** 是与压暗层（3006）**成对**的改动 —— 只改一边就会回到「卡暗」或「压暗无效」。</para>
        /// <para>**上界**：必须 **&lt; `CardFan.QCardBase`(3009)** —— 「点卡放大」那扇
        /// `CardDisplayWindow` 的卡是 3009..3011，它得压在换牌卡之上（原版就是这样：点一下牌看大图）。</para></summary>
        public const int QMulliganCard = 3007;

        /// <summary>压暗层的**不透明度** = **原版 `Shade.onAlphaLevel`**。
        /// <para>判据 = `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4399.json`（13 场同名同值）：
        /// `"onAlphaLevel": 0.800000011920929` · `"defaultTimeToSwitch": 0.5` · `shadeUI` → `MB_4215`（那件 `Image`，
        /// 出厂 `m_Color = (0,0,0,0)`、`m_Sprite = null` ⇒ 运行时只改颜色）。`GameObject/ShadePanel.json` 出厂
        /// `m_IsActive = false`。</para>
        /// <para>🔴 **2026-10-22（W62 · `D6`）就地订正（铁律 5）：原来这里写死 `0.88`、并怀疑「染色路径让 alpha 没生效」
        /// —— 【染色没问题】，是本条的错因判断错了。**实测（同一相机、同一战场，`21_battle_start.png`（有压暗）
        /// vs `24_hand_settled.png`（无压暗），背景废墟区 36,516 个像素，**按线性光**拟合）：
        /// 透射 = **0.1206**（中位）⇒ 反推 alpha = **0.879** = 写死的 0.88（误差在 8bit 量化内）。
        /// **错因**：拿 **sRGB 空间的比值**当透射 —— 本工程是 **Linear**（`ProjectSettings.asset:50`
        /// `m_ActiveColorSpace: 1`），`0.88` alpha 在 sRGB 上看起来就是「只剩 ~0.29–0.33」而不是 0.12
        /// （底色越暗这个比值越小，因为它是一个 gamma 曲线上的复合，不是一个常数）。
        /// ⇒ 真机复拍时**判据用线性光透射**：压暗区 = `1 − alpha` = **0.20**；卡上 = **1.0**。</para></summary>
        public const float ShadeAlpha = 0.8f;


        // ==================================================================
        //  换牌区那一行（原版 `MulliganAnchor` + 它身上的 `CardsHorizontalLayout`）
        //
        //  🔴 **2026-10-13（用户点名「开局的手卡替换你改错了，要求是原版」+ 给了实拍之后重做）**：
        //     原来我们把「换」钮贴在手牌原位（屏幕底部那排小牌）上 —— 原版**不是**那样：
        //     起手牌会被**逐张搬进 `MulliganAnchor` 那一行**（`PlayerHand.MoveCardToMulliganPos.c`
        //     的位 = `MulliganAnchor` 的 `CardsHorizontalLayout.GetPosition(i,n)`），换完
        //     `CompleteMulliganPhase` 把**同一批对象**并进真手牌列表 ⇒ 牌**从来不回手牌位**。
        // ==================================================================

        /// <summary>行中心 = **屏幕中心 + (0, −29) 设计px**（即 y = 540 − 29 = **511**）。
        /// 判据：原版 `MulliganAnchor` 是**只有 Transform、没有 RectTransform** 的节点，
        /// `m_LocalPosition = (0, **29**, 0)`，父件 `Mulligan` 整屏拉伸（pivot 居中）
        /// ⇒ 世界位 = 屏幕中心 + 29 px（Unity y 向上 ⇒ 从**上**数是 511）。
        /// 出处 = `bundle_scenes_scenes_battlearena1/Transform/Transform_1292.json` +
        /// 运行期 dump `资料/原版参照图/Unity参照管线_0825/data/panel_0914/runtime_rect_mulligan.tsv`
        /// （`/MulliganAnchor` 那行是 `TF`、没有 rect）。
        /// ✅ 与用户实拍互证：那张图上三张牌的**中心**量出来 ≈ 514 设计 px（差 &lt;1%）。</summary>
        const float RowCx = 960f, RowCy = 511f;

        /// <summary>换牌区那张卡的宽度（设计 px）= **292**。判据 = 用户给的原版实拍
        /// `C:/Users/qjh36/Desktop/warpforge参考图/战斗—替换手卡参考图（先手）.png` 逐像素量：
        /// 三张卡**同为 240 帧px**（x 372–612 / 700–940 / 1028–1268），而那一帧就是整块 1920×1080
        /// 画布（纵向量得的行中心 514 ↔ 预制体锚点 511，**两轴自洽**）⇒ `240 × 1080/887 = 292`。
        /// 🔴 **2026-10-19（战斗线 T10 · `A1749` 结案）：选牌面板【不偏大】、两扇面板【运行期输出本来就不同】**。
        ///   判据 = `资料/普查产出_1016战斗判据/T10_换牌阶段判据.md` §`A1749`：
        ///   · `MB_4052`（选牌）与 `MB_4881`（换牌）序列化字段**逐字相同**（`m_scale=183.41`、
        ///     `m_betweenElementsSpacing=502.2`），但 `CardsHorizontalLayout.GetPosition` 还吃运行态的
        ///     `m_elemetsParent.lossyScale` / `m_calculatedMaxLayoutSize` / 压缩分支 / `MaxCardsInHand`
        ///     ⇒ **同一组件不保证同一个输出**（铁律 5·c 那一类）；
        ///   · 选牌那一行：原版**运行期探针**量到中心距 **502.2 px**（`panel_0914b/报告.md:61-75`）
        ///     ⇒ 我们 `CardChoicePanel` 的 `383.8 / 502.2` **是对的，不动**；
        ///   · 换牌这一行：实拍量到卡宽 **292** ⇒ 本值与 `RowSpacingPx` **保持**。
        /// ⛔ 别再按「183.41 px / 卡本地单位」往换牌这一侧推（那会得到 383.9 的卡宽 —— 实拍否掉了）。</summary>
        const float CardWidthPx = 292f;

        /// <summary>换牌区相邻两张的**中心距**（设计 px）= **414**。判据 = 同上那张实拍
        /// （用户 2026-10-13 给的视频帧，**按 0.821 归一** —— 见 `T10_换牌阶段判据` §三-4）。
        /// ⚠️ **2026-10-19（T10）复核：本行原来的推导数自相矛盾、且与逐像素边界对不上，如实记**：
        ///   · 逐像素边界（372–612 / 700–940 / 1028–1268）⇒ 中心 492 / 820 / 1148 帧px
        ///     ⇒ **间距 328 帧px ⇒ 399 设计 px**（不是 414）；
        ///   · 原文另写「卡中心 485 / 840 / 1205 ⇒ 间距 340」——**算式本身不成立**（840−485=355、1205−840=365），
        ///     且与逐像素边界对不上。
        ///   ⇒ **414 暂时保留**（`T10_换牌阶段判据` §`A1749` 的裁定是「换牌保持不动」；且三个中心相对
        ///   屏心 960 还整体偏右 ≈38 px，说明这张视频帧的横向对齐本身没校准死）。
        ///   🔴 **真机复测后以实拍为准** —— 那时把这一行连同推导一起改掉，⛔ 别照 414 反推。</summary>
        const float RowSpacingPx = 414f;

        /// <summary>每张牌那颗钮（原版 `CardMulliganButtonFrame/Change Card Button`：
        /// `sd = (1.30, 0.33)`，中心 `ap = (0, −1.7513)`，而那一层框是 `2.3335 × 4.167`）。
        /// 换算：框宽 2.3335 ↔ 卡宽 ⇒ 每一「框单位」= 卡宽 / 2.3335 ⇒
        /// · 钮宽 = `1.30 / 2.3335 = **卡宽的 55.7%**`；
        /// · 钮高 = `0.33 / 2.3335`（同上换算）；
        /// · 钮中心在**卡下沿下方** `(1.7513 − 卡半高 1.6655) = 0.0858` 框单位
        ///   —— ⚠️ 这一格**取自实拍更直接**：实拍量得卡底 600、钮中心 632 ⇒ 32 帧px = **39 设计 px**。
        ///   （两个读法差 3 px，取实拍那个。）</summary>
        // 🔴 **2026-10-21（第六十会话）**：宽度从 `1.30/2.3335 = 0.557` 改成 **1.086 卡宽**
        //    —— 判据 = 原版 `ReplaceTextBg` 的 `RectTransform_-5362673195939889351.json`
        //    `m_SizeDelta = 2.5345×0.8232` 卡单位（≈317 设计 px，与参考图「替换」横条 292–316 px 对得上）；
        //    原来只画 161 px（参考的 55%）。高度不动（参考条≈40 px，原图按钮 0.33 框单位）。
        const float BtnWFactor = 1.086f;               // 钮宽 = 卡宽 × 它（原版 `ReplaceTextBg` 宽）
        const float BtnHFactor = 0.33f / 2.3335f;      // 钮高 = 卡宽 × 它
        const float BtnBelowCardPx = 39f;              // 钮**中心**在卡下沿下方多少 px

        /// <summary>换牌区那张卡的 `localScale`（= 卡宽 292 px）。⚠️ 与手牌同一份换算
        /// （`HandLayout.DefaultCardScale = 165/(CardView.Width×108)`）：卡宽 = `CardView.Width × scale × 108` px。</summary>
        static readonly float MulliganCardScale = CardWidthPx / (CardView.Width * 108f);

        /// <summary>换牌区那一行里**第 i 张（共 n 张）**的**世界位**。
        ///
        /// 🔴 **2026-10-13（实机才暴露的一条）**：`At()` 给的是**世界空间那一套帧**的坐标
        ///    （本面板那层**壳** `transform` 就在那一帧上），而**卡不在本面板的父链下**
        ///    （它们在 `HandArea` 里）⇒ 两者**不是一回事**（批处理里恰好相等 ⇒ 自检全绿；
        ///    实机里差一个缩放 ⇒ 牌摆歪）。所以这里**必须**经 `transform.TransformPoint` 换成真世界位。
        /// 🔴🔴 **2026-10-16（`T09` 批 3）**：本面板的**钮/压暗/字已经搬到画布上**（那是另一个帧），
        ///    **但这一条一个字都没改** —— 它算的是**卡**的位置，而**卡还留在世界空间**（第 5 步才迁）
        ///    ⇒ 卡的世界位**逐位不变**（任何宽高比下都是）。见文件头那两条「有意不迁」。
        /// ⚠️ 喂 `transform.position` / `DOMove`（世界）的用它；喂 `CardView.SetPose`（局部）的用
        ///    <see cref="RowLocalPos"/>。⛔ 判据（行心/卡宽/间距）仍只在这一处算。</summary>
        public Vector3 RowWorldPosAt(int i, int n)
        {
            // ⚠️ 局部位里**连 `Z`（这一层的深度）一起**给上 —— 卡是按「本面板局部位」喂进来的，
            //    少了它卡会落在这个面板的深处（`Z` 是我们给这一层定的那个常量）。
            return transform.TransformPoint(RowLocalAt(i, n) + new Vector3(0f, 0f, Z));
        }

        /// <summary>换牌区那一行第 i 张（共 n 张）在**本面板壳的局部位**里的位置（**不含**深度 `Z`）。
        /// 🔴 **判据只此一处**：卡走 <see cref="RowWorldPosAt"/>（世界）而**钮走 <see cref="RowDX"/>
        /// 那条设计 px 的路**（`BuildCardButtons`）—— **间距与行心的常量只此一份**，
        /// 两个帧各自做一次换算（⛔ 别在两处各推一遍，本仓红线）。</summary>
        public Vector3 RowLocalAt(int i, int n)
        {
            var c = At(RowCx, RowCy);
            return new Vector3(c.x + U(RowDX(i, n)), c.y, c.z);
        }

        /// <summary>这张牌在换牌区那一行里的**世界位**；**它不在这一批里** ⇒ 返回 `Vector3.zero`。
        /// ⚠️ **世界**位是给「飞牌动画」那条路的（`CardFeel.DealInPose` → `DOMove` 收世界位）；
        ///    要给 `CardView.SetPose` 用请走 <see cref="RowLocalPos"/>（那个吃**局部**位）。</summary>
        public Vector3 RowWorldPos(CardView c)
        {
            int i = _cards.IndexOf(c);
            return i < 0 ? Vector3.zero : RowWorldPosAt(i, _cards.Count);
        }

        /// <summary>同上，但换算成**这张卡父空间的局部位** —— 给 `CardView.SetPose` 用。
        ///
        /// 🔴 **2026-10-13（实机才暴露的一条）**：`RowWorldPosAt` 给的是**世界**位，而
        ///    `CardView.SetPose` 写的是 `transform.localPosition` ⇒ **两者在批处理里恰好相等**
        ///    （父链是单位变换 ⇒ 自检全绿），**实机里不等** ⇒ 牌被摆到别处
        ///    （症状：`-wfscene Battle` 起 player，「替换」钮留在换牌区、牌落到右下角那一带）。
        ///    ⇒ 凡是喂 `SetPose` 的都走这一条，凡是喂 `DOMove` 的都走 `RowWorldPos`。</summary>
        public Vector3 RowLocalPos(CardView c)
        {
            var w = RowWorldPos(c);
            var parent = c != null ? c.transform.parent : null;
            return parent != null ? parent.InverseTransformPoint(w) : w;
        }

        /// <summary>这张牌在这一批换牌区里吗（`BattleDriver` 判「发牌该落到哪一行」用）。</summary>
        public bool HasCard(CardView c) { return c != null && _cards.IndexOf(c) >= 0; }

        /// <summary>把换牌区这几张**再摆一遍**（幂等，可反复调）。
        ///
        /// 🔴 **2026-10-13：为什么需要它（实机才暴露的一条）** —— 手牌布局
        /// （`interaction.SetCards` → `HandLayout.Place`）**每一轮 `SyncHand`（= 每次 `RefreshAll`）
        /// 都会跑**，而它算的是**手牌位** ⇒ 换牌期会把这几张从换牌区**拉回屏幕底部那排**。
        /// 症状（`-wfscene Battle` 起 player 实测）：**「替换」钮留在换牌区、牌自己跑回去了**。
        /// ⚠️ **自检里结构上测不出来**：批处理 `animateRelayout = false`，而 `CommitDealtPoses`
        /// 只管**发牌那一帧** —— 断言过的是「那一帧」的位姿，管不到后面每一轮 `RefreshAll`。
        /// ⛔ 判据仍只有 <see cref="RowWorldPosAt"/> 一处（⛔ 别在这里重算一遍间距）。</summary>
        public void ReapplyRowPoses()
        {
            if (!Visible) return;
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c == null) continue;
                c.transform.DOKill();                                   // 掐掉手牌布局刚开的重排补间
                c.SetPose(RowLocalPos(c), 0f, MulliganCardScale);       // ⚠️ `SetPose` 吃**局部**位
                // 🔴 **2026-10-22（W62 · `D6`）**：**每一轮都重设队列** —— 刚才那一趟 `HandLayout.Place`
                //   把它按手牌槽位改回 2989..3000 了，不重设的话下一帧压暗层又盖回卡上（判据 → `QMulliganCard`）。
                c.SetQueue(QMulliganCard);
            }
        }

        /// <summary>换牌区那张卡的 `localScale`（= 卡宽 292 px）。⛔ 与 `BuildCardButtons` 同一份判据。</summary>
        public static float RowCardScale { get { return MulliganCardScale; } }

        public static MulliganPanel Create(Transform parent)
        {
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名件 `Mulligan` 实读：`RectTransform` · `anchor (0,0)-(1,1)` ·
            //    `sizeDelta (0,0)` ⇒ **绝对矩形 (0,0)-(1920,1080)**（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读）⇒ 整屏矩形（与文件头那段 dump「`Mulligan`（整屏，FrontCanvas 下）」一致）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「换牌面板根 = 整屏矩形」红。
            // 🔴🔴 **2026-10-16（`T09` 批 3）：这个节点【故意留着世界空间那一套】，它是「换牌区那一行」的
            //    换算基准**（见文件头那两条「有意不迁」）—— 本面板的**图与字一个都不挂它**，
            //    它们全挂在下面那块子画布的 `Mulligan` 帧上。⇒ `MenuDraw.SetPxSize` 这一句**原样不动**
            //    （`A218` 那条断言的期望值也就一个字不用改）。
            var go = new GameObject("MulliganPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            var p = go.AddComponent<MulliganPanel>();

            // ---- 🆕 `T09` 批 3：本面板那块 uGUI 子画布（`OverHand` / order 0）--------------------------
            // 层/order **照原版那一行**（`T09c` §一）；⛔ **子画布一个 `CanvasScaler` 都不加**
            // （原版全场景只有一个、在根上 ⇒ 加了会二次缩放）。
            // 🔑 `EnsureSubCanvas` **按名字幂等**（可以当「建/取」用）⇒ ⛔ **别在外面再包一层**
            //    （`生产侧 + A218 探针` 都会调 `Create`，包两层就是同一个规矩的第二份写法）。
            HudCanvas.Ensure(LayoutSpace.Cam);          // 幂等（HUD 图族会先建；这里兜底）
            var cvRt = HudCanvas.EnsureSubCanvas(null, SubCanvasName,
                                                 HudCanvas.SortingLayer.OverHand, 0);
            if (cvRt == null)
            {
                // ⛔ **不许静默退回世界空间 quad** —— 半迁移态（一半跟画布、一半跟世界）在 **16:9 下看不出来**，
                //    别的宽高比下两者会分家（`T09` §五·补⑦ 那条「只换半边比不换更坏」）。
                UnityEngine.Object.DestroyImmediate(go);
                Debug.LogError("[Battle] 换牌面板的 `OverHand` 子画布没建起来（根 canvas 没立？）"
                             + " ⇒ **本面板这次不建**（⛔ 不退回世界空间 quad）");
                return null;
            }
            p._cv = cvRt;
            // 画布上的**整屏帧** = 原版 `Mulligan`（`anchor (0,0)-(1,1)` + `sd (0,0)` ⇒ 铺满父件）
            //   ⇒ `FillParent`（⛔ 不是写死 1920×1080：`Expand` 档下屏比画布高时它更大，见 `T09` §五·补⑩）。
            //   **它的中心 = 屏心** ⇒ 面板内坐标 = `HudCanvas.PosPx(cx, cy)`（本文件所有几何常量都是
            //   1920×1080 那一档的设计 px、左上原点、y 向下 ⇒ 原样可搬）。
            var fgo = new GameObject("Mulligan", typeof(RectTransform));
            fgo.transform.SetParent(cvRt, false);
            p._frame = (RectTransform)fgo.transform;
            HudCanvas.FillParent(p._frame);
            HudCanvas.Add(p._frame, Z);                 // 兄弟序那本账（画布上 z 只用来排先后）
            var frame = p._frame;                       // 面板内所有件的父节点（⛔ 不是 `go.transform`）

            // 压暗（原版 `ShadePanel` / `Shade.SwitchShade`；透明度 = 原版 `onAlphaLevel`，见 `ShadeAlpha`）
            // 🔴🔴 **2026-10-24（第七十三会话 · 代理 `M1`）：这一件【改回世界空间 quad】。**
            //   **真机证据**（`_tmp_view/shots/2026-10-24_第七十三会话/11_after_match.png`，2560×1440）：
            //     换牌卡那一区（4 张全量）**max = 123**、p95 ≈ 100；背景 p95 ≈ 56–74。
            //     123 → 线性 **0.198 ≈ 1 − 0.8**（`ShadeAlpha`）⇒ **卡确实被压暗层盖着**，
            //     与 W62 `D6` 修之前**一模一样**（原版实拍同一区 max = 255）。
            //   **机制（W62 猜反的那一条）**：`renderQueue` 的优先级**不高于** `Canvas` 的排序层 ——
            //     换牌卡是**世界 `MeshRenderer`**（`Default` 层、队列 3001），压暗层是**画布件**
            //     （`OverHand` 排序层）⇒ **排序层先比，队列轮不到** ⇒ 卡永远在压暗层下面。
            //     `Battle/HudCanvas.cs:21` 早就记着同一条（「`sortingOrder` 会**先于** `renderQueue` 比较」）；
            //     W62 那句「抬到 3001 就压过画布」是**未经验证的推断**（`D6②`/`D6⑤` 又是拿这条推断
            //     反推亮度的 ⇒ **自证**，所以批处理一直绿、真机一直暗）。
            //   ⇒ **正解**：把压暗层与换牌卡放进**同一个渲染类别**（都是世界 `MeshRenderer`），
            //     让 `renderQueue` 真的能做判据：
            //       · 压暗层 = 世界 quad，队列 **`QMulliganShade`(3006)** —— 高于 13 场战场里**所有**几何
            //         （逐场数 manifest：>3000 的一共 18 件，只有 3001×3/3003×3/3004×8/3005×4，最大 **3005**，
            //         `battlearena1`/`battlearena2` 都有；9 场一件都没有）⇒ 战场照旧被压暗；
            //       · 换牌卡 = **`QMulliganCard`(3007)** —— 高于压暗层、且仍 < `CardFan.QCardBase`(3009)
            //         ⇒「点卡放大」那扇窗照样压在换牌卡之上；
            //       · 画布 HUD（提示行 / 换牌钮 / 继续 / 眼睛）= `OverHand` 排序层 ⇒ 仍压在所有世界件之上，
            //         不会被压暗，也不会被卡盖住。
            //   ⛔ **别再把它挪回画布**（哪怕换一个更低的排序层）：画布件只要排序层 > `Default` 就压世界件，
            //     而战场几何是 `Default` 层、队列 3000..3005 ⇒ 没有任何画布排序层能同时「压住战场」又
            //     「不压住卡」。⚠️ 这一句的判据 = 上面的真机像素 + `HudCanvas.cs:21`，**不是我们自己的常量**。
            var shadeQuad = ImageQuad.Create(go.transform, CardArt.Solid(), At(960f, 540f),
                                             LayoutSpace.DesignHeight, new Vector2(0.5f, 0.5f),
                                             "MulliganShade");
            p._shade = HudPiece.Of(shadeQuad);
            if (shadeQuad != null)
            {
                // 铺满**当前**可见区（原版那件是整屏 `Image`）：高 = 可见高、宽 = 可见宽（= 高 × 屏幕宽高比）
                shadeQuad.SetAspect(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight);
                shadeQuad.SetTint(new Color(0f, 0f, 0f, ShadeAlpha));
                shadeQuad.transform.localPosition += new Vector3(0f, 0f, Z);
                shadeQuad.SetRenderQueue(QMulliganShade);   // 压在战场之上、换牌卡之下
            }
            else Debug.LogError("[Battle] 换牌面板的压暗层没建起来（`CardArt.Solid()` 取不到？）");

            // 提示行（原版 `MulliganText` 那块 1344×79.4，中心 (967,106.5)）
            // 🔴 **2026-10-18（第十二轮 · W6）**：文案改成**原版词条** `Battle/Mulligan/Instructions`
            //   （TMP 原文 `Choose cards to replace in first hand`，见 `Core/Loc.cs`）。
            //   ⚠️ **原来那半句「（回车 = 完成）」已去掉** —— 原版文案里没有它；留一个只在中文档出现的
            //      括号半句就等于**又开一条写死路径**（本工程红线）。回车那把快捷键**照旧能用**
            //      （接线在 `BattleDriver` 的键处理里），只是不再印在提示行上。
            //   ⚠️ 字号按那块框的高度取 —— **原来是我们挑的**（保留原样，本轮不动版面）。
            // 🔴 **颜色那句 2026-10-13 就地订正（铁律 5 · `A1752`）：下面这半段【已被推翻】，保留更正痕。**
            //   原文写：「`MonoBehaviour_3731/3856` 的 `m_fontColor32` 都是纯白 ⇒ 两行都改成纯白」——
            //   **`m_fontColor32` 那一半没读错，结论错了**：顶点色是**乘在材质 `_FaceColor` 上**的，
            //   而两行挂的**不是同一份材质** ⇒ 提示行实际是**橙色**。
            //   逐条判据（材质名 / `_FaceColor` / 实拍）写在下面那一行上方，⛔ 别照本段改回去。
            //   （原文另一句「旁证：`战斗界面JSON权威表_0827.md:274-275` 记「白」」—— 那份表记的也是 `m_fontColor32`，
            //    同源错误，一并作废。）
            string promptText = Loc.T(PromptTerm);
            // 🔴 **2026-10-13（`A1752`）就地订正（铁律 5）：提示行是【橙色】，不是白。**
            //   上一轮只读了 `m_fontColor32`（`MonoBehaviour_3731.json` = `4294967295` = 白）就写成「两行都用纯白」
            //   —— **漏了【材质】**：原版这两行挂的**不是同一份 TMP 材质**（`m_sharedMaterial` 两个不同 PathID）：
            //   · 提示行 → **`Pragati-Regular Orange`**（`bundle_fonts_assets_all/Material/Material_-5772249770677823286.json`）
            //     `_FaceColor = (0.9960785, 0.5960785, 0.1176471, 1)` = **#FE981E**（材质名自己就叫 `Orange`）；
            //   · 第二行 → `Pragati-Regular Atlas Material`（`…/Material_-5050103597772954521.json`）`_FaceColor` = 白。
            //   `m_fontColor32`（顶点色，两行都是白）**是乘在材质 `_FaceColor` 上的** ⇒ 单看它一档推不出屏上颜色。
            //   旁证（两条独立）：① 用户实拍 `战斗—替换手卡参考图（先手/后手）.png` 两行**明显一橙一白**；
            //   ② `Battle/Label.cs` 的 `SetColor`/`TmpFont` 那一路：我们自己的字体材质 `_FaceColor` 是白
            //   ⇒ 这里直接给顶点色就能画出原版那个橙。
            Color promptColor = new Color(0.9960785f, 0.5960785f, 0.1176471f, 1f);   // = 原版 `Pragati-Regular Orange` 的 `_FaceColor`
            // 🔴 **2026-10-16（`T09` 批 3）**：`Label.Create`（世界空间）→ **`HudCanvas.MakeText`（画布）**。
            //   ⚠️ **单位是【设计 px】**（`At()` → `Px()`）：喂世界单位会**小 108 倍**、
            //      **而画面只是「小得看不见」、不报错**（`T09` §五·补⑨ 四）。`z` 那个形参就是旧那句
            //      `localPosition += (0,0,Z−0.05)` 的落点（画布上 z 只用来排兄弟序）。
            //   ⚠️ `MakeText` 拿不到 TMP 字体时**返回 null**（`Label.CreateUgui` 出声、⛔ 不退化成点阵）
            //      ⇒ 下面几处都保留 `!= null` 空判（原来 `Label.Create` 恒非 null，没这个必要）。
            var promptPos = Px(PromptCx, PromptCy);
            p._prompt = HudCanvas.MakeText(frame, promptText, promptPos.x, promptPos.y, 8,
                                           promptColor, new Vector2(0.5f, 0.5f), Z - 0.05f, "MulliganPrompt",
                                           face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-16（`A2102④`）：与 `TurnText` 那颗**同一口径** —— 开原版那条自适应。**
            //   原来这里只调 `SetScriptFont(…, PromptH * 0.55f)`（**那个 0.55 是我们挑的**）
            //   ⇒ 实渲 43.67，而原版**实渲 53.7**（小 19%）。
            //   ⚠️ 按语种选字体那句**保留**（`SetScriptFont`）—— 它现在只提供 `fontSizeBase` 那个起点；
            //      最终大小由 TMP 在 `[18, 65]` 里自己收敛（原版也是这么跑的，见 `PromptBoxW` 那条判据）。
            SetScriptFont(p._prompt, promptText, PromptFsBase);
            //   🔴 **2026-10-16（`T09` 批 3）**：框那两个参数**去掉 `U()`** —— uGUI 档的框就是**设计 px**
            //     （`Label.SetAutoFitBox` 内部拿它写 TMP 的 `sizeDelta`、并用 `NominalPx()`（按档取）折算字号
            //      ⇒ 喂世界单位会让框变成 1/108、字号收敛到一个荒谬的小值，**不报错**）。
            if (p._prompt != null)
                p._prompt.SetAutoFitBox(PromptBoxW, PromptBoxH, PromptFsMin, PromptFsMax, PromptFsBase);

            // 🆕 2026-09-26：**开局谁先手那一行** —— 判据（唯一）→ `资料/加时与冲突模式_原版规格.md` §2.8：
            //   原版在 `MulliganManager.ActivateMulligan` 里把这一行的词条**按先手/后手二选一**
            //   （`playerGoesFirst != 0` ⇒ `Battle/Tips/GoFirst`，否则 `Battle/Mulligan/secondTurn`；
            //    后者在场景资产里的英文兜底是 **"You go second"**，实测 `bundle_scenes_scenes_battlearena1`）。
            //   🔴 **它是原版唯一一处「先手/后手」的表现** —— 原版**没有硬币资产 / 动画 / 音效**
            //   （2026-09-26 全量查过：`coin`/`toss`/`dice` 在 91 个 bundle 的资产名与 MonoBehaviour 内容里
            //    都只命中商城的 "Add coins"）⇒ 我们的「投硬币」**只该在这一行上露出来**。
            // ⚠️ 文案：后手那句照原版英文兜底译；**先手那句只有词条名 `GoFirst`（英文原文没查到）⇒ 我们译的**。
            // 🔴 **2026-10-16（`T09` 批 3）**：同提示行那一处 —— `Label.Create` → `HudCanvas.MakeText`（**设计 px**）。
            var turnPos = Px(TurnCx, TurnCy);
            p._turnText = HudCanvas.MakeText(frame, "", turnPos.x, turnPos.y, 7,
                                             Color.white, new Vector2(0.5f, 0.5f), Z - 0.05f, "MulliganTurnText", face: TmpFont.Face.LatinCardBody);
            // ⚠️ 出厂这一下只是给个初始字号；**真正的字号按语种在 `SetTurnText` 里再设一遍**
            //    （那一行是**唯一**的口径：中文 `SetGlyphHeight` / 英文 `SetCapHeight(0.72em)`）。
            // 🔴🔴 **2026-10-16（`A1104`）就地订正 —— 本行原来只写 `SetGlyphHeight(U(TurnPx))`（= 字号写死 55），
            //   那句【不成立】：原版那一颗 TMP 是 `m_enableAutoSizing = 1`、`m_fontSizeMin/Max = 18/55`**
            //   （`MonoBehaviour_3856.json`）—— 运行期**实测渲染 41.8**，因为**框高只有 54.168**
            //   （55 px 的行高装不下 ⇒ 是**被框压下来的**，不是原版就写 55）。
            //   我们写死 55 ⇒ **比原版实渲大 ≈32%**（同一棵树：提示行原版 53.7、我们 43.67，那一颗反过来）。
            //   ✅ **判据（`A1104` 的结论）明确**：要复刻的是「**autoSizing + 那两个上下限**」，
            //      ⛔ **不是把 41.8 抄进来** —— auto 的产物**随语种/字体资产变**，钉一个数只对一种情况成立
            //      （这正是上一轮查出后**没顺手改**的正确理由）。
            //   框 = 原版 `TurnText` 的绝对矩形 **1307.064 × 54.168**（= 本类 `TurnW`/`TurnH` 那两个常数；
            //   判据 = `RectTransform_3403.json` 的 `sd (-36.936, -25.272)` + 父整屏 1920×1080，
            //   **13 场逐字段相同 + 运行期 dump 逐位相同**；全文 → `资料/普查产出_第六会话/W_换牌那行rect_A1076.md`）。
            //   ⚠️ 下面那句按语种的手工字号**保留**：它现在只提供**标称**（`cur`），自适应四格按它折算；
            //      开了自适应之后最终大小由 TMP 在 `[18,55]` 里自己收敛（原版也是这么跑的）。
            //   🔴 **2026-10-21（第六十一会话 · 换牌残余 ④）**：`base` 实参从 `TurnPx`(55) 改成
            //      **原版字段 36**（`TurnFsBase`）—— 同一次现读的 `MonoBehaviour_3856.json` 里
            //      `m_fontSizeBase = 36`。`m_fontSize = 55` 那一档仍由 `SetScriptFont(…, TurnPx)` 提供。
            if (p._turnText != null)
                p._turnText.SetAutoFitBox(TurnW, TurnH, 18f, 55f, TurnFsBase);   // 🔴 框 = **设计 px**（去掉 `U()`，同 `_prompt`）
                                                                                 // 🔴 base = **原版 `m_fontSizeBase` = 36**（见 `TurnFsBase`；原来喂的是 `TurnPx` 55）

            // 完成按钮：底图 `40k_bt_underbutton`（原版 577.5×63.8）+ 圆形播放钮 `40k_UI_bt_play`
            // 🔴 **2026-10-16（`T09` 批 3）**：三颗都换成画布上的 `HudPiece`（`Img` = 面板内**设计 px**、
            //   按**高**定、宽由贴图比例来 —— 与旧 `ImageQuad.Create` 逐字同义）。
            p._bar = Img(frame, CardArt.Ui("40k_bt_underbutton"), "MulliganContinueBar", BarCx, BarCy, BarH, Z,
                         // 🔴 2026-10-21：原版 `Button.m_Color = (0.3686,0.8941,0.5874,1)`（绿）
                         //    判据 = `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_5292.json`；
                         //    原来 `Img` 写死白 ⇒ 真机是灰色的。
                         new Color(0.3686f, 0.8941f, 0.5874f, 1f));
            // 🔴 **2026-09-27 修（PA 普查抓的）**：原版这条底 `m_PreserveAspect = 0`、`m_Type=0`（Simple）
            //    ⇒ **拉满 577.5×63.84**；`ImageQuad` 默认按贴图比例定宽，而 `40k_bt_underbutton` 是
            //    **485×83**（5.8434）⇒ 我们只画出 **372.8×63.84**、**窄 204.7 px**。
            //    实据：`battlearena1` 的 `Mulligan/ButtonsGroup/MulliganContinueButton/Button`
            //    （RT 2659 / GO 121 / MB 5292，直读 `m_PreserveAspect=0`、`m_Type=0`）。
            //    ⚠️ 同图同尺寸的 `CardChoicePanel` 早就有这一行（`_bar.SetAspect(BarW / BarH)`）——
            //    这一处是漏了，不是设计。
            if (p._bar != null) p._bar.SetAspect(BarW / BarH);
            p._play = Img(frame, CardArt.Ui("40k_UI_bt_play"), "MulliganContinueCircle", PlayCx, PlayCy, PlayH, Z);
            // ⚠️ 字号在 `SetDoneText` 里按**当前这一串**的语种设（原版那颗钮上会短暂变成倒计时秒数）。
            var donePos = Px(DoneCx, DoneCy);
            p._doneText = HudCanvas.MakeText(frame, DoneLabel, donePos.x, donePos.y, 6,
                                             new Color(1f, 0.92f, 0.75f), new Vector2(0.5f, 0.5f),
                                             Z - 0.05f, "MulliganDoneText", face: TmpFont.Face.LatinCardBody);
            p.SetDoneText(DoneLabel);
            // 🔴 **2026-10-16（`A2102④`）：与上面那两颗**同一口径** —— 按原版开自适应。**
            //   框与三档 → `DoneBoxW/H` / `DoneFsMin/Max/Base` 那条 doc（判据 = 现读那颗 TMP 的字段）。
            //   ⚠️ **放在 `SetDoneText` 之后**：那一句会按语种重设字号（它只提供起点），
            //     框一旦设上，最终大小由 TMP 在 `[18,45]` 里自己收敛。
            if (p._doneText != null)
                p._doneText.SetAutoFitBox(DoneBoxW, DoneBoxH, DoneFsMin, DoneFsMax, DoneFsBase);   // 🔴 框 = 设计 px

            // 眼睛（原版 `HideMulliganButton`，图 `40k_ui_bt_eye`）—— 暂时收起卡片上的按钮
            p._eye = Img(frame, CardArt.Ui("40k_UI_bt_eye"), "MulliganHideButton", EyeCx, EyeCy, EyeH, Z);

            // ⚠️ **2026-10-16（`T09` 批 3）：原来这里那五句 `localPosition += (0,0,Z/Z−0.05)` 全删了** ——
            //    画布上写 `localPosition.z` 会把件**推离画布平面**（z 在那里只用来排兄弟序）⇒
            //    那个层深度现在由上面每处的建件口 `z` 形参承担（`HudCanvas.Add` 按 z 降序排兄弟序，
            //    语义与旧那套**同向**：z 大的在前、画在底下；同 z 按建件先后 ⇒ 两颗字压在自己的底图上 ✓）。

            p.SetVisible(false);
            return p;
        }

        // ==================================================================
        //  开 / 关
        // ==================================================================

        /// <summary>开面板：把「换」按钮贴到每张起手牌上</summary>
        public void Open(IReadOnlyList<CardView> hand)
        {
            _marked.Clear();
            BuildCardButtons(hand);
            SetVisible(true);
        }

        public void Close()
        {
            ClearCardButtons();
            SetVisible(false);
        }

        void SetVisible(bool v)
        {
            Visible = v;
            if (_shade != null) _shade.gameObject.SetActive(v);
            if (_bar != null) _bar.gameObject.SetActive(v);
            if (_play != null) _play.gameObject.SetActive(v);
            if (_eye != null) _eye.gameObject.SetActive(v);
            if (_prompt != null) _prompt.gameObject.SetActive(v);
            if (_doneText != null) _doneText.gameObject.SetActive(v);
            if (_turnText != null) _turnText.gameObject.SetActive(v);
            // 🔴 **2026-10-13（`A1751`）**：**激活那一刻把那一行重写一遍** ——
            //    `SetTurnText` 的调用点排在 `Open()` 之前，那一下是在**面板关着**的时候写的：
            //    未激活时 `ForceMeshUpdate()` 是空操作、`textBounds` 是哨兵 ⇒ 量不准。
            //    这里紧跟着 `SetActive(true)` 再走一次，量得准、摆得对（幂等，多走几次无副作用）。
            if (v) ApplyTurnText();
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null) _cardBtns[i].gameObject.SetActive(v);
            for (int i = 0; i < _cardTexts.Count; i++)
                if (_cardTexts[i] != null) _cardTexts[i].gameObject.SetActive(v);
        }

        /// <summary>
        /// 把起手牌**搬进换牌区那一行**，并在每张牌下面贴一颗「替换」钮。
        ///
        /// 🔴 **2026-10-13 重做**（用户点名「开局的手卡替换你改错了，要求是原版」并给了实拍）：
        ///   原来这里**只贴钮、不动牌** —— 牌留在屏幕底部那排小手牌上，钮贴在手牌下缘外侧。
        ///   原版不是那样：`PlayerHand.MoveCardToMulliganPos.c` 把牌**逐张搬进 `MulliganAnchor` 那一行**
        ///   （位 = 它身上那个 `CardsHorizontalLayout.GetPosition(i,n)`），换完
        ///   `CompleteMulliganPhase` 再把**同一批对象**并进真手牌列表 ⇒ 换牌期牌**不在手牌位**。
        /// 几何判据全部写在 `RowCy` / `CardWidthPx` / `RowSpacingPx` / `Btn*Factor` 那几个常量的 doc 里
        /// （⛔ 别在这里另抄一份数）。
        /// </summary>
        void BuildCardButtons(IReadOnlyList<CardView> hand)
        {
            ClearCardButtons();                 // ⚠️ 它内含「把上一批的位姿复位」
            _cards.Clear();
            if (hand == null) return;
            for (int i = 0; i < hand.Count; i++) if (hand[i] != null) _cards.Add(hand[i]);
            int n = _cards.Count;
            if (n == 0) return;

            // ---- ① 起手牌搬进换牌区那一行 ----
            // 🔴 **2026-10-21（第六十一会话 · 换牌残余 ②）就地改写法（铁律 5）**：
            //   原来这里是「存世界位 + 直接写 `transform.localScale` + 直接写 `transform.position`」。
            //   两个问题，第二个是**真缺陷**：
            //   ① `transform.localScale` 是**卡体那一层**的缩放，而 `CardView` 的缩放是
            //      「基础缩放 × 高亮系数」（`CardView.ApplyScale`）—— 直接写它**不会更新 `_baseScale`**
            //      ⇒ `_baseScale` 仍停在**手牌尺度**（0.7301 = 165 px）。之后任何一次
            //      `ApplyScale()`（`SetHighlight`/`SetHighlightScale` 那一族会调）都会把这张卡
            //      **弹回手牌尺度**——静默、只在实机上看得见（批处理不开这些补间）。
            //   ② 写 `transform.position`（世界位）本身是对的（卡在 `HandArea` 那条父链下），
            //      但同族的 `ReapplyRowPoses` 早就走 `SetPose(局部位, 0, 卡宽尺度)`；两条写法并存 =
            //      同一条规则的第二次实现（本仓红线）。⇒ 现在统一成 `SetPose` 一条路。
            //   ⚠️ `RowLocalPos` **必须**在 `_cards` 填好之后调（它按 `_cards.IndexOf` 算槽位）——
            //      上面那一段已经 `_cards.Add` 完，顺序不能挪。
            float cardScale = MulliganCardScale;
            for (int i = 0; i < n; i++)
            {
                var c = _cards[i];
                _savedPos.Add(c.transform.localPosition);          // 父空间（`HandArea`）
                _savedRotZ.Add(c.transform.localEulerAngles.z);
                _savedScale.Add(c.transform.localScale);
                // 🔴 **2026-10-22（W62 · `D6`）**：记下**搬进来之前**的队列，`ClearCardButtons` 还回去。
                _savedQueue.Add(c.Queue);
                // 🔴 **2026-10-22（W63 · 换牌那排卡是斜的）**：**先掐掉这张卡身上还活着的手牌重排补间**，
                //   再写 `SetPose(…, 0f, …)`。为什么：`SyncHand` → `CardInteraction.Relayout` →
                //   `HandLayout.Place` 在实机上（`animateRelayout == 1`）开的是一条 **0.2 s 的
                //   `CardTween.ToPose`（位置 + **旋转** + 尺度）**，它会把牌慢慢转到**手牌扇形角**；
                //   本方法这一句只写**当时**的局部旋转、**不会**让那条补间停下 ⇒ 0.2 s 后补间的
                //   旋转尾巴最后落盘，换牌区那排牌就**位置对、角度斜**（用户真机报的正是它）。
                //   同族同形：`ReapplyRowPoses`（本文件 `:681`）早就是「先 `DOKill` 再 `SetPose`」。
                //   ⛔ 别改成 `DOKill(true)`：那会让补间**跳到终点**（扇形角），正好是要躲的那一档。
                c.transform.DOKill();
                c.SetPose(RowLocalPos(c), 0f, cardScale);          // 🔴 判据只此一处（`RowWorldPosAt` 的局部位版）
                // 🔴 **2026-10-22（W62 · `D6`）**：把这张卡的**整段队列**抬到压暗层之上（判据 → `QMulliganCard`）。
                //   ⚠️ 必须在 `SetPose` 之后、且**每一轮重建都要重设** —— `SyncHand`（`RefreshAll`）会走
                //   `HandLayout.Place` 把它们按手牌槽位改回 2989..3000。
                c.SetQueue(QMulliganCard);
            }

            // ---- ② 每张牌下面那颗「替换」钮 ----
            float btnW = CardWidthPx * BtnWFactor;                       // 原版 `sd.x = 1.30` 框单位
            float btnH = CardWidthPx * BtnHFactor;                       // 原版 `sd.y = 0.33` 框单位
            float halfCardPx = CardView.Height * cardScale * 108f * 0.5f; // 卡的世界半高（设计 px）
            for (int i = 0; i < n; i++)
            {
                // ⚠️ x/y 拿**卡的世界坐标当局部坐标**（「面板与棋盘同在世界空间」的老口径，今天等价）；
                // 🔴 **A276（2026-10-11）：z 改成裸局部 `Z`** —— 原来写的是
                //    `c.transform.position + (0, −h, Z − c.transform.position.z)`，那个 `− c…z` 项
                //    **自相消**（两个 `c.transform.position.z` 恰好抵消、恒等于 `Z`）⇒ 今天**逐位相同**，
                //    但没有表达出「这一层是面板的局部 z」这个意思，而且形状上很容易被后人「顺手化简」成
                //    `c.transform.position`（那样按钮的 z 就会跟卡走）。现在与同父的 `_shade`
                //    （`+= Z`）· `_prompt/_doneText/_turnText`（`+= Z − 0.05`，下一段）**同一套口径**。
                //    ⚠️ 这是**收口径、不是「对齐原版」** —— 这些层的**原版 z 关系没有判据**
                //    （`Z` 这个常量本身是我们挑的，见 `资料/普查产出_1010/V4b_三件口径.md` §Q2）。
                //    🔴 **2026-10-22（W62 · `D6`）补注**：上面那句「与同父的 `_shade`」**只对「用的是同一个
                //    `Z` 常量」成立，父件不同** —— `_shade` 是**世界空间**的 `ImageQuad`（`localPosition.z = Z`），
                //    而钮在**画布**上（z 只是兄弟序）。两者的先后**由渲染队列定**
                //    （`QMulliganCard` 3007 vs 压暗层 `QMulliganShade` 3006 —— 两者**都在世界空间**，
                //    见那两条 const 的 doc），⛔ 别再拿 z 推它们的关系。
                //    ⚠️ **2026-10-24（第七十三会话 · `M1`）**：上一版的「世界空间」那句当时**其实不成立**
                //    （那天的成品件是画布 `HudPiece`，本段却写成世界 —— 注释和代码打架）；现在代码真的改成
                //    世界 quad 了，这句才对得上。错因 = 那天先写了注释、实现走了画布支。
                //    改坏法：把 z 写成 `c.transform.position.z`（或整个 `c.transform.position`）⇒
                //    `Editor/BattleScene.cs` 的 A276 那条断言红（它把卡的世界 z 挪 0.4 再重建按钮）。
                // ⚠️ 钮是**本面板的子件** ⇒ 位置用**本面板的局部位**（`RowLocalAt`，判据只此一处）。
                // 🔴 **2026-10-13（实机踩到）**：原来这里拿 `c.transform.position`（**世界**坐标）当
                //    钮的**局部**坐标用，注释写着「面板与棋盘同在世界空间，今天等价」——**实机里不等**
                //    （卡在 `HandArea` 那条父链下，两者差一个缩放）⇒ 钮会整体偏离那一行。
                //    批处理里恰好相等 ⇒ 自检结构上测不出来。
                // 🔴🔴 **2026-10-16（`T09` 批 3）：钮整族搬到画布上 ⇒ x/y 改成【面板内设计 px】。**
                //    判据只此一处：横向偏移 = `RowDX(i,n)`（**与卡那条同一份常量**），纵向 = 行心
                //    `RowCy` 往下 `halfCardPx + BtnBelowCardPx`；
                //    ⛔ **别再用 `RowLocalAt`/`U()`**（那是**世界空间那一帧**的数 —— 但卡还在那一帧里，
                //    所以卡那边的 `RowWorldPosAt` **一个字没动**，见文件头那两条「有意不迁」）。
                //    ⚠️ 非 16:9 下「画布上的钮」与「世界空间的卡」本来就会错开一点（两个帧），
                //    那是已知代价、由第 5 步（迁卡）消掉，见文件头。
                float bx = RowCx + RowDX(i, n);                 // 设计 px（画布帧）
                float by = RowCy + halfCardPx + BtnBelowCardPx; // 设计 px，行心往下

                var q = Img(_frame, CardArt.Ui("UI_Button_Mulligan"), "MulliganBtn_" + i, bx, by, btnH, Z);
                // 🔴 **2026-10-12（`A1663`）**：这句原来是「没图就只靠『点卡』那条路，不静默失败」——
                //    **那个兜底已经不成立**（点卡那条路是**我们加的**、原版没有它，已删）⇒ 改成**出声**。
                //    ⛔ 不许退回「点卡也能标记」：那会把原版「点卡 = 看牌」整段盖掉（就是本次要修的偏离）。
                if (q == null)
                {
                    Debug.LogWarning($"[Battle] 换牌面板：第 {i} 张牌那颗「替换」钮没取到图 `UI_Button_Mulligan`"
                                   + " ⇒ **这一张标记不了、钮上的字也画不出来**"
                                   + "（那张图应该在 `Resources/` 里；跑一次素材导入。⛔ 不退回「点卡标记」那条路）");
                    continue;
                }
                // ⚠️ **原版那颗钮是【拉伸】的**（它 `Image` 的子节点把 rect 拉满 ⇒ 画出来的 = 框）：
                //    `sd = 1.30 × 0.33` = 比例 **3.94**，而这张图本身是 `410×124` = **3.31**
                //    ⇒ 照框画就是**横向拉宽**。⛔ 别退回「按图的原比例」。
                q.SetAspect(btnW / btnH);
                _cardBtns.Add(q);

                // 🔴 **2026-10-18（第十二轮 · W6）**：那颗钮上的字走**原版词条**
                //    `Battle/Mulligan/Replace`（TMP 原文 `Replace`，全库只 1 颗 —— `battleprefabs_vfxandmisc`
                //    的 `ReplaceText`；判据全文 → `Core/Loc.cs`）。原来那个写死的 `"换"` 已删。
                //    ⚠️ 中文那一列 2026-10-13 按**用户实拍**订正成「**替换**」（原来是我们自拟的「换」）。
                // 🔴 **2026-10-18（第十三轮 · G2b）**：**同一颗钮有两档字** —— 被标记的牌印
                //    `Battle/Mulligan/Undo`（判据见 `UndoTerm` 的 doc）。原来我们**只有一档**，
                //    标记之后那张牌上还写着「换」（= 原版那一刻印的是 Undo）。
                // 🔴 **2026-10-16（`T09` 批 3）**：`Label.Create` → `HudCanvas.MakeText`（**设计 px**、
                //    `z` 形参承担旧那句 `localPosition += (0,0,Z−0.05)`；画布上写 `localPosition.z` 会推离平面）。
                string btnText = CardBtnWord(_marked.Contains(i));
                // 🔴 **2026-10-21（第六十会话 · 真机缺陷：四颗钮全空）**：`bx/by` 是**面板内设计 px**
                //    （左上原点），而 `MakeText` 吃的是**父件局部坐标** —— 直接喂绝对 px ⇒ 四颗字
                //    落到屏幕上方外（`(+339,+782)` 而不是 `(-621,-242)`），钮只剩空框。
                //    同文件其它字都先过 `Px()`；这里补上。
                var btnLp = Px(bx, by);
                var t = HudCanvas.MakeText(_frame, btnText, btnLp.x, btnLp.y, 5, new Color(1f, 0.9f, 0.7f),
                                           new Vector2(0.5f, 0.5f), Z - 0.05f, "MulliganBtnText_" + i, face: TmpFont.Face.LatinCardBody);
                if (t != null)
                    SetScriptFont(t, btnText, 37f);           // 原版 `ReplaceText` 0.30 卡单位 ≈ 37.5 px（第六十会话现核）
                _cardTexts.Add(t);
            }
            BuildFeedback();
        }

        /// <summary>🆕 2026-10-19（B4 · `N70` P0-2）：建/重建按钮反馈（跟着 `_cardBtns` 一起重建）。</summary>
        void BuildFeedback()
        {
            _fxBar = BattleButtonFx.Tint(_bar, "Continue Button");
            var eh = CardArt.MenuUi("40k_UI_bt_eye_hover");
            var ep = CardArt.MenuUi("40k_UI_bt_eye_pressed");
            _fxEye = (eh != null && ep != null)
                   ? BattleButtonFx.Swap(_eye, "HideMulliganButton", _eye != null ? _eye.Texture as Texture2D : null, eh, ep)
                   : BattleButtonFx.Tint(_eye, "HideMulliganButton(tint-fallback)");
            _fxCardBtns.Clear();
            var mh = CardArt.Ui("UI_Button_Mulligan_hover");
            var mp = CardArt.Ui("UI_Button_Mulligan_Pressed");
            for (int i = 0; i < _cardBtns.Count; i++)
            {
                var q = _cardBtns[i];
                var fx = (mh != null && mp != null)
                       ? BattleButtonFx.Swap(q, "MulliganCardBtn_" + i,
                                             _marked.Contains(i) ? mp : CardArt.Ui("UI_Button_Mulligan"), mh, mp)
                       : BattleButtonFx.Tint(q, "MulliganCardBtn_" + i + "(tint-fallback)");
                _fxCardBtns.Add(fx);
            }
        }

        /// <summary>每帧喂状态（驱动层 `HandleMulligan` 里调）。</summary>
        public void TickFeedback(Vector3 world, bool held)
        {
            if (!Visible) { ResetFeedback(); return; }
            if (_fxBar == null) BuildFeedback();
            bool overDone = HitDone(world);
            FeedFx(_fxBar, overDone, held);
            FeedFx(_fxEye, HitEye(world), held);
            for (int i = 0; i < _fxCardBtns.Count && i < _cardBtns.Count; i++)
                FeedFx(_fxCardBtns[i], _cardBtns[i] != null && _cardBtns[i].Contains(world), held);
        }
        void ResetFeedback()
        {
            FeedFx(_fxBar, false, false); FeedFx(_fxEye, false, false);
            for (int i = 0; i < _fxCardBtns.Count; i++) FeedFx(_fxCardBtns[i], false, false);
        }
        static void FeedFx(BattleButtonFx fx, bool over, bool held)
        {
            if (fx == null) return;
            fx.SetHover(over); fx.SetPressed(over && held); fx.Tick();
        }
        /// <summary>自检口：直接喂一颗钮（`"done"` / `"eye"` / `"card0"` …）。</summary>
        public void SimulateFeedback(string node, bool hover, bool pressed)
        {
            if (_fxBar == null) BuildFeedback();
            BattleButtonFx fx = node == "done" ? _fxBar : node == "eye" ? _fxEye
                             : node.StartsWith("card") && _fxCardBtns.Count > 0
                               ? _fxCardBtns[Mathf.Clamp(int.Parse(node.Substring(4)), 0, _fxCardBtns.Count - 1)]
                               : null;
            if (fx == null) return;
            fx.SetHover(hover); fx.SetPressed(pressed);
        }
        public Color FeedbackKeyForTest(string node)
        {
            if (_fxBar == null) BuildFeedback();
            BattleButtonFx fx = node == "done" ? _fxBar : node == "eye" ? _fxEye : null;
            return fx != null ? fx.KeyForTest : Color.clear;
        }
        public string FeedbackTextureForTest(string node)
        {
            if (_fxBar == null) BuildFeedback();
            BattleButtonFx fx = node == "eye" ? _fxEye
                             : node != null && node.StartsWith("card") && _fxCardBtns.Count > 0
                               ? _fxCardBtns[Mathf.Clamp(int.Parse(node.Substring(4)), 0, _fxCardBtns.Count - 1)]
                               : null;
            return fx != null ? fx.TextureNameForTest : null;
        }

        void ClearCardButtons()
        {
            for (int i = 0; i < _cardBtns.Count; i++) if (_cardBtns[i] != null) Kill(_cardBtns[i].gameObject);
            for (int i = 0; i < _cardTexts.Count; i++) if (_cardTexts[i] != null) Kill(_cardTexts[i].gameObject);
            _cardBtns.Clear();
            _cardTexts.Clear();
            _fxCardBtns.Clear();

            // 🔴 **2026-10-13**：把起手牌**放回搬进来之前**的位姿（`Open` 存、这里还）。
            //   ⛔ **别依赖换牌结束后的 `RefreshAll()` 替我们复位** —— 它重摆的是 `_dealt`
            //   （这一轮新进手牌）那一份，**不保证覆盖到这几张**；不复位它们就会**留在屏幕中央**。
            // 🔴 **2026-10-21（第六十一会话 · 换牌残余 ②）**：归还**必须走 `CardView.SetPose`**，不能像原来那样
            //   直接写 `transform.position` / `transform.localScale` —— 理由与 `BuildCardButtons` 那段同一条：
            //   `_baseScale` 只有 `SetPose` 会写。直接写的话它会**停在换牌区那一档（1.292）**，而
            //   `HandLayout.Place` 在实机上走的是 `animateRelayout` 补间（只写 `transform.localScale`、
            //   **不写 `_baseScale`**）⇒ 之后玩家一悬停（`SetHighlight(Hover)` → `ApplyScale`）那张牌会
            //   **弹到换牌区尺度 × 1.05**（比手牌大 77%）。批处理里 `animateRelayout=false` ⇒ 结构上测不出来。
            for (int i = 0; i < _cards.Count && i < _savedPos.Count; i++)
            {
                if (_cards[i] == null) continue;
                _cards[i].SetPose(_savedPos[i], i < _savedRotZ.Count ? _savedRotZ[i] : 0f,
                                  i < _savedScale.Count ? _savedScale[i].x : 1f);
                // 🔴 **2026-10-22（W62 · `D6`）**：队列还回搬进来之前那一档（见 `QMulliganCard`）。
                //   ⚠️ 下一轮 `SyncHand` → `HandLayout.Place` 会再按手牌槽位重算一次（那才是正主），
                //   这里只是让「面板关掉到下一次 Refresh 之间」不留在 3007 那一档。
                if (i < _savedQueue.Count) _cards[i].SetQueue(_savedQueue[i]);
            }
            _savedPos.Clear();
            _savedRotZ.Clear();
            _savedScale.Clear();
            _savedQueue.Clear();
            _cards.Clear();
        }

        static void Kill(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        // ==================================================================
        //  交互（和设置面板/日志面板同一条路：批处理下由驱动层喂指针）
        // ==================================================================

        /// <summary>指针在哪张牌的「换」按钮上（-1 = 没点中）</summary>
        public int ButtonIndexAt(Vector3 world)
        {
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null && _cardBtns[i].Contains(world)) return i;
            return -1;
        }

        /// <summary>指针下的**那张手牌**（没有 ⇒ null）。
        /// 🔴 **「点到了哪张卡」的判据只此一处** —— 原版是每张卡自己的 `Collider2D`（换牌期开着），
        ///   我们这边转发到 <see cref="CardInteraction.Frontmost"/>（**删掉的那条标记分支原来就是这段扫描**，
        ///   本轮只换了命中之后**做什么**：原来 `ToggleMark`，现在回调 `OnCardSeen`）。
        /// 🔴🔴 **2026-10-19（战斗线 T10 · `A1668`）就地收口**：原来这里按下标递增取第一张
        ///   （doc 写着「重叠处取下标最小」），而手牌那条 `CardInteraction.HitTest` 取 **z 最小（最前）**
        ///   ⇒ **两条规则在扇形重叠区会选到不同的卡**。现在两处都调
        ///   <see cref="CardInteraction.Frontmost"/> —— **判据只此一份**。
        /// <para>判据 = 原版 `BattleManager__GetTargetInPos.c:104-130`：`Physics2D.GetRayIntersectionNonAlloc`
        /// 之后**只取数组第 [0] 项、不排序**（「谁在最上面」由物理层定）。</para>
        /// <para>🔴🔴 **2026-10-19（`T09` 步骤 B · `W57` · B2a）命中口也换成真 collider 了**：
        /// `Frontmost` 现在逐张问 <see cref="CardView.HitByCollider"/>（原版 `2DCard` 那颗
        /// `BoxCollider2D`）＋ 查询前 `Physics2D.SyncTransforms()`，**不再手算矩形**
        /// （旧实现 `CardView.Contains` 只留作一致性断言的对照物）。
        /// ⚠️ 本方法**继续只转发、不另写一份扫描** —— 两份扫描一旦分叉，`A1668` 那条
        /// 「换牌区与手牌走同一条判据」的灭自证断言就失去意义（⛔ 别把循环抄进来）。</para>
        /// <para>⚠️ **换牌那一行本来就不重叠**（`m_useRotation = 0`、平直横排、间距 &gt; 卡宽）⇒
        /// 收成一条判据之后，换牌期的行为**零变化**（z 相同 ⇒ 仍取扫到的第一张 = 原来的下标序结果）。</para></summary>
        public CardView CardAt(Vector3 world)
        {
            return CardInteraction.Frontmost(_cards, world);
        }

        /// <summary>指针在「完成换牌」上吗。
        /// <para>🔴 **2026-10-19（A1301③）就地更正**：上一版是「`_bar` / `_play` 的 `Contains` ＋
        /// 以 `DoneWorldPos` 为心、半径 `DoneW/2`(184.45px) 的那个圆」，三样都**不是原版的判据**：
        ///  · `_bar`/`_play` 走 `ImageQuad.Contains` = 量**画出来多大**（原版的判据是那颗节点自己的框）；
        ///  · 那个圆是**我们自己加的**，它**纵向**探出原版框 113 px（多认）；
        ///  · 而原版框上下那 40 px（`m_RaycastPadding`）**一点都没过**（漏认）。</para>
        /// <para>原版判据 = `Mulligan/ButtonsGroup/MulliganContinueButton/Button` 自己的 `rect`
        /// **577.5 × 63.84**（中心 = `BarCx/BarCy`）按 `DonePad*` 外扩 ⇒ **577.5 × 143.84**（中心不变）。
        /// 它**顺带**把同父的另两颗也包住了（`工具/menu_dump.py … "Mulligan"` 逐条核过）：
        /// `CircleButton`（80.47×79.64 @1763.45, 980.25）与 `Text`（368.93×62.24 @1525.05, 981.05）
        /// **都在框内**（x 1322.7–1900.2 · y 908.33–1052.17）⇒ **并集就是这一块**，不必再单独判它们
        /// （三颗子件的 `m_RaycastTarget` 都是 1 ⇒ 点它们照样冒泡到那颗钮）。</para>
        /// <para>🔴 算式**只此一份** —— 转发 `MenuDraw.PaddedHitRect`。⛔ 本件一个字都不写 `±pad`。
        /// 🔴🔴 **2026-10-16（`T09` 批 3）就地换口（铁律 5）：世界 → 设计 px 走 `PxOf`（画布帧），
        /// 不再是 `LayoutSpace.ToDesignPixel`（世界帧）。** 那两颗钮（`_bar` / 文字）**已经搬到画布上**，
        /// 而画布在非 16:9 下**两轴系数不同**（`T09` §七·8）⇒ 拿世界帧的逆去量画布上的件 = 量错
        /// （4:3 / 批处理下 y 差 1.333 倍 ⇒ **看得见的地方点不到**）。
        /// `PxOf` 是 `_frame.InverseTransformPoint`，与「件真正画在哪」同一个变换 ⇒ **任何宽高比下都咬合**；
        /// 16:9 下它与 `ToDesignPixel` 逐位同值（差 float 舍入）。</para></summary>
        public bool HitDone(Vector3 world)
        {
            var p = PxOf(world);                          // 面板内设计 px、左上原点（与下面那个框同帧）
            if (float.IsNaN(p.x)) return false;           // 面板没建出来 ⇒ 不认（⛔ 不拿 (0,0) 当命中）
            var hit = MenuDraw.PaddedHitRect(
                new PxRect(BarCx - BarW * 0.5f, BarCy - BarH * 0.5f,
                           BarCx + BarW * 0.5f, BarCy + BarH * 0.5f),
                new Vector4(DonePadL, DonePadB, DonePadR, DonePadT));
            return p.x >= hit.x1 && p.x <= hit.x2 && p.y >= hit.y1 && p.y <= hit.y2;
        }

        /// <summary>🆕 **2026-10-19（A1301③）自检用**：「完成换牌」那颗钮的**世界坐标**
        /// （= 原版那颗 `Button` 的**框中心** —— 与 `_bar` 画在哪无关，没取到图也照样给得出）。
        /// ⚠️ 同族的 `DoneWorldPos` 是那颗 **`Text` 子件**的中心（1525.05, 981.05），**两者不是同一个点**
        /// （`DoneWorldPos` 是驱动层喂点击用的、在框内 ⇒ 两条路都对，⛔ 别互相顶替）。
        /// 🔴 **2026-10-16（`T09` 批 3）**：三个 `*WorldPos` 与 `HitDone` 现在**同一个帧**（画布帧，见 `PxOf`）
        /// —— 它们必须同帧，否则「驱动层拿这个点去点 / 自检拿这个点当中心去探四条边」（`BattleScene` §A1301③）
        /// 两条路都会**离框**。</summary>
        public Vector3 BarWorldPos { get { return WorldOf(BarCx, BarCy); } }

        /// <summary>🆕 **2026-10-19（A1301③）自检用**：那颗钮的**原版命中框**尺寸（**设计 px**）
        /// = 577.5 × 63.84 四边按 `DonePad*` 外扩 ⇒ **577.5 × 143.84**（算式 = 唯一一份 `PaddedRect`）。</summary>
        public static Vector2 DoneHitPx
        {
            get
            {
                var r = MenuDraw.PaddedRect(new PxRect(0f, 0f, BarW, BarH),
                                            new Vector4(DonePadL, DonePadB, DonePadR, DonePadT));
                return new Vector2(r.W, r.H);
            }
        }

        /// <summary>指针在「眼睛」上吗（画布上那颗 `HudPiece` 的**世界四边形**判定 ⇒ 任何宽高比下都对）。</summary>
        public bool HitEye(Vector3 world) { return _eye != null && _eye.Contains(world); }
        /// <summary>「完成换牌」那颗**文字**（`Text` 子件）的中心 —— **驱动层喂点击用的**（`BattleDriver`
        /// 的 `SimulateMulliganDone` 与倒计时自动完成都点它）。
        /// 🔴 **2026-10-16（`T09` 批 3）**：画布帧（`WorldOf`）—— 见 `BarWorldPos` 那段（三个必须同帧）。</summary>
        public Vector3 DoneWorldPos { get { return WorldOf(DoneCx, DoneCy); } }
        /// <summary>「眼睛」那颗钮的中心 —— **驱动层喂点击用的**（`BattleDriver.SimulateMulliganEye`）。
        /// ⚠️ 它必须交**件真正画在哪**（`HitEye` 走 `Contains` 世界四边形）⇒ 画布帧，见 `WorldOf`。</summary>
        public Vector3 EyeWorldPos { get { return WorldOf(EyeCx, EyeCy); } }
        public Vector3 CardBtnWorldPos(int i)
        {
            return (i >= 0 && i < _cardBtns.Count && _cardBtns[i] != null) ? _cardBtns[i].transform.position
                                                                          : Vector3.zero;
        }

        /// <summary>标记/取消一张牌。点了哪张就把它记进「要换掉的」里。
        /// 🔴 **入口只该有两处**：真人点那张牌上的「换」钮（`HandleClick` 的 `ButtonIndexAt` 分支）
        /// 与自检（`BattleDriver.SimulateMulliganToggle` / 直接调）。
        /// ⛔ **点手牌本身不进这里** —— 那一下是「看牌」（原版 `CardScript.OnTouchUpAsButton` →
        /// `DisplayCard`），2026-10-12（`A1663`）已把那条我们加的路删掉。</summary>
        public void ToggleMark(int i)
        {
            if (i < 0 || i >= _cards.Count) return;
            if (_marked.Contains(i)) _marked.Remove(i); else _marked.Add(i);
            // 🆕 2026-10-15（`T06` · 战斗音响）：**换牌选中/取消**那一拍（原版 `Toggle` cue）。
            //   判据 = `BattleCardUI__SetAsMulliganSelected.c:99` —— 它在那条
            //   `SoundAssetCollection__get_Toggle` → `SoundManager__Play2D(...,0)` 上，
            //   **选中与取消都响**（那方法只做「写选定态」，不分方向）⇒ 本处放在分支之后、无条件播。
            BattleSfx.Play(BattleSfx.Toggle);
            ApplyMarks();
        }

        /// <summary>被标记「要换」的下标（**升序**，直接喂给 `RuleCore.Mulligan`）</summary>
        public List<int> Marked
        {
            get { var l = new List<int>(_marked); l.Sort(); return l; }
        }

        public bool IsMarked(int i) { return _marked.Contains(i); }

        /// <summary>标记的样子：卡**置灰**（`Unplayable` 那一档）+ 按钮换成按下态那张图
        /// + 钮上的字换档（标记 ⇒ `Battle/Mulligan/Undo`）。**判据只此一处**（`CardBtnWord`）。</summary>
        void ApplyMarks()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c == null) continue;
                bool m = _marked.Contains(i);
                c.SetHighlight(m ? CardHighlightState.Unplayable : CardHighlightState.Normal);
                if (i < _cardBtns.Count && _cardBtns[i] != null)
                    _cardBtns[i].SetTexture(CardArt.Ui(m ? "UI_Button_Mulligan_Pressed" : "UI_Button_Mulligan"));
                // 反馈的 Normal 跟着换（否则指针一离开就把「按下图」换回未标记那张）
                if (i < _fxCardBtns.Count && _fxCardBtns[i] != null)
                    _fxCardBtns[i].SetNormal(CardArt.Ui(m ? "UI_Button_Mulligan_Pressed" : "UI_Button_Mulligan"));
                // 🆕 2026-10-18（第十三轮 · G2b）：**字跟着档走**（原版三处都是点一下就把 term 重设一遍）
                if (i < _cardTexts.Count && _cardTexts[i] != null)
                    _cardTexts[i].SetText(CardBtnWord(m));
            }
        }

        /// <summary>
        /// 处理一次点击。返回 true = 这次点击被面板吃掉了（别再传给棋盘/手牌）。
        /// ⚠️ 和设置/日志面板一样：**面板开着时点哪儿都先经过这里**。
        /// 🔴 **2026-10-12（`A1663`，用户真机点名「不是点击手卡替换」）**：这里原来**还**有一条
        ///    「点卡本身也能标记/取消」（注释自己招着「点卡这条路是我们加的」）—— **已删**。
        ///    现在点手牌走原版那条路（`CardScript.OnTouchUpAsButton` → `BattleManager.DisplayCard`）：
        ///    本方法只把「点到了哪张卡」回调出去（`OnCardSeen`），**做什么由驱动层决定**。
        /// </summary>
        public bool HandleClick(Vector3 world)
        {
            if (!Visible) return false;

            if (HitDone(world))
            {
                var marks = Marked;
                if (OnDone != null) OnDone(marks);
                return true;
            }
            if (HitEye(world))
            {
                // 「眼睛」= 原版 `HideMulliganButton` → `MulliganManager.ToggleMulliganVisibility`
                // → `ShowMulliganElements(bool)`。**2026-09-17 照反编译核实过**：
                //   · **是开关不是按住** —— 它读 `activeInHierarchy` 再取反（`ToggleMulliganVisibility`）
                //     ⇒ 原来这句写「原版到底是按住还是开关**没查到**」已更正。
                //   · 一次收**四样**：两个 GameObject（`SetActive`×2）+ **每张卡的换牌按钮**
                //     （`ShowMulliganCards` 是个循环）+ **压暗层**（`Shade.SwitchShade(show)`）。
                // 🔴 我们原来只收了按钮、**压暗还盖着** —— 这是**漏做**：玩家按眼睛就是为了看战场，
                //    原版这时屏幕是亮的（`MulliganManager__ShowMulliganElements.c:6-17`）。
                bool show = !CardButtonsShown;
                for (int i = 0; i < _cardBtns.Count; i++)
                    if (_cardBtns[i] != null) _cardBtns[i].gameObject.SetActive(show);
                for (int i = 0; i < _cardTexts.Count; i++)
                    if (_cardTexts[i] != null) _cardTexts[i].gameObject.SetActive(show);
                if (_shade != null) _shade.gameObject.SetActive(show);
                return true;
            }

            int hit = ButtonIndexAt(world);
            if (hit >= 0) { ToggleMark(hit); return true; }

            // 🔴 点手牌本身 = **把这张牌放大展示**（原版 `CardScript__OnTouchUpAsButton.c:52-58`：
            //    换牌期只做「置 `displayingCardFlag` + `DisplayCard(card,1,1)`」）。
            // ⛔ **不再标记** —— 标记只走上面那颗「换」钮（原版 `ClickMulliganSelected`）。
            var seen = CardAt(world);
            if (seen != null) { if (OnCardSeen != null) OnCardSeen(seen); return true; }

            return true;      // 面板开着 → 点其它地方也吃掉（原版 `Shade.SwitchShade` 就是这个意思）
        }

        // ==================================================================
        //  自检
        // ==================================================================

        public int CardButtonCount { get { return _cardBtns.Count; } }

#if UNITY_EDITOR
        /// <summary>🆕 **2026-10-19（`T09` 步骤 B · `W57` · B2a）自检用**：换牌区这个点上有**几张**卡的
        /// **真 collider** 命中（0 = 一张都没有）。
        /// <para>用途 = 「重叠区取 z 最小」那条断言的**前提探针**：先证明两张**都**命中
        /// （否则 `CardAt` 那条断言退化成一个候选、验不出排序）。判据走的是与 `CardAt` **同一条**
        /// 生产口（`CardInteraction.FrontmostHitCountForTest` → `CardView.HitByCollider`），
        /// ⛔ 本件不重算任何矩形。</para></summary>
        public int CardHitCountForTest(Vector3 world)
        {
            return CardInteraction.FrontmostHitCountForTest(_cards, world);
        }
#endif
        /// <summary>自检用（**A276**）：第 i 张牌那个「换」按钮那一层的 **z 档**（没有 ⇒ `NaN`）。
        /// 判据 = 它只由本面板的 `Z`（建件口那个形参）决定，**不许跟卡的世界 z 走**。
        /// 🔴 **2026-10-16（`T09` 批 3）就地改一个读法（铁律 5）**：原来是 `transform.localPosition.z`
        /// —— 画布上那个 `z` **恒 0**（写它只会把件推离平面）⇒ 再读它这条断言**恒绿、没判别力**。
        /// 现在读 **`HudPiece.OrderZ`**（= 它在兄弟序那本账上的 z 档，与「谁压谁」一一对应）
        /// ⇒ 与旧那套**同一件事**、判别力也回来了（卡的世界 z 挪 0.4 也不该动它）。</summary>
        public float CardButtonLocalZ(int i)
        {
            if (i < 0 || i >= _cardBtns.Count || _cardBtns[i] == null) return float.NaN;
            return _cardBtns[i].OrderZ;
        }
        /// <summary>提示行现在写着什么（自检用）。
        /// 🔴 **A245：这一行没建出来时返回 `null`**（原来返 `"&lt;无>"`）—— `"&lt;无>"` 是**非空串**，
        ///    而 `Editor/BattleScene.cs` 那条断的是 `!string.IsNullOrEmpty(...)` ⇒ 两态（建了 / 没建）
        ///    **分不开**、那条断言等于没查。返 `null` 之后两种状态才判得出来。
        /// 🔴 **2026-10-16（`T09` 批 3）就地订正（铁律 5）**：原来这里写「⚠️ 今天**不可达**
        ///    （`Label.Create` 恒不返回 null ⇒ 这条是**潜在**缺口）」—— **已经成立、可达了**：
        ///    本面板的字现在走 `HudCanvas.MakeText`（= `Label.CreateUgui`），**拿不到 TMP 字体时它返 `null`**
        ///    （`Label.cs:176-179` 出声 + 销毁节点）⇒ 这一档是真状态，下面那几条断言**有判别力**。
        /// ⚠️ 同族的 `TurnText` / `DoneText` 一起改成 `null`（它们的断言是 `==` 字面量，
        ///    两态本来就分得开，改只是为了**同族一个口径**）；`Describe()` 那边自己补占位符。</summary>
        public string PromptText { get { return _prompt != null ? _prompt.Text : null; } }
        public bool BarHasArt { get { return _bar != null && _bar.Texture != null; } }

        /// <summary>底条的**渲染尺寸**（世界单位）—— 自检用它钉住「原版 PA=0 拉满 577.5×63.84」那条。
        /// 🔴 2026-09-27：`ImageQuad` 默认**按贴图比例定宽**（`_aspect = tex.w/tex.h`），
        ///   而这条底原版是 `PA=0` 硬拉伸 ⇒ 补 `SetAspect` 之前我们只画出 **372.8** 宽（窄 204.7px）。
        ///   量的是渲染尺寸，不是「框」—— 框一直都是对的，错的是往里画多大。
        /// 🔴🔴 **2026-10-16（`T09` 批 3）：这两个口【别再用 ×108 折回 px】。** `HudPiece.WorldW/H` 走
        ///   `GetWorldCorners()` ⇒ 交的是**世界单位**，而画布件从「设计 px」到世界单位的系数
        ///   **随屏变、而且两轴还能不同**（`T09` §七·8 实测：批处理下 x 1/108、y **1/144** ⇒
        ///   `BarWorldH * 108` 读出 **47.9** 而真值 **63.84** —— **画面其实是对的**）。
        ///   ⇒ 自检要 px 就用下面那个 **`BarSizePx`**（读 `sizeDelta`，与建件那一侧同口径）。</summary>
        public float BarWorldW { get { return _bar != null ? _bar.WorldW : 0f; } }
        public float BarWorldH { get { return _bar != null ? _bar.WorldH : 0f; } }
        /// <summary>🆕 **2026-10-16（`T09` 批 3）自检用**：底条画出来的**设计 px**（= `sizeDelta`）。
        /// 判据同 `BarWorldW/H`（原版 PA=0 拉满 577.5×63.84），只是量在**与建件同一把尺**上
        /// —— ⛔ 别再用 `BarWorldW * 108f`（见上面那条，非 16:9/批处理下会得到假数）。</summary>
        public Vector2 BarSizePx
        {
            get { return _bar != null ? HudCanvas.SizePx(_bar.Rect) : Vector2.zero; }
        }
        /// <summary>🔴 **2026-10-16（`T09` 批 3）自检用（**灭自证**）**：本面板的图与字**是不是都建在
        /// uGUI 子画布上**（而不是有条漏网、退回世界空间 quad）。
        /// <para>判据 = 那几件**每一件都 `HudPiece.IsUgui`** 且**都挂在 `_frame`（画布帧）的父链下**。
        /// 语义上它防的是本件**最坏的那一档**：一半在画布、一半在世界 —— 16:9 下逐位相同、全绿，
        /// 只有别的宽高比才露（`T09` §五·补⑦）。⛔ 别用「`HudPiece != null`」替它（那个两边都真）。
        /// ⚠️ 任一件没建出来（贴图/字体缺失）⇒ 也返 `false`，如实回答「不成立」。</para>
        /// <para>🔴 **2026-10-24（第七十三会话 · `M1`）就地订正（铁律 5）**：`_shade` **已从这一条里移出**
        /// —— 它现在是**世界空间 `ImageQuad`**（<see cref="QMulliganShade"/> 那条 doc 写了为什么「画布件不行」：
        /// 画布排序层先于 `renderQueue` 比较，任何 `OverHand` 画布都会盖住世界空间的换牌卡）。
        /// 本条的「灭自证」另一半（**世界档下必须为假**）由 `Editor/BattleScene.cs` 的 `D6` 那一段
        /// 从 `ShadeIsUgui` / `RowCard(i).Queue` 那两侧钉住。</para></summary>
        public bool OnCanvas
        {
            get
            {
                if (_frame == null || _frame.GetComponentInParent<Canvas>() == null) return false;
                return IsUgui(_bar) && IsUgui(_play) && IsUgui(_eye)
                    && IsUgui(_prompt) && IsUgui(_doneText) && IsUgui(_turnText);
            }
        }
        static bool IsUgui(HudPiece q)
        {
            return q != null && q.IsUgui && q.Rect != null
                && q.Rect.GetComponentInParent<Canvas>() != null;
        }
        static bool IsUgui(Label l) { return l != null && l.IsUgui; }
        public bool PlayHasArt { get { return _play != null && _play.Texture != null; } }
        public bool EyeHasArt { get { return _eye != null && _eye.Texture != null; } }

        /// <summary>卡片上的「换」按钮现在露着没有（眼睛那颗钮的开关状态）</summary>
        public bool CardButtonsShown
        {
            get { return _cardBtns.Count > 0 && _cardBtns[0] != null && _cardBtns[0].gameObject.activeSelf; }
        }

        /// <summary>压暗层还盖着没有。原版点「眼睛」时它跟按钮**一起**收起（`Shade.SwitchShade`）</summary>
        public bool ShadeActive { get { return _shade != null && _shade.gameObject.activeSelf; } }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-22（W62 · 战斗线 `D6`）：换牌期**层序**的只读探针（自检用）
        //  ⚠️ 只读、不改状态；每个口只答一件事（⛔ 别合成一个 `bool` —— 分开才分得清是哪一半错）。
        //  🔴 **2026-10-24（第七十三会话 · `M1`）**：`ShadeCanvasLayer` 换成 `ShadeRenderQueue` /
        //     `ShadeIsUgui` —— 压暗层已改回**世界空间 quad**（画布排序层先于 `renderQueue` 比较，
        //     原来那条「在 `OverHand` 画布上」的前提本身就是病灶，见 <see cref="QMulliganShade"/>）。
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>压暗层的**渲染队列号**（读回真实材质，⛔ 不是读 <see cref="QMulliganShade"/> —— 那是自证）。
        /// 它必须**大于战场全部几何的队列**（13 场实测最大 3005）**小于换牌卡**（3007）。
        /// 没建出来 / 不是世界 quad ⇒ **`int.MinValue`**（必红，不静默）。
        /// <para>🔴 **2026-10-24 修正（复跑 `BattleScene.Run` 当场抓到）**：本口原来把判据写成
        /// `!_shade.IsUgui`（= **在画布件上**才往下读）⇒ 改成世界 quad 之后**恒回 `int.MinValue`**，
        /// `D6②` 因此红（读数 `-2147483648`）。正确写法 = `_shade.IsUgui` 为**真**（画布件）才早退。
        /// ⚠️ 这一类「否号写反」的探针**不会崩**、只会静默回哨兵 —— 所以 `D6②` 那条「哨兵必红」是必要的。</para></summary>
        public int ShadeRenderQueue
        {
            get
            {
                if (_shade == null || _shade.IsUgui) return int.MinValue;   // 画布件没有「本工程的队列号」可言
                return _shade.Quad != null ? _shade.Quad.RenderQueue : int.MinValue;
            }
        }

        /// <summary>压暗层现在是不是 uGUI 画布件（**判据：必须是 `false`**）。
        /// 2026-10-24 真机实测：它是画布件时**队列 3001 的换牌卡照样被它盖住**（卡区 max = 123 ≈ 线性 0.198）
        /// ⇒ 只要这个口回 `true`，卡就一定是暗的。</summary>
        public bool ShadeIsUgui { get { return _shade != null && _shade.IsUgui; } }

        /// <summary>压暗层的透明度（读回**实际染色**，⛔ 不是读 <see cref="ShadeAlpha"/> —— 那是自证）。
        /// 没建出来 ⇒ `-1`。</summary>
        public float ShadeTintAlpha { get { return _shade != null ? _shade.Tint.a : -1f; } }

        /// <summary>换牌区第 `i` 张牌（**自检用**：要读它的 `CardView.Queue` 来跟压暗层比先后）。
        /// 越界 / 没建出来 ⇒ `null`。</summary>
        public CardView RowCard(int i) { return (i >= 0 && i < _cards.Count) ? _cards[i] : null; }

        /// <summary>换牌区现在有几张牌（同 `CardButtonCount`，但这个口数的是**卡**、不是钮）。</summary>
        public int RowCardCount { get { return _cards.Count; } }

        public string BarTex { get { return _bar != null && _bar.Texture != null ? _bar.Texture.name : "<无>"; } }
        public string EyeTex { get { return _eye != null && _eye.Texture != null ? _eye.Texture.name : "<无>"; } }
        /// <summary>🆕 **2026-10-18（W6）**：第 <paramref name="i"/> 张牌那颗钮上**写的什么**
        /// （自检用；没建出来时返回 `null`，同 `DoneText`/`TurnText` 那个口径 A245）。
        /// 🔴 **2026-10-18（第十三轮 · G2b）**：现在是**两档** —— 未标记 = `Battle/Mulligan/Replace`、
        /// 已标记 = `Battle/Mulligan/Undo`（判据 → `UndoTerm` 的 doc；档由 `CardBtnWord` 一处决定）。</summary>
        public string CardBtnWordAt(int i)
        {
            return i >= 0 && i < _cardTexts.Count && _cardTexts[i] != null ? _cardTexts[i].Text : null;
        }

        /// <summary>第 i 张牌那个按钮现在用的图（按下态应当是 `UI_Button_Mulligan_Pressed`）</summary>
        public string CardBtnTex(int i)
        {
            if (i < 0 || i >= _cardBtns.Count || _cardBtns[i] == null) return "<无>";
            return _cardBtns[i].Texture != null ? _cardBtns[i].Texture.name : "<无>";
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e1c）：**Editor-only 只读探针**
        //  只读：不改状态、不派事件。播放器里**不存在**这些成员（`#if` 包住）⇒ 零运行时开销。
        // ══════════════════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
        /// <summary>第 `i` 张牌那颗「替换」钮的视觉反馈对象（读 `TextureNameForTest` / `KeyForTest`）。
        /// 原版 = `Change Card Button` 的 `m_Transition = **2**`（`SpriteSwap`，⛔ 不是 ColorTint）+
        /// `UI_Button_Mulligan_hover/_Pressed` 两张图（判据 → 本类 `_fxCardBtns` 那段注释）。
        /// `null` ⇒ 没建出来（卡片没摆 / 子画布没立起来）。</summary>
        public BattleButtonFx CardBtnFxForTest(int i)
        {
            return (i >= 0 && i < _fxCardBtns.Count) ? _fxCardBtns[i] : null;
        }

        /// <summary>第 `i` 张牌那颗钮**当前露着的贴图**（同 `CardBtnTex`，但这个口在没图时返 `null`
        /// 而不是 `"<无>"` —— 便于 Editor 断言区分「没建」与「建了但贴图为空」）。</summary>
        public string CardBtnTextureNameForTest(int i)
        {
            var p = (i >= 0 && i < _cardBtns.Count) ? _cardBtns[i] : null;
            return p != null && p.Texture != null ? p.Texture.name : null;
        }
#endif

        public string Describe()
        {
            return $"换牌面板 开着={Visible} 卡按钮 {_cardBtns.Count} 个 标记 [{string.Join(",", Marked.ConvertAll(x => x.ToString()).ToArray())}]"
                 + $" 提示「{PromptText ?? "<无>"}」";      // 占位符挪到**打印这一侧**（A245：getter 返 null 才分得开两态）
        }
    }
}
