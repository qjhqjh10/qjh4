// B116W18_Assert.cs — 执行代理 `B116_W18`（2026-11-02 · 第一百一十六会话 · 引擎线）
//   文件所有权 = `Editor/RuleEngineTest.cs`（**只加同一物理行**）· `RuleEngine/Data/CardDatabase.cs` ·
//                `工具/gen_cards_engine.py` · 本文件（新建）
//
// ============================ 它盯的是什么 ============================
//  旧任务 `B-328`（施工单 `ENG-11` 的**数据面**）：给 `enchantment_templates.json` 的 `abilityBuffs` 行
//  补 `enchantmentId` / `enchantmentPathId` 两列。原版 `CardScript__AddEnchantment.c:24-33` 第一句读
//  `newEffect.enchantment(+0x88)`；我们这一列此前**没有** ⇒ `buffType 12`（13 行）/ `15`（5 行）
//  共 **18 行**接不上（引擎知道要挂一张附魔，却不知道挂哪张）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/CardScript__AddEnchantment.c:24-33`（读 `+0x88` 那一句）
//   · `dump.cs` 的 `CardEffect`（偏移 → `119082-119105`）
//   · 生成器那一半 = `工具/gen_cards_engine.py` 的 `fill_enchantment_pptr_columns`（含口径长注释）
//   · 数据面实测（2026-11-02 现读，18 行）：11 行 → `Night` · 3 行**自挂**（`Spreading Corruption`）·
//     2 行 → `Z-Sororitas-Raging Storm_enchantment1`（**2026-11-02 `B116_W28` 起已收进表里** ⇒ 不再出声）·
//     2 行 `m_PathID: 0`。B4 的「表外」分母随之 2 → **0**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：生成器里把 `enchantmentId` 改成恒 `""`（不查 `truth_by_path`）⇒ A1″/A2″ 红。
//   · A 段：把 `enchantmentPathId` 从「原值」改成「解出的 id」⇒ A1′ 红（-3433521700518459879 变成字符串）。
//   · A 段：把 `pid == 0` 那一档也当成「解不出」去报错 / 拿 `-1` 顶替 ⇒ A3′ 红（`m_PathID: 0` 是**原值**）。
//   · B 段：`AbilityBuffSpec` 的映射漏掉两格（`MapAbilityBuffs` 不填）⇒ B1/B3 全红（17 行全变成 0/null）。
//   · B 段：有人把这两列也塞进卡表（`ability_buffs_of`）⇒ C1 红（`cards_engine.json` 必须逐字节不变）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W18`：`B-328` —— `abilityBuffs` 的 `enchantmentId` / `enchantmentPathId` 两列。</summary>
public static class B116W18_Assert
{
    const string P = "[B116W18] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool EqT(long got, long want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    static bool EqS(string got, string want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（`{got ?? "null"}`）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 `{got ?? "null"}`，期望 `{want ?? "null"}`"); return false;
    }

    // ============================================================ 数据面常量（2026-11-02 现读）
    //  ⚠️ 这些是**真值**（原版 PPtr / 模板 id），不是我们挑的参数 —— 表变了就该红（提醒重算口径）。
    const string WAR_STORM_ID = "1c7892a2816a53d4388d6dc5272853b6";
    const string NIGHT_ID = "77af39fae01dff044ac948424e5479c1";
    const long NIGHT_PTR = -3433521700518459879L;

    const string SPREADING_ID = "875eedc90a1608d49a95b34a863ca104";      // 自挂
    const long SPREADING_PTR = -259810654571001761L;

    const string DEMONIC_FEAST_ID = "ba33eab5a0587d34d8809cfae2f14058";  // 两行 `m_PathID: 0`
    const string RAGING_STORM_OUT_ID = "2ef404c4fa412f840b8b6fa5ea6469a2"; // 收录闸② 收进来的（原「表外」，现读在表里）

    static IReadOnlyList<CardDatabase.AbilityBuffSpec> RowsOf(List<CardDef> tmpls, string id)
    {
        if (tmpls == null) return null;
        for (int i = 0; i < tmpls.Count; i++)
        {
            var t = tmpls[i];
            if (t != null && t.Id == id) return CardDatabase.AbilityBuffsOf(t);
        }
        return null;
    }

    static CardDatabase.AbilityBuffSpec Row(IReadOnlyList<CardDatabase.AbilityBuffSpec> rows, int ab, int lg, bool hand)
    {
        if (rows == null) return null;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r != null && r.Ab == ab && r.Lg == lg && r.IsHandBuff == hand) return r;
        }
        return null;
    }

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            // 先把卡表与第二张表的**两个写口**都跑过：`Parse()` 落 `_abilityBuffsById`（池卡那半边）、
            // `LoadEnchantmentTemplates()` 落模板那半边（两处同走 `RecordAbilityBuffs`，⛔ 不是两张表）。
            CardDatabase.Load();
            var tmpls = CardDatabase.LoadEnchantmentTemplates();
            Ok(tmpls != null && tmpls.Count > 0,
               "前提：第二张表读到了（`CardDatabase.LoadEnchantmentTemplates`）");
            if (tmpls == null || tmpls.Count == 0) return;

            SectionA_ThreeRows(tmpls);
            SectionB_Distribution(tmpls);
            SectionC_CardTableUntouched();
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B116W18 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================
    //  A · 抽 3 行（2 行有 PPtr + 1 行 `m_PathID: 0`）
    // ============================================================
    static void SectionA_ThreeRows(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- A · 抽 3 行：`Night` 指向 / 自挂 / `m_PathID: 0` ---");

        // A1：`Warp Storm` 的 (ab 0, lg 0) → `Night`（现读 11 行都指向它）
        {
            var rows = RowsOf(tmpls, WAR_STORM_ID);
            Ok(rows != null && rows.Count > 0, "A1 **前提**：`Warp Storm` 的行读到了");
            var r = Row(rows, 0, 0, false);
            Ok(r != null, "A1′ **前提**：它有 (ab 0, lg 0, buffEffect) 这一行");
            if (r != null)
            {
                EqT(r.EnchantmentPathId, NIGHT_PTR,
                    "A1″ 🧨 PPtr **原值**照落（`-3433521700518459879`；改成「解出的 id」⇒ 这里红）");
                EqS(r.EnchantmentId, NIGHT_ID,
                    "A1‴ 🧨 解出的模板 id = `Night`（生成器不查 `truth_by_path` ⇒ 这里红）");
            }
        }

        // A2：`Spreading Corruption` 的 (ab 1, lg 0) → **自挂**（PCtr 指向它自己）
        {
            var rows = RowsOf(tmpls, SPREADING_ID);
            Ok(rows != null && rows.Count > 0, "A2 **前提**：`Spreading Corruption` 的行读到了");
            var r = Row(rows, 1, 0, false);
            Ok(r != null, "A2′ **前提**：它有 (ab 1, lg 0, buffEffect) 这一行");
            if (r != null)
            {
                EqT(r.EnchantmentPathId, SPREADING_PTR, "A2″ 🧨 自挂那一行的 PPtr 原值");
                EqS(r.EnchantmentId, SPREADING_ID,
                    "A2‴ 🧨 解出来的 id 就是它自己（`BL74` 那张池卡的附魔模板）");
            }
        }

        // A3：`Daemonic Feast` 的 (ab 0, lg 0) —— **`m_PathID: 0`**：原版 PPtr 的 null，**不是错误**
        {
            var rows = RowsOf(tmpls, DEMONIC_FEAST_ID);
            Ok(rows != null && rows.Count > 0, "A3 **前提**：`Daemonic Feast` 的行读到了");
            var r = Row(rows, 0, 0, false);
            Ok(r != null, "A3′ **前提**：它有 (ab 0, lg 0, buffEffect) 这一行");
            if (r != null)
            {
                EqT(r.EnchantmentPathId, 0L,
                    "A3″ 🧨 `m_PathID: 0` 照落 **0**（拿 `-1` 顶 / 当成「解不出」⇒ 这里红）");
                Ok(string.IsNullOrEmpty(r.EnchantmentId),
                   "A3‴ 🧨 同上：`enchantmentId` 为空 =「这一行没有附魔指向」，⛔ 不许编一个 id");
            }
        }
    }

    // ============================================================
    //  B · 全表分布（18 行 / 16 有 PPtr / 2 个 null / 0 解不出 / 0 行指向表外）
    // ============================================================
    static void SectionB_Distribution(List<CardDef> tmpls)
    {
        Debug.Log(P + "--- B · 全表分布：18 行 · 16 有 PPtr · 0 解不出 ---");
        var tids = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < tmpls.Count; i++) if (tmpls[i] != null) tids.Add(tmpls[i].Id);

        int attachRows = 0, withPtr = 0, nullPtr = 0, unresolved = 0, offTable = 0;
        var offIds = new List<string>();
        for (int i = 0; i < tmpls.Count; i++)
        {
            var t = tmpls[i];
            if (t == null) continue;
            var rows = CardDatabase.AbilityBuffsOf(t);
            if (rows == null) continue;
            for (int j = 0; j < rows.Count; j++)
            {
                var r = rows[j];
                if (r == null) continue;
                if (r.BuffType != 12 && r.BuffType != 15 && r.BuffType != 16) continue;
                attachRows++;
                if (r.EnchantmentPathId == 0) { nullPtr++; continue; }
                withPtr++;
                if (string.IsNullOrEmpty(r.EnchantmentId)) { unresolved++; continue; }
                if (!tids.Contains(r.EnchantmentId)) { offTable++; if (!offIds.Contains(r.EnchantmentId)) offIds.Add(r.EnchantmentId); }
            }
        }
        EqT(attachRows, 18, "B1 `12`/`15`/`16` 合计 18 行（`B116W8_Assert.C1` 的同一组分母）");
        EqT(withPtr, 16, "B2 其中 **16** 行带非零 PPtr（另 2 行 `m_PathID: 0`）");
        EqT(nullPtr, 2, "B2′ `m_PathID: 0` 的行 = **2**（`Daemonic Feast`）");
        EqT(unresolved, 0, "B3 🧨 **0 行**「有 PPtr 但解不出模板 id」（非 0 ⇒ 生成器要出声、这里红）");
        EqT(offTable, 0,
            "B4 🧨 指向**表外**模板的行 = **0**（`Z-Sororitas-Raging Storm_enchantment1` 已被"
          + "「被非零 PPtr 指向」这条收录闸收进表里；非 0 ⇒ 收录闸又收窄回去了）");
        Ok(tids.Contains(RAGING_STORM_OUT_ID) && offIds.Count == 0,
           $"B4′ 那个 id 现在**在**本表里（`tids` 命中={tids.Contains(RAGING_STORM_OUT_ID)} · 表外 id 数={offIds.Count}）");
    }

    // ============================================================
    //  C · 反证：卡表那一列**不带**这两列（`cards_engine.json` 必须逐字节不变）
    // ============================================================
    static void SectionC_CardTableUntouched()
    {
        Debug.Log(P + "--- C · 反证：`cards_engine.json` 的 `abilityBuffs` 不带这两列 ---");
        // `BL74 Spreading Corruption` 是池卡，它的 (ab 0, lg 1) 就是那条 `buffType 15` 行 ——
        // 卡表的行**没有** `enchantment` 这一格（生成器只在模板表补）⇒ 两格必须是默认值。
        var rows = CardDatabase.AbilityBuffsOf("BL74");
        Ok(rows != null && rows.Count > 0, "C0 **前提**：池卡 `BL74` 的 `abilityBuffs` 读到了");
        if (rows == null) return;
        int nz = 0;
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] != null && (rows[i].EnchantmentPathId != 0 || !string.IsNullOrEmpty(rows[i].EnchantmentId))) nz++;
        EqT(nz, 0,
            "C1 🧨 卡表那几行两格全是默认值（**`cards_engine.json` 逐字节不变**的间接判据；"
          + "有人把这两列也塞进 `ability_buffs_of` ⇒ 这里红）");
    }
}
