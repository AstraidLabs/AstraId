import { useEffect } from "react";

type DocumentMetaOptions = {
  title: string;
  description: string;
};

const DESCRIPTION_META_SELECTOR = 'meta[name="description"]';

const useDocumentMeta = ({ title, description }: DocumentMetaOptions) => {
  useEffect(() => {
    document.title = title;

    let meta = document.querySelector<HTMLMetaElement>(DESCRIPTION_META_SELECTOR);

    if (!meta) {
      meta = document.createElement("meta");
      meta.name = "description";
      document.head.appendChild(meta);
    }

    meta.content = description;
  }, [description, title]);
};

export default useDocumentMeta;
