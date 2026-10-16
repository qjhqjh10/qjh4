// PositionOption.cs — 原版 `PositionOption` 枚举（**逐值照抄 dump，⛔ 别改数值**）
//
// 出处：`d:/2/tools/il2cpp_out/dump.cs:25161`（`TypeDefIndex 494`，`public enum PositionOption`，
//       **36 个具名值**，`self = 0` … `enemyHandWorld = 155`）。
//
// 它是什么：原版 `AnimInfo`（`dump.cs:25205`）里那三个字段的类型 ——
//   `startPosOption` / `endPosOption` / `pointTowardsOption`（偏移见 `dump.cs`：0x18 / 0x1C / 0x20）
//   ⇒ 也就是 `数据/游戏数据/card_anim_map.json` 里那 506 条 cardAnim 的**起点/终点/朝向**。
//
// 🔴 **解析它的是 `BattleManager.GetAnimTransform`，而那个方法【只实现了 10 档】**：
//   `0 self` · `1 cardTarget` · `2 abilityTarget` · `3 friendlyHero` · `4 enemyHero` · `5 boardCenter` ·
//   `14 nextFriendlyMinionSpot` · `18 friendlyHand` · `19 enemyHand` · `50 friendlyDeck` · `55 enemyDeck`，
//   **其余全部 = `CustomDebug.LogError`（「这一档本不该走到」）+ 退回我方督军的 `EffectAnchor`**。
//   ⇒ ⛔ **别照这个枚举去实现 36 档** —— 多写的档反而与原版不一致。
//   判据全文（逐档映射 + 每个 `+0x??` 的字段名）→ `资料/任务线/战斗线/任务/T05_逐卡演出与特效接线.md` §2·6。
//
// ⚠️ 实测数据里真出现过的取值（`animinfo_lookup.json` 的 `sp`/`ep`）：
//   `0/2/3/4/5/12/13/14/15/18/19/50` —— 其中 **`12`/`13`（能量）/`15`（敌方空位）落进「其余」那一档**
//   ⇒ **原版那两档本来就是报错 + 退回督军锚点**（不是我们缺件）。

namespace CardPresentation
{
    /// <summary>原版 `PositionOption`（`dump.cs:25161`）。**数值逐字照抄**，⛔ 别重排、别改名。</summary>
    public enum PositionOption
    {
        self = 0,
        cardTarget = 1,
        abilityTarget = 2,
        friendlyHero = 3,
        enemyHero = 4,
        boardCenter = 5,
        boardCenterUI = 6,
        cemetery = 7,
        friendlyPower = 8,
        enemyPower = 9,
        nextToCemetery = 10,
        nextToClock = 11,
        friendlyMana = 12,
        enemyMana = 13,
        nextFriendlyMinionSpot = 14,
        nextEnemyMinionSpot = 15,
        secondFriendlyMinionSpot = 16,
        thirdFriendlyMinionSpot = 17,
        friendlyHand = 18,
        enemyHand = 19,
        friendlyDeck = 50,
        enemyDeck = 55,
        friendlyPsychicMana = 60,
        friendlySpiritStoneMana = 61,
        enemyPsychicMana = 70,
        enemySpiritStoneMana = 71,
        nextToFriendlyPsychicMana = 80,
        nextToEnemyPsychicMana = 90,
        friendlyPlasmaMana = 100,
        enemyPlasmaMana = 110,
        nextToFriendlyPlasmaMana = 120,
        nextToEnemyPlasmaMana = 130,
        createCardPos = 140,
        stratagemGeneralShowCase = 145,
        friendlyHandWorld = 150,
        enemyHandWorld = 155,
    }
}
