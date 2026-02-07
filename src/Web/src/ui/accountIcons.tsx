import type { SVGProps } from "react";

type IconProps = SVGProps<SVGSVGElement>;

type AccountIcon = (props: IconProps) => JSX.Element;

const Icon = ({ children, className, ...props }: IconProps) => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
    {...props}
  >
    {children}
  </svg>
);

const DashboardIcon: AccountIcon = (props) => (
  <Icon {...props}>
    <rect x="3" y="3" width="8" height="8" rx="1" />
    <rect x="13" y="3" width="8" height="5" rx="1" />
    <rect x="13" y="10" width="8" height="11" rx="1" />
    <rect x="3" y="13" width="8" height="8" rx="1" />
  </Icon>
);

const ShieldIcon: AccountIcon = (props) => (
  <Icon {...props}>
    <path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6l8-3z" />
  </Icon>
);

const KeyIcon: AccountIcon = (props) => (
  <Icon {...props}>
    <circle cx="8" cy="12" r="3" />
    <path d="M11 12h10" />
    <path d="M18 9v6" />
    <path d="M21 10v4" />
  </Icon>
);

const MailIcon: AccountIcon = (props) => (
  <Icon {...props}>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="M3 7l9 6 9-6" />
  </Icon>
);

const MonitorIcon: AccountIcon = (props) => (
  <Icon {...props}>
    <rect x="3" y="4" width="18" height="12" rx="2" />
    <path d="M12 16v4" />
    <path d="M8 20h8" />
  </Icon>
);

const ActivityIcon: AccountIcon = (props) => (
  <Icon {...props}>
    <path d="M3 12h4l2-5 4 10 2-5h6" />
  </Icon>
);

const SettingsIcon: AccountIcon = (props) => (
  <Icon {...props}>
    <circle cx="12" cy="12" r="3" />
    <path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.2a1.7 1.7 0 0 0-1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1A2 2 0 1 1 4.4 17l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.2a1.7 1.7 0 0 0 1.5-1 1.7 1.7 0 0 0-.3-1.8l-.1-.1A2 2 0 1 1 7 4.4l.1.1a1.7 1.7 0 0 0 1.8.3h.1a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.2a1.7 1.7 0 0 0 1 1.5h.1a1.7 1.7 0 0 0 1.8-.3l.1-.1A2 2 0 1 1 19.6 7l-.1.1a1.7 1.7 0 0 0-.3 1.8v.1a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.2a1.7 1.7 0 0 0-1.5 1z" />
  </Icon>
);

const LogoutIcon: AccountIcon = (props) => (
  <Icon {...props}>
    <path d="M10 17l5-5-5-5" />
    <path d="M15 12H3" />
    <path d="M21 19V5a2 2 0 0 0-2-2h-6" />
  </Icon>
);

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

export const accountIcons: Record<AccountIconKey, AccountIcon> = {
  profile: DashboardIcon,
  security: ShieldIcon,
  mfa: ShieldIcon,
  email: MailIcon,
  password: KeyIcon,
  sessions: MonitorIcon,
  activity: ActivityIcon,
  admin: SettingsIcon,
  logout: LogoutIcon
};

export const accountMenuItems = [
  { key: "profile", label: "Dashboard", to: "/account", icon: accountIcons.profile },
  { key: "security", label: "Security", to: "/account/security", icon: accountIcons.security },
  { key: "mfa", label: "MFA", to: "/account/security/mfa", icon: accountIcons.mfa },
  { key: "email", label: "Change email", to: "/account/security/email", icon: accountIcons.email },
  { key: "password", label: "Change password", to: "/account/security/password", icon: accountIcons.password },
  { key: "sessions", label: "Sign out everywhere", to: "/account/security/sessions", icon: accountIcons.sessions },
  { key: "activity", label: "Recent logins", to: "/account/security/activity", icon: accountIcons.activity }
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
