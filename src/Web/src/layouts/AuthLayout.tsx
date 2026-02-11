import { Outlet } from "react-router-dom";
import useDocumentMeta from "../hooks/useDocumentMeta";
import { useLanguage } from "../i18n/LanguageProvider";

const AuthLayout = () => {
  const { t } = useLanguage();
  useDocumentMeta({
    title: t("auth.metaTitle"),
    description: t("auth.metaDescription")
  });

  return (
    <div className="auth-space-bg relative min-h-screen overflow-hidden bg-slate-950 text-slate-100">
      <div className="auth-space-stars" aria-hidden="true" />
      <div className="auth-space-stars auth-space-stars--slow" aria-hidden="true" />
      <div className="auth-space-glow" aria-hidden="true" />
      <main
        className="relative z-10 mx-auto flex min-h-screen w-full max-w-6xl items-center justify-center px-4 py-10 sm:px-6"
        aria-labelledby="auth-layout-title"
      >
        <h1 id="auth-layout-title" className="sr-only">{t("auth.layoutTitle")}</h1>
        <section aria-label={t("auth.layoutSection")} className="w-full max-w-md">
          <Outlet />
        </section>
      </main>
    </div>
  );
};

export default AuthLayout;
