import { Outlet } from "react-router-dom";
import Container from "../components/Container";

const AuthLayout = () => (
  <div className="min-h-screen bg-slate-950 text-slate-100">
    <Container>
      <main className="flex min-h-screen items-center justify-center py-10">
        <Outlet />
      </main>
    </Container>
  </div>
);

export default AuthLayout;
