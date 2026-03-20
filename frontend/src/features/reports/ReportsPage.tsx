import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Bar, BarChart, CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import { getAuthSession } from "../../store/auth-store";
import type { Account, AccountBalanceTrendItem, Category, CategorySpendItem, IncomeExpenseTrendItem, TransactionType } from "../../types/models";

function currency(value: number) {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(value);
}

function tooltipCurrency(value: number | string | readonly (number | string)[] | undefined) {
  const normalized = Array.isArray(value) ? Number(value[0]) : Number(value ?? 0);
  return currency(normalized);
}

export function ReportsPage() {
  const [filters, setFilters] = useState<{ startDate?: string; endDate?: string; accountId?: string; categoryId?: string; type?: TransactionType | "All" }>({
    type: "All",
  });
  const { showToast } = useToast();

  const { data: accounts = [] } = useQuery({
    queryKey: ["accounts"],
    queryFn: async () => (await api.get<Account[]>("/accounts")).data,
  });

  const { data: categories = [] } = useQuery({
    queryKey: ["categories"],
    queryFn: async () => (await api.get<Category[]>("/categories")).data,
  });

  const params = {
    startDate: filters.startDate || undefined,
    endDate: filters.endDate || undefined,
    accountId: filters.accountId || undefined,
    categoryId: filters.categoryId || undefined,
    type: filters.type === "All" ? undefined : filters.type,
  };

  const { data: categorySpend = [] } = useQuery({
    queryKey: ["report-category-spend", params],
    queryFn: async () => (await api.get<CategorySpendItem[]>("/reports/category-spend", { params })).data,
  });

  const { data: trend = [] } = useQuery({
    queryKey: ["report-income-expense", params],
    queryFn: async () => (await api.get<IncomeExpenseTrendItem[]>("/reports/income-vs-expense", { params })).data,
  });

  const { data: balanceTrend = [] } = useQuery({
    queryKey: ["report-balance-trend", params],
    queryFn: async () => (await api.get<AccountBalanceTrendItem[]>("/reports/account-balance-trend", { params })).data,
  });

  async function exportCsv() {
    try {
      const session = getAuthSession();
      const search = new URLSearchParams();
      if (params.startDate) search.set("startDate", params.startDate);
      if (params.endDate) search.set("endDate", params.endDate);
      if (params.accountId) search.set("accountId", params.accountId);
      if (params.categoryId) search.set("categoryId", params.categoryId);
      if (params.type) search.set("type", params.type);

      const response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5151/api"}/reports/export/csv?${search.toString()}`, {
        headers: {
          Authorization: `Bearer ${session?.accessToken ?? ""}`,
        },
      });

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `transactions-${Date.now()}.csv`;
      link.click();
      window.URL.revokeObjectURL(url);
      showToast("CSV exported");
    } catch {
      showToast("CSV export failed", "error");
    }
  }

  return (
    <AppShell title="Reports">
      <Card title="Filters" subtitle="Refine date range, account, category, and transaction type." className="page-section page-section--filters">
        <div className="toolbar-grid">
          <input type="date" value={filters.startDate ?? ""} onChange={(event) => setFilters((current) => ({ ...current, startDate: event.target.value }))} />
          <input type="date" value={filters.endDate ?? ""} onChange={(event) => setFilters((current) => ({ ...current, endDate: event.target.value }))} />
          <select value={filters.accountId ?? ""} onChange={(event) => setFilters((current) => ({ ...current, accountId: event.target.value }))}>
            <option value="">All accounts</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>
          <select value={filters.categoryId ?? ""} onChange={(event) => setFilters((current) => ({ ...current, categoryId: event.target.value }))}>
            <option value="">All categories</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
          <select value={filters.type ?? "All"} onChange={(event) => setFilters((current) => ({ ...current, type: event.target.value as TransactionType | "All" }))}>
            <option value="All">All types</option>
            <option value="Income">Income</option>
            <option value="Expense">Expense</option>
            <option value="Transfer">Transfer</option>
          </select>
          <button type="button" className="primary-button" onClick={exportCsv}>
            Export CSV
          </button>
        </div>
      </Card>

      <div className="page-grid page-grid--two">
        <Card title="Category Spend">
          <div className="chart-box">
            {categorySpend.length ? (
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={categorySpend}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="category" hide />
                  <YAxis />
                  <Tooltip formatter={(value) => tooltipCurrency(value)} />
                  <Bar dataKey="amount" radius={[12, 12, 0, 0]} fill="#0f172a" />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="empty-state">No category spend data for the selected filters.</div>
            )}
          </div>
        </Card>

        <Card title="Income vs Expense">
          <div className="chart-box">
            {trend.length ? (
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={trend}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" />
                  <YAxis />
                  <Tooltip formatter={(value) => tooltipCurrency(value)} />
                  <Line type="monotone" dataKey="income" stroke="#10b981" strokeWidth={3} />
                  <Line type="monotone" dataKey="expense" stroke="#ef4444" strokeWidth={3} />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="empty-state">No income and expense trend available yet.</div>
            )}
          </div>
        </Card>

        <Card title="Account Balance Trend">
          <div className="chart-box">
            {balanceTrend.length ? (
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={balanceTrend}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" />
                  <YAxis />
                  <Tooltip formatter={(value) => tooltipCurrency(value)} />
                  <Line type="monotone" dataKey="balance" stroke="#1d4ed8" strokeWidth={3} />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="empty-state">Add accounts and transactions to unlock balance trends.</div>
            )}
          </div>
        </Card>

        <Card title="Top spend categories">
          <div className="list-stack">
            {categorySpend.map((item) => (
              <div key={item.category} className="list-row">
                <div>
                  <strong>{item.category}</strong>
                  <span>{item.color}</span>
                </div>
                <strong>{currency(item.amount)}</strong>
              </div>
            ))}
            {!categorySpend.length && <div className="empty-state">Top spending categories will appear here after seeding or creating transactions.</div>}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}
