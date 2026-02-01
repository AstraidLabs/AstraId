import { Route, Routes } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import TopNav from "./components/TopNav";
import Container from "./components/Container";
import Home from "./pages/Home";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Callback from "./pages/Callback";
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
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/auth/callback" element={<Callback />} />
            <Route path="*" element={<NotFound />} />
          </Routes>
        </main>
      </Container>
    </div>
  );
};

export default App;
