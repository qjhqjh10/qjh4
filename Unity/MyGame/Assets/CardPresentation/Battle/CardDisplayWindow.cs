// CardDisplayWindow.cs — 卡牌放大展示窗（原版 `CardDisplayWindow`）· **战斗里的那处摆放**
//
// 原版出处：`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` 第一节 + 运行时 dump
//   `BattlePrefab/.../FrontCanvas/Safe area FrontCanvas/Card Display Window`（**activeSelf=False**，平时关着）
//   + 场景 JSON `bundle_scenes_scenes_battlearena1/`（2026-09-28 逐节点复读）。
//   · 遮罩 `Menu Dark Background`  4574.6×2572.4，`color = (0,0,0,0.773)`
//   · `Card Display`               752×868，屏幕正中（x[584,1336] y[106,974]）
//   · `Card Display/Cards/CardUI (0..4)` —— **5 个卡槽**（脚本字段 `cardUIs[5]`）
//   · `Card Display/LowerSection`  y 921.34~1058.34：`FlavourTextBG`(1320×178) → `LoreText`
//     (1250×93.47 · fs32 · 居中) + `Voice Over Button`(88.655² @1654.7→1743.4)
//     —— **就这两件，没有第三件**（`Show Card Text` 那颗钮是**菜单版**才有的，见下面 A860）
//
// 🔴 **2026-10-17（A860）：战斗版比菜单版少两样，逐字段核过 —— 别照菜单版补**。
//   判据（13 个竞技场**逐份实读**，`bundle_scenes_scenes_battlearena{1,2,3,aeldari,astramilitarum,
//   blacklegion,darkangels,emperorschildren,genestealers,leviathan,sororitas,spacewolves,tauviorla}`）：
//   · **`options`（`CardDisplayOptions`）= `{m_FileID:0, m_PathID:0}` = null —— 13/13 全都是 0**；
//     `informationPanel` 同样 13/13 为 0（菜单版那颗是 `2331` / `2206`）。
//     `ShowCard.c:153-167` 的 `else` 支（`showOptions == true` 才走）**那句守卫就是**
//     `if (options == null || card.field_0x4c != 0) goto LAB_1807fbe68`（`:159`）⇒ **战斗侧那块面板永远不会出现**
//     （`ShowBattleCard` 传进来的 `showOptions = DisplayCardEffects(...)` 于战斗侧是**空转**）。
//     节点树也一致：本窗 `Card Display` 的孩子只有 4 个（`LowerSection` / `Cards` / `TutorialObjs` /
//     `EffectList`），**没有** `CardDisplayOptions` 的宿主，也没有 `Panel`（三块面板）、
//     `Wildcard Segment`、`Card Counter` —— 那四件挂在**菜单版**的 `CardDisplayer Menu For Menu` 上。
//   · **`showCardTextButton`（眼睛钮）= `{m_PathID:0}` = null —— 13/13 全都是 0**。
//     `ShowCard.c:85-96` / `CardSwapFinished.c:20-45` / `Open.c:72-83` 三处**都拿 `!= null` 把着**，
//     `Open` 那个 `AddListener(→ ToggleCardState)` 也不接 ⇒ **战斗侧点不到、也画不出这颗钮**。
//     节点面复核：全 `assets_full` 里 `Show Card Text` 这个 GameObject **只有 1 份**
//     （`bundle_scenes_scenes_mainmenuwarpforge`）；13 个竞技场 `GameObject/` 目录里
//     `*Text*` 名字全表**零命中**。`LowerSection` 的 `RectTransform` 也实读到**只有 2 个孩子**
//     （`FlavourTextBG` 1320×178 + `Voice Over Button` 88.655² @ x 734.33）—— 菜单那份是 **3 个**。
//     ⚠️ **别拿图标认钮**：眼睛那张图 `-7255197835733746773` 在竞技场里**是有的**，但它挂在
//     `ChooseCardMenu` 的开关钮上（`MonoBehaviour_4813` → `m_TargetGraphic` = `_4921`，一个 `Image`）
//     —— 同 `40K_melee_glow` 那次的教训（铁律 3）。
//   ⇒ 本窗**不建** `Show Card Text`。2026-10-17 那一轮先删了钮、留了三个桩给白名单外的调用点；
//     **2026-10-17（B8）三个桩（`HitEye` / `ToggleLore` / `LoreVisible`）连同最后一个调用点
//     （`Battle/BattleDriver.cs` 的 `HandleDisplayWindowClick`）已一并删干净** —— 全仓零残留。
//     📌 留下的知识（那颗钮**菜单版**才有的真语义）：`CardDisplayWindow.ToggleCardState` 翻的是
//     `showCardInfo` 并逐格调 `CardTextsController.ChangeState / ChangeToInitialState`，
//     ⚠️ **不是**「切整块 `FlavourTextBG`」；战斗版这一块「露不露」现在只看**有没有字**（见 `RefreshLore`）。
//     ⛔ **别把它建回来** —— `Editor/BattleScene.cs` 有断言钉着（节点名 / 那一格不吃点击）。
//
// 🔴 **2026-09-28 两条订正**（判据全文 → `资料/阶段二_卡片详情窗_原版规格.md` §十）：
//   ① **卡体 743 px 是错的** —— 那是**另一棵树**的尺寸：多卡展示窗
//      `Generic Multi Card Display Combat/Viewport/Content/CardUI Reference` 的 `m_LocalScale = 223.14`
//      （`RectTransform_3421.json`）。**本窗** 5 个槽的 `m_LocalScale` **全是 250**
//      （`RectTransform_2874/2772/2908/3170/2663.json`）、`Cards`→`Card Display`→`Safe area FrontCanvas`
//      一路父级 scale 都是 1 ⇒ **本窗卡体 = 523.175×832.825 px**，**与菜单版同一个尺寸**。
//   ② **原版是同一个脚本、两处摆放**（正本 §〇·2）⇒ 两边共用 `CardFan`（扇形位姿/分层/着色）
//      与 `CardWinBox`（矩形），**判据只此一份**，不再各写一套。
//
// **怎么打开**（原版行为，两条链 —— 2026-09-28 补上棋盘那条）：
//   · **手牌**：反编译 `BasicCardUI.ToggleOpenCardDisplayOnTouch` —— 轻点卡牌就开关。
//     我们照这个做：手牌**按下→松开且几乎没移动**（`CardInteraction.TapThreshold`）就开关一次。
//   · **棋盘上的单位**（我方与对手**都给开**）：`CardCollider3D.OnPointerClick` →
//     `CardScript.OnTouchUpAsButton` 的 **C 段**（state 2/3/0x11）→ `BattleManager.DisplayCard`
//     → 本窗的 `ShowBattleCard`（**那条链里就调 `DisplayCardEffects`** ⇒ 有 buff 就出那块 `EffectList`）。
//     ⚠️ 棋盘上**拖拽**是另一件事（弹攻击三选一），见 `BattleDriver.BoardPress` 的注释。
//     判据全文 → `资料/待办判据_战场与战斗视图.md` §8b。
// **怎么关**（🆕 **2026-09-29 三条都接上了**；原来只做「再轻点同一张牌」，另外两条记的是「我们没接」）：
//   ① **再轻点同一张牌**（手牌 / 棋盘单位都走这一条）。
//   ② **点遮罩空白**（原版 `BackgroundCloseButton.OnPointerClick` → `CardDisplayWindow.OnBackgroundClick`
//      → `Close`，链有直证）—— 落点 = `BattleDriver.HandleDisplayWindowClick` 的最后那一支。
//   ③ **指针移开**（原版 `CardCollider.OnPointerExit` → `CardScript.OnTouchExit`，唯一守卫是
//      `displayingCardFlag`）—— 落点 = `BattleDriver.Update` 里那条每帧判据。
//      🔴 **这一条我们按实情收了口径**：守卫多加了「窗的地界」（`ContainsPointer`），
//      否则窗里的语音/眼睛钮永远点不到。**待实机核**（触屏是不是「抬手即关」产物里证实不了 ——
//      `StandaloneInputModule` 没反编译）。判据全文 → `资料/待办判据_战场与战斗视图.md` §8b。
using System.Collections.Generic;
using DG.Tweening;          // `OnComplete` 是它的扩展方法（换位收尾那一步）
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    public class CardDisplayWindow : MonoBehaviour
    {
        /// <summary>整块窗放到 HUD **前面**（相机看 +Z，z 越小越近）。
        /// 比结算面板（−0.5）还靠前 —— 展示窗是「盖在所有东西上面」的那一层。</summary>
        const float ZMask = -2.0f, ZContent = -2.1f;
        /// <summary>卡格之间的 z 阶梯（**只是兜底**：真正决定前后的是渲染队列，见 `CardFan.QueueFor`）。</summary>
        const float ZSlotStep = 0.02f;
        /// <summary>遮罩 / 文字 / 按钮那一档队列（**低于卡格的 `CardFan.QCardBase`** ⇒ 卡永远画在它们之上）。</summary>
        const int QChrome = 3000;

        Transform _root, _cards, _lower, _bg;
        ImageQuad _mask, _voiceBtn;
        Label _lore, _hint;
        AudioSource _voiceAudio;

        // 「谁给我加的 buff」那一竖列（原版 `Card Display/EffectList`）—— **建一次**，之后只切 activeSelf + 改字
        Transform _fxRoot;
        Label _fxTitle;
        ImageQuad[] _fxBg;
        Label[] _fxWho, _fxWhat;
        Vector3 _fxTitleBase;
        Vector3[] _fxWhoBase, _fxWhatBase;

        /// <summary>每个**位姿槽**上的卡（下标 0 = 前台）。判据与真值 → `CardFan`。</summary>
        public readonly CardView[] SlotViews = new CardView[CardFan.Slots];
        readonly CardDef[] _slotDefs = new CardDef[CardFan.Slots];
        bool _swapping;

        /// <summary>正在展示的那张卡（`CardData` 是 struct，不能拿 null 当「没有」⇒ 单独一个 bool）</summary>
        CardData _shown;
        bool _hasShown;

        /// <summary>窗是不是开着（自检用）</summary>
        public bool Visible { get; private set; }

        /// <summary>「谁给我加的 buff」现在露着几行（原版最多 5 槽；自检用）</summary>
        public int EffectRowCount { get; private set; }
        /// <summary>第 `i` 行「**谁给的**」（自检用）</summary>
        public string EffectWho(int i) { return (_fxWho != null && i >= 0 && i < _fxWho.Length && _fxWho[i] != null) ? _fxWho[i].Text : null; }
        /// <summary>第 `i` 行「**给了什么**」（自检用）</summary>
        public string EffectWhat(int i) { return (_fxWhat != null && i >= 0 && i < _fxWhat.Length && _fxWhat[i] != null) ? _fxWhat[i].Text : null; }

        /// <summary>第 `i` 行的**文字在底板前面吗**（z 更小 = 离相机更近；自检用）。
        /// 🔴 这条是 2026-09-28 踩出来的：`PlaceLeft` 拿「`SetZ` **之前**」的 `basePos` 重设位置，
        ///    每次改字都把 z 拨回 ≈0 ⇒ 被 −2.10 的底板压住 —— **图上只表现为「字暗一点」**
        ///    （量像素才看得出：白字峰值 190 而不是 255），**当时 940+ 条断言全绿**。
        ///    ⇒ 给它一条断言，别再靠眼睛。</summary>
        public bool EffectTextInFrontOfBg(int i)
        {
            if (_fxWho == null || i < 0 || i >= _fxWho.Length || _fxWho[i] == null) return false;
            float tz = _fxWho[i].transform.position.z;
            float bz = (_fxBg != null && i < _fxBg.Length && _fxBg[i] != null) ? _fxBg[i].transform.position.z : float.MaxValue;
            return tz < bz;
        }

        /// <summary>第 `i` 行「谁给的」那行字的**左缘**（屏幕 px · 自检用）。
        /// 🔴 左对齐是原版口径（`m_HorizontalAlignment = 1`），而「居中」这个 bug **肉眼才能发现** ——
        /// 所以给它一条断言：**短句与长句的左缘必须落在同一个 x**（都贴框左缘）。</summary>
        public float EffectWhoLeftPx(int i) { return LeftPx(_fxWho, i); }
        /// <summary>第 `i` 行「给了什么」那行字的左缘（屏幕 px）。</summary>
        public float EffectWhatLeftPx(int i) { return LeftPx(_fxWhat, i); }
        static float LeftPx(Label[] a, int i)
        {
            if (a == null || i < 0 || i >= a.Length || a[i] == null) return float.NaN;
            var lb = a[i];
            float wPx = LayoutSpace.PxX(lb.WorldW) - LayoutSpace.PxX(0f);
            return LayoutSpace.PxX(lb.transform.position.x) - wPx * 0.5f;   // 块左缘 = 中心 − 半个块宽
        }
        /// <summary>位姿槽上有几张卡（= 1 主卡 + 相关卡数，最多 `CardFan.Slots`；自检用）</summary>
        public int SlotCount { get; private set; }
        /// <summary>**前台那张**（永远是位姿槽 0）—— 原版 `cardInFront`。</summary>
        public CardView Card { get { return SlotViews[0]; } }
        /// <summary>前台那张的卡表项（没拿到 `CardDef` 时是 null）。</summary>
        public CardDef FrontDef { get { return _slotDefs[0]; } }
        /// <summary>换位动画在播没有（= 原版那个 `swappingCards` 闸）。</summary>
        public bool IsSwapping { get { return _swapping; } }
        /// <summary>位姿槽 `i` 上的卡（越界返回 null）。</summary>
        public CardView SlotView(int i) { return (i >= 0 && i < CardFan.Slots) ? SlotViews[i] : null; }
        /// <summary>位姿槽 `i` 上那张卡的卡表项（越界返回 null）。</summary>
        public CardDef SlotDef(int i) { return (i >= 0 && i < CardFan.Slots) ? _slotDefs[i] : null; }

        /// <summary>窗口上那行字（自检用）</summary>
        public string ShownTitle { get; private set; }
        /// <summary>窗口下面那段正文（自检用）</summary>
        public string ShownBody { get; private set; }
        /// <summary>语音按钮上一次播的是哪个文件（**没播成是 null**；自检用）</summary>
        public string LastVoiceFile { get; private set; }

        /// <summary>只有**前台那张**吃 tooltip（原版 `InitializeCardForDisplay` 的
        /// `Card2DController.ToggleTooltips(index == 0)`）—— 外面问这一条，不要自己数槽位。</summary>
        public static bool SlotHasTooltips(int i) { return i == 0; }

        public static CardDisplayWindow Create(Transform parent)
        {
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名 prefab `Card Display Window` 实读：`RectTransform` · `anchor (0,0)-(1,1)` ·
            //    `sizeDelta (0,0)` ⇒ **绝对矩形 (0,0)-(1920,1080)**（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读；它平时 `activeSelf=False`）⇒ 整屏矩形。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「卡牌展示窗根 = 整屏矩形」红。
            var go = new GameObject("Card Display Window", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            var w = go.AddComponent<CardDisplayWindow>();
            w.Build();
            w.Hide();
            return w;
        }

        void Build()
        {
            _root = transform;

            // 遮罩：**盖满整个可见区**（原版那张是 4574.6×2572.36，两倍屏还多，就是「铺满带余量」）。
            // ⚠️ 遮罩与提示行**建一次就留着**，开关只切 activeSelf —— 每次都重建的话，
            //    `Hide` 里把它们销毁了、`Show` 又不会重建，第二次打开就只剩一张光卡（踩过）。
            _mask = MenuDraw.Rect(_root, CardArt.Solid(),
                                  new PxRect(CardWinBox.MaskL, CardWinBox.MaskT, CardWinBox.MaskR, CardWinBox.MaskB),
                                  "Menu Dark Background", QChrome, CardWinBox.ShadeColor);
            SetZ(_mask, ZMask);

            // 🔴 **2026-10-18（W12 · W6 顺手核）：「这行是我们自加的」成立，判据补齐** ——
            //    原版 `Card Display Window` 子树里**没有**「再点一下关闭」这样的节点
            //    （2026-10-18 逐节点走了一遍 `battlearena1` 那棵树；`Battle/` 那 93 条代码字面量里
            //     也没有对应的键，见 `d:/2/tools/il2cpp_out/stringliteral.json`）。
            //    ⇒ 铁律 11 例外①（原版没有）⇒ **保留中文、不改**，只把判据落在这里。
            //    ⚠️ 代价如实记：**英文档下这行会露中文**（没有原版键可接）。
            _hint = Label.Create(_root, "再点一下关闭", EndPanel.Pos(960f, 1064f, ZContent), 2,
                                 new Color(0.7f, 0.7f, 0.75f), new Vector2(0.5f, 0.5f), "cdw_hint");
            if (_hint != null) _hint.SetRenderQueue(QChrome);

            // 结构照原版那棵树：`Card Display` → { `Cards`(5 个卡槽), `LowerSection` → `FlavourTextBG` → `LoreText` }
            var cardDisplay = MenuDraw.Node(_root, "Card Display",
                                            new PxRect(CardWinBox.CardL, CardWinBox.CardT, CardWinBox.CardR, CardWinBox.CardB));
            _cards = MenuDraw.Node(cardDisplay, "Cards",
                                   new PxRect(CardWinBox.CardsCx - CardWinBox.CardsS * 0.5f, CardWinBox.CardsCy - CardWinBox.CardsS * 0.5f,
                                              CardWinBox.CardsCx + CardWinBox.CardsS * 0.5f, CardWinBox.CardsCy + CardWinBox.CardsS * 0.5f));
            _lower = MenuDraw.Node(cardDisplay, "LowerSection",
                                   new PxRect(CardWinBox.LowerL, CardWinBox.LowerT, CardWinBox.LowerR, CardWinBox.LowerB));
            _bg = MenuDraw.Node(_lower, "FlavourTextBG",
                                new PxRect(CardWinBox.LowerL, CardWinBox.LoreBgT, CardWinBox.LowerR, CardWinBox.LoreBgB));

            // 一颗圆钮：语音（图在工程里才建）。**只这一颗** —— 眼睛钮（`Show Card Text`）战斗版原版就没有
            // （`showCardTextButton = {m_PathID:0}`，13/13；节点也零命中），2026-10-17 删掉，见文件头 A860。
            float vx = CardWinBox.VoiceCx - CardWinBox.BtnS * 0.5f;
            float by = CardWinBox.BtnCy - CardWinBox.BtnS * 0.5f;
            _voiceBtn = MenuDraw.Rect(_lower, CardArt.Ui("40k_UI_bt_voicelines"),
                                      new PxRect(vx, by, vx + CardWinBox.BtnS, by + CardWinBox.BtnS),
                                      "Voice Over Button", QChrome, null, true);
            if (CardArt.Ui("40k_UI_bt_voicelines") == null)
                Debug.Log("[展示窗] `40k_UI_bt_voicelines` 不在 `Resources/Art/ui/` ⇒ **语音钮不建**（不摆白方块）");

            _voiceAudio = gameObject.AddComponent<AudioSource>();
            _voiceAudio.playOnAwake = false;
            _voiceAudio.spatialBlend = 0f;          // 2D —— 原版这个 source 也挂在 UI 上
            _voiceAudio.volume = 0.85f;             // ⚠️ 这个系数是**我们自己定的**（原版逐 source 的音量没查到）
            // 🆕 2026-09-19：挂到 `Voices` 通道 ⇒ 受设置面板那根「语音」滑块控制
            _voiceAudio.outputAudioMixerGroup = WarpforgeAudio.VoicesGroup;

            SetChrome(false);
            BuildEffectList();
        }

        // ============================================================ 「谁给我加的 buff」那一竖列
        //
        // 原版：`Card Display/EffectList` —— **只有这扇窗有**（卡的 prefab 上没有这棵树），出厂关着，
        // 整组**当且仅当**「≥1 条 effect 的文字非空」时 `SetActive(true)`（`DisplayCardEffects`，唯一写者）。
        // ⚠️ **建一次就留着、开关只切 activeSelf** —— 同遮罩那条教训：`Hide` 里销毁了、`Show` 又不会重建，
        //    第二次打开就只剩一张光卡（那次踩过）。
        void BuildEffectList()
        {
            _fxRoot = MenuDraw.Node(_root, "EffectList",
                                    new PxRect(CardWinBox.EffL, CardWinBox.EffRowCy[0] - CardWinBox.EffRowH * 0.5f,
                                               CardWinBox.EffR,
                                               CardWinBox.EffRowCy[CardWinBox.EffSlots - 1] + CardWinBox.EffRowH * 0.5f));

            var tr = new PxRect(CardWinBox.EffTitleCx - CardWinBox.EffTitleW * 0.5f,
                                CardWinBox.EffTitleCy - CardWinBox.EffTitleH * 0.5f,
                                CardWinBox.EffTitleCx + CardWinBox.EffTitleW * 0.5f,
                                CardWinBox.EffTitleCy + CardWinBox.EffTitleH * 0.5f);
            _fxTitle = MenuDraw.Text(_fxRoot, tr, TitleText, Color.white, "AffectedBy",
                                     CardWinBox.EffTitlePx, CardWinBox.QEffect, tr.W, 19.3f, FxTitleAutoMax);
            if (_fxTitle != null)
            {
                _fxTitle.SetAlignLeft();
                SetZ(_fxTitle.transform, ZContent - 0.02f);
                // 🔴 **`basePos` 必须在 `SetZ` 之后取** —— 见 `PlaceLeft` 的注释（取早了会把 z 拨回去）
                _fxTitleBase = _fxTitle.transform.localPosition;
                PlaceLeft(_fxTitle, _fxTitleBase, CardWinBox.EffTitleW);
            }

            _fxBg = new ImageQuad[CardWinBox.EffSlots];
            _fxWho = new Label[CardWinBox.EffSlots];
            _fxWhat = new Label[CardWinBox.EffSlots];
            _fxWhoBase = new Vector3[CardWinBox.EffSlots];
            _fxWhatBase = new Vector3[CardWinBox.EffSlots];
            var bgTex = CardArt.Ui("40K_display");
            if (bgTex == null)
                Debug.LogWarning("[展示窗] `40K_display` 不在 `Resources/Art/ui/` ⇒ 效果清单**只画字、不画底板**（不摆白方块）");
            float wl = CardWinBox.EffCx - CardWinBox.EffWhoW * 0.5f, wr = CardWinBox.EffCx + CardWinBox.EffWhoW * 0.5f;
            for (int i = 0; i < CardWinBox.EffSlots; i++)
            {
                float cy = CardWinBox.EffRowCy[i];
                if (bgTex != null)
                {
                    _fxBg[i] = MenuDraw.Rect(_fxRoot, bgTex,
                        new PxRect(CardWinBox.EffCx - CardWinBox.EffBgW * 0.5f, cy - CardWinBox.EffBgH * 0.5f,
                                   CardWinBox.EffCx + CardWinBox.EffBgW * 0.5f, cy + CardWinBox.EffBgH * 0.5f),
                        "EffectBg", CardWinBox.QEffect);
                    SetZ(_fxBg[i], ZContent);
                }
                float wcy = cy + CardWinBox.EffWhoDy, tcy = cy + CardWinBox.EffWhatDy;
                // ⚠️ 文字比底板**再近 0.02** —— 同一个渲染队列里靠「到相机的距离」排序，
                //    同 z 的话谁压谁是**不确定的**（底板可能盖住字）。
                var whoR = new PxRect(wl, wcy - CardWinBox.EffWhoH * 0.5f, wr, wcy + CardWinBox.EffWhoH * 0.5f);
                _fxWho[i] = MenuDraw.Text(_fxRoot, whoR, "", Color.white, "EnchanterText",
                                          CardWinBox.EffTextPx, CardWinBox.QEffect, whoR.W, 10f, FxTextAutoMax);
                if (_fxWho[i] != null)
                {
                    _fxWho[i].SetAlignLeft();
                    // 🆕 **2026-10-16（A712 阶段 2 的尾巴 · W23 给的修法 · 主对话落）**：
                    //   原版这一颗的 `m_VerticalAlignment = 1024`（= `Bottom`），框 467.8×38.1（单行）。
                    //   ⚠️ **如实标注**：本件 `MenuDraw.Text(...)` **传了 `wrapPx`（= `whoR.W`，折行开着）** ⇒
                    //   长名字会折行；折行时 `Bottom` 的目标按**块高**折到末行（`Label.VOffsetBlockWorld` 管这条，
                    //   W25 2026-10-16 补），但**「我们折行后的形状」与「原版那一颗单行」是不是同一个形状没逐字核过** ——
                    //   原版那格的框宽 467.8 与我们的 `whoR` 宽度也**没逐字比对**。照原版落 `Bottom`，形状差异如实记。
                    _fxWho[i].SetVAlign(Label.VAlign.Bottom, LayoutSpace.Px(CardWinBox.EffWhoH));
                    SetZ(_fxWho[i].transform, ZContent - 0.02f);
                    _fxWhoBase[i] = _fxWho[i].transform.localPosition;   // ⚠️ 在 SetZ **之后**取（见 `PlaceLeft`）
                }
                var whatR = new PxRect(wl, tcy - CardWinBox.EffWhatH * 0.5f, wr, tcy + CardWinBox.EffWhatH * 0.5f);
                _fxWhat[i] = MenuDraw.Text(_fxRoot, whatR, "", Color.white, "EffectText",
                                           CardWinBox.EffTextPx, CardWinBox.QEffect, whatR.W, 10f, FxTextAutoMax);
                if (_fxWhat[i] != null)
                {
                    _fxWhat[i].SetAlignLeft();
                    SetZ(_fxWhat[i].transform, ZContent - 0.02f);
                    _fxWhatBase[i] = _fxWhat[i].transform.localPosition;   // ⚠️ 同上：在 SetZ **之后**取
                }
            }
            SetEffectRows(null);              // 出厂关着（原版 `m_IsActive = false`）
        }

        /// <summary>🔴 **A406（2026-10-12）：本窗四处 TMP 的 `m_fontSizeMax` / `m_fontSizeBase` 逐颗实读** ——
        /// 判据 = 节点本体（`bundle_scenes_scenes_battlearena1` 的 `GameObject/{AffectedBy,EnchanterText,
        /// EffectText,LoreText}.json` → 它 `m_Component` 里那颗 `TextMeshProUGUI`）：
        /// ```text
        /// AffectedBy    fs=0.4   min=0.2  max=0.4  base=36.0  ← 局部单位；×0.8854167×108.79123 = 画布 px
        /// EnchanterText fs=0.3   min=0.1  max=0.3  base=36.0
        /// EffectText    fs=0.3   min=0.1  max=0.3  base=36.0
        /// LoreText      fs=32.0  min=18.0 max=32.0 base=36.0  ← 已是画布 px（父链 scale = 1）
        /// ```
        /// ⇒ 上限（画布 px）= `AffectedBy` **38.5**（= `CardWinBox.EffTitlePx`）·
        ///   两个 `Effect*` **32.6**（= `CardWinBox.EffTextPx`）· `LoreText` **32.0**
        ///   —— ⚠️ **`LoreText` 是这一批唯一一处「上限 ≠ 我们原来传的标称」**：
        ///   `CardWinBox.LorePx = 35` 是**菜单版** `Card Detail Popup` 那颗的值（那边 `auto[10~35]`），
        ///   而**战斗这一份原版是 `auto[18~32]`** —— 两处摆放、两颗 TMP 的字段**真的不同**（铁律 5·c）。
        /// <para>⚠️ **`m_fontSizeBase` 这一格【不传】是有判据的，不是没读到**：原版四颗的 base 都是 **36.0**
        /// —— 那正是 TMP 的**序列化默认值**（`TMP_Text.cs:473`，作者的 `fontSize` setter 在
        /// `m_enableAutoSizing=1` 时**不回写 base**）⇒ 运行期它进的是
        /// `m_fontSize = Mathf.Clamp(base, min, max)`（`TextMeshPro.cs:2149`），**四颗全被夹到 `max`**。
        /// 而本口 `basePx &lt;= 0` 的缺省语义 = 「base 取调用方那一档（`cur`）」——
        /// 本窗这四颗的 `cur` 分别是 38.5 / 32.6 / 32.6 / 35，**前三颗恰好 = max**、
        /// `LoreText` 那颗 35 > max 32 ⇒ 同样**被夹到 max** ⇒ **与「原版把 36 夹到 max」逐位同效**。
        /// （换算：若照字面传 `basePx = 36`，在**我们**这套 px 口径里 36 落在 `[19.3, 38.5]` **区间内部**，
        ///  那反而与「原版被夹到 max」**不同** —— 单位不同的两个数不能直接对填。）</para></summary>
        const float FxTitleAutoMax = 38.5f, FxTextAutoMax = 32.6f, LoreAutoMax = 32f;

        /// <summary>标题那句 = **原版词条** `Battle/HUD/AffectedBy`（`Loc.T`）。
        /// 🔴 **2026-10-18（第十二轮 · W6）就地订正（铁律 5）**：这里原来写
        /// 「**这是我们写的** —— 原版那条词条在**远端 I2 语言表**里，本地只有一条英文样例 `'Affected by:'`」
        /// —— **「这是我们写的」不成立**：键和英文原文**都在本地**（那颗 `Localize` 就在
        /// `bundle_scenes_scenes_battlearena1/MonoBehaviour_4578.json`，`mTerm = Battle/HUD/AffectedBy`，
        /// 同族 TMP 逐字 `Affected by:`；判据全文 → `Core/Loc.cs` 那一块）。
        /// **错因**：「本地只有一条英文样例」被读成了「拿不到词条」—— 其实那条样例**就是**原版文案。</summary>
        public static string TitleText { get { return Loc.T(TitleTerm); } }
        /// <summary>标题词条键 —— **只此一份**。</summary>
        public const string TitleTerm = "Battle/HUD/AffectedBy";

        /// <summary>一行效果：**谁给的** + **给了什么**。</summary>
        public struct EffectRow
        {
            public string Who, What;
            public EffectRow(string who, string what) { Who = who; What = what; }
        }

        /// <summary>把引擎的限时增益（`UnitState.TempBuff`）翻成两行文字。
        /// <para>🔴 **2026-10-18（第十五轮 · `G5`）就地订正**：这里原来写「原版那几句模板在服务端
        /// （`GameStaticData.GetEffectDesc`，本地只有一条样例）⇒ **这里的措辞是我们写的**」——
        /// **「措辞是我们写的」这句在【中文】上仍然成立，但「原版模板在服务端」这个理由不完整**：
        /// 模板的**键**在本地（`Battle/Effect/Change*OneTurn`，见 `Core/Loc.cs` 那一块），
        /// 只有**值**在远端 I2 表。本批把消费面也钉死了 —— 原版是
        /// `CardEffectItem.LoadEffect` → `GameStaticData.GetEffectDesc`（`decomp_full` 两个方法体亲读），
        /// 而那正对应我们这一块（展示窗的「受到以下影响：」两行）。</para>
        /// <para>⇒ 现在**属性那几行走原版键**（`Battle/Effect/Change{MeleeAttack,RangedAttack,Health,Armour}OneTurn`，
        /// 占位符 `{0}` 填**带符号的数值**）；**关键词那几行照旧走 `CardText`**（关键词有它自己那一族词条，
        /// ⛔ 不是 `Battle/Effect/*`）。认不出来的名字**原样打出来**（不静默、不自造）。</para>
        /// <para>⚠️ 中文列 = 改之前 `AttrZh` 的原话（`"{0} 近战"` + `+2` ⇒ 逐字等于原来的 `"+2 近战"`）
        /// ⇒ **今天中文档零变化**；英文档从「中文」变成英文。</para>
        /// 「谁给的」用 `SourceCard`（施加它的**真卡名**，不是那个恒为「战术卡」的 `Src` —— 两者**别混**）。</summary>
        public static List<EffectRow> RowsOf(IReadOnlyList<UnitState.TempBuff> buffs)
        {
            var rows = new List<EffectRow>();
            if (buffs == null) return rows;
            for (int i = 0; i < buffs.Count; i++)
            {
                var b = buffs[i];
                if (b == null) continue;
                string what;
                if (b.IsKeyword) what = CardText.KeywordZh(b.Name);   // 关键词：它自己那一族词条，⛔ 不是 `Battle/Effect/*`
                else what = AttrText(b.Name, b.Value);
                if (string.IsNullOrEmpty(what)) what = b.Name;      // 认不出来就**原样打出来**（不静默、不自造）
                string who = b.SourceCard;
                if (string.IsNullOrEmpty(who)) who = b.Src;         // 兜底；两个都空才留白（上面那半句还在）
                rows.Add(new EffectRow(who, what));
            }
            return rows;
        }

        /// <summary>属性名 → 原版词条键（**只此一份**）。`null` = 认不出来（调用方原样打名字）。
        /// <para>🔴 键名的判据 = `GameStaticData.GetEffectDesc` 的 `case` 分派（`decomp_full` 亲读，
        /// 那 25 条键逐条列在 `Core/Loc.cs`）：`"attack"` 那一档在 `case 1` 里按
        /// `*(char*)(struct+0x32..0x35)`（哪几个属性非零）**多选一**——
        /// 单属性那四条是 `ChangeMeleeAttackOneTurn` / `ChangeRangedAttackOneTurn` /
        /// `ChangeHealthOneTurn` / `ChangeArmourOneTurn`（我们这边一条 `TempBuff` 只带一个属性 ⇒ 正好对上那些单属性键）。</para>
        /// <para>⚠️ **`…OneTurn` 而不是无后缀那四条**：`TempBuff` 是**限时**增益（到回合边界必撤，
        /// 见 `UnitState.RevertBuffs`），而原版无后缀的 `ChangeX` 是**永久**改属性 ⇒ 别拿错档。</para></summary>
        static string AttrTerm(string name)
        {
            switch (name)
            {
                case "attack": return "Battle/Effect/ChangeMeleeAttackOneTurn";
                case "ranged": return "Battle/Effect/ChangeRangedAttackOneTurn";
                case "health": return "Battle/Effect/ChangeHealthOneTurn";
                case "armour": return "Battle/Effect/ChangeArmourOneTurn";
            }
            return null;
        }

        /// <summary>属性那半句 = 原版词条 + 把 `{0}` 换成**带符号的数值**（正值带 `+`）。
        /// 属性名认不出 ⇒ 返回 `null`（调用方原样打名字）。
        /// <para>⚠️ **数值为 0 时只印属性名、不印 `{0}`**：原版 `GetEffectDesc` 在同类字段全 0 时
        /// 直接回**空串**（`case 1` 那条 `return DAT_1842b80e0`），我们**故意比原版多说一个词**
        /// （印属性名比印空白对玩家有用）—— 如实标，⛔ 别当成原版行为。</para></summary>
        static string AttrText(string name, int value)
        {
            string term = AttrTerm(name);
            if (term == null) return null;
            string s = Loc.T(term);
            if (s.IndexOf("{0}", System.StringComparison.Ordinal) < 0) return s;   // 表里那条没带占位符 ⇒ 原样
            if (value == 0) return s.Replace("{0} ", "").Replace(" {0}", "").Replace("{0}", "").Trim();
            return s.Replace("{0}", (value > 0 ? "+" : "") + value);
        }

        /// <summary>摆/收这块（`rows` 为空 = 整组不出现 —— 原版就是「有 buff 才露」）。
        /// 🔴 **先把根亮起来再改字**：TMP 在**未激活**的对象里量不出尺寸（`SetAutoFitBox` 那条踩过）。</summary>
        void SetEffectRows(IReadOnlyList<EffectRow> rows)
        {
            if (_fxRoot == null) return;
            int n = rows == null ? 0 : Mathf.Min(rows.Count, CardWinBox.EffSlots);
            if (rows != null && rows.Count > CardWinBox.EffSlots)
                Debug.LogWarning($"[展示窗] 效果清单有 {rows.Count} 条，原版只有 {CardWinBox.EffSlots} 个槽 ⇒ **只画前 {CardWinBox.EffSlots} 条**（不静默）");
            EffectRowCount = n;
            _fxRoot.gameObject.SetActive(n > 0);
            if (_fxTitle != null) _fxTitle.gameObject.SetActive(n > 0);
            for (int i = 0; i < CardWinBox.EffSlots; i++)
            {
                bool on = i < n;
                if (_fxBg[i] != null) _fxBg[i].gameObject.SetActive(on);
                if (_fxWho[i] != null)
                {
                    _fxWho[i].gameObject.SetActive(on);
                    if (on) { _fxWho[i].SetText(rows[i].Who ?? ""); PlaceLeft(_fxWho[i], _fxWhoBase[i], CardWinBox.EffWhoW); }
                }
                if (_fxWhat[i] != null)
                {
                    _fxWhat[i].gameObject.SetActive(on);
                    if (on) { _fxWhat[i].SetText(rows[i].What ?? ""); PlaceLeft(_fxWhat[i], _fxWhatBase[i], CardWinBox.EffWhatW); }
                }
            }
        }

        /// <summary>把一行字摆成**左对齐**（原版 `m_HorizontalAlignment = 1`；`AffectedBy` 也是 Left）。
        /// ⚠️ **不能只调 `Label.SetAlignLeft()`** —— 那只设 TMP 的 `alignment`，而 `Label.RefreshBounds()`
        ///    每次都把**字形块整体居中到锚点**上 ⇒ 短句看上去还是居中（2026-09-28 实拍抓到：
        ///    「先锋」两个字正正地飘在底板中间）。这里按「**块左缘贴框左缘**」再推一把 ——
        ///    多行折行时 `WorldW` ≈ 框宽 ⇒ 位移 ≈ 0，正好就是原版的行为。
        /// 🔴 **`basePos` 必须是「`SetZ` 之后」的 `localPosition`** —— 本函数是**整份**重设位置（含 z），
        ///    传进一个 z 还没推过的 `basePos`，就会把字从 −2.12 拨回 ≈0 ⇒ **被 −2.10 的底板压住**。
        ///    症状极隐蔽：图上只是「**字暗一点**」（白字峰值 190 而不是 255），**940+ 条断言全绿**。</summary>
        static void PlaceLeft(Label lb, Vector3 basePos, float boxWpx)
        {
            if (lb == null) return;
            float wPx = LayoutSpace.PxX(lb.WorldW) - LayoutSpace.PxX(0f);   // 世界长 → px（还是那一个换算口）
            float dx = Mathf.Max(0f, boxWpx - wPx) * 0.5f;
            lb.transform.localPosition = basePos + new Vector3(-LayoutSpace.Px(dx), 0f, 0f);
        }

        static void SetZ(ImageQuad q, float z)
        {
            if (q == null) return;
            var p = q.transform.localPosition; q.transform.localPosition = new Vector3(p.x, p.y, z);
        }

        /// <summary>把一个节点推到指定的 z。
        /// 🔴 **`MenuDraw` 建出来的东西 z 一律是 0**（它只算 x/y），而本窗的遮罩在 `ZMask = −2.0`
        /// —— 相机看 +Z、**z 越小越靠前** ⇒ 不推的话**文字/底图会被遮罩压住变暗**
        /// （2026-09-28 写完当场抓到的，同 `CardDetailPopup` 那条「遮罩画到卡上面」是一族）。</summary>
        static void SetZ(Transform t, float z)
        {
            if (t == null) return;
            var p = t.localPosition; t.localPosition = new Vector3(p.x, p.y, z);
        }

        void SetChrome(bool on)
        {
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_hint != null) _hint.gameObject.SetActive(on);
            if (_voiceBtn != null) _voiceBtn.gameObject.SetActive(on);
        }

        // ============================================================ 命中（战斗侧不走 PointerLayer，按 px 判）

        /// <summary>点在**哪一格**上（−1 = 没点到任何卡）。**前台优先**：
        /// 前面的卡盖住后面的，所以从槽 0 往后找、先命中的就是它。
        /// 命中区 = 原版点击接收器 `CardUI/2DCard/UI Collider`：**两轴比例不同 + 中心比卡心低 5px**
        /// （`m_SizeDelta(-0.2,-0.44)` · `m_AnchoredPosition.y = −0.02`）⇒ **判据只此一份 → `CardFan`**
        /// （`HitW` / `HitH` / `HitCy`；⛔ 别再退回「一个比例双轴同用 + 居中」—— 那是 A156 修掉的偏离）。</summary>
        public int HitSlot(Vector3 world)
        {
            if (!Visible) return -1;
            var px = LayoutSpace.ToPixel(world);
            for (int i = 0; i < SlotCount && i < CardFan.Slots; i++)
            {
                float hw = CardFan.HitW(i) * 0.5f;
                float hh = CardFan.HitH(i) * 0.5f;
                float dx = px.x - CardFan.Cx(i), dy = px.y - CardFan.HitCy(i);
                if (Mathf.Abs(dx) <= hw && Mathf.Abs(dy) <= hh) return i;
            }
            return -1;
        }

        /// <summary>指针还停在**窗的地界**里吗（关窗那条「指针移开」用的守卫）。
        ///
        /// 🔴 **如实标注：这是「我们按实情收的口径」，不是原版字面。**
        ///   原版那条是 `CardCollider.OnPointerExit` → `CardScript.OnTouchExit`，
        ///   **唯一守卫是 `displayingCardFlag`**（= 指针离开**那张卡**就关）。
        ///   照字面做的话，窗里那颗钮（语音）**永远点不到** —— 它在卡外的 `LowerSection` 上，
        ///   指针一离开卡就先关窗了。⇒ 我们把「地界」定成 **5 个卡格 ∪ 语音钮**：指针离开这整块才关。
        ///   ⚠️ **2026-10-17（B8）**：原来这里还有第三个落点（眼睛钮）。那颗钮战斗版原版就没有
        ///   （`showCardTextButton = {m_PathID:0}`，13/13）⇒ 连同它的桩一起删了，「地界」只剩这两块。
        ///   ⚠️ **待实机核**：原版在**触屏**上是不是「抬手即关」，产物里证实不了
        ///   （`StandaloneInputModule` 没反编译）。判据 → `资料/待办判据_战场与战斗视图.md` §8b。</summary>
        public bool ContainsPointer(Vector3 world)
        {
            if (!Visible) return false;
            if (HitSlot(world) >= 0) return true;
            return HitVoice(world);
        }

        /// <summary>语音按钮被点到了没有（px 判定，和别处同一套换算）。</summary>
        public bool HitVoice(Vector3 world)
        {
            if (_voiceBtn == null || !_voiceBtn.gameObject.activeSelf) return false;
            return HitBtn(world, CardWinBox.VoiceCx);
        }

        static bool HitBtn(Vector3 world, float cx)
        {
            var px = LayoutSpace.ToPixel(world);
            float h = CardWinBox.BtnS * 0.5f;
            return Mathf.Abs(px.x - cx) <= h && Mathf.Abs(px.y - CardWinBox.BtnCy) <= h;
        }

        /// <summary>
        /// 点语音按钮 → 播**正在展示的那张卡**的单位语音（原版 `voiceOverButton` + `voiceOverAudioSource`）。
        /// 判据（哪条语音）复用 `VoiceLines.TryPick` —— **只此一处**，不另写一张后缀表。
        /// ⚠️ 这张卡没有语音时**说出来**（红线：不许静默失败），返回 false。
        /// </summary>
        public bool PlayVoice()
        {
            LastVoiceFile = null;
            if (!_hasShown) return false;      // 窗里没有卡（`CardData` 是 struct，用它判「有没有」）
            // ⚠️ **`_shown.id` 是「名字」不是卡 id**（`ToCardData` 的注释：`id` 兼着显示名/配对的活）——
            //    语音表按**引擎卡 id** 索引 ⇒ 要用 `artId`（原版卡 = 卡 id；自造卡 = 卡名，自然查不到）。
            //    判据只此一份：`CardButtons.VoiceKey`。
            string key = CardButtons.VoiceKey(_shown);
            if (string.IsNullOrEmpty(key)) return false;
            if (!VoiceLines.Has(key))
            {
                Debug.Log($"[展示窗] 「{_shown.title}」没有单位语音（`VoiceLines.Has(\"{key}\")` 为假）—— 语音按钮这次没声音");
                return false;
            }
            string file, text;
            // `rng` 传 null = 取第一条（定死、可复现）；这个按钮是「听一下」，不需要随机
            // ⚠️ 用 `ForVoiceOver`（**随便哪条都行**）—— 借 `ForDeploy` 会挂空：有的卡只有 attack/death
            if (!VoiceLines.TryPick(key, VoiceLines.ForVoiceOver, null, out file, out text)) return false;
            var clip = VoiceLines.Clip(file);
            if (clip == null || _voiceAudio == null) return false;
            _voiceAudio.PlayOneShot(clip);
            LastVoiceFile = file;
            return true;
        }

        // ============================================================ 开 / 关

        /// <summary>开/关一次。已开着就关掉（原版那个方法名就是 `Toggle...`）。</summary>
        public void Toggle(CardData d, CardDef def) { if (Visible) Hide(); else Show(d, def); }

        /// <summary>开/关（**棋盘单位的轻点**走这条 —— 带「谁给我加的 buff」那几行）。
        /// `effects` 由调用方从引擎状态算（`RowsOf`），空/null ⇒ 那块整组不出现（原版就是「有 buff 才露」）。</summary>
        public void Toggle(CardData d, CardDef def, IReadOnlyList<EffectRow> effects)
        {
            if (Visible) Hide();
            else Show(d, def, effects);
        }

        /// <summary>开窗（不带卡表项 —— 只显示主卡、没有相关卡，并**出声**）。</summary>
        public void Show(CardData d) { Show(d, null, null); }

        /// <summary>开窗。`def` 是这张卡的**引擎卡表项** —— **相关卡要它才算得出来**（没有就只画主卡）。</summary>
        public void Show(CardData d, CardDef def) { Show(d, def, null); }

        /// <summary>开窗 + 那块「谁给我加的 buff」（原版 `ShowBattleCard` → `DisplayCardEffects` 一条链）。</summary>
        public void Show(CardData d, CardDef def, IReadOnlyList<EffectRow> effects)
        {
            Hide();                       // 先把上一次那一叠拆干净（换阵营要换卡框贴图，原地改不换图）

            ShownTitle = d.title;
            ShownBody = d.keywords;
            _shown = d; _hasShown = true;   // 语音按钮要按它查单位语音
            LastVoiceFile = null;
            // ⚠️ 这里原来还有一句 `LoreVisible = true;`（原版每次 `ShowCard` 都把 lore 复位）——
            //    那颗眼睛钮连同这个开关已删（判据见文件头）⇒ 「露不露」现在**只有一处判据**：
            //    `RefreshLore` 里按**有没有字**开关（原版 `SetCardLore.c:24-27`）。

            BuildFan(d, def);
            RefreshLore();
            RefreshButtons();

            SetChrome(true);
            Visible = true;
            gameObject.SetActive(true);
            SetEffectRows(effects);      // ⚠️ 放在最后 —— 它要先把那块亮起来再改字（TMP 未激活量不出尺寸）
        }

        public void Hide()
        {
            Visible = false;
            ShownTitle = null;
            ShownBody = null;
            _hasShown = false;
            LastVoiceFile = null;
            _swapping = false;
            SlotCount = 0;
            for (int i = 0; i < CardFan.Slots; i++) { SlotViews[i] = null; _slotDefs[i] = null; }
            if (_cards != null) MenuDraw.ClearChildren(_cards);
            if (_bg != null) MenuDraw.ClearChildren(_bg);
            _lore = null;
            SetChrome(false);
            SetEffectRows(null);         // 那块也跟着收（原版 `DisplayCardEffects` 每次重算）
        }

        // ============================================================ 卡片那一叠（1 主卡 + 8 相关卡）

        /// <summary>把「1 主卡 + N 相关卡」按**原版那条 clip 的扇形**摆出来（判据与真值 → `CardFan`）。
        /// ⚠️ 位姿槽 0 = 前台（原版 `cardUIs[0]`：屏心偏上 (960,480) · scale 250 · 转角 0）。</summary>
        void BuildFan(CardData d, CardDef def)
        {
            var cards = new List<CardData>();
            var defs = new List<CardDef>();
            cards.Add(d); defs.Add(def);
            if (def != null)
            {
                // 相关卡：判据只此一份（`RelatedCards.Find`），与原版那 4 个 `relatedCard1..4` 字段的关系 → 那个文件头
                foreach (var r in RelatedCards.Find(def, CardFan.Slots - 1))
                {
                    cards.Add(BattleDriver.ToCardData(r, r.Faction));
                    defs.Add(r);
                }
            }
            else
            {
                Debug.LogWarning("[展示窗] 没拿到卡表项（`CardDef`）⇒ **只显示主卡、没有相关卡**"
                               + "（相关卡是按卡表算的；不静默）");
            }

            SlotCount = Mathf.Min(cards.Count, CardFan.Slots);
            _swapping = false;
            for (int i = 0; i < CardFan.Slots; i++) { SlotViews[i] = null; _slotDefs[i] = null; }

            for (int i = 0; i < SlotCount; i++)
            {
                var cd = cards[i];
                var v = CardView.Create(_cards, cd, CardFan.SlotName(i));
                if (v == null)
                {
                    Debug.LogWarning("[展示窗] 第 " + i + " 格（" + cd.title + "）的 `CardView` 建不出来 —— 那一格没画，出声");
                    continue;
                }
                v.gameObject.SetActive(true);
                v.SetPose(SlotLocal(i), CardFan.Rot(i), CardFan.Hpx(i) / (CardView.Height * 108f));
                v.SetData(cd);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);   // 展示窗里的卡不吃「可出牌/不可出牌」那套状态色
                CardFan.ApplySlotTint(v, i == 0);            // 前台白 / 相关卡压暗（原版 `cardInBackGroundColorTint`）
                SlotViews[i] = v; _slotDefs[i] = defs[i];
            }
            CardFan.ApplyQueues(SlotViews, SlotCount, CardFan.QCardBase);
            Debug.Log($"[展示窗] 卡片那一叠：**{SlotCount} 格**（1 主卡 + {Mathf.Max(0, SlotCount - 1)} 相关卡）"
                    + " —— 位姿：槽 0–4 照原版 clip `Card Display Open`，槽 5–8 是**我们外推的**（`CardFan` 文件头）");
        }

        /// <summary>第 `i` 格的**局部坐标**（相对 `_cards`；z 按槽位阶梯，只作兜底，真正排序看渲染队列）。
        /// ⚠️ 走 `MenuDraw.Local` **而不是**直接拿 `EndPanel.Pos` —— 那样只有在「窗根正好在世界原点」时才对。</summary>
        Vector3 SlotLocal(int i)
        {
            float w = CardFan.Wpx(i), h = CardFan.Hpx(i);
            var l = MenuDraw.Local(_cards, CardFan.Cx(i) - w * 0.5f, CardFan.Cy(i) - h * 0.5f,
                                            CardFan.Cx(i) + w * 0.5f, CardFan.Cy(i) + h * 0.5f);
            return new Vector3(l.x, l.y, ZContent - i * ZSlotStep);
        }

        /// <summary>点第 `idx` 格 ⇒ **和前台那张两两换位**（原版 `CardDisplayWindow.ChangeCardPosition`）。
        /// **三个闸**（任一中就整个 return，什么都不做）：① 上一次没播完（`swappingCards`）·
        /// ② 点的是前台自己 · ③ 有 `Animation` 在播（⚠️ **本工程不适用** —— 原版摆位那条 legacy clip
        /// 我们没有，我们按 `CardFan` 的常量直接摆；这一点是**如实说明的偏离**）。
        /// 收尾照原版 `CardSwapFinished`：开闸 · 换前台指针 · **重设 lore / 语音 / 眼睛钮** ·
        /// **换色补间**（去前台变白、让位的压暗到 `cardInBackGroundColorTint`）。</summary>
        public void SwapToFront(int idx)
        {
            if (_swapping)
            {
                Debug.Log("[展示窗] 换位被挡：上一次还没播完（原版闸① `swappingCards`）");
                return;
            }
            if (idx <= 0 || idx >= CardFan.Slots) return;                  // 闸②：点的是前台自己（位姿槽 0）
            var a = SlotViews[0]; var b = SlotViews[idx];
            if (a == null || b == null) return;
            _swapping = true;

            var pa = a.transform.position; var pb = b.transform.position;
            float ra = a.transform.eulerAngles.z, rb = b.transform.eulerAngles.z;
            float sa = a.transform.localScale.x, sb = b.transform.localScale.x;

            // 六条 tween 全用同一个时长（原版 `relatedCardSwapTime` = 0.25s）：
            // 被点那张 → 前台位；前台那张 → 被点卡的槽位。
            CardTween.ToPose(b.transform, pa, ra, sa, CardFan.SwapTime, DG.Tweening.Ease.OutQuad).OnComplete(() =>
            {
                // 记账：两个**位姿槽**上的卡对调（前台永远是位姿槽 0）
                var td = _slotDefs[0]; _slotDefs[0] = _slotDefs[idx]; _slotDefs[idx] = td;
                var tv = SlotViews[0]; SlotViews[0] = SlotViews[idx]; SlotViews[idx] = tv;
                // 🔴 **层级按新的槽位重排**（原版那一步是 `SetSiblingIndex` 两两互换 —— 目的就是
                //    「现在站在前台的那张画在最上面」；我们这套的前后由**渲染队列**决定，见 `CardFan`）。
                CardFan.ApplyQueues(SlotViews, SlotCount, CardFan.QCardBase);
                _swapping = false;
                // 收尾（原版 `CardSwapFinished`）：重设 lore / 语音 / 眼睛钮 —— 认的都是**新前台**那张
                if (SlotViews[0] != null)
                {
                    _shown = SlotViews[0].Data;
                    ShownTitle = _shown.title;
                    ShownBody = _shown.keywords;
                }
                RefreshLore();
                RefreshButtons();
                Debug.Log("[展示窗] 换位完成：前台现在是「" + (SlotViews[0] != null ? ShownTitle : "?")
                        + "」（收尾重设了 lore / 语音 —— 原版 `CardSwapFinished`）");
            });
            CardTween.ToPose(a.transform, pb, rb, sb, CardFan.SwapTime, DG.Tweening.Ease.OutQuad);
            // 着色跟着换：去前台那张 → 白，让出前台那张 → `cardInBackGroundColorTint`
            // （原版 `ChangeCardPosition.c:203-221` 那两条 tween，同一时长）。
            CardFan.TweenTint(b, CardFan.BackTint, CardFan.FrontTint, CardFan.SwapTime);
            CardFan.TweenTint(a, CardFan.FrontTint, CardFan.BackTint, CardFan.SwapTime);
            Debug.Log($"[展示窗] 换位：第 {idx} 格 ⇄ 前台（{CardFan.SwapTime}s = 原版 `relatedCardSwapTime`）");
        }

        // ============================================================ 下缘那条（文字 + 两颗钮）

        /// <summary>效果文字条那行字。原版这里是 **Lore 背景故事**；我们没有 lore 文案
        /// （I2 词条在远端，本地拿不到 —— 正本 §十·2 已结案）⇒ 画**效果文字**，并**如实标注**。
        /// ⚠️ 换位收尾（原版 `CardSwapFinished` 的 `SetCardLore(新前台)`）会**重建**它 ⇒ 抽成独立方法。</summary>
        void RefreshLore()
        {
            if (_bg == null) return;
            MenuDraw.ClearChildren(_bg);
            _lore = null;
            // ⚠️ **先亮起来再建字** —— TMP 在**未激活**的对象里量不出尺寸（`textBounds` 是垃圾，
            //    实测能到 4.29e9 ⇒ 面板宽度顶到上限、一个字看不见）。最后再按开关收起/露出。
            _bg.gameObject.SetActive(true);

            // ① 底板：**按阵营**选图（原版 `FlavourTextSO.GetClanFlavorBackground`）—— 认的是**当前前台**那张。
            //    ⚠️ 原版 `FlavourTextBG` 的 `m_PreserveAspect = 0`（实读）⇒ **拉伸**铺满，别开 keepAspect。
            string fac = _hasShown ? _shown.faction : null;
            var tex = CardArt.FlavorBg(fac);
            if (tex != null)
                SetZ(MenuDraw.Rect(_bg, tex,
                                   new PxRect(CardWinBox.LowerL, CardWinBox.LoreBgT, CardWinBox.LowerR, CardWinBox.LoreBgB),
                                   "Image", QChrome, null, false), ZContent);
            else
                Debug.LogWarning($"[展示窗] 阵营「{fac}」没有风味底图（`CardArt.FlavorBg` 取不到）⇒ **只画字**（不静默）");

            // ② 那行字
            // 🔴 **没字 ⇒ 整块不亮**（原版 `SetCardLore.c:24-27`：`SetActive(loreObjectBG, !IsNullOrEmpty(flavourText))`）。
            //    ⚠️ **2026-10-17 就地更正**：这里原来是 `SetActive(LoreVisible)`，而那个开关恒 true
            //    ⇒ **没字也亮一块空的风味底板**（它当时倒是能被眼睛钮手动切掉，可战斗版原版根本没有那颗钮
            //    ⇒ 实际就是「永远露着」）⇒ 照原版收口（`Editor/BattleScene.cs` 有一条断言钉着）。
            //    🆕 **2026-10-17（B8）**：那个开关本身已删 —— 现在这一块的显隐**只有这一处判据**。
            string body = ShownBody ?? "";
            if (string.IsNullOrEmpty(body)) { _bg.gameObject.SetActive(false); return; }
            var r = new PxRect(CardWinBox.LoreL, CardWinBox.LoreT, CardWinBox.LoreR, CardWinBox.LoreB);
            // 原版 `LoreText`：**fs35**（auto 10–35）· 限宽换行 · **居中** · 白
            // 🔴 **2026-09-28：不是右对齐** —— `m_HorizontalAlignment = 2`（Center，位标志 Left=1/Center=2/Right=4），
            //    实拍也一致（正本 §十·5 第 4 条）。`MenuDraw.Text` 默认就是居中 ⇒ **不要** `AlignRight`。
            // 🔴 **A406（2026-10-12）就地订正上一行那两个数**：上面那句 `fs35 auto 10–35` 是**菜单版**
            //   （`Card Detail Popup`）那颗的值；**战斗这一份**（`bundle_scenes_scenes_battlearena1` 的
            //   `GameObject/LoreText.json`，父链 = `LowerSection/FlavourTextBG`）原版是
            //   **`字号=32.0 auto[18.0~32.0] 基准=36.0`** ⇒ 上限 **32**（见 `LoreAutoMax` 那段判据）。
            //   ⚠️ 本件**只改上限**（`autoMaxPx`）：下限那一格我们仍传 `10f`、原版战斗版是 **18** ——
            //      **min 不在 A333/A336 两条账里**（同 `MissionsTab` 那两处先例），**如实记着、没动**。
            _lore = MenuDraw.Text(_bg, r, body, Color.white, "LoreText", CardWinBox.LorePx, QChrome, r.W, 10f,
                                  LoreAutoMax);
            SetZ(_lore != null ? _lore.transform : null, ZContent);
            if (_lore == null)
                Debug.LogWarning("[展示窗] 效果文字条建不出来（`MenuDraw.Text` 返回 null）—— 不静默");
            _bg.gameObject.SetActive(true);    // 走到这里 body 必非空 ⇒ 照原版「有字才亮」（见上面 ② 那条判据）
        }

        /// <summary>遮罩用的纯色贴图（`ImageQuad` 得有一张图才肯建）。宽高比按传入的来。
        /// ⚠️ **`MultiCardDisplay` 也用这一个**（两张遮罩是同一个作用，别写第二份）。
        /// 🔴 2026-09-28 本文件重写时**一度把它弄丢**（`MultiCardDisplay` 三处调用立刻编译不过）——
        ///    别再删：它不是本窗私有的。</summary>
        static Texture2D _solid;
        static float _solidAspect;
        internal static Texture2D SolidTexFor(float aspect)
        {
            if (_solid != null && Mathf.Abs(_solidAspect - aspect) < 0.01f) return _solid;
            int h = 32;
            int w = Mathf.Max(4, Mathf.RoundToInt(h * aspect));
            _solid = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            _solid.SetPixels(px);
            _solid.Apply();
            _solidAspect = aspect;
            return _solid;
        }

        /// <summary>语音钮**按卡决定建不建**（原版 `CardDisplayWindow.SetCardVoiceOver`。
        /// ⚠️ **本窗只有这一颗** —— 眼睛钮战斗版原版就没有（见文件头 A860），别再照菜单版补一颗。
        /// 判据只此一份 → `CardButtons`（那里写了原版出处与我们的等价物）。</summary>
        void RefreshButtons()
        {
            bool voice = _hasShown && CardButtons.HasVoice(_shown);
            if (_voiceBtn != null)
            {
                _voiceBtn.gameObject.SetActive(voice);
                if (voice) SetZ(_voiceBtn, ZContent);
            }
        }
    }
}
