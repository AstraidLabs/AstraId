import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { AppError, apiRequest } from "../api/http";
import type { AdminRoleListItem, AdminUserDetail } from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { Field, FormError, HelpIcon } from "../components/Field";
import DiagnosticsPanel from "../../components/DiagnosticsPanel";
import { pushToast } from "../components/toast";
import { useAuthSession } from "../../auth/useAuthSession";
import { validateEmail } from "../validation/adminValidation";
import { parseProblemDetailsErrors } from "../validation/problemDetails";

export default function UserDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { session } = useAuthSession();
  const [user, setUser] = useState<AdminUserDetail | null>(null);
  const [roles, setRoles] = useState<AdminRoleListItem[]>([]);
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set());
  const [newPassword, setNewPassword] = useState("");
  const [profileErrors, setProfileErrors] = useState<{ email?: string }>({});
  const [profileError, setProfileError] = useState<string | null>(null);
  const [rolesError, setRolesError] = useState<string | null>(null);
  const [lockError, setLockError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [activationError, setActivationError] = useState<string | null>(null);
  const [profileDiagnostics, setProfileDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [rolesDiagnostics, setRolesDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [lockDiagnostics, setLockDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [passwordDiagnostics, setPasswordDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [activationDiagnostics, setActivationDiagnostics] = useState<
    ReturnType<typeof parseProblemDetailsErrors>["diagnostics"]
  >(undefined);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [savingProfile, setSavingProfile] = useState(false);
  const [resending, setResending] = useState(false);
  const [confirmDeactivate, setConfirmDeactivate] = useState(false);
  const [formState, setFormState] = useState({
    email: "",
    userName: "",
    phoneNumber: "",
    emailConfirmed: false,
    isActive: true,
  });

  useEffect(() => {
    if (!id) {
      return;
    }
    let isMounted = true;
    const fetchData = async () => {
      setLoading(true);
      try {
        const [userData, rolesData] = await Promise.all([
          apiRequest<AdminUserDetail>(`/admin/api/users/${id}`),
          apiRequest<AdminRoleListItem[]>("/admin/api/roles"),
        ]);
        if (isMounted) {
          setUser(userData);
          setRoles(rolesData);
          setSelectedRoles(new Set(userData.roles));
          setFormState({
            email: userData.email ?? "",
            userName: userData.userName ?? "",
            phoneNumber: userData.phoneNumber ?? "",
            emailConfirmed: userData.emailConfirmed,
            isActive: userData.isActive,
          });
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };
    fetchData();
    return () => {
      isMounted = false;
    };
  }, [id]);

  const handleRoleToggle = (roleName: string) => {
    setSelectedRoles((current) => {
      const next = new Set(current);
      if (next.has(roleName)) {
        next.delete(roleName);
      } else {
        next.add(roleName);
      }
      return next;
    });
  };

  const handleSaveRoles = async () => {
    if (!id) {
      return;
    }
    setRolesError(null);
    setRolesDiagnostics(undefined);
    setSaving(true);
    try {
      await apiRequest(`/admin/api/users/${id}/roles`, {
        method: "PUT",
        body: JSON.stringify({ roles: Array.from(selectedRoles) }),
        suppressToast: true,
      });
      pushToast({ message: "User roles updated.", tone: "success" });
    } catch (error) {
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setRolesError(parsed.generalError ?? "Unable to update roles.");
        setRolesDiagnostics(parsed.diagnostics);
        return;
      }
      setRolesError("Unable to update roles.");
      setRolesDiagnostics(undefined);
    } finally {
      setSaving(false);
    }
  };

  const handleSaveProfile = async () => {
    if (!id || !user) {
      return;
    }
    const emailValidation = validateEmail(formState.email);
    if (emailValidation.error) {
      setProfileErrors({ email: emailValidation.error });
      return;
    }
    setProfileErrors({});
    setProfileError(null);
    setProfileDiagnostics(undefined);
    setSavingProfile(true);
    try {
      await apiRequest(`/admin/api/users/${id}`, {
        method: "PUT",
        body: JSON.stringify({
          email: emailValidation.value,
          userName: formState.userName.trim() || null,
          phoneNumber: formState.phoneNumber.trim() || null,
          emailConfirmed: formState.emailConfirmed,
          isActive: formState.isActive,
        }),
        suppressToast: true,
      });
      setUser({
        ...user,
        email: emailValidation.value,
        userName: formState.userName.trim() || null,
        phoneNumber: formState.phoneNumber.trim() || null,
        emailConfirmed: formState.emailConfirmed,
        isActive: formState.isActive,
      });
      pushToast({ message: "User updated.", tone: "success" });
    } catch (error) {
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setProfileErrors({ email: parsed.fieldErrors.email?.[0] });
        setProfileError(parsed.generalError ?? "Unable to update user.");
        setProfileDiagnostics(parsed.diagnostics);
        return;
      }
      setProfileError("Unable to update user.");
      setProfileDiagnostics(undefined);
    } finally {
      setSavingProfile(false);
    }
  };

  const handleLockToggle = async () => {
    if (!id || !user) {
      return;
    }
    setLockError(null);
    setLockDiagnostics(undefined);
    const nextLocked = !user.isLockedOut;
    try {
      await apiRequest(`/admin/api/users/${id}/lock`, {
        method: "POST",
        body: JSON.stringify({ locked: nextLocked }),
        suppressToast: true,
      });
      setUser({ ...user, isLockedOut: nextLocked });
      pushToast({
        message: nextLocked ? "User locked." : "User unlocked.",
        tone: "success",
      });
    } catch (error) {
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setLockError(parsed.generalError ?? "Unable to update lock status.");
        setLockDiagnostics(parsed.diagnostics);
        return;
      }
      setLockError("Unable to update lock status.");
      setLockDiagnostics(undefined);
    }
  };

  const handleResetPassword = async () => {
    if (!id) {
      return;
    }
    if (!newPassword.trim()) {
      setPasswordError("New password is required.");
      return;
    }
    setPasswordError(null);
    setPasswordDiagnostics(undefined);
    try {
      await apiRequest(`/admin/api/users/${id}/reset-password`, {
        method: "POST",
        body: JSON.stringify({ newPassword }),
        suppressToast: true,
      });
      setNewPassword("");
      pushToast({ message: "Password reset.", tone: "success" });
    } catch (error) {
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setPasswordError(parsed.fieldErrors.newPassword?.[0] ?? parsed.generalError);
        setPasswordDiagnostics(parsed.diagnostics);
        return;
      }
      setPasswordError("Unable to reset password.");
      setPasswordDiagnostics(undefined);
    }
  };

  const handleResendActivation = async () => {
    if (!id || !user) {
      return;
    }
    setResending(true);
    setActivationError(null);
    setActivationDiagnostics(undefined);
    try {
      await apiRequest(`/admin/api/users/${id}/resend-activation`, {
        method: "POST",
        suppressToast: true,
      });
      pushToast({ message: "Activation email resent.", tone: "success" });
    } catch (error) {
      if (error instanceof AppError) {
        const parsed = parseProblemDetailsErrors(error);
        setActivationError(parsed.generalError ?? "Unable to resend activation email.");
        setActivationDiagnostics(parsed.diagnostics);
        return;
      }
      setActivationError("Unable to resend activation email.");
      setActivationDiagnostics(undefined);
    } finally {
      setResending(false);
    }
  };

  const handleDeactivate = async () => {
    if (!id) {
      return;
    }
    setConfirmDeactivate(false);
    await apiRequest(`/admin/api/users/${id}`, { method: "DELETE" });
    pushToast({ message: "User deactivated.", tone: "success" });
    navigate(-1);
  };

  if (!id) {
    return null;
  }

  if (loading) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        Loading user...
      </div>
    );
  }

  if (!user) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6 text-slate-300">
        User not found.
      </div>
    );
  }

  return (
    <section className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">{user.email ?? user.userName}</h1>
          <p className="text-sm text-slate-300">Manage user access and security settings.</p>
        </div>
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="rounded-md border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-900"
        >
          Back
        </button>
      </div>

      <div className="grid gap-4 rounded-lg border border-slate-800 bg-slate-900/40 p-6 md:grid-cols-2">
        <Field
          label="Email"
          tooltip="Primární přihlašovací email. Změna může vyžadovat novou aktivaci."
          hint="Povinné."
          error={profileErrors.email}
          required
        >
          <input
            className={`rounded-md border bg-slate-950 px-3 py-2 text-sm text-slate-100 ${
              profileErrors.email ? "border-rose-400" : "border-slate-700"
            }`}
            value={formState.email}
            onChange={(event) =>
              setFormState((current) => ({ ...current, email: event.target.value }))
            }
            onBlur={() =>
              setProfileErrors({ email: validateEmail(formState.email).error ?? undefined })
            }
            placeholder="user@example.com"
          />
        </Field>
        <Field
          label="Username"
          tooltip="Volitelný username pro přihlášení."
          hint="Když je prázdné, použije se email."
        >
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            value={formState.userName}
            onChange={(event) =>
              setFormState((current) => ({ ...current, userName: event.target.value }))
            }
            placeholder="username"
          />
        </Field>
        <Field
          label="Phone"
          tooltip="Volitelné telefonní číslo."
          hint="Použij E.164 formát."
        >
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            value={formState.phoneNumber}
            onChange={(event) =>
              setFormState((current) => ({ ...current, phoneNumber: event.target.value }))
            }
            placeholder="+420123456789"
          />
        </Field>
        <div className="flex flex-col gap-2 text-sm text-slate-200">
          <span className="text-xs uppercase tracking-wide text-slate-500">2FA enabled</span>
          <div className="rounded-md border border-slate-800 bg-slate-950/40 px-3 py-2 text-sm text-slate-200">
            {user.twoFactorEnabled ? "Yes" : "No"}
          </div>
        </div>
        <div className="flex flex-col gap-2 text-sm text-slate-200">
          <span className="text-xs uppercase tracking-wide text-slate-500">Recovery codes left</span>
          <div className="rounded-md border border-slate-800 bg-slate-950/40 px-3 py-2 text-sm text-slate-200">
            {user.recoveryCodesLeft}
          </div>
        </div>
        <label className="flex items-center gap-3 text-sm text-slate-200">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
            checked={formState.emailConfirmed}
            onChange={(event) =>
              setFormState((current) => ({
                ...current,
                emailConfirmed: event.target.checked,
              }))
            }
          />
          Email confirmed
        </label>
        <label className="flex items-center gap-3 text-sm text-slate-200">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
            checked={formState.isActive}
            onChange={(event) =>
              setFormState((current) => ({
                ...current,
                isActive: event.target.checked,
              }))
            }
          />
          Account active
        </label>
        <div className="flex flex-wrap items-center gap-3 md:col-span-2">
          <button
            type="button"
            onClick={handleSaveProfile}
            disabled={savingProfile}
            className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
          >
            {savingProfile ? "Saving..." : "Save details"}
          </button>
          {!user.emailConfirmed && (
            <button
              type="button"
              onClick={handleResendActivation}
              disabled={resending}
              className="rounded-md border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-200 hover:bg-slate-900 disabled:opacity-60"
            >
              {resending ? "Sending..." : "Resend activation"}
            </button>
          )}
          <HelpIcon tooltip="Aktivační email obsahuje token pro potvrzení adresy." />
        </div>
        <div className="md:col-span-2">
          <FormError message={profileError} diagnostics={profileDiagnostics} />
          <FormError message={activationError} diagnostics={activationDiagnostics} />
        </div>
      </div>

      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-white">Roles</h2>
            <p className="text-sm text-slate-400">
              Assign identity roles for access. Roles map to permissions.
            </p>
          </div>
          <button
            type="button"
            onClick={handleSaveRoles}
            disabled={saving}
            className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400 disabled:opacity-60"
          >
            {saving ? "Saving..." : "Save roles"}
          </button>
        </div>
        <div className="mt-2 flex items-center gap-2 text-xs text-slate-400">
          <HelpIcon tooltip="Role != permission. Role mapuje sadu permissionů, které se propisují do claimu permission." />
          Permissions are derived from role mappings.
        </div>
        <div className="mt-4 grid gap-3 md:grid-cols-2">
          {roles.map((role) => (
            <label
              key={role.id}
              className="flex items-center gap-3 rounded-md border border-slate-800 bg-slate-950/40 p-3 text-sm text-slate-200"
            >
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-600 bg-slate-900 text-indigo-400"
                checked={selectedRoles.has(role.name)}
                onChange={() => handleRoleToggle(role.name)}
              />
              {role.name}
            </label>
          ))}
        </div>
        <div className="mt-4">
          <FormError message={rolesError} diagnostics={rolesDiagnostics} />
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6">
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-semibold text-white">Account status</h2>
            <HelpIcon tooltip="Lockout blokuje přihlášení až do odemčení." />
          </div>
          <p className="text-sm text-slate-400">Lock or unlock this user.</p>
          <button
            type="button"
            onClick={handleLockToggle}
            disabled={session?.userId === user.id && !user.isLockedOut}
            className={`mt-4 rounded-md px-4 py-2 text-sm font-semibold ${
              user.isLockedOut
                ? "bg-emerald-500 text-white hover:bg-emerald-400"
                : "bg-rose-500 text-white hover:bg-rose-400"
            } disabled:cursor-not-allowed disabled:opacity-60`}
          >
            {user.isLockedOut ? "Unlock user" : "Lock user"}
          </button>
          {session?.userId === user.id && !user.isLockedOut && (
            <p className="mt-2 text-xs text-rose-300">You cannot lock your own account.</p>
          )}
          <FormError message={lockError} diagnostics={lockDiagnostics} />
        </div>

        <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6">
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-semibold text-white">Reset password</h2>
            <HelpIcon tooltip="Vygeneruje nové heslo. Uživatel by ho měl změnit při dalším přihlášení." />
          </div>
          <p className="text-sm text-slate-400">Set a new password for this user.</p>
          <div className="mt-4 flex flex-col gap-3">
            <input
              type="password"
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              placeholder="New password"
            />
            {passwordError && (
              <div className="flex flex-col gap-2">
                <p className="text-xs text-rose-300">{passwordError}</p>
                <DiagnosticsPanel
                  traceId={passwordDiagnostics?.traceId}
                  errorId={passwordDiagnostics?.errorId}
                  debug={passwordDiagnostics?.debug}
                  compact
                />
              </div>
            )}
            <button
              type="button"
              onClick={handleResetPassword}
              className="rounded-md bg-indigo-500 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-400"
            >
              Reset password
            </button>
          </div>
        </div>
      </div>

      <div className="rounded-lg border border-rose-500/40 bg-rose-500/10 p-6">
        <h2 className="text-lg font-semibold text-rose-100">Deactivate user</h2>
        <p className="text-sm text-rose-200/70">
          Deactivated users cannot log in until reactivated.
        </p>
        <button
          type="button"
          onClick={() => setConfirmDeactivate(true)}
          className="mt-4 rounded-md bg-rose-500 px-4 py-2 text-sm font-semibold text-white hover:bg-rose-400"
        >
          Deactivate user
        </button>
      </div>

      <ConfirmDialog
        title="Deactivate user?"
        description="This will disable sign-in for the user until reactivated."
        confirmLabel="Deactivate"
        isOpen={confirmDeactivate}
        onCancel={() => setConfirmDeactivate(false)}
        onConfirm={handleDeactivate}
      />
    </section>
  );
}
