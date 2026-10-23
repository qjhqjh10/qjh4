// SearchingOpponentWindow.cs — 阶段二第 3 层第 6 件：**`SearchingOpponentWindow`（找对手那扇全屏窗）**
//
// ============================ 出处（唯一正本）============================
// `资料/阶段二_战斗入口_原版规格.md` **§一（窗口参数）+ §二 D（逐节点表）**；
// 2026-09-24 建它时**又从根实读了一遍**（`工具/menu_rect.py` / `menu_dump.py`）——
// 补上正本表里没有的：`Found Player Container` 的 1138.41² 矩形与 `Player Name` 的字号/位置、
// 敌方那一格的 **`scl=(-1,1)` 镜像**、`Cancel Match` / `Button Text` 的矩形。
//
// 🔴 **窗口参数**：`type=0 **Fullscreen**` · `windowsPlacement=0 **None**` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`
//    —— **四个战斗入口窗里只有它是 Fullscreen + 无锚点**（另三扇都是 `1 Popup`；§一 的表）。
//
// ============================ 空态就是这个窗的常态 ============================
// 原版 `UISearchingOpponentPlayerInfo.Initialize(data, name)`（反编译）：`data == null` 时
//   `notFoundPlayerContainer.SetActive(true)` + `foundPlayerContainer.SetActive(false)` ——
// **找不到人 = 空态**。我们本地**没有对手**（也没有服务器）⇒ **对手那一格永远走空态**，这是如实画。
// ⚠️ 玩家那一格：原版也是拿服务端下发的玩家档案去填（`DeckAndWarlordData` + 玩家名）。
//    我们**没有玩家档案** ⇒ 两样都按原版 prefab 里的**自有值**来：立绘用**当前选中卡组的督军**
//    （那是真数据），名字用 prefab 里那行字 `Player name`（**不编一个名字出来**）。
//
// ---- 本地入口（**我们定的**）----
// 原版谁开这扇窗**本地查不到**（全量反编译里 `SearchingOpponentWindow` 没有开它的调用点，
// 与那四扇窗的入口同一个原因：LiveOps/匹配管理器在服务端配置里）。⇒ 按 §五 那条口径：
// **入口由我们定并如实标出来** —— 挂在**排位窗**的 `Battle!` 上（排位本来就是「等真人」那个模式），
// 其余三扇窗继续用窗口内的 `Searching Oponent Popup`。**这一条映射不是复刻。**
//
// 🆕 **2026-10-18（A932）：本窗多了一颗【自建的】「提示行」节点**（`NetMatchmaking.OnHint` 的消费方）——
//   **原版没有它**（用户当天拍板「接入」）。为什么非接不可 · 哪些参数是我们挑的 · 对端文本那一支（A961）
//   → 见下面那颗节点的注释块（搜 `A932`）。
using UnityEngine;
using CardPresentation.Net;

namespace CardPresentation
{
    /// <summary>`SearchingOpponentWindow` —— 全屏「正在找对手」。</summary>
    public class SearchingOpponentWindow : GameWindow
    {
        /// <summary>🔴 **2026-10-19（`N117`）**：本窗**盖住左竖导航**。
        /// <para>**为什么必须覆写**：基类那条判据（`type==Popup || placement==Popup`）在本窗上是
        /// **两条都不成立**（原版 prefab 实读 `type = 0 Fullscreen` · `windowsPlacement = 0 None` ——
        /// 四窗里只有它这样），但它的 `Background` 是**整屏 1920×1080 的压暗背板**
        /// （`QSr = 3120`，见 `Build` 里那句 `Menu Dark Background`）⇒ 匹配原版语义：左栏被它吃住。
        /// ⚠️ 不加这一条 ⇒ 「正在找对手」时点左栏会**走导航**（`QNavHit = 3156 &gt; QSr = 3120`）。</para></summary>
        public override bool BlocksLeftNav { get { return true; } }

        // ⚠️ `Cancel Match` 的队列**要比立绘高一档**（两者矩形有重叠：785–885 与 1035–1135 × 942–1018）
        public const int QSr = 3120, QSrArt = 3121, QSrArt1 = 3122, QSrText = 3123, QSrHit = 3124;

        // ---- 几何（§二 D + 2026-09-24 实读）----
        public const float BgL = -964.67f, BgT = -541.74f, BgR = 2884.67f, BgB = 1621.74f;
        public const float TitleL = 741.12f, TitleT = 136.00f, TitleR = 1178.88f, TitleB = 186.00f;
        public const float PlyL = -0.01f, PlyT = 1.00f, PlyR = 631.45f, PlyB = 1079.00f;
        public const float PlyFoundL = -253.49f, PlyFoundT = -29.21f, PlyFoundR = 884.93f, PlyFoundB = 1109.21f;
        public const float PlyNameL = 53.84f, PlyNameT = 853.00f, PlyNameR = 491.60f, PlyNameB = 903.00f;
        public const float PlyNotFoundL = 265.72f, PlyNotFoundT = 490.00f, PlyNotFoundR = 365.72f, PlyNotFoundB = 590.00f;
        public const float EnL = 1288.55f, EnT = 1.00f, EnR = 1920.00f, EnB = 1079.00f;
        public const float EnFoundL = 1035.07f, EnFoundT = -29.21f, EnFoundR = 2173.48f, EnFoundB = 1109.21f;
        public const float EnNameL = 1342.40f, EnNameT = 853.00f, EnNameR = 1780.16f, EnNameB = 903.00f;
        public const float EnNotFoundL = 1554.28f, EnNotFoundT = 490.00f, EnNotFoundR = 1654.28f, EnNotFoundB = 590.00f;
        public const float CxlL = 785.00f, CxlT = 942.00f, CxlR = 1135.00f, CxlB = 1018.00f;
        public const float CxlTxL = 798.00f, CxlTxT = 941.39f, CxlTxR = 1122.00f, CxlTxB = 1017.39f;
        public const string ArtButton = "40K_button";
        public static readonly Vector4 BtnBorder = new Vector4(234f, 46f, 234f, 46f);
        public const float BtnTexW = 489f, BtnTexH = 107f;
        /// <summary>玩家/敌人名那行字 —— **原版 prefab 里写的就是 `Player name`**（运行时才被真名覆盖）。</summary>
        public const string PlaceholderName = "Player name";

        // ==================================================================
        //  🆕 2026-10-18（A932）：**提示行** —— `NetMatchmaking.OnHint` 在**本窗**上的消费方
        //
        //  🔴 **这颗节点是【我们自建的】、原版没有** —— 判据 = **用户 2026-10-18 拍板「接入」**
        //     （账在 `资料/待办判据_1018.md` §B27 末尾：「要接得先裁」）。
        //     为什么非接不可：排位那条路走的就是**本窗**（全屏 `SearchingOpponentWindow`），
        //     而它原来**只有 `Title`（437.76×50 · 无 auto）· 两个 `Player Name` · 一颗 `Cancel Match`**，
        //     **没有任何一行放得下一句状态话** ⇒ 大厅阶段那几句（对面掉线 / 离开 / 回来）在本窗上
        //     **一个字都看不见**（「不许静默」那条红线的另一种形态：屏幕上在说假话）。
        //
        //  ⛔ **下面这些几何不是「原版就是这样」，是【我们挑的】**（铁律 3：原版没有这颗节点）：
        //     · **样式照同族那一颗** —— `Shell/SearchingMatchPopup.cs` 的 `Main Search message`
        //       （autosize **4~50** · base **36** · 折行 **700** · Center/Middle）。⚠️ 那是**另一扇窗**的节点；
        //     · **位置** = `Title`（y 136..186）**正下方** · **水平居中**（屏宽 1920 ⇒ x 960±350），
        //       框照同族那颗取 **700×148**。**这两条全是我们的选择。**
        //
        //  🔴 **对端文本那一支（A961）**：本行的话来自 `NetMatchmaking.LastHint`，它由
        //     `NetMatchmaking.HandleLobbyPeerClosed(why)` / `HandleLobbyPeerLost()` / `DeferToBattleLayer(what)` 拼出来，
        //     而那句里夹着的对端文本**在源头就钳过了** —— `NetSession.ClampPeerText` 是**一处闸**、四个收包入口
        //     （`MsgBye.reason` / `MsgProof.name` / `MsgAck.reason` / `MsgReject.reason`，清单见
        //     `NetProtocol.MaxPeerTextChars` 的注释）⇒ **对端可塞进来的那一段原串**到本窗时 ≤ 40 字。
        //     ⛔ **这里绝不写第二份钳**（会把自己写的中文提示也当成对端文本截掉）—— 同 `NetMatchmaking` 里
        //     `case NetKind.Deck` / `case NetKind.Start` 那两处「不写第二份钳」的口径。
        //     🔴 **2026-10-19（`A1083`）就地订正（铁律 5）**：这里原来写「走到本窗的字符串**已经 ≤
        //     `NetProtocol.MaxPeerTextChars`**」—— **第六会话 `P6d` 之后这半句不成立**：收侧现在是
        //     「**先钳、再取词**」，取完词那一句是**我们自己的文案**（`Core/Loc.cs` 的 EN 列，最长的一条 **140 字**）
        //     ⇒ 到这一行时**可以远超 40**。那条 ≤ 40 的上界管的是**对端可控的原串**，
        //     **不是取词之后的整句** —— 两件事，别混（错因：写这句时还是「发侧渲染好的整句」那套）。
        //     ⚠️ 本行按框放得下多少，判据 = `SearchingMatchPopup.HintLineMaxWidth`（**按字形宽度**算：
        //     80 个半宽字位 = 中文 40 字 ≈ 英文 80 字；推导与实测见那颗常量的注释块）—— 超了会**出声**
        //     （见 `ShowHint`），⛔ **不截断**（截了就是把该玩家看的话吃掉）。
        // ==================================================================
        public const float HintL = 610f, HintT = 200f, HintR = 1310f, HintB = 348f;   // 700×148 —— **我们挑的**

        /// <summary>此刻那行提示（`null` = 没有提示、那一行是空的）。自检读它。</summary>
        public string HintText { get; private set; }

        /// <summary>那一行字（`Build()` 建；出厂空串）。</summary>
        Label _hintLine;

        /// <summary>联机层要对玩家说一句（大厅阶段的掉线 / 离开 / 回来）⇒ **写在那一行上**；传空的 = 收回。
        /// 🔴 **只由 `NetMatchmaking.OnHint` 推**（`OnEnable` 订、`OnDisable` / `OnDestroy` 摘）——
        /// ⛔ 别在这儿自己判状态。判据与「为什么接在本窗」见上面那一节。</summary>
        public void ShowHint(string text)
        {
            if (string.IsNullOrEmpty(text)) { ClearHint(); return; }
            // 🔴 **2026-10-19（`A1083`）**：判据从「`text.Length > 40`」（只对中文档成立）换成
            //    **按字形宽度算**（与同族 `SearchingMatchPopup.ShowHint` 同一个口、同一把尺子 ——
            //    ⛔ 别在这两扇窗上各写一份算式）。**照旧只出声**：⛔ 不截断、⛔ 不静默、照旧照画。
            int width = SearchingMatchPopup.HintLineWidth(text);
            if (width > SearchingMatchPopup.HintLineMaxWidth)
                Debug.LogWarning($"[SearchingOpp] 提示行那句话占 {width} 个半宽字位（{text.Length} 个字符），"
                               + $"超过这一行放得下的 {SearchingMatchPopup.HintLineMaxWidth} 位"
                               + $"（= 中文 {SearchingMatchPopup.HintLineMaxChars} 字 / 英文约 80 字；"
                               + "框 700×148 · 自适应 4~50px，与同族 `Main Search message` 同一档）"
                               + "—— 会被压到比 50px 更小的字号，请把这一句写短"
                               + "（详细的那半句留给弹窗，两处本来就是两个口）：「" + text + "」");
            HintText = text;
            if (_hintLine != null) _hintLine.SetText(text);
            else Debug.LogWarning("[SearchingOpp] 提示行那颗节点不在（`Build` 没跑过 / 被销毁了？）⇒ "
                                  + "这句话**没画出来**（**不是静默**）：「" + text + "」");
            Debug.Log("[SearchingOpp] 提示行改口：「" + text + "」");
        }

        /// <summary>收回提示 ⇒ 那一行变回空的（= `NetMatchmaking.Reset()` 推 `OnHint(null)` 那一支）。
        /// ⚠️ 出厂本来就是空的 ⇒ 没提示时调它**什么都不做**（`HintText == null` 那道闸）。</summary>
        public void ClearHint()
        {
            if (HintText == null) return;
            HintText = null;
            if (_hintLine != null) _hintLine.SetText("");
            Debug.Log("[SearchingOpp] 提示行收回");
        }

        // ---- 订/摘 `NetMatchmaking.OnHint`（**只在真的活着的时候**订）----
        //  🔴 为什么挂 `OnEnable`/`OnDisable` 而不是 `Open()`/`Close()`：与 `Shell/SearchingMatchPopup.cs`
        //     同一处口径（`Open()` 可以重复调 ⇒ 挂它会订两次、还要摘两次）；`OnDestroy` 也必须摘
        //     （不摘的话，窗销毁之后提示一来就 `MissingReferenceException`）。
        //  ⚠️ `GameWindow` 基类**没有** `OnEnable`/`OnDisable`（只有 `OnDestroy` 在 `WindowsManager` 那边）
        //     ⇒ 这里声明不会撞名。
        void OnEnable() { NetMatchmaking.OnHint += ShowHint; }
        void OnDisable() { NetMatchmaking.OnHint -= ShowHint; }

        // ==================================================================
        //  🆕 「找到对手」那一态（联机那一支）—— 判据 → `资料/阶段二_多人界面_原版规格.md` §6·4
        //
        //  🔴 **原版这份 build 里这一态是【死代码】**：`SearchingOpponentWindow.OpponentFound(…)` 零调用点，
        //     原版窗只有「我方 Found + 敌方 Not Found」这一个态（= 我们原来照它做的那个）。
        //     ⇒ **「什么时候显示、显示多久」没有原版可抄** —— 下面这几条是**我们定的**：
        //       · 触发 = 联机配对拿到了对面那副牌（`NetMatchmaking.HasOpponent`）；
        //       · 时长 = **1s**（取原版 `MatchMakerManager.StartBattleWithDelay` 的 `waitLoadTime` 默认值，
        //         但「拿它当展示时间」是我们加的）；
        //       · 名字 = **机器名**（原版显示服务端账号 `playFabId`，本地没有对等物）。
        //  ⚠️ 单机（打 bot）那条路**原版也不显示这一态**（`ChangeToBotBattle` 全程不调它）⇒ 我们也不显示。
        // ==================================================================

        /// <summary>本窗要不要盯着联机配对结果（**排位窗那一支会置真**，联机接管了才有效）。</summary>
        public bool NetWatch;

        /// <summary>「找到对手」展示多久再切战场。**1s = 原版 `waitLoadTime` 的默认值**
        /// （`MatchMakerManager.StartBattleWithDelay`）；⚠️ **拿它当展示时间是我们挑的**（正本 §6·4）。</summary>
        public float PresentationHold = 1f;

        Transform _foeFound; PxRect _foeFoundRect, _foeNameRect; GameObject _foeNotFound;
        float _holdLeft = -1f;

        // ==================================================================
        //  🆕 **2026-10-19（`A1322` 残留 · U3）：这一局被撤 ⇒ 这扇窗自己关**
        //
        //  判据/口径 → `资料/普查产出_1019/L1_报告.md` §6·5 第 1 条（`NetMatchmaking.RevokeForLateCancel`
        //  那一笔的注释里也写着「那扇窗由玩家自己关 —— 要收干净 = 在 `Shell/` 这一侧监听并 `Close()`」）。
        //
        //  🔴 **信号怎么拿**：⛔ 不改 `Net/**`、也没有专供的事件 ⇒ 用**现有公开状态**判：
        //    这一局「活着的」两个特征 = `NetMatchmaking.Waiting`（我交了卡组、还没开局）
        //    或 `NetMatchmaking.HasOpponent`（对面那副牌到手）。
        //    `RevokeForLateCancel` / `ClearMatch` 两支都会把两格一起清空 ⇒ **两个都变假 = 这一局没了**。
        //    ⚠️ 为什么**不**拿 `OnHint` 的文案比：那是本地化字符串、还可能被别的支路说同一句
        //    （判字符串 = 把显示文案当状态位，改一个词就静默失效）。状态位才是判据。
        //  ⚠️ `_sawLiveMatch` 那道闸：只有**开窗时真有一局**（或「找到对手」那一跳跑过）才允许自己关 ——
        //    否则「开着一扇没有对局的窗」会在第一帧就自己关掉（自检夹具正是这一档）。
        bool _sawLiveMatch;

        /// <summary>自检用：把「这一局曾经活着」这一格置真（= `Open()` 在真匹配路径上做的那件事）。
        /// ⚠️ 生产路径**不调**它 —— 它只让自检能在没有联机会话夹具时把状态机推到「曾经有局」那一档。</summary>
        public void NoteMatchLiveForTest() { _sawLiveMatch = NetWatch; }

        /// <summary>自检/调试：把「切场景」这件事接过来（展示 `PresentationHold` 秒后自己走）。
        /// 正常路径由 `NetMatchmaking.HoldForPresentation` 调。</summary>
        public bool HoldSceneForPresentation(MsgStart st)
        {
            _sawLiveMatch = true;   // 「找到对手」都到了 ⇒ 这一局确实活过（被撤时要自己收窗）
            if (NetMatchmaking.HasOpponent && NetMatchmaking.FoeDeck != null)
                ShowOpponent(NetMatchmaking.FoeDeck.WarlordId, NetMatchmaking.FoeName);
            else
                Debug.LogWarning("[SearchingOpp] `HoldSceneForPresentation` 被调了，但**拿不到对面那副牌**"
                                 + "（`NetMatchmaking.HasOpponent == false`）⇒ 敌方那格仍走空态（出声）");
            _holdLeft = PresentationHold;
            return true;
        }

        /// <summary>把敌方那一格从 `Not Found` 翻成 `Found`：**对手本局那套牌里的督军立绘** + 名字。
        /// 🔴 立绘取的是**对手本局那副牌的督军**（原版 `Initialize` 里就是 `deckWarlord.cardImageReference`，
        /// 与对手头像无关 —— 正本 §6·4 第 3 条）。</summary>
        public void ShowOpponent(string warlordId, string playerName)
        {
            if (_foeFound == null) { Debug.LogWarning("[SearchingOpp] 窗还没建（`Build` 没跑过）⇒ 画不了对手那一格"); return; }
            for (int i = _foeFound.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_foeFound.GetChild(i).gameObject);
            var tex = string.IsNullOrEmpty(warlordId) ? null : CardArt.Portrait(warlordId);
            if (tex != null)
            {
                var q = MenuDraw.Rect(_foeFound, tex, _foeFoundRect, "Warlord Image", QSrArt, null, true);
                // 🔴 原版敌方那格的立绘是 `m_LocalScale = (-1,1,1)`（两人面对面）—— **只翻立绘、不翻名字**。
                //    ⚠️ 别给父节点设 scale：`MenuDraw` 是「先算世界坐标再减父位置」，父一有 scale
                //    子件的位置会被再乘一次（`资料/已知的坑.md:306`）⇒ 改成**翻 uv 的 x**
                //    （与 `CollectionWindow.BuildStyleArrow` 的镜像同一招）。
                if (q != null) q.SetUvRect(new Rect(1f, 0f, -1f, 1f));
            }
            else
                Debug.LogWarning("[SearchingOpp] 对手的督军立绘取不到（id=" + warlordId + "）—— 那一层不画，出声");
            MenuDraw.Text(_foeFound, _foeNameRect,
                          string.IsNullOrEmpty(playerName) ? PlaceholderName : playerName,
                          Color.white, "Player Name", 36f, QSrText, face: TmpFont.Face.LatinCardBody);
            _foeFound.gameObject.SetActive(true);
            if (_foeNotFound != null) _foeNotFound.SetActive(false);
            Debug.Log("[SearchingOpp] **找到对手**：督军「" + warlordId + "」· 名字「"
                      + (string.IsNullOrEmpty(playerName) ? PlaceholderName : playerName) + "」"
                      + "（立绘已按原版 uv 镜像；名字不翻）");
        }

        /// <summary>自检/调试：展示结束时要做什么 —— 默认走 `NetMatchmaking.GoNow()` 真切场景；
        /// 自检会换掉它（批处理里不能真切场景）。</summary>
        public System.Action OnPresentationDone;

        /// <summary>推进「展示 → 切场景」（`Update` 与自检都走它 —— 批处理没有帧循环）。</summary>
        public void Tick(float dt)
        {
            // 🆕 **2026-10-19（`A1322` 残留 · U3）**：这一局被撤（迟到的 `match.cancel` / 大厅阶段对面取消 /
            //   `Reset()`）⇒ **这扇窗自己关**，回大厅。判据见 `_sawLiveMatch` 那一节。
            //   ⚠️ 必须排在 `_holdLeft` 之前：被撤之后不能再走 `GoNow()` 那条「切战场」的路。
            if (_sawLiveMatch && NetWatch && !NetMatchmaking.Waiting && !NetMatchmaking.HasOpponent)
            {
                _sawLiveMatch = false;
                Debug.Log("[SearchingOpp] 这一局已经**被撤**（对面迟到的取消 / 大厅阶段取消 / 匹配复位）"
                        + "⇒ 这扇窗自己关掉、回大厅（`A1322`：不再等玩家按 Esc/Cancel 收窗）");
                Close();
                return;
            }
            if (_holdLeft < 0f) return;
            _holdLeft -= dt;
            if (_holdLeft > 0f) return;
            _holdLeft = -1f;
            Debug.Log("[SearchingOpp] 展示完毕 ⇒ 让路切战场（`NetMatchmaking.GoNow`）");
            if (OnPresentationDone != null) OnPresentationDone(); else NetMatchmaking.GoNow();
        }

        void Update() { Tick(Time.deltaTime); }

        /// <summary>关窗/销毁时**必须**把那口气放掉 —— 否则这一局会卡在「切不了场景」上（静默失败）。</summary>
        void ReleaseHold()
        {
            if (NetMatchmaking.HoldForPresentation == (System.Func<MsgStart, bool>)HoldSceneForPresentation)
                NetMatchmaking.HoldForPresentation = null;
            _holdLeft = -1f;
        }

        // ==================================================================
        //  🆕 **2026-10-23（W62 · 界面线）**：ESC 关这扇窗 ⇒ **必须把搜索真的取消掉**
        //
        //  🔴 **病灶**（真机 + 静态都对上）：本类原来的 `Close()` 只调 `ReleaseHold()`（放掉「切场景前那一口气」），
        //     **没有任何「取消搜索」**（不回调 `OnCancel`）⇒ 玩家按 ESC 关掉全屏匹配窗之后，
        //     匹配那一局**还挂着**（`SearchingMatchPopup` 的 `Searching` / 常驻 ticker 的注册 / 联机那一局的账都没清），
        //     而屏幕上**一个字都不剩**（玩家看不到任何提示）。
        //
        //  🔴 **原版判据**（第一权威 = 反编译，**本类自己的** `Close` 覆写）：
        //     `d:/2/tools/decomp_full/SearchingOpponentWindow__Close.c` 全文两句 ——
        //       ① `GameWindow__Close(this)`（= 基类那一套）；
        //       ② `if (MatchMakerManager 单例 != null) Everguild_MatchMakerManager__CancelSearch(manager, 0)`。
        //     ⇒ 原版**这扇窗的 `Close()` 一定取消搜索**。另外两个旁证：
        //       · `SearchingOpponentWindow__CancelButtonClick.c` = 尾调 `Close()`（虚表 `0x1b8`）⇒ **`Cancel` 钮也是走 `Close` 取消的**；
        //       · `SearchingOpponentWindow__OnDisable.c` **不取消**（它只 `RemoveListener`）。
        //     ⚠️ 原版 `Everguild.MatchMakerManager__CancelSearch.c` 本身是**有闸的轻动作**
        //     （`if (SearchOpponentManager 字段 != null) { CancelBattleSearch(停协程); 事件; }`），所以它无条件调没有副作用。
        //
        //  🔴 **为什么我们这边要加一道闸（`_escClose`）而不是照抄「无条件」**：我们 `OnCancel` 接的是
        //     **`RankedEventWindow.CancelSearch`** —— 它比原版那个重得多：`_search.AbortSearch()`（清状态 + 注销 ticker）
        //     **＋ 联机那一支 `NetMatchmaking.Cancel(...)`（会往对面发 `match.cancel`）＋ 自己关窗**。
        //     而本类有**两条原版没有的程序关窗路**，无条件取消会把它们打成回归：
        //        · `RankedEventWindow.OnSearchFinished()` → `_searchWin.Close()`（**配到人了 / 等满 12 秒要开战**）；
        //        · 本文件 `Tick()` 的 `A1322` 那一支（**这一局已经被撤**，再撤一次 = 往对面重发取消 + 弹一句「取消失败」——
        //          `NetMatchmaking.Cancel` 此刻没有活着的账 ⇒ 返回 false ⇒ 会弹 `Settings/Online/MatchCancelFailed`）。
        //     ⇒ 实现 = `ESCPressed()` 里置一个「这次关窗是玩家按 ESC 引起的」瞬时标志（`_escClose`），
        //       `Close()` 只在那一位为真时取消 —— 于是**恰好覆盖原版那两个调用点**（ESC / `Cancel` 钮），
        //       两扇程序关窗不受影响。**两道门槛（输入层 / `closeOnEsc`）仍由基类判**（⛔ 别在这儿抄第二份）。
        //
        //  ⚠️ **与 `Cancel()` 的关系**：那条路本来就取消（`CancelMatchMatchmaking → CancelSearch`），
        //     现在与 ESC 共用 `CancelSearchOnce()` ⇒ **两条路都只取消一次**（幂等）。
        // ==================================================================

        /// <summary>这次 `Close()` 是不是**玩家按 ESC** 引起的 —— 只有它才取消搜索（见上面那一节）。</summary>
        bool _escClose;

        /// <summary>`OnCancel` 是否已经发过（**幂等闸**）。
        /// 🔴 **非它不可**：`OnCancel` 在生产上是 `RankedEventWindow.CancelSearch`，而那条链**自己会再把这扇窗关掉**
        /// （`_searchWin.Close()`）⇒ 不幂等就会 `Close → OnCancel → Close → OnCancel …` **绕成无限递归**。</summary>
        bool _cancelSent;

        /// <summary>取消搜索 —— **只发一次**（见 `_cancelSent`）。没接 `OnCancel` 时**出声**（红线：不许静默失败）。</summary>
        void CancelSearchOnce()
        {
            if (_cancelSent) return;
            _cancelSent = true;
            if (OnCancel != null) OnCancel();
            else Debug.LogWarning("[SearchingOpp] 要取消搜索，但**没有接 `OnCancel`** ⇒ 这一局不会被撤"
                                + "（谁开的这扇窗？生产上 `RankedEventWindow.OpenSearchWindow` 会接）");
        }

        /// <summary>ESC / 返回：原版这条路上**一定会取消搜索** ——
        /// `SearchingOpponentWindow__Close.c`（本类自己的 `Close` 覆写）= `GameWindow.Close()` + `MatchMakerManager.CancelSearch()`；
        /// 而 `GameWindow__ESCPressed` 正是 `Close()`（虚表 `0x1b8`）。旁证：弹窗那一支
        /// `SearchingMatchWindowDemo__ESCPressed.c:24` 末尾也是**无条件** `CancelSearch()`。
        /// <para>⚠️ **先取消、再由基类关窗**（原版是 `Close()` 然后 `CancelSearch()`，两者顺序反过来）——
        ///   因为我们的 `CancelSearch` 那条链**自己会关窗**（`RankedEventWindow.CancelSearch → _searchWin.Close()`），
        ///   而 `Close()` 会经 `ShowPreviousWindow → TryOpen → Build()` **把宿主整棵重建、销毁旧的那份搜索弹窗**；
        ///   若先关窗，`AbortSearch()` 就打在重建后的新弹窗上（它本来就没在搜）⇒ **旧那份的状态永远清不掉**
        ///   （托管字段仍是 `Searching == true`）。原版两条路都以「取消 + 关窗」收尾，净效果一致。</para>
        /// <para>两道门槛（`PointerLayer.InputEnabled` / `closeOnEsc`）**仍然只有基类那一份**：门槛没过时基类直接
        /// `return false`、**根本不调 `Close()`** ⇒ `_escClose` 这一位也就不会触发取消（= 原版那一支的语义）。</para></summary>
        public override bool ESCPressed()
        {
            _escClose = true;
            try { return base.ESCPressed(); }
            finally { _escClose = false; }
        }

        public override void Close()
        {
            // 原版 `SearchingOpponentWindow__Close.c` 是「基类 Close + `MatchMakerManager.CancelSearch()`」——
            // 那一句本身是**有闸的轻动作**，所以原版无条件调；而我们的 `OnCancel` 会往对面发 `match.cancel`
            // ⇒ 只在**玩家关窗**（ESC）这一支取消，两条程序关窗（配到人 / 这一局被撤）不动。判据见上面那一节。
            if (_escClose) CancelSearchOnce();
            ReleaseHold();
            base.Close();
        }

        void OnDestroy() { ReleaseHold(); NetMatchmaking.OnHint -= ShowHint; }

        /// <summary>点 `Cancel` 的回调（排位窗用它取消匹配）。</summary>
        public System.Action OnCancel;

        internal int DeckIndex;

        public static SearchingOpponentWindow Create(WindowsManager mgr, int deckIndex)
        {
            var go = new GameObject("SearchingOpponentWindow");
            var win = go.AddComponent<SearchingOpponentWindow>();
            win.type = WindowType.Fullscreen;                 // 实证 type=0
            win.placement = WindowsPlacement.None;            // 实证 windowsPlacement=0（**四窗里只有它这样**）
            win.closeOnEsc = true;                            // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            win.Manager = mgr;
            win.DeckIndex = deckIndex;
            WindowsManager.AttachToAnchor(win);               // 无锚点 ⇒ 原地不动（照原版）
            return win;
        }

        /// <summary>最近一次开出来的那扇（自检用）。</summary>
        public static SearchingOpponentWindow LastOpened;

        public override void Open()
        {
            LastOpened = this;
            Build();
            // 🆕 2026-10-19（`A1322` 残留 · U3）：开窗那一刻**真的有一局**吗（联机那条路 = `TryStart` 已过）
            //   ⇒ 记下来，之后这两格被清空 = 这一局被撤 ⇒ `Tick` 自己关窗。判据见 `_sawLiveMatch` 那一节。
            _sawLiveMatch = NetWatch && (NetMatchmaking.Waiting || NetMatchmaking.HasOpponent);
            // 🆕 联机那一支：开窗即注册「切场景前那一口气」—— 配到人时由它把「找到对手」显示出来。
            //    ⚠️ 单机（打 bot）那条路**不注册** ⇒ 原来那条 12 秒链的行为一字不改。
            if (NetWatch)
            {
                NetMatchmaking.HoldForPresentation = HoldSceneForPresentation;
                Debug.Log("[SearchingOpp] 联机那一支：已接管「切场景前那一口气」（配到人时展示 "
                          + PresentationHold.ToString("0.##") + "s）");
            }
        }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            // 重建 ⇒ 把上一轮记下的敌方那格三个件作废（它们已经被销毁了）
            _foeFound = null; _foeNotFound = null;
            // 🆕 A932：提示行那一颗同理（旧的那颗已被销毁；状态也归零 —— 重开一扇窗不许留着上一局那句话）
            _hintLine = null; HintText = null;
            // 🆕 **2026-10-23（W62）**：重开 = 新的一局 ⇒ 幂等闸复位（上一次取消过，不妨碍这一局再取消一次）
            _cancelSent = false;

            MenuDraw.Rect(root, CardArt.Solid(), new PxRect(BgL, BgT, BgR, BgB),
                          "Background", QSr, new Color(0f, 0f, 0f, 0.71f));
            // 原版 hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            MenuDraw.Text(root, new PxRect(TitleL, TitleT, TitleR, TitleB), "Searching opponent",
                          Color.white, "Title", 36f, QSrText, face: TmpFont.Face.LatinCardBody);

            // 🆕 **2026-10-18（A932）：提示行**（**自建节点、原版没有** —— 判据 / 为什么接在本窗 /
            //   哪些参数是我们挑的 → 类头那一节）。出厂**空串**：没提示时它不占话。
            //   样式照同族 `SearchingMatchPopup` 的 `Main Search message`（autosize 4~50 · base 36 · 折行 700）。
            _hintLine = MenuDraw.Text(root, new PxRect(HintL, HintT, HintR, HintB), "", Color.white,
                                      "Hint Line", 50f, QSrText,   // A1024③ 判不出（原版无此节点，我们自加）⇒ 保持 Cjk
                                      wrapPx: HintR - HintL, autoMinPx: 4f, autoMaxPx: 50f, autoBasePx: 36f);

            // 玩家那一格：**有数据** ⇒ 走 `Found`（立绘 = 当前选中卡组的督军）
            BuildSide(root, "Searching Opponent Player Info Container",
                      new PxRect(PlyL, PlyT, PlyR, PlyB),
                      new PxRect(PlyFoundL, PlyFoundT, PlyFoundR, PlyFoundB),
                      new PxRect(PlyNameL, PlyNameT, PlyNameR, PlyNameB),
                      new PxRect(PlyNotFoundL, PlyNotFoundT, PlyNotFoundR, PlyNotFoundB),
                      true, false);
            // 对手那一格：**没有对手** ⇒ 走 `Not Found`（照原版 `Initialize(null, …)` 那一支）
            BuildSide(root, "Searching Opponent Enemy Info Container",
                      new PxRect(EnL, EnT, EnR, EnB),
                      new PxRect(EnFoundL, EnFoundT, EnFoundR, EnFoundB),
                      new PxRect(EnNameL, EnNameT, EnNameR, EnNameB),
                      new PxRect(EnNotFoundL, EnNotFoundT, EnNotFoundR, EnNotFoundB),
                      false, true);
            Debug.Log("[SearchingOpp] 对手那一格开局画的是**空态**（照原版 `Open`：我方格 `Initialize(真数据)`、"
                      + "敌方格显式 `notFound.SetActive(true)`）—— 本地单机没有对手，如实画；"
                      + "联机配到人时由 `ShowOpponent` 翻成 `Found`（那一步原版是死代码，见正本 §6·4）");

            // `Cancel Match`：原版是 **`type=Simple` + `preserveAspect`**（**不是 Sliced**！）
            //   —— 正本 §二 D 注⑥ 原来写「照原版 type=Sliced 会退化」是**错的**，已就地更正。
            var cancelQ = MenuDraw.Rect(root, CardArt.MenuUi(ArtButton), new PxRect(CxlL, CxlT, CxlR, CxlB),
                          "Cancel Match", QSrArt1, new Color(0.369f, 0.894f, 0.587f, 1f), true);
            // 原版 Button Text hAlign = **Center** ⇒ 不调 `Align*`
            MenuDraw.Text(root, new PxRect(CxlTxL, CxlTxT, CxlTxR, CxlTxB), "Cancel", Color.white,
                          "Button Text", 38f, QSrText, face: TmpFont.Face.LatinCardBody);
            // 🆕 A17：原版 `SearchingOpponentWindow` 根 `Cancel Match` 是 SpriteSwap（普查 §块 5 第 27 行）
            MenuDraw.Hit(root, "CancelHit", new PxRect(CxlL, CxlT, CxlR, CxlB), QSrHit, Cancel,
                         cancelQ, ArtButton);
        }

        /// <summary>一格玩家信息：`Found`（立绘 + 名字）与 `Not Found`（100² 空框）**二选一**。
        /// `isFoe = true` 时把这一格的三个件记下来，供 <see cref="ShowOpponent"/> 事后翻成 `Found`。
        /// ⚠️ 立绘的镜像**不在这里做**（这时候敌方那格还没有立绘）—— 见 `ShowOpponent` 的注释。</summary>
        void BuildSide(Transform root, string name, PxRect box, PxRect found, PxRect nameR, PxRect notFound,
                       bool hasData, bool isFoe)
        {
            var side = MenuDraw.Node(root, name, box);
            var f = MenuDraw.Node(side, "Found Player Container", found);
            var wl = CollectionData.Warlord(DeckIndex);
            // ⚠️ 用 `Portrait(wl.Id)` 而**不是** `PortraitByName`（后者按卡名取、同名卡会取错，
            //    `CardArt` 的注释里明确劝新代码别用它；这里 id 就在手上）
            var tex = hasData && wl != null ? CardArt.Portrait(wl.Id) : null;
            if (hasData && tex != null)
                MenuDraw.Rect(f, tex, found, "Warlord Image", QSrArt, null, true);
            else if (hasData)
                Debug.Log("[SearchingOpp] 督军立绘取不到（" + (wl != null ? wl.Name : "这套卡组没有督军") + "）—— 那一层不画，出声");
            // 名字：**用原版 prefab 自带的那行字**（运行时才被真名覆盖）—— 不编一个名字出来
            MenuDraw.Text(f, nameR, PlaceholderName, Color.white, "Player Name", 36f, QSrText, face: TmpFont.Face.LatinCardBody);

            // `Not Found Container`：原版**没有 Image**（只有 RectTransform，100×100）⇒ 照原版**空着**
            var nf = MenuDraw.Node(side, "Not Found Container", notFound);
            f.gameObject.SetActive(hasData);
            nf.gameObject.SetActive(!hasData);

            if (isFoe) { _foeFound = f; _foeFoundRect = found; _foeNameRect = nameR; _foeNotFound = nf.gameObject; }
        }

        /// <summary>点 `Cancel`（原版 `CancelMatchMatchmaking → MatchMakerManager.CancelSearch`）。
        /// 🔴 **2026-10-23（W62）**：改走 `CancelSearchOnce()` —— 与 ESC 共用同一道幂等闸，
        /// 否则「先回调、再 `Close()`」会让实现侧变成两条路各取消一次（而生产上 `OnCancel` 那条链**自己也会关窗**）。</summary>
        public void Cancel()
        {
            Debug.Log("[SearchingOpp] `Cancel Match` —— 取消匹配（原版 `CancelMatchMatchmaking`）");
            CancelSearchOnce();
            Close();
        }
    }
}
