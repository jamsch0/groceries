import { Controller } from "/lib/hotwired/stimulus/dist/stimulus.js";

export default class TransactionItemFormController extends Controller {
    static targets = ["barcodeButton", "barcodeData", "barcodeFormat", "barcodeFormField", "brand", "option"];

    #scanning = false;
    #scanIntervalId;
    #stream;

    connect() {
        if ('BarcodeDetector' in globalThis) {
            this.barcodeFormFieldTarget.hidden = false;
            if (this.barcodeDataTarget.value) {
                this.brandTarget.autofocus = false;
            }
        }
    }

    disconnect() {
        this.barcodeFormFieldTarget.hidden = true;
        this.brandTarget.autofocus = true;
        this.stopScanning();
    }

    filterNames(event) {
        for (const option of this.optionTargets) {
            if (!event.target.value) {
                option.disabled = false;
                continue;
            }

            const value = option.getAttribute("data-brand");
            option.disabled = value !== event.target.value;
        }
    }

    setPriceAndQuantity(event) {
        const { brand, name, price, quantity, unit } = event.target.form.elements;

        if (!brand.value || !name.value) {
            price.value = "";
            quantity.value = !unit.value ? "1" : "";
            return;
        }

        const option = this.optionTargets.find(option =>
            option.getAttribute("data-brand") === brand.value &&
            option.value === name.value);

        if (option != null) {
            if (!price.value) {
                price.value = option.getAttribute("data-price");
            }
            if (quantity.value || (!unit.value && quantity.value === "1")) {
                quantity.value = option.getAttribute("data-quantity") || (!unit.value ? "1" : "");
            }
        }
    }

    async scanBarcode(event) {
        event?.preventDefault();

        if (this.#scanning) {
            return;
        }

        this.#scanning = true;
        this.barcodeDataTarget.value = "";
        this.barcodeFormatTarget.value = "";

        this.#stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" } });

        const video = document.createElement("video");
        video.srcObject = this.#stream;
        await video.play();

        const detector = new BarcodeDetector();

        this.#scanIntervalId = setInterval(async () => {
            const barcodes = await detector.detect(video);
            if (barcodes.length === 0) {
                return;
            }

            const barcode = barcodes[0];
            this.barcodeDataTarget.value = barcode.rawValue;
            this.barcodeFormatTarget.value = barcode.format;

            this.stopScanning();

            const form = this.element.closest("form");
            for (const element of form.elements) {
                element.disabled = !element.name.startsWith("barcode");
            }

            form.requestSubmit(this.barcodeButtonTarget);
        }, 250);
    }

    stopScanning() {
        if (this.#scanning) {
            this.#stream?.getTracks()
                .forEach(track => track.stop());

            clearInterval(this.#scanIntervalId);
            this.#scanning = false;
        }
    }
}
