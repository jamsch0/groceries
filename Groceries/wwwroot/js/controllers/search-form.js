import { Controller } from "/lib/hotwired/stimulus/dist/stimulus.js";

export default class SearchFormController extends Controller {
    static targets = ["button"];

    #timeoutHandle;

    connect() {
        this.buttonTarget.hidden = true;
    }

    disconnect() {
        clearTimeout(this.#timeoutHandle);
        this.buttonTarget.hidden = false;
    }

    input() {
        clearTimeout(this.#timeoutHandle);
        this.#timeoutHandle = setTimeout(() => this.element.requestSubmit(), 500);
    }
}
