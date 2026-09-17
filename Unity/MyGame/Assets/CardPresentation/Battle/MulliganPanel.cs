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

        /// <summary>自检用：那颗钮上现在写的是什么</summary>
        public string DoneText { get { return _doneText != null ? _doneText.Text : "<无>"; } }

        public bool Visible { get; private set; }

        /// <summary>点「完成换牌」→ 回调「要换掉的手牌下标」（可能为空 = 不换）</summary>
        public Action<List<int>> OnDone;

        ImageQuad _shade;
        ImageQuad _bar, _play, _eye;      // Continue 的底 / 圆形播放钮 / 眼睛
        Label _prompt, _doneText;
        readonly List<ImageQuad> _cardBtns = new List<ImageQuad>();
        readonly List<Label> _cardTexts = new List<Label>();
        readonly List<CardView> _cards = new List<CardView>();
        readonly List<int> _marked = new List<int>();

        // ---- 原版绝对坐标（1920×1080，y 从**上**）----
        const float PromptCx = 967f, PromptCy = 106.5f, PromptW = 1344f, PromptH = 79.4f;
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
            var go = new GameObject("MulliganPanel");
            go.transform.SetParent(parent, false);
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
            p._prompt = Label.Create(go.transform, "选择要换掉的牌（回车 = 完成）", At(PromptCx, PromptCy), 8,
                                     new Color(1f, 0.94f, 0.82f), new Vector2(0.5f, 0.5f), "MulliganPrompt");
            if (p._prompt != null) p._prompt.SetCapHeight(U(PromptH * 0.55f));

            // 完成按钮：底图 `40k_bt_underbutton`（原版 577.5×63.8）+ 圆形播放钮 `40k_UI_bt_play`
            p._bar = ImageQuad.Create(go.transform, CardArt.Ui("40k_bt_underbutton"), At(BarCx, BarCy),
                                      U(BarH), new Vector2(0.5f, 0.5f), "MulliganContinueBar");
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
                var pos = c.transform.position + new Vector3(0f, -cardH * 0.30f, Z - c.transform.position.z);

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
        public string PromptText { get { return _prompt != null ? _prompt.Text : "<无>"; } }
        public bool BarHasArt { get { return _bar != null && _bar.Texture != null; } }
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
                 + $" 提示「{PromptText}」";
        }
    }
}
