const isNonEmptyString = (value: string | undefined): value is string =>
  Boolean(value && value.trim());

export const getAdminEntryUrl = () => {
  const configuredUrl = import.meta.env.VITE_ADMIN_ENTRY_URL;

  if (isNonEmptyString(configuredUrl)) {
    return configuredUrl.trim();
  }

  return "/admin";
};

export const isAbsoluteUrl = (value: string) =>
  /^https?:\/\//i.test(value);
