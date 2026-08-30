/** RF-32 — estado de carregamento explícito. */
export function Skeleton({
  width,
  height = 14,
}: {
  width: number | string;
  height?: number;
}) {
  return <span className="skeleton" style={{ width, height }} aria-hidden="true" />;
}
