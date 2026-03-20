export type AuthResponse = {
  userId: string;
  displayName: string;
  email: string;
  accessToken: string;
  refreshToken: string;
};

export type ForgotPasswordResponse = {
  message: string;
  resetToken?: string;
};

export type AccountType = "BankAccount" | "CreditCard" | "CashWallet" | "SavingsAccount";
export type CategoryType = "Income" | "Expense";
export type TransactionType = "Income" | "Expense" | "Transfer";
export type GoalStatus = "Active" | "Completed" | "Paused";
export type RecurringFrequency = "Daily" | "Weekly" | "Monthly" | "Yearly";

export type Account = {
  id: string;
  name: string;
  type: AccountType;
  openingBalance: number;
  currentBalance: number;
  institutionName?: string;
  updatedAt: string;
};

export type Category = {
  id: string;
  name: string;
  type: CategoryType;
  color: string;
  icon: string;
  isArchived: boolean;
};

export type Transaction = {
  id: string;
  accountId: string;
  accountName: string;
  destinationAccountId?: string;
  destinationAccountName?: string;
  categoryId?: string;
  categoryName?: string;
  type: TransactionType;
  amount: number;
  date: string;
  note?: string;
  merchant?: string;
  paymentMethod?: string;
  tags: string[];
  recurringTransactionId?: string;
  createdAt: string;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type Budget = {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  month: number;
  year: number;
  amount: number;
  actualSpend: number;
  utilizationPercent: number;
  alertThresholdPercent: number;
};

export type Goal = {
  id: string;
  name: string;
  targetAmount: number;
  currentAmount: number;
  progressPercent: number;
  targetDate?: string;
  linkedAccountId?: string;
  icon: string;
  color: string;
  status: GoalStatus;
};

export type RecurringTransaction = {
  id: string;
  title: string;
  type: TransactionType;
  amount: number;
  categoryId?: string;
  categoryName?: string;
  accountId: string;
  accountName?: string;
  destinationAccountId?: string;
  destinationAccountName?: string;
  frequency: RecurringFrequency;
  startDate: string;
  endDate?: string;
  nextRunDate: string;
  autoCreateTransaction: boolean;
  isPaused: boolean;
};

export type DashboardSummary = {
  currentMonthIncome: number;
  currentMonthExpense: number;
  netBalance: number;
  budgetProgressCards: Array<{
    id: string;
    category: string;
    budgetAmount: number;
    actualAmount: number;
    utilizationPercent: number;
    color: string;
  }>;
  spendingByCategory: Array<{ label: string; value: number; color: string }>;
  incomeExpenseTrend: Array<{ label: string; income: number; expense: number }>;
  recentTransactions: Array<{
    id: string;
    merchant: string;
    category: string;
    account: string;
    type: string;
    amount: number;
    date: string;
  }>;
  upcomingRecurringPayments: Array<{
    id: string;
    title: string;
    amount: number;
    nextRunDate: string;
    frequency: string;
  }>;
  savingsGoals: Array<{
    id: string;
    name: string;
    currentAmount: number;
    targetAmount: number;
    progressPercent: number;
    targetDate?: string;
    color: string;
  }>;
};

export type CategorySpendItem = { category: string; amount: number; color: string };
export type IncomeExpenseTrendItem = { label: string; income: number; expense: number };
export type AccountBalanceTrendItem = { label: string; balance: number };
