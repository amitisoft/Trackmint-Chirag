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

function DonutTooltip({ active, payload }: { active?: boolean; payload?: Array<{ name?: string; value?: number | string }> }) {
  if (!active || !payload?.length) {
    return null;
  }

  const item = payload[0];
  const label = item.name ?? "";
  const value = Number(item.value ?? 0);

  return (
    <div className="chart-tooltip chart-tooltip--donut">
      <strong>{label}</strong>
      <span>{tooltipCurrency(value)}</span>
    </div>
  );
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
  const categorySpend = data?.spendingByCategory ?? [];
  const totalCategorySpend = categorySpend.reduce((sum, item) => sum + item.value, 0);
  const topCategories = categorySpend.slice(0, 6);

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
            {categorySpend.length > 0 ? (
              <div className="donut-panel">
                <div className="donut-panel__visual">
                  <ResponsiveContainer width="100%" height={280}>
                    <PieChart>
                      <Pie data={categorySpend} dataKey="value" nameKey="label" innerRadius={78} outerRadius={108} stroke="none">
                        {categorySpend.map((item) => (
                          <Cell key={item.label} fill={item.color} />
                        ))}
                      </Pie>
                      <Tooltip
                        content={<DonutTooltip />}
                        cursor={false}
                        allowEscapeViewBox={{ x: true, y: true }}
                        position={{ x: 252, y: 108 }}
                        wrapperStyle={{ pointerEvents: "none", zIndex: 8 }}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                  <div className="donut-panel__center">
                    <span>Total spend</span>
                    <strong>{currency(totalCategorySpend)}</strong>
                  </div>
                </div>

                <div className="donut-panel__legend">
                  {topCategories.map((item) => (
                    <div key={item.label} className="donut-legend-item">
                      <div className="donut-legend-item__copy">
                        <span className="category-chip__dot category-chip__dot--large" style={{ background: item.color }} />
                        <div>
                          <strong>{item.label}</strong>
                          <small>{((item.value / totalCategorySpend) * 100 || 0).toFixed(0)}% of spend</small>
                        </div>
                      </div>
                      <strong>{currency(item.value)}</strong>
                    </div>
                  ))}
                </div>
              </div>
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
