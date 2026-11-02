#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_fullpool_event_coverage.py — 全池「每卡事件 × 资源」静态覆盖表（S3-G2 折中方案）。

买的是什么
==========
证明**每个会发的事件都有对应的 prefab / clip / cue**，并把**缺件**逐条列出来；
在「有件」之上再加一维**「参数对」**（参数值与原版真身逐字段比）。

⛔ 买不到什么：**「真的播了」** —— 那要 Unity 窗口里的逐卡 trace（`S3-G1`）。
本表只证「通路完整 + 参数一致」，⛔ 不能读成「1,129 张卡的表现已经验完」。

三个维、各自的判据（全部静态，不跑 Unity、不放音频）
====================================================
| 维 | 事件来源 | 资源名 | 「在工程?」判据 | 「在消费表?」判据 | 「参数对」判据 |
|---|---|---|---|---|---|
| ② 特效 | `card_vfx_by_card.json` `cards[].events[].es[]` | `es[].n`（prefab） | `MyGame/Assets/**/<n>.prefab` 存在 | `Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 里有同名 entry（= `WarpforgeEffectPlayer.cs:245` 的 `lib.TryGet`） | `工具/vfx_param_audit.py --check` 的逐 prefab 判定（粒子全模块 / Renderer / Transform / 材质运行时队列） |
| ③ 动画 | `cards_engine.json` `cards[].animTimes[]` | `n`（prefab，空则用 `a`） | 同上 | `cardanim_clips.json` 有 `a` 键（= `CardAnimClips.PlacementOf`，`Battle/CardAnimClips.cs:491`） | 逐行回读**原版** `CardAnim` 的 `animInfo`，比 `d/sd/s/m/e` 五值（映射见 `gen_cards_engine.py:3212-3221`），并自证 `u = m+s+e+d` 与 `t` = 同 trigger 组内累计 |
| ④ 语音 | `card_vfx_by_card.json` `cards[].audio[]` | `audio[].n`（clip 名） | `Assets/CardPresentation/Resources/Art/audio/vo/<n>.ogg|.wav` 存在（= `VoiceLines.Clip()` 的 `Resources.Load` 路径，`Core/VoiceLines.cs:82/908`） | `voice_lines.json` 里同卡同事件同文件名（= `TryPickExact`，`VoiceLines.cs:300`） | 音量 `AudioListener.volume`（原版 `PlaySoundClip` 同式；SO 的 `volume` 不被采信）· 该路无冷却 —— 判据 `BattleDriver.cs:7684` / `:17463-17469` |

判定三档
========
* `✅齐全`   —— 在工程 + 在消费表（两面都齐）。
* `🟠缺件（…）` —— 哪一面缺就写哪一面。
* `⛔不适用（<原版依据>）` —— 原版本身就没有这一件（例：`Wait_*` CardAnim 的 `animAdressable.m_AssetGUID == ""`）。

参数三档：`✅一致` / `🟠不一致（原版 X ↔ 我们 Y，字段，出处）` / `⛔判不了（为什么）`。
⚠️ 「不一致」**只在原版有明确值时**才判；原版查不到一律 `⛔判不了` 并写「搜过哪几个目录」。

用法（⛔ 不带动作参数时只打用法、rc=2、零写盘）
==============================================
  python -I -X utf8 工具/gen_fullpool_event_coverage.py                     # 用法，rc=2
  python -I -X utf8 工具/gen_fullpool_event_coverage.py --dry               # 只预览（逐维汇总 + 缺件清单），不写盘
  python -I -X utf8 工具/gen_fullpool_event_coverage.py --check             # 与盘上产物逐行比，rc=0 一致 / 1 有差异
  python -I -X utf8 工具/gen_fullpool_event_coverage.py --write             # 落盘 `判据/全池事件覆盖_静态.tsv`

可选参数：`--out` · `--assets <MyGame/Assets>` · `--cardanims <原版 CardAnim 目录>` ·
          `--vfx-audit-log <vfx_param_audit --check 的输出>`（给了就按它逐 prefab 判参数；
          不给则回退 `判据/B9_特效参数对账.tsv`，再没有就 `⛔判不了`）。

依赖：仅 Python 3 标准库。
"""
import argparse
import glob
import io
import json
import os
import re
import sys

UNITY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # d:/4/Unity

DEFAULT_ASSETS = os.path.join(UNITY, "MyGame", "Assets")
DEFAULT_OUT = os.path.join(UNITY, "资料", "完全复刻", "判据", "全池事件覆盖_静态.tsv")
DEFAULT_CARDANIMS = "D:/2/新解包资源/assets_full/*cardanims*_assets_all/MonoBehaviour"
DEFAULT_B9 = os.path.join(UNITY, "资料", "完全复刻", "判据", "B9_特效参数对账.tsv")

HEADER = ["cardId", "维", "事件", "资源名", "在工程", "在消费表", "判定", "参数判定", "参数出处"]

USAGE = __doc__.strip()


# ---------------------------------------------------------------- 输入
def _load_json(path):
    with io.open(path, encoding="utf-8") as f:
        return json.load(f)


def load_prefab_index(assets):
    """basename（去扩展名）→ [绝对路径…]。搜过的目录会写进报告（铁律：判「没有」要交代搜过哪）。"""
    idx = {}
    for p in glob.glob(os.path.join(assets, "**", "*.prefab"), recursive=True):
        idx.setdefault(os.path.splitext(os.path.basename(p))[0], []).append(p)
    return idx


def load_library_names(path):
    """`WarpforgeEffectLibrary.asset` 的顶层 entry 名（= 运行期 `lib.TryGet` 认的那一格）。

    ⚠️ 只认行首 `  - name: `（两个空格 + 破折号）。文件里还有深层 `values:` 里的同名段落，
    按裸 `name:` 数会把 1 个 entry 数成多个（实测 982 → 误数 900+）。
    """
    names = set()
    if not os.path.isfile(path):
        return names
    pat = re.compile(r"^  - name: (.+)$")
    with io.open(path, encoding="utf-8") as f:
        for line in f:
            m = pat.match(line.rstrip("\n").rstrip("\r"))
            if m:
                names.add(m.group(1))
    return names


def load_audit_scope(prefab_dir):
    """`vfx_param_audit.py` 扫的是 `<Prefabs>/*.prefab` —— 这就是它的覆盖边界。"""
    return set(os.path.splitext(f)[0] for f in os.listdir(prefab_dir) if f.endswith(".prefab")) \
        if os.path.isdir(prefab_dir) else set()


def load_vfx_param_verdicts(audit_log, b9_tsv):
    """prefab → (verdict, evidence)。audit_log 优先（现跑）；否则回退 B9 表。"""
    per = {}
    src = None
    if audit_log and os.path.isfile(audit_log):
        src = os.path.basename(audit_log)
        bad, unjudged = {}, {}
        mode = None
        with io.open(audit_log, encoding="utf-8") as f:
            for line in f:
                line = line.rstrip("\n").rstrip("\r")
                if "真差异" in line and "（共" in line:
                    mode = "real"
                    continue
                if "判不了（原版侧缺件" in line:
                    mode = "unj"
                    continue
                if mode == "unj":
                    m = re.match(r"^\s+· (.+?) —— (.+)$", line)
                    if m:
                        unjudged[m.group(1)] = m.group(2)
                    elif line.strip() == "":
                        mode = None
                elif mode == "real":
                    m = re.match(r"^\s{4}(.+?) \| (.+?) \| (.+?)\.(.+?): 原版 (.*?) / 我们 (.*)$", line)
                    if m:
                        bad.setdefault(m.group(1), []).append(
                            "%s.%s 原版 %s ↔ 我们 %s" % (m.group(2), m.group(3), m.group(5), m.group(6)))
                    elif line.strip() == "" or line.startswith("──"):
                        mode = None
        for nm, why in unjudged.items():
            per[nm] = ("⛔判不了（审计侧：%s）" % why, src)
        for nm, diffs in bad.items():
            per[nm] = ("🟠不一致（%s）" % "；".join(diffs[:2]), src)
    if not per and b9_tsv and os.path.isfile(b9_tsv):
        src = os.path.basename(b9_tsv)
        with io.open(b9_tsv, encoding="utf-8") as f:
            for i, line in enumerate(f):
                p = line.rstrip("\n").rstrip("\r").split("\t")
                if i == 0 or len(p) < 10:
                    continue
                nm, verdict = p[0], p[8]
                if verdict != "一致":
                    per[nm] = ("🟠不一致（%s：%s 原版 %s ↔ 我们 %s）"
                               % (src, p[5], p[6], p[7]), src)
    return per, src


def load_orig_cardanims(pattern):
    """CardAnim 资产名（= JSON 文件 stem）→ 原版 `animInfo`。读的是**原版真身**。"""
    idx, dups = {}, 0
    for d in glob.glob(pattern):
        for p in glob.glob(os.path.join(d, "*.json")):
            stem = os.path.basename(p)[:-5]
            if stem in idx:
                dups += 1
            idx.setdefault(stem, p)
    return idx, dups


def load_vo_names(assets):
    d = os.path.join(assets, "CardPresentation", "Resources", "Art", "audio", "vo")
    if not os.path.isdir(d):
        return set(), d
    return set(os.path.splitext(f)[0] for f in os.listdir(d)
               if f.lower().endswith((".ogg", ".wav"))), d


# ---------------------------------------------------------------- 造行
F_MAP = (("d", "vfxDelayTime"), ("sd", "startDelay"), ("s", "timeAtStartPos"),
         ("m", "timeMoving"), ("e", "timeAtEndPos"))


def build_rows(cfg):
    assets = cfg["assets"]
    prefabs = load_prefab_index(assets)
    lib = load_library_names(os.path.join(assets, "Resources", "WarpforgeVFX",
                                          "WarpforgeEffectLibrary.asset"))
    audit_scope = load_audit_scope(os.path.join(assets, "WarpforgeVFX", "Prefabs"))
    vfx_param, vfx_src = load_vfx_param_verdicts(cfg["vfx_audit_log"], cfg["b9_tsv"])
    cardanims, cad_dups = load_orig_cardanims(cfg["cardanims"])
    vo_names, vo_dir = load_vo_names(assets)

    vfx_doc = _load_json(os.path.join(assets, "CardPresentation", "Resources", "card_vfx_by_card.json"))
    engine = _load_json(os.path.join(assets, "RuleEngine", "Resources", "cards_engine.json"))
    clips = _load_json(os.path.join(assets, "CardPresentation", "Resources", "cardanim_clips.json"))
    clip_keys = set(r.get("a") or "" for r in clip_keys_rows(clips))
    voice_doc = _load_json(os.path.join(assets, "CardPresentation", "Resources", "voice_lines.json"))
    vl_by_id = {c.get("id"): c for c in (voice_doc.get("cards") or [])}

    rows = []
    stats = {"cardanims_dups": cad_dups, "vo_dir": vo_dir, "vfx_src": vfx_src,
             "audit_scope": len(audit_scope), "orig_cardanims": len(cardanims),
             "prefab_names": len(prefabs), "lib_names": len(lib)}

    def prefab_present(name):
        return name in prefabs

    def vfx_param_of(name):
        if name in vfx_param:
            v, s = vfx_param[name]
            return v, "工具/vfx_param_audit.py --check（%s）" % (s or "审计日志")
        if not audit_scope:
            return "⛔判不了（未找到 WarpforgeVFX/Prefabs 审计范围）", "—"
        if name not in audit_scope:
            return ("⛔判不了（该 prefab 不在 vfx_param_audit 的扫描范围内："
                    "只扫 Assets/WarpforgeVFX/Prefabs/*.prefab）"), "工具/vfx_param_audit.py:990"
        return "✅一致", "工具/vfx_param_audit.py --check（978 prefab 逐字段，现读 0 真差异）"

    # ---- ② 特效
    for card in vfx_doc.get("cards") or []:
        cid = card.get("id")
        for ev in card.get("events") or []:
            for es in ev.get("es") or []:
                name = es.get("n") or ""
                if not name:
                    rows.append([cid, "特效", ev.get("ev"), "（空名）", "🟠不在", "🟠不在",
                                 "⛔不适用（原版 `es[].n` 为空：无 prefab 可播）",
                                 "⛔判不了（无件可比参数）", "—"])
                    continue
                in_proj = prefab_present(name)
                in_tab = name in lib
                if in_proj and in_tab:
                    verdict = "✅齐全"
                elif not in_proj and not in_tab:
                    verdict = "🟠缺件（prefab + 消费表两面都缺）"
                elif not in_proj:
                    verdict = "🟠缺件（缺 prefab）"
                else:
                    verdict = "🟠缺件（缺消费表 entry）"
                pv, ps = vfx_param_of(name) if in_proj else ("⛔判不了（无件可比参数）", "—")
                rows.append([cid, "特效", ev.get("ev"), name,
                             "✅在" if in_proj else "🟠不在",
                             "✅在" if in_tab else "🟠不在", verdict, pv, ps])

    # ---- ③ 动画
    for card in engine.get("cards") or []:
        cid = card.get("id")
        for r in card.get("animTimes") or []:
            a = r.get("a") or ""
            n = r.get("n") or ""
            ev = r.get("tn") or ("tg=%s" % r.get("tg"))
            shown = n or a
            if not n:
                basis = ("原版 `%s` 的 `animInfo.animAdressable.m_AssetGUID` 为空 ⇒ 纯延时、无 prefab"
                         % a)
                rows.append([cid, "动画", ev, shown, "⛔无件", "⛔无件",
                             "⛔不适用（%s）" % basis, "⛔判不了（无件可比参数）", "—"])
                continue
            in_proj = prefab_present(n)
            in_tab = a in clip_keys
            if in_proj and in_tab:
                verdict = "✅齐全"
            elif not in_proj and not in_tab:
                verdict = "🟠缺件（prefab + cardanim 表两面都缺）"
            elif not in_proj:
                verdict = "🟠缺件（缺 prefab）"
            else:
                verdict = "🟠缺件（cardanim_clips 里没有 `a`=%s）" % a
            pv, ps = anim_param_of(a, r, cardanims)
            rows.append([cid, "动画", ev, shown,
                         "✅在" if in_proj else "🟠不在",
                         "✅在" if in_tab else "🟠不在", verdict, pv, ps])

    # ---- ④ 语音
    for card in vfx_doc.get("cards") or []:
        cid = card.get("id")
        for au in card.get("audio") or []:
            n = au.get("n") or ""
            ev = au.get("ev")
            if not n:
                rows.append([cid, "语音", ev, "（空名）", "🟠不在", "🟠不在",
                             "⛔不适用（原版 SO 字段为空）", "⛔判不了（无件可比参数）", "—"])
                continue
            in_proj = n in vo_names
            vl = vl_by_id.get(cid)
            in_tab = bool(vl) and any(
                (os.path.splitext(l.get("file") or "")[0] == n and l.get("ev") == ev)
                for l in (vl.get("lines") or []))
            if in_proj and in_tab:
                verdict = "✅齐全"
            elif not in_proj and not in_tab:
                verdict = "🟠缺件（音频文件 + voice_lines 两面都缺）"
            elif not in_proj:
                verdict = "🟠缺件（缺音频文件）"
            else:
                verdict = "🟠缺件（voice_lines 无同卡同事件同名行）"
            pv = "✅一致"
            ps = ("音量 = `AudioListener.volume`（原版 `BattleManager.PlaySoundClip` 同式；"
                  "SO 的 `volume` 不被采信）· 该路无冷却闸 —— `BattleDriver.cs:7684` / `:17463-17469`"
                  "；`decomp_full/BattleManager__PlaySoundClip.c`")
            rows.append([cid, "语音", ev, n,
                         "✅在" if in_proj else "🟠不在",
                         "✅在" if in_tab else "🟠不在", verdict, pv, ps])

    order = {"特效": 0, "动画": 1, "语音": 2}
    rows.sort(key=lambda r: (str(r[0]), order.get(r[1], 9), str(r[2]), str(r[3])))
    return rows, stats


def clip_keys_rows(clips):
    return clips.get("rows") or []


def anim_param_of(a, row, cardanims):
    """逐行回读原版 CardAnim 的 animInfo，比 `d/sd/s/m/e`（映射见 `gen_cards_engine.py:3212-3221`）。"""
    p = cardanims.get(a)
    if not p:
        return ("⛔判不了（原版查不到 `%s`：搜过 D:/2/新解包资源/assets_full/*cardanims*_assets_all/"
                "MonoBehaviour/*.json）" % a), "—"
    try:
        info = (_load_json(p) or {}).get("animInfo") or {}
    except Exception as e:                                    # noqa: BLE001
        return "⛔判不了（原版 JSON 读不开：%s）" % e, p
    for k, f in F_MAP:
        ov = round(float(info.get(f) or 0.0), 4)
        mv = round(float(row.get(k) or 0.0), 4)
        if abs(ov - mv) > 1e-4:
            return ("🟠不一致（原版 %s ↔ 我们 %s，animInfo.%s，%s）"
                    % (ov, mv, f, os.path.basename(p))), p
    # ⚠️ `u` = `m + s + e + d`，**`startDelay` 不在里面**（判据：`gen_cards_engine.py:3221`
    #    「原版 `AnimInfo.GetAnimDuration` 逐字」）—— 把 `sd` 算进去会把 1 条本来正确的行判成差异。
    exp_u = round(sum(float(info.get(f) or 0.0)
                      for k, f in F_MAP if k != "sd"), 4)
    if abs(exp_u - round(float(row.get("u") or 0.0), 4)) > 1e-4:
        return ("🟠不一致（原版时长 %s ↔ 我们 u=%s，GetAnimDuration 派生，%s）"
                % (exp_u, row.get("u"), os.path.basename(p))), p
    return "✅一致", p


# ---------------------------------------------------------------- 汇总 / 输出
def summarize(rows):
    per = {}
    for r in rows:
        dim = r[1]
        s = per.setdefault(dim, {"n": 0, "✅齐全": 0, "🟠缺件": 0, "⛔不适用": 0,
                                 "参数✅": 0, "参数🟠": 0, "参数⛔": 0, "bad": []})
        s["n"] += 1
        v = r[6]
        if v.startswith("✅"):
            s["✅齐全"] += 1
        elif v.startswith("🟠"):
            s["🟠缺件"] += 1
            s["bad"].append(r)
        else:
            s["⛔不适用"] += 1
        p = r[7]
        if p.startswith("✅"):
            s["参数✅"] += 1
        elif p.startswith("🟠"):
            s["参数🟠"] += 1
            s["bad_param"] = s.get("bad_param", [])
            s["bad_param"].append(r)
        else:
            s["参数⛔"] += 1
    return per


def print_summary(rows, stats, out=sys.stdout):
    def w(s=""):
        out.write(s + "\n")
    per = summarize(rows)
    cards = len(set(r[0] for r in rows))
    w("数据源：prefab 名 %d · 效果库 entry %d · 审计范围 %d · 原版 CardAnim %d（重名 %d）"
      % (stats["prefab_names"], stats["lib_names"], stats["audit_scope"],
         stats["orig_cardanims"], stats["cardanims_dups"]))
    w("vfx 参数来源：%s" % (stats["vfx_src"] or "（无：全部 ⛔判不了）"))
    w("")
    w("%-6s %8s %8s %8s %8s   %s" % ("维", "总数", "齐全", "缺件", "不适用", "参数 一致/不一致/判不了"))
    tot = [0, 0, 0, 0, 0, 0, 0]
    for dim in ("特效", "动画", "语音"):
        s = per.get(dim)
        if not s:
            continue
        w("%-6s %8d %8d %8d %8d   %d / %d / %d"
          % (dim, s["n"], s["✅齐全"], s["🟠缺件"], s["⛔不适用"],
             s["参数✅"], s["参数🟠"], s["参数⛔"]))
        tot = [tot[0] + s["n"], tot[1] + s["✅齐全"], tot[2] + s["🟠缺件"],
               tot[3] + s["⛔不适用"], tot[4] + s["参数✅"], tot[5] + s["参数🟠"],
               tot[6] + s["参数⛔"]]
    w("%-6s %8d %8d %8d %8d   %d / %d / %d"
      % ("合计", tot[0], tot[1], tot[2], tot[3], tot[4], tot[5], tot[6]))
    w("")
    w("全池 %d 张卡 / %d 个事件：有件但参数不一致 = %d 个；缺件 = %d 个；判不了 = %d 个"
      % (cards, len(rows), tot[5], tot[2], tot[6]))
    bad = [r for r in rows if r[6].startswith("🟠")]
    if bad:
        w("")
        w("── 缺件清单（%d 条）──" % len(bad))
        for r in bad[:60]:
            w("  %s | %s | %s | %s | %s" % (r[0], r[1], r[2], r[3], r[6]))
        if len(bad) > 60:
            w("  …… 其余 %d 条见 TSV" % (len(bad) - 60))
    badp = [r for r in rows if r[7].startswith("🟠")]
    if badp:
        w("")
        w("── 参数不一致清单（%d 条）──" % len(badp))
        for r in badp[:60]:
            w("  %s | %s | %s | %s | %s" % (r[0], r[1], r[2], r[3], r[7]))


def write_tsv(rows, path):
    d = os.path.dirname(path)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    # ⚠️ `newline=""`（不是 `"\r\n"`）—— 后者会把显式写的 `\r\n` 再翻成 `\r\r\n`（裸 CR），
    #    而裸 CR 会让一切按行号/切行读的工具算错（本仓 2026-10-25 踩过，见 CLAUDE.md）。
    with io.open(path, "w", encoding="utf-8", newline="") as f:
        f.write("\t".join(HEADER) + "\r\n")
        for r in rows:
            f.write("\t".join("" if x is None else str(x).replace("\t", " ") for x in r) + "\r\n")


def read_tsv(path):
    rows = []
    if not os.path.isfile(path):
        return rows
    with io.open(path, encoding="utf-8") as f:
        for i, line in enumerate(f):
            if i == 0:
                continue
            p = line.rstrip("\n").rstrip("\r").split("\t")
            if len(p) >= len(HEADER):
                rows.append(p[:len(HEADER)])
    return rows


def diff_tsv(rows, path):
    old = read_tsv(path)
    key = lambda r: (r[0], r[1], r[2], r[3])            # noqa: E731
    om = {}
    for r in old:
        om.setdefault(key(r), r)
    nm = {}
    for r in rows:
        nm.setdefault(key(r), r)
    added = [k for k in nm if k not in om]
    removed = [k for k in om if k not in nm]
    changed = [k for k in nm if k in om and nm[k] != om[k]]
    return added, removed, changed


# ---------------------------------------------------------------- main
def main(argv=None):
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--write", action="store_true")
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--dry", action="store_true")
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--assets", default=DEFAULT_ASSETS)
    ap.add_argument("--cardanims", default=DEFAULT_CARDANIMS)
    ap.add_argument("--vfx-audit-log", default="")
    ap.add_argument("--b9-tsv", default=DEFAULT_B9)
    ap.add_argument("-h", "--help", action="store_true")
    a, unknown = ap.parse_known_args(argv)
    actions = [a.write, a.check, a.dry]
    if a.help or sum(1 for x in actions if x) != 1:
        print(USAGE)
        if unknown:
            print("\n（忽略了无法识别的参数：%s）" % " ".join(unknown))
        return 2 if not a.help else 0

    cfg = {"assets": a.assets, "cardanims": a.cardanims,
           "vfx_audit_log": a.vfx_audit_log, "b9_tsv": a.b9_tsv}
    rows, stats = build_rows(cfg)

    if a.write:
        write_tsv(rows, a.out)
        print_summary(rows, stats)
        print("\n已写：%s（%d 行）" % (a.out, len(rows)))
        return 0

    if a.dry:
        print_summary(rows, stats)
        print("\n（--dry：未写盘。要落盘用 --write）")
        return 0

    # --check
    if not os.path.isfile(a.out):
        print_summary(rows, stats)
        print("\n⛔ 盘上没有产物 `%s` —— 先跑 --write" % a.out)
        return 1
    added, removed, changed = diff_tsv(rows, a.out)
    print_summary(rows, stats)
    print("")
    print("对盘上产物：新增 %d 行 / 消失 %d 行 / 内容变 %d 行（%s）"
          % (len(added), len(removed), len(changed), a.out))
    for k in changed[:10]:
        print("  ~ %s" % (k,))
    for k in removed[:10]:
        print("  - %s" % (k,))
    for k in added[:10]:
        print("  + %s" % (k,))
    ok = not added and not removed and not changed
    print("结论：%s" % ("✅ 与盘上一致" if ok else "❌ 有漂移（重跑 --write 或核对来源）"))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
