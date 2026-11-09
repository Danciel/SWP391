const PATHS = {
  assets: "M3 7a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7zm4 5h.01M12 9v6m3-3h-6",
  will: "M7 3h8l4 4v14H7a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2zm8 0v4h4M9 12h6M9 16h6",
  eye: "M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12zm10-3a3 3 0 1 0 0 6 3 3 0 0 0 0-6z",
  list: "M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01",
  wallet: "M3 7a2 2 0 0 1 2-2h13v4M3 7v10a2 2 0 0 0 2 2h14a1 1 0 0 0 1-1v-9a1 1 0 0 0-1-1H5a2 2 0 0 1-2-2zm13 6h.01",
  executor: "M12 3l8 3v6c0 4.5-3.2 8-8 9-4.8-1-8-4.5-8-9V6l8-3zm-3 9l2 2 4-4",
  scale: "M12 3v18M5 21h14M6 7h12M6 7l-3 7a3 3 0 0 0 6 0L6 7zm12 0l-3 7a3 3 0 0 0 6 0l-3-7z",
  admin: "M4 5h16v4H4zM4 11h16v4H4zM4 17h16v2H4zM8 7h.01M8 13h.01",
  shield: "M12 3l8 3v6c0 4.5-3.2 8-8 9-4.8-1-8-4.5-8-9V6l8-3z",
  lock: "M6 11h12v9H6zM8 11V8a4 4 0 0 1 8 0v3",
  clock: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zm0-13v5l3 2",
  bell: "M6 9a6 6 0 0 1 12 0c0 6 2 7 2 7H4s2-1 2-7zm4 10a2 2 0 0 0 4 0",
  check: "M5 12l5 5 9-10",
  user: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zm-8 9a8 8 0 0 1 16 0",
  key: "M15 7a4 4 0 1 1-3.5 6L4 20.5V17h3v-2h2l1.5-1.5A4 4 0 0 1 15 7zm1 1h.01",
};

export default function Icon({ name, size = 18, stroke = 1.8, className, style }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={stroke}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <path d={PATHS[name] || PATHS.shield} />
    </svg>
  );
}
