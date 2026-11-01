// B114_W18_Assert.cs — 第一百一十四会话 · 写手 `W-18`（2026-11-01）· 账 `B-248` 的断言
//
// ============================ 它盯的是什么 ============================
//  `Resources/Art/card_display_effects.json` 的**新列** `sprite` / `hasSpriteName`
//  （= 原版 `Image.m_Sprite` 的**资产名**，由 `工具/gen_mainmenu_effects.py` 写出来 —— 本波之前
//   这一列**不存在**，只有布尔 `hasSprite`，消费方只能按**节点名**硬编码，见 `CardDisplayEffects.BorderSprite`）。
//
//  判据 = 原版主菜单场景里那几个 `Image` 的 `m_Sprite` PPtr（`B-268`/`W-8` 已逐条读出，本文件只做客方核对）：
//    · `Level Up Effect`     （Image 2545）`m_Sprite = {m_FileID 6, m_PathID 1588741661991143589}` → **`OctagonUI Filled SDF`**
//    · `Level Up Effect Down`（Image 2057）`m_Sprite = {m_FileID 7, m_PathID 8880629346401627699}` → **`OctagonUI Border SDF`**
//    · `Side Border Left/Right`            `m_Sprite = 0` ⇒ 纯色块（`hasSprite = false`）
//  名字反查的判据与做法（为什么不能只靠新解包）见生成器里 `sprite_name_of` 的注释；
//  全文与「重生成前后逐条比对结果」→ `资料/完全复刻/报告/B114_W18_CDE_sprite名.md`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · `C1` 两件升级件的 `sprite` **必须不同** —— 谁让生成器「都写同一个名字 / 都写死一张图」⇒ 红。
//  · `C2` `sprite` **不得等于节点名** —— 谁把这一列偷懒写成 `"sprite": name`（从节点名复读）⇒ 红
//    （这正是本波要根治的病：名字必须来自 `m_Sprite` 的 PPtr，不是来自节点名）。
//  · `C3` 两档必须**并存**（至少一条 `hasSprite=false && sprite==""`、至少一条 `hasSprite=true && sprite!=""`）
//    —— 谁把这一列写成常量（恒空或恒有值）⇒ 红。
//  · `C4` 名字里**带空格**（= 原版资产名原样）—— 谁在下游把它「转成文件名下划线版」再写回 JSON ⇒ 红。
//  · `B4` 两个名字在我们盘上**真的取得回 Texture2D**（`CardArt.MenuUi` 换成下划线名）——
//    谁把名字打错一个字母（看着仍然「像」）⇒ 红；两个 Texture 还**必须是不同实例**。
//  · 所有「前提」都是**硬断言**（⛔ 不是 `if (json) Check(...)` 那种静默跳过）：
//    JSON 取不到 / 新列不存在 / 段解析不出 4 条 —— 一律**红**。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W18_Assert.Run()`；宿主 `Editor/BattleScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加（⚠️ 2026-11-01 `B114_W26` 现核订正：`BattleScene` 那行用的是局部变量 **`pass` / `fail`**，不是 `_sink`；照旧文抄会 `CS0103`）：
//      B114_W18_Assert.Run(); pass += B114_W18_Assert.Pass; fail += B114_W18_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 只读 `Resources/Art/card_display_effects.json` 的**文本**，走一个够用的小扫描器（逐行按键名取标量值），
//    ⛔ **不经过** `CdeDoc` DTO —— 那两个字段此刻还没进 DTO（`Shell/CardDisplayEffects.cs` 本波归 `W-17`）。
//  · 扫描器只认本生成器那种「一键一行、`indent = 1`」的写法（文件由 `json.dumps(..., indent=1)` 产出）；
//    换格式（压缩成一行）会让它解析出 0 条 ⇒ **前提断言当场红**，不会静默放行。
//  · `⑥` 那一段用**反射**直调生产代码的私有 `CardDisplayEffects.SpriteFor(CdeImage)`（合成条目，⛔ 不是另写一份逻辑），
//    只读生产侧的 `_warnedSpriteName` 闸来判「回退真的走到出声那一支」—— 这是唯一不跑游戏也能验它的办法。
//  · 本文件不建任何 GameObject、不写盘（只 `Resources.Load` 几张图 + 反射调两个私有成员）。
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-18`：账 `B-248` —— `card_display_effects.json` 的 `sprite` / `hasSpriteName` 两列。</summary>
public static class B114_W18_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W18] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ---- 判据常量（原版资产名，**独立于生成器与生产代码**；出处见文件头两条 PPtr）----
    const string WantLevelUp = "OctagonUI Filled SDF";
    const string WantLevelUpDown = "OctagonUI Border SDF";

    /// <summary>本文件的图件条目数（生成器 `IMAGE_NODES` 四条；少一条 = 判据文件被截断）。</summary>
    const int WantImageCount = 4;

    /// <summary>`images[]` 里一条图件里本文件关心的四个标量。</summary>
    sealed class Entry
    {
        public string Name = "";
        public bool HasSprite;
        /// <summary>原文里的 `sprite` 值（**未转义**的字面量；`null` = 这一条**没有这个键** = 旧文件）。</summary>
        public string Sprite;
        /// <summary>`hasSpriteName` 键**在不在**（`null` = 没有这个键）。</summary>
        public bool? HasSpriteName;
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W18（B-248）：`card_display_effects.json` 新列 `sprite` / `hasSpriteName`");

        var ta = Resources.Load<TextAsset>(CardDisplayEffects.DtoPath);
        Tr(ta != null, "A0 **前提**：`Resources/" + CardDisplayEffects.DtoPath + ".json` 取得到"
                     + "｜🧨 删掉那个文件 ⇒ 红（本断言**不许**在取不到时静默跳过）");
        if (ta == null) { Report(); return; }

        string text = ta.text;
        var list = ParseImages(text);

        // ---------------------------------------------------------------- ① 新列存在（前提）
        C(list.Count, WantImageCount,
          "A1 **前提**：`images` 段解析出 " + WantImageCount + " 条图件（生成器 `IMAGE_NODES` 四条）"
        + "｜🧨 判据文件被截断 / 换成压缩成一行 ⇒ 红");

        int withKeys = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i].Sprite != null && list[i].HasSpriteName.HasValue) withKeys++;
        C(withKeys, list.Count,
          "A2 ★★ **前提（新列存在）**：每条图件都带 `sprite` **和** `hasSpriteName` 两个键（" + withKeys + "/" + list.Count + "）"
        + "｜🧨 拿回 2026-10-27 之前那份 JSON（只有 `hasSprite`）⇒ 红。"
        + "这条就是「旧判据文件」的总闸 —— `W-18` 之前这一列根本不存在");

        // ---------------------------------------------------------------- ② 有 sprite 的两条：名字与 hasSprite 自洽
        var up = Get(list, "Level Up Effect");
        var down = Get(list, "Level Up Effect Down");
        var sl = Get(list, "Side Border Left");
        var sr = Get(list, "Side Border Right");

        Tr(up != null,  "A3 **前提**：`Level Up Effect` 那条在图件表里");
        Tr(down != null, "A3b **前提**：`Level Up Effect Down` 那条在图件表里");
        if (up != null)
        {
            C(up.HasSprite, true,
              "B0 **前提**：`Level Up Effect` 的 `hasSprite = true`（原版 `m_Sprite` PPtr 非空）");
            C(up.Sprite, WantLevelUp,
              "B1 ★★ `Level Up Effect` 的 `sprite` = `" + WantLevelUp + "`（原版 Image 2545 → pathID 1588741661991143589）"
            + "｜🧨 写成 `Level Up Effect`（节点名）或把两件写反 ⇒ 红");
            C(up.HasSpriteName, (bool?)true,
              "B1b `Level Up Effect` 的 `hasSpriteName = true`（拿到了名字）");
        }
        if (down != null)
        {
            C(down.HasSprite, true,
              "B2 **前提**：`Level Up Effect Down` 的 `hasSprite = true`");
            C(down.Sprite, WantLevelUpDown,
              "B2 ★★ `Level Up Effect Down` 的 `sprite` = `" + WantLevelUpDown + "`（原版 Image 2057 → pathID 8880629346401627699）"
            + "｜🧨 与上一件对调（Filled/Border 反了）⇒ 红 —— 原版这里是「父=实心、子=环」，别按直觉换");
            C(down.HasSpriteName, (bool?)true,
              "B2b `Level Up Effect Down` 的 `hasSpriteName = true`");
        }

        // ---------------------------------------------------------------- ③ 没有 sprite 的两条：写空串、且没名字
        if (sl != null)
        {
            C(sl.HasSprite, false, "B3 **前提**：`Side Border Left` 的 `hasSprite = false`（原版 `m_Sprite = 0` = 纯色块）");
            C(sl.Sprite, "",
              "B3b `Side Border Left` 的 `sprite = \"\"`（**空串**，⛔ 不是 null、⛔ 不是缺键）"
            + "｜🧨 生成器改成写 null / 省掉这个键 ⇒ 红");
            C(sl.HasSpriteName, (bool?)false, "B3c `Side Border Left` 的 `hasSpriteName = false`（没有 sprite 就没有名字）");
        }
        if (sr != null)
        {
            C(sr.HasSprite, false, "B3d **前提**：`Side Border Right` 的 `hasSprite = false`");
            C(sr.Sprite, "", "B3e `Side Border Right` 的 `sprite = \"\"`");
            C(sr.HasSpriteName, (bool?)false, "B3f `Side Border Right` 的 `hasSpriteName = false`");
        }

        // ---------------------------------------------------------------- ④ 名字在我们盘上是**真图**（不是编的字符串）
        Texture2D tu = up != null && !string.IsNullOrEmpty(up.Sprite) ? CardArt.MenuUi(up.Sprite.Replace(' ', '_')) : null;
        Texture2D td = down != null && !string.IsNullOrEmpty(down.Sprite) ? CardArt.MenuUi(down.Sprite.Replace(' ', '_')) : null;
        Tr(tu != null, "B4 **前提**：`" + WantLevelUp + "` 那张图我们盘上真的取得到"
                     + "（`CardArt.MenuUi` · 空格换下划线 = `OctagonUI_Filled_SDF`）"
                     + "｜🧨 JSON 里的名字打错一个字母 ⇒ 红");
        Tr(td != null, "B4b **前提**：`" + WantLevelUpDown + "` 那张图也取得到（`OctagonUI_Border_SDF`）");
        if (tu != null && td != null)
            Tr(tu != td, "B4c ★ 两张是**不同**的 Texture（Filled ≠ Border）"
                       + "｜🧨 让生成器把两件都写成同一个名字 ⇒ 红");

        // ---------------------------------------------------------------- ⑤ 灭自证（结构性反证）
        Tr(up != null && down != null && up.Sprite != null && up.Sprite != down.Sprite,
           "C1 ★★ 灭自证：两件升级件的 `sprite` **必须不同**（Filled vs Border）"
         + "｜🧨 生成器「一律写同一张」⇒ 红");
        Tr(up != null && up.Sprite != null && up.Sprite != up.Name && down != null && down.Sprite != null && down.Sprite != down.Name,
           "C2 ★★ 灭自证：`sprite` **不得等于节点名**（名字必须来自 `m_Sprite` 的 PPtr，不是从 `name` 复读）"
         + "｜🧨 生成器写成 `\"sprite\": name` ⇒ 红。这正是本波要根治的那个病");
        bool hasEmpty = false, hasNamed = false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Sprite == null) continue;
            if (!list[i].HasSprite && list[i].Sprite == "") hasEmpty = true;
            if (list[i].HasSprite && list[i].Sprite != "") hasNamed = true;
        }
        Tr(hasEmpty && hasNamed,
           "C3 ★★ 灭自证：两档**并存**（有 `hasSprite=false` + 空串的条目，也有 `hasSprite=true` + 非空名字的条目）"
         + "｜🧨 生成器把这一列写成常量（恒空 / 恒有值）⇒ 红");
        Tr(up != null && up.Sprite != null && up.Sprite.IndexOf(' ') >= 0,
           "C4 ★ 灭自证：`sprite` 里**带空格**（= 原版资产名原样，如 `OctagonUI Filled SDF`）"
         + "｜🧨 生成器或下游把它转成文件名下划线版再写回 JSON ⇒ 红");

        SectionFallback();

        Report();
    }

    // ============================================================ ⑥ `SpriteFor` 的**三档优先级**与「回退真的出声」
    /// <summary>直接调生产代码的私有 `CardDisplayEffects.SpriteFor(CdeImage)`（反射），用**合成条目**把三条路各走一遍：
    /// ① 判据有名字 ⇒ 用判据（**哪怕与硬编码表不一致**）· ② 判据没有名字 ⇒ 回退硬编码 + **真的走到出声那一支** ·
    /// ③ 判据名字取不到 ⇒ 也回退。⛔ 这不是「再实现一遍」，是量**生产那一段**的实际分支。</summary>
    static void SectionFallback()
    {
        MenuCheck.Section(_sink, "② `SpriteFor`：判据优先 / 缺失才回退硬编码 / 回退要真出声");

        var mi = typeof(CardDisplayEffects).GetMethod("SpriteFor", BindingFlags.NonPublic | BindingFlags.Static);
        Tr(mi != null, "D0 **前提**：`CardDisplayEffects.SpriteFor(CdeImage)` 存在（私有静态）"
                     + "｜🧨 有人把它删掉/改名（回退逻辑消失）⇒ 红");

        var warnFlag = typeof(CardDisplayEffects).GetField("_warnedSpriteName", BindingFlags.NonPublic | BindingFlags.Static);
        Tr(warnFlag != null, "D0b **前提**：出声闸 `_warnedSpriteName` 存在"
                           + "｜🧨 去掉「只出声一次」的闸 ⇒ 红");
        if (mi == null || warnFlag == null) return;

        // ① 判据有名字且与硬编码表**不一致** ⇒ 必须听判据的（证明「优先读 JSON」而不是「硬编码优先」）
        var a = new CardDisplayEffects.CdeImage { name = "Level Up Effect", hasSprite = true, hasSpriteName = true, sprite = "OctagonUI Border SDF" };
        var ta = (Texture2D)mi.Invoke(null, new object[] { a });
        C(ta == null ? "<null>" : ta.name, "OctagonUI_Border_SDF",
          "D1 ★★ 判据优先：条目 `Level Up Effect` 但判据写着 `OctagonUI Border SDF` ⇒ 取的是**判据那张**"
        + "｜🧨 把 `SpriteFor` 改回「先查 `BorderSprite(im.name)`」⇒ 会拿到 Filled ⇒ 红（这条是本波「不再按节点名硬编码」的落地闸）");

        // ② 判据**没有**名字（旧 JSON 那一档）⇒ 回退硬编码，而且**真的走了出声那一支**（闸被翻 true）
        warnFlag.SetValue(null, false);
        var b = new CardDisplayEffects.CdeImage { name = "Level Up Effect Down", hasSprite = true, hasSpriteName = false, sprite = "" };
        var tb = (Texture2D)mi.Invoke(null, new object[] { b });
        C(tb == null ? "<null>" : tb.name, "OctagonUI_Border_SDF",
          "D2 ★★ 回退：判据里没名字 ⇒ 按节点名回退到 `BorderSprite`（`Level Up Effect Down` → `OctagonUI_Border_SDF`）");
        C((bool)warnFlag.GetValue(null), true,
          "D2b ★★ **回退真的出声**：走回退之后 `_warnedSpriteName` 必须被翻成 `true`（= 那一支 `Debug.LogWarning` 真的执行过）"
        + "｜🧨 把 `WarnSpriteFallback` 删掉 / 改成静默 return ⇒ 红（本波红线：回退不许静默）");

        // ③ 判据给了名字但那张图取不到 ⇒ 仍要回退 + 出声（闸复位后再验一次）
        warnFlag.SetValue(null, false);
        var c3 = new CardDisplayEffects.CdeImage { name = "Level Up Effect", hasSprite = true, hasSpriteName = true, sprite = "__no_such_sprite__" };
        var tc = (Texture2D)mi.Invoke(null, new object[] { c3 });
        C(tc == null ? "<null>" : tc.name, "OctagonUI_Filled_SDF",
          "D3 ★ 判据名字取不到 ⇒ 回退硬编码（`Level Up Effect` → `OctagonUI_Filled_SDF`）");
        C((bool)warnFlag.GetValue(null), true,
          "D3b ★ 丢名字那一档也**真的出声**（`_warnedSpriteName` 翻 true）");
    }

    static void Report()
    {
        Debug.Log(_sink.P + "B114W18 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static Entry Get(List<Entry> list, string name)
    {
        for (int i = 0; i < list.Count; i++) if (list[i].Name == name) return list[i];
        return null;
    }

    // ============================================================ 小扫描器（只认 `json.dumps(indent=1)` 的写法）
    /// <summary>把 `"images": [ … ]` 段里的每条图件解析成 <see cref="Entry"/>。
    /// ⚠️ 只认「一个键一行」的排版（生成器就是 `json.dumps(..., indent=1)`）；格式一变 ⇒ 返回空表 ⇒ 前提断言红。</summary>
    static List<Entry> ParseImages(string text)
    {
        var list = new List<Entry>();
        string[] lines = text.Split('\n');
        bool inImages = false;
        Entry cur = null;
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].Trim();
            if (!inImages)
            {
                if (t.StartsWith("\"images\"")) inImages = true;
                continue;
            }
            if (t == "]" || t == "],") break;                       // `images` 段结束
            if (t.StartsWith("\"name\":")) { cur = new Entry(); cur.Name = StrOf(t); list.Add(cur); continue; }
            if (cur == null) continue;
            if (t.StartsWith("\"hasSprite\":")) { cur.HasSprite = t.Contains("true"); continue; }
            if (t.StartsWith("\"sprite\":")) { cur.Sprite = StrOf(t); continue; }
            if (t.StartsWith("\"hasSpriteName\":")) { cur.HasSpriteName = t.Contains("true"); continue; }
        }
        return list;
    }

    /// <summary>`   "key": "value",` → `value`（去引号、去行尾逗号）。值不是字符串时给原文。</summary>
    static string StrOf(string line)
    {
        int c = line.IndexOf(':');
        if (c < 0) return "";
        string v = line.Substring(c + 1).Trim();
        if (v.EndsWith(",")) v = v.Substring(0, v.Length - 1).Trim();
        if (v.Length >= 2 && v[0] == '"' && v[v.Length - 1] == '"') v = v.Substring(1, v.Length - 2);
        return v;
    }
}
