import { CircleHelp } from "lucide-react";

type FieldHelpProps = {
  text: string;
};

export function FieldHelp({ text }: FieldHelpProps) {
  return (
    <span className="field-help" tabIndex={0} aria-label={text} role="note">
      <CircleHelp size={14} />
      <span className="field-help__tooltip">{text}</span>
    </span>
  );
}
