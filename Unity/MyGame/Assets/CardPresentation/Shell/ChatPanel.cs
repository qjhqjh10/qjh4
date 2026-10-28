// ChatPanel.cs — 多人界面那一批 第 4 件之二：**聊天窗**（原版 `ChatPanel`，类 `ChatPanel : GameWindowWithTabs`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/聊天窗与挑战弹窗.md` —— §A 是 `ChatPanel` 的**层 × 参数**表（35 个 RT）·
// §A·2 是**运行期才存在**的那两棵树（`Chat Tab` / `ChatMessageRow`，在**另一个 bundle**
// `bundle_mainmenualwaysloaded_assets_all`）· §C·2 是布局组的参数与布局后位 · §D 是反编译侧的入口/调用/监听。
//
// ---- 🔴 五条判据（读原始 JSON 定的）----
// ① **窗口属性**（`ChatPanel` 组件 pid `1773913423464410674`）：`type = 1`(**Popup**) ·
//    `windowsPlacement = 5`(**Canvas**) · `closeOnESC = 1` · `updateNavPanel = 0` · `extraScaleSmallScreen = 1`。
//    ⚠️ **它是 Popup 不是 Fullscreen**（与社交窗相反）—— 开会把当前主窗压到背景。
// ② 🔴 **聊天消息行【原版有模板】**：`bundle_mainmenualwaysloaded_assets_all/GameObject/ChatMessageRow.json`
//    （根 RT `7760131448890879999`，组件 `ChatMessageUI`）。
//    ⚠️ 这条**推翻了**正本 `阶段二_多人界面_原版规格.md:277` 原来那句「聊天消息行全档查不到 ⇒ 要我们自己造」——
//    那句已就地订正（铁律 5）。**别自己造行**。
// ③ **页签是运行期生成的**（`TabButtons.tabButtonPrefab` = 那个 act F 的 `Orange Tab Toggle` 自己）：
//    `TabbedWindowComponents.tabPrefab` 跨文件指向 `bundle_mainmenualwaysloaded_assets_all` 的 `Chat Tab`；
//    `tabHolder` = `Chat/Tabs`（**无任何组件的空叶子**）。
//    🔴 频道枚举 = **`ChatRoom { Global = 0, Alliance = 1 }`**（只有两个值）。
//    原版按 `ChatGlobalManager.subscribedChannels`（服务器）建页签 ⇒ **本地一个频道都订阅不到**。
//    ⇒ **这是我们挑的**：按枚举把两个频道页签都建出来（否则整扇窗是空的、玩家以为坏了），
//      并在下面出声说明。⚠️ 视觉顺序 = 创建顺序（`m_ReverseArrangement=1` + `AddTabButton` 的
//      `SetSiblingIndex(0)`）⇒ **Global 在上、Alliance 在下**。
// ④ **`Enter Text` 的序列化高是 0，真实高 66.53**（`ContentSizeFitter` VerticalFit=PreferredSize 跑出来的）
//    ⇒ 用它和它两个孩子的**运行期**矩形（§A·1 第 71–77 行），别用序列化那份 0。
// ⑤ **`Player Options Panel` 出厂 act F**（点消息行上那个头像才亮）；5 个钮全要服务器。
//
// ---- 用户口径（2026-09-26）----
// 「**网络聊天功能暂时不做**，但**界面照建、数据留空态**」⇒ 输入框打不了字、发送键出声；
// 真正的收发消息**没有**（没有服务器，也没有 `ChatGlobalManager` 的对等物）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ChatPanel`。</summary>
    public class ChatPanel : GameWindowWithTabs
    {
        // 队列档（本窗自成一档；页与子件在它之上） —— 与社交窗的 3200 段、档案窗的 3160 段都不重叠
        // 🔴 **2026-10-13（A348）就地收窄（铁律 5）**：这一颗原来也是 `public` —— 实测**全仓代码引用 0**
        //    （唯一命中是 `Shell/AllianceMemberTab.cs` 的一句**注释**里提到 `ChatPanel.QBase = 3300`，
        //    那是层带说明、不是引用）。⚠️ 收窄判据要**两遍数**：先按「别的文件引用了没有」扫全仓，
        //    再在【本文件】里数一遍（同文件另一个顶层类要用就得留 —— 见下面那一段）。
        const int QBase = 3300;
        // ⚠️ 这几个**必须 public** —— 消息行在**另一个类**（`ChatMessageRow`）里建，它引用 `ChatPanel.Q*`。
        // ✅ **2026-10-11（A252）可见性收窄（只收回 `QBg`）**：A252 的判据表是按「**别的文件**引用了没有」
        //   扫的，而这里还有一类**它扫不到**的用法 —— **同一个文件里的另一个类**用**限定名**引用本类常量：
        //   排 `const` 的边界要在【本文件】里再数一遍 ⇒ `QBg` 是这一组里**唯一**两处都 0 引用的。
        //   留 `public` 的逐个带出处（**按内容认，⛔ 不写行号** —— 行号会漂）：
        //   `QContent`(`ChatMessageRow.Build` 建 `RowBackground` 那一行) · `QHead`(同方法里 `Sender`/`Time` 两行) ·
        //   `QFrame`(建 `Border`=`Player_Profile_Border`) · `QAvatar`(建 `Profile content`) ·
        //   `QText`(建正文 `Message`) · `QHit`(同方法里 `MenuDraw.Hit(pb,"Hit",…)`) 都在 `ChatMessageRow` 里；
        //   `QPanel` 另有 `Editor/CollectionScene.cs`（1 处）。⛔ 收它们 = 当场 CS0122。
        public const int QPanel = QBase;         // ✅ 留 `public`：`Editor/CollectionScene.cs` 引用（1 处）
        const int QBg = QBase + 1;               // 3301 窗底九宫 / 输入框底 / 发送键底（只在 `ChatPanel` 内用）
        public const int QContent = QBase + 2;   // ✅ 留 `public`：`ChatMessageRow` 建 `RowBackground` 那行引用
        // 🔴 **消息行内部的五级阶梯 —— 照原版兄弟序，不许合并**（2026-09-28 从解包 JSON 读出；
        //    此前记的是「没有画面尺子 ⇒ 没定」，现在**有**了）：
        //    · 一条消息行的 `m_Children`（`bundle_mainmenualwaysloaded_assets_all/RectTransform/
        //      RectTransform_7760131448890879999.json:22-39`）＝
        //        `RowBackground`(0) → `Player Header`(1) → `Friend Header`(2) → `Message`(3)
        //    · 头部内部（`RectTransform_-6649793586198712321.json`，Friend 侧 `…8335.json` 同序）＝
        //        `Sender`(0) → `Time`(1) → `Profile border`(2)
        //    · 而**框是 `Profile border` 自己身上的 Image**，`Profile content`（立绘）是它**唯一**的子节点
        //    ⇒ 实际画序 = **行底 < 信使名/时间 < 框 < 立绘 < 正文**
        //      （**头像压文字、正文压过头像** —— 头像会伸进 y=47 的正文带，原版由 `Message` 盖住它，
        //       而 `ChatMessageUI__Awake/Set` 里**没有**任何运行期改序 ⇒ 兄弟序就是最终序）。
        //    ⚠️ **我们原来把整组头像放在信使名/时间【下面】（反了）**，今天按这条改正。
        // ✅ 2026-10-11（A252）：这四个 + `QHit` **留 `public`** —— `ChatMessageRow` 用限定名引用（见上，按内容认）。
        public const int QHead = QBase + 3, QFrame = QBase + 4, QAvatar = QBase + 5, QText = QBase + 6;
        public const int QHit = QBase + 8;       // ✅ 留 `public`：`ChatMessageRow` + `Editor/CollectionScene.cs`（2 处）

        public static ChatPanel LastOpened { get; private set; }

        // ---------------------------------------------------------- 真值（§A·1，绝对画布像素）
        static readonly PxRect RootR = new PxRect(-78.12f, 0f, 1893.88f, 1080f);
        static readonly PxRect HolderR = new PxRect(-78.12f, -4f, 1893.88f, 1076f);
        static readonly PxRect CloseBgR = new PxRect(-2056.50f, -651.18f, 3872.26f, 1723.18f);
        static readonly Color CloseBgTint = new Color(0f, 0f, 0f, 0.518f);
        static readonly PxRect ChatR = new PxRect(563.88f, 146f, 1863.88f, 1076f);
        static readonly PxRect TabBtnColR = new PxRect(229.54f, 182.82f, 590.71f, 1006.61f);
        /// <summary>`Tab Buttons` 的 `VerticalLayoutGroup`：spacing 0 · align 2 (UpperRight) · **reverse=1** ⇒
        /// 子件 165×157.684，**从上往下**排，右对齐到 x=590.71。</summary>
        static readonly Vector2 TabBtnSz = new Vector2(165f, 157.684f);
        static readonly PxRect ChatBgR = new PxRect(563.88f, 146f, 1863.88f, 1076f);
        static readonly PxRect TabsR = new PxRect(613.88f, 161f, 1813.88f, 911f);
        // `Enter Text`：**运行期**矩形（CSF 跑出来的 66.53 高）
        static readonly PxRect EnterR = new PxRect(613.88f, 959.47f, 1813.88f, 1026.00f);
        static readonly PxRect InputR = new PxRect(653.88f, 979.47f, 1773.88f, 1006.00f);
        static readonly PxRect SendBtnR = new PxRect(1750.38f, 972.735f, 1790.38f, 1012.735f);
        static readonly PxRect CloseBtnR = new PxRect(1799.29f, 122.80f, 1873.67f, 198.40f);
        static readonly PxRect CloseIconR = new PxRect(1807.44f, 130.78f, 1864.30f, 188.90f);
        /// <summary>🔴 **2026-10-18（A1053）**：关窗钮那两颗子件（`Background` / `Icon`，同矩形 `CloseIconR`）
        /// **自己的** `m_RaycastPadding`（原版实读 `(-20)⁴`；分量序 L,B,R,T · **负 = 外扩**）
        /// ⇒ 命中区 = `CloseIconR` 外扩 20 = **96.86 × 98.13**（⛔ 不是根矩形 `CloseBtnR` 的 74.38×75.60）。
        /// 算式只走 `MenuDraw.PaddedRect`；口径 → `资料/普查产出_第四会话/普查_全仓命中区与关闭键族.md` §〇-1。</summary>
        static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);
        static readonly PxRect OptPanelR = new PxRect(176.58f, 349f, 563.88f, 739f);
        static readonly PxRect OptNameR = new PxRect(186.16f, 349f, 554.30f, 399f);
        /// <summary>`Player Options Panel/Buttons` 的 VLG：每键 357.3×57.6、步进 67.6（spacing 10）、
        /// 首键顶 = 399.00（§A·1 第 83–92 行逐键给出）。</summary>
        const float OptRowH = 57.6f, OptRowStep = 67.6f;

        public readonly List<string> MissingArt = new List<string>();
        /// <summary>两个频道键的底图（`button_bg`）—— **选中/未选中都在**，换的是**贴图**不是显隐
        /// （见 `TabButton` 上面那段四态判据 + `RefreshHighlights`）。</summary>
        readonly ImageQuad[] _tabBg = new ImageQuad[2];
        /// <summary>两个频道键的按钮 —— A17 的悬停/按下换图挂在它身上；切页换底图之后要 `SetNormalTex`。</summary>
        readonly WindowButton[] _tabBtn = new WindowButton[2];
        Transform[] _tabRoots = new Transform[2];
        Transform _holder, _tabCol, _enterText, _options;
        public int BuiltMessages { get; private set; }

        /// 🔶 自己设计（非原版）：聊天窗两个频道 `Channels = { Global, Alliance }` · 原版频道枚举值与页签文案取不到（判据③） · TODO(谁定的)｜原注：<summary>本窗的两个频道（`ChatRoom` 枚举的值 + 页签文案）。**这是我们挑的**（判据 ③）。</summary>
        public static readonly string[] Channels = { "Global", "Alliance" };

        public GameObject OptionsPanel { get { return _options != null ? _options.gameObject : null; } }
        public Transform TabsHolder { get { return tabHolder; } }
        public Transform EnterTextNode { get { return _enterText; } }

        /// <summary>取图（`ChatMessageRow` 那个静态 builder 也要用 ⇒ public）。取不到记进 `MissingArt`。</summary>
        public Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }        static Transform Node(Transform p, string n, PxRect r) { return MenuDraw.Node(p, n, r); }
        ImageQuad Rect(Transform p, string art, PxRect r, string n, int q, Color? tint = null, bool keepAspect = false)
        { return MenuDraw.Rect(p, art == null ? CardArt.Solid() : Art(art), r, n, q, tint, keepAspect); }
        GameObject Nine(Transform p, string art, PxRect r, Vector4 b, string n, int q, Color? tint = null)
        { var t = Art(art); return t == null ? null : MenuDraw.Nine(p, t, r, b, t.width, t.height, q, tint, true, n); }
        /// <summary>本窗私有的一段字（`MenuDraw.TextBox` 的薄包装；本文件另有一个**静态** `Text`，
        /// 那个收 `clip`/`clipSoft`、是聊天行专用的，见下方 `ChatMessageRow.Build`）。
        /// <para>🔴 **2026-10-13（A403② + A493#10）：`autoMin` / `alignLeft` 的缺省值【已删 · 必填】**
        /// —— 口径同 A323（缺省值**不是原版概念**，原版只有**逐个节点**的真值）。
        /// ⚠️ 与 `AllianceMemberTab.TextSoft`（A403①）**不同**：那一边 `alignLeft` 的缺省是**死的**
        /// （两个调用点都显式传），**这一边是【活的】** —— 现读 `Enter Text/InputField (TMP)` 上面那条
        /// `Text(input, InputR, "Type message", …)`（⛔ 按**内容**找，别按行号 —— A403 立项时它在 `:219`，
        /// 改完之后必然漂）**没传** `alignLeft` ⇒ 它今天吃的是缺省的 `true`。**所以删缺省之前必须先给那一行显式写出来**，
        /// 否则等于**静默**把那一处改成「不对齐（= 居中）」。本批已显式补 `alignLeft: true` + `autoMin: 0f`。
        /// · `autoMax` / `autoBase` **保留缺省**（`&lt;= 0` ⇒ 旧行为）—— 本窗四处调用点原版**全是
        ///   `m_enableAutoSizing = 0`**，两格**不适用**，多开一个永远填不上的口才是留洞
        ///   （逐站判据见 `资料/普查产出_1012/F2_字号线收尾.md` §2·6 的 `ChatPanel` 表）。</para></summary>
        Label Text(Transform p, PxRect r, string s, Color c, string n, float px, int q, float autoMin,
                   bool alignLeft, float autoMax = 0f, float autoBase = 0f,
                   TmpFont.Face face = TmpFont.Face.Cjk)
        {
            var lb = MenuDraw.TextBox(p, r, s, c, n, px, autoMin, q, autoMax, autoBase, face: face);
            if (lb != null && alignLeft) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        // ---------------------------------------------------------- 建

        public static ChatPanel Create(WindowsManager mgr)
        {
            var go = new GameObject("ChatPanel");
            var win = go.AddComponent<ChatPanel>();
            win.type = WindowType.Popup;                      // 实证 type = 1
            win.placement = WindowsPlacement.Canvas;          // 实证 windowsPlacement = 5
            win.closeOnEsc = true;                            // 实证 closeOnESC = 1
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
            if (tabButtons != null) tabButtons.Click(0);      // 默认落在 Global（视觉第 1 个）
            RefreshHighlights();
        }

        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();

            _holder = Node(transform, "Holder", HolderR);

            // `CloseBackground`：纯色 (0,0,0,0.518) + **点外关闭**（原版 `BackgroundCloseButton`，没有 Button 组件）
            var cb = Node(_holder, "CloseBackground", CloseBgR);
            Rect(cb, null, CloseBgR, "Image", QPanel, CloseBgTint);
            // 🔴 **2026-10-05（§三第29条 A77⑧）：压暗层的命中区改走公共件 `MenuDraw.ShadeHit`。**
            //   档 = **压暗层自己那一档**（`QPanel` = `QBase` = 3300），且**严格低于**本窗内容命中区最低档
            //   （`QHit` = 3308）—— 原编码本来就是 `QPanel`、**合规矩**，这一批只是收口到一份实现
            //   （⚠️ 不这么办会踩的那条坑见 `MenuDraw.ShadeHit` 的注释：同档时谁吃到命中退化成枚举顺序 ⇒
            //    「点不动的钮看着像正常工作」）。规矩与出处 → `资料/待办判据_阶段二与联机.md` §A25⑥ /（一）。
            //   ⚠️ 它**不传** `art`/`hoverArt`：压暗层是全屏纯色、没有悬停换图
            //   （对比 `CollectionWindow` 那颗 `Shared/Close Button` —— 那是**关窗钮、不是压暗层**，
            //    它带 SpriteSwap，⛔ 别往 `ShadeHit` 上收）。
            MenuDraw.ShadeHit(cb, CloseBgR, QPanel, QHit, () => Close(), "CloseHit");

            var chat = Node(_holder, "Chat", ChatR);

            // 左栏两个频道键（`Tab Buttons` VLG：reverse + UpperRight）
            _tabCol = Node(chat, "Tab Buttons", TabBtnColR);
            var tb = _tabCol.gameObject.AddComponent<TabButtons>();
            tabButtons = tb;
            tb.options.Clear();
            visualTypes.Clear();
            var holder = Node(_tabCol, "Buttons", TabBtnColR);
            for (int i = 0; i < Channels.Length; i++)
            {
                float top = TabBtnColR.y1 + i * TabBtnSz.y;
                _tabRoots[i] = TabButton(holder, i, Channels[i], top, top + TabBtnSz.y);
                tb.options.Add(new TabButtons.Option
                {
                    type = i == 0 ? WindowTabType.ChatGlobal : WindowTabType.ChatAlliance,
                    button = _tabRoots[i].GetComponentInChildren<WindowButton>(true),
                });
                visualTypes.Add(i == 0 ? WindowTabType.ChatGlobal : WindowTabType.ChatAlliance);
            }
            // 🔴 照原版：`tabButtonPrefab` 是**克隆母版**，`Initialize` 一进来就把它关掉。
            //    我们没有第三份，就把最后一个键当母版（原版也是这么用的）—— ⚠️ 但那会让它**不显示**。
            //    两个频道都要看得见 ⇒ **不设母版**（`tabButtonPrefab` 留 null），并在这里说明：
            //    这是**我们与原版的一处不同**（原版从服务器列表建，天然有第 3 个当母版）。
            tb.Initialize(this);

            // 聊天框底 + 页签内容区
            Nine(chat, "Chat_background", ChatBgR, new Vector4(138f, 113f, 137f, 107f), "ChatBackground", QBg,
                 new Color(1f, 1f, 1f, 0.867f));
            // 🆕 **2026-10-06（A94）：聊天框底吸收点击**。判据 = 原版 prefab
            //   `ChatPanel > Holder > Chat > ChatBackground` 那颗 `Image` 的 **`m_RaycastTarget = 1`**
            //   （2026-10-06 `rayscan` 实读）—— 射线打到它自己，父链上没有点击处理器
            //   （关窗那颗 `BackgroundCloseButton` 挂在更外层的 `CloseBackground` 上）⇒ 原版**什么都不做**。
            MenuDraw.Absorb(transform, "AbsorbHit", ChatBgR, QPanel, QHit);
            tabHolder = Node(chat, "Tabs", TabsR);

            // 输入行（运行期真实矩形，判据 ④）
            _enterText = Node(chat, "Enter Text", EnterR);
            Nine(_enterText, "Chat_text_background", EnterR, new Vector4(53f, 43f, 52f, 43f), "Background", QBg);
            var input = Node(_enterText, "InputField (TMP)", InputR);
            // 🔴 **A406（2026-10-12）逐站实读**：`ChatPanel/Holder/Chat/Enter Text/InputField (TMP)/Text Area/Placeholder`
            //   原版 `字号=28.0 基准=28.0`（**没有 `auto[…]` 段** ⇒ `m_enableAutoSizing = 0`）
            //   ⇒ `autoMax`/`autoBase` **不适用**；我们也传 `autoMin = 0`（缺省）⇒ **两边一致**。
            //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "ChatPanel" --depth 10 --md`。
            // 🔴 **2026-10-13（A493#10 落地）对齐真值 = `Left/Middle`** —— **同一颗节点、同一份 dump**
            //   现读（本批亲跑，不是抄别处的表）：
            //   `| ······6 | Placeholder | … | 'Type message' 字号=28.0 基准=28.0 对齐=Left/Middle 折行=3 色=(1,1,1,0.439) |`
            //   我们**今天就是按 Left 画的**（`Text` 的 `alignLeft` 缺省当时是 `true`）⇒ 这里显式写出来
            //   是**零行为变化**；写出来之后 `Text` 的缺省才敢删（见那个口的头）。
            //   ⛔ **就地订正 `资料/普查产出_1012/H37_字距顺序与静默口.md:133`**（A477 清单表第 #10 行）：
            //   那一行**原版对齐那一格是对的**（`Left/Middle`），但**「现在怎么画的」那一格写着「居中」——
            //   是错的**。错因：那件扫描器的判据是「附近没有 `Align*` 也没有 `alignLeft: true`」，它**看不到
            //   形参的缺省值**；而本口当时 `alignLeft` 的缺省**正是 `true`**（`if (lb != null && alignLeft)
            //   MenuDraw.AlignLeft(lb, r);` 是**真的跑了的**）⇒ 今天就画 Left。本批已按**现读代码 + 原版 dump**
            //   结账（铁律：两份说法打架比没有更糟 —— 表里那一格留在原处当痕迹，以本条为准）。
            //   ⚠️ 同族还剩一处**真偏离**，是 `H37 §四·3` 那 6 处「没查全」里的 `Message`
            //   （**现读**见下方 `ChatMessageRow.Build` 正文那一段；A477 立项时是 `:670`、A493#10 那批是 `:696`，行号已漂）
            //   （原版 `Left/Top`，我们两侧都没对齐 ⇒ 画成居中）—— ~~不在本批三条账上，只报不改~~
            //   ✅ **2026-10-14（A525）已补 `alignLeft: true`**（原来只在 `资料/普查产出_1013/W403_A414_对齐与字号.md` §七·1 挂着）。
            //   上面那句「只报不改」是 W403 立项时的话，改法落地后就地作废（铁律 5：保留更正痕迹）。
            // 🔴 **2026-10-18（第七轮）：占位符走词条** —— 键 = 原版那颗 `Placeholder` 的
            //   `Localize.mTerm` 原文 `MainMenu/Chat/TypeMessage`（1 颗；父链 =
            //   `Placeholder < Text Area < InputField (TMP) < Enter Text < Chat < Holder < ChatPanel`
            //   ⇒ **节点名与路径都和我们这一颗一致**）。
            //   英文列 = TMP 原文 `Type message`；中文列 = 「输入消息」(`zh_CN.csv:183` 精确命中)。
            //   ⛔ 节点名 `"Placeholder"` 与那串字号/对齐实参没动。
            // 🔴 **A1024③（2026-10-17）换面**：`ChatPanel` + `ChatMessageRow` 两棵根里 **0 颗 Asar**
            //    （判据 = `menu_dump … bundle_menus_assets_all "ChatPanel"` / `… bundle_mainmenualwaysloaded_assets_all
            //    "ChatMessageRow"` 逐颗现读）⇒ 本窗全部 `LatinCardBody`（Pragati）。
            var ph = Text(input, InputR, Loc.T("MainMenu/Chat/TypeMessage"), new Color(1f, 1f, 1f, 0.439f), "Placeholder", 28f, QText,
                 autoMin: 0f, alignLeft: true, face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-14（A526）**：原版这一颗是 **`折行 = 3`（`PreserveWhitespaceNoWrap`）**，
            //   而 `MenuDraw.TextBox` 建出来的恒是 `Normal(1)` ⇒ **只有输入框这一族这一格不对**
            //   （同族 `ChatMessageRow/{Sender,Time,Message}` 原版都是 `1` ⇒ 那一族我们对）。
            //   判据 = A493#10 那一行 dump 的原文（本批重读，逐字）：
            //     `| ······6 | Placeholder | … | 'Type message' 字号=28.0 基准=28.0 对齐=Left/Middle 折行=3 色=(1,1,1,0.439) |`
            //   ⚠️ **顺序**：`SetWrappingMode` 内部会 `ForceRelayout()`（A205）⇒ **它之后必须重新对齐一次**
            //   （`Battle/Label.cs` 那个口的头写着「要在对齐/量宽之前调」；同形先例 = `Deck/DeckRuntime.cs` 里 `_fltInputText.SetWrappingMode(…)` 之后那次对齐
            //   的 `SetWrappingMode(3)` → `AlignLeftOn`）。⛔ 别拿 `SetWrapping(false)` 顶替 —— 那是 `0` 档。
            if (ph != null)
            {
                ph.SetWrappingMode(3);
                MenuDraw.AlignLeft(ph, InputR);      // 重排会把 TMP 子节点挪走 ⇒ 对齐要压在它后面
            }
            MenuDraw.Hit(input, "InputHit", InputR, QHit, () => Debug.Log(
                "[Chat] 输入框**打不了字** —— 我们这套外壳没有文字输入系统；而且**聊天收发本身还没做**"
              + "（用户 2026-09-26 口径：网络聊天功能暂时不做，界面照建、数据留空态）。"));
            var send = Node(_enterText, "Button", SendBtnR);
            var sendIcon = Rect(send, "40k_UI_Chat_send", SendBtnR, "Image", QBg, null, true);
            // 🆕 **2026-10-19（N70 · U3）：悬停 / 按下反馈** —— 判据 = 原版 `ChatPanel > Enter Text > Button`
            //   （`everguild` 那颗 `EverguildButton`）：**`m_Transition = 1 (ColorTint)`** · `m_TargetGraphic` =
            //   **它自己那颗 `Image`**（本处那颗 `40k_UI_Chat_send` 图标）· `m_Colors` 是 UGUI 默认那组
            //   （HL `0.9607843` / PR `0.7843137` / fade `0.1`）—— 逐字段现读
            //   `python -I 工具/menu_dump.py bundle_menus_assets_all "ChatPanel" --depth 4`（那一行印 `trans=1`）。
            //   ⚠️ `TintOn` 而不是 `Bind`：原版 `m_SpriteState` 四格全空 ⇒ **不换图**、只做色偏。
            var sendHit = MenuDraw.Hit(send, "Hit", SendBtnR, QHit, () => Debug.Log(
                "[Chat] `TrySendMessage`：**没有服务器**，也没有 `ChatGlobalManager` 的对等物 ⇒ 发不出去。"),
                clickCue: WindowSounds.SendMessageCue);
            var sendWb = sendHit != null ? sendHit.GetComponent<WindowButton>() : null;
            if (sendWb != null && sendIcon != null) sendWb.TintOn(sendIcon);
            // 🔴 **2026-10-25（E4 · 账 311②）**：`CheckChatBan` 要按禁言态置灰/置文字 ⇒ 这两颗得抓住。
            _sendBtn = sendWb; _inputHint = ph;

            // 右上关闭钮（`ChatPanel.closeButton` 指的就是它）
            // 🔴🔴 **2026-10-18（A1149 第一半）圆底盘【挂点】归真**（本件 = 第九会话 P6）：
            //   改前圆底盘画在一颗**自造子件 `Background Round`** 上、而 `Generic Close Button Orange` 是颗**裸节点**；
            //   原版那颗 `Image` **就长在根节点自己身上**（无独立子件名）。
            //   判据（逐字段直读）=
            //   `python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all "ChatPanel" 8`：
            //   根 `Chat/Holder/Chat/Generic Close Button Orange` 自己带 `Image`：
            //   `UI_Button_Round_background` · `m_Type=0`(Simple) · **`m_PreserveAspect=1`** ·
            //   `m_PixelsPerUnitMultiplier=1.0` · `m_RaycastTarget=0` · 矩形 **74.39×75.61**；
            //   贴图 `m_Rect` = **237×237 正方** ⇒ 实绘 **74.39×74.39**。
            //   ⇒ 照兄弟窗先例 `Shell/BaseOfferPopup.cs` / `Shell/ReferralPopupWindow.cs` 的形状
            //   （`Rect` 画在根节点上、图取不到才退回 `Node`）。矩形与 `keepAspect` **改前就是对的**（本处只改挂点）。
            // 🆕 **2026-10-18（A1058 · 第六会话批 2）**：悬停换图那一层 = **子件 `Background`**
            //   （画的是 `40k_general_bt_yellow`，= 下面那颗 `closeFaceQ`），⛔ **不是圆底盘**。
            //   判据（原版 prefab 亲读）=
            //   `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "ChatPanel" --depth 8`：
            //   根 `Chat/Holder/Chat/Generic Close Button Orange` 那颗 `EverguildButton` 的
            //   **`m_TargetGraphic` = pid-1690241084677247438**；解该 pid ⇒ **所属 GO 名 = `Background`**、
            //   贴图 pid `5693181797853584851` → `40k_general_bt_yellow`（`d:/4/_tmp_view/sprite_pids_ALL.json`）。
            //   ⚠️ **2026-10-18 更正（铁律 5）**：原注释写「`trans=2` 换的是它自己的 Image」——
            //   **那句是错的**（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）⇒
            //   改前悬停把**圆底盘**换成黄圆图、黄圆本身不变。
            var closeBase = Rect(chat, "UI_Button_Round_background", CloseBtnR, "Generic Close Button Orange", QBg, null, true);
            var close = closeBase != null ? closeBase.transform : Node(chat, "Generic Close Button Orange", CloseBtnR);
            var closeFaceQ = Rect(close, "40k_general_bt_yellow", CloseIconR, "Background", QContent, null, true);
            Rect(close, "40k_general_bt_yellow_close", CloseIconR, "Icon", QContent + 1, null, true);
            // 🆕 A17：原版 `Chat>Holder>ChatPanel>Generic Close Button Orange` 是 SpriteSwap（实测 HL 见下）
            // 🆕 **2026-10-18（A1053 · 第六会话批 2）**：**命中区**归真值 —— 原版圆底盘
            //   `m_RaycastTarget = 0`，吃射线的是同矩形两颗子件（56.86×58.12）按 `(-20)⁴` 外扩
            //   ⇒ **96.86 × 98.13**；改前传根矩形 `CloseBtnR`（74.38×75.60）⇒ 每边小 11.2。
            //   判据 = `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all "ChatPanel" --depth 10
            //   --substr "Generic Close Button"`（实读 `96.86 x 98.13`）。
            MenuDraw.Hit(close, "Hit", MenuDraw.PaddedRect(CloseIconR, ClosePad),
                         QHit, () => Close(), closeFaceQ, "40k_general_bt_yellow",
                         "40k_general_bt_yellow_hover", clickCue: WindowSounds.DefaultCloseCue);

            // 玩家选项面板（出厂 act F；点消息行上的头像才亮）
            _options = Node(chat, "Player Options Panel", OptPanelR);
            Nine(_options, "40k_topmarquee_currency_display BW", OptPanelR, new Vector4(15f, 15f, 15f, 15f),
                 "Background", QContent, new Color(0.311f, 0.201f, 0.201f, 1f));
            // 🔴 **A406**：`Holder/Chat/Player Options Panel/Name` 原版
            //   `auto[10.0~**40.0**] 基准=**21.0**` ⇒ 上限 40（= 标称）· base **21.0**（同一条 dump）。
            // 🔴 **2026-10-25（E4 · 账 303/314/330）**：把 `Name` 那一颗抓在手里 —— 它现在要按
            //   **被点的那条消息**填 `messageData.playerName`（原版 `ChatPlayerOptionsPanel.ShowOptionsFor`
            //   第一句 `title.text = messageData.playerName`）。改前它恒为 prefab 样例串 `Fulanito Name`。
            _optName = Text(_options, OptNameR, "Fulanito Name", Color.white, "Name", 40f, QText, 10f, false, 40f, 21f,
                            face: TmpFont.Face.LatinCardBody);
            // `Buttons`（原版是个 `VerticalLayoutGroup`，5 个键排在它下面 —— 保留这一层，别把键挂到面板上）
            var optBtns = Node(_options, "Buttons", new PxRect(176.58f, 399f, 563.88f, 727f));
            string[] acts = { "Add as a friend", "Challenge", "Report message", "Block player", "Profile" };
            string[] nodes = { "Add as a friend", "Challenge", "Report", "Block", "Profile" };
            // 🆕 **2026-10-19（战斗线 `T17`）**：四颗钮的**官方词条**（逐颗实读 `bundle_menus_assets_all`
            //   的 `Localize.mTerm`：`MainMenu/Chat/{Add,Challenge,Report,Block}`）。
            //   `Profile` 那颗 prefab 上**没有 `Localize`** ⇒ 保留原 TMP 文本 `Profile`（⛔ 别拿
            //   `PlayerProfile/Profile` 硬套 —— 它是另一扇窗的标题，没有证据表明原版这颗钮用它）。
            //   ⚠️ `acts[i]` 仍是**动作 id**（`OnOption` 按它分支 + 日志），`actTerms[i]` 只决定**印什么字**。
            string[] actTerms = { "MainMenu/Chat/Add", "MainMenu/Chat/Challenge", "MainMenu/Chat/Report",
                                  "MainMenu/Chat/Block", null };
            for (int i = 0; i < nodes.Length; i++)
            {
                float y = OptPanelR.y1 + 50f + i * OptRowStep;
                var rowR = new PxRect(OptPanelR.x1 + 15f, y, OptPanelR.x2 - 15f, y + OptRowH);
                var row = Node(optBtns, nodes[i], rowR);
                var rowNine = Nine(row, "UI_Button_Mulligan", rowR, new Vector4(333f, 96f, 333f, 96f), "Image", QContent);
                // 🔴 **A406**：`Holder/Chat/Player Options Panel/Buttons/{Add as a friend,Challenge,Report,
                //   Block,Profile}/Button Text` 五颗**逐颗读过** —— 全是 `auto[10.0~**30.0**] 基准=**12.0**`
                //   ⇒ 上限 30（= 标称）· base **12.0**。判据 = 同一条 `menu_dump … "ChatPanel" --depth 10 --md`。
                string shown = actTerms[i] != null ? Loc.T(actTerms[i]) : acts[i];
                var textR = new PxRect(rowR.x1 + 12.69f, rowR.y1 - 19.65f, rowR.x2 - 13.84f, rowR.y1 + 57.14f);
                Text(row, textR, shown, Color.white, "Button Text", 30f, QText, 10f, false, 30f, 12f,
                     face: TmpFont.Face.LatinCardBody);
                string act = acts[i];
                // 🆕 A17：原版 `Chat>Player Options Panel>Buttons>*` 五颗都是 SpriteSwap（普查 §块 5 第 19 行）
                // 🆕 **2026-10-19（战斗线 `T16` · 命中区全仓普查）**：命中区 = **底图 ∪ `Button Text`**
                //   （原版那两颗**都吃射线**：钮底图 `Image.m_RaycastTarget=1`、`Button Text` 那颗 TMP
                //    也 `m_RaycastTarget=1`，两处 pad 都是 `(0,0,0,0)`；判据 = 本包 prefab 直读
                //    `ChatPanel::Holder/Chat/Player Options Panel/Buttons/<5 颗>`）。
                //   原版并集 = **357.30×86.17**（文字那颗 330.77×86.17 把整行上下各撑出 ≈20.9）。
                //   ⚠️ **我们画的 `Button Text` 只有 76.79 高**（`rowR.y1−19.65 … +57.14`）⇒ 并集按
                //   **我们画出来的文字框**算（77.25 高），⛔ 不照抄原版 86.17 —— 那会在画的东西外面
                //   留一圈点得到但看不见的区。文字高度差 9.4px 是**另一笔**（A406 的文字几何），
                //   不属本件（本件不改视觉 rect）。
                var hitR = MenuDraw.Union(rowR, textR);
                var oh = MenuDraw.Hit(row, "Hit", hitR, QHit, () => OnOption(act),
                                      clickCue: WindowSounds.PositiveCue);
                var owb = oh != null ? oh.GetComponent<WindowButton>() : null;
                if (owb != null) owb.BindNine(rowNine, "UI_Button_Mulligan");
                // 🔴 **2026-10-25（E4 · 账 330②）**：`Add as a friend` 这颗的可点态是**按被点的那条消息**
                //   算的 —— 原版 `ChatPlayerOptionsPanel.ShowOptionsFor` 里
                //   `AddButton.interactable = 好友表里查不到该 playerId`（判据 `ChatPlayerOptionsPanel.cs:74-112`）。
                //   抓在手里，由 `ShowOptionsFor(message)` 每次刷。
                if (act == "Add as a friend") _optAddFriend = owb;
            }
            _options.gameObject.SetActive(false);   // 出厂 act F（判据 ⑤）

            BuildTabs();
            if (MissingArt.Count > 0)
                Debug.LogWarning("[Chat] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        // ============================================================ 频道键的四态（A118①）
        // 🔴 **2026-10-07 就地订正（原来这两处与原版都不符）**：① 那一层只建**一份**、贴图**写死
        //    `40K_settings_button_hover`**；② `RefreshHighlights` 里 `SetActive(i == sel)`
        //    ⇒ **未选中时整层根本不在**（原版画的是 `offSprite`），而且**没有悬停/按下两档**。
        //
        // 判据（`bundle_menus_assets_all`，逐字段实读；图名由 `工具/menu_dump.py` 的 `sprite_index()` 解出）：
        //   · 对象 = `GameObject/Orange Tab Toggle.json`（出厂 act T）与
        //     `GameObject/Orange Tab Toggle_2394806283186384434.json`（出厂 act F）——
        //     🔴 **本窗用的是后者**：`ChatPanel > Tab Buttons` 那颗 `TabButtons` 组件的
        //     `tabButtonPrefab` 逐字指向 **act F 那一份**（`MonoBehaviour_-2151756644979855822.json` →
        //     pid `631496939800931890` = 那份上的 `EverguildToggle`）。键内几何两份一致
        //     （`button_bg` 铺满 165×157.684 · `Icon` (0,17.34)-(160,142.67) · `Label` (5,106.04)-(160,146.04)），
        //     我们现有的数就是照它们来的。
        //   · 组件 = 类 **`EverguildToggle`**（pid `-3271546083985396321` / `631496939800931890`）：
        //     `m_Transition = 2`(SpriteSwap) · **`colorTintOnValueChange = 0`** ⇒ `offColor(0.75,…)` **不生效**
        //     （切页**只换图、不换色**）· `changeSpriteOnValueChange = 1` ·
        //     `spriteToChange` = `m_TargetGraphic` = `button_bg` 那颗 Image ·
        //     `offSprite = 40K_settings_button`（未选中）· `onSprite = 40K_settings_button_hover`（选中）·
        //     `m_SpriteState.m_HighlightedSprite = 40K_settings_button_selected`（**悬停**那一档；
        //      那一张**不是** `_hover`）· `m_PressedSprite = 40K_settings_button_pressed`（按下那一档）·
        //     `m_SelectedSprite = 40K_settings_button_hover`（= `onSprite`；那是 uGUI 的「键盘选中」态，
        //      指针操作下不会出现 ⇒ 与选中态**同图**、无可观测差异，不复刻这一档）。
        //   · 语义（`d:/2/tools/decomp_full/EverguildToggle__ToggleSprite.c`）= `Image.set_sprite(isOn ?
        //     onSprite : offSprite)`（**改 `sprite`、不改颜色**）；`EverguildToggle__DoStateTransition.c:17`
        //     先调基类 `Selectable.DoStateTransition` ⇒ 悬停期间由 `overrideSprite` 顶上、离开落回 `sprite`
        //     ⇒ **两态自洽**。
        //   · 🔴 我们这套的对应物 = `WindowButton.SwapTo`（悬停换 `_tex`）/ `Exit`（落回 `_normalTex`）
        //     ⇒ **切页换完底图必须 `SetNormalTex`**，否则离开悬停会还原成「切页之前那张」
        //     （先例：`PlayerProfileWindow.RefreshHighlights`，同一条纪律）。
        //   ⚠️ 原版 `EverguildToggle.graphic = null`（页签**没有** `On` 子件）—— 与阵营格那种
        //      「`graphic` 指 `On` 遮一层」的情形**不是一回事**，⛔ 别照搬。
        const string TabArtOff = "40K_settings_button";              // offSprite = 未选中
        const string TabArtOn = "40K_settings_button_hover";         // onSprite  = 选中
        const string TabArtHover = "40K_settings_button_selected";   // m_HighlightedSprite（悬停）
        const string TabArtPressed = "40K_settings_button_pressed";  // m_PressedSprite（按下）

        /// <summary>🔴 **2026-10-25（N9 · 账 313）**：频道页签的**图标**（原版按频道给，不是同一张）。
        /// <para>判据（三条一起）：① `ChatPanel.SetupTabs` 给每个频道传
        /// `icon = ChannelType == 0 ? globalIcon : allianceIcon`；② 那两颗字段在组件
        /// `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_1773913423464410674.json` 上
        /// （`globalIcon` pid `1694953385498620726` → `40K_Chat_icon_Global`；
        /// `allianceIcon` pid `-4762003504173451848` → `40K_Chat_icon_Alliance v2`）；
        /// ③ 贴图动作 = `TabButtons.AddTabButton` 里的 `Image.set_sprite(icon)`。</para>
        /// <remarks>`Channels[0] = "Global"` · `Channels[1] = "Alliance"`（本文件 :115）⇒ 下标就是 `ChannelType`。
        /// ⚠️ 导出资产名里那个空格是 `_`（`40K_Chat_icon_Alliance_v2`）—— 与 `LeaderboardWindow` 那两页签
        /// 用的是**同一批图**（自检 `MainMenuScene.cs:10420/11472` 已断言过贴图名）。</remarks></summary>
        static string TabChannelIcon(int idx)
        {
            return (idx == 0) ? "40K_Chat_icon_Global" : "40K_Chat_icon_Alliance_v2";
        }

        /// <summary>`button_bg` 的色。🔴 **两份 prefab 的值不同**：act T 那份是 `(1, 0.5723677, 0, 1)`，
        /// **act F 那份（= 本窗 `tabButtonPrefab` 实际克隆的那份）是 `(1, 0.4274509847, 0, 1)`** —— 取后者
        /// （我们原来的 0.427 一直是对的，可反证）。⚠️ 两颗 Image 都带 `m_PixelsPerUnitMultiplier = 0.92`，
        /// 本工程 `ImageQuad` 没有这个属性（Simple 图上影响很小，如实记着）。</summary>
        static readonly Color TabBgTint = new Color(1f, 0.427f, 0f, 1f);

        /// <summary>一个频道键（底 `button_bg` + **按频道分的图标** + 文案）。
        /// <para>🔴 **2026-10-25（N9 · 账 313）**：原来两个频道键**都**画母版那张 `40K_icon_menu_chat`。
        /// 原版 `ChatPanel.SetupTabs` 是给每个频道传 `icon = ChannelType == 0 ? globalIcon : allianceIcon`，
        /// 由 `TabButtons.AddTabButton` 里 `Image.set_sprite(icon)` 贴上去
        /// （组件 `MonoBehaviour_1773913423464410674.json`：`globalIcon` pid `1694953385498620726` →
        /// **`40K_Chat_icon_Global`**；`allianceIcon` pid `-4762003504173451848` → **`40K_Chat_icon_Alliance v2`**；
        /// 两条判据的落点见 `TabChannelIcon` 的注释）。⇒ 现在按频道取图，与 `LeaderboardWindow` 那两页签同一批图。</para>
        /// <remarks>⚠️ 原版那个模板上挂的 `Label` **自己就是 inactive 的**（§A·1 第 67 行），
        /// 所以它的文案 `General` 是**设置窗的 term 残留**（`Settings/General/Title`）—— 我们**不用它**，
        /// 用频道的名字（`Global` / `Alliance`），并在这里说明这是我们的选择。</remarks></summary>
        Transform TabButton(Transform parent, int idx, string label, float y1, float y2)
        {
            var r = new PxRect(TabBtnColR.x2 - TabBtnSz.x, y1, TabBtnColR.x2, y2);
            var b = Node(parent, "Orange Tab Toggle " + idx, r);
            // `button_bg`：**两态都在**（原版换的是这张 Image 的 `sprite`）—— 出厂一律画 `offSprite`，
            // 选中那一档由 `RefreshHighlights` 换图（原版 `EverguildToggle.ToggleSprite` 就是那一刻跑的）。
            _tabBg[idx] = Rect(b, TabArtOff, r, "button_bg", QBg, TabBgTint);
            var iconR = new PxRect(r.x1, r.y1 + 17.34f, r.x2 - 5f, r.y2 - 15.01f);
            Rect(b, TabChannelIcon(idx), iconR, "Icon", QContent, null, true);
            // 🔴 **A406**：`Orange Tab Toggle/Label/Tab Toggle Title` 原版
            //   `auto[10.0~**35.0**] 基准=**35.0**` ⇒ 上限 35（= 标称）· base **35.0**。
            //   ⚠️ **两份实例逐字段读过、同值**（act T 那份 RT `-8278251871369237089` 与
            //   本窗 `tabButtonPrefab` 实际克隆的 act F 那份 RT `2421717888838184498`）——
            //   「只看 ChatPanel 树里那一份」在这里恰好也对，但是**读过才知道**。
            Text(b, new PxRect(r.x1 + 5f, r.y1 + 106.04f, r.x2 - 5f, r.y1 + 146.04f), label, Color.white,
                 "Label", 35f, QText, 10f, false, 35f, 35f, face: TmpFont.Face.LatinCardBody);
            // 🔴 A17 的两档**逐颗显式给**（`40K_settings_button` 那条后备规则推出的是
            //    `40K_settings_button_hover`，而原版这一颗的悬停图是 `…_selected`）——
            //    不显式给就是「悬停换了张错的图」+ `MissingSwapArt` 记一条。
            var hit = MenuDraw.Hit(b, "Hit", r, QHit, () => tabButtons.Click(idx),
                                   _tabBg[idx], TabArtOff, TabArtHover, TabArtPressed,
                                   clickCue: WindowSounds.SelectToggleCue);
            _tabBtn[idx] = hit != null ? hit.GetComponent<WindowButton>() : null;
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C9）**：原版这两颗频道键是 `EverguildToggle` +
            //   **`allowClickWhenAlreadySelected = 0`** ⇒ 再点当前频道时连按钮音一起跳过。
            if (_tabBtn[idx] != null)
            {
                _tabBtn[idx].allowClickWhenAlreadySelected = false;
                _tabBtn[idx].isOn = () => tabButtons != null && tabButtons.CurrentVisualIndex == idx;
            }
            return b;
        }

        /// <summary>两个频道页（`Chat Tab`：`Viewport`(`RectMask2D`) → `Content`，消息行挂 `Content`）。</summary>
        void BuildTabs()
        {
            tabs.Clear();
            for (int i = 0; i < Channels.Length; i++)
            {
                var t = Node(tabHolder, "Chat Tab " + Channels[i], TabsR);
                var page = t.gameObject.AddComponent<ChatTab>();
                page.SetHost(this, t, TabsR, Channels[i]);
                tabs.Add(page);
                t.gameObject.SetActive(false);
            }
            foreach (var t in tabs) t.Setup();
            var tab = tabs.Count > 0 ? tabs[0] as ChatTab : null;
            if (tab != null) tab.OnOpen();
        }

        /// <summary>切页：**换底图**，不换色、不显隐。
        /// 判据 = 原版 `EverguildToggle.ToggleSprite`（`Image.set_sprite(isOn ? onSprite : offSprite)`；
        /// `colorTintOnValueChange = 0` ⇒ `offColor` 不生效）—— 见 `TabButton` 上面那段四态判据。
        /// 🆕 2026-10-07（A118①）：原来这里是 `SetActive(i == sel)`（**未选中那一层根本不建**）。</summary>
        public override void RefreshHighlights()
        {
            int sel = tabButtons != null ? tabButtons.CurrentVisualIndex : -1;
            for (int i = 0; i < _tabBg.Length; i++)
            {
                if (_tabBg[i] == null) continue;
                var t = Art(i == sel ? TabArtOn : TabArtOff);
                if (t == null) continue;                  // 取不到就留在旧图上（`Art` 已记进 `MissingArt`）
                _tabBg[i].SetTexture(t);
                // 🔴 `SetTexture` 会把 `_aspect` 冲成**贴图自己的**比值（两张都是 168/156 = 1.0769）——
                //    拉回「按原版矩形定的」那个（165/157.684 = 1.0464）。不拉回来这颗 quad 的 `WorldW`
                //    会从 165 变成 169.8：**画面形状看不出、逻辑宽度被改掉**（同 `WindowButton.SetOn`
                //    与 `PlayerProfileWindow.RefreshHighlights` 那条纪律）。
                _tabBg[i].SetAspect(TabBtnSz.x / TabBtnSz.y);
                // 选中态是**这里**换的底图 ⇒ 同步给按钮记的「常态图」（否则悬停退出会还原成切页前那张）。
                if (_tabBtn[i] != null) _tabBtn[i].SetNormalTex(t);
            }
        }

        void OnOption(string act)
        {
            if (act == "Challenge")
            {
                // ★ 这一条**真的能开**（原版 `ChatPlayerOptionsPanel.Challenge` → `ChallengeManager.OpenStartChallengeWindow`）
                Debug.Log("[Chat] `Challenge` ⇒ 开**好友挑战弹窗**（原版走 `ChallengeManager.OpenStartChallengeWindow`）");
                var wm = Manager;
                if (wm != null)
                {
                    var d = DuelPopupWindow.Create(wm, "Everrookie2");
                    wm.OpenWindow(d);
                }
                else Debug.LogWarning("[Chat] 没有 `WindowsManager`，挑战弹窗开不了");
                return;
            }
            // 🔴 **2026-10-25（N9 · 账 329）**：原版 `Block player` / `Report message` / `Add as a friend` 三条**先弹一次二次确认**
            //   （`ChatPlayerOptionsPanel.ShowConfirmation`，判据 `ChatPlayerOptionsPanel.cs:168-221`：`ShowPopUp(text, localizeTexts:false, closeOnEsc:false, OK/Cancel)`）
            //   ——**这一步纯客户端、不碰服务器** ⇒ 本地可复刻、现在真的会弹（原来三条一概直接出声、确认框整个没有）。
            // 🆕 **2026-10-28（`B90-N5`）：`Block player` 的 onConfirm 也本地化了** —— 原版 = `instance.BlockPlayer(messageData.playerId)`
            //   （ILSpy `:245`），而它方法体 **0 处后端调用**（`ChatGlobalManager__BlockPlayer.c:27-77`）⇒ `Shell/ChatCache.BlockPlayer`；另两条仍要服务器。
            if (act == "Block player" || act == "Report message" || act == "Add as a friend")
            {
                ShowConfirmation(ConfirmTextOf(act), () => { if (act == "Block player") ChatCache.BlockPlayer(OptionsFor != null ? OptionsFor.PlayerId : null); else LogServerOnly(act); });
                return;
            }
            LogServerOnly(act);
        }

        /// <summary>确认之后**仍要服务器**的那两条（`SendReportMessage` / `AddFriendToList`）—— ⚠️ 2026-10-28（`B90-N5`）就地订正：原文把 `BlockPlayer` 也算在内，而它方法体 0 处后端调用、现已走 `Shell/ChatCache.BlockPlayer`。</summary>
        void LogServerOnly(string act)
        {
            Debug.Log("[Chat] `" + act + "`：要**服务器**（原版 `ChatPlayerOptionsPanel." + act.Replace(" ", "") + "`）。");
        }

        /// <summary>三条危险动作的确认文案。⚠️ **这几句是我们自拟的**：原版那三句是 I2 词条
        /// （`资料/普查产出_1013/WL2_四扇小窗.md:297` 现核：`…PopupButtonString` 那几个**不是序列化字段**、
        /// 运行时从 I2 填）⇒ 本地一个 value 都没有，⛔ 不硬套官方键。</summary>
        static string ConfirmTextOf(string act)
        {
            switch (act)
            {
                case "Block player": return "Block this player?";
                case "Report message": return "Report this message?";
                case "Add as a friend": return "Send a friend request?";
                default: return "Are you sure?";
            }
        }

        /// <summary>确认弹窗（等价实现 = ShowConfirmation，原版判据 `ChatPlayerOptionsPanel.cs:168-221`）：
        /// OK ⇒ **先跑回调再关窗**；Cancel ⇒ 只关窗；`localizeTexts:false` + `closeOnEsc:false`。
        /// <para>⚠️ 原版两颗钮的词条是**运行时从 I2 填**的（同 `AllianceMemberOptionsPopup` 那一族）⇒ 本机读不到；
        /// 这里取本仓通用那一对 `MainMenu/General/OK` / `MainMenu/General/Cancel`
        /// （`Shell/CardDetailPopup.cs:2102-2103` 逐字段实读过同一对）—— **这是我们的选择，不是原版判据**。</para></summary>
        public void ShowConfirmation(string confirmationText, System.Action onConfirm)
        {
            var wm = Manager;
            if (wm == null)
            {
                Debug.LogWarning("[Chat] 没有 `WindowsManager` ⇒ 确认弹窗开不了（原版 `ShowConfirmation` 这一跳）");
                return;
            }
            wm.ShowPopUp(confirmationText, Loc.T("MainMenu/General/OK"),
                         onConfirm != null ? onConfirm : (System.Action)(() => { }),
                         Loc.T("MainMenu/General/Cancel"), null);
        }

        /// <summary>消息行数变化时重画当前页（自检用；原版是 `ChatTab.CheckMessages`）。</summary>
        public void RefreshMessages()
        {
            foreach (var t in tabs)
            {
                var ct = t as ChatTab;
                if (ct != null && ct.gameObject.activeSelf) ct.Rebuild();
            }
        }

        // ============================================================ 选项面板 / 禁言（2026-10-25 · E4 · 账 303/314/330/311）
        // 🔴 新增成员一律追加在类尾（同日纪律：本文件被账按行号引用得多，插进已有成员之间会把下游行号整体推移）。

        Label _optName;
        Label _inputHint;
        WindowButton _optAddFriend, _sendBtn;

        /// <summary>当前选项面板对着的那条消息（`null` = 面板没开 / 已收起）。自检读口。</summary>
        public SocialData.ChatMessage OptionsFor { get; private set; }

        /// <summary>= 原版 `ChatPanel.ShowOptionsPanel(message)` + `ChatPlayerOptionsPanel.ShowOptionsFor(message)`：
        /// 按**被点的那条消息**填面板并摆出来。判据逐句现读（`ChatPanel.cs:561-563` ·
        /// `ChatPlayerOptionsPanel.cs:74-112`）：① `title.text = messageData.playerName`；
        /// ② `AddButton.interactable = 好友表里查不到该 playerId`。
        /// <para>🆕 **2026-10-27（C5 · 账 328）**：**按行定位这一跳已接** —— 原版
        /// `ChatPlayerOptionsPanel.ShowOptionsFor` 里那句「x/z 取面板自己的、y 取被点消息的 `transform.position.y`」
        /// （判据 `decomp_full/ChatPlayerOptionsPanel__ShowOptionsFor.c:52-66` + `…__SetPositionInScreen.c`）：
        /// 我们全是画布 px ⇒ 等价 = **保持面板自身矩形的高与 x，把中心 y 平移到被点那一行**（⛔ 不夹边界，原版也不夹）。</para>
        /// <para>⚠️ 本地**没有** `playerId` 的写入方：`SocialData.ChatMessage.PlayerId` 是本轮新加的字段，
        /// 默认 null ⇒ ② 退化成「一律可点」（与改前同观感），但**链路已经通了**：真数据填上 id 与好友表
        /// 之后这两条自己就生效（⛔ 不编一个假 id）。</para></summary>
        public void ShowOptionsFor(SocialData.ChatMessage m, float rowCenterY = float.NaN)
        {
            if (_options == null) { Debug.LogWarning("[Chat] 选项面板没建起来（`_options == null`）—— 点不开"); return; }
            OptionsFor = m;
            if (_optName != null)
                _optName.SetText(m != null && !string.IsNullOrEmpty(m.Sender) ? m.Sender : "");
            if (_optAddFriend != null)
            {
                bool alreadyFriend = m != null && IsFriend(m.PlayerId);
                _optAddFriend.Interactable = !alreadyFriend;
            }
            _options.gameObject.SetActive(true); if (!float.IsNaN(rowCenterY)) { float dy = rowCenterY - (OptPanelR.y1 + OptPanelR.y2) * 0.5f; MenuDraw.ApplyPxRect(_options, _options.parent, new PxRect(OptPanelR.x1, OptPanelR.y1 + dy, OptPanelR.x2, OptPanelR.y2 + dy)); }
            Debug.Log("[Chat] 开选项面板：对着「" + (m != null ? m.Sender : "?") + "」"
                    + "（原版 `ChatPanel.ShowOptionsPanel → ChatPlayerOptionsPanel.ShowOptionsFor(message)`；"
                    + "5 个钮的落地仍要服务器）");
        }

        /// <summary>好友表里有没有这个 id（原版 `FriendsManager` 的本地等价）。
        /// ⚠️ `SocialData.Friend.Id` 是本轮新加的字段；空 id 一律当「查不到」。</summary>
        public static bool IsFriend(string playerId)
        {
            // ✅ 2026-10-27（C5 · 账 373）：规则**收口**到 `SocialData.IsFriend`（全仓唯一一份实现）。
            //    两处写同一规则 = 迟早不一致；这里保留同名包装，免破既有调用点。
            //    判据 = 原版 `FriendsData.IsFriend(string playerId)`（`FriendsData.cs:40`），按 id 查好友表。
            //    空 id 一律 false；好友表恒空 ⇒ 生产侧今天恒 false。
            return SocialData.IsFriend(playerId);
        }

        /// <summary>= 原版 `ChatPanel.OnTabChange()`（判据 `ChatPanel.cs:462-470`，逐句）：
        /// `base.OnTabChange()` → **`optionsPanel.SetActive(false)`** → `CheckChatBan()`。
        /// 🔴 原来我们**没有覆写** ⇒ 选项面板挂在 `Chat` 下、不随页签收 ⇒ 切页之后它还亮着（账 311①）。</summary>
        protected override void OnTabChange()
        {
            base.OnTabChange();
            if (_options != null && _options.gameObject.activeSelf)
            {
                _options.gameObject.SetActive(false);
                Debug.Log("[Chat] 切页 ⇒ 收起 `Player Options Panel`（原版 `ChatPanel.OnTabChange` 第一句）");
            }
            OptionsFor = null;
            CheckChatBan();
        }

        /// <summary>= 原版 `ChatPanel.CheckChatBan()`（判据 `ChatPanel.cs:472-530`，逐句）：
        /// 按**当前页那个频道**问一次禁言 —— 禁言 ⇒ 发送键 / 输入框 `interactable = false` + 输入框写那句禁言文案；
        /// 没禁 ⇒ 都恢复、输入框清空。
        /// <para>🔴 **本地没有禁言数据源**：原版读 `PlayerDataManager.singletonManager.IsBannedFromChat(channelType)`
        /// （PlayFab 玩家档案）⇒ 接线口 = <see cref="ChatBanProvider"/>（**默认 null = 不禁言**）。</para>
        /// <para>⚠️ 两处**如实差异**（今天观感不受影响，因为默认不禁言）：
        /// ① 我们的「输入框」是**占位文字**（`MainMenu/Chat/TypeMessage`），没有真 `TMP_InputField`
        /// ⇒ 「清空输入框」那一支**不执行**（清掉占位串反而是错的样子）；
        /// ② 禁言文案原版是 I2 词条（本地无 value）⇒ 用 <see cref="BannedInputText"/> 那句自拟措辞。</para></summary>
        public void CheckChatBan()
        {
            int channelType = CurrentChannelType();
            bool banned = ChatBanProvider != null && ChatBanProvider(channelType);
            if (_sendBtn != null) _sendBtn.Interactable = !banned;
            if (_inputHint != null)
                _inputHint.SetText(banned ? BannedInputText : Loc.T("MainMenu/Chat/TypeMessage"));
            Debug.Log("[Chat] 禁言检查：频道 " + channelType + " ⇒ " + (banned ? "**已禁言**（发送键置灰）" : "没禁言")
                    + (ChatBanProvider == null ? "（本地没有禁言数据源 ⇒ 恒「没禁言」）" : ""));
        }

        /// <summary>禁言判据的接线口：参数 = 频道号（`0 = Global` / `1 = Alliance`，同 `ChatChannel.ChannelType`）。
        /// **默认 `null` = 一律没禁言**（本地没有 `PlayerDataManager.IsBannedFromChat` 的等价数据）。</summary>
        public static System.Func<int, bool> ChatBanProvider;

        /// 🔶 自己设计（非原版）：禁言时输入框里那句 `BannedInputText` · 原版是 I2 词条、本地无 value（原版判据拿不到） · TODO(谁定的)｜原注：<summary>禁言时输入框里那句。⚠️ 原版是 I2 词条（本地无 value）⇒ **这句是我们自拟的**。</summary>
        public const string BannedInputText = "You are banned from this chat channel.";

        /// <summary>当前页那个频道的号（`0 = Global` / `1 = Alliance`）—— 取不到页时按 0。</summary>
        int CurrentChannelType()
        {
            var tab = CurrentTabView as ChatTab;
            string ch = tab != null ? tab.Channel : (Channels.Length > 0 ? Channels[0] : null);
            for (int i = 0; i < Channels.Length; i++) if (Channels[i] == ch) return i;
            return 0;
        }
    }


    // ==================================================================
    //  `ChatMessageRow`（原版 `ChatMessageUI`，行模板 RT `7760131448890879999`）
    // ==================================================================

    /// <summary>消息行（聊天窗里**唯一**的「一行长什么样」的判据 —— §A·2 第 126–138 行）。
    /// 行里**两套头**：`Player Header`（自己发的，头像靠右）/ `Friend Header`（别人的，头像靠左），
    /// 同一时刻只亮一套；正文 `Message` 永远在 y=47。⚠️ 那个 y 是普查**按 uGUI 重算**的
    /// （`menu_dump` 因为 `m_IgnoreLayout` 那个 bug 把行内 y 全算偏了，§0·1 + §A·2 那两段）。
    /// 🔴 **行内一律按「相对行左上角」的偏移摆**（普查找那张表用的就是相对值；行宽由 viewport 给）——
    /// 别拿某个子节点的**世界坐标**去当像素用（那是两套单位，混了会静默摆歪）。</summary>
    public static class ChatMessageRow
    {
        const float PadX = 25f;          // 表里 `Player Header`/`Message` 的 x1 = 25.00
        const float HeadTop = 10f, HeadH = 25f;
        const float MsgTop = 47f;
        const float PBW = 146.03f, PBH = 163.14f;   // `Profile border` 146.03×163.14
        const float PBdx = 45f, PBdy = 15f;         // `a=(1,1) p=(0.5,1) pos=(45,15)`（自己发）/ `a=(0,1) pos=(-45,15)`（别人）

        /// <summary>一段文字 + **按裁切边界处理**（🆕 2026-10-05 · A38 顺手发现①）。
        /// 语义与 `MatchLogRow.Text` 逐条对齐（那一族两扇窗共用）：① **整块在框外 ⇒ 连节点都不建**
        /// （收口到 `MenuDraw.Visible`）；② 压在框边上的字**真的切**（`MenuDraw.ClipText` —— 原版
        /// `RectMask2D` 对文字与图**一视同仁**，TMP 逐字夹顶点 + 按同一仿射改 uv）。
        /// ⚠️ 裁的时机：`ClipText` 必须在**字号/换行/对齐都定完**之后调（`SetGlyphHeight` /
        /// `SetAutoFitBox` / `RefreshBounds` 任何一次重排都会把 mesh 重算回去）⇒ 本函数的调用方
        /// 一律「先建 → 再 `Align*` → 最后 `ClipText`」。
        /// 🆕 **2026-10-05（A78①）**：`clipSoft` = 软边（原版 `RectMask2D.m_Softness`，画布像素）——
        /// 原版掩码对文字**也是逐像素削 alpha** 的，所以文字不吃软边就等于「边带那一条不对」（静默）。
        /// ⚠️ 它**没有默认值**：漏传要**编不过**（照 `MenuDraw.Rect` 的 `uv0` 那条纪律），不是运行期静默。</summary>
        static Label Text(Transform p, PxRect r, string s, Color col, string n, float px, int q, PxRect? clip,
                          Vector2 clipSoft, bool alignLeft = false, bool alignRight = false,
                          TmpFont.Face face = TmpFont.Face.Cjk)
        {
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：改走 **`VisibleAbove`**（节点态那一版）——
            //   调用点（`ChatTab.Rebuild`）现在传的 `clip` 是 `null`（裁切状态在视口节点上）
            //   ⇒ 沿用裸 `Visible` 的话这里**永远放行**（静默：压在视口外的整行文字照建）。
            if (!MenuDraw.VisibleAbove(p, r, clip)) return null;
            // 🔴 **2026-10-12（A406）：本口【故意不加】`autoMaxPx` / `autoBasePx`** ——
            //   它的调用点全是 `ChatMessageRow`（独立根 `bundle_mainmenualwaysloaded_assets_all/GameObject/ChatMessageRow.json`）
            //   的件，而那一族**原版 `m_enableAutoSizing = 0`**（判据：
            //   `python 工具/menu_dump.py bundle_mainmenualwaysloaded_assets_all "ChatMessageRow" --depth 8 --md`
            //   ⇒ `Player Header/Sender` fs=18 基准=18 · `Time` 同 · `Message` fs=22 基准=22，**都没有 `auto[…]` 段**）
            //   ⇒ 上限/base **两格对本站不适用**；本口也固定传 `autoMinPx = 0f` ⇒ **与原来逐位等价**，
            //   **不需要**多开两个形参（多开=留一个永远不会被填的口）。
            //   ⚠️ **2026-10-14（A527）就地订正（铁律 5）**：这句原来写「它的**四个**调用点」——
            //   **现读只有 3 处**（`ChatMessageRow.Build` 里的 `Sender` / `Time` / `Message`；
            //   A406 立项时那 3 处在 `:678`/`:680`/`:698`，A435 阶段 2 之后漂到 `:722`/`:724`/`:749`）。
            //   **结论不变**（三处都是 `ChatMessageRow` 的件 ⇒ 不上上限/base 那两格），被订正的只是那个数。
            var lb = MenuDraw.TextBox(p, r, s, col, n, px, 0f, q, face: face);
            if (lb == null) return null;
            if (alignLeft) MenuDraw.AlignLeft(lb, r);
            else if (alignRight) MenuDraw.AlignRight(lb, r);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里是 `if (clip.HasValue) MenuDraw.ClipText(...)`。
            //   `clip` 现在恒为 `null` ⇒ 那个守卫恒假 ⇒ **这一族文字整块不吃裁切**（静默）。
            //   ⇒ 改成**无条件调**：`MenuDraw.ClipText` 内部自己解析（`ViewportClip.Resolve`）——
            //   没有节点、也没有显式框时它会**早退返回 false**（不会白挂守卫）；
            //   有节点时按**当下**的节点框裁（守卫每次重排都重新解析，A484 的判据）。
            //   ⚠️ 交出去的 `clip` 必须是**调用方原样那一份**（这里就是 `null`）—— ⛔ 别把解析后的框传进来。
            MenuDraw.ClipText(lb, clip, clipSoft);
            return lb;
        }

        /// <param name="clip">**显式覆盖**那一档的裁切边界（画布像素；`null` = 交给父链上的视口节点）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：生产调用点（`ChatTab.Rebuild`）现在传 **`null`** ——
        /// 裁切状态长在 `ChatTab/Viewport` 那颗 `ViewportClip` 上（迁移前是逐件传那一页的 `Viewport`）。
        /// 图/九宫格/命中区各自吃 `clip`（`MenuDraw` 那三个口传到 `Resolve`，非空 = 显式赢、`null` = 沿父链），
        /// 文字走上面那个 `Text` 包装。
        /// ✅ 软边（原版 `Viewport` 的 `m_Softness = (0,22)`）**已接**（🆕 2026-10-05 · A78①）——
        /// 值来自 `ChatTab.VpSoft`（原档逐字实读）；⚠️ **节点在时以节点的 `softness` 为准**，
        /// 本参数是**回落那一档**的带宽。
        /// 🔴 **命中区照旧【不吃】软边**（`MenuDraw.Hit` 故意不收它）：原版 `m_Softness` 只改渲染
        /// （掩码在 shader 里削 alpha），射线那一面只看矩形 ⇒ 命中区跟着缩就是**行为偏离**。</param>
        /// <param name="clipSoft">软边带宽（画布像素 · `(0,0)` = 硬边）。见 `ChatTab.VpSoft`。</param>
        public static void Build(ChatPanel win, Transform content, SocialData.ChatMessage m, PxRect r,
                                 PxRect? clip = null, Vector2 clipSoft = default(Vector2))
        {
            var row = MenuDraw.Node(content, "ChatMessageRow", r);
            float w = r.W;

            // `RowBackground`：`a=(0,0)-(1,1)` 全拉伸 · `WF_9Sliced` 九宫 (62,62,62,62)
            // 🔴 **2026-10-25（N9 · 账 300）**：**底色分两侧**。原来两侧都吃预制体模板色
            //   `(0.00392,0.0143,0.106,0.706)` —— 原版运行期在 `ChatMessageUI.Set` 里按 `isPlayer` 换掉：
            //   `background.color = isPlayer ? (0.0431,0.0941,0.1333,0.808) : (0.1059,0.0039,0.0039,0.706)`
            //   （判据 = prefab `bundle_mainmenualwaysloaded_assets_all/MonoBehaviour/
            //    MonoBehaviour_-7156338170297815041.json` 实读）。⇒ 见 `RowBgColor`。
            var bgTex = win.Art("WF_9Sliced");
            if (bgTex != null)
                MenuDraw.Nine(row, bgTex, r, new Vector4(62f, 62f, 62f, 62f), bgTex.width, bgTex.height,
                              ChatPanel.QContent, RowBgColor(m.Mine), true, "RowBackground",
                              clip: clip, clipSoftness: clipSoft);

            // 🔴 **2026-10-25（N9 · 账 301）**：**左右内边距按发信侧交换**。原版 `ChatMessageUI.SetLayout`
            //   把两个 padding 对调（prefab 的 VLG 基准是 `L25 / R60 / T10 / B10`）：
            //   **自己发 = L25 / R60**（名字在左、头像在右）、**别人发 = L60 / R25**。
            //   我们原来两侧恒 25 ⇒ 内容在「头像那一侧」多出 **35px**，头像自己（`PBdx` 是相对头内边距沿算的）
            //   也跟着偏 35。⇒ 见 `RowPadLeft` / `RowPadRight`（⛔ 别在别处再写一份 25/60）。
            float padL = RowPadLeft(m.Mine), padR = RowPadRight(m.Mine);

            // 头部：自己发的那套（`Player Header`）或别人的那套（`Friend Header`）
            var headR = new PxRect(r.x1 + padL, r.y1 + HeadTop, r.x2 - padR, r.y1 + HeadTop + HeadH);
            var head = MenuDraw.Node(row, m.Mine ? "Player Header" : "Friend Header", headR);

            // `Sender`（绿）与 `Time`（灰）铺在同一个矩形上：**一份左对齐、一份右对齐**
            // —— 自己发 ⇒ 名字靠右、时间靠左；别人发 ⇒ 反过来（两套头是镜像的）。
            var sR = new PxRect(headR.x1, headR.y1, headR.x2, headR.y1 + 24.38f);
            Text(head, sR, m.Sender ?? "", new Color(0.337f, 0.843f, 0.4f, 1f), "Sender", 18f, ChatPanel.QHead, clip,
                 clipSoft, alignLeft: !m.Mine, alignRight: m.Mine, face: TmpFont.Face.LatinCardBody);
            Text(head, sR, SocialData.TimeText(m), new Color(0.84f, 0.84f, 0.84f, 1f), "Time", 18f, ChatPanel.QHead, clip,
                 clipSoft, alignLeft: m.Mine, alignRight: !m.Mine, face: TmpFont.Face.LatinCardBody);

            // 头像框（`Profile border` + 里面的立绘）：自己发贴**右沿**（+45）、别人发贴**左沿**（−45），
            // 顶边都从头部顶边往上 15（`p=(0.5,1) pos.y=15`）。
            float pcx = m.Mine ? (r.x2 - padR + PBdx) : (r.x1 + padL - PBdx);
            var pbr = new PxRect(pcx - PBW * 0.5f, headR.y1 - PBdy, pcx + PBW * 0.5f, headR.y1 - PBdy + PBH);
            var pb = MenuDraw.Node(head, "Profile border", pbr);
            // ⚠️ 框与立绘的**先后照原版**：框（`Profile border` 自己身上的 Image）先、立绘（它唯一的子节点）后
            //    ⇒ 立绘盖住框（框心是不透明黑，立绘在下面就会整块看不见）。
            MenuDraw.Rect(pb, win.Art("Player_Profile_Border"), pbr, "Border", ChatPanel.QFrame, null, true, clip,
                          clipSoft);
            var pcR = new PxRect(pbr.x1 + 2.8f - 127.59f, pbr.y1 + 16.3f - 128.6f,
                                 pbr.x1 + 2.8f + 127.59f, pbr.y1 + 16.3f + 128.6f);
            MenuDraw.Rect(pb, string.IsNullOrEmpty(m.AvatarArt) ? null : CardArt.Cosmetics(m.AvatarArt),
                          pcR, "Profile content", ChatPanel.QAvatar, null, true, clip, clipSoft);

            // 正文（`Message`：22px · Left/Top · 折行）—— 永远在 y=47
            // 🔴 **2026-10-14（A525）**：原来这一行**两侧对齐一个都没传** ⇒ 吃那个静态口的缺省
            //   `alignLeft = false, alignRight = false` ⇒ 标签留在 `MenuDraw.Text` 摆的**矩形中心**（画成居中）。
            //   **上面那句注释一直是对的、是代码不对**（`H37 §四·3` 把它列在「没查全」里、W403 补跑 dump 查实）。
            //   判据 = 原版 `menu_dump … bundle_mainmenualwaysloaded_assets_all "ChatMessageRow" --depth 8 --md`：
            //     `| ·1 | Message | … | 'This is the message' 字号=22.0 基准=22.0 对齐=Left/Top 折行=1 色=(1,1,1,1) |`
            //   ⇒ 改法一行 = 补 `alignLeft: true`（⛔ 别改成右对齐 —— 原版是 `Left/Top`）。
            Text(row, new PxRect(r.x1 + padL, r.y1 + MsgTop, r.x2 - padR, r.y1 + MsgTop + 30f),
                 SocialData.Text(m), Color.white, "Message", 22f, ChatPanel.QText, clip, clipSoft, alignLeft: true,
                 face: TmpFont.Face.LatinCardBody);

            // 点头像 ⇒ 开玩家选项面板（原版 `ChatMessageUI.OnMessageClicked` / `ChatPlayerOptionsPanel`）
            // ⚠️ `clip` 也传下去（判据 = 原版 `RectMask2D` 的**射线那一面**：框外的点判不中任何东西）
            //    ⇒ 滚出视口的行**点不到**、压在视口边上的命中区**截到视口内**。
            // 🔴 **2026-10-25（E4 · 账 303/314/330 同一根因）**：原来这里只是「面板显隐取反」——
            //   **被点的那条消息 `m` 整个丢了** ⇒ 面板标题恒为 prefab 样例串、`Add as a friend` 恒可点、
            //   也没法按行定位。原版那三跳是：
            //     `ChatMessageUI.ShowOptions()`（`:270`）→ `ChatPanel.ShowOptionsPanel(message)`（`:561-563`，
            //     三行就一句 `optionsPanel.ShowOptionsFor(message)`）→
            //     `ChatPlayerOptionsPanel.ShowOptionsFor(ChatMessageUI message)`（`:74-112`）。
            //   ⇒ 现在把 `m` 原样传下去（`ShowOptionsFor` 是唯一消费口）。
            // 🔴 **2026-10-27（C5 · 账 296/299/328）**：① 头像命中区**只给「别人」那一侧**（原版 `portraitButton`
            //   只挂在 `Friend Header` 的 `Profile border` 上 —— 自己发的头像点不到，我们原来两侧都建）；
            //   ② **整行命中**挂在那颗 `MessageHit` 上 ⇒ 原版 `messageButton → OnMessageClicked`；
            //   ③ 队列：整行 = `QHit`（**必须高于 `Absorb` 那层 `QHit−1`**，否则聊天框底的吸收层先吃掉），
            //   头像 = `QHit+1`（高于整行 ⇒ **点头像优先**，不然点谁都退化成「点行翻译」）。
            if (!m.Mine) MenuDraw.Hit(pb, "Hit", pbr, ChatPanel.QHit + 1, () => win.ShowOptionsFor(m, (r.y1 + r.y2) * 0.5f), clip: clip); MenuDraw.Hit(row, "MessageHit", r, ChatPanel.QHit, () => OnRowClicked(win, m), clip: clip);
        }

        // ============================================================ 分侧参数（2026-10-25 · 账 300/301）
        // 🔴 **新增成员一律追加在类尾**（2026-10-25 纪律）：本文件被别的账/报告按行号引用得多，
        //    插进已有成员之间会把下游行号整体推移（同一天在 `Net/NetBattle.cs` 上踩过一次）。

        /// <summary>行底色分两侧（原版 `ChatMessageUI.Set` 里那句 `background.color = isPlayer ? A : B`）。
        /// <para>判据 = prefab `bundle_mainmenualwaysloaded_assets_all/MonoBehaviour/
        /// MonoBehaviour_-7156338170297815041.json` 实读：自己发 `(0.0431, 0.0941, 0.1333, 0.808)` ·
        /// 别人发 `(0.1059, 0.0039, 0.0039, 0.706)`。</para>
        /// <para>⚠️ 我们原来两侧都吃**预制体模板色** `(0.00392, 0.0143, 0.106, 0.706)` ——
        /// 那不是任何一侧的运行期色（账 300 的 P1）。</para></summary>
        public static Color RowBgColor(bool mine)
        {
            return mine ? new Color(0.0431f, 0.0941f, 0.1333f, 0.808f)
                        : new Color(0.1059f, 0.0039f, 0.0039f, 0.706f);
        }

        /// <summary>行左内边距（原版 `ChatMessageUI.SetLayout` 的两个 padding **按发信侧对调**：
        /// 自己发 = L25 / R60（名字在左、头像在右）；别人发 = L60 / R25）。
        /// ⚠️ 判据只此一处，⛔ 别在别处再写一份 25/60。</summary>
        public static float RowPadLeft(bool mine) { return mine ? PadX : PadX + PadSwap; }
        /// <summary>行右内边距（同上）。</summary>
        public static float RowPadRight(bool mine) { return mine ? PadX + PadSwap : PadX; }
        /// <summary>两个 padding 的差（60 − 25）。原版 `namePadding = 60` · `timePadding = 25`。</summary>
        public const float PadSwap = 35f;

        // ============================================================ 🆕 2026-10-27（C5 批 · 账 299）
        // 🔴 纪律：这一段**追加在类尾**（本文件被账按行号引用 ⇒ 只允许末尾追加）。
        static readonly HashSet<string> _saidOnce = new HashSet<string>();

        /// <summary>= 原版 `ChatMessageUI.OnMessageClicked()`（**逐句**，判据
        /// `out_cpp2il_dev_ilspy/ChatMessageUI.cs:280-315`）：① `type ∈ {Announcement(100), Message(50)}`
        /// ⇒ `TranslateMessage()`（走远端 `TranslationService`）；② `type == DeckShare(150)`
        /// ⇒ 取 `data` 的末行 → `CardDeck.DeserializeDeckString` → 开 `DeckInfoPopup`（**Import** 态）。
        /// <para>⚠️ **两处如实差异**：① 翻译走远端 ⇒ 本地**只出声一次、保持原文**
        /// （⛔ 不假装翻译过；`TranslationStatus` 三态字段留在 `SocialData.ChatMessage` 上给账 302 消费）；
        /// ② 卡组串用本仓的 `DeckLibrary.ImportString`（= 原版 `DeserializeDeckString` 的等价件，
        /// 与 `Shell/CollectionData.ImportDeck` 同一条链，⛔ 不另写一份解析）。</para></summary>
        static void OnRowClicked(ChatPanel win, SocialData.ChatMessage m)
        {
            if (win == null || m == null) return;
            if (m.Type == SocialData.ChatMessage.TypeMessage || m.Type == SocialData.ChatMessage.TypeAnnouncement)
            {
                if (_saidOnce.Add("translate"))
                    Debug.LogWarning("[Chat] 点消息行 = 原版 `TranslateMessage()` —— 它走**远端 `TranslationService`**，"
                                   + "本地没有对等物 ⇒ **不翻译、保持原文**（⛔ 不假装翻译过；三态字段 `TranslationStatus` 已留）。");
                return;
            }
            if (m.Type != SocialData.ChatMessage.TypeDeckShare)
            {
                if (_saidOnce.Add("type" + m.Type))
                    Debug.Log("[Chat] 点消息行：类型 " + m.Type + " 在原版 `OnMessageClicked` 里**没有分支**（只有 50/100/150 三档）⇒ 什么都不做。");
                return;
            }
            // ---- DeckShare：`data` 的**末行**才是卡组串（原版 `data.Split("\n")` → `Enumerable.Last`）
            string data = m.LocData;
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogWarning("[Chat] `DeckShare` 消息没有 `LocData`（原版 `data`）⇒ 打不开卡组详情（⛔ 不编一个空卡组）。");
                return;
            }
            int cut = data.LastIndexOf('\n');
            string deckString = cut >= 0 ? data.Substring(cut + 1) : data;
            var deck = RuleEngine.DeckLibrary.ImportString(deckString, CollectionData.Card);
            if (deck == null)
            {
                Debug.LogWarning("[Chat] `DeckShare` 的卡组串**解析不出来** ⇒ 打不开卡组详情（⛔ 不静默）。");
                return;
            }
            var mgr = win.Manager;
            if (mgr == null)
            {
                Debug.LogWarning("[Chat] 没有 `WindowsManager` ⇒ `DeckShare` 的卡组详情开不了。");
                return;
            }
            var info = new CollectionData.DeckInfo
            {
                Name = deck.Name ?? "",
                WarlordId = deck.WarlordId,
                Count = deck.CardIds != null ? deck.CardIds.Count : 0,
                CardbackId = deck.CardbackId,
                GameMode = deck.GameMode,
            };
            var hero = CollectionData.Card(deck.WarlordId);
            info.Faction = hero != null ? hero.Faction : "";
            // 原版 = `new DeckInfoContext(deck, Import, selectButton: null, checkOwnership, editButtonAction: null)`
            // ⇒ 我们的等价构造 = `CreateForPicked(…, State=Import, isPlayerDeck:false（别人的分享）, 无选卡组钮)`
            var popup = DeckInfoPopup.CreateForPicked(mgr, -1, deck, info,
                                                      DeckInfoPopup.DeckInfoState.Import, false, null, null);
            mgr.OpenWindow(popup);
            Debug.Log("[Chat] `DeckShare` ⇒ 开 `Deck info Popup`（**Import** 态）· 卡组「" + info.Name + "」"
                    + "（原版 `ChatMessageUI.OnMessageClicked` → `WindowsManager.OpenWindow<DeckInfoPopup>(context)`）");
        }
    }
}
