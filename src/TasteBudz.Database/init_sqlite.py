import argparse
from pathlib import Path
import sqlite3


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Initialize the local TasteBudz SQLite database.")
    parser.add_argument(
        "--with-test-data",
        action="store_true",
        help="Also apply the richer development test-data script.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    root = Path(__file__).resolve().parent
    db_path = root / "TasteBudz.sqlite"
    schema_path = root / "dbTasteBudz.sqlite.sql"
    seed_path = root / "dbTasteBudz.sqlite.seed.sql"
    test_data_path = root / "dbTasteBudz.sqlite.testdata.sql"

    if db_path.exists():
        db_path.unlink()

    schema = schema_path.read_text(encoding="utf-8")
    seed = seed_path.read_text(encoding="utf-8")
    test_data = test_data_path.read_text(encoding="utf-8") if args.with_test_data else None

    connection = sqlite3.connect(db_path)
    try:
        connection.execute("PRAGMA foreign_keys = ON;")
        connection.executescript(schema)
        connection.executescript(seed)
        if test_data is not None:
            connection.executescript(test_data)
        connection.commit()
        table_count = connection.execute(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';"
        ).fetchone()[0]
        user_count = connection.execute("SELECT COUNT(*) FROM UserAccounts;").fetchone()[0]
    finally:
        connection.close()

    print(
        f"Initialized {db_path} with {table_count} tables and {user_count} users"
        f"{' (including test data)' if args.with_test_data else ''}."
    )


if __name__ == "__main__":
    main()
