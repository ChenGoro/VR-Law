# Debug Dynamic Texts – Diagnosis Report

Summary of `[DebugDynamicTexts]` logs: which texts lose newlines/tabs, and where punctuation/numbers get extra spaces.

---

## 1. Root cause: `useFullTextLayout=False`

When **TIMINGS** shows `useFullTextLayout=False`, the karaoke highlighter could not match every word from the JSON `words` array to the JSON `text` string. It then **falls back** to building the line from the word list with **only spaces** between words. In that path:

- **Newlines are lost** → FINAL TMP has `Has \n: False`
- **Tabs are lost** → FINAL TMP has `Has \t: False`
- Paragraph breaks and indentation disappear; text becomes one long line.

When `useFullTextLayout=True`, the highlighter uses the full `text` and only wraps each word in color tags. Newlines are kept; tabs are turned into 4 spaces (so FINAL still has `Has \t: False` by design).

---

## 2. LegalScenario (Description) – all scenarios

| Scenario | LOADED     | JSON text  | useFullTextLayout | FINAL TMP    | Problem |
|----------|------------|------------|-------------------|--------------|---------|
| 01 (convenience store) | \n \t | \n \t | **False** | No \n, no \t | Newlines and tabs lost |
| 02 (deli)              | \n \t | \n \t | **False** | No \n, no \t | Newlines and tabs lost |
| 04 (rape)              | \n \t | \n \t | **False** | No \n, no \t | Newlines and tabs lost |

**Conclusion:** For **LegalScenario (Description)** the word list in the wordtimes JSON does not match the `text` exactly (e.g. comma in `"victim,"` in `words` vs `"victim "` or `"victim"` in `text`). So every Description block hits the fallback and loses formatting.

**Fix:** Regenerate or edit the Description wordtimes JSON so that each entry in `words` appears exactly in `text` in order (or rely on the existing “trim trailing punctuation” matching and fix any remaining mismatches).

---

## 3. AttorneyStatement – varies by scenario

| Scenario / run | LOADED | JSON text | useFullTextLayout | FINAL TMP   | Problem |
|----------------|--------|-----------|-------------------|-------------|---------|
| 02 (261 chars) | \n \t  | \n \t     | **True**          | \n kept, \t→spaces | OK |
| 01 (239 chars) | \n \t  | \n \t     | **False**         | No \n, no \t | Newlines and tabs lost |
| 04 (326 chars) | \n \t  | \n \t     | **True**          | \n kept     | OK |

**Conclusion:** When AttorneyStatement wordtimes have a single word that doesn’t match `text` (e.g. punctuation or tokenization difference), `useFullTextLayout` becomes False and that scenario’s attorney statement loses newlines/tabs.

---

## 4. ProsecutorStatement – varies by scenario

| Scenario / run | LOADED | JSON text | useFullTextLayout | FINAL TMP   | Problem |
|----------------|--------|-----------|-------------------|-------------|---------|
| 02 (209 chars) | \n \t  | \n \t     | **False**         | No \n, no \t | Lost |
| 04 (304 chars) | \n \t  | \n \t     | **False**         | No \n, no \t | Lost |
| 01 (249 chars) | \n \t  | \n \t     | **True**          | \n kept     | OK |

Same pattern as AttorneyStatement: one mismatched word in the JSON causes fallback and loss of formatting.

---

## 5. Punctuation and numbers – extra or wrong spaces

From the FINAL TMP strings in the logs:

- **Numbers:** Examples like `$82</color> <color=#FFFFFF>,000.</color>` show a **space before the comma** in amounts. That happens when the wordtimes JSON has separate tokens for `"$82"` and `",000."` (or `",000"`). The highlighter inserts a space between consecutive words, so you get `$82 ,000.` instead of `$82,000.`.
- **Hyphenated words:** If the JSON has `"break"` and `"-in"` as two words, the fallback path produces `break -in` (space before hyphen). When `useFullTextLayout=True`, the gap between those two words in `text` is preserved (often no space, so “break-in” can stay correct). So redundant space before `-in` is most visible when **useFullTextLayout=False** (fallback).
- **Quotes / commas:** If a word is tokenized as `"devastating,"` in `words` but the `text` has `devastating` without the comma, that mismatch can trigger fallback and also affect where spaces appear.

**Fix (data side):** In the wordtimes pipeline, either:

- Emit `words` so they match the exact substrings in `text` (including punctuation and no extra splitting of numbers), or  
- Merge number/currency tokens (e.g. one token `"$82,000."`) and keep hyphenated words as one token where the source text has no space.

**Fix (code side):** Already done: “trim trailing punctuation” when matching words so `"victim,"` can match `"victim"` in text and avoid fallback. Remaining issues are either (1) other mismatches (e.g. leading punctuation, numbers) or (2) fallback path always joining with a single space, which creates redundant spaces in numbers and hyphenated pairs when they are split into multiple words.

---

## 6. Summary table – “did this block keep newlines?”

| Block              | Scenario 01   | Scenario 02   | Scenario 03        | Scenario 04   |
|--------------------|---------------|---------------|-------------------|---------------|
| LegalScenario      | No (False)    | No (False)    | *(not in run)*    | No (False)    |
| AttorneyStatement  | No (False)    | Yes (True)    | *(not in run)*    | Yes (True)    |
| ProsecutorStatement| Yes (True)    | No (False)    | *(not in run)*    | No (False)    |

Scenario 03 was not played in the captured debug run, so its useFullTextLayout/FINAL state is unknown. Run with scenario 03 and enable DebugLogDynamicTexts to fill this in.

**Texts that did not load spaces and newlines (FINAL has no \n):**  
All LegalScenario descriptions in the sampled runs; AttorneyStatement when useFullTextLayout=False (e.g. scenario 01); ProsecutorStatement when useFullTextLayout=False (e.g. scenarios 02 and 04).

**Punctuation/number issues:**  
- Extra space in numbers (e.g. `$82 ,000.`) when word list has separate tokens.  
- Possible space before hyphen in “break -in” when using fallback path.  
- Fix by aligning wordtimes `words` with `text` (and tokenization) so `useFullTextLayout=True` more often, and by merging number/currency tokens in the word list.
