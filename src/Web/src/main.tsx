import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import "./index.css";
import { routerBase } from "./routing";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter basename={routerBase === "/" ? undefined : routerBase}>
      <App />
    </BrowserRouter>
  </React.StrictMode>
);
