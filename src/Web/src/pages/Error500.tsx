import ErrorPage from "./ErrorPage";

export default function Error500() {
  return (
    <ErrorPage
      title="500"
      description="Something went wrong on our side. Please try again."
      hint="If the issue persists, contact support and share the trace ID."
    />
  );
}
