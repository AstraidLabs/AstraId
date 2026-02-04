import { useEffect, useState } from "react";
import { AppError, apiRequest } from "../api/http";
import { Link } from "react-router-dom";
import type { AdminUserListItem, PagedResult } from "../api/types";
import { toAdminRoute } from "../../routing";
import { pushToast } from "../components/toast";
import { Field, FormError, HelpIcon } from "../components/Field";
import { validateEmail } from "../validation/adminValidation";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

export default function UsersList() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PagedResult<AdminUserListItem> | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [createFormError, setCreateFormError] = useState<string | null>(null);
  const [createDiagnostics, setCreateDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
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
    const emailValidation = validateEmail(newUser.email);
    if (emailValidation.error) {
      setCreateError(emailValidation.error);
      return;
    }
    setCreateError(null);
    setPasswordError(null);
    setCreateFormError(null);
    setCreateDiagnostics(undefined);
    setCreating(true);
    try {
      await apiRequest("/admin/api/users", {
        method: "POST",
        body: JSON.stringify({
          email: emailValidation.value,
          userName: newUser.userName.trim() || null,
          phoneNumber: newUser.phoneNumber.trim() || null,
          password: newUser.password.trim() || null,
        }),
        suppressToast: true,
      });
      pushToast({ message: "User created.", tone: "success" });
      setNewUser({ email: "", userName: "", phoneNumber: "", password: "" });
      setPage(1);
      setRefreshKey((current) => current + 1);
    } catch (error) {
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setCreateError(parsed.fieldErrors.email?.[0] ?? null);
        setPasswordError(parsed.fieldErrors.password?.[0] ?? null);
        setCreateFormError(parsed.generalError ?? "Unable to create user.");
        setCreateDiagnostics(parsed.diagnostics);
        return;
      }
      setCreateFormError("Unable to create user.");
      setCreateDiagnostics(undefined);
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

      <div className="flex flex-wrap items-end gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <div className="w-full max-w-sm">
          <Field
            label="Search"
            tooltip="Hledá v emailu nebo username."
            hint="Použij přesný fragment (např. jana@)."
          >
            <input
              className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
              placeholder="Search email or username"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
            />
          </Field>
        </div>
        <div>
          <label className="text-xs uppercase tracking-wide text-slate-400">Page size</label>
          <select
            className="mt-1 rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
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
      </div>

      <form
        onSubmit={handleCreateUser}
        className="grid gap-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4 md:grid-cols-2"
      >
        <Field
          label="Email"
          tooltip="Primární přihlašovací email. Aktivace se posílá na tuto adresu."
          hint="Povinné."
          error={createError}
          required
        >
          <input
            className={`w-full rounded-md border bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 ${
              createError ? "border-rose-400" : "border-slate-700"
            }`}
            placeholder="name@example.com"
            value={newUser.email}
            onChange={(event) =>
              setNewUser((current) => ({ ...current, email: event.target.value }))
            }
            onBlur={() => setCreateError(validateEmail(newUser.email).error ?? null)}
          />
        </Field>
        <Field
          label="Username"
          tooltip="Volitelný username pro přihlášení."
          hint="Když je prázdné, použije se email."
        >
          <input
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
            placeholder="Username"
            value={newUser.userName}
            onChange={(event) =>
              setNewUser((current) => ({ ...current, userName: event.target.value }))
            }
          />
        </Field>
        <Field
          label="Phone number"
          tooltip="Volitelné telefonní číslo uživatele."
          hint="Použij E.164 formát."
        >
          <input
            className="w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500"
            placeholder="+420123456789"
            value={newUser.phoneNumber}
            onChange={(event) =>
              setNewUser((current) => ({ ...current, phoneNumber: event.target.value }))
            }
          />
        </Field>
        <Field
          label="Temporary password"
          tooltip="Pokud necháš prázdné, systém vygeneruje aktivaci."
          hint="Volitelné. Musí splnit password policy."
          error={passwordError}
        >
          <input
            className={`w-full rounded-md border bg-slate-950 px-3 py-2 text-sm text-slate-100 placeholder:text-slate-500 ${
              passwordError ? "border-rose-400" : "border-slate-700"
            }`}
            type="password"
            placeholder="Temporary password"
            value={newUser.password}
            onChange={(event) =>
              setNewUser((current) => ({ ...current, password: event.target.value }))
            }
          />
        </Field>
        <div className="flex items-center gap-3 md:col-span-2">
          <button
            type="submit"
            disabled={creating}
            className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
          >
            {creating ? "Creating..." : "Create user"}
          </button>
          <span className="flex items-center gap-2 text-xs text-slate-400">
            <HelpIcon tooltip="Uživatel dostane aktivační email s potvrzovacím linkem." />
            New users will receive an activation email.
          </span>
        </div>
        <div className="md:col-span-2">
        <FormError message={createFormError} diagnostics={createDiagnostics} />
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
