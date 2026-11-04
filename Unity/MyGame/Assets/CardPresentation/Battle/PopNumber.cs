// PopNumber.cs — 战斗 HUD 的数值飘字（伤害 / 治疗 **两档分开**）
//
// 为什么有这个文件（`A1213②`，2026-10-17）
// ----------------------------------------
// 旧实现只有 `Core/CardFeel.PopNumber(..., bool heal)` **一颗函数兼两档**，字号一律 `SetCapHeight(0.30f)`。
// 原版是**两颗独立的 TMP 组件**，四格**逐值不同**：
//
//   · DamageText = `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_5890981294211439552`
//       `autoSizing=1 · m_fontSize=5.5 · m_fontSizeMin=0.35 · m_fontSizeMax=5.5 · m_fontSizeBase=36`
//       `m_TextWrappingMode=0（NoWrap）· HAlign=2（Center）· VAlign=512（Middle）· m_fontColor=白`
//       `m_isOrthographic = 0` ⇒ **TMP 渲染系数 ×0.1**（`TMP_Text.cs:7183`；见下面那条订正）
//       它的 **RectTransform 自己** `m_LocalScale = 1.54`
//       （`RectTransform/RectTransform_5482325324160211904.json`）
//   · HealText  = `…/MonoBehaviour_-7677963611639080000`
//       `autoSizing=0 · m_fontSize=0.7 · m_fontSizeMin=0.35 · m_fontSizeMax=1.4 · m_fontSizeBase=36`
//       `m_TextWrappingMode=0 · Center/Middle · 白`
//       `m_isOrthographic = 1` ⇒ ×1.0
//       它的 RectTransform（`…_-4069981099246511168.json`）`m_LocalScale = 1.0`
//
// 🔴 **2026-10-18（`N54②`）就地订正（铁律 5）—— 旧账写的「12.1 : 1」是【错的】。**
//    错因：那两颗是 **3D `TextMeshPro`**（不是 UGUI 的 `TextMeshProUGUI`），
//    而 TMP 的渲染系数里还有一跳 **`m_isOrthographic`**：
//      `fontScale = fontSize/pointSize × scale × (m_isOrthographic ? 1 : 0.1)`（`TMP_Text.cs:7183`）
//    ⇒ Damage 的 1 em（卡局部）= `5.5 × 0.1 × 1.54` = **0.847**（不是 `5.5 × 1.54` = 8.47）；
//       Heal 的 1 em = `0.7 × 1.0 × 1.0` = **0.700**
//    ⇒ **有效字号比 = 0.847 : 0.700 = 1.21 : 1** —— 旧账丢的正是那 10 倍。
//    ⚠️ 旧版的 `HealCapWorld = Damage × 0.7 ÷ (5.5 × 1.54)` 是同一个错的第二个受害者：
//      少了 `isOrthographic` 那一跳 ⇒ 治疗飘字被算成 **小 10 倍**（屏幕 2.67 px，原版 30.1 px）。
//    判据全文 → `资料/普查产出_1018/N54_飘字绝对字号_判据.md` §一~§五。
//
// 我们这一侧的锚（`N54②` 起：**绝对字号也照原版**，不再是「我们挑一个基准」）
// ------------------------------------------------------------------------
// 换算链（判据 §三，逐跳都有出处；本文件 §「原版字面量」那一段把它写成了可核的常量式）：
//   ① 1 em（卡局部）= 两颗各自的 `fontSize × isOrtho 系数 × 自己 RT 的 localScale`
//        ⇒ Damage **0.847**（5.5×0.1×1.54） · Heal **0.700**（0.7×1.0×1.0）
//   ② × **大写墨高 ÷ em = 0.656**（原版 Pragati-Regular SDF；与本工程 `Label.SetCapHeight` 同一口径）
//        ⇒ 大写高 = 0.556 / 0.459 卡局部单位
//   ③ × 【卡局部 → 我们世界单位】的桥 = **0.6071** = 场卡 `desiredScale` 0.36 × `UnitsToOurs`（182.14/108）
//        （判据 §三·5：我方场卡行 0.36 × 182.14 = 65.57 px/卡单位 = 0.6071 × 108 px/世界单位）
//   ⇒ **DamageCapWorld = 0.3374**（原版我方场卡 36.4 px）· **HealCapWorld = 0.2788**（30.1 px）
//   🔴 **2026-10-19（B4 · `N54②` 三条尾巴）**：
//      ① 自适应框改走**卡局部→我们世界**那一档（原来漏了 `desiredScale` ⇒ 框 2.78 倍）；
//      ② 字体面显式 `TmpFont.Face.LatinCardBody`（Pragati —— 原版那两颗 TMP 的 `m_fontAsset`）；
//      ③ 敌方行按 `e.Player` 取第二档锚（`EnemyBoardPxPerUnit 86.2` ⇒ 59.48/65.57 = 0.9071）。
//
// 🔴 **口径选择：用【墨高口径】(0.337)，⛔ 不用 `faceInfo.capLine` 那一档 (0.325)。**
//    理由：我们这一侧 `Label.SetCapHeight(h)` 的实现是 `fontSize = h / TmpFont.CapPerFontSize(face)`，
//    而 `CapPerFontSize` 量的是 `glyph.metrics.height + padding × 2`（**字形墨高**，不是 `capLine`）
//    ⇒ 只有喂【墨高】才能让屏幕上的墨高与原版相等。喂 capLine 那一档（0.325）实绘墨高会小
//    `1 − 0.6316/0.656` = **3.7%**（旧 0.30 那一版比原版小 11.0%）。
//    ⚠️ 两个口径的**比值完全相同**（都是 0.847 : 0.700）⇒ 上面那条 1.21 : 1 不受这个选择影响。
//
// ⚠️ **原版 Heal 的 min/max/base（0.35 / 1.4 / 36）是 `autoSizing=0` 下的死值**（从不参与收敛）
//    ⇒ 这里**不写**：写它们就得先把自适应打开，反而**改掉**原版行为（渲染结果会变）。
//    Damage 那三格是真在用的（`autoSizing=1`）⇒ 按「1 个原版 fontSize 单位 = 多少画布 px」折进去。
//
// ⚠️ `SetAutoFitBox` 内部会调 `SetWrapWidth`（无条件把折行设成 `Normal`）⇒ 原版 `wrap=0`
//    必须在它**之后**用 `SetWrappingMode(0)` 还原（同 `Label.SetWrapping` 的 doc 那条）。
//
// 时间轴照旧 = 原版 `InBattleDamageCounter Variation 1`（`CardFeel.PopIn/PopScaleIn/PopHoldUntil/PopOut`）。

using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    public static class PopNumber
    {
        // ================= 原版字面量（⛔ 改一个值就要同批改注释与断言） =================

        /// <summary>DamageText 的 `m_fontSize`。</summary>
        public const float DamageFontSize = 5.5f;
        /// <summary>DamageText 的 `m_fontSizeMin`。</summary>
        public const float DamageFontSizeMin = 0.35f;
        /// <summary>DamageText 的 `m_fontSizeMax`。</summary>
        public const float DamageFontSizeMax = 5.5f;
        /// <summary>DamageText 的 `m_fontSizeBase`。</summary>
        public const float DamageFontSizeBase = 36f;
        /// <summary>DamageText 的 `m_enableAutoSizing`（原版 = 1）。</summary>
        public const bool DamageAutoSizing = true;
        /// <summary>DamageText **自己**那颗 RectTransform 的 `m_LocalScale`（§二·7「旧账漏了的那一跳」）。</summary>
        public const float DamageLocalScale = 1.54f;
        /// <summary>DamageText 的 `m_isOrthographic`（原版 = **0**）⇒ TMP 渲染系数 **×0.1**
        /// （`TMP_Text.cs:7183` 的 `(m_isOrthographic ? 1 : 0.1f)`）。
        /// 🔴 `N54②` 之前**漏了这一跳** ⇒ 治疗被反算成小 10 倍，见文件头「就地订正」。</summary>
        public const float DamageOrthoScale = 0.1f;
        /// <summary>DamageText 的 `m_TextWrappingMode`（0 = NoWrap）。</summary>
        public const int DamageWrappingMode = 0;

        /// <summary>HealText 的 `m_fontSize`。</summary>
        public const float HealFontSize = 0.7f;
        /// <summary>HealText 的 `m_fontSizeMin`（**死值** —— `autoSizing=0`，见文件头）。</summary>
        public const float HealFontSizeMin = 0.35f;
        /// <summary>HealText 的 `m_fontSizeMax`（**死值**）。</summary>
        public const float HealFontSizeMax = 1.4f;
        /// <summary>HealText 的 `m_fontSizeBase`（**死值**）。</summary>
        public const float HealFontSizeBase = 36f;
        /// <summary>HealText 的 `m_enableAutoSizing`（原版 = 0）。</summary>
        public const bool HealAutoSizing = false;
        /// <summary>HealText 的 RectTransform `m_LocalScale`（= 1.0）。</summary>
        public const float HealLocalScale = 1f;
        /// <summary>HealText 的 `m_isOrthographic`（原版 = **1**）⇒ TMP 渲染系数 ×1.0。</summary>
        public const float HealOrthoScale = 1f;
        /// <summary>HealText 的 `m_TextWrappingMode`（0 = NoWrap）。</summary>
        public const int HealWrappingMode = 0;

        /// <summary>两颗 RT 的 `m_SizeDelta`（同一份：`0.800961… × 0.683734…`）。
        /// Damage 用之前还要乘它自己的 1.54；再过 `CardLocalToOurs`（= `desiredScale × UnitsToOurs`）折成我们的世界单位。
        /// 🔴 **2026-10-18（`N54②`）曾记的偏离（✅ 2026-10-19 B4 已修，判据复核无误）**：`BoxW/BoxH` 是**卡局部**单位，
        /// 而原来那两行用的是 `CardFeel.UnitsToOurs`（**棋盘层**单位 → 我们的世界，182.14/108）
        /// ⇒ 少乘了 `desiredScale` 0.36（本文件 `CardLocalToOurs`）。修前框是原版的 **2.78 倍**
        /// （224.7 × 191.8 px vs 80.9 × 69.0 px），后果 = 伤害那档的自适应**永不收缩**。
        /// 现在 `Pop()` 走 `CardLocalToOursFor(enemy)`（玩家 0.6071 / 敌方 0.5507）。</summary>
        public const float BoxW = 0.8009615f, BoxH = 0.6837339f;

        /// <summary>红色血痕（原版 `3DBody/DamageCounter/DamageIcon`）**自己的**局部缩放曲线的两个端点 ——
        /// 判据 = clip `InBattleDamageCounter Variation 1` 的 `m_ScaleCurves[path="DamageIcon"]`：
        /// **0.5763395 @0.1167 → 0.7 @0.2**（之后保持到 1.8333）。
        /// 🔴 **2026-10-21（W61 第 1 批 · 第 5 条）**：原来固定用 prefab 原值 `1.01665`（不跑曲线）⇒
        /// 实拍比原版大 **45%~76%**、也没有那一下 pop。⛔ 别写回 `1.01665`。
        /// ⚠️ prefab 上那个 `1.01665` 只在**动画不播**时才可见（原版有 Animator ⇒ 曲线覆盖它）。</summary>
        public const float DamageIconScaleFrom = 0.5763395f;
        /// <summary>血痕缩放的终点（同 <see cref="DamageIconScaleFrom"/>）。</summary>
        public const float DamageIconScaleTo = 0.7f;

        // ================= 我们这一侧 =================

        /// <summary>1 em 的**卡局部单位**值 = `fontSize × (isOrthographic ? 1 : 0.1) × 自己 RT 的 localScale`
        /// （原版 TMP 渲染链，判据 §三·2~3）。
        /// Damage = `5.5 × 0.1 × 1.54` = **0.847** · Heal = `0.7 × 1.0 × 1.0` = **0.700**。
        /// 🔑 **那个 ×0.1 就是我们没有的概念**：我们的 `Label` 是世界空间的 3D `TextMeshPro`
        /// （`TmpFont` 的字形标定就是拿 3D `TextMeshPro` 量的）⇒ **等价于原版 `isOrthographic = 1`**，
        /// 所以原版 Damage 那 0.1 必须在**折成我们世界单位之前**乘进来 —— 就落在下面这两个常量里。</summary>
        public const float DamageEmLocal = DamageFontSize * DamageOrthoScale * DamageLocalScale;
        /// <summary>同上，Heal 那一路（`0.7 × 1.0 × 1.0` = 0.700）。</summary>
        public const float HealEmLocal = HealFontSize * HealOrthoScale * HealLocalScale;

        /// <summary>原版字体（Pragati-Regular SDF）的**大写墨高 ÷ em** = **0.656**。
        /// 判据 = `N54` §三·7「本工程实测 M 墨高/em 0.656」，与 `TmpFont.CapPerFontSize / EmPerFontSize`
        /// 同一口径（字形墨高，含 SDF padding）。⚠️ `faceInfo.capLine / pointSize` 那一口径是 **0.6316**
        /// —— ⛔ 不用它，理由见文件头「口径选择」。</summary>
        public const float InkOverEm = 0.656f;

        /// <summary>卡局部单位 → 我们世界单位的桥 = 场卡 `desiredScale` **0.36** × `CardFeel.UnitsToOurs`（182.14/108）
        /// = **0.6071**（判据 §三·5：我方场卡行 65.57 px/卡单位 ÷ 108 px/世界单位）。
        /// ⚠️ 取的是**我方场卡**那一档；敌方那一档见 <see cref="EnemyCardLocalToOurs"/>（0.69 × 86.2/108）。
        /// 🔴 **2026-10-19（B4）**：本行原来写着「我们这颗飘字是 HUD 上的单档、不按敌我行分 —— 已记进没查清」；
        /// 那条尾巴**已修**（`PopDamage/PopHeal` 现在按 `enemy` 取两档锚，见 `CardLocalToOursFor`）。</summary>
        public const float CardLocalToOurs = ArenaSlots.PlayerCardScale * CardFeel.UnitsToOurs;

        /// <summary>**敌方那一行**的 px/世界单位 = **86.2**。
        /// 判据 = `V2`/`TargetReticle` 那份独立验算：`BoardCamera` FOV 46.3972° · z=−13.572，
        /// 敌行 z=+1.043 ⇒ 86.2 px/单位（玩家行 z=−6.655 ⇒ 182.14）。
        /// 🔴 **2026-10-19（B4 · `N54②` 尾巴③）**：原来本文件只有玩家那一档（`CardLocalToOurs`），
        /// 而原版这枚飘字是**挂在受击那张卡上的子件** ⇒ 天然跟着各行走
        /// （玩家行 `0.36 × 182.14 = 65.57 px/卡单位`；敌方行 `0.69 × 86.2 = 59.48` ⇒ **敌方小 9.3%**）。
        /// 我们这颗是 HUD 上的单档 ⇒ 必须按 `e.Player` 显式取两档锚，否则敌我一样大。</summary>
        public const float EnemyBoardPxPerUnit = 86.2f;

        /// <summary>敌方行：卡局部单位 → 我们世界单位 = `EnemyCardScale 0.69 × (86.2 / 108)` = **0.5507**。
        /// 与 <see cref="CardLocalToOurs"/>（玩家 0.6071）的比 = **0.9071**（= 原版 59.48/65.57）。</summary>
        public const float EnemyCardLocalToOurs = ArenaSlots.EnemyCardScale * (EnemyBoardPxPerUnit / CardFeel.OurPxPerUnit);

        /// <summary>按敌我取「卡局部单位 → 我们世界单位」那一档（判据只此一处）。</summary>
        public static float CardLocalToOursFor(bool enemy)
        {
            return enemy ? EnemyCardLocalToOurs : CardLocalToOurs;
        }

        /// <summary>伤害飘字的**大写墨高**（我们这侧的世界单位）= `0.847 × 0.656 × 0.6071` = **0.3374**。
        /// 原版我方场卡 = **36.4 px**（= 55.5 px/em × 0.656）；旧值 0.30 = 32.4 px（小 11.0%）。
        /// ⚠️ 它**不再是我们挑的基准**（旧版是从旧实现沿用 0.30 再反推治疗）—— 两档现在**各自**从原版字面量算。</summary>
        public const float DamageCapWorld = DamageEmLocal * InkOverEm * CardLocalToOurs;

        /// <summary>治疗飘字的**大写墨高** = `0.700 × 0.656 × 0.6071` = **0.2788**。
        /// 原版 = **30.1 px**（= 45.9 px/em × 0.656）；旧值 `0.024756` = 2.67 px —— 小 11.3 倍，
        /// 根因是旧式 `Damage × 0.7 ÷ (5.5 × 1.54)` **漏了 `isOrthographic` 的 ×0.1**（见文件头）。</summary>
        public const float HealCapWorld = HealEmLocal * InkOverEm * CardLocalToOurs;

        /// <summary>按敌我取伤害档的**大写墨高**（世界单位）。
        /// 玩家 = <see cref="DamageCapWorld"/>；敌方 = 同一算式 × `EnemyCardLocalToOurs`
        /// （= 原版敌方行 59.48 px/卡单位 ⇒ 比玩家小 9.3%，见 `EnemyBoardPxPerUnit`）。</summary>
        public static float DamageCapWorldFor(bool enemy)
        {
            return DamageEmLocal * InkOverEm * CardLocalToOursFor(enemy);
        }

        /// <summary>同上，治疗档。</summary>
        public static float HealCapWorldFor(bool enemy)
        {
            return HealEmLocal * InkOverEm * CardLocalToOursFor(enemy);
        }

        /// <summary>原版那两颗 TMP 用的**字体面** = `Pragati-Regular SDF`（`TmpFont.Face.LatinCardBody`）。
        /// 判据 = `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_5890981294211439552`
        /// （Damage）与 `…_-7677963611639080000`（Heal）的 `m_fontAsset` PPtr 都落到 Pragati；
        /// 而且原版 `InkOverEm = 0.656` **就是 Pragati 的实测大写墨高/em**（We 和 `TmpFont.CapPerFontSize` 同口径）。
        /// 🔴 **2026-10-19（B4 · `N54②` 尾巴②）**：原来 `Label.Create` 不传面 ⇒ 落到默认 CJK，
        /// 数字（纯拉丁）被画成 Noto 字形（行盒/字形都不同）。现在建完标签立刻 `SetFace`，
        /// **必须在 `SetCapHeight` 之前**（`CapPerFontSize` 按 `ActualFace` 取）。</summary>
        public const TmpFont.Face Face = TmpFont.Face.LatinCardBody;

        /// <summary>两档的**有效**高度比 = `0.847 ÷ 0.700` = **1.21**。
        /// 断言拿它当靶（`A1213②`）；🧨 把两档改回同一套参数 ⇒ 这里仍是 1.21，而**两件建出来的
        /// 实绘字号相等**（比 = 1.00）⇒ 那条灭自证当场红（差 0.21 ≫ 容差 0.05）。
        /// ⚠️ `N54` §五 写的是 **1.213** —— 那是拿 **capLine 口径**两位近似值反算的
        /// （0.325 ÷ 0.268 = 1.2127）；**按原版字面量精确算是 1.21 整**（0.847/0.700），
        /// 且两个口径的比值本来就相同 ⇒ 本常量由原版字面量算（⛔ 不写死字面量）。</summary>
        public static readonly float EffectiveScaleRatio = DamageEmLocal / HealEmLocal;

        /// <summary>最近一次飘字的补间（自检核对总时长用；原在 `CardFeel.LastPopTween`）。</summary>
        public static Tween LastPopTween;

        /// <summary>伤害档（原版 `DamageText` 那一路）。`amount` 可正可负，显示恒为**纯整数、无符号**（见 <see cref="Pop"/>）。
        /// <paramref name="enemy"/> = 挨打的是**敌方**那一行（原版飘字挂在卡上 ⇒ 玩家 65.57 / 敌方 59.48 px/卡单位）· <paramref name="key"/> = 「这是哪一张卡的伤害计数器」的稳定标识（原版每卡**只有一颗** `DamageCounter`，再中一次是**重播同一颗**）；不传 = 每次新建（自检里那些「同一帧连建两颗、比字号」的用例靠这一档）。</summary>
        public static Label PopDamage(Transform parent, Vector3 worldPos, int amount, bool enemy = false, object key = null)
        {
            return Pop(parent, worldPos, amount, false, enemy, key);
        }

        /// <summary>治疗档（原版 `HealText` 那一路）。显示恒带正号（原版 `"+"+量`）。
        /// <paramref name="key"/> 见 <see cref="PopDamage"/>（治疗档与伤害档**各一张**复用表 —— 原版是两颗不同的 TMP）。</summary>
        public static Label PopHeal(Transform parent, Vector3 worldPos, int amount, bool enemy = false, object key = null)
        {
            return Pop(parent, worldPos, amount, true, enemy, key);
        }

        static Label Pop(Transform parent, Vector3 worldPos, int amount, bool heal, bool enemy, object key)
        {
            // 🔴 **2026-11-04（`B139-E` · 账 `1-01`）就地改（铁律 5）**：本行原来是
            //    `(heal ? "+" : "-") + Mathf.Abs(amount)` —— **伤害档多了一个 `-`**。
            //    判据 = `decomp_full/CardDamageCounterController__DamageReceived.c:10-16`：原版把
            //    `System.Int32.ToString()` 的结果**直接**塞进 TMP，**没有任何符号前缀**（例：`5`）；
            //    prefab `MonoBehaviour_5890981294211439552.json` 里那颗 `m_text = "-5"` 只是**占位**，
            //    运行时被覆盖。⇒ 伤害档 = 纯整数；治疗档的 `"+"` 是原版真值（`CardScript._Heal…:155-204` 的 `"+"+量`），保留。
            string txt = heal ? ("+" + Mathf.Abs(amount)) : Mathf.Abs(amount).ToString();
            // 🔴 颜色照原版：两颗 TMP 的 `m_fontColor` **都是纯白 (1,1,1,1)**
            //    （`MonoBehaviour_5890981294211439552` / `…_-7677963611639080000`，2026-09-17 亲读）。
            //    「受伤红 / 治疗绿」是**我们挑的**，不是原版的做法 —— ⛔ 别改回去。
            //
            // 🔴 **2026-11-04（`B139-E` · 账 `1-02`）新增：复用同一颗**。判据 =
            //    `decomp_full/CardDamageCounterController__DamageReceived.c:10-18` —— 原版每张卡**只有一颗**
            //    `DamageCounter`（字段 `BattleCardUI +0x1a8`），再次命中是 `SetActive(true)` + `SetText` +
            //    `Animation.Play()`（**重启同一节点**，不叠加）。我们原来每一下都 `Label.Create` 一颗新的
            //    ⇒ 易伤两拍会在屏幕上同时挂两颗数字（最多重叠 0.83 s）。
            //    ⇒ 带 `key`（= 那张卡的稳定标识）时按 `key` 复用、**重播**；`key == null` 仍是「每次新建」。
            var live = key != null ? LiveTable(heal) : null;
            LivePop slot = null;
            if (live != null) live.TryGetValue(key, out slot);
            if (slot != null && slot.lbl == null) { live.Remove(key); slot = null; }   // Unity 假 null = 已被销毁

            Label lbl;
            if (slot == null)
            {
                lbl = Label.Create(parent, txt, worldPos, 16, Color.white,
                                   new Vector2(0.5f, 0.5f), "Pop_" + txt);
                if (lbl == null) return null;
            }
            else
            {
                // 重播：先杀掉上一条（⛔ 不 `OnComplete` —— 那会把节点藏起来），再**先激活**再换文本
                // （`Label`/TMP 在未激活时 `ForceMeshUpdate` 是空操作 ⇒ 反过来会让这一轮量到上一轮的尺寸）。
                lbl = slot.lbl;
                if (slot.tween != null) { slot.tween.Kill(false); slot.tween = null; }
                lbl.gameObject.SetActive(true);
                lbl.SetText(txt);
                lbl.transform.localPosition = worldPos;
                ReusedCount++;
            }

            // 🔴 字体面 = 原版 Pragati（见 `Face` 的 doc）。**必须在 SetCapHeight 之前** ——
            //    `Label.SetCapHeight` 内部按 `ActualFace` 取 `CapPerFontSize`。
            lbl.SetFace(Face);
            float bridge = CardLocalToOursFor(enemy);

            if (heal)
            {
                // 原版 Heal：`autoSizing=0` ⇒ **只定字号**（min/max/base 是死值，见文件头）
                lbl.SetCapHeight(HealEmLocal * InkOverEm * bridge);
                lbl.SetWrappingMode(HealWrappingMode);
            }
            else
            {
                lbl.SetCapHeight(DamageEmLocal * InkOverEm * bridge);
                // 原版 Damage：`autoSizing=1` + 四格。三格 min/max/base 是**原版 fontSize 单位**，
                // `SetAutoFitBox` 收的是**画布 px** ⇒ 用「本标签的标称 px ÷ 原版 fontSize」折算：
                // 这样 `fontSizeMin/Max/Base` 落到我们的 `cur` 上时，三者的**比值**与原版完全一致
                // （`Label.SetAutoFitBox` 内部做的正是 `cur × (minPx / nomPx)`）。
                float nomPx = lbl.FontPxNow;                    // = Label.NominalPx()（唯一那条 px 口径）
                float k = nomPx > 0f ? nomPx / DamageFontSize : 0f;
                if (k > 0f)
                {
                    // 框照原版：sizeDelta × 自己那颗 RT 的 1.54 × **卡局部→我们世界**的桥
                    // 🔴 2026-10-19（B4）：这里原来是 `CardFeel.UnitsToOurs`（少了 desiredScale 0.36 ⇒ 框大 2.78×、
                    //    自适应永不收缩）。判据与后果见 `BoxW/BoxH` 的 doc。
                    float boxW = BoxW * DamageLocalScale * bridge;
                    float boxH = BoxH * DamageLocalScale * bridge;
                    lbl.SetAutoFitBox(boxW, boxH,
                                      DamageFontSizeMin * k, DamageFontSizeMax * k, DamageFontSizeBase * k);
                    lbl.SetWrappingMode(DamageWrappingMode);    // SetAutoFitBox 顺带把折行开成 1 ⇒ 还原成原版的 0
                }
            }

            // 🔴 **2026-11-04（`B139-E` · 账 `5-02`）新增：伤害档的字材质贴原版描边**。
            //   判据 = 那颗 TMP 的 `m_sharedMaterial`（`MonoBehaviour_5890981294211439552.json` 的
            //   `m_FileID 11 / m_PathID -455814643003338116`）→ `bundle_fonts_assets_all/Material/
            //   Material_-455814643003338116.json`「Pragati-Regular Atlas Material Thick Outline」逐值实读：
            //   `_OutlineWidth 0.097` · `_OutlineColor (0,0,0,0.7882353)` · `_FaceDilate 0.105` · `_GradientScale 21`。
            //   ⛔ **治疗档不贴**：它的 `m_sharedMaterial` 是 `-5050103597772954521`（「Pragati-Regular Atlas
            //   Material」，实读 `_OutlineWidth 0` / `_FaceDilate 0`）⇒ 一刀切会把治疗也描上黑边。
            //   做法复用 `ErrorMessageBanner.ApplyBannerOutline`（**同一份材质 pid**，两处共用一份配方 ——
            //   铁律 6：判据只留一处，⛔ 别在这里再造第二份描边常量）。
            if (!heal) ErrorMessageBanner.ApplyBannerOutline(lbl);

            lbl.SetColor(new Color(lbl.color.r, lbl.color.g, lbl.color.b, 0f));

            // 🔴 **2026-10-21（第六十会话 · 用户报「只见白色 -2，不见红色血痕」）**：
            //    红色血痕 = 原版 `3DBody/DamageCounter/DamageIcon` —— 一张**SpriteRenderer**（sprite
            //    `Card Damage`，256²），z −0.05、**sortingOrder 1**（数字是 2 ⇒ 血痕在数字后一层）。
            //    我们**从没建过这个节点**（`CardFeel.cs` 当时写着「图标我们没画」）。
            //    这里从效果库的 `CardPrefab` 里取那颗现成节点、按原版 offset/sortingOrder 摆到飘字同一处，
            //    并把它挂进同一条 DOTween 时间线（α 0→0.8235 @PopIn、保持、PopOut→0；
            //    **缩放 0.5763→0.7 @PopIn→PopScaleIn** —— 见下面那段，2026-10-21 W61 第 5 条）。
            //    ⚠️ 它是**飘字节点的子件**（原版那颗 `DamageIcon` 就在 `DamageCounter` 根底下）——
            //    这样父节点 0.6641→1 的 pop 会**同时**作用在血痕上，位置偏移也跟着缩（与原版一致）。
            // 🔴 **2026-11-04（`B139-E` · 账 `5-13`）**：**治疗档那颗 `HealIcon` 也建出来了** ——
            //    判据 = `CardPrefab.prefab` 的 `HealIcon`（原版 `CardPrefab.prefab:1730` / 渲染件 212692131138185966）：
            //    sprite `Card Heal`（256²）· `m_Color (1,1,1,0.853)` · `sortingOrder 100` · `localScale 0.56046`
            //    · 自己是 `localPosition (0,0,0)`（那一跳 z 在父件 `HealText` 的 `localPosition.z = -0.05` 上）。
            //    ⚠️ 它**没有**动画（原版治疗那一下只 `DOScale` 父件 `HealText`，α 恒 0.853）⇒ 本文件不给它挂补间。
            //    ⚠️ 生产路径的治疗飘字走 `CardView.ShowHeal`（那颗 `HealText` 建在卡上）—— 本处这一档服务于
            //    `PopHeal` 这条 API（自检 / 将来若把治疗并回来时用的同一份实现）。
            SpriteRenderer hitIcon = slot != null ? slot.icon : null;
            if (hitIcon == null) hitIcon = CreateCounterIcon(lbl.transform, heal, bridge);
            if (hitIcon != null) ResetCounterIcon(hitIcon, heal, bridge);
            // 数字的 sortingOrder = 2（原版 DamageText = 2）
            foreach (var mr in lbl.GetComponentsInChildren<MeshRenderer>(true)) mr.sortingOrder = TextOrder;

            var tr = lbl.transform;

            var seq = BuildPopSequence(lbl, hitIcon, bridge, heal);

            // 收尾：**复用档**只藏不毁（原版那颗 `DamageCounter` 是常驻节点，下次 `Animation.Play` 重启）；
            //       **每次新建档**照旧销毁（否则自检里那些「同一帧连建十几颗」的用例会堆一屏）。
            if (live != null)
            {
                seq.OnComplete(() =>
                {
                    if (lbl == null) return;
                    SetLabelAlpha(lbl, 0f);
                    if (!heal && hitIcon != null) SetIconAlpha(hitIcon, 0f);
                    lbl.gameObject.SetActive(false);
                });
            }
            else
            {
                seq.OnComplete(() => { KillIcon(hitIcon); KillLabel(lbl); });
            }
            LastPopTween = CardTween.Use(seq, Ease.Linear, tr);
            if (live != null)
            {
                if (slot == null) { slot = new LivePop(); live[key] = slot; }
                slot.lbl = lbl; slot.icon = hitIcon; slot.tween = LastPopTween;
                PruneLive(live);
            }
            return lbl;
        }

        /// <summary>飘字那条时间轴（原版 clip `InBattleDamageCounter Variation 1`，**逐段照 clip**）。
        /// 抽出来是为了「复用同一颗」时能**整条重播**（原版 `Animation.Play()` 就是从头再放一遍）。</summary>
        static Sequence BuildPopSequence(Label lbl, SpriteRenderer hitIcon, float bridge, bool heal)
        {
            var tr = lbl.transform;
            tr.localScale = Vector3.one * CardFeel.PopScaleFrom;

            var seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => 0f, a => SetLabelAlpha(lbl, a), 1f, CardFeel.PopIn).SetEase(CardFeel.ClipSmoothStep));
            // ⚠️ 用 `Insert(0f, …)` 而不是 `Join` —— `Join` 会接在**当前游标**（0.1167）后面，
            //    这一条 0.2s 的缩放就会拖到 0.3167，把后面整条时间轴往后推 0.12s。
            //    时间轴要**严格照 clip**：0.2 缩完 → 停到 1.6667 → 1.8333 消失。
            seq.Insert(0f, tr.DOScale(Vector3.one, CardFeel.PopScaleIn).SetEase(CardFeel.ClipSmoothStep));
            seq.AppendInterval(CardFeel.PopHoldUntil - CardFeel.PopScaleIn);
            seq.Append(DOTween.To(() => 1f, a => SetLabelAlpha(lbl, a), 0f,
                                  CardFeel.PopOut - CardFeel.PopHoldUntil).SetEase(CardFeel.ClipSmoothStep));
            // 血痕：α 0→0.8235（原 clip）→ 保持 → PopOut 段淡到 0。
            // ⛔ 治疗那颗 `HealIcon` **不进这条时间线**：原版它的 α 是 prefab 常量 0.853，没有任何曲线驱动它。
            if (hitIcon != null && !heal)
            {
                seq.Insert(0f, DOTween.To(() => 0f, a => SetIconAlpha(hitIcon, a * 0.8235f), 1f,
                                          CardFeel.PopIn).SetEase(CardFeel.ClipSmoothStep));
                seq.Insert(CardFeel.PopHoldUntil,
                           DOTween.To(() => 1f, a => SetIconAlpha(hitIcon, a * 0.8235f), 0f,
                                      CardFeel.PopOut - CardFeel.PopHoldUntil).SetEase(CardFeel.ClipSmoothStep));
                // 🔴 2026-10-21（W61 第 1 批 · 第 5 条）：**血痕自己的缩放曲线** ——
                //   clip 的 `DamageIcon`：0.5763 @0.1167 → 0.7 @0.2（0.0833 s）。
                //   ⚠️ 用 **`Insert(PopIn, …)`（绝对时刻）**，⛔ 别用 `Join`/`Append` ——
                //      `Join` 接在当前游标（= `PopIn` 之后）没问题、`Append` 会把后面整条推后 0.083 s。
                //   缓动：clip 的关键帧是 auto 切线（`inSlope/outSlope = 0` + `weightedMode = 0`），
                //   真形状要 Unity 自己算 ⇒ 这里用**与父节点 pop 同一条** `OutQuad`（视觉同族）。
                var iconTr = hitIcon.transform;
                seq.Insert(CardFeel.PopIn,
                           DOTween.To(() => DamageIconScaleFrom,
                                      k => iconTr.localScale = Vector3.one * (k * bridge),
                                      DamageIconScaleTo,
                                      Mathf.Max(1e-4f, CardFeel.PopScaleIn - CardFeel.PopIn))
                                  .SetEase(IconScaleRamp));
            }
            return seq;
        }

        /// <summary>⚠️ **批处理下 `Object.Destroy` 不生效**（它要等下一帧，而批处理没有帧循环）——
        /// 和 `BattleDriver.Kill` 是同一条规矩。</summary>
        static void KillLabel(Label l)
        {
            if (l == null) return;
            if (Application.isPlaying) Object.Destroy(l.gameObject);
            else Object.DestroyImmediate(l.gameObject);
        }

        static void SetLabelAlpha(Label l, float a)
        {
            if (l == null) return;
            var c = l.color;
            l.SetColor(new Color(c.r, c.g, c.b, a));
        }

        /// <summary>递归按名字找子件（`Transform.Find` 只找直接子件 —— 见 CLAUDE.md 那条坑）。</summary>
        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindDeep(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        static void SetIconAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            var c = sr.color; c.a = Mathf.Clamp01(a); sr.color = c;
        }

        static void KillIcon(SpriteRenderer sr)
        {
            if (sr == null) return;
            if (Application.isPlaying) Object.Destroy(sr.gameObject);
            else Object.DestroyImmediate(sr.gameObject);
        }

        // ============ 原版 clip 的**缓动形状**（⛔ 别再用 `Ease.Linear` / `Ease.OutQuad` 顶）============
        //  判据 = clip `InBattleDamageCounter Variation 1`
        //  （`bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/AnimationClip_-3234148433819516763.json`）
        //  的 `m_FloatCurves` / `m_ScaleCurves` **每个关键帧的 `inSlope` / `outSlope`**（2026-11-03 现读）：
        //    · `DamageText.m_fontColor.a` 0→1 @[0, 0.1167]：两端切线 **都是 0.0**；
        //    · 保持 1.0 到 1.6667、再 →0 @1.8333：两端切线 **都是 0.0**；
        //    · 根 `ScaleCurves` 0.6641 @0 → 1.0 @0.2：两端切线 **都是 0.0**；
        //    · `DamageIcon` 的 `m_fontColor.a` 0→0.8235 @[0, 0.1167]：两端切线 0.0；
        //    · `DamageIcon` 的 `ScaleCurves` 0.5763 @0.1167 → 0.7 @0.2：**首键两端切线都是 0.0**、
        //      **末键 `inSlope = 0.06277556`**（🔴 2026-11-04 `B139-E` 订正：原来这里写反了，见下面那段）。
        //  ⇒ Unity 的 `AnimationCurve.Evaluate` 是 **Hermite**：两端切线为 0 时那条曲线**不是直线**，
        //    而是 `3t² − 2t³`（平滑阶跃）；只有「切线 == 弦斜率」时退化才成直线（本 clip 一条都没有）。
        //    ⚠️ 2026-11-03 之前这里写的是 `Ease.Linear`（透明度两条 + 血痕 α 两条）与 `Ease.OutQuad`
        //    （两条缩放），**都不是原版的形状** —— `OutQuad` 前段比原版快、`Linear` 两端有折角（铁律 5 留痕）。
        //  做法：直接喂 `AnimationCurve` 给 `SetEase` —— 求值语义（Hermite）与原版**逐位同一套**，
        //  ⛔ 别自己写一份「差不多的」缓动函数。四条零切线段**复用现成的那条** =
        //  `CardFeel.ClipSmoothStep`（`Core/CardFeel.cs:2011`，同一个「两端切线 0 ⇒ Hermite 退化成 smoothstep」判据，
        //  铁律 6：判据只留一处，⛔ 别在这里再造第二条同样的曲线）。归一的端点仍是 0/1 ⇒ DOTween 那侧归一化是恒等。
        static AnimationCurve _iconRamp;
        /// <summary>`DamageIcon` 缩放那一段的**切线方向** —— 🔴 **2026-11-04（`B139-E` · 账 `11-01`）就地改（铁律 5）**。
        ///
        /// <para>**原来写的是错的（装反了）**：本段原文写「`0.0628` 是**首键** `inSlope`」、代码也把非零切线挂在
        /// `Keyframe(0f, 0f, m0, m0)`（首键两端）上、末端放 0 ⇒ **形状整个反了**（起步陡、收尾平）。</para>
        ///
        /// <para>判据（一手，`AnimationClip_-3234148433819516763.json` 的 `m_ScaleCurves[path="DamageIcon"]`
        /// 段 `[0.11666667, 0.2]` 逐键实读）：
        /// **首键（t=0.11666667, v=0.5763395）`inSlope = outSlope = 0`** ·
        /// **末键（t=0.2, v=0.7）`inSlope = 0.06277556`、`outSlope = 0`**。
        /// ⇒ 归一化后：`Keyframe(0, 0, 0, 0)` + `Keyframe(1, 1, m1, 0)`，其中
        /// `m1 = 0.06277556 ÷ 弦斜率`，弦斜率 = `(0.7 − 0.5763395) ÷ (0.2 − 0.1167)` ≈ **1.4845**/秒
        /// ⇒ `m1 ≈ 0.0423`。⚠️ **必须换算**：`AnimationCurve` 的切线是「值/秒」，而这条曲线归一化后横轴是
        /// 0~1 的进度，两者不是同一量纲。
        /// 视觉量级（`B135` 实测）：归一化 max |Δ| ≈ **0.0106** @u≈0.5 ⇒ 缩放值 1.31e-3 ≈ 0.33 px（很小，但语义反了）。</para></summary>
        static AnimationCurve IconScaleRamp
        {
            get
            {
                if (_iconRamp == null)
                {
                    const float chordSlope = (DamageIconScaleTo - DamageIconScaleFrom)
                                           / (CardFeel.PopScaleIn - CardFeel.PopIn);
                    const float inSlope = 0.06277556f;             // clip 实读（`DamageIcon` ScaleCurves **末键** inSlope）
                    float m1 = chordSlope > 1e-6f ? inSlope / chordSlope : 0f;
                    // 首键两端 0（起步平） → 末键入切线 m1、出切线 0（收尾陡）—— **方向照原版**
                    var c = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, m1, 0f));
                    c.preWrapMode = WrapMode.Clamp; c.postWrapMode = WrapMode.Clamp;
                    _iconRamp = c;
                }
                return _iconRamp;
            }
        }

        /// <summary>自检读数口：血痕缩放那条曲线的**归一化入切线**（= `inSlope ÷ 弦斜率`，期望 ≈ 0.0423）。
        /// 与 <see cref="IconScaleRamp"/> 同一份数据 —— 断言用它，⛔ 不另造一个常量当靶（否则自证）。</summary>
        public static float IconScaleRampInTangent
        {
            get
            {
                var c = IconScaleRamp;
                if (c == null || c.length == 0) return 0f;
                return c[0].outTangent;      // 首键 outTangent 必须是 0；真正的非零切线在末键
            }
        }

        /// <summary>自检读数口：末键的**入切线**（归一化，期望 ≈ 0.0423）。</summary>
        public static float IconScaleRampEndInTangent
        {
            get
            {
                var c = IconScaleRamp;
                if (c == null || c.length < 2) return 0f;
                return c[1].inTangent;
            }
        }

        /// <summary>自检读数口：血痕缩放曲线在归一化进度 `u` 处的值（`u ∈ [0,1]`）。
        /// 判据形状 = Hermite（`m1 = 0.0423` 在**末键**）⇒ `u = 0.5` 时应是 **0.4947**；
        /// 切线装反那一版是 **0.5053**（差 0.0106，`B135` 实测）。断言两条各验一个。</summary>
        public static float IconRampAt(float u)
        {
            var c = IconScaleRamp;
            return c != null ? c.Evaluate(u) : 0f;
        }

        // ==================================================================
        //  🆕 2026-11-04（`B139-E`）：飘字底下那颗图标 + 「同一颗重播」的复用表
        //  ⚠️ 新成员一律追加在**类尾**（账按 `文件:行号` 引用，插在中间会让整片行号漂）。
        // ==================================================================

        /// <summary>原版 `DamageIcon` 的局部偏移（**卡局部单位**）：`CardPrefab.prefab` 的
        /// `Transform`（`DamageIcon`，fileID 2087739079688891721）实读
        /// `(0.09400177, −0.104, **−0.05000019**)`。
        /// 🔴 **2026-11-04（`B139-E` · 账 `11-07`）就地改（铁律 5）**：原来写的是 `(0.094, −0.104, **0**)`
        /// —— **z 少了 0.05 卡局部单位**（`B135` 动画 #33 实测差 = 0.05）。层序本身另有 `sortingOrder 1/2`，
        /// 但那不是「不用抄 z」的理由：原版 z 也真的偏了 0.05。</summary>
        public static readonly Vector3 DamageIconLocalPos = new Vector3(0.09400177f, -0.104f, -0.05000019f);

        /// <summary>原版治疗图标 `HealIcon` 的局部偏移 —— 它自己 `localPosition = (0, 0, 0)`，
        /// 那一跳 z 烘在**父件 `HealText`** 的 `localPosition.z = −0.05` 上（`CardPrefab.prefab` 的
        /// `RectTransform` fileID 7056487514831563120 实读）。本文件把飘字根当父 ⇒ 直接挂这一个 z。</summary>
        public static readonly Vector3 HealIconLocalPos = new Vector3(0f, 0f, -0.05f);

        /// <summary>原版 `HealIcon` 的 `localScale`（prefab 实读 **0.56046**，三轴同值）。</summary>
        public const float HealIconScale = 0.56046f;
        /// <summary>原版 `HealIcon` 的 `SpriteRenderer.m_SortingOrder`（prefab 实读 **100**）。</summary>
        public const int HealIconOrder = 100;
        /// <summary>原版 `HealIcon` 的 `m_Color.a`（prefab 实读 **0.853**，**恒定** —— 没有任何 clip 驱动它）。</summary>
        public const float HealIconAlpha = 0.853f;
        /// <summary>原版 `DamageIcon` 的 `sortingOrder`（prefab 实读 **1**；数字是 2 ⇒ 血痕在数字后一层）。</summary>
        public const int DamageIconOrder = 1;
        /// <summary>原版 `DamageText` / `HealText` 那颗 TMP 的 `sortingOrder`（= **2**，压在血痕之上）。</summary>
        public const int TextOrder = 2;

        /// <summary>一条「还活着、可以重播」的飘字（原版 `BattleCardUI` 上那颗 `DamageCounter` 的替身）。</summary>
        class LivePop
        {
            public Label lbl;
            public SpriteRenderer icon;
            public Tween tween;
        }

        // 伤害 / 治疗**各一张**表 —— 原版是两颗不同的 TMP 组件（`DamageText` / `HealText`），
        // 共用一个 key 会让「先受伤再被治疗」串成同一颗节点（那不是原版）。
        static readonly System.Collections.Generic.Dictionary<object, LivePop> _liveDamage
            = new System.Collections.Generic.Dictionary<object, LivePop>();
        static readonly System.Collections.Generic.Dictionary<object, LivePop> _liveHeal
            = new System.Collections.Generic.Dictionary<object, LivePop>();

        static System.Collections.Generic.Dictionary<object, LivePop> LiveTable(bool heal)
        {
            return heal ? _liveHeal : _liveDamage;
        }

        /// <summary>复用过的次数（诊断；`ReusedCount > 0` 才说明那条「同一颗重播」的路真的被走到了）。</summary>
        public static int ReusedCount { get; private set; }
        /// <summary>复用表里当前还挂着几条（诊断）。</summary>
        public static int LiveCount { get { return _liveDamage.Count + _liveHeal.Count; } }

        /// <summary>自检用：清空复用表（**只清引用、不销毁节点** —— 节点归调用方的父级管）。
        /// ⚠️ 每段断言前后都该调一次，否则上一段留下的节点会被下一段当成「可复用」。</summary>
        public static void ResetLive()
        {
            _liveDamage.Clear(); _liveHeal.Clear();
            ReusedCount = 0;
        }

        /// <summary>表太大时清掉「key 或节点已经没了」的那些（Unity 假 null ⇒ 用 `== null` 判，⛔ 别用 `ReferenceEquals`）。</summary>
        static void PruneLive(System.Collections.Generic.Dictionary<object, LivePop> live)
        {
            if (live.Count <= 32) return;
            var drop = new System.Collections.Generic.List<object>();
            foreach (var kv in live)
            {
                var uo = kv.Key as UnityEngine.Object;      // 非 Unity 对象的 key 落地为 null，跳过
                bool keyGone = !ReferenceEquals(uo, null) && uo == null;
                if (keyGone || kv.Value == null || kv.Value.lbl == null) drop.Add(kv.Key);
            }
            for (int i = 0; i < drop.Count; i++) live.Remove(drop[i]);
        }

        /// <summary>从效果库的 `CardPrefab` 里取那颗现成节点（`DamageIcon` / `HealIcon`）实例化到飘字底下。
        /// 取不到 ⇒ **出声**并返回 null（不许静默：那一下会少一个可见件）。</summary>
        static SpriteRenderer CreateCounterIcon(Transform parent, bool heal, float bridge)
        {
            string nodeName = heal ? "HealIcon" : "DamageIcon";
            try
            {
                var lib = WarpforgeVFX.WarpforgeEffectLibrary.Instance;
                var entry = lib != null ? lib.Get("CardPrefab") : null;
                var src = entry != null && entry.prefab != null
                        ? FindDeep(entry.prefab.transform, nodeName) : null;
                if (src == null)
                {
                    Debug.LogWarning($"[PopNumber] `CardPrefab` 里找不到 `{nodeName}` ⇒ 这一下没有"
                                   + (heal ? "治疗图标" : "红色血痕")
                                   + "（跑一次 `EffectExporter.RunListed` / 检查效果库是否含 CardPrefab）");
                    return null;
                }
                var go = Object.Instantiate(src.gameObject, parent, false);
                go.name = heal ? "PopHealIcon" : "PopDamageIcon";
                go.SetActive(true);
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    Debug.LogWarning($"[PopNumber] `CardPrefab/{nodeName}` 上没有 `SpriteRenderer` ⇒ 跳过");
                    if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
                    return null;
                }
                ResetCounterIcon(sr, heal, bridge);   // 按原版位姿/层级/α 初值摆好（复用档每次飘字还会再调一次）
                return sr;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PopNumber] 建{(heal ? "治疗图标" : "血痕")}失败：{e.GetType().Name}: {e.Message}");
                return null;
            }
        }

        /// <summary>把原版 `HealIcon` 挂到**卡上那颗** `HealText` 底下（`CardView.BuildHealText` 的接线口，
        /// 账 `5-13`）。结构与原版 prefab 逐字同形：`HealText`（`localPosition.z −0.05`）→ `HealIcon`
        /// （自己 `(0,0,0)` · `localScale 0.56046` · `sortingOrder 100` · `m_Color.a 0.853`）。
        /// <para>⚠️ 与 `Pop()` 里那一档的区别：飘字根**自己顶替**了 `HealText`（所以 z 由它补 −0.05、缩放乘桥）；
        /// 这里挂的是**已经存在**的 `HealText`（卡局部空间）⇒ 位置取 `(0,0,0)`、**不乘桥**。</para>
        /// <para>⚠️ 幂等：已经挂过（同名子件在）就直接返回那一颗，不重复建。</para></summary>
        public static SpriteRenderer AttachHealIcon(Transform healTextRoot)
        {
            if (healTextRoot == null) return null;
            var had = healTextRoot.Find("PopHealIcon");
            if (had != null)
            {
                var ex = had.GetComponent<SpriteRenderer>();
                if (ex != null) return ex;
            }
            var sr = CreateCounterIcon(healTextRoot, true, 1f);
            if (sr == null) return null;
            var t = sr.transform;
            t.localPosition = Vector3.zero;
            t.localScale = Vector3.one * HealIconScale;
            sr.sortingOrder = HealIconOrder;
            var c = sr.color; c.a = HealIconAlpha; sr.color = c;
            return sr;
        }

        /// <summary>把图标摆回原版位姿（**每次飘字都调** —— 复用同一颗时上一轮跑完停在终点上）。</summary>
        static void ResetCounterIcon(SpriteRenderer icon, bool heal, float bridge)
        {
            if (icon == null) return;
            var t = icon.transform;
            if (heal)
            {
                t.localPosition = HealIconLocalPos * bridge;
                t.localScale = Vector3.one * (HealIconScale * bridge);
                icon.sortingOrder = HealIconOrder;
                var c0 = icon.color; c0.a = HealIconAlpha; icon.color = c0;   // 恒定 α（原版无曲线）
            }
            else
            {
                t.localPosition = DamageIconLocalPos * bridge;
                t.localScale = Vector3.one * (DamageIconScaleFrom * bridge);
                icon.sortingOrder = DamageIconOrder;
                var c1 = icon.color; c1.a = 0f; icon.color = c1;              // 由时间线淡入到 0.8235
            }
        }
    }
}
