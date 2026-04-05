from __future__ import annotations

from typing import Any, Sequence

_MAX_HISTORY_CHARS = 8000
_MAX_PER_INPUT_CHARS = 2000


def format_history(judgments: Sequence[Any]) -> str:
    """Turn recent judgments into a readable transcript (oldest first)."""
    if not judgments:
        return ""

    chronological = list(reversed(list(judgments)))
    parts: list[str] = []
    for j in chronological:
        text = (getattr(j, "input_text", None) or "")[:_MAX_PER_INPUT_CHARS]
        winner = getattr(j, "winner", "")
        reason = getattr(j, "reason", "")
        parts.append(f"User: {text}")
        parts.append(f"AI: Winner: {winner}, Reason: {reason}")

    out = "\n\n".join(parts)
    if len(out) > _MAX_HISTORY_CHARS:
        return out[:_MAX_HISTORY_CHARS] + "\n…[history truncated]"
    return out
