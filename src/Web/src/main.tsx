import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import "./index.css";
import { routerBase } from "./routing";
import { AuthSessionProvider } from "./auth/useAuthSession";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <AuthSessionProvider>
      <BrowserRouter basename={routerBase === "/" ? undefined : routerBase}>
        <App />
      </BrowserRouter>
    </AuthSessionProvider>
  </React.StrictMode>
);
