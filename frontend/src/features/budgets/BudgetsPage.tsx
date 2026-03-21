import { useMemo, useState } from "react";
import { useForm, type UseFormReturn } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { Modal } from "../../components/common/Modal";
import { OverflowMenu } from "../../components/common/OverflowMenu";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import type { Budget, Category } from "../../types/models";

const today = new Date();

const budgetSchema = z.object({
  categoryId: z.string().min(1),
  month: z.number().min(1).max(12),
  year: z.number().min(2024),
  amount: z.number().positive(),
  alertThresholdPercent: z.number().min(1).max(200),
});

type BudgetFormValues = z.infer<typeof budgetSchema>;

export function BudgetsPage() {
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editing, setEditing] = useState<Budget | null>(null);
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  const { data: categories = [] } = useQuery({
    queryKey: ["categories"],
    queryFn: async () => (await api.get<Category[]>("/categories")).data,
  });

  const { data: budgets = [] } = useQuery({
    queryKey: ["budgets", today.getMonth() + 1, today.getFullYear()],
    queryFn: async () =>
      (await api.get<Budget[]>("/budgets", { params: { month: today.getMonth() + 1, year: today.getFullYear() } })).data,
  });

  const form = useForm<BudgetFormValues>({
    resolver: zodResolver(budgetSchema),
    values: useMemo(
      () => ({
        categoryId: editing?.categoryId ?? "",
        month: editing?.month ?? today.getMonth() + 1,
        year: editing?.year ?? today.getFullYear(),
        amount: editing?.amount ?? 0,
        alertThresholdPercent: editing?.alertThresholdPercent ?? 80,
      }),
      [editing],
    ),
  });

  const mutation = useMutation({
    mutationFn: async (values: BudgetFormValues) => {
      if (editing) {
        await api.put(`/budgets/${editing.id}`, { amount: values.amount, alertThresholdPercent: values.alertThresholdPercent });
      } else {
        await api.post("/budgets", values);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      showToast(editing ? "Budget updated" : "Budget created");
      setIsCreateOpen(false);
      setEditing(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => api.delete(`/budgets/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      showToast("Budget deleted");
    },
  });

  return (
    <AppShell title="Budgets">
      <Card
        title="Current month budgets"
        subtitle="Budget vs actual spend with visual utilization."
        className="page-section page-section--list"
        actions={
          <button type="button" className="primary-button" onClick={() => {
            setEditing(null);
            setIsCreateOpen(true);
          }}>
            Set budget
          </button>
        }
      >
          <div className="budget-card-stack">
            {budgets.map((budget) => (
              <div key={budget.id} className="budget-item">
                <div className="budget-item__header">
                  <div className="budget-item__copy">
                    <strong>{budget.categoryName}</strong>
                    <span className="budget-item__amount">
                      {budget.actualSpend.toFixed(0)} / {budget.amount.toFixed(0)}
                    </span>
                  </div>
                  <OverflowMenu
                    actions={[
                      { label: "Edit", onClick: () => setEditing(budget) },
                      { label: "Delete", onClick: () => deleteMutation.mutate(budget.id), tone: "danger" },
                    ]}
                  />
                </div>
                <div className="progress-track">
                  <div className="progress-fill" style={{ width: `${Math.min(budget.utilizationPercent, 100)}%`, background: budget.categoryColor }} />
                </div>
                <small className="muted-copy">{budget.utilizationPercent.toFixed(0)}% utilized</small>
              </div>
            ))}
            {!budgets.length && <div className="empty-state">No budgets for this month yet. Use the button above to create one.</div>}
          </div>
      </Card>

      <Modal open={isCreateOpen || Boolean(editing)} title={editing ? `Edit ${editing.categoryName}` : "Set budget"} onClose={() => {
        setIsCreateOpen(false);
        setEditing(null);
      }}>
        <BudgetForm categories={categories} form={form} onSubmit={(values) => mutation.mutate(values)} isLoading={mutation.isPending} disabledCategory={Boolean(editing)} />
      </Modal>
    </AppShell>
  );
}

function BudgetForm({
  categories,
  form,
  onSubmit,
  isLoading,
  disabledCategory,
}: {
  categories: Category[];
  form: UseFormReturn<BudgetFormValues>;
  onSubmit: (values: BudgetFormValues) => void;
  isLoading: boolean;
  disabledCategory?: boolean;
}) {
  return (
    <form className="form-grid" onSubmit={form.handleSubmit(onSubmit)}>
      <label>
        Category
        <select {...form.register("categoryId")} disabled={disabledCategory || !categories.filter((category) => category.type === "Expense").length}>
          <option value="">{categories.filter((category) => category.type === "Expense").length ? "Select category" : "Create an expense category first"}</option>
          {categories
            .filter((category) => category.type === "Expense")
            .map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
        </select>
        {!categories.filter((category) => category.type === "Expense").length && <small className="field-hint field-hint--warning">No expense category available. Create one in Settings first.</small>}
      </label>
      <label>
        Month
        <input type="number" {...form.register("month", { valueAsNumber: true })} disabled={disabledCategory} />
      </label>
      <label>
        Year
        <input type="number" {...form.register("year", { valueAsNumber: true })} disabled={disabledCategory} />
      </label>
      <label>
        Budget amount
        <input type="number" step="0.01" {...form.register("amount", { valueAsNumber: true })} />
      </label>
      <label>
        Alert threshold %
        <input type="number" {...form.register("alertThresholdPercent", { valueAsNumber: true })} />
      </label>
      <button type="submit" className="primary-button" disabled={isLoading}>
        {isLoading ? "Saving..." : "Save budget"}
      </button>
    </form>
  );
}
