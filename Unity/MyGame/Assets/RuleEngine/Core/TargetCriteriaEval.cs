// TargetCriteriaEval.cs — 原版 `TargetCriteria` 的**逐候选筛选**求值器（`FilterMethods.CheckIfMeetsCriteria` 的等价物）
//
// =====================================================================================
//  这是什么 / 谁在用（🆕 2026-10-24 · 执行代理 `W2` · `C5` Q4 / `A2` Q7）
// =====================================================================================
//  v25 卡表把原版每一条 `abilityLogic[].targetCriteria`（38 格）拆成两处落：
//    · `unitConditions` 的 `role="target"` 行 → `cond` / `ex`（= `conditionsFilter` / `excludeCondition`）；
//    · `abilityTargetCriteria` / `activeAbilityTargets` 的 `criteria` → **其余 36 格**。
//  ⇒ 消费端按 `(ab, lg)` 拼回**一个** criteria，交给本类逐候选判。拼装点在
//    `EffectText.AssignAbilityTargets`（普通 ability）/ `CardDef.ApplyActiveAbilityLogic`（主动能力），
//    落在 `EffectOp.TargetCriteriaSet`；调用点 = `EffectResolver.ResolveTargets`。
//
//  🔴 **原版不存在「两列谁先」**：只有一个 `TargetCriteria` 对象，`conditionsFilter`(`+0x38`) 与
//    `excludeCondition`(`+0x3d`) 就是其中两格。本文件按反编译**逐句**的固定次序判（下表）。
//  🔴 `lg == -1` 的那 27 行是 **ability 级** criteria（触发前判，`CardAbility.targetCriteria +0x30`）
//    —— **不进**本类（本类只服务 logic 级 `lg >= 0`；那一条是另一批）。
//
// =====================================================================================
//  判据（第一权威 = `d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c` 逐句）
// =====================================================================================
//  入口：`:48-58`  `criteria == null` ⇒ LogError + **false**；`targetsAffected == 0 (any)` ⇒ **true**（其余全不看）。
//  特例档（`:60-160`，按 `targetsAffected` 短路）：
//    20 self          ⇒ 候选 == thisCard（`Object.op_Equality(param_1, …)`；取不到 ⇒ 原版抛异常 ⇒ 我们 `null`）
//    30 target        ⇒ 候选 == targetCard（同上）
//    70 abilityOwner / 40 attacker / 80 actingCard ⇒ 候选 == thisCard（80 另有 `:169 return 1` 一支）
//    90 targetIfMeetsCriteria ⇒ 候选 == targetCard **只是准入**（不等 ⇒ false），
//       **相符后继续走下面那批常规格**（`:193-201`；⚠️ 与 30 不是同一档 —— 2026-10-25 `E9` 订正）
//    140 / 150        ⇒ 候选 ∈「上一条能力的目标」静态表（`:176-206`）
//    200 / 201        ⇒ 候选 ∈ `thisCard.referencedCards`（**我们的 `ResolveTargets` 早已单独处理这两档并返回**）
//    235 warlordsTalent ⇒ 拿督军天赋卡比卡名（**未建模** ⇒ `null`）
//  🔴 **原版这一档【根本不筛】的**（🆕 2026-10-24 · `W25` · `D1` 的 R1）—— 判据 = **建池**那个函数
//    `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`（建完池直接 `goto LAB_180818485` = `return`，
//    不经过 `FilterMethods__CheckIfMeetsCriteria`；真筛在 `:1086-1095`）：
//    120 friendlyHero（`:721-736`）· 130 enemyHero（`:738-747`）· 160 currentMana（`:220-319`）·
//    161 currentSpiritStone（`:321-420`）· 162 currentFaith（`:422-521`）· 170 maxMana（`:624-719`）·
//    180 choiceResolution（`:89-182`，**仅当 `filterSpecificCards` 为空**）· 190 lastPlayedIfMeetsCriteria
//    （`:173-182`）· 220 targetCountTraits（`:192-206`）· 230 highestHealthWarlord（`:809-833`）·
//    240 lowestHealth（`:835-843`）· 245 secretsPlayed（`:523-622`）⇒ 本类**一律 `true`**。
//    ⚠️ 别照 `CheckIfMeetsCriteria.c` 反推（那是另一个函数，140/150 两处口径不同）。
//  常规档 —— 下列次序 = `CheckIfMeetsCriteria.c` 的**语句次序**（`:203-408`），全部 AND、任一不满足立刻 false：
//    ① `excludeSimilarCards`(`+0x1f`)：候选卡名 == thisCard 卡名 ⇒ false（`:203-215`）
//    ② 阵营闸 `friendlyUnits`(`+0x14`) / `enemyUnits`(`+0x15`)（`:216-228`）
//    ③ `includeSelf`(`+0x1d`) / `excludeNotSelf`(`+0x1e`)（`:232-243`）
//    ④ `excludeFriendlyHero`(`+0x21`) / `excludeEnemyHero`(`+0x22`)（`:244-272`，逐句见那一格）
//    ⑤ `includeRemnants`(`+0x1b`) == 0 且候选是残骸 ⇒ false（`:273-278`）
//    ⑥ 卡种白名单（`:280-293`）：候选 `cardType` 10 Hero ⇒ 要求 `includeHero`；0 Minion ⇒ `includeMinions`；
//       20 Tactic ⇒ `includeSpells`；40 Whispers ⇒ `includeWhispers`（那一格为「假」⇒ false）
//    ⑦ `spellType`(`+0x68`) != 0 且 != 候选 `+0x90` ⇒ false（`:294-297`）
//    ⑧ `conditionsFilter`(`+0x38`) / `excludeCondition`(`+0x3d`) → `MeetsConditionCriteria`（`:298-303`）
//    ⑨ `keywordFilter`(`+0x2c`) + `excludeRace`(`+0x30`)（`:304-313`，语义见下面 `KeywordPasses`）
//    ⑩ `traitsFilter`(`+0x34`) + `excludeTrait`(`+0x3c`) → `MeetsTraitCriteria`（`:314-317`）
//    ⑪ 四组 `MeetsStatCriteria`：attack `+0x48/+0x4c` · health `+0x50/+0x54` · cost `+0x58/+0x5c` ·
//       rarity `+0x60/+0x64`（`:318-375`；逐句实现 = 下面 `MeetsStatCriteria` / `StatGroupsPass`）
//    ⑫ `filterSpecificCards`(`+0x70`)：非空 ⇒ **白名单**（候选卡名在表里才过，`:465-483`）
//    ⑬ `excludeEnchantments`(`+0x78`)：候选身上挂了表里任一张附魔 ⇒ false（`:430-463`）
//    ⑭ `armyFilter`(`+0x28`) + `chosenArmy`(`+0x24`) → `MeetsArmyCriteria`（`:396-406`，逐值见 `ArmyPasses`）
//    ⑮ `targetsAffected == 100` ⇒ 额外要求「相邻格里有满足者」（`:407-425`）—— **本版未实现**（`null`）
//  末尾 `:407-408`：`ta != 100` ⇒ **return 1**。
//  ⚠️ `selectFromChooseResolution` / `maxTargets` / `targetSelectMethod` **不在**这个函数里
//     （它们不是「筛」：前一个是候选池来源，后两个是**裁剪**）⇒ 本类**不看**它们。
//  🔴 `includeWeapons`(`+0x1a`) / `includeHeroParts`(`+0x1c`)：**原版一次都不读**（2026-10-24 `W19` 现核）——
//     判据 = `CheckIfMeetsCriteria.c:280-293` 与 `CheckIfRawCardMeetsCriteria.c:30-34` 的卡种白名单
//     **只读** `+0x16/+0x17/+0x18/+0x19` 四格（Hero/Minions/Spells/Whispers），
//     且 `CardTypeOptions` 只有 4 个值（`Minion 0 / Hero 10 / Tactic 20 / Whispers 40`，
//     `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTypeOptions.cs`）⇒ **没有任何卡种能落到这两格**。
//     ⇒ 它们是非 0 也**不参与判定**的死字段（SO 上的历史遗留）。本类**照原版：一律不看**，
//     ⛔ 别「顺手补一个」—— 那会凭空多出一条原版没有的筛。
//
//  🔴 **`null` = 本版判不了**（不是「不满足」）：调用方必须**保留**该候选并**出声**（⛔ 不许静默多数/少数）。
//     契约与 `ConditionEval` / `CountEval` 同一个（`null` = 判不了，由上层降级并出声）。
//
//  出处：`C5_引擎战斗剩余判据.md` Q4（join 规则 + 过滤顺序）· `E9_卡表v25_logic级判据列.md`（列形状）。

using System;

namespace RuleEngine
{
    /// <summary>原版 `TargetCriteria` 的逐候选筛选（**唯一实现**；⛔ 别在调用点另写一份）。</summary>
    public static class TargetCriteriaEval
    {
        /// <summary>
        /// 一个候选过不过这份 criteria。`true` = 过 · `false` = 淘汰 · `null` = **本版判不了**（保留候选 + 出声）。
        /// </summary>
        /// <param name="owner">这条 ability 的拥有者座位（0/1）—— 对应原版 `thisCard.isPlayer(+0x40)`。</param>
        /// <param name="set">36 格 criteria（`CardDef.CountSetSpec`；`null` ⇒ 不筛 = `true`）。
        /// ⚠️ 它**也带着** `TargetsAffected` —— 本类用它走特例档（`any/self/target/...`），
        /// 但 **`maxTargets` / `targetSelectMethod` 不看**（裁剪不是筛）。</param>
        /// <param name="candidate">候选单位（棋盘上的那一个）。</param>
        /// <param name="cond">`unitConditions` 里同 `(ab,lg)` 的 `role="target"` 行（`cond`/`ex`）。
        /// `null` ⇒ 这一格**没有条件行** = `none(0)` = **放行**（⛔ 不是「判不了」）。</param>
        /// <param name="thisCard">这条 ability 的**载体单位**（`ctx.ActingUnit`）。拿不到（战术卡）⇒ `null`。</param>
        /// <param name="targetCard">玩家点选的那一个目标（原版 `targetCard`）。没有 ⇒ `null`。</param>
        public static bool? Matches(BattleContext ctx, int owner, CardDef.CountSetSpec set,
                                    UnitState candidate, CardDef.ConditionSpec cond,
                                    UnitState thisCard, UnitState targetCard,
                                    out string gap)
        {
            gap = null;
            if (set == null) return true;                 // 没有 36 格 ⇒ 不筛（`any` 之外没有东西可判）
            if (candidate == null) return false;

            int ta = set.TargetsAffected;
            if (ta == CardDef.CountSetSpec.Unknown) { gap = "`targetsAffected` 未知（没落）"; return null; }

            // ---------------- 特例档（原版 `:57-160` 的短路）----------------
            switch (ta)
            {
                case 0:                                   // any —— 原版「disregards all other filters」
                    return true;
                case 20:                                  // self
                case 40:                                  // attacker
                case 70:                                  // abilityOwner
                case 80:                                  // actingCard（`:169` 直接 return 1）
                    if (ta == 80) return true;
                    if (thisCard == null) { gap = $"`ta={ta}` 要拿 thisCard 比身份，本版没有载体"; return null; }
                    return ReferenceEquals(candidate, thisCard);
                case 30:                                  // target
                    // 原版 `:76-94`（`0x1e`）：`candidate == targetCard` —— 比完身份**直接 `return`**
                    // （满足 ⇒ 1 / 不满足 ⇒ 0），**不再走** ①..⑮ 那批常规格。与 90 分开写（见下）。
                    if (targetCard == null) { gap = $"`ta={ta}` 要拿 targetCard 比身份，这次没有点选目标"; return null; }
                    return ReferenceEquals(candidate, targetCard);
                case 90:                                  // targetIfMeetsCriteria
                    // 🔴🆕 **2026-10-25（执行代理 `E9` · `J5` §二 F5·b）：与 30 **不是**同一档。**
                    //   原版 `:193-201`（`0x5a`）只做一件事：候选 ≠ targetCard ⇒ `return 0`；
                    //   **身份相符 ⇒ 不 return，继续落到 `:203` 起的常规格**（阵营 / 卡种 / ⑧ /
                    //   keyword / trait / 四组 stat / 白名单 / 附魔 / army 全都还要判）。
                    //   ⇒ 改前把 30/90 合并成一句 `return ReferenceEquals(...)` ⇒ 这一档
                    //   **少判了整条 36 格**（真偏差，不是「多筛」）。
                    //   判据 = `d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c:193-201`。
                    if (targetCard == null) { gap = $"`ta={ta}` 要拿 targetCard 比身份，这次没有点选目标"; return null; }
                    if (!ReferenceEquals(candidate, targetCard)) return false;
                    break;                                // 身份相符 ⇒ 继续走下面那批常规筛
                case 140:                                 // previousAbilityTargets
                case 150:                                 // previousAbilityFiltered
                    if (ctx == null) { gap = "`ta=150` 需要 ctx"; return null; }
                    if (ctx.PreviousAbilityTargets == null) { gap = "`ta=150` 的静态表不可用"; return null; }
                    {
                        bool inPrev = false;
                        for (int i = 0; i < ctx.PreviousAbilityTargets.Count; i++)
                            if (ReferenceEquals(ctx.PreviousAbilityTargets[i], candidate)) { inPrev = true; break; }
                        if (!inPrev) return false;
                    }
                    break;                                // 在表里 ⇒ 继续走下面那批常规筛
                case 200:                                 // 我们的 ResolveTargets 已单独处理并提前返回
                case 201:
                    gap = $"`ta={ta}` referencedCards —— 这条路在 `ResolveTargets` 早已单独处理，不该走到这里";
                    return null;
                case 235:                                 // warlordsTalent
                    gap = "`ta=235 warlordsTalent` 本版未建模（要比督军天赋卡）";
                    return null;
                case 100:                                 // adjacentToSelfIfMeetsCriteria：末尾还要查相邻
                    gap = "`ta=100` 的「相邻里有满足者」本版未实现";
                    return null;

                // ==================================================================================
                //  🆕 2026-10-24（执行代理 `W25` · `D1` 的 R1）：**原版这一档【根本不筛】**的短路
                // ==================================================================================
                //  判据（第一权威 = `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`，**建池**那个函数）：
                //  下面这些 `ta` 档**建完池就 `goto LAB_180818485`（`:1378-1380` = `return 池`）**
                //  ⇒ **不经过** `FilterMethods__CheckIfMeetsCriteria`（真正的逐候选筛在 `:1086-1095`，
                //  只有一路落到函数尾部才经过它）。
                //
                //  🔴 **⛔ 别照 `CheckIfMeetsCriteria.c` 的表反推** —— 那是**另一个函数**（它假设候选池
                //     已经建好）。单看它会给 140/150 一个与建池侧不同的答案（`:171-183` vs
                //     `GetTargets.c:849-891`）。本类挂在**建池侧** ⇒ 判据一律取 `GetTargets.c`。
                //
                //  🔴 这一条是**今天这批红（`Righteous Fury` / `GOF74` …）的根因**：`ta=120` 的行
                //     其余格是 `includeHero=0`（那是它们的默认值、不是语义）⇒ 按常规档筛会把
                //     **督军本身**淘汰 ⇒ 治疗空过。实测日志「按原版 targetCriteria 36 格筛：1 → 0」。
                case 120:                                 // friendlyHero —— `:721-736`，池 = 本方督军本身
                case 130:                                 // enemyHero —— `:738-747`（同出口 `LAB_180819e32`）
                case 160:                                 // currentMana —— `:220-319`
                case 161:                                 // currentSpiritStone —— `:321-420`
                case 162:                                 // currentFaith —— `:422-521`
                case 170:                                 // maxMana —— `:624-719`
                case 190:                                 // lastPlayedIfMeetsCriteria —— `:173-182`
                case 220:                                 // targetCountTraits —— `:192-206`（经 `LAB_180818474`）
                case 230:                                 // highestHealthWarlord —— `:809-833`
                case 240:                                 // lowestHealth —— `:835-843`
                case 245:                                 // secretsPlayed —— `:523-622`
                    return true;
                case 180:                                 // choiceResolution —— `:89-182`
                    // `filterSpecificCards` **为空**（或这一格没落 ⇒ 同样是「空」）时，原版从
                    // `LAB_180818466` 出去 ⇒ **不筛**；**非空**时它会一路落到尾部的 criteria 循环
                    // （那时 ⑫ 白名单与其余常规格都真的在用）。
                    // ⚠️ `null`（这一格没落 = 不知道它空不空）⇒ **判不了**，⛔ 不冒充「空」。
                    if (set.FilterSpecificCardIds == null)
                    { gap = "`ta=180 choiceResolution` 的 `filterSpecificCards` 未知（没落）⇒ 判不了这一档筛不筛"; return null; }
                    if (set.FilterSpecificCardIds.Length == 0) return true;
                    break;                                // 非空 ⇒ 落下面那批常规筛（§⑫ 就是白名单本身）
            }

            // ==========================================================================================
            //  常规档 —— 次序 = `CheckIfMeetsCriteria.c` 的**语句次序**（`:203-408`）。
            //  ⚠️ 原版是一条 `&&` 链（**短路**）；本类用 `null` 表示「这一维判不了」，遇到就立刻返回
            //     （= 保留候选 + 出声）。⇒ 若**前面**某一维本来能判出 `false`、**后面**某一维判不了，
            //     本类会返回 `null` 而不是 `false`。**这是本类契约的保守方向**（宁可多留一个候选并
            //     出声，也不冒充「确定淘汰」），如实记在这里 —— 不是原版语义，是本类的降级约定。
            // ==========================================================================================

            // ① `excludeSimilarCards`(`+0x1f`)（`:203-215`）：候选卡名 == thisCard 卡名 ⇒ 淘汰
            if (FlagOn(set.ExcludeSimilarCards))
            {
                if (thisCard == null) { gap = "`excludeSimilarCards` 要拿 thisCard 比卡名，本版没有载体"; return null; }
                if (thisCard.Card != null && candidate.Card != null
                    && string.Equals(thisCard.Card.Name, candidate.Card.Name, StringComparison.Ordinal))
                    return false;
            }

            // ② 阵营闸 `friendlyUnits`(`+0x14`) / `enemyUnits`(`+0x15`)（`:216-228`，**最先**判的之一）：
            //    `候选.isPlayer == thisCard.isPlayer` ⇒ 要看 `friendlyUnits`；否则看 `enemyUnits`；
            //    对应那一格为「假」⇒ 淘汰（**两个都假 ⇒ 一个候选都不留**，⛔ 不是「不过滤」）。
            //    ⚠️ **拿 `owner`（这条 ability 的归属座位）顶 `thisCard.isPlayer`**（`W2` 引入）：
            //       原版 `:216` 里 `thisCard == null` 直接抛（`FUN_1803f47a0`，不返回），
            //       而我们**战术卡那条路天然没有载体** ⇒ 照抄会让所有战术卡的 36 格 criteria 全变「判不了」。
            //       本工程的口径是「`thisCard` ≡ 这条 ability 的归属方」⇒ `owner` 是行为等价的最小替身。
            //       （2026-10-24 `W19` 复核：裁决线与 `ResolveTargets` 传的 `source` 恒为 `owner` 那一侧。）
            int side = SideOf(ctx, candidate);
            if (side < 0) { gap = "候选不在任何一方棋盘上 ⇒ 阵营判不了"; return null; }
            {
                bool sameSide = side == owner;
                int gate = sameSide ? set.FriendlyUnits : set.EnemyUnits;
                if (gate == CardDef.CountSetSpec.Unknown)
                { gap = sameSide ? "`friendlyUnits` 未知（没落）" : "`enemyUnits` 未知（没落）"; return null; }
                if (gate == 0) return false;
            }

            // ③ includeSelf(`+0x1d`) / excludeNotSelf(`+0x1e`)（`:232-243`）
            //    `includeSelf == 0` 且候选就是 thisCard ⇒ 淘汰；`excludeNotSelf != 0` 且不是它 ⇒ 淘汰。
            //    ⚠️ 拿不到 `thisCard`（战术卡那条路）⇒ 这两格**判不了**：⛔ 不当作「放行」也不当作「淘汰」；
            //       但为了不让一整份 criteria 白扔，按 `CountEval.IncludeUnit` 的既有口径**跳过这两格**
            //       （池内 52 行带 `includeSelf=1`、0 行带 `excludeNotSelf`）。
            {
                if (set.IncludeSelf == CardDef.CountSetSpec.Unknown
                    || set.ExcludeNotSelf == CardDef.CountSetSpec.Unknown)
                { gap = "`includeSelf`/`excludeNotSelf` 未知（没落）"; return null; }
                if (thisCard != null && candidate.Card != null && thisCard.Card != null)
                {
                    bool self = ReferenceEquals(candidate, thisCard);
                    if (self && set.IncludeSelf == 0) return false;
                    if (!self && set.ExcludeNotSelf != 0) return false;
                }
            }

            // ④ `excludeFriendlyHero`(`+0x21`) / `excludeEnemyHero`(`+0x22`)（`:244-272`，逐句）：
            //    <code>
            //    if (excludeFriendlyHero != 0) {
            //        if (thisCard == null) throw;                       // `:245`
            //        if (IsWarlordEquivalent(**候选**)) {               // `:246` —— 判的是**候选**，不是载体
            //            if (候选.isPlayer == thisCard.isPlayer) { Log(...); return false; }   // `:250-253`
            //        }
            //    }
            //    if (excludeEnemyHero != 0) { … 候选.isPlayer != thisCard.isPlayer ⇒ Log + return false; }
            //    </code>
            //    `IsWarlordEquivalent` = `GameStaticData__IsWarlordEquivalent.c:4`：`cardData.cardType == 10`
            //    ⇒ **「候选是 Hero（督军）类卡」**（`CardTypeOptions.Hero = 10`）。
            //    ⚠️ 原版 `:253`/`:265` 还 `Log` 一行（`… is a Warlord …`）—— 那是玩家可见日志，
            //      本层没有日志句柄 ⇒ 不复制（行为 = 淘汰）。
            //    ⚠️ `thisCard.isPlayer` 同样用 `owner` 顶（理由见 ②）。
            //    ⚠️ **现读全池 24 条 criteria 行里这两格全是 0**（`W19` 2026-10-24 现扫 1,809 行）
            //       ⇒ 今天零行为变化；实现它们是「判据已闭合」的机械补齐。
            {
                if (set.ExcludeFriendlyHero == CardDef.CountSetSpec.Unknown
                    || set.ExcludeEnemyHero == CardDef.CountSetSpec.Unknown)
                { gap = "`excludeFriendlyHero`/`excludeEnemyHero` 未知（没落）"; return null; }
                bool wantFriendly = set.ExcludeFriendlyHero != 0, wantEnemy = set.ExcludeEnemyHero != 0;
                if ((wantFriendly || wantEnemy) && candidate.Card != null
                    && string.Equals(candidate.Card.Type, "hero", StringComparison.OrdinalIgnoreCase))
                {
                    bool sameSide = side == owner;
                    if (sameSide && wantFriendly) return false;
                    if (!sameSide && wantEnemy) return false;
                }
            }

            // ⑤ includeRemnants（`:273-278`）
            if (!FlagOn(set.IncludeRemnants) && candidate.IsRemnant) return false;

            // ⑥ 卡种白名单（`:280-293`）
            //    ⚠️ 只认 Hero/Minions/Spells/Whispers 四格 —— `includeWeapons`/`includeHeroParts`
            //       **原版不读**（见文件头那条），所以这里也**不看**（非 0 也不影响结果）。
            {
                bool? kind = CardTypePasses(candidate, set);
                if (kind == null) { gap = "卡种白名单：候选的卡种本版没建模（或对应那一格未知）⇒ 判不了"; return null; }
                if (!kind.Value) return false;
            }

            // ⑦ spellType（`:294-297`）
            if (Known(set.SpellType) && set.SpellType != 0)
            {
                int candSpell = candidate.Card != null ? candidate.Card.SpellType : -1;
                if (candSpell < 0) { gap = "候选卡的 `spellType` 未知 ⇒ 判不了"; return null; }
                if (candSpell != set.SpellType) return false;
            }
            else if (set.SpellType == CardDef.CountSetSpec.Unknown)
            {
                gap = "`spellType` 未知（没落）"; return null;
            }

            // ⑧ conditionsFilter / excludeCondition（`:298-303`，求值器 = `ConditionEval`）
            {
                int c = cond != null ? cond.Condition : 0;            // 缺行 = none(0) = 放行
                bool ex = cond != null && cond.Exclude;
                bool? v = ConditionEval.MeetsUnit(ctx, candidate, c, ex);
                if (v == null) { gap = $"条件 `cond={c}`（ex={ex}）在这个候选身上判不了"; return null; }
                if (!v.Value) return false;
            }

            // ⑨ keywordFilter + excludeRace（`:304-313`；语义 = 下面 `KeywordPasses` 的三档）
            {
                bool? v = KeywordPasses(candidate, set.KeywordFilter, set.ExcludeRace);
                if (v == null) { gap = $"`keywordFilter={set.KeywordFilter}` 本版认不出（或 `excludeRace` 未知）"; return null; }
                if (!v.Value) return false;
            }

            // ⑩ traitsFilter + excludeTrait（`:314-317`）
            {
                bool? v = TraitPasses(candidate, set.TraitsFilter, set.ExcludeTrait);
                if (v == null) { gap = $"`traitsFilter={set.TraitsFilter}` 本版认不出（或 `excludeTrait` 未知）"; return null; }
                if (!v.Value) return false;
            }

            // ⑪ 四组 `MeetsStatCriteria`（`:318-375`）—— 逐句见下面 `StatGroupsPass` / `MeetsStatCriteria`
            {
                string statGap;
                bool? v = StatGroupsPass(ctx, side, candidate, thisCard, set, out statGap);
                if (v == null) { gap = statGap; return null; }
                if (!v.Value) return false;
            }

            // ⑫ filterSpecificCards（`:465-483` 白名单；按卡 id 比 —— 生成器已把 PPtr 解成 id）
            if (set.FilterSpecificCardIds != null && set.FilterSpecificCardIds.Length > 0)
            {
                string id = candidate.Card != null ? candidate.Card.Id : null;
                bool hit = false;
                for (int i = 0; i < set.FilterSpecificCardIds.Length; i++)
                    if (id != null && string.Equals(id, set.FilterSpecificCardIds[i], StringComparison.Ordinal))
                    { hit = true; break; }
                if (!hit) return false;
            }

            // ⑬ excludeEnchantments（`:430-463`）
            if (set.ExcludeEnchantmentIds != null && set.ExcludeEnchantmentIds.Length > 0)
            {
                var ench = candidate.Instance != null ? candidate.Instance.ActiveEnchantments : null;
                if (ench == null) { gap = "候选没有实例 ⇒ 附魔表读不到"; return null; }
                for (int i = 0; i < ench.Count; i++)
                {
                    var ec = ench[i] != null ? ench[i].Card : null;
                    if (ec == null) continue;
                    for (int k = 0; k < set.ExcludeEnchantmentIds.Length; k++)
                        if (string.Equals(ec.Id, set.ExcludeEnchantmentIds[k], StringComparison.Ordinal))
                            return false;
                }
            }

            // ⑭ armyFilter + chosenArmy（`:396-406`）
            {
                bool? v = ArmyPasses(candidate, set.ChosenArmy, set.ArmyFilter);
                if (v == null) { gap = $"`armyFilter={set.ArmyFilter}` 本版判不了"; return null; }
                if (!v.Value) return false;
            }

            return true;   // 原版 `:407-408`：`ta != 100` ⇒ return 1
        }

        // ==================================================================
        //  分项判据（⛔ 每个只有这一份）
        // ==================================================================

        /// <summary>`flag` 是「开着」（1）。`-1`（未知/没落）与 `0` 都**不当开** ——
        /// 但调用方要区分它们时得自己看（本类对「未知」的处理逐处写在 doc 里）。</summary>
        static bool FlagOn(int flag) { return flag > 0; }

        /// <summary>`-1` = 未知/没落 ⇒ 不能拿来判；其余（含 0）都是真值。</summary>
        static bool Known(int v) { return v != CardDef.CountSetSpec.Unknown; }

        // ==================================================================
        //  四组 `MeetsStatCriteria`（攻 / 血 / 费 / 稀有度）
        // ==================================================================

        // 原版 `StatFilterOptions` 的逐值镜像
        // （数值表 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/StatFilterOptions.cs`）。
        // ⛔ 别改成我们自己那套名字 —— 诊断/日志里要能按原版枚举名对得上。
        /// <summary>`NotApplicable(0)` —— 原版 `if (param_4 != 0)` 挡住的那一档 = 本组不参与。</summary>
        const int StatNotApplicable = 0;
        /// <summary>`EqualTo(1)`</summary>
        const int StatEqualTo = 1;
        /// <summary>`EqualOrHigher(2)`</summary>
        const int StatEqualOrHigher = 2;
        /// <summary>`EqualOrLower(3)`</summary>
        const int StatEqualOrLower = 3;
        /// <summary>`EqualOrLowerToThisUnitAttack(4)`</summary>
        const int StatEqualOrLowerToThisUnitAttack = 4;
        /// <summary>`HigherToThisUnitAttack(5)`</summary>
        const int StatHigherToThisUnitAttack = 5;
        /// <summary>`EqualOrLowerIncludingBastion(6)`</summary>
        const int StatEqualOrLowerIncludingBastion = 6;
        /// <summary>`LowerToThisUnitAttack(7)`</summary>
        const int StatLowerToThisUnitAttack = 7;
        /// <summary>`RandomChance(8)`</summary>
        const int StatRandomChance = 8;
        /// <summary>`EqualToAlt(9)`</summary>
        const int StatEqualToAlt = 9;
        /// <summary>`EqualOrHigherAlt(10)`</summary>
        const int StatEqualOrHigherAlt = 10;
        /// <summary>`EqualOrLowerAlt(11)`</summary>
        const int StatEqualOrLowerAlt = 11;

        /// <summary>
        /// 逐句照 `FilterMethods__MeetsStatCriteria.c`（签名照签名桩
        /// `d:/2/Warpforge_code/Scripts/Assembly-CSharp/FilterMethods.cs`：
        /// `MeetsStatCriteria(int cardStat, int thisCardStat, int statFilter, StatFilterOptions statFilterOptions, int altValue = 0)`）。
        /// <code>
        /// if (option != 0) {
        ///   case EqualTo(1)                      : return cardStat == statFilter;
        ///   case EqualOrHigher(2)                : return statFilter &lt;= cardStat;
        ///   case EqualOrLower(3)                 : return cardStat &lt;= statFilter;
        ///   case EqualOrLowerToThisUnitAttack(4) : return cardStat &lt;= thisCardStat;
        ///   case HigherToThisUnitAttack(5)       : return thisCardStat &lt; cardStat;
        ///   case EqualOrLowerIncludingBastion(6) : cardStat += altValue;   // 原版**落到 case 3**
        ///   case EqualOrLower(3)  的那句        : return cardStat &lt;= statFilter;   // ← case 6 落在这里
        ///   case LowerToThisUnitAttack(7)        : return cardStat &lt; thisCardStat;
        ///   case EqualToAlt(9)                   : return altValue == statFilter;
        ///   case EqualOrHigherAlt(10)            : return statFilter &lt;= altValue;
        ///   case EqualOrLowerAlt(11)             : return altValue &lt;= statFilter;
        ///   default                              : bVar1 = true;            // ← case 8 / 越界值落这里
        /// }
        /// return true;
        /// </code>
        /// 🔴 **`RandomChance(8)` 不掷骰** —— 反编译的 switch 里**没有 `case 8`**，落到末尾
        ///    `return true`（`bVar1` 的初值）。⛔ 别按枚举名自己加一次 RNG（那会引入不可复现的对局）。
        /// `null` = `option == -1`（我们数据里**没落**）⇒ **判不了**：⛔ 不拿「原版对越界值返回 true」顶，
        /// 那会把「数据缺失」冒充成「这一组不筛」（红线：不许静默失败）。
        /// </summary>
        static bool? MeetsStatCriteria(int cardStat, int thisCardStat, int statFilter, int option, int altValue)
        {
            if (option == CardDef.CountSetSpec.Unknown) return null;
            switch (option)
            {
                case StatNotApplicable: return true;                       // 原版整段被 `if (param_4 != 0)` 跳过
                case StatEqualTo: return cardStat == statFilter;
                case StatEqualOrHigher: return statFilter <= cardStat;
                case StatEqualOrLower: return cardStat <= statFilter;
                case StatEqualOrLowerToThisUnitAttack: return cardStat <= thisCardStat;
                case StatHigherToThisUnitAttack: return thisCardStat < cardStat;
                // 原版 case 6 先 `param_1 = param_1 + param_5;` 再**掉进 case 3** ⇒ 合并成一句
                case StatEqualOrLowerIncludingBastion: return cardStat + altValue <= statFilter;
                case StatLowerToThisUnitAttack: return cardStat < thisCardStat;
                case StatRandomChance: return true;                        // 反编译无此支（见 doc）
                case StatEqualToAlt: return altValue == statFilter;
                case StatEqualOrHigherAlt: return statFilter <= altValue;
                case StatEqualOrLowerAlt: return altValue <= statFilter;
                default: return true;                                      // 越界枚举值 ⇒ 原版 return true
            }
        }

        /// <summary>一组 stat 的两个字段都得「有值」才可判：`option` 是 `-1`（没落）或 `filter` 是 `-1` ⇒ 判不了。
        /// `option == 0 (NotApplicable)` 由调用方**先**跳过（原版 `if (param_4 != 0)`）。</summary>
        static bool StatArgsReady(int option, int filter, string name, out string gap)
        {
            gap = null;
            if (option == CardDef.CountSetSpec.Unknown) { gap = $"`{name}Option` 未知（没落）"; return false; }
            if (filter == CardDef.CountSetSpec.Unknown) { gap = $"`{name}` 未知（没落）"; return false; }
            return true;
        }

        /// <summary>第 2 个实参（`thisCardStat`）。**原版四组传的都是载体的 `CurrentMeleeAttack`**
        /// （判据见 <see cref="StatGroupsPass"/> 那张表）—— 只有 `option ∈ {4,5,7}` 读它。
        /// `false` = 这一档要拿载体比、但本版没有载体 ⇒ 判不了。</summary>
        static bool TryStatThisCardStat(int option, UnitState thisCard, out int value)
        {
            bool needs = option == StatEqualOrLowerToThisUnitAttack
                      || option == StatHigherToThisUnitAttack
                      || option == StatLowerToThisUnitAttack;
            if (!needs) { value = 0; return true; }        // 其余档不读第 2 个实参 ⇒ 载体缺失也能判
            if (thisCard == null) { value = 0; return false; }
            value = thisCard.Attack;
            return true;
        }

        /// <summary>
        /// 四组 `MeetsStatCriteria`（`FilterMethods__CheckIfMeetsCriteria.c:318-375`，**逐句**）。
        /// `true` = 四组都过 · `false` = 有一组淘汰（立刻返回）· `null` = 有一组判不了（`gap` 写明是哪一组）。
        ///
        /// <para>**四组的实参对照**（`CheckIfMeetsCriteria.c:321-374` 逐行现读；
        /// 第一权威，⛔ 别按直觉「顺手改正」）：</para>
        /// <code>
        /// | 组     | cardStat（候选当前值）        | thisCardStat        | statFilter | altValue（第 5 实参）          |
        /// | 攻     | `CurrentMeleeAttack`          | 载体`CurrentMeleeAttack` | +0x48  | 候选 `CurrentRangeAttack`      |
        /// | 血     | `CurrentHealth`（ObscuredInt +0x68） | 载体`CurrentMeleeAttack` | +0x50 | 候选 `GetCurrentTraitValueWithModifiers(0x352 /*bastion*/)` |
        /// | 费     | `CurrentCost`                 | 载体`CurrentMeleeAttack` | +0x58  | 0                              |
        /// | 稀有度 | `cardData.rarity`(+0x28)      | 载体`CurrentMeleeAttack` | +0x60  | 0                              |
        /// </code>
        /// <para>🔴 **血/费/稀有度三组的 `thisCardStat` 在原版里也传载体的近战攻击** ——
        /// `:326` 算一次 `get_CurrentRangeAttack(候选)`、`:345` 算一次 `get_CurrentMeleeAttack(载体)`，
        /// 之后 `:352`/`:361`/`:371` 三处**复用同一个寄存器**。看着像原版的复制粘贴，但它是第一权威里
        /// 的**字面事实** ⇒ 照抄。影响面：只有 `option ∈ {4,5,7}`（与载体比大小）读第 2 个实参，
        /// 而现读全池那三组只用了 `1/2/3`（`W19` 2026-10-24 现扫 24 行）⇒ **今天零行为差异**；
        /// 真出现 4/5/7 时按原版比较。</para>
        ///
        /// <para>**读数取的是「当前值」不是「基础值」**（三处都查实了）：
        /// `EntityScript__get_CurrentMeleeAttack.c` / `get_CurrentRangeAttack.c` 都是
        /// 基础值 + 卡上 effect 修正、夹 0；`EntityScript__get_currentHealth.c:5-10` 读的正是
        /// `+0x68` 那五个 int（`ObscuredInt` 的密文）；`EntityScript__get_CurrentCost.c` 是
        /// 基础费 `cardData+0x78` + effect 修正、夹 0。我们这一侧的对应物：
        /// `UnitState.Attack` / `RangedAttack` / `Health` 都是**当前值**（含 buff/光环/临时增益），
        /// 费用走 <see cref="RuleCore.CostOf"/>（全仓**唯一**费用判据，含持续改费；⛔ 别直接读 `Card.Cost`），
        /// 堡垒读 <see cref="UnitState.Bastion"/>（= `KwValue(bastion)`，与 `CurrentBastion` 同源）。</para>
        /// </summary>
        static bool? StatGroupsPass(BattleContext ctx, int side, UnitState candidate, UnitState thisCard,
                                    CardDef.CountSetSpec set, out string gap)
        {
            gap = null;

            // ---- ① 攻（`:321-332`）----
            if (set.AttackFilterOption != StatNotApplicable)
            {
                string g;
                if (!StatArgsReady(set.AttackFilterOption, set.AttackFilter, "attackFilter", out g))
                { gap = g; return null; }
                int thisStat;
                if (!TryStatThisCardStat(set.AttackFilterOption, thisCard, out thisStat))
                { gap = $"`attackFilterOption={set.AttackFilterOption}` 要与载体比攻击，本版没有载体"; return null; }
                bool? r = MeetsStatCriteria(candidate.Attack, thisStat, set.AttackFilter,
                                            set.AttackFilterOption, candidate.RangedAttack);
                if (r == null) { gap = $"`attackFilterOption={set.AttackFilterOption}` 认不出 ⇒ 判不了"; return null; }
                if (!r.Value) return false;
            }

            // ---- ② 血（`:333-355`）----
            if (set.CurrentHealthFilterOption != StatNotApplicable)
            {
                string g;
                if (!StatArgsReady(set.CurrentHealthFilterOption, set.CurrentHealthFilter, "currentHealthFilter", out g))
                { gap = g; return null; }
                int thisStat;
                if (!TryStatThisCardStat(set.CurrentHealthFilterOption, thisCard, out thisStat))
                { gap = $"`currentHealthFilterOption={set.CurrentHealthFilterOption}` 要与载体比，本版没有载体"; return null; }
                bool? r = MeetsStatCriteria(candidate.Health, thisStat, set.CurrentHealthFilter,
                                            set.CurrentHealthFilterOption, candidate.Bastion);
                if (r == null) { gap = $"`currentHealthFilterOption={set.CurrentHealthFilterOption}` 认不出 ⇒ 判不了"; return null; }
                if (!r.Value) return false;
            }

            // ---- ③ 费（`:356-366`）----
            if (set.ManaCostFilterOption != StatNotApplicable)
            {
                string g;
                if (!StatArgsReady(set.ManaCostFilterOption, set.ManaCostFilter, "manaCostFilter", out g))
                { gap = g; return null; }
                if (candidate.Card == null) { gap = "候选没有卡定义 ⇒ 费用读不到"; return null; }
                int thisStat;
                if (!TryStatThisCardStat(set.ManaCostFilterOption, thisCard, out thisStat))
                { gap = $"`manaCostFilterOption={set.ManaCostFilterOption}` 要与载体比，本版没有载体"; return null; }
                // 原版 `get_CurrentCost` = 基础费 + 卡上持续改费、夹 0 ⇒ 我们走唯一费用判据 `RuleCore.CostOf`。
                // ⚠️ 传的是**候选自己那一方**（`side`，不是 `owner`）：`CostOf` 里的
                //    `CostIfControls` / `CostPerEnemyHandCard` / `CostMod.Player` 都是按
                //    「这张牌的控制者」算的 —— 敌方候选的费要按敌方那一侧算，拿 `owner` 会读错棋盘。
                int cost = (ctx != null && candidate.Instance != null)
                         ? RuleCore.CostOf(ctx, side, candidate.Instance)
                         : candidate.Card.Cost;
                bool? r = MeetsStatCriteria(cost, thisStat, set.ManaCostFilter,
                                            set.ManaCostFilterOption, 0);
                if (r == null) { gap = $"`manaCostFilterOption={set.ManaCostFilterOption}` 认不出 ⇒ 判不了"; return null; }
                if (!r.Value) return false;
            }

            // ---- ④ 稀有度（`:367-377`）----
            if (set.RarityFilterOption != StatNotApplicable)
            {
                string g;
                if (!StatArgsReady(set.RarityFilterOption, set.RarityFilter, "rarityFilter", out g))
                { gap = g; return null; }
                int? rar = RarityOf(candidate.Card);
                if (rar == null)
                { gap = "候选的 `rarity` 认不出（不在 `CardRarity` 五档里）⇒ 判不了"; return null; }
                int thisStat;
                if (!TryStatThisCardStat(set.RarityFilterOption, thisCard, out thisStat))
                { gap = $"`rarityFilterOption={set.RarityFilterOption}` 要与载体比，本版没有载体"; return null; }
                bool? r = MeetsStatCriteria(rar.Value, thisStat, set.RarityFilter,
                                            set.RarityFilterOption, 0);
                if (r == null) { gap = $"`rarityFilterOption={set.RarityFilterOption}` 认不出 ⇒ 判不了"; return null; }
                if (!r.Value) return false;
            }

            return true;
        }

        /// <summary>
        /// 我们 `CardDef.Rarity`（**小写英文串**）→ 原版 `CardRarity` 的数值。
        /// 判据 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardRarity.cs`
        /// （`None = 0 / Common = 1 / Rare = 2 / Epic = 3 / Legendary = 4 / Special = 5`）
        /// ＋ 生成器 `工具/gen_cards_engine.py` 顶部那条映射
        /// （`TRUTH_RARITY = {1: common, 2: rare, 3: epic, 4: legendary, 5: special}`，
        ///  旁证 = 宝石色 hue 186/122/279/42/15 的官方卡框图 `{1..5}_40k_cardframe_rarity_*.png`）。
        /// 认不出 ⇒ `null`（判不了；⛔ 不猜成 `None = 0` —— 0 会让 `EqualOrLower` 全过）。
        /// </summary>
        static int? RarityOf(CardDef c)
        {
            if (c == null) return null;
            switch ((c.Rarity ?? "").Trim().ToLowerInvariant())
            {
                case "common": return 1;
                case "rare": return 2;
                case "epic": return 3;
                case "legendary": return 4;
                case "special": return 5;
                default: return null;
            }
        }

        /// <summary>候选在哪一方棋盘上（`-1` = 不在）。
        /// 判据：原版读候选 `isPlayer(+0x40)`；我们模型里只有棋盘有「归属」，所以回棋盘查。</summary>
        static int SideOf(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null) return -1;
            for (int p = 0; p < ctx.Players.Length; p++)
            {
                var b = ctx.Players[p].Board;
                for (int s = 0; s < b.Length; s++)
                    if (ReferenceEquals(b[s], u)) return p;
            }
            return -1;
        }

        /// <summary>卡种白名单（`:280-293`）。`null` = 这一档本版判不了。
        /// ⚠️ 原版「那一格为假 ⇒ 淘汰」；我们模型里 `-1`（没落）**不当假**（如实：判不了）。</summary>
        static bool? CardTypePasses(UnitState u, CardDef.CountSetSpec set)
        {
            string t = u.Card != null ? u.Card.Type : null;
            int need;
            if (t == "hero") need = set.IncludeHero;
            else if (t == "unit") need = set.IncludeMinions;
            else if (t == "tactic" || t == "defence") need = set.IncludeSpells;
            else return null;                                  // 没建模的卡种
            if (need == CardDef.CountSetSpec.Unknown) return null;
            return need != 0;
        }

        /// <summary>`keywordFilter`(`+0x2c`) + `excludeRace`(`+0x30`)，逐句照 `:304-313`：
        /// <code>
        /// if (keywordFilter == 0)                     ⇒ 不筛（true）
        /// if (keywordFilter != 0 &amp;&amp; excludeRace == 0) ⇒ 候选**没有**这个词 ⇒ false（有 ⇒ true）
        /// if (keywordFilter != 0 &amp;&amp; excludeRace != 0) ⇒ 候选**有**这个词 ⇒ false（没有 ⇒ true）
        /// </code>
        /// `keywordFilter` 是原版 `Keyword` 枚举（5 Infantry / 10 Vehicle / …）。
        /// ⚠️ 判「这张卡算不算某一类」走 `CreatePool.MatchesKind` —— **全仓唯一判据**（⛔ 别另写）。
        /// </summary>
        static bool? KeywordPasses(UnitState u, int keywordFilter, int excludeRace)
        {
            if (keywordFilter == CardDef.CountSetSpec.Unknown) return null;
            if (keywordFilter == 0) return true;
            if (excludeRace == CardDef.CountSetSpec.Unknown) return null;
            string word = KindWordOfKeyword(keywordFilter);
            if (word == null) return null;
            if (u.Card == null) return null;
            bool has = CreatePool.MatchesKind(u.Card, word);
            return excludeRace != 0 ? !has : has;
        }

        /// <summary>原版 `Keyword` 枚举 → 我们 `CreatePool.KindWords` 的词
        /// （数值表 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Keyword.cs`）。
        /// 认不出 ⇒ `null`（判不了，⛔ 不猜）。</summary>
        static string KindWordOfKeyword(int keyword)
        {
            switch (keyword)
            {
                case 5: return "infantry";
                case 10: return "vehicle";
                case 15: return "monster";
                case 20: return "structure";     // `Building` 在我们数据里是 subtype `Structure`
                case 25: return "battlesuit";
                case 30: return "drone";
                case 35: return "beast";
                case 40: return "daemon";
                default: return null;
            }
        }

        /// <summary>`traitsFilter`(`+0x34`) + `excludeTrait`(`+0x3c`)（`:314-317` → `MeetsTraitCriteria`）。
        /// 判法：候选单位**当前**身上有没有这个关键词（`UnitState.Has` —— 含运行时加的，
        /// 与 `CardCriteria.Matches(UnitState)` 同一口径）。`name` 走 `KeywordTable.Normalize`
        /// （唯一的关键词归一化口）；认不出 ⇒ `null`。
        /// ⚠️ `excludeTrait` 的取反方向**没有**单独的反编译方法体可读（`MeetsTraitCriteria` 的产物缺失）
        /// ⇒ 按与 `excludeRace` 同构的「排除=有就淘汰」处理，**并在这里如实标注**。
        /// </summary>
        static bool? TraitPasses(UnitState u, int traitsFilter, int excludeTrait)
        {
            if (traitsFilter == CardDef.CountSetSpec.Unknown) return null;
            if (traitsFilter == 0) return true;
            if (excludeTrait == CardDef.CountSetSpec.Unknown) return null;
            string name = TraitNameOf(traitsFilter);
            if (name == null) return null;
            string key = KeywordTable.Normalize(name);
            if (string.IsNullOrEmpty(key)) return null;      // 词表里没有 ⇒ 判不了（⛔ 不当「没有」）
            bool has = u.Has(key);
            return excludeTrait != 0 ? !has : has;
        }

        /// <summary>原版 `DefinedTrait` 枚举 → 名字（只收**池内数据真的用到**的那 15 个；
        /// 表 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`）。
        /// 认不出 ⇒ `null`（判不了）。</summary>
        static string TraitNameOf(int trait)
        {
            switch (trait)
            {
                case 5: return "ephemeral";
                case 70: return "stealth";
                case 80: return "shield";
                case 100: return "stun";
                case 310: return "invulnerable";
                case 390: return "flying";
                case 610: return "pack";
                case 650: return "swarm";
                // 🔴 **2026-10-24（`W19`）就地订正（铁律 5）**：本行原来写
                //    「不在 `KeywordTable.Prefixes` 里 ⇒ `Normalize` 返回 null ⇒ 判不了」—— **已过期**。
                //    现读 `Core/CardDef.cs` 的 `Prefixes` 表里有 `{ "frenzied", Destroyer }` 这一条
                //    （2026-10-15 接线批加的那批 `rage`/`frenzied`/`unstoppable`），
                //    `Destroyer = "destroyer"` ⇒ `Normalize("frenzied") == "destroyer"` ⇒ **判据成立**。
                //    语义判据：`DefinedTrait.frenzied = 680`（`DefinedTrait.cs`）而**卡面字是 `Destroyer`**
                //    （官方本地化 `Card_Trait/frenzied` = `Destroyer`/毁灭者；`frenzied_old` 才是
                //    `Frenzied` —— 改名史的实据，见 `CardDef.cs` 那一大段注释）。
                //    ⚠️ 全池现读 `cardTraits` 里**没有** `Frenzied` 字面量（那 10 张写的是 `Destroyer`）
                //    ⇒ 走 `u.Has("destroyer")` 才对得上；⛔ 别为它新建 `Frenzied` 常量（同一 trait 两份表示）。
                case 680: return "frenzied";
                case 910: return "tide";
                case 975: return "blind";
                case 1170: return "markerlight";
                case 1200: return "ambush";
                case 1240: return "stomp";
                // 🔴 2026-10-25（引擎线 `F1`）**就地订正（铁律 5）**：原来是 `return "huntMark";`
                //   —— 那串**过不了 `KeywordTable.Normalize`**：`Prefixes` 表里只登记了
                //   `new[] { "hunt mark", "huntmark" }`（**表头 = 带空格那一条**，第二半只是规范键），
                //   camelCase 的 `huntMark` 小写后是 `huntmark` ⇒ 前缀匹配不到 ⇒ `Normalize` 返回 `null`
                //   ⇒ `TraitPasses` 返回 **`null`（判不了）** ⇒ 消费端（`ApplyTargetCriteriaFilter` 的
                //   null 支）**整条 criteria 静默不筛**。
                //   影响面（卡池现读）：`traitsFilter = 1260` 共 **17 行 / 10 张卡**
                //   （`abilityLevelCriteria` 8 行 · `abilityTargetCriteria` 7 行 · `activeAbilityTargets` 2 行，
                //   全是 Space Wolves 那批猎杀标记卡）—— 它们此前**一格都没生效**。
                //   ⇒ 用卡面上的写法 `hunt mark`（`KeywordTable.Parse("Hunt Mark")` 现核 = `huntmark`）。
                //   ⛔ 别改成 `huntmark`：那不是前缀表里的**表头**（它只是别名那一半，`Normalize` 只比表头）。
                case 1260: return "hunt mark";
                default: return null;
            }
        }

        /// <summary>`armyFilter`(`+0x28`) + `chosenArmy`(`+0x24`) —— 逐值照 `FilterMethods__MeetsArmyCriteria.c`：
        /// 0 AnyArmy ⇒ true；1 OnlyChosenArmy ⇒ 相等；2 ExcludeChosenArmy ⇒ 不等；
        /// 3 OnlyChosenArmyOrNeutral ⇒ 相等 **或** Neutral；其余值 ⇒ 原版 `LogError` 后 **true**（照抄）。
        /// `null` = 本版判不了（枚举未知 / 阵营名认不出）。
        /// </summary>
        static bool? ArmyPasses(UnitState u, int chosenArmy, int armyFilter)
        {
            if (armyFilter == CardDef.CountSetSpec.Unknown) return null;
            if (armyFilter == 0) return true;
            if (armyFilter < 1 || armyFilter > 3) return true;   // 原版：LogError 后 return true
            if (chosenArmy == CardDef.CountSetSpec.Unknown) return null;
            string chosen = FactionOfArmy(chosenArmy);
            if (chosen == null) return null;
            string cand = u.Card != null ? u.Card.Faction : null;
            if (string.IsNullOrEmpty(cand)) return null;
            bool same = string.Equals(cand, chosen, StringComparison.OrdinalIgnoreCase);
            switch (armyFilter)
            {
                case 1: return same;
                case 2: return !same;
                case 3: return same || string.Equals(cand, "Neutral", StringComparison.OrdinalIgnoreCase);
                default: return true;
            }
        }

        /// <summary>原版 `CardArmy` 枚举 → 我们的 `CardDef.Faction` 字符串
        /// （数值表 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs`；
        ///  我们的 `faction` 列逐字用这些名字，2026-10-24 现读 13 种全对上）。
        /// 认不出 ⇒ `null`。</summary>
        static string FactionOfArmy(int army)
        {
            switch (army)
            {
                case 0: return "Neutral";
                case 10: return "Ultramarines";
                case 20: return "Goff";
                case 30: return "SaimHann";
                case 40: return "Sautekh";
                case 50: return "BlackLegion";
                case 60: return "Leviathan";
                case 70: return "TauEmpire";
                case 80: return "Sororitas";
                case 90: return "Genestealers";
                case 100: return "AstraMilitarum";
                case 110: return "DarkAngels";
                case 120: return "EmperorsChildren";
                case 130: return "SpaceWolves";
                default: return null;
            }
        }
    }
}
