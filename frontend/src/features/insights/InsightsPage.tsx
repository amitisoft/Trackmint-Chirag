import { useQuery } from "@tanstack/react-query";
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { api } from "../../services/api/client";
import type {
  FinancialHealthScoreResponse,
  ForecastDailyPoint,
  ForecastMonthResponse,
  InsightCard,
} from "../../types/models";

function currency(value: number) {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(value);
}

function percent(value: number) {
  return `${Math.round(value)}%`;
}

function toneClass(tone: string) {
  if (tone === "positive") {
    return "status-badge status-badge--completed";
  }

  if (tone === "warning") {
    return "status-badge status-badge--paused";
  }

  return "status-badge status-badge--active";
}

export function InsightsPage() {
  const { data: healthScore } = useQuery({
    queryKey: ["insights-health-score"],
    queryFn: async () => (await api.get<FinancialHealthScoreResponse>("/insights/health-score")).data,
  });

  const { data: forecastMonth } = useQuery({
    queryKey: ["forecast-month"],
    queryFn: async () => (await api.get<ForecastMonthResponse>("/forecast/month")).data,
  });

  const { data: forecastDaily = [] } = useQuery({
    queryKey: ["forecast-daily"],
    queryFn: async () => (await api.get<ForecastDailyPoint[]>("/forecast/daily")).data,
  });

  const { data: insights = [] } = useQuery({
    queryKey: ["insights-cards"],
    queryFn: async () => (await api.get<InsightCard[]>("/insights")).data,
  });

  return (
    <AppShell title="Insights">
      <div className="dashboard-grid">
        <Card className="summary-card summary-card--primary" title="Financial health score">
          <h2>{Math.round(healthScore?.score ?? 0)}</h2>
          <p>{healthScore?.score ? `${healthScore.score.toFixed(1)} / 100` : "Score appears once data is available."}</p>
        </Card>

        <Card className="summary-card summary-card--success" title="Projected end balance">
          <h2>{currency(forecastMonth?.projectedEndOfMonthBalance ?? 0)}</h2>
          <p>Current: {currency(forecastMonth?.currentBalance ?? 0)}</p>
        </Card>

        <Card className="summary-card summary-card--danger" title="Safe to spend">
          <h2>{currency(forecastMonth?.safeToSpend ?? 0)}</h2>
          <p>Based on upcoming recurring and recent run rate.</p>
        </Card>

        <Card title="Daily projected balance" className="dashboard-card dashboard-card--chart">
          <div className="chart-box">
            {forecastDaily.length > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <LineChart data={forecastDaily}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" tickFormatter={(value) => String(value).slice(5)} />
                  <YAxis />
                  <Tooltip formatter={(value) => currency(Number(value ?? 0))} />
                  <Line type="monotone" dataKey="projectedBalance" stroke="#1d4ed8" strokeWidth={3} dot={false} />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="empty-state">Forecast points will appear after transactions and recurring setup.</div>
            )}
          </div>
        </Card>

        <Card title="Health factors" className="dashboard-card dashboard-card--budget">
          <div className="budget-card-stack">
            {(healthScore?.factors ?? []).map((factor) => (
              <div key={factor.name} className="budget-progress">
                <div className="budget-progress__copy">
                  <strong>{factor.name}</strong>
                  <span>
                    {percent(factor.score)} • weight {percent(factor.weight * 100)}
                  </span>
                </div>
                <div className="progress-track">
                  <div className="progress-fill" style={{ width: `${Math.max(0, Math.min(100, factor.score))}%` }} />
                </div>
              </div>
            ))}
            {!(healthScore?.factors?.length ?? 0) && <div className="empty-state empty-state--table">No factor data yet.</div>}
          </div>
        </Card>

        <Card title="Actionable insights" className="dashboard-card dashboard-card--compact">
          <div className="list-stack">
            {insights.map((item) => (
              <div key={`${item.title}-${item.message}`} className="list-row list-row--aligned">
                <div>
                  <strong>{item.title}</strong>
                  <span>{item.message}</span>
                </div>
                <span className={toneClass(item.tone)}>{item.tone}</span>
              </div>
            ))}
            {!insights.length && <div className="empty-state empty-state--table">No insights available yet.</div>}
          </div>
        </Card>

        <Card title="Forecast watchlist" className="dashboard-card dashboard-card--compact">
          <div className="list-stack">
            {(forecastMonth?.upcomingKnownTransactions ?? []).slice(0, 8).map((item) => (
              <div key={`${item.title}-${item.date}-${item.amount}`} className="list-row list-row--aligned">
                <div>
                  <strong>{item.title}</strong>
                  <span>
                    {item.type} • {item.date}
                  </span>
                </div>
                <strong>{currency(item.amount)}</strong>
              </div>
            ))}
            {!(forecastMonth?.upcomingKnownTransactions?.length ?? 0) && <div className="empty-state empty-state--table">No known upcoming items for this month.</div>}
          </div>

          {(forecastMonth?.riskWarnings?.length ?? 0) > 0 && (
            <div className="insight-warning-list">
              {forecastMonth?.riskWarnings.map((warning) => (
                <p key={warning}>{warning}</p>
              ))}
            </div>
          )}

          {(healthScore?.suggestions?.length ?? 0) > 0 && (
            <div className="insight-suggestions">
              <h4>Suggestions</h4>
              <ul>
                {healthScore?.suggestions.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          )}
        </Card>
      </div>
    </AppShell>
  );
}
