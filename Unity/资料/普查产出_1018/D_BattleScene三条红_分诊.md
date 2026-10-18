# D · `BattleScene.Run` 三条红 —— 只读分诊

> 只读诊断报告（2026-10-18，第五十二会话批次之后）。⛔ 本报告没有改任何代码/数据/断言。
> 日志：`d:/4/_tmp_view/battle_run.log`（`BT === 合计：3270 通过 / 3 失败 ===`，日志尾 `:310012`）。
> 三条红的日志行：`battle_run.log:7837`（重名靠阵营钉）· `:8028`（Terminator 兜底）· `:157367`（卡面 ④ 残留记号）。
> 本批改动的文件里**没有** `Editor/BattleScene.cs`（`git status --porcelain -- …/BattleScene.cs` 为空）、
> 也没有 `Core/VfxMap.cs` / `Core/CardIcons.cs` / `Core/AbilityAnimDurations.cs` / `Core/EventTiming.cs`
> 与三份数据表（`card_vfx_by_card.json` · `ability_anim_durations.json` · `cards_engine.json`）——
> 它们相对 `HEAD` **全部未修改**（逐条 `git status` 现查为空）。

## 〇、总览

| # | 断言（`Editor/BattleScene.cs`） | 期望 → 实得 | 分类 | 是不是本批 |
|---|---|---|---|---|
| 1 | `:754-762` 重名靠阵营钉 | `Maulerfiend`/黑军团 → `TraitorsHate_Board` ⇒ 实得 `Buff_Blue_SW` | **(α) 断言过期**（夹具的事件归口变了） | ❌ 不是（第四十九会话 `cd7d3e8` · `T21`） |
| 2 | `:841-848` 表里没有的卡退回兜底 | `Terminator` 不在表里、`FallbackHits+1` ⇒ 实得**表里有**（2.00s）、计数 0→0 | **(α) 断言过期**（夹具卡现在有真值） | ❌ 不是（第五十会话 `3251df0` · `T14` 换源后重出表） |
| 3 | `:14813-14834` 卡面 ④ 残留记号 | 全池 `descZh` 过 `CardIcons.Rewrite` 后残留 0 ⇒ 实得 1（`GOF27`） | **(δ) 数据前提不成立**（官方中文正文里有 4 个**中文** `[[…]]` 记号，原版那一支只认枚举名）＋断言欠一条「已知数据异常」的出口 | ❌ 不是（第五十会话 `3251df0` · `K07` 并入官方中文表） |

**结论先说**：**三条没有一条是实现缺陷、也没有一条是本批回归**。
① 的「实得」是**正确的**（`TraitorsHate_Board` 现在挂在 `trigger` 事件上，而引擎在 Strike 那一刻发的正是 `EvtKind.Trigger`）；
② 的「实得」也是**正确的**（`UM103 Terminator` 现在确实在真值表里，2.00s）；
③ 的「实得」是**原版语义下的正确行为**（原版 `ModifyLocalization` 的 ③ 支只遍历 `Enum.GetValues`，中文词不在其中 ⇒ 原样留着），
但它背后压着一条**真数据缺陷**：官方中文表 `Card_Description/GOF27` 的**中文值描述的是另一版卡**（见 §3）。
**三条都是数据在第四十九/五十会话换过、断言没跟，而且 K06/K07 两份报告自己都明写「⛔ 没跑任何 `*.Run` 自检」**
（`资料/普查产出_1018/K06_池id迁移_报告.md:104` · `K07_本地化并入_报告.md:4`）⇒ 直到本批跑 `BattleScene.Run` 才露出来。

---

## 1) `重名靠阵营钉`：`Maulerfiend`/黑军团 → `Buff_Blue_SW`（期望 `TraitorsHate_Board`）

- **分类**：**(α) 断言过期**。⛔ 不是 (β)：`ResolveCardIdOf` 的阵营钉**是对的**（帝皇之子那一半就钉对了），
  实得值也不是「钉不出唯一」的产物 —— 它是**逐卡这一档没命中、掉到 `ByEvent["ability"]` 的通用兜底**
  （`Core/VfxMap.cs:151` 的 `{ Ability, "Buff_Blue_SW" }`）。断言文案里那句「钉不出唯一时出声并放弃」在这里是**误导**。
- **是不是本批**：**不是**。是本仓 **第四十九会话 `cd7d3e8`（2026-10-18 01:08，`T21`）** 改的：
  `工具/gen_card_vfx_by_card.py:168-180` 新增 `TRIGGER_EVENT`（`"Strike": EV_TRIGGER`）+
  `:862-893` 的 `so_refs()` 按 `CardAbility.trigger` 归口，随后重出了 `Resources/card_vfx_by_card.json`。
  断言本体自 `4136aab`（2026-10-15）起**一字未动**（`git blame -L 754,762` 现查这 9 行全是 `4136aab`）。
- **根因**：
  - 原版卡 SO 的这条动画挂在 **`cardAbilities[0]`，`trigger = 580 = AbilityTrigger.Strike`**
    （`数据/游戏数据/card_vfx_by_card.json` → `byCard.BL31`：`abilityTriggerName: "Strike"`, `abilityEvent: "trigger"`,
    `prefab: "TraitorsHate_Board"`, `conf: authoritative`, `src: cardSO.cardAbilities[0].abilityLogic[0].cardAnimations[0]`）。
  - T21 之前**一律归 `ability`**（2026-10-14 那份旧表 `Unity/_tmp_view/w1bak/card_vfx_by_card.rt.bak.json` 里就是 `BL31 ability → TraitorsHate_Board`）；
    T21 起按原版枚举归到 `trigger` ⇒ **`Resolve(Ability, …)` 这一问法从此永远查不到它**。
  - 这一改**不是退步、反而修了一条「永远不播」**：`EvtKind.Ability` 只在**主动技能那一族**发
    （`RuleCore.cs:7572` `UseAbility` · `:7730` `UseOathAbility` · `:7966` 替代行动），
    而 `Strike` 是「攻击后触发」。引擎在 Strike 那一刻发的是 `EvtKind.Trigger`：
    `RuleEngine/Core/RuleCore.cs:4579`（`FireTriggerAt(ctx, attacker, KeywordTable.Strike, …)`）→
    `:8284`（`ctx.Emit(EvtKind.Trigger, owner, slot, u.Name, keyword: …)`，**带的是卡名**）→
    表现层 `CardPresentation/Battle/BattleDriver.cs:12625`（`case EvtKind.Trigger: evt = VfxMap.Trigger`）⇒
    `VfxMap.Resolve("trigger", faction, "Maulerfiend")` 会按卡名+阵营钉到 `BL31` 并取到 `TraitorsHate_Board`。
    `BL31` 卡上确有 `Strike` 正文（`cards_engine.json`：`keywords: ["Armour 1","Strike"]`，
    `desc: Deal 1 damage to all enemies for each Dark Pact on your troops`）：
    `Strike` 在 `CardDef.BodyKeywords` 里（`RuleEngine/Core/CardDef.cs:661`），而它是这张卡**唯一**的带正文关键词
    ⇒ `CollectTriggerOps` 的 ⑤ 支 `CollectBareKeywordBody`（`:761-790`）会把裸写的那条 `desc` 认成 `strike` 的正文
    ⇒ `u.FxOps("strike") != null`，`FireTriggerAt` 不会在 `:8268` 早退。（⚠️ 本条是**静态判据**，没跑 Unity 验。）
- **判据**：
  - 原版：`CardScript__ResolveUnitAttacked.c:99-101` 的 `OnTrigger(0x244=580 Strike)`（施工单 D 已引，见 `RuleCore.cs:4542-4546`）；
    枚举名/数值 = `AbilityTrigger`；生成器归口表 `工具/gen_card_vfx_by_card.py:172-180` 与它的 doc `:168-171`
    （「判据不足的一律保持 `ability`（当前不可达，宁可少播、不可错播）」）。
  - 数据：`数据/游戏数据/card_vfx_by_card.json` 的 `byCard.BL31.trigger[0]`（authoritative）。
  - 自检的语义：`VfxMap.Resolve` 的 doc `Core/VfxMap.cs:496-497`（重名一律用 `faction` 钉）；
    `ResolveCardIdOf` `:571-593`。**这两处实现都没问题**。
- **建议改法（只写建议）**：
  - ⛔ **不要**把生成器改回 `ability`（那是把「Strike 动画永远不播」改回来）。
  - 改断言：`BattleScene.cs:754-759`。推荐**换一组同事件、两阵营都 authoritative 的夹具**：
    `Terminator`（`EC25` / `UM103`，`_byName` 顺序 EC25 在前）——
    本次 python 按 `VfxMap.EnsureLoaded` 的同一套算法（`confPriority` 取最优 + 按名/按阵营钉）模拟：
    `Resolve(Ability,"EmperorsChildren","Terminator") = "Buff EC Pink Twirl"` ·
    `Resolve(Ability,"Ultramarines","Terminator") = "Oath_Effect"`（两条都是 `conf: authoritative`）。
    ⚠️ 若保留 `Maulerfiend`，只能写成「BL 用 `trigger` / EC 用 `ability`」，而且必须改用
    **`ResolvePerCard`**（`VfxMap.cs:536`，不回落到按阵营/按事件）—— 否则「没命中」会被阵营兜底掩盖，
    这次就是被掩盖成了 `Buff_Blue_SW`。
  - 🔴 **顺带修一条空转的灭自证**：`:760-762` 的 `mfBL != mfEC` **今天是绿的但毫无意义**
    （BL 掉到了通用兜底 `Buff_Blue_SW`，当然不等于 EC 的逐卡值）。换夹具后要保证两条**都命中逐卡档**，
    这条判别式才重新有约束力。
  - 📌 另一条只管「别信」的旁证（不改也行）：`EC30` 的 `Buff_HideousMutation_EC` 现在是
    `cardAbilities[0] / trigger = Ecstasy(715)`，`Ecstasy` 不在 `TRIGGER_EVENT` 里 ⇒ 仍归 `ability`，
    而 `EvtKind.Ability` 只在主动技能那一族发 ⇒ **它在游戏里同样不可达**（生成器 doc 自己承认这一类「当前不可达」）。
    断言钉一个游戏里取不到的值，将来还会误导下一个人；用 `Terminator` 那组（UM103 是 `activeAbility`，可达）更硬。

---

## 2) `表里没有的卡退回兜底值`：`Terminator` 判据为空（期望计数 +1）→ 实得 2.00s、计数 0→0

- **分类**：**(α) 断言过期**（夹具卡的真值前提变了）。实现 `AbilityAnimDurations.TryGet` 的兜底/出声/计数都对
  （`Core/AbilityAnimDurations.cs:144-165`）。
- **是不是本批**：**不是**。`ability_anim_durations.json` 与 `AbilityAnimDurations.cs` 都不在本批 diff：
  - 该表在 `cd7d3e8`（第四十九会话）还是 **62 张卡、无 `UM103`**；
  - 在 `3251df0`（第五十会话 `T14`）变成 **93 张卡、含 `UM103`（`s: 2.0`）**（`git show <commit>:…ability_anim_durations.json` 现查）。
  - `BattleScene.cs:842` 的注释虽然**在 `3251df0` 被改过**（`UM_Terminator` → `UM103`），但改的是 id 写法，
    **没跟数据**（`git show 3251df0 -- …/BattleScene.cs` 只有这一行的 `-`/`+`）。
- **根因**：
  - 该表由 `工具/gen_ability_anim_durations.py:79-131` **从 `数据/游戏数据/card_vfx_by_card.json` 的
    `byCard[*].ability` authoritative 条目算出来**（不是直接读 2022 快照）。
  - `T14`（第五十会话 `3251df0`）把 `card_vfx_by_card.json` 的真值源换成了**当前 build 的 `allcards`（1,351 张）**
    （`git log -S allcards_1350 -- 工具/gen_card_vfx_by_card.py` 现查唯一命中 = `3251df0`；
    现读代码 `工具/gen_card_vfx_by_card.py:140` `CARD_SO_TRUTH = …/allcards_1350/cards_raw.json`，doc `:81`），
    于是 `UM103 Terminator` 的 `activeAbility`（`Oath_Effect` + `GainArmourEffect`）第一次进了这份真值；
    同一次提交里时长表被重出（62 → 93 张），它就有了 **2.0s**。
  - ⚠️ 现读 `数据/游戏数据/ability_anim_durations.json` 的 `cards` 里 `UM103.active = 2.0`；
    运行时的 `Resources/ability_anim_durations.json` `items` 里同样有 `{id:"UM103", name:"Terminator", s:2.0}`。
    ⇒ `TryGet("Terminator")` 走 `_byName` 命中、`FallbackHits` 不增；`noTruth` 返回 2.0，
    与 `EventTiming.AbilityFallbackSeconds = 1.0`（`Core/EventTiming.cs:279`）也不等 ⇒ 两个条件同时不成立。
  - 附带一条**生成器文案已过期**（不是本条红的根因，但会同族误导人）：
    `gen_ability_anim_durations.py:32/159/181` 仍写着「UM/SW 那批一张都没有真值」，
    而现读 `uncovered` 只剩 13 张（`SOR30` / `SW9` / `UM109` …），`cardsWithTruth` 已经 = `wantedCards` = 93。
- **判据**：
  - 「没真值 ⇒ 退回兜底 + 出声 + 计数」= `Core/AbilityAnimDurations.cs:144-165`（`FallbackHits` / `_warned` / warning 文案）；
  - 「哪些卡是真没真值」= `数据/游戏数据/ability_anim_durations.json` 的 **`uncovered`** 那一节
    （`SOR30 Celestian Sacresant` · `SOR65` · `SW9 Fenrisian Wolf` · `UM109` … 共 13 张，`why` 写的是
    「快照里有这张卡，但它的 `activeAbility` 一条 CardAnim 都没有」）。
  - 兜底值两处一致 = `EventTiming.AbilityFallbackSeconds`（1.0）与 `AbilityAnimDurations.FallbackSeconds`（1.0）。
- **建议改法（只写建议）**：
  - 换夹具：把 `Terminator` 换成 **`uncovered` 里的真卡**，例如 `Celestian Sacresant`（`SOR30`）或
    `Fenrisian Wolf`（`SW9`）—— 本次现读：两者**都在卡池**（`cards_engine.json`；名字**全池唯一**；
    `keywords` 分别是 `Vanguard./Armour 1/Pray` 与 `Tide 1/Ferocity`，即**真的会发 `EvtKind.Ability`** 的那一族），
    **都不在** `Resources/ability_anim_durations.json` 的 `items` 里（byId/byName 都不命中）。
  - **补一条前提断言**（这次就是缺它才把「数据变了」显示成「兜底坏了」）：
    先 `Check(!AbilityAnimDurations.Has("Celestian Sacresant"), "（前提）它确实不在真值表里")`，
    再断 `FallbackHits + 1` 与 `noTruth == AbilityFallbackSeconds`。
    `Has()` 就是为这件事存在的（`Core/AbilityAnimDurations.cs:168-175`）。
  - 同步把 `BattleScene.cs:842` 与 `gen_ability_anim_durations.py:32/159/181` 那几句「UM/SW 一张都没有真值」改掉
    （⛔ 别只改断言、把过期注释留着 —— 这正是本仓「两份说法打架比没有更糟」那条）。

---

## 3) `卡面 ④`：全池 `descZh` 过 `CardIcons.Rewrite` 后残留 1 张（`GOF27`）

- **分类**：**(δ) 数据前提不成立**（官方中文正文里出现了**原版渲染器按设计不认**的记号）；
  断言口径本身没错，但缺一条「这是官方数据异常、不是渲染 bug」的出口 ⇒ 附带半条 **(α)**。
  ⛔ **不是 (β)**：`CardIcons.RewriteMarkTraits` 的行为**与反编译逐句一致**。
- **是不是本批**：**不是**。`cards_engine.json` 不在本批 diff；这行 `descZh` 是**第五十会话 `3251df0`（`K07`）**换进来的：
  - `cd7d3e8` / `4136aab` 的 `GOF27.descZh` = `[[talent]]：$$8425b809e9e64e24f9035f709f5aaee4$$`（能正常渲染）；
  - `3251df0` 起 = `[[盔甲]] 2<br>[[震荡的]]<br>[[不稳定]] [[集结]]: ((Stun_a_random_enemy))`。
  - 来源 = 官方表 `数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms["Card_Description/GOF27"]`
    的**中文列**（本次 python 亲读，原文就是这个串）；灌入点是 `工具/gen_cards_engine.py:2067-2094`
    （官方 `descZh` 层，序②；**没有**任何「zh 与 en 内容是否同一张卡」的校验）。
- **根因（两层）**：
  1. **渲染层（不是 bug）**：原版 `SupportMethods__ModifyLocalization.c:320-467` 的 ③ 支
     **只遍历 `Enum.GetValues(<TraitType>)`**，把 `[[<枚举名>]]` 换成 `GameStaticData.TraitNameToString(...)`；
     `CardIcons.cs:683-712`（`RewriteMarkTraits`）与 `:718-725`（`TraitTermOf`）**逐句照抄**：
     认不出枚举名/图 ⇒ **原样留着 + 出声一次**（`:692-698`），日志里也确实出了那 4 声
     （`battle_run.log:52952 / 52977 / 53002 / 53027`：`[[盔甲]]` `[[震荡的]]` `[[不稳定]]` `[[集结]]`）。
     ⇒ **在原版的语义下，中文客户端的卡面也会印出 `[[盔甲]]` 这四串**（这是官方数据的锅，不是我们的）。
  2. **数据层（真缺陷）**：官方中文表这一条**描述的不是这张卡**。
     - 英文列（同一个键） = `[[talent]]: $$8425b809e9e64e24f9035f709f5aaee4$$` ⇒ 「Talent: Worst Temper」；
     - 我们的卡数据（`cards_engine.json` GOF27）也是 `desc: Talent: Worst Temper`、`keywords: ["Talent"]`、**`armour: 0`**；
     - PnP 成品卡图（`d:/2/Warpforge部队卡片/Orks/1督军/Warpforge_02_Boss-Zagstruk.png`，本次亲读）
       印的是 **`Talent: Worst Temper` / Warlord / 2·2·35**，**没有任何 Armour/Concussive/Unstable/Rally**。
     - 而中文值写的是 `[[盔甲]] 2 / [[震荡的]] / [[不稳定]] [[集结]]: …` —— **另一个版本的 Boss Zagstruk**。
     - 那四个中文词逐条对应官方 `Card_Trait/*` 的中文列（本次现读：`armour=盔甲` · `concussive=震荡的` ·
       `unstable=不稳定` · `rally=集结`）⇒ 中文译者**把「本地化后的词」填进了只认枚举名的记号里**。
  - 全池扫描（本次 python）：**只有这 1 张卡**的中文 `descZh` 带非 ASCII 的 `[[…]]`
    （全池 64 种 bracket token 里 4 个是中文，全在 GOF27）；官方表 `Card_Description/*` 全表同样**只有这 1 条**。
- **判据**：
  - 渲染语义：`d:/2/tools/decomp_full/SupportMethods__ModifyLocalization.c:320-467`（③ 支，`Enum.GetValues` + `TraitNameToString`）；
    本地对应实现 `Core/CardIcons.cs:683-712` 的 doc（`:660-682`，含「认不出就原样留着、不由本层私自加别名表」那条红线）。
  - 内容真值：铁律 7（PnP 成品卡图为尺子）→ 上面那张 `Warpforge_02_Boss-Zagstruk.png`；旁证 = 同一官方键的**英文列**。
  - 消费点（确认它真的会印到卡面上）：`CardPresentation/Battle/BattleDriver.cs:14193`
    （`CardIcons.Rewrite(c.Id, zh ? "descZh" : "desc", …)`）。
- **建议改法（只写建议）**：
  - **首选：改数据（不是改渲染器）**。⚠️ 这是一条**内容裁定**，要调度台/用户拍板，我不替它决定；两个候选：
    - **(a) 按 PnP 与英文列回正**（推荐）：`数据/游戏数据/cardface_fixes.json` 的 **`descZh`** 里加一条
      `"Boss Zagstruk": "天赋：最差的脾气"`（该层是 `descZh` 三个写手里的**最高序**，`gen_cards_engine.py:2095-2098`；
      名字取官方 `Card_Name/8425b809e9e64e24f9035f709f5aaee4` = **最差的脾气**；写法照同族 `天赋：锯骨大夫`）。
      这一改同时修掉**玩家会看到的错规则文字**（现在中文卡面在讲另一张卡）。
      ⇒ 卡面 ④ 自动回到 0 残留，`CardIcons` 一行都不用动。
    - **(b) 保留官方中文原文**：那就得让 `RewriteMarkTraits` 额外按**中文词**反查枚举 —— ⛔ **不建议**：
      原版没有这条路（判据见上），而且这条文本的内容本身就和卡不符，等于把错文本渲染得更漂亮。
  - **不管选哪个，都要补一条断言出口**：把「官方数据里存在不可渲染记号」这件事**显式钉住**，
    ⛔ 别只把 `faceResidual == 0` 放宽成 `<= 1`（那会让下一条同类缺陷静默通过）。建议：
    统计 `[[…]]` 内容含非 ASCII 的卡，断它**恰好等于** `{"GOF27"}`（或数据修好后 = 空集），
    并在注释里写清这是**官方 zh 表的已知异常**（判据 = 本报告 §3）。这样将来多一张同类卡会红，修好一张也会红（故意的）。
  - **顺手记一条账**：`gen_cards_engine.py` 的官方 `descZh` 层目前**不校验 zh/en 是否同一张卡**；
    本条就是从这个缺口进来的。要不要加通用校验是另一条账（本报告只指出缺口，不定方案）。

---

## 四、修复顺序 + 「改断言 vs 改实现」

| 顺序 | 条 | 改什么 | 为什么这个顺序 |
|---|---|---|---|
| 1 | #1 | **只改断言**（`BattleScene.cs:754-762`）：换 `Terminator` 那组同事件 authoritative 夹具；`mfBL != mfEC` 那条同步换成「两条都命中逐卡档」的判别式 | 实现是对的、只差夹具；改一行、零数据风险，且顺手修好一条**空转的灭自证** |
| 2 | #2 | **只改断言**（`BattleScene.cs:841-848`）+ 补一条 `Has()` 前提断言；同步改 `:842` 与 `gen_ability_anim_durations.py:32/159/181` 的过期文案 | 同上；前提断言能把「数据变了」与「兜底坏了」分开 |
| 3 | #3 | **改数据**（`cardface_fixes.json` 的 `descZh`，需拍板）＋ **断言加已知异常出口**；`CardIcons.cs`/生成器渲染逻辑**不动** | 渲染器是照原版实现的，不动它；真正要修的是「中文卡面在讲另一张卡」这条**用户可见**的缺陷 |

**一句话**：**#1、#2 = 改断言（实现不改）；#3 = 改数据（渲染器不改）+ 断言加一条显式出口**。
三条都**不是本批回归**：`#1` 来自 `cd7d3e8`（第四十九会话 `T21`），`#2`/`#3` 来自 `3251df0`（第五十会话 `T14`/`K07`）。

---

## 五、复核用的现读命令（都能复跑）

```bash
# #1：BL31 的这条动画现在挂哪个事件
python -I -X utf8 -c "import json;d=json.load(open('Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json',encoding='utf-8'));print([ (c['id'],c.get('fa'),[(e['ev'],[(x['n'],x['c']) for x in e['es']]) for e in c['events']]) for c in d['cards'] if c.get('nm')=='Maulerfiend'])"
# #2：Terminator 在不在时长表里
python -I -X utf8 -c "import json;d=json.load(open('Unity/MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json',encoding='utf-8'));print([i for i in d['items'] if i.get('name')=='Terminator' or i.get('id')=='UM103'])"
# #3：官方中文里那几个括号词是不是官方词条
python -I -X utf8 -c "import json;t=json.load(open('Unity/数据/游戏数据/官方本地化_en+zh-CN.json',encoding='utf-8'))['terms'];print(t['Card_Description/GOF27']);print(t['Card_Trait/armour'],t['Card_Trait/concussive'],t['Card_Trait/unstable'],t['Card_Trait/rally'])"
```

## 六、没查到的 / 没做的

- ⛔ 没跑 Unity / `*.Run` / player（只读诊断纪律）。
- 「官方中文客户端实际会怎么显示 GOF27」：**只按反编译 `SupportMethods.ModifyLocalization` 推**（③ 支只认枚举名），
  **没有实机截图** —— 原版已关服、中文档要能跑起来才验得到；这一点如实留在这里。
- 没查 `_tmp_view/w1bak/*.bak.json` 之外更早的 VFX 表版本（只用到 2026-10-14 那份做「T21 之前挂在 ability」的对照，够用）。
