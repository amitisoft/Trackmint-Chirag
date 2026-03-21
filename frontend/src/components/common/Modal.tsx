import { X } from "lucide-react";
import type { PropsWithChildren } from "react";

type ModalProps = PropsWithChildren<{
  open: boolean;
  title: string;
  onClose: () => void;
}>;

export function Modal({ open, title, onClose, children }: ModalProps) {
  if (!open) {
    return null;
  }

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div className="modal" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
        <div className="modal__header">
          <h3>{title}</h3>
          <button type="button" className="ghost-button modal__close" onClick={onClose} aria-label="Close dialog">
            <X size={18} />
            <span>Close</span>
          </button>
        </div>
        <div className="modal__content">{children}</div>
      </div>
    </div>
  );
}
