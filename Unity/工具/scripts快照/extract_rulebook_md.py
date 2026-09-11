#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
extract_rulebook_md.py — 规则书 PDF 转 Markdown (英文原版)
用法: d:/2/Warpforge_tools/py312/python.exe extract_rulebook_md.py <pdf> <out.md>
说明: 依赖系统 pdftotext (poppler); 页眉/页脚剔除, 编号节/附录/短行识别为标题,
      关键词表保留为代码块 (双栏布局无法转表格), 正文段落合并换行。
"""
import re
import subprocess
import sys

HEADER_RE = re.compile(r'WARPFORGE: OFFLINE RULEBOOK\s*[·-]\s*VERSION')
FOOTER_RE = re.compile(r'NOT OFFICIAL\s*[·-]\s*FAN PROJECT\s*[·-]\s*PAGE\s*\d+')
MAIN_H_RE = re.compile(r'^\d+\.\s+\S')
APP_H_RE = re.compile(r'^[A-D]\.\s+\S')
SKIP_WORDS = {'Keyword', 'Card', 'Original', 'Suggested', 'Table of Contents',
              'WARPFORGE', 'OFFLINE RULEBOOK', 'Version 1.5'}


def is_heading(line: str) -> bool:
    if MAIN_H_RE.match(line) or APP_H_RE.match(line):
        return True
    s = line.strip()
    if len(s) >= 60 or not s or s in SKIP_WORDS or s.startswith('•'):
        return False
    if s[-1] in '.!?:;,':
        return False
    if s.startswith('Keyword') and 'Effect' in s:
        return False
    return True


def main() -> int:
    pdf, out = sys.argv[1], sys.argv[2]
    raw = subprocess.run(['pdftotext', '-layout', '-enc', 'UTF-8', pdf, '-'],
                         capture_output=True).stdout.decode('utf-8')
    lines = [l.rstrip('\n').replace('\x0c', '').rstrip() for l in raw.split('\n')]
    # 剔除页眉页脚
    lines = [l for l in lines if not HEADER_RE.search(l) and not FOOTER_RE.search(l)]
    # 剔除封面块与目录区 (md 自身的标题结构即目录)
    for i, l in enumerate(lines):
        if l.strip() == 'Table of Contents':
            toc_start = i
            break
    else:
        toc_start = None
    if toc_start is not None:
        toc_end = next((i for i in range(toc_start + 1, len(lines))
                        if re.sub(r'^[^\w]+', '', lines[i].strip()) == 'D. Alternative card text'),
                       toc_start)
        del lines[toc_start:toc_end + 1]
    # 封面标题块 (WARPFORGE / OFFLINE RULEBOOK / Version 1.5)
    lines = [l for l in lines if l.strip() not in ('WARPFORGE', 'OFFLINE RULEBOOK', 'Version 1.5')]
    # 关键词表区间 -> 标记, 正文循环中在原位输出为代码块 (双栏布局保留原样)
    kw_start = kw_end = None
    for i, l in enumerate(lines):
        if l.strip().startswith('Keyword') and 'Effect' in l:
            kw_start = i
        if kw_start is not None and kw_start < i and MAIN_H_RE.match(l.strip()):
            kw_end = i
            break
    if kw_start is not None:
        kw_block = lines[kw_start:kw_end]
        body = lines[:kw_start] + ['__KEYWORD_TABLE__'] + lines[kw_end:]
    else:
        kw_block, body = [], lines
    # 正文: 段落合并 + 标题识别 (关键词表标记在原位展开)
    out_lines: list[str] = []
    i = 0
    para: list[str] = []
    for l in body:
        s = l.strip()
        if s == '__KEYWORD_TABLE__':
            if para:
                out_lines.append(' '.join(para))
                para = []
            out_lines.append('## Keywords (原表布局, 双栏无法转表格)')
            out_lines.append('```')
            out_lines.extend(kw_block)
            out_lines.append('```')
            continue
        if not s:
            continue
        if is_heading(l):
            if para:
                out_lines.append(' '.join(para))
                para = []
            out_lines.append(('## ' if MAIN_H_RE.match(s) or APP_H_RE.match(s) else '### ') + s)
            continue
        if s.startswith('•'):
            if para:
                out_lines.append(' '.join(para))
                para = []
            bullet = s.lstrip('•\u200b ').strip()
            out_lines.append('- ' + bullet)
            continue
        para.append(s)
    if para:
        out_lines.append(' '.join(para))
    with open(out, 'w', encoding='utf-8') as f:
        f.write('# Warpforge Offline Rulebook 1.5 (English)\n\n> 从官方离线规则书 PDF 提取的英文原版, 用于与中文翻译对照。\n\n')
        f.write('\n\n'.join(out_lines))
        f.write('\n')
    print(f'写出: {out} ({len(out_lines)} 段)')
    return 0


if __name__ == '__main__':
    sys.exit(main())
