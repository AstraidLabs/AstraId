import { useEffect, useMemo, useState } from "react";
import Alert from "../components/Alert";
import Card from "../components/Card";
import {
  confirmMfaSetup,
  disableMfa,
  getMfaStatus,
  regenerateRecoveryCodes,
  startMfaSetup,
  type MfaRecoveryCodesResponse,
  type MfaSetupResponse,
  type MfaStatus
} from "../api/authServer";
import { useAuthSession } from "../auth/useAuthSession";

const formatCodes = (codes: string[]) => codes.join("\n");

const AccountSecurity = () => {
  const { session, refresh } = useAuthSession();
  const [status, setStatus] = useState<MfaStatus | null>(null);
  const [setupData, setSetupData] = useState<MfaSetupResponse | null>(null);
  const [qrCodeSvg, setQrCodeSvg] = useState<string | null>(null);
  const [setupCode, setSetupCode] = useState("");
  const [disableCode, setDisableCode] = useState("");
  const [codes, setCodes] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");
  const [isWorking, setIsWorking] = useState(false);

  const isAuthenticated = session?.isAuthenticated ?? false;

  const codesText = useMemo(
    () => (codes.length ? formatCodes(codes) : ""),
    [codes]
  );

  const loadStatus = async () => {
    setLoading(true);
    setError("");
    try {
      const data = await getMfaStatus();
      setStatus(data);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Nepodařilo se načíst MFA stav."
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!isAuthenticated) {
      setLoading(false);
      return;
    }
    void loadStatus();
  }, [isAuthenticated]);

  const handleStartSetup = async () => {
    setActionError("");
    setIsWorking(true);
    try {
      const data = await startMfaSetup();
      setSetupData(data);
      setCodes([]);
      setQrCodeSvg(data.qrCodeSvg);
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : "Nepodařilo se spustit MFA setup."
      );
    } finally {
      setIsWorking(false);
    }
  };

  const handleConfirmSetup = async () => {
    if (!setupCode.trim()) {
      setActionError("Zadejte ověřovací kód z aplikace.");
      return;
    }
    setActionError("");
    setIsWorking(true);
    try {
      const response = await confirmMfaSetup({ code: setupCode });
      setCodes(response.recoveryCodes);
      setSetupCode("");
      setSetupData(null);
      setQrCodeSvg(null);
      await loadStatus();
      await refresh();
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : "Nepodařilo se ověřit kód."
      );
    } finally {
      setIsWorking(false);
    }
  };

  const handleRegenerateCodes = async () => {
    setActionError("");
    setIsWorking(true);
    try {
      const response: MfaRecoveryCodesResponse = await regenerateRecoveryCodes();
      setCodes(response.recoveryCodes);
      await loadStatus();
    } catch (err) {
      setActionError(
        err instanceof Error
          ? err.message
          : "Nepodařilo se vygenerovat recovery codes."
      );
    } finally {
      setIsWorking(false);
    }
  };

  const handleDisable = async () => {
    if (!disableCode.trim()) {
      setActionError("Zadejte ověřovací kód.");
      return;
    }
    setActionError("");
    setIsWorking(true);
    try {
      await disableMfa({ code: disableCode });
      setDisableCode("");
      setCodes([]);
      await loadStatus();
      await refresh();
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : "Nepodařilo se vypnout MFA."
      );
    } finally {
      setIsWorking(false);
    }
  };

  if (!isAuthenticated) {
    return (
      <div className="mx-auto max-w-2xl">
        <Card
          title="Zabezpečení účtu"
          description="Pro práci s MFA se musíte přihlásit."
        >
          <Alert variant="info">Přihlaste se a pokračujte v nastavení MFA.</Alert>
        </Card>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="mx-auto max-w-2xl">
        <Card title="Zabezpečení účtu">
          <p className="text-sm text-slate-300">Načítám MFA nastavení...</p>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6">
      <Card
        title="Zabezpečení účtu"
        description="Spravujte MFA pomocí authenticator aplikace a recovery codes."
      >
        {error ? <Alert variant="error">{error}</Alert> : null}
        {status ? (
          <div className="mt-4 grid gap-4 md:grid-cols-3">
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                MFA status
              </p>
              <p className="text-lg font-semibold text-white">
                {status.enabled ? "Aktivní" : "Neaktivní"}
              </p>
            </div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                Recovery codes
              </p>
              <p className="text-lg font-semibold text-white">
                {status.recoveryCodesLeft}
              </p>
              <p className="text-xs text-slate-400">Zbývajících kódů</p>
            </div>
            <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                Authenticator key
              </p>
              <p className="text-lg font-semibold text-white">
                {status.hasAuthenticatorKey ? "Vygenerován" : "Ne"}
              </p>
            </div>
          </div>
        ) : null}
      </Card>

      <Card
        title="Nastavení MFA"
        description="Použijte Authenticator aplikaci (Google Authenticator, Authy, Microsoft Authenticator apod.)."
      >
        {actionError ? <Alert variant="error">{actionError}</Alert> : null}
        {!status?.enabled ? (
          <div className="flex flex-col gap-4">
            <button
              type="button"
              onClick={handleStartSetup}
              disabled={isWorking}
              className="w-fit rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:opacity-60"
            >
              Spustit nastavení MFA
            </button>
            {setupData ? (
              <div className="grid gap-4 md:grid-cols-[200px,1fr]">
                <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-4">
                  {qrCodeSvg ? (
                    <div
                      className="mx-auto h-40 w-40 text-white"
                      aria-label="QR code pro MFA"
                      dangerouslySetInnerHTML={{ __html: qrCodeSvg }}
                    />
                  ) : (
                    <p className="text-xs text-slate-400">Načítám QR…</p>
                  )}
                </div>
                <div className="flex flex-col gap-3 text-sm text-slate-300">
                  <p>
                    Naskenujte QR kód v authenticator aplikaci. Pokud nemůžete
                    skenovat, opište tento klíč:
                  </p>
                  <code className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100">
                    {setupData.sharedKey}
                  </code>
                  <label className="text-sm text-slate-200">
                    Ověřovací kód
                    <input
                      className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
                      type="text"
                      value={setupCode}
                      onChange={(event) => setSetupCode(event.target.value)}
                    />
                  </label>
                  <button
                    type="button"
                    onClick={handleConfirmSetup}
                    disabled={isWorking}
                    className="w-fit rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Potvrdit MFA
                  </button>
                </div>
              </div>
            ) : (
              <p className="text-sm text-slate-400">
                Po spuštění uvidíte QR kód a klíč. MFA se aktivuje po ověření
                kódem z aplikace.
              </p>
            )}
          </div>
        ) : (
          <p className="text-sm text-slate-400">
            MFA je aktivní. Pro změnu konfigurace ji nejprve vypněte.
          </p>
        )}
      </Card>

      <Card
        title="Recovery codes"
        description="Recovery codes použijete, když nemáte přístup k authenticator aplikaci."
      >
        {codes.length ? (
          <div className="flex flex-col gap-3">
            <Alert variant="warning">
              Recovery codes se zobrazují pouze jednou. Uložte si je do bezpečí.
            </Alert>
            <textarea
              className="min-h-[140px] rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-sm text-slate-100"
              value={codesText}
              readOnly
            />
          </div>
        ) : (
          <p className="text-sm text-slate-400">
            Nové recovery codes vygenerujete tlačítkem níže.
          </p>
        )}
        <div className="mt-4">
          <button
            type="button"
            onClick={handleRegenerateCodes}
            disabled={!status?.enabled || isWorking}
            className="rounded-lg border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Vygenerovat nové recovery codes
          </button>
        </div>
      </Card>

      <Card
        title="Vypnutí MFA"
        description="Pro vypnutí MFA ověřte aktuální kód z authenticator aplikace."
      >
        <div className="flex flex-col gap-3">
          <label className="text-sm text-slate-200">
            Ověřovací kód
            <input
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-white"
              type="text"
              value={disableCode}
              onChange={(event) => setDisableCode(event.target.value)}
            />
          </label>
          <button
            type="button"
            onClick={handleDisable}
            disabled={!status?.enabled || isWorking}
            className="w-fit rounded-lg bg-rose-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-rose-400 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Vypnout MFA
          </button>
        </div>
      </Card>
    </div>
  );
};

export default AccountSecurity;
