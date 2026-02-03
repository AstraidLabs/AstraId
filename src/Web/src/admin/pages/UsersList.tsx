import { useEffect, useState } from "react";
import { apiRequest } from "../api/http";
import { Link } from "react-router-dom";
import type { AdminUserListItem, PagedResult } from "../api/types";
import { toAdminRoute } from "../../routing";
import { pushToast } from "../components/toast";

export default function UsersList() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PagedResult<AdminUserListItem> | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [creating, setCreating] = useState(false);
  const [newUser, setNewUser] = useState({
    email: "",
    userName: "",
    phoneNumber: "",
    password: "",
  });

  useEffect(() => {
    let isMounted = true;
    const fetchUsers = async () => {
      setLoading(true);
      const params = new URLSearchParams();
      if (search.trim()) {
        params.set("search", search.trim());
      }
      params.set("page", String(page));
      params.set("pageSize", String(pageSize));
      try {
        const data = await apiRequest<PagedResult<AdminUserListItem>>(
          `/admin/api/users?${params.toString()}`
        );
        if (isMounted) {
          setResult(data);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchUsers();
    return () => {
      isMounted = false;
    };
  }, [page, pageSize, search, refreshKey]);

  const handleCreateUser = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!newUser.email.trim()) {
      return;
    }
    setCreating(true);
    try {
      await apiRequest("/admin/api/users", {
        method: "POST",
        body: JSON.stringify({
          email: newUser.email.trim(),
          userName: newUser.userName.trim() || null,
          phoneNumber: newUser.phoneNumber.trim() || null,
          password: newUser.password.trim() || null,
        }),
      });
      pushToast({ message: "User created.", tone: "success" });
      setNewUser({ email: "", userName: "", phoneNumber: "", password: "" });
      setPage(1);
      setRefreshKey((current) => current + 1);
    } finally {
      setCreating(false);
    }
  };

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / result.pageSize)) : 1;

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold text-white">Users</h1>
        <p className="text-sm text-slate-300">Browse registered users and lock status.</p>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <input
          className="w-full max-w-sm rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          placeholder="Search email or username..."
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
        />
        <select
          className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
          value={pageSize}
          onChange={(event) => {
            setPageSize(Number(event.target.value));
            setPage(1);
          }}
        >
          {[10, 20, 30].map((size) => (
            <option key={size} value={size}>
              {size} / page
            </option>
          ))}
        </select>
      </div>

      <form
        onSubmit={handleCreateUser}
        className="grid gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4 md:grid-cols-2"
      >
        <input
          className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          placeholder="Email (required)"
          value={newUser.email}
          onChange={(event) => setNewUser((current) => ({ ...current, email: event.target.value }))}
        />
        <input
          className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          placeholder="Username"
          value={newUser.userName}
          onChange={(event) => setNewUser((current) => ({ ...current, userName: event.target.value }))}
        />
        <input
          className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          placeholder="Phone number"
          value={newUser.phoneNumber}
          onChange={(event) =>
            setNewUser((current) => ({ ...current, phoneNumber: event.target.value }))
          }
        />
        <input
          className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
          type="password"
          placeholder="Temporary password (optional)"
          value={newUser.password}
          onChange={(event) =>
            setNewUser((current) => ({ ...current, password: event.target.value }))
          }
        />
        <div className="flex items-center gap-3 md:col-span-2">
          <button
            type="submit"
            disabled={creating}
            className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
          >
            {creating ? "Creating..." : "Create user"}
          </button>
          <span className="text-xs text-slate-400">
            New users will receive an activation email.
          </span>
        </div>
      </form>

      <div className="overflow-hidden rounded-lg border border-slate-800">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-300">
            <tr>
              <th className="px-4 py-3 font-medium">Email</th>
              <th className="px-4 py-3 font-medium">Username</th>
              <th className="px-4 py-3 font-medium">Confirmed</th>
              <th className="px-4 py-3 font-medium">Locked</th>
              <th className="px-4 py-3 font-medium">Roles</th>
              <th className="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 bg-slate-950/40">
            {loading && (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-400">
                  Loading users...
                </td>
              </tr>
            )}
            {!loading && result?.items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-400">
                  No users found. Create the first user above.
                </td>
              </tr>
            )}
            {result?.items.map((user) => (
              <tr key={user.id} className="text-slate-100">
                <td className="px-4 py-3 font-medium">{user.email ?? "-"}</td>
                <td className="px-4 py-3 text-slate-300">{user.userName ?? "-"}</td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-3 py-1 text-xs font-semibold ${
                      user.emailConfirmed
                        ? "bg-emerald-500/20 text-emerald-200"
                        : "bg-amber-500/20 text-amber-200"
                    }`}
                  >
                    {user.emailConfirmed ? "Yes" : "No"}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-3 py-1 text-xs font-semibold ${
                      user.isLockedOut
                        ? "bg-rose-500/20 text-rose-200"
                        : "bg-emerald-500/20 text-emerald-200"
                    }`}
                  >
                    {user.isLockedOut ? "Locked" : "Active"}
                  </span>
                </td>
                <td className="px-4 py-3 text-slate-300">
                  {user.roles.length ? user.roles.join(", ") : "-"}
                </td>
                <td className="px-4 py-3 text-right">
                  <Link
                    to={toAdminRoute(`/users/${user.id}`)}
                    className="text-sm font-semibold text-indigo-300 hover:text-indigo-200"
                  >
                    Manage
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {result && (
        <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-slate-300">
          <span>
            Page {result.page} of {totalPages} · {result.totalCount} users
          </span>
          <div className="flex items-center gap-2">
            <button
              className="rounded-md border border-slate-700 px-3 py-1 text-slate-200 disabled:opacity-40"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
              disabled={result.page <= 1}
            >
              Previous
            </button>
            <button
              className="rounded-md border border-slate-700 px-3 py-1 text-slate-200 disabled:opacity-40"
              onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
              disabled={result.page >= totalPages}
            >
              Next
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
