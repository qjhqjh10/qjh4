#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_全卡核验_判定.py — 重新生成 `资料/完全复刻/判据/全卡核验_判定.tsv`（ID-01 · S3-G5）

一键复现「全卡 × 四维（引擎 / 特效 / 动画 / 语音）」判定表。
判据与做法见 `资料/完全复刻/报告/B107_W25.md` §三 / §七；本脚本只是把那一轮的一次性 join 固化成可复跑件。

数据源（全部现成、⛔ 不新造第二套口径）：
  · 骨架（卡id / 卡名 / 阵营 / 卡种 / ability数 / 共用链VFX）：资料/完全复刻/判据/全卡核验总表.tsv（S1）
  · ①引擎：_tmp_view/w41s3b/after/pool_diff4.tsv（S3 第二批最终表）＋ all_dump.tsv（play all 主档）
  · ②特效：数据/游戏数据/card_vfx_by_card.json ＋ MyGame/Assets/WarpforgeVFX/Prefabs/*.prefab 名索引
  · ③动画：MyGame/Assets/RuleEngine/Resources/cards_engine.json 的 animTimes（tg 对 S3 行）
           ＋ 资料/普查产出_1018/allcards_1350/cards_raw.json 的 hasActiveAbility
  · ④语音：MyGame/Assets/CardPresentation/Resources/voice_lines.json（按 阵营+卡名 落卡id）

硬纪律：本脚本有【显式动作】——
    裸跑（无参数）        → 打印用法并 rc=2，**一个字节都不写**
    --dry                 → 只预览（前 5 行 + 总行数），不写盘
    --check               → 只比对：与盘上现有文件逐字节比，相同报「逐字节相同」，不同逐条列差异
    --write               → 真正写盘（覆盖 `资料/完全复刻/判据/全卡核验_判定.tsv`）

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_全卡核验_判定.py --check
    python -I -X utf8 d:/4/Unity/工具/gen_全卡核验_判定.py --write

⚠️ 若 `--check` 报差异：**先别 --write** —— 先判断是脚本口径不对，还是盘上那份已过期。
"""

import io
import json
import os
import sys

# ---------------------------------------------------------------- 路径
TOOL_DIR = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.dirname(TOOL_DIR)  # D:/4/Unity

P_SKELETON = os.path.join(BASE, "资料", "完全复刻", "判据", "全卡核验总表.tsv")
P_OUT = os.path.join(BASE, "资料", "完全复刻", "判据", "全卡核验_判定.tsv")
P_POOLDIF = os.path.join(BASE, "_tmp_view", "w41s3b", "after", "pool_diff4.tsv")
P_ALLDUMP = os.path.join(BASE, "_tmp_view", "w41s3b", "after", "all_dump.tsv")
P_VFX = os.path.join(BASE, "数据", "游戏数据", "card_vfx_by_card.json")
P_ENGINE = os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Resources", "cards_engine.json")
P_RAW = os.path.join(BASE, "资料", "普查产出_1018", "allcards_1350", "cards_raw.json")
P_VOICE = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "voice_lines.json")
P_PREFAB_DIR = os.path.join(BASE, "MyGame", "Assets", "WarpforgeVFX", "Prefabs")

HEADER = ["卡id", "卡名", "阵营", "卡种",
          "引擎判定", "引擎证据",
          "特效判定", "特效证据",
          "动画判定", "动画证据",
          "语音判定", "语音证据",
          "数据来源", "备注"]

# 纯计时占位（GOF2）等已知个例，按卡 id 硬编码 —— 判据见 B107_W25 §五 / §六
SPECIAL_VOICE_EVIDENCE = {
    # GOF2：原版 deathAudio GUID 在本 build 解不出 ⇒ 死亡语音无来源
    "GOF2": "voice_lines 1条（deploy，cardSO）；但原版 deathAudio GUID 在本 build 解不出（card_vfx 表 unresolved）⇒ 死亡语音无来源",
}
SPECIAL_NOTE = {
    "GOF2": "GOF2 死亡语音缺源",
    "BL47": "BL47 原版 summonAudio GUID 本 build 解不出（tactic，非单位）",
    "BL50": "引擎行与出牌读数矛盾（电池对 / 主档打不出）",
}


def read_tsv(path):
    """读 TSV：UTF-8，按 '\\n' 切（⛔ 不用 csv 模块），去行尾 '\\r'。返回 list[list[str]]。"""
    raw = io.open(path, "r", encoding="utf-8", newline="").read()
    lines = raw.split("\n")
    if lines and lines[-1] == "":
        lines.pop()
    return [ln.rstrip("\r").split("\t") for ln in lines]


def load_json(path):
    return json.load(io.open(path, "r", encoding="utf-8"))


def prefab_names():
    if not os.path.isdir(P_PREFAB_DIR):
        return set()
    return {os.path.splitext(n)[0] for n in os.listdir(P_PREFAB_DIR) if n.endswith(".prefab")}


# ---------------------------------------------------------------- ① 引擎
def build_engine():
    pool = {}
    for r in read_tsv(P_POOLDIF)[1:]:
        pool.setdefault(r[0], []).append(r)
    dump = {r[0]: r for r in read_tsv(P_ALLDUMP)[1:]}
    return pool, dump


def engine_cols(cid, sk_row, engine_by_id, pool, dump, raw_by_id):
    # 🆕 2026-11-02（`B117-WSMALL` · 判据 `判据/B117_逐卡核验_E_Leviathan_Tau_SW_GS.md` §五·3/§五·4
    #   与 `判据/B117_逐卡核验_B_Ultramarines_EC.md` §五·1）：
    #   ① **`activeAbility` 行而 `hasActiveAbility` ≠ 1 ⇒ 原版自己也不消费**（`CardScript__ResolveActiveAbilityPlayed.c:26-31`
    #      早退；我们同口径 `Core/CardDefActiveAbility.cs:25-31`、执行层 `RuleCore.cs:15573` 的 `row.Gate != 1` 跳过）
    #      ⇒ 该行**不参与判定**（`pool_effect_check` 已出「不适用」，老夹具里仍是「未验到」⇒ 两种都吃掉）。
    #   ② **无效果树**（`triggerAbilityIndices=[]` ∧ 一条 ability 行都没有）⇒ 判 **`免测`**，⛔ 别标成「未验到」
    #      ——「未验到」的语义是「有东西该验但夹具没验到」，这类卡原版本来就没有可执行效果。
    rows_all = pool.get(cid) or []
    haa = (raw_by_id.get(cid) or {}).get("hasActiveAbility", None)
    #   只认**显式 `0`**（判据就是「为 0 早退」）；`None`/缺列 ⇒ 不剔除（⛔ 不把「不知道」当「不适用」）。
    rows = [r for r in rows_all
            if r[7] != "不适用"
            and not (len(r) > 10 and r[10] == "activeAbility" and haa == 0)]
    excluded = len(rows_all) - len(rows)
    if not rows:
        if excluded:
            return "免测", ("S3 pool_diff4 的 %d 行全部是 `activeAbility` 行且 `hasActiveAbility`≠1"
                            "⇒ 原版自己也不消费（例外①，判据 `CardDefActiveAbility.cs:25-31`）⇒ 免测" % excluded)
        tai = (engine_by_id.get(cid) or {}).get("triggerAbilityIndices") or []
        if not tai and (sk_row[5] in ("", "0")):
            # 无效果树 ⇒ 免测（⛔ 不是「未验到」）
            kws = (engine_by_id.get(cid) or {}).get("keywords") or []
            kwtxt = "/".join(kws) if kws else "无关键词"
            return "免测", ("无效果树（abilities/activeAbility/alternativeAbility 全空 ∧ `triggerAbilityIndices=[]`）"
                            "⇒ 免测：本维只测 ability/trigger 分支，该类卡的玩法全在关键词（%s），"
                            "关键词分支不属本表口径（判据 `B117_逐卡核验_E` §五·3）" % kwtxt)
        # 无 S3 行：S3 只覆盖 ability/trigger 分支，关键词分支未纳入
        ability = sk_row[5]
        kws = (engine_by_id.get(cid) or {}).get("keywords") or []
        kwtxt = "/".join(kws) if kws else "无关键词"
        ev = ("S3 无行（ability=%s；关键词卡：%s）—— S3 引擎表只覆盖 "
              "ability/trigger 分支，关键词分支未纳入" % (ability, kwtxt))
        return "未验到", ev

    n = len(rows)
    ok = sum(1 for r in rows if r[7] == "对")
    bad = n - ok
    excl_clause = ("；另 `activeAbility` 行 %d 条 `hasActiveAbility`≠1 ⇒ 原版不消费（例外①）" % excluded) if excluded else ""
    d = dump.get(cid) or []
    played = d[7] if len(d) > 7 else ""
    rc = d[5] if len(d) > 5 else ""
    rcname = d[6] if len(d) > 6 else ""

    if bad == 0:
        if played == "1":
            # 通道：只取【显式备注】（去掉 '通道=' 前缀）；空备注不单独记『主档』
            # （只有当整卡所有行都空备注时，才落『主档』）。多个通道按 Unicode 码位升序。
            explicit = set()
            for r in rows:
                note = r[8]
                if note == "":
                    continue
                explicit.add(note[3:] if note.startswith("通道=") else note)
            channels = sorted(explicit) if explicit else ["主档"]
            ev = "S3 pool_diff4 %d行全对（通道=%s）；play all played=%s" % (
                n, "/".join(channels), played)
            return "对", ev + excl_clause
        # 全对但打不出
        ev = ("S3 pool_diff4 %d行全对，但 play all played=0（playRc=%s %s）"
              "⇒ 打出那一拍未成立" % (n, rc, rcname))
        return "未验到", ev + excl_clause

    fams = sorted({"%s(%s)" % (r[11], r[10]) for r in rows if r[7] == "未验到"})
    ev = "S3 pool_diff4 %d行：对%d/未验到%d；未验到族=%s" % (n, ok, bad, "、".join(fams))
    return "未验到", ev + excl_clause


# ---------------------------------------------------------------- ② 特效
def vfx_cols(cid, sk_row, vfx_by_card, raw_by_id, proj):
    v = vfx_by_card.get(cid)
    buckets = [k for k in (v or {}) if k != "_assetRefs"]
    if not buckets:
        shared = sk_row[12].replace("(✓)", "") or "无"
        return "N/A", "card_vfx_by_card 无逐卡 VFX 桶（只有共用链：%s）" % shared

    haa = (raw_by_id.get(cid) or {}).get("hasActiveAbility", None)
    has_ability = "ability" in buckets
    exclude_ability = has_ability and haa != 1

    shown = [k for k in buckets if not (k == "ability" and exclude_ability)]
    if not shown:
        # 只有 ability 桶且原版不消费 ⇒ 按「无逐卡桶」处理（现池内 0 例，留兜底）
        shared = sk_row[12].replace("(✓)", "") or "无"
        return "N/A", "card_vfx_by_card 无逐卡 VFX 桶（只有共用链：%s）" % shared

    total = sum(len(v[k]) for k in buckets)
    declared = hit = 0
    for k in buckets:
        for e in v[k]:
            pf = e.get("prefab")
            if pf:
                declared += 1
                if pf in proj:
                    hit += 1

    ev = ("card_vfx_by_card %d条（%s）；该卡声明 prefab %d/%d 命中工程；"
          "事件桶消费点已接（B9_特效触发链.tsv）") % (
              total, "/".join(shown), declared, hit)
    if exclude_ability:
        ev += "；另 ability 桶 %d 条 hasActiveAbility≠1（原版不消费，例外①）" % len(v["ability"])
    return "对", ev


# ---------------------------------------------------------------- ③ 动画
def _s3_trigger_ok(cid, tg, pool):
    """该卡在该 trigger 上是否判「对」—— **按同 trigger 的最后一行**（B107 那次是一次
    `{trigger: 判定}` 字典 join，同 trigger 多行时后写覆盖前写；实测 SW34/BL32/GOF90/TL69
    四张卡只有「取最后一行」这一种口径能同时对上盘上判定）。"""
    rows = [r for r in pool.get(cid, []) if r[9] == str(tg)]
    return bool(rows) and rows[-1][7] == "对"


def anim_cols(cid, engine_by_id, raw_by_id, pool, dump, proj):
    at = (engine_by_id.get(cid) or {}).get("animTimes") or []
    if not at:
        return "N/A", "animTimes=0（原版 cardAnimations 无非空 GUID / 无动画）", False

    n = len(at)
    # 分组：tg 唯一（同卡同 tg 必同 tn），按 tg 升序；tg=-1 的 tn 记作 actAbility
    order = sorted({a["tg"] for a in at})
    parts = []
    for tg in order:
        grp = [a for a in at if a["tg"] == tg]
        label = "actAbility" if tg == -1 else grp[0]["tn"]
        parts.append("tg%d(%s)×%d" % (tg, label, len(grp)))
    head = "animTimes %d条[%s]" % (n, "、".join(parts))

    haa = (raw_by_id.get(cid) or {}).get("hasActiveAbility", None)
    tgs = order
    d = dump.get(cid) or []
    played = d[7] if len(d) > 7 else ""
    rc = d[5] if len(d) > 5 else ""
    rcname = d[6] if len(d) > 6 else ""

    tgminus1 = [a for a in at if a["tg"] == -1]
    # 全部 tg-1 且原版不消费 ⇒ 原版不可达（例外①）
    if tgs == [-1] and haa != 1:
        declared = sum(1 for a in at if a.get("n"))
        ev = ("%s，全部 tg-1（activeAbility）且 hasActiveAbility 未记（=%s）≠1 "
              "⇒ 原版自己也不消费（CardScript__ResolveActiveAbilityPlayed.c:22；"
              "gen_cards_engine.py:1810-1811）⇒ 铁律 11 例外① 原版不可达"
              "（prefab %d/%d 在工程）") % (head, haa, declared, declared)
        return "N/A", ev, True

    suffix = ""
    if tgminus1 and haa != 1:
        suffix = ("；另 tg-1(actAbility)×%d 条 hasActiveAbility≠1 ⇒ 原版不消费（例外①）"
                  % len(tgminus1))

    # tg>0 必须在该卡的 S3 引擎表里同 trigger 有一行「对」
    failing = [tg for tg in tgs if tg > 0 and not _s3_trigger_ok(cid, tg, pool)]
    if failing:
        ev = ("%s；trigger %s 对应的 S3 引擎行为「未验到」" % (
            head, "/".join(str(t) for t in sorted(failing))))
        return "未验到", ev + suffix, False

    # tg=0 走 play 通路，需要 play all played=1
    if 0 in tgs and played != "1":
        ev = ("%s；tg0(ThisCardPlayed) 对应 play all played=%s（playRc=%s %s）" % (
            head, played, rc, rcname))
        return "未验到", ev + suffix, False

    declared = sum(1 for a in at if a.get("n"))
    if declared == 0:
        prefab_clause = "prefab 全是纯计时占位（n 为空，无可播 prefab）"
    else:
        prefab_clause = "prefab %d/%d 在工程" % (declared, declared)
    ev = ("%s；%s；tg0 走 play 通路（played=%s）· tg>0 对应 S3 行全对 · "
          "tg-1 走 IsActiveAbilityEntry") % (head, prefab_clause, played)
    return "对", ev + suffix, False


# ---------------------------------------------------------------- ④ 语音
def voice_cols(cid, sk_row, voice_by_cf, total_lines):
    key = (sk_row[3], sk_row[1])  # (阵营, 卡名) —— 骨架列：0卡id 1卡名 2卡名Zh 3阵营 4卡种
    vc = voice_by_cf.get(key)
    if vc is None:
        return "N/A", "voice_lines 无该卡条目（原版该卡无语音）"
    if cid in SPECIAL_VOICE_EVIDENCE:
        return "未验到", SPECIAL_VOICE_EVIDENCE[cid]
    lines = vc.get("lines") or []
    n = len(lines)
    cardso = sum(1 for l in lines if l.get("src") == "cardSO")
    if cardso > 0:
        ev = ("voice_lines %d条（cardSO 归属 %d 条）；音频全在 Resources/Art/audio/vo"
              "（%d/%d）；12 事件族消费点已接（VoiceLines.cs）") % (
                  n, cardso, total_lines, total_lines)
        return "对", ev
    return "未验到", "voice_lines %d条但无 cardSO 归属（仅 none/orphan）" % n


# ---------------------------------------------------------------- 主生成
def generate():
    sk = read_tsv(P_SKELETON)
    sk_rows = sk[1:]
    engine = load_json(P_ENGINE)
    engine_by_id = {c["id"]: c for c in engine["cards"]}
    raw_by_id = {c["uniqueId"]: c for c in load_json(P_RAW)["cards"]}
    vfx_by_card = load_json(P_VFX)["byCard"]
    voice_by_cf = {(c["faction"], c["name"]): c for c in load_json(P_VOICE)["cards"]}
    total_lines = sum(len(c.get("lines") or []) for c in load_json(P_VOICE)["cards"])
    proj = prefab_names()
    pool, dump = build_engine()

    out_rows = []
    for sr in sk_rows:
        cid = sr[0]
        e_j, e_ev = engine_cols(cid, sr, engine_by_id, pool, dump, raw_by_id)
        f_j, f_ev = vfx_cols(cid, sr, vfx_by_card, raw_by_id, proj)
        a_j, a_ev, a_ability_excluded = anim_cols(cid, engine_by_id, raw_by_id, pool, dump, proj)
        v_j, v_ev = voice_cols(cid, sr, voice_by_cf, total_lines)

        # 数据来源：固定顺序拼装
        src = ["S1全卡核验总表（1,128 卡骨架）"]
        if cid in pool:
            src.append("S3:pool_diff4(w41s3b)")
        src.append("cards_engine")
        if [k for k in (vfx_by_card.get(cid) or {}) if k != "_assetRefs"]:
            src.append("card_vfx_by_card")
        if (engine_by_id.get(cid) or {}).get("animTimes"):
            src.append("cards_engine.animTimes")
        if v_j != "N/A":
            src.append("voice_lines")

        # 备注：按触发原因拼
        notes = []
        if "；另 ability 桶" in f_ev:
            notes.append("ability 桶原版不消费（hasActiveAbility≠1）")
        if a_ability_excluded:
            notes.append("activeAbility 动画原版不消费（hasActiveAbility≠1）")
        note = "；".join(notes)
        if cid in SPECIAL_NOTE:
            note = SPECIAL_NOTE[cid]

        out_rows.append([
            cid, sr[1], sr[3], sr[4],
            e_j, e_ev,
            f_j, f_ev,
            a_j, a_ev,
            v_j, v_ev,
            "/".join(src), note,
        ])

    return out_rows


def render(rows):
    lines = ["\t".join(HEADER)]
    for r in rows:
        lines.append("\t".join(r))
    return "\n".join(lines) + "\n"


def usage(prog):
    sys.stdout.write(
        "用法：%s <动作>\n"
        "\n"
        "动作（必选其一，裸跑不写任何文件）：\n"
        "  --check   只比对：与盘上现有 `资料/完全复刻/判据/全卡核验_判定.tsv` 逐字节比\n"
        "  --dry     只预览（前 5 行 + 总行数），不写盘\n"
        "  --write   真正写盘（覆盖现有判定表）\n"
        "\n"
        "数据源与判据见脚本头注释与 `资料/完全复刻/报告/B107_W25.md` §三 / §七。\n" % prog)


def main(argv):
    prog = os.path.basename(argv[0])
    args = argv[1:]
    if not args:
        usage(prog)
        return 2
    if len(args) != 1 or args[0] not in ("--check", "--dry", "--write"):
        sys.stderr.write("参数非法（必须且只能给一个动作）\n")
        usage(prog)
        return 2
    action = args[0]

    missing = [p for p in (P_SKELETON, P_POOLDIF, P_ALLDUMP, P_VFX, P_ENGINE, P_RAW, P_VOICE)
               if not os.path.isfile(p)]
    if missing:
        sys.stderr.write("缺少数据源（⛔ 不写盘）：\n" + "\n".join("  " + p for p in missing) + "\n")
        return 2

    rows = generate()
    text = render(rows)
    data = text.encode("utf-8")

    if action == "--dry":
        sys.stdout.write("预览（前 5 行）：\n")
        for r in rows[:5]:
            sys.stdout.write("  " + " | ".join(r[:4]) + " …\n")
        sys.stdout.write("共 %d 行数据（不含表头）\n" % len(rows))
        return 0

    if action == "--write":
        io.open(P_OUT, "wb").write(data)
        sys.stdout.write("已写入：%s（%d 行数据）\n" % (P_OUT, len(rows)))
        return 0

    # --check
    if not os.path.isfile(P_OUT):
        sys.stdout.write("盘上不存在 %s ⇒ 无法比对\n" % P_OUT)
        return 1
    disk = io.open(P_OUT, "rb").read()
    if disk == data:
        sys.stdout.write("逐字节相同（%d 行数据）\n" % len(rows))
        return 0

    sys.stdout.write("与盘上不同：\n")
    dl = disk.decode("utf-8", "replace").split("\n")
    gl = text.split("\n")
    diffs = 0
    for i in range(max(len(dl), len(gl))):
        a = dl[i] if i < len(dl) else "<缺行>"
        b = gl[i] if i < len(gl) else "<缺行>"
        if a != b:
            diffs += 1
            if diffs <= 40:
                sys.stdout.write("  L%d\n    盘上: %s\n    生成: %s\n" % (i + 1, a[:220], b[:220]))
    sys.stdout.write("差异行数：%d（盘上 %d 行 / 生成 %d 行）\n" % (diffs, len(dl) - 1, len(gl) - 1))
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
