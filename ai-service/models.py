from datetime import datetime, timezone

from sqlalchemy import Column, DateTime, Integer, String, Text
from sqlalchemy.orm import declarative_base

Base = declarative_base()


class Judgment(Base):
    __tablename__ = "judgments"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(String(255), nullable=False)
    input_text = Column(Text, nullable=False)
    winner = Column(String(512), nullable=False)
    reason = Column(Text, nullable=False)
    created_at = Column(
        DateTime(timezone=True),
        nullable=False,
        default=lambda: datetime.now(timezone.utc),
    )
