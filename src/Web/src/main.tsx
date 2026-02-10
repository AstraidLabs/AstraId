import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import "./index.css";
import { routerBase } from "./routing";
import { AuthSessionProvider } from "./auth/useAuthSession";
import { LanguageProvider } from "./i18n/LanguageProvider";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <LanguageProvider>
      <AuthSessionProvider>
        <BrowserRouter basename={routerBase === "/" ? undefined : routerBase}>
          <App />
        </BrowserRouter>
      </AuthSessionProvider>
    </LanguageProvider>
  </React.StrictMode>
);
