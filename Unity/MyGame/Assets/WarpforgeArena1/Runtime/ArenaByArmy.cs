// ArenaByArmy.cs — 「本局打哪个战场」的原版查表（**运行时**，2026-09-25）
//
// 🔴 **为什么要在运行时**：这张表的使用点是**开战那一刻**（战斗入口那几扇窗知道玩家选的卡组阵营），
//    而原来的落点 `ArenaBuilder` 是 **Editor 类** —— 运行时引用不到它（`Assets/WarpforgeArena1/Editor/`）。
//    ⇒ 判据放这里、编辑器与外壳**共用这一份**（`ArenaBuilder.ArenaForArmy` 只是转发）。
//
// **原版判据**（判据只写一处 ⇒ `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六）：
//   `SearchOpponentManager__StartBattle.c:57` `BattleArenaByArmySO.GetBattleArena(army)`
//     → `:61` `EverguildSceneManager.LoadScene(场景名)`
//   表本体 = `Warpforge_Data/sharedassets0.assets` · MonoBehaviour · **pathID 418** · **512 B** ·
//   14 条 `[int32 cardArmy][int32 len][场景名]` + 末尾 `defaultBattleArena`。
//   传进去的 `army` = **本地玩家的督军阵营**；只有 11 种竞技 matchType **且本地先手**时才覆盖成对手的。
//
// ✅ **2026-09-25 更正**：这张表**一直在本地**，上一轮记的「本地没有」是**误判**。
//    **错因**（会再犯，值得记）：内置文件（`level0` / `sharedassets0.assets`）里对象的 `m_Script` 是
//    **跨文件 PPtr**（指向 `globalgamemanagers.assets`）⇒ 解包器拿不到 typetree ⇒ **对象整个读不出**，
//    字段名**根本不落进 JSON**；而 player 数据里**不写字段名** ⇒
//    **拿字段名去 grep JSON dump，对内置文件是「无效否定」**。
//    （同族先例：`VarsGlobal` 整表当年也是这么漏掉的。）
//    复现办法：`UnityPy.load(...)` → `peek_name()=="BattleArenaByArmySO"` → `get_raw_data()` →
//    偏移 `0x34` = int32 数组长，逐条读 `[int32 army][int32 len][UTF-8，4 字节对齐]`，末尾读默认串。
//
// ⚠️ **原版存的是带空格的 Addressables 场景名**（`Battle Arena Aeldari`），**不是**我们的 `battlearenaX` 键
//    ⇒ 表里两列都留着：`OriginalName` 是**照字节抄的原版值**（别改写法，那是证据），`Scene` 是我们的键。
// ⚠️ **兜底 = `Battle Arena 1`**（原版 `defaultBattleArena`），**不是**「随便挑一个」。
//
// ✅ **2026-09-25：「一局一战场」已接上。** 战场几何是**建场时照 manifest 烘进场景**的
//    ⇒ 方案 = **一场一份 `Battle_<场>.unity`**（**不是**运行时实例化；三条依据见
//    `资料/阶段二_战斗入口_原版规格.md` §7·1）。建法：`WF_ARENA=<场> BattleScene.BuildAndSaveScene`
//    （会**自动进 Build Settings**）。选场 = 下面的 `BattleSceneNameFor`，两处开战点共用。
using System;
using UnityEngine;

public static class ArenaByArmy
{
    /// <summary>查不到时的兜底 —— 原版 `defaultBattleArena`。</summary>
    public const string DefaultScene = "battlearena1";

    /// <summary>`BattleArenaByArmySO` 的 14 条（**照原版数组序**，注意 `Sautekh(40)` 排在 `SaimHann(30)` **前**）。
    /// 第一列 = `CardArmy` 成员名（与我们 `cards_engine.json` 的 `faction` **逐个同名**，2026-09-25 已核）。</summary>
    public static readonly (string Army, int CardArmy, string OriginalName, string Scene)[] Rows =
    {
        //  Army              CardArmy   原版场景名（照字节抄）               我们的场景键
        ("Neutral",               0,   "Battle Arena 1",                  "battlearena1"),
        ("Ultramarines",         10,   "Battle Arena 1",                  "battlearena1"),
        ("Goff",                 20,   "Battle Arena 2",                  "battlearena2"),
        ("Sautekh",              40,   "Battle Arena 3",                  "battlearena3"),
        ("SaimHann",             30,   "Battle Arena Aeldari",            "battlearenaaeldari"),
        ("BlackLegion",          50,   "Battle Arena Black Legion",       "battlearenablacklegion"),
        ("Leviathan",            60,   "Battle Arena Leviathan",          "battlearenaleviathan"),
        ("TauEmpire",            70,   "Battle Arena Tau Viorla",         "battlearenatauviorla"),
        ("Sororitas",            80,   "Battle Arena Sororitas",          "battlearenasororitas"),
        ("Genestealers",         90,   "Battle Arena Genestealers",       "battlearenagenestealers"),
        ("AstraMilitarum",      100,   "Battle Arena Astra Militarum",    "battlearenaastramilitarum"),
        ("DarkAngels",          110,   "Battle Arena Dark Angels",        "battlearenadarkangels"),
        ("EmperorsChildren",    120,   "Battle Arena Emperors Children",  "battlearenaemperorschildren"),
        ("SpaceWolves",         130,   "Battle Arena Space Wolves",       "battlearenaspacewolves"),
    };

    /// <summary>照原版查表：`督军阵营 → 我们的场景键`。认不出 / 传空 ⇒ `DefaultScene`。
    /// 大小写不敏感（我们用的就是原版拼写，但别让大小写毁掉一次查表）。</summary>
    public static string SceneFor(string army)
    {
        foreach (var r in Rows)
            if (string.Equals(r.Army, army, StringComparison.OrdinalIgnoreCase))
                return r.Scene;
        return DefaultScene;
    }

    /// <summary>照原版查表：`督军阵营 → 原版场景名`（`"Battle Arena X"`）。认不出 / 传空 ⇒ 第一行那个。
    /// 用途 = **把「原版会选哪个」如实说出来**（日志 / 提示），以及在还没法换场时标明差距。</summary>
    public static string OriginalNameFor(string army)
    {
        foreach (var r in Rows)
            if (string.Equals(r.Army, army, StringComparison.OrdinalIgnoreCase))
                return r.OriginalName;
        return "Battle Arena 1";   // 原版 defaultBattleArena
    }

    /// <summary>这个名字的对战场景**载得入吗**（= 它在 Build Settings 里）。
    ///
    /// 🔴 **不能用 `Application.CanStreamedLevelBeLoaded`** —— **2026-09-25 实测踩过**：
    /// 在 `-batchmode -executeMethod` 下它把**全部 14 个**（**连已经登记好的 `Battle` 自己**）都判成 false
    /// ⇒ 13 个阵营全回落兜底，自检直接红两条。**它在那儿不可靠。**
    /// 改成**扫 Build Settings 的场景表**（`SceneManager.sceneCountInBuildSettings` +
    /// `SceneUtility.GetScenePathByBuildIndex`）—— 运行时与编辑器同一套 API，批处理下也对。
    /// ⚠️ 比的是**文件名去掉扩展名**（`LoadScene` 认的就是这个名字）。</summary>
    public static bool CanLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        int n = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < n; i++)
        {
            var p = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(p)) continue;
            if (System.IO.Path.GetFileNameWithoutExtension(p) == sceneName) return true;
        }
        return false;
    }

    /// <summary>本局该载入哪份**对战场景**。
    ///
    /// **为什么要多份**：战场几何是**建场时照 manifest 烘进场景**的（`BattleScene.BuildArena3D` →
    /// `ArenaBuilder.BuildContent`，根节点 `"Warpforge_" + mf.scene`）⇒ **一局一个战场 = 一场一份 Battle 场景**。
    /// 建法：`WF_ARENA=&lt;场&gt; BattleScene.BuildAndSaveScene` → 产出 `Battle_&lt;场&gt;.unity`（**会自动进 Build Settings**）。
    /// ⚠️ 场景在 `.gitignore` 里（`Assets/CardPresentation/Scenes/`）⇒ 多份**不占仓库**。
    ///
    /// 判据：`Battle_&lt;场&gt;` **载得入** ⇒ 用它；否则**回落 `Battle`**（= 兜底战场，见 `DefaultScene`）**并出声**。
    /// ⚠️ **不许静默回落** —— 玩家看到的战场和他选的阵营不符时，日志里必须查得出为什么
    /// （照 `ShellRuntime` 载主菜单那条的写法）。</summary>
    /// <summary>🆕 **2026-09-30（§27 架构）起恒为 `Battle`** —— 对战场景**只有一份**了。
    /// 原来返回 `Battle_<场>`（13 份场景、每份把战场烘死），现在战场由 `ArenaRuntimeLoader`
    /// 在**运行时**按 `SceneFor(督军阵营)` 从 `Resources/ArenaPrefabs/<场>.prefab` 实例化
    /// （判据只留一处：`ArenaRuntimeLoader.ResolveArenaKey`）。
    /// ⇒ **查表这件的产物不再是「场景名」，而是「哪一场」** —— 要那一个请用 `SceneFor(army)`。</summary>
    public static string BattleSceneNameFor(string army)
    {
        return BattleScene;      // 只有一份；arena 由运行时按 `SceneFor(army)` 取
    }

    /// <summary>那份唯一的对战场景名（`.gitignore` 里，由 `BattleScene.BuildAndSaveScene` 存）。</summary>
    public const string BattleScene = "Battle";
}
