// SceneStubAudit.cs — 存盘之后的**自证**：场景文件里【不许】有内联 `MonoScript` 存根（`--- !u!115`）。
//
// ============================ 为什么必须有它（A1506 / A1481 · 2026-10-11 查实） ============================
// Unity 只给「**文件名 == 类名**」的那个类建 `MonoScript` 资产。**次要类**（同一个 `.cs` 里除主类以外
// 的 `MonoBehaviour`）**没有 MonoScript** ⇒ 存场景时那个组件的 `m_Script` 指不到资产
// ⇒ Unity **当场烘一块内联 `!u!115` 存根**写进 level 文件
// ⇒ player 加载时 `Position out of bounds!` → `The file '.../levelN' is corrupted!` → **崩溃（退出码 3）**。
//
// **实测证据（修复前的备份还在，逐块可查）**：
//   · `/tmp/DeckEditor.unity.bak` —— **5** 块存根：`CardPreview`(card_ghost) ·
//     `WindowButton`(挂在 12 个 GameObject 上) · `CosmeticDraggingController`(cosm_drag) ·
//     `CardDraggingController`(card_drag) · `CosmeticPreview`(cosm_preview)
//   · `/tmp/Shell.unity.bak` —— **3** 块：`Shade` · `BlockingOverlay` · `WindowHolder`(3 个锚点)
//   这 8 个类**当时都没有同名 `.cs` 文件**（分别住在 `Core/DraggableController.cs` /
//   `Shell/PromptPopup.cs` / `Shell/ShellParts.cs` / `Shell/WindowsManager.cs` 里）。
//   🔑 **同文件对照实验**（最硬的那条判据）：`Shell/WindowsManager.cs` 的**主类** `WindowsManager`
//   **无存根**，而同文件的次要类 `WindowHolder` **有存根** —— 同一份文件、同一次存盘，差别只在类名与文件名。
//   ⇒ 2026-10-11 已把那 8 个类各自拆成独立文件（`Core/{CardPreview,CosmeticPreview,
//   CardDraggingController,CosmeticDraggingController}.cs` · `Shell/{WindowButton,Shade,BlockingOverlay,
//   WindowHolder}.cs`）。
//
// 🔴 **为什么需要这条看门狗**：那些类是**生成物**（`*Scene.BuildAndSaveScene` / `*Scene.Run` 建场后
//   `SaveScene()`），**存盘会把存根写回去** ⇒ 拆完文件不等于治好了，**每跑一次存盘就得再验一次**。
//   ⚠️ 而且第十六会话那次手工修复**只把 `m_Script` 换成 guid，没查「那个 guid 的文件主类对不对」** ⇒
//   `card_ghost`/`card_drag`/`cosm_drag`/`cosm_preview` 四处**全指向 `Core/DraggableController.cs`**
//   （主类 `DraggableController<T>`）⇒ 解析成了**别的类** ⇒ 正是 `A1481` 尾巴① 那 4 条
//   `Script attached … missing`。⇒ **「换成 guid」不是根治，拆文件才是**。
//
// ⛔ **不许静默**：有存根就 `Debug.LogError` 说清「**是哪几个类、各挂在哪些 GameObject 上**」。
// ✅ **干净时零输出**（一个字节都不打）—— 本仓自检的回归判据是 **stdout 逐字节相同**，
//    所以这条审计件**只在出问题时说话**。
//
// 📌 判据全文 → `资料/交接_第十七会话.md` §三 · `资料/任务_卡组卡牌战斗.md` 的 `A1506`。
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>存盘后的场景自证：**不许有内联 `MonoScript` 存根**（`A1506`）。干净时零输出。</summary>
public static class SceneStubAudit
{
    /// <summary>扫一遍刚存下来的场景文件。返回存根块数（**0 = 干净**；文件读不到返回 -1）。</summary>
    public static int Run(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath)) return 0;

        string[] lines;
        try { lines = File.ReadAllLines(scenePath); }
        catch (System.Exception e)
        {
            Debug.LogError("[SceneStubAudit] 读不到场景文件「" + scenePath + "」： " + e.Message
                         + " —— **没验成**（不是「验过了」）");
            return -1;
        }

        // ---- 逐行归属到它所在的 `--- !u!<class> &<id>` 块 ----
        var ownerClass = new int[lines.Length];
        var ownerId = new long[lines.Length];
        int curClass = 0; long curId = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("--- !u!"))
            {
                curClass = ParseClass(lines[i]); curId = ParseId(lines[i]);
            }
            ownerClass[i] = curClass; ownerId[i] = curId;
        }

        // ---- 三张表：GameObject 名 / 存根的 m_ClassName / MonoBehaviour 挂在哪颗 GO 上 ----
        var goName = new Dictionary<long, string>();
        var stubClass = new Dictionary<long, string>();
        var mbGo = new Dictionary<long, long>();
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].Trim();
            if (ownerClass[i] == 1 && t.StartsWith("m_Name:"))
                goName[ownerId[i]] = Unquote(t.Substring(7).Trim());

            else if (ownerClass[i] == 115 && t.StartsWith("m_ClassName:"))
                stubClass[ownerId[i]] = t.Substring(12).Trim();

            else if (ownerClass[i] == 114 && t.StartsWith("m_GameObject:"))
            {
                long g;
                if (TryRefId(t, out g)) mbGo[ownerId[i]] = g;
            }
        }
        if (stubClass.Count == 0) return 0;   // ✅ 干净

        // ---- 谁在引用这些存根 ----
        var owners = new Dictionary<long, List<string>>();
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].Trim();
            if (!t.StartsWith("m_Script:")) continue;
            long sid;
            if (!TryRefId(t, out sid) || !stubClass.ContainsKey(sid)) continue;
            long go; mbGo.TryGetValue(ownerId[i], out go);
            string nm;
            if (!goName.TryGetValue(go, out nm)) nm = "<未知 GameObject &" + go + ">";
            List<string> lst;
            if (!owners.TryGetValue(sid, out lst)) { lst = new List<string>(); owners[sid] = lst; }
            lst.Add(nm);
        }

        var sb = new StringBuilder();
        sb.Append("[SceneStubAudit] 🔴 「").Append(scenePath).Append("」里有 **")
          .Append(stubClass.Count).Append("** 块内联 `MonoScript` 存根（`--- !u!115`）—— ")
          .Append("这些类**没有同名 `.cs` 文件**，player 读这个 level 会 `Position out of bounds!` → corrupted → 崩溃。\n")
          .Append("  判据 / 修法 → `A1506`：把类拆成同名独立文件，然后**重新建场存盘**。\n");
        foreach (var kv in stubClass)
        {
            List<string> who;
            sb.Append("  · &").Append(kv.Key).Append("  m_ClassName=`")
              .Append(string.IsNullOrEmpty(kv.Value) ? "<空>" : kv.Value).Append("`");
            if (owners.TryGetValue(kv.Key, out who))
                sb.Append("  ← 挂在 ").Append(who.Count).Append(" 个 GameObject 上：").Append(string.Join(", ", who.ToArray()));
            else
                sb.Append("  ← **没有任何 `m_Script` 引用它**（可疑：存根是孤儿）");
            sb.Append('\n');
        }
        Debug.LogError(sb.ToString());
        return stubClass.Count;
    }

    static string Unquote(string s)
    {
        if (s.Length >= 2 && s[0] == '\'' && s[s.Length - 1] == '\'') return s.Substring(1, s.Length - 2);
        return s;
    }

    /// <summary>从 `--- !u!114 &123456` 里取出 classID。</summary>
    static int ParseClass(string line)
    {
        int i = line.IndexOf("!u!");
        if (i < 0) return 0;
        int j = i + 3, k = j;
        while (k < line.Length && char.IsDigit(line[k])) k++;
        int v;
        return int.TryParse(line.Substring(j, k - j), out v) ? v : 0;
    }

    /// <summary>从 `--- !u!1 &123456` / `--- !u!115 &789` 里取出 `&` 后面那个 id。
    /// ⚠️ **`&` 的下标随 classID 位数变**（`!u!1` → 8 · `!u!114` → 10… 不固定）⇒
    /// **必须现找**，⛔ 别写死一个下标（2026-10-11 自查抓到过一次）。</summary>
    static long ParseId(string line)
    {
        int i = line.IndexOf("!u!");
        if (i < 0) return 0;
        int amp = line.IndexOf('&', i);
        if (amp < 0) return 0;
        int k = amp + 1, j = k;
        while (k < line.Length && char.IsDigit(line[k])) k++;
        long v;
        return long.TryParse(line.Substring(j, k - j), out v) ? v : 0;
    }

    /// <summary>从 `m_Script: {fileID: 123, guid: ...}` / `m_GameObject: {fileID: 45}` 里取 fileID。
    /// 🔴 **冒号后面有一个空格**（Unity 的写法就是 `{fileID: 0}`）⇒ 必须先跳过空白，
    /// 否则会恒取到空串、恒 `false`（2026-10-11 拿真数据验算法时抓到过一次）。</summary>
    static bool TryRefId(string line, out long id)
    {
        id = 0;
        int i = line.IndexOf("{fileID:");
        if (i < 0) return false;
        i += 8;
        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
        int k = i;
        while (k < line.Length && (char.IsDigit(line[k]) || line[k] == '-')) k++;
        if (k == i) return false;
        return long.TryParse(line.Substring(i, k - i), out id);
    }
}
