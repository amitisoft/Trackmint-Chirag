import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../components/layout/AppShell";
import { Card } from "../../components/common/Card";
import { Modal } from "../../components/common/Modal";
import { OverflowMenu } from "../../components/common/OverflowMenu";
import { FieldHelp } from "../../components/common/FieldHelp";
import { api } from "../../services/api/client";
import { useToast } from "../../components/common/ToastProvider";
import type { Category, Rule, RuleActionType, RuleField, RuleOperator, TransactionType } from "../../types/models";

const ruleFields: RuleField[] = ["Merchant", "Amount", "Category", "TransactionType"];
const ruleOperators: RuleOperator[] = ["Equals", "Contains", "GreaterThan", "LessThan"];
const ruleActions: RuleActionType[] = ["SetCategory", "AddTag", "TriggerAlert"];
const transactionTypes: TransactionType[] = ["Income", "Expense", "Transfer"];

const ruleSchema = z.object({
  name: z.string().min(2, "Rule name is required"),
  conditionField: z.enum(ruleFields),
  conditionOperator: z.enum(ruleOperators),
  conditionValue: z.string().min(1, "Condition value is required"),
  actionType: z.enum(ruleActions),
  actionValue: z.string().min(1, "Action value is required"),
  priority: z.number().int().min(1).max(1000),
  isActive: z.boolean(),
});

type RuleFormValues = z.infer<typeof ruleSchema>;

function buildRuleDescription(rule: Rule) {
  return `If ${rule.conditionField} ${rule.conditionOperator} "${rule.conditionValue}", then ${rule.actionType} ${rule.actionValue}`;
}

export function RulesPage() {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editing, setEditing] = useState<Rule | null>(null);
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  const { data: rules = [] } = useQuery({
    queryKey: ["rules"],
    queryFn: async () => (await api.get<Rule[]>("/rules")).data,
  });

  const { data: categories = [] } = useQuery({
    queryKey: ["categories"],
    queryFn: async () => (await api.get<Category[]>("/categories")).data,
  });

  const defaults = useMemo<RuleFormValues>(
    () =>
      editing
        ? {
            name: editing.name,
            conditionField: editing.conditionField,
            conditionOperator: editing.conditionOperator,
            conditionValue: editing.conditionValue,
            actionType: editing.actionType,
            actionValue: editing.actionValue,
            priority: editing.priority,
            isActive: editing.isActive,
          }
        : {
            name: "",
            conditionField: "Merchant",
            conditionOperator: "Contains",
            conditionValue: "",
            actionType: "AddTag",
            actionValue: "",
            priority: 100,
            isActive: true,
          },
    [editing],
  );

  const form = useForm<RuleFormValues>({
    resolver: zodResolver(ruleSchema),
    mode: "onChange",
    values: defaults,
  });

  const saveMutation = useMutation({
    mutationFn: async (values: RuleFormValues) => {
      if (editing) {
        await api.put(`/rules/${editing.id}`, values);
      } else {
        await api.post("/rules", values);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["rules"] });
      showToast(editing ? "Rule updated" : "Rule created");
      setEditing(null);
      setIsFormOpen(false);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (ruleId: string) => {
      await api.delete(`/rules/${ruleId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["rules"] });
      showToast("Rule deleted");
    },
  });

  const selectedConditionField = form.watch("conditionField");
  const selectedActionType = form.watch("actionType");
  const canSubmit = form.formState.isValid && !saveMutation.isPending;

  return (
    <AppShell title="Rules Engine">
      <Card
        title="Automation rules"
        subtitle="Run auto-categorization, tagging, and alert hints whenever new transactions arrive."
        className="page-section page-section--list"
        actions={
          <button
            type="button"
            className="primary-button"
            onClick={() => {
              setEditing(null);
              setIsFormOpen(true);
            }}
          >
            New rule
          </button>
        }
      >
        <div className="list-stack">
          {rules.map((rule) => (
            <div key={rule.id} className="list-row list-row--aligned">
              <div>
                <strong>{rule.name}</strong>
                <span>{buildRuleDescription(rule)}</span>
                <small>Priority {rule.priority}</small>
              </div>
              <div className="inline-actions inline-actions--end rules-row__actions">
                <span className={`status-badge ${rule.isActive ? "status-badge--completed" : "status-badge--paused"}`.trim()}>{rule.isActive ? "Active" : "Paused"}</span>
                <OverflowMenu
                  actions={[
                    {
                      label: "Edit",
                      onClick: () => {
                        setEditing(rule);
                        setIsFormOpen(true);
                      },
                    },
                    {
                      label: "Delete",
                      onClick: () => deleteMutation.mutate(rule.id),
                      tone: "danger",
                    },
                  ]}
                />
              </div>
            </div>
          ))}
          {!rules.length && <div className="empty-state empty-state--table">No rules yet. Add your first automation rule.</div>}
        </div>
      </Card>

      <Modal
        open={isFormOpen || editing !== null}
        title={editing ? `Edit ${editing.name}` : "New rule"}
        onClose={() => {
          setIsFormOpen(false);
          setEditing(null);
        }}
      >
        <form className="form-grid" onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}>
          <label>
            <span className="label-inline">
              Rule name <span className="required-marker">*</span>
              <FieldHelp text="Use a clear name so you can quickly identify what this automation does." />
            </span>
            <input type="text" {...form.register("name")} />
          </label>
          <label>
            <span className="label-inline">
              Priority <span className="required-marker">*</span>
              <FieldHelp text="Lower number runs first. Example: 1 runs before 100." />
            </span>
            <input type="number" {...form.register("priority", { valueAsNumber: true })} />
            <small className="field-hint">Lower number runs first. Use 1 for highest priority.</small>
          </label>

          <label>
            <span className="label-inline">
              Condition field <span className="required-marker">*</span>
              <FieldHelp text="Pick what part of a transaction this rule should check, like Merchant or Amount." />
            </span>
            <select {...form.register("conditionField")}>
              {ruleFields.map((field) => (
                <option key={field} value={field}>
                  {field}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span className="label-inline">
              Operator <span className="required-marker">*</span>
              <FieldHelp text="Defines how comparison happens. Example: Contains for text, GreaterThan for amounts." />
            </span>
            <select {...form.register("conditionOperator")}>
              {ruleOperators.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
          </label>

          {selectedConditionField === "TransactionType" ? (
            <label>
              <span className="label-inline">
                Condition value <span className="required-marker">*</span>
                <FieldHelp text="The exact value this rule matches against the selected field." />
              </span>
              <select {...form.register("conditionValue")}>
                <option value="">Select type</option>
                {transactionTypes.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </label>
          ) : (
            <label>
              <span className="label-inline">
                Condition value <span className="required-marker">*</span>
                <FieldHelp text="The text or number to compare with the condition field." />
              </span>
              <input type={selectedConditionField === "Amount" ? "number" : "text"} step={selectedConditionField === "Amount" ? "0.01" : undefined} {...form.register("conditionValue")} />
            </label>
          )}

          <label>
            <span className="label-inline">
              Action type <span className="required-marker">*</span>
              <FieldHelp text="What should happen when condition matches: set category, add tag, or trigger alert." />
            </span>
            <select {...form.register("actionType")}>
              {ruleActions.map((action) => (
                <option key={action} value={action}>
                  {action}
                </option>
              ))}
            </select>
          </label>

          {selectedActionType === "SetCategory" ? (
            <label>
              <span className="label-inline">
                Action value (category) <span className="required-marker">*</span>
                <FieldHelp text="Select the category to auto-assign when this rule matches." />
              </span>
              <select {...form.register("actionValue")} disabled={!categories.length}>
                <option value="">{categories.length ? "Select category" : "Create category first"}</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
              {!categories.length && <small className="field-hint field-hint--warning">No categories available. Create one in Settings first.</small>}
            </label>
          ) : (
            <label>
              <span className="label-inline">
                Action value <span className="required-marker">*</span>
                <FieldHelp text="Tag text or alert message to apply when this rule matches." />
              </span>
              <input type="text" placeholder={selectedActionType === "AddTag" ? "Example: groceries" : "Example: High amount expense"} {...form.register("actionValue")} />
            </label>
          )}

          <label className="checkbox-row">
            <input type="checkbox" {...form.register("isActive")} />
            <span>Rule is active</span>
          </label>

          {!form.formState.isValid && <small className="field-hint field-hint--warning">Complete required fields marked with * to continue.</small>}
          <button type="submit" className="primary-button" disabled={!canSubmit}>
            {saveMutation.isPending ? "Saving..." : editing ? "Update rule" : "Save rule"}
          </button>
        </form>
      </Modal>
    </AppShell>
  );
}
