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
  | "privacy"
  | "export"
  | "delete"
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
  privacy: ShieldIcon,
  export: ActivityIcon,
  delete: KeyIcon,
  logout: LogoutIcon
};

export const accountMenuItems = [
  { key: "profile", labelKey: "account.nav.overview", to: "/account", icon: accountIcons.profile },
  { key: "security", labelKey: "account.nav.security", to: "/account/security", icon: accountIcons.security },
  { key: "mfa", labelKey: "security.mfa", to: "/account/security/mfa", icon: accountIcons.mfa },
  { key: "email", labelKey: "security.email", to: "/account/security/email", icon: accountIcons.email },
  { key: "password", labelKey: "security.password", to: "/account/security/password", icon: accountIcons.password },
  { key: "sessions", labelKey: "security.sessions", to: "/account/security/sessions", icon: accountIcons.sessions },
  { key: "activity", labelKey: "security.activity", to: "/account/security/activity", icon: accountIcons.activity },
  { key: "privacy", labelKey: "account.nav.privacy", to: "/account/privacy", icon: accountIcons.privacy },
  { key: "export", labelKey: "privacy.export", to: "/account/privacy", icon: accountIcons.export },
  { key: "delete", labelKey: "privacy.delete", to: "/account/privacy", icon: accountIcons.delete }
] as const;

export const securityItems = [
  { key: "mfa", labelKey: "security.mfa", to: "/account/security/mfa", descriptionKey: "security.mfa.description", icon: accountIcons.mfa },
  { key: "email", labelKey: "security.email", to: "/account/security/email", descriptionKey: "security.email.description", icon: accountIcons.email },
  { key: "password", labelKey: "security.password", to: "/account/security/password", descriptionKey: "security.password.description", icon: accountIcons.password },
  { key: "sessions", labelKey: "security.sessions", to: "/account/security/sessions", descriptionKey: "security.sessions.description", icon: accountIcons.sessions },
  { key: "activity", labelKey: "security.activity", to: "/account/security/activity", descriptionKey: "security.activity.description", icon: accountIcons.activity }
] as const;

export const accountCardIconClass = "h-5 w-5";
export const accountMenuIconClass = "h-4 w-4";
