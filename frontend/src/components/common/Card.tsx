import type { PropsWithChildren } from "react";

type CardProps = PropsWithChildren<{
  title?: string;
  subtitle?: string;
  actions?: React.ReactNode;
  className?: string;
}>;

export function Card({ title, subtitle, actions, className, children }: CardProps) {
  return (
    <section className={`card ${className ?? ""}`.trim()}>
      {(title || subtitle || actions) && (
        <header className="card__header">
          <div>
            {title && <h3>{title}</h3>}
            {subtitle && <p>{subtitle}</p>}
          </div>
          {actions && <div className="card__actions">{actions}</div>}
        </header>
      )}
      {children}
    </section>
  );
}
