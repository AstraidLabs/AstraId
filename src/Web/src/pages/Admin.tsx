import { Link } from "react-router-dom";
import Card from "../components/Card";
import { getAdminEntryUrl, isAbsoluteUrl } from "../utils/adminEntry";

const Admin = () => {
  const adminUrl = getAdminEntryUrl();
  const adminIsExternal = isAbsoluteUrl(adminUrl);

  return (
    <Card
      title="Administrace"
      description="Administrace je dostupná pouze na AuthServeru."
    >
      <div className="flex flex-col gap-4 text-sm text-slate-300">
        <p>Admin UI je dostupné na:</p>
        <p>
          <strong className="text-white">{adminUrl}</strong>
        </p>
        <p>Pokud jste administrátor, otevřete jej v nové záložce.</p>
        <div>
          {adminIsExternal ? (
            <a
              className="inline-flex rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700"
              href={adminUrl}
            >
              Otevřít administraci
            </a>
          ) : (
            <Link
              className="inline-flex rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700"
              to={adminUrl}
            >
              Otevřít administraci
            </Link>
          )}
        </div>
      </div>
    </Card>
  );
};

export default Admin;
