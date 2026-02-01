import Card from "../components/Card";

const NotFound = () => (
  <Card title="404" description="Stránka nebyla nalezena.">
    <p className="text-sm text-slate-300">
      Zkontrolujte URL nebo použijte navigaci v horním menu.
    </p>
  </Card>
);

export default NotFound;
