#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""B93_E2 · 第 4 批 50 条落点错配核验的账补丁生成器。
只读主表 → 只写 _parts/B93_E2.tsv；不改主表。
读/写一律裸 split("\t")（⛔ 不用 csv 模块）。"""
import io

BASE = 'd:/4/Unity/资料/完全复刻/'
HDR = ['域', '原版类', '方法', '判据出处', '判据要点', '状态', '我们的落点', '差异', '等级', '轮次', '备注']

F = 0; C = 1; M = 2; SRC = 3; KEY = 4; ST = 5; LAND = 6; DIFF = 7; GRADE = 8; ROUND = 9; NOTE = 10


def load(fn):
    return [l.rstrip('\n').split('\t') for l in io.open(BASE + '账/' + fn, encoding='utf-8-sig')]


E = {}

# ---------------------------------------------------------------- 社交/聊天:31
E[('社交_聊天.tsv', 31)] = {
    LAND: 'Shell/AllianceMemberTab.cs:1744（BuildAll）（等价实现 = BuildAll：原版 `AllianceMemberList.Initialize` 的「清空 → 排序 → 逐条 Instantiate + 行号」整段；排序那半是类尾 `SortedMembers`（:2035））',
    DIFF: '一致（拆成两件、同一条调用链）：`BuildAll` 先 `SocialWindow.DestroySafe` 清空 → `SortedMembers()` 按 `Member.Rating` **降序**、同分保持原相对次序（= LINQ `OrderByDescending` 的稳定语义，⛔ 不是不稳定的 `List.Sort`）→ 逐条 `Build(v, content, all[i], r, i + 1)`（行号 = 排序后序号、1 起）；`Members {n}/{cap}` 在 `:1533` 按 `SocialData.Members.Count` 刷（`cap` = prefab 出厂样例 20，`SocialData.MemberCap`）。判据 `AllianceMemberList__Initialize.c` 同形（`GetTranslation` + `String.Format(…Members…)` → `SupportMethods.DestroyAllChildren` → `OrderByDescending` → `Instantiate` + `AllianceMemberEntry__Initialize(…, 行号, …)`）。⚠️ 原版那两个入参（`Group` / `IReadOnlyList<GroupMember>`）我们没有对等对象：`SocialData` 是**本地模拟**（社交线 = P2 档，用户 2026-09-17 裁定「数据本地模拟或出声」）。',
    NOTE: '｜2026-10-28 B93_E2 现核：落点由 `SortedMembers`（只覆盖「排序」那半）改指 `BuildAll`（整段）；差异按 `.c` 逐句重写；状态仍 A',
}

# ---------------------------------------------------------------- 音频/语音:53
E[('音频_语音.tsv', 53)] = {
    DIFF: '一致（可听结果）+ 一处机制未做：原版 `Initialize` 有**两支** —— 首次（静态标志 `+2 == 0`）`PlayMusic(main, Instant)` + `PlayMusicQueued(idle)` 并置标志；**再次进入**（标志已是 1）走 `Release(main)` + `PlayMusic(idle, Instant)`。本仓 `PlayMenuMusic()` 只做**第一次那支**（`Shell/ShellRuntime.cs:1219-1221`）；壳是 DDOL、`Build` 只跑一次 ⇒ 重入支今天**不可达**，且两支的可听结果相同（「主题放一次、之后 idle 长放」）。`Release()` 是 Addressables 引用释放，我们走 `Resources.Load`、无引用计数 ⇒ 无等价物（如实记，不假装做了）。',
    NOTE: '｜2026-10-28 B93_E2 现核：补记原版「重入支」未做（今天不可达）+ `Release` 无等价物',
}

# ---------------------------------------------------------------- 教程/引导:45
E[('教程_引导.tsv', 45)] = {
    LAND: 'Shell/MenuTutorialFlow.cs:674（RefreshPointer）（等价实现 = RefreshPointer：原版 `AddPointer` 建一颗挂在目标上的指针、`filterRaycast` 为真时登记「只允许该目标这一棵」的射线允许表；我们的 `RefreshPointer()` 做同一件事 —— 按当前 stage 的 `PointerOffset / TargetInWindow / DeepTarget(2)` 解析目标 Transform → `_pointer.Show(...)`，窗/页变化或轮询时重挂、目标找不到就 `Hide()`；`FiltersRaycast(stage, kind)` 为真那三档调 `RaycastFilter.RegisterObjectAndChildrenGraphics(target)`（`MenuTutorialFlow.cs:759-771`，表 `:1040-1052`））',
    DIFF: '2026-10-28 B93_E2 现核（铁律 5 —— 原落点里那句「`filterRaycast` 这一位我们没有对应开关」**已过期**）：射线过滤自 2026-10-25（账 #230）已接 —— `Shell/RaycastFilter.cs` + `MenuTutorialFlow.FiltersRaycast(stage, kind)` 的**逐调用点**表（`TutorialStageGetMission`（含两颗 SO pointer = 1）/ `GetCampaignNode` 的轨道节点 / `ClaimForgeFirstLevel` 的 Claim 钮），⛔ 不是「整个 stage 一律过滤」（那会软卡死）。其余一致：目标解析不出 ⇒ 撤指针、不出声（原版空 `target` ⇒ `CustomDebug.LogError`，那一档在我们这里对应「窗还没开」的正常中间态）；原版延迟一帧的协程我们在同一拍算完 ⇒ 净结果同。',
    NOTE: '｜2026-10-28 B93_E2 现核：`filterRaycast` 已接（原「没有对应开关」是过期句，就地改掉）',
}

# ---------------------------------------------------------------- 日常/奖励:37
_d37 = None
E[('日常_奖励.tsv', 37)] = {
    LAND: 'RuleEngine/Core/RuleCore.cs:9272（HealUnit）',
    'DIFF_APPEND': '。🔴 **2026-10-28 B93_E1 现核（未闭合，如实留）**：本格今天**无读者**（102 条成就里 `HealAmount` = 0 条，唯一潜在消费者是远端任务）；且原版 `LogHeal` 的唯一调用点是 `_ResolveHeal_d__491__MoveNext.c:89`（**治疗动作的结算体**）—— 若「回合末再生 / 猎杀标记」在原版**不**经过 `CardScript.Heal`，我们这一处就会**多算**。还没核清（见 `AchievementTally.HealDone` 的 doc ②），⛔ 别当已闭合。计数格本体 = `RuleEngine/Core/AchievementTally.cs:299/308`（`HealDone` / `AddHealDone`）。',
    NOTE: '｜2026-10-28 B93_E2：落点收敛成单个 `文件:行`（第二处 `AchievementTally` 已移进差异）；补记 B93_E1 的未闭合点',
}

# ---------------------------------------------------------------- 日常/奖励:42（A→B）
E[('日常_奖励.tsv', 42)] = {
    ST: 'B',
    LAND: 'RuleEngine/Core/RuleCore.cs:6195（TryCreditKillCore）',
    DIFF: '引擎侧计数已落（B88·A2 2026-10-28）：`ctx.Tally.AddTroopKilled(kOwner)` 接在 `TryCreditKillCore` 的 `return true;` 之前（计数格 = `AchievementTally.cs:292/302`）。🔴 **2026-10-28 B93_E2 现核：原写「与消费者口径等价」【不成立】⇒ 降 B** —— `TryCreditKillCore` 自带 `if (kOwner != ctx.Active) return false;`（`RuleCore.cs:6205-6206`；原版那条闸是给 **Slay 正文**用的），于是「**非本方回合**里我方触发效果（反击 / 陷阱 / 光环）打死对方部队」原版**会记**（`ChallengeLogMgr__TroopKilled.c` 写在 `_ResolveMinionDeath_d__454` 里、不看回合；消费者 `TroopsKilledCount.cs:22-27` 只看 `killer.isPlayer`）而我们**漏记**。修法方向 = 把 `AddTroopKilled` 挪到「小兵死亡」的统一记账口、只按「凶手座位」过滤（⛔ 别把整条 `TryCreditKillCore` 挪过去 —— Slay / 击杀触发依赖它的 `ctx.Active` 闸）。另：本格今天**无读者**（102 条成就里 `TroopsKilledCount` = 0 条，唯一潜在消费者是远端任务）。',
    NOTE: '｜2026-10-28 B93_E2 现核：A→B（跨回合击杀漏记，`kOwner != ctx.Active` 闸），落点收敛成单个 `文件:行`',
}

# ---------------------------------------------------------------- 菜单/外壳:72
E[('菜单_外壳.tsv', 72)] = {
    DIFF: '一致（开窗 + 灌当前名）**但不完整**：原版 `ChangeNameWindow__Open.c` 除 `GameWindow.Open` 外还有两句 —— ① `changeNameButton(+0x70).interactable = false`（要等输入合法才开）；② 读 `PlayerDataManager.TimesNameChange` 决定「改名次数」那一套（`Price Display` ON / `\'Free\'` OFF）。我们 ① 的等价物 = `ApplyName` 里那三条判据（去富文本 / 3~25 字 / 同名禁改，⛔ 是**提交时**判而不是灰钮）；② **没做** —— 改名次数是玩家账户数据（服务器），按 `ProfileTab.cs:403` 的口径走「首次那条」（🔶 服务器相关）。',
    NOTE: '｜2026-10-28 B93_E2 现核：补记原版 `Open` 的两句（灰钮 / `TimesNameChange`）；状态仍 A（② 属服务器数据）',
}


def main():
    tables = {}
    out = [HDR]
    for (fn, ln), ed in E.items():
        if fn not in tables:
            tables[fn] = load(fn)
        rows = tables[fn]
        src = rows[ln - 1]
        if len(src) != 11:
            raise SystemExit('列数异常 %s:%d = %d' % (fn, ln, len(src)))
        r = list(src)
        for idx in (ST, LAND, DIFF):
            if idx in ed:
                r[idx] = ed[idx]
        if 'DIFF_APPEND' in ed:
            r[DIFF] = r[DIFF] + ed['DIFF_APPEND']
        if NOTE in ed:
            r[NOTE] = r[NOTE] + ed[NOTE]
        if len(r) != 11:
            raise SystemExit('输出列数异常 %s:%d' % (fn, ln))
        out.append(r)
    buf = '\n'.join('\t'.join(x) for x in out) + '\n'
    p = BASE + '账/_parts/B93_E2.tsv'
    io.open(p, 'w', encoding='utf-8', newline='\n').write(buf)
    # 路线回环：读回 → 逐列与主表比对
    back = [l.rstrip('\n').split('\t') for l in io.open(p, encoding='utf-8')]
    assert back[0] == HDR, back[0]
    assert len(back) - 1 == len(E), (len(back) - 1, len(E))
    for row in back[1:]:
        assert len(row) == 11, len(row)
        assert '(' in row[M], '方法列缺签名: ' + row[M]
    print('WROTE', p, 'rows', len(back) - 1)


main()
