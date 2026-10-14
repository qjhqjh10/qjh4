#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
_i2_decode_probe.py —— I2 Localization 远端词条表（加密的 I2Languages.temp）解码探针

来源 / 判据（全部本机可复查）：
  1) 原件：d:/2/远端缓存_旧profile_1013/导出/localization_temp_9412193字节_未解密.bin
     （= Unity Addressables 远端包 localization_assets_all 里 `I2Languages` 资产的 `temp` 字段，
       见 d:/2/远端缓存_旧profile_1013/导出/I2Languages_typetree.json 的 "temp" 数组：
       首字节 a6 15 19 41 4c cb 88 3d … 与 .bin 头 16 字节逐字节相同）
  2) 解码算法出处（第一权威，反编译方法体）：
     d:/2/tools/decomp_full/I2.Loc.LanguageSourceAsset__DecryptData.c
     d:/2/tools/decomp_full/I2.Loc.LanguageSourceAsset__DoByteSwap.c
       temp[i] = ((i 偶 ? 0x17 : 0xCD) * i) ^ temp[i] ^ (byte)password[i % password.Length]
       （循环原地做，随后 Encoding.UTF8.GetString(temp) -> JsonUtility.FromJson<LanguageSourceData>）
  3) 密码出处：d:/2/tools/decomp_full/I2.Loc.LanguageSourceAsset__.cctor.c
       stringObfuscatorPassword = <字符串字面量 0x18427C678>.ToCharArray()
     该字面量的值来自 d:/2/tools/il2cpp_out/stringliteral.json（address == 0x427C678）；
     同一串 UTF-8 字节在 global-metadata.dat 偏移 0xf2ae8 处原文可查（长度 138 字符 / 199 字节）。
     （同样这串字面量也被 I2.Loc.StringObfucator 的 cctor 使用，两处引用同一个地址。）

不依赖 Unity、不联网、不写任何工程文件。

用法：
  python -I -X utf8 d:/4/Unity/工具/_i2_decode_probe.py
  python -I -X utf8 d:/4/Unity/工具/_i2_decode_probe.py --in <bin 路径> --out <json 输出路径>
  python -I -X utf8 d:/4/Unity/工具/_i2_decode_probe.py --grep "Battle/Tips" --limit 30
"""

import argparse
import io
import json
import os
import sys

DEFAULT_BIN = r"d:/2/远端缓存_旧profile_1013/导出/localization_temp_9412193字节_未解密.bin"
STRINGLITERAL_JSON = r"d:/2/tools/il2cpp_out/stringliteral.json"
PASSWORD_LITERAL_ADDRESS = 0x427C678

# 字符串字面量 @ VA 0x18427C678（138 个 UTF-16 码元）—— 用纯 ASCII 转义写死，避免任何编码/行尾问题。
# 自检：--verify-literal 会拿 stringliteral.json 里的原值逐字符比。
PASSWORD_ESCAPED = (
    "\u00dd\u00fabUu\u00b8C\u00c1\u00c2\u00a7*4P\u00da\u00a9-\u00e1\u00a9\u00be@T6Dl\u00b1\u00d2W\u00e2uz"
    "\u00c5m4G\u00d0\u00f3\u00d8$=\u00cdg,\u00a5Q\u00eb\u00aeiKE\u00dfr\u00a1\u00d760\u00cdt4\u00f6\u00c3~^"
    "\u00aby:\u00c8d1<Q\u00db\u00dd\u00fabUu\u00b8C\u00c1\u00c2\u00a7*4P\u00da\u00a9-\u00e1\u00a9\u00be@T6Dl"
    "\u00b1\u00d2W\u00e2uz\u00c5m4G\u00d0\u00f3\u00d8$=\u00cdg,\u00a5Q\u00eb\u00aeiKE\u00dfr\u00a1\u00d760\u00cdt4"
    "\u00f6\u00c3~^\u00aby:\u00c8d"
)


def password() -> str:
    """取密码；若本地有 stringliteral.json 就顺便现核一遍（不一致直接抛，别静默用错钥匙）。"""
    pw = PASSWORD_ESCAPED
    if os.path.isfile(STRINGLITERAL_JSON):
        with io.open(STRINGLITERAL_JSON, encoding="utf-8") as f:
            table = json.load(f)
        for e in table:
            if int(e["address"], 16) == PASSWORD_LITERAL_ADDRESS:
                if e["value"] != pw:
                    raise SystemExit(
                        "密码自检失败：stringliteral.json[0x%X] 与脚本内常量不一致（长度 %d vs %d）"
                        % (PASSWORD_LITERAL_ADDRESS, len(e["value"]), len(pw))
                    )
                break
        else:
            print("[warn] stringliteral.json 里没有 0x%X 这一条，跳过现核" % PASSWORD_LITERAL_ADDRESS,
                  file=sys.stderr)
    return pw


def decode(raw: bytes, pw: str) -> bytes:
    """逐字节 XOR 还原（与 DecryptData/DoByteSwap 的循环等价）。"""
    n = len(pw)
    if n == 0:
        raise ValueError("password 为空")
    out = bytearray(len(raw))
    for i, b in enumerate(raw):
        key = 0x17 if (i & 1) == 0 else 0xCD
        out[i] = b ^ ((key * i) & 0xFF) ^ (ord(pw[i % n]) & 0xFF)
    return bytes(out)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="inp", default=DEFAULT_BIN, help="加密的 temp .bin")
    ap.add_argument("--out", dest="out", default=None, help="把还原出的 JSON 写到这里")
    ap.add_argument("--grep", dest="grep", default=None, help="只打印 Term 含该子串的条目")
    ap.add_argument("--limit", dest="limit", type=int, default=20, help="打印条数上限（默认 20）")
    args = ap.parse_args()

    with io.open(args.inp, "rb") as f:
        raw = f.read()
    print("输入: %s  (%d 字节)" % (args.inp, len(raw)))

    pw = password()
    print("密码字面量: %d 字符 @ stringliteral.json 0x%X" % (len(pw), PASSWORD_LITERAL_ADDRESS))

    plain = decode(raw, pw)
    try:
        text = plain.decode("utf-8")
    except UnicodeDecodeError as e:
        print("解码后不是合法 UTF-8 —— 算法/密码不对: %s" % e, file=sys.stderr)
        return 2

    head = plain[:48]
    if not head.lstrip()[:1] == b"{":
        print("解码后开头不是 '{'，可疑。前 48 字节: %r" % head, file=sys.stderr)
        return 3

    src = json.loads(text)
    terms = src.get("mTerms") or []
    langs = src.get("mLanguages") or []
    names = [l.get("Name") for l in langs]
    print("UTF-8 OK（%d 字符）· JSON OK（mTerms=%d · mLanguages=%d）" % (len(text), len(terms), len(langs)))
    print("语言列: %s" % ", ".join(str(x) for x in names))

    en = names.index("English") if "English" in names else 0
    zh = names.index("Simplified Chinese") if "Simplified Chinese" in names else None
    if zh is not None:
        print("zh-CN 非空条目: %d / %d" % (sum(1 for t in terms if t["Languages"][zh]), len(terms)))

    if args.out:
        with io.open(args.out, "w", encoding="utf-8", newline="") as f:
            f.write(json.dumps(src, ensure_ascii=False, indent=1))
        print("已写出: %s" % args.out)

    print("--- 前 %d 条 ---" % args.limit)
    shown = 0
    for t in terms:
        if args.grep and args.grep not in t["Term"]:
            continue
        cols = t["Languages"]
        line = "%s | EN: %s" % (t["Term"], (cols[en] or "").replace("\n", "\\n")[:70])
        if zh is not None and cols[zh]:
            line += " | ZH: %s" % cols[zh].replace("\n", "\\n")[:40]
        print(line)
        shown += 1
        if shown >= args.limit:
            break
    if shown == 0:
        print("(没有匹配 '%s' 的条目)" % args.grep)
    return 0


if __name__ == "__main__":
    sys.exit(main())
