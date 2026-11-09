export const MAX_FILE_BYTES = 2 * 1024 * 1024;

export function formatBytes(n) {
  if (!n && n !== 0) return "";
  if (n < 1024) return `${n} B`;
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
  return `${(n / (1024 * 1024)).toFixed(1)} MB`;
}

export function readAssetFile(file) {
  return new Promise((resolve, reject) => {
    if (file.size > MAX_FILE_BYTES) {
      reject(new Error(`File is too large (${formatBytes(file.size)}). Maximum size is ${formatBytes(MAX_FILE_BYTES)}.`));
      return;
    }
    const reader = new FileReader();
    reader.onerror = () => reject(new Error("Could not read this file."));
    reader.onload = () => {
      const result = String(reader.result || "");
      resolve({
        name: file.name,
        type: file.type || "application/octet-stream",
        size: file.size,
        data: result.slice(result.indexOf(",") + 1),
      });
    };
    reader.readAsDataURL(file);
  });
}

export function assetFileFromSecret(secret) {
  if (!secret?.fileData) return null;
  return {
    name: secret.fileName || "file",
    type: secret.fileType || "application/octet-stream",
    size: Number(secret.fileSize) || 0,
    data: secret.fileData,
  };
}

export function downloadAssetFile(secret) {
  if (!secret?.fileData) return;
  const bytes = Uint8Array.from(atob(secret.fileData), (c) => c.charCodeAt(0));
  const blob = new Blob([bytes], { type: secret.fileType || "application/octet-stream" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = secret.fileName || "file";
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
