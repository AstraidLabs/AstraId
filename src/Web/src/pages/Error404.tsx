import ErrorPage from "./ErrorPage";

export default function Error404() {
  return (
    <ErrorPage
      title="404"
      description="The requested resource was not found."
      hint="Check the URL or use the navigation to find what you need."
    />
  );
}
