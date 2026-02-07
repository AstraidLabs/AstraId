import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";

type Props = {
  to: string;
  icon: ReactNode;
  label: string;
  onClick?: () => void;
  end?: boolean;
};

const navClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-2 rounded-xl px-3 py-2 text-sm transition ${
    isActive
      ? "bg-indigo-500/20 text-indigo-100 ring-1 ring-indigo-400/30"
      : "text-slate-300 hover:bg-slate-800 hover:text-white"
  }`;

export default function AccountNavItem({ to, icon, label, onClick, end }: Props) {
  return (
    <NavLink to={to} className={navClass} onClick={onClick} end={end}>
      {icon}
      {label}
    </NavLink>
  );
}
