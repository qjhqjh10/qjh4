// MulliganPanel.cs — 开局换牌（原版 `Mulligan` 子树 / `MulliganManager` / `MulliganFrame`）
//
// **为什么有它**：原版抽完起手牌会进 `_SetupMulliganPhase`，双方换完才 `StartBattlePhase`；
// 规则书 :46「换牌（Mulligan）| **可弃回任意起手牌后重洗补抽**」。我们原来**全仓 0 命中**。
//
// ---- 结构照运行时 dump（`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv:437-450`）----
//   Mulligan（整屏，FrontCanvas 下）
//     ├ MulliganAnchor              —— 卡片的布局锚（原版挂的是 `CardsHorizontalLayout`；我们用手牌那套）
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
//  · **每张牌的「换」按钮**（原版 `MulliganFrame.changeCardButton`）：它是**运行时按卡生成的**，
//    dump 是静态树、**没有它** → **位置和大小是我们排的**（贴在每张手牌的下缘、宽 = 卡宽，
//    图用原版三态 `UI_Button_Mulligan` 410×124）。文案「换」也是我们起的（原版是 I2 本地化键）。
//  · **遮罩**：原版这一阶段有 `Shade.SwitchShade`（`_SetupMulliganPhase` 里调的），
//    但**颜色/透明度没查到** → 用了和战斗日志面板同款的一层压暗，**这是我们挑的**。
//  · **提示行文案**：「选择要换掉的牌」是我们起的（原版 `mulliganTextLocalize` 的 I2 词条本地没有）。
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
using System;
using System.Collections.Generic;
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
            SetScriptFont(_doneText, s, DoneH * 0.45f);
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

        /// <summary>按「这一局谁先手」设那一行。<paramref name="playerGoesSecond"/> = 我方是后手。
        /// 判据 → `资料/加时与冲突模式_原版规格.md` §2.8（原版 `MulliganManager.ActivateMulligan` 的二选一）。</summary>
        public void SetTurnText(bool playerGoesSecond)
        {
            if (_turnText == null) return;
            string t = playerGoesSecond ? TurnSecond : TurnFirst;
            _turnText.SetText(t);
            // 🔴 **字号按【这一串字】的语种选** —— 汉字 ≈ 1 em、拉丁大写 ≈ 0.72 em
            //    （判据 = `Battle/Label.SetCapHeight`/`SetGlyphHeight` 的 doc，`Loc.HasCjk` 是那条判据的**唯一一份**）。
            //    原来这里写死 `SetGlyphHeight`：中文档对，但英文档（`You go second`）会**大 39%**。
            SetScriptFont(_turnText, t, TurnPx);
        }

        /// <summary>提示行那条的词条键 —— **只此一份**（`Build()` 与自检都取它）。
        /// ⚠️ 原版的 `WaitText`（`Battle/Mulligan/WaitEnemy`）是**另一条**键，挂在等待横幅上，
        /// ⛔ 别把两条并成一条（它们挂在不同节点、`BattleDriver.cs:3662` 那一格根本没有对应词条）。</summary>
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
        /// **没有 `Undo` 这个英文串** —— 2026-10-18 按第一列精确查过）—— 如实标在 `Core/Loc.cs`。</para></summary>
        public const string UndoTerm = "Battle/Mulligan/Undo";

        /// <summary>那颗钮现在该写什么 —— **判据只此一处**（建钮和 `ApplyMarks` 都取它，
        /// ⛔ 别在两处各判一次 `marked`）。</summary>
        public static string CardBtnWord(bool marked)
        {
            return Loc.T(marked ? UndoTerm : ReplaceTerm);
        }

        /// <summary>按**这段文本的语种**定字号 —— 转发到 `Label.SetScriptHeight`（那条判据的**唯一一份**实现，
        /// 内部接的是 `Loc.HasCjk`）；本件只负责 px→世界单位那一跳（`U()` = /108）。
        /// 同族的另一处调用点见 `Battle/SettingsPanel.ApplyLangFont`（它现在也是转发）。</summary>
        internal static void SetScriptFont(Label l, string text, float px)
        {
            if (l == null) return;
            l.SetScriptHeight(text, px, 108f);
        }

        /// <summary>先手 / 后手那两句 = **原版词条**（各一条，⛔ 别合并）。
        /// 🔴 **2026-10-18（第十二轮 · W6）改**：原来这两个是写死的**中文字面量**，注释写
        /// 「后手那句照原版英文兜底译；先手那句**原版英文没查到**（只有词条名 `GoFirst`）⇒ 我们译的」。
        /// **键那一半现在查清了**：`Battle/Tips/GoFirst` 与 `Battle/Mulligan/secondTurn` **都在本地能读到**
        /// （前者只在**代码字面量**里 —— `d:/2/tools/il2cpp_out/stringliteral.json` `0x428A128`，
        /// prefab 上零 `Localize`；后者是 prefab `mTerm`，TMP 原文 `You go second`）。
        /// ⚠️ **文案那一半没变**：`Battle/Tips/GoFirst` 的英文原文**本地取不到**（远端 I2 表）
        /// ⇒ `Loc` 表里那一条 EN/ZH **都自拟**（如实标着，⛔ 别写成「照抄原版」）。</summary>
        public static string TurnFirst { get { return Loc.T(TurnFirstTerm); } }
        public static string TurnSecond { get { return Loc.T(TurnSecondTerm); } }
        /// <summary>先手 / 后手那两条词条键 —— **只此一份**。</summary>
        public const string TurnFirstTerm = "Battle/Tips/GoFirst";
        public const string TurnSecondTerm = "Battle/Mulligan/secondTurn";

        public bool Visible { get; private set; }

        /// <summary>点「完成换牌」→ 回调「要换掉的手牌下标」（可能为空 = 不换）</summary>
        public Action<List<int>> OnDone;

        ImageQuad _shade;
        ImageQuad _bar, _play, _eye;      // Continue 的底 / 圆形播放钮 / 眼睛
        Label _prompt, _doneText;
        /// <summary>🆕 2026-09-26：「你先手 / 你后手」那一行（原版 `MulliganText/TurnText`）。</summary>
        Label _turnText;
        readonly List<ImageQuad> _cardBtns = new List<ImageQuad>();
        readonly List<Label> _cardTexts = new List<Label>();
        readonly List<CardView> _cards = new List<CardView>();
        readonly List<int> _marked = new List<int>();

        // ---- 原版绝对坐标（1920×1080，y 从**上**）----
        const float PromptCx = 967f, PromptCy = 106.5f, PromptW = 1344f, PromptH = 79.4f;
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
        /// 的 `m_fontColor32 = 4294967295`）。断言在 `Editor/BattleScene.cs:11843`。</summary>
        const float TurnCx = 966.03f, TurnCy = 156.5f, TurnW = 1307.06f, TurnH = 54.17f, TurnPx = 55f;
        const float BarCx = 1611.45f, BarCy = 980.25f, BarW = 577.5f, BarH = 63.8f;
        const float PlayCx = 1763.45f, PlayCy = 980.25f, PlayH = 79.6f;
        const float DoneCx = 1525.05f, DoneCy = 981.05f, DoneW = 368.9f, DoneH = 62.2f;
        const float EyeCx = 184.45f, EyeCy = 908.5f, EyeH = 79.6f;

        static float U(float px) { return px / 108f; }
        static Vector3 At(float cx, float cy) { return LayoutSpace.ToWorld(cx / 1920f, 1f - cy / 1080f); }

        /// <summary>补间/自检用：面板的 z。和日志面板一样推到 −0.9 一带 ——
        /// 场上的粒子在 −0.5 那层会飘到面板上面（那个坑记在 `BattleLogPanel` 的注释里）</summary>
        const float Z = -0.9f;

        public static MulliganPanel Create(Transform parent)
        {
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名件 `Mulligan` 实读：`RectTransform` · `anchor (0,0)-(1,1)` ·
            //    `sizeDelta (0,0)` ⇒ **绝对矩形 (0,0)-(1920,1080)**（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读）⇒ 整屏矩形（与文件头那段 dump「`Mulligan`（整屏，FrontCanvas 下）」一致）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「换牌面板根 = 整屏矩形」红。
            var go = new GameObject("MulliganPanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            var p = go.AddComponent<MulliganPanel>();

            // 压暗（**我们挑的**，原版只查到有个 Shade，颜色/透明度没查到）
            p._shade = ImageQuad.Create(go.transform, CardArt.Solid(), At(960f, 540f), U(1200f),
                                        new Vector2(0.5f, 0.5f), "MulliganShade");
            if (p._shade != null)
            {
                p._shade.SetAspect(1920f / 1080f);
                p._shade.SetTint(new Color(0f, 0f, 0f, 0.55f));
                p._shade.transform.localPosition += new Vector3(0f, 0f, Z);
            }

            // 提示行（原版 `MulliganText` 那块 1344×79.4，中心 (967,106.5)）
            // 🔴 **2026-10-18（第十二轮 · W6）**：文案改成**原版词条** `Battle/Mulligan/Instructions`
            //   （TMP 原文 `Choose cards to replace in first hand`，见 `Core/Loc.cs`）。
            //   ⚠️ **原来那半句「（回车 = 完成）」已去掉** —— 原版文案里没有它；留一个只在中文档出现的
            //      括号半句就等于**又开一条写死路径**（本工程红线）。回车那把快捷键**照旧能用**
            //      （接线在 `BattleDriver` 的键处理里），只是不再印在提示行上。
            //   ⚠️ 字号按那块框的高度取 —— **原来是我们挑的**（保留原样，本轮不动版面）。
            // 🔴 **颜色照原版（2026-09-30 亲读原版资产）**：`bundle_scenes_scenes_battlearena1/MonoBehaviour/`
            //   `MonoBehaviour_3731.json`（`Choose cards to replace in first hand`）与 `MonoBehaviour_3856.json`
            //   （`You go second`）的 **`m_fontColor32` 都是 `4294967295`（= `0xFFFFFFFF` 纯白）**、
            //   `m_fontColor` 都是 `(1,1,1,1)`。原来两行都用暖色 `(1, 0.94, 0.82)` ⇒ **改成纯白**。
            //   （旁证：`资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md:274-275` 记「白」。）
            string promptText = Loc.T(PromptTerm);
            p._prompt = Label.Create(go.transform, promptText, At(PromptCx, PromptCy), 8,
                                     Color.white, new Vector2(0.5f, 0.5f), "MulliganPrompt");
            SetScriptFont(p._prompt, promptText, PromptH * 0.55f);

            // 🆕 2026-09-26：**开局谁先手那一行** —— 判据（唯一）→ `资料/加时与冲突模式_原版规格.md` §2.8：
            //   原版在 `MulliganManager.ActivateMulligan` 里把这一行的词条**按先手/后手二选一**
            //   （`playerGoesFirst != 0` ⇒ `Battle/Tips/GoFirst`，否则 `Battle/Mulligan/secondTurn`；
            //    后者在场景资产里的英文兜底是 **"You go second"**，实测 `bundle_scenes_scenes_battlearena1`）。
            //   🔴 **它是原版唯一一处「先手/后手」的表现** —— 原版**没有硬币资产 / 动画 / 音效**
            //   （2026-09-26 全量查过：`coin`/`toss`/`dice` 在 91 个 bundle 的资产名与 MonoBehaviour 内容里
            //    都只命中商城的 "Add coins"）⇒ 我们的「投硬币」**只该在这一行上露出来**。
            //   ⚠️ 文案：后手那句照原版英文兜底译；**先手那句只有词条名 `GoFirst`（英文原文没查到）⇒ 我们译的**。
            p._turnText = Label.Create(go.transform, "", At(TurnCx, TurnCy), 7,
                                       Color.white, new Vector2(0.5f, 0.5f), "MulliganTurnText");
            // ⚠️ 出厂这一下只是给个尺寸，**真正的字号按语种在 `SetTurnText` 里再设一遍**
            //    （那一行是**唯一**的口径：中文 `SetGlyphHeight` / 英文 `SetCapHeight(0.72em)`）。
            //    原版那一颗是 `m_fontSize = 55`（em 的像素值）。
            if (p._turnText != null) p._turnText.SetGlyphHeight(U(TurnPx));

            // 完成按钮：底图 `40k_bt_underbutton`（原版 577.5×63.8）+ 圆形播放钮 `40k_UI_bt_play`
            p._bar = ImageQuad.Create(go.transform, CardArt.Ui("40k_bt_underbutton"), At(BarCx, BarCy),
                                      U(BarH), new Vector2(0.5f, 0.5f), "MulliganContinueBar");
            // 🔴 **2026-09-27 修（PA 普查抓的）**：原版这条底 `m_PreserveAspect = 0`、`m_Type=0`（Simple）
            //    ⇒ **拉满 577.5×63.84**；`ImageQuad` 默认按贴图比例定宽，而 `40k_bt_underbutton` 是
            //    **485×83**（5.8434）⇒ 我们只画出 **372.8×63.84**、**窄 204.7 px**。
            //    实据：`battlearena1` 的 `Mulligan/ButtonsGroup/MulliganContinueButton/Button`
            //    （RT 2659 / GO 121 / MB 5292，直读 `m_PreserveAspect=0`、`m_Type=0`）。
            //    ⚠️ 同图同尺寸的 `CardChoicePanel` 早就有这一行（`_bar.SetAspect(BarW / BarH)`）——
            //    这一处是漏了，不是设计。
            if (p._bar != null) p._bar.SetAspect(BarW / BarH);
            p._play = ImageQuad.Create(go.transform, CardArt.Ui("40k_UI_bt_play"), At(PlayCx, PlayCy),
                                       U(PlayH), new Vector2(0.5f, 0.5f), "MulliganContinueCircle");
            // ⚠️ 字号在 `SetDoneText` 里按**当前这一串**的语种设（原版那颗钮上会短暂变成倒计时秒数）。
            p._doneText = Label.Create(go.transform, DoneLabel, At(DoneCx, DoneCy), 6,
                                       new Color(1f, 0.92f, 0.75f), new Vector2(0.5f, 0.5f), "MulliganDoneText");
            p.SetDoneText(DoneLabel);

            // 眼睛（原版 `HideMulliganButton`，图 `40k_ui_bt_eye`）—— 暂时收起卡片上的按钮
            p._eye = ImageQuad.Create(go.transform, CardArt.Ui("40k_UI_bt_eye"), At(EyeCx, EyeCy),
                                      U(EyeH), new Vector2(0.5f, 0.5f), "MulliganHideButton");

            foreach (var q in new[] { p._bar, p._play, p._eye })
                if (q != null) q.transform.localPosition += new Vector3(0f, 0f, Z);
            if (p._prompt != null) p._prompt.transform.localPosition += new Vector3(0f, 0f, Z - 0.05f);
            if (p._doneText != null) p._doneText.transform.localPosition += new Vector3(0f, 0f, Z - 0.05f);
            if (p._turnText != null) p._turnText.transform.localPosition += new Vector3(0f, 0f, Z - 0.05f);

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
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null) _cardBtns[i].gameObject.SetActive(v);
            for (int i = 0; i < _cardTexts.Count; i++)
                if (_cardTexts[i] != null) _cardTexts[i].gameObject.SetActive(v);
        }

        /// <summary>
        /// 每张起手牌贴一个「换」按钮。
        /// ⚠️ **位置是我们排的** —— 原版那个 `MulliganFrame` 是运行时实例化的，静态 dump 里没有它
        ///    （见文件头）。这里贴在卡下缘往上 30% 卡高处、宽 = 卡宽（图 410×124 按比例得高）。
        /// </summary>
        void BuildCardButtons(IReadOnlyList<CardView> hand)
        {
            ClearCardButtons();
            _cards.Clear();
            if (hand == null) return;

            for (int i = 0; i < hand.Count; i++)
            {
                var c = hand[i];
                if (c == null) continue;
                _cards.Add(c);

                float cardW = CardView.Width * c.transform.localScale.x;
                float cardH = CardView.Height * c.transform.localScale.y;
                float btnW = cardW;                                  // 宽 = 卡宽（我们排的）
                float btnH = btnW * (124f / 410f);                   // 图 `UI_Button_Mulligan` 的原比例
                // ⚠️ x/y 拿**卡的世界坐标当局部坐标**（「面板与棋盘同在世界空间」的老口径，今天等价）；
                // 🔴 **A276（2026-10-11）：z 改成裸局部 `Z`** —— 原来写的是
                //    `c.transform.position + (0, −h, Z − c.transform.position.z)`，那个 `− c…z` 项
                //    **自相消**（两个 `c.transform.position.z` 恰好抵消、恒等于 `Z`）⇒ 今天**逐位相同**，
                //    但没有表达出「这一层是面板的局部 z」这个意思，而且形状上很容易被后人「顺手化简」成
                //    `c.transform.position`（那样按钮的 z 就会跟卡走）。现在与同父的 `_shade`
                //    （`+= Z`）· `_prompt/_doneText/_turnText`（`+= Z − 0.05`，下一段）**同一套口径**。
                //    ⚠️ 这是**收口径、不是「对齐原版」** —— 这些层的**原版 z 关系没有判据**
                //    （`Z` 这个常量本身是我们挑的，见 `资料/普查产出_1010/V4b_三件口径.md` §Q2）。
                //    改坏法：把 z 写成 `c.transform.position.z`（或整个 `c.transform.position`）⇒
                //    `Editor/BattleScene.cs` 的 A276 那条断言红（它把卡的世界 z 挪 0.4 再重建按钮）。
                var pos = new Vector3(c.transform.position.x, c.transform.position.y - cardH * 0.30f, Z);

                var q = ImageQuad.Create(transform, CardArt.Ui("UI_Button_Mulligan"), pos, btnH,
                                         new Vector2(0.5f, 0.5f), "MulliganBtn_" + i);
                if (q == null) continue;                             // 没图就只靠「点卡」那条路，不静默失败
                q.SetAspect(410f / 124f);                            // 图是 410×124 的横条
                _cardBtns.Add(q);

                // 🔴 **2026-10-18（第十二轮 · W6）**：那颗钮上的字走**原版词条**
                //    `Battle/Mulligan/Replace`（TMP 原文 `Replace`，全库只 1 颗 —— `battleprefabs_vfxandmisc`
                //    的 `ReplaceText`；判据全文 → `Core/Loc.cs`）。原来那个写死的 `"换"` 已删。
                // 🔴 **2026-10-18（第十三轮 · G2b）**：**同一颗钮有两档字** —— 被标记的牌印
                //    `Battle/Mulligan/Undo`（判据见 `UndoTerm` 的 doc）。原来我们**只有一档**，
                //    标记之后那张牌上还写着「换」（= 原版那一刻印的是 Undo）。
                string btnText = CardBtnWord(_marked.Contains(i));
                var t = Label.Create(transform, btnText, pos, 5, new Color(1f, 0.9f, 0.7f),
                                     new Vector2(0.5f, 0.5f), "MulliganBtnText_" + i);
                if (t != null)
                {
                    SetScriptFont(t, btnText, 28f);           // 语种定字号（英文档不再大 39%）
                    t.transform.localPosition += new Vector3(0f, 0f, Z - 0.05f);
                }
                _cardTexts.Add(t);
            }
        }

        void ClearCardButtons()
        {
            for (int i = 0; i < _cardBtns.Count; i++) if (_cardBtns[i] != null) Kill(_cardBtns[i].gameObject);
            for (int i = 0; i < _cardTexts.Count; i++) if (_cardTexts[i] != null) Kill(_cardTexts[i].gameObject);
            _cardBtns.Clear();
            _cardTexts.Clear();
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

        /// <summary>指针在「完成换牌」上吗（底条或圆钮都算）</summary>
        public bool HitDone(Vector3 world)
        {
            if (_bar != null && _bar.Contains(world)) return true;
            if (_play != null && _play.Contains(world)) return true;
            var t = DoneWorldPos;
            return (world - t).sqrMagnitude < (U(DoneW * 0.5f)) * (U(DoneW * 0.5f));
        }

        public bool HitEye(Vector3 world) { return _eye != null && _eye.Contains(world); }
        public Vector3 DoneWorldPos { get { return At(DoneCx, DoneCy) + new Vector3(0f, 0f, Z); } }
        public Vector3 EyeWorldPos { get { return At(EyeCx, EyeCy) + new Vector3(0f, 0f, Z); } }
        public Vector3 CardBtnWorldPos(int i)
        {
            return (i >= 0 && i < _cardBtns.Count && _cardBtns[i] != null) ? _cardBtns[i].transform.position
                                                                          : Vector3.zero;
        }

        /// <summary>标记/取消一张牌。点了哪张就把它记进「要换掉的」里</summary>
        public void ToggleMark(int i)
        {
            if (i < 0 || i >= _cards.Count) return;
            if (_marked.Contains(i)) _marked.Remove(i); else _marked.Add(i);
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
                // 🆕 2026-10-18（第十三轮 · G2b）：**字跟着档走**（原版三处都是点一下就把 term 重设一遍）
                if (i < _cardTexts.Count && _cardTexts[i] != null)
                    _cardTexts[i].SetText(CardBtnWord(m));
            }
        }

        /// <summary>
        /// 处理一次点击。返回 true = 这次点击被面板吃掉了（别再传给棋盘/手牌）。
        /// ⚠️ 和设置/日志面板一样：**面板开着时点哪儿都先经过这里**。
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

            // 点卡本身也能标记/取消（原版是卡片上的按钮；我们两条路都留着，**点卡这条路是我们加的**）
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null && _cards[i].Contains(world)) { ToggleMark(i); return true; }

            return true;      // 面板开着 → 点其它地方也吃掉（原版 `Shade.SwitchShade` 就是这个意思）
        }

        // ==================================================================
        //  自检
        // ==================================================================

        public int CardButtonCount { get { return _cardBtns.Count; } }
        /// <summary>自检用（**A276**）：第 i 张牌那个「换」按钮写进去的**局部 z**（没有 ⇒ `NaN`）。
        /// 判据 = 它只由本面板的 `Z` 决定，**不许跟卡的世界 z 走**（同父的 `_shade`/`_prompt` 都是裸局部 z）。</summary>
        public float CardButtonLocalZ(int i)
        {
            if (i < 0 || i >= _cardBtns.Count || _cardBtns[i] == null) return float.NaN;
            return _cardBtns[i].transform.localPosition.z;
        }
        /// <summary>提示行现在写着什么（自检用）。
        /// 🔴 **A245：这一行没建出来时返回 `null`**（原来返 `"&lt;无>"`）—— `"&lt;无>"` 是**非空串**，
        ///    而 `Editor/BattleScene.cs` 那条断的是 `!string.IsNullOrEmpty(...)` ⇒ 两态（建了 / 没建）
        ///    **分不开**、那条断言等于没查。返 `null` 之后两种状态才判得出来。
        /// ⚠️ 今天**不可达**（`Label.Create` 恒不返回 null ⇒ 这条是**潜在**缺口、不是现患）；
        ///    改的是「让断言有判别力」，不是修一个正在发生的错。
        /// ⚠️ 同族的 `TurnText` / `DoneText` 一起改成 `null`（它们的断言是 `==` 字面量，
        ///    两态本来就分得开，改只是为了**同族一个口径**）；`Describe()` 那边自己补占位符。</summary>
        public string PromptText { get { return _prompt != null ? _prompt.Text : null; } }
        public bool BarHasArt { get { return _bar != null && _bar.Texture != null; } }

        /// <summary>底条的**渲染尺寸**（世界单位）—— 自检用它钉住「原版 PA=0 拉满 577.5×63.84」那条。
        /// 🔴 2026-09-27：`ImageQuad` 默认**按贴图比例定宽**（`_aspect = tex.w/tex.h`），
        ///   而这条底原版是 `PA=0` 硬拉伸 ⇒ 补 `SetAspect` 之前我们只画出 **372.8** 宽（窄 204.7px）。
        ///   量的是渲染尺寸，不是「框」—— 框一直都是对的，错的是往里画多大。</summary>
        public float BarWorldW { get { return _bar != null ? _bar.WorldW : 0f; } }
        public float BarWorldH { get { return _bar != null ? _bar.WorldH : 0f; } }
        public bool PlayHasArt { get { return _play != null && _play.Texture != null; } }
        public bool EyeHasArt { get { return _eye != null && _eye.Texture != null; } }

        /// <summary>卡片上的「换」按钮现在露着没有（眼睛那颗钮的开关状态）</summary>
        public bool CardButtonsShown
        {
            get { return _cardBtns.Count > 0 && _cardBtns[0] != null && _cardBtns[0].gameObject.activeSelf; }
        }

        /// <summary>压暗层还盖着没有。原版点「眼睛」时它跟按钮**一起**收起（`Shade.SwitchShade`）</summary>
        public bool ShadeActive { get { return _shade != null && _shade.gameObject.activeSelf; } }
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
        public string Describe()
        {
            return $"换牌面板 开着={Visible} 卡按钮 {_cardBtns.Count} 个 标记 [{string.Join(",", Marked.ConvertAll(x => x.ToString()).ToArray())}]"
                 + $" 提示「{PromptText ?? "<无>"}」";      // 占位符挪到**打印这一侧**（A245：getter 返 null 才分得开两态）
        }
    }
}
