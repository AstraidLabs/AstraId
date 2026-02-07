import type { SVGProps } from "react";

const base = "h-4 w-4";

const Icon = ({ children, ...props }: SVGProps<SVGSVGElement>) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={base} {...props}>
    {children}
  </svg>
);

export const HomeIcon = () => <Icon><path d="M3 11l9-8 9 8" /><path d="M5 10v10h14V10" /></Icon>;
export const UserIcon = () => <Icon><circle cx="12" cy="8" r="4" /><path d="M4 21c2-4 14-4 16 0" /></Icon>;
export const LockIcon = () => <Icon><rect x="4" y="11" width="16" height="10" rx="2" /><path d="M8 11V7a4 4 0 118 0v4" /></Icon>;
export const MailIcon = () => <Icon><rect x="3" y="5" width="18" height="14" rx="2" /><path d="M3 7l9 6 9-6" /></Icon>;
export const MonitorIcon = () => <Icon><rect x="3" y="4" width="18" height="12" rx="2" /><path d="M8 20h8" /><path d="M12 16v4" /></Icon>;
export const ShieldIcon = () => <Icon><path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6l8-3z" /></Icon>;
export const BackIcon = () => <Icon><path d="M19 12H5" /><path d="M12 19l-7-7 7-7" /></Icon>;
