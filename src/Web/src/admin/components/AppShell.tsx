import { useMemo, useState, type ReactNode } from "react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { ChevronRight, Menu, Shield } from "lucide-react";
import { useAuthSession } from "../../auth/useAuthSession";
import { toAdminRoute } from "../../routing";
import {
  ADMIN_CATEGORIES,
  buildBreadcrumbs,
  getRouteMeta,
  getVisibleAdminItems,
} from "../adminNavigation";
import useDocumentMeta from "../../hooks/useDocumentMeta";

type Props = {
  children: ReactNode;
};

const navItemClass = ({ isActive }: { isActive: boolean }) =>
  `group flex items-start gap-3 rounded-md px-3 py-2 text-sm font-medium transition ${
    isActive
      ? "bg-slate-800 text-white"
      : "text-slate-300 hover:bg-slate-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-400"
  }`;

export default function AppShell({ children }: Props) {
  const { session } = useAuthSession();
  const location = useLocation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const visibleItems = useMemo(
    () => getVisibleAdminItems(session?.permissions),
    [session?.permissions]
  );

  const visibleCategories = useMemo(
    () =>
      ADMIN_CATEGORIES.filter((category) =>
        visibleItems.some((item) => item.category === category.id)
      ),
    [visibleItems]
  );

  const routeMeta = useMemo(() => getRouteMeta(location.pathname), [location.pathname]);
  const breadcrumbs = useMemo(() => buildBreadcrumbs(location.pathname), [location.pathname]);

  useDocumentMeta({
    title: `${routeMeta.title} · AstraId Admin`,
    description: routeMeta.description,
  });

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <div className="flex min-h-screen">
        <aside
          className={`fixed inset-y-0 left-0 z-40 w-72 border-r border-slate-900 bg-slate-950/95 p-4 transition-transform md:static md:translate-x-0 ${
            mobileMenuOpen ? "translate-x-0" : "-translate-x-full"
          }`}
          aria-label="Admin navigation"
        >
          <div className="mb-4 px-2">
            <Link to={toAdminRoute("/")} className="text-lg font-semibold text-white">
              AstraId Admin
            </Link>
            <p className="mt-1 text-xs text-slate-500">Authorization server control panel</p>
          </div>

          <nav className="flex h-[calc(100vh-7rem)] flex-col gap-4 overflow-y-auto pb-6 pr-1">
            {visibleCategories.map((category) => {
              const CategoryIcon = category.icon;
              const items = visibleItems.filter((item) => item.category === category.id);

              return (
                <details key={category.id} open className="group rounded-lg bg-slate-900/20 p-2">
                  <summary className="flex cursor-pointer list-none items-center justify-between gap-2 rounded-md px-2 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-900/70">
                    <span className="flex items-center gap-2">
                      <CategoryIcon className="h-4 w-4 text-indigo-300" aria-hidden="true" />
                      {category.label}
                    </span>
                    <ChevronRight className="h-4 w-4 transition group-open:rotate-90" aria-hidden="true" />
                  </summary>

                  <div className="mt-2 flex flex-col gap-1">
                    <NavLink to={toAdminRoute(category.path)} className={navItemClass}>
                      <span>{category.label} overview</span>
                    </NavLink>
                    {items.map((item) => {
                      const Icon = item.icon;
                      return (
                        <NavLink
                          key={item.id}
                          to={toAdminRoute(item.path)}
                          className={navItemClass}
                          onClick={() => setMobileMenuOpen(false)}
                        >
                          <Icon className="mt-0.5 h-4 w-4 shrink-0 text-slate-400 group-hover:text-slate-200" aria-hidden="true" />
                          <span>{item.label}</span>
                        </NavLink>
                      );
                    })}
                  </div>
                </details>
              );
            })}
          </nav>
        </aside>

        <div className="flex min-h-screen flex-1 flex-col md:pl-0">
          <header className="sticky top-0 z-30 border-b border-slate-900 bg-slate-950/90 px-4 py-4 backdrop-blur md:px-8">
            <div className="flex items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => setMobileMenuOpen((prev) => !prev)}
                  className="inline-flex rounded-md border border-slate-800 p-2 text-slate-200 hover:bg-slate-900 md:hidden"
                  aria-label="Toggle navigation menu"
                  aria-expanded={mobileMenuOpen}
                >
                  <Menu className="h-5 w-5" aria-hidden="true" />
                </button>
                <div>
                  <div className="text-sm text-slate-400">Admin · AuthServer</div>
                  <h1 className="text-base font-semibold text-white md:text-lg">{routeMeta.title}</h1>
                </div>
              </div>

              <div className="flex items-center gap-2 rounded-md border border-slate-800 px-3 py-2 text-xs text-slate-300">
                <Shield className="h-4 w-4 text-emerald-300" aria-hidden="true" />
                <span>{session?.email ?? session?.userName ?? "Administrator"}</span>
              </div>
            </div>

            <nav aria-label="Breadcrumb" className="mt-3">
              <ol className="flex flex-wrap items-center gap-2 text-xs text-slate-400">
                {breadcrumbs.map((crumb, index) => {
                  const isLast = index === breadcrumbs.length - 1;

                  return (
                    <li key={crumb.path} className="flex items-center gap-2">
                      {index > 0 && <ChevronRight className="h-3.5 w-3.5" aria-hidden="true" />}
                      {isLast ? (
                        <span className="font-medium text-slate-200">{crumb.label}</span>
                      ) : (
                        <Link className="hover:text-slate-200" to={toAdminRoute(crumb.path)}>
                          {crumb.label}
                        </Link>
                      )}
                    </li>
                  );
                })}
              </ol>
            </nav>
          </header>

          <main className="flex w-full flex-1 flex-col gap-6 px-4 py-6 md:px-8">{children}</main>
          <footer className="border-t border-slate-900 px-8 py-3 text-xs text-slate-500">
            AstraId Admin UI
          </footer>
        </div>
      </div>

      {mobileMenuOpen && (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-black/40 md:hidden"
          aria-label="Close navigation menu"
          onClick={() => setMobileMenuOpen(false)}
        />
      )}
    </div>
  );
}
