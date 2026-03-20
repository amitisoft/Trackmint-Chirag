import { useQuery } from "@tanstack/react-query";
import { Area, AreaChart, Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { api } from "../../services/api/client";
import type { DashboardSummary } from "../../types/models";

function currency(value: number) {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(value);
}

function tooltipCurrency(value: number | string | readonly (number | string)[] | undefined) {
  const normalized = Array.isArray(value) ? Number(value[0]) : Number(value ?? 0);
  return currency(normalized);
}

export function DashboardPage() {
  const { data, isLoading } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: async () => {
      const response = await api.get<DashboardSummary>("/dashboard/summary");
      return response.data;
    },
  });

  const summaryCards = [
    { label: "Current Month Income", value: data?.currentMonthIncome ?? 0, tone: "success" },
    { label: "Current Month Expense", value: data?.currentMonthExpense ?? 0, tone: "danger" },
    { label: "Net Balance", value: data?.netBalance ?? 0, tone: "primary" },
  ];

  return (
    <AppShell title="Dashboard">
      <div className="dashboard-grid">
        {summaryCards.map((card) => (
          <Card key={card.label} className={`summary-card summary-card--${card.tone}`}>
            <p>{card.label}</p>
            <h2>{currency(card.value)}</h2>
          </Card>
        ))}

        <Card title="Budget pressure" subtitle="Highest utilization categories this month" className="dashboard-card dashboard-card--budget">
          <div className="budget-card-stack">
            {(data?.budgetProgressCards ?? []).map((item) => (
              <div key={item.id} className="budget-progress">
                <div className="budget-progress__copy">
                  <strong>{item.category}</strong>
                  <span>
                    {currency(item.actualAmount)} / {currency(item.budgetAmount)}
                  </span>
                </div>
                <div className="progress-track">
                  <div className="progress-fill" style={{ width: `${Math.min(item.utilizationPercent, 100)}%`, background: item.color }} />
                </div>
              </div>
            ))}
            {!data?.budgetProgressCards?.length && <p className="muted-copy">No budgets yet for the current month.</p>}
          </div>
        </Card>

        <Card title="Spending by Category" className="dashboard-card dashboard-card--chart">
          <div className="chart-box">
            {(data?.spendingByCategory?.length ?? 0) > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Pie data={data?.spendingByCategory ?? []} dataKey="value" nameKey="label" innerRadius={70} outerRadius={100}>
                    {(data?.spendingByCategory ?? []).map((item) => (
                      <Cell key={item.label} fill={item.color} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value) => tooltipCurrency(value)} />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="empty-state">No spending data for the current month yet.</div>
            )}
          </div>
        </Card>

        <Card title="Income vs Expense Trend" className="dashboard-card dashboard-card--chart">
          <div className="chart-box">
            {(data?.incomeExpenseTrend?.length ?? 0) > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <AreaChart data={data?.incomeExpenseTrend ?? []}>
                  <defs>
                    <linearGradient id="incomeGradient" x1="0" x2="0" y1="0" y2="1">
                      <stop offset="5%" stopColor="#10b981" stopOpacity={0.6} />
                      <stop offset="95%" stopColor="#10b981" stopOpacity={0} />
                    </linearGradient>
                    <linearGradient id="expenseGradient" x1="0" x2="0" y1="0" y2="1">
                      <stop offset="5%" stopColor="#ef4444" stopOpacity={0.6} />
                      <stop offset="95%" stopColor="#ef4444" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" />
                  <YAxis />
                  <Tooltip formatter={(value) => tooltipCurrency(value)} />
                  <Area type="monotone" dataKey="income" stroke="#10b981" fill="url(#incomeGradient)" />
                  <Area type="monotone" dataKey="expense" stroke="#ef4444" fill="url(#expenseGradient)" />
                </AreaChart>
              </ResponsiveContainer>
            ) : (
              <div className="empty-state">Trend lines appear after a few transactions across months.</div>
            )}
          </div>
        </Card>

        <Card title="Recent Transactions" className="dashboard-card dashboard-card--compact">
          <div className="list-stack">
            {(data?.recentTransactions ?? []).map((item) => (
              <div key={item.id} className="list-row">
                <div>
                  <strong>{item.merchant}</strong>
                  <span>{item.category}</span>
                </div>
                <div className="list-row__amount">
                  <strong>{currency(item.amount)}</strong>
                  <span>{item.date}</span>
                </div>
              </div>
            ))}
            {!data?.recentTransactions?.length && <p className="muted-copy">Add your first transaction to populate the dashboard.</p>}
          </div>
        </Card>

        <Card title="Upcoming Recurring" className="dashboard-card dashboard-card--compact">
          <div className="list-stack">
            {(data?.upcomingRecurringPayments ?? []).map((item) => (
              <div key={item.id} className="list-row">
                <div>
                  <strong>{item.title}</strong>
                  <span>{item.frequency}</span>
                </div>
                <div className="list-row__amount">
                  <strong>{currency(item.amount)}</strong>
                  <span>{item.nextRunDate}</span>
                </div>
              </div>
            ))}
            {!data?.upcomingRecurringPayments?.length && <p className="muted-copy">No upcoming recurring payments right now.</p>}
          </div>
        </Card>

        <Card title="Goal Progress" className="dashboard-card dashboard-card--chart">
          <div className="chart-box">
            {(data?.savingsGoals?.length ?? 0) > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={data?.savingsGoals ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" hide />
                  <YAxis />
                  <Tooltip formatter={(value) => `${Array.isArray(value) ? value[0] : value ?? 0}%`} />
                  <Bar dataKey="progressPercent" radius={[12, 12, 0, 0]}>
                    {(data?.savingsGoals ?? []).map((item) => (
                      <Cell key={item.id} fill={item.color} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="empty-state">Goal progress will show here once you create savings goals.</div>
            )}
          </div>
        </Card>
      </div>
      {isLoading && <p className="muted-copy">Loading dashboard...</p>}
    </AppShell>
  );
}
