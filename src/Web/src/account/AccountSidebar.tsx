import { useMemo } from "react";
import AccountNavItem from "../components/account/AccountNavItem";
import {
  HomeIcon,
  LockIcon,
  MailIcon,
  MonitorIcon,
  ShieldIcon,
  UserIcon
} from "../components/account/Icons";

type MenuItem = {
  to: string;
  label: string;
  icon: JSX.Element;
  end?: boolean;
};

type Props = {
  onNavigate?: () => void;
};

export default function AccountSidebar({ onNavigate }: Props) {
  const menuItems = useMemo<MenuItem[]>(
    () => [
      { to: "/account", label: "Overview", icon: <HomeIcon />, end: true },
      { to: "/account/profile", label: "Profile", icon: <UserIcon /> },
      { to: "/account/email", label: "Email", icon: <MailIcon /> },
      { to: "/account/password", label: "Password", icon: <LockIcon /> },
      { to: "/account/mfa", label: "MFA", icon: <ShieldIcon /> },
      { to: "/account/sessions", label: "Sessions", icon: <MonitorIcon /> },
      { to: "/account/security-events", label: "Security events", icon: <ShieldIcon /> }
    ],
    []
  );

  return (
    <nav className="space-y-1">
      <p className="px-3 pb-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Account</p>
      {menuItems.map((item) => (
        <AccountNavItem
          key={item.to}
          to={item.to}
          icon={item.icon}
          label={item.label}
          end={item.end}
          onClick={onNavigate}
        />
      ))}
    </nav>
  );
}
