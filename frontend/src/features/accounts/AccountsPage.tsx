import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { Modal } from "../../components/common/Modal";
import { OverflowMenu } from "../../components/common/OverflowMenu";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import type { Account, AccountMember, AccountMemberRole, AccountType } from "../../types/models";

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
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editing, setEditing] = useState<Account | null>(null);
  const [sharingAccount, setSharingAccount] = useState<Account | null>(null);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState<AccountMemberRole>("Editor");
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  const { data: accounts = [] } = useQuery({
    queryKey: ["accounts"],
    queryFn: async () => {
      const response = await api.get<Account[]>("/accounts");
      return response.data;
    },
  });

  const { data: members = [] } = useQuery({
    queryKey: ["account-members", sharingAccount?.id],
    enabled: Boolean(sharingAccount?.id),
    queryFn: async () => (await api.get<AccountMember[]>(`/accounts/${sharingAccount?.id}/members`)).data,
  });

  const form = useForm<AccountFormValues>({
    resolver: zodResolver(accountSchema),
    mode: "onChange",
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
      setIsCreateOpen(false);
      setEditing(null);
      form.reset({ name: "", type: "BankAccount", openingBalance: 0, institutionName: "" });
    },
  });

  const inviteMutation = useMutation({
    mutationFn: async () => {
      if (!sharingAccount) {
        return;
      }

      await api.post(`/accounts/${sharingAccount.id}/invite`, {
        email: inviteEmail.trim(),
        role: inviteRole,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["account-members", sharingAccount?.id] });
      showToast("Member invited");
      setInviteEmail("");
      setInviteRole("Editor");
    },
  });

  const updateRoleMutation = useMutation({
    mutationFn: async ({ accountId, userId, role }: { accountId: string; userId: string; role: AccountMemberRole }) => {
      await api.put(`/accounts/${accountId}/members/${userId}`, { role });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["account-members", sharingAccount?.id] });
      showToast("Member role updated");
    },
  });

  return (
    <AppShell title="Accounts">
      <Card
        title="Your accounts"
        subtitle="Balances across your bank, wallet, card, and savings accounts."
        className="page-section page-section--list"
        actions={
          <button
            type="button"
            className="primary-button"
            onClick={() => {
              setEditing(null);
              setIsCreateOpen(true);
            }}
          >
            New account
          </button>
        }
      >
        <div className="list-stack">
          {accounts.map((account) => (
            <div key={account.id} className="list-row list-row--account">
              <div>
                <strong>{account.name}</strong>
                <span>
                  {account.type} {"\u00B7"} {account.institutionName || "Institution not set"}
                </span>
                <small>
                  Access: {account.accessRole}
                  {account.isShared && account.ownerDisplayName ? ` \u00B7 Shared by ${account.ownerDisplayName}` : ""}
                </small>
              </div>
              <div className="inline-actions inline-actions--end">
                <strong>{formatCurrency(account.currentBalance)}</strong>
                <OverflowMenu
                  actions={[
                    ...(account.accessRole === "Owner"
                      ? [
                          {
                            label: "Share",
                            onClick: () => {
                              setSharingAccount(account);
                              setInviteEmail("");
                              setInviteRole("Editor");
                            },
                          },
                        ]
                      : []),
                    {
                      label: "Edit",
                      onClick: () => setEditing(account),
                    },
                  ]}
                />
              </div>
            </div>
          ))}
          {!accounts.length && <div className="empty-state">No accounts yet. Create one to start tracking balances.</div>}
        </div>
      </Card>

      <Modal
        open={isCreateOpen || Boolean(editing)}
        title={editing ? `Edit ${editing.name}` : "New account"}
        onClose={() => {
          setIsCreateOpen(false);
          setEditing(null);
        }}
      >
        <form className="form-grid" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
          <label>
            Name <span className="required-marker">*</span>
            <input type="text" {...form.register("name")} />
          </label>
          <label>
            Type <span className="required-marker">*</span>
            <select {...form.register("type")}>
              {accountTypes.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>
          <label>
            Opening balance <span className="required-marker">*</span>
            <input type="number" step="0.01" {...form.register("openingBalance", { valueAsNumber: true })} />
          </label>
          <label>
            Institution
            <input type="text" {...form.register("institutionName")} />
          </label>
          {!form.formState.isValid && <small className="field-hint field-hint--warning">Complete required fields marked with * to continue.</small>}
          <button type="submit" className="primary-button" disabled={!form.formState.isValid || mutation.isPending}>
            {mutation.isPending ? "Saving..." : editing ? "Update account" : "Save account"}
          </button>
        </form>
      </Modal>

      <Modal
        open={Boolean(sharingAccount)}
        title={sharingAccount ? `Share ${sharingAccount.name}` : "Share account"}
        onClose={() => setSharingAccount(null)}
      >
        <div className="form-grid">
          <label>
            Invite by email <span className="required-marker">*</span>
            <input type="email" value={inviteEmail} onChange={(event) => setInviteEmail(event.target.value)} placeholder="name@example.com" />
          </label>
          <label>
            Role <span className="required-marker">*</span>
            <select value={inviteRole} onChange={(event) => setInviteRole(event.target.value as AccountMemberRole)}>
              <option value="Editor">Editor</option>
              <option value="Viewer">Viewer</option>
            </select>
          </label>
          <button
            type="button"
            className="primary-button"
            disabled={!inviteEmail.trim() || inviteMutation.isPending || !sharingAccount}
            onClick={() => inviteMutation.mutate()}
          >
            {inviteMutation.isPending ? "Inviting..." : "Invite member"}
          </button>
        </div>

        <div className="list-stack" style={{ marginTop: "1rem" }}>
          {members.map((member) => (
            <div key={member.userId} className="list-row list-row--aligned">
              <div>
                <strong>{member.displayName}</strong>
                <span>{member.email}</span>
              </div>
              {member.isOwner ? (
                <span className="status-badge status-badge--active">Owner</span>
              ) : (
                <select
                  value={member.role}
                  onChange={(event) => {
                    if (!sharingAccount) {
                      return;
                    }

                    updateRoleMutation.mutate({
                      accountId: sharingAccount.id,
                      userId: member.userId,
                      role: event.target.value as AccountMemberRole,
                    });
                  }}
                >
                  <option value="Editor">Editor</option>
                  <option value="Viewer">Viewer</option>
                </select>
              )}
            </div>
          ))}
          {!members.length && <div className="empty-state empty-state--table">No members found.</div>}
        </div>
      </Modal>
    </AppShell>
  );
}
