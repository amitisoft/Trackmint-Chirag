import { useMemo, useState } from "react";
import { useForm, type UseFormReturn } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { Modal } from "../../components/common/Modal";
import { FieldHelp } from "../../components/common/FieldHelp";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import type { Account, Goal, GoalStatus } from "../../types/models";

const goalSchema = z.object({
  name: z.string().min(2),
  targetAmount: z.number().positive(),
  targetDate: z.string().optional(),
  linkedAccountId: z.string().optional(),
  icon: z.string().min(2),
  color: z.string().min(4),
  status: z.enum(["Active", "Completed", "Paused"] as const),
});

const contributionSchema = z.object({
  amount: z.number().positive(),
  sourceAccountId: z.string().optional(),
});

type GoalFormValues = z.infer<typeof goalSchema>;
type ContributionFormValues = z.infer<typeof contributionSchema>;

export function GoalsPage() {
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editing, setEditing] = useState<Goal | null>(null);
  const [activeGoal, setActiveGoal] = useState<Goal | null>(null);
  const [contributionMode, setContributionMode] = useState<"contribute" | "withdraw">("contribute");
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  const { data: accounts = [] } = useQuery({
    queryKey: ["accounts"],
    queryFn: async () => (await api.get<Account[]>("/accounts")).data,
  });

  const { data: goals = [] } = useQuery({
    queryKey: ["goals"],
    queryFn: async () => (await api.get<Goal[]>("/goals")).data,
  });

  const form = useForm<GoalFormValues>({
    resolver: zodResolver(goalSchema),
    mode: "onChange",
    values: useMemo(
      () => ({
        name: editing?.name ?? "",
        targetAmount: editing?.targetAmount ?? 0,
        targetDate: editing?.targetDate ?? "",
        linkedAccountId: editing?.linkedAccountId ?? "",
        icon: editing?.icon ?? "target",
        color: editing?.color ?? "#10b981",
        status: editing?.status ?? ("Active" as GoalStatus),
      }),
      [editing],
    ),
  });

  const contributionForm = useForm<ContributionFormValues>({
    resolver: zodResolver(contributionSchema),
    mode: "onChange",
    defaultValues: {
      amount: 0,
      sourceAccountId: "",
    },
  });

  const saveMutation = useMutation({
    mutationFn: async (values: GoalFormValues) => {
      const payload = {
        ...values,
        targetDate: values.targetDate || undefined,
        linkedAccountId: values.linkedAccountId || undefined,
      };

      if (editing) {
        await api.put(`/goals/${editing.id}`, payload);
      } else {
        await api.post("/goals", payload);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["goals"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      showToast(editing ? "Goal updated" : "Goal created");
      setIsCreateOpen(false);
      setEditing(null);
    },
  });

  const contributionMutation = useMutation({
    mutationFn: async (values: ContributionFormValues) => {
      if (!activeGoal) return;
      const endpoint = contributionMode === "contribute" ? "contribute" : "withdraw";
      await api.post(`/goals/${activeGoal.id}/${endpoint}`, {
        amount: values.amount,
        sourceAccountId: values.sourceAccountId || undefined,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["goals"] });
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      showToast(contributionMode === "contribute" ? "Contribution added" : "Withdrawal recorded");
      setActiveGoal(null);
    },
  });

  return (
    <AppShell title="Savings Goals">
      <Card
        title="Goal progress"
        subtitle="Track funding status, contributions, and withdrawals."
        className="page-section page-section--list"
        actions={
          <button type="button" className="primary-button" onClick={() => {
            setEditing(null);
            setIsCreateOpen(true);
          }}>
            Create goal
          </button>
        }
      >
          <div className="list-stack">
            {goals.map((goal) => (
              <div key={goal.id} className="budget-item">
                <div className="budget-item__header">
                  <div className="goal-item__copy">
                    <div className="goal-item__title-row">
                      <strong>{goal.name}</strong>
                      <span className={`status-badge status-badge--${goal.status.toLowerCase()}`}>{goal.status}</span>
                    </div>
                    <span className="goal-item__amount">
                      {goal.currentAmount.toFixed(0)} / {goal.targetAmount.toFixed(0)}
                    </span>
                  </div>
                  <div className="inline-actions">
                    <button type="button" className="ghost-button" onClick={() => setEditing(goal)}>
                      Edit
                    </button>
                    <button
                      type="button"
                      className="ghost-button"
                      onClick={() => {
                        setContributionMode("contribute");
                        setActiveGoal(goal);
                      }}
                    >
                      Contribute
                    </button>
                    <button
                      type="button"
                      className="ghost-button"
                      onClick={() => {
                        setContributionMode("withdraw");
                        setActiveGoal(goal);
                      }}
                    >
                      Withdraw
                    </button>
                  </div>
                </div>
                <div className="progress-track">
                  <div className="progress-fill" style={{ width: `${Math.min(goal.progressPercent, 100)}%`, background: goal.color }} />
                </div>
              </div>
            ))}
            {!goals.length && <div className="empty-state">No savings goals yet. Use the button above to create one.</div>}
          </div>
      </Card>

      <Modal open={isCreateOpen || Boolean(editing)} title={editing ? `Edit ${editing.name}` : "Create goal"} onClose={() => {
        setIsCreateOpen(false);
        setEditing(null);
      }}>
        <GoalForm form={form} accounts={accounts} onSubmit={(values) => saveMutation.mutate(values)} isLoading={saveMutation.isPending} />
      </Modal>

      <Modal open={Boolean(activeGoal)} title={`${contributionMode === "contribute" ? "Contribute to" : "Withdraw from"} ${activeGoal?.name ?? ""}`} onClose={() => setActiveGoal(null)}>
        <form className="form-grid" onSubmit={contributionForm.handleSubmit((values) => contributionMutation.mutate(values))}>
          <label>
            Amount <span className="required-marker">*</span>
            <input type="number" step="0.01" {...contributionForm.register("amount", { valueAsNumber: true })} />
          </label>
          <label>
            Account
            <select {...contributionForm.register("sourceAccountId")}>
              <option value="">Optional account</option>
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.name}
                </option>
              ))}
            </select>
          </label>
          {!contributionForm.formState.isValid && <small className="field-hint field-hint--warning">Complete required fields marked with * to continue.</small>}
          <button type="submit" className="primary-button" disabled={!contributionForm.formState.isValid || contributionMutation.isPending}>
            {contributionMutation.isPending ? "Saving..." : contributionMode === "contribute" ? "Add contribution" : "Withdraw"}
          </button>
        </form>
      </Modal>
    </AppShell>
  );
}

function GoalForm({
  form,
  accounts,
  onSubmit,
  isLoading,
}: {
  form: UseFormReturn<GoalFormValues>;
  accounts: Account[];
  onSubmit: (values: GoalFormValues) => void;
  isLoading: boolean;
}) {
  const canSubmit = form.formState.isValid && !isLoading;

  return (
    <form className="form-grid" onSubmit={form.handleSubmit(onSubmit)}>
      <label>
        Goal name <span className="required-marker">*</span>
        <input type="text" {...form.register("name")} />
      </label>
      <label>
        Target amount <span className="required-marker">*</span>
        <input type="number" step="0.01" {...form.register("targetAmount", { valueAsNumber: true })} />
      </label>
      <label>
        Target date
        <input type="date" {...form.register("targetDate")} />
      </label>
      <label>
        <span className="label-inline">
          Linked account
          <FieldHelp text="Optional account used as funding source reference for this goal." />
        </span>
        <select {...form.register("linkedAccountId")}>
          <option value="">No linked account</option>
          {accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name}
            </option>
          ))}
        </select>
      </label>
      <label>
        Icon <span className="required-marker">*</span>
        <input type="text" {...form.register("icon")} />
      </label>
      <label>
        Color <span className="required-marker">*</span>
        <input type="text" {...form.register("color")} />
      </label>
      <label>
        <span className="label-inline">
          Status <span className="required-marker">*</span>
          <FieldHelp text="Active means in progress, Paused means on hold, Completed means goal achieved." />
        </span>
        <select {...form.register("status")}>
          <option value="Active">Active</option>
          <option value="Completed">Completed</option>
          <option value="Paused">Paused</option>
        </select>
      </label>
      {!form.formState.isValid && <small className="field-hint field-hint--warning">Complete required fields marked with * to continue.</small>}
      <button type="submit" className="primary-button" disabled={!canSubmit}>
        {isLoading ? "Saving..." : "Save goal"}
      </button>
    </form>
  );
}
