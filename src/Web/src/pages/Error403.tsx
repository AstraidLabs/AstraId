import ErrorPage from "./ErrorPage";

export default function Error403() {
  return (
    <ErrorPage
      title="403"
      description="You don’t have permission to access this page."
      hint="If you believe this is a mistake, contact an administrator."
    />
  );
}
