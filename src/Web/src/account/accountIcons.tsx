import type { SVGProps } from "react";

const defaultIconClassName = "h-[17px] w-[17px] shrink-0";

type AccountIconProps = SVGProps<SVGSVGElement> & {
  className?: string;
};

export type AccountActionItem = {
  key: string;
  label: string;
  to: string;
  Icon: (props: AccountIconProps) => JSX.Element;
};

const Icon = ({ children, className = defaultIconClassName, ...props }: AccountIconProps) => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className={className}
    aria-hidden="true"
    {...props}
  >
    {children}
  </svg>
);

export const DashboardIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <rect x="3" y="3" width="8" height="8" rx="1" />
    <rect x="13" y="3" width="8" height="5" rx="1" />
    <rect x="13" y="10" width="8" height="11" rx="1" />
    <rect x="3" y="13" width="8" height="8" rx="1" />
  </Icon>
);

export const ProfileDetailsIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <circle cx="12" cy="8" r="4" />
    <path d="M4 21c2.5-4.5 13.5-4.5 16 0" />
    <circle cx="12" cy="12" r="9" />
  </Icon>
);

export const PasswordIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <rect x="3" y="11" width="18" height="10" rx="2" />
    <path d="M7 11V8a5 5 0 0110 0v3" />
    <circle cx="12" cy="16" r="1" />
  </Icon>
);

export const EmailIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="M3 7l9 6 9-6" />
  </Icon>
);

export const SessionsIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <rect x="3" y="4" width="18" height="12" rx="2" />
    <path d="M8 20h8" />
    <path d="M12 16v4" />
  </Icon>
);

export const MfaIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6l8-3z" />
    <path d="M9 12l2 2 4-4" />
  </Icon>
);

export const SecurityEventsIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <path d="M3 13h4l2 5 4-10 2 5h6" />
  </Icon>
);

export const AdminIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6l8-3z" />
  </Icon>
);

export const LogoutIcon = ({ className }: AccountIconProps) => (
  <Icon className={className}>
    <path d="M10 17l5-5-5-5" />
    <path d="M15 12H3" />
    <path d="M20 19a2 2 0 002-2V7a2 2 0 00-2-2h-5" />
  </Icon>
);

export const ACCOUNT_SELF_SERVICE_ITEMS: AccountActionItem[] = [
  { key: "dashboard", label: "Profile", to: "/account", Icon: DashboardIcon },
  { key: "profile-details", label: "Profile details", to: "/account/profile", Icon: ProfileDetailsIcon },
  { key: "password", label: "Password", to: "/account/password", Icon: PasswordIcon },
  { key: "email", label: "Email", to: "/account/email", Icon: EmailIcon },
  { key: "sessions", label: "Sessions", to: "/account/sessions", Icon: SessionsIcon },
  { key: "mfa", label: "MFA", to: "/account/mfa", Icon: MfaIcon },
  { key: "security-events", label: "Security events", to: "/account/security-events", Icon: SecurityEventsIcon }
];
