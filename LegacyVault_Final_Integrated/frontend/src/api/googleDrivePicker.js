// Google Picker requires a browser OAuth access token. The backend releases a
// short-lived token to the authenticated Owner for an unconsumed OAuth ticket.
// Do not persist, log, or send this token to your own storage.
let pickerLoader = null;

function loadGooglePicker() {
  if (window.google?.picker?.PickerBuilder) return Promise.resolve();
  if (pickerLoader) return pickerLoader;

  pickerLoader = new Promise((resolve, reject) => {
    const script = document.createElement("script");
    let timer;
    const fail = () => {
      clearTimeout(timer);
      script.remove();
      pickerLoader = null;
      reject(new Error("Could not load Google Picker. Check the browser connection."));
    };
    script.src = "https://apis.google.com/js/api.js";
    script.async = true;
    script.onload = () => {
      if (!window.gapi?.load) return fail();
      window.gapi.load("picker", {
        callback: () => {
          clearTimeout(timer);
          if (window.google?.picker?.PickerBuilder) resolve();
          else fail();
        },
        onerror: fail,
        timeout: 15000,
        ontimeout: fail,
      });
    };
    script.onerror = fail;
    timer = setTimeout(fail, 20000);
    document.head.appendChild(script);
  });
  return pickerLoader;
}

// Resolves only when the user explicitly selects a file in Google's dialog.
export async function pickGoogleDriveFile({ accessToken, apiKey, appId }) {
  if (!accessToken || !apiKey || !appId) {
    throw new Error("Missing Google Picker configuration.");
  }

  await loadGooglePicker();
  const gp = window.google.picker;

  return new Promise((resolve, reject) => {
    let picker;
    let settled = false;
    let expiry;

    const finish = (error, value) => {
      if (settled) return;
      settled = true;
      clearTimeout(expiry);
      try { picker?.setVisible(false); } catch { /* picker already closed */ }
      if (error) reject(error);
      else resolve(value);
    };

    try {
      // LIST mode is recommended for the narrower `drive.file` scope.
      const view = new gp.DocsView(gp.ViewId.DOCS);
      view.setMode(gp.DocsViewMode.LIST);

      picker = new gp.PickerBuilder()
        .addView(view)
        .setTitle("Select a file to verify in LegacyVault")
        .setOAuthToken(accessToken)
        .setDeveloperKey(apiKey)
        .setAppId(String(appId))
        .setOrigin(window.location.origin)
        .setMaxItems(1)
        .setCallback((data) => {
          if (data.action === gp.Action.CANCEL) {
            finish(new Error("Google Drive selection was cancelled."));
            return;
          }
          if (data.action !== gp.Action.PICKED) return;
          const doc = data[gp.Response.DOCUMENTS]?.[0];
          const id = doc?.[gp.Document.ID];
          if (typeof id !== "string" || !/^[A-Za-z0-9_-]{1,512}$/.test(id)) {
            finish(new Error("Google Picker did not return a valid file."));
            return;
          }
          finish(null, { id, name: doc?.[gp.Document.NAME] || "Google Drive file" });
        })
        .build();

      expiry = setTimeout(() => finish(new Error("Google Drive selection expired. Try again.")), 9 * 60 * 1000);
      picker.setVisible(true);
    } catch {
      finish(new Error("Could not open Google Picker. Verify the API key and project number."));
    }
  });
}
