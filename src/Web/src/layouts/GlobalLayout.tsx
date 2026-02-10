import { Outlet } from "react-router-dom";
import Container from "../components/Container";
import TopNav from "../components/TopNav";
import useDocumentMeta from "../hooks/useDocumentMeta";

const GlobalLayout = () => {
  useDocumentMeta({
    title: "AstraId | Secure Identity",
    description: "AstraId secure identity portal for sign-in, account management, and access control."
  });

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header>
        <TopNav />
      </header>
      <main>
        <Container>
          <div className="py-10">
            <Outlet />
          </div>
        </Container>
      </main>
      <footer className="border-t border-slate-800/80 bg-slate-950/60 py-4">
        <Container>
          <p className="text-xs text-slate-500">© {new Date().getFullYear()} AstraId</p>
        </Container>
      </footer>
    </div>
  );
};

export default GlobalLayout;
