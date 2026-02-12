import { type KeyboardEvent, useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import type { AuthSession } from "../api/authServer";
import { hasAdminAccess } from "../auth/adminAccess";
import { getAdminEntryUrl, isAbsoluteUrl } from "../utils/adminEntry";
import { accountIcons, accountMenuIconClass, accountMenuItems } from "../ui/accountIcons";
import { useLanguage } from "../i18n/LanguageProvider";

type Props = { session: AuthSession; onLogout: () => Promise<void> };

type MenuItem = { key: string; labelKey: string; to?: string; external?: boolean; danger?: boolean; action?: () => Promise<void>; Icon: any };

const initialsFromSession = (session: AuthSession) => (session.userName ?? session.email ?? "?").charAt(0).toUpperCase();

const ChevronDownIcon = ({ className }: { className?: string }) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
    <path d="m6 9 6 6 6-6" />
  </svg>
);

export default function AccountDropdown({ session, onLogout }: Props) {
  const [open, setOpen] = useState(false);
  const { t } = useLanguage();
  const [working, setWorking] = useState(false);
  const rootRef = useRef<HTMLDivElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const location = useLocation();
  const adminUrl = getAdminEntryUrl();

  const menuItems = useMemo<MenuItem[]>(() => {
    const items: MenuItem[] = accountMenuItems.map((item) => ({ ...item, Icon: item.icon }));
    if (hasAdminAccess(session.permissions)) items.push({ key: "admin", labelKey: "common.admin", to: adminUrl, external: isAbsoluteUrl(adminUrl), Icon: accountIcons.admin });
    items.push({ key: "logout", labelKey: "common.logout", action: onLogout, danger: true, Icon: accountIcons.logout });
    return items;
  }, [adminUrl, onLogout, session.permissions]);

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (event: MouseEvent) => !rootRef.current?.contains(event.target as Node) && setOpen(false);
    const onEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
        triggerRef.current?.focus();
      }
    };
    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onEscape);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onEscape);
    };
  }, [open]);

  const onMenuKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Tab") setOpen(false);
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    event.preventDefault();
    const nodes = menuRef.current?.querySelectorAll<HTMLElement>("[data-menu-item='true']");
    if (!nodes?.length) return;
    const current = Array.from(nodes).indexOf(document.activeElement as HTMLElement);
    const next = event.key === "ArrowDown" ? (current + 1 + nodes.length) % nodes.length : (current - 1 + nodes.length) % nodes.length;
    nodes[next].focus();
  };

  const onAction = async (action: () => Promise<void>) => {
    setWorking(true);
    try { await action(); setOpen(false); } finally { setWorking(false); }
  };

  return (
    <div className="relative" ref={rootRef}>
      <button ref={triggerRef} type="button" className="flex max-w-[260px] items-center gap-2 rounded-full border border-slate-700 px-3 py-2 text-sm text-slate-100 transition hover:border-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500" aria-label={t("account.dropdown.aria")} aria-haspopup="menu" aria-expanded={open} onClick={() => setOpen((c) => !c)}>
        <span className="inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-slate-800 text-xs font-semibold text-slate-200">{initialsFromSession(session)}</span>
        <span className="truncate">{session.userName ?? session.email ?? t("common.account")}</span>
        <ChevronDownIcon className="h-4 w-4 text-slate-400" />
      </button>
      {open && (
        <div ref={menuRef} role="menu" onKeyDown={onMenuKeyDown} className="absolute right-0 z-30 mt-2 w-64 rounded-xl border border-slate-700 bg-slate-900 p-2 shadow-2xl">
          {menuItems.map((item) => {
            const Icon = item.Icon;
            if (item.action) return <button key={item.key} role="menuitem" data-menu-item="true" disabled={working} onClick={() => onAction(item.action!)} className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm text-rose-200 transition hover:bg-rose-900/30 focus:outline-none focus:ring-2 focus:ring-indigo-500"><Icon className={accountMenuIconClass} />{working ? t("account.sessions.submitting") : t(item.labelKey as any)}</button>;
            if (item.external && item.to) return <a key={item.key} role="menuitem" data-menu-item="true" href={item.to} onClick={() => setOpen(false)} className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-100 transition hover:bg-slate-800 focus:outline-none focus:ring-2 focus:ring-indigo-500"><Icon className={accountMenuIconClass} />{t(item.labelKey as any)}</a>;
            const active = item.to === "/account" ? location.pathname === "/account" : !!item.to && location.pathname.startsWith(item.to);
            return <Link key={item.key} to={item.to ?? "#"} role="menuitem" data-menu-item="true" onClick={() => setOpen(false)} className={`flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-100 transition hover:bg-slate-800 focus:outline-none focus:ring-2 focus:ring-indigo-500 ${active ? "bg-slate-800/80" : ""}`}><Icon className={accountMenuIconClass} />{t(item.labelKey as any)}</Link>;
          })}
        </div>
      )}
    </div>
  );
}
