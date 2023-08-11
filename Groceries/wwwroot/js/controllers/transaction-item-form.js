import { Controller } from "/lib/hotwired/stimulus/dist/stimulus.js";

export default class TransactionItemFormController extends Controller {
    static targets = ["option", "price", "quantity"];

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
        const { brand, name } = event.target.form.elements;
        if (!brand.value || !name.value) {
            this.priceTarget.value = "";
            this.quantityTarget.value = "1";
            return;
        }

        const option = this.optionTargets.find(option =>
            option.getAttribute("data-brand") === brand.value &&
            option.value === name.value);

        if (option != null) {
            if (!this.priceTarget.value) {
                this.priceTarget.value = option.getAttribute("data-price");
            }
            if (!this.quantityTarget.value || this.quantityTarget.value === "1") {
                this.quantityTarget.value = option.getAttribute("data-quantity") || "1";
            }
        }
    }
}
