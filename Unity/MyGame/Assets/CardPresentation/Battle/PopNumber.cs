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

        /// <summary>伤害档（原版 `DamageText` 那一路）。`amount` 可正可负，显示恒带负号。
        /// <paramref name="enemy"/> = 挨打的是**敌方**那一行（原版飘字挂在卡上 ⇒ 玩家 65.57 / 敌方 59.48 px/卡单位）。</summary>
        public static Label PopDamage(Transform parent, Vector3 worldPos, int amount, bool enemy = false)
        {
            return Pop(parent, worldPos, amount, false, enemy);
        }

        /// <summary>治疗档（原版 `HealText` 那一路）。显示恒带正号。</summary>
        public static Label PopHeal(Transform parent, Vector3 worldPos, int amount, bool enemy = false)
        {
            return Pop(parent, worldPos, amount, true, enemy);
        }

        static Label Pop(Transform parent, Vector3 worldPos, int amount, bool heal, bool enemy)
        {
            string txt = (heal ? "+" : "-") + Mathf.Abs(amount);
            // 🔴 颜色照原版：两颗 TMP 的 `m_fontColor` **都是纯白 (1,1,1,1)**
            //    （`MonoBehaviour_5890981294211439552` / `…_-7677963611639080000`，2026-09-17 亲读）。
            //    「受伤红 / 治疗绿」是**我们挑的**，不是原版的做法 —— ⛔ 别改回去。
            var lbl = Label.Create(parent, txt, worldPos, 16, Color.white,
                                   new Vector2(0.5f, 0.5f), "Pop_" + txt);
            if (lbl == null) return null;

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

            lbl.SetColor(new Color(lbl.color.r, lbl.color.g, lbl.color.b, 0f));

            var tr = lbl.transform;
            tr.localScale = Vector3.one * CardFeel.PopScaleFrom;

            var seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => 0f, a => SetLabelAlpha(lbl, a), 1f, CardFeel.PopIn).SetEase(Ease.Linear));
            // ⚠️ 用 `Insert(0f, …)` 而不是 `Join` —— `Join` 会接在**当前游标**（0.1167）后面，
            //    这一条 0.2s 的缩放就会拖到 0.3167，把后面整条时间轴往后推 0.12s。
            //    时间轴要**严格照 clip**：0.2 缩完 → 停到 1.6667 → 1.8333 消失。
            seq.Insert(0f, tr.DOScale(Vector3.one, CardFeel.PopScaleIn).SetEase(Ease.OutQuad));
            seq.AppendInterval(CardFeel.PopHoldUntil - CardFeel.PopScaleIn);
            seq.Append(DOTween.To(() => 1f, a => SetLabelAlpha(lbl, a), 0f,
                                  CardFeel.PopOut - CardFeel.PopHoldUntil).SetEase(Ease.Linear));
            seq.OnComplete(() => KillLabel(lbl));
            LastPopTween = CardTween.Use(seq, Ease.Linear, tr);
            return lbl;
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
    }
}
