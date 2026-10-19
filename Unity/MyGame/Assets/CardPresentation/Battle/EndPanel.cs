// EndPanel.cs — 对局结算界面（原版 `EndBattlePanel`）
//
// ---- 尺寸出处 ----
// 全部来自运行时 UI dump `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`
// 里 `BattleHud/Canvas/BattleDoors/Canvas/EndBattlePanel` 那棵子树（实测 anchoredPosition / sizeDelta）：
//   Background        3840×2160   （两倍屏，铺满带余量）
//   Video Image       1920×1080  @ (-6, 64)
//   AllRewardsHolder  100×100    @ (0, -260)
//   SkullsHolder      648.1×52.4   图 `40k_main_bt_nametag`
//   skull1/2/3        64.3×71.2    图 `40k_battle_Win Skull`
//   RewardsHolder     495.2×49.0   图 `40k_main_bt_nametag`
//   RewardHolder      140×50 → DrawerHolder 65×50 @ (5,0) → Quantity 文字 0×45
//   HolderRating      187×45 → Trophy 60.1×60（图 `40k_UI_icon_ranked_Skirmish`）、
//                              RatingText 65.3×46.9、ArmyIcon 64.8×60（dump 里 activeSelf=False）
//
// ⚠️ **两处是我们排的，不是原版**（dump 里拿不到，别当原版抄）：
//   1. **纵向叠放顺序**：dump 里 `SkullsHolder` 和 `RewardsHolder` 的 anchoredPosition **都是 (0,0)** ——
//      说明位置是运行时 LayoutGroup 算出来的，静态 dump 只有建之前的初值。这里改成上下两行排。
//   2. ~~**未点亮的骷髅用半透明表示**~~ —— ✅ **2026-09-27 已照原版改掉**：
//      原版就是 `SetActive(false)`（**根本不显示**），而且 0 个时**整行连底板一起藏**
//      （`EndBattleDoors__ShowRewards.c:55` + `:58-68`；⚠️ 这条与「10/0x14/0x1e 是 `BattleWinner` 枚举」
//       是同一轮复核出来的 —— 那个方法体里只有两件事：选片 + 摆这排骷髅）。
//
// 奖杯图：原版那张是 `40k_UI_icon_ranked_Skirmish`（dump 里 `Trophy` 节点挂的 sprite）。
//
// 🔴 **2026-09-17 用户拍板：奖励那一行我们不做** —— 奖励（红水晶数量）与评分**都在服务器**，
//    单机版用不上 ⇒ **只留骷髅那行**（`SkullsHolder` + 三个骷髅），
//    `RewardsHolder` / `HolderRating` / `Trophy` / `RatingText` **一个都不建**。
//    上面 dump 里那两行尺寸**保留当参照**（将来真要做，照它摆就行），代码里不再生成。
//    （历史留痕：那段「奖杯图一度静默不画」的更正随奖励行一起去掉了；原版那张
//      `40k_UI_icon_ranked_Skirmish` 仍在 `Resources/Art/ui_deck/` 里，将来要做直接取。）
//
// 🔴🔴 **2026-10-16（`T09` 整层换 uGUI · 第 2 批 `PopUps` 组）：本件 + `BattleDoors.cs` 已从
//     【世界空间 quad】迁到【uGUI 子画布】。** 口径与配方 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` §五·补⑩。
//   · **宿主** = 与 `BattleDoors` **共用**的那块子画布（`BattleDoors.PairCanvas()`）= 原版 `BattleDoors/Canvas`
//     （**`PopUps` 层 / order 0**；判据 → `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一）。
//     原版 `EndBattlePanel` 就挂在它下面 —— 本件的根节点照此挂上去。
//   · ⚠️ **一处已知差异、本次有意不翻**：原版是 `EndBattlePanel` 挂在 `BattleDoors/Canvas` **下**；
//     我们反过来（`BattleDoors` 挂在 `EndPanel` 下，见 `Build()` 最后那句）。**如实记着**，⛔ 别顺手倒过来（那是另一笔账）。
//   · 🔴 **单位换了**：旧那一套是**世界单位**（`MenuDraw.SetPxSize` / `LayoutSpace.Px` 内部都是 **÷108**）；
//     uGUI 档的 `sizeDelta` / 字 / 位置要的是**真 px**（canvas 单位就是设计 px）。
//     ⇒ 一律走 `HudCanvas` 那几个助手（`SetPxSize` / `FillParent` / `PosPx` / `MakeText` / `MakeImagePiece`）。
//     ⛔ 沿用 `MenuDraw.SetPxSize` 会把整块面板缩成 **1/108**，**而画面只是「小得看不见」、不报错**。
//   · 🔴 **z 的含义换了、但值照搬**：旧那套 z 是世界 z（**大的在后**）、`HudCanvas.Add` 的 z 是**兄弟序**
//     （**大的也在后**）⇒ 三层 `ZDim(-0.50) / ZVideo(-0.55) / ZContent(-0.60)` 一个没动（见 `BattleDoors` 头部）。
//   · **退场的**：`MenuDraw.SetPxSize` · `ImageQuad.Create` · `Label.Create` · `EndPanel.SolidTex`（那张临时纯色图）
//     · 私有助手 `Content(px,py)` / `U(px)`（uGUI 档直接给 px）。
//     ⚠️ **保留**：`Pos(px,py,z)` 与 `PxPerUnit` —— `CardDisplayWindow` / `MultiCardDisplay` / 自检**还在用**它们
//       （那些件**没迁**，改它们不是本批的活）。
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    public class EndPanel : MonoBehaviour
    {
        /// <summary>原版界面按 1920×1080 设计；这套布局可见高 10 单位 → 108 px/单位</summary>
        public const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;

        /// <summary>像素坐标（左上原点）→ **世界坐标**（屏心为原点、÷108）。z 越小越靠近相机。
        /// ⚠️ 加上开门视频之后面板分了**三层**，三个 z 的含义见 `BattleDoors`。
        /// <para>🔴 **2026-10-16（`T09` 批 2）：本件自己【不再用】它**（迁到 uGUI 之后位置一律走
        /// `HudCanvas.PosPx`，单位 = 设计 px）；但 `Battle/CardDisplayWindow.cs` · `Battle/MultiCardDisplay.cs`
        /// 与自检**还在用**（那些件**没迁**）⇒ **保留**，⛔ 别顺手删。</para>
        /// <para>🔑 两种空间的换算式（写在这里免得下一个人再推一遍）：`Pos(px,py,z).x × 108 = px − 960`、
        /// `.y × 108 = 540 − py` —— 而 uGUI 档的锚点也在父中心、单位就是 px ⇒ **同一对数**。</para></summary>
        public static Vector3 Pos(float px, float py, float z)
        {
            return new Vector3((px - 960f) / PxPerUnit, (540f - py) / PxPerUnit, z);
        }

        /// <summary>面板是不是正显示着（自检用）。</summary>
        public bool Visible { get; private set; }

        /// <summary>这次显示点亮了几个骷髅（自检用）。</summary>
        public int ShownSkulls { get; private set; }

        /// <summary>三个骷髅的 quad（**index 0 = 最左**）。给自检用 ——
        /// 用户 2026-09-26 问「是不是从左到右依次点亮」，而当时**全仓没有一条断言管顺序**（只验总数）。
        /// 🔴 点亮 = `activeSelf`（**不是 alpha** —— 原版未点亮的是 `SetActive(false)`，见 `Show` 里那段）。
        /// <para>🔴 **2026-10-16（`T09` 批 2）类型 `ImageQuad[]` → `HudPiece[]`**：这一族现在是 uGUI 件。
        /// 两个读它的人（`Editor/BattleScene.cs` 与 `Editor/EndPanelProbe.cs`）只用 `.transform` /
        /// `.gameObject` ⇒ **它们一个字都不用改**（`HudPiece` 把这两个口转发给底下真正的对象）。</para></summary>
        public HudPiece[] SkullQuads { get { return _skulls; } }

        /// <summary>骷髅行那块底板的**渲染尺寸**（世界单位）—— 自检用它钉住「原版 PA=0 拉满 648.1×52.4」那条。
        /// 🔴 2026-09-27：`ImageQuad` 默认**按贴图比例定宽**，而 `40k_main_bt_nametag` 只有 109×41
        ///   ⇒ 补 `SetAspect` 之前我们只画出 **139.3** 宽（**窄 508.8px、只剩 21%**）。
        /// 🔴 2026-10-16：uGUI 档这一对读的是 `RectTransform.GetWorldCorners()`（`HudPiece` 里那两支）
        ///   ⇒ **仍然是世界单位**、与旧 quad 量的是同一件事，⛔ 别在这里乘 108。</summary>
        public float SkullPlateWorldW { get { return _skullPlate != null ? _skullPlate.WorldW : 0f; } }
        public float SkullPlateWorldH { get { return _skullPlate != null ? _skullPlate.WorldH : 0f; } }

        /// <summary>骷髅那一行的容器（原版 `SkullsHolder`）。自检用它验「0 个时整行连底板一起藏」。</summary>
        public Transform SkullRow { get { return _skullRow; } }

        /// <summary>🔴 **2026-10-18（第十二轮 · 战斗侧 · W6）新增：本局的结局 = 一个【枚举】，不是一句文案。**
        /// <para>**为什么必须分开**：原版这一格原来是把 `"胜利"/"失败"/"平局"` 三个中文字符串当**状态哨兵**
        /// （产在 `:265`、再拿 `==` 去比 `:272` 与 `:293-294`）—— 那是「拿显示文案当状态」。
        /// 两个后果，都是**看得见的**：① 一旦这几句走 `Loc.T`（中英切换），那些 `==` 比较**全部失效**
        /// （英文档下标题与**开门视频**都会判错）；② 顺带确认 `CardText.Phrase("VICTORY"/"DEFEAT")`
        /// 当年在 `Phrases` 表里**根本没有这两个键** ⇒ 中文档下结算大标题印的是英文 `VICTORY`。
        /// ⇒ 现在：**状态 = 本枚举**（`Outcome`），**显示文案 = `Loc.T(&lt;原版词条&gt;)`**，两者不再互为依据。</para></summary>
        public enum EndOutcome { None = 0, Victory = 1, Defeat = 2, Draw = 3 }

        /// <summary>本局的结局。**没显示时是 <see cref="EndOutcome.None"/>**。
        /// 🔴 判据（自检/开门视频/对局记录）一律读它，⛔ **别再去比较 <see cref="ResultText"/>**
        /// —— 那串字**跟着语言变**，用它当状态就是本类原来那个缺陷。</summary>
        public EndOutcome Outcome { get; private set; }

        /// <summary>结局 → 原版词条键。**只此一份**（显示与断言都走它）。
        /// 判据（键名出处、谁是原版载体）→ `Core/Loc.cs` 那一块 `Battle/` 的注释，⛔ 别在这儿抄第二份。</summary>
        public static string TermOf(EndOutcome o)
        {
            switch (o)
            {
                case EndOutcome.Victory: return "Battle/BattleEnd/Victory";
                case EndOutcome.Defeat:  return "Battle/BattleEnd/Defeat";
                case EndOutcome.Draw:    return "Battle/BattleEnd/Draw";
                default:                 return null;
            }
        }

        /// <summary>结果文字（自检用）：`Loc.T(TermOf(Outcome))` —— **跟着当前语言变**。没显示时是空串。
        /// ⚠️ **它只用来显示/比对界面**；判状态请读 <see cref="Outcome"/>。</summary>
        public string ResultText
        {
            get
            {
                string t = TermOf(Outcome);
                return t == null ? "" : Loc.T(t);
            }
        }
        /// <summary>副标题那行（`N 回合   敌方督军最低生命 X` / 投降时是另一种写法）。自检读它。</summary>
        public string SubText { get { return _sub != null ? _sub.Text : null; } }

        /// <summary>开门视频那层（自检用）。</summary>
        public BattleDoors Doors { get { return _doors; } }

        /// <summary>内容层（标题 / 副标题 / 骷髅）是不是露着的（自检用）。</summary>
        public bool ContentVisible { get { return _content != null && _content.gameObject.activeSelf; } }

        /// <summary>🔴 **结算后的出口闸门**（= 原版 `BattleManager.matchFinishedAndWaitingToLeave`，`+0x510`）：
        /// 面板显示着 **且** 开门视频播完（+0.15s）。**闸门没开时点/按都不理** ——
        /// 原版 `BattleManager__Update.c:105` 那句 `if (*(char *)(param_1 + 0x510) == '\0') return;`。
        /// 判据链：`_CloseBattleDoors_d__393__MoveNext.c:51-56`（等 `SetupDoor` 的返回值）→ `:72`（置位）；
        /// 读者 = `BattleDriver.HandleEndBattleExit()`（左键任意处 / ESC ⇒ 回主菜单）。
        /// ⚠️ **闸门本身的判据只有一份**（`BattleDoors.Finished`），⛔ 别在这里再推一遍「播完没有」。</summary>
        public bool ExitReady { get { return Visible && _doors != null && _doors.Finished; } }

        /// <summary>结果文字（标题）是不是露着的（自检用）。**放开门视频时应该是 false** —— 字在视频里。</summary>
        public bool TitleVisible { get { return _title != null && _title.gameObject.activeSelf; } }

        Transform _root, _content;
        BattleDoors _doors;
        /// <summary>🔴 **2026-10-16（`T09` 批 2）：类型 `ImageQuad` → `HudPiece`** —— 这一族现在是
        /// **uGUI 件**（`Image` / `RawImage` 那一侧），`HudPiece` 是它的适配层（`WorldW/WorldH` 两种模式
        /// 都返回**世界单位** ⇒ 自检读回那两条一个字不用改）。
        /// ⛔ **一个件只许在一边**（要么旧 quad、要么 uGUI）—— 本批**全部在 uGUI 这边**。</summary>
        HudPiece _dim;
        Label _title, _sub;
        readonly HudPiece[] _skulls = new HudPiece[DeckRules.SkullThresholds.Length];
        HudPiece _skullPlate;
        /// <summary>骷髅那一行的**容器**（原版 `SkullsHolder`）—— 三个骷髅是它的子节点，
        /// 整行的显隐靠它一个开关（原版 `skullHolderObj.SetActive(0 &lt; skullsObtained)`）。</summary>
        Transform _skullRow;

        public static EndPanel Create(Transform parent)
        {
            // 🔴 **2026-10-16（`T09` 批 2）**：根节点由「写 `sizeDelta` 的世界空间矩形」改成
            //    **挂在子画布上的 `RectTransform` + `FillParent`**。
            //    · **判据没变**：原版 `EndBattlePanel` 实读 = `RectTransform` · `anchor (0,0)-(1,1)` ·
            //      `sizeDelta (0,0)` ⇒ **绝对矩形 (0,0)-(1920,1080)**（`bundle_scenes_scenes_battlearena1`，
            //      2026-10-11 现读；父链 `BattleHud/Canvas/BattleDoors/Canvas/EndBattlePanel`）。
            //      ⇒ `FillParent`（锚点 0..1 + `sizeDelta 0`）**正是**那一对值（比原来写死 1920×1080 更贴判据：
            //      `Expand` 档下屏比画布宽时（21:9）画布 rect 比 1920 宽，写死的矩形会盖不满两边）。
            //    · ⛔ **`parent` 参数本件不再用**（先例 = `BattleDriver.BuildLoadingCover`）：面板**必须**挂在
            //      画布根 canvas 下才拿得到 `CanvasScaler`；⛔ **不挂 `hudRoot`** —— 那是**反向补震**用的，
            //      挂它下面会被补偿两次（口径 → `T09` §五·补⑨ 一·2）。
            //    · 改坏法：把 `FillParent` 换回 `MenuDraw.SetPxSize` ⇒ 整块面板缩成 **1/108**（画面「小得看不见」、
            //      **不报错**）；⛔ 这是本批的**头号静默坑**（`T09` §五·补⑩ 一）。
            var cv = BattleDoors.PairCanvas();
            if (cv == null) return null;      // `PairCanvas` 已出声；⛔ 不退回世界空间 quad（半迁移态更坏）
            var go = new GameObject("EndPanel", typeof(RectTransform));
            go.transform.SetParent(cv, false);
            HudCanvas.FillParent((RectTransform)go.transform);
            var p = go.AddComponent<EndPanel>();
            p.Build();
            p.Hide();
            return p;
        }

        void Build()
        {
            _root = transform;

            // 压暗背景：dump 里 `Background` 是一张 **3840×2160（两倍屏）的整屏图** ——
            // 也就是说原版的结算界面**把战场整块盖住**。我们没有那张图，用**重压暗**替代
            // （**这是替代，不是原版**）。压暗不够重的话，棋盘上的卡会从面板内容里透出来。
            // ⚠️ **压暗必须比卡靠前**（旧那套：z 越小越前）。原来建在 z=0 —— 和场上卡同层、
            //    而**手牌在 z 0~0.24（比它还远）**，于是手牌整排**没被压暗、从面板底下亮着**透出来
            //    （2026-09-12 看结算截图发现的；注释里早就担心「卡会透出来」，但 z 给错了）。
            //    `BattleDoors.ZDim` = −0.50：比开门视频(−0.55)和内容(−0.60)**都靠后**，
            //    但比任何一张卡都靠前。
            // 🔴 **2026-10-16（`T09` 批 2）改法**：uGUI 档就是**一张铺满的 `Image`**
            //    （`CardArt.Solid()` 那张 1×1 白图 + 黑 tint ⇒ 均匀压暗）。
            //    · **旧那套「按可见区宽高比现造一张纯色贴图」（`SolidTex`）退场** —— 那是为了绕过
            //      `ImageQuad` 的宽**只能由贴图比例推**；uGUI 的 `sizeDelta` 是显式写的
            //      （同一个做法见 `BattleDriver.BuildLoadingCover` 的 `LoadingMenuPanel`）。
            //    · **铺满用 `FillParent`**（⛔ 别写死 1920×1080，见 `Create` 那段）。
            //    · z 值一个没动 —— `MakeImagePiece` 先按默认 z（`HudCanvas.DefaultZ`）登记兄弟序，
            //      这里 `Reorder` 挪到压暗层（新那套 z = **兄弟序键**、大的画在底下，见 `BattleDoors` 头部）。
            _dim = HudCanvas.MakeImagePiece(_root, CardArt.Solid(), "dim", 0f, 0f,
                                            LayoutSpace.DesignPxW, LayoutSpace.DesignPxH,
                                            new Color(0f, 0f, 0f, 0.97f));
            if (_dim != null)
            {
                HudCanvas.FillParent(_dim.Rect);
                HudCanvas.Reorder(_dim.Rect, BattleDoors.ZDim);
                // 🔴 **2026-10-20（W57 · 战斗侧 `T09` 步骤 A3-d）：压暗层 = 全屏射线靶（模态闸）。**
                //   判据（**原版 prefab 现读**，`bundle_scenes_scenes_battlearena1` 与 `…battlearena2`
                //   两份同值）：`BattleDoors/Canvas/EndBattlePanel/**Background**` 那颗 `Image`（MB 4830 / 5043）：
                //     `m_RaycastTarget = 1` · `m_RaycastPadding = (0,0,0,0)` · **无 `Button` / `EventTrigger`**。
                //   ⇒ 对应计划 §2 A3「全屏件（含 `EndPanel._dim`）的 `raycastTarget` 置 true」——
                //   否则 uGUI 下点面板空白会把**背后的 HUD 钮**点掉（世界空间 quad 那会儿它们本来也收不到射线）。
                //   ⚠️ **只吃射线、不挂点击处理器**：原版这一屏**零按钮**（`EndPanel` 整棵子树只有
                //   `Background` / `Video Image` / `AllRewardsHolder`，一个 `m_OnClick` 都没有；2026-10-20 现读），
                //   而「点任意处离开」是 `BattleManager.Update` 里的**裸输入**
                //   `Input.GetMouseButtonDown(0)`（`BattleManager__Update.c:116-120`；我们那一条 →
                //   `BattleDriver.HandleEndBattleExit`）⇒ 它**不经过 uGUI**，⛔ 别在这里加点击动作。
                //   ⚠️ `m_RaycastPadding` 那 4 个 0 **不用显式写**： Unity 的 `Graphic.m_RaycastPadding`
                //   默认就是 `Vector4.zero`（现读本机 uGUI `…/com.unity.ugui/Runtime/UGUI/UI/Core/Graphic.cs:188`
                //   的字段初值 `new Vector4()`）。
                if (!_dim.SetRaycastTarget(true))
                    Debug.LogError("[EndPanel] 压暗层置 `raycastTarget` **失败**"
                                 + " ⇒ 结算面板背后的 HUD 钮会被**点穿**（模态闸失效、而画面完全正常）");
            }
            else Debug.LogError("[EndPanel] 压暗层没建出来（`CardArt.Solid()` 取不到）⇒ 结算界面后面会透出棋盘");

            // 内容层：标题 / 副标题 / 骷髅都挂这下面。
            // 开门视频在播的时候**整层藏起来**（原版那段视频自己就带 VICTORY/DEFEAT/DRAW 字样，
            // 见抽帧 `资料/战斗规格/战斗重建_0827/video_check_0828/frames/Victory_1_8.png`）——
            // 不藏的话我们的标题会叠在视频的字上，同一个词出现两遍。
            // 🔴 **2026-10-11（A218）**：`content` 是**我们自己的**中间层（原版 `EndBattlePanel` 下直接是
            //    `Video Image` / `AllRewardsHolder`，没有这一级）⇒ 矩形取它真正占的那块 = **屏矩形**。
            // 🔴 **2026-10-16（`T09` 批 2）**：`FillParent`（同根节点那条判据）；`Add(…, ZContent)` 把它压到
            //    开门视频**上面**那一层（`ZContent = −0.60` < `ZVideo = −0.55` ⇒ 兄弟序排在后面 = 画在上面）。
            var contentGo = new GameObject("content", typeof(RectTransform));
            contentGo.transform.SetParent(_root, false);
            HudCanvas.FillParent((RectTransform)contentGo.transform);
            _content = contentGo.transform;
            HudCanvas.Add((RectTransform)_content, BattleDoors.ZContent);

            // 结果：原版这块的文字是运行时填的，dump 里 `EndBattlePanel` 子树下没有结果文字节点
            //（只有 Video Image / AllRewardsHolder 那几块）→ **字号是我们挑的**。
            //
            // 🔴 **2026-10-18（第十二轮 · W6）就地补判据（铁律 5）**：原来这里只写了「子树下没有结果文字节点」
            //    —— **那句仍然成立**（原版 `EndBattlePanel` 确实没有标题节点，胜负靠那段视频），
            //    但**「所以没有词条」是错的**：原版**有** `Battle/BattleEnd/{Victory,Defeat,Draw}` 三条键，
            //    载体是 `bundle_menus_assets_all` 的 **`BattleLogItem`**（战报条目：
            //    `MonoBehaviour_-7028880557435028942.json` / `…_8607776031950241599.json` 的
            //    `victoryKey`/`defeatKey`/`drawKey` 三个 `LocalizedString` 字段）。
            //    ⇒ 我们这两句标题**本身仍是自加的**（如实标着），只是**取原版语义相同的键**（`Loc.T`），
            //      而不再借 `CardText.Phrase` 那条兜底路（那条路当年**根本没有这两个键** ⇒ 中文档印英文）。
            //    ⚠️ 英文列只有 `Victory` 有本地实据（那两颗 `result` TMP 的 `m_text = 'Victory'`）；
            //      `Defeat`/`Draw` 取不到 ⇒ 自拟，逐条写在 `Core/Loc.cs` 那一块。
            //
            // ⚠️ **纵向位置改过一轮（2026-09-12，加开门视频那轮）**：
            //    第一版把这一整块放在**上方**（标题 260 起），理由是「避开棋盘」——
            //    当时压暗还没铺满（`ImageQuad` 宽高比那个坑），棋盘会透出来。
            //    现在压暗铺满了，而且**原版的位置是知道的**：dump 里 `AllRewardsHolder`（奖励块的锚）
            //    在 **anchoredPosition (0, -260)** → 屏幕坐标 y = 540+260 = **800**，即**中心偏下**。
            //    加上开门视频之后更必须下移：徽章正片占着屏幕中央（抽帧见
            //    `资料/战斗规格/战斗重建_0827/video_check_0828/frames/Victory_1_8.png`），
            //    留在上方会正好糊在徽章上。
            //    所以标题/副标题/骷髅统一挪到 **y ≥ 730**，贴着原版那个锚点带。
            //
            // 🔴 **2026-10-16（`T09` 批 2）**：`Label.Create(...)` + `Content(px,py)` → **`HudCanvas.MakeText(...)`**。
            //    · **单位 = 设计 px**（⛔ 不是世界单位 —— 喂错**小到看不见、不报错**，见 `T09` §五·补⑨ 四）；
            //      `MakeText` 的 px/py 是**相对 `parent` 中心、y 向上**（与 `RectTransform` 同口径）。
            //    · 逐值换算：`Content(960, 260)` = 世界 `(0, +2.5926)` = **屏心上方 280 px** ⇒ `(0, +280)`；
            //      `Content(960, 730)` = `(0, −190)`。
            //    · **字号档一个没动**（`Label` 那三处「带单位的常数」内部按模式取 · uGUI 档 = 7 px/档）
            //      ⇒ 6 档在两种空间下**画出来一样大**（16:9 下逐位相同）。
            //    ⚠️ `MakeText` 拿不到 TMP 字体时**返回 null**（`Label.CreateUgui` 出声、⛔ 不退化成点阵）
            //      ⇒ 下面 `Show()` 里那几句都加了空判（原来 `Label.Create` 恒非 null，没这个必要）。
            _title = HudCanvas.MakeText(_content, "", 0f, 280f, 6, Color.white,
                                        new Vector2(0.5f, 0.5f), BattleDoors.ZContent, "end_title");
            // A1024③ 判不出（原版 `EndBattlePanel` 子树无 result 标题字）⇒ 保持 `Face.Cjk`，⛔ 不猜
            _sub = HudCanvas.MakeText(_content, "", 0f, -190f, 2, new Color(0.8f, 0.8f, 0.85f),
                                      new Vector2(0.5f, 0.5f), BattleDoors.ZContent, "end_sub");
            // A1024③ 判不出（同上）⇒ 保持 `Face.Cjk`

            // 骷髅行：底板 648.1×52.4，三个 64.3×71.2 的骷髅（尺寸是原版 dump 的）
            // ⚠️ 纵向位置按 `AllRewardsHolder` 的锚点 (0,-260) 排（见上面那段）
            // 🔴 **2026-09-27 照原版结构改**：三个骷髅是**那块底板的子节点**，整行一起开关 ——
            //    原版运行期 dump：`…/EndBattlePanel/AllRewardsHolder/SkullsHolder`（648.1×52.4 ·
            //    `40k_main_bt_nametag`）→ `skull1/2/3`（64.3×71.2 · `40k_battle_Win Skull`）
            //    （实据：`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv:1153-1156`）
            // 🔴 **2026-10-11（A218）**：骷髅行容器是 `RectTransform` + 写 `sizeDelta` —— 判据 = 原版
            //    `SkullsHolder` 实读（`bundle_scenes_scenes_battlearena1`，2026-10-11 现读）：
            //    `anchor (0,0) 重合 · pivot (0.5,0.5) · m_SizeDelta = (**648.1000, 52.3800**)`
            //    （与上面那段 dump 记的「648.1×52.4」同源；末位差异 0.02px 取本文件一直在用的 52.4，
            //     因为它跟着显示尺寸走，⛔ 别在别处再立一个）。
            //    🔴 **2026-10-16（`T09` 批 2）**：改走 `HudCanvas.SetPxSize`（**真 px**，⛔ 不是 `MenuDraw` 那个 ÷108 的）
            //    + `PosPx` 定位置（`Content(960, 790)` ⇒ 屏心上方 250 px ⇒ `anchoredPosition = (0, −250)`）；
            //    `Add(…, ZContent)` 让它跟内容层同层。
            _skullRow = new GameObject("SkullsHolder", typeof(RectTransform)).transform;
            _skullRow.SetParent(_content, false);
            {
                var rowRt = (RectTransform)_skullRow;
                HudCanvas.SetPxSize(rowRt, 648.1f, 52.4f);
                rowRt.anchoredPosition = HudCanvas.PosPx(960f, 790f);
                HudCanvas.Add(rowRt, BattleDoors.ZContent);
            }
            // 🔴 **2026-09-27 修（PA 普查抓的）**：原版那块底板 `m_PreserveAspect = 0`（`m_Type=0` Simple）
            //    ⇒ **拉满 648.1×52.4**；而「按贴图比例定宽」那一套（旧 `ImageQuad`）用的是贴图
            //    `40k_main_bt_nametag` = **109×41**（2.6585）⇒ 只画出 **139.3×52.4**，
            //    **窄了 508.8 px（只剩 21%）**。原版是「一条横贯的长底板」，我们是一小截。
            //    实据：`battlearena1` 的 `EndBattlePanel/AllRewardsHolder/SkullsHolder`
            //    （RT 2616 / GO 391 / Image MB 4938，直读 `m_PreserveAspect=0`、`m_Type=0`）。
            //    🔴 **2026-10-16（`T09` 批 2）**：那一对「先按贴图比例建、再 `SetAspect(648.1/52.4)` 盖掉」
            //    在 uGUI 档里**折成一个显式的 `sizeDelta`**（`ImageInParent` 的 w/h 参）—— 最终矩形
            //    **逐位不变**（648.1×52.4 = `52.4 × 648.1/52.4`）。
            _skullPlate = ImageInParent(_skullRow, CardArt.Ui("40k_main_bt_nametag"), "skull_plate",
                                        0f, 0f, 648.1f, 52.4f, BattleDoors.ZContent);
            var skullTex = CardArt.Ui("40k_battle_Win_Skull");
            // 旧那套是「给**高** 71.2 px、宽由贴图比例来」（`ImageQuad.Create` 的第 4 参是 worldHeight）——
            // uGUI 的 `Image.preserveAspect = false`（原版 `PA=0`）会**拉满**给它的矩形 ⇒
            // ⛔ 这里必须**自己**把宽按贴图比例算出来，否则画出来的是个被拉扁的方块。
            float skullW = 71.2f * ((skullTex != null && skullTex.height > 0)
                                    ? skullTex.width / (float)skullTex.height : 1f);
            float step = 64.3f + 12f;
            for (int i = 0; i < _skulls.Length; i++)
            {
                // `Content(960 + (i−1)·step, 790)` ⇒ 相对骷髅行中心（它自己就在 960 上）的 x 偏移 = 同一个数。
                float dx = (i - (_skulls.Length - 1) * 0.5f) * step;
                _skulls[i] = ImageInParent(_skullRow, skullTex, "skull_" + i,
                                           dx, 0f, skullW, 71.2f, BattleDoors.ZContent);
            }

            // 奖励行（原版 `RewardsHolder` 495.2×49.0 + `HolderRating` 187×45 / Trophy 60.1×60 / RatingText）
            // 🔴 **2026-09-17 用户拍板：不建** —— 奖励与评分都在服务器，单机版不做（见文件头）。只留上面的骷髅行。

            // 🔴 **2026-10-16（W22）：原来这里有一行「按 R 再来一局」的提示 —— 已删。**
            //    两条独立错处（判据 → `资料/普查产出_1016/判据_结算后出口.md` §2.3 / §2.6）：
            //      ① **原版这一屏零文字零按钮**：`EndBattlePanel` 子树只有 `Background` /
            //         `Video Image` / `AllRewardsHolder`（运行时 dump `runtime_ui_dump_Battle_Arena_1.tsv:1147-1164`，
            //         整棵 16 行），反编译里 `EndBattleDoors` 类**也没有任何 Text/Button 字段** ——
            //         结果字样就是那段开门视频本身。
            //      ② 那句话**说的事也不存在**：原版**没有「再来一局」**（`all_strings.txt` 里
            //         `rematch` / `play again` **零命中**）—— `R` 是**我们自己的调试键**
            //         （`真Play待验清单.md` D13 早已标「我们自己的」）。
            //    ⇒ 处置 = **照原版留空**（铁律 11：与原版不符的记下来之后【完全复刻】）。
            //    ⛔ 别再加回来 —— 真要提示「点任意处 / ESC 离开」，那也是原版没有的东西。
            //    ⚠️ `R` 那把调试键**照旧能用**，它的接线在 `BattleDriver.Update` 的 `Ctx.IsOver` 支里，
            //       那里写明「这是我们自己的键」。

            // 开门视频（原版 `EndBattleDoors`）。资产不在时它自己退回「没视频」。
            // 🔴 **2026-10-16（`T09` 批 2）**：`BattleDoors` 这一层挂在哪**没变**（还是 `_root` 下）——
            //    它自己保证「父件已在画布上就照旧挂下面」（见 `BattleDoors.Create`）。
            //    ⚠️ **父子关系与原版相反**（原版 `EndBattlePanel` 在 `BattleDoors/Canvas` **下**）——
            //    已知差异、**本次不翻**（文件头那条）。
            //    这里补一句 **z 记账**：它是 `_root` 的第三个子件，必须落在
            //    压暗(−0.50) 之后、内容(−0.60) 之前 ⇒ 兄弟序 [dim, doors, content] ⇒ 画出来
            //    **压暗在底、视频居中、内容在最上**（旧那套靠世界 z 排序，值一个没动）。
            _doors = BattleDoors.Create(_root);
            if (_doors != null) HudCanvas.Add((RectTransform)_doors.transform, BattleDoors.ZVideo);
        }

        /// <summary>显示结算。
        /// </summary>
        /// <param name="winner">`RuleCore` 那套：0=没结束，1/2=玩家序号+1，3=平局。</param>
        /// <param name="myIndex">我是几号玩家（0 基）。</param>
        /// <param name="skullsObtained">这局**已达成**的里程碑档数 —— 🔴 **2026-10-06（A148）改**：
        /// 原版「这局拿了几颗」= `BattleScoreManager.GetSkullCount()`（数 `HealthThresholdData.AlreadyAccomplished`
        /// 那些**只在「敌方督军生命变化」信号里置位**的标志，开局一个都没置）⇒ 这个数**必须由调用方给**
        /// （`BattleDriver._foeSkullCount`），面板**不许再拿生命反推** —— 反推在遭遇局开局会多出一颗
        /// （起始生命 15/20 ⇒ `SkullsFor(20) = 1`，而原版那时是 0）。</param>
        /// <param name="rounds">打了几个回合。</param>
        /// <param name="forfeitedBy">谁投降的（`BattleContext.ForfeitedBy`，`-1` = 正常打完）。
        /// 投降和「督军倒下」是**两种结局**（原版 `BattleResult.Forfeit`），副标题得说清是哪种。</param>
        /// <param name="minFoeWarlordHealth">敌方督军降到过的最低生命（`-1` = 不知道）。⚠️ **只用于副标题
        /// 那行字** —— 原版 `EndBattlePanel` 子树里**没有**这行说明，**它是我们自加的**（如实标着）；
        /// 与骷髅数**不再是同一件事**（2026-10-06 起拆开）。</param>
        /// <param name="skipSequence">🆕 2026-10-18（A940 收尾）：**跳过「正常结算演出」那一段**
        /// （= 原版 `TutorialStage.skipNormalBattleEndOnVictory` / `…OnDefeat`
        ///  —— **字段偏移 / 判据**：`d:/2/tools/il2cpp_out/dump.cs:42066`（`+0x60`）与 `:42068`（`+0x70`），
        ///  `TutorialStage` TypeDefIndex 828）。
        ///
        /// 🔴 **读数点如实标（本地产物里查不到）**：R2 §4 把它的消费者记成
        /// `BattleManager__BattleFinished.c` 的 `BasicBattleEndSequence` 分支 —— 我把那三份都读了
        /// （`BattleManager__BattleFinished.c` · `BattleManager__BasicBattleEndSequence.c` ·
        ///  `BattleManager._BasicBattleEndSequence_d__391__MoveNext.c`），**它们都没有读 `stage+0x60/+0x70`**
        /// （也没有 `GetCurrentTutorialStage`）；全库交叉搜「`GetCurrentTutorialStage` 之后 800 字符内出现
        /// `+ 0x60)` / `+ 0x70)`」**零命中**；🔁 换**文件级共现**那种搜法复核过，命中的 3 份
        /// （`_SetupMulliganPhase_d__341` / `DeadHero` / `GetWinnerAfterBattleEnd`）**全是「不同基址」的假命中**
        /// （详见 `BattleDriver.TutorialSkipsNormalEnd` 的注释）⇒ **真正的读数点在本地产物里拿不到**
        /// （疑似方法体缺失，同「教程那份 `PlayScriptedTurn` 的 `.c` 缺失」那一族）。
        /// ⛔ **不假装知道它在哪**：下面这条语义（「跳掉演出、直接进结算面板」）是按**字段名 + R2 的结论**
        /// 定的，**不是照原文照抄的**。
        /// ⚠️ 6 关的数据**两个值全是 false** ⇒ 今天**零可见影响**（铁律 11：机制仍要接上）。
        /// </param>
        public void Show(int winner, int myIndex, int skullsObtained, int rounds, int forfeitedBy = -1,
                         int minFoeWarlordHealth = -1, bool skipSequence = false)
        {
            Visible = true;
            ShownSkulls = skullsObtained;
            // 🔴 **状态与文案在这里分开**（见 `EndOutcome` 的 doc）：先定**枚举**，文案再由它推。
            Outcome = winner == 3 ? EndOutcome.Draw
                    : (winner == myIndex + 1 ? EndOutcome.Victory : EndOutcome.Defeat);

            // ⚠️ **先激活再写文字** —— 反过来的话 `Label.SetText` 在未激活的物体上跑，
            //    字形网格没重建，标题/副标题/评分全是空白（截图才发现）
            SetVisible(true);

            // 🔴 **2026-10-16（`T09` 批 2）**：两条都加了**空判** —— `HudCanvas.MakeText`（= `Label.CreateUgui`）
            //    在拿不到 TMP 字体时**返回 null**（出声，⛔ 不退化成点阵），而原来 `Label.Create` 恒非 null。
            if (_title != null) _title.SetText(ResultText);   // = `Loc.T(Battle/BattleEnd/*)`，中文档印中文
            // ⚠️ 副标题那行**是我们自加的**（原版 `EndBattlePanel` 子树里没有它）。
            //    🔴 **2026-10-18（第十二轮 · W6）补判据**：原版**有**三块战报统计的键 ——
            //    `Battle/BattleEnd/DamageDone` · `Battle/BattleEnd/Tactics` · `Battle/BattleEnd/TroopsDead`
            //    （`d:/2/tools/il2cpp_out/stringliteral.json` `0x4285268/0x4285368/0x4285460`）——
            //    但它们是**三个「标签 + 数值」的统计格**，**不是**我们这行「N 回合　敌方战将最低生命 X」；
            //    我们这行的形状原版没有对应件 ⇒ **仍按「我们自加」如实标**（⛔ 别拿那三条硬套）。
            //    🔴🔴 **2026-10-18（第十五轮 · `G5`）：那三个格的宿主本批查了一遍 —— 本地取不到。**
            //    方法体判据齐（`decomp_full/BattleEndPlayerData__Setup.c`，
            //    `BattleEndPlayerData` = TypeDefIndex 532，字段 `troopsText`(0x58) / `tacticsText`(0x60) /
            //    `damageText`(0x68)）：
            //      拼法 = `GetTranslation(词条) + ": " + 数值`（那个 `": "` 字面量 = `0x42C7DF8`，
            //      `stringliteral.json` 实读）；**顺序 = TroopsDead → Tactics → DamageDone**
            //      （值取 `MatchResultData + 0x48` 那个结构体的 `+0x10 / +0x14 / +0x18`）。
            //    **但它没有实例**：`assets_full`（91 个包 / 24.7 万文件）全盘按类名与字段名
            //      （`BattleEndPlayerData` / `troopsText` / `tacticsText` / `damageText` / `quoteText` /
            //       `cantLoseRankText`）搜过 —— **只命中 `globalgamemanagers/MonoScript/MonoScript_3980.json`
            //       那一条类注册**，**没有任何 prefab / 场景实例**；`Setup` 的调用点在
            //      `decomp_full` 的 25,096 个方法体里也**一个都搜不到**（只有它自己）。
            //      ⇒ 宿主 prefab 本地取不到 ⇒ **这一格不是「原版没有」，而是「我们看不见它长什么样」**
            //      （按铁律 11 记 **要做**，判据已抄在本段；**先做哪一步** → 先找到/进真 Play 看到那件 prefab）。
            //    ⚠️ 同族的 `EndBattlePanel` 子树（13 个战场场景里都有）里**没有**这三个格
            //      （它只有 `Background` / `Video Image` / `AllRewardsHolder`）⇒ ⛔ 别在结算面板上硬加。
            if (_sub != null)
                _sub.SetText(forfeitedBy >= 0
                             ? $"{rounds} 回合   " + (forfeitedBy == myIndex ? "我方投降" : "对方投降")
                             : (minFoeWarlordHealth >= 0
                                ? $"{rounds} 回合   敌方战将最低生命 {minFoeWarlordHealth}"
                                : $"{rounds} 回合"));  // 没传最低生命（-1）就只说回合数，**不编一个数出来**

            // 🔴 **未点亮的骷髅 = 【根本不显示】**，而且 **0 个的时候整行（连底板）都藏起来** ——
            //    原版 `EndBattleDoors__ShowRewards.c:55` `skullHolderObj.SetActive(0 < skullsObtained)`
            //    ＋ `:58-68` 只把**前 N 个** `SetActive(true)`（其余保持不激活）——
            //    **不是半透明**。⚠️ 原来我们 3 个都建、暗的 alpha 0.18（当时自标「我们挑的」）⇒ 2026-09-27 改掉。
            //    ⚠️ 判据来源与「10/0x14/0x1e 是 `BattleWinner` 枚举」同一轮（那个方法体里就两件事：选片 + 这排骷髅）。
            if (_skullRow != null) _skullRow.gameObject.SetActive(ShownSkulls > 0);
            for (int i = 0; i < _skulls.Length; i++)
            {
                if (_skulls[i] != null) _skulls[i].gameObject.SetActive(i < ShownSkulls);
            }

            // ---- 开门（原版 `EndBattleDoors`）----
            // 原版 `SetupDoor` 返回 `VideoClip.length + 一个常量`，调用方拿它等 —— 我们照这个约定，
            // 片长由 `BattleDoors` 拿着，`Advance` 推够就停。
            BattleDoors.Result doorRes = Outcome == EndOutcome.Victory ? BattleDoors.Result.Victory
                                       : Outcome == EndOutcome.Defeat ? BattleDoors.Result.Defeat
                                       : BattleDoors.Result.Draw;
            float len = _doors == null ? 0f : _doors.SetupDoor(doorRes);
            bool video = len > 0f;
            if (video)
            {
                _doors.Play();
                // 🔴 **`skipSequence` = 把这一段演出【一帧推完】**（原版那两个开关的语义，见 `Show` 的
                //    `<param name="skipSequence">` —— 读数点本地拿不到，这是我们按字段名定的边界）。
                //    ⚠️ **为什么必须「推完」而不是「不 SetupDoor」**：出口那个闸门是
                //    `ExitReady = Visible && _doors.Finished`（本文件 `:94`），而 `_finished` **只有**
                //    两处会置真：没片时 `SetupDoor` 自己置、有片时 `Advance` 推够 `_length`。
                //    ⇒ 光「不播」会把闸门**永久关死**（玩家出不去，静默）。
                //    ⚠️ 代价（**2026-10-18 已消除**）：`BattleDoors.Advance` 把解码器 `Pause` 在最后一帧、
                //    **留着当底图**（那是它既定的收尾行为，见它 `Stop` 的注释）⇒ 「跳掉」在这一档下
                //    原来**仍会看到那段视频的最后一帧**。
                //    🆕 现在紧跟一句 `DropFrame()`（= `Z1′`，`BattleDoors.cs` 那一件本轮已授权）——
                //    **只推完、再把那帧扔掉**：闸门照旧由 `Advance` 开，画面不再留底。
                //    ⛔ 两句顺序不能反（`DropFrame` 不碰闸门，但 `Advance` 之前丢帧等于什么都没丢）。
                //    ⚠️ 只有**跳过**那一档丢帧；正常那一路照旧停在最后一帧（**原版如此**，别顺手改）。
                if (skipSequence)
                {
                    _doors.Advance(_doors.Length);
                    _doors.DropFrame();
                }
            }

            // 奖励跟视频**同时**出。判据不是猜的：`EndBattleDoors.SetupDoor` 里
            // **自己就调了 `EndBattleDoors__ShowRewards(...)`**（反编译 `decomp_out/EndBattleDoors__SetupDoor.c:105`，
            // 前面先是一串把奖励物件 SetActive(false) 的调用），入场靠
            // `DG_Tweening_TweenSettingsExtensions__SetDelay(..., _DAT_1834b2bbc, ...)`（同文件 :194）——
            // 那个常量正好也是 `SetupDoor` 加在 `clip.length` 上的那个。
            // **所以不是「播完才揭晓」**（第一版我做成了播完才揭晓，那是错的，已改）。
            SetContentVisible(true);

            // 但**结果文字要藏**：这段视频自己带着 VICTORY / DEFEAT / DRAW 字样
            //（抽帧 `资料/战斗规格/战斗重建_0827/video_check_0828/frames/Victory_1_8.png`），
            // 而且原版 `EndBattlePanel` 子树里**根本没有结果文字节点** —— 结果字就是那段视频本身。
            // 不藏的话同一个词在屏幕上出现两遍。
            SetTitleVisible(!video);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 推时间。批处理里由 `BattleScene.Step` 手动推（那里没有帧循环）；真机由 `BattleDriver.Update` 每帧推。
        /// **时长按片长自己算，不依赖解码器出没出帧** —— 时序因此是可断言的。
        /// 播完之后 `BattleDoors` 会把解码器 `Pause` 住（**最后一帧留着当底图，不销毁**）。
        /// </summary>
        public void Advance(float dt)
        {
            if (!Visible || _doors == null || !_doors.Playing) return;
            _doors.Advance(dt);
        }

        void SetContentVisible(bool v)
        {
            if (_content != null) _content.gameObject.SetActive(v);
        }

        void SetTitleVisible(bool v)
        {
            if (_title != null) _title.gameObject.SetActive(v);
        }

        /// <summary>自检用：**奖励行应当一个都不建**（用户 2026-09-17 定：单机不做奖励与评分，只留骷髅）。
        /// 按**节点名**数（字段已经删了，数不到才说明真去干净了）。</summary>
        public int RewardRowPieces
        {
            get
            {
                if (_root == null) return 0;
                int n = 0;
                foreach (var t in _root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "reward_plate" || t.name == "trophy" || t.name == "rating") n++;
                return n;
            }
        }

        public void Hide()
        {
            Visible = false;
            ShownSkulls = 0;
            Outcome = EndOutcome.None;          // 🔴 状态复位（`ResultText` 由它推出来 = 空串）
            if (_doors != null) _doors.Stop();
            SetVisible(false);
        }

        void SetVisible(bool v)
        {
            foreach (var t in _root.GetComponentsInChildren<Transform>(true))
                t.gameObject.SetActive(v);
            // 根自己开着（否则 SetActive(false) 之后没法再打开），只有子物体跟着切
            gameObject.SetActive(true);
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 2）：在 `parent` 里按【相对父中心的设计 px】建一枚 uGUI 图**
        /// （中心锚 + 中心枢轴；`dx/dy` **y 向上**，`w/h` = 真 px）。
        /// <para>🔑 **为什么不能直接用 `HudCanvas.MakeImage` / `QuadFromTopPx`**：那两个把 `x/y` 当成
        /// **屏幕绝对 px**（锚点写 `x/1920`、`1 − y/1080`）—— 只有当 `parent` **恰好是一整屏矩形**时，
        /// 「屏幕绝对」才等于「相对父中心」。本面板里 `SkullsHolder`（648.1×52.4）就**不是**整屏
        /// ⇒ 走那一支会把骷髅摆到别处（**静默**，而且 16:9 下也错）。
        /// ⛔ **不自己写建图那一套**：底下的 `Sprite` / `Image` / 登记兄弟序全部转发给
        /// `HudCanvas.MakeImagePiece`，这里**只重写锚点/枢轴/位置/尺寸这四个字段**。</para>
        /// <para>⚠️ `parent` 不是整屏矩形 ⇒ 那四个字段必须**显式**写死（不能沿用 `MakeImage` 按
        /// 1920×1080 算出来的锚点）。</para></summary>
        static HudPiece ImageInParent(Transform parent, Texture2D tex, string name,
                                      float dxPx, float dyPx, float wPx, float hPx, float z)
        {
            if (tex == null) return null;         // 与旧 `ImageQuad.Create` 同一条：贴图取不到就不建（它出声）
            var p = HudCanvas.MakeImagePiece(parent, tex, name, 0f, 0f, wPx, hPx, Color.white);
            if (p == null || p.Rect == null) return p;
            var rt = p.Rect;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(dxPx, dyPx);
            rt.sizeDelta = new Vector2(wPx, hPx);
            HudCanvas.Reorder(rt, z);             // `MakeImagePiece` 先按默认 z 登记，这里挪到本层
            return p;
        }

        // ── 🆕 2026-10-20（W57 · `T09` 步骤 A3-d）：**Editor-only 只读探针** ────────────────────
        //  ⚠️ 只读、且**播放器构建里不存在**（`#if UNITY_EDITOR`）。断言写在 `Editor/BattleScene.cs`
        //     （本波口径 = **只做不验** ⇒ 本件不写断言）。
        //  ⚠️ 这一组存在的理由：本件**一个按钮都没有**（原版该屏零按钮，判据见 `Build()` 里压暗那一段），
        //     所以没有 `BattleHudButton.ClickCountForTest` 可读 ⇒ 改成读「压暗层到底吃不吃射线」。
        //     🔑 断言写法（给 `Editor/` 那侧）：用 `EventSystem.RaycastAll` 在屏心/四角各打一次，
        //     首中必须是 `DimGraphicForTest.gameObject`（= 面板背后的 HUD 收不到）；压暗**关着**时同一批点
        //     首中不能是它（配对 = 灭自证）。
#if UNITY_EDITOR
        /// <summary>压暗层那一件（`null` = 没建出来）。Editor-only 只读探针。</summary>
        public HudPiece DimPieceForTest { get { return _dim; } }

        /// <summary>压暗层的 uGUI `Image`（`null` = 没建 / 不是 uGUI 件）。Editor-only 只读探针。</summary>
        public UnityEngine.UI.Graphic DimGraphicForTest
        {
            get
            {
                if (_dim == null || !_dim.IsUgui) return null;
                var go = _dim.gameObject;
                return go != null ? go.GetComponent<UnityEngine.UI.Image>() : null;
            }
        }

        /// <summary>压暗层吃不吃射线。**原版判据 = `Background.m_RaycastTarget = 1`** ⇒ 这里应为 `true`。
        /// Editor-only 只读探针（`false` 也可能是「没建出来」，要用 <see cref="DimGraphicForTest"/> 分清）。</summary>
        public bool DimRaycastTargetForTest
        {
            get { var g = DimGraphicForTest; return g != null && g.raycastTarget; }
        }

        /// <summary>压暗层的 `raycastPadding`（**L,B,R,T** —— Unity `Graphic.raycastPadding` 的序）。
        /// 原版 `(0,0,0,0)`。Editor-only 只读探针；没有那件时返回 **四个 `NaN`**（⛔ 不拿 0 冒充「查过」）。</summary>
        public Vector4 DimRaycastPaddingForTest
        {
            get
            {
                var g = DimGraphicForTest;
                return g != null ? g.raycastPadding
                                 : new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
            }
        }
#endif
    }
}
