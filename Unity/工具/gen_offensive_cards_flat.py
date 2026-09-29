# -*- coding: utf-8 -*-
"""gen_offensive_cards_flat.py —— 把 `数据/游戏数据/offensive_cards.json` 压成 **Unity `JsonUtility` 读得动**的形。

为什么要这一步：源表里 `armies` 是**按阵营名做键的对象**，而 `JsonUtility` **不支持字典**
（工程里 `CardDatabase` 也是靠 DTO + 数组）。⇒ 这里只做**形状转换**，**不新增任何判断**：
  · `armies: { "Ultramarines": {...} }`  →  `armies: [ { "army": "Ultramarines", ... } ]`
  · 只留 UI 要用的字段（卡面贴图名 / 环境 SO 名 / prefab 名 / forgeLevel / 候选卡名），
    原表里的 `_schema` / `_sources` / 证据字段**一律不进**（它们留在源表那份里，判据只有一处）。

产物：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/OffensiveCards.json`
用法：PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_offensive_cards_flat.py
"""
import io, json, os

SRC = "d:/4/Unity/数据/游戏数据/offensive_cards.json"
DST = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/OffensiveCards.json"


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
        })
    io.open(DST, "w", encoding="utf-8", newline="\n").write(
        json.dumps(out, ensure_ascii=False, indent=1))
    print("FLAT 阵营 %d · 卡槽 %d → %s" %
          (len(out["armies"]), sum(len(a["cards"]) for a in out["armies"]), DST))


if __name__ == "__main__":
    main()
