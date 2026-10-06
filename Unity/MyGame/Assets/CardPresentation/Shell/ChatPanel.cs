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
        public const int QBase = 3300;
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

        /// <summary>本窗的两个频道（`ChatRoom` 枚举的值 + 页签文案）。**这是我们挑的**（判据 ③）。</summary>
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
        /// · `autoMax` / `autoBase` **保留缺省**（`<= 0` ⇒ 旧行为）—— 本窗四处调用点原版**全是
        ///   `m_enableAutoSizing = 0`**，两格**不适用**，多开一个永远填不上的口才是留洞
        ///   （逐站判据见 `资料/普查产出_1012/F2_字号线收尾.md` §2·6 的 `ChatPanel` 表）。</para></summary>
        Label Text(Transform p, PxRect r, string s, Color c, string n, float px, int q, float autoMin,
                   bool alignLeft, float autoMax = 0f, float autoBase = 0f)
        {
            var lb = MenuDraw.TextBox(p, r, s, c, n, px, autoMin, q, autoMax, autoBase);
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
            //   （**现读** `Shell/ChatPanel.cs:696`；A477 立项时是 `:670`，行号已漂）
            //   （原版 `Left/Top`，我们两侧都没对齐 ⇒ 画成居中）—— **不在本批三条账上，只报不改**，
            //   真值、判据命令与改法写在 `资料/普查产出_1013/W403_A414_对齐与字号.md` §七·1。
            Text(input, InputR, "Type message", new Color(1f, 1f, 1f, 0.439f), "Placeholder", 28f, QText,
                 autoMin: 0f, alignLeft: true);
            MenuDraw.Hit(input, "InputHit", InputR, QHit, () => Debug.Log(
                "[Chat] 输入框**打不了字** —— 我们这套外壳没有文字输入系统；而且**聊天收发本身还没做**"
              + "（用户 2026-09-26 口径：网络聊天功能暂时不做，界面照建、数据留空态）。"));
            var send = Node(_enterText, "Button", SendBtnR);
            Rect(send, "40k_UI_Chat_send", SendBtnR, "Image", QBg, null, true);
            MenuDraw.Hit(send, "Hit", SendBtnR, QHit, () => Debug.Log(
                "[Chat] `TrySendMessage`：**没有服务器**，也没有 `ChatGlobalManager` 的对等物 ⇒ 发不出去。"));

            // 右上关闭钮（`ChatPanel.closeButton` 指的就是它）
            var close = Node(chat, "Generic Close Button Orange", CloseBtnR);
            // 🔴 换图落在**圆底那一层**（原版 `trans=2` 换的是它自己的 Image；三层结构 = 圆底 + 黄面 + 叉）
            var closeBase = Rect(close, "UI_Button_Round_background", CloseBtnR, "Background Round", QBg, null, true);
            Rect(close, "40k_general_bt_yellow", CloseIconR, "Background", QContent, null, true);
            Rect(close, "40k_general_bt_yellow_close", CloseIconR, "Icon", QContent + 1, null, true);
            // 🆕 A17：原版 `Chat>Holder>ChatPanel>Generic Close Button Orange` 是 SpriteSwap（实测 HL 见下）
            MenuDraw.Hit(close, "Hit", CloseBtnR, QHit, () => Close(), closeBase, null, "40k_general_bt_yellow_hover");

            // 玩家选项面板（出厂 act F；点消息行上的头像才亮）
            _options = Node(chat, "Player Options Panel", OptPanelR);
            Nine(_options, "40k_topmarquee_currency_display BW", OptPanelR, new Vector4(15f, 15f, 15f, 15f),
                 "Background", QContent, new Color(0.311f, 0.201f, 0.201f, 1f));
            // 🔴 **A406**：`Holder/Chat/Player Options Panel/Name` 原版
            //   `auto[10.0~**40.0**] 基准=**21.0**` ⇒ 上限 40（= 标称）· base **21.0**（同一条 dump）。
            Text(_options, OptNameR, "Fulanito Name", Color.white, "Name", 40f, QText, 10f, false, 40f, 21f);
            // `Buttons`（原版是个 `VerticalLayoutGroup`，5 个键排在它下面 —— 保留这一层，别把键挂到面板上）
            var optBtns = Node(_options, "Buttons", new PxRect(176.58f, 399f, 563.88f, 727f));
            string[] acts = { "Add as a friend", "Challenge", "Report message", "Block player", "Profile" };
            string[] nodes = { "Add as a friend", "Challenge", "Report", "Block", "Profile" };
            for (int i = 0; i < nodes.Length; i++)
            {
                float y = OptPanelR.y1 + 50f + i * OptRowStep;
                var rowR = new PxRect(OptPanelR.x1 + 15f, y, OptPanelR.x2 - 15f, y + OptRowH);
                var row = Node(optBtns, nodes[i], rowR);
                var rowNine = Nine(row, "UI_Button_Mulligan", rowR, new Vector4(333f, 96f, 333f, 96f), "Image", QContent);
                // 🔴 **A406**：`Holder/Chat/Player Options Panel/Buttons/{Add as a friend,Challenge,Report,
                //   Block,Profile}/Button Text` 五颗**逐颗读过** —— 全是 `auto[10.0~**30.0**] 基准=**12.0**`
                //   ⇒ 上限 30（= 标称）· base **12.0**。判据 = 同一条 `menu_dump … "ChatPanel" --depth 10 --md`。
                Text(row, new PxRect(rowR.x1 + 12.69f, rowR.y1 - 19.65f, rowR.x2 - 13.84f, rowR.y1 + 57.14f),
                     acts[i], Color.white, "Button Text", 30f, QText, 10f, false, 30f, 12f);
                string act = acts[i];
                // 🆕 A17：原版 `Chat>Player Options Panel>Buttons>*` 五颗都是 SpriteSwap（普查 §块 5 第 19 行）
                var oh = MenuDraw.Hit(row, "Hit", rowR, QHit, () => OnOption(act));
                var owb = oh != null ? oh.GetComponent<WindowButton>() : null;
                if (owb != null) owb.BindNine(rowNine, "UI_Button_Mulligan");
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

        /// <summary>`button_bg` 的色。🔴 **两份 prefab 的值不同**：act T 那份是 `(1, 0.5723677, 0, 1)`，
        /// **act F 那份（= 本窗 `tabButtonPrefab` 实际克隆的那份）是 `(1, 0.4274509847, 0, 1)`** —— 取后者
        /// （我们原来的 0.427 一直是对的，可反证）。⚠️ 两颗 Image 都带 `m_PixelsPerUnitMultiplier = 0.92`，
        /// 本工程 `ImageQuad` 没有这个属性（Simple 图上影响很小，如实记着）。</summary>
        static readonly Color TabBgTint = new Color(1f, 0.427f, 0f, 1f);

        /// <summary>一个频道键（底 `button_bg` + 图标 `40K_icon_menu_chat` + 文案）。
        /// ⚠️ 原版那个模板上挂的 `Label` **自己就是 inactive 的**（§A·1 第 67 行），
        /// 所以它的文案 `General` 是**设置窗的 term 残留**（`Settings/General/Title`）—— 我们**不用它**，
        /// 用频道的名字（`Global` / `Alliance`），并在这里说明这是我们的选择。</summary>
        Transform TabButton(Transform parent, int idx, string label, float y1, float y2)
        {
            var r = new PxRect(TabBtnColR.x2 - TabBtnSz.x, y1, TabBtnColR.x2, y2);
            var b = Node(parent, "Orange Tab Toggle " + idx, r);
            // `button_bg`：**两态都在**（原版换的是这张 Image 的 `sprite`）—— 出厂一律画 `offSprite`，
            // 选中那一档由 `RefreshHighlights` 换图（原版 `EverguildToggle.ToggleSprite` 就是那一刻跑的）。
            _tabBg[idx] = Rect(b, TabArtOff, r, "button_bg", QBg, TabBgTint);
            var iconR = new PxRect(r.x1, r.y1 + 17.34f, r.x2 - 5f, r.y2 - 15.01f);
            Rect(b, "40K_icon_menu_chat", iconR, "Icon", QContent, null, true);
            // 🔴 **A406**：`Orange Tab Toggle/Label/Tab Toggle Title` 原版
            //   `auto[10.0~**35.0**] 基准=**35.0**` ⇒ 上限 35（= 标称）· base **35.0**。
            //   ⚠️ **两份实例逐字段读过、同值**（act T 那份 RT `-8278251871369237089` 与
            //   本窗 `tabButtonPrefab` 实际克隆的 act F 那份 RT `2421717888838184498`）——
            //   「只看 ChatPanel 树里那一份」在这里恰好也对，但是**读过才知道**。
            Text(b, new PxRect(r.x1 + 5f, r.y1 + 106.04f, r.x2 - 5f, r.y1 + 146.04f), label, Color.white,
                 "Label", 35f, QText, 10f, false, 35f, 35f);
            // 🔴 A17 的两档**逐颗显式给**（`40K_settings_button` 那条后备规则推出的是
            //    `40K_settings_button_hover`，而原版这一颗的悬停图是 `…_selected`）——
            //    不显式给就是「悬停换了张错的图」+ `MissingSwapArt` 记一条。
            var hit = MenuDraw.Hit(b, "Hit", r, QHit, () => tabButtons.Click(idx),
                                   _tabBg[idx], TabArtOff, TabArtHover, TabArtPressed);
            _tabBtn[idx] = hit != null ? hit.GetComponent<WindowButton>() : null;
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
            Debug.Log("[Chat] `" + act + "`：要**服务器**（原版 `ChatPlayerOptionsPanel." + act.Replace(" ", "") + "`）。");
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
    }

    // ==================================================================
    //  `ChatTab` —— 一个频道页（原版 `ChatTab` + `ChatContentView`）
    // ==================================================================

    /// <summary>原版 `ChatTab`（`content` 指向 `ChatContentView`）。`ChatContentView` 是 **OSA 虚拟列表**
    /// （`OSA&lt;BaseParamsWithPrefab, ChatEntryView&gt;`）—— 我们没有 OSA ⇒ 用**逐行建 + MenuScroll** 那条路
    /// （同 `BattleLogTab` 的做法）。参数照抄原档：`_ContentPadding = (25,25,5,5)` · `_ContentSpacing = 10` ·
    /// `_DefaultItemSize = 60`（判据 → 普查 §A·2 第 112–117 行）。</summary>
    public class ChatTab : WindowTabBase
    {
        public override WindowTabType Type
        { get { return _channel == "Global" ? WindowTabType.ChatGlobal : WindowTabType.ChatAlliance; } }

        ChatPanel _win;
        Transform _root, _content;
        PxRect _rect;
        string _channel;
        public int BuiltRows { get; private set; }
        public string Channel { get { return _channel; } }

        const float PadL = 25f, PadR = 25f, PadT = 5f, PadB = 5f, Spacing = 10f, DefaultRowH = 60f;

        /// <summary>🔴 **原版 `Chat Tab/Viewport` 的 `RectMask2D` 软边 = `(0, 22)`**（画布像素：
        /// `x` 管左右两条边、`y` 管上下两条边；`(0,0)` = 硬边 = 本页原来那套行为）。
        /// 判据 = **逐字实读原档**：`assets_full/bundle_mainmenualwaysloaded_assets_all/MonoBehaviour/
        /// MonoBehaviour_-7904774033703794794.json` —— 那个 `RectMask2D` 挂的 GO 正是 `Chat Tab/Viewport`
        /// 的 RT（`m_Softness = {x:0, y:22}` · `m_Padding = (0,0,0,0)` · `m_Enabled = 1`；
        /// 整包 1 条，全量表 → `d:/4/_tmp_view/q1_rm2d.txt` 的 `bundle_mainmenualwaysloaded_assets_all` 那一节）。
        /// 纵向滚动区 ⇒ **上下各 22px 渐隐、左右硬边**。
        /// <para>🔴 **为什么是「逐件传下去」而不是像别的窗那样设一个 `ClipSoftness`**（A78①）：
        ///   本窗是 `ChatPanel : GameWindowWithTabs : GameWindow : MonoBehaviour`，**不是 `MenuWindowBase` 的子类**
        ///   ⇒ 那三兄弟（`Clip` / `ClipSoftness` / `ClipPad`）**当年一个都够不着**
        ///   （🔴 **2026-10-10 订正（A194）**：三兄弟**已上移到 `GameWindow`**（A78② 落地）⇒ **今天够得着了**；
        ///   ⚠️ **本窗仍走「逐件传」是 A78① 的裁定、不是回归** —— ⛔ 别看到「够得着」就去改成设 `ClipSoftness`。
        ///   原文：「它们只长在 `MenuWindowBase` 上」（按**名字**找那三个字段 —— ⛔ 别写行号，它会过期））。而 `MenuDraw.Rect/Nine/ClipText` 本来就收 `clipSoftness`
        ///   ⇒ 这里照本页既有的做法（`clip` 也是逐件传的，见 `Setup`/`Rebuild`）把软边一起传下去 ——
        ///   机制**只有 `MenuDraw` 那一份**，本页不新写第二份。
        ///   ⚠️ 全量表里 `GameWindow` 族（`bundle_generalgamewindows_assets_all` 那 5 个 `RectMask2D`）
        ///   **软边全是 `(0,0)`** ⇒ **今天只有本窗需要这条路**；将来真要给那一族加，
        ///   该做的是把「当前 clip/软边」挪到 `GameWindow` 那一层，⛔ 不是每扇窗各抄一段。</para></summary>
        static readonly Vector2 VpSoft = new Vector2(0f, 22f);

        /// <summary>🆕 **2026-10-05（A38 顺手发现①）**：这一页的**纵向滚动区**（全壳唯一一份滚动实现
        /// = `MenuScroll`）。补之前它**连滚动区都没有** ⇒ 消息行既不滚也不裁，**超一屏就画到框外**、
        /// 第一屏之外的行永远看不到也点不到（`grep MenuScroll Shell/ChatPanel.cs` 那时零命中）。
        /// 判据与对齐对象 → `Setup` 里那段注释。</summary>
        MenuScroll _scroll;

        /// <summary>自检用：批处理里没有滚轮事件 ⇒ 直调 `MenuScroll.Wheel/SetOffset`（**和真滚同一条路**）。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        /// <summary>内容底边（画布 px · 屏幕坐标 · 偏移 0 时）—— 就是写进滚动区 `ContentX2` 的那个值。</summary>
        public float ContentBottom { get; private set; }

        public void SetHost(ChatPanel win, Transform root, PxRect rect, string channel)
        { _win = win; _root = root; _rect = rect; _channel = channel; }

        public override void Setup()
        {
            // `Viewport`（`RectMask2D`，softness (0,22)）+ `Content`
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：这颗 `Viewport` 就是**裁切状态的载体**
            //    （= 原版 `Chat Tab/Viewport` 身上那个 `RectMask2D`）—— 参数取原档逐字实读的
            //    `m_Padding = (0,0,0,0)` · `m_Softness = (0,22)`（见上面 `VpSoft` 那段）
            //    ⇒ 与迁移前「`SetClip`/逐件传 `vpR` + `VpSoft`」逐位同值。
            var vp = ViewportClip.Hang(_root, "Viewport", _rect, Vector4.zero,
                                       new Vector2Int((int)VpSoft.x, (int)VpSoft.y)).transform;

            // 🆕 **2026-10-05（A38 顺手发现①）：把滚动区补上** —— 做法**照 `Shell/BattleLogPopup.cs`**
            //   （同一族：全壳只有一份滚动实现 `MenuScroll`；`BattleLogPopup` / 排行榜 / 对局历史那批
            //   都是这么接的），逐项对齐：`Owner` / `OnChanged` / `ContentX2` / 逐行 `Shift` + `ClipRect`
            //   一个都不少。⛔ **别在这里另写一份滚动逻辑**（CLAUDE.md §三）。
            // 🔴 **原版这一格不是 uGUI `ScrollRect`** —— ⛔ 别照抄 `BattleLogPopup` 那组 `Clamped/1.0` 的数
            //   （铁律 5·c：一个值 ≠ 全部情况）。实读原档：`Chat Tab` 那个 GO 上**没有 `ScrollRect`**
            //   （`bundle_mainmenualwaysloaded_assets_all/GameObject/Chat Tab.json` 的 5 个组件 =
            //    RT + `Image`(α0) + **`ChatContentView`** + `ChatTab`，另两处 GraphicRaycaster；
            //    同一棵树里 `Viewport` 挂的是 `RectMask2D`）。它是一棵 **TheFallenGames OSA 虚拟列表**：
            //   `class ChatContentView : OSA<BaseParamsWithPrefab, ChatEntryView>`（`Warpforge_code` 签名桩
            //   `ChatContentView.cs:8`），滚动参数全在组件 `MonoBehaviour_-8786325734254409834.json` 的
            //   `_Params` 里，**逐字段实读**：
            //     `_Content` = `Viewport/Content`(RT `-7815640432147003498`) · `_Viewport` = `Viewport`(RT `870065029823367062`)
            //     · `_Orientation = 0`(竖) · `_ContentPadding = (L25, R25, T5, B5)` · `_ContentSpacing = 10`
            //     · `_DefaultItemSize = 60` · `_ScrollSensivity = 20` · `_ScrollSensivityOnXAxis = 100` ·
            //       `_DragEnabled = 1` · `_ScrollEnabled = 1` · `_UseUnscaledTime = 1` ·
            //       `_ItemTransversalSize = 0`（行宽取 viewport 宽）· `_Scrollbar = null` ·
            //       `_Effects._ElasticMovement = 1` · `_PullElasticity = 0.3` · `_ReleaseTime = 0.1` ·
            //       `_Inertia = 1` · `_InertiaDecelerationRate = 0.865` · `_CutMovementOnPointerDown = 1` ·
            //       `_MaxSpeed = 10000`（普查 §A·2 第 112–117 行逐值相同）
            //   ⇒ **能落到 `MenuScroll` 上的只有下面这几条**：纵向 · 上对齐 · pad(25,25,5,5) ·
            //     spacing 10 · itemSize 60。**OSA 那套弹性/惯性/灵敏度是另一个机制**（它自己的 `_Effects` +
            //     `_PullElasticity` + 自绘滚动），而全壳的滚动只有一份实现（CLAUDE.md §三）⇒ 手感仍走
            //     `MenuScroll` 的 Clamped + 全壳滚轮系数（`NotchK`）。
            //     ⚠️ **如实标**：这不是「照原版抄了全部参数」，是「机制不同，取能对上的那几项」。
            //   ⚠️ `_Gravity = 3` 对应哪一档**没查到**（OSA 的 `Gravity` 枚举本地没解出来，普查 §E 已记）——
            //     它不影响上面那几项（纵向 + 上对齐）。
            //   ✅ **2026-10-05（A78①）：软边（`m_Softness = (0,22)`）已接** —— 见 `VpSoft` 那段注释
            //     （机制在 `MenuDraw.ApplySoftEdges`，本页把 `VpSoft` 逐件传给 `MenuDraw`）。
            //     🔴 **本页原来那句「⛔ 别顺手在这儿接」已作废**，它当时的顾虑是「重复重切把 uv 缩掉」
            //     —— 那条**潜伏缺陷已经修掉**（`ApplySoftEdges` 的 `uv0` 改成显式入参，A52/F1，
            //     断言见 `Editor/ShellScene.cs` ⑤·c）。⛔ 别再照旧注释把这条接线推回去。
            PointerLayer.UnregisterOwnedBy(_root != null ? _root.gameObject : gameObject);   // 重开一次窗 ⇒ 旧的那份是死条目
            _scroll = MenuScroll.TopAligned(_rect, 0f);   // 内容高在 `Rebuild` 里按条数写（原版 OSA 那份 `_Content` 的高）
            _scroll.Owner = _root.gameObject;             // 这一页不显示时指针层跳过它（切页走 `SetActive`）
            _scroll.OnChanged = Rebuild;                  // 🔴 滚轮只改 `Offset`、**画是调用方的事**：不接 = 滚了什么都不动
            // ⚠️ 走 `SocialPage.RegisterScroll` 那一份（**会出声**）——`PointerLayer.RegisterScroll` 在
            //   指针层还不在场景里时是 `return`（登记表都没建）⇒ 滚轮永远落不上来，而画面看着完全正常；
            //   `SocialPage.RegisterScroll`（`SocialWindow.cs:319-329`）先判这一条、并顺带判 `Owner` 空不空。
            //   （同 `BattleLogPopup.cs:156` 那一处的选择；日志前缀是 `[Social]`，那是那个公共件的既有文案。）
            SocialPage.RegisterScroll(_scroll);

            // ⚠️ `Content` 摆在**视口左上、零高**（原版那个 RT 是全拉伸的；行位置由每行自己的绝对矩形给）
            //    —— 与 `BattleLogPopup` 逐字同形。⛔ 建完行之后**别再挪它**（子件是按「父节点当时的位置」
            //    换算 `localPosition` 的，挪一下整排都偏）。
            _content = MenuDraw.Node(vp, "Content", new PxRect(_rect.x1, _rect.y1, _rect.x2, _rect.y1));
            Rebuild();
        }

        public override void OnOpen() { Rebuild(); }

        /// <summary>按消息条数逐行建（原版 OSA 池化生成 `ChatEntryView`）。本地没服务器 ⇒ 恒 0 条。
        /// 🆕 2026-10-05：行按**滚动偏移之后**的位置摆（`MenuScroll.Shift`），整行滚出视口的**不建**。</summary>
        public void Rebuild()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(_content.GetChild(i).gameObject);
            BuiltRows = 0;
            var all = SocialData.ChatMessages;

            // 🔴 **内容高 = 原版 OSA 的算法**：`padTop + Σ(itemSize) + spacing×(n−1) + padBottom`
            //   （`_ContentPadding = (25,25,5,5)` · `_ContentSpacing = 10` · 行高给了就用行高、
            //    否则 `_DefaultItemSize = 60`）。
            //   必须写进滚动区：`MenuScroll` 的 `ContentX1/X2` 就是内容两端，不写 ⇒ `ContentX1 == ContentX2`
            //   ⇒ `ClampHi == 0` ⇒ **这一页一格都滚不动**，而「整行滚出视口 ⇒ 不建」那道守卫会把第二屏起
            //   **彻底藏掉**（不是「画到框外至少看得见」）—— 同 `BattleLogPopup.BuildRows` 那条注释。
            //   ⚠️ **空表那一支也要写**（写成视口顶）—— 否则上一次的内容高留在区里，是个静默的脏值
            //   （同 `LeaderboardWindow` / `BattleLogPopup` 那两处）。
            float y = _rect.y1 + PadT;
            int n = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Channel != _channel) continue;
                y += (all[i].Height > 0f ? all[i].Height : DefaultRowH) + Spacing;
                n++;
            }
            ContentBottom = n == 0 ? _rect.y1 : y - Spacing + PadB;
            if (_scroll != null) _scroll.ContentX2 = ContentBottom;

            y = _rect.y1 + PadT;
            float w = _rect.W - PadL - PadR;
            var vpR = _scroll != null ? _scroll.Viewport : _rect;   // 滚动区就是唯一那份；没有才退回本页矩形
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Channel != _channel) continue;
                float h = all[i].Height > 0f ? all[i].Height : DefaultRowH;
                var rr = new PxRect(_rect.x1 + PadL, y, _rect.x1 + PadL + w, y + h);
                if (_scroll != null) rr = _scroll.Shift(rr);   // 内容坐标 → 屏幕坐标（**只做偏移、不裁**）
                y += h + Spacing;                              // ⚠️ 累加**必须用内容坐标**，别用 Shift 之后的
                // 整行滚出视口 ⇒ **连节点一起不建**（与档案窗那一页 `BattleLogTab.cs` / 弹窗那份
                // `BattleLogPopup.BuildRows` 同形）。求交那一份 = `MenuDraw.Visible`（**全工程唯一一份**求交；
                // `ClipRect` 是它「顺带夹出可见矩形」的那版）。⚠️ 2026-10-07 更正（铁律 5 / A12①）：原文写
                // 「= `MenuDraw.ClipRect`（全工程唯一一份）」—— 收口后那两句是**同一份**。
                if (!MenuDraw.ClipRect(rr, vpR, out _)) continue;
                // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：`clip` 传 **`null`** —— 裁切状态已经迁到
                //    `Setup()` 建的那颗 `Viewport` 节点上（`ViewportClip`），`MenuDraw.*` 会沿父链解析到它。
                //    ⚠️ 上面那道 `ClipRect(rr, vpR, …)` 是**另一件事**（「整行滚出视口 ⇒ 连节点一起不建」的
                //    粗筛，用的是滚动区自己的矩形），**照旧传 `vpR`**。
                //    ⚠️ `VpSoft` 照旧传（它现在是**回落那一档**的软边；节点在时由节点说了算）。
                ChatMessageRow.Build(_win, _content, all[i], rr, null, VpSoft);
                BuiltRows++;                     // 现在 = **真建出来几行**（滚出视口的不算；断言用）
            }
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
                          Vector2 clipSoft, bool alignLeft = false, bool alignRight = false)
        {
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：改走 **`VisibleAbove`**（节点态那一版）——
            //   调用点（`ChatTab.Rebuild`）现在传的 `clip` 是 `null`（裁切状态在视口节点上）
            //   ⇒ 沿用裸 `Visible` 的话这里**永远放行**（静默：压在视口外的整行文字照建）。
            if (!MenuDraw.VisibleAbove(p, r, clip)) return null;
            // 🔴 **2026-10-12（A406）：本口【故意不加】`autoMaxPx` / `autoBasePx`** ——
            //   它的四个调用点全是 `ChatMessageRow`（独立根 `bundle_mainmenualwaysloaded_assets_all/GameObject/ChatMessageRow.json`）
            //   的件，而那一族**原版 `m_enableAutoSizing = 0`**（判据：
            //   `python 工具/menu_dump.py bundle_mainmenualwaysloaded_assets_all "ChatMessageRow" --depth 8 --md`
            //   ⇒ `Player Header/Sender` fs=18 基准=18 · `Time` 同 · `Message` fs=22 基准=22，**都没有 `auto[…]` 段**）
            //   ⇒ 上限/base **两格对本站不适用**；本口也固定传 `autoMinPx = 0f` ⇒ **与原来逐位等价**，
            //   **不需要**多开两个形参（多开=留一个永远不会被填的口）。
            var lb = MenuDraw.TextBox(p, r, s, col, n, px, 0f, q);
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

            // `RowBackground`：`a=(0,0)-(1,1)` 全拉伸 · `WF_9Sliced` 九宫 (62,62,62,62) · 色 (0.00392,0.0143,0.106,0.706)
            var bgTex = win.Art("WF_9Sliced");
            if (bgTex != null)
                MenuDraw.Nine(row, bgTex, r, new Vector4(62f, 62f, 62f, 62f), bgTex.width, bgTex.height,
                              ChatPanel.QContent, new Color(0.00392f, 0.0143f, 0.106f, 0.706f), true, "RowBackground",
                              clip: clip, clipSoftness: clipSoft);

            // 头部：自己发的那套（`Player Header`）或别人的那套（`Friend Header`）
            var headR = new PxRect(r.x1 + PadX, r.y1 + HeadTop, r.x2 - PadX, r.y1 + HeadTop + HeadH);
            var head = MenuDraw.Node(row, m.Mine ? "Player Header" : "Friend Header", headR);

            // `Sender`（绿）与 `Time`（灰）铺在同一个矩形上：**一份左对齐、一份右对齐**
            // —— 自己发 ⇒ 名字靠右、时间靠左；别人发 ⇒ 反过来（两套头是镜像的）。
            var sR = new PxRect(headR.x1, headR.y1, headR.x2, headR.y1 + 24.38f);
            Text(head, sR, m.Sender ?? "", new Color(0.337f, 0.843f, 0.4f, 1f), "Sender", 18f, ChatPanel.QHead, clip,
                 clipSoft, alignLeft: !m.Mine, alignRight: m.Mine);
            Text(head, sR, m.Time ?? "", new Color(0.84f, 0.84f, 0.84f, 1f), "Time", 18f, ChatPanel.QHead, clip,
                 clipSoft, alignLeft: m.Mine, alignRight: !m.Mine);

            // 头像框（`Profile border` + 里面的立绘）：自己发贴**右沿**（+45）、别人发贴**左沿**（−45），
            // 顶边都从头部顶边往上 15（`p=(0.5,1) pos.y=15`）。
            float pcx = m.Mine ? (r.x2 - PadX + PBdx) : (r.x1 + PadX - PBdx);
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
            Text(row, new PxRect(r.x1 + PadX, r.y1 + MsgTop, r.x2 - PadX, r.y1 + MsgTop + 30f),
                 m.Text ?? "", Color.white, "Message", 22f, ChatPanel.QText, clip, clipSoft);

            // 点头像 ⇒ 开玩家选项面板（原版 `ChatMessageUI.OnMessageClicked` / `ChatPlayerOptionsPanel`）
            // ⚠️ `clip` 也传下去（判据 = 原版 `RectMask2D` 的**射线那一面**：框外的点判不中任何东西）
            //    ⇒ 滚出视口的行**点不到**、压在视口边上的命中区**截到视口内**。
            MenuDraw.Hit(pb, "Hit", pbr, ChatPanel.QHit, () =>
            {
                Debug.Log("[Chat] 点头像 ⇒ 原版开 `ChatPlayerOptionsPanel`（5 个钮全要服务器）。");
                var panel = win.transform.Find("Holder/Chat/Player Options Panel");
                if (panel != null) panel.gameObject.SetActive(!panel.gameObject.activeSelf);
            }, clip: clip);
        }
    }
}
