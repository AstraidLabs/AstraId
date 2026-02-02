export type ToastTone = "error" | "success" | "info";

export type ToastPayload = {
  message: string;
  tone?: ToastTone;
};

const toastTarget = new EventTarget();

export function pushToast(payload: ToastPayload) {
  toastTarget.dispatchEvent(new CustomEvent<ToastPayload>("toast", { detail: payload }));
}

export function subscribeToToasts(handler: (payload: ToastPayload) => void) {
  const listener = (event: Event) => {
    const custom = event as CustomEvent<ToastPayload>;
    handler(custom.detail);
  };
  toastTarget.addEventListener("toast", listener);
  return () => toastTarget.removeEventListener("toast", listener);
}
