// AbilityText.cs — `Card_Abilities/*` 词条的【取词 + 记号渲染】共用件
//   🆕 2026-10-31（执行代理 `C7` · 第一百零九会话）
//
// ============================ 为什么要有这个文件 ============================
// 判据 = `Unity/资料/完全复刻/判据/B109_R19_poisoned_effectText.md` §2·2 A / B。
//
// 同一件事原来有【两份】实现：
//   ① `Battle/SkillPanel.cs` 的私有 `AbilityRaw / AbilityMarked / AbilityTerm` —— **有**字体闸；
//   ② `Battle/CardDisplayWindow.TryAddEffectTextRow` 自己写的那一遍 —— **漏了**字体闸。
// ⇒ ② 那条路（`abilityEffectTexts` 的右列）在「语档 = 中文 + 中文字体资产缺失」这一档
//   会把中文列打成**方块**，而 ① 不会 —— 这是我们自己的真缺陷（⛔ 不是原版行为）。
// ⇒ 收成这一份，两边都转调它。⛔ **别再加第三份**
//   （工程红线：「两处写同一条规则 = 迟早不一致」）。
//
// ============================ 原版判据（第一权威 = 反编译）============================
//   · 取词 = `RawCardScript.GetLocalizedAbilityDesc(suffix)`：
//     键 = `"Card_Abilities/" + (activeAbilityTextReferenceId ?? uniqueId) + suffix`
//     （`cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2374-2446` ·
//      `decomp_full/RawCardScript__GetLocalizedAbilityDesc{,RemovingSiege}.c`）。
//     ⚠️ **拼键那一半不在本文件** —— 它只有一处 = `CardDef.EffectTextTermOf`
//        （`Card_Abilities/<src>`）；本文件只吃**已经拼好的键**。
//   · 记号渲染 = `SupportMethods.ModifyLocalization`（我们侧 = `CardIcons.RewriteMarks`，
//     `[[…]]` / `((…))` / `$$…$$` / `(N)` 四支，与 `BattleDriver.FaceTextFull` 同一套）。
//
// ============================ 三条闸（缺一条就是上面那个缺陷）============================
//   ① **缺键**判 `Loc.T(key) != key` —— `Loc.HasEntry` **只认手写表**，而 `Card_Abilities/*`
//      那一族在官方全表里 ⇒ 拿 `HasEntry` 会恒 false（把有词条的判成「没有」）。
//   ② **字体闸**：中文档但中文字体资产缺失（`CardText.Zh == false`）⇒ 取官方**英文**列
//      （`Loc.OfficialEnglish`）—— 与 `CardText.TermOr` / `BattleDriver.FaceTextFull` 同一条闸。
//   ③ **记号渲染的 `zh` 位** = `CardText.Zh && Loc.Current == Chinese`（与 ② 同一个闸）——
//      ②已改取英文列，记号里的子词条也必须取英文列，⛔ 否则同一格里中英混排。
// ==========================================================================

namespace CardPresentation
{
    /// <summary>`Card_Abilities/*` 的取词 + 记号渲染 —— **判据只此一份**（文件头有全套出处）。
    /// <para>消费方：`Battle/SkillPanel.cs`（技能面板描述）· `Battle/CardDisplayWindow.cs`
    /// 的 `TryAddEffectTextRow`（「受到以下影响」右列的第一跳）。</para>
    /// <para>⛔ 别在别处再写一份「`Loc.T` + `CardIcons.RewriteMarks`」—— 那正是本文件要收掉的重复。</para></summary>
    public static class AbilityText
    {
        // ==================================================================
        //  自检口（⛔ 产品路径不许读这一格）
        // ==================================================================

        static bool? _zhGateForTest;

        /// <summary>自检用：把「字体闸 + 语档」这一个布尔**按给定值**放进去（`null` = 恢复真值 <see cref="CardText.Zh"/>）。
        /// <para>为什么需要这个口：`CardText.Zh` 依赖 `TmpFont.Available`，而编辑器里中文字体资产**始终在**
        /// ⇒ 不放开这个口就测不到「中文档 + 缺中文字体」那一档 —— 而那正是本件要修的那个缺陷。</para>
        /// <para>⚠️ 只给 `Editor/*_Assert` 用；⛔ 产品路径一律走真值。口径同 `Loc.RestoreForTest` /
        /// `Loc.ResetForTest`（自检写完要还原）。</para></summary>
        public static void SetZhGateForTest(bool? zh) { _zhGateForTest = zh; }

        /// <summary>闸的当前取值：自检覆盖优先，否则 = <see cref="CardText.Zh"/>（字体资产 + 语档两个条件）。</summary>
        static bool Zh { get { return _zhGateForTest ?? CardText.Zh; } }

        // ==================================================================
        //  三个口
        // ==================================================================

        /// <summary>官方词条取一条（三档：手写表 → bulk → 官方全表，走 <see cref="Loc.T"/>）+ 记号渲染；缺键 ⇒ `null`。
        /// <para>⚠️ 缺键判据用 `Loc.T(key) != key`（`Loc.HasEntry` **只认手写表** ⇒ 那一族恒 false）。</para></summary>
        public static string Term(string key)
        {
            string v = Raw(key);
            return v == null ? null : Marked(v);
        }

        /// <summary>官方词条的**原值**（未渲染记号）；缺键 ⇒ `null`。
        /// <para>⚠️ **字体闸**：中文档但中文字体资产缺失（<see cref="CardText.Zh"/> == false）⇒ 取官方**英文**列
        /// （与 `BattleDriver.FaceTextFull` / `CardText.TermOr` 同一条闸，⛔ 否则面板上会印方块）。</para>
        /// <para>⚠️ 调方若还要「缺键 ⇒ 印原文」那一档（原版 `GameStaticData.cs:1778-1782`），
        /// 拿本口返回的 `null` 自己兜 —— 本口 ⛔ 不返回键名、⛔ 不返回原文。</para></summary>
        public static string Raw(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!Zh && Loc.Current == AvailableLanguages.Chinese)
            {
                string en = Loc.OfficialEnglish(key);
                return string.IsNullOrEmpty(en) ? null : en;
            }
            string v = Loc.T(key);
            return (!string.IsNullOrEmpty(v) && v != key) ? v : null;
        }

        /// <summary>记号渲染（原版 `SupportMethods.ModifyLocalization` 那一层）= <see cref="CardIcons.RewriteMarks"/>。
        /// <para>⚠️ 那个 `zh` 位要连**字体闸**一起看：字体缺失时 <see cref="Raw"/> 已改取英文列
        /// ⇒ 记号里的子词条也必须取英文列（否则一格里中英混排）。</para>
        /// <para>🔴 **2026-10-31（`C7`）与原来 `SkillPanel.AbilityMarked` 的唯一差别**：去掉了
        /// `CardIcons.HasOfficialMarks` 那条快路 —— 原版 `ModifyLocalization` 的四支**都要走**，
        /// 那条快路会把 `(N)`（费用图标）那一支整个跳过。现读 `Card_Abilities/*` 120 条里 `(N)`
        /// **0 处**（`官方本地化_en+zh-CN.json` 现数）⇒ 今天零可观察差异，这里只收成**一条口径**。</para></summary>
        public static string Marked(string v)
        {
            if (string.IsNullOrEmpty(v)) return v;
            return CardIcons.RewriteMarks(v, Zh && Loc.Current == AvailableLanguages.Chinese);
        }
    }
}
