import { Controller } from "/lib/hotwired/stimulus/dist/stimulus.js";

export default class ListFilterController extends Controller {
    static targets = ["option"];

    filter(event) {
        for (const option of this.optionTargets) {
            if (!event.target.value) {
                option.disabled = false;
                continue;
            }

            const value = option.getAttribute("data-list-filter-value");
            option.disabled = value !== event.target.value;
        }
    }
}
