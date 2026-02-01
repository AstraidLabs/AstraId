import { useEffect, useMemo, useState } from "react";
import { useAuth } from "react-oidc-context";
import Card from "../components/Card";
import Alert from "../components/Alert";
import { ApiError } from "../api/http";
import { getMe, type MeResponse } from "../api/endpoints";
import { usePermissions } from "../auth/usePermissions";

const Profile = () => {
  const auth = useAuth();
  const [me, setMe] = useState<MeResponse | null>(null);
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(false);

  const token = auth.user?.access_token;

  const fallbackPermissions = useMemo(
    () => me?.permissions ?? [],
    [me?.permissions]
  );
  const { permissions } = usePermissions(fallbackPermissions);

  useEffect(() => {
    if (!token) {
      setMe(null);
      return;
    }

    let mounted = true;

    const load = async () => {
      setLoading(true);
      setError("");

      try {
        const data = await getMe(token);
        if (mounted) {
          setMe(data);
        }
      } catch (err) {
        if (mounted) {
          if (err instanceof ApiError) {
            setError(err.message);
          } else {
            setError("Nepodařilo se načíst profil.");
          }
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      mounted = false;
    };
  }, [token]);

  const profile = auth.user?.profile;

  return (
    <div className="flex flex-col gap-6">
      <Card
        title="Profil uživatele"
        description="Informace z identity tokenu a /api/me."
      >
        {!auth.isAuthenticated ? (
          <Alert variant="warning">
            Nejste přihlášeni. Přihlaste se pro zobrazení profilu.
          </Alert>
        ) : null}
        {error ? <Alert variant="error">{error}</Alert> : null}

        <dl className="mt-4 grid gap-4 text-sm text-slate-200 sm:grid-cols-3">
          <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
            <dt className="text-slate-400">sub</dt>
            <dd className="mt-1 text-white">{profile?.sub ?? "—"}</dd>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
            <dt className="text-slate-400">name</dt>
            <dd className="mt-1 text-white">
              {profile?.name ?? me?.name ?? "—"}
            </dd>
          </div>
          <div className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
            <dt className="text-slate-400">email</dt>
            <dd className="mt-1 text-white">
              {profile?.email ?? me?.email ?? "—"}
            </dd>
          </div>
        </dl>

        <div className="mt-6">
          <h3 className="text-sm font-semibold text-slate-300">Permissions</h3>
          <div className="mt-3 flex flex-wrap gap-2">
            {permissions.length === 0 ? (
              <span className="text-sm text-slate-500">
                Žádné oprávnění v tokenu ani v /api/me.
              </span>
            ) : (
              permissions.map((permission) => (
                <span
                  key={permission}
                  className="rounded-full border border-indigo-400/40 bg-indigo-500/10 px-3 py-1 text-xs text-indigo-100"
                >
                  {permission}
                </span>
              ))
            )}
          </div>
        </div>

        <p className="mt-6 text-sm text-slate-400">
          {loading ? "Načítání dat z API..." : ""}
        </p>
      </Card>
    </div>
  );
};

export default Profile;
