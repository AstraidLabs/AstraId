import { useEffect } from "react";
import { useAuth } from "react-oidc-context";
import { useNavigate } from "react-router-dom";

const AuthCallback = () => {
  const auth = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    const handleCallback = async () => {
      await auth.signinRedirectCallback();
      navigate("/");
    };

    void handleCallback();
  }, [auth, navigate]);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <div className="mx-auto max-w-2xl px-6 py-12">
        <h1 className="text-2xl font-semibold">Signing you in...</h1>
        <p className="mt-2 text-slate-300">Processing authentication callback.</p>
      </div>
    </div>
  );
};

export default AuthCallback;
