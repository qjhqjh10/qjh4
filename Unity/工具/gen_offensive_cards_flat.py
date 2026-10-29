# -*- coding: utf-8 -*-
"""gen_offensive_cards_flat.py —— 把 `数据/游戏数据/offensive_cards.json` 压成 **Unity `JsonUtility` 读得动**的形。

为什么要这一步：源表里 `armies` 是**按阵营名做键的对象**，而 `JsonUtility` **不支持字典**
（工程里 `CardDatabase` 也是靠 DTO + 数组）。⇒ 这里只做**形状转换**，**不新增任何判断**：
  · `armies: { "Ultramarines": {...} }`  →  `armies: [ { "army": "Ultramarines", ... } ]`
  · 只留 UI 要用的字段（卡面贴图名 / 环境 SO 名 / prefab 名 / forgeLevel / 候选卡名），
    原表里的 `_schema` / `_sources` / 证据字段**一律不进**（它们留在源表那份里，判据只有一处）。

产物：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/OffensiveCards.json`
用法：PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_offensive_cards_flat.py
      加 `--dry` = 只跑计算、打印读数、**零写盘**（默认行为不变；`A1648` 同族口径）。
      🆕 加 `--check` = 只重算、与盘上产物比 md5，**零写盘**（rc 0 = 一致 / 1 = 缺或过期；口径同
      `gen_prebuilt_bot_decks.py --check`）。
"""
import io, json, os, sys, hashlib

# 🔴 `--dry` = 只跑计算、打印读数、**零写盘**。默认（不带它）行为与以前完全一样。
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_offensive_cards_flat.py\n      加 `--dry` = 只跑计算、打印读数、**零写盘**（默认行为不变；`A1648` 同族口径）。\n      加 `--check` = 只重算、与盘上产物比 md5，**零写盘**（rc 0 = 一致 / 1 = 缺或过期）。\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

DRY = "--dry" in sys.argv[1:]
# 🆕 2026-10-30（B99_A5）：`--check` = 只对账、不写盘（口径照 `gen_prebuilt_bot_decks.py:--check`）。
CHECK = "--check" in sys.argv[1:]

SRC = "d:/4/Unity/数据/游戏数据/offensive_cards.json"
DST = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/OffensiveCards.json"
# 🆕 2026-10-27（`B9_E2UDW`）：防御卡（原版 `EnviromentalEffectCardsSO.enviromentalEffects[].defensiveCards`）
#   —— 由 `工具/gen_defensive_cards.py` 解出。**文件不在就照旧不并**（老行为不变）。
DEF = "d:/4/Unity/数据/游戏数据/defensive_cards.json"


def main():
    d = json.load(io.open(SRC, encoding="utf-8"))
    out = {"armies": []}
    for army, a in sorted(d["armies"].items()):
        cards = []
        for i, c in enumerate(a.get("cards") or []):
            cards.append({
                "idx": i,
                "name": c.get("cardName_candidate") or "",
                "nameFrom": c.get("cardName_candidate_src") or "",
                "envSO": c.get("envSO_name") or "",
                "prefab": c.get("prefab_name") or "",
                "face": c.get("face_texture") or "",
                "forgeLevel": c.get("forgeLevel") or 0,
            })
        e = a.get("emptyOffensiveCard") or {}
        out["armies"].append({
            "army": army,
            "armyId": a.get("armyId") or 0,
            "defaultEnvSO": (a.get("defaultEnviromentalEffectVFX") or {}).get("envSO_name") or "",
            "emptyName": e.get("cardName_candidate") or "",
            "emptyFace": e.get("face_texture") or "",
            "cards": cards,
            # 🆕 2026-10-27（`B9_E2UDW`）：防御卡槽（账 208 `GetEnvCardListDraft` 的防御半边）。
            #   判据 = `工具/gen_defensive_cards.py` 的产物；卡名**解不出**（`m_FileID: 4` 外部文件 +
            #   防御卡没有配套卡面贴图）⇒ `name` 恒空、只留 `cardRefPathID` 供以后解名。
            "defensiveCards": [],
        })

    # 把防御卡并进来（**没有那个文件就照旧**）。
    if os.path.exists(DEF):
        dd = json.load(io.open(DEF, encoding="utf-8"))
        by = {k: (v.get("defensiveCards") or []) for k, v in (dd.get("armies") or {}).items()}
        for a in out["armies"]:
            a["defensiveCards"] = [
                {
                    "idx": c.get("idx", i),
                    "name": "",
                    "forgeLevel": c.get("forgeLevel") or 0,
                    "cardRefPathID": (c.get("cardRef") or {}).get("m_PathID") or 0,
                }
                for i, c in enumerate(by.get(a["army"]) or [])
            ]
    # ⚠️ 先算好再写（别在 write 的实参里做会抛异常的事 —— 那会把文件截成 0 字节，CLAUDE.md 记过）
    text = json.dumps(out, ensure_ascii=False, indent=1)
    if CHECK:
        # 🆕 2026-10-30（B99_A5）：只对账、不写盘。缺文件 / 内容不同都算 FAIL（rc 1）。
        want = hashlib.md5(text.encode("utf-8")).hexdigest()
        if not os.path.exists(DST):
            print("MISSING", DST)
            print("--check FAIL（盘上没有产物）")
            return 1
        have = hashlib.md5(open(DST, "rb").read()).hexdigest()
        if have != want:
            print("DIFF", DST, "disk=%s want=%s" % (have, want))
            print("--check FAIL（盘上产物过期 ⇒ 重跑本脚本，**别手改**）")
            return 1
        print("SAME", DST, have)
        print("--check OK（盘上产物与本次重算逐字节一致）")
        return 0
    if DRY:
        print("干跑（未写盘）%s：FLAT 阵营 %d · 卡槽 %d · 防御槽 %d" %
              (DST, len(out["armies"]), sum(len(a["cards"]) for a in out["armies"]),
               sum(len(a["defensiveCards"]) for a in out["armies"])))
        return
    io.open(DST, "w", encoding="utf-8", newline="\n").write(text)
    print("FLAT 阵营 %d · 卡槽 %d · 防御槽 %d → %s" %
          (len(out["armies"]), sum(len(a["cards"]) for a in out["armies"]),
           sum(len(a["defensiveCards"]) for a in out["armies"]), DST))


if __name__ == "__main__":
    sys.exit(main())
