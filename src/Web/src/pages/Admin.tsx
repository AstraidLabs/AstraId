import Card from "../components/Card";

const Admin = () => {
  return (
    <Card
      title="Administrace"
      description="Administrace je dostupná pouze na AuthServeru."
    >
      <div className="flex flex-col gap-4 text-sm text-slate-300">
        <p>Admin je na 7001.</p>
        <p>
          Admin UI běží na{" "}
          <strong className="text-white">https://localhost:7001/admin</strong>.
        </p>
        <p>Pokud jste administrátor, otevřete jej v nové záložce.</p>
        <div>
          <a
            className="inline-flex rounded-lg bg-slate-800 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:bg-slate-700"
            href="https://localhost:7001/admin"
          >
            Otevřít administraci
          </a>
        </div>
      </div>
    </Card>
  );
};

export default Admin;
