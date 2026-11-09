// Real photographs from Wikimedia Commons (public domain / CC BY / CC BY-SA).
// Swap any file name here to change the picture site-wide.
const commons = (file, width = 1400) =>
  `https://commons.wikimedia.org/wiki/Special:FilePath/${encodeURIComponent(file.replace(/ /g, "_"))}?width=${width}`;

export const PHOTOS = {
  vaultDoor: commons("Hall's Safe & Lock Co Vault Door.jpg", 1200),
  federalVault: commons("Cleveland FRB Vault Door.jpg", 1400),
  depositBoxes: commons("Safeaccounts.jpg", 1200),
  depositVault: commons("Safe deposit vault 1908.jpg", 1600),
  willShakespeare: commons("Shakespeare-Testament.jpg", 900),
  willPatterson: commons("James Patterson (1794-1877) last will and testament.jpg", 1400),
  will1755: commons("Last Will and Testament (1755).jpg", 1600),
  depositRoom: commons("University National Bank building - door to safe deposit room 02.jpg", 1400),
};

export const PHOTO_CREDIT = "Photos: Wikimedia Commons (public domain / CC BY / CC BY-SA)";

// Dark-green wash laid over every photo so text stays readable.
export const WASH = "linear-gradient(100deg, rgba(14,44,38,0.92) 0%, rgba(21,59,51,0.72) 55%, rgba(21,59,51,0.35) 100%)";
