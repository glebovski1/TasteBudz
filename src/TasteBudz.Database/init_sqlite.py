from pathlib import Path
import sqlite3


def main() -> None:
    root = Path(__file__).resolve().parent
    db_path = root / "TasteBudz.sqlite"
    schema_path = root / "dbTasteBudz.sqlite.sql"
    seed_path = root / "dbTasteBudz.sqlite.seed.sql"

    if db_path.exists():
        db_path.unlink()

    schema = schema_path.read_text(encoding="utf-8")
    seed = seed_path.read_text(encoding="utf-8")

    connection = sqlite3.connect(db_path)
    try:
        connection.execute("PRAGMA foreign_keys = ON;")
        connection.executescript(schema)
        connection.executescript(seed)
        connection.commit()
        table_count = connection.execute(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';"
        ).fetchone()[0]
    finally:
        connection.close()

    print(f"Initialized {db_path} with {table_count} tables.")


if __name__ == "__main__":
    main()
