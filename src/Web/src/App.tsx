import { Route, Routes } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import TopNav from "./components/TopNav";
import Container from "./components/Container";
import Home from "./pages/Home";
import Profile from "./pages/Profile";
import Admin from "./pages/Admin";
import Callback from "./pages/Callback";
import Integrations from "./pages/Integrations";
import NotFound from "./pages/NotFound";
import Alert from "./components/Alert";

const App = () => {
  const auth = useAuth();

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <TopNav />
      <Container>
        <main className="py-10">
          {auth.error ? (
            <div className="mb-6">
              <Alert variant="error">
                {auth.error.message ?? "Přihlášení selhalo."}
              </Alert>
            </div>
          ) : null}
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/profile" element={<Profile />} />
            <Route path="/admin" element={<Admin />} />
            <Route path="/integrations" element={<Integrations />} />
            <Route path="/auth/callback" element={<Callback />} />
            <Route path="*" element={<NotFound />} />
          </Routes>
        </main>
      </Container>
    </div>
  );
};

export default App;
