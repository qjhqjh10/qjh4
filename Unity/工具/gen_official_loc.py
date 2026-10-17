#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_official_loc.py —— 原版官方本地化表（I2 `I2Languages` 远端包）→ 工程用的精简件

**这是什么**：原版客户端的**官方多语言词条表**（9,447 条 × 15 种语言），来源是 Addressables
远端包 `localization_assets_all` 里那个 `I2Languages` 资产的 `temp` 字段（**加密**）。
2026-10-14（第二十七会话）解开：算法 = **逐字节重复密钥 XOR**（不是压缩），
判据与脚本 → `工具/_i2_decode_probe.py`（本脚本**调它**，⛔ 不另抄一份解码逻辑）。

**为什么要有这一份**：在这之前全项目一直以为「原版客户端根本没有中文表」
（`CLAUDE.md` 与 `项目任务.md` 都这么写着）—— 那是**只看了离线包体**得出的结论：
中文表**不在**客户端安装目录里，而是**运行时从远端 Addressables 下的**。
⇒ 现在有了它，一整族「我们自己译」的兜底件都可以换成原版真值
（卡名 / 卡面效果文字 / 聊天 / 成就 / 头像 / 称号 / 商店 / 任务 / 战场提示 …）。

**输出**（只留 **en（第 0 列）与 zh-CN（第 8 列）** —— 其余 13 种语言本项目用不到；
要全量的跑 `工具/_i2_decode_probe.py --out <path>`）：
  `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`
  ```json
  { "_note": …, "_source": …, "_langs": ["en","zh-CN"],
    "terms": { "<键>": "<en>"            // en 与 zh 相同时压成一个字符串
             , "<键>": ["<en>", "<zh>"]  // 不同时是两元组
             } }
  ```
  ⚠️ **zh-CN 列里带 `[[token]]` 与 `((X_y,...))` 记号** —— 那是 I2 的**占位符**，
  由客户端在运行时换成图标/数字（`[[waystone]]`、`((Spirit_x,1))`、`((Melee_attack_icon_x,+1))`、
  `<br>`）。⇒ **它不是「可以直接印的成品文字」**，用之前要经我们那层图标重写
  （`CardPresentation/Core/CardIcons.cs` 的 `Rewrite`，那份词表目前只认 OCR 的 `[Bracket]` 形态，
  **`[[...]]` / `((...))` 还没接** —— 这是接它之前必须先做的一步）。

用法：
  python -I -X utf8 d:/4/Unity/工具/gen_official_loc.py
  python -I -X utf8 d:/4/Unity/工具/gen_official_loc.py --check    # 只对账，不写文件
"""
import argparse
import io
import json
import os
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
PROBE = os.path.join(HERE, "_i2_decode_probe.py")
OUT = r"d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json"

IDX_EN = 0
IDX_ZH = 8          # `mLanguages` 的第 8 项 = `Simplified Chinese` / `zh-CN`（脚本会现核一遍）

NOTE = (
    "原版官方本地化表（I2 Localization 的 `I2Languages`，来自 Addressables 远端包 "
    "`localization_assets_all`）。**本文件只留 en 与 zh-CN 两列**；"
    "🆕 **2026-10-17 起 13 种语言全量另有生成器** → `工具/gen_official_loc_13lang.py`"
    "（产出 `Resources/official_loc_13lang.json` + `数据/游戏数据/官方本地化_13lang.json`，"
    "`Core/Loc.cs` 按 `Loc.Current` 取列）。"
    "⚠️ zh-CN 列是 **I2 占位符模板**（`[[token]]` / `((X_y,…))` / `<br>`），不是成品文字 —— "
    "印出来之前要过图标重写那一层。"
)
SOURCE = (
    "原件 = `d:/2/远端缓存_旧profile_1013/导出/localization_temp_9412193字节_未解密.bin`"
    "（9,412,193 字节；抢救自旧 Windows 用户缓存的 Unity Addressables 缓存，"
    "按哈希命名 ⇒ 按包名搜不到，见 `d:/2/远端缓存_旧profile_1013/README.md`）。"
    "解码 = **逐字节重复密钥 XOR**：`temp[i] = ((i 偶 ? 0x17 : 0xCD) * i) ^ temp[i] ^ pw[i % pw.Length]`，"
    "密钥 = 硬编码字符串字面量（VA `0x18427C678`，138 字符），"
    "出处 = `d:/2/tools/decomp_full/I2.Loc.LanguageSourceAsset__DecryptData.c` + `__.cctor.c`；"
    "**不是压缩**（前任试过的 gzip/zlib/deflate/lzma/bz2 方向是错的）。"
    "⚠️ 年代限制：与 `allcards` 同一批 —— Unity **2022.3.38f1 / 2025-07-30** 的快照，"
    "**不含**快照之后新增的内容（卡池那 266 张没有真值的卡，这里同样可能缺键）。"
)


def decode(out_path):
    """调解码探针把词条表落成 JSON（⛔ 解码逻辑只此一处）。"""
    cmd = [sys.executable, "-I", "-X", "utf8", PROBE, "--out", out_path]
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0 or not os.path.exists(out_path):
        sys.exit(f"❌ 解码探针失败（rc={r.returncode}）\n{r.stdout[-2000:]}\n{r.stderr[-2000:]}")
    return json.load(io.open(out_path, encoding="utf-8"))


def build(doc):
    langs = doc.get("mLanguages") or []
    if len(langs) <= IDX_ZH or langs[IDX_ZH].get("Code") != "zh-CN":
        sys.exit(f"❌ `mLanguages[{IDX_ZH}]` 不是 zh-CN（实得 {langs[IDX_ZH] if len(langs) > IDX_ZH else None}）"
                 f" —— 列号改过，别照旧号取中文")
    terms = {}
    empty = 0
    for x in doc.get("mTerms") or []:
        key = x.get("Term")
        if not key:
            continue
        L = x.get("Languages") or []
        en = L[IDX_EN] if len(L) > IDX_EN else ""
        zh = L[IDX_ZH] if len(L) > IDX_ZH else ""
        if not en and not zh:
            empty += 1
            continue
        terms[key] = en if en == zh else [en, zh]
    return {"_note": NOTE, "_source": SOURCE, "_langs": ["en", "zh-CN"],
            "_count": len(terms), "terms": terms}, empty


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    args = ap.parse_args()

    tmp = os.path.join(tempfile.mkdtemp(prefix="wf_official_loc_"), "i2.json")
    doc, empty = build(decode(tmp))

    pref = {}
    for k in doc["terms"]:
        pref[k.split("/")[0]] = pref.get(k.split("/")[0], 0) + 1
    print(f"词条 {doc['_count']} 条（en/zh 都空的丢掉 {empty} 条）· 键族 {len(pref)} 个")
    for k, v in sorted(pref.items(), key=lambda x: -x[1])[:15]:
        print(f"  {k:24s} {v}")
    for fam in ("Card_Name/", "Card_Description/", "Card_Abilities/",
                "Card_Trait/", "CardTraitDescription/", "Battle/"):
        n = sum(1 for k in doc["terms"] if k.startswith(fam))
        print(f"  [{fam}] {n}")

    text = json.dumps(doc, ensure_ascii=False, separators=(",", ":"))
    if args.check:
        if not os.path.exists(OUT):
            print(f"\n--check：{OUT} 不存在")
            return 1
        old = json.load(io.open(OUT, encoding="utf-8"))
        same = old.get("terms") == doc["terms"] and old.get("_count") == doc["_count"]
        print(f"\n--check：{'一致 ✅' if same else '**已过期** ❌（重跑不带 --check）'}")
        return 0 if same else 1

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(doc, ensure_ascii=False, separators=(",", ":")))
    print(f"\n写出 {OUT}  ({os.path.getsize(OUT) / 1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
