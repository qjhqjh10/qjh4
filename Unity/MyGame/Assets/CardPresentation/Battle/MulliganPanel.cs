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
        /// <summary>「完成换牌」那颗钮上的默认文字。
        /// ⚠️ **文案是我们起的**（原版是 I2 词条 `Battle/Mulligan/ButtonDone`，词条内容本地没有）。
        /// 倒计时进最后 10 秒时会把它换成剩余秒数（原版 `MulliganManager.SetMulliganTimer`，见 `BattleDriver.TickMulligan`）</summary>
        public const string DoneLabel = "完成换牌";

        /// <summary>把「完成换牌」那颗钮上的字换掉（原版 `MulliganManager.mulliganButtonText`）。
        /// 原版倒计时 `<10` 秒时**每秒**刷一次这个字（`MulliganCountdown` → `SetMulliganTimer`）。</summary>
        public void SetDoneText(string s) { if (_doneText != null) _doneText.SetText(s); }

        /// <summary>自检用：那颗钮上现在写的是什么。
        /// 🔴 **A245：没建出来时返回 `null`**（原来返 `"<无>"`）—— 同族一个口径，见 `PromptText`。</summary>
        public string DoneText { get { return _doneText != null ? _doneText.Text : null; } }

        /// <summary>「你先手 / 你后手」那一行现在显示什么（自检用）。
        /// 🔴 **A245：没建出来时返回 `null`**（原来返 `"<无>"`）—— 同族一个口径，见 `PromptText`。</summary>
        public string TurnText { get { return _turnText != null ? _turnText.Text : null; } }

        /// <summary>这一行那个 label（**自检要量它的实际位置** —— 判据是原版 `TurnText` 的 rect，不是我们的常量）。</summary>
        public Label TurnLabel { get { return _turnText; } }

        /// <summary>提示行那个 label（**自检要量它的颜色** —— 判据 = 原版 `m_fontColor32 = 4294967295` 纯白）。</summary>
        public Label PromptLabel { get { return _prompt; } }

        /// <summary>按「这一局谁先手」设那一行。<paramref name="playerGoesSecond"/> = 我方是后手。
        /// 判据 → `资料/加时与冲突模式_原版规格.md` §2.8（原版 `MulliganManager.ActivateMulligan` 的二选一）。</summary>
        public void SetTurnText(bool playerGoesSecond)
        {
            if (_turnText != null) _turnText.SetText(playerGoesSecond ? TurnSecond : TurnFirst);
        }

        /// <summary>后手那句照原版英文兜底 `"You go second"` 译；先手那句**原版英文没查到**（只有词条名 `GoFirst`）⇒ 我们译的。</summary>
        public const string TurnFirst = "你先手";
        public const string TurnSecond = "你后手";

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
        /// 🔴 **2026-09-29 取到真 rect、已照它改**：原版 `TurnText` = **左上 (312.50, 129.42) · 1307.06×54.17**
        /// （父 `MulliganText` = 左上 (295.00, 66.78) · 1344×79.44 ⇒ 中心 (967, 156.5)）。
        /// 出处：`bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3403.json`（`TurnText`）
        /// + `_2828.json`（父）—— **序列化在场景里**，运行期 dump 逐位相同、**不在任何 clip/DOTween 里**
        /// （全包 grep `Animation/Animator/CanvasGroup` 零命中，`MulliganText` 上连脚本组件都没有）。
        /// ⚠️ **原来记的「RT 没取到、怀疑在预制体那边」是错的** —— RT 就在那个场景包里，只是没被名字索引到。
        /// 原来我们自己放的那一版中心在 y=178，**比原版低 21.5 px、还高了 5.8 px**。
        /// 字号：原版 TMP `m_fontSize = **55**`（auto 18–55，Center/Middle）—— 用 `SetGlyphHeight(55px)` 表达
        /// （`MenuDraw.Text` 传的 `fontPx` 就是这一个口；`SetCapHeight` 那半高写法会偏小）。
        /// ⚠️ **已知未改**：原版这两行 `m_fontColor32` 都是**纯白**，我们用的是暖色 (1,0.94,0.82)（提示行也是）——
        /// 两行一致，暂不动，等并排看时再定。</summary>
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
            // ⚠️ 文案是我们起的（I2 词条本地没有）；字号按那块框的高度取的 —— **也是我们挑的**。
            // 「回车」那半句是我们加的兜底（原版只有按钮）—— 换牌卡在开局之前，点不动就开不了局
            // 🔴 **颜色照原版（2026-09-30 亲读原版资产）**：`bundle_scenes_scenes_battlearena1/MonoBehaviour/`
            //   `MonoBehaviour_3731.json`（`Choose cards to replace in first hand`）与 `MonoBehaviour_3856.json`
            //   （`You go second`）的 **`m_fontColor32` 都是 `4294967295`（= `0xFFFFFFFF` 纯白）**、
            //   `m_fontColor` 都是 `(1,1,1,1)`。原来两行都用暖色 `(1, 0.94, 0.82)` ⇒ **改成纯白**。
            //   （旁证：`资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md:274-275` 记「白」。）
            p._prompt = Label.Create(go.transform, "选择要换掉的牌（回车 = 完成）", At(PromptCx, PromptCy), 8,
                                     Color.white, new Vector2(0.5f, 0.5f), "MulliganPrompt");
            if (p._prompt != null) p._prompt.SetCapHeight(U(PromptH * 0.55f));

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
            if (p._turnText != null) p._turnText.SetGlyphHeight(U(TurnPx));   // 原版 `m_fontSize = 55`（em 的像素值）

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
            p._doneText = Label.Create(go.transform, DoneLabel, At(DoneCx, DoneCy), 6,
                                       new Color(1f, 0.92f, 0.75f), new Vector2(0.5f, 0.5f), "MulliganDoneText");
            if (p._doneText != null) p._doneText.SetCapHeight(U(DoneH * 0.45f));

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

                var t = Label.Create(transform, "换", pos, 5, new Color(1f, 0.9f, 0.7f),
                                     new Vector2(0.5f, 0.5f), "MulliganBtnText_" + i);
                if (t != null)
                {
                    t.SetCapHeight(U(28f));
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

        /// <summary>标记的样子：卡**置灰**（`Unplayable` 那一档）+ 按钮换成按下态那张图</summary>
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
        /// 🔴 **A245：这一行没建出来时返回 `null`**（原来返 `"<无>"`）—— `"<无>"` 是**非空串**，
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
