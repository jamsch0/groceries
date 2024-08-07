import { Application } from "/lib/hotwired/stimulus/dist/stimulus.js";

import ModalController from "./controllers/modal.js";
import SearchFormController from "./controllers/search-form.js";
import TransactionItemFormController from "./controllers/transaction-item-form.js";

const app = Application.start();
app.register("modal", ModalController);
app.register("search-form", SearchFormController);
app.register("transaction-item-form", TransactionItemFormController);

let timeout;
document.addEventListener("turbo:visit", () => {
    clearTimeout(timeout);
});
document.addEventListener("turbo:render", () => {
    clearTimeout(timeout);
    if (document.getElementById("sidebarToggle").checked) {
        timeout = setTimeout(() => document.getElementById("sidebarToggle").checked = false, 500);
    }
});
