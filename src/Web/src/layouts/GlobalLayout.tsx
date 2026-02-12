import { Outlet } from "react-router-dom";
import Container from "../components/Container";
import GlobalFooter from "../components/GlobalFooter";
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
      <GlobalFooter />
    </div>
  );
};

export default GlobalLayout;
