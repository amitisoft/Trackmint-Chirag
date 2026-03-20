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
import type { Category, CategoryType } from "../../types/models";

const categorySchema = z.object({
  name: z.string().min(2),
  type: z.enum(["Income", "Expense"] as const),
  color: z.string().min(4),
  icon: z.string().min(2),
});

export function SettingsPage() {
  const [editing, setEditing] = useState<Category | null>(null);
  const queryClient = useQueryClient();
  const { showToast } = useToast();

  const { data: categories = [] } = useQuery({
    queryKey: ["categories"],
    queryFn: async () => {
      const response = await api.get<Category[]>("/categories");
      return response.data;
    },
  });

  const form = useForm<z.infer<typeof categorySchema>>({
    resolver: zodResolver(categorySchema),
    values: editing
      ? {
          name: editing.name,
          type: editing.type,
          color: editing.color,
          icon: editing.icon,
        }
      : {
          name: "",
          type: "Expense",
          color: "#3b82f6",
          icon: "wallet",
        },
  });

  const saveMutation = useMutation({
    mutationFn: async (values: z.infer<typeof categorySchema>) => {
      if (editing) {
        await api.put(`/categories/${editing.id}`, { ...values, isArchived: editing.isArchived });
      } else {
        await api.post("/categories", values);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["categories"] });
      showToast(editing ? "Category updated" : "Category created");
      setEditing(null);
    },
  });

  const archiveMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/categories/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["categories"] });
      showToast("Category archived");
    },
  });

  return (
    <AppShell title="Settings & Categories">
      <div className="page-grid page-grid--two">
        <Card title="Categories" subtitle="Manage custom income and expense categories.">
          <div className="list-stack">
            {categories.map((category) => (
              <div key={category.id} className="list-row list-row--aligned">
                <div className="category-chip category-chip--details">
                  <span className="category-chip__dot category-chip__dot--large" style={{ background: category.color }} />
                  <div className="category-chip__meta">
                    <strong>{category.name}</strong>
                    <span>
                      {category.type} {"\u00B7"} {category.icon}
                    </span>
                  </div>
                </div>
                <div className="inline-actions">
                  <button type="button" className="ghost-button" onClick={() => setEditing(category)}>
                    Edit
                  </button>
                  {!category.isArchived && (
                    <button type="button" className="ghost-button ghost-button--danger" onClick={() => archiveMutation.mutate(category.id)}>
                      Archive
                    </button>
                  )}
                </div>
              </div>
            ))}
            {!categories.length && <div className="empty-state">Categories will appear here after you add or import them.</div>}
          </div>
        </Card>

        <Card title="New category" subtitle="Keep your reporting taxonomy clean and consistent.">
          <CategoryForm onSubmit={(values) => saveMutation.mutate(values)} form={form} isLoading={saveMutation.isPending} />
        </Card>
      </div>

      <Modal open={Boolean(editing)} title={`Edit ${editing?.name ?? "category"}`} onClose={() => setEditing(null)}>
        <CategoryForm onSubmit={(values) => saveMutation.mutate(values)} form={form} isLoading={saveMutation.isPending} />
      </Modal>
    </AppShell>
  );
}

function CategoryForm({
  form,
  onSubmit,
  isLoading,
}: {
  form: ReturnType<typeof useForm<z.infer<typeof categorySchema>>>;
  onSubmit: (values: z.infer<typeof categorySchema>) => void;
  isLoading: boolean;
}) {
  return (
    <form className="form-grid" onSubmit={form.handleSubmit(onSubmit)}>
      <label>
        Category name
        <input type="text" {...form.register("name")} />
      </label>
      <label>
        Type
        <select {...form.register("type")}>
          {(["Expense", "Income"] as CategoryType[]).map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>
      </label>
      <label>
        Color
        <input type="text" {...form.register("color")} />
      </label>
      <label>
        Icon label
        <input type="text" {...form.register("icon")} />
      </label>
      <button type="submit" className="primary-button" disabled={isLoading}>
        {isLoading ? "Saving..." : "Save category"}
      </button>
    </form>
  );
}
