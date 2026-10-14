# -*- coding: utf-8 -*-
"""收口 · 重钉 ruleprobe 的两个指纹（按 `ruleprobe/README.md:847-850` 的三步）：
 ① `PinnedPoolMd5`  ← 卡池变了（本会话改了 6 张卡的 `desc`/`descZh`）
 ② `PinnedParseMd5` ← 解析输出变了（解析差异 **6 行**，前 6 张 = `BL34`/`GOF18`/`SOR29`/`SOR9`/`TAU2`/`TL_Psychophage`，**一张不多不少**）
 ③ `PinnedBaselineMd5` **不动**（基线文件现读 md5 与它逐字符相同）
用法：python -I -X utf8 d:/4/Unity/工具/_repin_1014.py
"""
import io

P = r'D:\4\Unity\工具\ruleprobe\Program.cs'
POOL_OLD = 'public const string PinnedPoolMd5 = "9ab791869c375210bd5eb8376aa37b75";'
POOL_NEW = 'public const string PinnedPoolMd5 = "37f349cbe7aa79f41c34181eba6fd5eb";'
PARSE_OLD = 'public const string PinnedParseMd5 = "e3a4b0820b1ff2f274b208a0732b5e88";'
PARSE_NEW = 'public const string PinnedParseMd5 = "79c93e35242f09ab3a8dca0c60a1275e";'

NOTE = """    // 🔴 **2026-10-14（第三十会话）有意重钉 —— 两个常量一起钉**：
    //   ① `PinnedPoolMd5`：第三十会话按 `A1987`/`A1993` 改了 **6 张卡**的卡面文字
    //      （`SOR9`/`TAU2`/`TL_Psychophage` 补 `Talent:` 行 · `GOF18`/`BL34` 补 `Talent:` 行与中文名 · `SOR29` 的 `Deal 3` → `Deal 2-3`）
    //      ⇒ 卡池 md5 必变（`9ab79186…` → `37f349cb…`）；
    //   ② `PinnedParseMd5`：`check` 现跑的「解析差异」= **6 行**，**前 6 张正是上面那 6 张、一张不多不少**
    //      ⇒ 差异**可逐条归因到那次改动**，不是解析器被改坏 ⇒ 有意重钉（`e3a4b082…` → `79c93e35…`）；
    //   ③ `PinnedBaselineMd5` **本趟不动** —— 基线文件 `基线/out_baseline.txt` 现读 md5 = `6c0e41d71599bd7a42f078a50e90f614`，与它**逐字符相同**。
    //   ⛔ **这不是「把红抹平」**：与 `A1943` 那次重钉吞掉 1118 行漂移不同 —— 本趟的差异**只有 6 行、且能点名列出来**。
"""


def main():
    b = io.open(P, 'rb').read()
    crlf = b.count(b'\r\n')
    print('  Program.cs 行尾：CRLF=%d / LF=%d' % (crlf, b.count(b'\n')))
    assert b.count(b'\x00') == 0
    t = b.decode('utf-8')
    assert t.count(POOL_OLD) == 1, '池 md5 锚点命中 %d' % t.count(POOL_OLD)
    assert t.count(PARSE_OLD) == 1, '解析 md5 锚点命中 %d' % t.count(PARSE_OLD)
    assert POOL_NEW not in t and PARSE_NEW not in t, '已经钉过'
    t = t.replace(POOL_OLD, NOTE + POOL_NEW).replace(PARSE_OLD, PARSE_NEW)
    out = t.encode('utf-8')
    io.open(P, 'wb').write(out)
    print('OK 重钉完成（%d -> %d 字节）' % (len(b), len(out)))


if __name__ == '__main__':
    main()
