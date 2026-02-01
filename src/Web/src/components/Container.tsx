import type { PropsWithChildren } from "react";

const Container = ({ children }: PropsWithChildren) => (
  <div className="mx-auto w-full max-w-6xl px-6">{children}</div>
);

export default Container;
