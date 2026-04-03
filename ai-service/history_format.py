from __future__ import annotations

from typing import Sequence

from models import Judgment

_MAX_HISTORY_CHARS = 8000
_MAX_PER_INPUT_CHARS = 2000


def format_history(judgments: Sequence[Judgment]) -> str:
    """Turn recent judgments into a readable transcript (oldest first)."""
    if not judgments:
        return ""

    chronological = list(reversed(list(judgments)))
    parts: list[str] = []
    for j in chronological:
        text = (j.input_text or "")[:_MAX_PER_INPUT_CHARS]
        parts.append(f"User: {text}")
        parts.append(f"AI: Winner: {j.winner}, Reason: {j.reason}")

    out = "\n\n".join(parts)
    if len(out) > _MAX_HISTORY_CHARS:
        return out[:_MAX_HISTORY_CHARS] + "\n…[history truncated]"
    return out


def build_prompt_input(history: str, current_input: str) -> str:
    """Block passed into the judge template as INPUT (includes context + current turn)."""
    history_block = history.strip() if history else "(none)"
    return f"""Previous context:
{history_block}

Current input:
{current_input}

Return ONLY JSON:
{{ "winner": "...", "reason": "..." }}
"""
