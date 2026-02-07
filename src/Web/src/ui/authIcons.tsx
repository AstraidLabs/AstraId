import type { SVGProps } from "react";

type IconProps = SVGProps<SVGSVGElement>;

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

export const MailIcon = (props: IconProps) => (
  <Icon {...props}>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="M3 7l9 6 9-6" />
  </Icon>
);

export const LockIcon = (props: IconProps) => (
  <Icon {...props}>
    <rect x="4" y="11" width="16" height="10" rx="2" />
    <path d="M8 11V8a4 4 0 0 1 8 0v3" />
  </Icon>
);

export const EyeIcon = (props: IconProps) => (
  <Icon {...props}>
    <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6z" />
    <circle cx="12" cy="12" r="3" />
  </Icon>
);

export const EyeOffIcon = (props: IconProps) => (
  <Icon {...props}>
    <path d="M3 3l18 18" />
    <path d="M10.5 6.7A10.9 10.9 0 0 1 12 6c6.5 0 10 6 10 6a18 18 0 0 1-3.1 3.9" />
    <path d="M8.6 8.7A5 5 0 0 0 16 15.4" />
    <path d="M6.5 6.5C3.8 8.1 2 12 2 12s3.5 6 10 6c1.6 0 3-.3 4.2-.9" />
  </Icon>
);

export const LoginIcon = (props: IconProps) => (
  <Icon {...props}>
    <path d="M10 17l5-5-5-5" />
    <path d="M15 12H3" />
    <path d="M21 19V5a2 2 0 0 0-2-2h-6" />
  </Icon>
);

export const UserPlusIcon = (props: IconProps) => (
  <Icon {...props}>
    <circle cx="10" cy="8" r="4" />
    <path d="M2 20c0-3.3 3.6-6 8-6" />
    <path d="M19 8v6" />
    <path d="M16 11h6" />
  </Icon>
);

export const CheckIcon = (props: IconProps) => (
  <Icon {...props}>
    <path d="m4 12 5 5L20 6" />
  </Icon>
);
