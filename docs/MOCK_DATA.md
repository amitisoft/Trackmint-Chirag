# TrackMint Mock Data

Use this when you want realistic demo data in the app without manually creating records one by one.

## What this seed adds

- 4 accounts
- 6 budgets
- 4 savings goals
- 6 recurring transactions
- 100+ transactions across income, expense, and transfer flows

This is enough to populate:

- dashboard cards
- recent transactions
- budget progress
- recurring section
- reports and charts
- account balances

## Before you run it

1. Sign up once in TrackMint from the UI.
2. Keep that email address ready.
3. Open DBeaver and connect to `personal_finance_tracker`.

The SQL script seeds data into an existing TrackMint user, so the login will still work with your normal password.

## File to run

[trackmint-demo-seed.sql](/c:/Users/Lenovo/Desktop/Hackathon/docs/sql/trackmint-demo-seed.sql)

## DBeaver steps

1. Open DBeaver.
2. Open your PostgreSQL connection.
3. Right-click the `personal_finance_tracker` database.
4. Click `SQL Editor`.
5. Click `Open SQL Script`.
6. Open [trackmint-demo-seed.sql](/c:/Users/Lenovo/Desktop/Hackathon/docs/sql/trackmint-demo-seed.sql).
7. Replace this line near the top:

```sql
v_user_email text := 'replace-with-your-signup-email@example.com';
```

8. Put your real signup email there.
9. Run the script.
10. Refresh the database tree in DBeaver.
11. Refresh the TrackMint browser app after logging in.

## Important behavior

- The script keeps your user account.
- The script clears old accounts, budgets, goals, recurring items, and transactions for that user before inserting fresh demo data.
- Your categories are kept and unarchived. Missing required categories are inserted automatically.

## If you want to rerun it

You can rerun the same script for the same user. It is designed to replace the seeded working data for that user instead of endlessly duplicating records.
