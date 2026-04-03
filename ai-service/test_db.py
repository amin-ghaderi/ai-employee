from sqlalchemy import text

from db import engine


def main() -> None:
    try:
        with engine.connect() as connection:
            connection.execute(text("SELECT 1"))
        print("Database connection successful.")
    except Exception as exc:
        print(f"Database connection failed: {exc}")


if __name__ == "__main__":
    main()
