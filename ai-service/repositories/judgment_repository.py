from sqlalchemy.orm import Session

from models import Judgment


class JudgmentRepository:
    def __init__(self, db_session: Session) -> None:
        self._db = db_session

    def get_recent_judgments(self, user_id: str, limit: int = 5) -> list[Judgment]:
        return (
            self._db.query(Judgment)
            .filter(Judgment.user_id == user_id)
            .order_by(Judgment.created_at.desc())
            .limit(limit)
            .all()
        )

    def save_judgment(
        self,
        user_id: str,
        input_text: str,
        winner: str,
        reason: str,
    ) -> Judgment:
        row = Judgment(
            user_id=user_id,
            input_text=input_text,
            winner=winner,
            reason=reason,
        )
        self._db.add(row)
        self._db.commit()
        self._db.refresh(row)
        return row
