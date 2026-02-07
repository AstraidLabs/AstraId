import type { LucideIcon } from "lucide-react";
import { Activity, KeyRound, LayoutDashboard, LogOut, Mail, MonitorOff, Settings, Shield, ShieldCheck, User } from "lucide-react";

export type AccountIconKey =
  | "profile"
  | "security"
  | "mfa"
  | "email"
  | "password"
  | "sessions"
  | "activity"
  | "admin"
  | "logout";

export const accountIcons: Record<AccountIconKey, LucideIcon> = {
  profile: LayoutDashboard,
  security: Shield,
  mfa: ShieldCheck,
  email: Mail,
  password: KeyRound,
  sessions: MonitorOff,
  activity: Activity,
  admin: Settings,
  logout: LogOut
};

export const accountMenuItems = [
  { key: "profile", label: "Profile", to: "/account", icon: accountIcons.profile },
  { key: "security", label: "Security", to: "/account/security", icon: accountIcons.security }
] as const;

export const securityItems = [
  { key: "mfa", label: "MFA management", to: "/account/security/mfa", description: "Set up, disable, and recover multi-factor authentication.", icon: accountIcons.mfa },
  { key: "email", label: "Change email", to: "/account/security/email", description: "Start a secured email change flow and confirm ownership.", icon: accountIcons.email },
  { key: "password", label: "Change password", to: "/account/security/password", description: "Update your password and refresh account protection.", icon: accountIcons.password },
  { key: "sessions", label: "Sign out all sessions", to: "/account/security/sessions", description: "Invalidate sessions on every other device.", icon: accountIcons.sessions },
  { key: "activity", label: "Recent login activity", to: "/account/security/activity", description: "Review recent sign-in successes, failures, and logout events.", icon: accountIcons.activity }
] as const;

export const accountCardIconClass = "h-5 w-5";
export const accountMenuIconClass = "h-4 w-4";
