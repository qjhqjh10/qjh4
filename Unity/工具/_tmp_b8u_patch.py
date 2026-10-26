# -*- coding: utf-8 -*-
"""B8_U：生成 账/_parts/B8_U.tsv（只放改过的行·11 列·LF）。
⛔ 不改主表；按 (类, 方法) 定位 + assert，不写死行号。跑法：
    python -I -X utf8 d:/4/Unity/工具/_tmp_b8u_patch.py
"""
import io

BASE = 'd:/4/Unity/资料/完全复刻/账/'
OUT = BASE + '_parts/B8_U.tsv'


def load(name):
    b = io.open(BASE + name, 'rb').read()
    assert b.count(b'\r') == b.count(b'\r\n'), 'bare CR in ' + name
    lines = b.decode('utf-8').split('\n')
    hdr = lines[0].split('\t')
    assert len(hdr) == 11, hdr
    rows = []
    for l in lines[1:]:
        if not l.strip():
            continue
        f = l.split('\t')
        assert len(f) == 11, (name, f[0], f[1], f[2], len(f))
        rows.append(f)
    return hdr, rows


def find(rows, cls, meth):
    hits = [r for r in rows if r[1] == cls and r[2] == meth]
    assert len(hits) == 1, (cls, meth, len(hits))
    return hits[0]


out = []
D = '2026-10-26'


def emit(r):
    assert len(r) == 11, (r[1], r[2], len(r))
    for c in r:
        assert '\t' not in c and '\n' not in c, (r[1], r[2])
    out.append('\t'.join(r))


# ============================================================ 战斗界面_HUD
hud_hdr, hud = load('战斗界面_HUD.tsv')

# --- 1. ActiveSkillDisplay.SetupActiveSkill ---------------------------------
r = find(hud, 'ActiveSkillDisplay', 'SetupActiveSkill(CardScript parentCard)')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:115-196（🥈 `SetupActiveSkill`；'
        '**订正 2026-10-26（`B8_U`）：拼后缀那道闸在 :160**；`:193` = else 支「描述原样」；'
        '`:188` = `text6 + " " + targetAvailableText`；`:196` 写 `descText`；`:172` = `maxTargets==1` 判定）')
r[4] = ('填费/名字/描述；「可选目标」那段**拼进 descText**（desc + " " + targetAvailableText），'
        '**只在 `targetsAvailableCounter != null && targetsAffected != any` 时拼**（:160；else 支 :191-195 = 描述原样）；'
        '`TargetsAvailableText` 节点**零写入**；两档 = `maxTargets==1 && 有目标 ⇒ "(" + 第一个目标卡名 + ")"`（SupportMethods.cs:1494）/ '
        '否则 `"{n} available"`（:1478）')
r[7] = (r[7] + ' ｜【`B8_U` ' + D + ' 复核】面板侧三件**逐行核过**（`SkillPanel.SetTargetsSuffix` = 空格 + **纯文本**、'
        '`_targets` 恒写空串、`targetsAny` 闸在且默认 false）；剩余两支 = ① `EffectSpec` 无 `maxTargets`/`targetsAffected`（引擎线）'
        '② `BattleDriver.ShowSkillPanel` 未喂（F5）⇒ **都在本代理白名单外，本行未改代码**。'
        '观测量 = `SkillPanel.TargetsText` + `DescText` 尾部（原版：单目标 ⇒ `(名字)`、多目标 ⇒ `{n} 可用`；我们：同，但 `targetsAny` 恒 false ⇒ 永不抑制）')
r[10] = ('🔴 **判据出处订正（铁律 5）**：账原写「闸在 :170」→ 实读 **:160**；'
         '⚠️ **`B8_U` ' + D + ' 再订正**：`:170` 只是局部变量 `string targetAvailableText;` 的**声明**（不是判据），'
         '「描述原样」那句在 **:193**；`maxTargets==1` 判定在 **:172**。'
         '｜代码侧同步订正：`Battle/SkillPanel.cs:2083` 的注释原也写 `:170`，已就地改为 `:160`（**零行数变化**）。'
         '｜面板侧已补齐（含 targetsAny 闸）；剩两支不在本线：① `EffectSpec` 无 `maxTargets`/`targetsAffected`（引擎线）② `BattleDriver.ShowSkillPanel` 喂值（F5）')
emit(r)

# --- 2. BasicCardUI.SetEmptyRarityStars -------------------------------------
r = find(hud, 'BasicCardUI', 'SetEmptyRarityStars()')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/BasicCardUI.cs:**514-517**（🥈 `SetEmptyRarityStars`；'
        '⚠️ **`B8_U` ' + D + ' 订正（铁律 5）**：原写 `:583-586` = `OnDescriptionShow`，**另一个方法**）'
        '+ CardRarityHolderController.cs:13-52（`SetCardRarity(rarity, maxRarity)`：`maxRarity == rarityLevelSprites.Length` ⇒ '
        '索引 = clamp(rarity-1, 0, len-1)；不等 ⇒ `LogError` 后取 `sprites[0]` —— **两支都落 sprite[0]**）')
r[4] = ('`cardRarityHolderController.SetCardRarity(0, maxCardRarity)` ⇒ 索引 = `clamp(rarity-1,0,len-1)` = **0 ⇒ 最低档（common）那颗宝石**'
        '（不是「不画」）；宝石 = 直接写 `cardRarityRenderer.sprite`（无淡变、无开关）')
r[5] = 'A'
r[6] = ('CardPresentation/Battle/CardDisplayWindow.cs:1870-1894（`ShowBattleCard` 里的 masked 克隆 —— :1877 `masked.rarity = null`）+ '
        'Core/CardView.cs:11834（`RarityGem`：未知/空 rarity 落 common 那张）+ :14251（`SetStatsHidden`，`B8_N` 那批）'
        '（等价实现 = ShowBattleCard 的 masked 克隆）')
r[7] = ('逐句对过：原版 = `cardRarityHolderController.SetCardRarity(0, maxCardRarity)` ⇒ `clamp(-1,0,len-1)` = **0 ⇒ sprite[0]**；'
        '我们 = 展示窗克隆 `CardData` 把 `rarity` 置 null ⇒ `RarityGem` 落 common 那张（同一颗宝石）。'
        '**差别** = 原版走「索引夹到 0」，我们走「未知档 = common」—— 两支落到同一张贴图；'
        '卡框不受影响（框档走 `CardTierFor` 恒 ≥ 1，`CardArt.Frame` 里那条 rarity 兜底永不走）。'
        '伏击支那一侧（`CardScript:14231`）在本工程 Board 面无观感（宝石层在 Board 面本来就不画：`CardView.cs` 的 `Show(_gem, !board)`）')
r[8] = ''
r[10] = ('🔴 **判据出处订正（铁律 5）**：`:583-586` 是 `OnDescriptionShow`；`SetEmptyRarityStars` 真身在 **:514-517**。'
         '本行 = **三处合成的结果**：F6 的 `rarity = null` + `B8_N` 的 `SetStatsHidden` 写口 + `B8_U` ' + D + ' 在 `ShowBattleCard` 里那句调用'
         '（护甲整格关、其余三格印 `?`）⇒ 若 `B8_N` 那批回退，本行要跟着回退。'
         '观测量 = 伏击窗开窗后：宝石贴图 == `RarityGem(null)` 那张（common）、且 `CardView.StatsHiddenFor(s)` 真 + `AmbushMarkText(s) == "?"`'
         '（读口在 `Core/CardView.cs:14283 / :14362`；**断言尚未写、宿主本批未跑**）')
emit(r)

# --- 3. BattleCardUI.ShowAvailableTargets -----------------------------------
r = find(hud, 'BattleCardUI', 'ShowAvailableTargets()')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/BattleCardUI.cs:1272-1345（🥈，`B8_U` ' + D + ' 逐句复读）'
        '+ SupportMethods.cs:1478/1494（两档词条）')
r[4] = ('**三合一闸**（:1275）：`isPlayer && cardState == inHandShowing && ShouldShowTargetsAvailable()`；'
        '`maxTargets==1 && 有目标`（:1285）⇒ 目标卡名（目标是对面的 `Trap`（:1306-1312）/ `SecretOrder`（:1324-1328）战术卡时换成 '
        '`Card_Trait/trap` / `Card_Trait/secretOrder` 词条），否则 `GetTargetsAvailableText(n)`；'
        '末句 **`SetDescription(desc + "<i>" + 那段 + "</i>")`（:1342 —— **无空格** + 富文本斜体）**；'
        '描述取的是 `GetLocalizedCardDescWithLineBreak()`（:1332/:1338，**带换行那一版**），不是普通 desc')
r[7] = (r[7] + ' ｜【`B8_U` ' + D + ' 复核】:1272-1345 逐句读过，账的三合一闸/两档/词条替换/`<i>` 全部对得上；'
        '**仍 C**（三步落点全在白名单外：`Core/CardView` 描述写口 · `RuleCore.PlayTargetPool` · `Hand/CardInteraction`）。'
        '观测量 = 抬牌后卡面描述那颗 TMP 的 `text` 尾部（原版 = `desc + "<i>(名字)"` 或 `"<i>{n} available"` + `</i>`；我们 = 无后缀）。'
        '⚠️ 落地前先确认那颗 TMP 的 `richText = true`，否则 `<i>` 会**原样印出来**（这是与技能面板抽共用函数的坑之一）')
r[10] = (r[10] + ' ｜🔴 **`B8_U` ' + D + ' 提醒**：本方法与 `ActiveSkillDisplay.SetupActiveSkill` 的拼接**口径不同**'
         '（本处 = `desc + "<i>" + 后缀 + "</i>"`（无空格、斜体、带换行 desc）；技能面板 = `desc + " " + 后缀`（空格、纯文本））'
         '⇒ ⛔ **别抽共用函数**。两处已各自在代码注释里写死（`Battle/SkillPanel.cs` 那一块）。')
emit(r)

# --- 4. BattleErrorUIManager.OpponentDesyncCancelMatch ----------------------
# ⛔ **不出这一行**：`_parts/B8_L.tsv` 已把同一行判 **A** 并把缺口真落了（`Net/NetBattle.cs:1815 LeaveWithoutResult`
#    + `Close(say:false)`），本代理那份 C 是**过期的** ⇒ 若合并会把 A 盖回 C（`check_patch_conflicts` 报的正是这一条）。
#    本代理对同一缺口的判据订正（`Abort` 已存在 / 真缺「离场」那一半 / ⛔ 别再写 `CancelMatchWithoutResult`）
#    与 `B8_L` 的落地**互相独立地一致** ⇒ 只写进报告，不动账。


# --- 5. CardDisplayWindow.SetupTutorialObj ----------------------------------
r = find(hud, 'CardDisplayWindow', 'SetupTutorialObj(RawCardScript rawCardData, bool forceHide)')[:]
r[3] = ('decomp_full/CardDisplayWindow__SetupTutorialObj.c:26-72（🥇 现读，闸的六项在 :44-58）+ '
        'cpp2il/out_cpp2il_dev_ilspy/CardDisplayWindow.cs:1097-1141（🥈 同体）+ 调用点 :446 '
        '（`forceHide` 的真来源 = `ShowBattleCard:664` 的 `DisplayCardEffects` 返回值）')
r[4] = ('六项闸全真 ⇒ `tutorialObj.SetActive(true)`，然后 `tutorialObjUnit.SetActive(rawCardData.IsUnit())`、'
        '`tutorialObjCost.SetActive(cardType != Hero(10) && manaCost > 0)`（+0x94 / +0x78；Hero 判据另有 `!= 10`）；'
        '闸 = ① `BattleManager.Instance == null` ⇒ 强制 forceHide ② `LiveOpsManager.GetHandler<GameModes>().GetActiveEvent<TutorialEvent>()` 非空 '
        '③ 事件 `+0x20` 非空 ④ 该事件里那个 Dictionary 的某 key 计数 **< 6** ⑤ `!forceHide` ⑥ `enableTutorial`（+0xe0）。'
        '⚠️ `forceHide` = **「这张卡有没有非空效果行」** ⇒ 有 buff 行就不画教程指向物（同一块区域让给效果清单）')
r[6] = '（无；展示窗没有 `TutorialObjs` 子树）'
r[7] = ('缺「教程指向物」本身。🔴 **`B8_U` ' + D + ' 把判据补齐（原写「prefab 与摆放判据未取」）**：'
        '13 场 `Battle Arena*.unity` 的 `CardDisplayWindow` 组件都带三个 PPtr（`enableTutorial = 1`；核过 Arena 1/2/3 的值、13 场核到节点在）+ '
        '`TutorialObjs` 子树已从场景序列化现读（Arena 1）：根 = 全屏 RT（anchor 0,0-1,1，size 3501.37×2291.01，pos −46.56 / 4.38），两个子件 = '
        '① `UnitObjs`（center，100×100）下三条 TMP：`MeleeText` 380×55 @(−474,−218) 词条 `Battle/Tips/MeleeAttack` · '
        '`RangedText` 380×55 @(−410,−289.59) 词条 `Battle/Tips/RangeAttack`（**Range** 不是 Ranged）· '
        '`HealthText` 380×55 @(481,−273) 词条 `Battle/Tips/HealthPoints`；'
        '② `EnergyText` 340.269×59.342 @(497,252) 词条 `Battle/Tips/EnergyCost`（默认字 "Energy Cost"）。'
        '⚠️ 未核 = 那个 Dictionary 的 key（`DAT_1842c3bd8` 常量未解，含义疑为「教程进度计数」）· 其余 10 场的 rect 未逐场读')
r[10] = ('**转教程线**（`教程_引导.tsv` 已有一族）；本窗只负责「有这件就显隐」。'
         '破表判据（三件 PPtr / `TutorialObjs` 子树 / 四条词条）已由 `B8_U` ' + D + ' 补进「差异」列 ⇒ 教程线可直接照建四条 TMP + 一个「教程在跑」入参，'
         '不必再回场景翻；`forceHide` 那一侧我们已有等价量（`SetEffectRows` 的 `n > 0`）')
emit(r)

# --- 6/7. Mana 两行（体尾订正） ---------------------------------------------
r = find(hud, 'ManaManager', 'SetManaSaved(int savedMana, int unUsedMana, int totalMana, bool useNumberChange)')[:]
r[3] = r[3].replace('ManaTypeHolder.cs:186-260',
                    'ManaTypeHolder.cs:**186-246**（尾行订正 `B8_U` ' + D + '：`:248` 起已是 `ToggleManaAccumulation`）')
r[7] = (r[7] + ' ｜【`B8_U` ' + D + '】观测量 = 生产路径上 `SetSavedMana` 的调用次数（现读 **0 次**，因为引擎没有 savedMana 这一格）'
        '⇒ 本行今日**只能靠自检口**验：`SetSavedManaForTest(1, 3, 4, true)` 后 ON 亮 / OFF 灭 / 数字 2→3')
r[10] = (r[10] + ' ｜`B8_U` ' + D + '：体尾 `:260` → **`:246`** 已订正（半句判据原来落在 `ToggleManaAccumulation` 里）。'
         '⚠️ 同一处错行号还写在 `Battle/BattleDriver.cs:32630` 的注释里（`cpp2il …/ManaTypeHolder.cs:186-260`）⇒ **改动请求**（`BattleDriver.cs` 不在本代理白名单）')
emit(r)

r = find(hud, 'ManaTypeHolder', 'SetSavedMana(int savedMana, int unUsedMana, int totalMana, bool useNumberChange)')[:]
r[3] = r[3].replace('ManaTypeHolder.cs:186-260',
                    'ManaTypeHolder.cs:**186-246**（尾行订正 `B8_U` ' + D + '：`:248` 起已是 `ToggleManaAccumulation`）')
r[7] = (r[7] + ' ｜【`B8_U` ' + D + '】观测量（自检口已备）：`SetSavedManaForTest(saved=1, unUsed=3, total=4, useNumberChange:true)` ⇒ '
        '① `VFXON.activeSelf = true` / `VFXOFF = false` ② 那颗数字先 `3−1 = 2`、1.0 s 后回 `3` ③ `manaAccumulationImageOn.color.a` 在 0.5 s 后开始、1.0 s 到目标（`DOFade(目标,1.0).SetDelay(0.5)`，而 `DelayedCall` 在 **1.0 s** 就触发 —— **补间要到 1.5 s 才结束**）；'
        '原版侧数值由 `.rdata` 两个常量推出（`0.5f`@`0x1834b2bb4` · `1.0f`@`0x1834b2bb8`，同一个 `1.0f` 既是 DOFade 时长又是 DelayedCall 延迟）')
emit(r)

# ============================================================ 特效动画
fx_hdr, fx = load('特效动画.tsv')

OBJ_NOTE_WRONG = ('【`B8_U` ' + D + ' 判据订正（铁律 5）：本行原「判据要点」是**另一个方法**的体（整列串了一格），已重写；'
                  '账原「要点」描述的语义仍在 `ObjAnimEffects` 里，只是落在了隔壁行】')

# --- PlayDelayedEffect ------------------------------------------------------
r = find(fx, 'ObjAnimEffects', 'PlayDelayedEffect(AnimEffectData effectData)')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs:222-235（🥈 `PlayDelayedEffect`）+ :13-86'
        '（状态机 `_003CPlayDelayedEffect_003Ed__6.MoveNext`：`WaitForSeconds(animDelay)` 之后 `_003C_003E4__this.PlayEffect(effectData)`）')
r[4] = ('**只是延迟协程**：`new WaitForSeconds(animDelay)` → 等完调 `PlayEffect(effectData)`；'
        '本方法自己**不分派 effectType、不实例化、不播声音**（「按 animDelay 分流」是 `Start`/`PlayEffects` 的事）')
r[7] = r[7].replace('真缺口：', '真缺口（等价物 = 一条 `WaitForSeconds` 协程，随「AnimEffectData 驱动」一起建）：')
r[10] = r[10] + ' ｜' + OBJ_NOTE_WRONG
emit(r)

# --- PlayEffect -------------------------------------------------------------
r = find(fx, 'ObjAnimEffects', 'PlayEffect(AnimEffectData effectData)')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs:175-220（🥈 `PlayEffect`）+ `AnimEffectType.cs`'
        '（enum：`Undefined = 0` / `Fade = 10` / `AnimPrefab = 20` / `PlaySound = 30` / `ScreenShake = 40`）')
r[4] = ('**按 effectType 分派**（:178 起）：`> Fade` 那三档 —— 20 ⇒ `ResolveAnimPrefab`；'
        '30 ⇒ **内联** `audioSource.PlayOneShot(clip, SoundFxVolume * asset.volume)`（无 source ⇒ `LogWarning("No audiosource available")`；音量 0 或 clip 空 ⇒ 不播）；'
        '40 ⇒ **内联** `Resources.FindObjectsOfTypeAll<CameraShake>()[0].DoScreenShake(shakeInfo)`（数组空/首元素空 ⇒ 不做）；'
        '`== Fade(10)` ⇒ `ResolveFade`；`== Undefined(0)` ⇒ **什么都不做**')
r[7] = r[7].replace('真缺口：同 ObjAnimEffects', '真缺口（三者都缺一个统一分派层：`WarpforgeEffectPlayer.Play` / `WFSoundPlayer` / `WFModuleScreenShake` 各自都能做，但没有「按 effectType 选一个」的那一层）')
r[10] = (r[10] + ' ｜' + OBJ_NOTE_WRONG +
         ' ｜🔴 做这一行时**照 :175-220 做**：`PlaySound` / `ScreenShake` 是**内联**在本方法里的，'
         '`ResolvePlaySound` / `ResolveScreenShake` 两个方法在原版是**死代码**（0 调用者，见那两行）')
emit(r)

# --- PlayEffects ------------------------------------------------------------
r = find(fx, 'ObjAnimEffects', 'PlayEffects()')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs:137-173（🥈 `PlayEffects`）'
        '+ :94-135（`Start`，同一套遍历但多一道 `waitToPlayEffects` 闸）')
r[4] = ('**遍历 `animEffects`**：逐条 `animDelay != 0 && effectType != Fade` ⇒ `StartCoroutine(PlayDelayedEffect(e))`，'
        '否则立即 `PlayEffect(e)`；空数组/null ⇒ 直接 return。**本方法没有 `waitToPlayEffects` 闸**（那道只在 `Start`）')
r[7] = r[7].replace('真缺口：无统一分派件', '真缺口（= 「`AnimEffectData[]` 驱动」那一层：遍历 + 分流 + 起协程）')
r[10] = (r[10] + ' ｜' + OBJ_NOTE_WRONG +
         ' ｜与 `Start` 的**唯一差别** = 有没有 `waitToPlayEffects` 那道闸（`Start` 有）。本行是「外部触发一次」的入口')
emit(r)

# --- ResolveAnimPrefab ------------------------------------------------------
r = find(fx, 'ObjAnimEffects', 'ResolveAnimPrefab(AnimEffectData effectData)')[:]
r[3] = 'cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs:289-311（🥈 `ResolveAnimPrefab`）'
r[4] = ('`!GameStaticData._disableParticles && animPrefab != null` ⇒ `Instantiate(animPrefab)` 后把 `transform.parent` 设成**本组件自己的 transform**；'
        '`animPrefab == null` ⇒ `LogWarning("Undefinded anim prefab")`。⚠️ 实例化的 position/rotation 两个实参被反编译成 `(&num)(&vector)`（default 局部）⇒ '
        '**疑似置零**，方法体里**没看到 `posOffset` 参与**')
r[6] = 'WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:191（按名播 prefab 入口；无「按数据在指定 transform 下实例化」这一形）'
r[7] = r[7].replace('真缺口：同 ObjAnimEffects',
                    '真缺口（有「按名播 prefab」入口，缺「按 AnimEffectData 在自身 transform 下实例化 + `_disableParticles` 闸 + 空 prefab 出声」）')
r[10] = (r[10] + ' ｜' + OBJ_NOTE_WRONG +
         ' ｜⚠️ 原账这一行写的是「协程等 animDelay」（那是 `PlayDelayedEffect`）⇒ 照原账实现会把它写成协程')
emit(r)

# --- ResolveFade ------------------------------------------------------------
r = find(fx, 'ObjAnimEffects', 'ResolveFade(AnimEffectData effectData)')[:]
r[3] = 'cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs:237-287（🥈 `ResolveFade`）'
r[4] = ('对 `spritesToFade[]` 的每个非空项：`SupportMethods.SetSpriteAlpha(项, startAlpha)` → '
        '`DOFade(项, endAlpha, animDuration).SetDelay(animDelay)`；再对 `textsToFade[]` 同法（`SetTextAlpha` + TMP `DOFade`）。'
        '两段各自独立起补间（**不是一条 Sequence**）；数组为 null 会直接 NRE（原版没判空）')
r[6] = '（无；只有零散的 DOFade）'
r[7] = '真缺口（没有「数据驱动的批量淡变」：起点先置 `startAlpha`、再 `DOFade` 到 `endAlpha` 并 `SetDelay(animDelay)`）'
r[10] = (r[10] + ' ｜' + OBJ_NOTE_WRONG +
         ' ｜⚠️ 原账这一行写的是「`_disableParticles` + Instantiate」（那是 `ResolveAnimPrefab`）⇒ **原账把 Fade 与 AnimPrefab 两行的落点/差异也换错了**（本行原落点写的是 EffectPlayer 的 prefab 入口）')
emit(r)

# --- ResolvePlaySound（原版死代码 ⇒ D） -------------------------------------
r = find(fx, 'ObjAnimEffects', 'ResolvePlaySound(AnimEffectData effectData)')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs:313-336（🥈 `ResolvePlaySound`，**private**）'
        '+ 调用者普查：本类 0 处（`PlayEffect` :184-206 **内联**了同一段）')
r[4] = ('`audioSource.PlayOneShot(clip, SoundFxVolume * asset.volume)`；无 source ⇒ `LogWarning`；音量 0 或 clip 空 ⇒ 不播。'
        '🔴 **原版死代码**：本类 **0 调用者**（`private` + 唯一同类用法已内联在 `PlayEffect`）⇒ 全库不可能被调到')
r[5] = 'D'
r[6] = '（不建独立方法；要做的行为在 `ObjAnimEffects.PlayEffect` 那一条里）'
r[7] = ('**原版死代码 ⇒ 铁律 11 例外①（原版本身不调它）**。行为由 `PlayEffect`（:184-206）内联承担 ⇒ 本行核销到 `PlayEffect` 那一行，'
        '**⛔ 别照本行另建一个永远不跑的方法**')
r[8] = ''
r[10] = ('🔴 **`B8_U` ' + D + ' 判据订正（铁律 5）**：原账把「批量淡变」写在本行（那是 `ResolveFade`），'
         '并按「要实现的方法」定成 C。实读 ⇒ 本方法是**死代码**（`private`、全类 0 调用者、体与 `PlayEffect` 的内联段逐字等价）⇒ **判 D**。'
         '⚠️ 若将来复核：只核 `PlayEffect`，别再核本行')
emit(r)

# --- ResolveScreenShake（原版死代码 ⇒ D） -----------------------------------
r = find(fx, 'ObjAnimEffects', 'ResolveScreenShake(AnimEffectData effectData)')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs:338-345（🥈 `ResolveScreenShake`，**private**）'
        '+ 调用者普查：本类 0 处（`PlayEffect` :207-214 **内联**了同一段）')
r[4] = ('`Resources.FindObjectsOfTypeAll<CameraShake>()[0].DoScreenShake(shakeInfo)`（数组空 / 首元素空 ⇒ 不做）。'
        '🔴 **原版死代码**：本类 **0 调用者** ⇒ 不可达；且 13 场 `CameraShake` 实例 **0 个** ⇒ 连 `PlayEffect` 内联那段在原版战斗场景里也**空转**')
r[5] = 'D'
r[6] = '（不建独立方法；内联段的等价物 = `WarpforgeVFX/Runtime/WFModuleScreenShake.cs:167-196`，随 `PlayEffect` 那一行走）'
r[7] = ('**原版死代码 ⇒ 铁律 11 例外①**；而且内联那段在原版 13 场里也无实例可作用（`CameraShake` 0 个）⇒ 净观感 = 空（一个字都不变）')
r[8] = ''
r[10] = ('🔴 **`B8_U` ' + D + ' 判据订正（铁律 5）**：原账把「`PlayOneShot`」写在本行（那是 `ResolvePlaySound`）⇒ 整列串了一格；'
         '实读 ⇒ 本方法**死代码**（`private`、0 调用者）⇒ **判 D**。⚠️ 屏震的真落点只在 `PlayEffect` 的内联段 + '
         '`CameraShakerManager.DoSimpleShake` 那一族（那是别的账行）')
emit(r)

# --- Start（原 D 的依据是别的方法 ⇒ 改 C） ----------------------------------
r = find(fx, 'ObjAnimEffects', 'Start()')[:]
r[3] = 'cpp2il/out_cpp2il_dev_ilspy/ObjAnimEffects.cs:94-135（🥈 `Start`）'
r[4] = ('`if (waitToPlayEffects) return;` 之后**遍历 `animEffects`**（与 `PlayEffects` 同一套分流：`animDelay != 0 && effectType != Fade` ⇒ 起协程，'
        '否则立即 `PlayEffect`）；数组空/null ⇒ return。**这是「自动播」入口，闸 = 序列化字段 `waitToPlayEffects`**')
r[5] = 'C'
r[6] = '（无；我们没有 `ObjAnimEffects` 这一层，9 处实例的 `AnimEffectData` 数组也没进任何数据文件）'
r[7] = ('真缺口 = 与 `PlayEffects` 同一个「`AnimEffectData[]` 驱动」层（多一道 `waitToPlayEffects` 闸）。'
        '⚠️ 9 处实例的 `waitToPlayEffects` / `animEffects` 序列化值**本轮未读**（场景导出里 0 命中）⇒ 若全是 `waitToPlayEffects=1` + 空数组，本行实际空转')
r[8] = 'P2'
r[10] = ('🔴 **`B8_U` ' + D + ' 判据订正（铁律 5）**：本行原「判据要点」与「D 的依据」**都是 `ResolveScreenShake` 的内容**'
         '（`CameraShake` 0 实例 ⇒ 空转），与 `Start` 无关 ⇒ **D 的依据不成立**，改判 **C**（缺口同 `PlayEffects`）。'
         '待核 = 9 处实例的 `waitToPlayEffects` / `animEffects` 序列化值（若全空可回判 D）')
emit(r)

# --- CardEffectsService.EnableCardEffectSlots（上限已实现 ⇒ A） -------------
r = find(fx, 'CardEffectsService', 'EnableCardEffectSlots(bool isPlayer, List<CardEffect> effectsToDisplay)')[:]
r[3] = ('cpp2il/out_cpp2il_dev_ilspy/CardEffectsService.cs:91-132（🥈 `EnableCardEffectSlots`，现读全 42 行）'
        '+ :18-89（`EnableEffects` = 它 + `DisableLeftoverSlots`，返回「非空条数 > 0」）+ :134-148（`DisableLeftoverSlots`）'
        '+ `CardDisplayWindow.cs:664 / :711 / :446`（这个 bool 一路传成 `SetupTutorialObj` 的 `forceHide`）'
        '+ **13 场序列化实测 `cardEffectSlots` 数组长恒 = 5**')
r[4] = ('从当前槽号起逐条 `LoadEffect`：**文本空的也占一槽**（`Disable` 但槽号仍 +1 ⇒ 留空洞），非空才计数；'
        '槽数组用完 ⇒ **静默截断**并 `return num`。⇒ 上限 = `cardEffectSlots.Length`，我们这边 = 5')
r[5] = 'A'
r[6] = ('CardPresentation/Battle/CardDisplayWindow.cs:915-943（等价实现 = SetEffectRows：`Mathf.Min(rows.Count, CardWinBox.EffSlots)` + '
        '超限 `LogWarning`）+ Core/CardWinBox.cs:88（`EffSlots = 5`）+ Battle/CardDisplayWindow.cs:851-877（RowsOf 铸行）')
r[7] = ('逐句对过（判据 3 段共 70 行）：**上限 5 / 超限截断 / 非空才计数 / 返回值语义**四件都同。'
        '**唯一的机制差别** = 原版「文本为空的效果**也占一槽**」⇒ 清单中间会**留空洞**；我们按 buff 铸行、空行根本不铸 ⇒ **不留空洞**（净可观察：最多 5 行，不会更多）')
r[8] = ''
r[9] = 'R5'
r[10] = ('🔴 **`B8_U` ' + D + ' 结论：本行不是 C（判据代理 J 建议的 `B→C` 不成立）** —— '
         '「原版有条数上限、我们这边无上限」这句是**过期的**：`CardWinBox.EffSlots = 5` 自 `A985⑨` 起就在，`SetEffectRows` 里 `Mathf.Min` 截断 + 出声；'
         '上限的真值已由 13 场序列化**实测 = 5**（此前的「需读 prefab」那条**已核销**）。'
         '观测量 = 一张挂 6 条限时增益的单位开窗后 `CardDisplayWindow.EffectRowCount`（原版 5 / 我们 5；'
         '灭自证：把 `Mathf.Min` 去掉 ⇒ 第 6 条去写 `_fxWho[5]` 越界，`BattleScene.cs:9676` 那条断言会红）')
emit(r)

# --- 8. 其余 8 条 C：只补「可观察量 + 阻塞归属」（状态不变，判据未重读的不冒充复核） ---
def tag(r, blocker, obs, reread=None):
    head = (' ｜【`B8_U` ' + D + '】' +
            (('本代理已逐句复读方法体：' + reread + '；') if reread else
             '判据出处已由 `B8_J` 补到行号（本代理**未重读**方法体，见 `判据/B8_特效动画与战斗界面_判据表.tsv`）；'))
    r[10] = r[10] + head + '仍 **C**，阻塞 = ' + blocker + '。观测量 = ' + obs


r = find(hud, 'BattleCardManager',
         'CreateCard(RawCardScript rawCardData, Transform parent, bool isPlayerY, CardArmy mainArmy, bool armedTrap)')[:]
tag(r, '`DeckRules.PlayerDeck` 没有「这一份是异画副本」那一格（数据/引擎线）',
    '带异画的卡组进战斗后，那张卡视图的 `ArtLayerTexture`（原版 = 该卡的异画图 / 我们今日 = 基础立绘）')
emit(r)

r = find(hud, 'BattleCardUI', 'BattleCardSetup(CardArmy mainArmy, bool isPlayer)')[:]
tag(r, '`CardArt` 没有 `DeckCardbackSdf`（E7）+ `DeckRuntime` 要带 `cardbackId`（数据侧）',
    '牌堆底下那层 SDF 掩码用的贴图（原版 = 卡组卡背派生 / 我们今日 = 阵营默认背派生）')
emit(r)

r = find(hud, 'BattleCardUI', 'ChooseDissolveMaterial()')[:]
tag(r, '`Core/CardView.cs:6818 / :6961`（E7 的文件）',
    '传说卡溶解材质名（原版 = `Card 3d WH40K Dissolve Legendary` / 我们今日 = 普通那份；两份资产都在工程里）')
emit(r)

r = find(hud, 'BattleCardUI', 'ShowOriginalRemnantCard(bool show)')[:]
tag(r, '`Core/CardView.cs:2496-2522 / :2632`（E7）',
    '残骸态 3D 卡身 `body3D.activeSelf`（原版 = `show && isRemnant && cardArmy ∈ {SaimHann, Sautekh}` / 我们恒 false）')
emit(r)

r = find(hud, 'BattleHud', 'BroadcastChangedLanguage()')[:]
tag(r, '`Core/CardView.cs` 的「卡面重排字」那一半（E7）；日志半边已接',
    '在设置窗切语言后，卡面那颗 TMP 的 `ShownTitle` / 描述文字（原版 = 整棵 HUD 重排 / 我们今日 = 日志重拼了、卡面字不变）')
emit(r)

r = find(hud, 'CardDisplayWindow', 'ShowBattleCard(CardScript card, int warlordHealthModifier)')[:]
r[4] = (r[4] + ' ⑤ `ChangeSize(Small, instant)` 在**产品路径上是空转**：`CardImageController.ChangeState` 见 `currentState == Small(0)` 就早退'
        '（图本来就是 Small ⇒ 画面上用的是材质默认 0.82；`CardView.cs:1006-1008` 那段判据 + `CardImageController__ChangeState.c`）'
        '⇒ 我们照「默认即 Small」处理，不需要单独的档位件')
r[5] = 'A'
r[6] = ('CardPresentation/Battle/CardDisplayWindow.cs:1870-1894（`ShowBattleCard` 全文：①:1887 · ②:1877 · ③:1875-1876 · ④:1879 · ⑥:1886 · ⑦:1891 · ⑧:1893）'
        '+ :1899（`MainCardShowsAmbushArt` 读口）')
r[7] = ('逐条对齐（原版 :657-708 = `DisplayCardEffects` → `ShowCard(..., showOptions)` → `ToggleStats(true)` 三句 + 伏击支 ①~⑧）：'
        '① `ToggleStats(false)` ⇒ `CardView.SetStatsHidden(true, hideArmourCell: true)`（原版 `HideStat` 就是**印 `?`**，2D 展示窗那支额外把护甲整格关掉）；'
        '② `SetEmptyRarityStars` ⇒ 克隆 `CardData` 把 `rarity` 置 null ⇒ 宝石落 common；③ `SetTexts("","",Minion,0)` ⇒ 清 `title`/`keywords`；'
        '④ 换伏击图 ⇒ `CardView.FaceDownArtTexture()` 写 `artOverride`；⑤ `ChangeSize(Small)` ⇒ **空转**（见判据要点）；'
        '⑥ `DisableAllCards` ⇒ 不传 `def`（只建主卡）；⑦ `loreObjectBG` 关；⑧ `voiceOverButton` 关。'
        '**差别** = 我们这一叠只建主卡（原版是 `cardUIs[]` 多实例），②的宝石是「未知档 ⇒ common」而不是「索引夹到 0」（同张贴图）')
r[8] = ''
r[10] = ('🔴 **铁律 5 订正链**：原判 A「一致」漏了整条伏击支（F6 补 ②③④⑥⑦⑧）→ 本代理 ' + D + ' 把 **① 藏数值**接上'
         '（`B8_N` 提供 `CardView.SetStatsHidden` 写口，本代理在 `ShowBattleCard` 里调 —— **必须在 `Show` 之后**，视图是 Show 里新建的）⇒ ①~⑧ 全落。'
         '观测量 = `MainCardShowsAmbushArt`（比 `ArtLayerTexture == FaceDownArtTexture()`，⛔ 不读自家 bool）+ `CardView.StatsHiddenFor(s)` 真 + `AmbushMarkText(s) == "?"` + `ShownTitle == ""`；'
         '**断言尚未写、宿主本批未跑**（`Editor/BattleScene.cs` 不在本代理白名单）⇒ 给调度台：加一条「伏击支四项」断言 + 灭自证（只换图不清字 / 只清数据不藏数值 两版都要红）')
emit(r)

r = find(hud, 'ChooseCardMenu', 'AutoProcessDone()')[:]
tag(r, '`BattleDriver.cs:1123` 的回放分支（E5）',
    '回放局遇到 `choose` 那一刻：面板是否弹出 + 面板开后 2 s 是否自动确认第一张（原版 = 弹、选第一张、`WaitForSeconds(2f)` 后 `ProcessChooseDone` / 我们 = 不弹面板、直接应用录像动作）')
emit(r)

r = find(hud, 'ChooseCardMenu',
         'Setup(List<RawCardScript> chooseCards, string actingCardId, int timerCounter, int target)')[:]
tag(r, '`BattleDriver.cs` 的回放分支（E5，与 HUD:363 同一批）；面板本体已在 `Battle/CardChoicePanel.cs` / `ChoosePanel.cs`',
    '标题键回落（`Battle/ChooseCard/Instructions-<id>` → `…/Instructions`）+ 确认钮初始不可点 + `timerCounter <= 1` 时 0.5 s 后自动确认（我们三件都已在 `CardChoicePanel.Open` / `TickOpenDelay` 里）',
    reread='原版 :559-649 全文（含 `isEnviromental = actingCardId ∈ {offensive, defensive}` ⇒ 只用来藏 `chooseMenuAnchor`；回放分支 :628-639；`timerCounter <= 1` 分支 :640-648）')
emit(r)

# ============================================================ 写盘
assert out, 'no rows'
assert hud_hdr == fx_hdr, 'header mismatch'
txt = '\t'.join(hud_hdr) + '\n' + '\n'.join(out) + '\n'
io.open(OUT, 'wb').write(txt.encode('utf-8'))
print('wrote', OUT)
print('rows:', len(out))
for l in out:
    f = l.split('\t')
    print('  ', f[0], '|', f[1], '|', f[2], '| 状态', f[5], '| 等级', f[8])
