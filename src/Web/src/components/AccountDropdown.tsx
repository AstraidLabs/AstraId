import { type KeyboardEvent, useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import type { AuthSession } from "../api/authServer";
import { ACCOUNT_SELF_SERVICE_ITEMS, AdminIcon, LogoutIcon } from "../account/accountIcons";
import { hasAdminAccess } from "../auth/adminAccess";
import { getAdminEntryUrl, isAbsoluteUrl } from "../utils/adminEntry";

type Props = {
  session: AuthSession;
  onLogout: () => Promise<void>;
};

type MenuItem = {
  key: string;
  label: string;
  icon: JSX.Element;
  to?: string;
  external?: boolean;
  danger?: boolean;
  action?: () => Promise<void>;
};


const initialsFromSession = (session: AuthSession) => {
  const display = session.userName ?? session.email ?? "User";
  return display.charAt(0).toUpperCase();
};

export default function AccountDropdown({ session, onLogout }: Props) {
  const [open, setOpen] = useState(false);
  const [working, setWorking] = useState(false);
  const rootRef = useRef<HTMLDivElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const location = useLocation();
  const adminUrl = getAdminEntryUrl();
  const adminExternal = isAbsoluteUrl(adminUrl);

  const menuItems = useMemo<MenuItem[]>(() => {
    const items: MenuItem[] = ACCOUNT_SELF_SERVICE_ITEMS.map((item) => ({
      key: item.key,
      label: item.label,
      to: item.to,
      icon: <item.Icon />
    }));

    if (hasAdminAccess(session.permissions)) {
      items.push({
        key: "admin",
        label: "Admin",
        icon: <AdminIcon />,
        to: adminUrl,
        external: adminExternal
      });
    }

    items.push({ key: "logout", label: "Logout", icon: <LogoutIcon />, action: onLogout, danger: true });

    return items;
  }, [adminExternal, adminUrl, onLogout, session]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const onPointerDown = (event: MouseEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    const onEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
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
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") {
      return;
    }

    event.preventDefault();
    const menuItemsNodes = menuRef.current?.querySelectorAll<HTMLElement>("[data-menu-item='true']");
    if (!menuItemsNodes?.length) {
      return;
    }

    const currentIndex = Array.from(menuItemsNodes).indexOf(document.activeElement as HTMLElement);
    if (event.key === "ArrowDown") {
      const nextIndex = currentIndex === -1 ? 0 : (currentIndex + 1) % menuItemsNodes.length;
      menuItemsNodes[nextIndex].focus();
      return;
    }

    const previousIndex = currentIndex <= 0 ? menuItemsNodes.length - 1 : currentIndex - 1;
    menuItemsNodes[previousIndex].focus();
  };

  const onAction = async (action: () => Promise<void>) => {
    setWorking(true);
    try {
      await action();
      setOpen(false);
    } finally {
      setWorking(false);
    }
  };

  return (
    <div className="relative" ref={rootRef}>
      <button
        type="button"
        className="flex max-w-[260px] items-center gap-2 rounded-full border border-slate-700 px-3 py-2 text-sm text-slate-100 transition hover:border-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((current) => !current)}
      >
        <span className="inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-slate-800 text-xs font-semibold text-slate-200">
          {initialsFromSession(session)}
        </span>
        <span className="truncate">{session.userName ?? session.email ?? "Account"}</span>
        <span aria-hidden="true" className="text-slate-400">▾</span>
      </button>

      {open ? (
        <div
          ref={menuRef}
          role="menu"
          aria-label="Account actions"
          onKeyDown={onMenuKeyDown}
          className="absolute right-0 z-30 mt-2 max-h-80 w-64 overflow-auto rounded-xl border border-slate-700 bg-slate-900 p-2 shadow-2xl"
        >
          {menuItems.map((item) => {
            if (item.action) {
              return (
                <div key={item.key}>
                  <div className="my-1 h-px bg-slate-700" />
                  <button
                    type="button"
                    role="menuitem"
                    data-menu-item="true"
                    disabled={working}
                    onClick={() => onAction(item.action as () => Promise<void>)}
                    className={`flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm transition focus:outline-none focus:ring-2 focus:ring-indigo-500 ${
                      item.danger
                        ? "text-rose-200 hover:bg-rose-900/30 focus:bg-rose-900/30"
                        : "text-slate-100 hover:bg-slate-800 focus:bg-slate-800"
                    } disabled:opacity-60`}
                  >
                    {item.icon}
                    <span>{working ? "Logging out..." : item.label}</span>
                  </button>
                </div>
              );
            }

            if (item.external && item.to) {
              return (
                <a
                  key={item.key}
                  role="menuitem"
                  data-menu-item="true"
                  href={item.to}
                  className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-100 transition hover:bg-slate-800 focus:bg-slate-800 focus:outline-none"
                  onClick={() => setOpen(false)}
                >
                  {item.icon}
                  <span>{item.label}</span>
                </a>
              );
            }

            const isActive = item.to === "/account"
              ? location.pathname === "/account"
              : !!item.to && location.pathname.startsWith(item.to);

            return (
              <Link
                key={item.key}
                to={item.to ?? "#"}
                role="menuitem"
                data-menu-item="true"
                className={`flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-100 transition hover:bg-slate-800 focus:bg-slate-800 focus:outline-none ${isActive ? "bg-slate-800/80" : ""}`}
                onClick={() => setOpen(false)}
              >
                {item.icon}
                <span>{item.label}</span>
              </Link>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}
