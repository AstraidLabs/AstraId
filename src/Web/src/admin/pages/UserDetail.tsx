import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { apiRequest } from "../api/http";
import type { AdminRoleListItem, AdminUserDetail } from "../api/types";
import ConfirmDialog from "../components/ConfirmDialog";
import { pushToast } from "../components/toast";

export default function UserDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [user, setUser] = useState<AdminUserDetail | null>(null);
  const [roles, setRoles] = useState<AdminRoleListItem[]>([]);
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set());
  const [newPassword, setNewPassword] = useState("");
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
    setSaving(true);
    try {
      await apiRequest(`/admin/api/users/${id}/roles`, {
        method: "PUT",
        body: JSON.stringify({ roles: Array.from(selectedRoles) }),
      });
      pushToast({ message: "User roles updated.", tone: "success" });
    } finally {
      setSaving(false);
    }
  };

  const handleSaveProfile = async () => {
    if (!id || !user) {
      return;
    }
    if (!formState.email.trim()) {
      pushToast({ message: "Email is required.", tone: "error" });
      return;
    }
    setSavingProfile(true);
    try {
      await apiRequest(`/admin/api/users/${id}`, {
        method: "PUT",
        body: JSON.stringify({
          email: formState.email.trim(),
          userName: formState.userName.trim() || null,
          phoneNumber: formState.phoneNumber.trim() || null,
          emailConfirmed: formState.emailConfirmed,
          isActive: formState.isActive,
        }),
      });
      setUser({
        ...user,
        email: formState.email.trim(),
        userName: formState.userName.trim() || null,
        phoneNumber: formState.phoneNumber.trim() || null,
        emailConfirmed: formState.emailConfirmed,
        isActive: formState.isActive,
      });
      pushToast({ message: "User updated.", tone: "success" });
    } finally {
      setSavingProfile(false);
    }
  };

  const handleLockToggle = async () => {
    if (!id || !user) {
      return;
    }
    const nextLocked = !user.isLockedOut;
    await apiRequest(`/admin/api/users/${id}/lock`, {
      method: "POST",
      body: JSON.stringify({ locked: nextLocked }),
    });
    setUser({ ...user, isLockedOut: nextLocked });
    pushToast({
      message: nextLocked ? "User locked." : "User unlocked.",
      tone: "success",
    });
  };

  const handleResetPassword = async () => {
    if (!id || !newPassword.trim()) {
      return;
    }
    await apiRequest(`/admin/api/users/${id}/reset-password`, {
      method: "POST",
      body: JSON.stringify({ newPassword }),
    });
    setNewPassword("");
    pushToast({ message: "Password reset.", tone: "success" });
  };

  const handleResendActivation = async () => {
    if (!id || !user) {
      return;
    }
    setResending(true);
    try {
      await apiRequest(`/admin/api/users/${id}/resend-activation`, { method: "POST" });
      pushToast({ message: "Activation email resent.", tone: "success" });
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
        <label className="flex flex-col gap-2 text-sm text-slate-200">
          <span className="text-xs uppercase tracking-wide text-slate-500">Email</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            value={formState.email}
            onChange={(event) =>
              setFormState((current) => ({ ...current, email: event.target.value }))
            }
          />
        </label>
        <label className="flex flex-col gap-2 text-sm text-slate-200">
          <span className="text-xs uppercase tracking-wide text-slate-500">Username</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            value={formState.userName}
            onChange={(event) =>
              setFormState((current) => ({ ...current, userName: event.target.value }))
            }
          />
        </label>
        <label className="flex flex-col gap-2 text-sm text-slate-200">
          <span className="text-xs uppercase tracking-wide text-slate-500">Phone</span>
          <input
            className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            value={formState.phoneNumber}
            onChange={(event) =>
              setFormState((current) => ({ ...current, phoneNumber: event.target.value }))
            }
          />
        </label>
        <div className="flex flex-col gap-2 text-sm text-slate-200">
          <span className="text-xs uppercase tracking-wide text-slate-500">2FA enabled</span>
          <div className="rounded-md border border-slate-800 bg-slate-950/40 px-3 py-2 text-sm text-slate-200">
            {user.twoFactorEnabled ? "Yes" : "No"}
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
        </div>
      </div>

      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold text-white">Roles</h2>
            <p className="text-sm text-slate-400">Assign identity roles for access.</p>
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
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6">
          <h2 className="text-lg font-semibold text-white">Account status</h2>
          <p className="text-sm text-slate-400">Lock or unlock this user.</p>
          <button
            type="button"
            onClick={handleLockToggle}
            className={`mt-4 rounded-md px-4 py-2 text-sm font-semibold ${
              user.isLockedOut
                ? "bg-emerald-500 text-white hover:bg-emerald-400"
                : "bg-rose-500 text-white hover:bg-rose-400"
            }`}
          >
            {user.isLockedOut ? "Unlock user" : "Lock user"}
          </button>
        </div>

        <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-6">
          <h2 className="text-lg font-semibold text-white">Reset password</h2>
          <p className="text-sm text-slate-400">Set a new password for this user.</p>
          <div className="mt-4 flex flex-col gap-3">
            <input
              type="password"
              className="rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              placeholder="New password"
            />
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
