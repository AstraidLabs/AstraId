type Props = {
  message?: string;
};

export default function LoadingState({ message = "Loading..." }: Props) {
  return <p className="text-sm text-slate-400">{message}</p>;
}
