import { useEffect, useRef, useState } from "react";
import { MoreHorizontal } from "lucide-react";

type OverflowAction = {
  label: string;
  onClick: () => void;
  tone?: "default" | "danger";
};

type OverflowMenuProps = {
  actions: OverflowAction[];
};

export function OverflowMenu({ actions }: OverflowMenuProps) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      if (!ref.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleEscape);

    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleEscape);
    };
  }, []);

  return (
    <div className="overflow-menu" ref={ref}>
      <button type="button" className="ghost-button overflow-menu__trigger" onClick={() => setOpen((current) => !current)} aria-label="More actions" aria-expanded={open}>
        <MoreHorizontal size={18} />
      </button>

      {open && (
        <div className="overflow-menu__panel">
          {actions.map((action) => (
            <button
              key={action.label}
              type="button"
              className={`overflow-menu__item ${action.tone === "danger" ? "overflow-menu__item--danger" : ""}`.trim()}
              onClick={() => {
                setOpen(false);
                action.onClick();
              }}
            >
              {action.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
