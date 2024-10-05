import { Controller } from "/lib/hotwired/stimulus/dist/stimulus.js";

export default class ModalController extends Controller {
    static targets = ["frame"];

    open() {
        if (!this.element.open) {
            this.element.showModal();
        }
    }

    close(event) {
        if (!this.element.open) {
            return;
        }
        if (event.type === "turbo:submit-end" && (event.detail.formSubmission.method === "GET" || !event.detail.success)) {
            // Don't close modal if form method was GET or submission failed
            return;
        }

        event.preventDefault();

        this.element.close();
        this.frameTarget.src = undefined;
        this.frameTarget.innerHTML = "";

        switch (event.type) {
            case "turbo:submit-end":
                Turbo.visit(location.href, { action: "replace" });
                break;
            case "popstate":
                event.stopImmediatePropagation();
                history.go(1);
                break;
        }
    }
}
