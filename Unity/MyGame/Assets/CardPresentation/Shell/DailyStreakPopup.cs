// DailyStreakPopup.cs — 「日常」第 2 层：每日连登窗（原版 `Daily Streak Popup`）
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §五（层 × 参数表）· `资料/日常_调用链_三窗.md` §二（调用链 + 两态；⚠️ 更正：原来指 `资料/日常_调用链_DailyStreak.md`，2026-10-10 已并入）。
// **类名照原版**：`DailyStreakWindow : LiveOpsEventWindow<MainMenuMission>`（MB `8654310213240890027`）。
// prefab 根 pid `2821652005965210283`。
//
// **窗口参数（实证）**：`type=0(Fullscreen)` · `windowsPlacement=15(Popup)` · `closeOnESC=0` ·
// `extraScaleSmallScreen=1.0` · **`scaleMultiplierFirstElement = 1.2`**。
//
// **矩形出处**：`工具/menu_rect.py bundle_menus_assets_all "Daily Streak Popup" --depth 5 --relative`
// 与 `… "Daily Streak Reward Popup Entry" --depth 3 --relative --root-size 379.816x516.301` **机械走链**算出。
//
// ---- 🔴 两态（**靠面板互斥，不是两套资产**）----
// 驱动字段是 **任务对象的** `MainMenuMission.HasFailed`@0x30 / `FailedValue`@0x34（不是窗口自己的），
// 唯一写入点 = `Open()`（四步顺序与出厂的 T/F 都对上）。**出厂亮着的是「断了」那一态**
// （`Streak Failed` active=true · `Streak Successful` active=false）—— 别照着建还以为默认是连胜。
// ⚠️ **`Timer` 在 `Streak Successful` 面板里** ⇒ **断签态下倒计时看不见**（原版实况）。
//
// ---- 🔴 关窗与返回 ----
// 返回钮与背景遮罩**走同一个 `CloseButtonClicked` → `Close()`（vtable Slot 8）**，
// 而 `Close()` = `LiveOp.TryCollect(() => base.Close())` ⇒ **关窗会先自动收取**（原版的「收完再关」）。
//
// ---- 🔴 奖格轨的视口 / 裁切 / 滚动（**2026-10-08 · A182 建的**）----
// 原版 `Streak Successful/Rewards Scroll View` 底下是三层：`ScrollRect`(**m_Enabled=0 · h=0 v=0**)
// → **`Viewport`(RectMask2D, `m_Softness=(89,0)` `m_Padding=(0,0,0,0)`) → `Rewards Content`(HLG)**，
// 三层**同矩形** 0,159.33 → 1920,964.94（`menu_dump.py … "Daily Streak Popup" --depth 5` 实读；
// 软边/pad 逐处表 = `d:/4/_tmp_view/q1_rm2d.txt:87-88`）。**本件之前这三层一层都没建**
// （连 `Clip` 都一处没有 ⇒ 第 6、7 格照画到屏幕外）。现在按上面那几个字段建全，见 `BuildTrack`。
//
// ---- ⚠️ 我们挑的 / 没做的 ----
//   · **数据全是我们自建**（`DailyData`）—— 原版走 PlayFab 云脚本 #907（`CloudHandle.UpdateMissionPoints`）；
//   · **`bg` 用 `UIGradient` 画**：实测那对颜色是 **#390503 → #0C0004 · angle 82**，与奖励窗 `Content Area` 的
//     完全一致 ⇒ 复用 `RewardsWindow.GradC1/GradC2`（不是猜的，是读出来的）；
//   · **`Streak Failed` 面板**：出厂就亮，但我们单机口径下默认 `HasFailed=false`（见 `DailyData`）⇒
//     默认看到的是 `Streak Successful`。面板本身照建，驱动是数据。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Daily Streak Popup` —— 连登窗。单机口径：主菜单进场时按「本地有没有可领」决定弹不弹。</summary>
    public class DailyStreakPopup : GameWindow
    {
        // ---------------- 窗口根（正本 §五 表 #1~#4）
        public static readonly PxRect Shade = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        public static readonly PxRect Bg = new PxRect(0f, 152.84f, 1920f, 964.94f);
        public static readonly PxRect SepTop = new PxRect(-5.03f, 152.84f, 1925.03f, 167.74f);
        public static readonly PxRect SepBottom = new PxRect(-5.03f, 950.04f, 1925.03f, 964.94f);

        // ---------------- 断签态（`Streak Failed`，出厂 active=true）
        public static readonly PxRect FailedAnchor = new PxRect(910f, 490f, 1010f, 590f);
        public static readonly PxRect F_Broken = new PxRect(47.43f, 338.55f, 1872.57f, 475.45f);
        public static readonly PxRect F_Lost = new PxRect(47.43f, 465.35f, 1872.57f, 552.45f);
        public static readonly PxRect F_Info = new PxRect(47.43f, 583f, 1872.57f, 633f);
        public static readonly PxRect F_ResetBtn = new PxRect(727.72f, 656.5f, 1192.28f, 759.5f);
        public static readonly PxRect F_ResetText = new PxRect(744.02f, 666.55f, 1175.23f, 749.36f);

        // ---------------- 连胜态（`Streak Successful`，出厂 active=false）
        public static readonly PxRect SuccPanel = new PxRect(0f, 152.84f, 1920f, 964.94f);
        public static readonly PxRect S_FillLine = new PxRect(134.43f, 525.70f, 1622.40f, 592.07f);
        /// <summary>🆕 **2026-10-13（A518）**：`Fill Line` 的**视觉框**（= 布局框 × 它自己的 `m_LocalScale 1.2`）。
        /// <para>🔴 **绕【左中】放大，不是绕中心** —— 判据 = prefab 那一颗的 `RectTransform_3187973920738910891.json`
        /// 亲读：`m_Pivot = (0, 0.5)` · `m_AnchorMin = m_AnchorMax = (0, 0.5)` ·
        /// `m_AnchoredPosition = (134.42572021484375, 0)` · `m_SizeDelta = (1487.978515625, 66.37200164794922)` ·
        /// `m_LocalScale = (1.2000001668930054 ×3)`。
        /// ⇒ 左沿**不动**（134.4257）、右沿 = `134.4257 + 1487.9785 × 1.2` = **1920.0**（正好铺满整幅）；
        /// 竖向绕中线各 66.372 × 0.6 = 39.823 ⇒ `519.07 → 598.71`。</para>
        /// <para>⚠️ **别拿 `ScaleAbout`（本文件那个）来算这一颗** —— 那是奖格那条 `scaleMultiplierFirstElement`
        /// 的口径（绕**中心**，因为奖格 prefab 的 pivot 实测 `(.5,.5)`）；这一颗 pivot 是 `(0,.5)`，
        /// 绕中心会把左沿推到 **−14.37**（屏幕外）。</para>
        /// <para>尺寸侧的旁证（同一个 1.2、只印尺寸不印位置）：
        /// `python 工具/menu_rect.py bundle_menus_assets_all "Daily Streak Popup" --depth 5 --relative`
        /// 行末印 `视觉框=×1.2 → 视觉 1785.57×79.65`；正本 `资料/日常_原版规格.md:487` 6a 也记着 `scl=(1.2)`。</para>
        /// <para>🔴 同一颗原版还是 **`Sliced`**：贴图 `40k_generial_bar_fill` **12×12 · 九宫 4,4,4,4** ·
        /// `m_PixelsPerUnitMultiplier = 2` ⇒ 画出来的角块 = `4 ÷ (100/100 × 2)` = **2px**
        /// （判据 = 同一次 dump 的行末 `40k_generial_bar_fill 12×12 九宫4,4,4,4 | Sliced (0.434,0,0.0567,0.624) ppuMul=2.0`；
        /// 换算口径见 `MenuDraw.Nine` 的 `borderOutPx` 形参）。</para></summary>
        public static readonly PxRect S_FillLineVisual = ScaleLeftAbout(S_FillLine, 1.2f);
        /// <summary>绕**左中**缩放（原版 pivot `(0,.5)` 的几何效果）。只服务 `Fill Line`
        /// —— ⛔ 别拿它替换 `ScaleAbout`（那个是绕中心的，见 `S_FillLineVisual` 的注释）。</summary>
        static PxRect ScaleLeftAbout(PxRect r, float s)
        {
            float cy = (r.y1 + r.y2) * 0.5f, half = (r.y2 - r.y1) * 0.5f * s;
            return new PxRect(r.x1, cy - half, r.x1 + r.W * s, cy + half);
        }
        public static readonly PxRect S_CurLabel = new PxRect(43f, 212.42f, 533.91f, 295.07f);
        public static readonly PxRect S_CurValue = new PxRect(545.91f, 212.42f, 583.76f, 295.07f);
        public static readonly PxRect S_Scroll = new PxRect(0f, 159.33f, 1920f, 964.94f);
        public static readonly PxRect S_Info = new PxRect(47.47f, 830f, 1872.53f, 880f);
        public static readonly PxRect S_Timer = new PxRect(598.31f, 964.94f, 1321.69f, 1080f);
        public static readonly PxRect S_TimerNext = new PxRect(573.31f, 997.47f, 935f, 1047.47f);
        public static readonly PxRect S_TimerClock = new PxRect(940f, 1002.47f, 980f, 1042.47f);
        public static readonly PxRect S_TimerText = new PxRect(985f, 997.47f, 1346.69f, 1047.47f);

        // ---------------- 顶栏（`Header With Back Button`）
        public static readonly PxRect HeaderRoot = new PxRect(0f, 21.65f, 550f, 131.20f);
        /// <summary>🆕 **2026-10-13（A519）**：`Header Background` —— 原版顶栏底下**两颗底图**里的**第一颗**
        /// （本件之前我们**一颗都没建**，只建了下面那颗 `Header Background (1)`）。
        /// <para>判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10` 实读
        /// `Header Background 0.0 21.7 → 595.3 137.0 595.30×115.36`（**y 与 `Header Background (1)` 逐值相同**）。
        /// ⚠️ `工具/menu_rect.py … --relative` 给的是 **0 宽**（0.00 → 0.00）—— 那是**布局跑之前的模板位**：
        /// 这一颗挂 `ContentSizeFitterMinMax` + `HorizontalLayoutGroup`（`pad=155,61,0,0` · spacing 5.5），
        /// 撑开后的宽 = `155 + 379.30(Window Title) + 61` = **595.30**（`widthMin 550 / widthMax 1250` 都没钳到）
        /// ⇒ 用 dump（跑过布局）那一份，⛔ **别用 0**。</para>
        /// <para>🔴 原版 `Window Title` 挂在**这一颗**底下（见 `BuildHeader`）。</para></summary>
        public static readonly PxRect H_Plate = new PxRect(0f, 21.65f, 595.30f, 137.01f);
        public static readonly PxRect H_Title = new PxRect(155f, 38f, 534.30f, 120.66f);
        public static readonly PxRect H_Bg = new PxRect(-462.10f, 21.65f, 87.90f, 137.01f);
        public static readonly PxRect H_Back = new PxRect(-24.40f, 23.67f, 143.48f, 134.99f);
        // ⚠️ `WF_Campaign_Info_Background` 的**九宫 / 贴图尺寸**（740×167 · border (335,0,395,0) ·
        //    `m_PixelsPerUnitMultiplier` 缺省 = 1 ⇒ 画出来的角块就是 335 / 395px）**本窗不再留第二份** ——
        //    2026-10-17（A866）收口到 `WindowHeader.PlateBorder` / `PlateTexW` / `PlateTexH`
        //    （本窗原来那一组与 `Shell/LiveOpsEventWindow.cs` 的**逐值相同** = 「同一条规则抄两处」）。
        //    判据（`Sprite/*.json` 实测 + A641 那次全包清点：用这张 sprite 的 20 个 `Image` 实例 `m_Type` 全是 1
        //    ⇒ **别退回 `Simple`**）写在 `WindowHeader` 那三个常量上，⛔ 不在这里再抄一遍。

        // ---------------- 奖格（Entry 局部坐标，见 `menu_rect.py … "Daily Streak Reward Popup Entry"`）
        // 🔴 **2026-10-10 已查实的偏离（`A1242②`）—— ⛔ 本件【值一个都没改】，只把判据落在这儿**：
        //   下面这一族（`EntryW` / `EntryPitch` 与那 7 个 `E_*`）**整族漏了三处祖先 `m_LocalScale`**：
        //     · **条目根** `Daily Streak Reward Popup Entry` = **1.2**（`rt 7189168232786238593`）；
        //       `scaleMultiplierFirstElement` 是**在它之上再乘** 1.2 ⇒ 原版**第一格 = 1.44、其余 = 1.2**，
        //       判据 = `DailyStreakWindow__RefreshRewards.c:97-102` 实读（`set_localScale(get_localScale()×f)`，
        //       只对 `iVar15 == lVar9+0x10` 那一格做）。我们只在**基线 1.0** 上放大了第一格 ⇒ 每一格都小 1/1.2。
        //     · **两层容器** `NormalReward`(ls 0.8) / `Collect`(ls 0.7) —— 它俩底下那几颗 `E_*` 是
        //       **未缩放帧**的 rect ⇒ 屏上分别大 1/0.96 与 1/0.84（`P-L` 修好之后的 `menu_dump` 现读）。
        //     · `Rewards Content` 那条 HLG 序列化 **`m_ChildScaleWidth = 1`**
        //       （`MonoBehaviour_-4833776845445324117.json`；同包另一条 spacing/pad 相同的 HLG 那份是 **0**
        //       ⇒ ⛔ 别拿它当默认值）⇒ **原版中心距 = 379.816 × 1.2 − 64 = 391.78**，而 `EntryPitch` = **315.816**（少 24%）。
        //   🔑 **可见后果**（判据 = 用户实拍 `资料/原版参照图/用户实拍_1017/奖励-每日连胜的参考图…png`）：
        //     原版相邻两卡**有缝**（卡框 ÷ 中心距 ≈ 336.66/391.78 = **0.859**，实拍量到 0.852），
        //     而**我们这边是重叠的**（`E_Bg` 350.69 宽 vs 中心距 315.816）。
        //   ⚠️ `A1242②` 点名的那两颗，**原版屏上** = `Reward Name` **324.64 × 31.00** ·
        //     `Collect Text` **209.78 × 42.06**（`menu_dump.py bundle_menus_assets_all
        //     "Daily Streak Reward Popup Entry" --depth 4` 现读；`R-M` 那对 `405.95/299.12` 是**旧尺子**的读数，⛔ 别用）；
        //     我们传的是 **338.17 / 249.74** ⇒ **偏宽 4.2% / 19.1%**（⛔ 不是 `R-M` 记的「偏窄」）。
        //   ⛔ **别只改那两个数**：整族要**一起**按「每颗 `E_*` 乘它所在容器的 `ls`（绕**该容器**中心）·
        //     条目本身按 1.2 渲染（首格再 ×1.2）· 中心距 391.78」重算 —— 且
        //     `Editor/RewardsScene.cs:7208`（中心距）与 `:7231`（首格 `BG` 上沿 = 289.70）那两条断言**必须同批改**。
        //     **本件停手只报**（那两条断言不在本件的白名单里）。
        public const float EntryW = 379.816f, EntryH = 516.301f;
        /// <summary>`Rewards Content` 的 HLG：**spacing = −64**（相邻两格**故意重叠 64**）、align=3(MiddleLeft)。</summary>
        public const float EntrySpacing = -64f;
        public static float EntryPitch { get { return EntryW + EntrySpacing; } }

        /// <summary>`Rewards Content` 那个 `HorizontalLayoutGroup` 的 **pad.L = 2**（原文
        /// `pad=2,0,58,0`）⇒ **第一格从内容左沿 +2 起排**（内容 = 左对齐容器，它的左沿 = 视口左沿）。</summary>
        public const float ContentPadL = 2f;
        /// <summary>🆕 **2026-10-11（A240）**：同一个 HLG 的 **pad.T = 58 / pad.B = 0**（原文 `pad=2,0,58,0`，
        /// 形状 = UGUI 的 `(L,R,T,B)` ⇒ 那三个数是 **L 2 · R 0 · T 58**、B 缺省 0）。
        /// 🔴 **它 + `align = 3(MiddleLeft)` 一起决定条目的纵向摆位**（见 `EntryTop`）。</summary>
        public const float ContentPadT = 58f, ContentPadB = 0f;

        /// <summary>🆕 **2026-10-11（A240）**：`Rewards Content` 自己的矩形 —— 原版 = **x 2.0（CSF 撑出来的
        /// 零宽点）· y 132.09 → 944.19**（`menu_dump.py … "Daily Streak Popup" --depth 5` 实读；
        /// 波 C1 报告 §一 那张表的第 3 层）。⛔ **y 那两个数是原版值，别拿它当「条目上沿」**（原来就是这么错的）。
        /// <para>⚠️ x 那一对从 `0` 改成 **2.0**（原版值）——**零画面变化**：`MenuDraw.Node` 只吃矩形的**中心**、
        /// 而两个子件（条目）都是按**画布绝对坐标**摆的（`Local()` 里对父做了一次 `InverseTransformPoint`）
        /// ⇒ 这个容器的位置挪 2px 不会挪动任何看得见的东西。</para></summary>
        public static readonly PxRect S_Content = new PxRect(2f, 132.09f, 2f, 944.19f);

        /// <summary>🔴 **条目的上沿**（`Rewards Content` 里的局部 y）—— 原版那条 HLG 是
        /// **`align = 3(MiddleLeft)`（竖向居中）** + `pad T58 / B0` ⇒ 条目在**内缩后的内容区**
        /// 里垂直居中：
        /// <code>
        /// EntryTop = (132.09 + 58 + 944.19 − 0) / 2 − 516.301 / 2 = 567.14 − 258.15 = **308.99**
        /// </code>
        /// 🔴 **2026-10-11（A240）之前我们摆在 **132.09**（顶对齐）—— 差 **177px**，而且
        /// `scaleMultiplierFirstElement = 1.2` 放大那一格之后**顶出视口**（原版不会）。
        /// 判据 → `资料/普查产出_1008/波C1_A182_四扇窗裁切.md` §四·4（原版上沿 ≈ **309**）。</summary>
        public static float EntryTop
        {
            get
            {
                return (S_Content.y1 + ContentPadT + S_Content.y2 - ContentPadB) * 0.5f - EntryH * 0.5f;
            }
        }
        /// <summary>`Rewards Content` 跑完 `ContentSizeFitter` 之后的宽 —— 原版 = pad.L 2 + 7 格 ×379.816
        /// + 6 个 spacing(−64) = **2276.71**（`menu_dump` 那一行给的是**出厂 0 子件**时的 `MinSize=2`）。
        /// **视口只有 1920** ⇒ 可滚范围 = 2276.71 − 1920 = **356.71px**。</summary>
        public static float TrackContentW { get { return ContentPadL + EntryPitch * (DailyData.StreakDays - 1) + EntryW; } }

        // ---------------- 🔴 奖格轨的视口（`Streak Successful/Rewards Scroll View/Viewport`）
        // 原版那一层是 **`RectMask2D`**（组件不在 `MonoBehaviour/` 里按名字查得到 —— 它是 Unity 内置件，
        // 判据 = 解包 `RectMask2D` 实例的 `m_Softness` / `m_Padding`；逐处表 → `d:/4/_tmp_view/q1_rm2d.txt`）：
        //   · `soft=(89,0) pad=(0.0,0.0,0.0,0.0) en=1`（`q1_rm2d.txt:87-88`，
        //     路径 `Daily Streak Popup/Streak Successful/Rewards Scroll View/Viewport`）
        //   · 结构 `Rewards Scroll View`(ScrollRect → `Viewport`(RectMask2D) → `Rewards Content`(HLG)
        //     —— `menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 5` 实读。
        /// <summary>原版 `RectMask2D.m_Softness`（画布像素 · x 管左右 · y 管上下）= **(89,0)**。
        /// ⚠️ **不是 `(0,89)`** —— 这条轨是**横向**滚的，渐隐带在**左右**两条边上（写反了会横切）。
        /// 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：类型从 `Vector2` 改成 `Vector2Int`** ——
        ///   它现在**就是**喂给 `ViewportClip.softness` 的那个值，而原版那个字段的类型逐字是
        ///   `RectMask2D.m_Softness: Vector2Int`（判据 → `Shell/ViewportClip.cs` 的字段注释）。</summary>
        public static readonly Vector2Int TrackSoft = new Vector2Int(89, 0);
        /// <summary>原版 `RectMask2D.m_Padding` = **(0,0,0,0)**（形状 = UGUI 的 `(L,B,R,T)`；正 = 缩小）。</summary>
        public static readonly Vector4 TrackPad = Vector4.zero;
        public static readonly PxRect E_Bg = new PxRect(14.56f, 26.95f, 365.25f, 480.15f);
        public static readonly PxRect E_Highlight = new PxRect(-69.29f, -59.49f, 449.10f, 566.59f);
        public static readonly PxRect E_Holder = new PxRect(20.21f, 52.72f, 359.79f, 400.58f);
        public static readonly PxRect E_Name = new PxRect(20.82f, 416.66f, 358.99f, 448.96f);
        public static readonly PxRect E_Extra = new PxRect(242.41f, 59.85f, 342.41f, 159.85f);
        public static readonly PxRect E_Collect = new PxRect(9.89f, 63.09f, 283.53f, 145.81f);
        public static readonly PxRect E_CollectText = new PxRect(17.02f, 79.79f, 266.76f, 129.86f);

        // ---------------- 图
        public const string ArtSepLine = "40k_main_line";
        public const string ArtBackOpaque = "UI_Deck_Information_submenu_Back_opaque";
        /// <summary>`Highlight` 的图 —— 与每日奖励窗同一个（**按 pid 反查**：`m_Sprite.m_PathID 1969816644568467221`）。</summary>
        public const string ArtHighlight = "OctagonUI_Border_SDF_2";
        public const string ArtExtra = "40k_main_bt_rewards";
        public const string ArtClaim = "WF_Special_offer_Value";
        public const string ArtFillBar = "40k_generial_bar_fill";
        public const string ArtClock = "WF_icon_clock";
        public const string ArtHeaderBg = "WF_Campaign_Info_Background";
        public const string ArtBackBtn = "UI_Button_Menu_Back";
        public const string ArtMulligan = "UI_Button_Mulligan";

        public static readonly Color HighlightTint = new Color(1f, 1f, 1f, 0.624f);
        public static readonly Color ClaimTint = new Color(0.06f, 0.57f, 0.13f, 1f);
        /// <summary>🔴 **2026-10-13（A642）**：原版那一颗的 `m_Color` 实读 = **`(0.434, 0, 0.0567, 0.624)`**。
        /// 原来这里写的是 `(0.43, 0, 0.06, 0.62)` —— **四个分量各差一点点**（0.004 / 0 / 0.0033 / 0.004）。
        /// 判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10`
        /// 那一行的行末 `Sliced (0.434,0,0.0567,0.624) ppuMul=2.0`（`工具/menu_dump.py:573` 印的就是
        /// `Image.m_Color` 本体，不是另算的）。</summary>
        public static readonly Color FillLineTint = new Color(0.434f, 0f, 0.0567f, 0.624f);

        public const int QShade = 3002, QPanel = 3006, QContent = 3010, QText = 3011, QOverlay = 3014;

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();
        public readonly System.Collections.Generic.List<Transform> entries = new System.Collections.Generic.List<Transform>();

        /// <summary>奖格轨的滚动区（**全壳唯一一份滚动实现** = `MenuScroll`）。
        /// 自检直调 `ScrollBy`/`SetOffset`（批处理里没有滚轮事件 ⇒ 和真滚同一条路）。
        /// 🔴 **本窗【不】登记给 `PointerLayer`** —— 判据 = 原版那颗 `ScrollRect` 的实读字段：
        /// `h=0 v=0 mode=1 inertia=1 …` **且 `m_Enabled = 0`**（`menu_dump.py … "Daily Streak Popup"`
        /// 的 `Rewards Scroll View` 那一行）⇒ **玩家滚不动它**；位移只有一条路
        /// = `DailyRewardSelector.Initialize/AdjustView` 末尾的 `FocusOnItem(scrollRect, items[day−3])`
        /// （`d:/2/tools/decomp_full/DailyRewardSelector__Initialize.c` / `__AdjustView.c`，两处同源）。
        /// ⚠️ 那条 `FocusOnItem` 只在 **`5 &lt; 当前天`** 时走（同上两文件里 `if (5 &lt; …)`）⇒ 我们这台
        /// `DailyData.StreakCollected` 是常量 **5** ⇒ **当前数据下恒不移动**（照原版如实做，不额外发明）。</summary>
        MenuScroll _trackScroll;
        /// <summary>自检用（量可滚范围 / 直调 `ScrollBy`）。</summary>
        public MenuScroll TrackScroll { get { return _trackScroll; } }
        /// <summary>奖格轨的内容容器（`Rewards Content`）—— 滚动偏移一变就重建它。</summary>
        Transform _trackContent;

        public bool HasFailed { get; private set; }
        public float PanelH { get { return EntryH; } }

        /// <summary>🆕 **2026-10-05（A81）**：压暗层的**命中区**节点（「点窗外关窗」）—— 自检用
        /// （`MenuDraw.ShadeRuleOk(darkHit, qShade, qContentMin, out why)` 的 `darkHit`）。</summary>
        public Transform ShadeHit { get { return transform.Find("BackgroundHit"); } }

        public static DailyStreakPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Daily Streak Popup");
            var win = go.AddComponent<DailyStreakPopup>();
            win.type = WindowType.Fullscreen;             // 实证 type=0
            win.placement = WindowsPlacement.Popup;       // 实证 windowsPlacement=15
            win.closeOnEsc = false;                       // 实证 closeOnESC=0
            win.extraScaleSmallScreen = 1f;               // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); }

        public void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            MissingArt.Clear(); entries.Clear();
            HasFailed = DailyData.StreakFailed();

            // 1) 压暗整屏
            MenuDraw.Rect(root, CardArt.Solid(), Shade, "Menu Dark Background", QShade,
                          new Color(0f, 0f, 0f, 0.77f));
            // 🔴 **2026-10-05（A81）**：压暗层的**点击区**（「点窗外关窗」）—— **原来零 `ShadeHit(`**。
            //   判据：原版是压在 `Menu Dark Background` **自身节点**上的 `BackgroundCloseButton`；
            //   本窗的接线字段叫 **`backgroundButton`**（`DailyStreakWindow__Start.c`，偏移 `0x98`）——
            //   ⚠️ 名字各窗不同（全工程 35 处），**别按名字 grep 一次就下结论**。
            //   🔴 **它挂的处理函数与顶栏「返回」钮是【同一个】** —— 实据：
            //     `DailyStreakWindow__Start.c` 里 `backgroundButton.onClick` 用的是 **`DAT_1842b3520`**，
            //     而 `DailyStreakWindow__Open.c` 里 `header.Initialize(…, delegate(DAT_1842b3520))` 用的是
            //     **同一个方法常量**（`resetStreakButton` 那颗用的是 `DAT_1842b3820` = 另一个方法，别混）。
            //     ⇒ 这里的动作**照抄 `BuildHeader` 那颗返回钮**（`StreakAutoCollect` + `Close`，
            //     原版 `Close()` 覆写里就是 `TryCollect(() => base.Close())`），⛔ 不是裸 `Close()`。
            //   档 = **压暗层自己那一档 `QShade`(3002)**，**严格低于**本窗内容命中区档 `QContent`(3010)
            //   （内容命中区 = `Header Back Button` / `Reset Streak` / 奖格的 `Collect`，三颗都在 `QContent`）。
            //   出处 → `资料/待办判据_阶段二与联机.md` §A81 · 公共件规矩 → `MenuDraw.ShadeHit` 的注释。
            MenuDraw.ShadeHit(root, Shade, QShade, QContent,
                              () => { DailyData.StreakAutoCollect(); Close(); }, "BackgroundHit");
            // 2) `bg`：**无 sprite + `UIGradient`** —— 实测那对颜色与奖励窗 `Content Area` **完全一致**
            //    （`m_color1 #390503 / m_color2 #0C0004 / m_angle 82`）⇒ 直接复用那两个常量。
            MenuDraw.Rect(root, CardArt.Gradient(RewardsWindow.GradC1, RewardsWindow.GradC2, 82f), Bg, "bg", QPanel);
            // 🆕 **2026-10-06（A94）：`bg` 那块整幅面板底图吸收点击**。判据 = 原版 prefab
            //   `Daily Streak Popup > bg` 那颗 `Image` 的 **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）
            //   —— 射线打到它自己、父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）
            //   ⇒ 原版点这里**什么都不做**。
            //   ⚠️ 两个面板（`Streak Successful` / `Streak Failed`）**没有自己的底图**（它们只是容器）
            //   ⇒ 吸收层就这一块（盖得住两边）。
            MenuDraw.Absorb(root, "AbsorbHit", Bg, QShade, QContent);
            // 3) 两条分隔线
            MenuDraw.Rect(root, Art(ArtSepLine), SepTop, "Separator Line Top", QPanel);
            MenuDraw.Rect(root, Art(ArtSepLine), SepBottom, "Separator Line Bottom", QPanel);

            // 4) 两个面板 —— **互斥**（`HasFailed` 决定谁开）
            BuildSuccessful(root);
            BuildFailed(root);

            // 5) 顶栏
            BuildHeader(root);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[DailyStreak] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>连胜面板：`Fill Line` + `Current Streak` + 奖格轨 + `Info` + `Timer`。</summary>
        void BuildSuccessful(Transform root)
        {
            var p = MenuDraw.Node(root, "Streak Successful", SuccPanel);
            // 🔴 **2026-10-13（A518）**：这一颗原来是**裸 `MenuDraw.Rect`**（`Simple` + **布局框**）—— 两处都不符原版：
            //   ① **`scl=1.2` 没接**：原版画出来 **1785.57×79.65**，我们画 1487.98×66.37（**绕左中**放大，见 `S_FillLineVisual`）；
            //   ② 原版那颗是 **`Sliced`**（`40k_generial_bar_fill` 12×12 · 九宫 4,4,4,4 · `ppuMul=2` ⇒ 角块 2px），我们走 `Simple`。
            //   判据（本件亲跑，⛔ 不是抄表）：
            //     `python 工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10`
            //     ⇒ `Fill Line 134.4 525.7 1622.4 592.1 1487.98 66.37 | 视觉框=×1.2 → 视觉 1785.57×79.65
            //         40k_generial_bar_fill 12×12 九宫4,4,4,4 | Sliced (0.434,0,0.0567,0.624) ppuMul=2.0`；
            //     pivot 那一半 = prefab `RectTransform_3187973920738910891.json` 亲读（`m_Pivot=(0,0.5)`）。
            //   🔴 **色值那一笔 2026-10-13（A642）已做**（原文写「仍用本文件的 `FillLineTint`（`(0.43,0,0.06,0.62)`）
            //      —— 原版实测是 `(0.434,0,0.0567,0.624)`，两者差一点点，但那一笔不属于本账」）——
            //      A642 就是那条「另立的账」，`FillLineTint` 现已钉成原版值（判据见它的注释）。
            MenuDraw.Nine(p, Art(ArtFillBar), S_FillLineVisual,
                          new Vector4(4f, 4f, 4f, 4f), 12f, 12f, QPanel, FillLineTint,
                          true, "Fill Line", new Vector4(2f, 2f, 2f, 2f));
            // 🔴 **2026-10-13（A493 #1/#2）**：这两颗补**显式左对齐**，第 ① 颗顺带换**稳定节点名**。
            //   判据 = 原版 `Daily Streak Popup/Streak Successful/Current Streak`（= 本处的新名字）**与它的子件**
            //   `Current Streak Value`：**两颗都是 `对齐=Left/Capline`** ——
            //   `python 工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10 --no-sprite` 实读：
            //   `Current Streak` `43.0 212.4 533.9 295.1`（= 我们的 `S_CurLabel`）·
            //   `Current Streak Value` `545.9 212.4 583.8 295.1`（= 我们的 `S_CurValue`），矩形逐值相同。
            //   ⚠️ **不显式对齐的后果**：`Label` 默认把文字块**居中**摆在框心（`MenuDraw.Text` 建的锚点 pivot=(.5,.5)）
            //   ⇒ 原版贴左、我们居中。`MenuDraw.AlignLeft` 收的是**框**，两处都对得上。
            //   🔴 **第 ① 颗原来把【文案】当【节点名】传**（`DailyData.StreakCurrentLabel()` 的返回值 = `'Current streak:'`
            //   同时当节点名）⇒ 节点名随语言/文案变（本仓正在中英混用）⇒ 改成原版那个名字 `Current Streak`。
            //   🔴 **2026-10-13（A517）字距 5 已补**（原文写「⛔ 本账只做对齐 …… 那另立账，别在这里顺手加」，
            //   那是 A493 那一轮的边界；A517 就是那个「另立的账」）。原版 `m_characterSpacing = 5`（同一次 dump 的 `字距=` 列）。
            //   🔴 **次序不能反（A475 那个坑）**：`AlignLeftOn` 是「量**当时的** `WorldW` 再反推整块位置」
            //   （`Battle/Label.cs` 的 `SetAutoFitBox` 头一句就是 `RefreshBounds()`），而 `SetCharSpacing` 会**改渲染宽**
            //   ⇒ 排在它**之后**调 = 那一行按**旧宽**定位、字整体往左溢出 Δ宽/2，**而且一声不响**。
            //   原版那两行是**静态序列化字段**（不存在「先对齐、后加字距」这种次序）⇒ 照原版就只能是「字距在前、对齐在后」。
            //   ⛔ 这里**不补** `ForceRelayout()` —— `AlignLeftOn` 自己就会按**含字距**的 `textBounds` 重量一次
            //   （同 `Shell/LiveOpsEventWindow.cs` 里那处「顺序不能反」的口径 那处的口径）。
            var curLbl = MenuDraw.Text(p, S_CurLabel, DailyData.StreakCurrentLabel(), Color.white, "Current Streak", 70f, QText);
            if (curLbl != null) curLbl.SetCharSpacing(5f);
            MenuDraw.AlignLeft(curLbl, S_CurLabel);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `对齐=Left/**Capline**`（同上那条 dump 的实读）。
            MenuDraw.SetVAlign(curLbl, Label.VAlign.Capline, S_CurLabel);
            // 🔴 **2026-10-13（A520）**：`Current Streak Value` 的**父子关系**改成照原版 —— 原版它是
            //   `Streak Successful/Current Streak` 的**子件**（`menu_dump.py … "Daily Streak Popup" --depth 10`
            //   实读层级：`Current Streak` 深 2、`Current Streak Value` 深 3），我们原来是**兄弟**。
            //   ⚠️ 位置**不受影响**：`MenuDraw.Text` 吃的是**画布绝对矩形**（`Local()` 对父做一次
            //   `PosInDesignSpace` 反算），父件是谁只改**树形**、不改落点（`S_CurValue` 那四个数照旧）。
            //   🔴 **写断言的人注意**：⛔ 别写 `FindPath(succ, "Current Streak/Current Streak Value")`
            //   （调度台口径）；按名字取要用**递归**的 `FindChild(succ, "Current Streak Value")`（它穿子树）。
            var curVal = MenuDraw.Text(curLbl != null ? curLbl.transform : p, S_CurValue,
                                       DailyData.StreakCurrentValue(), Color.white, "Current Streak Value", 80f, QText);
            if (curVal != null) curVal.SetCharSpacing(5f);
            MenuDraw.AlignLeft(curVal, S_CurValue);
            MenuDraw.SetVAlign(curVal, Label.VAlign.Capline, S_CurValue);   // A712 阶段 2：原版 `Left/Capline` 的纵向那一半

            var view = MenuDraw.Node(p, "Rewards Scroll View", S_Scroll);
            // 🔴 **2026-10-08（A182）：`Viewport` 这一层原来是缺的** —— 原版结构是
            //   `Rewards Scroll View`(ScrollRect) → **`Viewport`(RectMask2D)** → `Rewards Content`(HLG)
            //   （`menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 5` 实读；
            //    三层 **同矩形** 0,159.33 → 1920,964.94）。带掩码的那一层（也是 `Clip` 的落点）就是它。
            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）**：这一层就是**视口节点**，裁切状态挂在它身上
            //   （= 原版 `RectMask2D` 挂 `Streak Successful/Rewards Scroll View/Viewport`；
            //   契约 → `Shell/ViewportClip.cs` 文件头）。走 `ViewportClip.Hang` ⇒
            //   **框（节点自己的 rect）· `padding` · `softness` 三样一次写死**：
            //   `TrackPad = (0,0,0,0)` · `TrackSoft = (89,0)`（原版实读 `q1_rm2d.txt:87-88`）。
            //   ⛔ **别改回 `Node(...)`** —— 那样节点上就没有状态了。
            var vp = ViewportClip.Hang(view, "Viewport", S_Scroll, TrackPad, TrackSoft).transform;
            _trackContent = MenuDraw.Node(vp, "Rewards Content", S_Content);
            // 滚动区：**左对齐内容**（原版 `Rewards Content` 贴视口左边 + `ContentSizeFitter`）
            // ⇒ 范围 `[0, 内容右端 − 视口右端]` = `[0, 356.71]`（由 `MenuScroll` 自己算）。
            // 🔴 `OnChanged` 指向**幂等**的重建（先清后建）—— 正是 `资料/阶段二_滚动与指针_原版规格.md`
            //    §四 第 1、2 条那两个坑（越建越多 / 相对位移翻倍）的规矩。
            _trackScroll = MenuScroll.LeftAligned(S_Scroll, TrackContentW);
            _trackScroll.Owner = gameObject;      // ⚠️ C# 的对象初始化器**只能跟在 `new` 后面** —— 别写在方法调用后面
            _trackScroll.OnChanged = BuildTrack;
            BuildTrack();
            FocusCurrentDay();          // 原版 `DailyRewardSelector` 那一段（当前数据下恒不移动，见字段注释）
            // 🔴 **2026-10-13（A493 #3）**：`Info` 补**显式左对齐**。
            //   判据 = 原版 `Daily Streak Popup/Streak Successful/Info`：**`对齐=Left/Midline`**
            //   （`menu_dump.py … "Daily Streak Popup" --depth 10 --no-sprite` 实读 `47.5 830.0 1872.5 880.0`，
            //   与我们的 `S_Info` 逐值相同）。
            //   ⚠️ **别一刀切**：同名的另一颗在**断签面板**里（本文件 `BuildFailed` 那句 `F_Info`）——
            //   原版那颗是 **`Center/Midline`**（同一次 dump：`47.4 583.0 1872.6 633.0`，`对齐=Center/Midline`）
            //   ⇒ 那一颗**保持居中、不许改**。
            var info = MenuDraw.Text(p, S_Info, DailyData.StreakInfoText(), Color.white, "Info", 36f, QText);
            MenuDraw.AlignLeft(info, S_Info);
            MenuDraw.SetVAlign(info, Label.VAlign.Midline, S_Info);   // A712 阶段 2：原版 `Left/Midline` 的纵向那一半
            // ⚠️ `Timer` **在本面板里** ⇒ 断签态下看不到倒计时（原版实况）
            var t = MenuDraw.Node(p, "Timer", S_Timer);
            // 🔴 **2026-10-13（A493 #4/#5）**：`Timer` 底下两颗各补对齐，**两颗方向相反、别一刀切** ——
            //   判据 = 原版 `Daily Streak Popup/Streak Successful/Timer` 的两个子件（同一次 dump 实读）：
            //   · `Next Rewards text` `573.3 997.5 935.0 1047.5` ⇒ **`对齐=Right/Midline`**（**全窗唯一一颗右对齐**）
            //   · `Timer Text`       `985.0 997.5 1346.7 1047.5` ⇒ **`对齐=Left/Midline`**
            //   两个矩形与我们的 `S_TimerNext` / `S_TimerText` 逐值相同。
            // 🔴 **2026-10-13（A643）**：改写**中性口** `MoreRewardsInText()`（原来叫 `StreakNextRewardsText()`）
            //   —— 那条词条是**本窗与每日奖励窗共用**的（原版同一条 I2 词条：本窗英文 `'More Rewards In'`、
            //   奖励窗那份是 es `'Más Recompensas En'`）⇒ 口名不许带 `Streak`（判据 → `DailyData.MoreRewardsInText` 的 doc）。
            var nxt = MenuDraw.Text(t, S_TimerNext, DailyData.MoreRewardsInText(), Color.white, "Next Rewards text", 36f, QText);
            MenuDraw.AlignRight(nxt, S_TimerNext);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `Timer` 底下两颗**同档 `Midline`**
            //   （横向相反：`Next Rewards text` 是 `Right`、`Timer Text` 是 `Left`；判据同上那条 dump）。
            MenuDraw.SetVAlign(nxt, Label.VAlign.Midline, S_TimerNext);
            MenuDraw.Rect(t, Art(ArtClock), S_TimerClock, "Image", QContent);
            var tmr = MenuDraw.Text(t, S_TimerText, DailyData.StreakTimerText(), Color.white, "Timer Text", 36f, QText);
            MenuDraw.AlignLeft(tmr, S_TimerText);
            MenuDraw.SetVAlign(tmr, Label.VAlign.Midline, S_TimerText);
            p.gameObject.SetActive(!HasFailed);
        }

        /// <summary>建奖格轨的 7 格（**幂等：先清后建** —— 滚动偏移一变 `OnChanged` 就会重入这里）。
        /// 🔴 三件事全在这一段里成对拿捏（原版都长在 `Viewport` 那颗 `RectMask2D` 上）：
        ///   · **裁切** `Clip` = 视口矩形（`S_Scroll`，与 `Viewport` 同矩形）；
        ///   · **软边** `ClipSoftness` = **(89,0)**；**padding** `ClipPad` = **(0,0,0,0)**。
        /// ⚠️ **格节点照样建满 7 个**（原版 `DailyRewardSelector.CreateRewards` 也是实例化全部 7 格、
        ///   被掩码裁掉的只是**像素**）⇒ `entries.Count` 恒 = `DailyData.StreakDays`；
        ///   整块落在视口外的那些**子件**（图标/角标…）由 `ClipRect` 各自「不建」。</summary>
        void BuildTrack()
        {
            if (_trackContent == null) return;
            for (int i = _trackContent.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_trackContent.GetChild(i).gameObject);
            entries.Clear();

            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：那六行没了。** 旧写法 = 「存 `Clip`/`ClipSoftness`/
            //   `ClipPad` 三件 → 设 `S_Scroll` / `TrackSoft(89,0)` / `TrackPad(0,0,0,0)` → 建完成对还原」。
            //   现在状态长在**视口节点**上（`Build()` 里 `ViewportClip.Hang(view, "Viewport", S_Scroll,
            //   TrackPad, TrackSoft)` 那一句已经把框 / pad / soft 一次写死）⇒ 本函数**一个字都不设**。
            //   判据（原版实读）：`Daily Streak Popup/Streak Successful/Rewards Scroll View/Viewport`
            //   = `soft=(89,0) pad=(0,0,0,0)`（`d:/4/_tmp_view/q1_rm2d.txt:87-88`）。
            //   🔴 **改坏法**（现在唯一能红的地方）= 改 `Build()` 里 `ViewportClip.Hang(…)` 那两个实参；
            //      ⛔ **别再把那六行加回来** —— 那会让 `Editor/RewardsScene.cs` 的 A489
            //      （`NodeShadowedByParam == 0`）红。
            int n = DailyData.StreakDays;
            int first = DailyData.StreakCollected;          // `scaleMultiplierFirstElement` 只作用在**这一格**
            for (int i = 0; i < n; i++)
            {
                // **内容坐标**：从 `Rewards Content` 左沿 + HLG 的 `pad.L`(2) 起排，再整体 `Shift` 到屏幕。
                // 🔴 **2026-10-11（A240）：y 从 132.09（顶对齐）改成 `EntryTop`（= 308.99，竖向居中）** ——
                //   原版那条 HLG 是 `align = 3(MiddleLeft)` + `pad T58/B0`（判据 → `EntryTop` 的注释）。
                //   ⛔ **别再用 `S_Content.y1` 当条目的上沿**：那是**内容容器**的上沿，不是条目的
                //   （差 58 + (754.1 − 516.301)/2 = 176.9px），而放大那一格会因此顶出视口。
                float x1 = ContentPadL + EntryPitch * i;
                var r = new PxRect(x1, EntryTop, x1 + EntryW, EntryTop + EntryH);
                // 原版 `DailyStreakWindow.scaleMultiplierFirstElement = 1.2` —— 唯一读取点 = `RefreshRewards` 的
                // 第一次循环（`i == challenge.collectedRewards`）⇒ **本次第一个「还没领」的奖格**放大 1.2。
                // 实测该 prefab 根的 `m_Pivot = (.5,.5)` ⇒ 绕**中心**放大。
                if (i == first) r = ScaleAbout(r, 1.2f);
                if (_trackScroll != null) r = _trackScroll.Shift(r);
                entries.Add(BuildEntry(_trackContent, r, i));
            }
        }

        /// <summary>原版 `DailyRewardSelector.Initialize` / `AdjustView` 的**收尾那一段**（两处逐行同源）：
        /// <code>
        /// if (5 &lt; 当前天) FocusOnItem(scrollRect, items[当前天 − 3]);   // 格中心 = 视口中心
        /// </code>
        /// 判据 → `d:/2/tools/decomp_full/DailyRewardSelector__Initialize.c` 与 `__AdjustView.c`；
        /// `FocusOnItem` 的语义 → `ScrollViewFocusFunctions__FocusOnItem.c`（`normalizedPosition`）
        /// + `ScrollViewFocusFunctions__CalculateFocusedScrollPosition.c`（取 item 矩形中心）。
        /// ⚠️ **本机数据走不到**：`DailyData.StreakCollected` 是常量 **5** ⇒ `5 &lt; 5` 为假 ⇒ 恒不移动
        /// （= 原版开机那一刻的样子；照做、不额外发明）。⚠️ 也**不**依赖原版那条 `if` 之外的东西：
        /// 原版 `ScrollRect` 是 `m_Enabled = 0`（见 `TrackScroll` 的注释）⇒ 位移只有这一条路。</summary>
        public void FocusCurrentDay()
        {
            if (_trackScroll == null) return;
            int day = DailyData.StreakCollected;
            if (day <= 5) return;                                    // 照原版那条 `5 < index`
            int focus = Mathf.Clamp(day - 3, 0, DailyData.StreakDays - 1);
            float cx = ContentPadL + EntryPitch * focus + EntryW * 0.5f;
            _trackScroll.FocusOn(cx);        // 绝对设值（`SetOffset`）—— ⛔ 别改成相对加（规格 §四 第 2 条）
        }

        /// <summary>断签面板：`STREAK BROKEN` + 掉的层数 + 说明 + `Reset Streak`。</summary>
        void BuildFailed(Transform root)
        {
            var anchor = MenuDraw.Node(root, "Streak Failed", FailedAnchor);
            // 🆕 **2026-10-16（A712 阶段 2）**：断签面板这四颗的**纵向档全是原版实读**
            //  （`python 工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 12 --md`）：
            //   · `Daily Streak Broken`（fs128.1）= `Center/**Midline**` · `Current Streak Lost count`（fs66.9）= `Center/Midline`
            //   · `Info`（fs36）= `Center/Midline` ⇒ 见上面 :375 那条「**别一刀切**」（与 `Streak Successful/Info` 的横向档相反）
            //   · `Generic Simplified UI Button/Button Text`（`'Reset Streak'` fs55）= `Center/**Capline**`
            var fBreak = MenuDraw.Text(anchor, F_Broken, DailyData.StreakBrokenText(), Color.white, "Daily Streak Broken", 128.1f, QText);
            MenuDraw.SetVAlign(fBreak, Label.VAlign.Midline, F_Broken);
            var fLost = MenuDraw.Text(anchor, F_Lost, DailyData.StreakLostText(), Color.white, "Current Streak Lost count", 66.9f, QText);
            MenuDraw.SetVAlign(fLost, Label.VAlign.Midline, F_Lost);
            var fInfo = MenuDraw.Text(anchor, F_Info, DailyData.StreakInfoText(), Color.white, "Info", 36f, QText);
            MenuDraw.SetVAlign(fInfo, Label.VAlign.Midline, F_Info);
            var btn = MenuDraw.Rect(anchor, Art(ArtMulligan), F_ResetBtn, "Generic Simplified UI Button", QContent);
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 55px。
            //   判据 = 原版 `menus ▸ Daily Streak Popup ▸ Streak Failed/Generic Simplified UI Button/Button Text`
            //   （`'Reset Streak'`）：**`m_enableAutoSizing = 1`** · `m_fontSizeMin = **10**` ·
            //   `m_fontSizeMax = **55**` · `m_fontSizeBase = **12**` · **折行 = 0**。
            //   ⚠️ 本窗刻度 = 原版刻度（我们传的 `55f` 就是原版 `m_fontSize` 原值）⇒ 四格照抄。
            //   ⚠️ 折行 0 ⇒ `SetAutoFitBox` 内部 `SetWrapWidth` 会**无条件**开成 `Normal` ⇒ 紧跟一句关掉
            //   （必须在 `SetVAlign` **之前** —— `SetWrapping` 会推版面）。
            var fReset = MenuDraw.Text(anchor, F_ResetText, DailyData.ResetStreakText(), Color.white, "Button Text", 55f, QText,
                                       F_ResetText.W, 10f, 55f, 12f);
            if (fReset != null) fReset.SetWrapping(false);          // 原版折行=0（A205：关这一下顺带推版面）
            MenuDraw.SetVAlign(fReset, Label.VAlign.Capline, F_ResetText);
            if (btn != null)
            {
                var hit = btn.gameObject.AddComponent<WindowButton>();
                // 原版 `ResetStreakAfterFail` **不发 PlayFab**（prefab 的 `m_OnClick` 持久调用表是空的，
                // 纯代码挂）；而且它**只是换画面** —— 不写 `HasFailed`、也不减 `currentValue`。
                hit.onClick = () => { DailyData.ResetStreak(); Close(); };
                // 🆕 A17：原版 `Streak Failed>Generic Simplified UI Button` 是 SpriteSwap（普查 §块 4 第 9 行）
                hit.BindSelf(ArtMulligan);
            }
            anchor.gameObject.SetActive(HasFailed);
        }

        /// <remarks>🔴 **2026-10-17（A866）：四层建法已收口到 `WindowHeader.WithBackButton`**
        /// （全工程 4 扇窗各抄一遍 ⇒ 收成一份）。本窗这一档的**差异**（逐格对照 →
        /// `资料/普查产出_1018/S5_A866窗头收口.md` §2）：
        /// · 根名缺省；· 标题走 **`FitAfterSpacing`**（🔴 **2026-10-18（A968）改的**：原来是 `SpacingOnly`
        ///   —— 那意味着**不调 `SetAutoFitBox`**，而原版那一颗 `m_enableAutoSizing = 1`、区间 `18 … 67.55`
        ///   （判据见 `BuildHeader` 里那一长段）⇒ 现已接上自适应，并补了 `TitleFitW/H` = 原版矩形 379.30×82.66）；
        /// · 多一档 `TitleVAlign = Capline`（A712 阶段 2）；
        /// · 返回钮 = `BoundOnQuad`（图**自己**就是按钮，**没有 `BackHit` 节点**）且 `BackKeepAspect = false`
        ///   （另三扇是 `true`）；· `onClick` 先 `StreakAutoCollect()` 再 `Close()`。</remarks>
        void BuildHeader(Transform root)
        {
            // 🔴 **2026-10-13（A519）**：原版这一层底下是**两颗底图**（我们原来只建了一颗，少了 `Header Background`）。
            //   判据 = 本件亲跑 `python 工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10`
            //   实读的兄弟序与矩形（**两颗 sprite pid 相同** = `6473405944757030420` = `WF_Campaign_Info_Background`）：
            //     `Header With Back Button` (0,21.65 → 550,131.20)
            //       ├ `Header Background`      (0,21.65 → **595.30**,137.01)   ← **本件补的**
            //       │   └ `Window Title`       (155,38 → 534.30,120.66)       ← 原版挂在**这一颗**下（本件改挂）
            //       ├ `Header Background (1)`  (−462.10,21.65 → 87.90,137.01) ← 我们原来只有这一颗
            //       └ `Header Back Button`     (−24.40,23.67 → 143.48,134.99)
            //   ⚠️ **「是漏了还是有意省的」判不出【本窗的】意图**（grep `资料/日常_*.md` 里 `Header Background` **零命中**
            //      ⇒ 本窗这一颗没被记过）—— 但同族先例 `资料/阶段二_战斗入口_原版规格.md:191-193` 记着**同一份结构**
            //      ⇒ 按**铁律 11** 补上。三个 rect = `H_Plate` / `H_Title` / `H_Bg` 三条常量，各自的判据写在常量块上。
            //   🔴 **它的可见后果不小**：原来唯一那颗底图只盖 x∈[−462.1, 87.9]，而 `Window Title` 在 x∈[155, 534.3]
            //      ⇒ 标题那一段后面根本没有底板（本窗自检此前对顶栏零覆盖）。
            // 🔴 **A641**：尖角那一颗原来是**裸 `MenuDraw.Rect`**（`m_Type = Simple`，把整张 740×167 拉到
            //   550×115.36）—— **原版是 `Sliced`**；共件一律走 `MenuDraw.Nine`（= A641 要的那一支）。
            //   判据（现读两处互证：`menu_dump` 那一行 + 全包 20 个用这张 sprite 的 `Image` 实例 `m_Type`
            //   全是 1）写在 `WindowHeader.PlateBorder` 的 doc 上，⛔ 不是抄表。
            WindowHeader.WithBackButton(root, new WindowHeader.Spec
            {
                RootRect = HeaderRoot,
                PlateRect = H_Plate,
                TitleRect = H_Title,
                TitleText = DailyData.StreakWindowTitle(),
                // 🔴 **2026-10-18（A968）：这里原来是 `SpacingOnly`（= 只补字距、不补自适应）—— 已改成
                //   `FitAfterSpacing`，并补上自适应那个**框**（`TitleFitW/H`）。**
                //   判据（本轮亲读原版解包资源，逐字段）= 本窗 `Window Title` 那颗 TMP
                //   （`bundle_menus_assets_all` 的 `MonoBehaviour_3324232435684942507.json`，`m_text "Daily Streak"`）：
                //   **`m_enableAutoSizing = 1`** · `m_fontSizeMin = 18.0` · `m_fontSizeMax = 67.55` ·
                //   `m_fontSizeBase = 36.0` · `m_characterSpacing = 5.0`；同包 **8 颗 `Window Title` 逐颗现读、逐值相同**。
                //   ⇒ 原版那一颗**是自适应的**，区间与共件常量（`TitleAutoMinPx 18 / Max 67.55 / Base 36`）逐值相同。
                //   🔴 **次序 = `FitAfterSpacing`**（调度台裁定：另三扇里 2/3 用它；原版序列化字段不表达次序，
                //     两档静态收敛 —— 共件 `TitleFit` 的 doc 自陈「无牙口」）。
                //   🔴 **`TitleFitW/H` 不能省**（A968 落地时新发现的连带）：那两个缺省是 **0**，而
                //     `SetAutoFitBox` 头一句就是 `SetWrapWidth(worldW)` ⇒ 传 0 会把 TMP 的 `sizeDelta.x`
                //     写成 **0**（`Core/TmpFont.cs` 的 `SetWrapWidthRect`：`sizeDelta = (width, 0)` + 折行开 **Normal**）
                //     ⇒ `Daily Streak` 会**一个字一行**地竖排。值 = 原版那一颗自己的矩形宽高
                //     （`H_Title` = 155 … 534.30 × 38 … 120.66 ⇒ **379.30 × 82.66**，两处来源同一条：
                //      `menu_dump` 实读的 `Window Title` 矩形 + 本窗那条 `H_Title` 常量）。
                TitleMode = WindowHeader.TitleFit.FitAfterSpacing,
                TitleFitW = H_Title.W, TitleFitH = H_Title.H,
                // A712 阶段 2：原版 `Left/Capline` 的**纵向那一半**（共件里 `SetVAlign` 排在 `AlignLeft` 之后，
                // 与本窗原来的次序逐句相同）。
                TitleVAlign = Label.VAlign.Capline,
                WingRect = H_Bg,
                BackRect = H_Back,
                // 图**自己**就是按钮（`WindowButton` 挂在图上）—— A17：原版 `Header With Back Button>Header Back Button`
                // 是 SpriteSwap（普查 §块 4 第 10 行）；⚠️ 高亮图是 `UI_Button_Menu_Back_Hover`（**大写 H**，走那张表）。
                BackButtonStyle = WindowHeader.BackStyle.BoundOnQuad,
                BackKeepAspect = false,        // 本窗原版那颗 `Image` 是 `Simple`（另三扇是 `preserveAspect = 1`）
                BackSwapArt = ArtBackBtn,
                PlateTex = Art(ArtHeaderBg),
                BackTex = Art(ArtBackBtn),
                QPlate = QPanel, QWing = QPanel, QTitle = QText, QBack = QContent,
                // 原版：返回钮与背景遮罩**走同一个 `CloseButtonClicked` → `Close()`**，
                // 而 `Close()` = `LiveOp.TryCollect(() => base.Close())` ⇒ **关窗会先自动收取**。
                OnBack = () => { DailyData.StreakAutoCollect(); Close(); },
            });
        }

        /// <summary>一个奖格：`NormalReward` 那一组（`BG` / `Highlight` / 图标 / 名字 / 角标） + `Collect`。</summary>
        Transform BuildEntry(Transform parent, PxRect entry, int day)
        {
            var e = MenuDraw.Node(parent, "Daily Streak Reward Popup Entry (" + day + ")", entry);
            bool unlocked = DailyData.StreakRewardUnlocked(day);
            bool claimed = DailyData.StreakRewardClaimed(day);

            // 🔴 上面那些 `E_*` 是**格内局部坐标**（原点 = 奖格左上角）——
            //    本窗的奖格**会被 `scaleMultiplierFirstElement` 放大 1.2**，所以必须按
            //    「最终左上角 + 局部坐标 × k」映射（k = 最终宽 ÷ 设计宽），**不能当画布坐标直接用**
            //    （2026-09-23 踩到：所有奖格画到同一处，而断言的宽比对用的是「同一张 BG」⇒ 只报了一条不疼不痒的错）。
            float k = entry.W / EntryW;
            PxRect O(PxRect r) { return new PxRect(entry.x1 + r.x1 * k, entry.y1 + r.y1 * k,
                                                   entry.x1 + r.x2 * k, entry.y1 + r.y2 * k); }

            // 🔴 **2026-10-08（A182）：这一格里的每一件都改走带裁切的那条路**（`DrawRect` / 本文件的 `Text`）
            //    —— 它们都长在 `Viewport` 的 `RectMask2D` 之下，压在视口边上的那几格必须被真裁掉
            //    （原来走裸 `MenuDraw.Rect/Text`：`Rewards Scroll View` 之外的部分**照画出去**，而所有断言全绿）。
            DrawRect(e, Art(ArtBackOpaque), O(E_Bg), "BG", QPanel);
            var hl = DrawRect(e, Art(ArtHighlight), O(E_Highlight), "Highlight", QPanel, HighlightTint);
            if (hl != null) hl.gameObject.SetActive(unlocked && !claimed);
            DrawRect(e, Art(DailyData.StreakRewardIcon(day)), O(E_Holder), "Reward Holder", QContent, null, true);
            // 🆕 **2026-10-10（A1212 块 10）接上自适应**：原版那一颗四格 = `18 / 40 / 36 / 1`
            //   （出处 = `资料/普查产出_第十会话/R6_包装层战斗与其余.md` §3；归组 =
            //   `资料/普查产出_第十一会话/RA1212_切块表.md` 块 10 #33）。
            //   闸（`MenuDraw.TextCore`）：`wrapPx O(E_Name).W > 0` ∧ `18 > 0` ∧ `34.05×k > 18` ⇒ 全真。
            //   ⚠️ `折行 = 1` ⇒ 不用还原。⚠️ 用**命名实参**（`WindowsManager.Text` 的 `align` 是 `int`
            //   且排在 `autoMaxPx` 前面，位置一错会**静默**绑错 —— 同 `Collect Text` 那条注释）。
            //   ⚠️ 三格**不乘 `k`**：本族已接的先例 = `Shell/CampaignRewardWindow.cs` 的
            //   `WarnAutoMin/WarnAutoMax/WarnAutoBase`（同一条 `GameWindow.Text` 的路，全按原版画布 px **原样传**）。
            var lbRwName = Text(e, O(E_Name), DailyData.StreakRewardName(day), Color.white, "Reward Name",
                                34.05f * k, QText, wrapPx: O(E_Name).W, autoMinPx: 18f,
                                autoMaxPx: 40f, autoBasePx: 36f);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版
            //   `Daily Streak Reward Popup Entry/Reward Name` = `对齐=Center/**Midline**`（fs34.05）
            //   （判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Daily Streak Reward Popup Entry" --depth 4 --md`；
            //    ⚠️ 横向是 `Center` ⇒ **不给 `align`**，那一半 A679 已核过）。
            MenuDraw.SetVAlign(lbRwName, Label.VAlign.Midline, O(E_Name));
            // `Extra Reward Indicator`：**本窗出厂是 true**（每日奖励窗那份是 false）⇒ 照画
            DrawRect(e, Art(ArtExtra), O(E_Extra), "Extra Reward Indicator", QContent);

            // `Collect`（色 (0.06,0.57,0.13,1) + 'Claim'；整组的 `scl=0.7` 已经烘进上面那些矩形里，
            // 但 TMP 的 `m_fontSize` 是**未缩放**的值 ⇒ 字号要自己乘 0.7）
            if (unlocked && !claimed)
            {
                var c = DrawRect(e, Art(ArtClaim), O(E_Collect), "Collect", QContent, ClaimTint);
                // 🔴 **2026-10-16（A679）**：补 **`align: 1`（左对齐）** —— 原版那一颗的 TMP
                //    是 **`对齐=Left/Midline`**，我们原来没传 ⇒ `Label` 默认把文字**居中**在框心（`align = 0`）。
                //    判据（**现读**，⛔ 不是抄表）：`工具/menu_dump.py bundle_menus_assets_all
                //    "Daily Streak Reward Popup Entry" --depth 4` ⇒
                //    `Collect Text  … 'Claim' 字号=52.85 对齐=Left/Midline`；回读原字段 =
                //    `P/MonoBehaviour/MonoBehaviour_-3990580155211047807.json` 的 `m_HorizontalAlignment = 1`。
                //    ⚠️ **同格里的 `Reward Name`(:577) 原版是 `Center`**（`MonoBehaviour_-6745572749593994111.json`
                //    = 2）⇒ 那一处**不传才对**，⛔ 别顺手给它也补一个 `align`（补了反而错）。
                //    ⚠️ 两套枚举**只有 `1` 同义**（原版 TMP `Left=1 · Center=2 · Right=4`；
                //    本仓 `GameWindow.Text` 的 `align` = `0=居中 · 1=左 · 2=右`）——
                //    对照段与 A635 那次踩坑 → `Shell/WindowsManager.cs` 的 `align` 形参注释 ·
                //    `资料/普查产出_1016/A679_align全表.md` §一·A / §三。
                //    ⚠️ 用**命名实参**（⛔ 别把它插到 `autoMaxPx` 前面当位置实参）——
                //    `int` 字面量能隐式转 `float`，位置一错会**静默**绑成 `autoMaxPx`、对齐退回 0。
                var lbCollect = Text(e, O(E_CollectText), DailyData.StreakClaimText(), Color.white, "Collect Text",
                     52.85f * 0.7f * k, QText, wrapPx: O(E_CollectText).W, autoMinPx: 18f,
                     autoMaxPx: 72f, autoBasePx: 36f, align: 1);
                // 🆕 **2026-10-10（A1212 块 10）接上自适应**：原版那一颗四格 = `18 / 72 / 36 / 1`
                //   （出处 = `RA1212_切块表.md` 块 10 #34）；闸全真（`52.85×0.7×k ≈ 37 > 18`）。
                //   ⚠️ `折行 = 1` ⇒ 不用还原。⚠️ 三格**不乘 `k`**（理由同 `Reward Name` 那一处）。
                // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 同一行 dump 实读 `对齐=Left/**Midline**`。
                MenuDraw.SetVAlign(lbCollect, Label.VAlign.Midline, O(E_CollectText));
                if (c != null)
                {
                    var hit = c.gameObject.AddComponent<WindowButton>();
                    int d = day;
                    // 🆕 **2026-10-11（批次1 · W1 · A309）**：这一下现在**会弹原版那扇 `Reward Window`**
                    //   （开窗在 `DailyData.CollectStreak` 里面 = 我们唯一的发奖口那一处；原版
                    //   `RewardService.Collect` 就是「发完奖 → 开一扇 Collect 态的全屏领奖窗」）。
                    hit.onClick = () => DailyData.CollectStreak(d);
                }
            }
            return e;
        }

        /// <summary>绕矩形**中心**缩放（原版 `localScale` 的几何效果；该 prefab 根的 pivot 实测 (.5,.5)）。</summary>
        static PxRect ScaleAbout(PxRect r, float s)
        {
            float cx = (r.x1 + r.x2) * 0.5f, cy = (r.y1 + r.y2) * 0.5f;
            return new PxRect(cx + (r.x1 - cx) * s, cy + (r.y1 - cy) * s,
                              cx + (r.x2 - cx) * s, cy + (r.y2 - cy) * s);
        }

        // ⚠️ **本窗原来在这里就地抄了一份 `Text(...)` 薄包装**（收「整块在视口外 ⇒ 不建 / 压在边上 ⇒ 裁」
        //    那三步）。🆕 **2026-10-11（A241）已上移到共同基类 `GameWindow.Text`**（`Shell/WindowsManager.cs`）
        //    —— 本文件的**调用点一个都没改**（同一个 `Text(...)`，现在解析到基类那一份）。
        //    ⛔ 别在本文件再抄回来：那三份逐字相同的副本正是 A241 要收掉的东西
        //    （判据 → `资料/待办判据_1008.md` §A241）。

        Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        public string Dump()
        {
            return $"DailyStreak：{(HasFailed ? "断签态" : "连胜态")} · {entries.Count} 个奖格 · 取不到的图 {MissingArt.Count} 张";
        }
    }
}
