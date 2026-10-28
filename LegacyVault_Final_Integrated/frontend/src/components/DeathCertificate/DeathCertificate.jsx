import { useMemo } from "react";
import { buildCertificateSvg } from "../../utils/deathCertificate";

// Read-only preview of the certificate (all values are escaped in the util).
export default function DeathCertificate({ data, maxWidth = 520 }) {
  const svg = useMemo(() => buildCertificateSvg(data), [data]);
  return (
    <div
      role="img"
      aria-label="Death certificate preview"
      style={{ width: "100%", maxWidth, lineHeight: 0, boxShadow: "0 2px 14px rgba(0,0,0,.18)" }}
      dangerouslySetInnerHTML={{ __html: svg.replace(/ width="\d+" height="\d+"/, ' width="100%"') }}
    />
  );
}
