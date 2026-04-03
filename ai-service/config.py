import os

DATABASE_URL = os.getenv(
    "DATABASE_URL",
    "postgresql://postgres:Amin%401366@localhost:5432/ai_employee",
)
