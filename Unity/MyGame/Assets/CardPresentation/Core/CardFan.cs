// CardFan.cs — 「1 主卡 + N 相关卡」那一叠的**扇形位姿 · 分层 · 着色**（判据只此一份）
//
// 为什么单开一个文件：**原版是同一个脚本 `CardDisplayWindow`、两处摆放** ——
// 菜单版 `Shell/CardDetailPopup` 与战斗版 `Battle/CardDisplayWindow` 都要用这组数，
// 写两份迟早不一致（项目准则：「判据要共用一份」）。判据全文 → `资料/阶段二_卡片详情窗_原版规格.md` §8·7。

using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class CardFan
    {
        // ============================================================ 扇形位姿（真值）
        //
        // 出处：`CardDisplayWindow.displayAnimation`（MB_2033 → `Animation` PathID 623，`m_PlayAutomatically=true`）
        //   → clip **`Card Display Open`**
        //   = `d:/2/新解包资源/assets_full/bundle_duplicateassetisolation_assets_all/AnimationClip/AnimationClip_1560242982351263260.json`
        //   （legacy · 60fps · 0.3167s / 19 帧 · events 空）。
        //
        // 🔴 **2026-09-28 实测：战斗版和菜单版引的是【同一条 clip】（同一个 pid）** ⇒ **两扇窗同一套数**：
        //   · `bundle_scenes_scenes_battlearena1/Animation/Animation_1608.json` 的
        //     `m_Animations[0] = {m_FileID:8, m_PathID:1560242982351263260}`，挂在 `Card Display Window`(GO 419) 上；
        //   · 那条 clip 里 5 条 scale/rotation 曲线 + 10 条 anchoredPosition 浮点曲线的 `path`
        //     正好命中战斗树 `Card Display/Cards/CardUI (0..4)`；
        //   · 那 5 个槽的 `m_LocalScale` **全是 250**（`RectTransform_2874/2772/2908/3170/2663.json`），
        //     `Cards`/`Card Display`/`Safe area FrontCanvas` 一路父级 scale 都是 1。
        //   ⇒ 正本 §〇·2 原来写的「菜单 250 / 战斗 223.14」**是错的**：那个 `223.14` 的 `CardUI Reference`
        //     属于**另一棵树** `Safe area FrontCanvas/Generic Multi Card Display Combat/Viewport/Content/`
        //     （多卡展示窗的模板，`RectTransform_3421.json`）—— 见正本 §十·4，2026-09-28 已就地订正。
        //
        // ⚠️ 下面抄的是 **clip 的终态（t=0.3166667）**；起点是 `(0,60) · 0° · scale 210`（19 帧补间）。
        // ⚠️ 槽 0–4 = **原版真值**（原版只有 5 格 = 1 主卡 + `RawCardScript.relatedCard1..4`）；
        //    **槽 5–8 是我们外推的**（用户 2026-09-26 授权做 8 个相关卡槽）：
        //    走向照原版（继续往左、继续变斜、继续缩小），**步长取原版最后一档的一半**
        //    （否则第 8 格会跑出屏幕左边缘）。
        static readonly float[] _x = { 0f, -121f, -232f, -332f, -432f, -482f, -532f, -582f, -632f };
        static readonly float[] _y = { 60f, 53f, 49f, 45f, 27f, 24f, 21f, 18f, 15f };
        // 🔴 槽 3 是 **12.750°**（`z=0.11103530 w=0.99381644` ⇒ `2·atan2(z,w)`）。
        //    本工程 2026-09-27 记的是 `12.648` —— **2026-09-28 从 clip 原文复算出是 12.750，已订正**。
        static readonly float[] _rot = { 0f, 2.510f, 5.890f, 12.750f, 19.200f, 22.5f, 25.8f, 29.1f, 32.4f };
        static readonly float[] _scl = { 250f, 232.9537f, 221.5926f, 204.5463f, 181.8148f, 170.8f, 159.8f, 148.8f, 137.8f };

        /// <summary>卡位个数：**1 主卡 + 8 相关卡**（原版是 1+4；**8 这一档是我们挑的**，用户授权的冗余）。</summary>
        public const int Slots = 9;

        /// <summary>换位动画时长 —— 原版 `CardDisplayWindow.relatedCardSwapTime`（字段 `+0xC0`，
        /// `dump.cs` 里字段名与偏移逐一核过）。</summary>
        public const float SwapTime = 0.25f;

        // ============================================================ 开窗那一拍（原版 `Card Display Open`）
        //
        // 🔴 **2026-10-16（本会话）：逐值从那条 legacy clip 的原文读出**（⛔ 不是我们挑的）。
        //   出处同文件头那一份：`bundle_duplicateassetisolation_assets_all/AnimationClip/`
        //   `AnimationClip_1560242982351263260.json`（`m_Name = "Card Display Open"` · `m_Legacy = true` ·
        //   `m_SampleRate = 60` · `m_WrapMode = 1`(Clamp) · `m_Events` 空 · `m_MuscleClipSize = 0`）：
        //   · **20 条曲线** = `m_RotationCurves` 5 + `m_ScaleCurves` 5 + `m_FloatCurves` 10（两条位移/槽），
        //     `path` 正好命中那 5 个槽（`Card Display/Cards/CardUI` 与 `CardUI (1..4)`）；
        //   · 每条都从 **t = 0** 到 **t = 0.3166666626930237**（20 个关键帧 = 19 帧 @60fps）；
        //   · **起点（t=0）5 个槽一模一样**：`m_AnchoredPosition = (0, 60)` · 四元数 = 单位（euler z 0°）
        //     · `m_LocalScale = 210`；
        //   · **缓动 = 平滑步 `t²(3−2t)`**：那 10 条位移 + 5 条缩放是「2 键 + 两端 `inSlope/outSlope` 全 0」
        //     的赫米特 ⇒ **正好退化成平滑步**；5 条旋转是 60fps 采样出来的 20 键，
        //     本会话逐键复算：**与平滑步的偏差 ≤ 1e-6 度**（且 4 条槽的归一化进度逐键相同，spread ≤ 2e-7）
        //     ⇒ 20 条曲线**同一条缓动**。
        //
        // 挂载面（照原版实读）：窗根 `Card Display Window` 上那颗 `Animation`
        //   （`bundle_scenes_scenes_battlearena1/Animation/Animation_1608.json`）：
        //   `m_Animations` **只有 1 条**（就是这条 clip）· `m_PlayAutomatically = true` · `m_WrapMode = 0`(Once)
        //   ⇒ **窗口一亮就播、播完停在终态**；`CardDisplayWindow__Close.c` 里**没有**第二条 clip
        //   ⇒ **原版只有开窗动画、没有关窗动画**（⛔ 别自己补一条关窗的）。
        // ⚠️ 本工程**不建**那颗 `Animation` 组件：legacy `AnimationClip` 从 bundle 搬进工程会变空壳
        //   （`资料/已知的坑.md`「bundle 资产搬进工程的两条死路」）⇒ 等价物是**代码补间**
        //   （`Battle/CardDisplayWindow.PlayOpenAnimation`），曲线数值就用下面这几个常量。

        /// <summary>开窗补间时长 = clip 最后一个关键帧的 `time`：**0.3166667 s**（60fps · 19 帧）。</summary>
        public const float OpenTime = 0.3166667f;
        /// <summary>开窗起点缩放 —— clip 里 5 条 `m_ScaleCurves` 在 t=0 一律 `m_LocalScale = 210`。
        /// ⚠️ 量纲：这是**原版 localScale**；落到我们这套要按「卡体 px ÷ (`CardView.Height`×108)」换算
        /// （见 <see cref="OpenStartHpx"/>）。</summary>
        public const float OpenStartScale = 210f;
        /// <summary>开窗起点 `m_AnchoredPosition`（10 条位移曲线在 t=0 一律 `(0, 60)`）。</summary>
        public const float OpenStartAnchoredX = 0f, OpenStartAnchoredY = 60f;
        /// <summary>开窗起点那一拍**卡心**的屏幕 px —— `Cards` 的屏心是 (960,540)、原版 y 向上 ⇒ 取负。</summary>
        public const float OpenStartCx = 960f + OpenStartAnchoredX;
        public const float OpenStartCy = 540f - OpenStartAnchoredY;
        /// <summary>开窗起点那一拍**卡体**的宽/高（px）= 卡单位 `2.0927×3.3313` × `OpenStartScale`。</summary>
        public const float OpenStartWpx = 2.0927f * OpenStartScale;      // 439.467
        public const float OpenStartHpx = 3.3313f * OpenStartScale;      // 699.573
        /// <summary>开窗起点那一拍的倾角 = **0°**（5 条旋转曲线的 t=0 都是单位四元数）。</summary>
        public const float OpenStartRot = 0f;

        /// <summary>开窗那条缓动曲线：**平滑步 `t²(3−2t)`**（出处见上面那一块）。
        /// <para>🔴 **它不是 DOTween 的 `Ease.InOutQuad`**（那个是 `t&lt;0.5 ? 2t² : 1−2(1−t)²`，形状不同）
        /// ⇒ 调用方一律 `Ease.Linear` 建 tween、**在这一个函数里自己做这条曲线**
        /// （⛔ 别改成 `SetEase(Ease.InOutQuad)` 或 `InOutSine` —— 那都不是原版这条）。</para></summary>
        public static float OpenEase(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>槽名：**前台那张不带后缀**，其余 `CardUI (1..8)`（照原版命名）。</summary>
        public static string SlotName(int i) { return i == 0 ? "CardUI" : "CardUI (" + i + ")"; }

        /// <summary>点在哪一格（第 `i` 格）就把它换到前台。</summary>
        public static float X(int i) { return _x[i]; }
        public static float Y(int i) { return _y[i]; }
        public static float Rot(int i) { return _rot[i]; }

        /// <summary>第 `i` 格相对**前台那张**的倍率（原版 `localScale` / 250）。</summary>
        public static float Rel(int i) { return _scl[i] / _scl[0]; }

        /// <summary>前台那张的**卡本体**尺寸（px）= `2.0927×3.3313` × 250
        /// （卡体单位来自 `RectTransform_3438.json` 的 `m_SizeDelta`）。</summary>
        public const float FrontWpx = 2.0927f * 250f;      // 523.175
        public const float FrontHpx = 3.3313f * 250f;      // 832.825
        /// <summary>第 `i` 格的卡本体宽/高（px）。</summary>
        public static float Wpx(int i) { return FrontWpx * Rel(i); }
        public static float Hpx(int i) { return FrontHpx * Rel(i); }
        /// <summary>第 `i` 格中心的屏幕像素坐标（1920×1080 · 左上原点 · y 向下）。
        /// 原版 `anchoredPosition` 的 y 向上 ⇒ 取负。</summary>
        public static float Cx(int i) { return 960f + _x[i]; }
        public static float Cy(int i) { return 540f - _y[i]; }

        // ============================================================ 分层（原版没有，我们必须补）
        //
        // 🔴 `CardView` 把**每一层**都硬编码成渲染队列 3000 ⇒ 多格叠在一起时「谁盖谁」就只剩
        //    「到相机的距离」在排，而那**不可控**：2026-09-27 实拍抓到**后面那张的卡名画到了
        //    前面那张的立绘之上**，而断言全绿。做法 = **每格一个独立队列**（前台最高），
        //    并把**整格所有层一起平移**（卡内层序靠 z 偏移，整体平移不破坏它）。
        //    `3009–3019` 这 11 个号是空的（避让 `LiveOpsEventWindow` 3104-3116 与详情窗自己的 3110-3113）。
        public const int QCardBase = 3009;

        // ------------------------------------------------------------ 点击区（原版 `CardUI/2DCard/UI Collider`）
        //
        // 🔴 **判据 = 解包原件字段**（2026-10-07 现读 · 第一权威），两棵树**逐字段相同**：
        //   · 战斗 `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2801.json`
        //   · 菜单 `bundle_scenes_scenes_mainmenuwarpforge/RectTransform/RectTransform_1220.json`
        //   两颗 `UI Collider`：`m_AnchorMin=(0,0)` · `m_AnchorMax=(1,1)`（**拉伸锚**）· `m_Pivot=(0.5,0.5)`
        //                   · `m_AnchoredPosition=(0, −0.02)` · `m_SizeDelta=(−0.2, −0.44)`
        //   父件 `2DCard`（`RectTransform_2876` / `RectTransform_1323`，两棵同为
        //     `anchor(0.5,0.5)` · `ap(0,0)` · **`m_SizeDelta = 2.0927 × 3.3313`**（卡单位））
        //   ⇒ 点击区 = `(2.0927−0.2) × (3.3313−0.44)` = **1.8927 × 2.8913** 卡单位，
        //     中心比卡心**低 0.02 卡单位**（原版 y 向上 ⇒ 屏幕 y 向下 ⇒ **下移**）。
        //   ⇒ 前台（scale 250）：**473.175 × 722.825** px（卡本体 523.175 × 832.825）
        //     · x 比 = 1.8927 / 2.0927 = **0.9044298** · y 比 = 2.8913 / 3.3313 = **0.8679200**
        //     · 四边内缩（px @ 前台）：**左 25 · 上 60 · 右 25 · 下 50**
        //
        // 🔴 **两轴的比例【不一样】** —— 原版那两个 `m_SizeDelta` 分量是**绝对卡单位**，不是百分比。
        //   ⚠️ 2026-10-07 之前这里只有一个 `HitRatio = 0.9043`（那是 **x** 那个比）**双轴同用 + 居中**
        //   ⇒ y 轴**多吃「上 20.149 / 下 10.149」px** 的两条窄带，而那两条窄带**在原版会关窗**
        //   （A156；推导 → `资料/普查产出_1006/A132_A133_两扇窗.md` §四·4）。
        //   ⇒ **别再退回「一个比例双轴同用」**：两轴各一个常数。
        // ⚠️ 碰撞按**未旋转**的矩形判（同原版：吃射线的是这颗矩形本身，与卡自己的转角无关）。
        public const float HitRatioX = 0.90443f;   // 1.8927 / 2.0927
        public const float HitRatioY = 0.86792f;   // 2.8913 / 3.3313

        /// <summary>点击区中心相对**卡心**的屏幕**下移**（px @ 前台 scale 250）
        /// = 原版 `m_AnchoredPosition.y = −0.02` × 250。**两扇窗共用这一个数**。</summary>
        public const float HitCyDropPx = 5f;

        /// <summary>第 `i` 格点击区的宽 / 高（px）。**判据只此一份** —— 两扇窗都调它，别再各写一份比例。</summary>
        public static float HitW(int i) { return Wpx(i) * HitRatioX; }
        public static float HitH(int i) { return Hpx(i) * HitRatioY; }

        /// <summary>第 `i` 格点击区**中心**的屏幕 y（px · 左上原点）= 卡心 + `HitCyDropPx`×该格缩放。
        /// ⚠️ **不是 `Cy(i)`** —— 原版那颗 `ap.y = −0.02` 让它比卡心低（前台 5px）。</summary>
        public static float HitCy(int i) { return Cy(i) + HitCyDropPx * Rel(i); }

        /// <summary>把**整格卡**的所有层搬到同一个队列 `q`。
        ///
        /// 🔴 **2026-10-12（`A1665`）：本方法已收口成 `CardView.SetQueue` 的转发** —— 实现只有那一处
        ///    （⛔ 别在这儿再写第二份：两处写同一条规则 = 迟早不一致）。
        ///    原来它自己遍历 `GetComponentsInChildren` 并 `mr.material.renderQueue = q`，那段逻辑
        ///    **逐字搬进了 `CardView.SetQueue`** —— 行为对本方法的 4 个调用点（战斗「点卡看详情」
        ///    `Battle/CardDisplayWindow.cs` · 菜单详情窗 `Shell/CardDetailPopup.cs` ·
        ///    `Shell/BoosterPackOpenWindow.cs` · 日志悬停卡 `Battle/BattleDriver.cs`）**逐位不变**。
        ///
        /// ⚠️ 但**语义**上两边现在是同一套：一张卡 = 一段队列，卡间由布局给不同的号
        ///    （手牌那一段见 `HandLayout.queueTop`）。所以 `QueueFor` 那条 **stride = 1** 仍然成立
        ///    —— 「一张卡一个号」两边一致，不会互相踩。
        /// 🔴 `.material` 那条坑（写共享材质会把 `Resources/Fonts/Warpforge Trait TextSprites.asset`
        ///    的 `m_CustomRenderQueue` 写脏并落盘）的解释也随实现搬进了 `CardView.SetQueue`。</summary>
        public static void SetCardQueue(CardView v, int q)
        {
            if (v == null) return;
            v.SetQueue(q);      // 唯一的那一份实现在 `CardView.SetQueue`
        }

        /// <summary>第 `i` 格的渲染队列（**前台最高**）。`baseQ` = 这叠卡的起始号
        /// —— **两扇窗各用自己那段**（战斗窗 `QCardBase`；菜单详情窗 `CardDetailPopup.QCardStack`，
        /// 那一段必须落在它自己的遮罩之上）。</summary>
        public static int QueueFor(int i, int count, int baseQ) { return baseQ + Mathf.Max(0, count - 1 - i); }

        /// <summary>按**当前槽位**把整叠卡的队列重排一遍。**换位之后必须再调一次** ——
        /// 队列是挂在「那一格视图」上的，而换位只换了位姿与数组 ⇒ 不重排的话
        /// 「刚换到前台的那张」还带着它**原来那个较低的号**、会被身后的卡盖住。
        /// （原版那一步是 `SetSiblingIndex` 两两互换 —— 目的相同：让现在站在前台的画在最上面。）</summary>
        public static void ApplyQueues(CardView[] views, int count, int baseQ)
        {
            if (views == null) return;
            for (int i = 0; i < Slots && i < count; i++)
                SetCardQueue(views[i], QueueFor(i, count, baseQ));
        }

        // ============================================================ 着色（2026-09-28 新补）
        //
        // 🔴 原版：**前台那张是白的、其余（相关卡）压暗到 `cardInBackGroundColorTint`**，
        //    换位时按 `relatedCardSwapTime`（0.25s）**补间换色**。
        // 出处（两条独立证据）：
        //   · `CardDisplayWindow__InitializeCardForDisplay.c:73-97`：`if (param_3 != 0)`（不是前台槽）
        //     ⇒ 取窗口 `+0xC4..+0xD0` 当颜色；前台槽走 `_DAT_1834b2e50`；
        //   · `CardDisplayWindow__ChangeCardPosition.c:203-221`：换位收尾那两条 tween ——
        //     去前台那张用 `_DAT_1834b2e50`、让出前台那张用 `+0xC4`。
        // 两组数**当场解出来**（`RVA = 地址 − ImageBase(0x180000000)` → PE 节表 → 文件偏移）：
        //   `_DAT_1834b2e50/54/58/5c` = **(1,1,1,1)**；`+0xC4` = `cardInBackGroundColorTint`。
        // 字段名与偏移**逐字核过**：`d:/2/tools/il2cpp_out/dump.cs` = `private Color cardInBackGroundColorTint; // 0xC4`，
        // 值 = **(0.65, 0.65, 0.65, 1)**（MB_2033 序列化字段，正本 §一 已记）。
        public static readonly Color FrontTint = new Color(1f, 1f, 1f, 1f);
        public static readonly Color BackTint = new Color(0.65f, 0.65f, 0.65f, 1f);

        /// <summary>按「是不是前台」给一格上色（**要在 `SetHighlight` 之后再调** ——
        /// `SetHighlight` 自己也写 `SetTint`，谁后调谁生效）。</summary>
        public static void ApplySlotTint(CardView v, bool front)
        {
            if (v == null) return;
            v.SetTint(front ? FrontTint : BackTint);
        }

        /// <summary>把一格的颜色**补间**到目标色（原版换位那两条 tween，时长同为 `relatedCardSwapTime`）。
        /// 起步先把起点色写上 —— 否则从「当前色」起步，连点两次时会跳。</summary>
        public static void TweenTint(CardView v, Color from, Color to, float dur)
        {
            if (v == null) return;
            v.SetTint(from);
            float t = 0f;
            var tw = DG.Tweening.DOTween.To(() => t, x => { t = x; v.SetTint(Color.Lerp(from, to, x)); }, 1f, dur);
            CardTween.Use(tw, DG.Tweening.Ease.OutQuad, v);
        }
    }
}
