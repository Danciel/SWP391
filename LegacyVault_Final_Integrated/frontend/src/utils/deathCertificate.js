// Death-certificate template (Giấy chứng tử) drawn as SVG so the same markup
// is used for the on-screen preview and for the PNG file sent to Legal Verify.
// It is a DEMO template: the footer says so and it carries no legal value.

export const EMPTY_CERTIFICATE = {
  deceasedName: "",
  gender: "Nam",
  dateOfBirth: "",
  nationality: "Việt Nam",
  idNumber: "",
  lastResidence: "",
  dateOfDeath: "",
  timeOfDeath: "",
  placeOfDeath: "",
  causeOfDeath: "",
  registrationNumber: "",
  bookNumber: "",
  registrationDate: "",
  registrarName: "",
  issuingAuthority: "",
};

export const REQUIRED_CERTIFICATE_FIELDS = [
  ["deceasedName", "Full name of the deceased"],
  ["dateOfDeath", "Date of death"],
  ["placeOfDeath", "Place of death"],
  ["causeOfDeath", "Cause of death"],
  ["issuingAuthority", "Issuing authority"],
  ["registrationNumber", "Registration number"],
  ["registrationDate", "Registration date"],
  ["registrarName", "Registrar (signer)"],
];

const esc = (v) =>
  String(v ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");

// yyyy-mm-dd -> dd/mm/yyyy (leave anything else as typed)
export function fmtCertDate(v) {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(v || "");
  return m ? `${m[3]}/${m[2]}/${m[1]}` : v || "";
}

function longDate(v) {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(v || "");
  return m
    ? `ngày ${Number(m[3])} tháng ${Number(m[2])} năm ${m[1]}`
    : "ngày ..... tháng ..... năm .......";
}

function wrap(text, max) {
  const words = String(text || "").split(/\s+/).filter(Boolean);
  const lines = [];
  let cur = "";
  for (const w of words) {
    if ((cur + " " + w).trim().length > max && cur) {
      lines.push(cur);
      cur = w;
    } else {
      cur = (cur + " " + w).trim();
    }
  }
  if (cur) lines.push(cur);
  return lines.slice(0, 2);
}

function star(cx, cy, R, r) {
  const pts = [];
  for (let i = 0; i < 10; i++) {
    const rad = i % 2 === 0 ? R : r;
    const a = (Math.PI / 5) * i - Math.PI / 2;
    pts.push(`${(cx + rad * Math.cos(a)).toFixed(1)},${(cy + rad * Math.sin(a)).toFixed(1)}`);
  }
  return pts.join(" ");
}

export function buildCertificateSvg(input = {}) {
  const d = { ...EMPTY_CERTIFICATE, ...input };
  const W = 800;
  const H = 1130;
  const ink = "#16213e";
  const red = "#b3261e";

  const rows = [
    ["Họ, chữ đệm, tên người chết", "Full name of the deceased", d.deceasedName.toUpperCase(), 40],
    ["Ngày, tháng, năm sinh", "Date of birth", fmtCertDate(d.dateOfBirth), 40],
    ["Giới tính / Quốc tịch", "Sex / Nationality", [d.gender, d.nationality].filter(Boolean).join("  /  "), 40],
    ["Giấy tờ tùy thân", "ID number", d.idNumber, 40],
    ["Nơi cư trú cuối cùng", "Last residence", d.lastResidence, 46],
    ["Đã chết vào lúc", "Time of death", [d.timeOfDeath, fmtCertDate(d.dateOfDeath)].filter(Boolean).join("  ngày  "), 40],
    ["Nơi chết", "Place of death", d.placeOfDeath, 46],
    ["Nguyên nhân chết", "Cause of death", d.causeOfDeath, 46],
    ["Nơi đăng ký", "Registered at", d.issuingAuthority, 46],
  ];

  let y = 408;
  let body = "";
  for (const [vi, en, value, max] of rows) {
    const lines = wrap(value, max);
    const rowH = lines.length > 1 ? 66 : 48;
    body += `<text x="70" y="${y}" font-size="15" fill="${ink}">${esc(vi)}</text>`;
    body += `<text x="70" y="${y + 15}" font-size="10.5" font-style="italic" fill="#667">${esc(en)}</text>`;
    const shown = lines.length ? lines : [""];
    shown.forEach((ln, i) => {
      const ly = y + i * 24;
      body += `<text x="300" y="${ly}" font-size="17" font-weight="700" fill="${ink}">${esc(ln)}</text>`;
      body += `<line x1="300" y1="${ly + 6}" x2="735" y2="${ly + 6}" stroke="#889" stroke-width="0.8" stroke-dasharray="1.5 3"/>`;
    });
    y += rowH;
  }

  const authority = d.issuingAuthority || "ỦY BAN NHÂN DÂN";
  const ringText = esc(`ỦY BAN NHÂN DÂN ★ ${authority.replace(/^ủy ban nhân dân\s*/i, "").toUpperCase()} ★`);

  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" width="${W}" height="${H}" font-family="'Times New Roman', Times, serif">
  <rect width="${W}" height="${H}" fill="#fffdf7"/>
  <rect x="22" y="22" width="${W - 44}" height="${H - 44}" fill="none" stroke="${red}" stroke-width="4"/>
  <rect x="34" y="34" width="${W - 68}" height="${H - 68}" fill="none" stroke="#c9a227" stroke-width="1.5"/>
  <rect x="42" y="42" width="${W - 84}" height="${H - 84}" fill="none" stroke="${red}" stroke-width="0.8"/>
  <g fill="${red}"><circle cx="34" cy="34" r="5"/><circle cx="${W - 34}" cy="34" r="5"/><circle cx="34" cy="${H - 34}" r="5"/><circle cx="${W - 34}" cy="${H - 34}" r="5"/></g>

  <text x="${W / 2}" y="100" text-anchor="middle" font-size="19" font-weight="700" fill="${ink}">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</text>
  <text x="${W / 2}" y="127" text-anchor="middle" font-size="17" font-weight="700" fill="${ink}">Độc lập - Tự do - Hạnh phúc</text>
  <line x1="290" y1="136" x2="510" y2="136" stroke="${ink}" stroke-width="1"/>

  <polygon points="${star(W / 2, 190, 22, 9)}" fill="${red}"/>
  <text x="${W / 2}" y="268" text-anchor="middle" font-size="46" font-weight="700" letter-spacing="6" fill="${red}">GIẤY CHỨNG TỬ</text>
  <text x="${W / 2}" y="294" text-anchor="middle" font-size="14" font-style="italic" fill="#667">DEATH CERTIFICATE</text>
  <text x="${W / 2}" y="322" text-anchor="middle" font-size="15" fill="${ink}">(Bản chính)</text>

  <text x="70" y="362" font-size="15" fill="${ink}">Số: <tspan font-weight="700">${esc(d.registrationNumber || ".........")}</tspan>      Quyển số: <tspan font-weight="700">${esc(d.bookNumber || ".........")}</tspan></text>
  <line x1="70" y1="376" x2="730" y2="376" stroke="${red}" stroke-width="1.2"/>

  ${body}

  <text x="70" y="1004" font-size="15" fill="${ink}">Đăng ký ${esc(longDate(d.registrationDate))}</text>
  <text x="560" y="960" text-anchor="middle" font-size="15" font-weight="700" fill="${ink}">TM. ỦY BAN NHÂN DÂN</text>
  <text x="560" y="980" text-anchor="middle" font-size="15" font-weight="700" fill="${ink}">CHỦ TỊCH / NGƯỜI KÝ</text>
  <path d="M500 1034 c18 -34 30 -34 38 -6 c6 18 14 18 26 -18 c8 -22 18 -16 22 4 c4 18 14 14 28 -10" fill="none" stroke="#1d3fbf" stroke-width="2.2" stroke-linecap="round" opacity="0.85"/>
  <text x="560" y="1064" text-anchor="middle" font-size="17" font-weight="700" fill="${ink}">${esc(d.registrarName || "")}</text>

  <g transform="translate(450 1000) rotate(-14)" opacity="0.88" fill="none" stroke="${red}">
    <circle r="84" stroke-width="3.5"/>
    <circle r="76" stroke-width="1"/>
    <circle r="46" stroke-width="1"/>
    <defs><path id="ring" d="M -61 0 A 61 61 0 1 1 61 0 A 61 61 0 1 1 -61 0"/></defs>
    <text font-size="10.5" font-weight="700" fill="${red}" stroke="none" letter-spacing="0.6"><textPath href="#ring" startOffset="0" textLength="372" lengthAdjust="spacingAndGlyphs">${ringText}</textPath></text>
    <polygon points="${star(0, -4, 25, 10)}" fill="${red}" stroke="none"/>
    <text y="30" text-anchor="middle" font-size="9" font-weight="700" fill="${red}" stroke="none">ĐÃ ĐĂNG KÝ</text>
  </g>

  <text x="${W / 2}" y="${H - 50}" text-anchor="middle" font-size="10" fill="#99a">Mẫu minh họa của hệ thống LegacyVault - chỉ dùng cho demo, không có giá trị pháp lý.</text>
</svg>`;
}

// SVG markup -> PNG Blob (what is uploaded to the backend).
export function svgToPngBlob(svg, width = 1240) {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(new Blob([svg], { type: "image/svg+xml;charset=utf-8" }));
    const img = new Image();
    img.onload = () => {
      const scale = width / img.width;
      const canvas = document.createElement("canvas");
      canvas.width = Math.round(img.width * scale);
      canvas.height = Math.round(img.height * scale);
      const ctx = canvas.getContext("2d");
      ctx.fillStyle = "#fffdf7";
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      URL.revokeObjectURL(url);
      canvas.toBlob(
        (blob) => (blob ? resolve(blob) : reject(new Error("Could not create the certificate image."))),
        "image/png"
      );
    };
    img.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error("Could not draw the certificate."));
    };
    img.src = url;
  });
}

export function validateCertificate(data) {
  for (const [key, label] of REQUIRED_CERTIFICATE_FIELDS) {
    if (!String(data[key] || "").trim()) return `${label} is required.`;
  }
  if (data.dateOfBirth && data.dateOfBirth > data.dateOfDeath) {
    return "Date of birth must be before the date of death.";
  }
  if (data.registrationDate && data.registrationDate < data.dateOfDeath) {
    return "Registration date cannot be before the date of death.";
  }
  return "";
}
