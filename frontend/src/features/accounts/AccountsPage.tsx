import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { Modal } from "../../components/common/Modal";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import type { Account, AccountType } from "../../types/models";

const accountTypes: AccountType[] = ["BankAccount", "CreditCard", "CashWallet", "SavingsAccount"];

const accountSchema = z.object({
  name: z.string().min(2),
  type: z.enum(accountTypes),
  openingBalance: z.number().min(0),
  institutionName: z.string().optional(),
});

type AccountFormValues = z.infer<typeof accountSchema>;

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(value);
}

export function AccountsPage() {
  const [editing, setEditing] = useState<Account | null>(null);
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  const { data: accounts = [] } = useQuery({
    queryKey: ["accounts"],
    queryFn: async () => {
      const response = await api.get<Account[]>("/accounts");
      return response.data;
    },
  });

  const form = useForm<AccountFormValues>({
    resolver: zodResolver(accountSchema),
    values: editing
      ? {
          name: editing.name,
          type: editing.type,
          openingBalance: editing.openingBalance,
          institutionName: editing.institutionName ?? "",
        }
      : {
          name: "",
          type: "BankAccount",
          openingBalance: 0,
          institutionName: "",
        },
  });

  const mutation = useMutation({
    mutationFn: async (values: AccountFormValues) => {
      if (editing) {
        await api.put(`/accounts/${editing.id}`, values);
      } else {
        await api.post("/accounts", values);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      showToast(editing ? "Account updated" : "Account created");
      setEditing(null);
      form.reset({ name: "", type: "BankAccount", openingBalance: 0, institutionName: "" });
    },
  });

  return (
    <AppShell title="Accounts">
      <div className="page-grid page-grid--two">
        <Card title="Your accounts" subtitle="Balances across your bank, wallet, card, and savings accounts." className="page-section page-section--list">
          <div className="list-stack">
            {accounts.map((account) => (
              <div key={account.id} className="list-row list-row--aligned">
                <div>
                  <strong>{account.name}</strong>
                  <span>
                    {account.type} {"\u00B7"} {account.institutionName || "Institution not set"}
                  </span>
                </div>
                <div className="inline-actions inline-actions--end">
                  <strong>{formatCurrency(account.currentBalance)}</strong>
                  <button type="button" className="ghost-button" onClick={() => setEditing(account)}>
                    Edit
                  </button>
                </div>
              </div>
            ))}
            {!accounts.length && <div className="empty-state">No accounts yet. Create one to start tracking balances.</div>}
          </div>
        </Card>

        <Card title="New account" subtitle="Create bank, cash, card, or savings accounts." className="page-section page-section--form">
          <form className="form-grid" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
            <label>
              Name
              <input type="text" {...form.register("name")} />
              <small>{form.formState.errors.name?.message}</small>
            </label>
            <label>
              Type
              <select {...form.register("type")}>
                {accountTypes.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Opening balance
              <input type="number" step="0.01" {...form.register("openingBalance", { valueAsNumber: true })} />
            </label>
            <label>
              Institution
              <input type="text" {...form.register("institutionName")} />
            </label>
            <button type="submit" className="primary-button" disabled={mutation.isPending}>
              {mutation.isPending ? "Saving..." : "Save account"}
            </button>
          </form>
        </Card>
      </div>

      <Modal open={Boolean(editing)} title={`Edit ${editing?.name ?? "account"}`} onClose={() => setEditing(null)}>
        <form className="form-grid" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
          <label>
            Name
            <input type="text" {...form.register("name")} />
          </label>
          <label>
            Type
            <select {...form.register("type")}>
              {accountTypes.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>
          <label>
            Opening balance
            <input type="number" step="0.01" {...form.register("openingBalance", { valueAsNumber: true })} />
          </label>
          <label>
            Institution
            <input type="text" {...form.register("institutionName")} />
          </label>
          <button type="submit" className="primary-button" disabled={mutation.isPending}>
            Update account
          </button>
        </form>
      </Modal>
    </AppShell>
  );
}
