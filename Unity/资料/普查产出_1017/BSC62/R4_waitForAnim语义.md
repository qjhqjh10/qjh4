# R4 · waitForAnim 语义现核

> 只读现核 · 2026-10-17 · 单问：`AbilityLogic.PlayAnim` 里 `iVar11 == <列表长度-1>` 那个列表数的是
> 【步骤(cardAnim 的 N 条)】还是【同一个步骤的多个目标(targets)】；`0x58` 是每拍各一份还是整个技能一份。

## 一、结论（一句话）

**数的是【同一个步骤的多个目标（targets）】** —— `iVar11 == param_4._size - 1` = 「**这一拍循环到的最后一个目标**」；
「一个技能的 N 个有序演出步骤（N 拍）」是**另一个表**（外层 enumerator 走的 `AbilityLogic.cardAnimations`）。
**`0x58`（= `AnimInfo.waitAnimToApplyEffect`）是【每一拍各一份】**（不同拍可不同值），**不是整个技能共用一个**。
⇒ `waitForAnim = （是最后一个目标）&&（这一拍自己的 waitAnimToApplyEffect != 0）`。

## 二、证据（逐条）

1. **签名就是答案** —— `d:/2/tools/il2cpp_out/dump.cs:19516`：
   `private void PlayAnim(BattleManager manager, AbilityData abilityData, List<CardScript> abilityTargets)`
   ⇒ 第 4 参（= 反编译的 `param_4`）= **`List<CardScript>` 的 targets**。
   （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityLogic.cs:98` 同。）
2. **外层是步骤** —— `PlayAnim.c:45-46` enumerator 走 `param_1 + 0x18`；
   `dump.cs:19457` = `public List<AssetReferenceTyped<CardAnim>> cardAnimations; // 0x18`
   ⇒ 外层 `foreach` = **N 拍**，每拍 = 一张 `CardAnim` SO。
3. **被比的 size 是 param_4 的** —— `PlayAnim.c:178` `while (iVar11 < *(int*)(param_4 + 0x18))`；
   `:217` `if (iVar11 == *(int*)(param_4 + 0x18) + -1)` ⇒ 比的是 **param_4（targets）的 `_size`**。
4. **`lVar5` 是「这一拍的」AnimInfo** —— `PlayAnim.c:65` `lVar5 = …AssetReferenceTyped<object>__Load(plVar3,…)`（**每个外层迭代重载一次**）；
   `:70` `lVar5 = *(longlong *)(lVar5 + 0x18)`；`dump.cs:20379` `public AnimInfo animInfo; // 0x18`（`CardAnim : ScriptableObject`）
   ⇒ `lVar5` = **当前这一拍的 AnimInfo**。`lVar5+0x18/0x1c/0x20` = `startPosOption/endPosOption/pointTowardsOption`
   （`dump.cs:25210-25212`），代码正是按 `PositionOption` 传给 `AbilityLogic__GetPos`（`:194/:203/:215`）。
5. **`0x58` 是谁** —— `dump.cs:25236` `public bool waitAnimToApplyEffect; // 0x58`
   （`[FormerlySerializedAs("pauseDuringAnim")]`）。`:218` 与 `:274` 读的都是 `*(char*)(lVar5 + 0x58)` = **这一拍的** animInfo 字段。
6. **内层元素是「卡」** —— `:185 CardScript__IsInHand(lVar9,0)` · `:226 get_Item(param_4,iVar11)` 被放到 `AddCardAnim` 的 `targetCard` 位
   （`dump.cs:6627 AddCardAnim(AnimInfo animInfo, … bool waitForAnim, … CardScript casterCard, CardScript targetCard, …)`）。
7. **调用侧传进来的就是 GetTargets 的结果** —— `PlayAbility.c:262-265` `uVar14 = AbilityLogic__GetTargets(…)；*(param_1 + 0x78) = uVar14;`
   `:` `273 AbilityLogic__PlayAnim(param_1,param_2,param_3,*puVar1,0)`；`dump.cs:19481` `private List<CardScript> possibleTargets; // 0x78`。
8. **反证「每拍各一份」** —— 非 multi-target 那一支 `:274` 把 `lVar5 + 0x58` **原样**当 `waitForAnim` 传走、**没有任何「最后一拍」门**。

## 三、我排除掉的另一种读法（以及为什么排除）

**被排除**：「`param_4` = 一个技能的 N 个步骤 ⇒ 只有最后一拍才 waitForAnim」
（= `T05_逐卡演出与特效接线.md` §四② 第 3 步 · `AbilityAnimDurations.cs:12`/`:15` 的写法）。
**排除理由**：① 签名写着 `List<CardScript> abilityTargets`（证据 1）；
② `param_4` 的元素在循环体里**一律被当卡**（`IsInHand` / `GetPos` / `targetCard`，证据 6），
而**步骤在外层**（`cardAnimations`，元素是 `AssetReferenceTyped<CardAnim>`，证据 2）；
③ 调用点传进来的就是 `GetTargets(...)`（证据 7）。
**错因（可复查）**：把 `iVar11` 那个循环的**上界来源**误记成 `cardAnimations.Count` ——
实际那里是**外层 `foreach`(enumerator)** 与**内层 `while`(param_4)** 两个不同的表，`iVar11` 只属内层。

## 四、该照哪一处写

⇒ **照摸底代理那一说**：「**每一拍看自己那张 `CardAnim.animInfo.waitAnimToApplyEffect(0x58)`**；
`(iVar11 == count-1)` 是**多目标循环里的最后一个目标**」。
⇒ 落地口径：`waitForAnim(拍 i, 目标 j) = (j == targets.Count-1) && animInfo_i.waitAnimToApplyEffect`。
我们若**不按目标逐个播**（一拍只播一次），就退化成 `= animInfo_i.waitAnimToApplyEffect`
—— 这**正好等于原版单目标那一支**（`PlayAnim.c:274`）。
⚠️ **T05 §四② 第 3 步** 与 **`AbilityAnimDurations.cs:12` / `:15`** 的「**只有最后一拍**才 waitForAnim」**是错的**，
按铁律 5 应就地订正（那句把 `param_4`=targets 当成了 `cardAnimations`=steps）。
⚠️ **连带**：`AbilityAnimDurations` 文件头「排在末尾那条若 `waitAnimToApplyEffect != 0` 才 `waitForCompletion=true`」里
的「**排在末尾那条**」是同源误读 —— **时长求和不受影响**（它确实是 N 拍之和），但**「哪一拍带 wait 标记」这句要改**。

## 五、还没查清的（⛔ 不猜）

1. **`waitForAnim` 最终谁消费** —— 它经 `BattleAction___ctor` → `AddAutoActionToQueue` 存进 `BattleAction`（`BattleManager__AddCardAnim.c:34,95`），
   我**没追到读它的那一处**；`BattleManager._ResolveAnim_d__466__MoveNext.c` 里读的是 `AnimInfo + 0x60/0x68/0x70/0x78/0x80`，**没读 0x58**。
2. **`PositionOption == 2` 是哪一档** —— 它是「这一拍要不要**按目标逐个**播」的判据（`PlayAnim.c:171`）。我**只**据 `dump.cs:25210-25212` +
   `GetPos(…PositionOption)` 断定 `lVar5+0x18/0x1c/0x20` 是那三个位置选项，**没去查枚举定义**。
3. **实际影响面** —— 真数据里有多少张卡的**非末拍** `waitAnimToApplyEffect != 0`（决定这条订正会不会真改变行为），**没查**。

**搜过的词 / 文件**：`PlayAnim` `AddCardAnim` `waitAnimToApplyEffect` `cardAnimations` `waitForAnim`；
`d:/2/tools/decomp_full/`（`AbilityLogic__PlayAnim.c` 全 281 行 · `AbilityLogic__PlayAbility.c` 前 1891 行 ·
`AbilityLogic__DeepCopy.c` · `BattleManager__AddCardAnim.c` · `AnimInfo__GetAnimDuration.c` ·
`BattleManager._ResolveAnim_d__466__MoveNext.c`）· `d:/2/tools/il2cpp_out/dump.cs` · `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{AbilityLogic,CardAnim,AnimInfo}.cs` ·
`d:/4/Unity/MyGame/Assets/CardPresentation/Core/AbilityAnimDurations.cs` · `d:/4/Unity/资料/任务线/战斗线/任务/T05_逐卡演出与特效接线.md`。
