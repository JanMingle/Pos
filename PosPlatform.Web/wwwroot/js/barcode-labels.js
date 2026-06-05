window.barcodeLabels = {
    print: function (labels, currencySymbol) {
        if (!labels || labels.length === 0) {
            alert("No labels selected.");
            return;
        }

        const expandedLabels = [];

        labels.forEach(label => {
            const qty = Number(label.quantity ?? label.Quantity ?? 1);
            const safeQty = qty > 0 ? qty : 1;

            for (let i = 0; i < safeQty; i++) {
                expandedLabels.push({
                    productName: label.productName ?? label.ProductName ?? "",
                    sku: label.sku ?? label.SKU ?? "",
                    barcode: label.barcode ?? label.Barcode ?? "",
                    code: label.code ?? label.Code ?? "",
                    sellingPrice: Number(label.sellingPrice ?? label.SellingPrice ?? 0)
                });
            }
        });

        if (expandedLabels.length === 0) {
            alert("No labels selected.");
            return;
        }

        const printWindow = window.open("", "_blank", "width=1100,height=800");

        if (!printWindow) {
            alert("Popup blocked. Please allow popups for this site and try again.");
            return;
        }

        const escapeHtml = (value) => {
            return String(value ?? "")
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#039;");
        };

        const labelHtml = expandedLabels.map((label, index) => {
            const code = label.code || label.barcode || label.sku;
            const price = `${escapeHtml(currencySymbol || "R")} ${label.sellingPrice.toFixed(2)}`;

            return `
                <div class="label-card">
                    <div class="label-name">${escapeHtml(label.productName)}</div>
                    <svg id="barcode-${index}" class="barcode-svg"></svg>
                    <div class="label-code">${escapeHtml(code)}</div>
                    <div class="label-footer">
                        <span>${escapeHtml(label.sku)}</span>
                        <strong>${price}</strong>
                    </div>
                </div>
            `;
        }).join("");

        const dataJson = JSON.stringify(expandedLabels);

        printWindow.document.open();
        printWindow.document.write(`
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Barcode Labels</title>
    <style>
        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            padding: 10mm;
            background: #ffffff;
            color: #111827;
            font-family: Arial, Helvetica, sans-serif;
        }

        .label-sheet {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 5mm;
        }

        .label-card {
            width: 100%;
            min-height: 38mm;
            padding: 4mm;
            border: 1px solid #d1d5db;
            border-radius: 3mm;
            page-break-inside: avoid;
            overflow: hidden;
        }

        .label-name {
            height: 11mm;
            overflow: hidden;
            color: #111827;
            font-size: 10px;
            font-weight: 700;
            line-height: 1.2;
            text-align: center;
        }

        .barcode-svg {
            width: 100%;
            height: 16mm;
            display: block;
            margin-top: 1mm;
        }

        .label-code {
            margin-top: 1mm;
            color: #374151;
            font-size: 9px;
            font-weight: 700;
            text-align: center;
            letter-spacing: .04em;
            overflow: hidden;
            white-space: nowrap;
            text-overflow: ellipsis;
        }

        .label-footer {
            margin-top: 2mm;
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 4px;
            color: #111827;
            font-size: 9px;
        }

        .label-footer span {
            color: #6b7280;
            overflow: hidden;
            white-space: nowrap;
            text-overflow: ellipsis;
        }

        .label-footer strong {
            white-space: nowrap;
            font-size: 10px;
        }

        @page {
            size: A4;
            margin: 8mm;
        }

        @media print {
            body {
                padding: 0;
            }

            .label-card {
                border-color: #000000;
            }
        }
    </style>
</head>
<body>
    <div class="label-sheet">
        ${labelHtml}
    </div>

    <script src="https://cdn.jsdelivr.net/npm/jsbarcode@3.11.6/dist/JsBarcode.all.min.js"><\/script>
    <script>
        const labels = ${dataJson};

        window.onload = function () {
            labels.forEach(function (label, index) {
                const code = label.code || label.barcode || label.sku;

                if (!code) {
                    return;
                }

                JsBarcode("#barcode-" + index, code, {
                    format: "CODE128",
                    displayValue: false,
                    height: 42,
                    width: 1.45,
                    margin: 0
                });
            });

            setTimeout(function () {
                window.focus();
                window.print();
            }, 500);
        };
    <\/script>
</body>
</html>
        `);
        printWindow.document.close();
    }
};