import { Outlet } from "react-router-dom";
import Container from "../components/Container";
import TopNav from "../components/TopNav";
import useDocumentMeta from "../hooks/useDocumentMeta";
import { useLanguage } from "../i18n/LanguageProvider";

const GlobalLayout = () => {
  const { t } = useLanguage();
  useDocumentMeta({
    title: t("common.metaTitle"),
    description: t("common.metaDescription")
  });

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header>
        <TopNav />
      </header>
      <main aria-label={t("common.main")}>
        <Container>
          <div className="py-10">
            <Outlet />
          </div>
        </Container>
      </main>
      <footer className="border-t border-slate-800/80 bg-slate-950/60 py-4">
        <Container>
          <p className="text-xs text-slate-500">{t("footer.copyright").replace("{{year}}", String(new Date().getFullYear()))}</p>
        </Container>
      </footer>
    </div>
  );
};

export default GlobalLayout;
