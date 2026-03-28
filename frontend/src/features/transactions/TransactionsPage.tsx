import { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { Modal } from "../../components/common/Modal";
import { OverflowMenu } from "../../components/common/OverflowMenu";
import { Pagination } from "../../components/common/Pagination";
import { FieldHelp } from "../../components/common/FieldHelp";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import type { Account, Budget, Category, PagedResult, Rule, Transaction, TransactionType } from "../../types/models";

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
}).superRefine((values, context) => {
  if (values.type !== "Transfer") {
    return;
  }

  if (!values.destinationAccountId) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: "Destination account is required for transfer",
      path: ["destinationAccountId"],
    });
  }

  if (values.destinationAccountId && values.destinationAccountId === values.accountId) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      message: "Destination account must be different from source account",
      path: ["destinationAccountId"],
    });
  }
});

type TransactionFormValues = z.infer<typeof transactionSchema>;

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(value);
}

function extractRuleAlerts(note?: string) {
  if (!note) {
    return [];
  }

  const matches = [...note.matchAll(/\[Rule Alert:\s*([^\]]+)\]/gi)];
  return matches
    .map((item) => item[1]?.trim())
    .filter((item): item is string => Boolean(item));
}

async function getBudgetThresholdAlert(transaction: Transaction) {
  if (transaction.type !== "Expense" || !transaction.categoryId) {
    return null;
  }

  const [yearText, monthText] = transaction.date.split("-");
  const year = Number(yearText);
  const month = Number(monthText);
  if (!Number.isFinite(year) || !Number.isFinite(month) || month < 1 || month > 12) {
    return null;
  }

  const budgets = (await api.get<Budget[]>("/budgets", { params: { month, year } })).data;
  const matchingBudget = budgets.find((item) => item.categoryId === transaction.categoryId);
  if (!matchingBudget) {
    return null;
  }

  if (matchingBudget.utilizationPercent < matchingBudget.alertThresholdPercent) {
    return null;
  }

  return `${matchingBudget.categoryName} crossed budget alert at ${matchingBudget.utilizationPercent.toFixed(0)}% (threshold ${matchingBudget.alertThresholdPercent}%).`;
}

export function TransactionsPage() {
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editing, setEditing] = useState<Transaction | null>(null);
  const [filters, setFilters] = useState({ search: "", type: "All", accountId: "All", categoryId: "All" });
  const [page, setPage] = useState(1);
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const pageSize = 10;

  const { data: accounts = [] } = useQuery({
    queryKey: ["accounts"],
    queryFn: async () => (await api.get<Account[]>("/accounts")).data,
  });

  const { data: categories = [] } = useQuery({
    queryKey: ["categories"],
    queryFn: async () => (await api.get<Category[]>("/categories")).data,
  });
  const { data: rules = [] } = useQuery({
    queryKey: ["rules"],
    queryFn: async () => (await api.get<Rule[]>("/rules")).data,
  });

  const { data } = useQuery({
    queryKey: ["transactions", filters, page],
    queryFn: async () => {
      const response = await api.get<PagedResult<Transaction>>("/transactions", {
        params: {
          search: filters.search || undefined,
          type: filters.type === "All" ? undefined : filters.type,
          accountId: filters.accountId === "All" ? undefined : filters.accountId,
          categoryId: filters.categoryId === "All" ? undefined : filters.categoryId,
          page,
          pageSize,
        },
      });
      return response.data;
    },
  });

  useEffect(() => {
    setPage(1);
  }, [filters.search, filters.type, filters.accountId, filters.categoryId]);

  const totalPages = Math.max(1, Math.ceil((data?.totalCount ?? 0) / pageSize));
  const currentPage = Math.min(page, totalPages);

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
    mode: "onChange",
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

      if (editing?.id) {
        const response = await api.put<Transaction>(`/transactions/${editing.id}`, payload);
        return response.data;
      } else {
        const response = await api.post<Transaction>("/transactions", payload);
        return response.data;
      }
    },
    onSuccess: async (savedTransaction) => {
      queryClient.invalidateQueries({ queryKey: ["transactions"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      const alerts = extractRuleAlerts(savedTransaction.note);
      const budgetAlert = await getBudgetThresholdAlert(savedTransaction);
      if (alerts.length) {
        showToast(`${editing ? "Transaction updated" : "Transaction saved"} • Alert: ${alerts.join(" | ")}`, "warning");
      } else {
        showToast(editing ? "Transaction updated" : "Transaction saved");
      }
      if (budgetAlert) {
        showToast(budgetAlert, "warning");
      }
      setIsCreateOpen(false);
      setEditing(null);
    },
    onError: (error: any) => {
      showToast(error?.response?.data?.message ?? "Unable to save transaction. Please check required fields.", "error");
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
  const selectedType = form.watch("type");
  const selectedCategoryId = form.watch("categoryId");
  const matchingCategories = categories.filter((category) => category.type === selectedType);
  const hasAutoCategoryRule = rules.some((rule) => rule.isActive && rule.actionType === "SetCategory");
  const requiresCategorySelection = selectedType !== "Transfer" && !hasAutoCategoryRule;
  const canSubmit = form.formState.isValid
    && !saveMutation.isPending
    && accounts.length > 0
    && (!requiresCategorySelection || Boolean(selectedCategoryId));

  return (
    <AppShell title="Transactions">
      <Card title="Filters" subtitle="Search by note or merchant and narrow by account, category, or transaction type." className="page-section page-section--filters">
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
          <button
            type="button"
            className="primary-button"
            onClick={() => {
              setEditing(null);
              setIsCreateOpen(true);
            }}
          >
            Add transaction
          </button>
        </div>
        {!accounts.length && <small className="field-hint field-hint--warning">No accounts yet. Create an account first before adding or filtering transactions meaningfully.</small>}
      </Card>

      <Card title="Transaction list" subtitle={`${data?.totalCount ?? 0} records`} className="page-section page-section--table">
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
                  <td className="table-actions">
                    <OverflowMenu
                      actions={[
                        {
                          label: "Edit",
                          onClick: () => setEditing(item),
                        },
                        {
                          label: "Delete",
                          onClick: () => deleteMutation.mutate(item.id),
                          tone: "danger",
                        },
                      ]}
                    />
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
        <Pagination page={currentPage} totalPages={totalPages} onPageChange={setPage} />
      </Card>

      <Modal
        open={isCreateOpen || editing !== null}
        title={editing?.id ? "Edit transaction" : "Add transaction"}
        onClose={() => {
          setIsCreateOpen(false);
          setEditing(null);
        }}
      >
        <form className="form-grid" onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}>
          <label>
            <span className="label-inline">
              Type <span className="required-marker">*</span>
              <FieldHelp text="Income adds money, Expense deducts money, Transfer moves money between accounts." />
            </span>
            <select {...form.register("type")}>
              <option value="Expense">Expense</option>
              <option value="Income">Income</option>
              <option value="Transfer">Transfer</option>
            </select>
          </label>
          <label>
            <span className="label-inline">
              Amount <span className="required-marker">*</span>
            </span>
            <input type="number" step="0.01" {...form.register("amount", { valueAsNumber: true })} />
          </label>
          <label>
            <span className="label-inline">
              Date <span className="required-marker">*</span>
            </span>
            <input type="date" {...form.register("date")} />
          </label>
          <label>
            <span className="label-inline">
              Source account <span className="required-marker">*</span>
              <FieldHelp text="Account from which this transaction amount is taken." />
            </span>
            <select {...form.register("accountId")} disabled={!accounts.length}>
              {!accounts.length && <option value="">Create an account first</option>}
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.name}
                </option>
              ))}
            </select>
            {!accounts.length && <small className="field-hint field-hint--warning">No source account available. Create one in Accounts first.</small>}
          </label>
          {selectedType === "Transfer" ? (
            <label>
              <span className="label-inline">
                Destination account <span className="required-marker">*</span>
                <FieldHelp text="Account where transfer amount should be added." />
              </span>
              <select {...form.register("destinationAccountId")} disabled={accounts.length < 2}>
                <option value="">Select account</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name}
                  </option>
                ))}
              </select>
              {accounts.length < 2 && <small className="field-hint field-hint--warning">Create at least two accounts before recording a transfer.</small>}
            </label>
          ) : (
            <label>
              <span className="label-inline">
                Category
                <FieldHelp text="Optional only when an active SetCategory rule is configured. Otherwise choose category manually." />
              </span>
              <select {...form.register("categoryId")} disabled={!matchingCategories.length}>
                <option value="">{matchingCategories.length ? "Select category" : "Create a matching category first"}</option>
                {categories
                  .filter((category) => category.type === selectedType)
                  .map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
              </select>
              {!matchingCategories.length ? (
                <small className="field-hint field-hint--warning">No {selectedType.toLowerCase()} category found. Create one in Settings first.</small>
              ) : (
                <small className="field-hint">Optional if your active rule auto-sets category.</small>
              )}
              {requiresCategorySelection && !selectedCategoryId && (
                <small className="field-hint field-hint--warning">Category is required because no active SetCategory rule is available.</small>
              )}
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
            <span className="label-inline">
              Tags
              <FieldHelp text="Enter comma-separated values like groceries, family, business." />
            </span>
            <input type="text" placeholder="groceries, family" {...form.register("tags")} />
          </label>
          {!form.formState.isValid && <small className="field-hint field-hint--warning">Complete required fields marked with * to continue.</small>}
          <button type="submit" className="primary-button" disabled={!canSubmit}>
            {saveMutation.isPending ? "Saving..." : "Save transaction"}
          </button>
        </form>
      </Modal>
    </AppShell>
  );
}
