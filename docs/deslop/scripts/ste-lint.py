import re, sys, json, glob, os

# English score_version 2 is unchanged. --zh (or auto on Chinese prose) is a
# local zh-Hans pass: violations per 100 Han characters. It does not enter
# the English total.

# Score v2: adds complex_tense (perfect tenses, modal stacks), exempts
# adjectival/stative participles from the passive count, moves "provide" to the
# banned list, adds a noun-train marker and a --strict mode. The episode's
# published numbers were measured with score v1 (this file's git history at the
# episode date); v1 and v2 totals are close but not directly comparable.
SCORE_VERSION = 2

# Shape v1: the Layer 2 checks (skill 2.0). These count the ORDER of a reply to
# a person, not the words. They are reported apart from the score and never
# enter `total`, so `total_per100w` keeps one meaning across skill versions.
SHAPE_VERSION = 1

MARKETING = ["seamless","seamlessly","robust","powerful","cutting-edge","effortless","effortlessly",
    "world-class","next-generation","revolutionary","blazing","lightning-fast","elegant","delightful",
    "turnkey","best-in-class","state-of-the-art","game-changing","first-class","battle-tested",
    "enterprise-grade","supercharge","unlock","unleash","empower","empowers"]
BANNED = ["begin","begins","commence","commences","initiate","initiates","originate",
    "utilize","utilizes","utilizing","leverage","leverages","leveraging","facilitate","facilitates",
    "ensure","ensures","ensuring","prior to","subsequent to","obtain","obtains","acquire","acquires",
    "demonstrate","demonstrates","additionally","furthermore","moreover","comprehensive","comprehensively",
    "utilization","aforementioned","henceforth","therein","whilst","amongst","numerous","myriad","plethora",
    "provide","provides","provided",
    "in order to","a variety of","in the event that","due to the fact that","it is important to note"]
# STE's own recurring-errors list (see ste-recurring-errors.md). Counted only
# with --strict: these are correct STE but would flag normal prose in docs.
STRICT_BANNED = ["however","since","should","shall","using","follow","follows","followed"]
PHRASAL = ["spin up","spin down","reach out","dive into","dives into","diving into","kick off","kicks off",
    "roll out","rolls out","tear down","ramp up","circle back","drill down","spun up","reaching out"]
MODAL_HEDGE = ["it is important to note","it should be noted","it is worth noting","please note that",
    "as mentioned","as noted above"]
BE = r"(?:am|is|are|was|were|be|been|being)"
PP_IRREG = r"(?:done|made|sent|read|built|kept|held|set|put|run|written|shown|given|taken|found|got|gotten|seen|known|thrown|drawn)"
# Rule 3.3: a past participle used as an adjective is not passive. These
# stative participles only count as passive when a by-agent follows.
STATIVE = r"(?:closed|opened?|damaged|completed?|installed|connected|required|expected|configured|enabled|disabled|deprecated|supported)"
FUNC_WORDS = set("""a an the this that these those of for to in on at by with from as and or but if
when then than not no is are was were be been being am do does did has have had will would can could
may might must should shall it its their your our his her they we you i""".split())

# --- Layer 2 (shape) word lists -------------------------------------------
# An opener is counted in the first sentence only. Everything else is counted
# over the whole text.
OPENERS = ["great question","good question","let me","i'll","i will","sure","certainly",
    "absolutely","looking at your","to answer your question","happy to help",
    "i'd be happy to","that's a great","first,","alright","okay so","let's"]
CLOSERS = ["let me know if","hope this helps","hope that helps","happy to clarify",
    "feel free to ask","feel free to reach","anything else","if you have any questions",
    "if you have any other questions","i'm here to help","do not hesitate","don't hesitate"]
RECAP = ["to summarize","in summary","to recap","as a recap","in conclusion","to sum up",
    "i have now","i've now","overall,"]
# Only hedges that carry no fact. A qualifier that bounds a claim is not here.
HEDGE = ["perhaps","arguably","presumably","conceivably","ostensibly","somewhat",
    "kind of","sort of","more or less","it seems that","it appears that","fairly straightforward"]
VAGUE = ["some work","a bit of work","a while","shortly","fairly soon","a few moments",
    "not too long","some time","in no time","quite quick","pretty quick"]
SOFTENER = ["uh oh","oh no","there seems to be a problem","there seems to be an issue",
    "unfortunately","i apologize","apologies","my apologies","sorry about that"]

def strip_code(t):
    t = re.sub(r"```.*?```", "\n\n", t, flags=re.S)
    t = re.sub(r"`[^`]*`", " ", t)
    return t


def strip_front_matter(text):
    return re.sub(r"\A\ufeff?---[ \t]*\r?\n.*?^(?:---|\.\.\.)[ \t]*(?:\r?\n|\Z)",
                  "", text, count=1, flags=re.S | re.M)


def strip_emphasis(text):
    pattern = r"(?<![\\\w])(\*{1,3}|_{1,3})(?=[^\s*_])(.+?)(?<=[^\s\\*_])\1(?!\w)"
    while True:
        stripped = re.sub(pattern, r"\2", text, flags=re.S)
        if stripped == text:
            return text
        text = stripped


def prose_blocks(text):
    blocks = []
    pending = []
    quote_depth = 0

    def flush():
        if pending:
            blocks.append(" ".join(pending))
            pending.clear()

    for line in strip_emphasis(text).splitlines():
        s = line.strip()
        quote = re.match(r"^(?:>[ \t]*)+", s)
        depth = quote.group().count(">") if quote else 0
        if depth != quote_depth:
            flush()
            quote_depth = depth
        if quote:
            s = s[quote.end():]
        if not s or re.fullmatch(r"(?:[-*_][ \t]*){3,}|=+", s):
            flush()
            continue
        heading = re.match(r"^#{1,6}\s+", s)
        item = re.match(r"^(?:[-*+]|\d+[.)])\s+", s)
        if heading or s.startswith("|"):
            flush()
            blocks.append(s[heading.end():] if heading else s)
        else:
            if item:
                flush()
                s = s[item.end():]
            pending.append(s)
    flush()
    return blocks


def paragraphs(raw):
    """Split into paragraphs AFTER the fenced blocks are gone.

    A code block that holds a blank line used to be split first, which left two
    fragments with one fence each. strip_code matches a PAIR, so it removed
    neither, and every line of code then counted as a sentence. A config block
    with 9 blank lines scored 9 long paragraphs against 50 words of prose.
    A fence becomes a paragraph break, so the real paragraphs stay apart.
    """
    t = re.sub(r"```.*?```", "\n\n", raw, flags=re.S)
    t = re.sub(r"^[ \t]*```.*$", "\n", t, flags=re.M)   # a fence with no partner
    t = re.sub(r"`[^`]*`", " ", t)
    return [p for p in re.split(r"\n\s*\n", t) if p.strip()]

def sentences(text):
    out = []
    for s in prose_blocks(text):
        parts = re.split(r"(?<=[.!?:])\s+(?=[A-Z0-9\"'\-])", s)
        for p in parts:
            p = p.strip()
            if p: out.append(p)
    return out

def wc(s):
    return len([w for w in re.findall(r"[A-Za-z0-9][A-Za-z0-9'\-/]*", s)])

def count_ci(text, phrases):
    n = 0; hits = []
    low = text.lower()
    for ph in phrases:
        for m in re.finditer(r"(?<![a-z])" + re.escape(ph) + r"(?![a-z])", low):
            n += 1; hits.append(ph)
    return n, hits

def noun_trains(text):
    """Runs of 4+ consecutive non-function lowercase words (Rule 2.1 proxy).
    Heuristic marker only - proper nouns break a run, the leading word of each
    sentence is skipped, and the count stays out of the total."""
    hits = []
    for s in sentences(text):
        words = re.findall(r"[A-Za-z][A-Za-z'\-]*", s)[1:]
        run = []
        for w in words + [""]:
            if w and w.lower() not in FUNC_WORDS and not w[0].isupper():
                run.append(w)
            else:
                if len(run) >= 4: hits.append(" ".join(run))
                run = []
    return hits

def count_in(fragment, phrases):
    """Presence count over one fragment - each phrase counts at most one time."""
    low = fragment.lower().replace("’", "'")
    n = 0; hits = []
    for ph in phrases:
        if re.search(r"(?<![a-z])" + re.escape(ph) + r"(?![a-z])", low):
            n += 1; hits.append(ph)
    return n, hits


def long_lists(raw):
    """Runs of more than five consecutive list items (Layer 2 cap). A table row
    starts with a pipe, so a reference table never counts."""
    n = 0; run = 0
    for line in raw.split("\n"):
        if re.match(r"^\s*(?:[-*+]|\d+[.)])\s+", line):
            run += 1
        elif not line.strip():
            continue
        else:
            if run > 5: n += 1
            run = 0
    if run > 5: n += 1
    return n


def shape(raw):
    """Layer 2 counts. Applies to a reply to a person, not to a reference doc."""
    text = "\n\n".join(prose_blocks(strip_code(raw))).replace("’", "'")
    sents = sentences(text)
    s = {}
    s["preamble_opener"] = count_in(sents[0], OPENERS)[0] if sents else 0
    s["closer"], ch = count_ci(text, CLOSERS)
    s["recap_marker"], rh = count_ci(text, RECAP)
    s["hedge"], hh = count_ci(text, HEDGE)
    s["vague_estimate"], vh = count_ci(text, VAGUE)
    s["error_softener"], sh = count_ci(text, SOFTENER)
    s["action_list(>5)"] = long_lists(re.sub(r"```.*?```", "\n", raw, flags=re.S))
    return s, list(dict.fromkeys(ch + rh + hh + vh + sh))[:6]


# --- zh-Hans ---------------------------------------------------------------
# High-precision slop only. Ambiguous technical terms (闭环, 对齐, 落地) stay
# out of the linter; the skill tells the writer when to keep them.
# Corpus frames from MystiaExtensionFramework/docs, plus generic AI openers.
# Do not add: 落地 对齐 闭环 进行时 本文 提供 保证 即可 正是 意味着 必须 不得.
ZH_PHRASES = [
    "值得注意的是", "需要指出的是", "需要注意的是", "众所周知", "不难发现", "显而易见",
    "综上所述", "总而言之", "总的来说", "与此同时", "除此之外", "不仅如此", "更重要的是",
    "本文将", "本文旨在", "本指南将", "本文件旨在", "在当今",
    "希望对您有所帮助", "希望对你有所帮助", "希望本文对你有所帮助",
    "如有疑问", "欢迎交流指正", "让我们拭目以待",
    "也就是说", "换句话说", "换言之",
    "顺带一提", "顺带发现", "顺带一条", "顺带纠正", "顺带的收益", "顺带要删", "顺带一个",
    "顺便", "目的是", "之类",
    # English calques. Compounds only; 面/红/族/壳 alone stay unflagged.
    "投机性", "账本", "守卫", "裸写", "裸循环", "裸异常", "转发裸",
    "钉成", "钉死", "钉住", "控制族", "对话族", "回调族", "枚举家族",
    "工厂", "铸", "唯一通路", "通路", "就会红", "立刻红", "红起来", "全程绿", "转绿",
    "真偏离", "偏离", "放宽", "加宽", "漂移", "产出方", "消费者", "喂给", "假构建", "押着",
    "判据", "启动期致命", "便宜的就绪", "薄壳", "托管壳", "大声失败", "响亮拒绝",
    "类型化拒绝", "手拼", "歪斜", "归宿", "口径", "兜底", "咽喉", "跳板", "写穿",
    "默认体", "公共面", "注入面", "创作面", "冻结面", "内部面", "空转", "下沉",
    "投标", "不发明", "懒建", "心智模型", "撒谎", "白拖", "可达域", "有效域",
    "常驻层", "不透支", "测试缝", "后写覆盖", "镜像",
    "赋能", "抓手", "颗粒度", "底层逻辑", "顶层设计", "组合拳",
    "全方位", "一站式", "无缝", "极致", "助力", "深度融合", "深度赋能",
]
ZH_STRICT = ["应该", "应当", "或许", "尽量", "最好"]
ZH_REGEX = [
    (r"随着[^。！？\n]{0,16}的(?:快速)?发展", "随着…的发展"),
    (r"进行(?:了)?(?:详细|深入|全面|进一步|有效|系统|相应)?(?:地|的)?(?:分析|优化|检查|处理|探讨|研究|总结|梳理|评估|测试|配置|部署|验证|确认|说明|介绍|讨论)", "进行+动作"),
    (r"(?:可能可以|或许能够|应该可以|不妨可以)", "modal_stack"),
    (r"显著(?:提升|提高|改善|增强)(?!了?\s*[\d０-９])", "显著提升"),
    (r"(?:用|以)[^。！？\n]{0,24}的方式", "的方式"),
]
ZH_LONG = 40


def han_stats(text):
    prose = strip_code(strip_front_matter(text))
    han = len(re.findall(r"[\u4e00-\u9fff]", prose))
    latin = len(re.findall(r"[A-Za-z]{2,}", prose))
    return han, latin


def is_zh(text):
    han, latin = han_stats(text)
    return han >= 40 and han >= latin * 2


def zh_prose(raw):
    lines = []
    for line in strip_code(raw).splitlines():
        s = re.sub(r"^(?:>\s*)+", "", line.strip())
        s = re.sub(r"^#{1,6}\s+", "", s)
        s = re.sub(r"^(?:[-*+]|\d+[.)、])\s+", "", s)
        if not s or re.fullmatch(r"(?:[-*_][ \t]*){3,}", s):
            lines.append("")
            continue
        if s.startswith("|"):
            s = s.replace("|", " ")
        lines.append(s)
    return "\n".join(lines)


def zh_sentences(text):
    parts = re.split(r"[。！？!?；;\n]+", text)
    return [p.strip() for p in parts if re.search(r"[\u4e00-\u9fff]", p)]


def take_spans(text, phrases, regexes):
    occupied = [False] * len(text)
    hits = []

    def take(i, end, label):
        if end <= i or any(occupied[i:end]):
            return
        for k in range(i, end):
            occupied[k] = True
        hits.append(label)

    for pattern, label in regexes:
        for m in re.finditer(pattern, text):
            take(m.start(), m.end(), label)
    for ph in sorted(set(phrases), key=len, reverse=True):
        start = 0
        while True:
            i = text.find(ph, start)
            if i < 0:
                break
            take(i, i + len(ph), ph)
            start = i + len(ph)
    return hits


def em_dash_count(text):
    doubles = text.count("——")
    rest = text.replace("——", "")
    return doubles + rest.count("—") + rest.count("–")


def lint_zh(raw, strict=False):
    prose = zh_prose(raw)
    sents = zh_sentences(prose)
    chars = len(re.findall(r"[\u4e00-\u9fff]", prose)) or 1
    phrases = list(ZH_PHRASES)
    if strict:
        phrases += ZH_STRICT
    hits = take_spans(prose, phrases, ZH_REGEX)
    longs = []
    for s in sents:
        n = len(re.findall(r"[\u4e00-\u9fff]", s))
        if n > ZH_LONG:
            longs.append((n, s))
    paras = [p for p in re.split(r"\n\s*\n", prose) if re.search(r"[\u4e00-\u9fff]", p)]
    v = {
        "long_sentence(>40c)": len(longs),
        "semicolon": prose.count("；") + prose.count(";"),
        "banned_word": len(hits),
        "long_paragraph(>6s)": sum(1 for p in paras if len(zh_sentences(p)) > 6),
        "em_dash": em_dash_count(prose),
    }
    if strict:
        v["strict_banned_word"] = sum(hits.count(p) for p in ZH_STRICT)
        # strict phrases are already inside banned_word; do not count them twice
        v["banned_word"] -= v["strict_banned_word"]
    total = sum(v.values())
    per = round(total * 100.0 / chars, 2)
    return {
        "score_version": SCORE_VERSION,
        "shape_version": SHAPE_VERSION,
        "mode": "zh-hans-strict" if strict else "zh-hans",
        "words": chars, "sentences": len(sents),
        "violations": v, "total": total,
        "total_per100w": per,
        "total_per100c": per,
        "shape": {}, "shape_total": 0, "sample_shape": [],
        "em_dash(slop-marker)": v["em_dash"],
        "noun_train(>=4w,marker)": 0,
        "longest_sentence_words": max((n for n, _ in longs), default=0),
        "sample_marketing": [],
        "sample_banned": list(dict.fromkeys(hits))[:8],
        "sample_noun_train": [],
        "sample_long_sentence": ["{}c: {}".format(n, s[:40]) for n, s in
                                 sorted(longs, key=lambda x: -x[0])[:2]],
    }


def lint(text, strict=False, zh=False):
    han, _ = han_stats(text)
    # --zh on a file with no Chinese prose stays on the English scorer.
    if is_zh(text) or (zh and han >= 8):
        return lint_zh(strip_front_matter(text), strict=strict)
    raw = strip_front_matter(text)
    text = "\n\n".join(prose_blocks(strip_code(raw)))
    sents = sentences(text)
    words = sum(wc(s) for s in sents) or 1
    v = {}
    longs = [(wc(s), s) for s in sents if wc(s) > 20]
    v["long_sentence(>20w)"] = len(longs)
    v["semicolon"] = text.count(";")
    v["contraction"] = len(re.findall(
        r"\b(?:\w+['’](?:t|re|ve|ll|d|m)|(?:it|he|she|that|what|who|where|when|why|how|there|here|let)['’]s)\b",
        text, re.I))
    passive_parts = re.findall(rf"\b{BE}\s+(\w+ed|{PP_IRREG})\b", text, re.I)
    v["passive_voice"] = sum(1 for p in passive_parts if not re.fullmatch(STATIVE, p, re.I)) \
        + len(re.findall(rf"\b{BE}\s+{STATIVE}\s+by\b", text, re.I))
    v["complex_tense"] = len(re.findall(
        rf"\b(?:(?:may|might|could|would|should|must|will|shall|can)\s+)?(?:have|has|had)\s+(?:been\s+)?(?:\w+ed|{PP_IRREG})\b",
        text, re.I))
    v["ing_main_verb"] = len(re.findall(rf"\b{BE}\s+\w+ing\b", text, re.I))
    v["nominalization"] = len(re.findall(r"\b(?:perform(?:s|ed)?|conduct(?:s|ed)?|carry out|carries out|make use of|makes use of)\b", text, re.I)) + len(re.findall(r"\b\w{4,}(?:tion|ment|ance|ence)\s+of\b", text, re.I))
    v["phrasal_verb"], _ = count_ci(text, PHRASAL)
    v["banned_word"], bh = count_ci(text, BANNED)
    v["marketing_adjective"], mh = count_ci(text, MARKETING)
    v["modal_hedge"], _ = count_ci(text, MODAL_HEDGE)
    v["long_paragraph(>6s)"] = sum(1 for p in paragraphs(raw) if len(sentences(p)) > 6)
    em = raw.count("—") + raw.count("–")
    trains = noun_trains(text)
    if strict:
        n_strict, sh = count_ci(text, STRICT_BANNED)
        # "may" is matched case-sensitively so the month "May" stays clean
        n_strict += len(re.findall(r"(?<![A-Za-z])may(?![a-z])", text))
        v["strict_banned_word"] = n_strict
        v["em_dash"] = em
    total = sum(v.values())
    sh, sh_samples = shape(raw)
    return {
        "score_version": SCORE_VERSION,
        "shape_version": SHAPE_VERSION,
        "mode": "strict" if strict else "flavored",
        "words": words, "sentences": len(sents),
        "violations": v, "total": total,
        "total_per100w": round(total*100.0/words, 2),
        "shape": sh, "shape_total": sum(sh.values()),
        "sample_shape": sh_samples,
        "em_dash(slop-marker)": em,
        "noun_train(>=4w,marker)": len(trains),
        "longest_sentence_words": (max(longs)[0] if longs else max((wc(s) for s in sents), default=0)),
        "sample_marketing": list(dict.fromkeys(mh))[:6],
        "sample_banned": list(dict.fromkeys(bh))[:6],
        "sample_noun_train": trains[:3],
        "sample_long_sentence": ["{}w: {}".format(n, s[:70]) for n, s in
                                 sorted(longs, key=lambda x: -x[0])[:2]],
    }

if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            try:
                stream.reconfigure(encoding="utf-8", errors="replace")
            except Exception:
                pass
    args = sys.argv[1:]
    strict = "--strict" in args
    zh = "--zh" in args
    as_json = "--json" in args
    show_shape = "--shape" in args
    fail_over = None
    fail_shape = None
    for flag in ("--fail-over", "--fail-shape"):
        if flag in args:
            i = args.index(flag)
            value = float(args[i + 1])
            if flag == "--fail-over": fail_over = value
            else: fail_shape = value
            del args[i:i + 2]
    files = [a for a in args if a not in ("--strict", "--json", "--shape", "--zh")]
    worst = 0.0
    worst_shape = 0
    read_error = False
    if not files:
        sys.stdin.reconfigure(encoding="utf-8")
        r = lint(sys.stdin.read(), strict=strict, zh=zh)
        print(json.dumps(r, indent=2, ensure_ascii=False))
        worst = r["total_per100w"]
        worst_shape = r["shape_total"]
    else:
        exp = []
        for f in files:
            if any(c in f for c in "*?["):
                matches = sorted(glob.glob(f))
                if not matches:
                    print(f"ste-lint: {f}: No files match the pattern.", file=sys.stderr)
                    read_error = True
                exp += matches
            else:
                exp.append(f)
        for f in exp:
            try:
                with open(f, encoding="utf-8") as fh:
                    source = fh.read()
            except (OSError, UnicodeError) as error:
                print(f"ste-lint: {f}: {error}", file=sys.stderr)
                read_error = True
                continue
            r = lint(source, strict=strict, zh=zh)
            worst = max(worst, r["total_per100w"])
            worst_shape = max(worst_shape, r["shape_total"])
            if as_json:
                print(json.dumps({"file": f, **r}, indent=2, ensure_ascii=False))
            else:
                tail = f" shape={r['shape_total']:2d}" if show_shape else ""
                if str(r.get("mode", "")).startswith("zh-hans"):
                    unit, score = "chars", "per100c"
                else:
                    unit, score = "words", "per100w"
                print(f"{os.path.basename(f):32} {unit}={r['words']:4d} total={r['total']:3d} {score}={r['total_per100w']:6.2f} em_dash={r['em_dash(slop-marker)']:2d}{tail}")
    if read_error or (fail_over is not None and worst > fail_over):
        sys.exit(1)
    if fail_shape is not None and worst_shape > fail_shape:
        sys.exit(1)
