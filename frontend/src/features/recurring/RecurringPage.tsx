import { useMemo, useState } from "react";
import { useForm, type UseFormReturn } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { Modal } from "../../components/common/Modal";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import type { Account, Category, RecurringFrequency, RecurringTransaction, TransactionType } from "../../types/models";

const recurringSchema = z.object({
  title: z.string().min(2),
  type: z.enum(["Income", "Expense", "Transfer"] as const),
  amount: z.number().positive(),
  categoryId: z.string().optional(),
  accountId: z.string().min(1),
  destinationAccountId: z.string().optional(),
  frequency: z.enum(["Daily", "Weekly", "Monthly", "Yearly"] as const),
  startDate: z.string().min(1),
  endDate: z.string().optional(),
  nextRunDate: z.string().optional(),
  autoCreateTransaction: z.boolean(),
  isPaused: z.boolean(),
});

type RecurringFormValues = z.infer<typeof recurringSchema>;

export function RecurringPage() {
  const [editing, setEditing] = useState<RecurringTransaction | null>(null);
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  const { data: accounts = [] } = useQuery({
    queryKey: ["accounts"],
    queryFn: async () => (await api.get<Account[]>("/accounts")).data,
  });

  const { data: categories = [] } = useQuery({
    queryKey: ["categories"],
    queryFn: async () => (await api.get<Category[]>("/categories")).data,
  });

  const { data: items = [] } = useQuery({
    queryKey: ["recurring"],
    queryFn: async () => (await api.get<RecurringTransaction[]>("/recurring")).data,
  });

  const form = useForm<RecurringFormValues>({
    resolver: zodResolver(recurringSchema),
    values: useMemo(
      () => ({
        title: editing?.title ?? "",
        type: editing?.type ?? ("Expense" as TransactionType),
        amount: editing?.amount ?? 0,
        categoryId: editing?.categoryId ?? "",
        accountId: editing?.accountId ?? accounts[0]?.id ?? "",
        destinationAccountId: editing?.destinationAccountId ?? "",
        frequency: editing?.frequency ?? ("Monthly" as RecurringFrequency),
        startDate: editing?.startDate ?? new Date().toISOString().slice(0, 10),
        endDate: editing?.endDate ?? "",
        nextRunDate: editing?.nextRunDate ?? "",
        autoCreateTransaction: editing?.autoCreateTransaction ?? true,
        isPaused: editing?.isPaused ?? false,
      }),
      [editing, accounts],
    ),
  });

  const mutation = useMutation({
    mutationFn: async (values: RecurringFormValues) => {
      const payload = {
        ...values,
        categoryId: values.type === "Transfer" ? undefined : values.categoryId || undefined,
        destinationAccountId: values.type === "Transfer" ? values.destinationAccountId || undefined : undefined,
        endDate: values.endDate || undefined,
        nextRunDate: values.nextRunDate || values.startDate,
      };

      if (editing) {
        await api.put(`/recurring/${editing.id}`, payload);
      } else {
        await api.post("/recurring", payload);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recurring"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      showToast(editing ? "Recurring item updated" : "Recurring item created");
      setEditing(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => api.delete(`/recurring/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recurring"] });
      showToast("Recurring item deleted");
    },
  });

  return (
    <AppShell title="Recurring Transactions">
      <div className="page-grid page-grid--two">
        <Card title="Recurring schedule" subtitle="Bills, subscriptions, salaries, and auto-generated items.">
          <div className="list-stack">
            {items.map((item) => (
              <div key={item.id} className="list-row list-row--aligned">
                <div>
                  <strong>{item.title}</strong>
                  <span>{item.frequency} {"\u00B7"} next run {item.nextRunDate}</span>
                </div>
                <div className="inline-actions">
                  <button type="button" className="ghost-button" onClick={() => setEditing(item)}>
                    Edit
                  </button>
                  <button type="button" className="ghost-button ghost-button--danger" onClick={() => deleteMutation.mutate(item.id)}>
                    Delete
                  </button>
                </div>
              </div>
            ))}
            {!items.length && <div className="empty-state">No recurring items yet. Add subscriptions, salary, or bills here.</div>}
          </div>
        </Card>

        <Card title="New recurring item" subtitle="Set frequency, account, and optional auto-generation.">
          <RecurringForm form={form} accounts={accounts} categories={categories} onSubmit={(values) => mutation.mutate(values)} isLoading={mutation.isPending} />
        </Card>
      </div>

      <Modal open={Boolean(editing)} title={`Edit ${editing?.title ?? "item"}`} onClose={() => setEditing(null)}>
        <RecurringForm form={form} accounts={accounts} categories={categories} onSubmit={(values) => mutation.mutate(values)} isLoading={mutation.isPending} />
      </Modal>
    </AppShell>
  );
}

function RecurringForm({
  form,
  accounts,
  categories,
  onSubmit,
  isLoading,
}: {
  form: UseFormReturn<RecurringFormValues>;
  accounts: Account[];
  categories: Category[];
  onSubmit: (values: RecurringFormValues) => void;
  isLoading: boolean;
}) {
  return (
    <form className="form-grid" onSubmit={form.handleSubmit(onSubmit)}>
      <label>
        Title
        <input type="text" {...form.register("title")} />
      </label>
      <label>
        Type
        <select {...form.register("type")}>
          <option value="Expense">Expense</option>
          <option value="Income">Income</option>
          <option value="Transfer">Transfer</option>
        </select>
      </label>
      <label>
        Amount
        <input type="number" step="0.01" {...form.register("amount", { valueAsNumber: true })} />
      </label>
      <label>
        Source account
        <select {...form.register("accountId")}>
          {accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name}
            </option>
          ))}
        </select>
      </label>
      {form.watch("type") === "Transfer" ? (
        <label>
          Destination account
          <select {...form.register("destinationAccountId")}>
            <option value="">Select account</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>
        </label>
      ) : (
        <label>
          Category
          <select {...form.register("categoryId")}>
            <option value="">Select category</option>
            {categories
              .filter((category) => category.type === form.watch("type"))
              .map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
          </select>
        </label>
      )}
      <label>
        Frequency
        <select {...form.register("frequency")}>
          {(["Daily", "Weekly", "Monthly", "Yearly"] as RecurringFrequency[]).map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>
      </label>
      <label>
        Start date
        <input type="date" {...form.register("startDate")} />
      </label>
      <label>
        End date
        <input type="date" {...form.register("endDate")} />
      </label>
      <label>
        Next run date
        <input type="date" {...form.register("nextRunDate")} />
      </label>
      <label className="checkbox-row">
        <input type="checkbox" {...form.register("autoCreateTransaction")} />
        Auto-create transaction
      </label>
      <label className="checkbox-row">
        <input type="checkbox" {...form.register("isPaused")} />
        Paused
      </label>
      <button type="submit" className="primary-button" disabled={isLoading}>
        {isLoading ? "Saving..." : "Save recurring item"}
      </button>
    </form>
  );
}
