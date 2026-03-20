import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { Modal } from "../../components/common/Modal";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import type { Account, Category, PagedResult, Transaction, TransactionType } from "../../types/models";

const transactionSchema = z.object({
  accountId: z.string().min(1),
  destinationAccountId: z.string().optional(),
  categoryId: z.string().optional(),
  type: z.enum(["Income", "Expense", "Transfer"] as const),
  amount: z.number().positive(),
  date: z.string().min(1),
  note: z.string().optional(),
  merchant: z.string().optional(),
  paymentMethod: z.string().optional(),
  tags: z.string().optional(),
});

type TransactionFormValues = z.infer<typeof transactionSchema>;

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(value);
}

export function TransactionsPage() {
  const [editing, setEditing] = useState<Transaction | null>(null);
  const [filters, setFilters] = useState({ search: "", type: "All", accountId: "All", categoryId: "All" });
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

  const { data } = useQuery({
    queryKey: ["transactions", filters],
    queryFn: async () => {
      const response = await api.get<PagedResult<Transaction>>("/transactions", {
        params: {
          search: filters.search || undefined,
          type: filters.type === "All" ? undefined : filters.type,
          accountId: filters.accountId === "All" ? undefined : filters.accountId,
          categoryId: filters.categoryId === "All" ? undefined : filters.categoryId,
          pageSize: 50,
        },
      });
      return response.data;
    },
  });

  const defaultValues = useMemo(
    () => ({
      accountId: editing?.accountId ?? accounts[0]?.id ?? "",
      destinationAccountId: editing?.destinationAccountId ?? "",
      categoryId: editing?.categoryId ?? "",
      type: editing?.type ?? ("Expense" as TransactionType),
      amount: editing?.amount ?? 0,
      date: editing?.date ?? new Date().toISOString().slice(0, 10),
      note: editing?.note ?? "",
      merchant: editing?.merchant ?? "",
      paymentMethod: editing?.paymentMethod ?? "",
      tags: editing?.tags?.join(", ") ?? "",
    }),
    [editing, accounts],
  );

  const form = useForm<TransactionFormValues>({
    resolver: zodResolver(transactionSchema),
    values: defaultValues,
  });

  const saveMutation = useMutation({
    mutationFn: async (values: TransactionFormValues) => {
      const payload = {
        ...values,
        destinationAccountId: values.destinationAccountId || undefined,
        categoryId: values.type === "Transfer" ? undefined : values.categoryId || undefined,
        tags: values.tags ? values.tags.split(",").map((item) => item.trim()).filter(Boolean) : [],
      };

      if (editing) {
        await api.put(`/transactions/${editing.id}`, payload);
      } else {
        await api.post("/transactions", payload);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["transactions"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      showToast(editing ? "Transaction updated" : "Transaction saved");
      setEditing(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/transactions/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["transactions"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      showToast("Transaction deleted");
    },
  });

  return (
    <AppShell title="Transactions">
      <Card title="Filters" subtitle="Search by note or merchant and narrow by account, category, or transaction type.">
        <div className="toolbar-grid">
          <input placeholder="Search merchant or note" value={filters.search} onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value }))} />
          <select value={filters.type} onChange={(event) => setFilters((current) => ({ ...current, type: event.target.value }))}>
            <option value="All">All types</option>
            <option value="Income">Income</option>
            <option value="Expense">Expense</option>
            <option value="Transfer">Transfer</option>
          </select>
          <select value={filters.accountId} onChange={(event) => setFilters((current) => ({ ...current, accountId: event.target.value }))}>
            <option value="All">All accounts</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>
          <select value={filters.categoryId} onChange={(event) => setFilters((current) => ({ ...current, categoryId: event.target.value }))}>
            <option value="All">All categories</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
          <button type="button" className="primary-button" onClick={() => setEditing({} as Transaction)}>
            Add transaction
          </button>
        </div>
      </Card>

      <Card title="Transaction list" subtitle={`${data?.totalCount ?? 0} records`}>
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Merchant</th>
                <th>Category</th>
                <th>Account</th>
                <th>Type</th>
                <th>Amount</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(data?.items ?? []).map((item) => (
                <tr key={item.id}>
                  <td>{item.date}</td>
                  <td>{item.merchant || "Manual entry"}</td>
                  <td>{item.categoryName || "Transfer"}</td>
                  <td>{item.accountName}</td>
                  <td>{item.type}</td>
                  <td>{formatCurrency(item.amount)}</td>
                  <td className="inline-actions">
                    <button type="button" className="ghost-button" onClick={() => setEditing(item)}>
                      Edit
                    </button>
                    <button type="button" className="ghost-button ghost-button--danger" onClick={() => deleteMutation.mutate(item.id)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
              {!(data?.items?.length ?? 0) && (
                <tr>
                  <td colSpan={7}>
                    <div className="empty-state empty-state--table">No transactions match the current filters.</div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>

      <Modal open={editing !== null} title={editing?.id ? "Edit transaction" : "Add transaction"} onClose={() => setEditing(null)}>
        <form className="form-grid" onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}>
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
            Date
            <input type="date" {...form.register("date")} />
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
            Merchant
            <input type="text" {...form.register("merchant")} />
          </label>
          <label>
            Payment method
            <input type="text" {...form.register("paymentMethod")} />
          </label>
          <label>
            Note
            <textarea rows={3} {...form.register("note")} />
          </label>
          <label>
            Tags
            <input type="text" placeholder="groceries, family" {...form.register("tags")} />
          </label>
          <button type="submit" className="primary-button" disabled={saveMutation.isPending}>
            {saveMutation.isPending ? "Saving..." : "Save transaction"}
          </button>
        </form>
      </Modal>
    </AppShell>
  );
}
