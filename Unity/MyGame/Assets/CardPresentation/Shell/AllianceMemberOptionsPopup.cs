// AllianceMemberOptionsPopup.cs — **联盟成员操作面板**（原版 `AllianceMemberOptionsPopup : GameWindow`）
//
// ============================ 出处（唯一正本）============================
// prefab：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 根 GO `Member Options Panel`
//   · 窗口参数 MB = `MonoBehaviour_2699463439353286802` 的**兄弟件**（本扇那一颗由 pid 反查，
//     见下面「窗参」—— 与 `GenericOptionsPanel` 同一个骨架、**没有 `Template`**）；
//   · 逐节点几何 = `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Member Options Panel" --depth 12 --md`
//     （绝对框与 `--relative` 框两份都核过）；
// 行为 = 反编译 `d:/2/tools/decomp_full/AllianceMemberOptionsPopup__{Awake,Open,Close,ShowConfirmation}.c`
//   ＋ 字段偏移表 `dump.cs`（`TypeDefIndex 2543`；`GroupMember` = 2526 / `GroupRole` = 2527）。
//
// ============================ 窗参（MB 逐字段实读）============================
// `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
// `extraScaleSmallScreen = 1.0`（= **不覆盖**；烤着的是 `menuScale = 1.35`，同 `GenericOptionsPanel`）·
// `openSound/closeSound = null`、`useDefaultCloseSoundIfNull = 1`。
// 八个字段各指哪个节点（`dump.cs` 的偏移表 + prefab 的 pid 反查，8/8 逐个对上）：
//   `playerName(+0x70) → Name` · `addAsFriendButton(+0x78) → Add as a friend` · `viewProfileButton(+0x80) → Profile`
//   `promoteButton(+0x88) → Promote` · `demoteButton(+0x90) → Demote` · `kickPlayerButton(+0x98) → Kick`
//   `quitButton(+0xA0) → Quit` · `challengeButton(+0xA8) → Challenge` · `DEBUG_ADDSKULLS(+0xB0) → Debug Add Skulls`
//   ⚠️ **字段顺序 ≠ 兄弟序**（`Challenge` 是**第一颗**子件，却挂在 `+0xA8` 上）⇒ 建树按 `m_Children`，
//   映射按字段表，两件事分开读（同 `TrophyInfoPopup` 那条「七字段各指哪个节点」的口径）。
//
// ============================ 🔴 原版的显隐模型（`Open()` 逐句读出来的）============================
// 设 `me` = 本机玩家的 `GroupMember`、`m` = 被选中的那一员：
//   · `sameGroup  = String.Equals(me.groupId, m.groupId)`      （两个 `+0x20`）
//   · `outrank    = sameGroup && m.role < me.role`             （`role` = `GroupMember.+0x48`，枚举 `GroupRole`）
//   · `isSelf     = myPlayfabId == m.id`                       （`+0x10`；`myPlayfabId` = 单例的 `+0x328`）
//   · `isFriend   = FriendsData.IsFriend(m.id)`
// 于是（**每条四个字段的来源都写在同一行**）：
//   `Challenge`        = `!isSelf`
//   `Add as a friend`  = `!isSelf && !isFriend`
//   `Profile`          = `!isSelf`
//   `Promote`          = `outrank && role < 3 (Leader)`
//   `Demote`           = `outrank && role > 0 (Member)`
//   `Kick`             = `outrank`
//   `Quit`             = `isSelf`
//   `Debug Add Skulls` = **永远关**（`Awake()` 第一句：`DEBUG_ADDSKULLS.gameObject.SetActive(false)`）
// 另外 `Open()` 还会把 `Promote` 那颗的**文字**按 role 换掉：
//   `role == 2 (Admin)` ⇒ I2 词条 **`SocialMenu/Alliances/TransferLeadership`** · 否则 ⇒ **`SocialMenu/Alliances/Promote`**
//   （两个 term key 从二进制字面量表实读：`stringliteral.json` 的 `0x184253cb0` / `0x184253ab0`）。
//   🔴 **词条表在远端 CCD、本地一个 value 都没有** ⇒ 我们用 **prefab 出厂原文 `Promote`**
//   （同一颗的 `Localize` 组件在 prefab 里就是**禁用**的 —— dump 的 `⛔MULTI-COMP(5组件·1禁用)` 那条）。
//
// ============================ 🔴 三处「照原版自己的分支」 ============================
// ① **`Debug Add Skulls` 运行期恒关**（`Awake()`）—— prefab 出厂 `act = T`、字段 `m_Enabled = 1`，
//    **它是靠代码关的**。我们照运行期那一档建（关），并在断言里两态都钉。
// ② **`Template` 这一扇根本没有**（根的直接子件只有 5 个：`Menu Dark Background` / `bg shadow` / `bg` /
//    `Name` / `Buttons`）—— ⛔ 别照 `GenericOptionsPanel` 顺手补一颗。
// ③ 🔴 **地板的 `Menu Dark Background` 的 `Image.m_Color` 是 `(0,0,0,0)` —— `a = 0`，整块透明**
//    （与 `GenericOptionsPanel` 逐值相同；同族别的窗是 `(0,0,0,0.773)`）。它只**吃点击**
//    （uGUI 的 `Graphic.Raycast` 不看 alpha，`m_RaycastTarget = 1` 就够）。⛔ 别顺手改成 0.773。
//    ⚠️ **两扇唯一的差**：本扇的 `BackgroundOverDrawController` 是**启用**的（`GenericOptionsPanel` 那颗被禁）
//    —— 它是一条**全局栈**（`OnEnable` 把栈顶那颗 `Image` 置 `enabled = false`、自己入栈；
//    `OnDisable` 出栈并恢复），管的只是「同一时刻只画最上面那一个整屏底」。
//    **它不写 `m_Color`**（`BackgroundOverDrawController__{OnEnable,OnDisable}.c` 里只 `Behaviour.set_enabled`）
//    ⇒ 透明仍然是透明。我们**没有**那条栈（本仓窗与窗不叠整屏底）⇒ 如实记，不实现。
//    ⇒ **布局上的差**：`GenericOptionsPanel` 靠 `Start()` 关掉 `Template` ⇒ 根高 61.6；本扇**没有那一件**
//      ⇒ 根高 = `10 + 36.6 + 5 + 515 + 10 = 576.6`（dump 实测 576.6 ✓，**两档同值** —— 这扇没有那个歧义）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `AllianceMemberOptionsPopup`（`GameWindow` 子类）—— 联盟成员列表里点某一员弹出的操作菜单。
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3340–3347**（`GenericOptionsPanel` 3330–3337 之上、
    /// 聊天窗 3300 / 挑战弹窗 3400 之间 —— 层带不许重叠）。</para>
    /// <para>⚠️ **本地走不到这条路**：八个按钮的显隐**全要服务器**（`GroupMember` / `FriendsData` 都在
    /// PlayFab 那侧）⇒ 这扇窗在正常路径上开不出来（`Open()` 里数据为空时原版直接抛）。
    /// 详见 `Shell/AllianceMemberTab.cs:1357` 那条记账。</para></summary>
    public class AllianceMemberOptionsPopup : GameWindow
    {
        // ============================================================ 队列档（本窗自成一档 · 3340–3347）
        public const int QShade = 3340,        // 压暗/吃点击的整屏底（`Menu Dark Background`，**a = 0**）
                         QShadeHit = 3341,     // 上面那件的命中区
                         QBgShadow = 3342,     // `bg shadow`
                         QBg = 3343,           // `bg`（面板底，不透明）
                         QName = 3344,         // `Name`（成员名）
                         QBtnBg = 3345,        // 八颗按钮的底
                         QBtnText = 3346;      // 八颗 `Button Text`
        /// <summary>本窗**内容命中区**那一档。</summary>
        public const int QHit = 3347;

        // ============================================================ 冻结值（全部 dump / MB 实读 · 画布 px）
        public const float W = 387.30f;                       // 根 `sizeDelta.x`
        public const float PadTop = 10f, PadBottom = 10f;     // 根 VLG 的 `m_Padding.m_Top/.m_Bottom`
        public const float Spacing = 5f;                      // 根 VLG 的 `m_Spacing`
        public const float NameH = 36.60f;                    // `Name` 的 `LayoutElement.m_PreferredHeight`
        public const float NameW = 355f;                      // `Name` 的 `m_SizeDelta.x`
        public const float BtnW = 357.30f;                    // 每颗按钮的 `m_SizeDelta.x`
        public const float BtnH = 60f;                        // 每颗按钮的 `LayoutElement.m_PreferredHeight`
        public const float BtnStep = 65f;                     // 60 + `Buttons` 的 `m_Spacing` 5
        public const float BtnTextW = 330.77f, BtnTextH = 86.17f;   // `AspectRatioFitter(3.8386404514312744)` 跑完
        public const float BtnTextDx = 12.6881f;              // 357.3 × 0.0355112
        /// <summary>根在画布 y 向下坐标里的 pivot 点 y = `540 − anchoredPosition.y` = 540 − (−223.3)。
        /// ⚠️ 本扇 pivot 是 **`(0.5, 0)`（底）** —— 与 `GenericOptionsPanel` 的 `(0.5,1)`（顶）**反着**，
        /// 所以「高度变了往哪边长」也反着：这一扇是**往上长**（底沿恒在 763.30）。逐份实读。</summary>
        public const float DefPivotBottomY = 540f - (-223.3f);
        public const float ShadowHalfW = 31.58625f, ShadowHalfH = 39.49215f;
        /// <summary>`bg shadow` 的 `anchoredPosition.x` —— ⚠️ **本扇是 −0.8670044**，而 `GenericOptionsPanel`
        /// 是 −0.8671875（同一个设计值、两份 float 落盘不同）⇒ **逐份实读、别互抄**（铁律 5·c）。</summary>
        public const float ShadowDx = -0.8670044f;
        public const float ShadeW = 4574.60f, ShadeH = 2572.36f;      // 以**面板中心**为中心
        public const float CharWidth = 355f;                          // 未用（留档：`Name` 的框宽见 `NameW`）
        /// <summary>八颗按钮的**竖直步进**下的 `Buttons` 高度 = `60×8 + 5×7 = 515`（dump 实测 515.00 ✓）。</summary>
        public const float ButtonsH8 = 515f;

        public const string ArtShadow = "OctagonUI_Filled_SDF";                  // 工程里没有 ⇒ 出声、不画
        public const string ArtBg = "40k_topmarquee_currency_display_BW";
        public const string ArtBtn = "UI_Button_Mulligan",
                            ArtBtnHover = "UI_Button_Mulligan_hover",
                            ArtBtnPressed = "UI_Button_Mulligan_Pressed";
        public const float BtnTextFont = 35f, BtnTextFontBase = 12f,
                           BtnTextFontMin = 10f, BtnTextFontMax = 35f;
        /// <summary>八条文字各自的 `m_fontSize`（**只有最后一条不同** —— 逐颗实读，别拿一个顶一片）。</summary>
        public const float DebugTextFont = 26.3f;
        public const float NameFont = 38.6f, NameFontBase = 36f, NameFontMin = 18f, NameFontMax = 72f;
        public static readonly Color ShadowTint = new Color(0f, 0f, 0f, 0.44705883f);
        public static readonly Color BgTint = new Color(0.31132078f, 0.20118371f, 0.20118371f, 1f);
        /// <summary>整屏底：**`a = 0`**（见文件头③）。</summary>
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0f);
        /// <summary>烤在 prefab 里的 `menuScale`（同 `GenericOptionsPanel.MenuScale`，逐值实读）。</summary>
        public const float MenuScale = 1.35f;

        // ============================================================ 八颗按钮的冻结点表
        /// <summary>一颗按钮的**静态事实**（名字 / 标签 / tint / 字号 / 指向哪个 `[SerializeField]` 字段）。
        /// ⚠️ 标签是 **prefab 出厂原文**（西班牙语那份 —— 原版靠 `Localize` 在运行期换掉，
        /// 我们**没有词条表**，同 `TrophyInfoPopup.FeatureLabel` 的口径）。
        /// ⚠️ `Tint` 是那颗 `Image.m_Color` 的**序列化值**（逐颗实读）—— `Promote` 绿 / `Demote`·`Kick`·`Quit` 红 /
        /// `Debug` 品红，是**原版设计者按语义打的色**（`EverguildButtonMaterialModifier` 是 `IMaterialModifier`、
        /// **不写 `graphic.color`** ⇒ 这些 tint 真的会画出来）。⛔ 别「统一成白的」。</summary>
        public struct Btn
        {
            public string Node, Label, Field;
            public Color Tint;
            public float Font;
        }

        /// <summary>**兄弟序**逐字照 `m_Children`（`Challenge` 第一 —— 它的字段偏移 `+0xA8` 是最后第二个，
        /// 两件事不是一回事）。</summary>
        public static readonly Btn[] Buttons =
        {
            new Btn { Node = "Challenge",         Label = "Retar",                        Field = "challengeButton",    Tint = new Color(1f, 1f, 1f, 1f),                                    Font = BtnTextFont },
            new Btn { Node = "Add as a friend",   Label = "Añadir como amigo",            Field = "addAsFriendButton",  Tint = new Color(1f, 1f, 1f, 1f),                                    Font = BtnTextFont },
            new Btn { Node = "Profile",           Label = "Perfil",                       Field = "viewProfileButton",  Tint = new Color(1f, 1f, 1f, 1f),                                    Font = BtnTextFont },
            new Btn { Node = "Promote",           Label = "Promote",                      Field = "promoteButton",      Tint = new Color(0.007393122f, 1f, 0f, 1f),                          Font = BtnTextFont },
            new Btn { Node = "Demote",            Label = "Degradar",                     Field = "demoteButton",       Tint = new Color(0.8018868f, 0.02647741f, 0.02647741f, 1f),          Font = BtnTextFont },
            new Btn { Node = "Kick",              Label = "Expulsar Jugador",             Field = "kickPlayerButton",   Tint = new Color(0.8f, 0.02745098f, 0.02745098f, 1f),                Font = BtnTextFont },
            new Btn { Node = "Quit",              Label = "Abandonar Alianza",            Field = "quitButton",         Tint = new Color(0.8f, 0.02745098f, 0.02745098f, 1f),                Font = BtnTextFont },
            new Btn { Node = "Debug Add Skulls",  Label = "ADD SKULLS TO CURRENT EVENT",  Field = "DEBUG_ADDSKULLS",    Tint = new Color(0.9234619f, 0f, 1f, 1f),                            Font = DebugTextFont },
        };

        /// <summary>节点表里的下标（**自检与 `Apply` 共用这一份，⛔ 别在别处再写一遍名字**）。</summary>
        public const int IChallenge = 0, IAddFriend = 1, IProfile = 2, IPromote = 3,
                         IDemote = 4, IKick = 5, IQuit = 6, IDebug = 7;

        /// <summary>`Promote` 那颗运行期会换字的两个 I2 词条键（二进制字面量表实读）。
        /// 本地没有词条表 ⇒ 只用作出声与断言，不换字。</summary>
        public const string TermPromote = "SocialMenu/Alliances/Promote";
        public const string TermTransferLeadership = "SocialMenu/Alliances/TransferLeadership";

        // ============================================================ 数据（= 原版从 `GroupMember` / `FriendsData` 读的那几个）
        /// <summary>被选中的那一员 + 本机玩家的相对关系（原版是由 `get_Member()` 与两个单例现算的，
        /// 我们收成一份只读入参 —— 那四个来源本地都没有）。
        /// <para>⚠️ **本地没有任何数据源**（`GroupMember` / `FriendsData` 全在 PlayFab 侧）
        /// ⇒ 出厂值 = <see cref="None"/>：所有 `[SerializeField]` 节点都照 **prefab 出厂态**摆
        /// （八颗全开），**只有 `Debug Add Skulls` 是关的**（`Awake()` 那条是无条件的两态之一，不依赖数据）。</para></summary>
        public struct MemberView
        {
            public string Name;
            /// <summary>`GroupRole`：`Member = 0 / Moderator = 1 / Admin = 2 / Leader = 3`（枚举实读）。</summary>
            public int Role;
            public bool IsSelf;
            public bool IsFriend;
            /// <summary>`sameGroup`（`me.groupId == m.groupId`）。</summary>
            public bool SameGroup;
            /// <summary>本机玩家的 `GroupRole`（拿来算 `outrank`）。</summary>
            public int MyRole;

            /// <summary>`outrank = sameGroup &amp;&amp; role &lt; myRole`（`Open()` 里那个 `bVar12`）。</summary>
            public bool Outrank { get { return SameGroup && Role < MyRole; } }
            /// <summary>没有任何数据 ⇒ **prefab 出厂态**（八颗全开 + 名字空 + debug 关）。</summary>
            public static MemberView None
            {
                get { return new MemberView { Name = "", Role = 0, IsSelf = false, IsFriend = false, SameGroup = false, MyRole = 0 }; }
            }
        }

        MemberView _view = MemberView.None;
        /// <summary>当前铺的那一份（自检读口）。</summary>
        public MemberView View { get { return _view; } }
        /// <summary>**真的按数据算过显隐**了吗（`false` = 走的是「没数据 ⇒ 照 prefab 出厂态」那一支）。
        /// 自检靠它把「两态」分开断。</summary>
        public bool HasData { get; private set; }

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static AllianceMemberOptionsPopup LastOpened { get; private set; }

        Label _name;
        Transform _buttons;
        readonly Transform[] _btnNodes = new Transform[8];
        readonly Label[] _btnTexts = new Label[8];
        readonly ImageQuad[] _btnBgs = new ImageQuad[8];

        // ============================================================ 开

        /// <summary>建一扇（原版 `WindowsManager.OpenWindow(allianceMemberOptionsPopup, 那一员)` 的等价物）。</summary>
        public static AllianceMemberOptionsPopup Create(WindowsManager mgr, MemberView? view = null)
        {
            var go = new GameObject("Member Options Panel");     // 节点名照原版（`WindowsManager` 复用的键同源）
            var win = go.AddComponent<AllianceMemberOptionsPopup>();
            win.type = WindowType.Popup;                  // 实读 `type = 1`
            win.placement = WindowsPlacement.Popup;       // 实读 `windowsPlacement = 15`
            win.closeOnEsc = true;                        // 实读 `closeOnESC = 1`
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            var sc = go.AddComponent<TransformScalerBySmallScreenUI>();
            sc.menuScale = MenuScale;
            sc.Initialize();
            win.Manager = mgr;
            win._view = view ?? MemberView.None;
            win.HasData = view.HasValue;
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[MemberOptions] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
            Debug.Log("[MemberOptions] 开了 `Member Options Panel`（原版 `AllianceMemberOptionsPopup`）。"
                    + "⚠️ **本地走不到**：八颗按钮的显隐全要服务器（`GroupMember` / `FriendsData` 都在 PlayFab 侧）；"
                    + "`Open()` 在数据为空时原版直接抛 ⇒ 没有「没数据」那一支可照。"
                    + "我们出厂按 **prefab 出厂态**摆（八颗全开）—— 唯一例外 = `Debug Add Skulls`，"
                    + "`Awake()` 里**无条件** `SetActive(false)`。"
                    + "· 标题 = prefab 出厂原文（原版是 `Member.Name`）。");
        }

        /// <summary>铺一份数据（**自检用**，也是数据路唯一的入口；产品路径上那份数据来自服务器）。</summary>
        public void SetMember(MemberView v) { _view = v; HasData = true; Apply(); }

        // ============================================================ 几何（**唯一一份算式**）

        /// <summary>面板高度 = `PadTop + NameH + Spacing + 515 + PadBottom` = **576.6**（dump 实测 ✓）。
        /// ✅ **这一扇没有 `GenericOptionsPanel` 那个「`Template` 关掉之后高度会不会重算」的歧义** ——
        /// 它根的直接子件里**没有 `Template`**，5 件全是布局件，两档同值。</summary>
        public const float RootHeight = 576.6f;      // = 10 + 36.6 + 5 + 515 + 10（写成常量，自检直接比）

        /// <summary>面板矩形。`pivot = (0.5,0)`（**底**）⇒ 没给锚点时底沿固定在 763.30、高度**向上**长；
        /// 给了锚点 ⇒ 等价于原版 `SetPosition`（**竖直居中到锚点**，两支代数上收敛到同一结果）。</summary>
        public PxRect RootRect(Vector2? anchor = null)
        {
            float h = RootHeight;
            if (anchor.HasValue)
                return new PxRect(anchor.Value.x - W * 0.5f, anchor.Value.y - h * 0.5f,
                                  anchor.Value.x + W * 0.5f, anchor.Value.y + h * 0.5f);
            float y2 = DefPivotBottomY;
            return new PxRect(960f - W * 0.5f, y2 - h, 960f + W * 0.5f, y2);
        }

        /// <summary>整屏底：**以面板中心为中心**的 4574.60×2572.36（逐份实读；不是铺满屏那个矩形）。</summary>
        public static PxRect ShadeRect(PxRect root)
        {
            float cx = (root.x1 + root.x2) * 0.5f, cy = (root.y1 + root.y2) * 0.5f;
            return new PxRect(cx - ShadeW * 0.5f, cy - ShadeH * 0.5f, cx + ShadeW * 0.5f, cy + ShadeH * 0.5f);
        }

        /// <summary>`bg shadow`：面板框向外各撑半量、再平移 `ShadowDx`。</summary>
        public static PxRect ShadowRect(PxRect root)
        {
            return new PxRect(root.x1 - ShadowHalfW + ShadowDx, root.y1 - ShadowHalfH,
                              root.x2 + ShadowHalfW + ShadowDx, root.y2 + ShadowHalfH);
        }

        /// <summary>`Name`：`sizeDelta = (355, 36.6)`、顶沿 = 面板顶 + 10（dump 实测 782.50,196.70→1137.50,233.30 ✓）。</summary>
        public static PxRect NameRect(PxRect root)
        {
            float y1 = root.y1 + PadTop;
            return new PxRect(960f - NameW * 0.5f, y1, 960f + NameW * 0.5f, y1 + NameH);
        }

        /// <summary>`Buttons` 容器：零宽、顶沿 = 面板顶 + 51.6、高 515（dump 实测 960.00,238.30→960.00,753.30 ✓）。</summary>
        public static PxRect ButtonsRect(PxRect root)
        {
            float top = root.y1 + PadTop + NameH + Spacing;
            return new PxRect(960f, top, 960f, top + ButtonsH8);
        }

        /// <summary>第 `i` 颗按钮（dump 实测：`Challenge` 238.30→298.30，步进 65）。</summary>
        public static PxRect BtnRect(PxRect root, int i)
        {
            float top = root.y1 + PadTop + NameH + Spacing + BtnStep * i;
            return new PxRect(960f - BtnW * 0.5f, top, 960f + BtnW * 0.5f, top + BtnH);
        }

        /// <summary>`Button Text`（`AspectRatioFitter` 跑完：330.77 × 86.17，**竖直居中**在按钮上；
        /// dump 实测 794.04,238.30→1124.81,311.32 —— 即按钮中心 268.30 ± 43.085 ✓）。</summary>
        public static PxRect BtnTextRect(PxRect btn)
        {
            float x1 = btn.x1 + BtnTextDx, cy = (btn.y1 + btn.y2) * 0.5f;
            return new PxRect(x1, cy - BtnTextH * 0.5f, x1 + BtnTextW, cy + BtnTextH * 0.5f);
        }

        // ============================================================ 建

        public void Build()
        {
            var root = transform;
            MenuDraw.ClearChildren(root);
            MissingArt.Clear();
            _name = null; _buttons = null;

            var R = RootRect();
            var S = ShadeRect(R);

            // ---- 1) `Menu Dark Background`（**a = 0** 的整屏底，只吃点击）----
            MenuDraw.Rect(root, CardArt.Solid(), S, "Menu Dark Background", QShade, ShadeTint);
            MenuDraw.ShadeHit(root, S, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 2) `bg shadow` → 3) `bg` ----
            var shadowTex = Tex(ArtShadow, "`bg shadow` 的八边形投影");
            if (shadowTex != null)
                MenuDraw.Rect(root, shadowTex, ShadowRect(R), "bg shadow", QBgShadow, ShadowTint);
            else
                MenuDraw.Node(root, "bg shadow", ShadowRect(R));
            var bgTex = Tex(ArtBg, "面板底 `bg`");
            if (bgTex != null)
                MenuDraw.Nine(root, bgTex, R, new Vector4(15f, 15f, 15f, 15f), 44f, 39f, QBg, BgTint, true, "bg");
            else
                MenuDraw.Node(root, "bg", R);

            // ---- 4) `Name`（成员名；原版 `playerName.SetText(Member.Name)`）----
            var nameR = NameRect(R);
            _name = MenuDraw.TextBox(root, nameR, _view.Name ?? "", Color.white, "Name",
                                     NameFont, NameFontMin, QName, NameFontMax, NameFontBase);

            // ---- 5) `Buttons`：**八颗写死的子件**（不是模板 —— 这一扇没有 `Template`）----
            _buttons = MenuDraw.Node(root, "Buttons", ButtonsRect(R));
            for (int i = 0; i < Buttons.Length; i++)
            {
                var br = BtnRect(R, i);
                var bt = Buttons[i];
                var bn = MenuDraw.Node(_buttons, bt.Node, br);
                var art = Tex(ArtBtn, "`" + bt.Node + "` 的按钮底");
                // 🔴 **原版那颗 `Image` 是 `m_Type = 1 (Sliced)`** ＋ `m_PixelsPerUnitMultiplier = 3.0`
                //    ⇒ 走九宫格、角块按 `border ÷ 3` 缩（同 `BoosterInfoPopup` / `TrophyInfoPopup` 那条口径）。
                //    ⛔ 别图省事用 `MenuDraw.Rect`（那是 Simple 拉伸）—— 一张 410×124 带 333/96 大边的图
                //    直接拉到 357.3×60 会把四个角挤变形。
                GameObject bgo = null;
                if (art != null)
                    bgo = MenuDraw.Nine(bn, art, br, new Vector4(333f, 96f, 333f, 96f), 410f, 124f, QBtnBg, bt.Tint,
                                        true, "Image",
                                        new Vector4(333f / 3f, 96f / 3f, 333f / 3f, 96f / 3f));
                var bq = bgo != null ? bgo.GetComponentInChildren<ImageQuad>() : null;
                var tx = MenuDraw.TextBox(bn, BtnTextRect(br), bt.Label, Color.white, "Button Text",
                                          bt.Font, BtnTextFontMin, QBtnText, BtnTextFontMax, BtnTextFontBase);
                int idx = i;
                // ⚠️ 换图那一跳**不在 `Hit` 里传 `target`** —— 九宫格被切成 9 张小 quad，只换中心那格
                //    = 边框不跟着亮（`WindowButton.BindNine` 的注释就是为这件事写的）⇒ 命中区先建、
                //    再自己 `BindNine`。
                var hit = MenuDraw.Hit(bn, "Hit", br, QHit, () => OnClicked(idx));
                if (hit != null && bgo != null)
                {
                    var wb = hit.GetComponent<WindowButton>();
                    if (wb != null) wb.BindNine(bgo, ArtBtn, ArtBtnHover, ArtBtnPressed);
                    else Debug.LogWarning("[MemberOptions] `" + bt.Node + "` 的命中区上没有 `WindowButton`"
                                        + " ⇒ 悬停/按下**不换图**。");
                }
                _btnNodes[i] = bn; _btnTexts[i] = tx; _btnBgs[i] = bq;
            }

            Apply();
        }

        /// <summary>把 `_view` 铺上去（= 原版 `Open()` 那一大段 `SetActive` 梯子 + `playerName.SetText`）。
        /// <para>🔴 **没数据时（`HasData == false`）走的是「照 prefab 出厂态」那一支**：八颗全开
        /// （`Debug Add Skulls` 除外 —— 它由 `Awake()` 无条件关掉）。**两态都能断**（见 `Editor/ShopScene.cs`）。</para></summary>
        public void Apply()
        {
            if (_name != null) _name.SetText(_view.Name ?? "");
            bool self = _view.IsSelf, friend = _view.IsFriend, outrank = _view.Outrank;
            int role = _view.Role;
            for (int i = 0; i < Buttons.Length; i++)
            {
                bool on;
                if (!HasData)
                {
                    on = i != IDebug;          // 出厂态：八颗全开，`Debug Add Skulls` 恒关（`Awake()`）
                }
                else
                {
                    switch (i)
                    {
                        case IChallenge: on = !self; break;                              // `!isSelf`
                        case IAddFriend: on = !self && !friend; break;                    // `!isSelf && !isFriend`
                        case IProfile:   on = !self; break;                              // `!isSelf`
                        case IPromote:   on = outrank && role < 3; break;                // `outrank && role < Leader`
                        case IDemote:    on = outrank && role > 0; break;                // `outrank && role > Member`
                        case IKick:      on = outrank; break;                            // `outrank`
                        case IQuit:      on = self; break;                               // `isSelf`
                        default:         on = false; break;                              // `Debug Add Skulls`
                    }
                }
                if (_btnNodes[i] != null) _btnNodes[i].gameObject.SetActive(on);
            }
            // `Promote` 那颗的运行期换字（role == Admin ⇒ `TransferLeadership`）。
            // 🔴 本地没有 I2 词条表 ⇒ **不出声就不用换**：只有真按数据算过、且 role 真是 Admin 时才出声说明。
            if (HasData && role == 2 && _btnTexts[IPromote] != null)
                Debug.Log("[MemberOptions] `Promote` 那一颗：原版在 `role == Admin(2)` 时把文字换成 I2 词条 "
                        + "`" + TermTransferLeadership + "`（否则 `" + TermPromote + "`）—— "
                        + "**词条表在远端 CCD、本地一个 value 都没有** ⇒ 字面仍是 prefab 出厂原文 `Promote`。");
        }

        /// <summary>点了某一颗。原版八条各是一个服务器调用（`PlayFabWrapper…`）＋ 两条 `ShowConfirmation`。
        /// 🔴 我们**没有服务器** ⇒ **只出声、不改任何状态**（红线：不许静默失败 / 不许假装成功）。
        /// ⚠️ 与 `AllianceMemberTab.cs:1357` 那条记账同源（「8 个钮全要服务器」）。</summary>
        void OnClicked(int i)
        {
            var bt = Buttons[i];
            Debug.Log("[MemberOptions] 点了 `" + bt.Node + "`（原版字段 `" + bt.Field + "`）—— "
                    + "**本地没有服务器** ⇒ 只出声、状态没有变。"
                    + "原版那一路是 PlayFab 的 GenericCloudScriptHandler；`Quit` / `Kick` / `Demote` 三条"
                    + "还会先走 `ShowConfirmation(...)` 弹一句确认（字符串也是服务器词条）。");
        }

        // ============================================================ 取图（取不到必须出声）
        readonly System.Collections.Generic.List<string> _missArt = new System.Collections.Generic.List<string>();
        /// <summary>本窗**取不到的图**（自检读口）。</summary>
        public System.Collections.Generic.List<string> MissingArt { get { return _missArt; } }

        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[MemberOptions] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 读口（自检专用）
        /// <summary>`Name` 那颗（自检读口）。</summary>
        public Label NameLabel { get { return _name; } }
        /// <summary>`Buttons` 容器（自检读口）。</summary>
        public Transform ButtonsNode { get { return _buttons; } }
        /// <summary>`Buttons` 的**直系子件名**（**兄弟序** —— 自检按它断层级，⛔ 不用 `FindChild`：
        /// 那条路走整棵子树、层级错了照样捞得到）。</summary>
        public System.Collections.Generic.List<string> ButtonNames()
        {
            var l = new System.Collections.Generic.List<string>();
            if (_buttons != null)
                foreach (Transform c in _buttons) l.Add(c.name);
            return l;
        }
        /// <summary>第 `i` 颗按钮的节点（自检读口；`null` = 没建）。</summary>
        public Transform BtnNode(int i) { return (i >= 0 && i < 8) ? _btnNodes[i] : null; }
        /// <summary>第 `i` 颗按钮的文字（自检读口）。</summary>
        public Label BtnText(int i) { return (i >= 0 && i < 8) ? _btnTexts[i] : null; }
        /// <summary>第 `i` 颗按钮的底（自检读口）。</summary>
        public ImageQuad BtnBg(int i) { return (i >= 0 && i < 8) ? _btnBgs[i] : null; }

        /// <summary>自检用：把当前状态摊开。</summary>
        public string DebugDump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("name=\"").Append(_name != null ? _name.Text : "-").Append("\" hasData=").Append(HasData).Append(" vis=[");
            for (int i = 0; i < 8; i++)
            {
                if (i > 0) sb.Append(',');
                var n = _btnNodes[i];
                sb.Append(Buttons[i].Node).Append('=').Append(n != null && n.gameObject.activeSelf);
            }
            sb.Append("] missingArt=").Append(_missArt.Count);
            return sb.ToString();
        }

        // ============================================================ 对账表（**纯数据 · 不参与渲染**）
        /// <summary>一个节点的**冻结事实**（路径 / **相对根左上角的框** / 出厂 `activeSelf`）——
        /// 逐格对着 `python 工具/menu_dump.py bundle_menus_assets_all "Member Options Panel" --depth 12 --relative --md`
        /// 的现读值抄，供 `_tmp_view/wl2/check_table.py` 逐格对账（不符必须是 0）。</summary>
        public struct ReconRow
        {
            public string Path;
            public PxRect R;
            public bool On;
            public ReconRow(string p, PxRect r, bool on = true) { Path = p; R = r; On = on; }
        }

        /// <summary>逐节点对账表（**22 条 = 原版 22 个节点**）。
        /// 八颗钮：`y1 = 51.60 + 65i`、高 60、x `15.00→372.30`（`Buttons` 的 VLG 跑完那一档）；
        /// 每颗的 `Button Text` = `(27.69, btn.y1 − 13.14) → (358.46, btn.y1 + 73.02)`
        /// （`AspectRatioFitter` 跑完的 330.77×86.17 **竖直居中**在钮上）。</summary>
        public static readonly ReconRow[] Recon = BuildRecon();

        static ReconRow[] BuildRecon()
        {
            var l = new System.Collections.Generic.List<ReconRow>();
            l.Add(new ReconRow("Member Options Panel", new PxRect(0f, 0f, 387.30f, 576.60f)));
            l.Add(new ReconRow("Menu Dark Background", new PxRect(-2093.65f, -997.88f, 2480.95f, 1574.48f)));
            l.Add(new ReconRow("bg shadow",            new PxRect(-32.45f, -39.49f, 418.02f, 616.09f)));
            l.Add(new ReconRow("bg",                   new PxRect(0f, 0f, 387.30f, 576.60f)));
            l.Add(new ReconRow("Name",                 new PxRect(16.15f, 10.00f, 371.15f, 46.60f)));
            l.Add(new ReconRow("Buttons",              new PxRect(193.65f, 51.60f, 193.65f, 566.60f)));
            for (int i = 0; i < Buttons.Length; i++)
            {
                float y1 = 51.60f + 65f * i;
                l.Add(new ReconRow("Buttons/" + Buttons[i].Node,
                                   new PxRect(15.00f, y1, 372.30f, y1 + 60f)));
                l.Add(new ReconRow("Buttons/" + Buttons[i].Node + "/Button Text",
                                   new PxRect(27.69f, y1 - 13.14f, 358.46f, y1 + 73.02f)));
            }
            return l.ToArray();
        }
    }
}
