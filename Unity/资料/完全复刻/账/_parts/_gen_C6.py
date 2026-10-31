#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成本次执行代理 C6 的账补丁 `账/_parts/C6.tsv`（只写补丁，⛔ 不动主表）。

做法：从主表按 (域, 原版类, 方法) 精确取出整行（11 列原样保留），只在指定列**追加**一段说明，
再写进补丁文件。合并由调度台用 `merge_ledger_parts.py --only C6` 做。
"""
import io
import os
import sys

ROOT = r"d:/4/Unity/资料/完全复刻/账"
HEADER = "域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注"


def read_rows(path):
    text = io.open(path, "rb").read().decode("utf-8")
    rows = text.split("\n")
    if rows and rows[-1] == "":
        rows.pop()
    return rows


def find(rows, dom, cls, method):
    hits = [(i, r) for i, r in enumerate(rows)
            if r.split("\t")[:3] == [dom, cls, method]]
    if len(hits) != 1:
        sys.exit("!! %s / %s / %s 命中 %d 行（要求恰好 1）" % (dom, cls, method, len(hits)))
    i, r = hits[0]
    cols = r.split("\t")
    if len(cols) != 11:
        sys.exit("!! 行 %d 列数 = %d（要 11）" % (i + 1, len(cols)))
    return cols


def main():
    out = []
    daily = read_rows(os.path.join(ROOT, "日常_奖励.tsv"))
    battle = read_rows(os.path.join(ROOT, "战斗逻辑驱动.tsv"))

    # ---- 1) 战斗逻辑驱动 · EnemyHeroDamageScorer.OnHealthChange（P1：替代物已接线，状态仍 D）
    c = find(battle, "战斗逻辑驱动", "EnemyHeroDamageScorer",
             "OnHealthChange(EnemyHealthChangeSignal evt)")
    c[7] += (" ｜🆕 2026-10-31（`B109_R20` §1·4 · 执行代理 `C6`）：**替代物已接线** —— "
             "`BattleDriver.UpdateHud` 的「敌方督军生命变了」那一支现在**先存旧值**再调 "
             "`ChallengeEngine.OnWarlordDamaged(false, prevFoeHp − foeHpNow, foeHpNow)` "
             "⇒ `ChallengeData.DamageEnemyWarlord` **不再恒 0**（改前全仓只有 Editor 三处调用）。"
             "原版 `OnScore` 通道仍断（本 build 0 监听）⇒ **状态维持 D**；口径是**借**评分器那一格"
             "（原版挑战那一格走服务端 `ChallengeVarResult`、本地无判据）。")
    out.append("\t".join(c))

    # ---- 2) 日常/奖励 · LogHeal（P2：表现层消费者已接）
    c = find(daily, "日常/奖励", "ChallengeLogMgr", "LogHeal(BattleAction battleAction)")
    c[10] += (" ｜🆕 2026-10-31（`B109_R20` §3 P2 · `C6`）：休眠那一句的前半已变 —— 结算拍 "
              "`BattleDriver` 调 `ChallengeEngine.FeedRunCounters(Ctx.Tally, _me)` 读 "
              "`AchievementTally.HealDoneOf(seat)` 写进 `ChallengeData.HealAmount` ⇒ 那格**不再恒 0**；"
              "仍**无 UI 读者**（`ChallengeEngine.ProgressOf` 全仓 0 调用者、挑战清单界面未建）⇒ 休眠如实标不改。")
    out.append("\t".join(c))

    # ---- 3) 日常/奖励 · TroopKilled（P2：同上）
    c = find(daily, "日常/奖励", "ChallengeLogMgr", "TroopKilled(BattleAction battleAction)")
    c[10] += (" ｜🆕 2026-10-31（`B109_R20` §3 P2 · `C6`）：休眠那一句的前半已变 —— 结算拍 "
              "`BattleDriver` 调 `ChallengeEngine.FeedRunCounters(Ctx.Tally, _me)` 读 "
              "`AchievementTally.TroopsKilledOf(seat)` 写进 `ChallengeData.TroopsKilled` ⇒ 那格**不再恒 0**；"
              "仍**无 UI 读者**（`ProgressOf` 全仓 0 调用者）⇒ 休眠如实标不改。")
    out.append("\t".join(c))

    # ---- 4) 日常/奖励 · LogAddEffect（P2：**没接**，如实记）
    c = find(daily, "日常/奖励", "ChallengeLogMgr", "LogAddEffect(CardEffect battleActionCardEffect)")
    c[10] += (" ｜🆕 2026-10-31（`B109_R20` §3 P2 · `C6`）：`ChallengeData.EffectsAdded` **仍未接** —— "
              "`AchievementTally` 里没有「施加效果条数」这一格，`ChallengeEngine.FeedRunCounters` 只喂 "
              "`TroopsKilled` / `HealAmount`（⛔ 不拿别的量顶替，那会静默记错账）；要接得先在 "
              "`RuleEngine/Core/EffectResolver` 的分派口加一格 tally（本批白名单外）⇒ 判据与后续落点见 "
              "`资料/完全复刻/报告/C6.md` §P2。")
    out.append("\t".join(c))

    dst = os.path.join(ROOT, "_parts", "C6.tsv")
    body = HEADER + "\n" + "\n".join(out) + "\n"
    io.open(dst, "wb").write(body.encode("utf-8"))
    print("written:", dst, len(out), "rows")


if __name__ == "__main__":
    main()
