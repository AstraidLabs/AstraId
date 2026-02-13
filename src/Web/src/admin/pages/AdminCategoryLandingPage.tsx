import { Link, Navigate } from "react-router-dom";
import {
  canAccessAdminItem,
  getCategoryById,
  type AdminCategoryId,
  ADMIN_NAV_ITEMS,
} from "../adminNavigation";
import { toAdminRoute } from "../../routing";
import { useAuthSession } from "../../auth/useAuthSession";

type Props = {
  categoryId: AdminCategoryId;
};

export default function AdminCategoryLandingPage({ categoryId }: Props) {
  const { session } = useAuthSession();
  const category = getCategoryById(categoryId);

  if (!category) {
    return <Navigate to={toAdminRoute("/")} replace />;
  }

  const items = ADMIN_NAV_ITEMS.filter(
    (item) => item.category === categoryId && canAccessAdminItem(item, session?.permissions)
  );

  return (
    <section className="space-y-6" aria-labelledby={`${categoryId}-heading`}>
      <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
        <h1 id={`${categoryId}-heading`} className="text-2xl font-semibold text-white">
          {category.label}
        </h1>
        <p className="mt-2 text-sm text-slate-300">{category.description}</p>
      </div>

      {items.length === 0 ? (
        <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-6 text-sm text-slate-300">
          No sections are available for your current permission set.
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {items.map((item) => {
            const Icon = item.icon;

            return (
              <Link
                key={item.id}
                to={toAdminRoute(item.path)}
                className="group rounded-xl border border-slate-800 bg-slate-900/30 p-5 transition hover:border-indigo-500/60 hover:bg-slate-900/60 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-400"
              >
                <div className="flex items-start gap-3">
                  <div className="rounded-lg bg-slate-800/80 p-2 text-indigo-300">
                    <Icon className="h-5 w-5" aria-hidden="true" />
                  </div>
                  <div>
                    <h2 className="text-base font-semibold text-white group-hover:text-indigo-200">
                      {item.label}
                    </h2>
                    <p className="mt-1 text-sm text-slate-400">{item.description}</p>
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </section>
  );
}
