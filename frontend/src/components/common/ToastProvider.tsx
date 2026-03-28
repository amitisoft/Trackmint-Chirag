import { createContext, useContext, useMemo, useState } from "react";

type Toast = {
  id: number;
  message: string;
  tone?: "success" | "error" | "warning";
};

type ToastContextValue = {
  showToast: (message: string, tone?: Toast["tone"]) => void;
};

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const value = useMemo(
    () => ({
      showToast: (message: string, tone: Toast["tone"] = "success") => {
        const toast: Toast = { id: Date.now(), message, tone };
        setToasts((current) => [...current, toast]);

        window.setTimeout(() => {
          setToasts((current) => current.filter((item) => item.id !== toast.id));
        }, 3000);
      },
    }),
    [],
  );

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="toast-stack">
        {toasts.map((toast) => (
          <div key={toast.id} className={`toast toast--${toast.tone ?? "success"}`}>
            {toast.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error("useToast must be used within ToastProvider");
  }

  return context;
}
