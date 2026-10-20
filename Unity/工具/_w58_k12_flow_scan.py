#!/usr/bin/env python
# W58-K12 step 2: find ALL reads of RawCardScript.cardShortId (+0x40)
# Approach: (a) direct param/this +0x40 in methods typed as RawCardScript;
#           (b) locals loaded from RawCardScript-typed fields (+0x40 on the local);
#           (c) locals assigned from RawCardScript-returning getters (+0x40 on the local).
import io, os, re, collections, glob

DUMP = r"d:/2/tools/il2cpp_out/dump.cs"
DEC  = r"d:/2/tools/decomp_full"

def read(p):
    return io.open(p, "r", encoding="utf-8", errors="replace").read().splitlines()

lines = read(DUMP)

cls_re = re.compile(r'^(?:public |private |internal |protected )?(?:static |sealed |abstract )*(?:class|struct|interface|enum)\s+([A-Za-z0-9_\.<>`]+)')
field_re = re.compile(r'^\s*(?:public|private|internal|protected)\s+(?:static\s+)?(?:readonly\s+)?RawCardScript\s+([A-Za-z0-9_<>]+)\s*;\s*//\s*0x([0-9A-Fa-f]+)')
meth_re = re.compile(r'^\s*(?:public|private|internal|protected|static|virtual|override|abstract|sealed|extern|new|\s)*([A-Za-z0-9_<>\.`]+)\s*\((.*)\)\s*\{\s*\}\s*$')

cur = None
raw_fields = collections.defaultdict(dict)   # class -> {offset:int -> field}
raw_methods = collections.defaultdict(list)  # class -> [(name, sig)]
ret_raw = []                                 # (class, name, sig) returning RawCardScript
for i, ln in enumerate(lines, 1):
    m = cls_re.match(ln)
    if m:
        cur = m.group(1)
        continue
    if not cur:
        continue
    fm = field_re.search(ln)
    if fm:
        raw_fields[cur][int(fm.group(2), 16)] = fm.group(1)
        continue
    s = ln.strip()
    if s.endswith("{ }") and "(" in s and not s.startswith("//"):
        nm = re.search(r'([A-Za-z0-9_<>\.`]+)\s*\(', s)
        if nm:
            raw_methods[cur].append((nm.group(1), s, i))
            # return type detection
            head = s[:nm.start()]
            if "RawCardScript" in head:
                ret_raw.append((cur, nm.group(1), s, i))

print("classes with RawCardScript-typed fields:", len(raw_fields))
for c, d in raw_fields.items():
    print("   ", c, {hex(k): v for k, v in d.items()})
print("methods returning RawCardScript:", [(c, n) for c, n, s, i in ret_raw])

# ---- index decomp files ----
files = sorted(os.listdir(DEC))
def files_for(cls, name):
    c1 = cls.replace("+", ".")
    cands = [c1 + "__" + name + ".c", c1.replace(".", "__") + "__" + name + ".c",
             c1.split(".")[-1] + "__" + name + ".c"]
    return [c for c in cands if c in files]

# ---- build getter-name list (decomp symbol suffix) ----
getter_names = set()
for c, n, s, i in ret_raw:
    getter_names.add(n)
getter_names |= {"get_rawCardData", "get_OriginalCard", "get_referencedBaseCard", "GetTalentCard", "get_currentAlternateArt"}

# ---- scan all decomp files ----
pat40 = re.compile(r'\+\s*0x40\b')
pat_ld = re.compile(r'^\s*(\w+)\s*=\s*(.+)$')
findings = []
scanned = 0
for fn in files:
    if not fn.endswith(".c"):
        continue
    scanned += 1
    body = read(os.path.join(DEC, fn))
    cls = fn.split("__")[0]
    offs = raw_fields.get(cls, {})
    # track locals that hold a RawCardScript
    rawloc = set()
    hits = []
    for j, ln in enumerate(body, 1):
        # local loaded from typed field on this
        if offs:
            for off, fname in offs.items():
                hexs = "0x%x" % off
                if ("param_1 + " + hexs) in ln or ("param_1 + 0x%X" % off) in ln:
                    lm = pat_ld.match(ln)
                    if lm:
                        rawloc.add(lm.group(1))
        # local assigned from RawCardScript-returning method
        for g in getter_names:
            if g + "(" in ln:
                lm = pat_ld.match(ln)
                if lm:
                    rawloc.add(lm.group(1))
                break
        # +0x40 hit
        if pat40.search(ln):
            base40 = None
            m2 = re.search(r'\*\s*\(\s*(?:int|uint|longlong|undefined8|undefined4|char|undefined1)\s*\*\s*\)\s*\(\s*(\w+)\s*\+\s*0x40', ln)
            if m2:
                base40 = m2.group(1)
            hits.append((j, ln.strip(), base40))
    for j, ln, b in hits:
        if b and b in rawloc:
            findings.append((fn, j, "LOCAL-FROM-RAWCARD-FIELD", ln))

print("scanned files:", scanned)
print("findings via RawCardScript-typed field/getter locals:", len(findings))
for f in findings:
    print("HIT %s:%d [%s] %s" % f)

# also report any local that got +0x40 & might be RawCardScript but unresolved
print("---- unresolved locals with +0x40 (sample per file, for manual check) ----")
cnt = 0
for fn in files:
    if not fn.endswith(".c"): continue
    body = read(os.path.join(DEC, fn))
    cls = fn.split("__")[0]
    offs = raw_fields.get(cls, {})
    if not offs: continue
    rawloc = set()
    for j, ln in enumerate(body, 1):
        for off in offs:
            if ("param_1 + 0x%x" % off) in ln:
                lm = pat_ld.match(ln)
                if lm: rawloc.add(lm.group(1))
        if pat40.search(ln):
            m2 = re.search(r'\*\s*\(\s*(?:int|uint|longlong|undefined8|undefined4|char|undefined1)\s*\*\s*\)\s*\(\s*(\w+)\s*\+\s*0x40', ln)
            if m2 and rawloc and m2.group(1) not in rawloc:
                print("MAYBE %s:%d %s" % (fn, j, ln.strip()[:150])); cnt += 1
                if cnt > 40: break
    if cnt > 40: break
