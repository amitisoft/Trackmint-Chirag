import { NavLink } from "react-router-dom";
import { LayoutDashboard, ArrowLeftRight, PiggyBank, Target, ChartColumnBig, Repeat2, WalletCards, Settings, LogOut } from "lucide-react";
import { useAuthStore } from "../../store/auth-store";

const navigation = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard },
  { to: "/transactions", label: "Transactions", icon: ArrowLeftRight },
  { to: "/budgets", label: "Budgets", icon: PiggyBank },
  { to: "/goals", label: "Goals", icon: Target },
  { to: "/reports", label: "Reports", icon: ChartColumnBig },
  { to: "/recurring", label: "Recurring", icon: Repeat2 },
  { to: "/accounts", label: "Accounts", icon: WalletCards },
  { to: "/settings", label: "Settings", icon: Settings },
];

export function AppShell({ title, children }: { title: string; children: React.ReactNode }) {
  const session = useAuthStore((state) => state.session);
  const clearSession = useAuthStore((state) => state.clearSession);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand__badge">TM</div>
          <div>
            <strong>TrackMint</strong>
            <span>Money clarity</span>
          </div>
        </div>

        <nav className="sidebar__nav">
          {navigation.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink key={item.to} to={item.to} end={item.to === "/"} className={({ isActive }) => `nav-link ${isActive ? "nav-link--active" : ""}`}>
                <Icon size={18} />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>

        <button type="button" className="ghost-button sidebar__logout" onClick={clearSession}>
          <LogOut size={16} />
          <span>Sign out</span>
        </button>
      </aside>

      <main className="main-panel">
        <header className="topbar">
          <div>
            <p className="eyebrow">Finance cockpit</p>
            <h1>{title}</h1>
          </div>
          <div className="topbar__profile">
            <span>{session?.displayName ?? "User"}</span>
            <small>{session?.email}</small>
          </div>
        </header>

        <div className="page-content">{children}</div>
      </main>
    </div>
  );
}
