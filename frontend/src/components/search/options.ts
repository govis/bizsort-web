import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';

export interface OptionValue {
    value: number;
    text: string;
    selected?: boolean;
}

export class SearchOptions extends LitElement {
    static properties = {
        values: { type: Array }
    };

    declare values: OptionValue[];

    constructor() {
        super();
        this.values = [];
    }

    private _toggle(item: OptionValue) {
        item.selected = !item.selected;
        this.requestUpdate();
        
        // Dispatch event when selection changes
        this.dispatchEvent(new CustomEvent('options-changed', {
            composed: true,
            bubbles: true,
            detail: { values: this.selectedValues() }
        }));
    }

    public selectedValues(): number {
        let total = 0;
        for (const item of this.values) {
            if (item.selected) {
                total += item.value;
            }
        }
        return total;
    }

    static styles = css`
        :host {
            display: flex;
            gap: 12px;
            justify-content: center;
        }

        /* Base styles for the button (elevation) */
        .search-option {
            box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);
            border-radius: var(--wa-border-radius-pill);
        }

        /* Inactive State: Matches legacy --search-home-background with 0.8 opacity */
        .search-option[variant="neutral"] {
            --wa-color-neutral-fill-loud: var(--search-home-background, rgba(83, 109, 254, 0.85));
            --wa-color-neutral-on-loud: var(--text-color-on-primary, #fff);
            --wa-color-neutral-border-loud: transparent;
            opacity: 0.8;
        }

        /* Active State: Matches legacy --paper-indigo-a400 */
        .search-option[variant="brand"] {
            --wa-color-brand-fill-loud: #3d5afe;
            --wa-color-brand-on-loud: var(--text-color-on-primary, #fff);
            --wa-color-brand-border-loud: transparent;
            opacity: 1;
        }
    `;

    render() {
        return html`
            ${this.values.map(item => html`
                <wa-button 
                    class="search-option"
                    variant="${item.selected ? 'brand' : 'neutral'}"
                    size="medium"
                    pill
                    @click="${() => this._toggle(item)}"
                >
                    <wa-icon slot="start" library="system" name="${item.selected ? 'check' : 'minus'}"></wa-icon>
                    ${item.text}
                </wa-button>
            `)}
        `;
    }
}

if (!customElements.get('search-options')) {
    customElements.define('search-options', SearchOptions);
}
