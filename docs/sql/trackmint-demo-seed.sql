CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TEMP TABLE IF NOT EXISTS "TrackMintSeedContext" ("UserId" uuid);
TRUNCATE TABLE "TrackMintSeedContext";

DO $seed$
DECLARE
    v_user_email text := 'replace-with-your-signup-email@example.com';
    v_user_id uuid;
    v_now timestamptz := timezone('utc', now());

    v_checking_id uuid := gen_random_uuid();
    v_cash_id uuid := gen_random_uuid();
    v_credit_id uuid := gen_random_uuid();
    v_savings_id uuid := gen_random_uuid();

    v_food_id uuid;
    v_rent_id uuid;
    v_utilities_id uuid;
    v_transport_id uuid;
    v_entertainment_id uuid;
    v_shopping_id uuid;
    v_health_id uuid;
    v_subscriptions_id uuid;
    v_salary_id uuid;
    v_freelance_id uuid;
    v_bonus_id uuid;
    v_investment_id uuid;

    v_rent_recurring_id uuid := gen_random_uuid();
    v_sip_recurring_id uuid := gen_random_uuid();
    v_netflix_recurring_id uuid := gen_random_uuid();
    v_phone_recurring_id uuid := gen_random_uuid();
    v_gym_recurring_id uuid := gen_random_uuid();
    v_salary_recurring_id uuid := gen_random_uuid();

    v_tx_date date;
    v_amount numeric(12,2);
    i integer;
BEGIN
    SELECT "Id"
    INTO v_user_id
    FROM "Users"
    WHERE lower("Email") = lower(v_user_email)
    LIMIT 1;

    IF v_user_id IS NULL THEN
        RAISE EXCEPTION 'TrackMint seed aborted. User with email % was not found. Sign up first, then replace v_user_email in this script.', v_user_email;
    END IF;

    INSERT INTO "TrackMintSeedContext" ("UserId") VALUES (v_user_id);

    DELETE FROM "Transactions" WHERE "UserId" = v_user_id;
    DELETE FROM "RecurringTransactions" WHERE "UserId" = v_user_id;
    DELETE FROM "Budgets" WHERE "UserId" = v_user_id;
    DELETE FROM "Goals" WHERE "UserId" = v_user_id;
    DELETE FROM "AuditLogs" WHERE "UserId" = v_user_id;
    DELETE FROM "Accounts" WHERE "UserId" = v_user_id;

    UPDATE "Categories"
    SET "IsArchived" = false,
        "UpdatedAt" = v_now
    WHERE "UserId" = v_user_id;

    INSERT INTO "Categories" ("Id", "CreatedAt", "UpdatedAt", "UserId", "Name", "Type", "Color", "Icon", "IsArchived")
    SELECT gen_random_uuid(), v_now, v_now, v_user_id, template."Name", template."Type", template."Color", template."Icon", false
    FROM (
        VALUES
            ('Food', 'Expense', '#ef4444', 'utensils'),
            ('Rent', 'Expense', '#f97316', 'home'),
            ('Utilities', 'Expense', '#f59e0b', 'bolt'),
            ('Transport', 'Expense', '#8b5cf6', 'car'),
            ('Entertainment', 'Expense', '#ec4899', 'film'),
            ('Shopping', 'Expense', '#06b6d4', 'shopping-bag'),
            ('Health', 'Expense', '#10b981', 'heart'),
            ('Subscriptions', 'Expense', '#6366f1', 'repeat'),
            ('Salary', 'Income', '#16a34a', 'briefcase'),
            ('Freelance', 'Income', '#22c55e', 'laptop'),
            ('Bonus', 'Income', '#84cc16', 'sparkles'),
            ('Investment', 'Income', '#14b8a6', 'trending-up')
    ) AS template("Name", "Type", "Color", "Icon")
    WHERE NOT EXISTS (
        SELECT 1
        FROM "Categories" c
        WHERE c."UserId" = v_user_id
          AND c."Name" = template."Name"
          AND c."Type" = template."Type"
    );

    SELECT "Id" INTO v_food_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Food' AND "Type" = 'Expense';
    SELECT "Id" INTO v_rent_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Rent' AND "Type" = 'Expense';
    SELECT "Id" INTO v_utilities_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Utilities' AND "Type" = 'Expense';
    SELECT "Id" INTO v_transport_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Transport' AND "Type" = 'Expense';
    SELECT "Id" INTO v_entertainment_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Entertainment' AND "Type" = 'Expense';
    SELECT "Id" INTO v_shopping_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Shopping' AND "Type" = 'Expense';
    SELECT "Id" INTO v_health_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Health' AND "Type" = 'Expense';
    SELECT "Id" INTO v_subscriptions_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Subscriptions' AND "Type" = 'Expense';
    SELECT "Id" INTO v_salary_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Salary' AND "Type" = 'Income';
    SELECT "Id" INTO v_freelance_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Freelance' AND "Type" = 'Income';
    SELECT "Id" INTO v_bonus_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Bonus' AND "Type" = 'Income';
    SELECT "Id" INTO v_investment_id FROM "Categories" WHERE "UserId" = v_user_id AND "Name" = 'Investment' AND "Type" = 'Income';

    INSERT INTO "Accounts" ("Id", "CreatedAt", "UpdatedAt", "UserId", "Name", "Type", "OpeningBalance", "CurrentBalance", "InstitutionName")
    VALUES
        (v_checking_id, v_now, v_now, v_user_id, 'Axis Salary Account', 'BankAccount', 18000.00, 18000.00, 'Axis Bank'),
        (v_cash_id, v_now, v_now, v_user_id, 'Daily Cash Wallet', 'CashWallet', 3500.00, 3500.00, NULL),
        (v_credit_id, v_now, v_now, v_user_id, 'HDFC Millennia Card', 'CreditCard', 0.00, 0.00, 'HDFC Bank'),
        (v_savings_id, v_now, v_now, v_user_id, 'Emergency Savings', 'SavingsAccount', 42000.00, 42000.00, 'State Bank of India');

    INSERT INTO "Budgets" ("Id", "CreatedAt", "UpdatedAt", "UserId", "CategoryId", "Month", "Year", "Amount", "AlertThresholdPercent")
    VALUES
        (gen_random_uuid(), v_now, v_now, v_user_id, v_food_id, EXTRACT(MONTH FROM CURRENT_DATE), EXTRACT(YEAR FROM CURRENT_DATE), 12000.00, 80),
        (gen_random_uuid(), v_now, v_now, v_user_id, v_transport_id, EXTRACT(MONTH FROM CURRENT_DATE), EXTRACT(YEAR FROM CURRENT_DATE), 5000.00, 75),
        (gen_random_uuid(), v_now, v_now, v_user_id, v_entertainment_id, EXTRACT(MONTH FROM CURRENT_DATE), EXTRACT(YEAR FROM CURRENT_DATE), 4500.00, 85),
        (gen_random_uuid(), v_now, v_now, v_user_id, v_shopping_id, EXTRACT(MONTH FROM CURRENT_DATE), EXTRACT(YEAR FROM CURRENT_DATE), 6500.00, 80),
        (gen_random_uuid(), v_now, v_now, v_user_id, v_health_id, EXTRACT(MONTH FROM CURRENT_DATE), EXTRACT(YEAR FROM CURRENT_DATE), 3000.00, 70),
        (gen_random_uuid(), v_now, v_now, v_user_id, v_subscriptions_id, EXTRACT(MONTH FROM CURRENT_DATE), EXTRACT(YEAR FROM CURRENT_DATE), 1800.00, 90);

    INSERT INTO "Goals" ("Id", "CreatedAt", "UpdatedAt", "UserId", "Name", "TargetAmount", "CurrentAmount", "TargetDate", "LinkedAccountId", "Icon", "Color", "Status")
    VALUES
        (gen_random_uuid(), v_now, v_now, v_user_id, 'Emergency Fund', 250000.00, 108000.00, CURRENT_DATE + 300, v_savings_id, 'shield', '#14b8a6', 'Active'),
        (gen_random_uuid(), v_now, v_now, v_user_id, 'Goa Trip', 60000.00, 19000.00, CURRENT_DATE + 120, v_savings_id, 'plane', '#f97316', 'Active'),
        (gen_random_uuid(), v_now, v_now, v_user_id, 'New Laptop', 95000.00, 51000.00, CURRENT_DATE + 180, v_savings_id, 'laptop', '#8b5cf6', 'Active'),
        (gen_random_uuid(), v_now, v_now, v_user_id, 'Bike Down Payment', 80000.00, 32000.00, CURRENT_DATE + 240, v_savings_id, 'bike', '#22c55e', 'Active');

    INSERT INTO "RecurringTransactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "Title", "Type", "Amount", "CategoryId", "AccountId", "DestinationAccountId", "Frequency", "StartDate", "EndDate", "NextRunDate", "AutoCreateTransaction", "IsPaused")
    VALUES
        (v_rent_recurring_id, v_now, v_now, v_user_id, 'Apartment Rent', 'Expense', 18000.00, v_rent_id, v_checking_id, NULL, 'Monthly', CURRENT_DATE - 240, NULL, CURRENT_DATE + 4, true, false),
        (v_sip_recurring_id, v_now, v_now, v_user_id, 'Monthly SIP', 'Transfer', 7000.00, NULL, v_checking_id, v_savings_id, 'Monthly', CURRENT_DATE - 240, NULL, CURRENT_DATE + 6, true, false),
        (v_netflix_recurring_id, v_now, v_now, v_user_id, 'Netflix', 'Expense', 649.00, v_subscriptions_id, v_credit_id, NULL, 'Monthly', CURRENT_DATE - 240, NULL, CURRENT_DATE + 8, true, false),
        (v_phone_recurring_id, v_now, v_now, v_user_id, 'Phone Bill', 'Expense', 899.00, v_utilities_id, v_checking_id, NULL, 'Monthly', CURRENT_DATE - 240, NULL, CURRENT_DATE + 10, true, false),
        (v_gym_recurring_id, v_now, v_now, v_user_id, 'Gym Membership', 'Expense', 1499.00, v_health_id, v_credit_id, NULL, 'Monthly', CURRENT_DATE - 240, NULL, CURRENT_DATE + 12, true, false),
        (v_salary_recurring_id, v_now, v_now, v_user_id, 'Monthly Salary', 'Income', 62000.00, v_salary_id, v_checking_id, NULL, 'Monthly', CURRENT_DATE - 240, NULL, CURRENT_DATE + 11, true, false);

    FOR i IN 0..5 LOOP
        v_tx_date := (date_trunc('month', CURRENT_DATE)::date - (i || ' months')::interval)::date + 1;

        INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
        VALUES
            (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_salary_id, 'Income', 62000.00 + (i * 1800), v_tx_date, 'Primary salary credit', 'TrackMint Payroll', 'Bank Transfer', ARRAY['salary', 'monthly'], v_salary_recurring_id),
            (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_freelance_id, 'Income', 8500.00 + ((i % 3) * 2500), v_tx_date + 12, 'Freelance project payout', 'Client Payment', 'UPI', ARRAY['freelance', 'side-income'], NULL),
            (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, v_savings_id, NULL, 'Transfer', 7000.00 + ((i % 2) * 1000), v_tx_date + 4, 'Move money into savings', 'Internal Transfer', 'Bank Transfer', ARRAY['savings', 'goal'], v_sip_recurring_id),
            (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_rent_id, 'Expense', 18000.00, v_tx_date + 2, 'Monthly rent payment', 'Green Residency', 'Bank Transfer', ARRAY['rent', 'housing'], v_rent_recurring_id);

        IF i IN (1, 4) THEN
            INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
            VALUES
                (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_bonus_id, 'Income', 15000.00 + (i * 1000), v_tx_date + 20, 'Performance bonus', 'TrackMint Payroll', 'Bank Transfer', ARRAY['bonus', 'salary'], NULL);
        END IF;
    END LOOP;

    FOR i IN 1..92 LOOP
        v_tx_date := CURRENT_DATE - ((i * 2) % 178);

        CASE i % 8
            WHEN 0 THEN
                INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
                VALUES
                    (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_food_id, 'Expense', 220.00 + ((i % 5) * 90), v_tx_date, 'Lunch and groceries', 'Blinkit', 'UPI', ARRAY['food', 'daily'], NULL);
            WHEN 1 THEN
                INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
                VALUES
                    (gen_random_uuid(), v_now, v_now, v_user_id, v_credit_id, NULL, v_shopping_id, 'Expense', 950.00 + ((i % 4) * 420), v_tx_date, 'Online shopping order', 'Amazon', 'Credit Card', ARRAY['shopping', 'online'], NULL);
            WHEN 2 THEN
                INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
                VALUES
                    (gen_random_uuid(), v_now, v_now, v_user_id, v_cash_id, NULL, v_transport_id, 'Expense', 140.00 + ((i % 6) * 55), v_tx_date, 'Auto and metro spend', 'Local Transit', 'Cash', ARRAY['transport', 'commute'], NULL);
            WHEN 3 THEN
                INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
                VALUES
                    (gen_random_uuid(), v_now, v_now, v_user_id, v_credit_id, NULL, v_entertainment_id, 'Expense', 300.00 + ((i % 5) * 180), v_tx_date, 'Movies or streaming nights', 'BookMyShow', 'Credit Card', ARRAY['fun', 'weekend'], NULL);
            WHEN 4 THEN
                INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
                VALUES
                    (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_utilities_id, 'Expense', 700.00 + ((i % 4) * 160), v_tx_date, 'Electricity, water, and broadband', 'Utility Provider', 'Bank Transfer', ARRAY['utilities', 'home'], NULL);
            WHEN 5 THEN
                INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
                VALUES
                    (gen_random_uuid(), v_now, v_now, v_user_id, v_credit_id, NULL, v_health_id, 'Expense', 450.00 + ((i % 3) * 250), v_tx_date, 'Pharmacy and wellness expenses', 'Apollo Pharmacy', 'Credit Card', ARRAY['health', 'care'], NULL);
            WHEN 6 THEN
                INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
                VALUES
                    (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_subscriptions_id, 'Expense', 199.00 + ((i % 4) * 120), v_tx_date, 'Digital subscriptions', 'Subscription Bundle', 'UPI', ARRAY['subscriptions', 'digital'], NULL);
            ELSE
                INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
                VALUES
                    (gen_random_uuid(), v_now, v_now, v_user_id, v_cash_id, NULL, v_food_id, 'Expense', 120.00 + ((i % 5) * 60), v_tx_date, 'Tea, snacks, and quick bites', 'Cafe Stop', 'Cash', ARRAY['food', 'snacks'], NULL);
        END CASE;
    END LOOP;

    INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
    VALUES
        (gen_random_uuid(), v_now, v_now, v_user_id, v_credit_id, NULL, v_subscriptions_id, 'Expense', 649.00, CURRENT_DATE - 18, 'Streaming subscription', 'Netflix', 'Credit Card', ARRAY['subscription', 'entertainment'], v_netflix_recurring_id);

    INSERT INTO "Transactions" ("Id", "CreatedAt", "UpdatedAt", "UserId", "AccountId", "DestinationAccountId", "CategoryId", "Type", "Amount", "TransactionDate", "Note", "Merchant", "PaymentMethod", "Tags", "RecurringTransactionId")
    VALUES
        (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_utilities_id, 'Expense', 899.00, CURRENT_DATE - 12, 'Mobile recharge and bill', 'Jio', 'UPI', ARRAY['utilities', 'phone'], v_phone_recurring_id),
        (gen_random_uuid(), v_now, v_now, v_user_id, v_credit_id, NULL, v_health_id, 'Expense', 1499.00, CURRENT_DATE - 9, 'Gym renewal', 'Cult Fit', 'Credit Card', ARRAY['fitness', 'health'], v_gym_recurring_id),
        (gen_random_uuid(), v_now, v_now, v_user_id, v_checking_id, NULL, v_investment_id, 'Income', 4200.00, CURRENT_DATE - 14, 'Mutual fund dividend', 'Groww', 'Bank Transfer', ARRAY['investment', 'returns'], NULL);

    UPDATE "Accounts" a
    SET "CurrentBalance" = a."OpeningBalance" + COALESCE((
        SELECT SUM(
            CASE
                WHEN t."Type" = 'Income' AND t."AccountId" = a."Id" THEN t."Amount"
                WHEN t."Type" = 'Expense' AND t."AccountId" = a."Id" THEN -t."Amount"
                WHEN t."Type" = 'Transfer' AND t."AccountId" = a."Id" THEN -t."Amount"
                WHEN t."Type" = 'Transfer' AND t."DestinationAccountId" = a."Id" THEN t."Amount"
                ELSE 0
            END
        )
        FROM "Transactions" t
        WHERE t."UserId" = v_user_id
    ), 0),
        "UpdatedAt" = v_now
    WHERE a."UserId" = v_user_id;

    INSERT INTO "AuditLogs" ("Id", "CreatedAt", "UpdatedAt", "UserId", "Action", "EntityType", "EntityId", "MetadataJson")
    VALUES
        (gen_random_uuid(), v_now, v_now, v_user_id, 'SeededMockData', 'System', NULL, '{"source":"docs/sql/trackmint-demo-seed.sql"}');

    RAISE NOTICE 'TrackMint mock data inserted for %.', v_user_email;
END
$seed$;

SELECT 'Accounts' AS "Table", COUNT(*) AS "Count" FROM "Accounts" WHERE "UserId" = (SELECT "UserId" FROM "TrackMintSeedContext")
UNION ALL
SELECT 'Budgets', COUNT(*) FROM "Budgets" WHERE "UserId" = (SELECT "UserId" FROM "TrackMintSeedContext")
UNION ALL
SELECT 'Goals', COUNT(*) FROM "Goals" WHERE "UserId" = (SELECT "UserId" FROM "TrackMintSeedContext")
UNION ALL
SELECT 'RecurringTransactions', COUNT(*) FROM "RecurringTransactions" WHERE "UserId" = (SELECT "UserId" FROM "TrackMintSeedContext")
UNION ALL
SELECT 'Transactions', COUNT(*) FROM "Transactions" WHERE "UserId" = (SELECT "UserId" FROM "TrackMintSeedContext")
ORDER BY "Table";
