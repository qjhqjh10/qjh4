// DuelPopupWindow.cs — 多人界面那一批 第 4 件之三：**好友挑战弹窗**
//   （原版节点名 `MessagePopupWindowDuel`，**类名 `DuelPopupWindow`**，`GameWindow`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/聊天窗与挑战弹窗.md` §B（层 × 参数表，15 个 RT）· §B·1（`MessageText` 全文）·
// §C·1（带 Button 的节点 = 要接的交互点）· §D·2/§D·3（谁写文案 / 谁开它）；
// 入口链路 → `资料/普查产出_0927/多人界面_入口与调用.md` §③「好友挑战」那一条。
//
// ---- 🔴 五条判据（读原始 JSON 定的）----
// ① **窗口属性**：`type = 1`(**Popup**) · **`windowsPlacement = 15`(Popup)** · `extraScaleSmallScreen = 1.15` ·
//    `closeOnESC = 1` · `updateNavPanel = 0`。`DuelPopupWindow : GameWindow`（**不是** `GameWindowWithTabs`）。
//    ⚠️ **`extraScaleSmallScreen = 1.15`** —— 逐窗实测，别拿练习窗那个 1.075 或公用的 1.0。
// ② **三个钮挂到哪**（`OnEnable` 里挂，普查 §D）：`skirmishButton` → `OnSkirmishButtonPressed` ·
//    `classicButton` → `OnClassicButtonPressed` · `closeButton` / `closeButtonBackground` → `OnCloseButtonPressed`(= `Close()`)。
// ③ 🔴 **`Button Classic` 的文案是 `Continue`**（节点名叫 `Classic`）—— 原档就这么写，**别「改对」**。
// ④ **`MessageText` 的全文**（原档没被截断，是索引截的）：
//      `Do you want to challenge <b><color=#FCDEBB>Everrookie2</b></color>?`
//    字号 **52.5**、`m_enableAutoSizing = 0`（min18/max72 是**死值**）；⚠️ **闭合标签顺序是反的**
//    （`</b></color>`），原档如此、TMP 容错 —— 照抄。
//    运行期走 `I2.Loc` 的 term **`MainMenu/Chat/ChallengeConfirmation`** + `String.Format(译文, PlayerName)`
//    ⇒ **译文里带 `{0}`**。那句中译文**本地没有**（词条在远端 CCD）⇒ 我们用英文那句当模板，
//    把 `Everrookie2` 换成真的名字 —— **这是我们的选择**，在下面标着。
// ⑤ **`Button Skirmish/Skirmish image` 溢出了按钮框**（按钮高 76、图标 100）—— **原档就是这样**，
//    别把它「夹」回框里。
//
// ---- 三个钮各自要什么 ----
// `Skirmish` / `Classic` 都是**开一局好友对决**（原版 `ChallengeManager` → 各自那个 `IPlayEvent`）——
// 要**对手在线 + 服务器** ⇒ 我们**出声**，不静默。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `DuelPopupWindow`（`MessagePopupWindowDuel`）。</summary>
    public class DuelPopupWindow : GameWindow
    {
        // 队列档：**弹窗必须比页高一档**（`RewardsScene` 有一条断言钉着「弹窗 > 页 > 窗」）——
        // 本窗 3400 段，比社交窗的页（3200+）与聊天窗（3300+）都高。
        public const int QBase = 3400;
        // ⚠️ `public`（2026-10-04 A47 接线批）：自检宿主要拿这两个档核「压暗命中区档 = 压暗层那一档
        //    且严格 < 本窗内容命中区最低档」这条不变量（`MenuDraw.ShadeRuleOk`）。
        // 🔴 **2026-10-08（A220）收窄**：脚本逐常量扫全工程 `.cs` 的**外部代码引用**（限定名
        //    `DuelPopupWindow.<档>`，已剔注释）—— `QPanel` **1 处** · `QHit` **2 处**
        //    （都在 `Editor/MainMenuScene.cs`「好友挑战弹窗」那一段：`MenuDraw.CheckShadeRule` 一条 +
        //    `CheckAbsorbRule` 一条）⇒ 这两个留 `public`；同批一起放宽的 `QBg`/`QContent`/`QText`
        //    **外部 0 处** ⇒ 回 `const`（只有本类自用）。整段理由见 `Shell/BoosterPackOpenWindow.cs` 里那段常量收窄的理由注。
        public const int QPanel = QBase;     // 3400 压暗层（`Menu Dark Background`）自己那一档
        public const int QHit = QBase + 5;   // 3405 窗内命中区的档（关窗钮 + 两颗模式钮；**严格 > `QPanel`**）
        const int QBg = QBase + 1;           // 3401 窗底九宫（`40k_popup`）与钮的底
        const int QContent = QBase + 2;      // 3402 窗内那几件（`Background fill` 平铺 · 钮上的图标）
        const int QText = QBase + 3;         // 3403 文案（`MessageText`、钮上的字）

        public static DuelPopupWindow LastOpened { get; private set; }

        // ---- 真值（§B 那张表，绝对画布像素）----
        // 🔴 **2026-10-05 重取（A88）**：`工具/menu_dump.py` 的 `_child_sizes` 补成**完整 uGUI**之后
        //   （`flexible` + 主轴 `childSize` + `offsetInCell`，判据 = 本地 uGUI 源码
        //   `HorizontalOrVerticalLayoutGroup.cs:186-216`），下面 `Buttons` 那六颗的「跑后值」变了 ——
        //   组内两格各从 350 撑到 **387.70**（`fmul = (775.40−700)/2 = 37.70`），`align=4`(MiddleCenter)
        //   再给每颗 **+18.85** 的 `offsetInCell`（`= (387.70−350)×0.5`）⇒ **组左沿 572.30 + 18.85 = 591.15**。
        //   重取命令：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "MessagePopupWindowDuel" --depth 12 --md`
        //   （⚠️ 旧值 610.00… 是 W1/W2 那两版的输出，**已作废**）。
        static readonly PxRect DarkBgR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly Color DarkBgTint = new Color(0f, 0f, 0f, 0.773f);
        static readonly PxRect WindowR = new PxRect(535f, 245f, 1385f, 675f);
        static readonly PxRect MaskR = new PxRect(545.40f, 254.44f, 1375.13f, 665.20f);
        static readonly PxRect MsgR = new PxRect(575.00f, 363.08f, 1345.00f, 489.81f);
        static readonly PxRect BtnsR = new PxRect(572.30f, 560.00f, 1347.70f, 650.00f);
        static readonly PxRect SkirmishR = new PxRect(591.15f, 567.00f, 941.15f, 643.00f);
        static readonly PxRect SkirmishTxR = new PxRect(693.54f, 573.49f, 928.15f, 636.51f);
        static readonly PxRect SkirmishIcR = new PxRect(599.80f, 553.20f, 699.80f, 653.20f);
        static readonly PxRect ClassicR = new PxRect(978.85f, 567.00f, 1328.85f, 643.00f);
        static readonly PxRect ClassicTxR = new PxRect(1086.95f, 573.49f, 1315.85f, 636.51f);
        static readonly PxRect ClassicIcR = new PxRect(994.50f, 554.10f, 1094.50f, 654.10f);
        static readonly PxRect CloseR = new PxRect(1341.80f, 212.10f, 1416.80f, 287.10f);
        static readonly PxRect CloseIcR = new PxRect(1351.12f, 222.35f, 1407.48f, 276.85f);
        /// <summary>两个钮的底图染色：**(0.369,0.894,0.587,1)**（绿）。</summary>
        static readonly Color BtnTint = new Color(0.369f, 0.894f, 0.587f, 1f);
        static readonly Vector4 BtnBorder = new Vector4(234f, 46f, 234f, 46f);

        /// <summary>原档那句的模板（`{0}` = 对手名）。⚠️ 闭合标签顺序照原档，别「修」。</summary>
        public const string MessageFormat = "Do you want to challenge <b><color=#FCDEBB>{0}</b></color>?";

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        public string OpponentName { get; private set; }
        public Label MessageLabel { get; private set; }
        public Transform SkirmishHit { get; private set; }
        public Transform ClassicHit { get; private set; }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }
        static Transform Node(Transform p, string n, PxRect r) { return MenuDraw.Node(p, n, r); }
        ImageQuad Rect(Transform p, string art, PxRect r, string n, int q, Color? tint = null, bool keepAspect = false)
        { return MenuDraw.Rect(p, art == null ? CardArt.Solid() : Art(art), r, n, q, tint, keepAspect); }
        GameObject Nine(Transform p, string art, PxRect r, Vector4 b, string n, int q, Color? tint = null)
        { var t = Art(art); return t == null ? null : MenuDraw.Nine(p, t, r, b, t.width, t.height, q, tint, true, n); }

        // ---------------------------------------------------------- 建

        /// <summary>`opponent` = 被挑战者的名字（原版 `playerDuelData.PlayerName`）。</summary>
        public static DuelPopupWindow Create(WindowsManager mgr, string opponent)
        {
            var go = new GameObject("MessagePopupWindowDuel");
            var win = go.AddComponent<DuelPopupWindow>();
            win.type = WindowType.Popup;                       // 实证 type = 1
            win.placement = WindowsPlacement.Popup;            // 实证 windowsPlacement = 15
            win.closeOnEsc = true;                             // 实证 closeOnESC = 1
            win.extraScaleSmallScreen = 1.15f;                 // 实证 1.15（逐窗不同！）
            win.Manager = mgr;
            win.OpponentName = opponent;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();

            // `Menu Dark Background`（纯色 (0,0,0,0.773) + 点外关闭 —— 原版是 `BackgroundCloseButton`，没有 Button 组件）
            var dark = Node(transform, "Menu Dark Background", DarkBgR);
            Rect(dark, null, DarkBgR, "Image", QPanel, DarkBgTint);
            // 🔴 **2026-10-04（A47 接线批）**：收口到公共件 `MenuDraw.ShadeHit`（档 = 压暗层自己那一档
            //   `QPanel` = 3400 < 内容命中区最低档 `QHit` = 3405）。原编码本来就合规矩。
            MenuDraw.ShadeHit(dark, DarkBgR, QPanel, QHit, () => Close(), "CloseHit");

            var win = Node(transform, "Window", WindowR);
            var bg = Node(win, "Generic Popup Background", WindowR);
            Nine(bg, "40k_popup", WindowR, new Vector4(169f, 160f, 169f, 160f), "Image", QBg);
            // 🆕 **2026-10-06（A94）：弹窗面板底图吸收点击**。判据 = 原版 prefab
            //   `MessagePopupWindowDuel > Window > Generic Popup Background` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版**什么都不做**。
            MenuDraw.Absorb(transform, "AbsorbHit", WindowR, QPanel, QHit);
            // `Mask`（`m_ShowMaskGraphic = 0`）+ `Background fill`（`40k_popup_texture`，原版 **Tiled**）
            var mask = Node(bg, "Mask", MaskR);
            var fillTex = Art("40k_popup_texture");
            if (fillTex != null)
            {
                // 原版 `ppuMul = 2.0`；贴图 128×128（`Sprite/40k_popup_texture.json`：`m_PixelsToUnits = 100`）
                // ⇒ 一格 = **128 ÷ 2 = 64 画布像素**（UGUI `tileWidth = m_Rect.width ÷ (ppu ÷ refPPU × ppuMul)`，
                //    refPPU 默认 100 ⇒ 除数就是 ppuMul）。与另外 5 处同一张图的 `FillTilePx = 64` 同值。
                // 🔴 **2026-10-03 订正**：这里原来传 `69.12f`，注释算的是「128/(100×2) = 0.64 世界单位 ×108」
                //    —— 那 0.64 是把 UGUI 的 `pixelsPerUnit`（= ppu ÷ refPPU = 100/100 = **1**）当成了 100
                //    （1 画布单位 = 1 设计像素，不该再折一道）⇒ 偏大 8%；而 `ImageQuad.PixelsPerUnit`
                //    当时也错写成 100 ⇒ 再偏大 8%。两个 8% **叠**在一起，渲出来是 **74.65 px**（= 64 × 1.08²）。
                //    现在两处都归位。⚠️ 这条与 `ImageQuad.PixelsPerUnit` 的改动是**同一次**：
                //    只改常量、留着 69.12 ⇒ 这一格**仍偏大 8%**（判据值是 64）。
                MenuDraw.Tiled(mask, fillTex, MaskR, 64f, QContent, "Background fill");
            }

            // `MessageText`：52.5px · Center/Middle · 折行 1（min18/max72 是死值 ⇒ 不传 autoMin）
            MessageLabel = MenuDraw.TextBox(win, MsgR, string.Format(MessageFormat, OpponentName ?? ""),
                                            Color.white, "MessageText", 52.5f, 0f, QText);

            var btns = Node(win, "Buttons", BtnsR);
            DuelButton(btns, "Button Skirmish", "Skirmish image", "40k_gamemode_icon_skirmish",
                       SkirmishR, SkirmishTxR, SkirmishIcR, "Skirmish", () => OnMode("Skirmish"));
            SkirmishHit = btns.Find("Button Skirmish/Hit");
            DuelButton(btns, "Button Classic", "Classic Image", "40k_gamemode_icon_classic",
                       ClassicR, ClassicTxR, ClassicIcR, "Continue", () => OnMode("Classic"));
            ClassicHit = btns.Find("Button Classic/Hit");

            // 右上那颗绿圆钮 = **关闭钮**（节点名没写 Close，图标是关闭 ⇒ 普查 §B 判为 closeButton）
            var close = Node(win, "Generic Rounded Button Green", CloseR);
            Rect(close, "UI_Button_Round_background", CloseR, "Image", QBg, null, true);
            Rect(close, "40k_bt_close", CloseIcR, "Icon", QContent, null, true);
            MenuDraw.Hit(close, "Hit", CloseR, QHit, () => Close());

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Duel] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>一个模式钮：底 `40K_button`（绿）+ 文案 + **溢出的**模式图标（判据 ⑤）。</summary>
        void DuelButton(Transform parent, string name, string iconName, string icon, PxRect r, PxRect txR,
                        PxRect icR, string text, System.Action onClick)
        {
            var b = Node(parent, name, r);
            Nine(b, "40K_button", r, BtnBorder, "Image", QBg, BtnTint);
            // 🆕 **2026-10-12（A336③）**：`…/Buttons/Generic UI Button/Button Text` 实读
            //    = `fs 38.0 · auto[12~38] · base **12.0**`（判据 = 全库 `'Skirmish'`/`'Continue'` 里
            //    与 `12~38` 这个窗口配对的 4 颗 —— `fs=38 auto[12~38] base=12` ×4；另一族
            //    `fs=38 auto[18~38] base=36` ×8 **不是本窗**）。上限 = 标称 ⇒ A333 本来就对。
            MenuDraw.TextBox(b, txR, text, Color.white, "Button Text", 38f, 12f, QText, 38f, 12f);
            Rect(b, icon, icR, iconName, QContent, null, true);
            MenuDraw.Hit(b, "Hit", r, QHit, onClick);
        }

        /// <summary>原版 `OnSkirmishButtonPressed` / `OnClassicButtonPressed` —— 两个方法**逐行同构**，
        /// 只差取的那个 `IPlayEvent`（普查 §③「好友挑战」那一条）。它们要的是**对手在线 + 服务器**。</summary>
        void OnMode(string mode)
        {
            Debug.Log("[Duel] `" + mode + "`：原版 `ChallengeManager.OpenDuel` 拿「对手 + 模式」去开一局 —— "
                    + "要**服务器**（对手得在线）⇒ 本地开不了。**没有静默**，点了就报这一句。"
                    + "（对面名字：`" + (OpponentName ?? "") + "`）");
        }
    }
}
