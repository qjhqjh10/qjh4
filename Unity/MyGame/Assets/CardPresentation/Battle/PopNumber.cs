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
//       它的 **RectTransform 自己** `m_LocalScale = 1.54`
//       （`RectTransform/RectTransform_5482325324160211904.json`）
//   · HealText  = `…/MonoBehaviour_-7677963611639080000`
//       `autoSizing=0 · m_fontSize=0.7 · m_fontSizeMin=0.35 · m_fontSizeMax=1.4 · m_fontSizeBase=36`
//       `m_TextWrappingMode=0 · Center/Middle · 白`
//       它的 RectTransform（`…_-4069981099246511168.json`）`m_LocalScale = 1.0`
//
// 🔴 **两档的有效字号比 = `5.5 × 1.54 : 0.7` = 12.1 : 1** —— 旧账记的 7.86 : 1 **漏了 Damage 自己那 1.54**。
//    两者父链其余层级全是 1.0（`Transform/Transform_1008882827660663744.json` = 1.0）⇒ 1.54 是唯一的那一跳。
//
// 我们这一侧的锚
// --------------
// 原版 `m_fontSize` 是 TMP 的世界单位，与本工程 `Label` 的世界单位不同源（旧注释已登记「搬不过来」）
// ⇒ **Damage 沿用旧实现的 `SetCapHeight(0.30f)` 当基准**，Heal 由**同一个比值**反算：
//     `HealCapWorld = 0.30 × 0.7 ÷ (5.5 × 1.54) = 0.024756…`
// ⇒ 两档的**实绘高度比仍是 12.1 : 1**（断言 → `Editor/BattleScene.cs` 的 `A1213②` 那一节）。
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
        /// <summary>HealText 的 `m_TextWrappingMode`（0 = NoWrap）。</summary>
        public const int HealWrappingMode = 0;

        /// <summary>两颗 RT 的 `m_SizeDelta`（同一份：`0.800961… × 0.683734…`）。
        /// Damage 用之前还要乘它自己的 1.54；再过 `CardFeel.UnitsToOurs` 折成我们的世界单位。</summary>
        public const float BoxW = 0.8009615f, BoxH = 0.6837339f;

        // ================= 我们这一侧 =================

        /// <summary>伤害飘字的**大写高**（我们这侧的基准；旧实现就是它，别改成「按原版 fontSize 换算」——
        /// 那两个 fontSize 不同源，见文件头）。</summary>
        public const float DamageCapWorld = 0.30f;

        /// <summary>治疗档的大写高 = `0.30 × 0.7 ÷ (5.5 × 1.54)` = **0.024756…**。
        /// ⚠️ 它是**从原版比值反算**出来的，⛔ 不是又一个「我们挑的数」。</summary>
        public static readonly float HealCapWorld =
            DamageCapWorld * (HealFontSize / (DamageFontSize * DamageLocalScale));

        /// <summary>两档的**有效**高度比 = `(5.5 × 1.54) ÷ 0.7` = **12.1**。
        /// 断言拿它当靶（`A1213②`）；🧨 把两档改回同一套参数 ⇒ 这里算出来还是 12.1，
        /// 但**两件建出来的实绘字号相等** ⇒ 那条灭自证当场红（见的断言写法）。</summary>
        public static readonly float EffectiveScaleRatio =
            (DamageFontSize * DamageLocalScale) / HealFontSize;

        /// <summary>最近一次飘字的补间（自检核对总时长用；原在 `CardFeel.LastPopTween`）。</summary>
        public static Tween LastPopTween;

        /// <summary>伤害档（原版 `DamageText` 那一路）。`amount` 可正可负，显示恒带负号。</summary>
        public static Label PopDamage(Transform parent, Vector3 worldPos, int amount)
        {
            return Pop(parent, worldPos, amount, false);
        }

        /// <summary>治疗档（原版 `HealText` 那一路）。显示恒带正号。</summary>
        public static Label PopHeal(Transform parent, Vector3 worldPos, int amount)
        {
            return Pop(parent, worldPos, amount, true);
        }

        static Label Pop(Transform parent, Vector3 worldPos, int amount, bool heal)
        {
            string txt = (heal ? "+" : "-") + Mathf.Abs(amount);
            // 🔴 颜色照原版：两颗 TMP 的 `m_fontColor` **都是纯白 (1,1,1,1)**
            //    （`MonoBehaviour_5890981294211439552` / `…_-7677963611639080000`，2026-09-17 亲读）。
            //    「受伤红 / 治疗绿」是**我们挑的**，不是原版的做法 —— ⛔ 别改回去。
            var lbl = Label.Create(parent, txt, worldPos, 16, Color.white,
                                   new Vector2(0.5f, 0.5f), "Pop_" + txt);
            if (lbl == null) return null;

            if (heal)
            {
                // 原版 Heal：`autoSizing=0` ⇒ **只定字号**（min/max/base 是死值，见文件头）
                lbl.SetCapHeight(HealCapWorld);
                lbl.SetWrappingMode(HealWrappingMode);
            }
            else
            {
                lbl.SetCapHeight(DamageCapWorld);
                // 原版 Damage：`autoSizing=1` + 四格。三格 min/max/base 是**原版 fontSize 单位**，
                // `SetAutoFitBox` 收的是**画布 px** ⇒ 用「本标签的标称 px ÷ 原版 fontSize」折算：
                // 这样 `fontSizeMin/Max/Base` 落到我们的 `cur` 上时，三者的**比值**与原版完全一致
                // （`Label.SetAutoFitBox` 内部做的正是 `cur × (minPx / nomPx)`）。
                float nomPx = lbl.FontPxNow;                    // = Label.NominalPx()（唯一那条 px 口径）
                float k = nomPx > 0f ? nomPx / DamageFontSize : 0f;
                if (k > 0f)
                {
                    // 框照原版：sizeDelta × 自己那颗 RT 的 1.54 × 3D→我们世界单位的桥
                    float boxW = BoxW * DamageLocalScale * CardFeel.UnitsToOurs;
                    float boxH = BoxH * DamageLocalScale * CardFeel.UnitsToOurs;
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
