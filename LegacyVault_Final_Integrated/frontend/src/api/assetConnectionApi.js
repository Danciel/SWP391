
import {
  BASE_URL,
  ApiError,
  getToken,
  request
} from "./client";

// ========================================
// B1. SUPPORTED ASSET PROVIDERS
// ========================================

const PROVIDERS = {
  "Online Account": ["GMAIL", "GITHUB"],
  "Cloud Storage": ["GOOGLE_DRIVE"]
};

const TYPES = {
  "Online Account": "ACCOUNT",
  "Cloud Storage": "CLOUD_STORAGE",
  "Document": "DOCUMENT"
};

// ========================================
// B2. BACKEND URL
// ========================================

function backendOrigin() {
  return new URL(
    import.meta.env.VITE_BACKEND_TARGET ||
      "https://localhost:7015"
  ).origin;
}

// ========================================
// B3. OPEN OAUTH POPUP
// ========================================

function openConnectionPopup() {
  const popup = window.open(
    "about:blank",
    "legacyvault_asset_oauth",
    "width=570,height=720"
  );

  if (!popup) {
    throw new Error(
      "Please allow popups for LegacyVault."
    );
  }

  popup.document.title =
    "Connect account — LegacyVault";

  popup.document.body.textContent =
    "Preparing secure provider sign-in...";

  return popup;
}

// ========================================
// B4. WAIT FOR OAUTH TICKET
// ========================================

function waitForTicket(popup, authorizationUrl) {
  return new Promise((resolve, reject) => {
    const expectedOrigin = backendOrigin();

    let settled = false;
    let poll;
    let timeout;

    function finish(error, ticket) {
      if (settled) return;

      settled = true;

      window.removeEventListener(
        "message",
        onMessage
      );

      clearInterval(poll);
      clearTimeout(timeout);

      if (error) reject(error);
      else resolve(ticket);
    }

    function onMessage(event) {
      if (
        event.origin !== expectedOrigin ||
        event.source !== popup
      ) {
        return;
      }

      // A provider identity mismatch must be displayed on the form,
      // not silently interpreted as a closed popup.
      if (event.data?.type === "LEGACYVAULT_OAUTH_ERROR") {
        const message = typeof event.data.message === "string"
          ? event.data.message
          : "Account verification failed.";
        finish(new Error(message));
        return;
      }

      if (event.data?.type !== "LEGACYVAULT_OAUTH") {
        return;
      }

      const ticket = event.data.ticket;

      if (
        typeof ticket !== "string" ||
        !/^[\w-]{16,200}$/.test(ticket)
      ) {
        finish(
          new Error("Invalid OAuth ticket.")
        );
        return;
      }

      finish(null, ticket);
    }

    poll = setInterval(() => {
      if (popup.closed) {
        finish(
          new Error(
            "The provider sign-in window was closed."
          )
        );
      }
    }, 400);

    timeout = setTimeout(() => {
      finish(
        new Error(
          "Verification expired after 10 minutes."
        )
      );
    }, 10 * 60 * 1000);

    window.addEventListener(
      "message",
      onMessage
    );

    try {
      popup.location.replace(
        authorizationUrl
      );
    } catch {
      finish(
        new Error(
          "Could not open authorization page."
        )
      );
    }
  });
}

// ========================================
// B5. CONNECT ACCOUNT VIA OAUTH
// GMAIL / GITHUB / GOOGLE DRIVE
// ========================================

export async function connectAccountAsset(form) {
  const permitted =
    PROVIDERS[form.type] || [];

  const provider = String(
    form.accountType || ""
  ).toUpperCase();

  if (!permitted.includes(provider)) {
    throw new Error(
      "Unsupported provider."
    );
  }

  if (!form.name?.trim()) {
    throw new Error(
      "Asset name is required."
    );
  }

  // Google Drive: requires an accessible file
  if (
    provider === "GOOGLE_DRIVE" &&
    !form.fileId?.trim()
  ) {
    throw new Error(
      "Google Drive File ID is required."
    );
  }

  // Gmail / GitHub: declared account identity
  const needsIdentifier =
    ["GMAIL", "GITHUB"].includes(provider);

  const expectedIdentifier =
    String(
      form.expectedIdentifier || ""
    ).trim();

  if (
    needsIdentifier &&
    !expectedIdentifier
  ) {
    throw new Error(
      provider === "GMAIL"
        ? "Enter the Google account email address to verify."
        : "Enter your GitHub username or verified email."
    );
  }

  if (provider === "GMAIL" &&
      !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(expectedIdentifier)) {
    throw new Error("Enter a valid Google account email address.");
  }

  // Secure handover data
  const handoverPassword =
    form.handoverPassword || "";

  if (
    needsIdentifier &&
    handoverPassword &&
    !form.handoverConsent
  ) {
    throw new Error(
      "Please confirm authorization to store handover details."
    );
  }

  if (handoverPassword.length > 1024) {
    throw new Error(
      "Handover data exceeds the size limit."
    );
  }

  // Open popup immediately on user interaction
  const popup = openConnectionPopup();

  try {
    // ========================================
    // B5.1. START OAUTH
    // ========================================

    const started = await request(
      `/assets/connections/${provider.toLowerCase()}/start`,
      {
        method: "POST",
        body: {
          name: form.name.trim(),

          frontendOrigin: window.location.origin,

          description:
            form.description || "",

          ...(needsIdentifier
            ? { expectedIdentifier }
            : {}),

          ...(provider === "GOOGLE_DRIVE"
            ? {
                fileId: form.fileId.trim()
              }
            : {})
        }
      }
    );

    if (!started?.authorizationUrl) {
      throw new Error(
        "Backend did not return an authorization URL."
      );
    }

    // ========================================
    // B5.2. VERIFY VIA PROVIDER
    // ========================================

    const ticket = await waitForTicket(
      popup,
      started.authorizationUrl
    );

    // ========================================
    // B5.3. COMPLETE & SAVE ASSET
    // ========================================

    // Password is NOT sent to Google / GitHub.
    // It is sent only to LegacyVault Backend
    // over HTTPS after OAuth succeeds.

    const completed = await request(
      "/assets/connections/complete",
      {
        method: "POST",
        body: {
          ticket,

          handoverPassword:
            needsIdentifier
              ? handoverPassword
              : "",

          handoverConsent:
            !!form.handoverConsent
        }
      }
    );

    // ========================================
    // B5.4. RETURN CREATED ASSET
    // ========================================

    return {
      ...completed,

      id: completed.assetId,

      name: form.name.trim(),

      description:
        form.description || "",

      assetType: TYPES[form.type],

      provider
    };

  } finally {
    if (!popup.closed) {
      popup.close();
    }
  }
}

// ========================================
// B7. FILE REQUEST WITH JWT
// ========================================

async function fileRequest(
  path,
  { method = "GET", body } = {}
) {
  const headers = {};

  const token = getToken();

  if (token) {
    headers.Authorization =
      `Bearer ${token}`;
  }

  let response;

  try {
    response = await fetch(
      `${BASE_URL}${path}`,
      {
        method,
        headers,
        body
      }
    );
  } catch {
    throw new ApiError(
      "Cannot connect to Backend.",
      0,
      null
    );
  }

  if (!response.ok) {
    let payload = null;

    try {
      payload = await response.json();
    } catch {
      // Non-JSON response
    }

    throw new ApiError(
      payload?.message ||
        `HTTP ${response.status}`,
      response.status,
      payload
    );
  }

  return response;
}

// ========================================
// B8. UPLOAD DOCUMENT
// ========================================

export async function uploadDocumentAsset(form) {
  if (!form.name?.trim()) {
    throw new Error(
      "Asset name is required."
    );
  }

  // Main's existing AssetFileField reads files as base64 metadata.
  // Convert it to a File for the existing encrypted /documents/upload endpoint.
  let selectedFile = form.file;
  if (selectedFile && !(selectedFile instanceof File) && selectedFile.data) {
    const raw = String(selectedFile.data).replace(/^data:[^,]*,/, "");
    let bytes;
    try {
      const binary = atob(raw);
      bytes = Uint8Array.from(binary, c => c.charCodeAt(0));
    } catch {
      throw new Error("Invalid uploaded file data.");
    }
    selectedFile = new File([bytes], selectedFile.name || "document.pdf", {
      type: selectedFile.type || "application/octet-stream"
    });
  }
  if (!(selectedFile instanceof File)) {
    throw new Error("Please select a document.");
  }
  if (selectedFile.size > 2 * 1024 * 1024) {
    throw new Error(
      "File must be smaller than 2 MB."
    );
  }

  const data = new FormData();

  data.append(
    "file",
    selectedFile
  );

  data.append(
    "name",
    form.name.trim()
  );

  data.append(
    "description",
    form.description || ""
  );

  const response = await fileRequest(
    "/assets/documents/upload",
    {
      method: "POST",
      body: data
    }
  );

  const result = await response.json();

  return {
    ...result,

    id: result.assetId,

    name: form.name.trim(),

    description:
      form.description || "",

    assetType: "DOCUMENT",

    provider: "LOCAL_DOCUMENT"
  };
}

// ========================================
// B9. DOCUMENT INTEGRITY
// ========================================

export async function checkDocumentIntegrity(id) {
  return request(
    `/assets/documents/${encodeURIComponent(id)}/integrity`
  );
}

// ========================================
// B10. DOWNLOAD DOCUMENT
// ========================================

export async function downloadDocument(id) {
  const response = await fileRequest(
    `/assets/documents/${encodeURIComponent(id)}/download`
  );

  const disposition =
    response.headers.get(
      "content-disposition"
    ) || "";

  const match = disposition.match(
    /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i
  );

  let fileName =
    "legacyvault-document";

  try {
    if (match?.[1]) {
      fileName = decodeURIComponent(
        match[1]
      );
    }
  } catch {
    // Use default filename
  }

  const blob = await response.blob();

  const url =
    URL.createObjectURL(blob);

  try {
    const anchor =
      document.createElement("a");

    anchor.href = url;

    anchor.download = fileName;

    document.body.appendChild(
      anchor
    );

    anchor.click();
    anchor.remove();

  } finally {
    setTimeout(
      () => URL.revokeObjectURL(url),
      30000
    );
  }
}
