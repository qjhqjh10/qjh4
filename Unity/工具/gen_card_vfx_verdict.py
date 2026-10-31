#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_card_vfx_verdict.py —— 生成 `资料/完全复刻/判据/全卡核验_判定.tsv`（一行 = 一张卡 × 四维）。

判据/规格：`资料/完全复刻/判据/B9_全卡端到端核验_规格.md` §三 S2 / §四·2。
本脚本 = 把 **B107_W25** 那次「会话临时目录里的 join」重建并落盘（W25 报告 §七 明说脚本没进仓库）。

⛔ 无参 = 只打用法并退出，**不写盘**；显式 `--write` 才写。

用法
----
    python -I -X utf8 d:/4/Unity/工具/gen_card_vfx_verdict.py                # 打用法 + 只读统计
    python -I -X utf8 d:/4/Unity/工具/gen_card_vfx_verdict.py --dry          # 只读：判一遍、打印分布
    python -I -X utf8 d:/4/Unity/工具/gen_card_vfx_verdict.py --write        # 真写判据/全卡核验_判定.tsv
    python -I -X utf8 d:/4/Unity/工具/gen_card_vfx_verdict.py --write --out <path> --rules legacy

输入（全部现读，⛔ 不缓存）
--------------------------
| 环节 | 文件 |
|---|---|
| 骨架（1,128 张池内卡） | `判据/全卡核验总表.tsv` 的 `卡id` 列 |
| ① 引擎（动态） | `Unity/_tmp_view/w41s3b/after/pool_diff4.tsv`（S3 第二批最终表）+ `all_dump.tsv` |
| ② 特效（静态 S2） | `数据/游戏数据/card_vfx_by_card.json` 的 `byCard` + 工程内 `.prefab` 名索引 + `VfxMap.cs` 的反向闸 |
| ③ 动画（静态 S2） | `MyGame/Assets/RuleEngine/Resources/cards_engine.json` 的 `animTimes` + 同 ① 的 S3 表 |
| ④ 语音（静态 S2） | `MyGame/Assets/CardPresentation/Resources/voice_lines.json` + `card_vfx_by_card.stats.authoritative.authoritativeUnresolvedRows` |

判定四档：`对` / `错` / `未验到` / `N/A`（N/A = 原版该维本来就没有声明）。

⚠️ 两条口径（照 W25 §三 原样）
- ① 是**动态读数**（S3 实跑）；②③④ 是**静态通路**（资源在不在工程 / 事件桶有没有消费点 /
  触发闸可不可达）—— 「对」= 静态通路成立，**不是**「已实拍播放」。
- 「特效像不像」只有用户能判（用户 2026-10-26 口径）⇒ 本表不做并排比。

🔴 **规则版本（`--rules`）**
- `current`（默认）：**「没有发射口」这个名字表从源码现读** ——
  `CardPresentation/Core/VfxMap.cs` 的 `NoEmitTriggerNames`。
  2026-10-31 `B108_A1` 之后该表**已清空**（6 个 trigger 全补了发射口）⇒ 这条规则自然不再命中。
  **⛔ 不要再把它写死成 `{81,700,695,647,291,701}`** —— 那正是 W25 表的过期点（见报告 B109_E8）。
- `legacy`：**逐字复刻 B107_W25 的口径**（那几个 trigger 硬编码判「错」），只用于**回归自证**
  —— 用现数据跑 `--rules legacy` 应与 2026-10-31 14:39 那份旧表**逐格一致**（除数据确实变了的行）。

🔴 **一个已知的重建细节（照旧口径保留、⛔ 不是正确性主张）**
③ 里 `tg>0` 要拿「同一 trigger 的 S3 行」定生死；同一 (卡, trigger) 有多行时，
旧脚本是**字典后写覆盖**（取**最后一行**的判定），不是「全对才过」也不是「有对就过」。
实测证据：`BL32`(未验到,对)→对、`TL69`(未验到,未验到,对)→对、`SW34`(对,对,未验到)→未验到。
本脚本按这个语义实现（`--rules legacy` 才能与旧表 0 差异）；`current` 同样沿用，保持可比。
另外两处同类的「照旧」：①① 证据里的「通道」与「未验到族」列表按**码位排序**（`sorted()`），不是行序；
②③ 里 `tg=-1` 的 `tn` 在 `cards_engine.json` 是空串，旧表显示成 `actAbility`。
三处都只为「让新旧差异只剩真变化」，⛔ 都不是正确性主张（详见 `报告/B109_E8.md` §1·3）。
"""
import io
import json
import os
import re
import sys
import collections

ROOT = "d:/4/Unity"
P_S1 = ROOT + "/资料/完全复刻/判据/全卡核验总表.tsv"
P_OUT = ROOT + "/资料/完全复刻/判据/全卡核验_判定.tsv"
P_S3 = ROOT + "/_tmp_view/w41s3b/after/pool_diff4.tsv"
P_DUMP = ROOT + "/_tmp_view/w41s3b/after/all_dump.tsv"
P_VFX = ROOT + "/数据/游戏数据/card_vfx_by_card.json"
P_ENG = ROOT + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
P_VOI = ROOT + "/MyGame/Assets/CardPresentation/Resources/voice_lines.json"
P_VFXMAP = ROOT + "/MyGame/Assets/CardPresentation/Core/VfxMap.cs"
ASSETS = ROOT + "/MyGame/Assets"

# W25 当时的硬编码（`--rules legacy` 用；⛔ current 不用，见文件头）
LEGACY_NOEMIT_TG = {81, 700, 695, 647, 291, 701}

# 旧表里 3 条**人工备注**（W25 报告里写死的事实，逐字保留；⛔ 不是脚本能推出来的）
MANUAL_REMARK = {
    "BL47": "BL47 原版 summonAudio GUID 本 build 解不出（tactic，非单位）",
    "BL50": "引擎行与出牌读数矛盾（电池对 / 主档打不出）",
    "GOF2": "GOF2 死亡语音缺源",
}
AUDIO_FIELDS = ("summonAudio", "attackAudio", "deathAudio", "attackSound", "deathSound", "spawnAudio")
AUDIO_EVENT_ZH = {"deathAudio": "死亡", "attackAudio": "攻击", "summonAudio": "登场"}

HEADER = ["卡id", "卡名", "阵营", "卡种",
          "引擎判定", "引擎证据", "特效判定", "特效证据",
          "动画判定", "动画证据", "语音判定", "语音证据",
          "数据来源", "备注"]

USAGE = __doc__


def read_tsv(p):
    raw = io.open(p, "rb").read().decode("utf-8")
    lines = [l.rstrip("\r") for l in raw.split("\n") if l != ""]
    return [l.split("\t") for l in lines]


def parse_noemit_from_source(path):
    """从 `VfxMap.cs` 的 `NoEmitTriggerNames` 数组字面量里读名字（只读字符串字面量，跳过注释）。"""
    src = io.open(path, "rb").read().decode("utf-8")
    m = re.search(r"NoEmitTriggerNames\s*=\s*\{(.*?)\};", src, re.S)
    if not m:
        raise SystemExit("🔴 `%s` 里找不到 `NoEmitTriggerNames = { ... };` —— 判据源变了，先核 VfxMap.cs" % path)
    body = m.group(1)
    body = re.sub(r"/\*.*?\*/", "", body, flags=re.S)
    body = re.sub(r"//[^\n]*", "", body)
    return [s for s in re.findall(r'"([^"]*)"', body)]


class Data(object):
    def __init__(self):
        s1 = read_tsv(P_S1)
        self.s1_header, s1rows = s1[0], s1[1:]
        self.s1 = {r[0]: dict(zip(self.s1_header, r)) for r in s1rows}
        self.order = [r[0] for r in s1rows]

        eng = json.loads(io.open(P_ENG, "rb").read().decode("utf-8"))
        self.eng = {c["id"]: c for c in eng["cards"]}

        vfx = json.loads(io.open(P_VFX, "rb").read().decode("utf-8"))
        self.by_card = vfx["byCard"]
        self.unresolved = vfx["stats"]["authoritative"]["authoritativeUnresolvedRows"]

        voi = json.loads(io.open(P_VOI, "rb").read().decode("utf-8"))
        self.voice = {c["id"]: c for c in voi["cards"]}

        s3 = read_tsv(P_S3)
        self.s3_header = s3[0]
        self.s3 = collections.defaultdict(list)
        for r in s3[1:]:
            self.s3[r[0]].append(dict(zip(self.s3_header, r)))

        dp = read_tsv(P_DUMP)
        dh = dp[0]
        self.dump = {}
        for r in dp[1:]:
            d = dict(zip(dh, r))
            self.dump[d["id"]] = d

        self.prefabs = set()
        for dirpath, _dirs, files in os.walk(ASSETS):
            for f in files:
                if f.endswith(".prefab"):
                    self.prefabs.add(f[:-7])

    def buckets(self, cid):
        b = self.by_card.get(cid) or {}
        return {k: v for k, v in b.items() if k != "_assetRefs"}

    def has_active_ability(self, cid):
        return (self.eng.get(cid) or {}).get("hasActiveAbility", 0) == 1


# ---------------- ① 引擎效果 ----------------
def dim1(D, cid):
    rows = D.s3.get(cid) or []
    if not rows:
        kws = (D.eng.get(cid) or {}).get("keywords") or []
        kw = "/".join(kws) if kws else "无关键词"
        return "未验到", ("S3 无行（ability=0；关键词卡：%s）—— S3 引擎表只覆盖 ability/trigger 分支，关键词分支未纳入" % kw)

    n_ok = sum(1 for r in rows if r["判定"] == "对")
    n_bad = len(rows) - n_ok
    if n_bad:
        fams = []
        for r in rows:
            if r["判定"] == "对":
                continue
            f = "%s(%s)" % (r["族名"], r["trigger名"])
            if f not in fams:
                fams.append(f)
        fams = sorted(fams)     # 旧脚本按码位排序（见文件头「重建细节」）
        return "未验到", "S3 pool_diff4 %d行：对%d/未验到%d；未验到族=%s" % (len(rows), n_ok, n_bad, "、".join(fams))

    dv = D.dump.get(cid)
    played = dv.get("played") if dv else None
    if played != "1":
        return "未验到", ("S3 pool_diff4 %d行全对，但 play all played=%s（playRc=%s %s）⇒ 打出那一拍未成立"
                          % (len(rows), played,
                             dv.get("playRc") if dv else "?", dv.get("rcName") if dv else "?"))

    chans = []
    for r in rows:
        note = (r["备注"] or "").strip()
        if not note:
            continue
        c = note.split("通道=", 1)[1].strip() if "通道=" in note else note
        if c not in chans:
            chans.append(c)
    if not chans:
        chans = ["主档"]
    chans = sorted(chans)       # 同上：旧脚本按码位排序
    return "对", "S3 pool_diff4 %d行全对（通道=%s）；play all played=1" % (len(rows), "/".join(chans))


# ---------------- ② 特效 ----------------
def dim2(D, cid, noemit_names):
    bk = D.buckets(cid)
    names = list(bk)
    if not bk:
        shared = (D.s1[cid].get("共用链VFX(非逐卡)") or "无").replace("(✓)", "")
        return "N/A", "card_vfx_by_card 无逐卡 VFX 桶（只有共用链：%s）" % shared, ""

    total = sum(len(v) for v in bk.values())
    withp = [(k, e) for k in names for e in bk[k] if e.get("prefab")]
    hit = sum(1 for _k, e in withp if e["prefab"] in D.prefabs)

    # 反向闸：该桶挂在「引擎没有发射口」的 trigger 上（名字表现读 VfxMap.NoEmitTriggerNames）
    dead = []
    for k in names:
        for e in bk[k]:
            nm = e.get("abilityTriggerName")
            if k == "trigger_unmapped" or (nm and nm in noemit_names):
                tag = ("%s(%s)" % (e.get("abilityTrigger"), nm)) if nm else k
                if tag not in dead:
                    dead.append(tag)
    if dead:
        n_dead = sum(1 for k in names for e in bk[k]
                     if k == "trigger_unmapped" or (e.get("abilityTriggerName") in noemit_names))
        return "错", ("card_vfx_by_card %d条，其中 %d条挂在 %s —— VfxMap.NoEmitTriggerNames 无发射口"
                      "（Core/VfxMap.cs:3261）；原版有（CardScript.cs:7653/7836/7843/7850/14744 · "
                      "RawCardScript.cs:1962）⇒ 该件永不播（同卡声明 prefab %d/%d 在工程）"
                      % (total, n_dead, "、".join(dead), hit, len(withp))), "特效触发点缺失（原版有发射口）"

    remark = ""
    shown = list(names)
    ability_n = 0
    if "ability" in bk and not D.has_active_ability(cid):
        ability_n = len(bk["ability"])
        shown = [n for n in names if n != "ability"]
        if not shown:
            return ("N/A",
                    "card_vfx_by_card 只有 ability 桶 %d 条且 hasActiveAbility≠1（原版不消费，例外①）" % ability_n,
                    "ability 桶原版不消费（hasActiveAbility≠1）")
        remark = "ability 桶原版不消费（hasActiveAbility≠1）"

    if hit != len(withp):
        return "错", "card_vfx_by_card %d条（%s）；prefab %d/%d 命中工程" % (
            total, "/".join(shown), hit, len(withp)), remark

    ev = ("card_vfx_by_card %d条（%s）；该卡声明 prefab %d/%d 命中工程；"
          "事件桶消费点已接（B9_特效触发链.tsv）" % (total, "/".join(shown), hit, len(withp)))
    if ability_n:
        ev += "；另 ability 桶 %d 条 hasActiveAbility≠1（原版不消费，例外①）" % ability_n
    return "对", ev, remark


# ---------------- ③ 动画 ----------------
def _anim_desc(cnt):
    out = []
    for (t, n), c in sorted(cnt.items(), key=lambda kv: kv[0][0]):
        if not n:
            n = "actAbility" if t == -1 else ""      # 旧表口径：tg=-1 的 `tn` 为空 ⇒ 显示 `actAbility`
        out.append("tg%s(%s)×%d" % (t, n, c))
    return "、".join(out)


def dim3(D, cid, noemit_names, rules):
    at = (D.eng.get(cid) or {}).get("animTimes") or []
    if not at:
        return "N/A", "animTimes=0（原版 cardAnimations 无非空 GUID / 无动画）", ""

    cnt = collections.Counter()
    for e in at:
        cnt[(e.get("tg"), e.get("tn"))] += 1
    desc = _anim_desc(cnt)
    allp = [e.get("n") for e in at if e.get("n")]
    hit = sum(1 for p in allp if p in D.prefabs)

    if allp and hit != len(allp):
        return "错", "animTimes %d条[%s]；prefab %d/%d 在工程" % (len(at), desc, hit, len(allp)), "动画 prefab 不在工程"

    # 「无发射口」这一拍：名字表现读（current）／硬编码 6 个（legacy）
    dead = []
    for (t, n) in cnt:
        if rules == "legacy":
            if t in LEGACY_NOEMIT_TG:
                dead.append(t)
        elif n and n in noemit_names:
            dead.append(t)
    if dead:
        n_dead = sum(c for (t, _n), c in cnt.items() if t in dead)
        tags = []
        for t in dead:
            nm = [n for (tt, n), _c in cnt.items() if tt == t and n]
            tags.append("%s(%s)" % (t, nm[0]) if nm else str(t))
        return "错", ("animTimes %d条[%s]，其中 %d 条挂在 %s —— 无发射口"
                      "（VfxMap.cs:3261）；原版有（同特效证据）⇒ 该动画永不播（prefab %d/%d 在工程）"
                      % (len(at), desc, n_dead, "、".join(tags), hit, len(allp))), "动画触发点缺失（原版有发射口）"

    # tg>0 ⇒ 同一 trigger 的 S3 行（同 trigger 多行 = 后写覆盖，见文件头）
    last = {}
    for r in D.s3.get(cid, []):
        last[r["trigger"]] = r["判定"]
    miss = []
    for (t, _n) in cnt:
        if t is None or t <= 0:
            continue
        if last.get(str(t)) != "对":
            miss.append(t)
    miss = sorted(set(miss))

    suffix = ""
    if any(t == -1 for (t, _n) in cnt) and not D.has_active_ability(cid):
        n_act = sum(c for (t, _n), c in cnt.items() if t == -1)
        suffix = "；另 tg-1(actAbility)×%d 条 hasActiveAbility≠1 ⇒ 原版不消费（例外①）" % n_act

    if miss:
        return "未验到", "animTimes %d条[%s]；trigger %s 对应的 S3 引擎行为「未验到」%s" % (
            len(at), desc, "/".join(str(t) for t in miss), suffix), ""

    if any(t == 0 for (t, _n) in cnt):
        dv = D.dump.get(cid)
        if dv is None or dv.get("played") != "1":
            return "未验到", "animTimes %d条[%s]；tg0(ThisCardPlayed) 对应 play all played=%s（playRc=%s %s）" % (
                len(at), desc, dv.get("played") if dv else "?",
                dv.get("playRc") if dv else "?", dv.get("rcName") if dv else "?"), ""

    if not allp:
        return "对", ("animTimes %d条[%s]；prefab 全是纯计时占位（n 为空，无可播 prefab）；"
                      "tg0 走 play 通路（played=1）· tg>0 对应 S3 行全对 · tg-1 走 IsActiveAbilityEntry" % (len(at), desc)), ""

    if all(t == -1 for (t, _n) in cnt) and not D.has_active_ability(cid):
        return ("N/A",
                ("animTimes %d条[%s]，全部 tg-1（activeAbility）且 hasActiveAbility 未记（=0）≠1 "
                 "⇒ 原版自己也不消费（CardScript__ResolveActiveAbilityPlayed.c:22；gen_cards_engine.py:1810-1811）"
                 "⇒ 铁律 11 例外① 原版不可达（prefab %d/%d 在工程）" % (len(at), desc, hit, len(allp))),
                "activeAbility 动画原版不消费（hasActiveAbility≠1）")

    return "对", ("animTimes %d条[%s]；prefab %d/%d 在工程；tg0 走 play 通路（played=1）· "
                  "tg>0 对应 S3 行全对 · tg-1 走 IsActiveAbilityEntry" % (len(at), desc, hit, len(allp))), ""


# ---------------- ④ 语音 ----------------
def dim4(D, cid):
    v = D.voice.get(cid)
    unres = [r for r in D.unresolved if r["cardId"] == cid and r["soField"] in AUDIO_FIELDS]
    if not v or not v.get("lines"):
        return "N/A", "voice_lines 无该卡条目（原版该卡无语音）", ""

    lines = v["lines"]
    srcs = collections.Counter(l.get("src") for l in lines)
    if unres:
        f = unres[0]["soField"]
        evs = "、".join(dict.fromkeys(l.get("ev") for l in lines))
        who = "（%s，cardSO）" % evs if "cardSO" in srcs else ""
        zh = AUDIO_EVENT_ZH.get(f, f)
        return "未验到", ("voice_lines %d条%s；但原版 %s GUID 在本 build 解不出（card_vfx 表 unresolved）"
                          "⇒ %s语音无来源" % (len(lines), who, f, zh)), ""

    if "cardSO" in srcs:
        return "对", ("voice_lines %d条（cardSO 归属 %d 条）；音频全在 Resources/Art/audio/vo（1897/1897）；"
                      "12 事件族消费点已接（VoiceLines.cs）" % (len(lines), srcs["cardSO"])), ""
    return "未验到", "voice_lines %d条但无 cardSO 归属（仅 %s）" % (
        len(lines), "/".join(sorted(k for k in srcs if k)) or "none/orphan"), ""


def source_chain(D, cid):
    parts = ["S1全卡核验总表（1,128 卡骨架）"]
    if D.s3.get(cid):
        parts.append("S3:pool_diff4(w41s3b)")
    parts.append("cards_engine")
    if D.buckets(cid):
        parts.append("card_vfx_by_card")
    if (D.eng.get(cid) or {}).get("animTimes"):
        parts.append("cards_engine.animTimes")
    if (D.voice.get(cid) or {}).get("lines"):
        parts.append("voice_lines")
    return "/".join(parts)


def build(D, rules):
    noemit = parse_noemit_from_source(P_VFXMAP)
    rows = []
    for cid in D.order:
        s1 = D.s1[cid]
        d1, e1 = dim1(D, cid)
        d2, e2, r2 = dim2(D, cid, noemit)
        d3, e3, r3 = dim3(D, cid, noemit, rules)
        d4, e4, r4 = dim4(D, cid)
        remark = "；".join(x for x in (r2, r3, r4) if x)
        if cid in MANUAL_REMARK:
            remark = MANUAL_REMARK[cid]
        rows.append([cid, s1["卡名"], s1["阵营"], s1["卡种"],
                     d1, e1, d2, e2, d3, e3, d4, e4,
                     source_chain(D, cid), remark])
    return rows, noemit


def main(argv):
    if "--help" in argv or "-h" in argv:
        sys.stdout.write(USAGE)
        return 0
    write = "--write" in argv
    rules = "current"
    if "--rules" in argv:
        rules = argv[argv.index("--rules") + 1]
        if rules not in ("current", "legacy"):
            raise SystemExit("🔴 --rules 只认 current / legacy")
    out = P_OUT
    if "--out" in argv:
        out = argv[argv.index("--out") + 1]
    if not write:
        sys.stdout.write(USAGE)
        sys.stdout.write("\n[只读模式] 没带 `--write` ⇒ 不写盘。下面只打统计。\n\n")

    D = Data()
    rows, noemit = build(D, rules)
    print("骨架 = %d 张（判据/全卡核验总表.tsv）" % len(rows))
    print("VfxMap.NoEmitTriggerNames 现读 = %d 个%s" % (
        len(noemit), ("：" + ", ".join(noemit)) if noemit else "（已清空）"))
    print("规则版本 = %s" % rules)
    print("prefab 名索引 = %d（Assets/**/*.prefab）" % len(D.prefabs))
    for name, idx in (("① 引擎", 4), ("② 特效", 6), ("③ 动画", 8), ("④ 语音", 10)):
        c = collections.Counter(r[idx] for r in rows)
        print("  %s：%s" % (name, " · ".join("%s %d" % (k, c[k]) for k in ("对", "错", "未验到", "N/A") if c[k])))

    if not write:
        return 0

    body = "\t".join(HEADER) + "\n" + "".join("\t".join(r) + "\n" for r in rows)
    io.open(out, "wb").write(body.encode("utf-8"))
    print("\n✅ 已写 → %s（%d 行数据 + 1 行表头；UTF-8 · LF · 无 BOM）" % (out, len(rows)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
